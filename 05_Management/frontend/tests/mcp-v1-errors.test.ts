// @vitest-environment node
// V1: D3 domain error envelope, version rules, error priority and metadata omission.
import { afterEach, describe, expect, it } from 'vitest';
import { CatalogReadError } from '../mcp/catalog-errors';
import type { CatalogSnapshot } from '../mcp/catalog-reader';
import {
  ControlledCatalogFile, ERROR_CODES, MAX_CATALOG, catalogBytes, connectHarness, expectEnvelope, expectError, expectSuccess, fileBacked,
  linkedCatalog, makeCatalog, makeRecord, makeSource, makeSystem, osError, type ErrorCode, type ErrorEnvelope, type Harness, type Paging,
  type ToolName,
} from './mcp-fixtures';

const harnesses: Harness[] = [];
async function open(readSnapshot: (signal: AbortSignal) => Promise<CatalogSnapshot>, now?: () => number) {
  const harness = await connectHarness(now ? { readSnapshot, now } : { readSnapshot });
  harnesses.push(harness);
  return harness;
}
afterEach(async () => { await Promise.all(harnesses.splice(0).map(harness => harness.close())); });

const WRONG_HASH = 'f'.repeat(64);
const validArgs: Record<ToolName, Record<string, unknown>> = {
  list_systems: {}, search_records: {}, get_system: { id: 'sys-alpha' }, get_record: { id: 'rec-1' }, get_source: { id: 'src-a' },
};

function failing(code: ErrorCode, details?: Record<string, number | string[]>) {
  let reads = 0;
  return { get reads() { return reads; }, readSnapshot: async () => { reads += 1; throw new CatalogReadError(code as never, details); } };
}

describe('version rules', () => {
  it('VERSION_REQUIRED for offset > 0 without expectedHash, before any I/O, whatever the catalog state', async () => {
    const missing = failing('CATALOG_MISSING');
    const harness = await open(missing.readSnapshot);
    for (const [tool, args] of [['list_systems', { offset: 1 }], ['search_records', { offset: 5, systemId: 'no-such-system' }], ['list_systems', { offset: 100000, query: 'x' }]] as const) {
      const envelope = expectError(tool, await harness.call(tool, args), 'VERSION_REQUIRED');
      expect(envelope.snapshot).toBeNull();
      expect(envelope.error.retryable).toBe(false);
    }
    expect(missing.reads).toBe(0);
    // offset 0 (explicit or default) does not need a hash.
    expectError('list_systems', await harness.call('list_systems', { offset: 0 }), 'CATALOG_MISSING');
    expect(missing.reads).toBe(1);
  });

  it('VERSION_CONFLICT on every tool returns only the requested hash and the current metadata', async () => {
    const catalog = linkedCatalog();
    const harness = await open(fileBacked(catalog).readSnapshot);
    const current = expectSuccess('list_systems', await harness.call('list_systems')).snapshot;
    for (const tool of Object.keys(validArgs) as ToolName[]) {
      const envelope = expectError(tool, await harness.call(tool, { ...validArgs[tool], expectedHash: WRONG_HASH }), 'VERSION_CONFLICT');
      expect(envelope.snapshot).toEqual(current);
      expect(envelope.error.details).toEqual({ expectedHash: WRONG_HASH });
      expect(envelope.error.retryable).toBe(false);
    }
  });

  it('a hash from an earlier page pins later pages and details; after an edit they conflict instead of mixing, without rebuild', async () => {
    const file = new ControlledCatalogFile(catalogBytes(linkedCatalog()));
    const reader = file.reader();
    const harness = await open(signal => reader.readSnapshot(signal));
    const first = expectSuccess<{ items: Array<{ id: string }>; paging: Paging }>('list_systems', await harness.call('list_systems', { limit: 2 }));
    const pinned = first.snapshot.hash;
    expectSuccess('get_system', await harness.call('get_system', { id: 'sys-beta', expectedHash: pinned }));
    const edited = linkedCatalog();
    edited.revision = 'fixture-r2';
    edited.systems.push(makeSystem('sys-0new', { title: '새 시스템' }));
    file.replace(catalogBytes(edited));
    const second = expectError('list_systems', await harness.call('list_systems', { limit: 2, offset: 2, expectedHash: pinned }), 'VERSION_CONFLICT');
    expect(second.snapshot?.revision).toBe('fixture-r2');
    expect(second.snapshot?.hash).not.toBe(pinned);
    expectError('get_system', await harness.call('get_system', { id: 'sys-beta', expectedHash: pinned }), 'VERSION_CONFLICT');
    const fresh = expectSuccess<{ items: Array<{ id: string }>; paging: Paging }>('list_systems', await harness.call('list_systems', { limit: 2 }));
    expect(fresh.snapshot.hash).toBe(second.snapshot?.hash);
    expect(fresh.data.items.map(item => item.id)).toEqual(['sys-0new', 'sys-alpha']);
    expect(fresh.data.paging.total).toBe(first.data.paging.total + 1);
  });
});

describe('D3 decision order', () => {
  // Each case pairs two (or more) faults; the earlier stage must win.
  it('I/O and size faults precede JSON/schema/reference, which precede expectedHash, which precedes ID existence', async () => {
    const cases: Array<{ label: string; build: () => (signal: AbortSignal) => Promise<CatalogSnapshot>; tool: ToolName; args: Record<string, unknown>; code: ErrorCode }> = [
      { label: 'missing beats conflict + unknown id', build: () => failing('CATALOG_MISSING').readSnapshot, tool: 'get_system', args: { id: 'nope', expectedHash: WRONG_HASH }, code: 'CATALOG_MISSING' },
      {
        label: 'unreadable beats conflict + unknown systemId',
        build: () => { const file = new ControlledCatalogFile(catalogBytes(linkedCatalog())); file.failNext('open', osError('EACCES')); return file.reader().readSnapshot; },
        tool: 'search_records', args: { systemId: 'nope', expectedHash: WRONG_HASH }, code: 'CATALOG_UNREADABLE',
      },
      {
        label: 'too large beats invalid JSON',
        build: () => new ControlledCatalogFile(new Uint8Array(MAX_CATALOG + 10).fill(0x7b)).reader().readSnapshot, tool: 'list_systems', args: {}, code: 'CATALOG_TOO_LARGE',
      },
      {
        label: 'changed during read beats invalid JSON',
        build: () => {
          const file = new ControlledCatalogFile(catalogBytes(linkedCatalog()));
          file.onNext('read', () => file.replace(new TextEncoder().encode('{ SENTINEL_BROKEN')));
          return file.reader().readSnapshot;
        },
        tool: 'get_record', args: { id: 'nope', expectedHash: WRONG_HASH }, code: 'CATALOG_CHANGED_DURING_READ',
      },
      { label: 'invalid JSON beats conflict + unknown id', build: () => new ControlledCatalogFile(new TextEncoder().encode('{"x":')).reader().readSnapshot, tool: 'get_source', args: { id: 'nope', expectedHash: WRONG_HASH }, code: 'CATALOG_INVALID' },
      {
        label: 'duplicate IDs beat conflict', build: () => new ControlledCatalogFile(catalogBytes(makeCatalog({ sources: [makeSource('d'), makeSource('d')] }))).reader().readSnapshot,
        tool: 'get_source', args: { id: 'd', expectedHash: WRONG_HASH }, code: 'CATALOG_INVALID',
      },
      {
        label: 'broken reference beats conflict + unknown id', build: () => new ControlledCatalogFile(catalogBytes(makeCatalog({ records: [makeRecord('r', { systemIds: ['ghost'] })] }))).reader().readSnapshot,
        tool: 'get_record', args: { id: 'nope', expectedHash: WRONG_HASH }, code: 'CATALOG_REFERENCE_BROKEN',
      },
      { label: 'conflict beats unknown id', build: () => fileBacked(linkedCatalog()).readSnapshot, tool: 'get_system', args: { id: 'nope', expectedHash: WRONG_HASH }, code: 'VERSION_CONFLICT' },
      { label: 'conflict beats unknown systemId', build: () => fileBacked(linkedCatalog()).readSnapshot, tool: 'search_records', args: { systemId: 'nope', expectedHash: WRONG_HASH }, code: 'VERSION_CONFLICT' },
      {
        label: 'conflict beats oversized detail',
        build: () => fileBacked(makeCatalog({ systems: [makeSystem('big', { behavior: Array.from({ length: 40 }, () => 'b'.repeat(1000)) })] })).readSnapshot,
        tool: 'get_system', args: { id: 'big', expectedHash: WRONG_HASH }, code: 'VERSION_CONFLICT',
      },
      { label: 'unknown systemId beats an empty page past total', build: () => fileBacked(linkedCatalog()).readSnapshot, tool: 'search_records', args: { systemId: 'nope', offset: 0, query: 'zzz' }, code: 'NOT_FOUND' },
    ];
    for (const testCase of cases) {
      const harness = await open(testCase.build());
      const envelope = expectError(testCase.tool, await harness.call(testCase.tool, testCase.args), testCase.code);
      expect(envelope.error.code, testCase.label).toBe(testCase.code);
      if (testCase.code.startsWith('CATALOG_')) expect(envelope.snapshot, testCase.label).toBeNull();
    }
  });

  it('VERSION_REQUIRED precedes a corrupt catalog; offset with the right hash reaches the catalog fault', async () => {
    const backing = { reads: 0 };
    const file = new ControlledCatalogFile(new TextEncoder().encode('{ broken'));
    const reader = file.reader();
    const harness = await open(signal => { backing.reads += 1; return reader.readSnapshot(signal); });
    expectError('search_records', await harness.call('search_records', { offset: 3 }), 'VERSION_REQUIRED');
    expect(backing.reads).toBe(0);
    expectError('search_records', await harness.call('search_records', { offset: 3, expectedHash: WRONG_HASH }), 'CATALOG_INVALID');
    expect(backing.reads).toBe(1);
  });
});

describe('domain error envelope hygiene', () => {
  it('reachable error codes carry fixed bounded messages, the retryable table, no data and no echo of input, paths, OS text or raw JSON', async () => {
    const observed = new Map<ErrorCode, ErrorEnvelope>();
    const record = (tool: ToolName, envelope: ErrorEnvelope) => {
      const json = JSON.stringify(envelope);
      for (const leak of ['SENTINEL', 'C:\\', 'v1-fixture', 'EACCES', 'errno', 'syscall', 'Error:', '    at ']) expect(json.includes(leak), `${envelope.error.code} leaks ${leak}`).toBe(false);
      expect(envelope.error.message.length).toBeLessThanOrEqual(256);
      observed.set(envelope.error.code, envelope);
      void tool;
    };
    const run = async (readSnapshot: (signal: AbortSignal) => Promise<CatalogSnapshot>, tool: ToolName, args: Record<string, unknown>) => {
      const harness = await open(readSnapshot);
      const envelope = expectEnvelope(tool, await harness.call(tool, args));
      if (envelope.ok) throw new Error(`expected an error for ${tool}`);
      record(tool, envelope);
    };
    const accessDenied = new ControlledCatalogFile(catalogBytes(linkedCatalog()));
    accessDenied.failNext('open', osError('EACCES'));
    await run(failing('CATALOG_MISSING').readSnapshot, 'list_systems', {});
    await run(accessDenied.reader().readSnapshot, 'list_systems', {});
    await run(async () => { throw Object.assign(new Error('SENTINEL unexpected C:\\v1-fixture\\x'), { code: 'EACCES' }); }, 'get_system', { id: 'x' });
    await run(new ControlledCatalogFile(new Uint8Array(MAX_CATALOG + 1)).reader().readSnapshot, 'get_record', { id: 'x' });
    await run(new ControlledCatalogFile(new TextEncoder().encode('{"SENTINEL_RAW_JSON": [')).reader().readSnapshot, 'get_source', { id: 'x' });
    await run(new ControlledCatalogFile(catalogBytes(makeCatalog({ systems: [makeSystem('s', { recordIds: ['ghost-record'] })] }))).reader().readSnapshot, 'list_systems', {});
    const changing = new ControlledCatalogFile(catalogBytes(linkedCatalog()));
    changing.onNext('read', () => changing.replace(catalogBytes(makeCatalog({ revision: 'r-other-length' }))));
    await run(changing.reader().readSnapshot, 'search_records', {});
    await run(fileBacked(linkedCatalog()).readSnapshot, 'get_record', { id: 'SENTINEL_UNKNOWN_ID' });
    await run(fileBacked(linkedCatalog()).readSnapshot, 'search_records', { systemId: 'SENTINEL_UNKNOWN_SYSTEM' });
    await run(fileBacked(linkedCatalog()).readSnapshot, 'get_system', { id: 'sys-alpha', expectedHash: WRONG_HASH });
    await run(fileBacked(linkedCatalog()).readSnapshot, 'list_systems', { offset: 1, query: 'SENTINEL_QUERY' });
    await run(fileBacked(makeCatalog({ sources: [makeSource('big', { note: `SENTINEL_BODY${'n'.repeat(20_000)}` })] })).readSnapshot, 'get_source', { id: 'big' });

    // The broken-reference examples are catalog IDs by design; only the sentinel-free form is checked above.
    const broken = observed.get('CATALOG_REFERENCE_BROKEN');
    expect(broken?.error.details).toEqual({ count: 1, examples: expect.any(Array) });
    expect(observed.get('VERSION_CONFLICT')?.error.details).toEqual({ expectedHash: WRONG_HASH });
    expect(observed.get('RESPONSE_TOO_LARGE')?.snapshot).not.toBeNull();
    expect([...observed.keys()].sort()).toEqual(ERROR_CODES.filter(code => !['INVALID_ARGUMENT', 'RATE_LIMITED', 'REQUEST_CANCELLED'].includes(code)).sort());
    for (const [code, envelope] of observed) expect(envelope.error.retryable, code).toBe(['CATALOG_MISSING', 'CATALOG_UNREADABLE', 'CATALOG_CHANGED_DURING_READ'].includes(code));
  });

  it('oversized snapshot metadata is replaced by snapshot:null with details.metadataOmitted on success and error paths', async () => {
    const catalog = linkedCatalog();
    catalog.revision = `r-${'😀'.repeat(5_000)}`;
    catalog.sourceCommit = `"\\${'c'.repeat(3_000)}`;
    const harness = await open(fileBacked(catalog).readSnapshot);
    const cases: Array<[ToolName, Record<string, unknown>, ErrorCode, Record<string, unknown>]> = [
      ['list_systems', {}, 'RESPONSE_TOO_LARGE', { metadataOmitted: true }],
      ['search_records', { query: 'zzz-none' }, 'RESPONSE_TOO_LARGE', { metadataOmitted: true }],
      ['get_system', { id: 'sys-alpha' }, 'RESPONSE_TOO_LARGE', { metadataOmitted: true }],
      ['get_source', { id: 'src-a' }, 'RESPONSE_TOO_LARGE', { metadataOmitted: true }],
      ['get_record', { id: 'missing' }, 'NOT_FOUND', { metadataOmitted: true }],
      ['search_records', { systemId: 'missing' }, 'NOT_FOUND', { metadataOmitted: true }],
      ['get_record', { id: 'rec-1', expectedHash: WRONG_HASH }, 'VERSION_CONFLICT', { expectedHash: WRONG_HASH, metadataOmitted: true }],
    ];
    for (const [tool, args, code, details] of cases) {
      const envelope = expectError(tool, await harness.call(tool, args), code);
      expect(envelope.snapshot).toBeNull();
      expect(envelope.error.details).toEqual(details);
    }
  });

  it('metadata that still fits keeps every result within 16 KiB and the default page still progresses', async () => {
    const catalog = makeCatalog({ asOf: 'a'.repeat(3_500), systems: Array.from({ length: 12 }, (_, index) => makeSystem(`s${String(index).padStart(2, '0')}`)) });
    const harness = await open(fileBacked(catalog).readSnapshot);
    let offset = 0;
    let hash: string | undefined;
    const seen: string[] = [];
    for (let guard = 0; guard < 20; guard += 1) {
      const page = expectSuccess<{ items: Array<{ id: string }>; paging: Paging }>('list_systems', await harness.call('list_systems', { offset, ...(hash ? { expectedHash: hash } : {}) }));
      expect(page.snapshot.asOf).toBe(catalog.asOf);
      hash ??= page.snapshot.hash;
      expect(page.data.items.length).toBeGreaterThan(0);
      seen.push(...page.data.items.map(item => item.id));
      if (page.data.paging.nextOffset === null) break;
      offset = page.data.paging.nextOffset;
    }
    expect(seen).toEqual(catalog.systems.map(item => item.id));
  });
});

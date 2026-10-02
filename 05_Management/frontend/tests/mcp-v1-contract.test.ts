// @vitest-environment node
// V1: five-tool surface, DTO/envelope contract, shared UI search semantics, paging and
// the SDK strict-input refusal channel. Expectations come from goal D2/D3.
import { mkdtemp, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import { afterEach, describe, expect, it } from 'vitest';
import * as catalogQuery from '../electron/catalog-query';
import * as uiCatalog from '../src/recordCatalog';
import type { RecordCatalog } from '../electron/catalog-contract';
import {
  ControlledCatalogFile, TOOL_NAMES, baselineFilterRecords, baselineFilterSystems, catalogBytes, codeUnitCompare, connectHarness,
  expectEnvelope, expectError, expectRefusal, expectSuccess, fileBacked, linkedCatalog, makeCatalog, makeRecord, makeSource, makeSystem,
  refusalObservation, walkPages, type Harness, type Paging,
} from './mcp-fixtures';

const harnesses: Harness[] = [];
async function open(readSnapshot: Parameters<typeof connectHarness>[0]['readSnapshot'], era: 'legacy' | 'modern' = 'legacy') {
  const harness = await connectHarness({ readSnapshot, era });
  harnesses.push(harness);
  return harness;
}
afterEach(async () => { await Promise.all(harnesses.splice(0).map(harness => harness.close())); });

type SystemItem = { id: string; area: string; truncatedFields: string[] } & Record<string, unknown>;
type RecordItem = { id: string; type: string; systemIds: string[]; truncatedFields: string[] } & Record<string, unknown>;
type List<T> = { items: T[]; paging: Paging };

describe('tool surface', () => {
  it.each(['legacy', 'modern'] as const)('%s: exactly five read-only tools with strict D2 input bounds and output schemas, no catalog content', async era => {
    const backing = fileBacked(linkedCatalog());
    const harness = await open(backing.readSnapshot, era);
    const { tools } = await harness.client.listTools();
    expect(tools.map(tool => tool.name).sort()).toEqual([...TOOL_NAMES].sort());
    expect(backing.reads).toBe(0);
    const listing = JSON.stringify(tools);
    for (const fixtureText of ['sys-alpha', 'Alpha 전투', 'rec-1', 'SENTINEL_LOCATOR', 'r-ä']) expect(listing.includes(fixtureText)).toBe(false);
    const byName = Object.fromEntries(tools.map(tool => [tool.name, tool]));
    for (const tool of tools) {
      expect(tool.inputSchema.additionalProperties).toBe(false);
      expect(tool.outputSchema).toBeDefined();
      expect(tool.annotations).toMatchObject({ readOnlyHint: true, destructiveHint: false, openWorldHint: false });
      const properties = tool.inputSchema.properties as Record<string, Record<string, unknown>>;
      expect(properties.expectedHash).toMatchObject({ type: 'string', pattern: '^[a-f0-9]{64}$' });
      // No path/URL/root style arguments on any tool.
      expect(Object.keys(properties).filter(key => /path|url|uri|root|file|locator/i.test(key))).toEqual([]);
    }
    for (const name of ['list_systems', 'search_records'] as const) {
      const properties = byName[name]?.inputSchema.properties as Record<string, Record<string, unknown>>;
      expect(properties.query).toMatchObject({ type: 'string', maxLength: 256 });
      expect(properties.area).toMatchObject({ type: 'string', maxLength: 128 });
      expect(properties.limit).toMatchObject({ type: 'integer', minimum: 1, maximum: 50 });
      expect(properties.offset).toMatchObject({ type: 'integer', minimum: 0, maximum: 100000 });
      expect(byName[name]?.inputSchema.required ?? []).toEqual([]);
    }
    const searchProperties = byName.search_records?.inputSchema.properties as Record<string, Record<string, unknown>>;
    expect(Object.keys(searchProperties).sort()).toEqual(['area', 'expectedHash', 'limit', 'offset', 'query', 'systemId', 'type']);
    expect(searchProperties.type?.enum).toEqual(['변경', '결정', '검증', '계획']);
    expect(searchProperties.systemId).toMatchObject({ type: 'string', minLength: 1, maxLength: 128 });
    expect(Object.keys(byName.list_systems?.inputSchema.properties ?? {}).sort()).toEqual(['area', 'expectedHash', 'limit', 'offset', 'query']);
    for (const name of ['get_system', 'get_record', 'get_source'] as const) {
      const schema = byName[name]?.inputSchema;
      expect(Object.keys(schema?.properties ?? {}).sort()).toEqual(['expectedHash', 'id']);
      expect(schema?.required).toEqual(['id']);
      expect((schema?.properties as Record<string, unknown>).id).toMatchObject({ type: 'string', minLength: 1, maxLength: 128 });
    }
  });
});

describe('envelopes and DTOs', () => {
  it('list/search summaries carry exactly the D2 fields with catalog metadata, identical text and structured JSON', async () => {
    const catalog = linkedCatalog();
    const backing = fileBacked(catalog);
    const harness = await open(backing.readSnapshot);
    const systems = expectSuccess<List<SystemItem>>('list_systems', await harness.call('list_systems'));
    expect(systems.snapshot).toEqual({ hash: expect.stringMatching(/^[a-f0-9]{64}$/), revision: 'r-ä-"quoted"', asOf: '2019-12-31T23:59:59Z', sourceCommit: 'deadbeefdeadbeefdeadbeefdeadbeefdeadbeef' });
    expect(systems.data.items[0]).toEqual({
      id: 'sys-alpha', title: 'Alpha 전투 "엔진"', area: '게임 기반', summary: '타격 판정과 😀 이모지', implementationStatus: '구현',
      integrationStatus: '연결', verificationStatus: '검증', lookupSupported: true, truncatedFields: [],
    });
    const records = expectSuccess<List<RecordItem>>('search_records', await harness.call('search_records', { query: 'alpha' }));
    expect(records.data.items.find(item => item.id === 'rec-2')).toEqual({
      id: 'rec-2', type: '결정', title: '동기화 결정', summary: 'Alpha Beta 공통', status: '완료', systemIds: ['sys-alpha', 'sys-beta'], lookupSupported: true, truncatedFields: [],
    });
    expect(records.snapshot).toEqual(systems.snapshot);
  });

  it('details return every contract field unmodified and drop unknown extra JSON fields', async () => {
    const catalog = linkedCatalog();
    const raw = JSON.parse(JSON.stringify(catalog)) as Record<string, unknown> & { systems: Array<Record<string, unknown>>; records: Array<Record<string, unknown>>; sources: Array<Record<string, unknown>> };
    raw.systems[0] = { ...raw.systems[0], extraField: 'EXTRA_SENTINEL' };
    raw.records[0] = { ...raw.records[0], extraField: 'EXTRA_SENTINEL' };
    raw.sources[0] = { ...raw.sources[0], extraField: 'EXTRA_SENTINEL' };
    raw.extraTop = 'EXTRA_SENTINEL';
    const harness = await open(fileBacked(raw).readSnapshot);
    for (const system of catalog.systems) {
      const outcome = await harness.call('get_system', { id: system.id });
      expect(expectSuccess<{ system: unknown }>('get_system', outcome).data.system).toEqual(system);
      expect(JSON.stringify(outcome.authored).includes('EXTRA_SENTINEL')).toBe(false);
    }
    for (const record of catalog.records) {
      const outcome = await harness.call('get_record', { id: record.id });
      expect(expectSuccess<{ record: unknown }>('get_record', outcome).data.record).toEqual(record);
      expect(JSON.stringify(outcome.authored).includes('EXTRA_SENTINEL')).toBe(false);
    }
    for (const source of catalog.sources) {
      const outcome = await harness.call('get_source', { id: source.id });
      expect(expectSuccess('get_source', outcome).data).toEqual({ source, evidenceRead: false, availabilityVerified: false });
      expect(JSON.stringify(outcome.authored).includes('EXTRA_SENTINEL')).toBe(false);
    }
  });

  it('get_source reports metadata only and never opens the locator file', async () => {
    const root = await mkdtemp(join(tmpdir(), 'dawnholder-v1-locator-'));
    try {
      const locatorPath = join(root, 'evidence.md');
      await writeFile(locatorPath, 'EVIDENCE_BODY_SENTINEL');
      const catalog = makeCatalog({ sources: [makeSource('src-file', { locator: locatorPath, kind: 'local', availability: 'local-only' }), makeSource('src-url', { locator: 'https://example.invalid/x' })] });
      const file = new ControlledCatalogFile(catalogBytes(catalog));
      const reader = file.reader();
      const harness = await open(signal => reader.readSnapshot(signal));
      for (const id of ['src-file', 'src-url']) {
        const outcome = await harness.call('get_source', { id });
        const { data } = expectSuccess<{ source: { locator: string }; evidenceRead: boolean; availabilityVerified: boolean }>('get_source', outcome);
        expect(data.evidenceRead).toBe(false);
        expect(data.availabilityVerified).toBe(false);
        expect(JSON.stringify(outcome.authored).includes('EVIDENCE_BODY_SENTINEL')).toBe(false);
      }
      expect(new Set(file.openedPaths)).toEqual(new Set([file.openedPaths[0]]));
      expect(file.openedPaths).toHaveLength(2);
    } finally {
      expect(resolve(root).startsWith(resolve(tmpdir()))).toBe(true);
      await rm(root, { recursive: true, force: true });
    }
  });

  it('navigates list → system → related records by systemId → record → source on one pinned hash', async () => {
    const catalog = linkedCatalog();
    const harness = await open(fileBacked(catalog).readSnapshot);
    const list = expectSuccess<List<SystemItem>>('list_systems', await harness.call('list_systems', { query: '전투' }));
    expect(list.data.items.map(item => item.id)).toEqual(['sys-alpha']);
    const expectedHash = list.snapshot.hash;
    const system = expectSuccess<{ system: { recordIds: string[]; sourceIds: string[] } }>('get_system', await harness.call('get_system', { id: 'sys-alpha', expectedHash }));
    const related = expectSuccess<List<RecordItem>>('search_records', await harness.call('search_records', { systemId: 'sys-alpha', expectedHash }));
    expect(related.data.items.map(item => item.id)).toEqual(['rec-1', 'rec-2']);
    expect(related.data.items.every(item => item.systemIds.includes('sys-alpha'))).toBe(true);
    const record = expectSuccess<{ record: { sourceIds: string[] } }>('get_record', await harness.call('get_record', { id: 'rec-1', expectedHash }));
    const source = expectSuccess<{ source: { id: string } }>('get_source', await harness.call('get_source', { id: record.data.record.sourceIds[0] ?? '', expectedHash }));
    expect(source.data.source.id).toBe('src-a');
    expect(system.data.system.sourceIds).toEqual(['src-a']);
  });
});

describe('search semantics shared with the UI', () => {
  it('UI and MCP import the very same search functions', () => {
    expect(uiCatalog.matchesQuery).toBe(catalogQuery.matchesQuery);
    expect(uiCatalog.filterSystems).toBe(catalogQuery.filterSystems);
    expect(uiCatalog.filterRecords).toBe(catalogQuery.filterRecords);
  });

  it('shared filters keep the baseline (18c8ca6) UI semantics, and MCP lists equal them in code unit order', async () => {
    const catalog = linkedCatalog();
    const harness = await open(fileBacked(catalog).readSnapshot);
    const queries = ['', '   ', 'alpha', 'ALPHA', 'alpha beta', '  beta   alpha ', '전투', '판정 😀', 'İstanbul', 'istanbul', 'gamma', 'line', '"엔진"', 'zzz-none', 'rec-1', 'sys-'];
    const areas = ['', '게임 기반', '서버 플랫폼', 'Management', '없는 영역'];
    for (const query of queries) {
      for (const area of areas) {
        const expectedSystems = baselineFilterSystems(catalog, query, area).map(item => item.id);
        expect(catalogQuery.filterSystems(catalog, query, area).map(item => item.id)).toEqual(expectedSystems);
        const systems = expectSuccess<List<SystemItem>>('list_systems', await harness.call('list_systems', { query, area, limit: 50 }));
        expect(systems.data.items.map(item => item.id)).toEqual([...expectedSystems].sort(codeUnitCompare));
        for (const type of ['', '변경', '결정', '검증', '계획']) {
          const expectedRecords = baselineFilterRecords(catalog, query, area, type).map(item => item.id);
          expect(catalogQuery.filterRecords(catalog, query, area, type).map(item => item.id)).toEqual(expectedRecords);
          const args = { query, area, limit: 50, ...(type ? { type } : {}) };
          const records = expectSuccess<List<RecordItem>>('search_records', await harness.call('search_records', args));
          expect(records.data.items.map(item => item.id)).toEqual([...expectedRecords].sort(codeUnitCompare));
        }
      }
    }
  });

  it('systemId is an exact AND filter; unknown systemId is NOT_FOUND; unmatched area/query is an empty page', async () => {
    const catalog = linkedCatalog();
    const harness = await open(fileBacked(catalog).readSnapshot);
    const ids = async (args: Record<string, unknown>) => expectSuccess<List<RecordItem>>('search_records', await harness.call('search_records', args)).data.items.map(item => item.id);
    expect(await ids({ systemId: 'sys-beta' })).toEqual(['rec-2', 'rec-3']);
    expect(await ids({ systemId: 'sys-beta', type: '검증' })).toEqual(['rec-3']);
    expect(await ids({ systemId: 'sys-beta', query: 'alpha' })).toEqual(['rec-2']);
    expect(await ids({ systemId: 'sys-beta', area: '게임 기반' })).toEqual(['rec-2']);
    expect(await ids({ systemId: 'sys-gamma', area: '게임 기반' })).toEqual([]);
    expect(await ids({ area: '없는 영역' })).toEqual([]);
    expect(await ids({ query: 'zzz-none' })).toEqual([]);
    // Exact match: prefixes, case variants and padded IDs are other IDs.
    for (const systemId of ['sys-bet', 'SYS-BETA', ' sys-beta', 'sys-beta ']) expectError('search_records', await harness.call('search_records', { systemId }), 'NOT_FOUND');
    const missing = expectError('search_records', await harness.call('search_records', { systemId: 'no-such-system', query: 'zzz-none' }), 'NOT_FOUND');
    expect(missing.snapshot?.revision).toBe(catalog.revision);
  });

  it('orders by UTF-16 code units, not locale or code points', async () => {
    const ids = ['b', 'ä', 'B', 'a10', '\uFF5E', 'a2', '\u{1F600}', 'a', 'Z', '가'];
    const catalog = makeCatalog({ systems: ids.map(id => makeSystem(id)), records: ids.map(id => makeRecord(`r${id}`)) });
    const harness = await open(fileBacked(catalog).readSnapshot);
    const expected = ['B', 'Z', 'a', 'a10', 'a2', 'b', 'ä', '가', '\u{1F600}', '\uFF5E'];
    expect([...ids].sort(codeUnitCompare)).toEqual(expected);
    const systems = expectSuccess<List<SystemItem>>('list_systems', await harness.call('list_systems', { limit: 50 }));
    expect(systems.data.items.map(item => item.id)).toEqual(expected);
    const records = expectSuccess<List<RecordItem>>('search_records', await harness.call('search_records', { limit: 50 }));
    expect(records.data.items.map(item => item.id)).toEqual(expected.map(id => `r${id}`));
  });

  it('paging visits every match exactly once for default, 1, 3, 7 and 50 limits and terminates', async () => {
    const systems = Array.from({ length: 53 }, (_, index) => makeSystem(`sys-${String(index).padStart(3, '0')}`, { area: index % 2 ? '홀수' : '짝수' }));
    const records = Array.from({ length: 41 }, (_, index) => makeRecord(`rec-${String(index).padStart(3, '0')}`, { systemIds: [systems[index % 5]?.id ?? ''], type: index % 3 ? '변경' : '결정' }));
    const catalog = makeCatalog({ systems, records });
    const harness = await open(fileBacked(catalog).readSnapshot);
    for (const limit of [undefined, 1, 3, 7, 50]) {
      const extra = limit === undefined ? {} : { limit };
      const all = await walkPages(harness, 'list_systems', extra);
      expect(all.error).toBeUndefined();
      expect(all.ids).toEqual(systems.map(item => item.id));
      expect(all.pages.every(page => page.paging.total === 53 && page.paging.limit === (limit ?? 10))).toBe(true);
      const odd = await walkPages(harness, 'list_systems', { ...extra, area: '홀수' });
      expect(odd.ids).toEqual(systems.filter(item => item.area === '홀수').map(item => item.id));
      const filtered = await walkPages(harness, 'search_records', { ...extra, type: '결정', systemId: 'sys-000' });
      expect(filtered.ids).toEqual(records.filter(item => item.type === '결정' && item.systemIds.includes('sys-000')).map(item => item.id));
    }
  });

  it('offset at or past total returns an empty page with nextOffset null', async () => {
    const catalog = makeCatalog({ systems: [makeSystem('s1'), makeSystem('s2'), makeSystem('s3')] });
    const harness = await open(fileBacked(catalog).readSnapshot);
    const first = expectSuccess<List<SystemItem>>('list_systems', await harness.call('list_systems', { limit: 2 }));
    expect(first.data.paging).toEqual({ total: 3, offset: 0, limit: 2, returned: 2, nextOffset: 2 });
    for (const offset of [3, 4, 100000]) {
      const page = expectSuccess<List<SystemItem>>('list_systems', await harness.call('list_systems', { offset, expectedHash: first.snapshot.hash }));
      expect(page.data).toEqual({ items: [], paging: { total: 3, offset, limit: 10, returned: 0, nextOffset: null } });
    }
  });

  it('same hash and same conditions give identical server-authored results across instances and both revisions', async () => {
    const catalog = linkedCatalog();
    const first = await open(fileBacked(catalog).readSnapshot, 'legacy');
    const second = await open(fileBacked(catalog).readSnapshot, 'modern');
    const calls: Array<[string, Record<string, unknown>]> = [
      ['list_systems', { query: 'a' }], ['search_records', { area: '게임 기반' }], ['get_system', { id: 'sys-beta' }], ['get_record', { id: 'rec-1' }], ['get_source', { id: 'src-b' }],
    ];
    for (const [name, args] of calls) {
      const a = await first.call(name, args);
      const b = await second.call(name, args);
      expect(a.authored).toEqual(b.authored);
      expectEnvelope(name as typeof TOOL_NAMES[number], a);
      expectEnvelope(name as typeof TOOL_NAMES[number], b);
    }
  });
});

describe('strict input refusal channel', () => {
  const longKey = `C:\\SENTINEL_KEY_${'k'.repeat(30_000)}`;
  const manyKeys = Object.fromEntries(Array.from({ length: 300 }, (_, index) => [`SENTINEL_MANY_${index}`, index]));
  const invalidCalls: Array<[string, string, Record<string, unknown> | unknown[] | string]> = [
    ['unknown path key', 'list_systems', { 'C:\\Users\\SENTINEL_PATH_KEY\\secret.txt': 'SENTINEL_VALUE' }],
    ['unknown url key', 'get_source', { id: 'src-a', url: 'https://SENTINEL_URL.invalid/' }],
    ['path argument on detail', 'get_record', { id: 'rec-1', path: 'C:\\SENTINEL_PATH_VALUE' }],
    ['30k-unit unknown key', 'list_systems', { [longKey]: 1 }],
    ['300 unknown keys', 'search_records', manyKeys],
    ['nested object value', 'list_systems', { query: { SENTINEL_NESTED: 'SENTINEL_NESTED_VALUE' } }],
    ['array value', 'search_records', { type: ['SENTINEL_ARRAY'] }],
    ['query 257 units', 'list_systems', { query: `SENTINEL_Q${'q'.repeat(247)}` }],
    ['query 100k units', 'search_records', { query: `SENTINEL_Q${'q'.repeat(100_000)}` }],
    ['area 129 units', 'list_systems', { area: `SENTINEL_A${'a'.repeat(119)}` }],
    ['id 129 units', 'get_system', { id: `SENTINEL_ID${'i'.repeat(118)}` }],
    ['id 100k units', 'get_record', { id: `SENTINEL_ID${'i'.repeat(100_000)}` }],
    ['empty id', 'get_source', { id: '' }],
    ['missing id', 'get_system', {}],
    ['empty systemId', 'search_records', { systemId: '' }],
    ['empty type', 'search_records', { type: '' }],
    ['invalid type', 'search_records', { type: 'SENTINEL_TYPE' }],
    ['limit 0', 'list_systems', { limit: 0 }],
    ['limit 51', 'list_systems', { limit: 51 }],
    ['limit 1.5', 'search_records', { limit: 1.5 }],
    ['limit string', 'list_systems', { limit: 'SENTINEL_LIMIT' }],
    ['limit null', 'list_systems', { limit: null }],
    ['offset -1', 'list_systems', { offset: -1 }],
    ['offset 100001', 'search_records', { offset: 100001 }],
    ['uppercase hash', 'get_system', { id: 'sys-alpha', expectedHash: 'A'.repeat(64) }],
    ['63-char hash', 'list_systems', { expectedHash: 'a'.repeat(63) }],
    ['sentinel hash', 'get_record', { id: 'rec-1', expectedHash: `SENTINEL_HASH${'0'.repeat(51)}` }],
    ['offset on detail', 'get_record', { id: 'rec-1', offset: 1 }],
    ['systemId on list_systems', 'list_systems', { systemId: 'sys-alpha' }],
    ['arguments array', 'list_systems', ['SENTINEL_ARRAY_ARGS']],
    ['arguments string', 'list_systems', 'SENTINEL_STRING_ARGS'],
  ];

  for (const era of ['legacy', 'modern'] as const) {
    it(`${era}: every malformed input is refused before the handler, without reflection, within 1 KiB`, async () => {
      const backing = fileBacked(linkedCatalog());
      const harness = await open(backing.readSnapshot, era);
      const sizes: Record<string, { channel: string; bytes: number; serverAnswered: boolean }> = {};
      for (const [label, name, args] of invalidCalls) {
        const outcome = await harness.call(name, args);
        sizes[label] = { ...expectRefusal(outcome, ['SENTINEL', 'C:\\', 'secret', 'https://']), serverAnswered: outcome.response !== undefined };
      }
      expect(harness.entered).toEqual([]);
      expect(backing.reads).toBe(0);
      console.info(`[V1-MEASURE] refusal ${era} ${JSON.stringify(sizes)}`);
      expect(Math.max(...Object.values(sizes).map(size => size.bytes))).toBeLessThanOrEqual(1024);
    });

    it(`${era}: boundary values just inside the D2 limits are accepted`, async () => {
      const catalog = makeCatalog({ systems: [makeSystem('s'.repeat(128))], records: [makeRecord('r'.repeat(128), { systemIds: ['s'.repeat(128)] })], sources: [makeSource('o'.repeat(128))] });
      const harness = await open(fileBacked(catalog).readSnapshot, era);
      const first = expectSuccess<List<SystemItem>>('list_systems', await harness.call('list_systems', { query: ' '.repeat(256), area: '', limit: 50, offset: 0 }));
      expect(first.data.items).toHaveLength(1);
      expectSuccess('list_systems', await harness.call('list_systems', { query: 'q'.repeat(256), area: 'a'.repeat(128), limit: 1, offset: 100000, expectedHash: first.snapshot.hash }));
      expectSuccess('search_records', await harness.call('search_records', { systemId: 's'.repeat(128), type: '변경', limit: 50 }));
      expectSuccess('get_system', await harness.call('get_system', { id: 's'.repeat(128) }));
      expectSuccess('get_record', await harness.call('get_record', { id: 'r'.repeat(128), expectedHash: first.snapshot.hash }));
      expectSuccess('get_source', await harness.call('get_source', { id: 'o'.repeat(128) }));
      expect(harness.entered).toHaveLength(6);
    });

    // Goal D3: refusal results of any SDK/handler path stay ≤1 KiB and do not echo input.
    it(`${era}: an unknown tool name is refused without echoing it and within 1 KiB`, async () => {
      const backing = fileBacked(linkedCatalog());
      const harness = await open(backing.readSnapshot, era);
      const observations = [];
      for (const name of ['read_file', `SENTINEL_TOOL_C:\\secret\\${'x'.repeat(5_000)}`]) {
        const outcome = await harness.call(name, {});
        expect(outcome.result?.structuredContent).toBeUndefined();
        const observed = refusalObservation(outcome);
        observations.push({ nameUnits: name.length, channel: observed.channel, bytes: observed.bytes, reflected: observed.json.includes(name) || observed.json.includes(JSON.stringify(name).slice(1, -1)) });
      }
      console.info(`[V1-MEASURE] unknown-tool ${era} ${JSON.stringify(observations)}`);
      expect(harness.entered).toEqual([]);
      expect(backing.reads).toBe(0);
      expect(observations).toEqual(observations.map(item => ({ ...item, bytes: Math.min(item.bytes, 1024), reflected: false })));
    });
  }
});

describe('fixture sanity', () => {
  it('linked fixture satisfies the shared contract validators', async () => {
    const { readCatalog, catalogReferenceErrors } = await import('../electron/catalog-contract');
    const parsed = readCatalog(JSON.parse(JSON.stringify(linkedCatalog()))) as RecordCatalog | null;
    expect(parsed).not.toBeNull();
    expect(catalogReferenceErrors(parsed as RecordCatalog)).toEqual([]);
  });
});

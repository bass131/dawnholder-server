// @vitest-environment node
// Requirement: goal 「만들 것」 4 (MCP reads the v2 index) and index-v2-design.md 「MCP」
// tool table and 스냅샷 rule: v2 previews and detail objects, `snapshot` is `{ hash }` only and
// equals the app version of the same bytes. Expected previews are cut here by definition and
// expected hashes come from node:crypto, never from the product's preview or hash functions.
import { afterAll, afterEach, beforeAll, describe, expect, it } from 'vitest';
import type { RecordCatalog } from '../electron/catalog-contract';
import { createCatalogStore } from '../electron/catalog-store';
import { createCatalogReader, nodeCatalogFileOperations } from '../mcp/catalog-reader';
import {
  RECORD_TOOL_NAMES, catalogBytes, codeUnitCompare, connectHarness, decodedTextHash, expectSuccess, linkedCatalog,
  makeCatalog, makeRecord, makeSource, makeSystem, previewByDefinition, screenFilterRecords, screenFilterSystems,
  sha256, staticSnapshot, type Harness,
} from './mcp-fixtures';
import { createOwnedTempRepository, type OwnedTempRepository } from './record-sources/owned-temp-repository';

let harness: Harness | undefined;
afterEach(async () => { await harness?.close(); harness = undefined; });

async function connectTo(catalog: RecordCatalog) {
  harness = await connectHarness({ readSnapshot: async () => staticSnapshot(catalog) });
  return harness;
}

const byId = (a: { id: string }, b: { id: string }) => codeUnitCompare(a.id, b.id);

function systemPreview(system: RecordCatalog['systems'][number]) {
  const title = previewByDefinition(system.title);
  return { id: system.id, area: system.area, title, lookupSupported: system.id.length <= 128, truncatedFields: title === system.title ? [] : ['title'] };
}
function recordPreview(record: RecordCatalog['records'][number]) {
  const title = previewByDefinition(record.title);
  return {
    id: record.id, type: record.type, title, systemIds: record.systemIds,
    lookupSupported: record.id.length <= 128, truncatedFields: title === record.title ? [] : ['title'],
  };
}

describe('record tool previews (design 「MCP」 tool table)', () => {
  it('list_systems returns { id, area, title, lookupSupported, truncatedFields } previews in ID order', async () => {
    const catalog = linkedCatalog();
    const { data } = expectSuccess<{ items: object[] }>('list_systems', await (await connectTo(catalog)).call('list_systems', { limit: 50 }));
    expect(data.items).toEqual([...catalog.systems].sort(byId).map(systemPreview));
  });

  it('search_records returns { id, type, title, systemIds, lookupSupported, truncatedFields } previews in ID order', async () => {
    const catalog = linkedCatalog();
    const { data } = expectSuccess<{ items: object[] }>('search_records', await (await connectTo(catalog)).call('search_records', { limit: 50 }));
    expect(data.items).toEqual([...catalog.records].sort(byId).map(recordPreview));
  });

  it('cuts preview titles at 80 UTF-16 code units and keeps a surrogate pair whole', async () => {
    const exact = 't'.repeat(80);
    const over = 'o'.repeat(81);
    const pairAtCut = `${'p'.repeat(79)}😀tail`;
    const catalog = makeCatalog({
      systems: [makeSystem('exact', { title: exact }), makeSystem('over', { title: over }), makeSystem('pair', { title: pairAtCut })],
      records: [makeRecord('over', { title: over }), makeRecord('pair', { title: pairAtCut })],
    });
    const server = await connectTo(catalog);
    const systems = expectSuccess<{ items: Array<{ id: string; title: string; truncatedFields: string[] }> }>('list_systems', await server.call('list_systems', {})).data.items;
    const records = expectSuccess<{ items: Array<{ id: string; title: string; truncatedFields: string[] }> }>('search_records', await server.call('search_records', {})).data.items;
    const titleOf = (items: typeof systems, id: string) => items.find(item => item.id === id);

    expect(titleOf(systems, 'exact')).toMatchObject({ title: exact, truncatedFields: [] });
    expect(titleOf(systems, 'over')).toMatchObject({ title: 'o'.repeat(80), truncatedFields: ['title'] });
    expect(titleOf(systems, 'pair')).toMatchObject({ title: 'p'.repeat(79), truncatedFields: ['title'] });
    expect(titleOf(records, 'over')).toMatchObject({ title: 'o'.repeat(80), truncatedFields: ['title'] });
    expect(titleOf(records, 'pair')).toMatchObject({ title: 'p'.repeat(79), truncatedFields: ['title'] });
  });
});

describe('record search keeps parity with the screen search (design 「화면」 검색)', () => {
  const cases: Array<{ label: string; tool: 'list_systems' | 'search_records'; args: Record<string, string> }> = [
    { label: 'system by linked source title', tool: 'list_systems', args: { query: '따옴표' } },
    { label: 'system by area text', tool: 'list_systems', args: { query: '서버 플랫폼' } },
    { label: 'system with area filter', tool: 'list_systems', args: { area: '게임 기반' } },
    { label: 'system by case-insensitive title', tool: 'list_systems', args: { query: 'gamma' } },
    { label: 'record by PR number', tool: 'search_records', args: { query: '#102' } },
    { label: 'record by linked source title', tool: 'search_records', args: { query: 'Beta 전달' } },
    { label: 'record with type filter', tool: 'search_records', args: { type: '변경' } },
    { label: 'record with area filter', tool: 'search_records', args: { area: '서버 플랫폼' } },
  ];
  for (const { label, tool, args } of cases) {
    it(`${tool}: ${label}`, async () => {
      const catalog = linkedCatalog();
      const expected = tool === 'list_systems'
        ? screenFilterSystems(catalog, args.query ?? '', args.area ?? '')
        : screenFilterRecords(catalog, args.query ?? '', args.area ?? '', args.type ?? '');
      expect(expected.length, 'oracle must match something').toBeGreaterThan(0);
      const { data } = expectSuccess<{ items: Array<{ id: string }> }>(tool, await (await connectTo(catalog)).call(tool, { ...args, limit: 50 }));
      expect(data.items.map(item => item.id)).toEqual(expected.sort(byId).map(item => item.id));
    });
  }

  it('search_records systemId keeps only records linked to that system', async () => {
    const catalog = linkedCatalog();
    const { data } = expectSuccess<{ items: Array<{ id: string }> }>('search_records', await (await connectTo(catalog)).call('search_records', { systemId: 'sys-beta' }));
    expect(data.items.map(item => item.id)).toEqual(['rec-2', 'rec-3']);
  });
});

describe('record tool details (design 「MCP」 tool table)', () => {
  it('get_system returns the complete v2 system object', async () => {
    const catalog = linkedCatalog();
    const server = await connectTo(catalog);
    for (const system of catalog.systems) {
      const { data } = expectSuccess<{ system: object }>('get_system', await server.call('get_system', { id: system.id }));
      expect(data.system).toEqual(system);
    }
  });

  it('get_record returns the complete v2 record object including pullRequests', async () => {
    const catalog = linkedCatalog();
    const server = await connectTo(catalog);
    for (const record of catalog.records) {
      const { data } = expectSuccess<{ record: object }>('get_record', await server.call('get_record', { id: record.id }));
      expect(data.record).toEqual(record);
    }
  });

  it('get_source returns { source, evidenceRead: false, availabilityVerified: false } with the v2 source', async () => {
    const catalog = linkedCatalog();
    const server = await connectTo(catalog);
    for (const source of catalog.sources) {
      const { data } = expectSuccess('get_source', await server.call('get_source', { id: source.id }));
      expect(data).toEqual({ source, evidenceRead: false, availabilityVerified: false });
    }
  });
});

describe('record responses carry no narrative, status or old metadata fields (design 「색인 형식」)', () => {
  const removedKeys = [
    'summary', 'responsibility', 'behavior', 'implementationStatus', 'integrationStatus', 'verificationStatus',
    'limitations', 'nextSteps', 'reason', 'status', 'details', 'note', 'revision', 'asOf', 'sourceCommit', 'scopeNote',
  ];
  function keysOf(value: unknown, found = new Set<string>()): Set<string> {
    if (Array.isArray(value)) value.forEach(item => keysOf(item, found));
    else if (value && typeof value === 'object') {
      for (const [key, child] of Object.entries(value)) { found.add(key); keysOf(child, found); }
    }
    return found;
  }

  it('no record tool response contains a removed key anywhere', async () => {
    const catalog = linkedCatalog();
    const server = await connectTo(catalog);
    const calls: Array<[typeof RECORD_TOOL_NAMES[number], Record<string, unknown>]> = [
      ['list_systems', {}], ['search_records', {}], ['get_system', { id: 'sys-alpha' }], ['get_record', { id: 'rec-2' }], ['get_source', { id: 'src-a' }],
    ];
    for (const [tool, args] of calls) {
      const outcome = await server.call(tool, args);
      expectSuccess(tool, outcome);
      const present = [...keysOf(outcome.authored?.structuredContent)].filter(key => removedKeys.includes(key));
      expect(present, tool).toEqual([]);
    }
  });
});

describe('record tool snapshot is { hash } and equals the app version of the same bytes (design 「MCP」 스냅샷)', () => {
  let repository: OwnedTempRepository;
  beforeAll(() => { repository = createOwnedTempRepository(); });
  afterAll(() => repository?.remove());

  const variants: Array<{ label: string; catalog: RecordCatalog; space: number }> = [
    { label: 'linked catalog with Korean, emoji and escapes', catalog: linkedCatalog(), space: 2 },
    { label: 'compact JSON of the same catalog', catalog: linkedCatalog(), space: 0 },
    { label: 'one source with a null section', catalog: makeCatalog({ sources: [makeSource('only', { section: null })] }), space: 2 },
  ];
  for (const [index, { label, catalog, space }] of variants.entries()) {
    it(`every record tool reports the same { hash } as the app store: ${label}`, async () => {
      const bytes = catalogBytes(catalog, space);
      const path = repository.write(`records/catalog-${index}.json`, bytes);
      const expectedHash = decodedTextHash(bytes);
      expect(expectedHash, 'valid UTF-8 without BOM hashes like its bytes').toBe(sha256(bytes));

      const app = await createCatalogStore(path).read();
      expect(app.ok).toBe(true);
      const appVersion = app.ok ? app.version : '';

      const reader = createCatalogReader({ catalogPath: path, fileOperations: nodeCatalogFileOperations });
      harness = await connectHarness({ readSnapshot: signal => reader.readSnapshot(signal) });
      const someSource = catalog.sources[0]?.id ?? 'src-a';
      const calls: Array<[typeof RECORD_TOOL_NAMES[number], Record<string, unknown>]> = [
        ['list_systems', {}], ['search_records', {}], ['get_source', { id: someSource }],
      ];
      if (catalog.systems[0]) calls.push(['get_system', { id: catalog.systems[0].id }]);
      if (catalog.records[0]) calls.push(['get_record', { id: catalog.records[0].id }]);
      for (const [tool, args] of calls) {
        const { snapshot } = expectSuccess(tool, await harness.call(tool, args));
        expect(Object.keys(snapshot), tool).toEqual(['hash']);
        expect(snapshot.hash, tool).toBe(expectedHash);
        expect(snapshot.hash, tool).toBe(appVersion);
      }
    });
  }
});

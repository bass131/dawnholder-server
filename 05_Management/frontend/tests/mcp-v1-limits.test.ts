// @vitest-environment node
// V1: D2 preview truncation, long IDs, Unicode/escaping and the 16 KiB / 8 KiB response
// budgets, including paging around oversized items. Sizes are the server-authored
// `{ content, structuredContent, isError }` JSON; SDK fields and wire overhead are V2.
import { afterEach, describe, expect, it } from 'vitest';
import type { DevelopmentRecord, RecordCatalog, SystemRecord } from '../electron/catalog-contract';
import {
  DEFAULT_PAGE_TARGET, MAX_RESULT, authoredBytes, connectHarness, expectEnvelope, expectError, expectSuccess, fileBacked, hasLoneSurrogate,
  linkedCatalog, makeCatalog, makeRecord, makeSource, makeSystem, walkPages, type Harness, type Paging,
} from './mcp-fixtures';

const harnesses: Harness[] = [];
async function open(catalog: unknown) {
  const harness = await connectHarness({ readSnapshot: fileBacked(catalog).readSnapshot });
  harnesses.push(harness);
  return harness;
}
afterEach(async () => { await Promise.all(harnesses.splice(0).map(harness => harness.close())); });

type Summary = Record<string, unknown> & { id: string; truncatedFields: string[]; lookupSupported: boolean };
type List = { items: Summary[]; paging: Paging };
const pair = '😀';

describe('summary previews', () => {
  it('cut title/status at 80 and summary at 160 UTF-16 units, mark truncatedFields, never split a surrogate pair', async () => {
    const systems: SystemRecord[] = [
      makeSystem('s-exact', { title: 't'.repeat(80), summary: 's'.repeat(160), implementationStatus: 'i'.repeat(80), integrationStatus: 'g'.repeat(80), verificationStatus: 'v'.repeat(80) }),
      makeSystem('s-over', { title: 't'.repeat(81), summary: 's'.repeat(161), implementationStatus: 'i'.repeat(81), integrationStatus: 'g'.repeat(500), verificationStatus: '검'.repeat(81) }),
      makeSystem('s-pair-cut', { title: `${'t'.repeat(79)}${pair}x`, summary: `${'s'.repeat(159)}${pair}`, implementationStatus: `${'i'.repeat(79)}${pair}` }),
      makeSystem('s-pair-fit', { title: `${'t'.repeat(78)}${pair}`, summary: `${'s'.repeat(158)}${pair}` }),
    ];
    const records: DevelopmentRecord[] = [
      makeRecord('r-over', { title: '제'.repeat(200), summary: `${pair.repeat(80)}${pair}`, status: `${'s'.repeat(79)}${pair}` }),
      makeRecord('r-exact', { title: 'x'.repeat(80), summary: 'y'.repeat(160), status: 'z'.repeat(80) }),
    ];
    const harness = await open(makeCatalog({ systems, records }));
    const listed = expectSuccess<List>('list_systems', await harness.call('list_systems', { limit: 50 }));
    const byId = Object.fromEntries(listed.data.items.map(item => [item.id, item]));
    expect(byId['s-exact']).toMatchObject({ title: 't'.repeat(80), summary: 's'.repeat(160), truncatedFields: [] });
    expect(byId['s-over']).toMatchObject({ title: 't'.repeat(80), summary: 's'.repeat(160), implementationStatus: 'i'.repeat(80), integrationStatus: 'g'.repeat(80), verificationStatus: '검'.repeat(80) });
    expect(new Set(byId['s-over']?.truncatedFields)).toEqual(new Set(['title', 'summary', 'implementationStatus', 'integrationStatus', 'verificationStatus']));
    expect(byId['s-pair-cut']).toMatchObject({ title: 't'.repeat(79), summary: 's'.repeat(159), implementationStatus: 'i'.repeat(79) });
    expect(new Set(byId['s-pair-cut']?.truncatedFields)).toEqual(new Set(['title', 'summary', 'implementationStatus']));
    expect(byId['s-pair-fit']).toMatchObject({ title: `${'t'.repeat(78)}${pair}`, summary: `${'s'.repeat(158)}${pair}`, truncatedFields: [] });
    const searched = expectSuccess<List>('search_records', await harness.call('search_records', { limit: 50 }));
    const records2 = Object.fromEntries(searched.data.items.map(item => [item.id, item]));
    expect(records2['r-over']).toMatchObject({ title: '제'.repeat(80), summary: pair.repeat(80), status: 's'.repeat(79) });
    expect(new Set(records2['r-over']?.truncatedFields)).toEqual(new Set(['title', 'summary', 'status']));
    expect(records2['r-exact']?.truncatedFields).toEqual([]);
    for (const item of [...listed.data.items, ...searched.data.items]) {
      for (const value of Object.values(item)) if (typeof value === 'string') expect(hasLoneSurrogate(value)).toBe(false);
    }
  });

  it('never truncates IDs, area, type or linked ID arrays; lookupSupported reflects the 128-unit input limit', async () => {
    const longArea = `영역-${'a'.repeat(400)}`;
    const linked = Array.from({ length: 30 }, (_, index) => `linked-system-${String(index).padStart(2, '0')}-${'l'.repeat(60)}`);
    const systems = [
      makeSystem('y'.repeat(128)), makeSystem('x'.repeat(129)), makeSystem(pair.repeat(64)), makeSystem(pair.repeat(65)),
      makeSystem(`long-${'z'.repeat(1_000)}`, { area: longArea }), ...linked.map(id => makeSystem(id)),
    ];
    const records = [makeRecord(`rec-${'r'.repeat(300)}`, { type: '계획', title: '긴 ID 기록', summary: '연결 배열', systemIds: linked })];
    const harness = await open(makeCatalog({ systems, records }));
    const items: Summary[] = [];
    let offset = 0;
    let expectedHash: string | undefined;
    do {
      const page = expectSuccess<List>('list_systems', await harness.call('list_systems', { limit: 50, offset, ...(expectedHash ? { expectedHash } : {}) }));
      expectedHash ??= page.snapshot.hash;
      items.push(...page.data.items);
      offset = page.data.paging.nextOffset ?? -1;
    } while (offset >= 0);
    expect(items).toHaveLength(35);
    const listed = { data: { items } };
    const support = Object.fromEntries(listed.data.items.map(item => [item.id, item.lookupSupported]));
    expect(support['y'.repeat(128)]).toBe(true);
    expect(support['x'.repeat(129)]).toBe(false);
    expect(support[pair.repeat(64)]).toBe(true);
    expect(support[pair.repeat(65)]).toBe(false);
    const long = listed.data.items.find(item => item.id.startsWith('long-'));
    expect(long).toMatchObject({ id: `long-${'z'.repeat(1_000)}`, area: longArea, lookupSupported: false });
    const record = expectSuccess<List>('search_records', await harness.call('search_records')).data.items[0];
    expect(record).toMatchObject({ id: `rec-${'r'.repeat(300)}`, type: '계획', systemIds: linked, lookupSupported: false, truncatedFields: [] });
    // Known limitation: listed but not detail-addressable through the 128-unit ID input.
    const refused = await harness.call('get_system', { id: 'x'.repeat(129) });
    expect(refused.result?.isError ?? refused.thrown !== undefined).toBe(true);
    expect(harness.entered.filter(name => name === 'get_system')).toHaveLength(0);
  });

  it('Unicode and escaping survive byte-exactly in text and structured content', async () => {
    const catalog = linkedCatalog();
    const harness = await open(catalog);
    for (const record of catalog.records) {
      expect(expectSuccess<{ record: unknown }>('get_record', await harness.call('get_record', { id: record.id })).data.record).toEqual(record);
    }
    for (const source of catalog.sources) {
      expect(expectSuccess<{ source: unknown }>('get_source', await harness.call('get_source', { id: source.id })).data.source).toEqual(source);
    }
    const emoji = expectSuccess<{ system: { id: string } }>('get_system', await harness.call('get_system', { id: 'sys-😀' }));
    expect(emoji.data.system.id).toBe('sys-😀');
  });
});

// Large but valid summaries: area is never truncated, so it scales one summary's size.
function sizedSystem(id: string, areaUnits: number, char = '가'): SystemRecord {
  return makeSystem(id, { area: char.repeat(areaUnits) });
}

describe('response budgets', () => {
  it('default page targets 8 KiB, returns one 8–16 KiB summary alone, and still visits every item once', async () => {
    const systems = [
      ...Array.from({ length: 25 }, (_, index) => sizedSystem(`a${String(index).padStart(2, '0')}`, 200)),
      sizedSystem('b-alone-1', 1_600), sizedSystem('b-alone-2', 2_400),
      ...Array.from({ length: 5 }, (_, index) => sizedSystem(`c${index}`, 10)),
    ];
    const harness = await open(makeCatalog({ systems }));
    const walk = await walkPages(harness, 'list_systems', {});
    expect(walk.error).toBeUndefined();
    expect(walk.ids).toEqual(systems.map(item => item.id).sort());
    for (const page of walk.pages) {
      expect(page.bytes).toBeLessThanOrEqual(MAX_RESULT);
      if (page.returned > 1) expect(page.bytes).toBeLessThanOrEqual(DEFAULT_PAGE_TARGET);
      expect(page.returned).toBeGreaterThan(0);
    }
    const alone = walk.pages.filter(page => page.returned === 1 && page.bytes > DEFAULT_PAGE_TARGET);
    expect(alone.length).toBeGreaterThanOrEqual(2);
    expect(walk.pages.some(page => page.returned < 10 && page.paging.nextOffset !== null)).toBe(true);
    console.info(`[V1-MEASURE] default-page bytes=${JSON.stringify(walk.pages.map(page => [page.returned, page.bytes]))}`);
  });

  it('limit 50 pages stay within 16 KiB, never exceed the requested limit, and visit every item once', async () => {
    const systems = Array.from({ length: 120 }, (_, index) => sizedSystem(`s${String(index).padStart(3, '0')}`, 40 + (index % 7) * 60, index % 2 ? '"' : '가'));
    const records = Array.from({ length: 80 }, (_, index) => makeRecord(`r${String(index).padStart(3, '0')}`, { systemIds: systems.slice(index, index + 12).map(item => item.id) }));
    const harness = await open(makeCatalog({ systems, records }));
    for (const tool of ['list_systems', 'search_records'] as const) {
      const walk = await walkPages(harness, tool, { limit: 50 });
      expect(walk.error).toBeUndefined();
      expect(walk.ids).toEqual((tool === 'list_systems' ? systems : records).map(item => item.id));
      for (const page of walk.pages) {
        expect(page.bytes).toBeLessThanOrEqual(MAX_RESULT);
        expect(page.returned).toBeLessThanOrEqual(50);
      }
      expect(walk.pages.some(page => page.returned < 50 && page.paging.nextOffset !== null)).toBe(true);
      console.info(`[V1-MEASURE] ${tool} limit50 bytes=${JSON.stringify(walk.pages.map(page => [page.returned, page.bytes]))}`);
    }
  });

  it('a summary over 16 KiB is RESPONSE_TOO_LARGE at its own offset; the page before stops short of it and paging resumes after it', async () => {
    const systems = [sizedSystem('a', 10), sizedSystem('b', 10), makeSystem('c-huge', { area: `SENTINEL_AREA${'가'.repeat(6_000)}` }), sizedSystem('d', 10)];
    const harness = await open(makeCatalog({ systems }));
    for (const limit of [undefined, 50]) {
      const first = expectSuccess<List>('list_systems', await harness.call('list_systems', limit ? { limit } : {}));
      expect(first.data.items.map(item => item.id)).toEqual(['a', 'b']);
      expect(first.data.paging).toMatchObject({ total: 4, offset: 0, returned: 2, nextOffset: 2 });
      const blocked = await harness.call('list_systems', { offset: 2, expectedHash: first.snapshot.hash, ...(limit ? { limit } : {}) });
      const envelope = expectError('list_systems', blocked, 'RESPONSE_TOO_LARGE');
      expect(envelope.snapshot?.hash).toBe(first.snapshot.hash);
      expect(JSON.stringify(blocked.authored).includes('SENTINEL_AREA')).toBe(false);
      const after = expectSuccess<List>('list_systems', await harness.call('list_systems', { offset: 3, expectedHash: first.snapshot.hash, ...(limit ? { limit } : {}) }));
      expect(after.data.items.map(item => item.id)).toEqual(['d']);
    }
  });

  it('details up to 16 KiB are whole; larger details are RESPONSE_TOO_LARGE with no partial content', async () => {
    const fits = (count: number) => Array.from({ length: count }, (_, index) => `행동 ${index} ${'b'.repeat(100)}`);
    const catalog: RecordCatalog = makeCatalog({
      systems: [makeSystem('fit', { behavior: fits(45) }), makeSystem('huge', { behavior: [...fits(80), 'SENTINEL_DETAIL_TAIL'] })],
      records: [makeRecord('fit-r', { details: fits(45) }), makeRecord('huge-r', { details: ['SENTINEL_DETAIL_HEAD', ...fits(80)] })],
      sources: [makeSource('fit-s', { note: 'n'.repeat(5_000) }), makeSource('huge-s', { note: `SENTINEL_NOTE${'n'.repeat(9_000)}` })],
    });
    const harness = await open(catalog);
    const checks: Array<['get_system' | 'get_record' | 'get_source', string, unknown]> = [
      ['get_system', 'fit', catalog.systems[0]], ['get_record', 'fit-r', catalog.records[0]], ['get_source', 'fit-s', catalog.sources[0]],
    ];
    for (const [tool, id, expected] of checks) {
      const outcome = await harness.call(tool, { id });
      const data = expectSuccess<Record<string, unknown>>(tool, outcome).data;
      expect(Object.values(data)[0]).toEqual(expected);
      expect(authoredBytes(outcome)).toBeGreaterThan(DEFAULT_PAGE_TARGET);
    }
    for (const [tool, id] of [['get_system', 'huge'], ['get_record', 'huge-r'], ['get_source', 'huge-s']] as const) {
      const outcome = await harness.call(tool, { id });
      const envelope = expectError(tool, outcome, 'RESPONSE_TOO_LARGE');
      expect(envelope.snapshot).not.toBeNull();
      expect(JSON.stringify(outcome.authored).includes('SENTINEL')).toBe(false);
      expect(JSON.stringify(outcome.authored).includes('행동')).toBe(false);
    }
  });

  it('seeded sweep with escaping-heavy text: every result within 16 KiB, multi-item default pages within 8 KiB, complete paging', async () => {
    let seed = 0x5eed;
    const random = () => { seed = (seed * 1_103_515_245 + 12_345) % 2_147_483_648; return seed / 2_147_483_648; };
    const alphabet = ['a', '가', '"', '\\', '\u0001', '\n', pair, ' ', '<', 'é'];
    const textOf = (max: number) => Array.from({ length: Math.floor(random() * max) }, () => alphabet[Math.floor(random() * alphabet.length)]).join('');
    const systems = Array.from({ length: 60 }, (_, index) => makeSystem(`sys-${String(index).padStart(2, '0')}-${textOf(20)}`, {
      title: textOf(200), summary: textOf(400), area: textOf(index % 9 === 0 ? 1_500 : 60), implementationStatus: textOf(120),
      integrationStatus: textOf(120), verificationStatus: textOf(120), responsibility: textOf(300), behavior: [textOf(300), textOf(300)],
    }));
    const records = Array.from({ length: 60 }, (_, index) => makeRecord(`rec-${String(index).padStart(2, '0')}-${textOf(20)}`, {
      title: textOf(200), summary: textOf(400), status: textOf(120), reason: textOf(200), details: [textOf(800)],
      systemIds: systems.filter(() => random() < 0.2).map(item => item.id), type: (['변경', '결정', '검증', '계획'] as const)[index % 4] ?? '변경',
    }));
    const catalog = makeCatalog({ systems, records });
    const harness = await open(catalog);
    for (const limit of [undefined, 7, 50]) {
      for (const tool of ['list_systems', 'search_records'] as const) {
        const walk = await walkPages(harness, tool, limit ? { limit } : {});
        expect(walk.error).toBeUndefined();
        const expected = (tool === 'list_systems' ? systems : records).map(item => item.id).sort((a, b) => (a < b ? -1 : a > b ? 1 : 0));
        expect(walk.ids).toEqual(expected);
        for (const page of walk.pages) if (limit === undefined && page.returned > 1) expect(page.bytes).toBeLessThanOrEqual(DEFAULT_PAGE_TARGET);
      }
    }
    for (const system of systems) {
      const envelope = expectEnvelope('get_system', await harness.call('get_system', { id: system.id }));
      if (envelope.ok) expect(envelope.data.system).toEqual(system);
      else expect(envelope.error.code).toBe('RESPONSE_TOO_LARGE');
    }
    for (const record of records) {
      const envelope = expectEnvelope('get_record', await harness.call('get_record', { id: record.id }));
      if (envelope.ok) expect(envelope.data.record).toEqual(record);
      else expect(envelope.error.code).toBe('RESPONSE_TOO_LARGE');
    }
  });
});

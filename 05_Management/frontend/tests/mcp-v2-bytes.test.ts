// @vitest-environment node
// V2: response sizes over real stdio (goal D2 "응답/목록 예산/초과", completion condition 4).
// Application bytes = server-authored {content, structuredContent, isError} as on the wire
// (<= 16,384; default list target 8,192 unless one item). Wire bytes = whole JSON-RPC line;
// overhead = wire - application (id, jsonrpc, modern resultType/_meta). Canonical numbers come
// from the production entry (read-only); multilingual/escaping/link-array/snapshot limits use
// TEMP v2 fixtures through the V2 fixture entry. Pages are walked with the first page's hash. Both
// entries run the built mcp-dist, so these tests wait for the step-5 rebuild (pr2-t2 contract J).
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { afterAll, afterEach, describe, expect, it } from 'vitest';
import type { DevelopmentRecord, RecordCatalog, SystemRecord } from '../electron/catalog-contract';
import { DEFAULT_PAGE_TARGET, MAX_RESULT, codeUnitCompare, expectEnvelope, expectError, expectSuccess, makeCatalog, makeRecord, makeSource, makeSystem, type ToolName } from './mcp-fixtures';
import { CANONICAL_CATALOG, CANONICAL_GUIDE, ERAS, StdioProcess, removeTempRoots, sleep, tempRoot, type Era, type Exchange } from './mcp-v2/harness';
import { closeAndCheck, outcome, processPool, writeCatalog } from './mcp-v2/support';

const pool = processPool();
afterEach(async () => { await pool.closeAll(); });
afterAll(() => { removeTempRoots(); });

interface Sample { tool: string; label: string; app: number; wire: number; client: number; returned?: number }
const overhead = (sample: Sample) => sample.wire - sample.app;

// Respects RATE_LIMITED.retryAfterMs (client-side pacing); counts the waits.
async function paced(proc: StdioProcess, tool: ToolName, args: Record<string, unknown>, stats: { waits: number }): Promise<Exchange> {
  for (let attempt = 0; attempt < 50; attempt += 1) {
    const exchange = await proc.call(tool, args);
    const envelope = expectEnvelope(tool, outcome(exchange));
    if (envelope.ok || envelope.error.code !== 'RATE_LIMITED') return exchange;
    stats.waits += 1;
    await sleep(Number(envelope.error.details?.retryAfterMs ?? 100) + 5);
  }
  throw new Error('rate limit never cleared');
}

function sample(tool: string, label: string, exchange: Exchange, returned?: number): Sample {
  const value: Sample = { tool, label, app: exchange.appBytes ?? -1, wire: exchange.wireBytes ?? -1, client: exchange.clientBytes };
  if (returned !== undefined) value.returned = returned;
  expect(value.app).toBeGreaterThan(0);
  expect(value.app).toBeLessThanOrEqual(MAX_RESULT);
  return value;
}

async function walkAll(proc: StdioProcess, tool: 'list_systems' | 'search_records', args: Record<string, unknown>, label: string, samples: Sample[], stats: { waits: number }, options: { stopOnError?: boolean } = {}) {
  const ids: string[] = [];
  let offset = 0;
  let hash: string | undefined;
  for (let page = 0; page < 500; page += 1) {
    const exchange = await paced(proc, tool, { ...args, offset, ...(hash ? { expectedHash: hash } : {}) }, stats);
    const envelope = expectEnvelope(tool, outcome(exchange));
    if (!envelope.ok) {
      if (options.stopOnError) return { ids, hash, error: envelope.error.code, errorOffset: offset, exchange };
      throw new Error(`${label}: ${envelope.error.code} at offset ${offset}`);
    }
    hash ??= envelope.snapshot.hash;
    const data = envelope.data as { items: Array<{ id: string }>; paging: { limit: number; nextOffset: number | null } };
    const measured = sample(tool, `${label}@${offset}`, exchange, data.items.length);
    // Default list target: several items stay <= 8 KiB; only a single item may exceed it.
    if (args.limit === undefined && data.items.length > 1) expect(measured.app).toBeLessThanOrEqual(DEFAULT_PAGE_TARGET);
    samples.push(measured);
    ids.push(...data.items.map(item => item.id));
    if (data.paging.nextOffset === null) return { ids, hash, error: undefined, errorOffset: undefined, exchange };
    offset = data.paging.nextOffset;
  }
  throw new Error('paging did not terminate');
}

const byMax = (samples: Sample[], predicate: (item: Sample) => boolean) => samples.filter(predicate).reduce<Sample | undefined>((best, item) => (!best || item.app > best.app ? item : best), undefined);

describe('canonical catalog through the production entry: per-tool bytes', () => {
  it('both revisions concurrently: every page/detail <= 16 KiB, default pages <= 8 KiB, walks complete, wire overhead recorded', async () => {
    const before = readFileSync(CANONICAL_CATALOG);
    const canonical = JSON.parse(before.toString('utf8')) as RecordCatalog;
    const systemIds = canonical.systems.map(item => item.id).sort(codeUnitCompare);
    const recordIds = canonical.records.map(item => item.id).sort(codeUnitCompare);
    const procs = await Promise.all(ERAS.map(era => pool.start({ era, label: `bytes-prod-${era}` })));
    const results = await Promise.all(procs.map(async proc => {
      const samples: Sample[] = [];
      const stats = { waits: 0 };
      const systemsDefault = await walkAll(proc, 'list_systems', {}, 'systems-default', samples, stats);
      expect(systemsDefault.ids).toEqual(systemIds);
      const systemsMax = await walkAll(proc, 'list_systems', { limit: 50 }, 'systems-limit50', samples, stats);
      expect(systemsMax.ids).toEqual(systemIds);
      const recordsDefault = await walkAll(proc, 'search_records', {}, 'records-default', samples, stats);
      expect(recordsDefault.ids).toEqual(recordIds);
      const recordsMax = await walkAll(proc, 'search_records', { limit: 50 }, 'records-limit50', samples, stats);
      expect(recordsMax.ids).toEqual(recordIds);
      for (const id of canonical.systems.map(item => item.id)) {
        const related = await walkAll(proc, 'search_records', { systemId: id }, `related-${id}`, samples, stats);
        expect(related.ids).toEqual(canonical.records.filter(record => record.systemIds.includes(id)).map(record => record.id).sort(codeUnitCompare));
      }
      const hash = systemsDefault.hash;
      for (const [tool, list] of [['get_system', canonical.systems], ['get_record', canonical.records], ['get_source', canonical.sources]] as const) {
        for (const item of list) {
          const exchange = await paced(proc, tool, { id: item.id, expectedHash: hash }, stats);
          const envelope = expectSuccess(tool, outcome(exchange));
          const key = tool === 'get_system' ? 'system' : tool === 'get_record' ? 'record' : 'source';
          // Detail is complete: every catalog field preserved (no truncation of details).
          expect((envelope.data as Record<string, unknown>)[key]).toEqual(item);
          samples.push(sample(tool, item.id, exchange));
        }
      }
      // Design 「MCP」 new tools over the same stdio budget: the first page of every versioned
      // source section, local-only sources refused, and every guide card with its document.
      for (const source of canonical.sources) {
        const exchange = await paced(proc, 'read_source_section', { id: source.id, expectedHash: hash }, stats);
        if (source.availability === 'versioned') {
          const envelope = expectSuccess('read_source_section', outcome(exchange));
          expect((envelope.data as { source: unknown }).source).toEqual(source);
        } else {
          expectError('read_source_section', outcome(exchange), 'SOURCE_NOT_READABLE');
        }
        samples.push(sample('read_source_section', source.id, exchange));
      }
      const guide = JSON.parse(readFileSync(CANONICAL_GUIDE, 'utf8')) as { cards: Array<{ id: string }> };
      const cardIds: string[] = [];
      let guideHash: string | undefined;
      for (let offset: number | null = 0, page = 0; offset !== null && page < 100; page += 1) {
        const exchange = await paced(proc, 'list_guide_cards', { offset, ...(guideHash ? { expectedHash: guideHash } : {}) }, stats);
        const envelope = expectSuccess('list_guide_cards', outcome(exchange));
        guideHash ??= envelope.snapshot.hash;
        const data = envelope.data as { items: Array<{ id: string }>; paging: { nextOffset: number | null } };
        const measured = sample('list_guide_cards', `cards@${offset}`, exchange, data.items.length);
        if (data.items.length > 1) expect(measured.app).toBeLessThanOrEqual(DEFAULT_PAGE_TARGET);
        samples.push(measured);
        cardIds.push(...data.items.map(item => item.id));
        offset = data.paging.nextOffset;
      }
      expect(cardIds).toEqual(guide.cards.map(card => card.id));
      for (const id of cardIds) {
        const exchange = await paced(proc, 'get_guide_card', { id, expectedHash: guideHash }, stats);
        expectSuccess('get_guide_card', outcome(exchange));
        samples.push(sample('get_guide_card', id, exchange));
      }
      return { era: proc.options.era, samples, stats };
    }));
    const report: Record<string, unknown> = {};
    for (const { era, samples, stats } of results) {
      const firstDefault = (prefix: string) => samples.find(item => item.label === `${prefix}@0`);
      const perTool = Object.fromEntries((['list_systems', 'search_records', 'get_system', 'get_record', 'get_source', 'read_source_section', 'list_guide_cards', 'get_guide_card'] as const).map(tool => {
        const max = byMax(samples, item => item.tool === tool);
        return [tool, max && { maxApp: max.app, label: max.label, wire: max.wire, client: max.client, overhead: overhead(max) }];
      }));
      const overheads = samples.map(overhead);
      report[era] = {
        calls: samples.length, rateLimitWaits: stats.waits, perTool,
        defaultSystemsPage0: firstDefault('systems-default'), defaultRecordsPage0: firstDefault('records-default'),
        maxSystemsPage: byMax(samples, item => item.label.startsWith('systems-limit50')), maxRecordsPage: byMax(samples, item => item.label.startsWith('records-limit50')),
        maxWire: Math.max(...samples.map(item => item.wire)), overheadMin: Math.min(...overheads), overheadMax: Math.max(...overheads),
      };
    }
    console.info(`[V2-MEASURE] canonical-bytes ${JSON.stringify(report)}`);
    for (const proc of procs) await closeAndCheck(proc);
    expect(readFileSync(CANONICAL_CATALOG).equals(before)).toBe(true);
  }, 240_000);
});

// ------------------------------------------------------------------ fixtures

// Record index v2 fixtures (index-v2-design.md 「색인 형식」): only v2 keys, IDs of [a-z0-9-], titles
// equal to their trim(); sizes come from titles, areas and link arrays instead of narrative fields.
const koreanMix = (length: number, seed: number) => Array.from({ length }, (_, index) => '가나다라마바사아자차카타파하漢字😀'.charAt((index + seed) % 15)).join('');
function multilingualCatalog(): RecordCatalog {
  const systems = Array.from({ length: 40 }, (_, index) => makeSystem(`sys-ml-${String(index).padStart(2, '0')}`, {
    title: `제목 ${koreanMix(90, index)}`, area: `${index % 3 ? '게임 기반' : 'Management'} ${koreanMix(100, index + 1)}`,
  }));
  return makeCatalog({ systems });
}
const ESCAPES = '\u0001\u0002\u001f"\\  </script>\t\n';
// Brackets keep escape-heavy titles valid: v2 titles may not start or end with whitespace.
const bracketed = (text: string) => `[${text}]`;
function escapingCatalog(): RecordCatalog {
  const systems = [makeSystem('sys-esc', { title: bracketed(ESCAPES.repeat(8)) })];
  const records = Array.from({ length: 30 }, (_, index) => makeRecord(`rec-esc-${String(index).padStart(2, '0')}`, {
    title: bracketed(`${ESCAPES.repeat(7)}${'\u0007'.repeat(60)}`), systemIds: ['sys-esc'],
  }));
  return makeCatalog({ systems, records });
}
const longId = (index: number) => `sys-long-${String(index).padStart(4, '0')}-${'l'.repeat(100 - 14)}`;
function linkArrayCatalog(): RecordCatalog {
  const systems = Array.from({ length: 120 }, (_, index) => makeSystem(longId(index)));
  const records = [
    makeRecord('rec-a-small', { systemIds: [longId(0)] }),
    makeRecord('rec-b-big', { systemIds: systems.slice(0, 45).map(item => item.id) }),
    makeRecord('rec-c-huge', { systemIds: systems.map(item => item.id) }),
    makeRecord('rec-d-after', { systemIds: [longId(1)] }),
  ];
  return makeCatalog({ systems, records });
}
function detailCatalog(): RecordCatalog {
  const sources = [makeSource('src-fit', { title: `출처 ${'n'.repeat(6_000)}` }), makeSource('src-huge', { title: `출처 ${'노'.repeat(6_000)}` })];
  const systems: SystemRecord[] = [
    makeSystem('sys-fit', { title: `행동 ${'b'.repeat(6_000)}`, sourceIds: ['src-fit'] }),
    makeSystem('sys-huge', { title: `행동 ${'b'.repeat(18_000)}` }),
  ];
  const records: DevelopmentRecord[] = [
    makeRecord('rec-fit', { title: `상세 ${'d'.repeat(6_000)}`, systemIds: ['sys-fit'] }),
    makeRecord('rec-huge', { title: `상세 ${'"'.repeat(9_000)}`, systemIds: ['sys-huge'] }),
  ];
  return makeCatalog({ systems, records, sources });
}
function smallCatalog(): RecordCatalog {
  return makeCatalog({ systems: Array.from({ length: 5 }, (_, index) => makeSystem(`sys-meta-${index}`)), records: [makeRecord('rec-meta', { systemIds: ['sys-meta-0'] })] });
}
async function bothEras(label: string, catalog: RecordCatalog, body: (proc: StdioProcess, era: Era) => Promise<Record<string, unknown>>) {
  const root = tempRoot(`bytes-${label}`);
  const path = join(root, 'catalog.json');
  writeCatalog(path, catalog);
  const procs = await Promise.all(ERAS.map(era => pool.start({ era, label: `bytes-${label}-${era}`, entry: 'fixture', catalog: path })));
  const reports = await Promise.all(procs.map(proc => body(proc, proc.options.era)));
  console.info(`[V2-MEASURE] fixture-bytes ${label} ${JSON.stringify({ legacy: reports[0], modern: reports[1] })}`);
  for (const proc of procs) await closeAndCheck(proc);
  return reports;
}

describe('fixtures through the fixture entry: pagination budget and oversize failures over stdio', () => {
  it('multilingual previews: default pages <= 8 KiB, limit 50 pages <= 16 KiB, truncation marked, no gaps', async () => {
    const catalog = multilingualCatalog();
    const ids = catalog.systems.map(item => item.id).sort(codeUnitCompare);
    await bothEras('multilingual', catalog, async proc => {
      const samples: Sample[] = [];
      const stats = { waits: 0 };
      const byDefault = await walkAll(proc, 'list_systems', {}, 'ml-default', samples, stats);
      const byMax50 = await walkAll(proc, 'list_systems', { limit: 50 }, 'ml-50', samples, stats);
      expect(byDefault.ids).toEqual(ids);
      expect(byMax50.ids).toEqual(ids);
      const first = expectSuccess('list_systems', outcome(await proc.call('list_systems', { limit: 1 })));
      // Design 「MCP」 tool table: only the title is cut (80 units), the area stays whole.
      const item = (first.data as { items: Array<{ title: string; area: string; truncatedFields: string[] }> }).items[0];
      expect(item?.truncatedFields).toEqual(['title']);
      expect(item?.title.length).toBeLessThanOrEqual(80);
      expect(item?.area).toBe(catalog.systems[0]?.area);
      const defaults = samples.filter(entry => entry.label.startsWith('ml-default'));
      const max50 = samples.filter(entry => entry.label.startsWith('ml-50'));
      return {
        defaultPages: defaults.length, defaultReturned: defaults.map(entry => entry.returned), defaultMaxApp: Math.max(...defaults.map(entry => entry.app)),
        limit50Pages: max50.length, limit50Returned: max50.map(entry => entry.returned), limit50MaxApp: Math.max(...max50.map(entry => entry.app)),
        maxWire: Math.max(...samples.map(entry => entry.wire)), overhead: [...new Set(samples.map(overhead))],
      };
    });
  }, 60_000);

  it('escaping-heavy records: content text and structuredContent both counted; pages bounded and complete', async () => {
    const catalog = escapingCatalog();
    const ids = catalog.records.map(item => item.id).sort(codeUnitCompare);
    await bothEras('escaping', catalog, async proc => {
      const samples: Sample[] = [];
      const stats = { waits: 0 };
      const byDefault = await walkAll(proc, 'search_records', {}, 'esc-default', samples, stats);
      const byMax50 = await walkAll(proc, 'search_records', { limit: 50 }, 'esc-50', samples, stats);
      expect(byDefault.ids).toEqual(ids);
      expect(byMax50.ids).toEqual(ids);
      const detail = expectSuccess('get_record', outcome(await proc.call('get_record', { id: 'rec-esc-00' })));
      expect((detail.data as { record: { title: string } }).record.title).toBe(catalog.records[0]?.title);
      return {
        defaultReturned: samples.filter(entry => entry.label.startsWith('esc-default')).map(entry => entry.returned),
        limit50Returned: samples.filter(entry => entry.label.startsWith('esc-50')).map(entry => entry.returned),
        maxApp: Math.max(...samples.map(entry => entry.app)), maxWire: Math.max(...samples.map(entry => entry.wire)),
      };
    });
  }, 60_000);

  it('long link arrays: an 8-16 KiB preview is returned alone; a >16 KiB preview is RESPONSE_TOO_LARGE at its offset; the caller can resume after it', async () => {
    await bothEras('links', linkArrayCatalog(), async proc => {
      const samples: Sample[] = [];
      const stats = { waits: 0 };
      const walked = await walkAll(proc, 'search_records', {}, 'links', samples, stats, { stopOnError: true });
      expect(walked.ids).toEqual(['rec-a-small', 'rec-b-big']);
      expect(walked.error).toBe('RESPONSE_TOO_LARGE');
      expect(walked.errorOffset).toBe(2);
      const big = samples.find(entry => entry.label === 'links@1');
      expect(big?.returned).toBe(1);
      expect(big?.app).toBeGreaterThan(DEFAULT_PAGE_TARGET);
      const tooLarge = expectError('search_records', outcome(walked.exchange), 'RESPONSE_TOO_LARGE');
      expect(tooLarge.snapshot?.hash).toBe(walked.hash);
      const resumed = expectSuccess('search_records', outcome(await proc.call('search_records', { offset: 3, expectedHash: walked.hash })));
      expect((resumed.data as { items: Array<{ id: string }> }).items.map(item => item.id)).toEqual(['rec-d-after']);
      // The same oversize also stops a limit-50 page before it, without dropping it silently.
      const page = expectSuccess('search_records', outcome(await proc.call('search_records', { limit: 50 })));
      const paging = (page.data as { items: Array<{ id: string }>; paging: { returned: number; nextOffset: number | null } });
      expect(paging.items.map(item => item.id)).toEqual(['rec-a-small', 'rec-b-big']);
      expect(paging.paging.nextOffset).toBe(2);
      return { samples: samples.map(entry => ({ label: entry.label, app: entry.app, wire: entry.wire, returned: entry.returned })), errorAppBytes: walked.exchange.appBytes, errorWire: walked.exchange.wireBytes };
    });
  }, 60_000);

  it('oversized details are RESPONSE_TOO_LARGE without content; fitting details are complete', async () => {
    const catalog = detailCatalog();
    await bothEras('detail', catalog, async proc => {
      const fit: Record<string, number | undefined> = {};
      const huge: Record<string, number | undefined> = {};
      for (const [tool, fitId, hugeId, key] of [['get_system', 'sys-fit', 'sys-huge', 'system'], ['get_record', 'rec-fit', 'rec-huge', 'record'], ['get_source', 'src-fit', 'src-huge', 'source']] as const) {
        const ok = await proc.call(tool, { id: fitId });
        const success = expectSuccess(tool, outcome(ok));
        const list = key === 'system' ? catalog.systems : key === 'record' ? catalog.records : catalog.sources;
        expect((success.data as Record<string, unknown>)[key]).toEqual(list.find(item => item.id === fitId));
        fit[tool] = ok.appBytes;
        const bad = await proc.call(tool, { id: hugeId });
        const failure = expectError(tool, outcome(bad), 'RESPONSE_TOO_LARGE');
        expect(failure.snapshot).not.toBeNull();
        expect(bad.responseLine?.text.includes('bbbbbbbbbb') || bad.responseLine?.text.includes('노노노노')).toBe(false);
        huge[tool] = bad.appBytes;
      }
      return { fitAppBytes: fit, tooLargeAppBytes: huge };
    });
  }, 60_000);

  // Design 「MCP」 스냅샷: metadata is the fixed-size `{ hash }`; the v1 hand-written metadata that
  // could overflow and be omitted no longer exists (「색인 형식」).
  it('metadata: every page and error over stdio carries the fixed-size { hash } snapshot, never metadataOmitted', async () => {
    await bothEras('metadata', smallCatalog(), async proc => {
      const samples: Sample[] = [];
      const walked = await walkAll(proc, 'list_systems', {}, 'meta', samples, { waits: 0 });
      expect(walked.ids).toHaveLength(5);
      const seen: Record<string, unknown> = {};
      for (const [tool, args, code] of [['get_record', { id: 'missing-id' }, 'NOT_FOUND'], ['get_system', { id: 'sys-meta-0', expectedHash: 'f'.repeat(64) }, 'VERSION_CONFLICT']] as const) {
        const exchange = await proc.call(tool, args);
        const envelope = expectError(tool, outcome(exchange), code);
        expect(envelope.snapshot).toEqual({ hash: walked.hash });
        expect(envelope.error.details?.metadataOmitted).toBeUndefined();
        seen[tool] = { code: envelope.error.code, app: exchange.appBytes, wire: exchange.wireBytes };
      }
      return { returned: samples.map(entry => entry.returned), app: samples.map(entry => entry.app), seen };
    });
  }, 60_000);
});

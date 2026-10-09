// @vitest-environment node
// V3-R1 (verifier-owned): code review follow-ups on the server adapter.
// - R06: an unexpected (non-CatalogReadError) error from the read or query step keeps the public
//   CATALOG_UNREADABLE contract and leaves one fixed diagnostic line on stderr. Its name, message,
//   stack, path, code and the request input never reach stderr, stdout or the client, and stdout
//   stays JSON-RPC only. Normal results and mapped domain errors write no diagnostic. Checked over
//   real stdio with the BUILT server factory (tests/mcp-v3-r1/internal-error-entry.mjs), both revisions.
// - R03: an explicit limit of 10 keeps the default page's 8 KiB budget (shared DEFAULT_LIST_LIMIT);
//   other limits keep the 16 KiB result cap.
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { afterEach, describe, expect, it } from 'vitest';
import {
  DEFAULT_PAGE_TARGET, MAX_RESULT, connectHarness, expectError, expectSuccess, fileBacked, makeCatalog, makeSystem, walkPages, type Harness,
} from './mcp-fixtures';
import { startRawChild, type RawChild } from './mcp-v3-r1/raw-stdio';

const FRONTEND = fileURLToPath(new URL('..', import.meta.url));
const ENTRY = join(FRONTEND, 'tests', 'mcp-v3-r1', 'internal-error-entry.mjs');
const DIAGNOSTIC = 'Catalog MCP internal error.\n';
const UNREADABLE_MESSAGE = '기록 카탈로그를 읽지 못했습니다. 다시 요청하세요.';
const INPUT_SENTINEL = 'SENTINEL_R06_INPUT';

const children: RawChild[] = [];
afterEach(async () => { for (const child of children.splice(0)) await child.close(); });
async function start(mode: string, era: 'legacy' | 'modern') {
  const child = await startRawChild(ENTRY, [mode], era, FRONTEND);
  children.push(child);
  return child;
}
// stdout purity and a natural exit after EOF.
async function finish(child: RawChild) {
  children.splice(children.indexOf(child), 1);
  const exit = await child.close();
  expect(child.transport.nonJson).toEqual([]);
  for (const line of child.transport.lines) {
    const json = JSON.parse(line) as Record<string, unknown>;
    expect(json.jsonrpc).toBe('2.0');
    expect(typeof json.method === 'string' || 'result' in json || 'error' in json).toBe(true);
  }
  expect(exit).toEqual({ code: 0, signal: null, forcedKill: false });
}

describe('R06: unexpected internal errors keep the public contract and leave a fixed diagnostic', () => {
  for (const era of ['legacy', 'modern'] as const) {
    for (const mode of ['read-throw', 'query-throw']) {
      it(`${era} ${mode}: CATALOG_UNREADABLE with the fixed message, one fixed stderr line per failure, nothing reflected anywhere`, async () => {
        const child = await start(mode, era);
        const calls = [
          ['list_systems', { query: `${INPUT_SENTINEL}_QUERY`, area: `${INPUT_SENTINEL}_AREA` }],
          ['search_records', { query: `${INPUT_SENTINEL}_QUERY`, type: '검증' }],
          ['get_system', { id: `${INPUT_SENTINEL}_ID` }],
        ] as const;
        const clientJson: string[] = [];
        for (const [tool, args] of calls) {
          const outcome = await child.call(tool, { ...args });
          const envelope = expectError(tool, outcome, 'CATALOG_UNREADABLE');
          expect(envelope).toEqual({ ok: false, snapshot: null, error: { code: 'CATALOG_UNREADABLE', message: UNREADABLE_MESSAGE, retryable: true } });
          clientJson.push(JSON.stringify(outcome.result));
        }
        await finish(child);
        const stderr = child.stderr();
        expect(stderr).toBe(DIAGNOSTIC.repeat(calls.length));
        for (const text of [stderr, ...clientJson, ...child.transport.lines]) {
          expect(text).not.toMatch(/SENTINEL|TypeError|module\.js|catalog\.json/);
        }
        console.info(`[V3-R1-MEASURE] r06 ${JSON.stringify({ era, mode, stderr, stdoutLines: child.transport.lines.length, nonJson: child.transport.nonJson.length })}`);
      });
    }

    it(`${era}: a normal result and a mapped domain error write no diagnostic`, async () => {
      const ok = await start('ok', era);
      const listed = expectSuccess('list_systems', await ok.call('list_systems', { query: INPUT_SENTINEL }));
      expect(listed.data).toEqual({ items: [], paging: { total: 0, offset: 0, limit: 10, returned: 0, nextOffset: null } });
      await finish(ok);
      expect(ok.stderr()).toBe('');

      const domain = await start('domain', era);
      expectError('get_record', await domain.call('get_record', { id: INPUT_SENTINEL }), 'CATALOG_MISSING');
      await finish(domain);
      expect(domain.stderr()).toBe('');
    });
  }
});

describe('R03: the default list budget is shared by an omitted and an explicit limit of 10', () => {
  const harnesses: Harness[] = [];
  afterEach(async () => { await Promise.all(harnesses.splice(0).map(harness => harness.close())); });

  it('limit omitted and limit 10 page identically within 8 KiB; limit 11 may use the 16 KiB cap', async () => {
    // Previews large enough that ten of them exceed the 8 KiB target (area is never truncated).
    const systems = Array.from({ length: 30 }, (_, index) => makeSystem(`s${String(index).padStart(2, '0')}`, { area: '가'.repeat(200) }));
    const harness = await connectHarness({ readSnapshot: fileBacked(makeCatalog({ systems })).readSnapshot });
    harnesses.push(harness);
    const omitted = await walkPages(harness, 'list_systems', {});
    const explicit = await walkPages(harness, 'list_systems', { limit: 10 });
    const eleven = await walkPages(harness, 'list_systems', { limit: 11 });
    for (const walk of [omitted, explicit, eleven]) {
      expect(walk.error).toBeUndefined();
      expect(walk.ids).toEqual(systems.map(item => item.id));
    }
    expect(explicit.pages.map(page => [page.returned, page.bytes])).toEqual(omitted.pages.map(page => [page.returned, page.bytes]));
    for (const page of explicit.pages) if (page.returned > 1) expect(page.bytes).toBeLessThanOrEqual(DEFAULT_PAGE_TARGET);
    expect(explicit.pages[0]?.returned).toBeLessThan(10);
    expect(Math.max(...eleven.pages.map(page => page.bytes))).toBeGreaterThan(DEFAULT_PAGE_TARGET);
    for (const page of eleven.pages) expect(page.bytes).toBeLessThanOrEqual(MAX_RESULT);
    console.info(`[V3-R1-MEASURE] r03 ${JSON.stringify({ omitted: omitted.pages.map(page => [page.returned, page.bytes]), explicit10: explicit.pages.map(page => [page.returned, page.bytes]), limit11: eleven.pages.map(page => [page.returned, page.bytes]) })}`);
  });
});

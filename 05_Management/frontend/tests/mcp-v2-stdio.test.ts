// @vitest-environment node
// V2: real stdio integration with two independent child processes (goal completion
// conditions 1/2/3/5, D2/D3/D4). Requirement source: goal.md, not the implementation.
// - production entry (built mcp-dist/mcp/main.js): canonical catalog read-only, no seams.
// - V2 fixture entry: same built product factories, TEMP fixture path, IPC counters.
// Every exchange runs over real pipes; stdout lines are parsed independently.
import { readFileSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { afterAll, afterEach, describe, expect, it } from 'vitest';
import { catalogBytes, expectEnvelope, expectError, expectSuccess, linkedCatalog, makeCatalog, makeRecord, makeSource, makeSystem, type ToolName } from './mcp-fixtures';
import {
  CANONICAL_CATALOG, ERAS, EXIT_DEADLINE_MS, PRODUCTION_ENTRY, REVISION, StdioProcess, removeTempRoots, sha256, sleep, structured, tempRoot, textHash, waitFor,
  type Era, type JsonObject, type SpawnOptions,
} from './mcp-v2/harness';
import { closeAndCheck, outcome, processPool, writeCatalog } from './mcp-v2/support';

// V3-R1: the expected serverInfo.version is read from the build-info.js next to the entry the
// clients actually run, never taken from the server's answer and no longer pinned to one build.
// That this digest belongs to the current sources is shown by a fresh TEMP build
// (tests/mcp-v3-build.test.ts) and its input coverage by tests/mcp-v3-r1-build.test.ts.
function builtDigest(): string {
  expect(readFileSync(PRODUCTION_ENTRY, 'utf8')).toContain("from './build-info.js'");
  const match = /^export const BUILD_VERSION = "(0\.0\.0\+sha256\.[0-9a-f]{64})";\n$/.exec(readFileSync(join(dirname(PRODUCTION_ENTRY), 'build-info.js'), 'utf8'));
  if (!match?.[1]) throw new Error('mcp-dist/mcp/build-info.js holds no build digest');
  return match[1];
}
const pool = processPool();
const start = (options: SpawnOptions) => pool.start(options);
afterEach(async () => { await pool.closeAll(); });
afterAll(() => { removeTempRoots(); });

interface Navigation { calls: Array<{ tool: ToolName; args: JsonObject; structured: unknown; appBytes: number; wireBytes: number | null; clientBytes: number }>; hash: string }

// list/search -> system -> related records (systemId) -> record -> source, pinned to one hash.
async function navigate(proc: StdioProcess): Promise<Navigation> {
  const calls: Navigation['calls'] = [];
  const run = async (tool: ToolName, args: JsonObject) => {
    const exchange = await proc.call(tool, args);
    const envelope = expectSuccess(tool, outcome(exchange));
    calls.push({ tool, args, structured: structured(exchange), appBytes: exchange.appBytes ?? -1, wireBytes: exchange.wireBytes, clientBytes: exchange.clientBytes });
    return envelope;
  };
  const list = await run('list_systems', {});
  const hash = list.snapshot.hash;
  const systems = (list.data as { items: Array<{ id: string }> }).items;
  expect(systems.length).toBeGreaterThan(0);
  const system = await run('get_system', { id: systems[0]?.id, expectedHash: hash });
  const systemId = (system.data as { system: { id: string; recordIds: string[] } }).system.id;
  const related = await run('search_records', { systemId, expectedHash: hash });
  const records = (related.data as { items: Array<{ id: string; systemIds: string[] }> }).items;
  for (const item of records) expect(item.systemIds).toContain(systemId);
  const recordId = records[0]?.id ?? (system.data as { system: { recordIds: string[] } }).system.recordIds[0];
  const record = await run('get_record', { id: recordId, expectedHash: hash });
  const sourceId = (record.data as { record: { sourceIds: string[] } }).record.sourceIds[0]
    ?? (system.data as { system: { sourceIds: string[] } }).system.sourceIds[0];
  const source = await run('get_source', { id: sourceId, expectedHash: hash });
  expect((source.data as JsonObject).evidenceRead).toBe(false);
  expect((source.data as JsonObject).availabilityVerified).toBe(false);
  for (const call of calls) expect((call.structured as { snapshot: { hash: string } }).snapshot.hash).toBe(hash);
  return { calls, hash };
}

describe('production entry: two simultaneous independent clients on the canonical catalog', () => {
  it('legacy 2025-11-25 and pinned modern 2026-07-28 explore the same snapshot, stay isolated and exit 0 on EOF', async () => {
    const canonicalBefore = readFileSync(CANONICAL_CATALOG);
    const canonical = JSON.parse(canonicalBefore.toString('utf8')) as { revision: string; asOf: string; sourceCommit: string };
    const buildDigest = builtDigest();
    const [legacy, modern] = await Promise.all([start({ era: 'legacy', label: 'prod-legacy' }), start({ era: 'modern', label: 'prod-modern' })]);
    expect(legacy.child.pid).not.toBe(modern.child.pid);
    for (const proc of [legacy, modern]) {
      expect(proc.negotiated).toBe(REVISION[proc.options.era]);
      expect(proc.serverVersion).toBe(buildDigest);
      const tools = (await proc.client.listTools()).tools;
      expect(tools.map(tool => tool.name).sort()).toEqual(['get_record', 'get_source', 'get_system', 'list_systems', 'search_records']);
    }
    // Wire-level handshake evidence: legacy has initialize/initialized, modern carries the
    // pinned revision in per-request metadata and never sends initialize.
    const legacyInit = legacy.transport.outgoing.find(entry => entry.message.method === 'initialize')?.message;
    expect((legacyInit?.params as JsonObject | undefined)?.protocolVersion).toBe('2025-11-25');
    const legacyInitReply = legacy.lines.find(line => line.json?.id === legacyInit?.id)?.json?.result as JsonObject | undefined;
    expect(legacyInitReply?.protocolVersion).toBe('2025-11-25');
    expect(legacy.transport.outgoing.some(entry => entry.message.method === 'notifications/initialized')).toBe(true);
    expect(modern.transport.outgoing.some(entry => entry.message.method === 'initialize')).toBe(false);
    const modernMeta = JSON.stringify(modern.transport.outgoing.find(entry => entry.message.method === 'tools/list')?.message.params ?? {});
    expect(modernMeta).toContain('2026-07-28');
    console.info(`[V2-MEASURE] handshake ${JSON.stringify({
      legacyOutgoingMethods: legacy.transport.outgoing.map(entry => entry.message.method ?? 'response'),
      modernOutgoingMethods: modern.transport.outgoing.map(entry => entry.message.method ?? 'response'),
      modernToolsListParams: JSON.parse(modernMeta),
      modernDiscoverResult: modern.lines.find(line => line.json?.id === modern.transport.outgoing.find(entry => entry.message.method === 'server/discover')?.message.id)?.json?.result,
      legacyInitializeResultKeys: Object.keys(legacyInitReply ?? {}),
    })}`);

    // Interleave both clients so the two processes really overlap.
    const [a, b] = await Promise.all([navigate(legacy), navigate(modern)]);
    expect(a.hash).toBe(textHash(canonicalBefore));
    expect(a.hash).toBe(sha256(canonicalBefore));
    expect(b.hash).toBe(a.hash);
    // Same hash + same arguments -> identical domain results across the two processes.
    expect(b.calls.map(call => call.structured)).toEqual(a.calls.map(call => call.structured));
    const snapshot = (a.calls[0]?.structured as { snapshot: JsonObject }).snapshot;
    expect(snapshot).toEqual({ hash: a.hash, revision: canonical.revision, asOf: canonical.asOf, sourceCommit: canonical.sourceCommit });

    // Same conditions repeated on one process are deterministic too.
    const again = await navigate(legacy);
    expect(again.calls.map(call => call.structured)).toEqual(a.calls.map(call => call.structured));

    // Closing one client leaves the other process serving.
    const legacyExit = await closeAndCheck(legacy);
    const afterClose = await navigate(modern);
    expect(afterClose.hash).toBe(a.hash);
    expect(modern.child.exitCode).toBeNull();
    const modernExit = await closeAndCheck(modern);
    expect(readFileSync(CANONICAL_CATALOG).equals(canonicalBefore)).toBe(true);
    console.info(`[V2-MEASURE] prod-two-clients ${JSON.stringify({ hash: a.hash, snapshot, legacyExit, modernExit, navigationBytes: a.calls.map(call => ({ tool: call.tool, legacyApp: call.appBytes, legacyWire: call.wireBytes })), modernWire: b.calls.map(call => call.wireBytes) })}`);
  }, 60_000);
});

// ------------------------------------------------------------------ fixture entry, real I/O

function pagedCatalog() {
  const systems = Array.from({ length: 23 }, (_, index) => makeSystem(`sys-${String(index).padStart(2, '0')}`, { area: index % 2 ? '서버 플랫폼' : '게임 기반', summary: `요약 ${index} 검색어` }));
  const sources = [makeSource('src-1', { locator: 'C:\\v2-fixture\\SENTINEL_LOCATOR\\never-opened.md' })];
  const records = Array.from({ length: 31 }, (_, index) => makeRecord(`rec-${String(index).padStart(2, '0')}`, {
    type: (['변경', '결정', '검증', '계획'] as const)[index % 4] ?? '변경', systemIds: [`sys-${String(index % 23).padStart(2, '0')}`], sourceIds: ['src-1'],
  }));
  for (const system of systems) system.recordIds = records.filter(record => record.systemIds.includes(system.id)).map(record => record.id);
  return makeCatalog({ revision: 'v2-paged-r1', asOf: '2018-05-06T07:08:09Z', sourceCommit: 'feedfacefeedfacefeedfacefeedfacefeedface', systems, records, sources });
}

async function walk(proc: StdioProcess, tool: 'list_systems' | 'search_records', args: JsonObject, hash?: string) {
  const ids: string[] = [];
  let offset = 0;
  let pinned = hash;
  for (let page = 0; page < 1_000; page += 1) {
    const exchange = await proc.call(tool, { ...args, offset, ...(pinned ? { expectedHash: pinned } : {}) });
    const envelope = expectSuccess(tool, outcome(exchange));
    pinned ??= envelope.snapshot.hash;
    const data = envelope.data as { items: Array<{ id: string }>; paging: { nextOffset: number | null; offset: number } };
    expect(data.paging.offset).toBe(offset);
    ids.push(...data.items.map(item => item.id));
    if (data.paging.nextOffset === null) return { ids, hash: pinned };
    offset = data.paging.nextOffset;
  }
  throw new Error('paging did not terminate');
}

describe('fixture entry (real I/O on a TEMP fixture): two simultaneous clients', () => {
  it('same fixture -> same results; catalog update is visible on the next request without rebuild/restart; old hash conflicts', async () => {
    const root = tempRoot('update');
    const path = join(root, 'catalog.json');
    const first = pagedCatalog();
    const firstHash = writeCatalog(path, first);
    const [legacy, modern] = await Promise.all([
      start({ era: 'legacy', label: 'fx-legacy', entry: 'fixture', catalog: path }),
      start({ era: 'modern', label: 'fx-modern', entry: 'fixture', catalog: path }),
    ]);
    const pids = [legacy.child.pid, modern.child.pid];

    // Full page walk with the first hash: no gaps/duplicates, fixed UTF-16 order, both clients equal.
    const expectedSystemIds = first.systems.map(system => system.id).sort();
    const [walkA, walkB] = await Promise.all([walk(legacy, 'list_systems', {}), walk(modern, 'list_systems', {})]);
    expect(walkA.ids).toEqual(expectedSystemIds);
    expect(walkB.ids).toEqual(walkA.ids);
    expect(walkA.hash).toBe(firstHash);
    const filtered = await Promise.all([walk(legacy, 'search_records', { type: '결정', area: '서버 플랫폼', limit: 3 }), walk(modern, 'search_records', { type: '결정', area: '서버 플랫폼', limit: 3 })]);
    expect(filtered[1].ids).toEqual(filtered[0].ids);
    const oracle = first.records.filter(record => record.type === '결정' && record.systemIds.some(id => first.systems.find(system => system.id === id)?.area === '서버 플랫폼')).map(record => record.id).sort();
    expect(filtered[0].ids).toEqual(oracle);

    // Metadata preserved, not replaced by current time/HEAD.
    const listed = expectSuccess('list_systems', outcome(await modern.call('list_systems', { limit: 1 })));
    expect(listed.snapshot).toEqual({ hash: firstHash, revision: 'v2-paged-r1', asOf: '2018-05-06T07:08:09Z', sourceCommit: 'feedfacefeedfacefeedfacefeedfacefeedface' });

    // Source locator is data only: the fixture process never opens anything but the catalog.
    const source = expectSuccess('get_source', outcome(await legacy.call('get_source', { id: 'src-1' })));
    expect((source.data as { source: { locator: string } }).source.locator).toBe('C:\\v2-fixture\\SENTINEL_LOCATOR\\never-opened.md');

    // Update the catalog in place (atomic rename) while both processes keep running.
    const second = pagedCatalog();
    second.revision = 'v2-paged-r2';
    second.systems[0] = { ...second.systems[0]!, title: '갱신된 시스템 제목' };
    second.systems.push(makeSystem('sys-zz-new', { area: '게임 기반' }));
    const secondHash = writeCatalog(path, second);
    expect(secondHash).not.toBe(firstHash);
    for (const proc of [legacy, modern]) {
      const next = expectSuccess('get_system', outcome(await proc.call('get_system', { id: 'sys-00' })));
      expect(next.snapshot.hash).toBe(secondHash);
      expect(next.snapshot.revision).toBe('v2-paged-r2');
      expect((next.data as { system: { title: string } }).system.title).toBe('갱신된 시스템 제목');
      // Old hash: detail and later page conflict, return only the current metadata, no data.
      const detail = expectError('get_system', outcome(await proc.call('get_system', { id: 'sys-00', expectedHash: firstHash })), 'VERSION_CONFLICT');
      expect(detail.snapshot?.hash).toBe(secondHash);
      expect(detail.error.details).toEqual({ expectedHash: firstHash });
      const page = expectError('list_systems', outcome(await proc.call('list_systems', { offset: 10, expectedHash: firstHash })), 'VERSION_CONFLICT');
      expect(page.snapshot?.hash).toBe(secondHash);
      // Later page without hash is VERSION_REQUIRED (never silently mixes versions).
      expectError('list_systems', outcome(await proc.call('list_systems', { offset: 10 })), 'VERSION_REQUIRED');
    }
    const newWalk = await walk(legacy, 'list_systems', {});
    expect(newWalk.ids).toContain('sys-zz-new');
    expect(newWalk.hash).toBe(secondHash);

    // Same process ids: no restart happened. Counters: one server instance per process,
    // one open per admitted read, every handle closed before search/serialization.
    expect([legacy.child.pid, modern.child.pid]).toEqual(pids);
    for (const proc of [legacy, modern]) {
      const counters = await proc.counters();
      expect(counters.factoryCalls).toBe(1);
      expect(counters.opens).toBe(counters.readSnapshotCalls);
      expect(counters.closes).toBe(counters.opens);
      expect(counters.openAtSettle).toBe(0);
      expect(new Set(counters.openedPaths)).toEqual(new Set([path]));
      console.info(`[V2-MEASURE] fx-update-counters ${proc.options.label} ${JSON.stringify({ ...counters, openedPaths: counters.openedPaths.length })}`);
    }
    await Promise.all([closeAndCheck(legacy), closeAndCheck(modern)]);
  }, 60_000);
});

// ------------------------------------------------------------------ quota per process

describe('per-process quota survives the real serveStdio lifecycle', () => {
  for (const era of ERAS) {
    it(`${era}: one server instance per connection; frozen clock admits exactly 10 calls, the rest RATE_LIMITED without reads`, async () => {
      const root = tempRoot(`quota-${era}`);
      const path = join(root, 'catalog.json');
      writeCatalog(path, linkedCatalog());
      const proc = await start({ era, label: `quota-${era}`, entry: 'fixture', catalog: path, clock: 'frozen' });
      const results: string[] = [];
      for (let index = 0; index < 15; index += 1) {
        const exchange = await proc.call(index % 2 ? 'get_system' : 'list_systems', index % 2 ? { id: 'sys-alpha' } : {});
        const envelope = expectEnvelope(index % 2 ? 'get_system' : 'list_systems', outcome(exchange));
        results.push(envelope.ok ? 'ok' : envelope.error.code);
        if (!envelope.ok) {
          expect(envelope.error.code).toBe('RATE_LIMITED');
          expect(envelope.error.retryable).toBe(true);
          expect(envelope.error.details?.retryAfterMs).toBe(100);
        }
      }
      expect(results).toEqual([...Array(10).fill('ok'), ...Array(5).fill('RATE_LIMITED')]);
      const counters = await proc.counters();
      expect(counters.factoryCalls).toBe(1);
      expect(counters.factoryEras).toEqual([era]);
      expect(counters.entered).toBe(15);
      expect(counters.readSnapshotCalls).toBe(10);
      console.info(`[V2-MEASURE] quota-frozen ${era} ${JSON.stringify({ results, factoryCalls: counters.factoryCalls, entered: counters.entered, reads: counters.readSnapshotCalls })}`);
      await closeAndCheck(proc);
    }, 30_000);
  }

  it('production entry with the real clock: a sequential burst is limited near the bucket, recovers after retryAfterMs (both revisions, concurrently)', async () => {
    const [legacy, modern] = await Promise.all([start({ era: 'legacy', label: 'prod-quota-legacy' }), start({ era: 'modern', label: 'prod-quota-modern' })]);
    const burst = async (proc: StdioProcess) => {
      const outcomes: Array<{ ok: boolean; code?: string; retryAfterMs?: unknown; at: number }> = [];
      const started = performance.now();
      for (let index = 0; index < 25; index += 1) {
        const envelope = expectEnvelope('list_systems', outcome(await proc.call('list_systems', { query: `burst-${index}` })));
        outcomes.push(envelope.ok ? { ok: true, at: Math.round(performance.now() - started) } : { ok: false, code: envelope.error.code, retryAfterMs: envelope.error.details?.retryAfterMs, at: Math.round(performance.now() - started) });
      }
      const elapsedMs = performance.now() - started;
      const successes = outcomes.filter(item => item.ok).length;
      // Bucket: 10 initial tokens + 10/s refill during the burst. Anything near 25 would mean
      // the bucket was re-created per request.
      expect(successes).toBeGreaterThanOrEqual(10);
      expect(successes).toBeLessThanOrEqual(10 + Math.ceil(elapsedMs / 100) + 1);
      expect(successes).toBeLessThan(25);
      for (const item of outcomes.filter(entry => !entry.ok)) {
        expect(item.code).toBe('RATE_LIMITED');
        expect(item.retryAfterMs).toEqual(expect.any(Number));
        expect(item.retryAfterMs as number).toBeGreaterThanOrEqual(1);
        expect(item.retryAfterMs as number).toBeLessThanOrEqual(1000);
      }
      await sleep(1_100);
      const recovered = expectEnvelope('list_systems', outcome(await proc.call('list_systems', { query: 'after-wait' })));
      expect(recovered.ok).toBe(true);
      return { successes, limited: outcomes.length - successes, elapsedMs: Math.round(elapsedMs), outcomes };
    };
    const [a, b] = await Promise.all([burst(legacy), burst(modern)]);
    console.info(`[V2-MEASURE] prod-quota ${JSON.stringify({ legacy: a, modern: b })}`);

    // Pipelined burst of 8 in ONE stdin write: concurrency cap 4 / no queue is per process.
    const pipelined = async (proc: StdioProcess) => {
      await sleep(1_100);
      const meta = proc.envelopeMeta();
      const ids = Array.from({ length: 8 }, (_, index) => `v2-pipe-${proc.options.era}-${index}`);
      proc.writeBurst(ids.map((id, index) => ({ jsonrpc: '2.0', id, method: 'tools/call', params: { ...(meta === undefined ? {} : { _meta: meta }), name: 'list_systems', arguments: { query: `pipe-${index}` } } })));
      await waitFor(() => ids.every(id => proc.responseLineFor(id)), 10_000, 'pipelined responses');
      const codes = ids.map(id => {
        const envelope = ((proc.responseLineFor(id)?.json?.result as JsonObject).structuredContent) as { ok: boolean; error?: { code: string; details?: { retryAfterMs?: number } } };
        return envelope.ok ? 'ok' : `${envelope.error?.code}:${envelope.error?.details?.retryAfterMs}`;
      });
      return codes;
    };
    const [pipeA, pipeB] = await Promise.all([pipelined(legacy), pipelined(modern)]);
    console.info(`[V2-MEASURE] prod-pipelined-8 ${JSON.stringify({ legacy: pipeA, modern: pipeB })}`);
    for (const codes of [pipeA, pipeB]) {
      expect(codes.filter(code => code === 'ok').length).toBeGreaterThanOrEqual(4);
      for (const code of codes.filter(item => item !== 'ok')) expect(code).toMatch(/^RATE_LIMITED:/);
    }
    await Promise.all([closeAndCheck(legacy), closeAndCheck(modern)]);
  }, 60_000);
});

// ------------------------------------------------------------------ cancellation (controlled I/O)

async function controlled(era: Era, label: string) {
  const root = tempRoot(`cancel-${label}`);
  const path = join(root, 'catalog.json');
  writeFileSync(path, catalogBytes(linkedCatalog()));
  return start({ era, label, entry: 'fixture', mode: 'controlled', catalog: path });
}

// Commands travel over IPC and requests over stdin; the counters() round trip guarantees the
// pause is registered before the next request can reach the paused operation.
async function pauseAt(proc: StdioProcess, op: string, abortAware: boolean) {
  proc.command({ c: 'pause', op, abortAware });
  await proc.counters();
}
async function waitReached(proc: StdioProcess, op: string, from: number) {
  return proc.waitEvent(event => event.t === 'reached' && event.op === op, from, 5_000, `reached ${op}`);
}
function idsWithResponses(proc: StdioProcess): Set<unknown> {
  return new Set(proc.lines.filter(line => line.json && ('result' in line.json || 'error' in line.json)).map(line => line.json?.id));
}

describe('cancellation over real stdio (injected I/O promises)', () => {
  for (const era of ERAS) {
    for (const [op, abortAware] of [['read', true], ['read', false], ['open', true], ['close', false]] as const) {
      it(`${era}: cancel while ${op} is paused (${abortAware ? 'I/O observes signal' : 'I/O ignores signal'}) -> no success data, handle and slot released, next query normal`, async () => {
        const proc = await controlled(era, `${era}-${op}-${abortAware}`);
        expectSuccess('list_systems', outcome(await proc.call('list_systems', { query: 'warm' })));
        const from = proc.events.length;
        await pauseAt(proc, op, abortAware);
        const controller = new AbortController();
        const pending = proc.call('get_system', { id: 'sys-alpha' }, { signal: controller.signal });
        await waitReached(proc, op, from);
        controller.abort();
        const cancelled = await pending;
        expect(cancelled.result).toBeUndefined();
        expect(cancelled.thrown).toBeDefined();
        const cancelId = cancelled.request?.id;
        expect(proc.transport.outgoing.some(entry => entry.message.method === 'notifications/cancelled' && (entry.message.params as JsonObject).requestId === cancelId)).toBe(true);
        if (!abortAware) {
          // The paused I/O ignores the signal; release it so the reader can finish and close.
          await sleep(50);
          proc.command({ c: 'release', op });
        }
        await waitFor(async () => { const counters = await proc.counters(); return counters.closes === counters.opens && counters.inMemoryOpen === 0; }, 5_000, 'handle closed');
        // Prove the slot is free and the process healthy: a follow-up call succeeds; then the
        // cancelled id must still have no response line (SDK suppression) and no success data.
        expectSuccess('get_system', outcome(await proc.call('get_system', { id: 'sys-beta' })));
        await sleep(30);
        const responded = idsWithResponses(proc);
        expect(responded.has(cancelId)).toBe(false);
        const leaked = proc.lines.filter(line => line.json?.id === cancelId);
        expect(leaked).toEqual([]);
        const counters = await proc.counters();
        expect(counters.openAtSettle).toBe(0);
        expect(counters.readSnapshotErrors.REQUEST_CANCELLED ?? 0).toBe(1);
        // All 4 slots free again: four paused reads are admitted, the 5th is RATE_LIMITED(100).
        const before = proc.events.length;
        for (let index = 0; index < 4; index += 1) await pauseAt(proc, 'read', false);
        const held = Array.from({ length: 4 }, (_, index) => proc.call('list_systems', { query: `hold-${index}` }));
        await waitFor(() => proc.events.slice(before).filter(event => event.t === 'reached' && event.op === 'read').length === 4, 5_000, '4 reads paused');
        const fifth = expectError('list_systems', outcome(await proc.call('list_systems', { query: 'fifth' })), 'RATE_LIMITED');
        expect(fifth.error.details?.retryAfterMs).toBe(100);
        for (let index = 0; index < 4; index += 1) proc.command({ c: 'release', op: 'read' });
        for (const exchange of await Promise.all(held)) expectSuccess('list_systems', outcome(exchange));
        console.info(`[V2-MEASURE] cancel ${era} op=${op} abortAware=${abortAware} ${JSON.stringify({ cancelId, responseForCancelledId: responded.has(cancelId), channel: 'sdk-suppressed', thrown: cancelled.thrown, opens: counters.opens, closes: counters.closes, readErrors: counters.readSnapshotErrors })}`);
        await closeAndCheck(proc);
      }, 30_000);
    }
  }

  it('one client process ends (EOF) while its request is paused; the other client process keeps serving', async () => {
    const [legacy, modern] = await Promise.all([controlled('legacy', 'iso-legacy'), controlled('modern', 'iso-modern')]);
    const from = legacy.events.length;
    await pauseAt(legacy, 'read', true);
    const inFlight = legacy.call('get_system', { id: 'sys-alpha' });
    await waitReached(legacy, 'read', from);
    // modern keeps working while legacy is stuck mid-read
    expectSuccess('get_system', outcome(await modern.call('get_system', { id: 'sys-alpha' })));
    const exit = await legacy.close();
    const settled = await inFlight;
    expect(settled.result).toBeUndefined();
    expect(exit.forcedKill).toBe(false);
    expect(exit.code).toBe(0);
    expect(exit.msAfterEof as number).toBeLessThan(EXIT_DEADLINE_MS);
    expect(legacy.lines.some(line => line.json?.id === settled.request?.id)).toBe(false);
    for (let index = 0; index < 5; index += 1) expectSuccess('list_systems', outcome(await modern.call('list_systems', { query: `still-${index}` })));
    console.info(`[V2-MEASURE] isolation ${JSON.stringify({ legacyExit: exit, legacyStdoutLines: legacy.lines.length, legacyStderr: legacy.stderrBytes(), inFlightThrown: settled.thrown })}`);
    await closeAndCheck(modern);

    // A non-abort-aware paused request at EOF: the process must still exit by itself.
    const stuck = await controlled('modern', 'stuck-modern');
    const at = stuck.events.length;
    await pauseAt(stuck, 'read', false);
    const never = stuck.call('list_systems', {});
    await waitReached(stuck, 'read', at);
    const stuckExit = await stuck.close();
    const stuckOutcome = await never;
    expect(stuckOutcome.result).toBeUndefined();
    expect(stuckExit.forcedKill).toBe(false);
    expect(stuckExit.code).toBe(0);
    console.info(`[V2-MEASURE] eof-with-ignored-signal ${JSON.stringify({ exit: stuckExit, stderr: stuck.stderrText().slice(0, 200) })}`);
  }, 40_000);
});


// ------------------------------------------------------------------ failures over stdio

describe('missing / corrupt / changing catalogs over real stdio (fixture entry)', () => {
  for (const era of ERAS) {
    it(`${era}: explicit error codes in D3 order, no data, no path/OS text, next request recovers`, async () => {
      const root = tempRoot(`fail-${era}`);
      const path = join(root, 'catalog.json');
      const good = linkedCatalog();
      const goodHash = writeCatalog(path, good);
      const proc = await start({ era, label: `fail-${era}`, entry: 'fixture', catalog: path });
      const seen: Record<string, unknown> = {};
      const expectCode = async (label: string, code: string, tool: ToolName, args: JsonObject) => {
        const exchange = await proc.call(tool, args);
        const envelope = expectError(tool, outcome(exchange), code as Parameters<typeof expectError>[2]);
        expect(exchange.responseLine?.text.includes(root)).toBe(false);
        expect(exchange.responseLine?.text.includes('dawnholder-v2-')).toBe(false);
        expect(exchange.responseLine?.text.includes('SENTINEL_FIXTURE_OS_TEXT')).toBe(false);
        seen[label] = { code: envelope.error.code, retryable: envelope.error.retryable, snapshot: envelope.snapshot === null ? null : 'present', details: envelope.error.details, app: exchange.appBytes };
        return envelope;
      };
      const { rmSync } = await import('node:fs');
      rmSync(path);
      // Missing beats hash/ID checks.
      await expectCode('missing', 'CATALOG_MISSING', 'get_system', { id: 'no-such-id', expectedHash: 'f'.repeat(64) });
      writeFileSync(path, '{"schemaVersion":1, SENTINEL_BROKEN_JSON');
      await expectCode('invalid-json', 'CATALOG_INVALID', 'get_record', { id: 'rec-1', expectedHash: 'f'.repeat(64) });
      writeCatalog(path, { ...good, systems: [...good.systems, { ...good.systems[0]!, id: good.systems[0]!.id }] });
      await expectCode('duplicate-id', 'CATALOG_INVALID', 'list_systems', {});
      const broken = { ...good, records: good.records.map((record, index) => ({ ...record, systemIds: [...record.systemIds, `ghost-${index}`] })) };
      writeCatalog(path, broken);
      const refs = await expectCode('reference', 'CATALOG_REFERENCE_BROKEN', 'search_records', { expectedHash: 'f'.repeat(64) });
      expect(refs.error.details?.count).toBe(good.records.length);
      expect((refs.error.details?.examples as string[]).length).toBeLessThanOrEqual(5);
      writeFileSync(path, Buffer.alloc(2 * 1024 * 1024 + 1, 0x20));
      await expectCode('too-large', 'CATALOG_TOO_LARGE', 'list_systems', {});
      // Recovery on the next request after the file is valid again (no restart).
      writeCatalog(path, good);
      const back = expectSuccess('get_system', outcome(await proc.call('get_system', { id: 'sys-alpha', expectedHash: goodHash })));
      expect(back.snapshot.hash).toBe(goodHash);
      const counters = await proc.counters();
      expect(counters.openAttempts).toBe(counters.readSnapshotCalls);
      expect(counters.closes).toBe(counters.opens);
      console.info(`[V2-MEASURE] failures-real ${era} ${JSON.stringify({ seen, counters: { reads: counters.readSnapshotCalls, openAttempts: counters.openAttempts, openFailures: counters.openFailures, errors: counters.readSnapshotErrors } })}`);
      await closeAndCheck(proc);

      // Deterministic interference with injected I/O over the same stdio path.
      const ctl = await controlled(era, `fail-ctl-${era}`);
      const ctlSeen: Record<string, unknown> = {};
      // Commands go over IPC, requests over stdin: a counters() round trip orders them.
      const ctlCode = async (label: string, code: string, setup: () => void, during: (from: number) => Promise<void> = async () => undefined) => {
        setup();
        await ctl.counters();
        const from = ctl.events.length;
        const pending = ctl.call('get_system', { id: 'sys-alpha' });
        await during(from);
        const exchange = await pending;
        const envelope = expectError('get_system', outcome(exchange), code as Parameters<typeof expectError>[2]);
        expect(exchange.responseLine?.text.includes('SENTINEL_FIXTURE_OS_TEXT')).toBe(false);
        ctlSeen[label] = { code: envelope.error.code, retryable: envelope.error.retryable };
        expectSuccess('get_system', outcome(await ctl.call('get_system', { id: 'sys-beta' })));
      };
      const bigger = catalogBytes({ ...linkedCatalog(), scopeNote: 'grown '.repeat(50) });
      await ctlCode('changed-between-stats', 'CATALOG_CHANGED_DURING_READ', () => ctl.command({ c: 'pause', op: 'read', abortAware: false }), async from => {
        await waitReached(ctl, 'read', from);
        ctl.command({ c: 'setBytes', b64: Buffer.from(bigger).toString('base64') });
        await ctl.waitEvent(event => event.t === 'bytesSet', from);
        ctl.command({ c: 'release', op: 'read' });
      });
      ctl.command({ c: 'setBytes', b64: Buffer.from(catalogBytes(linkedCatalog())).toString('base64') });
      await ctlCode('open-eperm', 'CATALOG_UNREADABLE', () => ctl.command({ c: 'failNext', op: 'open', code: 'EPERM' }));
      await ctlCode('stat-eacces', 'CATALOG_UNREADABLE', () => ctl.command({ c: 'failNext', op: 'stat', code: 'EACCES' }));
      await ctlCode('read-ebusy', 'CATALOG_UNREADABLE', () => ctl.command({ c: 'failNext', op: 'read', code: 'EBUSY' }));
      await ctlCode('open-enoent', 'CATALOG_MISSING', () => ctl.command({ c: 'failNext', op: 'open', code: 'ENOENT' }));
      const ctlCounters = await ctl.counters();
      expect(ctlCounters.closes).toBe(ctlCounters.opens);
      expect(ctlCounters.openAttempts).toBe(ctlCounters.readSnapshotCalls);
      console.info(`[V2-MEASURE] failures-injected ${era} ${JSON.stringify({ ctlSeen, openAttempts: ctlCounters.openAttempts, reads: ctlCounters.readSnapshotCalls, errors: ctlCounters.readSnapshotErrors })}`);
      await closeAndCheck(ctl);
    }, 30_000);
  }
});

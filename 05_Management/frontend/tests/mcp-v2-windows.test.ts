// @vitest-environment node
// V2: real Windows file I/O interference with MCP reads (goal D3, completion condition 3, main
// msg_f1bd05219af4). Two real fixture-entry child processes read a TEMP catalog through the BUILT
// production reader (nodeCatalogFileOperations, real clock, real token bucket) at their 10/s ceiling.
// Criteria: MCP read errors are explicit and the next request recovers, a success is always one
// complete version, the reader never retries internally and its handle is closed before
// search/serialization. Non-atomic in-place edits are a separate real-I/O observation (the
// deterministic V1 tests remain the proof for ordering). The UI store save tests were removed with
// the record index v2 (goal 2026-10-06-record-source-unification 「만들 것」 3).
import { closeSync, openSync, readFileSync, writeFileSync, writeSync } from 'node:fs';
import { spawn } from 'node:child_process';
import { join } from 'node:path';
import { afterAll, afterEach, describe, expect, it } from 'vitest';
import { expectEnvelope } from './mcp-fixtures';
import { CANONICAL_CATALOG, StdioProcess, removeTempRoots, sleep, tempRoot, textHash } from './mcp-v2/harness';
import { closeAndCheck, outcome, processPool } from './mcp-v2/support';

const pool = processPool();
afterEach(async () => { await pool.closeAll(); });
afterAll(() => { removeTempRoots(); });

// A realistic-size fixture: the canonical catalog's text (read-only) with a varying marker. Record
// index v2 rejects any extra key (index-v2-design.md 「색인 형식」), so the marker goes into the first
// system's title instead of a hand-written revision.
const BASE = JSON.parse(readFileSync(CANONICAL_CATALOG, 'utf8')) as { systems: Array<{ title: string }> } & Record<string, unknown>;
const versionText = (marker: string) => JSON.stringify({
  ...BASE, systems: BASE.systems.map((system, index) => (index === 0 ? { ...system, title: `${system.title} ${marker}` } : system)),
}, null, 2);

interface ReaderStats {
  calls: number; ok: number; codes: Record<string, number>; unknownHashes: number; afterErrorRecovered: number; afterErrorPending: boolean;
  maxConsecutiveErrors: number; consecutive: number; admitted?: number; admittedPerSecond?: number; durationMs?: number;
}
function newStats(): ReaderStats {
  return { calls: 0, ok: 0, codes: {}, unknownHashes: 0, afterErrorRecovered: 0, afterErrorPending: false, maxConsecutiveErrors: 0, consecutive: 0 };
}

// Calls as fast as the per-process bucket admits (waits retryAfterMs on RATE_LIMITED).
async function readLoop(proc: StdioProcess, known: Set<string>, stats: ReaderStats, running: () => boolean) {
  const started = performance.now();
  let index = 0;
  while (running()) {
    const tool = index % 2 ? 'get_system' : 'list_systems';
    const exchange = await proc.call(tool, index % 2 ? { id: 'combat' } : { query: `w${index % 7}` });
    index += 1;
    stats.calls += 1;
    const envelope = expectEnvelope(tool, outcome(exchange));
    if (envelope.ok) {
      stats.ok += 1;
      if (!known.has(envelope.snapshot.hash)) stats.unknownHashes += 1;
      if (stats.afterErrorPending) { stats.afterErrorRecovered += 1; stats.afterErrorPending = false; }
      stats.consecutive = 0;
      continue;
    }
    stats.codes[envelope.error.code] = (stats.codes[envelope.error.code] ?? 0) + 1;
    if (envelope.error.code === 'RATE_LIMITED') { await sleep(Number(envelope.error.details?.retryAfterMs ?? 100)); continue; }
    stats.afterErrorPending = true;
    stats.consecutive += 1;
    stats.maxConsecutiveErrors = Math.max(stats.maxConsecutiveErrors, stats.consecutive);
  }
  stats.durationMs = Math.round(performance.now() - started);
  stats.admitted = stats.calls - (stats.codes.RATE_LIMITED ?? 0);
  stats.admittedPerSecond = Number((stats.admitted / (stats.durationMs / 1000)).toFixed(2));
}

// After a ceiling-load loop the bucket may be empty: honour retryAfterMs, then report the
// first admitted (non-RATE_LIMITED) envelope.
async function admitted(proc: StdioProcess, query: string) {
  for (let attempt = 0; attempt < 30; attempt += 1) {
    const envelope = expectEnvelope('list_systems', outcome(await proc.call('list_systems', { query: `${query}-${attempt}` })));
    if (envelope.ok || envelope.error.code !== 'RATE_LIMITED') return envelope;
    await sleep(Number(envelope.error.details?.retryAfterMs ?? 100));
  }
  throw new Error('never admitted');
}

describe('Windows real file I/O: MCP reads (two processes at 10/s)', () => {
  it('real sharing violation: an exclusive (FileShare.None) handle makes MCP reads CATALOG_UNREADABLE (retryable, no data, one open attempt) and the next request after release recovers', async () => {
    const root = tempRoot('win-exclusive');
    const catalogPath = join(root, 'catalog.json');
    writeFileSync(catalogPath, versionText('v2-exclusive'), 'utf8');
    const procs = await Promise.all([
      pool.start({ era: 'legacy', label: 'excl-legacy', entry: 'fixture', catalog: catalogPath }),
      pool.start({ era: 'modern', label: 'excl-modern', entry: 'fixture', catalog: catalogPath }),
    ]);
    for (const proc of procs) expect(expectEnvelope('list_systems', outcome(await proc.call('list_systems', {}))).ok).toBe(true);
    // A separate OS process holds the file with no sharing until told to release it.
    const quoted = catalogPath.replaceAll("'", "''");
    const script = `$f = [System.IO.File]::Open('${quoted}', 'Open', 'ReadWrite', 'None'); [Console]::Out.WriteLine('locked'); [Console]::Out.Flush(); [void][Console]::In.ReadLine(); $f.Close()`;
    const holder = spawn('powershell.exe', ['-NoProfile', '-NonInteractive', '-Command', script], { stdio: ['pipe', 'pipe', 'pipe'], windowsHide: true });
    let holderOut = '';
    holder.stdout.on('data', (chunk: Buffer) => { holderOut += chunk.toString('utf8'); });
    const holderExit = new Promise<number | null>(done => holder.once('exit', code => done(code)));
    const deadline = performance.now() + 15_000;
    while (!holderOut.includes('locked')) { if (performance.now() > deadline) throw new Error('lock holder did not start'); await sleep(20); }
    const during: Array<{ era: string; code: string; retryable: boolean; data: boolean }> = [];
    for (const proc of procs) {
      const exchange = await proc.call('get_system', { id: 'combat' });
      const envelope = expectEnvelope('get_system', outcome(exchange));
      expect(envelope.ok).toBe(false);
      if (!envelope.ok) {
        expect(envelope.error.code).toBe('CATALOG_UNREADABLE');
        expect(envelope.error.retryable).toBe(true);
        during.push({ era: proc.options.era, code: envelope.error.code, retryable: envelope.error.retryable, data: 'data' in envelope });
      }
      expect(exchange.responseLine?.text.includes(root)).toBe(false);
    }
    holder.stdin.write('release\n');
    expect(await holderExit).toBe(0);
    for (const proc of procs) expect(expectEnvelope('get_system', outcome(await proc.call('get_system', { id: 'combat' }))).ok).toBe(true);
    const counters = await Promise.all(procs.map(proc => proc.counters()));
    for (const item of counters) {
      expect(item.openAttempts).toBe(item.readSnapshotCalls);
      expect(item.readSnapshotErrors.CATALOG_UNREADABLE).toBe(1);
      expect(item.closes).toBe(item.opens);
    }
    console.info(`[V2-MEASURE] windows-exclusive ${JSON.stringify({ during, counters: counters.map(item => ({ reads: item.readSnapshotCalls, openAttempts: item.openAttempts, openFailures: item.openFailures, errors: item.readSnapshotErrors })) })}`);
    for (const proc of procs) await closeAndCheck(proc);
  }, 60_000);

  it('real I/O observation: non-atomic in-place rewrites/growth while two processes read never yield a non-version success', async () => {
    const root = tempRoot('win-nonatomic');
    const catalogPath = join(root, 'catalog.json');
    const versions = [versionText('v2-na-a'), versionText('v2-na-bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb')];
    writeFileSync(catalogPath, versions[0] as string, 'utf8');
    const known = new Set(versions.map(text => textHash(Buffer.from(text, 'utf8'))));
    const procs = await Promise.all([
      pool.start({ era: 'legacy', label: 'na-legacy', entry: 'fixture', catalog: catalogPath, clock: 'real' }),
      pool.start({ era: 'modern', label: 'na-modern', entry: 'fixture', catalog: catalogPath, clock: 'real' }),
    ]);
    let running = true;
    const stats = [newStats(), newStats()];
    const loops = procs.map((proc, index) => readLoop(proc, known, stats[index]!, () => running));
    const writer = { rewrites: 0, failures: {} as Record<string, number> };
    for (let index = 0; index < 120; index += 1) {
      const text = versions[index % 2] as string;
      const bytes = Buffer.from(text, 'utf8');
      try {
        // In-place truncate + two chunked writes with a gap: a visible partial window.
        const fd = openSync(catalogPath, 'w');
        try {
          writeSync(fd, bytes, 0, bytes.length >> 1);
          await sleep(4);
          writeSync(fd, bytes, bytes.length >> 1, bytes.length - (bytes.length >> 1));
        } finally { closeSync(fd); }
        writer.rewrites += 1;
      } catch (error) {
        const code = (error as { code?: string }).code ?? 'UNKNOWN';
        writer.failures[code] = (writer.failures[code] ?? 0) + 1;
      }
      await sleep(20);
    }
    running = false;
    await Promise.all(loops);
    writeFileSync(catalogPath, versions[0] as string, 'utf8');
    for (const proc of procs) expect((await admitted(proc, 'recovered')).ok).toBe(true);
    const counters = await Promise.all(procs.map(proc => proc.counters()));
    console.info(`[V2-MEASURE] windows-nonatomic ${JSON.stringify({ writer, readers: stats, counters: counters.map(item => ({ reads: item.readSnapshotCalls, errors: item.readSnapshotErrors, openAttempts: item.openAttempts, openFailures: item.openFailures, opens: item.opens, closes: item.closes, openAtSettle: item.openAtSettle })) })}`);
    for (const item of stats) {
      expect(item.unknownHashes, 'partial/non-version success').toBe(0);
      for (const code of Object.keys(item.codes)) expect(['RATE_LIMITED', 'CATALOG_UNREADABLE', 'CATALOG_CHANGED_DURING_READ', 'CATALOG_INVALID', 'CATALOG_MISSING']).toContain(code);
    }
    for (const item of counters) { expect(item.closes).toBe(item.opens); expect(item.openAtSettle).toBe(0); expect(item.openAttempts).toBe(item.readSnapshotCalls); }
    for (const proc of procs) await closeAndCheck(proc);
  }, 60_000);
});

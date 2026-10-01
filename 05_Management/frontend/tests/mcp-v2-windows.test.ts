// @vitest-environment node
// V2: real Windows file I/O interference between MCP reads and the UI store save (goal D3,
// completion condition 3, main msg_f1bd05219af4). Two real fixture-entry child processes
// read a TEMP catalog through the BUILT production reader (nodeCatalogFileOperations, real
// clock, real token bucket) at their 10/s ceiling while this process runs the real
// createCatalogStore.save (atomic rename, backup, lock, conflict) with an attempt observer.
// Criteria: every save succeeds under MCP load (a save failure is a preservation regression),
// rename attempt distribution recorded, MCP read errors are explicit and the next request
// recovers, a success is always one complete version, the reader never retries internally
// and its handle is closed before search/serialization. Non-atomic in-place edits are a
// separate real-I/O observation (the deterministic V1 tests remain the proof for ordering).
import { closeSync, existsSync, openSync, readdirSync, readFileSync, writeFileSync, writeSync } from 'node:fs';
import { spawn } from 'node:child_process';
import { mkdir, open as openFile, rename as nativeRename, unlink } from 'node:fs/promises';
import { join } from 'node:path';
import { afterAll, afterEach, describe, expect, it } from 'vitest';
import type { CatalogRenameObservation } from '../electron/catalog-rename';
import { createCatalogStore } from '../electron/catalog-store';
import { expectEnvelope } from './mcp-fixtures';
import { CANONICAL_CATALOG, StdioProcess, removeTempRoots, sleep, tempRoot, textHash } from './mcp-v2/harness';
import { closeAndCheck, outcome, processPool } from './mcp-v2/support';

const pool = processPool();
afterEach(async () => { await pool.closeAll(); });
afterAll(() => { removeTempRoots(); });

// A realistic-size fixture: the canonical catalog's text (read-only) with a varying revision.
const BASE = JSON.parse(readFileSync(CANONICAL_CATALOG, 'utf8')) as Record<string, unknown>;
const versionText = (revision: string) => JSON.stringify({ ...BASE, revision }, null, 2);

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

function distribution(observations: Array<CatalogRenameObservation & { save: number }>) {
  const finalAttempts: Record<number, number> = {};
  const failedCodes: Record<string, number> = {};
  const bySave = new Map<number, number>();
  for (const item of observations) {
    bySave.set(item.save, Math.max(bySave.get(item.save) ?? 0, item.attempt));
    if (item.outcome === 'failed') failedCodes[item.code ?? 'none'] = (failedCodes[item.code ?? 'none'] ?? 0) + 1;
  }
  for (const attempts of bySave.values()) finalAttempts[attempts] = (finalAttempts[attempts] ?? 0) + 1;
  const retried = observations.filter(item => item.outcome === 'succeeded' && item.attempt > 1).map(item => Math.round(item.elapsedMs));
  return { finalAttempts, failedCodes, retriedSaveElapsedMs: retried };
}

function leftovers(directory: string) {
  return readdirSync(directory).filter(name => name.endsWith('.tmp') || name === '.catalog.lock');
}

describe('Windows real file I/O: MCP reads (two processes at 10/s) vs createCatalogStore.save', () => {
  it('control, then 200 saves under load: all succeed, backup/lock correct, reads explicit/recovering, attempts distribution recorded', async () => {
    const root = tempRoot('win-load');
    const records = join(root, 'records');
    const backups = join(root, 'backups');
    await mkdir(records, { recursive: true });
    const catalogPath = join(records, 'catalog.json');
    const backupPath = join(backups, 'catalog.backup.json');
    const initial = versionText('v2-win-initial');
    writeFileSync(catalogPath, initial, 'utf8');
    const known = new Set([textHash(Buffer.from(initial, 'utf8'))]);
    const observations: Array<CatalogRenameObservation & { save: number }> = [];
    let current = -1;
    const store = createCatalogStore(catalogPath, backupPath, { onAttempt: observation => { observations.push({ save: current, ...observation }); } });
    let baseline = await store.read();
    if (!baseline.ok) throw new Error('fixture unreadable');

    const saveSeries = async (prefix: string, count: number, gapMs: number) => {
      const results: Array<{ index: number; ok: boolean; code?: string; ms: number }> = [];
      for (let index = 0; index < count; index += 1) {
        current = results.length + (prefix === 'load' ? 1_000 : 0);
        const previousText = baseline.ok ? baseline.text : '';
        const text = versionText(`v2-${prefix}-${index}`);
        known.add(textHash(Buffer.from(text, 'utf8')));
        const started = performance.now();
        const result = await store.save({ text, expectedVersion: baseline.ok ? baseline.version : null });
        const ms = Math.round(performance.now() - started);
        if (result.ok) {
          // Backup holds the previous version; the lock is released; the catalog is the new text.
          expect(readFileSync(backupPath, 'utf8')).toBe(previousText);
          expect(existsSync(join(records, '.catalog.lock'))).toBe(false);
          baseline = result;
          results.push({ index, ok: true, ms });
        } else {
          results.push({ index, ok: false, code: result.code, ms });
          baseline = await store.read();
        }
        if (gapMs) await sleep(gapMs);
      }
      return results;
    };

    // Control: no MCP readers.
    const control = await saveSeries('control', 50, 0);
    const controlDistribution = distribution(observations.filter(item => item.save < 1_000));

    // Load: two real MCP child processes reading at their per-process ceiling.
    const procs = await Promise.all([
      pool.start({ era: 'legacy', label: 'win-legacy', entry: 'fixture', catalog: catalogPath, clock: 'real' }),
      pool.start({ era: 'modern', label: 'win-modern', entry: 'fixture', catalog: catalogPath, clock: 'real' }),
    ]);
    let running = true;
    const stats = [newStats(), newStats()];
    const loops = procs.map((proc, index) => readLoop(proc, known, stats[index]!, () => running));
    await sleep(300);
    const load = await saveSeries('load', 200, 15);
    await sleep(300);
    running = false;
    await Promise.all(loops);
    const loadDistribution = distribution(observations.filter(item => item.save >= 1_000));

    // Every read error was followed by a successful request from the same client.
    for (const proc of procs) expect((await admitted(proc, 'final')).ok).toBe(true);
    const counters = await Promise.all(procs.map(proc => proc.counters()));
    const report = {
      control: { saves: control.length, ok: control.filter(item => item.ok).length, failures: control.filter(item => !item.ok), distribution: controlDistribution, maxSaveMs: Math.max(...control.map(item => item.ms)) },
      load: { saves: load.length, ok: load.filter(item => item.ok).length, failures: load.filter(item => !item.ok), distribution: loadDistribution, maxSaveMs: Math.max(...load.map(item => item.ms)) },
      readers: stats.map((item, index) => ({ era: procs[index]?.options.era, ...item })),
      counters: counters.map(item => ({ factoryCalls: item.factoryCalls, entered: item.entered, readSnapshotCalls: item.readSnapshotCalls, ok: item.readSnapshotOk, errors: item.readSnapshotErrors, openAttempts: item.openAttempts, openFailures: item.openFailures, opens: item.opens, closes: item.closes, openAtSettle: item.openAtSettle, maxConcurrentReads: item.maxConcurrentReads })),
    };
    console.info(`[V2-MEASURE] windows-load ${JSON.stringify(report)}`);

    expect(control.every(item => item.ok)).toBe(true);
    expect(load.filter(item => !item.ok), 'save failed under MCP read load').toEqual([]);
    for (const item of stats) {
      expect(item.unknownHashes, 'a read returned a non-version snapshot').toBe(0);
      expect(item.codes.CATALOG_INVALID ?? 0, 'partial content observed through atomic rename').toBe(0);
      for (const code of Object.keys(item.codes)) expect(['RATE_LIMITED', 'CATALOG_UNREADABLE', 'CATALOG_CHANGED_DURING_READ']).toContain(code);
      expect(item.ok).toBeGreaterThan(20);
      // Ceiling load: admitted reads follow the bucket (10 initial + 10/s), and stay near it.
      expect(item.admitted as number).toBeLessThanOrEqual(10 + Math.ceil((item.durationMs as number) / 100) + 2);
      expect(item.admitted as number).toBeGreaterThanOrEqual(Math.floor(0.8 * (item.durationMs as number) / 100));
    }
    for (const item of counters) {
      expect(item.factoryCalls).toBe(1);
      // No hidden reader retry, no cache: exactly one open per readSnapshot, all closed,
      // and no handle still open when readSnapshot settled.
      expect(item.openAttempts).toBe(item.readSnapshotCalls);
      expect(item.closes).toBe(item.opens);
      expect(item.openAtSettle).toBe(0);
    }
    expect(leftovers(records)).toEqual([]);
    expect(leftovers(backups)).toEqual([]);
    for (const proc of procs) await closeAndCheck(proc);
  }, 120_000);

  it('real I/O: stale version conflicts and a held lock is busy (both preserve catalog/backup/lock); exhaustion and non-retryable errors keep the write failure', async () => {
    const root = tempRoot('win-sem');
    const records = join(root, 'records');
    await mkdir(records, { recursive: true });
    const catalogPath = join(records, 'catalog.json');
    const backupPath = join(root, 'backups', 'catalog.backup.json');
    writeFileSync(catalogPath, versionText('v2-sem-0'), 'utf8');
    const store = createCatalogStore(catalogPath, backupPath);
    const first = await store.read();
    if (!first.ok) throw new Error('fixture unreadable');
    const second = await store.save({ text: versionText('v2-sem-1'), expectedVersion: first.version });
    if (!second.ok) throw new Error('first save failed');
    const snapshot = () => ({ catalog: readFileSync(catalogPath, 'utf8'), backup: readFileSync(backupPath, 'utf8') });

    // Stale expectedVersion -> conflict, nothing written.
    const beforeConflict = snapshot();
    const conflict = await store.save({ text: versionText('v2-sem-stale'), expectedVersion: first.version });
    expect(conflict.ok).toBe(false);
    if (!conflict.ok) expect(conflict.code).toBe('conflict');
    expect(snapshot()).toEqual(beforeConflict);
    expect(leftovers(records)).toEqual([]);

    // Existing lock -> busy, nothing written, the lock is NOT auto-deleted.
    writeFileSync(join(records, '.catalog.lock'), '');
    const busy = await store.save({ text: versionText('v2-sem-busy'), expectedVersion: second.version });
    expect(busy.ok).toBe(false);
    if (!busy.ok) expect(busy.code).toBe('busy');
    expect(snapshot()).toEqual(beforeConflict);
    expect(existsSync(join(records, '.catalog.lock'))).toBe(true);
    await unlink(join(records, '.catalog.lock'));

    // Forced sharing violation: hold a real read handle on the catalog during the save.
    const held: Array<CatalogRenameObservation> = [];
    const heldStore = createCatalogStore(catalogPath, backupPath, { onAttempt: observation => { held.push(observation); } });
    const handle = await openFile(catalogPath, 'r');
    const beforeHeld = snapshot();
    const draft = versionText('v2-sem-held');
    const heldStarted = performance.now();
    const heldResult = await heldStore.save({ text: draft, expectedVersion: second.version });
    const heldMs = Math.round(performance.now() - heldStarted);
    await handle.close();
    const heldReport = { ok: heldResult.ok, code: heldResult.ok ? null : heldResult.code, ms: heldMs, attempts: held.length, codes: [...new Set(held.map(item => item.code))], lastElapsed: held.at(-1)?.elapsedMs };
    console.info(`[V2-MEASURE] windows-held-handle ${JSON.stringify(heldReport)}`);
    if (heldResult.ok) {
      // This OS let rename replace a file with an open read handle: no exhaustion to observe.
      expect(held.at(-1)?.outcome).toBe('succeeded');
    } else {
      expect(heldResult.code).toBe('write');
      expect(held.every(item => item.outcome === 'failed' && ['EPERM', 'EACCES', 'EBUSY'].includes(item.code ?? ''))).toBe(true);
      expect(Math.max(...held.map(item => item.elapsedMs))).toBeLessThan(1_000);
      expect(heldMs).toBeGreaterThanOrEqual(990);
      expect(heldMs).toBeLessThan(2_000);
      expect(held.length).toBeGreaterThan(5);
      // Catalog unchanged; backup holds the previous (= current) version; lock/temp cleaned.
      expect(readFileSync(catalogPath, 'utf8')).toBe(beforeHeld.catalog);
      expect(readFileSync(backupPath, 'utf8')).toBe(beforeHeld.catalog);
      expect(leftovers(records)).toEqual([]);
      expect(draft).toBe(versionText('v2-sem-held'));
      // Recovery once the handle is gone.
      const recovered = await heldStore.save({ text: draft, expectedVersion: second.version });
      expect(recovered.ok).toBe(true);
    }

    // Non-retryable rename error (EIO) with real time: one attempt, immediate write failure.
    const current = await store.read();
    if (!current.ok) throw new Error('unreadable');
    const eio: CatalogRenameObservation[] = [];
    const eioStore = createCatalogStore(catalogPath, backupPath, {
      onAttempt: observation => { eio.push(observation); },
      rename: async (source, target) => {
        if (target === catalogPath) throw Object.assign(new Error('SENTINEL EIO'), { code: 'EIO' });
        await nativeRename(source, target);
      },
    });
    const beforeEio = readFileSync(catalogPath, 'utf8');
    const eioStarted = performance.now();
    const eioResult = await eioStore.save({ text: versionText('v2-sem-eio'), expectedVersion: current.version });
    const eioMs = Math.round(performance.now() - eioStarted);
    expect(eioResult.ok).toBe(false);
    if (!eioResult.ok) expect(eioResult.code).toBe('write');
    expect(eio).toHaveLength(1);
    expect(eio[0]?.code).toBe('EIO');
    expect(eioMs).toBeLessThan(500);
    expect(readFileSync(catalogPath, 'utf8')).toBe(beforeEio);
    expect(leftovers(records)).toEqual([]);
    console.info(`[V2-MEASURE] windows-eio ${JSON.stringify({ attempts: eio.length, ms: eioMs })}`);
  }, 60_000);

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

// @vitest-environment node
// V1: internal contract of the approved Windows save correction. Only the final catalog
// rename retries EPERM/EACCES/EBUSY with 10→20→40 ms capped backoff and a monotonic 1 s
// deadline for starting attempts; exhaustion keeps the existing write failure. Virtual
// clock and injected rename only; real save/MCP contention is V2/V3.
import { mkdir, mkdtemp, readFile, readdir, rename as nativeRename, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { createCatalogRenamer, CATALOG_RENAME_RETRY_MS, type CatalogRenameObservation } from '../electron/catalog-rename';
import { createCatalogStore } from '../electron/catalog-store';
import { linkedCatalog, makeCatalog, osError, sha256 } from './mcp-fixtures';

// Virtual time advances only through wait() or an explicit per-attempt cost.
function virtualRename(script: Array<string | null | (() => void)>, options: { attemptCostMs?: number[] } = {}) {
  let time = 0;
  const waits: number[] = [];
  const observations: CatalogRenameObservation[] = [];
  const calls: Array<[string, string]> = [];
  const errors: unknown[] = [];
  const renamer = createCatalogRenamer({
    now: () => time,
    wait: async milliseconds => { waits.push(milliseconds); time += milliseconds; },
    onAttempt: observation => { observations.push(observation); },
    rename: async (source, target) => {
      calls.push([source, target]);
      time += options.attemptCostMs?.[calls.length - 1] ?? 0;
      const step = script.length ? script.shift() : script.at(-1);
      if (typeof step === 'function') { step(); return; }
      if (step === null || step === undefined) return;
      const error = osError(step);
      errors.push(error);
      throw error;
    },
  });
  return { renamer, waits, observations, calls, errors, time: () => time };
}

function alwaysFailing(code: string) {
  return virtualRename(Array.from({ length: 1_000 }, () => code));
}

describe('createCatalogRenamer', () => {
  it('uses a 1000 ms retry window constant', () => {
    expect(CATALOG_RENAME_RETRY_MS).toBe(1000);
  });

  for (const code of ['EPERM', 'EACCES', 'EBUSY']) {
    it(`${code}: 10→20→40 ms capped backoff, no attempt starts at or after 1000 ms, last error rethrown`, async () => {
      const run = alwaysFailing(code);
      const thrown = await run.renamer('tmp', 'catalog').then(() => undefined, (error: unknown) => error);
      const expectedStarts = [0, 10, ...Array.from({ length: 25 }, (_, index) => 30 + 40 * index)];
      expect(run.calls).toHaveLength(27);
      expect(run.observations.map(item => item.elapsedMs)).toEqual(expectedStarts);
      expect(run.observations.every(item => item.outcome === 'failed' && item.code === code)).toBe(true);
      expect(run.observations.map(item => item.attempt)).toEqual(expectedStarts.map((_, index) => index + 1));
      expect(run.waits).toEqual([10, 20, ...Array.from({ length: 24 }, () => 40), 10]);
      expect(run.waits.reduce((sum, value) => sum + value, 0)).toBe(1000);
      expect(Math.max(...run.observations.map(item => item.elapsedMs))).toBeLessThan(1000);
      expect(thrown).toBe(run.errors.at(-1));
    });
  }

  for (const code of ['EIO', 'ENOENT', 'EEXIST', 'EXDEV', 'ENOSPC']) {
    it(`${code} is not transient: one attempt, no wait`, async () => {
      const run = virtualRename([code]);
      await expect(run.renamer('tmp', 'catalog')).rejects.toBe(run.errors[0]);
      expect(run.calls).toHaveLength(1);
      expect(run.waits).toEqual([]);
      expect(run.observations).toEqual([{ attempt: 1, elapsedMs: 0, outcome: 'failed', code }]);
    });
  }

  it('an error without a string code is not retried', async () => {
    const failure = new Error('no code');
    const renamer = createCatalogRenamer({ now: () => 0, wait: async () => { throw new Error('must not wait'); }, rename: async () => { throw failure; } });
    await expect(renamer('tmp', 'catalog')).rejects.toBe(failure);
    const numeric = Object.assign(new Error('numeric code'), { code: -4048 });
    const observations: CatalogRenameObservation[] = [];
    const second = createCatalogRenamer({ now: () => 0, wait: async () => { throw new Error('must not wait'); }, rename: async () => { throw numeric; }, onAttempt: item => observations.push(item) });
    await expect(second('tmp', 'catalog')).rejects.toBe(numeric);
    expect(observations).toEqual([{ attempt: 1, elapsedMs: 0, outcome: 'failed' }]);
  });

  it('recovers on a later attempt and reports the attempt distribution', async () => {
    const run = virtualRename(['EPERM', 'EBUSY', 'EACCES', null]);
    await run.renamer('tmp', 'catalog');
    expect(run.waits).toEqual([10, 20, 40]);
    expect(run.observations).toEqual([
      { attempt: 1, elapsedMs: 0, outcome: 'failed', code: 'EPERM' },
      { attempt: 2, elapsedMs: 10, outcome: 'failed', code: 'EBUSY' },
      { attempt: 3, elapsedMs: 30, outcome: 'failed', code: 'EACCES' },
      { attempt: 4, elapsedMs: 70, outcome: 'succeeded' },
    ]);
  });

  it('a transient failure followed by a non-transient one stops immediately with the latter', async () => {
    const run = virtualRename(['EPERM', 'EIO']);
    const thrown = await run.renamer('tmp', 'catalog').then(() => undefined, (error: unknown) => error);
    expect(thrown).toBe(run.errors[1]);
    expect((thrown as { code?: string }).code).toBe('EIO');
    expect(run.waits).toEqual([10]);
    expect(run.calls).toHaveLength(2);
  });

  it('a slow attempt that ends past the deadline is not followed by another attempt (OS latency is not cut short)', async () => {
    const run = virtualRename(['EPERM', 'EPERM'], { attemptCostMs: [1_500] });
    await expect(run.renamer('tmp', 'catalog')).rejects.toBe(run.errors[0]);
    expect(run.calls).toHaveLength(1);
    expect(run.waits).toEqual([]);
    expect(run.observations).toEqual([{ attempt: 1, elapsedMs: 1_500, outcome: 'failed', code: 'EPERM' }]);
  });

  it('the last wait is clipped to the remaining window and no attempt follows it', async () => {
    const run = virtualRename(['EBUSY', 'EBUSY'], { attemptCostMs: [995] });
    await expect(run.renamer('tmp', 'catalog')).rejects.toBe(run.errors[0]);
    expect(run.waits).toEqual([5]);
    expect(run.calls).toHaveLength(1);
    expect(run.time()).toBe(1_000);
  });
});

describe('createCatalogStore with CatalogRenameOptions', () => {
  let root: string;
  let path: string;
  let backup: string;
  const original = JSON.stringify(linkedCatalog(), null, 2);
  const next = JSON.stringify(makeCatalog({ revision: 'v1-rename-next' }), null, 2);
  const textVersion = (text: string) => sha256(Buffer.from(text, 'utf8'));

  beforeEach(async () => {
    root = await mkdtemp(join(tmpdir(), 'dawnholder-v1-rename-'));
    path = join(root, 'records', 'catalog.json');
    backup = join(root, 'backup', 'last-good.json');
    await mkdir(join(root, 'records'));
    await writeFile(path, original);
  });
  afterEach(async () => {
    expect(resolve(root).startsWith(join(resolve(tmpdir()), 'dawnholder-v1-rename-'))).toBe(true);
    await rm(root, { recursive: true, force: true });
  });

  it('applies only to the final catalog rename; backup rename stays native; temp and lock are cleaned', async () => {
    const calls: Array<[string, string]> = [];
    const observations: CatalogRenameObservation[] = [];
    const store = createCatalogStore(path, backup, { rename: async (source, target) => { calls.push([source, target]); await nativeRename(source, target); }, onAttempt: item => observations.push(item) });
    const result = await store.save({ text: next, expectedVersion: textVersion(original) });
    expect(result).toMatchObject({ ok: true, version: textVersion(next) });
    expect(calls).toHaveLength(1);
    expect(calls[0]?.[1]).toBe(path);
    expect(calls[0]?.[0]).toMatch(/[\\/]records[\\/]\.catalog-[0-9a-f-]{36}\.tmp$/);
    expect(observations).toEqual([{ attempt: 1, elapsedMs: expect.any(Number), outcome: 'succeeded' }]);
    expect(await readFile(path, 'utf8')).toBe(next);
    expect(await readFile(backup, 'utf8')).toBe(original);
    expect(await readdir(join(root, 'records'))).toEqual(['catalog.json']);
    expect((await readdir(join(root, 'backup'))).sort()).toEqual(['last-good.json']);
  });

  it('transient failures then success: saved, previous text backed up, attempt distribution observed', async () => {
    let time = 0;
    const observations: CatalogRenameObservation[] = [];
    const failures = ['EPERM', 'EBUSY'];
    const store = createCatalogStore(path, backup, {
      now: () => time, wait: async milliseconds => { time += milliseconds; }, onAttempt: item => observations.push(item),
      rename: async (source, target) => { const code = failures.shift(); if (code) throw osError(code); await nativeRename(source, target); },
    });
    expect(await store.save({ text: next, expectedVersion: textVersion(original) })).toMatchObject({ ok: true, version: textVersion(next) });
    expect(observations.map(item => [item.attempt, item.outcome, item.code ?? null])).toEqual([[1, 'failed', 'EPERM'], [2, 'failed', 'EBUSY'], [3, 'succeeded', null]]);
    expect(await readFile(path, 'utf8')).toBe(next);
    expect(await readFile(backup, 'utf8')).toBe(original);
    expect(await readdir(join(root, 'records'))).toEqual(['catalog.json']);
  });

  it('exhaustion returns the existing write failure and preserves catalog bytes, prepared backup and caller draft; lock and temp are removed', async () => {
    let time = 0;
    let attempts = 0;
    const input = { text: next, expectedVersion: textVersion(original) };
    const frozenInput = JSON.stringify(input);
    const store = createCatalogStore(path, backup, {
      now: () => time, wait: async milliseconds => { time += milliseconds; },
      rename: async () => { attempts += 1; throw osError('EBUSY'); },
    });
    const result = await store.save(input);
    expect(result).toEqual({ ok: false, code: 'write', message: '기록 또는 복구용 백업을 저장하지 못했습니다. 현재 기록과 초안을 보존합니다.' });
    expect(JSON.stringify(result).includes('SENTINEL')).toBe(false);
    expect(attempts).toBe(27);
    expect(time).toBe(1_000);
    expect(await readFile(path, 'utf8')).toBe(original);
    expect(await readFile(backup, 'utf8')).toBe(original);
    expect(JSON.stringify(input)).toBe(frozenInput);
    expect(await readdir(join(root, 'records'))).toEqual(['catalog.json']);
    // The store still works afterwards (no leftover lock).
    expect(await createCatalogStore(path, backup).save({ text: next, expectedVersion: textVersion(original) })).toMatchObject({ ok: true });
  });

  it('keeps the lock during retries: a concurrent save is busy, and the lock is released afterwards', async () => {
    let release!: () => void;
    let reached!: () => void;
    const reachedPromise = new Promise<void>(done => { reached = done; });
    const gate = new Promise<void>(done => { release = done; });
    let first = true;
    const store = createCatalogStore(path, backup, {
      now: () => 0, wait: async () => undefined,
      rename: async (source, target) => {
        if (first) { first = false; reached(); await gate; throw osError('EPERM'); }
        await nativeRename(source, target);
      },
    });
    const saving = store.save({ text: next, expectedVersion: textVersion(original) });
    await reachedPromise;
    expect(await readdir(join(root, 'records'))).toContain('.catalog.lock');
    const competing = await createCatalogStore(path, backup).save({ text: original, expectedVersion: textVersion(original) });
    expect(competing).toMatchObject({ ok: false, code: 'busy' });
    release();
    expect(await saving).toMatchObject({ ok: true, version: textVersion(next) });
    expect(await readdir(join(root, 'records'))).toEqual(['catalog.json']);
  });

  it('a version conflict never reaches the rename; production default (no options) still saves', async () => {
    let renames = 0;
    const store = createCatalogStore(path, backup, { rename: async () => { renames += 1; } });
    expect(await store.save({ text: next, expectedVersion: 'a'.repeat(64) })).toMatchObject({ ok: false, code: 'conflict' });
    expect(renames).toBe(0);
    expect(await readFile(path, 'utf8')).toBe(original);
    expect(await createCatalogStore(path, backup).save({ text: next, expectedVersion: textVersion(original) })).toMatchObject({ ok: true, version: textVersion(next) });
  });
});

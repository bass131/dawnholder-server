// @vitest-environment node
// V1: internal contract of the approved Windows save correction. Only the final catalog
// rename retries EPERM/EACCES/EBUSY; new attempts start before a monotonic 1 s deadline and
// exhaustion keeps the existing write failure. Virtual clock and injected rename only; real
// save/MCP contention is V2/V3.
// V3-R1 (R08): judged by the requirement invariants in ./mcp-v3-r1/rename-contract, not by the
// product's backoff schedule (10→20→40 ms is an implementation choice). The schedule is logged.
import { mkdir, mkdtemp, readFile, readdir, rename as nativeRename, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { createCatalogRenamer, type CatalogRenameObservation } from '../electron/catalog-rename';
import { createCatalogStore } from '../electron/catalog-store';
import { linkedCatalog, makeCatalog, osError, sha256 } from './mcp-fixtures';
import { RENAME_SCENARIOS, RETRY_START_DEADLINE_MS, scheduleOf, type CreateRenamer } from './mcp-v3-r1/rename-contract';

const create: CreateRenamer = options => createCatalogRenamer(options);

describe('createCatalogRenamer', () => {
  for (const scenario of RENAME_SCENARIOS) {
    it(scenario.title, async () => {
      const run = await scenario.check(create);
      if (run) console.info(`[V3-R1-MEASURE] rename-schedule ${JSON.stringify({ scenario: scenario.title.split(':')[0], ...scheduleOf(run) })}`);
    });
  }
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
    const starts: number[] = [];
    const input = { text: next, expectedVersion: textVersion(original) };
    const frozenInput = JSON.stringify(input);
    const store = createCatalogStore(path, backup, {
      now: () => time, wait: async milliseconds => { time += milliseconds; },
      rename: async () => { starts.push(time); throw osError('EBUSY'); },
    });
    const result = await store.save(input);
    expect(result).toEqual({ ok: false, code: 'write', message: '기록 또는 복구용 백업을 저장하지 못했습니다. 현재 기록과 초안을 보존합니다.' });
    expect(JSON.stringify(result).includes('SENTINEL')).toBe(false);
    expect(starts.length).toBeGreaterThanOrEqual(2);
    expect(Math.max(...starts)).toBeLessThan(RETRY_START_DEADLINE_MS);
    expect(time).toBeLessThanOrEqual(RETRY_START_DEADLINE_MS);
    console.info(`[V3-R1-MEASURE] store-exhaustion ${JSON.stringify({ attempts: starts.length, settledAt: time })}`);
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

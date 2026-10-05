// @vitest-environment node
import { createHash } from 'node:crypto';
import { mkdtemp, mkdir, readFile, readdir, rm, writeFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import { join, resolve } from 'node:path';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { createCatalogStore } from '../electron/catalog-store';
import { catalogReferenceErrors, MAX_CATALOG_BYTES, readCatalog } from '../electron/catalog-contract';

const original = await readFile(new URL('../../records/catalog.json', import.meta.url), 'utf8');
const parsed = readCatalog(JSON.parse(original));
if (!parsed) throw new Error('Canonical catalog is invalid');
const canonical = parsed;
const hash = (text: string) => createHash('sha256').update(text).digest('hex');
const changed = JSON.stringify({ ...canonical, revision: 'independent-test-revision' });
const fixtureParent = fileURLToPath(new URL('../../.verification/', import.meta.url));
let root: string;
let path: string;
let backup: string;
let store: ReturnType<typeof createCatalogStore>;
beforeEach(async () => {
  root = await mkdtemp(join(fixtureParent, 'system-records-store-'));
  path = join(root, 'records', 'catalog.json');
  backup = join(root, 'backup', 'last-good.json');
  await mkdir(join(root, 'records'));
  await writeFile(path, original);
  store = createCatalogStore(path, backup);
});
afterEach(async () => {
  // Delete only our exact direct-child fixture, never a computed external path.
  expect(resolve(root).startsWith(resolve(fixtureParent) + '\\system-records-store-')).toBe(true);
  await rm(root, { recursive: true, force: true });
});

describe('runtime catalog persistence and preservation', () => {
  it('reads current file bytes, saves without a build, preserves exact last-good bytes and reloads', async () => {
    expect(await store.read()).toMatchObject({ ok: true, text: original, version: hash(original) });
    expect(await store.save({ text: changed, expectedVersion: hash(original) })).toMatchObject({ ok: true, version: hash(changed) });
    expect(await readFile(backup, 'utf8')).toBe(original);
    expect(await createCatalogStore(path, backup).read()).toMatchObject({ ok: true, text: changed });
    expect(await readdir(join(root, 'records'))).toEqual(['catalog.json']);
  });
  it('can initialize a missing file only against a missing baseline', async () => {
    await rm(path);
    expect(await store.read()).toMatchObject({ code: 'missing' });
    expect(await store.save({ text: original, expectedVersion: hash(original) })).toMatchObject({ code: 'conflict' });
    expect(await store.save({ text: original, expectedVersion: null })).toMatchObject({ ok: true });
  });
  it('rejects malformed, duplicate, dangling-reference and oversized drafts without changing file or backup', async () => {
    const duplicate = structuredClone(canonical); duplicate.systems.push(duplicate.systems[0]!);
    const dangling = structuredClone(canonical); dangling.systems[0]!.sourceIds.push('missing-evidence');
    for (const text of ['{', '{}', JSON.stringify(duplicate), JSON.stringify(dangling), '한'.repeat(MAX_CATALOG_BYTES)]) {
      expect(await store.save({ text, expectedVersion: hash(original) })).toMatchObject({ ok: false, code: 'invalid' });
      expect(await readFile(path, 'utf8')).toBe(original);
    }
    for (const input of [null, [], { text: original }, { text: original, expectedVersion: 'invalid' }]) {
      expect(await store.save(input)).toMatchObject({ code: 'invalid' });
    }
    expect(await readdir(root)).toEqual(['records']);
  });
  it('reports a damaged file with its exact version so explicit repair does not replace the last-good backup', async () => {
    await mkdir(join(root, 'backup')); await writeFile(backup, original);
    await writeFile(path, '{damaged');
    expect(await store.read()).toMatchObject({ ok: false, code: 'invalid', text: '{damaged', version: hash('{damaged') });
    expect(await store.save({ text: changed, expectedVersion: hash('{damaged') })).toMatchObject({ ok: true });
    expect(await readFile(backup, 'utf8')).toBe(original);
  });
  it('rejects stale window versions and simultaneous writers without lost updates', async () => {
    const other = createCatalogStore(path, backup);
    const results = await Promise.all([store.save({ text: changed, expectedVersion: hash(original) }), other.save({ text: original + '\n', expectedVersion: hash(original) })]);
    expect(results.filter(result => result.ok)).toHaveLength(1);
    expect(results.filter(result => !result.ok).map(result => result.code)).toEqual([expect.stringMatching(/busy|conflict/)]);
    expect(await other.save({ text: original, expectedVersion: hash(original) })).toMatchObject({ code: 'conflict' });
    expect(await readFile(backup, 'utf8')).toBe(original);
  });
  it('does not steal an existing lock and preserves bytes on backup write failure', async () => {
    const lock = join(root, 'records', '.catalog.lock'); await writeFile(lock, 'owned by another process');
    expect(await store.save({ text: changed, expectedVersion: hash(original) })).toMatchObject({ code: 'busy' });
    expect(await readFile(lock, 'utf8')).toBe('owned by another process');
    await rm(lock);
    await writeFile(join(root, 'backup'), 'not a directory');
    expect(await store.save({ text: changed, expectedVersion: hash(original) })).toMatchObject({ code: 'write' });
    expect(await readFile(path, 'utf8')).toBe(original);
    expect(await readdir(join(root, 'records'))).toEqual(['catalog.json']);
  });
  it('ignores injected path fields and distinguishes oversized reads and file access failures', async () => {
    const outside = join(root, 'unrelated.json'); await writeFile(outside, 'keep');
    expect(await store.save({ text: changed, expectedVersion: hash(original), path: outside, backupPath: outside })).toMatchObject({ ok: true });
    expect(await readFile(outside, 'utf8')).toBe('keep');
    await writeFile(path, 'x'.repeat(MAX_CATALOG_BYTES + 1));
    expect(await store.read()).toMatchObject({ code: 'invalid' });
    await rm(path); await mkdir(path);
    expect(await store.read()).toMatchObject({ code: 'load' });
  });
});

describe('canonical record contract', () => {
  it('keeps complete unique references and reciprocal system-record discovery', () => {
    expect(catalogReferenceErrors(canonical)).toEqual([]);
    for (const system of canonical.systems) for (const id of system.recordIds) expect(canonical.records.find(record => record.id === id)?.systemIds).toContain(system.id);
    for (const record of canonical.records) for (const id of record.systemIds) expect(canonical.systems.find(system => system.id === id)?.recordIds).toContain(record.id);
    expect(canonical.systems.map(system => system.id)).toEqual(expect.arrayContaining(['combat', 'persistence', 'management-records', 'transport', 'party', 'quest']));
    expect(canonical.systems.find(system => system.id === 'persistence')?.implementationStatus).toMatch(/미완료/);
    expect(canonical.records.find(record => record.id === 'verify-combined')?.status).toMatch(/최신 main 재실행 아님/);
    expect(canonical.records.find(record => record.id === 'plan-contracts')?.status).toMatch(/클라이언트\/UI 적용부터 저장·복원 종합 검증까지의 후속 단계 미착수/);
    for (const source of canonical.sources) {
      expect(source.section.length).toBeGreaterThan(0);
      if (source.kind === 'git') expect(source.revision).toMatch(/^[a-f0-9]{40}$/);
      if (source.kind === 'local') expect(source.revision).toMatch(/^SHA256:[A-F0-9]{64}$/);
    }
  });
});

// @vitest-environment node
// Requirement: goal 「만들 것」 3 (no save, backup or conflict handling in the store) and
// index-v2-design.md 「Electron 경계」: createCatalogStore(path) only reads. Its result is
// { ok: true, catalog, version } or { ok: false, code, message } with missing·invalid·load,
// and never carries editable text. The 2 MiB limit stays. Reference breaks reject the whole
// index as invalid (design 「색인 형식」, Astra msg_28ce06a18c04 Q7).
// Expected versions are the SHA-256 of the UTF-8 file text, computed here with node:crypto.
import { createHash } from 'node:crypto';
import { existsSync, readdirSync, readFileSync } from 'node:fs';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { MAX_CATALOG_BYTES } from '../electron/catalog-contract';
import { createCatalogStore } from '../electron/catalog-store';
import { catalogText, linkedCatalog, recordFixture, systemFixture } from './record-sources/catalog-v2-fixture';
import { createOwnedTempRepository, type OwnedTempRepository } from './record-sources/owned-temp-repository';

const sha256 = (text: string) => createHash('sha256').update(Buffer.from(text, 'utf8')).digest('hex');

let repository: OwnedTempRepository;
let catalogPath: string;
beforeEach(() => {
  repository = createOwnedTempRepository();
  catalogPath = repository.write('records/catalog.json', catalogText(linkedCatalog()));
});
afterEach(() => repository.remove());

describe('read-only catalog store', () => {
  it('exposes only read and takes only the catalog path', () => {
    const store = createCatalogStore(catalogPath);
    expect(Object.keys(store)).toEqual(['read']);
    expect(createCatalogStore).toHaveLength(1);
  });

  it('reads a v2 index as catalog and content version without editable text', async () => {
    const text = catalogText(linkedCatalog());
    const result = await createCatalogStore(catalogPath).read();
    expect(result).toEqual({ ok: true, catalog: linkedCatalog(), version: sha256(text) });
    expect(Object.keys(result).sort()).toEqual(['catalog', 'ok', 'version']);
  });

  it('reports a missing file as missing without text', async () => {
    const result = await createCatalogStore(repository.path('records/none.json')).read();
    expect(result).toMatchObject({ ok: false, code: 'missing', message: expect.any(String) });
    expect(result).not.toHaveProperty('text');
  });

  it('reports a directory in place of the file as load', async () => {
    const directory = repository.mkdir('records/as-directory.json');
    expect(await createCatalogStore(directory).read()).toMatchObject({ ok: false, code: 'load' });
  });

  it('rejects malformed JSON, the v1 format, duplicate IDs and dangling references as invalid without text', async () => {
    const duplicate = linkedCatalog();
    duplicate.systems.push(systemFixture({ id: 'records-view' }));
    const dangling = linkedCatalog();
    dangling.records.push(recordFixture({ id: 'dangling', sourceIds: ['no-such-source'] }));
    const danglingSystem = linkedCatalog();
    danglingSystem.systems[0]!.relatedSystemIds.push('no-such-system');
    const v1 = { schemaVersion: 1, revision: 'r', asOf: 'a', sourceCommit: 'c', scopeNote: 'n', sources: [], systems: [], records: [] };
    const baseline = await createCatalogStore(catalogPath).read();
    expect(baseline.ok, 'unchanged v2 baseline must be accepted').toBe(true);
    for (const [name, text] of [
      ['malformed', '{'],
      ['v1', JSON.stringify(v1)],
      ['duplicate', catalogText(duplicate)],
      ['dangling record source', catalogText(dangling)],
      ['dangling related system', catalogText(danglingSystem)],
    ] as const) {
      const path = repository.write(`records/${name.replaceAll(' ', '-')}.json`, text);
      const result = await createCatalogStore(path).read();
      expect(result, name).toMatchObject({ ok: false, code: 'invalid' });
      expect(result, name).not.toHaveProperty('text');
    }
  });

  it('keeps the 2 MiB limit and rejects a larger file as invalid', async () => {
    expect(MAX_CATALOG_BYTES).toBe(2 * 1024 * 1024);
    const path = repository.write('records/over.json', ' '.repeat(MAX_CATALOG_BYTES + 1));
    expect(await createCatalogStore(path).read()).toMatchObject({ ok: false, code: 'invalid' });
  });

  it('never writes beside the catalog: no lock, temporary, or backup file after reads', async () => {
    const before = readFileSync(catalogPath);
    const store = createCatalogStore(catalogPath);
    await store.read();
    await store.read();
    expect(readdirSync(repository.path('records'))).toEqual(['catalog.json']);
    expect(readdirSync(repository.root)).toEqual(['records']);
    expect(readFileSync(catalogPath).equals(before)).toBe(true);
  });

  it('has no save-time rename helper module', () => {
    expect(existsSync(new URL('../electron/catalog-rename.ts', import.meta.url))).toBe(false);
  });
});

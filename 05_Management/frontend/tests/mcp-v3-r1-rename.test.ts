// @vitest-environment node
// V3-R1 (verifier-owned): code review follow-ups R07 and R08 of the Windows save rename retry.
// - R07: the test-only attempt observer never changes the native rename result. A throwing
//   observer leaves a successful save successful and a failing rename failing with its own error;
//   exhaustion still keeps the catalog, the prepared backup, the caller's draft and no lock/temp.
// - R08: the requirement invariants used by tests/mcp-v1-rename.test.ts (./mcp-v3-r1/rename-contract)
//   must accept other legal backoff schedules and reject real contract violations. Both are shown
//   on source mutants of the product renamer, written to an owned TEMP directory and imported from
//   there. The product file is only read.
import { lstatSync, mkdtempSync, readFileSync, readdirSync, rmSync, writeFileSync } from 'node:fs';
import { mkdir, readFile, readdir, rename as nativeRename, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { afterAll, describe, expect, it } from 'vitest';
import { createCatalogRenamer } from '../electron/catalog-rename';
import { createCatalogStore } from '../electron/catalog-store';
import { linkedCatalog, makeCatalog, osError, sha256 } from './mcp-fixtures';
import { RENAME_SCENARIOS, expectDeadlineRespected, runRenamer, type CreateRenamer } from './mcp-v3-r1/rename-contract';

const PRODUCT_RENAMER = fileURLToPath(new URL('../electron/catalog-rename.ts', import.meta.url));
const TEMP_PREFIX = 'dawnholder-v3-r1-rename-';
const owned: string[] = [];
function ownedTemp(): string {
  const root = mkdtempSync(join(tmpdir(), TEMP_PREFIX));
  owned.push(root);
  return root;
}
// Removes only what this file created: a direct OS TEMP child with our prefix and no links inside.
afterAll(() => {
  for (const root of owned.splice(0)) {
    const absolute = resolve(root);
    if (resolve(absolute, '..') !== resolve(tmpdir()) || !absolute.slice(resolve(tmpdir()).length + 1).startsWith(TEMP_PREFIX)) throw new Error(`refusing to remove ${absolute}`);
    const walk = (dir: string): void => readdirSync(dir).forEach(name => {
      const stat = lstatSync(join(dir, name));
      if (stat.isSymbolicLink()) throw new Error(`refusing to remove a tree with a link: ${join(dir, name)}`);
      if (stat.isDirectory()) walk(join(dir, name));
    });
    walk(absolute);
    rmSync(absolute, { recursive: true, force: true });
  }
});

const product: CreateRenamer = options => createCatalogRenamer(options);
const throwingObserver = () => { throw new Error('SENTINEL observer failure'); };

// ------------------------------------------------------------------ R07 observer isolation
const OBSERVER_SCENARIOS: Array<{ title: string; check(create: CreateRenamer): Promise<void> }> = [
  {
    title: 'a throwing observer after a successful rename keeps the success',
    async check(create) {
      let renames = 0;
      const renamer = create({ now: () => 0, wait: async () => undefined, onAttempt: throwingObserver, rename: async () => { renames += 1; } });
      await expect(renamer('tmp', 'catalog')).resolves.toBeUndefined();
      expect(renames).toBe(1);
    },
  },
  {
    title: 'a throwing observer on transient failures does not stop the retry, and the later success stands',
    async check(create) {
      let time = 0;
      const codes = ['EPERM', 'EBUSY'];
      let renames = 0;
      const renamer = create({
        now: () => time, wait: async milliseconds => { time += milliseconds; }, onAttempt: throwingObserver,
        rename: async () => { renames += 1; const code = codes.shift(); if (code) throw osError(code); },
      });
      await expect(renamer('tmp', 'catalog')).resolves.toBeUndefined();
      expect(renames).toBe(3);
    },
  },
  {
    title: 'a throwing observer on a non-target failure keeps that native error',
    async check(create) {
      const failure = osError('EIO');
      const renamer = create({ now: () => 0, wait: async () => { throw new Error('must not wait'); }, onAttempt: throwingObserver, rename: async () => { throw failure; } });
      await expect(renamer('tmp', 'catalog')).rejects.toBe(failure);
    },
  },
  {
    title: 'a throwing observer during exhaustion keeps the deadline and the last native error',
    async check(create) {
      const run = await runRenamer(options => create({ ...options, onAttempt: observation => { options.onAttempt?.(observation); throwingObserver(); } }), ['EBUSY']);
      expectDeadlineRespected(run);
      expect(run.starts.length).toBeGreaterThanOrEqual(2);
    },
  },
];

describe('R07: the attempt observer cannot change the rename result', () => {
  for (const scenario of OBSERVER_SCENARIOS) it(scenario.title, () => scenario.check(product));

  describe('through the real store', () => {
    const original = JSON.stringify(linkedCatalog(), null, 2);
    const next = JSON.stringify(makeCatalog({ revision: 'v3-r1-observer-next' }), null, 2);
    const textVersion = (text: string) => sha256(Buffer.from(text, 'utf8'));
    async function fixture() {
      const root = ownedTemp();
      const path = join(root, 'records', 'catalog.json');
      const backup = join(root, 'backup', 'last-good.json');
      await mkdir(join(root, 'records'));
      await writeFile(path, original);
      return { root, path, backup };
    }

    it('a successful save with a throwing observer reports success, writes the new text and backs up the previous one', async () => {
      const { root, path, backup } = await fixture();
      const store = createCatalogStore(path, backup, { onAttempt: throwingObserver, rename: (source, target) => nativeRename(source, target) });
      expect(await store.save({ text: next, expectedVersion: textVersion(original) })).toMatchObject({ ok: true, version: textVersion(next) });
      expect(await readFile(path, 'utf8')).toBe(next);
      expect(await readFile(backup, 'utf8')).toBe(original);
      expect(await readdir(join(root, 'records'))).toEqual(['catalog.json']);
    });

    it('exhaustion with a throwing observer is the existing write failure; catalog, prepared backup, draft kept; no lock or temp', async () => {
      const { root, path, backup } = await fixture();
      let time = 0;
      const input = { text: next, expectedVersion: textVersion(original) };
      const frozen = JSON.stringify(input);
      const store = createCatalogStore(path, backup, {
        now: () => time, wait: async milliseconds => { time += milliseconds; }, onAttempt: throwingObserver,
        rename: async () => { throw osError('EBUSY'); },
      });
      const result = await store.save(input);
      expect(result).toEqual({ ok: false, code: 'write', message: '기록 또는 복구용 백업을 저장하지 못했습니다. 현재 기록과 초안을 보존합니다.' });
      expect(JSON.stringify(result)).not.toMatch(/SENTINEL|EBUSY/);
      expect(await readFile(path, 'utf8')).toBe(original);
      expect(await readFile(backup, 'utf8')).toBe(original);
      expect(JSON.stringify(input)).toBe(frozen);
      expect(await readdir(join(root, 'records'))).toEqual(['catalog.json']);
      expect(await createCatalogStore(path, backup).save(input)).toMatchObject({ ok: true });
    });
  });
});

// ------------------------------------------------------------------ R08 discriminating power
interface Mutant { name: string; edits: Array<[string, string]>; why: string }
// Legal variants: other schedules inside the same requirement.
const LEGAL: Mutant[] = [
  { name: 'faster-growth-cap-100', edits: [['backoffMs = Math.min(40, backoffMs * 2);', 'backoffMs = Math.min(100, backoffMs * 3);']], why: 'different growth and cap' },
  { name: 'constant-25', edits: [['let backoffMs = 10;', 'let backoffMs = 25;'], ['backoffMs = Math.min(40, backoffMs * 2);', 'backoffMs = 25;']], why: 'constant backoff' },
  {
    name: 'give-up-instead-of-clipped-wait',
    edits: [['await wait(Math.min(backoffMs, CATALOG_RENAME_RETRY_MS - elapsedMs));', 'if (elapsedMs + backoffMs >= CATALOG_RENAME_RETRY_MS) throw error;\n        await wait(backoffMs);']],
    why: 'stops when the next start would miss the deadline instead of waiting out the remainder',
  },
];
// Contract violations, each with the scenario title prefix expected to catch it.
const VIOLATIONS: Array<Mutant & { caughtBy: string }> = [
  { name: 'retries-EIO', edits: [["['EPERM', 'EACCES', 'EBUSY']", "['EPERM', 'EACCES', 'EBUSY', 'EIO']"]], why: 'retries a non-target code', caughtBy: 'EIO is not transient' },
  { name: 'drops-EBUSY', edits: [["['EPERM', 'EACCES', 'EBUSY']", "['EPERM', 'EACCES']"]], why: 'does not retry a target code', caughtBy: 'EBUSY:' },
  { name: 'no-deadline-after-wait', edits: [['if (now() - started >= CATALOG_RENAME_RETRY_MS) throw error;', '']], why: 'starts an attempt at the deadline', caughtBy: 'EPERM:' },
  { name: 'window-1500', edits: [['export const CATALOG_RENAME_RETRY_MS = 1000;', 'export const CATALOG_RENAME_RETRY_MS = 1500;']], why: 'starts attempts after 1000 ms', caughtBy: 'EPERM:' },
  { name: 'unclipped-wait', edits: [['await wait(Math.min(backoffMs, CATALOG_RENAME_RETRY_MS - elapsedMs));', 'await wait(backoffMs);']], why: 'sleeps past the deadline before failing', caughtBy: 'EPERM:' },
  { name: 'gives-up-after-two', edits: [['|| elapsedMs >= CATALOG_RENAME_RETRY_MS) throw error;', '|| attempt >= 2) throw error;']], why: 'stops long before the window is used', caughtBy: 'EPERM:' },
  { name: 'replaces-last-error', edits: [['if (now() - started >= CATALOG_RENAME_RETRY_MS) throw error;', "if (now() - started >= CATALOG_RENAME_RETRY_MS) throw new Error('rename exhausted');"]], why: 'loses the last native error', caughtBy: 'EPERM:' },
  { name: 'zero-backoff', edits: [['await wait(Math.min(backoffMs, CATALOG_RENAME_RETRY_MS - elapsedMs));', 'await wait(0);']], why: 'busy-loops without backoff', caughtBy: 'EPERM:' },
  { name: 'retries-without-code', edits: [["if (!code || !['EPERM'", "if (!['EPERM'"], ["['EPERM', 'EACCES', 'EBUSY'].includes(code)", "['EPERM', 'EACCES', 'EBUSY', undefined].includes(code)"]], why: 'retries errors without a string code', caughtBy: 'an error without a string code' },
];
const OBSERVER_UNGUARDED: Mutant = {
  name: 'observer-unguarded',
  edits: [['    try { options.onAttempt?.(observation); }', '    options.onAttempt?.(observation); try { /* guard removed */ }']],
  why: 'an observer exception escapes into the rename result',
};

describe('R08: the rename invariants accept legal schedules and reject contract violations (product source mutants)', () => {
  const source = readFileSync(PRODUCT_RENAMER, 'utf8');
  const directory = ownedTemp();
  async function load(mutant: Mutant): Promise<CreateRenamer> {
    let text = source;
    for (const [from, to] of mutant.edits) {
      expect(text.includes(from), `${mutant.name}: mutation anchor present`).toBe(true);
      text = text.replace(from, to);
    }
    expect(text).not.toBe(source);
    const file = join(directory, `${mutant.name}.ts`);
    writeFileSync(file, text, { flag: 'wx' });
    const module = await import(/* @vite-ignore */ pathToFileURL(file).href) as typeof import('../electron/catalog-rename');
    return options => module.createCatalogRenamer(options);
  }
  async function failedScenarios(create: CreateRenamer, scenarios: Array<{ title: string; check(create: CreateRenamer): Promise<unknown> }>) {
    const failed: string[] = [];
    for (const scenario of scenarios) {
      try { await scenario.check(create); } catch { failed.push(scenario.title); }
    }
    return failed;
  }

  it('the product itself satisfies every scenario', async () => {
    expect(await failedScenarios(product, [...RENAME_SCENARIOS, ...OBSERVER_SCENARIOS])).toEqual([]);
  });

  for (const mutant of LEGAL) {
    it(`legal variant ${mutant.name} (${mutant.why}) passes every scenario`, async () => {
      const create = await load(mutant);
      const exhausted = await runRenamer(create, ['EPERM']);
      console.info(`[V3-R1-MEASURE] rename-legal-variant ${JSON.stringify({ name: mutant.name, attempts: exhausted.starts.length, starts: exhausted.starts, settledAt: exhausted.settledAt })}`);
      expect(await failedScenarios(create, [...RENAME_SCENARIOS, ...OBSERVER_SCENARIOS])).toEqual([]);
    });
  }

  for (const mutant of VIOLATIONS) {
    it(`violation ${mutant.name} (${mutant.why}) is rejected`, async () => {
      const failed = await failedScenarios(await load(mutant), RENAME_SCENARIOS);
      console.info(`[V3-R1-MEASURE] rename-violation ${JSON.stringify({ name: mutant.name, failed })}`);
      expect(failed.some(title => title.startsWith(mutant.caughtBy)), `${mutant.name} caught by "${mutant.caughtBy}"`).toBe(true);
    });
  }

  it(`R07 violation ${OBSERVER_UNGUARDED.name} (${OBSERVER_UNGUARDED.why}) is rejected by the observer scenarios`, async () => {
    const failed = await failedScenarios(await load(OBSERVER_UNGUARDED), OBSERVER_SCENARIOS);
    console.info(`[V3-R1-MEASURE] rename-violation ${JSON.stringify({ name: OBSERVER_UNGUARDED.name, failed })}`);
    expect(failed).toContain('a throwing observer after a successful rename keeps the success');
  });
});

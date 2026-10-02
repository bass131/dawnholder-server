// @vitest-environment node
// V2: the production entry's catalog is fixed by its own module location (goal D4/설계 2,
// completion condition 4). Run the BUILT entry from other cwds with path/URL argv, catalog-ish
// env variables and client roots pointing at a decoy fixture: the response hash must stay the
// canonical module-relative catalog. Static checks are made on the built output that actually
// runs (mcp-dist), complementing V1's source-level boundary test: no path selection, no raw
// file read/write, no network/process API, no test seam supplied by main.js.
import { readFileSync, readdirSync, writeFileSync } from 'node:fs';
import { mkdir } from 'node:fs/promises';
import { dirname, join, relative, resolve } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { afterAll, afterEach, describe, expect, it } from 'vitest';
import { catalogBytes, expectSuccess, linkedCatalog } from './mcp-fixtures';
import { CANONICAL_CATALOG, ERAS, FRONTEND, PRODUCTION_ENTRY, removeTempRoots, sha256, tempRoot, textHash, type JsonObject } from './mcp-v2/harness';
import { closeAndCheck, outcome, processPool } from './mcp-v2/support';

const pool = processPool();
afterEach(async () => { await pool.closeAll(); });
afterAll(() => { removeTempRoots(); });

const DIST = join(FRONTEND, 'mcp-dist');
function distFiles(directory = DIST): string[] {
  return readdirSync(directory, { withFileTypes: true }).flatMap(entry => entry.isDirectory() ? distFiles(join(directory, entry.name)) : [join(directory, entry.name)]);
}
const rel = (path: string) => relative(DIST, path).replaceAll('\\', '/');

describe('built output static boundary (mcp-dist, what the client runs)', () => {
  it('file set, import graph and forbidden APIs', () => {
    const files = distFiles();
    // V3-R1: derived from the requirement instead of a frozen 12-file list. The output is exactly the
    // compiled MCP adapter sources (mcp/*.ts) plus the three pure shared modules the goal allows; a
    // store/rename/UI/Electron-runtime module, any other shared module or a stale file fails here, and
    // every file still passes the import and API checks below.
    const adapter = readdirSync(join(FRONTEND, 'mcp')).filter(name => name.endsWith('.ts')).map(name => `mcp/${name.replace(/\.ts$/, '.js')}`);
    const allowedShared = ['electron/catalog-contract.js', 'electron/catalog-hash.js', 'electron/catalog-query.js'];
    expect(files.map(rel).sort()).toEqual([...allowedShared, ...adapter].sort());
    const allowedBare = new Set(['@modelcontextprotocol/server', '@modelcontextprotocol/server/stdio', 'zod', 'node:fs/promises', 'node:url', 'node:crypto']);
    const imports: Record<string, string[]> = {};
    for (const file of files) {
      const source = readFileSync(file, 'utf8');
      const specifiers = [...source.matchAll(/(?:^|\n)\s*(?:import|export)\b[^'"]*?from\s*['"]([^'"]+)['"]/g), ...source.matchAll(/\bimport\s*\(\s*['"]?([^'")]+)/g)].map(match => match[1] ?? '');
      imports[rel(file)] = specifiers;
      for (const specifier of specifiers) {
        if (specifier.startsWith('.')) {
          const target = resolve(dirname(file), specifier);
          expect(target.startsWith(DIST), `${rel(file)} imports outside mcp-dist: ${specifier}`).toBe(true);
          expect(/catalog-store|catalog-rename|tests|desktop-dist|src[\\/]/.test(target), `${rel(file)} -> ${specifier}`).toBe(false);
        } else {
          expect(allowedBare.has(specifier), `${rel(file)} imports ${specifier}`).toBe(true);
        }
      }
      expect(/\bimport\s*\(/.test(source), `${rel(file)} dynamic import`).toBe(false);
      expect(/\brequire\s*\(/.test(source), `${rel(file)} require`).toBe(false);
      for (const token of ['process.argv', 'process.env', 'process.cwd', 'roots/list', 'listRoots', 'writeFile', 'appendFile', 'rename(', 'unlink', 'rmSync', 'rm(', 'mkdir', 'fetch(', 'child_process', 'spawn', 'exec(', 'createWriteStream', 'http:', 'https:']) {
        expect(source.includes(token), `${rel(file)} contains ${token}`).toBe(false);
      }
      // The only native file open is the reader's read-only open (bare calls; method
      // definitions and fileOperations.open(...) delegation are not native opens).
      const opens = [...source.matchAll(/(?<![.\w]|async )open\(([^)]*)\)/g)].map(match => match[1]);
      if (rel(file) === 'mcp/catalog-reader.js') expect(opens).toEqual(["path, 'r'"]);
      else expect(opens, rel(file)).toEqual([]);
    }
    const main = readFileSync(join(DIST, 'mcp', 'main.js'), 'utf8');
    // main supplies only the fixed reader and the build version: no clock, no observer.
    expect(main).toMatch(/createCatalogServer\(\{\s*readSnapshot:\s*reader\.readSnapshot,\s*version:\s*BUILD_VERSION\s*\}\)/);
    expect(main.includes('onToolHandlerEntered')).toBe(false);
    expect(/\bnow\s*:/.test(main)).toBe(false);
    expect(main).toContain("new URL('../../../records/catalog.json', import.meta.url)");
    expect(fileURLToPath(new URL('../../../records/catalog.json', pathToFileURL(PRODUCTION_ENTRY)))).toBe(CANONICAL_CATALOG);
    // V3-R1: the build identity is a content digest, not a pinned value; that it matches the current
    // sources is shown by a fresh TEMP build (tests/mcp-v3-build.test.ts, tests/mcp-v3-r1-build.test.ts).
    const buildInfo = readFileSync(join(DIST, 'mcp', 'build-info.js'), 'utf8');
    expect(buildInfo).toMatch(/^export const BUILD_VERSION = "0\.0\.0\+sha256\.[0-9a-f]{64}";\n$/);
    expect(imports['mcp/main.js']).toContain('./build-info.js');
    console.info(`[V2-MEASURE] dist-imports ${JSON.stringify(imports)} build=${buildInfo.trim()}`);
  });
});

describe('production entry ignores cwd, argv, env and client roots', () => {
  it('both revisions, decoy catalogs everywhere: hash stays the canonical module-relative catalog; tools expose no path/URL input; server never asks for roots', async () => {
    const canonicalBefore = readFileSync(CANONICAL_CATALOG);
    const canonicalHash = textHash(canonicalBefore);
    const root = tempRoot('path-decoy');
    const decoy = catalogBytes({ ...linkedCatalog(), revision: 'SENTINEL_DECOY_REVISION' });
    const decoyHash = textHash(decoy);
    expect(decoyHash).not.toBe(canonicalHash);
    // Decoys at every plausible cwd-relative location of '../../../records/catalog.json'.
    const decoyPaths = [join(root, 'catalog.json'), join(root, 'records', 'catalog.json'), join(root, 'a', 'b', 'c', 'records', 'catalog.json'), join(root, '05_Management', 'records', 'catalog.json')];
    for (const path of decoyPaths) { await mkdir(dirname(path), { recursive: true }); writeFileSync(path, decoy); }
    const cwd = join(root, 'a', 'b', 'c', 'd');
    await mkdir(cwd, { recursive: true });
    const decoyBefore = decoyPaths.map(path => sha256(readFileSync(path)));
    const decoyUrl = pathToFileURL(decoyPaths[1] as string).href;
    const env: NodeJS.ProcessEnv = {
      ...process.env, CATALOG_PATH: decoyPaths[1], CATALOG: decoyPaths[1], MCP_CATALOG_PATH: decoyPaths[1], DAWNHOLDER_CATALOG: decoyPaths[1],
      RECORDS_CATALOG: decoyPaths[1], CATALOG_URL: 'https://SENTINEL.invalid/catalog.json', INIT_CWD: root, PWD: cwd, MCP_ROOTS: decoyUrl,
    };
    const extraArgs = [decoyPaths[1] as string, '--catalog', decoyPaths[1] as string, decoyUrl, 'https://SENTINEL.invalid/catalog.json', '--root', root];
    const procs = await Promise.all(ERAS.map(era => pool.start({ era, label: `path-${era}`, cwd, env, extraArgs, roots: [{ uri: pathToFileURL(root).href, name: 'SENTINEL_ROOT' }] })));
    const report: JsonObject[] = [];
    for (const proc of procs) {
      if (proc.options.era === 'legacy') await proc.client.sendRootsListChanged();
      const tools = (await proc.client.listTools()).tools;
      expect(tools.map(tool => tool.name).sort()).toEqual(['get_record', 'get_source', 'get_system', 'list_systems', 'search_records']);
      for (const tool of tools) {
        const schema = tool.inputSchema as { additionalProperties?: unknown; properties?: Record<string, unknown> };
        expect(schema.additionalProperties).toBe(false);
        for (const key of Object.keys(schema.properties ?? {})) expect(['query', 'area', 'type', 'systemId', 'limit', 'offset', 'id', 'expectedHash']).toContain(key);
      }
      const listed = expectSuccess('list_systems', outcome(await proc.call('list_systems', {})));
      expect(listed.snapshot.hash).toBe(canonicalHash);
      expect(listed.snapshot.revision).not.toBe('SENTINEL_DECOY_REVISION');
      const firstId = (listed.data as { items: Array<{ id: string }> }).items[0]?.id;
      const detail = expectSuccess('get_system', outcome(await proc.call('get_system', { id: firstId })));
      expect(detail.snapshot.hash).toBe(canonicalHash);
      // decoy-only IDs do not exist in the canonical catalog
      const decoyId = await proc.call('get_system', { id: 'sys-alpha' });
      expect((decoyId.result?.structuredContent as { error?: { code?: string } })?.error?.code).toBe('NOT_FOUND');
      expect(proc.rootsRequests).toBe(0);
      expect(proc.lines.some(line => line.json?.method === 'roots/list')).toBe(false);
      report.push({ era: proc.options.era, cwd, hash: listed.snapshot.hash, rootsRequests: proc.rootsRequests, serverVersion: proc.serverVersion });
      await closeAndCheck(proc);
    }
    console.info(`[V2-MEASURE] path-invariance ${JSON.stringify({ canonicalHash, decoyHash, argv: extraArgs.length, envKeys: Object.keys(env).filter(key => /CATALOG|ROOT|INIT_CWD|PWD/.test(key)), report })}`);
    expect(decoyPaths.map(path => sha256(readFileSync(path)))).toEqual(decoyBefore);
    expect(readFileSync(CANONICAL_CATALOG).equals(canonicalBefore)).toBe(true);
  }, 60_000);
});

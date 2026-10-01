// @vitest-environment node
// V1: static import / file-access boundary of the MCP, the fixed production entry, the
// shared UI modules, and the catalog paths of the built MCP and Electron entries.
// Source is only read here; the one exception (V3-R1) is the build digest test, which builds
// an owned TEMP copy.
import { existsSync, readdirSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname, join, relative, resolve } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { describe, expect, it } from 'vitest';
import { createTempCopy } from './mcp-v3/temp-copy';

const frontend = fileURLToPath(new URL('..', import.meta.url));
const read = (path: string) => readFileSync(join(frontend, path), 'utf8');
const rel = (path: string) => relative(frontend, path).replaceAll('\\', '/');

function specifiers(source: string): { staticSpecifiers: string[]; dynamic: boolean } {
  // Module-level `import … from '…'`, `import '…'` and `export … from '…'` statements only.
  const statements = [...source.matchAll(/^(?:import|export)\b[^;]*?\bfrom\s*['"]([^'"]+)['"]|^import\s*['"]([^'"]+)['"]/gm)];
  const staticSpecifiers = statements.map(match => (match[1] ?? match[2]) as string);
  const dynamic = /\bimport\s*\(|\brequire\s*\(|createRequire/.test(source);
  return { staticSpecifiers, dynamic };
}

// Transitive module graph of the MCP sources (relative imports resolved to .ts files).
function mcpGraph() {
  const files = new Set<string>();
  const bare = new Set<string>();
  let dynamic = false;
  const queue = readdirSync(join(frontend, 'mcp')).filter(name => name.endsWith('.ts')).map(name => join(frontend, 'mcp', name));
  while (queue.length) {
    const file = queue.shift() as string;
    if (files.has(file)) continue;
    files.add(file);
    const parsed = specifiers(readFileSync(file, 'utf8'));
    dynamic ||= parsed.dynamic;
    for (const specifier of parsed.staticSpecifiers) {
      if (!specifier.startsWith('.')) { bare.add(specifier); continue; }
      const target = resolve(dirname(file), specifier).replace(/\.js$/, '.ts');
      queue.push(existsSync(target) ? target : `${target}.ts`);
    }
  }
  return { files: [...files].map(rel).sort(), bare: [...bare].sort(), dynamic };
}

describe('MCP module and file-access boundary', () => {
  const graph = mcpGraph();

  it('imports only MCP modules, the shared contract/query/hash modules, the SDK, zod and read-only Node APIs', () => {
    const mcpFiles = readdirSync(join(frontend, 'mcp')).filter(name => name.endsWith('.ts')).map(name => `mcp/${name}`);
    expect(graph.files).toEqual([...mcpFiles, 'electron/catalog-contract.ts', 'electron/catalog-hash.ts', 'electron/catalog-query.ts'].sort());
    expect(graph.bare).toEqual(['@modelcontextprotocol/server', '@modelcontextprotocol/server/stdio', 'node:crypto', 'node:fs/promises', 'node:url', 'zod']);
    expect(graph.dynamic).toBe(false);
    for (const forbidden of ['electron/catalog-store.ts', 'electron/catalog-rename.ts', 'electron/main.ts', 'electron/preload.cts']) expect(graph.files).not.toContain(forbidden);
    expect(graph.files.some(file => file.startsWith('src/'))).toBe(false);
  });

  it('contains no write, delete, process, network or shell calls, and opens files read-only', () => {
    const forbiddenCall = /\b(writeFile|appendFile|rename|unlink|rmdir|rm|mkdir|copyFile|cp|createWriteStream|symlink|truncate|chmod|chown|utimes|fetch|spawn|exec|execFile|fork|XMLHttpRequest|WebSocket)\s*\(/;
    for (const file of graph.files) {
      const source = read(file);
      expect(forbiddenCall.exec(source)?.[0] ?? null, file).toBeNull();
      expect(/process\.(argv|env|cwd|chdir)|listRoots|roots\/list|\.roots\b/.test(source), file).toBe(false);
    }
    const reader = read('mcp/catalog-reader.ts');
    // The only native open is the read-only one; the others are the injected interface.
    const nativeOpens = [...reader.matchAll(/(?<![.\w])open\((?!path: string|path, signal\))/g)].map(match => reader.slice(match.index, match.index + 16));
    expect(nativeOpens).toEqual(["open(path, 'r');"]);
    expect(/from 'node:fs\/promises'/.test(reader) && /import \{ open, type FileHandle \} from 'node:fs\/promises'/.test(reader)).toBe(true);
  });

  it('registers no path/URL/shell tool and no write-capable surface', () => {
    const server = read('mcp/catalog-server.ts');
    const registered = [...server.matchAll(/registerTool\('([^']+)'/g)].map(match => match[1]);
    expect(registered).toEqual(['list_systems', 'search_records', 'get_system', 'get_record', 'get_source']);
    expect(/registerResource|registerPrompt|setRequestHandler|sampling|elicit/.test(server)).toBe(false);
  });
});

describe('fixed production entry', () => {
  const main = read('mcp/main.ts');

  it('selects the catalog only from its own module URL and injects no test seam or option', () => {
    expect(main).toContain("catalogPath: fileURLToPath(new URL('../../../records/catalog.json', import.meta.url)),");
    expect(main).toContain('fileOperations: nodeCatalogFileOperations,');
    expect(main).toMatch(/createCatalogServer\(\{ readSnapshot: reader\.readSnapshot, version: BUILD_VERSION \}\)/);
    for (const forbidden of ['process.argv', 'process.env', 'process.cwd', 'onToolHandlerEntered', 'now:', 'listRoots', 'roots/list', 'CATALOG_PATH', 'parseArgs']) expect(main.includes(forbidden), forbidden).toBe(false);
    // The only mention of roots is the comment stating they do not select a path.
    expect(main.split(/\r?\n/).filter(line => /roots/i.test(line))).toEqual(['// No process arguments, environment, caller cwd or client roots select a path.']);
    expect([...main.matchAll(/createCatalogReader\(/g)]).toHaveLength(1);
  });

  it('built MCP entry and Electron main resolve the same 05 catalog; build layout keeps the Electron rootDir/outDir/main', () => {
    const mcpConfig = JSON.parse(read('tsconfig.mcp.json')) as { compilerOptions: Record<string, unknown>; include: string[] };
    const electronConfig = JSON.parse(read('tsconfig.electron.json')) as { compilerOptions: Record<string, unknown>; include: string[] };
    const pkg = JSON.parse(read('package.json')) as { main: string; scripts: Record<string, string> };
    expect(mcpConfig.compilerOptions).toMatchObject({ rootDir: '.', outDir: 'mcp-dist' });
    expect(mcpConfig.include).toEqual(['mcp/**/*.ts']);
    expect(electronConfig.compilerOptions).toMatchObject({ rootDir: 'electron', outDir: 'desktop-dist' });
    expect(electronConfig.include).toEqual(['electron/**/*.ts', 'electron/**/*.cts']);
    expect(pkg.main).toBe('desktop-dist/main.js');
    expect(pkg.scripts['mcp:build']).toBe('node scripts/build-mcp.mjs');
    expect(read('electron/main.ts')).toContain("fileURLToPath(new URL('../../records/catalog.json', import.meta.url))");
    const builtMcp = pathToFileURL(join(frontend, 'mcp-dist', 'mcp', 'main.js'));
    const builtElectron = pathToFileURL(join(frontend, 'desktop-dist', 'main.js'));
    const mcpCatalog = fileURLToPath(new URL('../../../records/catalog.json', builtMcp));
    const electronCatalog = fileURLToPath(new URL('../../records/catalog.json', builtElectron));
    expect(mcpCatalog).toBe(electronCatalog);
    expect(mcpCatalog).toBe(resolve(frontend, '..', 'records', 'catalog.json'));
  });

  it.skipIf(!existsSync(join(frontend, 'mcp-dist', 'mcp', 'main.js')))('existing MCP build output contains only MCP and shared contract/query/hash modules with the fixed URL', () => {
    const listing = (directory: string): string[] => readdirSync(join(frontend, directory), { withFileTypes: true })
      .flatMap(entry => entry.isDirectory() ? listing(`${directory}/${entry.name}`) : [`${directory}/${entry.name}`]);
    const built = listing('mcp-dist').sort();
    const expected = [
      ...readdirSync(join(frontend, 'mcp')).filter(name => name.endsWith('.ts')).map(name => `mcp-dist/mcp/${name.replace(/\.ts$/, '.js')}`),
      'mcp-dist/electron/catalog-contract.js', 'mcp-dist/electron/catalog-hash.js', 'mcp-dist/electron/catalog-query.js',
    ].sort();
    expect(built).toEqual(expected);
    expect(readFileSync(join(frontend, 'mcp-dist', 'mcp', 'main.js'), 'utf8')).toContain("new URL('../../../records/catalog.json', import.meta.url)");
    expect(readFileSync(join(frontend, 'mcp-dist', 'mcp', 'build-info.js'), 'utf8')).toMatch(/^export const BUILD_VERSION = "0\.0\.0\+sha256\.[a-f0-9]{64}";\n$/);
  });

  // V3-R1: this used to look for the old build script's literal input list. The digest contract is
  // now checked by behaviour in an owned TEMP copy (the canonical tree is only read): changing any
  // module this file's own static graph reaches (independent of the compiler listing the build uses),
  // the MCP build config, the package manifest, the lockfile or the build script changes the
  // digest, and catalog data does not.
  it.runIf(process.platform === 'win32')('the build digest covers MCP sources, shared modules, build settings and the lockfile', () => {
    const copy = createTempCopy('v1-boundary');
    try {
      const base = copy.buildMcp();
      const inputs = [...mcpGraph().files, 'tsconfig.mcp.json', 'package.json', 'package-lock.json', 'scripts/build-mcp.mjs'];
      expect(inputs).toContain('electron/catalog-contract.ts');
      const unchanged = inputs.filter(input => {
        const path = join(copy.frontend, input);
        const bytes = readFileSync(path);
        // A trailing comment or JSON whitespace keeps the build valid.
        writeFileSync(path, Buffer.concat([bytes, Buffer.from(/\.(ts|mjs)$/.test(input) ? '\n// v3-r1 digest probe\n' : '\n')]));
        try { return copy.buildMcp() === base; } finally { writeFileSync(path, bytes); }
      });
      expect(unchanged).toEqual([]);
      const catalog = readFileSync(copy.catalog);
      writeFileSync(copy.catalog, Buffer.concat([catalog, Buffer.from('\n')]));
      try { expect(copy.buildMcp()).toBe(base); } finally { writeFileSync(copy.catalog, catalog); }
      console.info(`[V3-R1-MEASURE] v1-boundary-digest ${JSON.stringify({ base, inputs })}`);
    } finally {
      copy.remove();
    }
  }, 300_000);
});

describe('UI sharing and preserved contracts', () => {
  it('the UI module only re-exports the shared contract and search; the store hashes through the shared function', () => {
    const ui = read('src/recordCatalog.ts').trim().split(/\r?\n/);
    expect(ui).toEqual(["export * from '../electron/catalog-contract';", "export { matchesQuery, filterSystems, filterRecords } from '../electron/catalog-query';"]);
    const store = read('electron/catalog-store.ts');
    expect(store).toContain("import { catalogHash } from './catalog-hash.js';");
    expect(store.includes('createHash')).toBe(false);
    expect([...store.matchAll(/catalogHash\(/g)].length).toBeGreaterThanOrEqual(3);
    expect(read('electron/catalog-hash.ts')).toContain("createHash('sha256').update(text, 'utf8').digest('hex')");
    // The renamer is used only for the final catalog rename; the backup rename stays native.
    expect([...store.matchAll(/renameCatalog\(/g)]).toHaveLength(1);
    expect(store).toContain('await renameCatalog(temporaryPath, path);');
    expect(store).toContain('await rename(backupTemporary, backupPath);');
  });

  it('the rename override is not reachable from IPC, config or environment', () => {
    const electronMain = read('electron/main.ts');
    expect(electronMain).toMatch(/createCatalogStore\(\s*fileURLToPath\(new URL\('\.\.\/\.\.\/records\/catalog\.json', import\.meta\.url\)\),\s*fileURLToPath\(new URL\('\.\.\/\.\.\/\.verification\/system-records-last-good\.json', import\.meta\.url\)\),?\s*\)/);
    const rename = read('electron/catalog-rename.ts');
    expect(/process\.(env|argv)/.test(rename)).toBe(false);
    expect(read('electron/preload.cts').includes('rename')).toBe(false);
  });
});

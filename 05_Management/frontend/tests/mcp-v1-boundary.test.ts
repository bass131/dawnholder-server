// @vitest-environment node
// Static import / file-access boundary of the MCP, the fixed production entry, the shared UI
// modules, and the fixed paths of the built MCP and Electron entries. index-v2-design.md
// 「MCP 빌드 경계」 table sets the nine shared electron modules, the allowed bare imports, the four
// file-opening modules and the process-execution rule; 「MCP」 sets the three fixed main.ts paths.
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

// Design 「MCP 빌드 경계」: the electron modules the MCP build graph contains, exactly.
const SHARED_MODULES = [
  'electron/catalog-contract.ts', 'electron/catalog-hash.ts', 'electron/catalog-query.ts',
  'electron/source-section-contract.ts', 'electron/source-section-store.ts',
  'electron/checkout-contract.ts', 'electron/checkout-store.ts',
  'electron/system-guide-contract.ts', 'electron/system-guide-store.ts',
];
const ALLOWED_BARE = [
  '@modelcontextprotocol/server', '@modelcontextprotocol/server/stdio', 'node:crypto', 'node:fs', 'node:fs/promises', 'node:path', 'node:url', 'node:util', 'zod',
];
// The MCP reader and the three stores are the only modules that open files.
const FILE_OPENERS = ['mcp/catalog-reader.ts', 'electron/source-section-store.ts', 'electron/checkout-store.ts', 'electron/system-guide-store.ts'];

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

  it('imports only MCP modules, the nine shared electron modules, the SDK, zod and read-only Node APIs', () => {
    const mcpFiles = readdirSync(join(frontend, 'mcp')).filter(name => name.endsWith('.ts')).map(name => `mcp/${name}`);
    expect(graph.files).toEqual([...mcpFiles, ...SHARED_MODULES].sort());
    expect(graph.bare.filter(specifier => !ALLOWED_BARE.includes(specifier)), 'bare imports outside the design list').toEqual([]);
    expect(graph.dynamic).toBe(false);
    for (const forbidden of ['electron/catalog-store.ts', 'electron/catalog-rename.ts', 'electron/main.ts', 'electron/preload.cts']) expect(graph.files).not.toContain(forbidden);
    expect(graph.files.some(file => file.startsWith('src/'))).toBe(false);
  });

  it('contains no write, delete, process, network or shell calls; only the reader and three stores open files, read-only', () => {
    const forbiddenCall = /\b(writeFile|appendFile|rename|unlink|rmdir|rm|mkdir|copyFile|cp|createWriteStream|symlink|truncate|chmod|chown|utimes|fetch|XMLHttpRequest|WebSocket)\s*\(/;
    // Process execution is a bare call (no child_process import is allowed); a regex `.exec(` stays legal.
    const processCall = /(?<![.\w])(spawn|spawnSync|exec|execSync|execFile|execFileSync|fork)\s*\(/;
    expect([processCall.test('exec("cmd")'), processCall.test('spawnSync("cmd")'), processCall.test('/x/.exec(text)')]).toEqual([true, true, false]);
    for (const file of graph.files) {
      const source = read(file);
      expect(forbiddenCall.exec(source)?.[0] ?? null, file).toBeNull();
      expect(processCall.exec(source)?.[0] ?? null, file).toBeNull();
      expect(/from ['"](node:)?child_process['"]/.test(source), `${file} imports child_process`).toBe(false);
      expect(/process\.(argv|env|cwd|chdir)|listRoots|roots\/list|\.roots\b/.test(source), file).toBe(false);
      const importsFs = /from ['"]node:fs(\/promises)?['"]/.test(source);
      if (importsFs) expect(FILE_OPENERS, `${file} imports node:fs`).toContain(file);
    }
    for (const store of FILE_OPENERS.filter(file => file.startsWith('electron/'))) {
      const opens = [...read(store).matchAll(/(?<![.\w])open\(([^)]*)\)/g)].map(match => match[1]);
      expect(opens.length, store).toBeGreaterThan(0);
      for (const args of opens) expect(args, `${store} opens read-only`).toMatch(/,\s*'r'$/);
    }
    const reader = read('mcp/catalog-reader.ts');
    // The only native open is the read-only one; the others are the injected interface.
    const nativeOpens = [...reader.matchAll(/(?<![.\w])open\((?!path: string|path, signal\))/g)].map(match => reader.slice(match.index, match.index + 16));
    expect(nativeOpens).toEqual(["open(path, 'r');"]);
    expect(/from 'node:fs\/promises'/.test(reader) && /import \{ open, type FileHandle \} from 'node:fs\/promises'/.test(reader)).toBe(true);
  });

  it('registers exactly the eight read tools and no path/URL/shell tool or write-capable surface', () => {
    const server = read('mcp/catalog-server.ts');
    const registered = [...server.matchAll(/registerTool\('([^']+)'/g)].map(match => match[1]);
    expect([...registered].sort()).toEqual(['get_guide_card', 'get_record', 'get_source', 'get_system', 'list_guide_cards', 'list_systems', 'read_source_section', 'search_records']);
    expect(/registerResource|registerPrompt|setRequestHandler|sampling|elicit/.test(server)).toBe(false);
  });
});

describe('fixed production entry', () => {
  const main = read('mcp/main.ts');

  it('selects the catalog, the guide and the repository root only from its own module URL and injects no test seam or option', () => {
    expect(main).toContain("catalogPath: fileURLToPath(new URL('../../../records/catalog.json', import.meta.url)),");
    expect(main).toContain("new URL('../../../records/system-guide.json', import.meta.url)");
    expect(main).toContain("new URL('../../../../', import.meta.url)");
    expect(main).toContain('fileOperations: nodeCatalogFileOperations,');
    expect(main).toMatch(/createSourceSectionStore\(\{\s*repositoryRoot\b/);
    expect(main).toMatch(/createCheckoutStore\(\{\s*repositoryRoot\b/);
    expect(main).toContain('createSystemGuideStore(');
    // Design 「MCP」: a new guide store per request, so no module-level store instance.
    expect(main.split(/\r?\n/).filter(line => /^(const|let|var)\b.*createSystemGuideStore\(/.test(line))).toEqual([]);
    const serverCall = main.slice(main.indexOf('createCatalogServer('), main.indexOf('createCatalogServer(') + 600);
    for (const option of ['readSnapshot: reader.readSnapshot', 'version: BUILD_VERSION', 'readGuide', 'readSourceSection', 'readCheckout']) expect(serverCall, option).toContain(option);
    for (const forbidden of ['process.argv', 'process.env', 'process.cwd', 'onToolHandlerEntered', 'now:', 'listRoots', 'roots/list', 'CATALOG_PATH', 'parseArgs']) expect(main.includes(forbidden), forbidden).toBe(false);
    // The only mention of roots is the comment stating they do not select a path.
    expect(main.split(/\r?\n/).filter(line => /roots/i.test(line))).toEqual(['// No process arguments, environment, caller cwd or client roots select a path.']);
    expect([...main.matchAll(/createCatalogReader\(/g)]).toHaveLength(1);
  });

  it('built MCP entry and Electron main resolve the same catalog, guide and repository root; build layout keeps the Electron rootDir/outDir/main', () => {
    const mcpConfig = JSON.parse(read('tsconfig.mcp.json')) as { compilerOptions: Record<string, unknown>; include: string[] };
    const electronConfig = JSON.parse(read('tsconfig.electron.json')) as { compilerOptions: Record<string, unknown>; include: string[] };
    const pkg = JSON.parse(read('package.json')) as { main: string; scripts: Record<string, string> };
    expect(mcpConfig.compilerOptions).toMatchObject({ rootDir: '.', outDir: 'mcp-dist' });
    expect(mcpConfig.include).toEqual(['mcp/**/*.ts']);
    expect(electronConfig.compilerOptions).toMatchObject({ rootDir: 'electron', outDir: 'desktop-dist' });
    expect(electronConfig.include).toEqual(['electron/**/*.ts', 'electron/**/*.cts']);
    expect(pkg.main).toBe('desktop-dist/main.js');
    expect(pkg.scripts['mcp:build']).toBe('node scripts/build-mcp.mjs');
    const electronMain = read('electron/main.ts');
    expect(electronMain).toContain("fileURLToPath(new URL('../../records/catalog.json', import.meta.url))");
    expect(electronMain).toContain("new URL('../../records/system-guide.json', import.meta.url)");
    expect(electronMain).toContain("new URL('../../../', import.meta.url)");
    const builtMcp = pathToFileURL(join(frontend, 'mcp-dist', 'mcp', 'main.js'));
    const builtElectron = pathToFileURL(join(frontend, 'desktop-dist', 'main.js'));
    const pairs: Array<[string, string, string]> = [
      ['../../../records/catalog.json', '../../records/catalog.json', resolve(frontend, '..', 'records', 'catalog.json')],
      ['../../../records/system-guide.json', '../../records/system-guide.json', resolve(frontend, '..', 'records', 'system-guide.json')],
      ['../../../../', '../../../', resolve(frontend, '..', '..')],
    ];
    for (const [fromMcp, fromElectron, expected] of pairs) {
      const mcpPath = fileURLToPath(new URL(fromMcp, builtMcp));
      expect(mcpPath).toBe(fileURLToPath(new URL(fromElectron, builtElectron)));
      expect(resolve(mcpPath)).toBe(resolve(expected));
    }
  });

  // Design 「MCP 빌드 경계」: mcp-dist is the v1 build until step 5 runs `npm run mcp:build`.
  it.skipIf(!existsSync(join(frontend, 'mcp-dist', 'mcp', 'main.js')))('existing MCP build output contains only MCP and the nine shared modules with the three fixed URLs', () => {
    const listing = (directory: string): string[] => readdirSync(join(frontend, directory), { withFileTypes: true })
      .flatMap(entry => entry.isDirectory() ? listing(`${directory}/${entry.name}`) : [`${directory}/${entry.name}`]);
    const built = listing('mcp-dist').sort();
    const expected = [
      ...readdirSync(join(frontend, 'mcp')).filter(name => name.endsWith('.ts')).map(name => `mcp-dist/mcp/${name.replace(/\.ts$/, '.js')}`),
      ...SHARED_MODULES.map(file => `mcp-dist/${file.replace(/\.ts$/, '.js')}`),
    ].sort();
    expect(built, 'stale mcp-dist: rebuild with `npm run mcp:build` (step 5)').toEqual(expected);
    const builtMain = readFileSync(join(frontend, 'mcp-dist', 'mcp', 'main.js'), 'utf8');
    for (const url of ["new URL('../../../records/catalog.json', import.meta.url)", "new URL('../../../records/system-guide.json', import.meta.url)", "new URL('../../../../', import.meta.url)"]) expect(builtMain).toContain(url);
    expect(readFileSync(join(frontend, 'mcp-dist', 'mcp', 'build-info.js'), 'utf8')).toMatch(/^export const BUILD_VERSION = "0\.0\.0\+sha256\.[a-f0-9]{64}";\n$/);
  });

  // V3-R1: the digest contract is checked by behaviour in an owned TEMP copy (the canonical tree is
  // only read): changing any module of the MCP graph or of the nine shared modules (design 「MCP 빌드
  // 경계」), the MCP build config, the package manifest, the lockfile or the build script changes the
  // digest, and catalog or guide data does not.
  it.runIf(process.platform === 'win32')('the build digest covers MCP sources, the nine shared modules, build settings and the lockfile, not record data', () => {
    const copy = createTempCopy('v1-boundary');
    try {
      const base = copy.buildMcp();
      const inputs = [...new Set([...mcpGraph().files, ...SHARED_MODULES, 'tsconfig.mcp.json', 'package.json', 'package-lock.json', 'scripts/build-mcp.mjs'])];
      const unchanged = inputs.filter(input => {
        const path = join(copy.frontend, input);
        const bytes = readFileSync(path);
        // A trailing comment or JSON whitespace keeps the build valid.
        writeFileSync(path, Buffer.concat([bytes, Buffer.from(/\.(ts|mjs)$/.test(input) ? '\n// v3-r1 digest probe\n' : '\n')]));
        try { return copy.buildMcp() === base; } finally { writeFileSync(path, bytes); }
      });
      expect(unchanged).toEqual([]);
      for (const data of [copy.catalog, copy.guide]) {
        const bytes = readFileSync(data);
        writeFileSync(data, Buffer.concat([bytes, Buffer.from('\n')]));
        try { expect(copy.buildMcp(), data).toBe(base); } finally { writeFileSync(data, bytes); }
      }
      console.info(`[V3-R1-MEASURE] v1-boundary-digest ${JSON.stringify({ base, inputs })}`);
    } finally {
      copy.remove();
    }
  }, 600_000);
});

describe('UI sharing and preserved contracts', () => {
  it('the UI module only re-exports the shared contract and search; the store hashes through the shared function', () => {
    const ui = read('src/recordCatalog.ts').trim().split(/\r?\n/);
    expect(ui).toEqual(["export * from '../electron/catalog-contract';", "export { matchesQuery, filterSystems, filterRecords } from '../electron/catalog-query';"]);
    const store = read('electron/catalog-store.ts');
    expect(store).toContain("import { catalogHash } from './catalog-hash.js';");
    expect(store.includes('createHash')).toBe(false);
    expect([...store.matchAll(/catalogHash\(/g)].length).toBeGreaterThanOrEqual(1);
    expect(read('electron/catalog-hash.ts')).toContain("createHash('sha256').update(text, 'utf8').digest('hex')");
    // Record index v2 (goal 2026-10-06-record-source-unification 「만들 것」 3, index-v2-design.md
    // 「Electron 경계」): the store only reads, so no rename, backup or lock remains in it.
    expect(/\b(rename|unlink|writeFile|mkdir)\b/.test(store)).toBe(false);
  });

  it('the store is built only from the fixed catalog path and no save path reaches IPC', () => {
    const electronMain = read('electron/main.ts');
    expect(electronMain).toMatch(/createCatalogStore\(\s*fileURLToPath\(new URL\('\.\.\/\.\.\/records\/catalog\.json', import\.meta\.url\)\),?\s*\)/);
    expect(electronMain.includes('system-records-last-good')).toBe(false);
    expect(existsSync(join(frontend, 'electron', 'catalog-rename.ts'))).toBe(false);
    const preload = read('electron/preload.cts');
    expect(/rename|save/i.test(preload)).toBe(false);
  });
});

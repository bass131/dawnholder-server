// @vitest-environment node
// V3: build-path and build-identity regression (goal 범위·D4, V3 row of the verification table).
// - The Electron entry stays `desktop-dist/main.js` and resolves `../../records/catalog.json`;
//   the built MCP entry resolves the same 05 catalog, the 05 guide and the repository root
//   (index-v2-design.md 「MCP」 구현 접점). Checked on a FRESH build in an owned TEMP copy (old
//   ignored output in the canonical tree is not evidence for the current source).
// - serverInfo.version is a build digest that changes with every MCP source, shared module,
//   build config and lockfile input, and not with catalog or guide data (which need no rebuild).
// - A catalog edit in the copy is read by the UI store and the copy's MCP entry with the same hash,
//   no rebuild. The UI save and its backup were removed with the record index v2 (goal
//   2026-10-06-record-source-unification 「만들 것」 3, index-v2-design.md 「Electron 경계」).
// Source edits for the digest checks happen only inside the TEMP copy.
import { readFileSync, writeFileSync } from 'node:fs';
import { dirname, join, relative, resolve, sep } from 'node:path';
import { afterAll, beforeAll, describe, expect, it } from 'vitest';
import { Client } from '@modelcontextprotocol/client';
import { StdioClientTransport } from '@modelcontextprotocol/client/stdio';
import { createCatalogStore } from '../electron/catalog-store';
import { CANONICAL_CATALOG, CANONICAL_GUIDE, CANONICAL_REPOSITORY, FRONTEND, builtVersion, createTempCopy, moduleRelativePaths, relativeImportClosure, sha256, type TempCopy } from './mcp-v3/temp-copy';

const json = (path: string) => JSON.parse(readFileSync(path, 'utf8')) as Record<string, unknown>;
// Design 「MCP」 빌드 경계: the nine pure shared modules the MCP entry may reach.
const SHARED_MODULES = [
  'electron/catalog-contract', 'electron/catalog-hash', 'electron/catalog-query',
  'electron/checkout-contract', 'electron/checkout-store',
  'electron/source-section-contract', 'electron/source-section-store',
  'electron/system-guide-contract', 'electron/system-guide-store',
];
const ALLOWED_BARE = [
  '@modelcontextprotocol/server', '@modelcontextprotocol/server/stdio', 'node:crypto', 'node:fs', 'node:fs/promises', 'node:path', 'node:url', 'node:util', 'zod',
];
const measure = (label: string, value: unknown) => console.info(`[V3-MEASURE] ${label} ${JSON.stringify(value)}`);

describe('canonical package and build configuration keep the entry layout', () => {
  it('package main, Electron rootDir/outDir and the MCP outDir/start command', () => {
    const pkg = json(join(FRONTEND, 'package.json')) as { main: string; scripts: Record<string, string> };
    expect(pkg.main).toBe('desktop-dist/main.js');
    expect(pkg.scripts['desktop:build']).toContain('tsc --project tsconfig.electron.json');
    expect(pkg.scripts['mcp:build']).toBe('node scripts/build-mcp.mjs');
    expect(pkg.scripts['mcp:start']).toBe('node mcp-dist/mcp/main.js');
    const electron = json(join(FRONTEND, 'tsconfig.electron.json')) as { compilerOptions: Record<string, string>; include: string[] };
    expect(electron.compilerOptions.rootDir).toBe('electron');
    expect(electron.compilerOptions.outDir).toBe('desktop-dist');
    expect(electron.include).toEqual(['electron/**/*.ts', 'electron/**/*.cts']);
    const mcp = json(join(FRONTEND, 'tsconfig.mcp.json')) as { compilerOptions: Record<string, string> };
    expect(mcp.compilerOptions.outDir).toBe('mcp-dist');
  });
});

describe.runIf(process.platform === 'win32')('fresh build in an owned TEMP 05 copy', () => {
  let copy: TempCopy;
  let baseVersion: string;
  beforeAll(() => {
    copy = createTempCopy('build');
    copy.buildDesktopMain();
    baseVersion = copy.buildMcp();
  }, 180_000);
  afterAll(() => {
    if (copy) measure('cleanup', copy.remove());
    // The junction target (canonical install) must survive the cleanup.
    expect(readFileSync(join(FRONTEND, 'node_modules', 'typescript', 'package.json'), 'utf8')).toContain('"typescript"');
  }, 180_000);

  it('the package main exists after the build and both built entries resolve the copy\'s own catalog; the MCP entry also its guide and root', () => {
    const pkg = json(join(copy.frontend, 'package.json')) as { main: string };
    const desktopMain = join(copy.frontend, pkg.main);
    const desktop = moduleRelativePaths(desktopMain);
    const mcp = moduleRelativePaths(join(copy.frontend, 'mcp-dist', 'mcp', 'main.js'));
    measure('entry-paths', { desktop, mcp });
    expect(desktop['../../records/catalog.json']).toBe(copy.catalog);
    expect(mcp['../../../records/catalog.json']).toBe(copy.catalog);
    expect(mcp['../../../records/system-guide.json']).toBe(copy.guide);
    expect(resolve(mcp['../../../../'] ?? '')).toBe(resolve(copy.repositoryRoot));
    expect(Object.keys(mcp).sort()).toEqual(['../../../../', '../../../records/catalog.json', '../../../records/system-guide.json']);
    for (const path of [...Object.values(desktop), ...Object.values(mcp)]) {
      expect(path.startsWith(copy.root + sep)).toBe(true);
      expect(path).not.toBe(CANONICAL_CATALOG);
      expect(path).not.toBe(CANONICAL_GUIDE);
      expect(resolve(path)).not.toBe(resolve(CANONICAL_REPOSITORY));
    }
  });

  it('build identity is content based: the copy digest equals the canonical build digest of the same sources', () => {
    expect(baseVersion).toMatch(/^0\.0\.0\+sha256\.[0-9a-f]{64}$/);
    // The canonical mcp-dist is a user artifact this step may not rebuild; until the step-5 build
    // it still holds the previous sources' digest (pr2-t2 contract J).
    expect(baseVersion, 'canonical mcp-dist is older than the sources: rebuild in step 5').toBe(builtVersion(FRONTEND));
  });

  it('built import graphs keep the dependency direction: MCP reaches only MCP and pure shared modules; the desktop main never reaches MCP', () => {
    const toJs = (specifier: string, from: string) => resolve(dirname(from), specifier);
    const mcpDist = join(copy.frontend, 'mcp-dist');
    const mcp = relativeImportClosure(join(mcpDist, 'mcp', 'main.js'), toJs);
    const reached = mcp.files.map(file => relative(mcpDist, file).replaceAll('\\', '/'));
    measure('mcp-closure', { reached, bare: mcp.bare });
    expect(reached.filter(file => file.startsWith('electron/'))).toEqual(SHARED_MODULES.map(file => `${file}.js`));
    expect(mcp.bare.filter(name => !ALLOWED_BARE.includes(name)), 'bare imports outside the design list').toEqual([]);
    const desktopDist = join(copy.frontend, 'desktop-dist');
    const desktop = relativeImportClosure(join(desktopDist, 'main.js'), toJs);
    expect(desktop.files.every(file => file.startsWith(desktopDist + sep))).toBe(true);
    expect(desktop.bare.some(name => name.startsWith('@modelcontextprotocol/') || name === 'zod')).toBe(false);
  });

  it('a catalog edit in the copy is read by the UI store and the copy\'s MCP entry with the same hash, no rebuild, version is the build digest', async () => {
    // Index v2 has no hand-written revision (「색인 형식」): the marker goes into a system title.
    const original = JSON.parse(readFileSync(copy.catalog, 'utf8')) as { systems: Array<{ id: string; title: string }> };
    const target = original.systems[0];
    if (!target) throw new Error('canonical catalog has no system');
    const markedTitle = `${target.title} v3-build-copy-marker`;
    writeFileSync(copy.catalog, `${JSON.stringify({ ...original, systems: original.systems.map(system => (system.id === target.id ? { ...system, title: markedTitle } : system)) }, null, 2)}\n`);
    const store = createCatalogStore(copy.catalog);
    const transport = new StdioClientTransport({ command: process.execPath, args: [join(copy.frontend, 'mcp-dist', 'mcp', 'main.js')], cwd: FRONTEND, stderr: 'pipe' });
    const client = new Client({ name: 'v3-build', version: '0.0.0' }, { supportedProtocolVersions: ['2025-11-25'] });
    await client.connect(transport);
    try {
      type Envelope = { ok: boolean; snapshot: { hash: string }; data?: { system: { title: string } } };
      const read = async () => ((await client.callTool({ name: 'get_system', arguments: { id: target.id } })).structuredContent as Envelope);
      const first = await read();
      const stored = await store.read();
      expect(first.ok).toBe(true);
      expect(first.snapshot.hash).toBe(sha256(readFileSync(copy.catalog)));
      expect(first.snapshot.hash).not.toBe(sha256(readFileSync(CANONICAL_CATALOG)));
      expect(stored.ok && stored.version).toBe(first.snapshot.hash);
      expect(Object.keys(first.snapshot)).toEqual(['hash']);
      expect(first.data?.system.title).toBe(markedTitle);
      expect(client.getServerVersion()?.version).toBe(baseVersion);
      expect(baseVersion).not.toContain(first.snapshot.hash);
      expect(builtVersion(copy.frontend)).toBe(baseVersion);
    } finally {
      await client.close();
    }
  }, 60_000);

  it('the digest covers every source the MCP entry reaches plus build config and lockfile, but not catalog or guide data', () => {
    const toTs = (specifier: string, from: string) => resolve(dirname(from), specifier.replace(/\.js$/, '.ts'));
    const sources = relativeImportClosure(join(copy.frontend, 'mcp', 'main.ts'), toTs).files.map(file => relative(copy.frontend, file).replaceAll('\\', '/'));
    for (const shared of SHARED_MODULES) expect(sources).toContain(`${shared}.ts`);
    const inputs = [...sources, 'tsconfig.mcp.json', 'package.json', 'package-lock.json', 'scripts/build-mcp.mjs'];
    const versions: Record<string, string> = {};
    for (const input of inputs) {
      const path = join(copy.frontend, input);
      const bytes = readFileSync(path);
      // Append a no-op (comment or JSON whitespace) so the build still succeeds.
      writeFileSync(path, Buffer.concat([bytes, Buffer.from(/\.(ts|mjs)$/.test(input) ? '\n// v3 digest probe\n' : '\n')]));
      try { versions[input] = copy.buildMcp(); } finally { writeFileSync(path, bytes); }
    }
    measure('digest-inputs', { baseVersion, inputs: Object.keys(versions) });
    for (const input of inputs) expect({ input, changed: versions[input] !== baseVersion }).toEqual({ input, changed: true });
    expect(new Set(Object.values(versions)).size).toBe(inputs.length);

    const catalog = readFileSync(copy.catalog);
    writeFileSync(copy.catalog, Buffer.concat([catalog, Buffer.from('\n')]));
    try { expect(copy.buildMcp()).toBe(baseVersion); } finally { writeFileSync(copy.catalog, catalog); }
    const guide = readFileSync(copy.guide);
    writeFileSync(copy.guide, Buffer.concat([guide, Buffer.from('\n')]));
    try { expect(copy.buildMcp()).toBe(baseVersion); } finally { writeFileSync(copy.guide, guide); }
    expect(copy.buildMcp()).toBe(baseVersion);
  }, 300_000);
});

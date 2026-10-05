// @vitest-environment node
// Independent test of the Mermaid loader build boundary (메인 msg_e4a38159a4e3 조건 1·2). Only the flowchart, sequence
// and state diagram loaders and the dagre layout loader of the two registered Mermaid sources may enter the bundle.
// A pinned input drift is refused before bundling with a message naming what to review, and an excluded loader that
// is reached at runtime rejects instead of producing a diagram. Loader targets are read here from the installed
// sources by detector id and loader name; the policy's own tables are never used as the expected answer.
import { copyFileSync, mkdirSync, mkdtempSync, readdirSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { createRequire } from 'node:module';
import { tmpdir } from 'node:os';
import { dirname, join } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { build, type Plugin } from 'vite';
import { afterAll, describe, expect, it } from 'vitest';

interface LoaderEdge { importer: string; target: string; kind: string }
interface LoaderPolicy {
  plugin: Plugin;
  metadata: { allowedEdges: LoaderEdge[]; blockedEdges: LoaderEdge[] };
}
interface PolicyModule { createDiagramLoaderPolicy(root: string): Promise<LoaderPolicy> }
type ResolveHook = (target: string, importer: string | undefined) => string | null;
interface ProbeExports {
  mermaid: { parse(text: string): Promise<unknown> };
  layoutRender(data: unknown, svg: unknown): Promise<unknown>;
  select(node: Element): unknown;
}
interface ProbeWindow { eval(code: string): unknown; document: Document; structuredClone?: typeof structuredClone; LoaderProbe?: ProbeExports }
interface JsdomModule {
  JSDOM: new (html: string, options: { runScripts: 'outside-only'; pretendToBeVisual: boolean }) => { window: ProbeWindow };
}

const frontend = fileURLToPath(new URL('..', import.meta.url));
const policyUrl = pathToFileURL(join(frontend, 'scripts/diagram-loader-policy.mjs')).href;
const { createDiagramLoaderPolicy } = await import(policyUrl) as PolicyModule;
const { JSDOM } = createRequire(import.meta.url)('jsdom') as JsdomModule;
const slash = (path: string) => path.replaceAll('\\', '/');

// The two registered sources: the package's ESM import entry and the chunk that registers the layout loaders.
const mermaidPackage = 'node_modules/mermaid/package.json';
const lodashPackage = 'node_modules/lodash-es/package.json';
const packageExports = JSON.parse(readFileSync(join(frontend, mermaidPackage), 'utf8')) as { exports: { '.': { import: string } } };
const coreFile = slash(join('node_modules/mermaid', packageExports.exports['.'].import));
const chunkDir = 'node_modules/mermaid/dist/chunks/mermaid.core';
const layoutName = readdirSync(join(frontend, chunkDir)).find(name =>
  readFileSync(join(frontend, chunkDir, name), 'utf8').includes('registerDefaultLayoutLoaders('));
if (!layoutName) throw new Error('installed Mermaid has no layout loader registry chunk');
const layoutFile = `${chunkDir}/${layoutName}`;
const coreSource = readFileSync(join(frontend, coreFile), 'utf8');
const layoutSource = readFileSync(join(frontend, layoutFile), 'utf8');

function pairs(source: string, pattern: RegExp): Array<[string, string]> {
  return [...source.matchAll(pattern)].map(match => [match[1] ?? '', match[2] ?? '']);
}
// Each core detector block names its id before its loader import; each layout loader names itself before its import.
const detectors = pairs(coreSource, /var id\d* = "([^"]+)";[\s\S]*?await import\("([^"]+)"\)/g);
const layouts = pairs(layoutSource, /name: "([^"]+)",\s*loader: [^\n]*?import\("([^"]+)"\)/g);
const elkTarget = /import\("(\.\/elk-[^"]+)"\)/.exec(layoutSource)?.[1];
if (elkTarget) layouts.push(['elk', elkTarget]);
const allowedDetectorIds = new Set(['flowchart-v2', 'flowchart-elk', 'sequence', 'stateDiagram']);
const allowedDiagramTargets = new Set(detectors.filter(([id]) => allowedDetectorIds.has(id)).map(([, target]) => target));
const blockedDiagramTargets = new Set(detectors.map(([, target]) => target).filter(target => !allowedDiagramTargets.has(target)));
const dagreTarget = layouts.find(([name]) => name === 'dagre')?.[1] ?? '';
const blockedLayoutTargets = new Set(layouts.map(([, target]) => target).filter(target => target !== dagreTarget));
const allDynamicTargets = (source: string) => [...source.matchAll(/\bimport\(\s*"([^"]+)"\s*\)/g)].map(match => match[1] ?? '');

const originalModule = (file: string, target: string) => slash(join(frontend, dirname(file), target));
const realIds = (ids: string[]) => ids.filter(id => !id.startsWith('\0')).map(slash);
const packageModules = (ids: string[], name: string) => realIds(ids).filter(id => id.includes(`/node_modules/${name}/`));
// The probe runs in a jsdom realm, so its Error is not this realm's Error; read the message structurally.
const errorMessage = (error: unknown) => (typeof error === 'object' && error !== null && 'message' in error && typeof error.message === 'string' ? error.message : '');
const isPolicyRejection = (error: unknown) => /^Unsupported Mermaid (diagram|layout) loader: /.test(errorMessage(error));

interface BuildResult { code: string; moduleIds: string[] }
async function bundle(root: string, entry: string, plugins: Plugin[], alias: Array<{ find: RegExp; replacement: string }> = []): Promise<BuildResult> {
  const moduleIds: string[] = [];
  const output = await build({
    root, configFile: false, logLevel: 'silent', publicDir: false,
    resolve: { alias },
    build: {
      write: false, minify: false,
      lib: { entry, formats: ['iife'], name: 'LoaderProbe', fileName: () => 'probe.js' },
      rolldownOptions: { output: { codeSplitting: false } },
    },
    plugins: [...plugins, { name: 'probe-graph', generateBundle() { moduleIds.push(...this.getModuleIds()); } }],
  });
  const results = Array.isArray(output) ? output : [output];
  const chunks = results.flatMap(result => ('output' in result ? result.output : [])).filter(item => item.type === 'chunk');
  expect(chunks).toHaveLength(1);
  return { code: chunks[0]?.code ?? '', moduleIds };
}

// The runtime probe exports Mermaid, the layout registry's render and d3's select from an entry that is never written.
function probeEntry(root: string): Plugin {
  const entry = join(root, '__loader_probe__.js');
  const code = [
    'export { default as mermaid } from \'mermaid\';',
    `export { render as layoutRender } from ${JSON.stringify(slash(join(root, layoutFile)))};`,
    'export { select } from \'d3\';',
  ].join('\n');
  return { name: 'loader-probe-entry', enforce: 'pre', resolveId: id => (id === entry ? id : null), load: id => (id === entry ? code : null) };
}

function evaluate(code: string): { exports: ProbeExports; document: Document } {
  const { window } = new JSDOM('<!doctype html><html><body></body></html>', { runScripts: 'outside-only', pretendToBeVisual: true });
  // jsdom lacks structuredClone, which the Electron renderer has and the bundled parsers use.
  window.structuredClone ??= structuredClone;
  window.eval(code);
  if (!window.LoaderProbe) throw new Error('probe bundle did not expose its exports');
  return { exports: window.LoaderProbe, document: window.document };
}

async function outcome(run: () => Promise<unknown>): Promise<{ resolved: boolean; error?: unknown }> {
  try {
    await run();
    return { resolved: true };
  } catch (error) {
    return { resolved: false, error };
  }
}

describe('targets read from the installed Mermaid sources', () => {
  it('finds the registered loaders this test classifies', () => {
    expect(new Set(allDynamicTargets(coreSource))).toEqual(new Set(detectors.map(([, target]) => target)));
    expect(new Set(allDynamicTargets(layoutSource))).toEqual(new Set(layouts.map(([, target]) => target)));
    expect(allowedDiagramTargets.size).toBe(3);
    expect(dagreTarget).not.toBe('');
    expect(blockedDiagramTargets.size).toBeGreaterThan(0);
    expect(blockedLayoutTargets.size).toBeGreaterThan(0);
  });
});

describe('product renderer graph with the policy', () => {
  it('keeps the allowed loader modules and replaces every other registered loader before resolution', async () => {
    const policy = await createDiagramLoaderPolicy(frontend);
    const elkAlias = [{ find: /^elkjs(?:\/.*)?$/, replacement: join(frontend, 'src/diagrams/elk-disabled.ts') }];
    const { moduleIds } = await bundle(frontend, join(frontend, 'src/diagrams/renderer.ts'), [policy.plugin], elkAlias);
    const ids = new Set(realIds(moduleIds));
    for (const target of allowedDiagramTargets) expect(ids, target).toContain(originalModule(coreFile, target));
    expect(ids).toContain(originalModule(layoutFile, dagreTarget));
    for (const target of blockedDiagramTargets) expect(ids, target).not.toContain(originalModule(coreFile, target));
    for (const target of blockedLayoutTargets) expect(ids, target).not.toContain(originalModule(layoutFile, target));
    const blocked = policy.metadata.blockedEdges.map(edge => `${edge.importer} ${edge.target}`).sort();
    const expected = [
      ...[...blockedDiagramTargets].map(target => `${coreFile} ${target}`),
      ...[...blockedLayoutTargets].map(target => `${layoutFile} ${target}`),
    ].sort();
    expect(blocked).toEqual(expected);
    for (const name of ['@mermaid-js/parser', 'cytoscape', 'cytoscape-cose-bilkent', 'elkjs']) expect(packageModules(moduleIds, name), name).toEqual([]);
  });
});

describe('excluded loaders reached at runtime', () => {
  const allowedSamples = ['flowchart TD\n  A --> B', 'sequenceDiagram\n  A->>B: hi', 'stateDiagram-v2\n  [*] --> S'];
  const excludedSamples = ['gitGraph\n  commit', 'pie\n  "a" : 1', 'classDiagram\n  class A', 'architecture-beta\n  service db(database)[DB]'];

  it('builds core and layout with the policy, loads, parses the three allowed types and rejects the others', async () => {
    const policy = await createDiagramLoaderPolicy(frontend);
    const { code, moduleIds } = await bundle(frontend, join(frontend, '__loader_probe__.js'), [policy.plugin, probeEntry(frontend)]);
    for (const target of blockedLayoutTargets) expect(realIds(moduleIds)).not.toContain(originalModule(layoutFile, target));
    expect(packageModules(moduleIds, 'elkjs')).toEqual([]);
    const { exports, document } = evaluate(code);
    for (const text of allowedSamples) {
      const result = await outcome(() => exports.mermaid.parse(text));
      expect(result, text).toEqual({ resolved: true });
    }
    for (const text of excludedSamples) {
      const result = await outcome(() => exports.mermaid.parse(text));
      expect(result.resolved, text).toBe(false);
      expect(isPolicyRejection(result.error), `${text}: ${String(result.error)}`).toBe(true);
    }
    const svg = exports.select(document.body.appendChild(document.createElementNS('http://www.w3.org/2000/svg', 'svg')));
    for (const [name] of layouts) {
      const result = await outcome(() => exports.layoutRender({ layoutAlgorithm: name, nodes: [], edges: [], config: {} }, svg));
      if (name === 'dagre') expect(isPolicyRejection(result.error), name).toBe(false);
      else expect(isPolicyRejection(result.error), `${name}: ${String(result.error)}`).toBe(true);
    }
  });

  it('accepts the same excluded inputs without the policy, so the rejection comes from the build boundary', async () => {
    const { code, moduleIds } = await bundle(frontend, join(frontend, '__loader_probe__.js'), [probeEntry(frontend)],
      [{ find: /^elkjs(?:\/.*)?$/, replacement: join(frontend, 'src/diagrams/elk-disabled.ts') }]);
    for (const target of blockedDiagramTargets) expect(realIds(moduleIds)).toContain(originalModule(coreFile, target));
    expect(packageModules(moduleIds, 'cytoscape').length).toBeGreaterThan(0);
    const { exports } = evaluate(code);
    for (const text of excludedSamples) {
      const result = await outcome(() => exports.mermaid.parse(text));
      expect(result, `${text}: ${String(result.error)}`).toEqual({ resolved: true });
    }
  });
});

describe('resolver scope of the policy plugin', () => {
  const coreId = slash(join(frontend, coreFile));
  const gitTarget = detectors.find(([id]) => id === 'gitGraph')?.[1] ?? '';
  const sequenceTarget = detectors.find(([id]) => id === 'sequence')?.[1] ?? '';

  it('applies only to the exact registered importer and its registered targets', async () => {
    const policy = await createDiagramLoaderPolicy(frontend);
    const resolveId = policy.plugin.resolveId as ResolveHook;
    expect(resolveId(gitTarget, coreId)).not.toBeNull();
    expect(resolveId(sequenceTarget, coreId)).toBeNull();
    expect(resolveId(gitTarget, slash(join(frontend, 'src/diagrams/renderer.ts')))).toBeNull();
    expect(resolveId(gitTarget, undefined)).toBeNull();
    expect(resolveId('./chunks/mermaid.core/not-registered.mjs', coreId)).toBeNull();
    expect(policy.metadata.allowedEdges.map(edge => edge.target)).toEqual([sequenceTarget]);
    expect(policy.metadata.blockedEdges.map(edge => edge.target)).toEqual([gitTarget]);
  });

  it('does not turn an inherited enumerable key into an allowed loader', async () => {
    const policy = await createDiagramLoaderPolicy(frontend);
    const resolveId = policy.plugin.resolveId as ResolveHook;
    const prototype = Object.prototype as Record<string, unknown>;
    prototype.gitGraph = gitTarget;
    prototype.elk = elkTarget;
    try {
      expect(resolveId(gitTarget, coreId)).not.toBeNull();
      expect(resolveId(elkTarget ?? '', slash(join(frontend, layoutFile)))).not.toBeNull();
    } finally {
      delete prototype.gitGraph;
      delete prototype.elk;
    }
    expect(policy.metadata.allowedEdges).toEqual([]);
  });
});

describe('pinned input drift is refused before bundling', () => {
  const owned: string[] = [];
  afterAll(() => { for (const path of owned) rmSync(path, { recursive: true, force: true }); });

  function fixture(edit: (root: string) => void): string {
    const root = mkdtempSync(join(tmpdir(), 'dh-loader-policy-'));
    owned.push(root);
    for (const file of [mermaidPackage, lodashPackage, coreFile, layoutFile]) {
      mkdirSync(dirname(join(root, file)), { recursive: true });
      copyFileSync(join(frontend, file), join(root, file));
    }
    edit(root);
    return root;
  }
  const rewrite = (file: string, change: (text: string) => string) => (root: string) => {
    const path = join(root, file);
    const before = readFileSync(path, 'utf8');
    const after = change(before);
    expect(after).not.toBe(before);
    writeFileSync(path, after);
  };
  const once = (text: string, from: string, to: string) => {
    const index = text.indexOf(from);
    return index < 0 ? text : text.slice(0, index) + to + text.slice(index + from.length);
  };
  const lastOnce = (text: string, from: string, to: string) => {
    const index = text.lastIndexOf(from);
    return index < 0 ? text : text.slice(0, index) + to + text.slice(index + from.length);
  };
  const flowTarget = detectors.find(([id]) => id === 'flowchart-v2')?.[1] ?? '';
  const sequenceTarget = detectors.find(([id]) => id === 'sequence')?.[1] ?? '';
  const swimlaneTarget = layouts.find(([name]) => name === 'swimlane')?.[1] ?? '';

  const cases: Array<[string, string, (root: string) => void]> = [
    ['Mermaid version', mermaidPackage, rewrite(mermaidPackage, text => once(text, '"version": "12.0.0"', '"version": "12.0.1"'))],
    ['Mermaid manifest bytes at the same version', mermaidPackage, rewrite(mermaidPackage, text => `${text}\n`)],
    ['Mermaid ESM export', mermaidPackage, rewrite(mermaidPackage, text => once(text, `"import": "${packageExports.exports['.'].import}"`, '"import": "./dist/mermaid.esm.mjs"'))],
    ['lodash-es version', lodashPackage, rewrite(lodashPackage, text => once(text, '"version": "4.18.1"', '"version": "4.18.2"'))],
    ['lodash-es manifest bytes at the same version', lodashPackage, rewrite(lodashPackage, text => `${text} `)],
    ['core bytes only', coreFile, rewrite(coreFile, text => `${text}\n// drift\n`)],
    ['core new dynamic target', coreFile, rewrite(coreFile, text => `${text}\nexport const extraLoader = () => import("./chunks/mermaid.core/extra.mjs");\n`)],
    ['core duplicate of an allowed target', coreFile, rewrite(coreFile, text => `${text}\nexport const again = () => import(${JSON.stringify(sequenceTarget)});\n`)],
    ['core one of the shared flow imports removed', coreFile, rewrite(coreFile, text => lastOnce(text, `await import(${JSON.stringify(flowTarget)})`, 'await Promise.resolve({ diagram: void 0 })'))],
    ['core new static import', coreFile, rewrite(coreFile, text => `${text}\nimport "./chunks/mermaid.core/extra-static.mjs";\n`)],
    ['core nonliteral import', coreFile, rewrite(coreFile, text => `${text}\nexport const anyLoader = name => import(name);\n`)],
    ['layout bytes only', layoutFile, rewrite(layoutFile, text => `${text}\n// drift\n`)],
    ['layout duplicate of an excluded target', layoutFile, rewrite(layoutFile, text => `${text}\nexport const again = () => import(${JSON.stringify(swimlaneTarget)});\n`)],
    ['layout dagre target renamed', layoutFile, rewrite(layoutFile, text => once(text, `import(${JSON.stringify(dagreTarget)})`, 'import("./dagre-RENAMED.mjs")'))],
  ];

  it('accepts an unchanged copy of the four pinned files', async () => {
    const policy = await createDiagramLoaderPolicy(fixture(() => undefined));
    expect(policy.plugin.name).toBeTypeOf('string');
  });

  it.each(cases)('%s', async (_name, file, edit) => {
    const root = fixture(edit);
    const result = await outcome(() => createDiagramLoaderPolicy(root));
    expect(result.resolved).toBe(false);
    const message = errorMessage(result.error);
    expect(message).toContain(file);
    expect(message).toContain('scripts/diagram-loader-policy.mjs');
  });

  it('refuses a missing registered source', async () => {
    const root = fixture(root => rmSync(join(root, layoutFile)));
    expect((await outcome(() => createDiagramLoaderPolicy(root))).resolved).toBe(false);
  });
});

describe.runIf(process.platform === 'win32')('root path spelling on Windows', () => {
  // `cd /d c:\...` in cmd gives Node a lower-case drive letter in import.meta.url, which build-diagrams uses as root.
  it('keeps the boundary when the project root is given with a lower-case drive letter', async () => {
    const root = frontend.replace(/^[A-Z]:/, drive => drive.toLowerCase());
    expect(root).not.toBe(frontend);
    const policy = await createDiagramLoaderPolicy(root);
    const elkAlias = [{ find: /^elkjs(?:\/.*)?$/, replacement: join(root, 'src/diagrams/elk-disabled.ts') }];
    const { moduleIds } = await bundle(root, join(root, 'src/diagrams/renderer.ts'), [policy.plugin], elkAlias);
    expect(policy.metadata.blockedEdges.length).toBe(blockedDiagramTargets.size + blockedLayoutTargets.size);
    expect(packageModules(moduleIds, 'cytoscape')).toEqual([]);
    expect(packageModules(moduleIds, '@mermaid-js/parser')).toEqual([]);
  });
});

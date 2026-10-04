import { build, parseSync } from 'vite';
import { readFile, writeFile, copyFile, mkdir } from 'node:fs/promises';
import { createHash } from 'node:crypto';
import { fileURLToPath } from 'node:url';
import { dirname, join, relative, resolve } from 'node:path';
import { createDiagramLoaderPolicy } from './diagram-loader-policy.mjs';

const root = resolve(fileURLToPath(new URL('..', import.meta.url)));
const dist = join(root, 'dist');
const mermaidManifest = JSON.parse(await readFile(join(root, 'node_modules/mermaid/package.json'), 'utf8'));
const lodashManifest = JSON.parse(await readFile(join(root, 'node_modules/lodash-es/package.json'), 'utf8'));
const project = JSON.parse(await readFile(join(root, 'package.json'), 'utf8'));
const loaderPolicy = await createDiagramLoaderPolicy(root);
if (lodashManifest.version !== project.overrides?.['lodash-es']) {
  throw new Error('package.json overrides.lodash-es와 scripts/diagram-loader-policy.mjs의 승인 버전 고정을 함께 검토하세요.');
}
const graph = [];
const output = [];
await build({
  root, configFile: false, base: './', publicDir: false,
  resolve: { alias: [{ find: /^elkjs(?:\/.*)?$/, replacement: join(root, 'src/diagrams/elk-disabled.ts') }] },
  build: {
    outDir: dist, emptyOutDir: false, assetsInlineLimit: 0, minify: false,
    lib: { entry: join(root, 'src/diagrams/renderer.ts'), formats: ['iife'], name: 'ManagementDiagramRenderer', fileName: () => 'diagram-renderer.js' },
    rolldownOptions: { output: { codeSplitting: false, comments: { legal: true } } },
  },
  plugins: [loaderPolicy.plugin, {
    name: 'diagram-build-contract',
    async generateBundle(_options, bundle) {
      for (const id of this.getModuleIds()) {
        const info = this.getModuleInfo(id);
        graph.push({ id, importedIds: info?.importedIds ?? [], dynamicallyImportedIds: info?.dynamicallyImportedIds ?? [], isExternal: info?.isExternal ?? false });
      }
      for (const asset of Object.values(bundle)) {
        if (asset.type === 'chunk') output.push({ fileName: asset.fileName, moduleIds: asset.moduleIds, modules: asset.modules, imports: asset.imports, dynamicImports: asset.dynamicImports });
      }
      await mkdir(dist, { recursive: true });
      await writeFile(join(dist, 'diagram-build-graph-diagnostic.json'), JSON.stringify({ graph, output }, null, 2));
      if (graph.some(module => /[\\/]node_modules[\\/]elkjs(?:[\\/]|$)/.test(module.id))) throw new Error('제품 graph에 EPL elkjs 모듈이 있습니다.');
      if (!graph.some(module => module.id.replaceAll('\\', '/').endsWith('/mermaid/dist/mermaid.core.mjs'))) throw new Error('ESM core를 graph에서 찾을 수 없습니다.');
      if (!graph.some(module => /[\\/]node_modules[\\/]lodash-es[\\/]/.test(module.id))) throw new Error('lodash-es 실제 모듈을 graph에서 찾을 수 없습니다.');
      for (const edge of loaderPolicy.metadata.blockedEdges) {
        const originalTarget = resolve(root, dirname(edge.importer), edge.target).replaceAll('\\', '/');
        if (graph.some(module => module.id.replaceAll('\\', '/') === originalTarget)) {
          throw new Error(`제외 loader 원천이 제품 graph에 남았습니다: ${edge.importer} → ${edge.target}`);
        }
      }
      // Rolldown reports collapsed dynamic chunks as self references. Inspect both that graph and actual script syntax.
      if (output.length !== 1 || output.some(chunk => chunk.imports.length || chunk.dynamicImports.some(name => name !== chunk.fileName))) throw new Error('sandbox에는 외부 import 없는 단일 classic script만 허용합니다.');
      for (const asset of Object.values(bundle)) {
        if (asset.type !== 'chunk') continue;
        const parsed = parseSync(asset.fileName, asset.code, { sourceType: 'script' });
        if (parsed.errors.length) throw new Error('자체 IIFE를 classic script로 파싱할 수 없습니다.');
        const forbidden = [];
        function visit(value) {
          if (!value || typeof value !== 'object') return;
          if (['ImportExpression', 'ImportDeclaration', 'ExportNamedDeclaration', 'ExportDefaultDeclaration', 'ExportAllDeclaration'].includes(value.type)) forbidden.push(value.type);
          for (const child of Object.values(value)) { if (Array.isArray(child)) child.forEach(visit); else visit(child); }
        }
        visit(parsed.program);
        if (forbidden.length) throw new Error(`IIFE에 모듈 구문이 있습니다: ${forbidden.join(',')}`);
      }
    },
  }],
});
const bundle = await readFile(join(dist, 'diagram-renderer.js'));
if (/org\.eclipse\.elk|Eclipse Public License|elkjs\/lib\/elk\.bundled/.test(bundle.toString('utf8'))) throw new Error('생성 산출물에 EPL 원천 표식이 있습니다.');
const included = [...new Set(output.flatMap(chunk => chunk.moduleIds))];
const packages = new Map();
for (const id of included) {
  let location = dirname(resolve(id.split('?')[0]));
  while (location.startsWith(root) && location !== root) {
    try {
      const manifest = JSON.parse(await readFile(join(location, 'package.json'), 'utf8'));
      if (manifest.name && location.includes('node_modules')) { packages.set(location, { path: relative(root, location).replaceAll('\\', '/'), name: manifest.name, version: manifest.version, license: manifest.license ?? null }); break; }
    } catch {}
    location = dirname(location);
  }
}
await mkdir(dist, { recursive: true });
await copyFile(join(root, 'diagram-renderer.html'), join(dist, 'diagram-renderer.html'));
await copyFile(join(root, 'src/diagrams/renderer.css'), join(dist, 'diagram-renderer.css'));
await writeFile(join(dist, 'diagram-build-graph.json'), JSON.stringify({ vite: '8.3.1', input: mermaidManifest.exports['.'].import, mermaidVersion: mermaidManifest.version, lodashVersion: lodashManifest.version, loaderPolicy: loaderPolicy.metadata, graph, output, includedPackages: [...packages.values()], bundleBytes: bundle.length, bundleSha256: createHash('sha256').update(bundle).digest('hex') }, null, 2));
console.info(`[diagrams] core→IIFE ${bundle.length} bytes; ${graph.length} resolved modules; ${included.length} output module IDs; ${packages.size} packages; lodash-es ${lodashManifest.version}; elkjs excluded`);

import { parseSync } from 'vite';
import { readFile } from 'node:fs/promises';
import { createHash } from 'node:crypto';
import { join } from 'node:path';

// Add a diagram/layout here, then review the installed source/version/import pins
// below, independently run the real renderer, and update the included-code notices.
// Detector aliases may share one target; this table controls loader code, while the
// application's policy.ts still owns the accepted document syntax.
const allowedLoaders = Object.freeze({
  diagram: Object.freeze({
    flowchart: './chunks/mermaid.core/flowDiagram-KWPJA3E3.mjs',
    sequence: './chunks/mermaid.core/sequenceDiagram-PO4LG4MO.mjs',
    state: './chunks/mermaid.core/stateDiagram-v2-GCMORJYK.mjs',
  }),
  layout: Object.freeze({
    dagre: './dagre-6A5THRUB.mjs',
  }),
});

const sourceContracts = {
  diagram: {
    file: 'node_modules/mermaid/dist/mermaid.core.mjs',
    sha256: '07703cb6ec75ac4a0d8b7224676a5a695977981cad8a1c9a82d785377c6ebe64',
    staticImports: [
      './chunks/mermaid.core/chunk-6AEJRKK7.mjs',
      './chunks/mermaid.core/chunk-LNGE3PJU.mjs',
      './chunks/mermaid.core/chunk-GNY47TPC.mjs',
      './chunks/mermaid.core/chunk-DUW6YSOI.mjs',
      './chunks/mermaid.core/chunk-UA2S7LBM.mjs',
      './chunks/mermaid.core/chunk-Z7XXMR3K.mjs',
      './chunks/mermaid.core/chunk-5DYCD2WN.mjs',
      './chunks/mermaid.core/chunk-7INBJB4K.mjs',
      './chunks/mermaid.core/chunk-7PRAP22T.mjs',
      './chunks/mermaid.core/chunk-MBY4JIJT.mjs',
      './chunks/mermaid.core/chunk-742MDFTN.mjs',
      './chunks/mermaid.core/chunk-J5ZVWO5B.mjs',
      './chunks/mermaid.core/chunk-ZIGJFQKS.mjs',
      './chunks/mermaid.core/chunk-O7XYJQB3.mjs',
      './chunks/mermaid.core/chunk-X3CZISLH.mjs',
      './chunks/mermaid.core/chunk-Y2CYZVJY.mjs',
      'ts-dedent', 'd3', 'stylis', 'dompurify', 'es-toolkit/compat',
    ],
    dynamicImports: [
      './chunks/mermaid.core/c4Diagram-YGBWAQC7.mjs',
      './chunks/mermaid.core/flowDiagram-KWPJA3E3.mjs',
      './chunks/mermaid.core/diagram-22UHCM2B.mjs',
      './chunks/mermaid.core/swimlanesDiagram-TC7HE7FX.mjs',
      './chunks/mermaid.core/erDiagram-OPXOYQCR.mjs',
      './chunks/mermaid.core/gitGraphDiagram-X574FWY7.mjs',
      './chunks/mermaid.core/ganttDiagram-FUAMR5RP.mjs',
      './chunks/mermaid.core/infoDiagram-VRGFBTTK.mjs',
      './chunks/mermaid.core/pieDiagram-5QR66LMP.mjs',
      './chunks/mermaid.core/quadrantDiagram-O4NWA36T.mjs',
      './chunks/mermaid.core/xychartDiagram-PMCCYNJV.mjs',
      './chunks/mermaid.core/requirementDiagram-PLB6GJNP.mjs',
      './chunks/mermaid.core/sequenceDiagram-PO4LG4MO.mjs',
      './chunks/mermaid.core/classDiagram-v2-NBCMYWYE.mjs',
      './chunks/mermaid.core/stateDiagram-v2-GCMORJYK.mjs',
      './chunks/mermaid.core/journeyDiagram-ZHPQQLJL.mjs',
      './chunks/mermaid.core/flowDiagram-KWPJA3E3.mjs',
      './chunks/mermaid.core/timeline-definition-EJHVYXUP.mjs',
      './chunks/mermaid.core/mindmap-definition-NLK3R4M7.mjs',
      './chunks/mermaid.core/kanban-definition-PNTS6WVX.mjs',
      './chunks/mermaid.core/sankeyDiagram-IPEJSGJF.mjs',
      './chunks/mermaid.core/diagram-MLGK6HIB.mjs',
      './chunks/mermaid.core/diagram-MPIPVDR6.mjs',
      './chunks/mermaid.core/blockDiagram-BEXU5L5S.mjs',
      './chunks/mermaid.core/diagram-CDSNMT55.mjs',
      './chunks/mermaid.core/architectureDiagram-NJMV4G6O.mjs',
      './chunks/mermaid.core/diagram-ATOU4E4O.mjs',
      './chunks/mermaid.core/ishikawaDiagram-OU5B5YK6.mjs',
      './chunks/mermaid.core/vennDiagram-UO4OBE2U.mjs',
      './chunks/mermaid.core/diagram-3UASUU5V.mjs',
      './chunks/mermaid.core/usecaseDiagram-POWQR4AR.mjs',
      './chunks/mermaid.core/wardleyDiagram-VNRHLVJA.mjs',
      './chunks/mermaid.core/cynefinDiagram-VND7K2PF.mjs',
      './chunks/mermaid.core/railroadDiagram-XR7U4H2S.mjs',
      './chunks/mermaid.core/ebnfDiagram-ZINNZB2B.mjs',
      './chunks/mermaid.core/abnfDiagram-O67JEVCF.mjs',
      './chunks/mermaid.core/pegDiagram-GJSIUBJH.mjs',
    ],
  },
  layout: {
    file: 'node_modules/mermaid/dist/chunks/mermaid.core/chunk-GNY47TPC.mjs',
    sha256: '624eeefcd2d27b96c0f75db3f8edd1ff4997b5bdeb9e6404433219c6bfbe4804',
    staticImports: [
      './chunk-DUW6YSOI.mjs', './chunk-UA2S7LBM.mjs', './chunk-Z7XXMR3K.mjs',
      './chunk-5DYCD2WN.mjs', './chunk-7INBJB4K.mjs', './chunk-ZIGJFQKS.mjs',
      './chunk-O7XYJQB3.mjs', './chunk-X3CZISLH.mjs', './chunk-Y2CYZVJY.mjs',
    ],
    dynamicImports: [
      './elk-276RUBZZ.mjs', './dagre-6A5THRUB.mjs',
      './swimlanes-2SLR337P.mjs', './cose-bilkent-JH36ORCC.mjs',
    ],
  },
};

const manifestContracts = [
  {
    file: 'node_modules/mermaid/package.json', version: '12.0.0',
    sha256: '60e90be250dacb4339bba40939d959b06983344ce40210a0a29ae39874fa7b38',
  },
  {
    file: 'node_modules/lodash-es/package.json', version: '4.18.1',
    sha256: '83d054ea84cf0efe4db931cf24c4ca9dde533a0e49779d26176be9d43061c903',
  },
];
const virtualPrefix = '\0management-unsupported-loader:';
const sha256 = bytes => createHash('sha256').update(bytes).digest('hex');
const normalizePath = path => path.replaceAll('\\', '/');

function policyError(file, detail) {
  return new Error(`Mermaid loader policy: ${file}: ${detail}. Review this installed source and `
    + 'scripts/diagram-loader-policy.mjs allowedLoaders/sourceContracts/manifestContracts; '
    + 'do not update pins without reviewing loader imports and independently running the renderer.');
}

function inspectImports(file, source) {
  const parsed = parseSync(file, source, { sourceType: 'module' });
  if (parsed.errors.length) throw policyError(file, 'source cannot be parsed');
  const imports = { staticImports: [], dynamicImports: [] };
  function visit(node) {
    if (!node || typeof node !== 'object') return;
    if (node.type === 'ImportDeclaration' || node.type === 'ImportExpression') {
      if (typeof node.source?.value !== 'string') throw policyError(file, 'nonliteral import');
      const kind = node.type === 'ImportDeclaration' ? 'staticImports' : 'dynamicImports';
      imports[kind].push(node.source.value);
    }
    for (const child of Object.values(node)) {
      if (Array.isArray(child)) child.forEach(visit);
      else visit(child);
    }
  }
  visit(parsed.program);
  return imports;
}

export async function createDiagramLoaderPolicy(root) {
  const manifests = [];
  for (const contract of manifestContracts) {
    const bytes = await readFile(join(root, contract.file));
    const manifest = JSON.parse(bytes.toString('utf8'));
    if (manifest.version !== contract.version) throw policyError(contract.file, `expected version ${contract.version}`);
    if (sha256(bytes) !== contract.sha256) throw policyError(contract.file, 'whole manifest hash changed');
    manifests.push(manifest);
  }
  if (manifests[0].exports?.['.']?.import !== './dist/mermaid.core.mjs') {
    throw policyError(manifestContracts[0].file, 'ESM core export changed');
  }

  const sources = new Map();
  const sourcePins = [];
  for (const [kind, contract] of Object.entries(sourceContracts)) {
    const bytes = await readFile(join(root, contract.file));
    const source = bytes.toString('utf8');
    const imports = inspectImports(contract.file, source);
    const problems = [];
    if (sha256(bytes) !== contract.sha256) problems.push('whole source hash changed');
    for (const importKind of ['staticImports', 'dynamicImports']) {
      // Compare multiplicity as well as targets: two flow detectors share a loader.
      if (JSON.stringify(imports[importKind].sort()) !== JSON.stringify([...contract[importKind]].sort())) {
        problems.push(`${importKind} changed (${JSON.stringify(imports[importKind])})`);
      }
    }
    if (problems.length) throw policyError(contract.file, problems.join('; '));
    for (const name of Object.keys(allowedLoaders[kind])) {
      if (!Object.hasOwn(allowedLoaders[kind], name)) continue;
      if (!imports.dynamicImports.includes(allowedLoaders[kind][name])) {
        throw policyError(contract.file, `allowed ${kind} loader ${name} has no matching import`);
      }
    }
    sources.set(normalizePath(join(root, contract.file)), { kind, source, contract });
    sourcePins.push({ kind, file: contract.file, bytes: bytes.length, sha256: sha256(bytes), ...imports });
  }

  const blocked = new Map();
  const allowedEdges = new Map();
  const metadata = {
    allowedLoaders,
    manifests: manifestContracts,
    sources: sourcePins,
    allowedEdges: [],
    blockedEdges: [],
  };
  return {
    metadata,
    plugin: {
      name: 'management-diagram-loader-policy',
      enforce: 'pre',
      resolveId(target, importer) {
        const source = importer && sources.get(normalizePath(importer));
        if (!source || !source.contract.dynamicImports.includes(target)) return null;
        const table = allowedLoaders[source.kind];
        const allowed = Object.keys(table).some(name => Object.hasOwn(table, name) && table[name] === target);
        const edge = { importer: source.contract.file, target, kind: source.kind };
        if (allowed) {
          allowedEdges.set(`${importer}:${target}`, edge);
          metadata.allowedEdges = [...allowedEdges.values()];
          return null;
        }
        const id = virtualPrefix + source.kind + ':' + target;
        blocked.set(id, edge);
        metadata.blockedEdges = [...blocked.values()];
        return id;
      },
      load(id) {
        const source = sources.get(normalizePath(id));
        if (source) return source.source;
        const edge = blocked.get(id);
        if (!edge) return null;
        // The dynamic module throws on evaluation, preserving an explicit rejected
        // loader call even when Rolldown folds all chunks into the classic IIFE.
        const message = `Unsupported Mermaid ${edge.kind} loader: ${edge.target}; `
          + `allowed loaders: ${Object.keys(allowedLoaders[edge.kind]).join(', ')}`;
        return `throw new Error(${JSON.stringify(message)});`;
      },
    },
  };
}

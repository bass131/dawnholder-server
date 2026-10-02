// @vitest-environment node
// V3-R1 (verifier-owned): code review follow-ups on the MCP build, checked only in owned TEMP 05
// copies (tests/mcp-v3/temp-copy: node_modules is a read-only junction, cleanup unlinks it first).
// - R05: the build digest follows the compiler's real source graph. New shared modules (runtime,
//   transitive, type-only) count without editing any list; files outside the graph and catalog
//   data do not; the same sources at another path give the same digest. The build clears only
//   mcp-dist, keeps mcp-dist/mcp/main.js where package.json starts it, and refuses links or any
//   other output target without deleting anything.
// - R01/R04: the type links added by the follow-ups really make the compiler reject a tool-table
//   or DTO mismatch, and the shared record-type list reaches the MCP input schema.
import { existsSync, mkdirSync, readFileSync, readdirSync, renameSync, rmdirSync, symlinkSync, unlinkSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { afterAll, beforeAll, describe, expect, it } from 'vitest';
import { FRONTEND, builtVersion, createTempCopy, type TempCopy } from './mcp-v3/temp-copy';

const measure = (label: string, value: unknown) => console.info(`[V3-R1-MEASURE] ${label} ${JSON.stringify(value)}`);
const listing = (directory: string): string[] => readdirSync(directory, { withFileTypes: true })
  .flatMap(entry => entry.isDirectory() ? listing(join(directory, entry.name)).map(name => `${entry.name}/${name}`) : [entry.name]);

// Applies edits (path -> new text, or null to delete) inside the copy, runs fn, then restores bytes.
function withEdits<T>(copy: TempCopy, edits: Record<string, string | null>, fn: () => T): T {
  const saved = new Map<string, Buffer | null>();
  for (const [relative, text] of Object.entries(edits)) {
    const path = join(copy.frontend, relative);
    saved.set(path, existsSync(path) ? readFileSync(path) : null);
    if (text === null) unlinkSync(path); else writeFileSync(path, text);
  }
  try {
    return fn();
  } finally {
    for (const [path, bytes] of saved) {
      if (bytes) writeFileSync(path, bytes); else if (existsSync(path)) unlinkSync(path);
    }
  }
}
const appendTo = (copy: TempCopy, relative: string, text: string) => readFileSync(join(copy.frontend, relative), 'utf8') + text;

describe.runIf(process.platform === 'win32')('R05: build digest and output cleanup (owned TEMP copies)', () => {
  let copy: TempCopy;
  let base: string;
  beforeAll(() => {
    copy = createTempCopy('r1-build');
    base = copy.buildMcp();
  }, 180_000);
  afterAll(() => {
    if (copy) measure('cleanup', copy.remove());
    expect(readFileSync(join(FRONTEND, 'node_modules', 'typescript', 'package.json'), 'utf8')).toContain('"typescript"');
  }, 180_000);

  it('a new shared module and the one it imports enter the digest without any list edit; an unimported file does not', () => {
    const wire = appendTo(copy, 'mcp/catalog-dto.ts', "\nimport { probeA } from '../electron/r1-probe-a.js';\nexport const r1Probe = probeA;\n");
    const a = "import { probeB } from './r1-probe-b.js';\nexport const probeA = (): string => probeB();\n";
    const versions = withEdits(copy, { 'mcp/catalog-dto.ts': wire, 'electron/r1-probe-a.ts': a, 'electron/r1-probe-b.ts': "export const probeB = (): string => 'b1';\n" }, () => {
      const connected = copy.buildMcp();
      const built = listing(join(copy.frontend, 'mcp-dist'));
      const transitive = withEdits(copy, { 'electron/r1-probe-b.ts': "export const probeB = (): string => 'b2';\n" }, () => copy.buildMcp());
      const direct = withEdits(copy, { 'electron/r1-probe-a.ts': `${a}// edited\n` }, () => copy.buildMcp());
      const unrelated = withEdits(copy, { 'electron/r1-unimported.ts': "export const unused = 1;\n" }, () => copy.buildMcp());
      return { connected, transitive, direct, unrelated, built };
    });
    measure('r05-new-shared-modules', { base, ...versions });
    expect(versions.built).toEqual(expect.arrayContaining(['electron/r1-probe-a.js', 'electron/r1-probe-b.js']));
    expect(new Set([base, versions.connected, versions.transitive, versions.direct]).size).toBe(4);
    expect(versions.unrelated).toBe(versions.connected);
    // Removing the probe restores the base digest and the build drops the probe output (stale cleanup).
    expect(copy.buildMcp()).toBe(base);
    expect(listing(join(copy.frontend, 'mcp-dist')).filter(name => name.includes('r1-'))).toEqual([]);
  }, 120_000);

  it('a type-only import enters the digest', () => {
    const wire = appendTo(copy, 'mcp/catalog-dto.ts', "\nimport type { R1Shape } from '../electron/r1-types.js';\nexport type R1Alias = R1Shape;\n");
    const versions = withEdits(copy, { 'mcp/catalog-dto.ts': wire, 'electron/r1-types.ts': 'export interface R1Shape { a: string }\n' }, () => {
      const connected = copy.buildMcp();
      const edited = withEdits(copy, { 'electron/r1-types.ts': 'export interface R1Shape { a: string; b?: number }\n' }, () => copy.buildMcp());
      return { connected, edited };
    });
    measure('r05-type-only', { base, ...versions });
    expect(new Set([base, versions.connected, versions.edited]).size).toBe(3);
    expect(copy.buildMcp()).toBe(base);
  }, 120_000);

  it('catalog data and a UI-only source are not inputs (the extra tsconfig inputs are measured)', () => {
    const catalog = readFileSync(copy.catalog);
    writeFileSync(copy.catalog, Buffer.concat([catalog, Buffer.from('\n')]));
    try { expect(copy.buildMcp()).toBe(base); } finally { writeFileSync(copy.catalog, catalog); }
    expect(withEdits(copy, { 'src/App.tsx': appendTo(copy, 'src/App.tsx', '\n// v3-r1 ui-only edit\n') }, () => copy.buildMcp())).toBe(base);
    // Not part of the MCP compile, yet hashed by the build script: recorded, not asserted.
    const extra = Object.fromEntries(['tsconfig.json', 'tsconfig.electron.json'].map(file => [file,
      withEdits(copy, { [file]: appendTo(copy, file, '\n') }, () => copy.buildMcp()) !== base]));
    measure('r05-extra-config-inputs-change-digest', extra);
    expect(copy.buildMcp()).toBe(base);
  }, 120_000);

  it('the same sources under a root with Korean and a space in its name give the same digest as the canonical build', () => {
    const other = createTempCopy('경로 공백');
    try {
      const version = other.buildMcp();
      measure('r05-path-independence', { root: other.root, version });
      expect(version).toBe(base);
      expect(version).toBe(builtVersion(FRONTEND));
    } finally {
      measure('cleanup-other', other.remove());
    }
  }, 180_000);

  it('the build clears stale output only inside mcp-dist and keeps the started entry where package.json expects it', () => {
    mkdirSync(join(copy.frontend, 'mcp-dist', 'r1-stale-dir'), { recursive: true });
    writeFileSync(join(copy.frontend, 'mcp-dist', 'mcp', 'r1-stale.js'), 'export {};\n');
    writeFileSync(join(copy.frontend, 'mcp-dist', 'r1-stale-dir', 'x.js'), 'export {};\n');
    writeFileSync(join(copy.frontend, 'r1-sibling.txt'), 'keep');
    expect(copy.buildMcp()).toBe(base);
    const built = listing(join(copy.frontend, 'mcp-dist'));
    expect(built.filter(name => name.includes('r1-'))).toEqual([]);
    expect(built).toContain('mcp/main.js');
    expect((JSON.parse(readFileSync(join(copy.frontend, 'package.json'), 'utf8')) as { scripts: Record<string, string> }).scripts['mcp:start']).toBe('node mcp-dist/mcp/main.js');
    expect(readFileSync(join(copy.frontend, 'r1-sibling.txt'), 'utf8')).toBe('keep');
    unlinkSync(join(copy.frontend, 'r1-sibling.txt'));
  }, 120_000);

  it('the build refuses a junction as mcp-dist, a junction inside it and another outDir, deleting nothing', () => {
    const sentinel = join(copy.root, 'r1-sentinel');
    mkdirSync(sentinel);
    writeFileSync(join(sentinel, 'keep.txt'), 'keep');
    const output = join(copy.frontend, 'mcp-dist');
    const before = listing(output);
    const refusals: Record<string, string> = {};

    // 1. mcp-dist itself is a junction to the sentinel.
    const moved = join(copy.root, 'r1-real-mcp-dist');
    renameSync(output, moved);
    symlinkSync(sentinel, output, 'junction');
    try {
      const result = copy.run(['scripts/build-mcp.mjs']);
      expect(result.status).not.toBe(0);
      refusals.junctionOutput = result.output.trim().split(/\r?\n/).find(line => /refuses|must be/.test(line)) ?? result.output.slice(0, 200);
    } finally {
      rmdirSync(output);
      renameSync(moved, output);
    }
    expect(readFileSync(join(sentinel, 'keep.txt'), 'utf8')).toBe('keep');

    // 2. A junction inside mcp-dist.
    const inner = join(output, 'mcp', 'r1-link');
    symlinkSync(sentinel, inner, 'junction');
    try {
      const result = copy.run(['scripts/build-mcp.mjs']);
      expect(result.status).not.toBe(0);
      refusals.innerJunction = result.output.trim().split(/\r?\n/).find(line => /refuses|must be/.test(line)) ?? result.output.slice(0, 200);
      expect(listing(output).filter(name => !name.startsWith('mcp/r1-link'))).toEqual(before);
    } finally {
      rmdirSync(inner);
    }
    expect(readFileSync(join(sentinel, 'keep.txt'), 'utf8')).toBe('keep');

    // 3. tsconfig.mcp.json points the output elsewhere.
    mkdirSync(join(copy.frontend, 'r1-other-out'));
    writeFileSync(join(copy.frontend, 'r1-other-out', 'keep.txt'), 'keep');
    const config = readFileSync(join(copy.frontend, 'tsconfig.mcp.json'), 'utf8');
    withEdits(copy, { 'tsconfig.mcp.json': config.replace('"outDir": "mcp-dist"', '"outDir": "r1-other-out"') }, () => {
      const result = copy.run(['scripts/build-mcp.mjs']);
      expect(result.status).not.toBe(0);
      refusals.otherOutDir = result.output.trim().split(/\r?\n/).find(line => /refuses|must be/.test(line)) ?? result.output.slice(0, 200);
    });
    expect(readFileSync(join(copy.frontend, 'r1-other-out', 'keep.txt'), 'utf8')).toBe('keep');
    expect(listing(output)).toEqual(before);
    measure('r05-cleanup-refusals', refusals);
    unlinkSync(join(copy.frontend, 'r1-other-out', 'keep.txt'));
    rmdirSync(join(copy.frontend, 'r1-other-out'));
    unlinkSync(join(sentinel, 'keep.txt'));
    rmdirSync(sentinel);
    expect(copy.buildMcp()).toBe(base);
  }, 120_000);
});

describe.runIf(process.platform === 'win32')('R01/R04: compiler-enforced links (owned TEMP copy, tsc --noEmit)', () => {
  let copy: TempCopy;
  beforeAll(() => { copy = createTempCopy('r1-types'); }, 180_000);
  afterAll(() => { if (copy) measure('cleanup', copy.remove()); }, 180_000);
  const typecheck = () => copy.run(['node_modules/typescript/bin/tsc', '--project', 'tsconfig.mcp.json', '--noEmit']);
  const errors = (output: string) => output.split(/\r?\n/).filter(line => /error TS\d+/.test(line)).map(line => line.slice(0, 220));

  it('the unmodified copy typechecks', () => {
    expect(typecheck().status).toBe(0);
  }, 60_000);

  it('R01: outputSchemas must name exactly the input tool names (a missing or an extra tool fails to compile)', () => {
    const schemas = readFileSync(join(copy.frontend, 'mcp', 'catalog-schemas.ts'), 'utf8');
    const getSource = /\n {2}get_source: output\([^\n]*\n/.exec(schemas)?.[0];
    expect(getSource).toBeTruthy();
    const missing = withEdits(copy, { 'mcp/catalog-schemas.ts': schemas.replace(getSource as string, '\n') }, typecheck);
    const extra = withEdits(copy, { 'mcp/catalog-schemas.ts': schemas.replace(getSource as string, `${getSource as string}  extra_tool: output(z.strictObject({})),\n`) }, typecheck);
    // Registration in catalog-server stays explicit: a tool added to both schema tables but not
    // registered still compiles (residual of the minimal R01 option, measured only).
    const inputLine = 'get_system: detailInput, get_record: detailInput, get_source: detailInput,';
    expect(schemas).toContain(inputLine);
    const unregistered = withEdits(copy, { 'mcp/catalog-schemas.ts': schemas
      .replace(inputLine, `${inputLine} extra_tool: detailInput,`)
      .replace(getSource as string, `${getSource as string}  extra_tool: output(z.strictObject({})),\n`) }, typecheck);
    measure('r01-output-schema-links', { missing: errors(missing.output), extra: errors(extra.output), unregisteredBothTables: unregistered.status });
    // The satisfies clause itself rejects both, not only a later use in catalog-server.
    expect(missing.status).not.toBe(0);
    expect(missing.output).toMatch(/catalog-schemas\.ts\(\d+,\d+\): error TS2741: Property 'get_source' is missing/);
    expect(extra.status).not.toBe(0);
    expect(extra.output).toMatch(/catalog-schemas\.ts\(\d+,\d+\): error TS2353: [^\n]*'extra_tool'/);
  }, 60_000);

  it('R04: a contract field the zod detail schema lacks fails to compile; an extra zod-only field is measured', () => {
    const contract = readFileSync(join(copy.frontend, 'electron', 'catalog-contract.ts'), 'utf8');
    const schemas = readFileSync(join(copy.frontend, 'mcp', 'catalog-schemas.ts'), 'utf8');
    expect(contract).toContain('  id: string; title: string; area: string; summary: string; responsibility: string;');
    const contractOnly = withEdits(copy, { 'electron/catalog-contract.ts': contract.replace('  id: string; title: string; area: string; summary: string; responsibility: string;', '  id: string; title: string; area: string; summary: string; responsibility: string; r1Field: string;') }, typecheck);
    expect(schemas).toContain('id: text, title: text, area: text, summary: text, responsibility: text, behavior: strings,');
    const zodOnly = withEdits(copy, { 'mcp/catalog-schemas.ts': schemas.replace('id: text, title: text, area: text, summary: text, responsibility: text, behavior: strings,', 'id: text, title: text, area: text, summary: text, responsibility: text, behavior: strings, r1Field: text,') }, typecheck);
    measure('r04-dto-links', { contractOnly: { status: contractOnly.status, error: errors(contractOnly.output) }, zodOnly: { status: zodOnly.status, error: errors(zodOnly.output) } });
    expect(contractOnly.status).not.toBe(0);
    // The zod schema's own `satisfies z.ZodType<SystemRecord>` rejects it (TS1360), besides the DTO builder.
    expect(contractOnly.output).toMatch(/catalog-schemas\.ts\(\d+,\d+\): error TS1360: /);
  }, 60_000);

  it('R04: a record type added to the shared contract list reaches the built MCP input enum', () => {
    const contract = readFileSync(join(copy.frontend, 'electron', 'catalog-contract.ts'), 'utf8');
    const list = "export const RECORD_TYPES = ['변경', '결정', '검증', '계획'] as const;";
    expect(contract).toContain(list);
    const read = () => {
      copy.buildMcp();
      const result = copy.run(['--input-type=module', '-e', "const { inputSchemas } = await import('./mcp-dist/mcp/catalog-schemas.js'); console.log(JSON.stringify(inputSchemas.search_records.shape.type.unwrap().options));"]);
      expect(result.status, result.output).toBe(0);
      return JSON.parse(result.output.trim().split(/\r?\n/).at(-1) as string) as string[];
    };
    const extended = withEdits(copy, { 'electron/catalog-contract.ts': contract.replace(list, "export const RECORD_TYPES = ['변경', '결정', '검증', '계획', '보류'] as const;") }, read);
    const restored = read();
    measure('r04-enum-shared', { extended, restored });
    expect(extended).toEqual(['변경', '결정', '검증', '계획', '보류']);
    expect(restored).toEqual(['변경', '결정', '검증', '계획']);
  }, 120_000);
});

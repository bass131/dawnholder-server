// V3: an owned TEMP copy of 05_Management/{frontend sources, records} that keeps the original
// relative layout (frontend/<entry> -> ../../records/catalog.json). Builds and source edits for
// path/digest checks happen only here; the canonical tree is read, never written.
// node_modules is a directory junction to the canonical install (read-only use). Cleanup runs the
// guarded native PowerShell script, which unlinks the junction before deleting the owned root.
import { spawnSync } from 'node:child_process';
import { createHash, randomBytes } from 'node:crypto';
import { cpSync, existsSync, mkdirSync, readFileSync, symlinkSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';

export const FRONTEND = fileURLToPath(new URL('../../', import.meta.url));
export const CANONICAL_CATALOG = fileURLToPath(new URL('../../../records/catalog.json', import.meta.url));
const CLEANUP_SCRIPT = fileURLToPath(new URL('./remove-owned-temp.ps1', import.meta.url));
const SOURCES = [
  'electron', 'mcp', 'scripts', 'src', 'index.html', 'package.json', 'package-lock.json',
  'tsconfig.json', 'tsconfig.electron.json', 'tsconfig.mcp.json', 'vite.config.ts',
];

export const sha256 = (bytes: string | Uint8Array) => createHash('sha256').update(bytes).digest('hex');

export interface TempCopy {
  root: string;
  frontend: string;
  catalog: string;
  backup: string;
  run(args: string[]): { status: number | null; output: string };
  buildMcp(): string;
  buildDesktopMain(): void;
  remove(): string;
}

export function createTempCopy(label: string): TempCopy {
  const token = `v3-${label}-${randomBytes(6).toString('hex')}`;
  const root = join(tmpdir(), `dawnholder-${token}`);
  mkdirSync(root); // Fails if the path already exists: the root is ours alone.
  writeFileSync(join(root, '.v3-owner.json'), JSON.stringify({
    owner: 'M-1 V3 verifier test', task: token, createdAt: new Date().toISOString(), pid: process.pid,
    purpose: 'TEMP 05 copy for build path/digest regression; canonical tree is read only',
  }));
  const frontend = join(root, '05_Management', 'frontend');
  for (const entry of SOURCES) cpSync(join(FRONTEND, entry), join(frontend, entry), { recursive: true, errorOnExist: true, force: false });
  const catalog = join(root, '05_Management', 'records', 'catalog.json');
  mkdirSync(join(root, '05_Management', 'records'));
  writeFileSync(catalog, readFileSync(CANONICAL_CATALOG), { flag: 'wx' });
  const junction = join(frontend, 'node_modules');
  symlinkSync(join(FRONTEND, 'node_modules'), junction, 'junction');

  function run(args: string[]) {
    const result = spawnSync(process.execPath, args, { cwd: frontend, encoding: 'utf8', timeout: 120_000, windowsHide: true });
    return { status: result.status, output: `${result.stdout ?? ''}${result.stderr ?? ''}` };
  }
  return {
    root, frontend, catalog,
    backup: join(root, '05_Management', '.verification', 'system-records-last-good.json'),
    run,
    buildMcp() {
      const result = run(['scripts/build-mcp.mjs']);
      if (result.status !== 0) throw new Error(`mcp build failed: ${result.output}`);
      return builtVersion(frontend);
    },
    buildDesktopMain() {
      const result = run(['node_modules/typescript/bin/tsc', '--project', 'tsconfig.electron.json']);
      if (result.status !== 0) throw new Error(`desktop main build failed: ${result.output}`);
    },
    remove() {
      const result = spawnSync('powershell.exe', [
        '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', CLEANUP_SCRIPT,
        '-Root', root, '-Task', token, '-Junction', junction,
      ], { encoding: 'utf8', windowsHide: true, timeout: 120_000 });
      const output = `${result.stdout ?? ''}${result.stderr ?? ''}`;
      if (result.status !== 0 || existsSync(root)) throw new Error(`cleanup failed: ${output}`);
      return output.trim();
    },
  };
}

export function builtVersion(frontend: string): string {
  const source = readFileSync(join(frontend, 'mcp-dist', 'mcp', 'build-info.js'), 'utf8');
  const match = /BUILD_VERSION = "([^"]+)"/.exec(source);
  if (!match?.[1]) throw new Error('build-info.js has no BUILD_VERSION');
  return match[1];
}

// Resolve every `new URL('<relative>', import.meta.url)` literal of a built module to a path.
export function moduleRelativePaths(file: string): Record<string, string> {
  const source = readFileSync(file, 'utf8');
  return Object.fromEntries([...source.matchAll(/new URL\('([^']+)', import\.meta\.url\)/g)]
    .map(match => [match[1] ?? '', fileURLToPath(new URL(match[1] ?? '', pathToFileURL(file)))]));
}

// Relative import closure of an ES module graph, starting at `entry` (built .js or source .ts).
export function relativeImportClosure(entry: string, toFile: (specifier: string, from: string) => string): { files: string[]; bare: string[] } {
  const seen = new Set<string>();
  const bare = new Set<string>();
  const pending = [entry];
  while (pending.length) {
    const file = pending.pop()!;
    if (seen.has(file)) continue;
    seen.add(file);
    const source = readFileSync(file, 'utf8');
    for (const match of source.matchAll(/(?:^|\n)\s*(?:import|export)\b[^'"]*?from\s*['"]([^'"]+)['"]|\bimport\s*\(\s*['"]([^'"]+)['"]/g)) {
      const specifier = match[1] ?? match[2] ?? '';
      if (specifier.startsWith('.')) pending.push(toFile(specifier, file));
      else bare.add(specifier);
    }
  }
  return { files: [...seen].sort(), bare: [...bare].sort() };
}

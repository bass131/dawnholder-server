import { createHash } from 'node:crypto';
import { lstat, readFile, readdir, realpath, rm, writeFile } from 'node:fs/promises';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { dirname, isAbsolute, join, relative, resolve, sep } from 'node:path';

const root = fileURLToPath(new URL('../', import.meta.url));
const compiler = fileURLToPath(new URL('../node_modules/typescript/bin/tsc', import.meta.url));
const configPath = join(root, 'tsconfig.mcp.json');
const config = JSON.parse(await readFile(configPath, 'utf8'));
const realRoot = await realpath(root);

function localPath(file) {
  const name = relative(realRoot, file);
  if (!name || isAbsolute(name) || name === '..' || name.startsWith(`..${sep}`) || name.split(sep).includes('node_modules')) {
    throw new Error('MCP build input must stay inside frontend sources.');
  }
  return name.split(sep).join('/');
}

// Ask the installed compiler for this build's source graph, including transitive
// shared imports and type imports. TypeScript 7 exposes this through its CLI.
// Dependency declarations are excluded; package/lockfile identify dependencies.
async function buildSources() {
  const listed = spawnSync(process.execPath, [compiler, '--project', 'tsconfig.mcp.json', '--listFilesOnly'], {
    cwd: root, encoding: 'utf8', windowsHide: true,
  });
  if (listed.status !== 0 || listed.error) {
    process.stderr.write(listed.stderr ?? '');
    throw new Error('MCP compiler could not enumerate build inputs.');
  }
  const sources = [];
  for (const file of listed.stdout.split(/\r?\n/).filter(Boolean)) {
    if (file.split(/[\\/]/).includes('node_modules')) continue;
    sources.push(localPath(await realpath(file)));
  }
  return sources;
}

const files = [...new Set([
  ...await buildSources(),
  'scripts/build-mcp.mjs', 'tsconfig.mcp.json', 'tsconfig.electron.json', 'tsconfig.json', 'package.json', 'package-lock.json',
])].sort();
const digest = createHash('sha256');
for (const path of files) {
  const bytes = await readFile(join(root, path));
  digest.update(`${path}\0${bytes.length}\0`);
  digest.update(bytes);
}
const version = `0.0.0+sha256.${digest.digest('hex')}`;

async function assertNoLinks(directory) {
  for (const entry of await readdir(directory, { withFileTypes: true })) {
    const file = join(directory, entry.name);
    const stat = await lstat(file);
    if (stat.isSymbolicLink()) throw new Error('MCP output cleanup refuses links.');
    if (stat.isDirectory()) await assertNoLinks(file);
  }
}

// Delete only this frontend's expected output; never follow a junction/symlink.
const output = resolve(root, 'mcp-dist');
if (resolve(root, config.compilerOptions?.outDir ?? '') !== output || await realpath(dirname(output)) !== realRoot) {
  throw new Error('MCP output cleanup target must be frontend/mcp-dist.');
}
try {
  const stat = await lstat(output);
  if (!stat.isDirectory() || stat.isSymbolicLink() || await realpath(output) !== join(realRoot, 'mcp-dist')) {
    throw new Error('MCP output cleanup refuses an unexpected directory or link.');
  }
  await assertNoLinks(output);
} catch (error) {
  if (error?.code !== 'ENOENT') throw error;
}
await rm(output, { recursive: true, force: true });
const compile = spawnSync(process.execPath, [compiler, '--project', 'tsconfig.mcp.json'], { cwd: root, stdio: 'inherit', windowsHide: true });
if (compile.status !== 0 || compile.error) process.exit(compile.status ?? 1);
await writeFile(join(output, 'mcp/build-info.js'), `export const BUILD_VERSION = ${JSON.stringify(version)};\n`, 'utf8');
console.info(`Catalog MCP built: ${version}`);

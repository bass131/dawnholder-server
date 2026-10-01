import { createHash } from 'node:crypto';
import { readFile, readdir, writeFile } from 'node:fs/promises';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { join } from 'node:path';

const root = fileURLToPath(new URL('../', import.meta.url));
const compiler = fileURLToPath(new URL('../node_modules/typescript/bin/tsc', import.meta.url));
const compile = spawnSync(process.execPath, [compiler, '--project', 'tsconfig.mcp.json'], { cwd: root, stdio: 'inherit' });
if (compile.status !== 0 || compile.error) process.exit(compile.status ?? 1);

async function sources(directory) {
  const entries = await readdir(join(root, directory), { withFileTypes: true });
  const lists = await Promise.all(entries.map(entry => entry.isDirectory() ? sources(`${directory}/${entry.name}`) : entry.name.endsWith('.ts') ? [`${directory}/${entry.name}`] : []));
  return lists.flat();
}
const files = [
  ...await sources('mcp'),
  'electron/catalog-contract.ts', 'electron/catalog-query.ts', 'electron/catalog-hash.ts',
  'scripts/build-mcp.mjs', 'tsconfig.mcp.json', 'tsconfig.electron.json', 'tsconfig.json', 'package.json', 'package-lock.json',
].sort();
const digest = createHash('sha256');
for (const path of files) {
  const bytes = await readFile(join(root, path));
  digest.update(`${path}\0${bytes.length}\0`);
  digest.update(bytes);
}
const version = `0.0.0+sha256.${digest.digest('hex')}`;
await writeFile(join(root, 'mcp-dist/mcp/build-info.js'), `export const BUILD_VERSION = ${JSON.stringify(version)};\n`, 'utf8');
console.info(`Catalog MCP built: ${version}`);

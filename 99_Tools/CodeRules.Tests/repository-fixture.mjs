import { spawnSync } from 'node:child_process';
import { createHash } from 'node:crypto';
import { copyFile, cp, mkdir, mkdtemp, readFile, readdir, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { dirname, join, relative, sep } from 'node:path';
import { fileURLToPath } from 'node:url';

export const repositoryRoot = fileURLToPath(new URL('../../', import.meta.url));
const checkerDirectory = join(repositoryRoot, '99_Tools', 'CodeRules');
const checkerEntry = join(checkerDirectory, 'check-code-rules.mjs');
const managementFrontend = join(repositoryRoot, '05_Management', 'frontend');

export const sha256 = bytes => createHash('sha256').update(bytes).digest('hex');

// Tool locations are an environment contract, not test constants. CI supplies a job-local
// analyzer and a native python3. On Windows the WindowsApps python alias is not an interpreter,
// so a WSL distribution must be named explicitly.
export function toolEnvironment() {
  const tools = {
    pssaManifest: process.env.CODE_RULES_PSSA_MANIFEST || null,
    python: process.env.CODE_RULES_PYTHON || 'python3',
    wslDistribution: process.env.CODE_RULES_WSL_DISTRIBUTION || null,
  };
  const missing = [];
  if (!tools.pssaManifest) missing.push('CODE_RULES_PSSA_MANIFEST (approved PSScriptAnalyzer 1.25.0 manifest)');
  if (process.platform === 'win32' && !tools.wslDistribution) {
    missing.push('CODE_RULES_WSL_DISTRIBUTION (Windows needs WSL Python)');
  }
  return { tools, missing };
}

// One owned directory per test process: temporary repositories may be removed afterwards,
// while raw checker evidence stays under the results directory that CI uploads.
export async function createWorkspace() {
  const resultsParent = process.env.CODE_RULES_RESULTS || await mkdtemp(join(tmpdir(), 'code-rules-results-'));
  await mkdir(resultsParent, { recursive: true });
  const results = await mkdtemp(join(resultsParent, 'run-'));
  const workParent = process.env.CODE_RULES_TEST_WORK || tmpdir();
  await mkdir(workParent, { recursive: true });
  const work = await mkdtemp(join(workParent, 'w-'));
  let caseNumber = 0;
  return {
    results,
    work,
    nextCase(name) {
      caseNumber += 1;
      return `${String(caseNumber).padStart(2, '0')}-${name}`;
    },
    async dispose() {
      if (process.env.CODE_RULES_KEEP_WORK === '1') return;
      await rm(work, { recursive: true, force: true, maxRetries: 3 });
    },
  };
}

export class TemporaryRepository {
  static async create(workspace, name) {
    const root = join(workspace.work, name);
    await mkdir(root);
    const repository = new TemporaryRepository(root);
    repository.git('init', '-q');
    repository.git('config', 'user.name', 'CodeRules independent tests');
    repository.git('config', 'user.email', 'code-rules-tests@invalid');
    repository.git('config', 'core.autocrlf', 'false');
    repository.git('config', 'commit.gpgsign', 'false');
    repository.git('config', 'core.hooksPath', join(root, '.no-hooks'));
    await repository.copyChecker();
    return repository;
  }

  constructor(root) {
    this.root = root;
  }

  path(name) {
    return join(this.root, ...name.split('/'));
  }

  git(...args) {
    const result = spawnSync('git', args, { cwd: this.root, encoding: 'utf8' });
    if (result.status !== 0) {
      throw new Error(`git ${args.join(' ')} failed (${result.status}): ${result.stderr}${result.error ?? ''}`);
    }
    return result.stdout;
  }

  async write(name, content) {
    await mkdir(dirname(this.path(name)), { recursive: true });
    await writeFile(this.path(name), content);
  }

  async read(name) {
    return readFile(this.path(name), 'utf8');
  }

  async remove(name) {
    await rm(this.path(name), { recursive: true, force: true });
  }

  commit(message) {
    this.git('add', '-A');
    this.git('commit', '-q', '--allow-empty', '-m', message);
    return this.git('rev-parse', 'HEAD').trim();
  }

  // The checker reads its configuration from the inspected root, so the fixture carries
  // byte-identical copies of the product checker files and proves they were not altered.
  async copyChecker() {
    const destination = this.path('99_Tools/CodeRules');
    await mkdir(destination, { recursive: true });
    for (const entry of await readdir(checkerDirectory, { withFileTypes: true })) {
      if (!entry.isFile()) throw new Error(`Unexpected checker entry: ${entry.name}`);
      await copyFile(join(checkerDirectory, entry.name), join(destination, entry.name));
      const original = sha256(await readFile(join(checkerDirectory, entry.name)));
      const copied = sha256(await readFile(join(destination, entry.name)));
      if (original !== copied) throw new Error(`Checker copy differs: ${entry.name}`);
    }
  }

  // Minimal Management frontend that keeps the three real typecheck scripts and the exact
  // installed TypeScript package (including its native platform package) without npm install.
  async addTypeScriptFrontend() {
    const realPackage = JSON.parse(await readFile(join(managementFrontend, 'package.json'), 'utf8'));
    const realLock = JSON.parse(await readFile(join(managementFrontend, 'package-lock.json'), 'utf8'));
    const lockedVersion = realLock.packages?.['node_modules/typescript']?.version;
    const frontend = '05_Management/frontend';
    const scripts = {};
    for (const name of ['typecheck', 'desktop:typecheck', 'mcp:typecheck']) scripts[name] = realPackage.scripts[name];
    await this.write(`${frontend}/package.json`, `${JSON.stringify({
      name: 'code-rules-fixture',
      private: true,
      scripts,
      devDependencies: { typescript: realPackage.devDependencies.typescript },
    }, null, 2)}\n`);
    await this.write(`${frontend}/package-lock.json`, `${JSON.stringify({
      name: 'code-rules-fixture',
      lockfileVersion: 3,
      requires: true,
      packages: {
        '': { name: 'code-rules-fixture', devDependencies: { typescript: realPackage.devDependencies.typescript } },
        'node_modules/typescript': { version: lockedVersion, dev: true },
      },
    }, null, 2)}\n`);
    const compilerOptions = {
      strict: true,
      target: 'ES2022',
      module: 'ESNext',
      moduleResolution: 'Bundler',
      types: [],
    };
    const projects = {
      'tsconfig.json': { noEmit: true },
      'tsconfig.electron.json': { outDir: 'desktop-dist' },
      'tsconfig.mcp.json': { outDir: 'mcp-dist' },
    };
    for (const [name, options] of Object.entries(projects)) {
      const project = { compilerOptions: { ...compilerOptions, ...options }, include: ['src'] };
      await this.write(`${frontend}/${name}`, `${JSON.stringify(project, null, 2)}\n`);
    }
    await this.write('.gitignore', 'node_modules/\n');
    const modules = join(managementFrontend, 'node_modules');
    await cp(join(modules, 'typescript'), this.path(`${frontend}/node_modules/typescript`), { recursive: true });
    for (const entry of await readdir(join(modules, '@typescript'), { withFileTypes: true })) {
      if (!entry.isDirectory() || !entry.name.startsWith('typescript-')) continue;
      await cp(join(modules, '@typescript', entry.name),
        this.path(`${frontend}/node_modules/@typescript/${entry.name}`), { recursive: true });
    }
  }

  // Byte snapshot of the working tree (without Git metadata or installed packages) used to
  // prove that the checker neither fixes, formats nor executes the inspected files.
  async snapshot() {
    const files = new Map();
    const visit = async directory => {
      for (const entry of await readdir(directory, { withFileTypes: true })) {
        if (['.git', 'node_modules'].includes(entry.name)) continue;
        const absolute = join(directory, entry.name);
        const name = relative(this.root, absolute).split(sep).join('/');
        if (entry.isSymbolicLink()) {
          files.set(name, 'link');
        } else if (entry.isDirectory()) {
          await visit(absolute);
        } else {
          files.set(name, sha256(await readFile(absolute)));
        }
      }
    };
    await visit(this.root);
    return files;
  }
}

// Runs the product entry point exactly as a user would, with an explicit root and output.
// Every invocation writes its argv, exit and raw streams next to the checker's own results.
export async function runChecker(workspace, repository, caseName, options) {
  const { tools } = toolEnvironment();
  const selected = { ...tools, pwsh: null, ...options.tools };
  const evidence = join(workspace.results, caseName);
  await mkdir(evidence, { recursive: true });
  // A caller-provided output is not created here: link and boundary cases must observe
  // exactly what the checker itself creates or refuses.
  const output = options.output ?? evidence;
  const runs = async () => {
    try {
      return (await readdir(output)).filter(name => name.startsWith('run-'));
    } catch (error) {
      if (error.code === 'ENOENT') return [];
      throw error;
    }
  };
  const before = new Set(await runs());
  const args = [checkerEntry, '--root', options.root ?? repository.root, '--output', output];
  if (options.scope !== undefined) args.push('--scope', options.scope);
  if (options.base !== undefined) args.push(`--base=${options.base}`);
  if (selected.pssaManifest) args.push('--pssa-manifest', selected.pssaManifest);
  if (selected.pwsh) args.push('--pwsh', selected.pwsh);
  if (selected.python) args.push('--python', selected.python);
  if (selected.wslDistribution) args.push('--wsl-distribution', selected.wslDistribution);
  const result = spawnSync(process.execPath, args, {
    cwd: repository.root,
    env: { ...process.env, ...options.env },
    encoding: 'utf8',
    timeout: 900000,
    maxBuffer: 64 * 1024 * 1024,
  });
  const created = (await runs()).filter(name => !before.has(name));
  const invocation = {
    argv: [process.execPath, ...args],
    cwd: repository.root,
    extraEnvironment: options.env ?? {},
    exit: result.status,
    signal: result.signal,
    error: result.error?.message ?? null,
    stdout: result.stdout,
    stderr: result.stderr,
    createdRuns: created,
  };
  const invocationName = `invocation-${created[0] ?? `none-${Date.now()}`}.json`;
  await writeFile(join(evidence, invocationName), `${JSON.stringify(invocation, null, 2)}\n`);
  const runDirectory = created.length === 1 ? join(output, created[0]) : null;
  const report = runDirectory ? JSON.parse(await readFile(join(runDirectory, 'results.json'), 'utf8')) : null;
  const text = runDirectory ? await readFile(join(runDirectory, 'results.txt'), 'utf8') : null;
  return { ...invocation, output, runDirectory, report, text };
}

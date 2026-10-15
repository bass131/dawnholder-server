// Shared fixture for the execution lock regressions. It builds throwaway Git repositories and record
// folders under EXECUTION_LOCK_TESTS_WORK, starts harmless Node commands (fixture-child.mjs), runs
// the public runCli of 99_Tools/ExecutionLock/execution-lock.mjs in this process or in a separate
// process (runner-bootstrap.mjs), and supplies the injected observation of the explicit test seam.
// It holds no judgement of the lock: every expected status, exit and admission is written in
// execution-lock.test.mjs from the execution contract text.
//
// Injected observation (contract "시험 seam과 실제 진입"): observe() returns
// { observedAt, complete, memory, processes } where processes are the fixture's own processes that
// recorded themselves (the test process, runner bootstraps, fixture commands and bystanders) and
// are still alive. Memory, completeness and failures come from the scenario's machine state, never
// from a real Windows measurement. Fidelity limits, reported with the results:
// - a fixture command becomes visible once its first statement has written its record, tens of
//   milliseconds after spawn; a real OS lists it at once;
// - processes the product itself starts besides the requested command (for example a wrapper) are
//   not listed, because the fixture cannot see them without a real OS adapter;
// - liveness of a record without an end record is process.kill(pid, 0), so a PID reused by an
//   unrelated process within the same test would still be listed;
// - a reused PID is simulated by a synthetic entry for a PID no real process holds.
//
// Cleanup never terminates by PID: fixture commands stop themselves when the scenario's stop-all
// file appears or their hold limit passes. Work folders are kept as evidence.
import { spawn, spawnSync } from 'node:child_process';
import { createHash } from 'node:crypto';
import { existsSync, readdirSync, readFileSync, realpathSync, renameSync, writeFileSync } from 'node:fs';
import { mkdir, mkdtemp, readdir, readFile, stat, writeFile } from 'node:fs/promises';
import { isAbsolute, join, relative, resolve } from 'node:path';
import { performance } from 'node:perf_hooks';
import { fileURLToPath, pathToFileURL } from 'node:url';

export const repositoryRoot = fileURLToPath(new URL('../../', import.meta.url));
export const productPath = join(repositoryRoot, '99_Tools', 'ExecutionLock', 'execution-lock.mjs');
export const childScript = fileURLToPath(new URL('./fixture-child.mjs', import.meta.url));
export const bootstrapScript = fileURLToPath(new URL('./runner-bootstrap.mjs', import.meta.url));

export const GIB = 1024 ** 3;

// The product file does not exist yet before implementation; tests that need it fail with this
// error before any of their product assertions run.
export class ProductMissingError extends Error {
  constructor() {
    super('PRODUCT_MISSING: 99_Tools/ExecutionLock/execution-lock.mjs does not exist; '
      + 'no product assertion of this test ran.');
    this.name = 'ProductMissingError';
  }
}

// A broken fixture or environment, kept apart from product failures in the results.
export class FixtureError extends Error {
  constructor(message) {
    super(`FIXTURE_ERROR: ${message}`);
    this.name = 'FixtureError';
  }
}

let productLoad;
export function loadProduct() {
  productLoad ??= import(pathToFileURL(productPath).href).catch(error => {
    if (error?.code === 'ERR_MODULE_NOT_FOUND' && !existsSync(productPath)) throw new ProductMissingError();
    throw error;
  });
  return productLoad;
}

export function workRoot() {
  const root = process.env.EXECUTION_LOCK_TESTS_WORK;
  if (!root || !isAbsolute(root) || !existsSync(root)) {
    throw new FixtureError('Set EXECUTION_LOCK_TESTS_WORK to an existing absolute directory for the '
      + 'throwaway repositories, records and evidence of these tests.');
  }
  return root;
}

export const sleep = ms => new Promise(done => setTimeout(done, ms));

// A child environment without the parent test runner's context, so nested node processes run alone.
export function childEnvironment(extra = {}) {
  const environment = { ...process.env, ...extra };
  delete environment.NODE_TEST_CONTEXT;
  return environment;
}

export async function waitFor(check, { timeoutMs = 15_000, intervalMs = 25, what }) {
  const limit = Date.now() + timeoutMs;
  for (;;) {
    const value = await check();
    if (value) return value;
    if (Date.now() > limit) throw new Error(`Timed out after ${timeoutMs} ms waiting for ${what}.`);
    await sleep(intervalMs);
  }
}

// Path identity for comparisons: real path, one separator style, case-insensitive on Windows.
export function canonicalPath(path) {
  const real = existsSync(path) ? realpathSync.native(path) : resolve(path);
  const normal = resolve(real);
  return process.platform === 'win32' ? normal.toLowerCase() : normal;
}

export function git(cwd, args) {
  const result = spawnSync('git', args, { cwd, encoding: 'utf8', env: childEnvironment() });
  if (result.status !== 0) {
    throw new FixtureError(`git ${args.join(' ')} failed in ${cwd}: ${result.stderr || result.error}`);
  }
  return result.stdout.trim();
}

// A throwaway repository with one empty commit and optionally a linked worktree, all under the
// given folder. Identity is passed per command so no Git configuration is changed.
export async function createRepository(root, { worktree = false } = {}) {
  await mkdir(root, { recursive: true });
  git(root, ['init', '-q']);
  git(root, ['-c', 'user.name=execution-lock-fixture', '-c', 'user.email=fixture@invalid',
    'commit', '-q', '--allow-empty', '-m', 'fixture']);
  const repository = { root, worktree: null, worktreeSubdirectory: null, commonDir: null };
  if (worktree) {
    repository.worktree = `${root} linked worktree`;
    git(root, ['worktree', 'add', '-q', '-b', 'fixture-linked', repository.worktree]);
    repository.worktreeSubdirectory = join(repository.worktree, 'sub dir');
    await mkdir(repository.worktreeSubdirectory, { recursive: true });
  }
  repository.commonDir = git(root, ['rev-parse', '--path-format=absolute', '--git-common-dir']);
  return repository;
}

function writeJsonAtomically(path, value) {
  writeFileSync(`${path}.partial`, JSON.stringify(value));
  renameSync(`${path}.partial`, path);
}

function readJson(path) {
  try {
    return JSON.parse(readFileSync(path, 'utf8'));
  } catch (error) {
    if (error.code === 'ENOENT') return null;
    throw error;
  }
}

export function isAlive(pid) {
  try {
    process.kill(pid, 0);
    return true;
  } catch (error) {
    return error.code === 'EPERM';
  }
}

// Records the calling runner process (the test process or a bootstrap) so the injected observation
// lists it like any other live process of the scenario.
export function recordSelf(recordDir, label, name = 'node.exe') {
  writeJsonAtomically(join(recordDir, `${label}.started.json`), {
    label,
    name,
    pid: process.pid,
    parentPid: process.ppid,
    startedAt: new Date(performance.timeOrigin).toISOString(),
    cwd: process.cwd(),
    argv: process.argv.slice(2),
  });
}

export function recordEnd(recordDir, label, exitCode, reason) {
  writeJsonAtomically(join(recordDir, `${label}.ended.json`), {
    label,
    pid: process.pid,
    exitCode,
    reason,
    endedAt: new Date().toISOString(),
  });
}

export function startedRecord(recordDir, label) {
  return readJson(join(recordDir, `${label}.started.json`));
}

export function endedRecord(recordDir, label) {
  return readJson(join(recordDir, `${label}.ended.json`));
}

export function isRunning(recordDir, label) {
  const started = startedRecord(recordDir, label);
  return Boolean(started) && !endedRecord(recordDir, label) && isAlive(started.pid);
}

function liveProcesses(recordDir, identityOverrides = {}) {
  const processes = [];
  for (const entry of readdirSync(recordDir)) {
    if (!entry.endsWith('.started.json')) continue;
    const record = readJson(join(recordDir, entry));
    if (!record || endedRecord(recordDir, record.label) || !isAlive(record.pid)) continue;
    const override = identityOverrides[String(record.pid)] ?? {};
    processes.push({
      pid: record.pid,
      parentPid: record.parentPid,
      startedAt: override.startedAt ?? record.startedAt,
      name: override.name ?? record.name,
    });
  }
  return processes;
}

// A synthetic entry stands for a process that no longer exists (a reused PID). It is listed only
// while no real process holds that PID, so a wrong termination attempt cannot reach a real process;
// otherwise it is left out and recorded in refusals for the test to report as a fixture error.
function syntheticProcesses(entries = [], refusals) {
  const listed = [];
  for (const entry of entries) {
    if (isAlive(entry.pid)) refusals.push(entry.pid);
    else listed.push({ pid: entry.pid, parentPid: entry.parentPid, startedAt: entry.startedAt, name: entry.name });
  }
  return listed;
}

// machine() returns the scenario's injected machine state: { complete, memory, failure,
// identityOverrides, syntheticProcesses }. failure 'throw' rejects, 'hang' never settles.
export function createObservation({ recordDir, machine }) {
  const observation = {
    calls: 0,
    refusals: [],
    async observe() {
      observation.calls += 1;
      const state = machine();
      if (state.failure === 'throw') throw new Error('Injected observation failure (test seam).');
      if (state.failure === 'hang') return new Promise(() => {});
      return {
        observedAt: new Date().toISOString(),
        complete: state.complete,
        memory: { ...state.memory },
        processes: [
          ...liveProcesses(recordDir, state.identityOverrides),
          ...syntheticProcesses(state.syntheticProcesses, observation.refusals),
        ],
      };
    },
  };
  return observation;
}

export const ampleMachine = () => ({
  complete: true,
  memory: { physicalAvailableBytes: 64 * GIB, commitAvailableBytes: 64 * GIB },
  failure: null,
  identityOverrides: {},
  syntheticProcesses: [],
});

let runDirectory;
export async function runFolder() {
  runDirectory ??= await mkdtemp(join(workRoot(), 'run-'));
  return runDirectory;
}

// One scenario = one record folder (one simulated machine), evidence parent, a working folder whose
// name has spaces and punctuation, and an isolated state root that does not exist yet.
export async function createScenario(name) {
  const dir = join(await runFolder(), name);
  const scenario = {
    name,
    dir,
    recordDir: join(dir, 'records'),
    evidenceDir: join(dir, 'evidence'),
    workDir: join(dir, 'work dir (a&b)'),
    requestDir: join(dir, 'requests'),
    stateRoot: join(dir, 'state'),
    machinePath: join(dir, 'records', 'machine.json'),
    machine: ampleMachine(),
    bootstraps: [],
    controllers: [],
  };
  for (const folder of [scenario.recordDir, scenario.evidenceDir, scenario.workDir, scenario.requestDir]) {
    await mkdir(folder, { recursive: true });
  }
  writeJsonAtomically(scenario.machinePath, scenario.machine);
  recordSelf(scenario.recordDir, 'test-runner');
  scenario.observation = createObservation({ recordDir: scenario.recordDir, machine: () => scenario.machine });
  return scenario;
}

// Bootstraps read machine.json, this process reads the object; JSON cannot carry NaN or Infinity,
// so those values are only used by in-process runs.
export function setMachine(scenario, changes) {
  scenario.machine = { ...scenario.machine, ...changes };
  writeJsonAtomically(scenario.machinePath, scenario.machine);
}

export async function writeSpec(scenario, label, options = {}) {
  const spec = {
    label,
    name: options.name ?? 'Unity.exe',
    recordDir: scenario.recordDir,
    release: options.hold === false ? null : join(scenario.recordDir, `${label}.release`),
    exitCode: options.exitCode ?? 0,
    stdout: options.stdout ?? null,
    stderr: options.stderr ?? null,
    grandchild: options.grandchild ?? null,
    holdMaxMs: options.holdMaxMs ?? 90_000,
  };
  const path = join(scenario.recordDir, `${label}.spec.json`);
  await writeFile(path, JSON.stringify(spec, null, 2));
  return path;
}

export async function release(scenario, label) {
  await writeFile(join(scenario.recordDir, `${label}.release`), '');
}

// The execution object of the public request for one fixture command.
export async function execution(scenario, label, { kind = 'unity', part = 'Content', args = [], ...spec } = {}) {
  const specPath = await writeSpec(scenario, label, spec);
  return {
    kind,
    part,
    file: process.execPath,
    args: [childScript, specPath, ...args],
    cwd: scenario.workDir,
    timeoutMs: spec.timeoutMs ?? 120_000,
    ...(spec.waitTimeoutMs === undefined ? {} : { waitTimeoutMs: spec.waitTimeoutMs }),
  };
}

export async function writeRequest(scenario, name, request) {
  const path = join(scenario.requestDir, `${name}.json`);
  await writeFile(path, JSON.stringify(request, null, 2));
  return path;
}

// A harmless process of the test itself that is not under any runner (an "external" process).
export async function startBystander(scenario, label, name) {
  const specPath = await writeSpec(scenario, label, { name });
  const bystander = spawn(process.execPath, [childScript, specPath], { stdio: 'ignore', windowsHide: true });
  bystander.unref();
  return waitFor(() => startedRecord(scenario.recordDir, label), { what: `bystander ${label} to start` });
}

// The public runCli in this process through the explicit seam. stateRoot null means the default
// state location of the product.
export function runInProcess(product, scenario, requestPath, { stateRoot = scenario.stateRoot, signal } = {}) {
  const controller = new AbortController();
  scenario.controllers.push(controller);
  const dependencies = { observe: scenario.observation.observe, signal: signal ?? controller.signal };
  if (stateRoot !== null) dependencies.stateRoot = stateRoot;
  return product.runCli([requestPath], dependencies);
}

// The public runCli in a separate Node process (runner-bootstrap.mjs) with its own cwd.
export async function startBootstrap(scenario, label, requestPath, options = {}) {
  const config = {
    label,
    recordDir: scenario.recordDir,
    machinePath: scenario.machinePath,
    stateRoot: options.stateRoot === undefined ? scenario.stateRoot : options.stateRoot,
    barrier: options.barrier ?? null,
    cancelFile: join(scenario.recordDir, `${label}.cancel`),
  };
  const configPath = join(scenario.recordDir, `${label}.bootstrap.json`);
  await writeFile(configPath, JSON.stringify(config, null, 2));
  const child = spawn(process.execPath, [bootstrapScript, configPath, requestPath], {
    cwd: options.cwd ?? scenario.workDir,
    env: childEnvironment(),
    stdio: ['ignore', 'pipe', 'pipe'],
    windowsHide: true,
  });
  const output = { stdout: '', stderr: '' };
  child.stdout.on('data', chunk => { output.stdout += chunk; });
  child.stderr.on('data', chunk => { output.stderr += chunk; });
  const done = new Promise(settle => {
    child.on('close', (code, signal) => settle({ code, signal, ...output }));
  });
  // 'exit' does not wait for output pipes, which a surviving command could still hold.
  const exited = new Promise(settle => {
    child.on('exit', (code, signal) => settle({ code, signal }));
  });
  const bootstrap = { label, child, done, exited, cancelFile: config.cancelFile };
  scenario.bootstraps.push(bootstrap);
  return bootstrap;
}

// Ends every fixture command of the scenario through its stop file and reports what is still alive.
export async function stopScenario(scenario) {
  for (const controller of scenario.controllers) controller.abort();
  await writeFile(join(scenario.recordDir, 'stop-all'), '');
  const commandLabels = () => readdirSync(scenario.recordDir)
    .filter(entry => entry.endsWith('.spec.json'))
    .map(entry => entry.slice(0, -'.spec.json'.length));
  const alive = () => commandLabels().filter(label => isRunning(scenario.recordDir, label));
  try {
    await waitFor(() => alive().length === 0, { timeoutMs: 10_000, what: 'fixture commands to stop' });
  } catch {
    // Left for their own hold limit; reported, never killed by PID.
  }
  // Bootstraps see stop-all as cancellation; a product that ignores it is left to the bootstrap's
  // own lifetime bound.
  await Promise.race([Promise.all(scenario.bootstraps.map(bootstrap => bootstrap.done)), sleep(10_000)]);
  return alive();
}

export async function listFiles(root) {
  if (!existsSync(root)) return [];
  const files = [];
  for (const entry of await readdir(root, { recursive: true, withFileTypes: true })) {
    if (entry.isFile()) files.push(join(entry.parentPath ?? entry.path, entry.name));
  }
  return files.sort();
}

export async function snapshot(root) {
  const files = new Map();
  for (const path of await listFiles(root)) {
    files.set(relative(root, path), createHash('sha256').update(await readFile(path)).digest('hex'));
  }
  return files;
}

export async function directoriesIn(root) {
  if (!existsSync(root)) return [];
  const entries = await readdir(root, { withFileTypes: true });
  return entries.filter(entry => entry.isDirectory()).map(entry => join(root, entry.name)).sort();
}

export const sha256 = bytes => createHash('sha256').update(bytes).digest('hex');

export async function isDirectory(path) {
  try {
    return (await stat(path)).isDirectory();
  } catch {
    return false;
  }
}

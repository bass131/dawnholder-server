// Requirement tests for 99_Tools/ExecutionLock, written before the product. Expected outcomes come
// from the execution contract and the user decision it implements, never from the product:
// - contract v1: E/pr2-execution-contract-v1.md (SHA256 b43a07ff…e9aae2), decision 4 of
//   msg_b49fffe1ccaa (Unity one at a time Content first, WSL Core first, Core WSL + Unity only with
//   6 GiB physical and 4 GiB commit free);
// - supplement v1.1: E/pr2-execution-contract-v1.1-supplement.md (SHA256 b26bf807…75a5e542,
//   2026-10-15T04:07:05Z), the lead's answer msg_f2207ea7146f to question msg_f7d5a2ac1d82: argv is
//   [requestPath]; fixed result fields; owner loss reclaimed only on proven end; same PID with
//   another start identity is blocked/1 and kept; observation failure is tool-error/2; an observed
//   WSL client of unknown owner is blocked/1; EXECUTION_LOCK_TESTS_WORK is required.
// Covered: entry and input bounds, shared state under the Git common directory, atomic admission,
// Unity bundles by parentPid, waiting priority without preemption, one WSL command, the 6/4 GiB
// gate below/equal/above, lifetime (exit, nonzero, timeout, cancel, owner loss, PID reuse, damaged
// state), the status/exit table and evidence separation.
// The seam is runCli(argv, { observe, stateRoot, signal }); observations here are injected test
// values (lock-fixture.mjs lists their fidelity limits), never real Windows measurements.
//
// Environment: EXECUTION_LOCK_TESTS_WORK, an existing absolute folder for throwaway repositories,
// records and evidence (kept after the run).
import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { existsSync } from 'node:fs';
import { readFile, writeFile } from 'node:fs/promises';
import { join, sep } from 'node:path';
import { before, test } from 'node:test';
import { pathToFileURL } from 'node:url';

import {
  GIB,
  FixtureError,
  ampleMachine,
  canonicalPath,
  childEnvironment,
  childScript,
  createRepository,
  createScenario,
  directoriesIn,
  endedRecord,
  execution,
  git,
  isAlive,
  isDirectory,
  isRunning,
  listFiles,
  loadProduct,
  productPath,
  release,
  runFolder,
  runInProcess,
  setMachine,
  sha256,
  sleep,
  snapshot,
  startBootstrap,
  startBystander,
  startedRecord,
  stopScenario,
  waitFor,
  writeRequest,
  writeSpec,
} from './lock-fixture.mjs';

// Contract "결과와 근거" status / wrapper exit table; failed carries the command's own exit.
const STATUS_EXIT = {
  observed: 0,
  succeeded: 0,
  blocked: 1,
  'input-error': 2,
  'tool-error': 2,
  'timed-out': 124,
  cancelled: 130,
};
const KNOWN_STATUSES = [...Object.keys(STATUS_EXIT), 'failed'];
const NOT_STARTED_STATUSES = ['blocked', 'input-error', 'tool-error'];

// Supplement v1.1 Q2: root fields of every result, null when not obtained.
const RESULT_FIELDS = [
  'version',
  'status',
  'exitCode',
  'code',
  'message',
  'repair',
  'requestId',
  'repositoryCommonDir',
  'repositoryKey',
  'stateRoot',
  'evidenceDir',
  'executionId',
  'commandExitCode',
  'observationSource',
  'observedAt',
];

// Contract "Admission과 관측" 4: the Core WSL + Unity concurrency gate.
const PHYSICAL_GATE = 6 * GIB;
const COMMIT_GATE = 4 * GIB;

// Contract input bounds.
const REQUEST_LIMIT_BYTES = 1024 * 1024;
const PART_MAX = 64;
const ARGS_MAX = 256;
const ARG_MAX_CHARS = 32768;
const TIMEOUT_MAX_MS = 3_600_000;
const WAIT_TIMEOUT_MAX_MS = 300_000;

const DEFAULT_STATE_FOLDER = 'dawnholder-execution-lock';

// Arguments that a shell or a naive command line would change.
const TRICKY_ARGS = [
  'plain',
  'two words',
  ' leading and trailing ',
  'double"quote',
  "single'quote",
  'back\\slash',
  'trailing backslash\\',
  'semi;colon&amp|pipe<lt>gt',
  '%PATH%',
  '$HOME',
  '`tick`',
  '*?[glob]',
  '한글 인자',
  'tab\tinside',
  '',
];

// Settling time for a waiting run to register before the next step. In-process runs register
// within milliseconds; the margin keeps the order a property of the scenario, not of timing luck.
const REGISTRATION_SETTLE_MS = 1000;

let repos;
before(async () => {
  const root = await runFolder();
  repos = {
    // Default state location tests: one repository with a linked worktree, and a second repository.
    shared: await createRepository(join(root, 'repo shared'), { worktree: true }),
    other: await createRepository(join(root, 'repo other')),
    // Repository for tests that inject an isolated stateRoot.
    isolated: await createRepository(join(root, 'repo isolated')),
  };
});

async function scenarioFor(t, name) {
  const scenario = await createScenario(name);
  t.after(async () => {
    const residue = await stopScenario(scenario);
    if (residue.length > 0) t.diagnostic(`fixture commands left to their hold limit: ${residue.join(', ')}`);
  });
  return scenario;
}

function runRequest(scenario, executionObject, repoRoot = repos.isolated.root) {
  return { version: 1, action: 'run', repoRoot, evidenceDir: scenario.evidenceDir, execution: executionObject };
}

function inspectRequest(repoRoot = repos.isolated.root) {
  return { version: 1, action: 'inspect', repoRoot };
}

async function withTimeout(promise, ms, what) {
  let timer;
  const limit = new Promise((_, reject) => {
    timer = setTimeout(() => reject(new Error(`Timed out after ${ms} ms waiting for ${what}.`)), ms);
  });
  try {
    return await Promise.race([promise, limit]);
  } finally {
    clearTimeout(timer);
  }
}

// ---------------------------------------------------------------------------------------------
// Result judgements. Each helper checks one meaning so a failure names what was wrong.

function assertResult(result, status, label) {
  assert.ok(result && typeof result === 'object' && !Array.isArray(result), `${label}: result object`);
  assert.equal(result.version, 1, `${label}: version`);
  assert.equal(result.status, status, `${label}: ${JSON.stringify(result)}`);
  if (status !== 'failed') assert.equal(result.exitCode, STATUS_EXIT[status], `${label}: exitCode`);
  for (const field of RESULT_FIELDS) assert.ok(field in result, `${label}: field ${field} (null when not obtained)`);
  assert.equal(typeof result.message, 'string', `${label}: message`);
  assert.ok(result.message.trim().length > 0, `${label}: empty message`);
  if (status === 'observed' || status === 'succeeded') return;
  for (const field of ['code', 'repair']) {
    assert.equal(typeof result[field], 'string', `${label}: ${field} names the cause and the next check`);
    assert.ok(result[field].trim().length > 0, `${label}: empty ${field}`);
  }
  if (NOT_STARTED_STATUSES.includes(status)) {
    assert.equal(result.commandExitCode, null, `${label}: no command exit when nothing started`);
  }
  if (status === 'input-error') {
    for (const field of ['executionId', 'evidenceDir', 'observationSource', 'observedAt']) {
      assert.equal(result[field], null, `${label}: ${field} is null for a request rejected at entry`);
    }
  }
}

function assertKnownResult(result, label) {
  assert.ok(result && KNOWN_STATUSES.includes(result.status), `${label}: known status: ${JSON.stringify(result)}`);
}

function assertIsoUtc(value, label) {
  assert.equal(typeof value, 'string', `${label}: ISO UTC string`);
  assert.match(value, /Z$/, `${label}: UTC`);
  assert.ok(!Number.isNaN(Date.parse(value)), `${label}: parseable time`);
}

// Supplement v1.1 Q2: the seam is named exactly, with the time of the observation used.
function assertInjectedObservation(result, label) {
  assert.equal(result.observationSource, 'injected', `${label}: observationSource`);
  assertIsoUtc(result.observedAt, `${label}: observedAt`);
}

function assertSamePath(actual, expected, label) {
  assert.equal(typeof actual, 'string', `${label}: path string`);
  assert.equal(canonicalPath(actual), canonicalPath(expected), label);
}

function assertLocation(result, { commonDir, stateRoot }, label) {
  assertSamePath(result.repositoryCommonDir, commonDir, `${label}: repositoryCommonDir`);
  assert.ok(result.repositoryKey !== null && result.repositoryKey !== undefined, `${label}: repositoryKey`);
  assertSamePath(result.stateRoot, stateRoot, `${label}: stateRoot`);
}

// The new run folder of a run: an existing directory strictly below the requested evidence parent.
function assertRunEvidence(result, evidenceParent, label) {
  assert.equal(typeof result.evidenceDir, 'string', `${label}: evidenceDir`);
  assert.ok(existsSync(result.evidenceDir), `${label}: evidenceDir exists`);
  assert.ok(canonicalPath(result.evidenceDir).startsWith(canonicalPath(evidenceParent) + sep),
    `${label}: evidenceDir is a new folder below ${evidenceParent}: ${result.evidenceDir}`);
  assert.ok(result.executionId !== null && result.executionId !== undefined, `${label}: executionId`);
  return result.evidenceDir;
}

async function evidenceFiles(folder) {
  const files = [];
  for (const path of await listFiles(folder)) files.push({ path, bytes: await readFile(path) });
  return files;
}

const evidenceText = files => files.map(file => file.bytes.toString('utf8')).join('\n');

// ---------------------------------------------------------------------------------------------
// Run helpers.

// Starts one run and waits until its command starts (admitted) or the run settles first.
async function attempt(product, scenario, label, options = {}) {
  const { stateRoot, signal, repoRoot, decisionTimeoutMs = 30_000, ...executionOptions } = options;
  const executionObject = await execution(scenario, label, executionOptions);
  const requestPath = await writeRequest(scenario, label, runRequest(scenario, executionObject, repoRoot));
  const pending = runInProcess(product, scenario, requestPath, { stateRoot, signal });
  let settled = null;
  pending.then(result => { settled = { result }; }, error => { settled = { error }; });
  await waitFor(() => settled || startedRecord(scenario.recordDir, label), {
    timeoutMs: decisionTimeoutMs,
    what: `${label} to start its command or settle`,
  });
  if (settled?.error) throw settled.error;
  return {
    label,
    pending,
    requestPath,
    admitted: Boolean(startedRecord(scenario.recordDir, label)),
    result: settled?.result ?? null,
  };
}

async function admitted(product, scenario, label, options = {}) {
  const run = await attempt(product, scenario, label, options);
  assert.ok(run.admitted, `${label}: expected the command to start: ${JSON.stringify(run.result)}`);
  return run;
}

function assertNotAdmitted(run, status) {
  assert.equal(run.admitted, false, `${run.label}: the command must not start`);
  assertResult(run.result, status, run.label);
}

async function finish(scenario, run, timeoutMs = 20_000) {
  await release(scenario, run.label);
  return withTimeout(run.pending, timeoutMs, `${run.label} to finish after release`);
}

async function waitEnded(scenario, label) {
  return waitFor(() => endedRecord(scenario.recordDir, label) || !isRunning(scenario.recordDir, label),
    { timeoutMs: 15_000, what: `${label} to end` });
}

async function bootstrapResult(bootstrap, what) {
  const done = await withTimeout(bootstrap.done, 30_000, what);
  const lines = done.stdout.split('\n').filter(line => line.length > 0);
  assert.equal(lines.length, 1, `${what}: one JSON result line: ${JSON.stringify(done)}`);
  return { ...done, result: JSON.parse(lines[0]) };
}

// ---------------------------------------------------------------------------------------------
// Fixture self-checks. They need no product; passing them shows the fixture works, not the lock.

test('fixture: harmless command records argv and cwd, writes both streams and exits as told', async t => {
  const scenario = await scenarioFor(t, 'fixture-command');
  const specPath = await writeSpec(scenario, 'command',
    { hold: false, stdout: 'OUT-MARK', stderr: 'ERR-MARK', exitCode: 7 });
  const run = spawnSync(process.execPath, [childScript, specPath, ...TRICKY_ARGS], {
    cwd: scenario.workDir,
    encoding: 'utf8',
    env: childEnvironment(),
    timeout: 30_000,
  });
  assert.equal(run.error, undefined, `spawn: ${run.error}`);
  assert.equal(run.status, 7);
  const started = startedRecord(scenario.recordDir, 'command');
  assert.deepEqual(started.argv, TRICKY_ARGS);
  assert.equal(canonicalPath(started.cwd), canonicalPath(scenario.workDir));
  assert.equal(run.stdout, 'OUT-MARK');
  assert.equal(run.stderr, 'ERR-MARK');
});

test('fixture: injected observation lists live fixture processes with their recorded identity only', async t => {
  const scenario = await scenarioFor(t, 'fixture-observation');
  const record = await startBystander(scenario, 'bystander', 'Unity.exe');
  const seen = await scenario.observation.observe();
  assert.equal(seen.complete, true);
  assert.deepEqual(seen.memory, ampleMachine().memory);
  const listed = seen.processes.find(item => item.pid === record.pid);
  assert.deepEqual(listed, { pid: record.pid, parentPid: process.pid, startedAt: record.startedAt, name: 'Unity.exe' });
  assert.ok(seen.processes.some(item => item.pid === process.pid), 'the test process itself is listed');

  const otherStart = '2000-01-01T00:00:00.000Z';
  setMachine(scenario, { identityOverrides: { [record.pid]: { startedAt: otherStart, name: 'other.exe' } } });
  const reused = (await scenario.observation.observe()).processes.find(item => item.pid === record.pid);
  assert.deepEqual(reused, { pid: record.pid, parentPid: process.pid, startedAt: otherStart, name: 'other.exe' });

  // A synthetic entry is refused while a real process holds its PID.
  const synthetic = { pid: record.pid, parentPid: 1, startedAt: otherStart, name: 'gone.exe' };
  setMachine(scenario, { identityOverrides: {}, syntheticProcesses: [synthetic] });
  const refused = await scenario.observation.observe();
  assert.ok(!refused.processes.some(item => item.name === 'gone.exe'));
  assert.deepEqual(scenario.observation.refusals, [record.pid]);

  await release(scenario, 'bystander');
  await waitEnded(scenario, 'bystander');
  await waitFor(() => !isAlive(record.pid), { what: 'the bystander process to exit' });
  const after = await scenario.observation.observe();
  assert.ok(!after.processes.some(item => item.pid === record.pid && item.name === 'Unity.exe'),
    'an ended process is not listed');
  assert.ok(after.processes.some(item => item.pid === record.pid && item.name === 'gone.exe'),
    'a synthetic entry is listed once no real process holds the PID');

  setMachine(scenario, { failure: 'throw' });
  await assert.rejects(scenario.observation.observe(), /Injected observation failure/);
});

test('fixture: a linked worktree and its repository share one Git common directory', async () => {
  const fromWorktree = git(repos.shared.worktreeSubdirectory,
    ['rev-parse', '--path-format=absolute', '--git-common-dir']);
  assert.equal(canonicalPath(fromWorktree), canonicalPath(repos.shared.commonDir));
  assert.notEqual(canonicalPath(repos.other.commonDir), canonicalPath(repos.shared.commonDir));
});

test('fixture: an import child keeps running after its root exits and names the root as parent', async t => {
  const scenario = await scenarioFor(t, 'fixture-grandchild');
  const grandchild = await writeSpec(scenario, 'import', { name: 'Unity.exe' });
  const rootSpec = await writeSpec(scenario, 'root', { hold: false, grandchild });
  const run = spawnSync(process.execPath, [childScript, rootSpec], { env: childEnvironment(), timeout: 30_000 });
  assert.equal(run.status, 0);
  const root = startedRecord(scenario.recordDir, 'root');
  const imported = startedRecord(scenario.recordDir, 'import');
  assert.equal(imported.parentPid, root.pid);
  assert.ok(endedRecord(scenario.recordDir, 'root'));
  assert.ok(isRunning(scenario.recordDir, 'import'), 'the import child outlives its root');
  await release(scenario, 'import');
  await waitEnded(scenario, 'import');
});

// ---------------------------------------------------------------------------------------------
// Public entry and input (contract "공개 진입과 입력", "시험 seam과 실제 진입"; v1.1 Q1).

function runTopLevel(args, cwd) {
  return spawnSync(process.execPath, [productPath, ...args], {
    cwd,
    encoding: 'utf8',
    env: childEnvironment(),
    timeout: 60_000,
  });
}

function assertTopLevelInputError(run, label) {
  assert.equal(run.error, undefined, `${label}: spawn ${run.error}`);
  const lines = run.stdout.split('\n').filter(line => line.trim().length > 0);
  assert.equal(lines.length, 1, `${label}: stdout is one JSON result: ${JSON.stringify(run.stdout)}`);
  assertResult(JSON.parse(lines[0]), 'input-error', label);
  assert.equal(run.status, 2, `${label}: process exit`);
}

test('top-level CLI: anything but exactly one request path is input-error/2 with one JSON result', async t => {
  await loadProduct();
  const scenario = await scenarioFor(t, 'cli-arguments');
  const requestPath = await writeRequest(scenario, 'inspect', inspectRequest());
  assertTopLevelInputError(runTopLevel([], scenario.workDir), 'no argument');
  assertTopLevelInputError(runTopLevel([requestPath, requestPath], scenario.workDir), 'two arguments');
  assertTopLevelInputError(runTopLevel(['--observe', requestPath], scenario.workDir), 'a flag before the path');
});

test('top-level CLI: unreadable, non-UTF-8, non-JSON, oversized or seam-field requests are input-error/2', async t => {
  await loadProduct();
  const scenario = await scenarioFor(t, 'cli-request-files');
  const files = {
    missing: join(scenario.requestDir, 'missing.json'),
    notUtf8: join(scenario.requestDir, 'not-utf8.json'),
    notJson: join(scenario.requestDir, 'not-json.json'),
    oversized: join(scenario.requestDir, 'oversized.json'),
  };
  await writeFile(files.notUtf8, Buffer.from([0x7b, 0xff, 0xfe, 0x7d]));
  await writeFile(files.notJson, '{"version": 1,');
  const body = JSON.stringify(inspectRequest());
  await writeFile(files.oversized, body + ' '.repeat(REQUEST_LIMIT_BYTES + 1 - Buffer.byteLength(body)));
  for (const [label, path] of Object.entries(files)) {
    assertTopLevelInputError(runTopLevel([path], scenario.workDir), label);
  }
  // The product CLI has no way to choose a state root or an injected observation from the request.
  const stateRootField = await writeRequest(scenario, 'state-root-field',
    { ...inspectRequest(), stateRoot: scenario.stateRoot });
  const observeField = await writeRequest(scenario, 'observe-field',
    { ...inspectRequest(), observe: ampleMachine() });
  assertTopLevelInputError(runTopLevel([stateRootField], scenario.workDir), 'stateRoot field');
  assertTopLevelInputError(runTopLevel([observeField], scenario.workDir), 'observe field');
  assert.equal(existsSync(scenario.stateRoot), false);
});

test('import of execution-lock.mjs exports runCli and runs nothing', async t => {
  await loadProduct();
  const scenario = await scenarioFor(t, 'import-only');
  const repository = await createRepository(join(scenario.dir, 'repo import'));
  const script = `const module = await import(${JSON.stringify(pathToFileURL(productPath).href)});`
    + 'process.stdout.write(typeof module.runCli);';
  const run = spawnSync(process.execPath, ['--input-type=module', '-e', script], {
    cwd: repository.root,
    encoding: 'utf8',
    env: childEnvironment(),
    timeout: 30_000,
  });
  assert.equal(run.status, 0, run.stderr);
  assert.equal(run.stdout, 'function', 'import writes nothing but what the caller prints');
  assert.equal(existsSync(join(repository.commonDir, DEFAULT_STATE_FOLDER)), false, 'no state on import');
});

test('runCli: undefined fields, wrong types and out-of-range values are input-error/2 and start nothing', async t => {
  const product = await loadProduct();
  const scenario = await scenarioFor(t, 'input-validation');
  const outsideFile = join(scenario.workDir, 'not-there.exe');
  // Each case changes one part of a valid Unity run request.
  const cases = {
    'version 2': request => { request.version = 2; },
    'version as string': request => { request.version = '1'; },
    'unknown action': request => { request.action = 'stop'; },
    'undefined top-level field': request => { request.extra = true; },
    'stateRoot in the request': request => { request.stateRoot = scenario.stateRoot; },
    'observe in the request': request => { request.observe = ampleMachine(); },
    'missing repoRoot': request => { delete request.repoRoot; },
    'relative repoRoot': request => { request.repoRoot = 'repo isolated'; },
    'nonexistent repoRoot': request => { request.repoRoot = join(scenario.dir, 'no repo here'); },
    'run without evidenceDir': request => { delete request.evidenceDir; },
    'relative evidenceDir': request => { request.evidenceDir = 'evidence'; },
    'nonexistent evidenceDir': request => { request.evidenceDir = join(scenario.dir, 'no evidence'); },
    'run without execution': request => { delete request.execution; },
    'inspect with execution': request => { request.action = 'inspect'; },
    'undefined execution field': request => { request.execution.extra = 1; },
    'missing kind': request => { delete request.execution.kind; },
    'unknown kind': request => { request.execution.kind = 'docker'; },
    'missing part': request => { delete request.execution.part; },
    'empty part': request => { request.execution.part = ''; },
    'part with a space': request => { request.execution.part = 'Co re'; },
    'part over 64 characters': request => { request.execution.part = 'P'.repeat(PART_MAX + 1); },
    'part not a string': request => { request.execution.part = 7; },
    'missing file': request => { delete request.execution.file; },
    'relative file': request => { request.execution.file = 'node'; },
    'nonexistent file': request => { request.execution.file = outsideFile; },
    'missing args': request => { delete request.execution.args; },
    'args not an array': request => { request.execution.args = 'a b'; },
    'args with a number': request => { request.execution.args.push(5); },
    'over 256 args': request => {
      request.execution.args.push(...Array(ARGS_MAX + 1 - request.execution.args.length).fill('x'));
    },
    'arg over 32768 characters': request => { request.execution.args.push('a'.repeat(ARG_MAX_CHARS + 1)); },
    'arg with NUL': request => { request.execution.args.push('nul\u0000inside'); },
    'missing cwd': request => { delete request.execution.cwd; },
    'relative cwd': request => { request.execution.cwd = 'work'; },
    'nonexistent cwd': request => { request.execution.cwd = join(scenario.dir, 'no cwd'); },
    'missing timeoutMs': request => { delete request.execution.timeoutMs; },
    'timeoutMs 0': request => { request.execution.timeoutMs = 0; },
    'timeoutMs over 3600000': request => { request.execution.timeoutMs = TIMEOUT_MAX_MS + 1; },
    'timeoutMs fraction': request => { request.execution.timeoutMs = 1.5; },
    'timeoutMs string': request => { request.execution.timeoutMs = '1000'; },
    'waitTimeoutMs negative': request => { request.execution.waitTimeoutMs = -1; },
    'waitTimeoutMs over 300000': request => { request.execution.waitTimeoutMs = WAIT_TIMEOUT_MAX_MS + 1; },
    'waitTimeoutMs fraction': request => { request.execution.waitTimeoutMs = 0.5; },
  };
  let index = 0;
  for (const [label, change] of Object.entries(cases)) {
    index += 1;
    const commandLabel = `invalid-${index}`;
    const request = runRequest(scenario, await execution(scenario, commandLabel, { hold: false }));
    change(request);
    const requestPath = await writeRequest(scenario, commandLabel, request);
    const stateRoot = join(scenario.dir, `state-${index}`);
    const result = await withTimeout(runInProcess(product, scenario, requestPath, { stateRoot }), 30_000, label);
    assertResult(result, 'input-error', label);
    assert.equal(startedRecord(scenario.recordDir, commandLabel), null, `${label}: nothing started`);
    assert.equal(existsSync(stateRoot), false, `${label}: rejected at entry, no state written`);
  }
  for (const [label, body] of Object.entries({ 'array request': [], 'null request': null })) {
    const requestPath = await writeRequest(scenario, label.replace(' ', '-'), body);
    assertResult(await runInProcess(product, scenario, requestPath), 'input-error', label);
  }
  const oversized = await writeRequest(scenario, 'oversized', inspectRequest());
  const body = JSON.stringify(inspectRequest());
  await writeFile(oversized, body + ' '.repeat(REQUEST_LIMIT_BYTES + 1 - Buffer.byteLength(body)));
  assertResult(await runInProcess(product, scenario, oversized), 'input-error', 'request over 1 MiB');
  for (const argv of [[], [oversized, oversized]]) {
    assertResult(await product.runCli(argv, { observe: scenario.observation.observe, stateRoot: scenario.stateRoot }),
      'input-error', `runCli argv of ${argv.length}`);
  }
});

test('runCli: values at the input bounds are accepted', { timeout: 180_000 }, async t => {
  const product = await loadProduct();
  const scenario = await scenarioFor(t, 'input-bounds');
  const runToEnd = async (label, options) => {
    const request = runRequest(scenario, await execution(scenario, label, { hold: false, ...options }));
    const requestPath = await writeRequest(scenario, label, request);
    return withTimeout(runInProcess(product, scenario, requestPath), 60_000, label);
  };
  assertResult(await runToEnd('part-64', { part: 'P'.repeat(PART_MAX) }), 'succeeded', 'part of 64 characters');
  assertResult(await runToEnd('timeout-max', { timeoutMs: TIMEOUT_MAX_MS }), 'succeeded', 'timeoutMs 3600000');
  assertResult(await runToEnd('wait-zero', { waitTimeoutMs: 0 }), 'succeeded', 'waitTimeoutMs 0');
  assertResult(await runToEnd('wait-max', { waitTimeoutMs: WAIT_TIMEOUT_MAX_MS }), 'succeeded', 'waitTimeoutMs 300000');

  // args = [fixture-child.mjs, spec, ...254 more] is exactly 256 entries.
  const extra = Array.from({ length: ARGS_MAX - 2 }, (_, i) => `arg-${i}`);
  assertResult(await runToEnd('args-256', { args: extra }), 'succeeded', '256 args');
  assert.deepEqual(startedRecord(scenario.recordDir, 'args-256').argv, extra);

  // One argument of exactly 32768 characters passes input validation. Windows limits a whole
  // command line to 32767 characters, so the start itself may fail there; it must not be an input
  // error and must not leave the reservation behind.
  const longArg = await runToEnd('arg-32768', { args: ['a'.repeat(ARG_MAX_CHARS)] });
  assertKnownResult(longArg, 'arg of 32768 characters');
  assert.notEqual(longArg.status, 'input-error', 'arg of 32768 characters is within the input bound');
  assertResult(await runToEnd('after-long-arg', {}), 'succeeded', 'next run after the long argument');

  const exactLimit = await writeRequest(scenario, 'exact-limit', inspectRequest());
  const body = JSON.stringify(inspectRequest());
  await writeFile(exactLimit, body + ' '.repeat(REQUEST_LIMIT_BYTES - Buffer.byteLength(body)));
  assertResult(await runInProcess(product, scenario, exactLimit), 'observed', 'request of exactly 1 MiB');
});

// ---------------------------------------------------------------------------------------------
// Inspect and shared state (contract "공용 상태와 원자성"; v1.1 Q2).

test('inspect: an empty state is observed/0 and creates no state folder or lock', async t => {
  const product = await loadProduct();
  const scenario = await scenarioFor(t, 'inspect-empty');
  const repository = await createRepository(join(scenario.dir, 'repo inspect'));
  const requestPath = await writeRequest(scenario, 'inspect', inspectRequest(repository.root));

  const injected = await runInProcess(product, scenario, requestPath);
  assertResult(injected, 'observed', 'inspect with an isolated state root');
  assertInjectedObservation(injected, 'inspect with an isolated state root');
  assertLocation(injected, { commonDir: repository.commonDir, stateRoot: scenario.stateRoot }, 'isolated inspect');
  assert.equal(injected.evidenceDir, null, 'inspect makes no evidence folder');
  assert.equal(injected.executionId, null, 'inspect has no execution');
  assert.equal(existsSync(scenario.stateRoot), false, 'inspect does not create the state root');

  const defaultState = await runInProcess(product, scenario, requestPath, { stateRoot: null });
  assertResult(defaultState, 'observed', 'inspect at the default state location');
  assertLocation(defaultState,
    { commonDir: repository.commonDir, stateRoot: join(repository.commonDir, DEFAULT_STATE_FOLDER) },
    'default inspect');
  assert.equal(existsSync(join(repository.commonDir, DEFAULT_STATE_FOLDER)), false,
    'inspect does not create the default state folder');
});

test('inspect: failed observation is tool-error/2, not an empty state', async t => {
  const product = await loadProduct();
  const scenario = await scenarioFor(t, 'inspect-observation-failure');
  setMachine(scenario, { failure: 'throw' });
  const requestPath = await writeRequest(scenario, 'inspect', inspectRequest());
  const result = await runInProcess(product, scenario, requestPath);
  assertResult(result, 'tool-error', 'inspect with failed observation');
  assert.equal(result.observationSource, 'injected', 'the source that failed is recorded');
  assert.equal(result.observedAt, null, 'no observation time without an observation');
});

test('inspect: from another worktree it sees the same state and changes nothing', { timeout: 120_000 }, async t => {
  const product = await loadProduct();
  const scenario = await scenarioFor(t, 'inspect-other-worktree');
  // A lost owner leaves a state that no live runner rewrites, so any change would be inspect's.
  const lost = await startLostOwner(scenario, 'orphan', { repoRoot: repos.shared.root, cwd: repos.shared.root });

  const before = await snapshot(scenario.stateRoot);
  assert.ok(before.size > 0, 'a running execution is kept in the shared state on disk');
  const requestPath = await writeRequest(scenario, 'inspect', inspectRequest(repos.shared.worktreeSubdirectory));
  const inspected = await runInProcess(product, scenario, requestPath);
  assertResult(inspected, 'observed', 'inspect from the linked worktree');
  assertLocation(inspected, { commonDir: repos.shared.commonDir, stateRoot: scenario.stateRoot }, 'worktree inspect');
  assert.deepEqual(await snapshot(scenario.stateRoot), before, 'inspect reads only, even with a lost owner');
  assert.ok(isRunning(scenario.recordDir, lost.importLabel), 'inspect terminates nothing');

  await endLostOwner(scenario, 'orphan', lost);
  const fromMain = await admitted(product, scenario, 'from-main',
    { name: 'node.exe', repoRoot: repos.shared.root, waitTimeoutMs: 10_000 });
  const result = await finish(scenario, fromMain);
  assertResult(result, 'succeeded', 'run from the main worktree');
  assert.deepEqual(inspected.repositoryKey, result.repositoryKey, 'one repository key for both worktrees');
});

test('shared state: worktrees of one repository use one state under the Git common directory', { timeout: 120_000 }, async t => {
  await loadProduct();
  const scenario = await scenarioFor(t, 'default-state');
  const sharedState = join(repos.shared.commonDir, DEFAULT_STATE_FOLDER);
  // Commands are reported as node.exe so only the state, not a Unity process name, can block.
  const holderExecution = await execution(scenario, 'holder', { name: 'node.exe' });
  const holderRequest = await writeRequest(scenario, 'holder',
    runRequest(scenario, holderExecution, repos.shared.root));
  const holder = await startBootstrap(scenario, 'holder-runner', holderRequest,
    { cwd: repos.shared.root, stateRoot: null });
  await waitFor(() => startedRecord(scenario.recordDir, 'holder'), { what: 'holder command' });

  const fromWorktree = await execution(scenario, 'from-worktree', { name: 'node.exe', waitTimeoutMs: 0 });
  const worktreeRequest = await writeRequest(scenario, 'from-worktree',
    runRequest(scenario, fromWorktree, repos.shared.worktreeSubdirectory));
  const second = await startBootstrap(scenario, 'worktree-runner', worktreeRequest,
    { cwd: repos.shared.worktreeSubdirectory, stateRoot: null });
  const worktree = (await bootstrapResult(second, 'worktree runner')).result;
  assertResult(worktree, 'blocked', 'Unity from the linked worktree while one runs');
  assert.equal(startedRecord(scenario.recordDir, 'from-worktree'), null);
  assertLocation(worktree, { commonDir: repos.shared.commonDir, stateRoot: sharedState }, 'worktree runner');

  // A different repository has its own state and is not blocked by this one's.
  const otherExecution = await execution(scenario, 'other-repo', { name: 'node.exe', waitTimeoutMs: 0 });
  const otherRequest = await writeRequest(scenario, 'other-repo',
    runRequest(scenario, otherExecution, repos.other.root));
  const otherRunner = await startBootstrap(scenario, 'other-runner', otherRequest,
    { cwd: repos.other.root, stateRoot: null });
  await waitFor(() => startedRecord(scenario.recordDir, 'other-repo'), { what: 'other repository command' });
  await release(scenario, 'other-repo');
  const other = (await bootstrapResult(otherRunner, 'other runner')).result;
  assertResult(other, 'succeeded', 'other repository');
  assertLocation(other,
    { commonDir: repos.other.commonDir, stateRoot: join(repos.other.commonDir, DEFAULT_STATE_FOLDER) }, 'other runner');

  await release(scenario, 'holder');
  const held = (await bootstrapResult(holder, 'holder runner')).result;
  assertResult(held, 'succeeded', 'holder');
  assertLocation(held, { commonDir: repos.shared.commonDir, stateRoot: sharedState }, 'holder');
  assert.deepEqual(worktree.repositoryKey, held.repositoryKey, 'one key for the repository and its worktree');
  assert.notDeepEqual(other.repositoryKey, held.repositoryKey, 'another repository has another key');
  assert.ok(await isDirectory(sharedState));
  for (const worktreeGitDir of await directoriesIn(join(repos.shared.commonDir, 'worktrees'))) {
    assert.equal(existsSync(join(worktreeGitDir, DEFAULT_STATE_FOLDER)), false, 'no per-worktree state');
  }
});

test('atomic admission: simultaneous Unity requests from separate processes admit exactly one', { timeout: 180_000 }, async t => {
  await loadProduct();
  const scenario = await scenarioFor(t, 'atomic-admission');
  const cwds = [repos.isolated.root, scenario.workDir, scenario.dir];
  for (let round = 1; round <= 3; round += 1) {
    const barrier = join(scenario.recordDir, `round-${round}.go`);
    const runners = [];
    for (let index = 0; index < cwds.length; index += 1) {
      const label = `round-${round}-${index}`;
      const executionObject = await execution(scenario, label, { name: 'node.exe', waitTimeoutMs: 0 });
      const requestPath = await writeRequest(scenario, label, runRequest(scenario, executionObject));
      const bootstrap = await startBootstrap(scenario, `${label}-runner`, requestPath, { cwd: cwds[index], barrier });
      runners.push({ label, bootstrap });
    }
    await waitFor(() => runners.every(runner => existsSync(join(scenario.recordDir, `${runner.label}-runner.ready`))),
      { what: `round ${round} runners to be ready` });
    await writeFile(barrier, '');

    const exited = new Set();
    for (const runner of runners) runner.bootstrap.done.then(() => exited.add(runner.label));
    const startedRunners = () => runners.filter(runner => startedRecord(scenario.recordDir, runner.label));
    const allExited = () => exited.size === runners.length;
    const oneStartedOthersExited = () => startedRunners().length >= 1 && exited.size >= runners.length - 1;
    await waitFor(() => allExited() || oneStartedOthersExited(),
      { timeoutMs: 30_000, what: `round ${round} decisions` });
    await sleep(300);
    const winners = startedRunners();
    assert.equal(winners.length, 1, `round ${round}: exactly one command starts: ${winners.map(r => r.label)}`);
    for (const runner of runners.filter(item => item !== winners[0])) {
      const { result } = await bootstrapResult(runner.bootstrap, runner.label);
      assertResult(result, 'blocked', `round ${round} ${runner.label}`);
    }
    await release(scenario, winners[0].label);
    const { result: winnerResult } = await bootstrapResult(winners[0].bootstrap, winners[0].label);
    assertResult(winnerResult, 'succeeded', `round ${round} winner`);
  }
});

// ---------------------------------------------------------------------------------------------
// Unity admission and bundles (contract "Admission과 관측" 1, 6; v1.1 Q4, Q5).

test('unity: an external Unity root blocks a new Unity whatever its name case and is not terminated', async t => {
  const product = await loadProduct();
  const scenario = await scenarioFor(t, 'unity-external');
  for (const [index, name] of ['Unity.exe', 'unity.EXE'].entries()) {
    const external = await startBystander(scenario, `external-${index}`, name);
    const run = await attempt(product, scenario, `new-unity-${index}`, { waitTimeoutMs: 0 });
    assertNotAdmitted(run, 'blocked');
    assertInjectedObservation(run.result, `external ${name}`);
    assert.ok(isAlive(external.pid) && isRunning(scenario.recordDir, `external-${index}`), `${name} untouched`);
    await release(scenario, `external-${index}`);
    await waitEnded(scenario, `external-${index}`);
  }
});

test('unity: a second Unity is blocked while one runs and the running one is not preempted', { timeout: 120_000 }, async t => {
  const product = await loadProduct();
  const scenario = await scenarioFor(t, 'unity-one');
  const holder = await admitted(product, scenario, 'core-unity', { part: 'Core' });
  const content = await attempt(product, scenario, 'content-unity', { part: 'Content', waitTimeoutMs: 0 });
  assertNotAdmitted(content, 'blocked');
  assert.ok(isRunning(scenario.recordDir, 'core-unity'), 'Content priority does not stop a running Core Unity');
  assertResult(await finish(scenario, holder), 'succeeded', 'holder');
});

test('unity: a live import child keeps the bundle after its root exits', { timeout: 120_000 }, async t => {
  const product = await loadProduct();
  const scenario = await scenarioFor(t, 'unity-import-child');
  // node.exe names: only the parentPid ancestry, not a Unity name, ties the child to the bundle.
  const importSpec = await writeSpec(scenario, 'import', { name: 'node.exe' });
  const rootExecution = await execution(scenario, 'root', { name: 'node.exe', hold: false, grandchild: importSpec });
  const rootRequest = await writeRequest(scenario, 'root', runRequest(scenario, rootExecution));
  const rootPending = runInProcess(product, scenario, rootRequest);
  await waitFor(() => endedRecord(scenario.recordDir, 'root') && startedRecord(scenario.recordDir, 'import'),
    { what: 'root to exit with its import child running' });
  await sleep(REGISTRATION_SETTLE_MS);

  const probe = await attempt(product, scenario, 'probe', { name: 'node.exe', waitTimeoutMs: 0 });
  assertNotAdmitted(probe, 'blocked');
  assert.ok(isRunning(scenario.recordDir, 'import'), 'a new request does not terminate the import child');

  await release(scenario, 'import');
  await waitEnded(scenario, 'import');
  // v1.1 Q4: when the original run returns is not fixed, but it ends in finite time after the
  // bundle ended, and the lock is then free.
  assertKnownResult(await withTimeout(rootPending, 30_000, 'original run after its bundle ended'), 'original run');
  const next = await admitted(product, scenario, 'next', { name: 'node.exe', waitTimeoutMs: 15_000 });
  assertResult(await finish(scenario, next), 'succeeded', 'next Unity after the bundle ended');
});

test('unity: a same-name Unity that is not a descendant is not merged into the running bundle', { timeout: 120_000 }, async t => {
  const product = await loadProduct();
  const scenario = await scenarioFor(t, 'unity-same-name');
  const holder = await admitted(product, scenario, 'own-unity', { name: 'Unity.exe' });
  const external = await startBystander(scenario, 'other-unity', 'Unity.exe');
  assertResult(await finish(scenario, holder), 'succeeded', 'own Unity ends without waiting for another root');
  assert.ok(isAlive(external.pid) && isRunning(scenario.recordDir, 'other-unity'), 'the other root is untouched');
  const next = await attempt(product, scenario, 'next', { waitTimeoutMs: 0 });
  assertNotAdmitted(next, 'blocked');
});

// ---------------------------------------------------------------------------------------------
// Waiting priority (contract "Admission과 관측" 2).

async function startWaiter(product, scenario, label, options) {
  const executionObject = await execution(scenario, label, { waitTimeoutMs: 60_000, ...options });
  const requestPath = await writeRequest(scenario, label, runRequest(scenario, executionObject));
  const pending = runInProcess(product, scenario, requestPath);
  await sleep(REGISTRATION_SETTLE_MS);
  return { label, pending };
}

async function nextStarted(scenario, waiters) {
  await waitFor(() => waiters.some(waiter => startedRecord(scenario.recordDir, waiter.label)),
    { timeoutMs: 30_000, what: 'a waiting run to start' });
  await sleep(300);
  return waiters.filter(waiter => startedRecord(scenario.recordDir, waiter.label));
}

async function assertAdmissionOrder(scenario, holder, waiters, expectedOrder) {
  assert.ok(isRunning(scenario.recordDir, holder.label), 'the running holder is not preempted by waiting runs');
  assert.ok(waiters.every(waiter => !startedRecord(scenario.recordDir, waiter.label)),
    'nothing starts beside the holder');
  assertResult(await finish(scenario, holder), 'succeeded', holder.label);
  let remaining = [...waiters];
  for (const expected of expectedOrder) {
    const started = await nextStarted(scenario, remaining);
    assert.deepEqual(started.map(waiter => waiter.label), [expected], `admitted next: ${expected}`);
    const run = remaining.find(waiter => waiter.label === expected);
    assertResult(await finish(scenario, run), 'succeeded', expected);
    remaining = remaining.filter(waiter => waiter !== run);
  }
}

test('unity waits: Content goes before an earlier Core and equal priority keeps registration order', { timeout: 180_000 }, async t => {
  const product = await loadProduct();
  const scenario = await scenarioFor(t, 'unity-priority');
  const holder = await admitted(product, scenario, 'rules-unity', { part: 'Rules' });
  const waiters = [
    await startWaiter(product, scenario, 'core-first', { part: 'Core' }),
    await startWaiter(product, scenario, 'content-second', { part: 'Content' }),
    await startWaiter(product, scenario, 'content-third', { part: 'Content' }),
  ];
  await assertAdmissionOrder(scenario, holder, waiters, ['content-second', 'content-third', 'core-first']);
});

test('wsl waits: Core goes before an earlier other part and the running WSL is not preempted', { timeout: 180_000 }, async t => {
  const product = await loadProduct();
  const scenario = await scenarioFor(t, 'wsl-priority');
  const wsl = { kind: 'wsl', name: 'wsl.exe' };
  const holder = await admitted(product, scenario, 'content-wsl', { ...wsl, part: 'Content' });
  const waiters = [
    await startWaiter(product, scenario, 'rules-first', { ...wsl, part: 'Rules' }),
    await startWaiter(product, scenario, 'core-second', { ...wsl, part: 'Core' }),
    await startWaiter(product, scenario, 'management-third', { ...wsl, part: 'Management' }),
  ];
  await assertAdmissionOrder(scenario, holder, waiters, ['core-second', 'rules-first', 'management-third']);
});

// ---------------------------------------------------------------------------------------------
// WSL admission (contract "Admission과 관측" 3; v1.1 Q5, Q6).

test('wsl: one coordinated WSL command at a time', { timeout: 120_000 }, async t => {
  const product = await loadProduct();
  const scenario = await scenarioFor(t, 'wsl-one');
  const holder = await admitted(product, scenario, 'core-wsl', { kind: 'wsl', part: 'Core', name: 'node.exe' });
  const second = await attempt(product, scenario, 'second-core-wsl',
    { kind: 'wsl', part: 'Core', name: 'node.exe', waitTimeoutMs: 0 });
  assertNotAdmitted(second, 'blocked');
  assertResult(await finish(scenario, holder), 'succeeded', 'holder');
});

test('wsl: an observed WSL client of unknown owner blocks a new WSL run and is not terminated', async t => {
  const product = await loadProduct();
  const scenario = await scenarioFor(t, 'wsl-unknown-owner');
  for (const [index, name] of ['wsl.exe', 'WSL.EXE'].entries()) {
    const external = await startBystander(scenario, `external-${index}`, name);
    const run = await attempt(product, scenario, `core-wsl-${index}`,
      { kind: 'wsl', part: 'Core', name: 'node.exe', waitTimeoutMs: 0 });
    assertNotAdmitted(run, 'blocked');
    assertInjectedObservation(run.result, `external ${name}`);
    assert.ok(isAlive(external.pid) && isRunning(scenario.recordDir, `external-${index}`), `${name} untouched`);
    await release(scenario, `external-${index}`);
    await waitEnded(scenario, `external-${index}`);
  }
});

test('wsl: WSL VM residency processes are not counted as a running command', { timeout: 120_000 }, async t => {
  const product = await loadProduct();
  const scenario = await scenarioFor(t, 'wsl-vm-residency');
  for (const name of ['vmmem', 'VmmemWSL', 'wslservice.exe']) await startBystander(scenario, `vm-${name}`, name);
  const run = await admitted(product, scenario, 'core-wsl',
    { kind: 'wsl', part: 'Core', name: 'node.exe', waitTimeoutMs: 0 });
  assertResult(await finish(scenario, run), 'succeeded', 'WSL beside VM residency');
});

// ---------------------------------------------------------------------------------------------
// Memory gate of Core WSL + Unity (contract "Admission과 관측" 4, 5; goal completion 3; v1.1 Q6).

function memory(physicalAvailableBytes, commitAvailableBytes) {
  return { physicalAvailableBytes, commitAvailableBytes };
}

// The gate decision keeps its observation: source and time in the result, and when a run folder
// exists the observed byte counts are found in it.
async function assertGateObservation(result, physical, commit, label) {
  assertInjectedObservation(result, label);
  if (result.evidenceDir === null) return;
  const text = evidenceText(await evidenceFiles(result.evidenceDir));
  for (const value of [physical, commit]) {
    assert.ok(text.includes(String(value)), `${label}: observed ${value} bytes kept in ${result.evidenceDir}`);
  }
}

async function checkSecondAdmission(product, scenario, label, options, physical, commit, expectAdmitted) {
  setMachine(scenario, { memory: memory(physical, commit) });
  const run = await attempt(product, scenario, label, { waitTimeoutMs: 0, ...options });
  const caseName = `${label} physical ${physical} commit ${commit}`;
  if (expectAdmitted) {
    assert.ok(run.admitted, `${caseName}: admitted: ${JSON.stringify(run.result)}`);
    const result = await finish(scenario, run);
    assertResult(result, 'succeeded', caseName);
    assertRunEvidence(result, scenario.evidenceDir, caseName);
    await assertGateObservation(result, physical, commit, caseName);
    return null;
  }
  assertNotAdmitted(run, 'blocked');
  await assertGateObservation(run.result, physical, commit, caseName);
  return run.result.code;
}

const GATE_CASES = [
  // physical, commit, admitted
  [PHYSICAL_GATE - 1, COMMIT_GATE, false],
  [PHYSICAL_GATE, COMMIT_GATE, true],
  [PHYSICAL_GATE + 1, COMMIT_GATE, true],
  [PHYSICAL_GATE, COMMIT_GATE - 1, false],
  [PHYSICAL_GATE, COMMIT_GATE + 1, true],
  [PHYSICAL_GATE - 1, 64 * GIB, false],
  [64 * GIB, COMMIT_GATE - 1, false],
];

test('memory gate: Core WSL beside a running Unity needs physical >= 6GiB and commit >= 4GiB', { timeout: 300_000 }, async t => {
  const product = await loadProduct();
  const scenario = await scenarioFor(t, 'gate-wsl-after-unity');
  const holder = await admitted(product, scenario, 'content-unity', { part: 'Content' });
  let memoryCode = null;
  let index = 0;
  for (const [physical, commit, expectAdmitted] of GATE_CASES) {
    index += 1;
    const code = await checkSecondAdmission(product, scenario, `core-wsl-${index}`,
      { kind: 'wsl', part: 'Core', name: 'wsl.exe' }, physical, commit, expectAdmitted);
    memoryCode ??= code;
  }
  setMachine(scenario, { memory: ampleMachine().memory });
  const conflict = await attempt(product, scenario, 'second-unity', { part: 'Content', waitTimeoutMs: 0 });
  assertNotAdmitted(conflict, 'blocked');
  assert.notEqual(conflict.result.code, memoryCode, 'a memory block and a Unity conflict have different codes');
  assertResult(await finish(scenario, holder), 'succeeded', 'holder');
});

test('memory gate: Unity beside a running Core WSL applies the same gate', { timeout: 300_000 }, async t => {
  const product = await loadProduct();
  const scenario = await scenarioFor(t, 'gate-unity-after-wsl');
  const holder = await admitted(product, scenario, 'core-wsl', { kind: 'wsl', part: 'Core', name: 'wsl.exe' });
  let index = 0;
  for (const [physical, commit, expectAdmitted] of GATE_CASES.slice(0, 4)) {
    index += 1;
    await checkSecondAdmission(product, scenario, `content-unity-${index}`,
      { part: 'Content' }, physical, commit, expectAdmitted);
  }
  setMachine(scenario, { memory: ampleMachine().memory });
  assertResult(await finish(scenario, holder), 'succeeded', 'holder');
});

test('memory gate: other-part WSL and Unity are serialized even with ample memory', { timeout: 180_000 }, async t => {
  const product = await loadProduct();
  const scenario = await scenarioFor(t, 'gate-other-part');
  const unity = await admitted(product, scenario, 'content-unity', { part: 'Content' });
  const rulesWsl = await attempt(product, scenario, 'rules-wsl',
    { kind: 'wsl', part: 'Rules', name: 'wsl.exe', waitTimeoutMs: 0 });
  assertNotAdmitted(rulesWsl, 'blocked');
  assertResult(await finish(scenario, unity), 'succeeded', 'unity holder');

  const wsl = await admitted(product, scenario, 'content-wsl', { kind: 'wsl', part: 'Content', name: 'wsl.exe' });
  const laterUnity = await attempt(product, scenario, 'later-unity', { part: 'Content', waitTimeoutMs: 0 });
  assertNotAdmitted(laterUnity, 'blocked');
  assertResult(await finish(scenario, wsl), 'succeeded', 'wsl holder');
});

test('memory gate: a solo run has no memory threshold', { timeout: 120_000 }, async t => {
  const product = await loadProduct();
  const scenario = await scenarioFor(t, 'gate-solo');
  setMachine(scenario, { memory: memory(1, 1) });
  const unity = await admitted(product, scenario, 'solo-unity', { part: 'Content', waitTimeoutMs: 0 });
  assertResult(await finish(scenario, unity), 'succeeded', 'solo Unity with 1 byte free');
  const wsl = await admitted(product, scenario, 'solo-wsl',
    { kind: 'wsl', part: 'Core', name: 'wsl.exe', waitTimeoutMs: 0 });
  assertResult(await finish(scenario, wsl), 'succeeded', 'solo WSL with 1 byte free');
});

test('memory gate: a failed or invalid observation is tool-error/2 and never admission', { timeout: 300_000 }, async t => {
  const product = await loadProduct();
  const scenario = await scenarioFor(t, 'gate-invalid-observation');
  const holder = await admitted(product, scenario, 'content-unity', { part: 'Content' });
  // In-process only: NaN and Infinity cannot pass through the bootstrap's JSON machine file.
  const invalid = {
    'negative physical': { memory: memory(-1, 64 * GIB) },
    'NaN physical': { memory: memory(Number.NaN, 64 * GIB) },
    'infinite commit': { memory: memory(64 * GIB, Number.POSITIVE_INFINITY) },
    'physical as string': { memory: memory(String(64 * GIB), 64 * GIB) },
    'missing commit': { memory: { physicalAvailableBytes: 64 * GIB } },
    'incomplete observation': { complete: false },
    'observation throws': { failure: 'throw' },
  };
  let index = 0;
  for (const [label, change] of Object.entries(invalid)) {
    index += 1;
    scenario.machine = { ...ampleMachine(), ...change };
    const run = await attempt(product, scenario, `core-wsl-${index}`,
      { kind: 'wsl', part: 'Core', name: 'wsl.exe', waitTimeoutMs: 0 });
    scenario.machine = ampleMachine();
    assertNotAdmitted(run, 'tool-error');
    assert.equal(run.result.observationSource, 'injected', `${label}: the source that failed is recorded`);
  }
  assertKnownResult(await finish(scenario, holder), 'holder');
});

test('observation: a hanging observation ends in tool-error/2 within a finite time', { timeout: 200_000 }, async t => {
  const product = await loadProduct();
  const scenario = await scenarioFor(t, 'observation-hang');
  scenario.machine = { ...ampleMachine(), failure: 'hang' };
  const run = await attempt(product, scenario, 'solo-unity', { waitTimeoutMs: 0, decisionTimeoutMs: 180_000 });
  assertNotAdmitted(run, 'tool-error');
});

// ---------------------------------------------------------------------------------------------
// Execution, results and evidence (contract "실행 수명과 실패", "결과와 근거"; v1.1 Q2).

test('run: exit 0 is succeeded/0; argv and cwd reach the command; raw evidence stays in its run folder', { timeout: 120_000 }, async t => {
  await loadProduct();
  const scenario = await scenarioFor(t, 'run-success');
  // Distinct byte counts so the observation is recognisable in the evidence.
  const physical = 50 * GIB + 123;
  const commit = 40 * GIB + 321;
  setMachine(scenario, { memory: memory(physical, commit) });
  const argvMark = 'argv-mark-0815';
  const executionObject = await execution(scenario, 'command', {
    hold: false,
    args: [...TRICKY_ARGS, argvMark],
    stdout: 'CHILD-STDOUT-MARK',
    stderr: 'CHILD-STDERR-MARK',
  });
  const requestPath = await writeRequest(scenario, 'command', runRequest(scenario, executionObject));
  const requestBytes = await readFile(requestPath);
  const runner = await startBootstrap(scenario, 'runner', requestPath, { cwd: repos.isolated.root });
  const { result, stdout, stderr } = await bootstrapResult(runner, 'runner');

  assertResult(result, 'succeeded', 'exit 0');
  assert.equal(result.commandExitCode, 0);
  assert.equal(typeof result.requestId, 'string');
  assert.ok(result.requestId.length > 0);
  assertInjectedObservation(result, 'run');
  assertLocation(result, { commonDir: repos.isolated.commonDir, stateRoot: scenario.stateRoot }, 'run');
  for (const mark of ['CHILD-STDOUT-MARK', 'CHILD-STDERR-MARK']) {
    assert.ok(!stdout.includes(mark) && !stderr.includes(mark), `${mark} is not mixed into the runner's output`);
  }

  const command = startedRecord(scenario.recordDir, 'command');
  assert.deepEqual(command.argv, [...TRICKY_ARGS, argvMark], 'argv reaches the command unchanged, without a shell');
  assert.equal(canonicalPath(command.cwd), canonicalPath(scenario.workDir));

  const runFolder = assertRunEvidence(result, scenario.evidenceDir, 'run');
  const created = await directoriesIn(scenario.evidenceDir);
  assert.equal(created.length, 1, `one new run folder under evidenceDir: ${created}`);
  const runArea = canonicalPath(created[0]);
  assert.ok(canonicalPath(runFolder) === runArea || canonicalPath(runFolder).startsWith(runArea + sep),
    'the result names the new run folder');
  const beside = (await listFiles(scenario.evidenceDir)).filter(path => !canonicalPath(path).startsWith(runArea + sep));
  assert.deepEqual(beside, [], 'nothing written beside the run folder');
  const files = await evidenceFiles(runFolder);
  const text = file => file.bytes.toString('utf8');
  assert.ok(files.some(file => text(file).includes('CHILD-STDOUT-MARK') && !text(file).includes('CHILD-STDERR-MARK')),
    'child stdout in its own evidence file');
  assert.ok(files.some(file => text(file).includes('CHILD-STDERR-MARK') && !text(file).includes('CHILD-STDOUT-MARK')),
    'child stderr in its own evidence file');
  assert.ok(files.some(file => file.bytes.equals(requestBytes) || text(file).includes(sha256(requestBytes))),
    'request bytes or their SHA-256 in the evidence');
  const allText = evidenceText(files);
  assert.ok(allText.includes(argvMark), 'the command argv is in the evidence');
  assert.ok(allText.includes('injected'), 'the evidence says its observation was injected');
  for (const value of [physical, commit]) {
    assert.ok(allText.includes(String(value)), `observed ${value} bytes in the evidence`);
  }
});

test('run: nonzero 1~255 is failed with that exit and commandExitCode', { timeout: 120_000 }, async t => {
  const product = await loadProduct();
  const scenario = await scenarioFor(t, 'run-nonzero');
  for (const code of [1, 7, 255]) {
    const executionObject = await execution(scenario, `exit-${code}`, { hold: false, exitCode: code });
    const requestPath = await writeRequest(scenario, `exit-${code}`, runRequest(scenario, executionObject));
    const result = await withTimeout(runInProcess(product, scenario, requestPath), 60_000, `exit ${code}`);
    assertResult(result, 'failed', `exit ${code}`);
    assert.equal(result.exitCode, code, `exit ${code}: wrapper exit`);
    assert.equal(result.commandExitCode, code, `exit ${code}: command exit`);
    assertRunEvidence(result, scenario.evidenceDir, `exit ${code}`);
  }
});

test('run: a command exit outside 1~255 is wrapper 1 with the original in commandExitCode',
  { timeout: 120_000, skip: process.platform !== 'win32' && 'POSIX truncates exit codes to 0~255' }, async t => {
    const product = await loadProduct();
    const scenario = await scenarioFor(t, 'run-wide-exit');
    const executionObject = await execution(scenario, 'exit-300', { hold: false, exitCode: 300 });
    const requestPath = await writeRequest(scenario, 'exit-300', runRequest(scenario, executionObject));
    const result = await withTimeout(runInProcess(product, scenario, requestPath), 60_000, 'exit 300');
    assertResult(result, 'failed', 'exit 300');
    assert.equal(result.exitCode, 1);
    assert.equal(result.commandExitCode, 300);
  });

test('run: two runs into one evidenceDir make separate run folders and keep the first intact', { timeout: 120_000 }, async t => {
  const product = await loadProduct();
  const scenario = await scenarioFor(t, 'run-evidence-folders');
  const runOnce = async label => {
    const executionObject = await execution(scenario, label, { hold: false, stdout: label });
    const requestPath = await writeRequest(scenario, label, runRequest(scenario, executionObject));
    return withTimeout(runInProcess(product, scenario, requestPath), 60_000, label);
  };
  const first = await runOnce('first');
  assertResult(first, 'succeeded', 'first');
  const firstFiles = await snapshot(scenario.evidenceDir);
  const second = await runOnce('second');
  assertResult(second, 'succeeded', 'second');
  assert.notDeepEqual(second.requestId, first.requestId);
  assert.notDeepEqual(second.executionId, first.executionId);
  assert.notEqual(canonicalPath(assertRunEvidence(second, scenario.evidenceDir, 'second')),
    canonicalPath(assertRunEvidence(first, scenario.evidenceDir, 'first')), 'each run has its own folder');
  const afterFiles = await snapshot(scenario.evidenceDir);
  for (const [path, hash] of firstFiles) assert.equal(afterFiles.get(path), hash, `${path} is not overwritten`);
});

test('run: a start failure is a nonzero result and releases its own reservation', { timeout: 120_000 }, async t => {
  const product = await loadProduct();
  const scenario = await scenarioFor(t, 'run-start-failure');
  const notExecutable = join(scenario.workDir, 'not executable.txt');
  await writeFile(notExecutable, 'plain text');
  const request = runRequest(scenario, await execution(scenario, 'broken', { hold: false }));
  request.execution.file = notExecutable;
  request.execution.args = [];
  const requestPath = await writeRequest(scenario, 'broken', request);
  const result = await withTimeout(runInProcess(product, scenario, requestPath), 60_000, 'start failure');
  assertKnownResult(result, 'start failure');
  assert.notEqual(result.exitCode, 0, 'a command that could not start is not success');
  assert.ok(!['succeeded', 'observed'].includes(result.status), `start failure status: ${result.status}`);
  const next = await admitted(product, scenario, 'next', { waitTimeoutMs: 0 });
  assertResult(await finish(scenario, next), 'succeeded', 'next run after the start failure');
});

test('run: timeout is timed-out/124, terminates its own command only and frees the lock', { timeout: 120_000 }, async t => {
  const product = await loadProduct();
  const scenario = await scenarioFor(t, 'run-timeout');
  const bystander = await startBystander(scenario, 'bystander', 'node.exe');
  const executionObject = await execution(scenario, 'slow', { name: 'node.exe', timeoutMs: 1500 });
  const requestPath = await writeRequest(scenario, 'slow', runRequest(scenario, executionObject));
  const result = await withTimeout(runInProcess(product, scenario, requestPath), 60_000, 'timed-out run');
  assertResult(result, 'timed-out', 'timeout');
  const command = startedRecord(scenario.recordDir, 'slow');
  assert.ok(command, 'the command had started');
  await waitFor(() => !isAlive(command.pid), { timeoutMs: 10_000, what: 'the timed-out command to be terminated' });
  assert.ok(isAlive(bystander.pid) && isRunning(scenario.recordDir, 'bystander'), 'an unrelated process is untouched');
  const next = await admitted(product, scenario, 'next', { waitTimeoutMs: 0 });
  assertResult(await finish(scenario, next), 'succeeded', 'next run after the timeout');
});

test('cancel during a run: cancelled/130, own processes handled, the lock follows the live bundle', { timeout: 120_000 }, async t => {
  const product = await loadProduct();
  const scenario = await scenarioFor(t, 'cancel-running');
  const bystander = await startBystander(scenario, 'bystander', 'node.exe');
  const importSpec = await writeSpec(scenario, 'import', { name: 'node.exe' });
  const executionObject = await execution(scenario, 'root', { name: 'node.exe', grandchild: importSpec });
  const requestPath = await writeRequest(scenario, 'root', runRequest(scenario, executionObject));
  const controller = new AbortController();
  const pending = runInProcess(product, scenario, requestPath, { signal: controller.signal });
  await waitFor(() => startedRecord(scenario.recordDir, 'import'), { what: 'root and import child to start' });
  await sleep(REGISTRATION_SETTLE_MS);
  controller.abort();
  assertResult(await withTimeout(pending, 30_000, 'cancelled run'), 'cancelled', 'cancel');
  const root = startedRecord(scenario.recordDir, 'root');
  await waitFor(() => !isAlive(root.pid), { timeoutMs: 10_000, what: 'the cancelled root to be terminated' });
  assert.ok(isAlive(bystander.pid) && isRunning(scenario.recordDir, 'bystander'), 'unrelated process untouched');

  // Whether the confirmed import child is terminated or kept as residue, the lock must follow it.
  const probe = await attempt(product, scenario, 'probe', { name: 'node.exe', waitTimeoutMs: 0 });
  if (isRunning(scenario.recordDir, 'import')) {
    assert.equal(probe.admitted, false, 'a live import child keeps the lock after cancel');
  } else {
    assert.ok(probe.admitted, `no live process of the bundle: the lock is free: ${JSON.stringify(probe.result)}`);
    assertResult(await finish(scenario, probe), 'succeeded', 'probe');
  }
});

test('cancel while waiting: cancelled/130, only its own wait is removed and the holder is untouched', { timeout: 120_000 }, async t => {
  const product = await loadProduct();
  const scenario = await scenarioFor(t, 'cancel-waiting');
  const holder = await admitted(product, scenario, 'holder', { part: 'Rules' });
  const controller = new AbortController();
  const waiting = await execution(scenario, 'content-waiter', { part: 'Content', waitTimeoutMs: 60_000 });
  const waitingRequest = await writeRequest(scenario, 'content-waiter', runRequest(scenario, waiting));
  const pending = runInProcess(product, scenario, waitingRequest, { signal: controller.signal });
  await sleep(REGISTRATION_SETTLE_MS);
  controller.abort();
  assertResult(await withTimeout(pending, 15_000, 'cancelled wait'), 'cancelled', 'cancel while waiting');
  assert.equal(startedRecord(scenario.recordDir, 'content-waiter'), null);
  assert.ok(isRunning(scenario.recordDir, 'holder'), 'the holder keeps running');

  const preCancelled = await execution(scenario, 'pre-cancelled', { part: 'Content' });
  const preRequest = await writeRequest(scenario, 'pre-cancelled', runRequest(scenario, preCancelled));
  const preResult = await runInProcess(product, scenario, preRequest, { signal: AbortSignal.abort() });
  assertResult(preResult, 'cancelled', 'already-cancelled request');
  assert.equal(startedRecord(scenario.recordDir, 'pre-cancelled'), null);

  assertResult(await finish(scenario, holder), 'succeeded', 'holder');
  // A Core request is admitted only if the cancelled Content wait is gone.
  const core = await admitted(product, scenario, 'core-after', { part: 'Core', waitTimeoutMs: 10_000 });
  assertResult(await finish(scenario, core), 'succeeded', 'Core after the cancelled Content wait');
});

test('wait expiry: blocked/1 after waitTimeoutMs and its wait no longer outranks others', { timeout: 120_000 }, async t => {
  const product = await loadProduct();
  const scenario = await scenarioFor(t, 'wait-expiry');
  const holder = await admitted(product, scenario, 'holder', { part: 'Rules' });
  const immediate = await attempt(product, scenario, 'content-immediate', { part: 'Content', waitTimeoutMs: 0 });
  assertNotAdmitted(immediate, 'blocked');
  const startedAt = Date.now();
  const expired = await attempt(product, scenario, 'content-waiter', { part: 'Content', waitTimeoutMs: 800 });
  const waitedMs = Date.now() - startedAt;
  assertNotAdmitted(expired, 'blocked');
  assert.ok(waitedMs >= 750, `waited about waitTimeoutMs before giving up: ${waitedMs} ms`);
  assert.ok(isRunning(scenario.recordDir, 'holder'), 'the holder keeps running');
  assertResult(await finish(scenario, holder), 'succeeded', 'holder');
  const core = await admitted(product, scenario, 'core-after', { part: 'Core', waitTimeoutMs: 10_000 });
  assertResult(await finish(scenario, core), 'succeeded', 'Core after the expired Content wait');
});

// ---------------------------------------------------------------------------------------------
// Residual state, owner loss and PID reuse (contract "공용 상태와 원자성", "실행 수명과 실패"; v1.1 Q3).

// Starts a run in a separate runner process, then kills that runner (our own process, identified by
// the handle that started it). Whether the root command dies with its runner depends on how the
// product starts it (a non-detached Node child dies with its parent on Windows), so the bundle
// that must outlive the owner is the import child, which the root starts detached. Without an
// import child the root is ended as well, leaving a state whose processes are all gone.
async function startLostOwner(scenario, label, { repoRoot, cwd, importChild = true } = {}) {
  const importLabel = `${label}-import`;
  const grandchild = importChild ? await writeSpec(scenario, importLabel, { name: 'node.exe' }) : null;
  const executionObject = await execution(scenario, label, { name: 'node.exe', grandchild });
  const requestPath = await writeRequest(scenario, label, runRequest(scenario, executionObject, repoRoot));
  const owner = await startBootstrap(scenario, `${label}-runner`, requestPath, { cwd });
  const root = await waitFor(() => startedRecord(scenario.recordDir, label), { what: `${label} to start` });
  if (importChild) {
    await waitFor(() => startedRecord(scenario.recordDir, importLabel), { what: `${importLabel} to start` });
  }
  await sleep(REGISTRATION_SETTLE_MS);
  const ownerRecord = startedRecord(scenario.recordDir, `${label}-runner`);
  if (ownerRecord.pid !== owner.child.pid) throw new FixtureError('bootstrap record does not match its process');
  owner.child.kill('SIGKILL');
  await withTimeout(owner.exited, 15_000, 'killed runner to exit');
  if (importChild && !isRunning(scenario.recordDir, importLabel)) {
    throw new FixtureError(`${importLabel} did not outlive the killed runner`);
  }
  if (!importChild) {
    await release(scenario, label);
    await waitFor(() => !isAlive(root.pid), { timeoutMs: 15_000, what: `${label} to end` });
  }
  return { root, importLabel: importChild ? importLabel : null };
}

async function endLostOwner(scenario, label, lost) {
  await release(scenario, label);
  if (lost.importLabel) await release(scenario, lost.importLabel);
  await waitFor(() => !isAlive(lost.root.pid), { timeoutMs: 15_000, what: `${label} to end` });
  if (lost.importLabel) await waitEnded(scenario, lost.importLabel);
}

test('owner loss: a killed runner whose bundle lives keeps the lock; a proven end frees it', { timeout: 120_000 }, async t => {
  const product = await loadProduct();
  const scenario = await scenarioFor(t, 'owner-loss');
  const lost = await startLostOwner(scenario, 'orphan');
  const probe = await attempt(product, scenario, 'probe', { name: 'node.exe', waitTimeoutMs: 0 });
  assertNotAdmitted(probe, 'blocked');
  assert.ok(isRunning(scenario.recordDir, lost.importLabel),
    'the lost owner\'s bundle is not terminated by a new request');

  await endLostOwner(scenario, 'orphan', lost);
  // v1.1 Q3: the end proven by a complete observation lets a new request reclaim the state.
  const next = await admitted(product, scenario, 'next', { name: 'node.exe', waitTimeoutMs: 10_000 });
  assertResult(await finish(scenario, next), 'succeeded', 'next run after the proven end');
});

test('PID reuse: the recorded PID with another start identity is blocked/1 and the state is kept', { timeout: 120_000 }, async t => {
  const product = await loadProduct();
  const scenario = await scenarioFor(t, 'pid-reuse');
  // A plain conflict first, for a code to compare with.
  const holder = await admitted(product, scenario, 'holder', { name: 'node.exe' });
  const busy = await attempt(product, scenario, 'probe-busy', { name: 'node.exe', waitTimeoutMs: 0 });
  assertNotAdmitted(busy, 'blocked');
  assertResult(await finish(scenario, holder), 'succeeded', 'holder');

  const lost = await startLostOwner(scenario, 'orphan', { importChild: false });
  // The recorded root PID now reported as a different incarnation (a synthetic entry; see
  // lock-fixture.mjs for why it can never stand for a real process).
  const reusedAt = new Date(Date.parse(lost.root.startedAt) + 60 * 60 * 1000).toISOString();
  setMachine(scenario, {
    syntheticProcesses: [{ pid: lost.root.pid, parentPid: process.pid, startedAt: reusedAt, name: 'unrelated.exe' }],
  });
  const kept = await snapshot(scenario.stateRoot);
  const reused = await attempt(product, scenario, 'probe-reused', { name: 'node.exe', waitTimeoutMs: 0 });
  if (scenario.observation.refusals.length > 0) throw new FixtureError('the recorded PID was taken by a real process');
  assertNotAdmitted(reused, 'blocked');
  assert.notEqual(reused.result.code, busy.result.code, 'an identity mismatch has its own code');
  const after = await snapshot(scenario.stateRoot);
  for (const [path, hash] of kept) assert.equal(after.get(path), hash, `${path} kept, not reclaimed or repaired`);

  // Once the PID is not observed at all, the end is proven and the state is reclaimed.
  setMachine(scenario, { syntheticProcesses: [] });
  const next = await admitted(product, scenario, 'next', { name: 'node.exe', waitTimeoutMs: 10_000 });
  assertResult(await finish(scenario, next), 'succeeded', 'next run after the PID is gone');
});

test('damaged state: inspect and run are tool-error/2, nothing starts and the damage is preserved', { timeout: 120_000 }, async t => {
  const product = await loadProduct();
  const scenario = await scenarioFor(t, 'damaged-state');
  // A lost owner leaves a state that no live runner rewrites while it is damaged.
  const lost = await startLostOwner(scenario, 'orphan');
  const files = await listFiles(scenario.stateRoot);
  assert.ok(files.length > 0, 'a running execution is kept in the shared state on disk');
  const originals = new Map();
  const damage = '{"damaged":';
  for (const path of files) {
    originals.set(path, await readFile(path));
    try {
      await writeFile(path, damage);
    } catch (error) {
      throw new FixtureError(`could not damage ${path}: ${error.code}`);
    }
  }
  const inspect = await writeRequest(scenario, 'inspect', inspectRequest());
  assertResult(await runInProcess(product, scenario, inspect), 'tool-error', 'inspect of damaged state');
  const run = await attempt(product, scenario, 'probe', { name: 'node.exe', waitTimeoutMs: 0 });
  assertNotAdmitted(run, 'tool-error');
  for (const path of files) {
    assert.ok(existsSync(path), `${path} preserved, not deleted`);
    assert.equal(await readFile(path, 'utf8'), damage, `${path} preserved, not repaired`);
  }
  assert.ok(isRunning(scenario.recordDir, lost.importLabel), 'the recorded bundle is not terminated');
  for (const [path, bytes] of originals) await writeFile(path, bytes);
  await endLostOwner(scenario, 'orphan', lost);
});

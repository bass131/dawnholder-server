// Independent regressions for how a runner settles a command it has started: when another request
// reclaims the ended bundle first, when the ended root's PID is reused before the runner settles, when
// an observation fails right after spawn, and when a confirmed child outlives a timed-out root.
// Expected outcomes come from the contract, not from the product:
// - contract v1 (E/pr2-execution-contract-v1.md, SHA256 b43a07ff…e9aae2) "결과와 근거": exit 0 is
//   succeeded/0, timeout is timed-out/124, cancel is cancelled/130, blocked/1 means not started,
//   tool-error/2 is an observation/state/tool failure; "실행 수명과 실패": the runner keeps its lease
//   while the execution lasts, keeps it while a confirmed child of the bundle lives, and the end
//   proven by PID + start identity frees the state; a new request may reclaim a proven-ended bundle;
// - supplement v1.1 (b26bf807…75a5e542) Q2 commandExitCode is the real child exit when known; Q4 the
//   lock is free once the bundle end is proven; the reuse premise is that of the first independent
//   verdict's defect 1 (E/pr2-verify/verdict.md): a runner that ended its own childless root has proven
//   the end itself, so a reuse of that PID must not keep the lock.
//
// Fidelity: observations are injected (lock-fixture.mjs and its limits). "Native-like" only stretches
// an observation to the order of a Windows CIM query. A reused PID is a synthetic entry added by this
// file's observation wrapper only while the ended root's PID is not alive; no real OS PID reuse.
//
// Environment: EXECUTION_LOCK_TESTS_WORK, an existing absolute folder (kept after the run).
import assert from 'node:assert/strict';
import { join } from 'node:path';
import { before, test } from 'node:test';

import {
  createRepository,
  createScenario,
  endedRecord,
  execution,
  isAlive,
  isRunning,
  loadProduct,
  release,
  runFolder,
  sleep,
  startedRecord,
  stopScenario,
  waitFor,
  writeRequest,
  writeSpec,
} from './lock-fixture.mjs';

// Contract "결과와 근거" status / wrapper exit table.
const STATUS_EXIT = { succeeded: 0, blocked: 1, 'tool-error': 2, 'timed-out': 124, cancelled: 130 };

// The order of one native Windows CIM process/memory query (about 0.35 s in the raw native runs).
const NATIVE_LIKE_OBSERVE_MS = 350;

// Time for a started run to identify its root and for a waiting run to register.
const SETTLE_MS = 1000;

const HOUR_MS = 60 * 60 * 1000;

// Parent of the synthetic reusing process: a PID that no live process holds.
const UNRELATED_PARENT_PID = 4_194_220;

let repository;
before(async () => {
  repository = await createRepository(join(await runFolder(), 'repo settlement'));
});

async function scenarioFor(t, name) {
  const scenario = await createScenario(name);
  t.after(async () => {
    const residue = await stopScenario(scenario);
    if (residue.length > 0) t.diagnostic(`fixture commands left to their hold limit: ${residue.join(', ')}`);
  });
  return scenario;
}

function runRequest(scenario, executionObject) {
  return {
    version: 1,
    action: 'run',
    repoRoot: repository.root,
    evidenceDir: scenario.evidenceDir,
    execution: executionObject,
  };
}

function runCli(product, scenario, requestPath, { observe = scenario.observation.observe, signal } = {}) {
  const dependencies = { observe, stateRoot: scenario.stateRoot };
  if (signal) dependencies.signal = signal;
  return product.runCli([requestPath], dependencies);
}

function delayedObserve(observe, delayMs) {
  return async () => {
    await sleep(delayMs);
    return observe();
  };
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

function assertStatus(result, status, label) {
  assert.ok(result && typeof result === 'object', `${label}: result object`);
  assert.equal(result.status, status, `${label}: ${JSON.stringify(result)}`);
  assert.equal(result.exitCode, STATUS_EXIT[status], `${label}: exitCode`);
}

async function startRun(product, scenario, label, executionOptions, runOptions = {}) {
  const executionObject = await execution(scenario, label, executionOptions);
  const requestPath = await writeRequest(scenario, label, runRequest(scenario, executionObject));
  return runCli(product, scenario, requestPath, runOptions);
}

// Starts one run and returns once its command started or the run settled without starting it.
async function attempt(product, scenario, label, executionOptions, runOptions = {}) {
  const pending = startRun(product, scenario, label, executionOptions, runOptions);
  let settled = null;
  pending.then(result => { settled = { result }; }, error => { settled = { error }; });
  await waitFor(() => settled || startedRecord(scenario.recordDir, label), {
    timeoutMs: 30_000,
    what: `${label} to start or settle`,
  });
  if (settled?.error) throw settled.error;
  return { label, pending, admitted: Boolean(startedRecord(scenario.recordDir, label)), result: settled?.result ?? null };
}

async function admittedAndFinished(product, scenario, label, runOptions = {}) {
  const run = await attempt(product, scenario, label, { name: 'Unity.exe', waitTimeoutMs: 10_000 }, runOptions);
  assert.ok(run.admitted, `${label}: the lock must be free: ${JSON.stringify(run.result)}`);
  await release(scenario, label);
  assertStatus(await withTimeout(run.pending, 30_000, `${label} to finish`), 'succeeded', label);
}

// ---------------------------------------------------------------------------------------------
// Another waiting request reclaims the interrupted holder's ended bundle before the holder settles.

async function interruptedWhileAnotherWaits(t, name, interrupt) {
  const product = await loadProduct();
  const scenario = await scenarioFor(t, name);
  const controller = new AbortController();
  // The holder observes at a native-like pace; the waiter polls with the plain injected observation.
  const holderPending = startRun(product, scenario, 'holder', {
    name: 'Unity.exe',
    timeoutMs: interrupt === 'timeout' ? 2500 : 120_000,
  }, {
    observe: delayedObserve(scenario.observation.observe, NATIVE_LIKE_OBSERVE_MS),
    signal: controller.signal,
  });
  await waitFor(() => startedRecord(scenario.recordDir, 'holder'), { what: 'the holder to start' });
  const waiterPending = startRun(product, scenario, 'waiter', { name: 'Unity.exe', waitTimeoutMs: 60_000 });
  await sleep(SETTLE_MS);
  assert.equal(startedRecord(scenario.recordDir, 'waiter'), null, `${name}: the waiter does not start beside the holder`);
  if (interrupt === 'cancel') controller.abort();

  const holder = await withTimeout(holderPending, 60_000, `${name}: the interrupted holder`);
  assertStatus(holder, interrupt === 'timeout' ? 'timed-out' : 'cancelled',
    `${name}: an interrupted holder reports its interruption even if a waiting request reclaimed the ended bundle`);
  await waitFor(() => startedRecord(scenario.recordDir, 'waiter'), {
    timeoutMs: 30_000,
    what: `${name}: the waiter to start after the holder's bundle ended`,
  });
  await release(scenario, 'waiter');
  assertStatus(await withTimeout(waiterPending, 30_000, `${name}: the waiter`), 'succeeded', `${name}: waiter`);
}

test('timeout while another request waits: the holder reports timed-out/124 and the waiter starts after it',
  { timeout: 240_000 }, async t => {
    for (const trial of [1, 2]) await interruptedWhileAnotherWaits(t, `timeout-waited-${trial}`, 'timeout');
  });

test('cancel while another request waits: the holder reports cancelled/130 and the waiter starts after it',
  { timeout: 240_000 }, async t => {
    for (const trial of [1, 2]) await interruptedWhileAnotherWaits(t, `cancel-waited-${trial}`, 'cancel');
  });

// ---------------------------------------------------------------------------------------------
// The ended root's PID is reused before the runner records the end.

// Lists a different, later incarnation at the root's PID as soon as that root is no longer alive.
function reusingObserve(scenario, rootLabel) {
  let reused = null;
  return async () => {
    const observation = await scenario.observation.observe();
    const root = startedRecord(scenario.recordDir, rootLabel);
    if (root && !isAlive(root.pid)) {
      reused ??= {
        pid: root.pid,
        parentPid: UNRELATED_PARENT_PID,
        startedAt: new Date(Date.parse(root.startedAt) + HOUR_MS).toISOString(),
        name: 'unrelated.exe',
      };
      observation.processes.push(reused);
    }
    return observation;
  };
}

async function rootReusedBeforeSettlement(t, name, ending) {
  const product = await loadProduct();
  const scenario = await scenarioFor(t, name);
  const observe = reusingObserve(scenario, 'root');
  const pending = startRun(product, scenario, 'root', {
    name: 'Unity.exe',
    timeoutMs: ending === 'timeout' ? 2500 : 120_000,
  }, { observe });
  await waitFor(() => startedRecord(scenario.recordDir, 'root'), { what: 'the root to start' });
  await sleep(SETTLE_MS);
  if (ending === 'exit') await release(scenario, 'root');
  const result = await withTimeout(pending, 60_000, `${name}: the run`);
  const root = startedRecord(scenario.recordDir, 'root');
  await waitFor(() => !isAlive(root.pid), { timeoutMs: 10_000, what: 'the root to be gone' });

  if (ending === 'exit') {
    assertStatus(result, 'succeeded', `${name}: a childless root that exited 0 is a succeeded run`);
    assert.equal(result.commandExitCode, 0, `${name}: commandExitCode is the real exit`);
  } else {
    assertStatus(result, 'timed-out', `${name}: a timed-out run reports timed-out`);
  }
  // The runner proved the end of its own childless root by its handle; the later incarnation at that
  // PID is another process and must not keep the lock.
  await admittedAndFinished(product, scenario, 'probe', { observe });
}

test('root PID reused right after a normal exit: the run is succeeded/0 and leaves no lease',
  { timeout: 120_000 }, async t => {
    await rootReusedBeforeSettlement(t, 'reused-after-exit', 'exit');
  });

test('root PID reused right after the runner ended it on timeout: the run is timed-out/124 and leaves no lease',
  { timeout: 120_000 }, async t => {
    await rootReusedBeforeSettlement(t, 'reused-after-timeout', 'timeout');
  });

// ---------------------------------------------------------------------------------------------
// Observation fails right after spawn, then recovers while the command is still running.

test('observation failing right after spawn: the started command is not reported as unstarted and the lock is freed after it',
  { timeout: 120_000 }, async t => {
    const product = await loadProduct();
    const scenario = await scenarioFor(t, 'observation-fails-after-spawn');
    let failuresLeft = 3;
    const flaky = async () => {
      if (startedRecord(scenario.recordDir, 'root') && failuresLeft > 0) {
        failuresLeft -= 1;
        throw new Error('Injected observation failure right after spawn (test seam).');
      }
      return scenario.observation.observe();
    };
    const pending = startRun(product, scenario, 'root', { name: 'Unity.exe' }, { observe: flaky });
    let settled = null;
    pending.then(result => { settled = result; }, () => {});
    const first = await waitFor(() => startedRecord(scenario.recordDir, 'root'), { what: 'the root to start' });
    await waitFor(() => failuresLeft === 0, { what: 'the injected failures to be consumed' });
    await sleep(SETTLE_MS);
    assert.ok(isRunning(scenario.recordDir, 'root'), 'the started command keeps running after the failures');
    await release(scenario, 'root');
    const result = settled ?? await withTimeout(pending, 60_000, 'the run');

    assert.notEqual(result.status, 'blocked', `a started command is never blocked (not started): ${JSON.stringify(result)}`);
    assert.ok(['tool-error', 'succeeded'].includes(result.status),
      `observation failure after the start is a tool failure or, once the end is proven, success: ${JSON.stringify(result)}`);
    assert.equal(result.exitCode, STATUS_EXIT[result.status], 'exit follows the status table');
    await waitFor(() => endedRecord(scenario.recordDir, 'root'), { what: 'the root to end' });
    assert.equal(result.commandExitCode, 0, `commandExitCode is the real exit of the started command: ${JSON.stringify(result)}`);
    assert.equal(startedRecord(scenario.recordDir, 'root').pid, first.pid, 'the command was started once, not respawned');
    await admittedAndFinished(product, scenario, 'probe');
  });

// ---------------------------------------------------------------------------------------------
// A confirmed child outlives the root that the runner ended on timeout.

test('timeout with a live import child: timed-out/124, the lock follows the child and is free after it ends',
  { timeout: 120_000 }, async t => {
    const product = await loadProduct();
    const scenario = await scenarioFor(t, 'timeout-live-child');
    const importSpec = await writeSpec(scenario, 'import', { name: 'Unity.exe' });
    const pending = startRun(product, scenario, 'root', { name: 'Unity.exe', timeoutMs: 2500, grandchild: importSpec });
    await waitFor(() => startedRecord(scenario.recordDir, 'import'), { what: 'root and import child to start' });
    assertStatus(await withTimeout(pending, 60_000, 'the timed-out run'), 'timed-out', 'timeout with a live child');
    const root = startedRecord(scenario.recordDir, 'root');
    await waitFor(() => !isAlive(root.pid), { timeoutMs: 10_000, what: 'the timed-out root to be ended' });

    // The contract lets the runner end a confirmed child or keep it; the lock must follow what lives.
    const probe = await attempt(product, scenario, 'probe', { name: 'Unity.exe', waitTimeoutMs: 0 });
    if (isRunning(scenario.recordDir, 'import')) {
      if (probe.admitted) await release(scenario, 'probe');
      assert.equal(probe.admitted, false, `a live confirmed child keeps the lease after the timeout: ${JSON.stringify(probe.result)}`);
      assertStatus(probe.result, 'blocked', 'probe beside the live child');
      await release(scenario, 'import');
      await waitFor(() => !isRunning(scenario.recordDir, 'import'), { what: 'the import child to end' });
    } else {
      assert.ok(probe.admitted, `no live process of the bundle: the lock is free: ${JSON.stringify(probe.result)}`);
      await release(scenario, 'probe');
      assertStatus(await probe.pending, 'succeeded', 'probe');
    }
    await admittedAndFinished(product, scenario, 'next');
  });

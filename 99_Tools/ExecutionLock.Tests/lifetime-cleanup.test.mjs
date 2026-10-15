// Independent regressions for the lifetime of a started execution: what the runner leaves in the
// shared state after it interrupts its own command, and how it reports a command that has started.
// Expected outcomes come from the execution contract, not from the product:
// - contract v1 (E/pr2-execution-contract-v1.md, SHA256 b43a07ff…e9aae2) "실행 수명과 실패":
//   the runner keeps its lease while waiting for the execution to end; a root or wrapper end
//   releases nothing only while a live child of the same bundle is confirmed; on cancel/timeout it
//   may end only the processes it started and confirmed;
// - the same contract "결과와 근거": blocked/1 means the command was not started (policy, conflict
//   or wait expiry); timed-out/124 and cancelled/130 report the actual cleanup separately;
// - supplement v1.1 (E/pr2-execution-contract-v1.1-supplement.md, SHA256 b26bf807…75a5e542) Q3: a
//   recorded PID seen with another start identity is blocked/1 and its state is kept.
// Together: a runner that ended its own root, with no other live process in the bundle, has proven
// the end itself, so a later reuse of that PID must not turn the finished run into a block.
//
// Observations are injected test values from lock-fixture.mjs (its fidelity limits apply); the
// delayed variant only stretches each observation to the order of a Windows CIM query.
//
// Environment: EXECUTION_LOCK_TESTS_WORK, an existing absolute folder (kept after the run).
import assert from 'node:assert/strict';
import { join } from 'node:path';
import { before, test } from 'node:test';

import {
  FixtureError,
  createRepository,
  createScenario,
  execution,
  isAlive,
  isRunning,
  loadProduct,
  release,
  runFolder,
  setMachine,
  sleep,
  startBootstrap,
  startedRecord,
  stopScenario,
  waitFor,
  writeRequest,
} from './lock-fixture.mjs';

// Contract "결과와 근거" status / wrapper exit table.
const STATUS_EXIT = { succeeded: 0, blocked: 1, 'timed-out': 124, cancelled: 130 };

// The order of one native Windows CIM process/memory query (about 0.35 s in the implementer's raw
// run-20261015T052453007Z events). Used only to stretch the injected observation.
const NATIVE_LIKE_OBSERVE_MS = 350;

// Time for a started run to settle (root identified, first polls done) before interrupting it.
const SETTLE_MS = 1000;

// An hour later than the recorded start: an unmistakably different incarnation of the same PID.
const REUSED_LATER_MS = 60 * 60 * 1000;

let repository;
before(async () => {
  repository = await createRepository(join(await runFolder(), 'repo lifetime'));
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

function delayedObserve(observe, delayMs) {
  if (delayMs === 0) return observe;
  return async () => {
    await sleep(delayMs);
    return observe();
  };
}

function runCli(product, scenario, requestPath, { observe = scenario.observation.observe, signal } = {}) {
  const dependencies = { observe, stateRoot: scenario.stateRoot };
  if (signal) dependencies.signal = signal;
  return product.runCli([requestPath], dependencies);
}

function assertStatus(result, status, label) {
  assert.ok(result && typeof result === 'object', `${label}: result object`);
  assert.equal(result.status, status, `${label}: ${JSON.stringify(result)}`);
  assert.equal(result.exitCode, STATUS_EXIT[status], `${label}: exitCode`);
}

// Starts one run and returns once its command started or the run settled without starting it.
async function attempt(product, scenario, label, executionOptions) {
  const executionObject = await execution(scenario, label, executionOptions);
  const requestPath = await writeRequest(scenario, label, runRequest(scenario, executionObject));
  const pending = runCli(product, scenario, requestPath);
  let settled = null;
  pending.then(result => { settled = { result }; }, error => { settled = { error }; });
  await waitFor(() => settled || startedRecord(scenario.recordDir, label), {
    timeoutMs: 30_000,
    what: `${label} to start or settle`,
  });
  if (settled?.error) throw settled.error;
  return { pending, admitted: Boolean(startedRecord(scenario.recordDir, label)), result: settled?.result ?? null };
}

// One interrupted run whose command has no child; then the killed root's PID is reported as another
// process (Windows reuses PIDs) and a new Unity request asks for admission.
async function interruptedThenReused(t, { name, interrupt, observeDelayMs }) {
  const product = await loadProduct();
  const scenario = await scenarioFor(t, name);
  const controller = new AbortController();
  const executionObject = await execution(scenario, 'root', {
    name: 'Unity.exe',
    timeoutMs: interrupt === 'timeout' ? 1500 : 120_000,
  });
  const requestPath = await writeRequest(scenario, 'root', runRequest(scenario, executionObject));
  const pending = runCli(product, scenario, requestPath, {
    observe: delayedObserve(scenario.observation.observe, observeDelayMs),
    signal: controller.signal,
  });
  const root = await waitFor(() => startedRecord(scenario.recordDir, 'root'), { what: 'root to start' });
  if (interrupt === 'cancel') {
    await sleep(SETTLE_MS);
    controller.abort();
  }
  const interrupted = await pending;
  const expectedStatus = interrupt === 'timeout' ? 'timed-out' : 'cancelled';
  assertStatus(interrupted, expectedStatus, `${name}: interrupted run`);

  const rootEnded = await waitFor(() => !isAlive(root.pid), {
    timeoutMs: 10_000,
    what: 'the interrupted root to be ended by its own runner',
  });
  assert.ok(rootEnded, 'the runner ended the root it started');

  const reusedAt = new Date(Date.parse(root.startedAt) + REUSED_LATER_MS).toISOString();
  setMachine(scenario, {
    syntheticProcesses: [{ pid: root.pid, parentPid: process.pid, startedAt: reusedAt, name: 'unrelated.exe' }],
  });
  const probe = await attempt(product, scenario, 'probe', { name: 'Unity.exe', waitTimeoutMs: 0 });
  if (scenario.observation.refusals.length > 0) {
    throw new FixtureError('the ended root PID was taken by a real process before the probe');
  }
  const probeStatus = probe.result?.status ?? 'admitted';
  assert.ok(probe.admitted,
    `${name}: the runner proved its own bundle ended (root ended by its handle, no child), so a later `
      + `reuse of that PID must not keep the lock: probe ${probeStatus} ${probe.result?.code ?? ''}; `
      + `interrupted result: ${interrupted.message}`);
  await release(scenario, 'probe');
  assertStatus(await probe.pending, 'succeeded', `${name}: probe after the interrupted run`);
}

test('timeout: a runner that ended its own childless root leaves no lease for a reused PID to block',
  { timeout: 120_000 }, async t => {
    await interruptedThenReused(t, { name: 'timeout-reuse', interrupt: 'timeout', observeDelayMs: 0 });
  });

test('timeout with native-like observation time: the ended root does not leave a lease behind',
  { timeout: 120_000 }, async t => {
    await interruptedThenReused(t, {
      name: 'timeout-reuse-slow',
      interrupt: 'timeout',
      observeDelayMs: NATIVE_LIKE_OBSERVE_MS,
    });
  });

test('cancel: a runner that ended its own childless root leaves no lease for a reused PID to block',
  { timeout: 120_000 }, async t => {
    await interruptedThenReused(t, { name: 'cancel-reuse', interrupt: 'cancel', observeDelayMs: 0 });
  });

test('a started command is not reported as not admitted when another record later shows PID reuse',
  { timeout: 120_000 }, async t => {
    const product = await loadProduct();
    const scenario = await scenarioFor(t, 'started-then-conflict');
    const holder = await attempt(product, scenario, 'holder', { name: 'Unity.exe' });
    assert.ok(holder.admitted, `holder must start: ${JSON.stringify(holder.result)}`);
    await sleep(SETTLE_MS);

    // A second runner, in its own process, waits behind the holder and is then lost (killed).
    const waiterExecution = await execution(scenario, 'waiter', { name: 'Unity.exe', waitTimeoutMs: 60_000 });
    const waiterRequest = await writeRequest(scenario, 'waiter', runRequest(scenario, waiterExecution));
    const waiter = await startBootstrap(scenario, 'waiter-runner', waiterRequest);
    const waiterRecord = await waitFor(() => startedRecord(scenario.recordDir, 'waiter-runner'), {
      what: 'waiting runner to record itself',
    });
    await sleep(SETTLE_MS);
    if (startedRecord(scenario.recordDir, 'waiter')) throw new FixtureError('the waiter was admitted');

    // From the moment the lost runner dies, its PID is reported as a later incarnation.
    const reusedAt = new Date(Date.parse(waiterRecord.startedAt) + REUSED_LATER_MS).toISOString();
    setMachine(scenario, {
      syntheticProcesses: [{
        pid: waiterRecord.pid,
        parentPid: process.pid,
        startedAt: reusedAt,
        name: 'unrelated.exe',
      }],
    });
    waiter.child.kill('SIGKILL');
    await waiter.exited;
    scenario.observation.refusals.length = 0;

    const early = await Promise.race([holder.pending, sleep(5000).then(() => null)]);
    const holderStillRunning = isRunning(scenario.recordDir, 'holder');
    if (scenario.observation.refusals.length > 0) {
      throw new FixtureError('the lost runner PID was taken by a real process');
    }
    if (early !== null) {
      assert.notEqual(early.status, 'blocked',
        'blocked/1 means the command was not started; this command started '
          + `(still running when its runner returned: ${holderStillRunning}): ${JSON.stringify(early)}`);
    }
    await release(scenario, 'holder');
    const final = early ?? await holder.pending;
    assert.notEqual(final.status, 'blocked', `a started command is never "not admitted": ${JSON.stringify(final)}`);
  });

// Independent regressions for the boundary between a coordinated bundle and an external Unity when
// Windows has reused a parent PID. Expected outcomes come from the contract, not from the product:
// - user decision 4 (msg_b49fffe1ccaa) and goal "범위 4": Unity runs one at a time; import children
//   join a bundle through parentPid, and another owner's Unity is never mistaken for the same bundle;
// - contract v1 (E/pr2-execution-contract-v1.md, SHA256 b43a07ff…e9aae2) "Admission과 관측" 1 and 6:
//   an external Unity already running blocks a new Unity start; a same-name root is not merged;
//   "실행 수명과 실패": another incarnation of a PID is handled conservatively;
// - supplement v1.1 Q5: another Unity root is not merged by name alone.
// A process cannot be the child of a process that started after it. When a dead launcher's PID is
// reused by a coordinated root, the older external Unity still names that PID as its parent; it is
// still an external Unity and must still block a new Unity.
//
// Observations are injected test values from lock-fixture.mjs; the external Unity is a synthetic
// entry for a PID that no live process holds (the fixture refuses it otherwise).
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
  loadProduct,
  release,
  runFolder,
  setMachine,
  sleep,
  startedRecord,
  stopScenario,
  waitFor,
  writeRequest,
} from './lock-fixture.mjs';

// Synthetic PIDs near the top of the default 22-bit PID space, used only while no process holds them.
const EXTERNAL_UNITY_PID = 4_194_100;
const DEAD_LAUNCHER_PID = 4_194_104;

// The external Unity started an hour before the coordinated root.
const EARLIER_MS = 60 * 60 * 1000;

const SETTLE_MS = 1000;

let repository;
before(async () => {
  repository = await createRepository(join(await runFolder(), 'repo ancestry'));
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

async function attempt(product, scenario, label, executionOptions) {
  const executionObject = await execution(scenario, label, executionOptions);
  const requestPath = await writeRequest(scenario, label, runRequest(scenario, executionObject));
  const pending = product.runCli([requestPath], {
    observe: scenario.observation.observe,
    stateRoot: scenario.stateRoot,
  });
  let settled = null;
  pending.then(result => { settled = { result }; }, error => { settled = { error }; });
  await waitFor(() => settled || startedRecord(scenario.recordDir, label), {
    timeoutMs: 30_000,
    what: `${label} to start or settle`,
  });
  if (settled?.error) throw settled.error;
  return { label, pending, admitted: Boolean(startedRecord(scenario.recordDir, label)), result: settled?.result ?? null };
}

function externalUnity(parentPid, startedAt) {
  return { pid: EXTERNAL_UNITY_PID, parentPid, startedAt, name: 'Unity.exe' };
}

// Ends a probe that was wrongly admitted so the scenario does not keep it.
async function endIfAdmitted(scenario, run) {
  if (!run.admitted) return;
  await release(scenario, run.label);
  await run.pending;
}

test('an external Unity whose dead parent PID now belongs to a coordinated root still blocks a new Unity',
  { timeout: 120_000 }, async t => {
    const product = await loadProduct();
    const scenario = await scenarioFor(t, 'reused-parent-pid');
    // A coordinated Core WSL command runs; Unity beside it is allowed only by the memory gate,
    // which is ample here, so only the external Unity can block the probes below.
    const core = await attempt(product, scenario, 'core-wsl', { kind: 'wsl', part: 'Core', name: 'wsl.exe' });
    assert.ok(core.admitted, `Core WSL must start: ${JSON.stringify(core.result)}`);
    await sleep(SETTLE_MS);
    const root = startedRecord(scenario.recordDir, 'core-wsl');
    const externalStartedAt = new Date(Date.parse(root.startedAt) - EARLIER_MS).toISOString();

    // Control: the same external Unity whose launcher is gone blocks a new Unity.
    setMachine(scenario, { syntheticProcesses: [externalUnity(DEAD_LAUNCHER_PID, externalStartedAt)] });
    const control = await attempt(product, scenario, 'probe-control', {
      name: 'Unity.exe',
      waitTimeoutMs: 0,
    });
    await endIfAdmitted(scenario, control);
    if (scenario.observation.refusals.length > 0) throw new FixtureError('a synthetic PID is held by a real process');
    assert.equal(control.admitted, false, 'control: an external Unity blocks a new Unity');
    assert.equal(control.result?.status, 'blocked', `control: ${JSON.stringify(control.result)}`);

    // The launcher's PID is now the coordinated root's PID; the root started an hour after the Unity.
    setMachine(scenario, { syntheticProcesses: [externalUnity(root.pid, externalStartedAt)] });
    const reused = await attempt(product, scenario, 'probe-reused', {
      name: 'Unity.exe',
      waitTimeoutMs: 0,
    });
    await endIfAdmitted(scenario, reused);
    if (scenario.observation.refusals.length > 0) throw new FixtureError('a synthetic PID is held by a real process');
    assert.equal(reused.admitted, false,
      'an external Unity that started before the coordinated root cannot be that root\'s child; '
        + `a second Unity was admitted: ${JSON.stringify(reused.result)}`);
    assert.equal(reused.result?.status, 'blocked', `reused parent PID: ${JSON.stringify(reused.result)}`);

    await release(scenario, 'core-wsl');
    await core.pending;
  });

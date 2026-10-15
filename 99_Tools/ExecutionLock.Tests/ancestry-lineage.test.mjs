// Independent regressions for the bundle/external boundary when the ancestry of an external Unity runs
// through more than its immediate parent, and for a genuine import child that must stay in its bundle.
// Expected outcomes come from the contract, not from the product:
// - user decision 4 (msg_b49fffe1ccaa) and goal "범위 4": Unity runs one at a time; an import child
//   joins its bundle through parentPid; another owner's Unity is never mistaken for the same bundle;
// - contract v1 (E/pr2-execution-contract-v1.md, SHA256 b43a07ff…e9aae2) "Admission과 관측" 1, 4, 6:
//   an external Unity blocks a new Unity; Core WSL beside one Unity bundle is admitted when physical
//   >= 6 GiB and commit >= 4 GiB; "실행 수명과 실패": another incarnation of a PID is conservative;
// - supplement v1.1 (b26bf807…75a5e542) Q5: Unity.exe/wsl.exe names and the Content/Core parts.
// A process cannot descend from a process that started after it, at any hop of its ancestry.
//
// Fidelity: observations are injected (lock-fixture.mjs and its limits). A reused PID is simulated:
// the older external processes are synthetic entries for PIDs that no live process holds (the fixture
// refuses them otherwise) and their parentPid names a real harmless Node root or member recorded by the
// fixture. No real OS PID reuse, Unity or WSL runs here.
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
  writeSpec,
} from './lock-fixture.mjs';

// Synthetic PIDs near the top of the default 22-bit PID space (multiples of 4 like Windows PIDs),
// listed only while no process holds them.
const EXTERNAL_UNITY_PID = 4_194_200;
const OLDER_LAUNCHER_PID = 4_194_204;
const DEAD_PID = 4_194_208;

const HOUR_MS = 60 * 60 * 1000;

// Time for a started run to identify its root and record its children before the next request.
const SETTLE_MS = 1000;

let repository;
before(async () => {
  repository = await createRepository(join(await runFolder(), 'repo lineage'));
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

// Starts one run and returns once its command started or the run settled without starting it.
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

// Ends a probe that was wrongly admitted so the scenario does not keep it.
async function endIfAdmitted(scenario, run) {
  if (!run.admitted) return;
  await release(scenario, run.label);
  await run.pending;
}

function assertBlocked(run, why) {
  assert.equal(run.admitted, false, `${why}; a second Unity was admitted: ${JSON.stringify(run.result)}`);
  assert.equal(run.result?.status, 'blocked', `${why}: ${JSON.stringify(run.result)}`);
  assert.equal(run.result?.exitCode, 1, `${why}: blocked is exit 1`);
}

function assertNoRefusal(scenario) {
  if (scenario.observation.refusals.length > 0) throw new FixtureError('a synthetic PID is held by a real process');
}

const isoBefore = (startedAt, ms) => new Date(Date.parse(startedAt) - ms).toISOString();

function externalUnity(parentPid, startedAt) {
  return { pid: EXTERNAL_UNITY_PID, parentPid, startedAt, name: 'Unity.exe' };
}

function olderLauncher(parentPid, startedAt) {
  return { pid: OLDER_LAUNCHER_PID, parentPid, startedAt, name: 'launcher.exe' };
}

test('an external Unity under an older launcher whose dead parent PID now belongs to a coordinated root still blocks',
  { timeout: 120_000 }, async t => {
    const product = await loadProduct();
    const scenario = await scenarioFor(t, 'reused-pid-two-hops');
    const core = await attempt(product, scenario, 'core-wsl', { kind: 'wsl', part: 'Core', name: 'wsl.exe' });
    assert.ok(core.admitted, `Core WSL must start: ${JSON.stringify(core.result)}`);
    await sleep(SETTLE_MS);
    const root = startedRecord(scenario.recordDir, 'core-wsl');
    // Launcher two hours and its Unity one hour before the coordinated root: neither can descend from it.
    const launcherStartedAt = isoBefore(root.startedAt, 2 * HOUR_MS);
    const unityStartedAt = isoBefore(root.startedAt, HOUR_MS);

    // Control: the launcher's own parent is gone, so the Unity is plainly external.
    setMachine(scenario, {
      syntheticProcesses: [olderLauncher(DEAD_PID, launcherStartedAt), externalUnity(OLDER_LAUNCHER_PID, unityStartedAt)],
    });
    const control = await attempt(product, scenario, 'probe-control', { name: 'Unity.exe', waitTimeoutMs: 0 });
    await endIfAdmitted(scenario, control);
    assertNoRefusal(scenario);
    assertBlocked(control, 'control: an external Unity under a launcher blocks a new Unity');

    // The launcher's dead parent PID is now the coordinated root's PID (reused one hop above the Unity).
    setMachine(scenario, {
      syntheticProcesses: [olderLauncher(root.pid, launcherStartedAt), externalUnity(OLDER_LAUNCHER_PID, unityStartedAt)],
    });
    const reused = await attempt(product, scenario, 'probe-reused', { name: 'Unity.exe', waitTimeoutMs: 0 });
    await endIfAdmitted(scenario, reused);
    assertNoRefusal(scenario);
    assertBlocked(reused, 'a launcher older than the coordinated root cannot be its child, so its Unity is external');

    await release(scenario, 'core-wsl');
    await core.pending;
  });

test('an external Unity whose dead parent PID now belongs to a recorded child of a coordinated root still blocks',
  { timeout: 120_000 }, async t => {
    const product = await loadProduct();
    const scenario = await scenarioFor(t, 'reused-member-pid');
    // The root starts a real harmless child that joins its bundle through parentPid.
    const memberSpec = await writeSpec(scenario, 'member', { name: 'node.exe' });
    const core = await attempt(product, scenario, 'core-wsl', {
      kind: 'wsl',
      part: 'Core',
      name: 'wsl.exe',
      grandchild: memberSpec,
    });
    assert.ok(core.admitted, `Core WSL must start: ${JSON.stringify(core.result)}`);
    await waitFor(() => startedRecord(scenario.recordDir, 'member'), { what: 'the bundle child to start' });
    await sleep(SETTLE_MS);
    const member = startedRecord(scenario.recordDir, 'member');

    // The external Unity started an hour before that child, whose PID it names as its dead parent's.
    setMachine(scenario, { syntheticProcesses: [externalUnity(member.pid, isoBefore(member.startedAt, HOUR_MS))] });
    const probe = await attempt(product, scenario, 'probe', { name: 'Unity.exe', waitTimeoutMs: 0 });
    await endIfAdmitted(scenario, probe);
    assertNoRefusal(scenario);
    assertBlocked(probe, 'a Unity older than the bundle child cannot be its child, so it is external');

    await release(scenario, 'member');
    await release(scenario, 'core-wsl');
    await core.pending;
  });

test('a Unity import child started by the coordinated Unity root stays in its bundle, so Core WSL beside it is admitted',
  { timeout: 120_000 }, async t => {
    const product = await loadProduct();
    const scenario = await scenarioFor(t, 'genuine-import-child');
    const importSpec = await writeSpec(scenario, 'import', { name: 'Unity.exe' });
    const unity = await attempt(product, scenario, 'content-unity', {
      name: 'Unity.exe',
      part: 'Content',
      grandchild: importSpec,
    });
    assert.ok(unity.admitted, `Content Unity must start: ${JSON.stringify(unity.result)}`);
    await waitFor(() => startedRecord(scenario.recordDir, 'import'), { what: 'the import child to start' });
    await sleep(SETTLE_MS);

    // One Unity bundle (root + its own import child), ample memory: Core WSL beside it passes the gate.
    const core = await attempt(product, scenario, 'core-wsl', {
      kind: 'wsl',
      part: 'Core',
      name: 'wsl.exe',
      waitTimeoutMs: 0,
    });
    const coreAdmitted = core.admitted;
    if (coreAdmitted) {
      await release(scenario, 'core-wsl');
      const coreResult = await core.pending;
      assert.equal(coreResult.status, 'succeeded', `Core WSL beside the bundle: ${JSON.stringify(coreResult)}`);
    }
    assert.ok(coreAdmitted, 'the import child is part of the one Unity bundle, not a second Unity root: '
      + `${JSON.stringify(core.result)}`);

    await release(scenario, 'import');
    await release(scenario, 'content-unity');
    const unityResult = await unity.pending;
    assert.equal(unityResult.status, 'succeeded', `Content Unity bundle: ${JSON.stringify(unityResult)}`);
  });

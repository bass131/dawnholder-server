// Independent regressions for the read-only inspect against the committed state: a leftover partial
// write stays an error that is kept byte for byte, and transactions held for a native-like observation
// time do not turn a healthy state into an error. Expected outcomes come from the contract, not from
// the product:
// - contract v1 (E/pr2-execution-contract-v1.md, SHA256 b43a07ff…e9aae2) "공용 상태와 원자성": inspect
//   only reads; a damaged or partial state file and unknown ownership are failures that are kept, never
//   deleted or repaired; waits and retries stay within finite bounds; "결과와 근거": observed/0 for a
//   completed read, tool-error/2 for a state failure, and a run that cannot read its state starts nothing;
// - the product README promise the fix keeps (99_Tools/ExecutionLock/README.md): a valid transaction in
//   progress still lets inspect read the committed snapshot.
// The leftover file uses the product's own pending-write name (state-<uuid>.pending, as written by an
// interrupted state write) so the leftover is exactly what a crashed writer leaves behind.
//
// Fidelity: observations are injected (lock-fixture.mjs and its limits); "native-like" only stretches
// each observation of the running requests to the order of a Windows CIM query.
//
// Environment: EXECUTION_LOCK_TESTS_WORK, an existing absolute folder (kept after the run).
import assert from 'node:assert/strict';
import { randomUUID } from 'node:crypto';
import { readFile, writeFile } from 'node:fs/promises';
import { join } from 'node:path';
import { before, test } from 'node:test';

import {
  createRepository,
  createScenario,
  execution,
  loadProduct,
  release,
  runFolder,
  sleep,
  snapshot,
  startedRecord,
  stopScenario,
  waitFor,
  writeRequest,
} from './lock-fixture.mjs';

const NATIVE_LIKE_OBSERVE_MS = 350;
const SLOW_RUNS = 4;

let repository;
before(async () => {
  repository = await createRepository(join(await runFolder(), 'repo snapshot'));
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

function runCli(product, scenario, requestPath, observe = scenario.observation.observe) {
  return product.runCli([requestPath], { observe, stateRoot: scenario.stateRoot });
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

async function finishedRun(product, scenario, label, observe) {
  const executionObject = await execution(scenario, label, { name: 'node.exe', hold: false });
  const requestPath = await writeRequest(scenario, label, runRequest(scenario, executionObject));
  return withTimeout(runCli(product, scenario, requestPath, observe), 60_000, `${label} to finish`);
}

test('a leftover partial state write makes inspect and run tool-error/2, starts nothing and is kept byte for byte',
  { timeout: 120_000 }, async t => {
    const product = await loadProduct();
    const scenario = await scenarioFor(t, 'leftover-pending');
    const first = await finishedRun(product, scenario, 'first');
    assert.equal(first.status, 'succeeded', `a committed snapshot exists first: ${JSON.stringify(first)}`);

    const leftoverPath = join(scenario.stateRoot, `state-${randomUUID()}.pending`);
    const leftover = '{"version":1,"partial":';
    await writeFile(leftoverPath, leftover);
    const before = await snapshot(scenario.stateRoot);

    const inspectPath = await writeRequest(scenario, 'inspect', { version: 1, action: 'inspect', repoRoot: repository.root });
    const inspect = await withTimeout(runCli(product, scenario, inspectPath), 30_000, 'inspect to settle');
    assert.equal(inspect.status, 'tool-error', `a partial write is a state failure, not a clean read: ${JSON.stringify(inspect)}`);
    assert.equal(inspect.exitCode, 2, 'tool-error is exit 2');

    const runExecution = await execution(scenario, 'probe', { name: 'node.exe', waitTimeoutMs: 0 });
    const runPath = await writeRequest(scenario, 'probe', runRequest(scenario, runExecution));
    const run = await withTimeout(runCli(product, scenario, runPath), 30_000, 'run to settle');
    assert.equal(run.status, 'tool-error', `a run that cannot read its state is a tool failure: ${JSON.stringify(run)}`);
    assert.equal(startedRecord(scenario.recordDir, 'probe'), null, 'nothing starts on a partial state');

    const after = await snapshot(scenario.stateRoot);
    assert.deepEqual([...after.keys()].sort(), [...before.keys()].sort(), 'no state file was added, promoted or removed');
    for (const [path, hash] of before) assert.equal(after.get(path), hash, `${path} kept byte for byte`);
    assert.equal(await readFile(leftoverPath, 'utf8'), leftover, 'the leftover partial write is not repaired');
  });

test('inspect is observed/0 every time while runs hold transactions for a native-like observation time',
  { timeout: 180_000 }, async t => {
    const product = await loadProduct();
    const scenario = await scenarioFor(t, 'inspect-slow-transactions');
    const first = await finishedRun(product, scenario, 'first');
    assert.equal(first.status, 'succeeded', `a committed snapshot exists first: ${JSON.stringify(first)}`);

    const inspectPath = await writeRequest(scenario, 'inspect', { version: 1, action: 'inspect', repoRoot: repository.root });
    const outcomes = new Map();
    const failures = [];
    let running = true;
    const inspecting = (async () => {
      while (running) {
        const result = await runCli(product, scenario, inspectPath);
        const key = `${result.status}/${result.code}`;
        outcomes.set(key, (outcomes.get(key) ?? 0) + 1);
        if (result.status !== 'observed') failures.push(result);
        await sleep(5);
      }
    })();
    try {
      const slow = delayedObserve(scenario.observation.observe, NATIVE_LIKE_OBSERVE_MS);
      for (let index = 1; index <= SLOW_RUNS; index += 1) {
        const executionObject = await execution(scenario, `slow-${index}`, { name: 'node.exe' });
        const requestPath = await writeRequest(scenario, `slow-${index}`, runRequest(scenario, executionObject));
        const pending = runCli(product, scenario, requestPath, slow);
        await waitFor(() => startedRecord(scenario.recordDir, `slow-${index}`), { what: `slow-${index} to start` });
        await sleep(NATIVE_LIKE_OBSERVE_MS * 3);
        await release(scenario, `slow-${index}`);
        const result = await withTimeout(pending, 60_000, `slow-${index} to finish`);
        assert.equal(result.status, 'succeeded', `slow-${index}: ${JSON.stringify(result)}`);
      }
    } finally {
      running = false;
      await inspecting;
    }
    const total = [...outcomes.values()].reduce((sum, count) => sum + count, 0);
    t.diagnostic(`inspect outcomes: ${JSON.stringify(Object.fromEntries(outcomes))}`);
    assert.ok(total > 0, 'inspect ran during the slow transactions');
    assert.deepEqual(failures, [], 'every inspect read the committed snapshot while transactions were held');
  });

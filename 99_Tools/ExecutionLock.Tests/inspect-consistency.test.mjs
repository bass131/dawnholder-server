// Independent regression for the read-only inspect while other runners change the shared state
// through normal, successful runs. Expected outcomes come from the contract and the tool's own
// documented promise, not from the product's code:
// - contract v1 (E/pr2-execution-contract-v1.md, SHA256 b43a07ff…e9aae2) "공용 상태와 원자성":
//   inspect only reads; damaged or partial state is a failure, never an empty state; state
//   transitions happen at one shared atomic boundary;
// - 99_Tools/ExecutionLock/README.md (SHA256 81b67e67ff06d547…) "진입과 요청": inspect can read
//   the committed snapshot even while a valid transaction remains; damage, a partial write and a
//   missing first snapshot are errors.
// A state written only by successful runs is neither damaged nor partial, so an inspect taken at any
// moment after the first snapshot exists is observed/0. A tool-error there would report a healthy
// state as damaged.
//
// Observations are injected test values from lock-fixture.mjs.
//
// Environment: EXECUTION_LOCK_TESTS_WORK, an existing absolute folder (kept after the run).
import assert from 'node:assert/strict';
import { join } from 'node:path';
import { before, test } from 'node:test';

import {
  createRepository,
  createScenario,
  execution,
  loadProduct,
  runFolder,
  stopScenario,
  writeRequest,
} from './lock-fixture.mjs';

// Short runs whose state changes (register, reserve, spawn, exit) are inspected meanwhile.
const SHORT_RUNS = 20;

let repository;
before(async () => {
  repository = await createRepository(join(await runFolder(), 'repo inspect'));
});

async function scenarioFor(t, name) {
  const scenario = await createScenario(name);
  t.after(async () => {
    const residue = await stopScenario(scenario);
    if (residue.length > 0) t.diagnostic(`fixture commands left to their hold limit: ${residue.join(', ')}`);
  });
  return scenario;
}

function dependencies(scenario) {
  return { observe: scenario.observation.observe, stateRoot: scenario.stateRoot };
}

async function shortRun(product, scenario, label) {
  const executionObject = await execution(scenario, label, { name: 'Unity.exe', hold: false, waitTimeoutMs: 10_000 });
  const requestPath = await writeRequest(scenario, label, {
    version: 1,
    action: 'run',
    repoRoot: repository.root,
    evidenceDir: scenario.evidenceDir,
    execution: executionObject,
  });
  return product.runCli([requestPath], dependencies(scenario));
}

test('inspect during successful runs is observed/0 every time once the first snapshot exists',
  { timeout: 180_000 }, async t => {
    const product = await loadProduct();
    const scenario = await scenarioFor(t, 'inspect-during-writes');
    const first = await shortRun(product, scenario, 'first');
    assert.equal(first.status, 'succeeded', `the first snapshot comes from a successful run: ${JSON.stringify(first)}`);

    const inspectPath = await writeRequest(scenario, 'inspect', { version: 1, action: 'inspect', repoRoot: repository.root });
    let runsDone = false;
    const runStatuses = [];
    const runs = (async () => {
      try {
        for (let index = 0; index < SHORT_RUNS; index += 1) {
          runStatuses.push((await shortRun(product, scenario, `short-${index}`)).status);
        }
      } finally {
        runsDone = true;
      }
    })();

    const inspectCounts = new Map();
    const failures = [];
    while (!runsDone) {
      const result = await product.runCli([inspectPath], dependencies(scenario));
      const key = `${result.status}/${result.code}`;
      inspectCounts.set(key, (inspectCounts.get(key) ?? 0) + 1);
      if (result.status !== 'observed' && failures.length < 5) failures.push({ code: result.code, message: result.message });
    }
    await runs;

    t.diagnostic(`inspect results: ${JSON.stringify(Object.fromEntries(inspectCounts))}`);
    t.diagnostic(`run results: ${JSON.stringify(runStatuses)}`);
    assert.ok(runStatuses.length === SHORT_RUNS && runStatuses.every(status => status === 'succeeded'),
      `every short run succeeds, so every state written is a legitimate one: ${JSON.stringify(runStatuses)}`);
    const inspected = [...inspectCounts.values()].reduce((sum, count) => sum + count, 0);
    assert.ok(inspected > 0, 'at least one inspect ran during the runs');
    assert.deepEqual(failures, [],
      `inspect of a healthy state must not report damage: ${JSON.stringify(Object.fromEntries(inspectCounts))}`);
  });

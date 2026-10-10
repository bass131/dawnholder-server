// Re-verification checks for the fix of liveness helper defect 1 (99_Tools/Orca/check-liveness.mjs),
// run as a black box. They cover the boundaries around "messages sent to a Dispatch" that the
// acceptance test `defect 1` in verify-liveness.test.mjs does not reach.
// Expected values are literals from the requirement texts, not from the helper:
// - goal 「설계 / 생존 신호 간격 helper」 (01_Phases/goals/2026-10-10-operating-tool-guards/goal.md
//   at 691677e3): a readable input with no Dispatch is input-error (line 143); the signals are what
//   the Dispatch sent, and a message to it (`to_handle` starting with `dispatch:`) is not a signal
//   even with a payload dispatchId, while it still answers questions (line 145); question waits
//   (line 146); exit 0/1/2 (line 147);
// - README 「생존 신호 간격」 (99_Tools/README.md at 691677e3): `to_handle` is omitted, null or text,
//   anything else is an input error (line 78); a message to `dispatch:` joins no Dispatch at all,
//   and an omitted or null `to_handle` keeps the old grouping (line 80); a tool failure is an
//   input-error whose diagnostic gives the cause (lines 88 and 90).
import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { mkdtemp, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { test } from 'node:test';
import { fileURLToPath, pathToFileURL } from 'node:url';

const repositoryRoot = fileURLToPath(new URL('../../', import.meta.url));
const cliPath = join(repositoryRoot, '99_Tools', 'Orca', 'check-liveness.mjs');

// goal line 147.
const documentedExit = { within: 0, over: 1, 'input-error': 2 };

function runHelper(args, nodeOptions = []) {
  const environment = { ...process.env };
  delete environment.NODE_TEST_CONTEXT;
  const run = spawnSync(process.execPath, [...nodeOptions, cliPath, ...args],
    { cwd: repositoryRoot, encoding: 'utf8', env: environment, timeout: 30000 });
  return { exit: run.status, stdout: run.stdout ?? '', stderr: run.stderr ?? '' };
}

function result(run, status, label) {
  const report = `${label}\nexit: ${run.exit}\nstdout: ${run.stdout}\nstderr: ${run.stderr}`;
  let output;
  try {
    output = JSON.parse(run.stdout);
  } catch {
    assert.fail(`${report}\n(stdout is not one JSON value)`);
  }
  assert.equal(output.status, status, report);
  assert.equal(run.exit, documentedExit[status], report);
  return output;
}

function onlyDispatch(output, id) {
  const found = output.dispatches.filter(entry => entry.dispatchId === id);
  assert.equal(found.length, 1, `exactly one entry for ${id}: ${JSON.stringify(output.dispatches)}`);
  return found[0];
}

function assertDiagnosticFields(output, label) {
  assert.ok(output.diagnostics.length > 0, `${label}: a diagnostic`);
  for (const diagnostic of output.diagnostics) {
    for (const field of ['code', 'path', 'message', 'repair']) {
      assert.equal(typeof diagnostic[field], 'string', `${label}: diagnostic ${field}`);
    }
  }
}

async function scratchFolder(t) {
  const folder = await mkdtemp(join(tmpdir(), 'reverify-liveness-'));
  t.after(() => rm(folder, { recursive: true, force: true }));
  return folder;
}

async function messagesFile(t, messages) {
  const path = join(await scratchFolder(t), 'messages.json');
  await writeFile(path, `${JSON.stringify(messages, null, 2)}\n`);
  return path;
}

const DISPATCH = 'ctx_reverify00001';
const OTHER_DISPATCH = 'ctx_reverify00002';
const WORKER = 'term_reverify-worker';
const OTHER_WORKER = 'term_reverify-other-worker';
const LEAD = 'term_reverify-lead';
const payloadFor = dispatchId => JSON.stringify({ taskId: 'task_reverify01', dispatchId });
const signal = (id, type, at, extra = {}) => ({
  id, type, from_handle: WORKER, to_handle: 'run:run_reverify001', thread_id: null, created_at: at,
  subject: type === 'heartbeat' ? 'alive' : `[Rules Sol] ${type}`, payload: payloadFor(DISPATCH), ...extra,
});
// The shape of a lead's follow-up to a Dispatch (real example: msg_a67174874724).
const toDispatch = (id, type, at, recipient, payloadDispatchId, extra = {}) => ({
  id, type, from_handle: LEAD, to_handle: `dispatch:${recipient}`, thread_id: null, created_at: at,
  subject: '[Rules 리드 Opus] 알림', payload: payloadFor(payloadDispatchId), ...extra,
});
const point = (id, at, type) => ({ id, at, type });
const gap = (from, to, seconds) => ({ from, to, seconds });

test('a reply sent to the Dispatch still ends the question wait but is not a signal', async t => {
  const path = await messagesFile(t, [
    signal('msg_r1', 'heartbeat', '2026-10-10T00:00:00Z'),
    // A blocking ask is sent from the dispatch: address with the Dispatch id in payload.
    signal('msg_rq', 'question', '2026-10-10T00:01:00Z', { from_handle: `dispatch:${DISPATCH}`, subject: 'Question' }),
    toDispatch('msg_rr', 'status', '2026-10-10T00:11:00Z', DISPATCH, DISPATCH, { thread_id: 'msg_rq' }),
    signal('msg_r2', 'heartbeat', '2026-10-10T00:15:00Z'),
    signal('msg_r3', 'worker_done', '2026-10-10T00:16:00Z'),
  ]);
  // Without the reply the question would run 840 s to msg_r2; with it the wait is 600 s and the
  // ordinary gap after it is 240 s.
  const entry = onlyDispatch(result(runHelper([path]), 'within', 'reply sent to the Dispatch'), DISPATCH);
  assert.equal(entry.signalCount, 4);
  assert.equal(entry.firstAt, '2026-10-10T00:00:00Z');
  assert.equal(entry.lastAt, '2026-10-10T00:16:00Z');
  assert.equal(entry.maxGapSeconds, 240);
  assert.deepEqual(entry.overGaps, []);
  assert.deepEqual(entry.questionWaits, [{
    ...gap(point('msg_rq', '2026-10-10T00:01:00Z', 'question'), point('msg_rr', '2026-10-10T00:11:00Z', 'status'), 600),
    replyId: 'msg_rr',
  }]);
});

test('worker messages with an omitted, null or empty to_handle are still signals', async t => {
  const omitted = signal('msg_t1', 'heartbeat', '2026-10-10T00:00:00Z');
  delete omitted.to_handle;
  const path = await messagesFile(t, [
    omitted,
    signal('msg_t2', 'heartbeat', '2026-10-10T00:03:20Z', { to_handle: null }),
    signal('msg_t3', 'status', '2026-10-10T00:06:40Z', { to_handle: '' }),
    signal('msg_t4', 'worker_done', '2026-10-10T00:10:00Z'),
  ]);
  // Dropping any of the first three would leave a 400 s gap or move firstAt.
  const entry = onlyDispatch(result(runHelper([path]), 'within', 'omitted, null and empty to_handle'), DISPATCH);
  assert.equal(entry.signalCount, 4);
  assert.equal(entry.firstAt, '2026-10-10T00:00:00Z');
  assert.equal(entry.lastAt, '2026-10-10T00:10:00Z');
  assert.equal(entry.maxGapSeconds, 200);
  assert.deepEqual(entry.overGaps, []);
});

for (const [label, value] of [
  ['zero', 0],
  ['false', false],
  ['a number', 7],
  ['an object', { handle: `dispatch:${DISPATCH}` }],
  ['an array', [`dispatch:${DISPATCH}`]],
]) {
  test(`a to_handle that is ${label} is an input error naming the field and the input`, async t => {
    const path = await messagesFile(t, [
      signal('msg_n1', 'heartbeat', '2026-10-10T00:00:00Z'),
      signal('msg_n2', 'heartbeat', '2026-10-10T00:01:00Z', { to_handle: value }),
    ]);
    const output = result(runHelper([path]), 'input-error', `to_handle ${label}`);
    assertDiagnosticFields(output, `to_handle ${label}`);
    assert.ok(output.diagnostics.some(diagnostic =>
      diagnostic.message.includes('to_handle') && diagnostic.path.includes('messages.json')),
    `to_handle ${label}: cause and input location: ${JSON.stringify(output.diagnostics)}`);
  });
}

test('a worker_done sent to the Dispatch does not end its measurement', async t => {
  const path = await messagesFile(t, [
    signal('msg_d1', 'heartbeat', '2026-10-10T00:00:00Z'),
    toDispatch('msg_d_in', 'worker_done', '2026-10-10T00:01:00Z', DISPATCH, DISPATCH),
    signal('msg_d2', 'heartbeat', '2026-10-10T00:06:40Z'),
    signal('msg_d3', 'worker_done', '2026-10-10T00:07:00Z'),
  ]);
  const entry = onlyDispatch(result(runHelper([path]), 'over', 'incoming worker_done'), DISPATCH);
  assert.equal(entry.signalCount, 3);
  assert.equal(entry.lastAt, '2026-10-10T00:07:00Z');
  assert.equal(entry.maxGapSeconds, 400);
  assert.deepEqual(entry.overGaps, [
    gap(point('msg_d1', '2026-10-10T00:00:00Z', 'heartbeat'), point('msg_d2', '2026-10-10T00:06:40Z', 'heartbeat'), 400),
  ]);
});

test('a message to one Dispatch carrying another Dispatch id is a signal of neither', async t => {
  const path = await messagesFile(t, [
    signal('msg_a1', 'heartbeat', '2026-10-10T00:00:00Z'),
    signal('msg_b1', 'heartbeat', '2026-10-10T00:00:00Z', { from_handle: OTHER_WORKER, payload: payloadFor(OTHER_DISPATCH) }),
    signal('msg_b2', 'worker_done', '2026-10-10T00:01:40Z', { from_handle: OTHER_WORKER, payload: payloadFor(OTHER_DISPATCH) }),
    toDispatch('msg_cross', 'dispatch', '2026-10-10T00:03:20Z', OTHER_DISPATCH, DISPATCH),
    signal('msg_a2', 'worker_done', '2026-10-10T00:06:40Z'),
  ]);
  const output = result(runHelper([path]), 'over', 'cross-addressed follow-up');
  const first = onlyDispatch(output, DISPATCH);
  assert.equal(first.signalCount, 2);
  assert.deepEqual(first.overGaps, [
    gap(point('msg_a1', '2026-10-10T00:00:00Z', 'heartbeat'), point('msg_a2', '2026-10-10T00:06:40Z', 'worker_done'), 400),
  ]);
  const other = onlyDispatch(output, OTHER_DISPATCH);
  assert.equal(other.signalCount, 2);
  assert.equal(other.maxGapSeconds, 100);
});

test('a Dispatch reached only by follow-ups is not reported', async t => {
  const path = await messagesFile(t, [
    signal('msg_w1', 'heartbeat', '2026-10-10T00:00:00Z'),
    toDispatch('msg_only_in', 'dispatch', '2026-10-10T00:00:30Z', OTHER_DISPATCH, OTHER_DISPATCH),
    signal('msg_w2', 'worker_done', '2026-10-10T00:01:00Z'),
  ]);
  const output = result(runHelper([path]), 'within', 'follow-up to a silent Dispatch');
  assert.deepEqual(output.dispatches.map(entry => entry.dispatchId), [DISPATCH]);
});

test('an input holding only follow-ups to Dispatches has no Dispatch and is an input error', async t => {
  const path = await messagesFile(t, [
    toDispatch('msg_f1', 'dispatch', '2026-10-10T00:00:00Z', DISPATCH, DISPATCH),
    toDispatch('msg_f2', 'status', '2026-10-10T00:10:00Z', DISPATCH, DISPATCH),
  ]);
  const output = result(runHelper([path]), 'input-error', 'only follow-ups');
  assertDiagnosticFields(output, 'only follow-ups');
});

test('an unexpected runtime failure is one input-error result whose diagnostic gives the cause', async t => {
  const folder = await scratchFolder(t);
  const messagesPath = join(folder, 'messages.json');
  await writeFile(messagesPath, `${JSON.stringify([signal('msg_injected', 'heartbeat', '2026-10-10T00:00:00Z')])}\n`);
  // Test double: makes the helper's id merge throw for this one message id, a failure no input
  // check covers.
  const preloadPath = join(folder, 'inject-failure.mjs');
  await writeFile(preloadPath, [
    'const set = Map.prototype.set;',
    'Map.prototype.set = function (key, value) {',
    "  if (key === 'msg_injected') {",
    "    throw new Error('injected failure at msg_injected');",
    '  }',
    '  return set.call(this, key, value);',
    '};',
    '',
  ].join('\n'));
  const run = runHelper([messagesPath], ['--import', pathToFileURL(preloadPath).href]);
  const output = result(run, 'input-error', 'injected runtime failure');
  assertDiagnosticFields(output, 'injected runtime failure');
  assert.ok(output.diagnostics.some(diagnostic => diagnostic.message.includes('injected failure at msg_injected')),
    `the cause is reported: ${JSON.stringify(output.diagnostics)}`);
});

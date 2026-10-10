// Independent verifier checks for the liveness gap helper 99_Tools/Orca/check-liveness.mjs, run as a
// black box. They cover boundaries the pre-implementation suite (check-liveness.test.mjs) left open.
// Expected values are literals from the requirement texts, not from the helper:
// - goal 「설계 / 생존 신호 간격 helper」 (01_Phases/goals/2026-10-10-operating-tool-guards/goal.md
//   lines 142-147 at bae5c541): the signals are "그 Dispatch가 보낸 모든 메시지(heartbeat·status·
//   question·escalation·worker_done)"; a question wait needs a reply "다른 발신자의 답 R"; the
//   threshold option is `--threshold-seconds <n>`;
// - README 「생존 신호 간격」 (99_Tools/README.md lines 78-80 at bae5c541) for the choices the goal
//   left to the implementer: one finite nonnegative threshold given once, a single signal has a
//   maximum gap of 0, measuring stops at the first worker_done, and a tie group with a missing
//   sequence keeps input order.
// The coordinator follow-up below copies the shape of a real Orca message (msg_a67174874724 in
// .backups/verification/2026-10-10-operating-tool-guards/lead-r2/tdd-dispatch-inbox-raw.json):
// type `dispatch`, sent by the lead's terminal to `dispatch:<id>` with the Dispatch id in payload.
import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { mkdtemp, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { test } from 'node:test';
import { fileURLToPath } from 'node:url';

const repositoryRoot = fileURLToPath(new URL('../../', import.meta.url));
const cliPath = join(repositoryRoot, '99_Tools', 'Orca', 'check-liveness.mjs');

// goal line 147.
const documentedExit = { within: 0, over: 1, 'input-error': 2 };

function runHelper(args) {
  const environment = { ...process.env };
  delete environment.NODE_TEST_CONTEXT;
  const run = spawnSync(process.execPath, [cliPath, ...args], { cwd: repositoryRoot, encoding: 'utf8', env: environment, timeout: 30000 });
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

async function messagesFile(t, messages) {
  const folder = await mkdtemp(join(tmpdir(), 'verify-liveness-'));
  t.after(() => rm(folder, { recursive: true, force: true }));
  const path = join(folder, 'messages.json');
  await writeFile(path, `${JSON.stringify(messages, null, 2)}\n`);
  return path;
}

const DISPATCH = 'ctx_verifier00001';
const WORKER = 'term_verifier-worker';
const LEAD = 'term_verifier-lead';
const signal = (id, type, at, extra = {}) => ({
  id, type, from_handle: WORKER, to_handle: 'run:run_verifier001', thread_id: null, created_at: at,
  subject: type === 'heartbeat' ? 'alive' : `[Rules Sol] ${type}`,
  payload: JSON.stringify({ taskId: 'task_verifier01', dispatchId: DISPATCH }), ...extra,
});
const point = (id, at, type) => ({ id, at, type });
const gap = (from, to, seconds) => ({ from, to, seconds });

test('defect 1: a coordinator follow-up sent to the Dispatch is not a worker signal', async t => {
  const path = await messagesFile(t, [
    signal('msg_v1', 'heartbeat', '2026-10-10T00:00:00Z'),
    { id: 'msg_lead_followup', type: 'dispatch', from_handle: LEAD, to_handle: `dispatch:${DISPATCH}`, thread_id: null,
      created_at: '2026-10-10T00:03:20Z', subject: '[Rules 리드 Opus] 알림',
      payload: JSON.stringify({ taskId: 'task_verifier01', dispatchId: DISPATCH }) },
    signal('msg_v2', 'heartbeat', '2026-10-10T00:06:40Z'),
    signal('msg_v3', 'worker_done', '2026-10-10T00:07:00Z'),
  ]);
  // The worker itself was silent for 400 s; the lead's message does not end that silence.
  const entry = onlyDispatch(result(runHelper([path]), 'over', 'coordinator follow-up'), DISPATCH);
  assert.equal(entry.signalCount, 3);
  assert.equal(entry.maxGapSeconds, 400);
  assert.deepEqual(entry.overGaps, [
    gap(point('msg_v1', '2026-10-10T00:00:00Z', 'heartbeat'), point('msg_v2', '2026-10-10T00:06:40Z', 'heartbeat'), 400),
  ]);
});

test('a later message from the same sender in the question thread is not an answer', async t => {
  const path = await messagesFile(t, [
    signal('msg_v1', 'heartbeat', '2026-10-10T00:00:00Z'),
    signal('msg_q', 'question', '2026-10-10T00:01:00Z', { thread_id: 'msg_q', subject: 'Question' }),
    // Same sender, no Dispatch id: neither a signal nor an answer.
    { id: 'msg_self', type: 'status', from_handle: WORKER, to_handle: 'run:run_verifier001', thread_id: 'msg_q',
      created_at: '2026-10-10T00:02:00Z', subject: 'Question follow-up', payload: null },
    signal('msg_v2', 'worker_done', '2026-10-10T00:07:01Z'),
  ]);
  const entry = onlyDispatch(result(runHelper([path]), 'over', 'same-sender thread message'), DISPATCH);
  assert.deepEqual(entry.questionWaits, []);
  assert.deepEqual(entry.overGaps, [
    gap(point('msg_q', '2026-10-10T00:01:00Z', 'question'), point('msg_v2', '2026-10-10T00:07:01Z', 'worker_done'), 361),
  ]);
});

test('a Dispatch with one signal reports a maximum gap of 0 and is within', async t => {
  const path = await messagesFile(t, [signal('msg_v1', 'heartbeat', '2026-10-10T00:00:00Z')]);
  const entry = onlyDispatch(result(runHelper([path]), 'within', 'one signal'), DISPATCH);
  assert.equal(entry.signalCount, 1);
  assert.equal(entry.firstAt, '2026-10-10T00:00:00Z');
  assert.equal(entry.lastAt, '2026-10-10T00:00:00Z');
  assert.equal(entry.maxGapSeconds, 0);
  assert.deepEqual(entry.overGaps, []);
  assert.deepEqual(entry.questionWaits, []);
});

test('messages after the first worker_done are not measured', async t => {
  const path = await messagesFile(t, [
    signal('msg_v1', 'heartbeat', '2026-10-10T00:00:00Z'),
    signal('msg_v2', 'worker_done', '2026-10-10T00:01:00Z'),
    signal('msg_v3', 'status', '2026-10-10T00:20:00Z'),
  ]);
  const entry = onlyDispatch(result(runHelper([path]), 'within', 'late status'), DISPATCH);
  assert.equal(entry.signalCount, 2);
  assert.equal(entry.lastAt, '2026-10-10T00:01:00Z');
  assert.equal(entry.maxGapSeconds, 60);
});

test('a same-time group with a missing sequence keeps input order', async t => {
  const path = await messagesFile(t, [
    signal('msg_v1', 'heartbeat', '2026-10-10T00:00:00Z', { sequence: 1 }),
    signal('msg_first_in_input', 'heartbeat', '2026-10-10T00:01:00Z'),
    signal('msg_second_in_input', 'status', '2026-10-10T00:01:00Z', { sequence: 3 }),
    signal('msg_v4', 'worker_done', '2026-10-10T00:07:00Z', { sequence: 5 }),
  ]);
  // Input order puts the sequenced message last in the tie, so the 360 s gap starts there.
  const entry = onlyDispatch(result(runHelper([path]), 'over', 'mixed sequence tie'), DISPATCH);
  assert.deepEqual(entry.overGaps, [
    gap(point('msg_second_in_input', '2026-10-10T00:01:00Z', 'status'), point('msg_v4', '2026-10-10T00:07:00Z', 'worker_done'), 360),
  ]);
});

for (const [label, args] of [
  ['a repeated threshold', ['--threshold-seconds', '300', '--threshold-seconds', '60']],
  ['a threshold without a value', ['--threshold-seconds']],
  ['a negative threshold', ['--threshold-seconds', '-1']],
]) {
  test(`${label} is an input error with a repair`, async t => {
    const path = await messagesFile(t, [signal('msg_v1', 'heartbeat', '2026-10-10T00:00:00Z')]);
    const output = result(runHelper([path, ...args]), 'input-error', label);
    assert.ok(output.diagnostics.length > 0, `${label}: a diagnostic`);
    for (const diagnostic of output.diagnostics) {
      for (const field of ['code', 'path', 'message', 'repair']) {
        assert.equal(typeof diagnostic[field], 'string', `${label}: diagnostic ${field}`);
      }
    }
  });
}

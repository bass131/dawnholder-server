// Independent regressions for the liveness gap helper 99_Tools/Orca/check-liveness.mjs, run as a
// black box. Expected outcomes come from the approved design and hand-measured records, not from
// the helper:
// - goal 「설계 / 생존 신호 간격 helper」 (01_Phases/goals/2026-10-10-operating-tool-guards/goal.md
//   lines 140-146 at 852f88ca): `node 99_Tools/Orca/check-liveness.mjs <file>... [--threshold-seconds
//   <n>]`, default 300; inputs are saved `inbox --json`/`check --json` outputs (result.messages),
//   wait outputs with leading `_keepalive` JSON lines, or message arrays, merged by id; grouping by
//   payload (object or JSON string) dispatchId, else a `dispatch:<id>` from_handle; every message of
//   a Dispatch is a signal in created_at order up to worker_done; status within/over/input-error with
//   exit 0/1/2; no writes, network or state changes;
// - the Rules lead's supplement 1 (E/tdd/work/ask1-answer.txt): output keys
//   {status, thresholdSeconds, dispatches[{dispatchId, signalCount, firstAt, lastAt, maxGapSeconds,
//   overGaps[{from{id,at,type}, to{id,at,type}, seconds}], questionWaits[{from, to, seconds,
//   replyId}]}], diagnostics[{code, path, message, repair}]}; question wait rule B: for a question Q
//   of a Dispatch, the earliest reply R from another sender with thread_id Q at or before the
//   Dispatch's next signal N makes Q->R a question wait (reported at any length, not in
//   maxGapSeconds or overGaps) and R->N an ordinary gap that starts at R; without such a reply Q->N
//   is ordinary; a readable input with no Dispatch is input-error; dispatch order and diagnostic
//   code values are not fixed;
// - supplement 2 (E/tdd/work/ask2-answer.txt): equal created_at order by sequence ascending, else by
//   input order;
// - over means more than the threshold ("300초 초과", goal lines 4 and 75).
// Hand-measured values and the fixtures' provenance are in liveness-sources.json. Synthetic cases
// are built in each test and named as such.
import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { existsSync } from 'node:fs';
import { copyFile, mkdtemp, readFile, readdir, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { test } from 'node:test';
import { fileURLToPath } from 'node:url';
import { isDeepStrictEqual } from 'node:util';

const repositoryRoot = fileURLToPath(new URL('../../', import.meta.url));
const cliPath = join(repositoryRoot, '99_Tools', 'Orca', 'check-liveness.mjs');
const fixturesDirectory = fileURLToPath(new URL('./liveness-fixtures/', import.meta.url));
const fixture = name => join(fixturesDirectory, name);

// goal line 145.
const documentedExit = { within: 0, over: 1, 'input-error': 2 };

function runHelper(args, { cwd = repositoryRoot } = {}) {
  const environment = { ...process.env };
  delete environment.NODE_TEST_CONTEXT;
  const result = spawnSync(process.execPath, [cliPath, ...args], { cwd, encoding: 'utf8', env: environment, timeout: 30000 });
  return { exit: result.status, stdout: result.stdout ?? '', stderr: result.stderr ?? '', error: result.error };
}

function runReport(run, label) {
  const entry = existsSync(cliPath) ? '' : ' [entry missing: 99_Tools/Orca/check-liveness.mjs]';
  return `${label}${entry}\nexit: ${run.exit}\nstdout: ${run.stdout}\nstderr: ${run.stderr}${run.error ? `\nerror: ${run.error}` : ''}`;
}

// One JSON result on stdout whose status matches the documented exit.
function result(run, status, label) {
  let output;
  try {
    output = JSON.parse(run.stdout);
  } catch {
    assert.fail(runReport(run, `${label}: stdout is one JSON value`));
  }
  assert.equal(output.status, status, runReport(run, `${label}: status`));
  assert.equal(run.exit, documentedExit[status], runReport(run, `${label}: exit for ${status}`));
  return output;
}

function dispatch(output, id) {
  assert.ok(Array.isArray(output.dispatches), 'dispatches is an array');
  const found = output.dispatches.filter(entry => entry.dispatchId === id);
  assert.equal(found.length, 1, `exactly one entry for ${id}: ${JSON.stringify(output.dispatches.map(entry => entry.dispatchId))}`);
  return found[0];
}

const point = (id, at, type) => ({ id, at, type });
const gap = (from, to, seconds) => ({ from, to, seconds });

function assertDiagnostics(output, label) {
  assert.ok(Array.isArray(output.diagnostics) && output.diagnostics.length > 0, `${label}: at least one diagnostic`);
  for (const diagnostic of output.diagnostics) {
    for (const field of ['code', 'path', 'message', 'repair']) {
      assert.equal(typeof diagnostic[field], 'string', `${label}: diagnostic ${field} is a string`);
    }
  }
}

// Real messages: closeout review (303 s), J sentence Sol (391 s) and the narrowed re-review with a
// question, from one inbox output.
test('closeout review Dispatch: the hand-measured 303 s gap is the only one over 300 s', () => {
  const run = runHelper([fixture('closeout-rereview-inbox.json')]);
  const output = result(run, 'over', 'closeout rereview inbox');
  assert.equal(output.thresholdSeconds, 300, 'default threshold');
  const entry = dispatch(output, 'ctx_a8a439c04dce');
  assert.equal(entry.signalCount, 9);
  assert.equal(entry.firstAt, '2026-10-10T05:39:08Z');
  assert.equal(entry.lastAt, '2026-10-10T05:54:20Z');
  assert.equal(entry.maxGapSeconds, 303);
  assert.deepEqual(entry.overGaps, [
    gap(point('msg_633ee531d33a', '2026-10-10T05:46:55Z', 'heartbeat'), point('msg_ff1d70074a60', '2026-10-10T05:51:58Z', 'heartbeat'), 303),
  ]);
  assert.deepEqual(entry.questionWaits, []);
});

test('J sentence Sol Dispatch: the hand-measured 391 s gap', () => {
  const output = result(runHelper([fixture('closeout-rereview-inbox.json')]), 'over', 'closeout rereview inbox');
  const entry = dispatch(output, 'ctx_381dddcf1934');
  assert.equal(entry.signalCount, 5);
  assert.equal(entry.firstAt, '2026-10-09T16:05:41Z');
  assert.equal(entry.lastAt, '2026-10-09T16:16:30Z');
  assert.equal(entry.maxGapSeconds, 391);
  assert.deepEqual(entry.overGaps, [
    gap(point('msg_62805b71a7eb', '2026-10-09T16:05:41Z', 'heartbeat'), point('msg_072d7b083db1', '2026-10-09T16:12:12Z', 'heartbeat'), 391),
  ]);
  assert.deepEqual(entry.questionWaits, []);
});

test('re-review Dispatch: a real answered question is a question wait and the rest stays within 300 s', () => {
  const output = result(runHelper([fixture('closeout-rereview-inbox.json')]), 'over', 'closeout rereview inbox');
  const entry = dispatch(output, 'ctx_91581c5e133d');
  assert.equal(entry.signalCount, 8, 'the lead reply is not a signal of the Dispatch');
  assert.equal(entry.firstAt, '2026-10-10T06:03:22Z');
  assert.equal(entry.lastAt, '2026-10-10T06:14:15Z');
  assert.equal(entry.maxGapSeconds, 164);
  assert.deepEqual(entry.overGaps, []);
  assert.deepEqual(entry.questionWaits, [{
    from: point('msg_81471c9d0554', '2026-10-10T06:06:45Z', 'question'),
    to: point('msg_2b11f01cfb83', '2026-10-10T06:07:46Z', 'status'),
    seconds: 61,
    replyId: 'msg_2b11f01cfb83',
  }]);
});

test('--threshold-seconds 50 on the real re-review: hand-measured gaps over 50 s are over and the 61 s question wait is not', () => {
  const output = result(runHelper([fixture('closeout-rereview-inbox.json'), '--threshold-seconds', '50']), 'over', 'threshold 50');
  assert.equal(output.thresholdSeconds, 50);
  const entry = dispatch(output, 'ctx_91581c5e133d');
  assert.equal(entry.maxGapSeconds, 164);
  assert.deepEqual(entry.overGaps, [
    gap(point('msg_29bed491e568', '2026-10-10T06:03:22Z', 'heartbeat'), point('msg_54afaec8e34e', '2026-10-10T06:06:06Z', 'heartbeat'), 164),
    gap(point('msg_c42fd4012616', '2026-10-10T06:08:57Z', 'status'), point('msg_857e74efcb4e', '2026-10-10T06:11:21Z', 'heartbeat'), 144),
    gap(point('msg_857e74efcb4e', '2026-10-10T06:11:21Z', 'heartbeat'), point('msg_8042379f7320', '2026-10-10T06:12:54Z', 'heartbeat'), 93),
    gap(point('msg_8042379f7320', '2026-10-10T06:12:54Z', 'heartbeat'), point('msg_2564e95527b2', '2026-10-10T06:14:15Z', 'worker_done'), 81),
  ]);
  assert.equal(entry.questionWaits.length, 1);
  assert.equal(entry.questionWaits[0].seconds, 61);
});

test('intake review Dispatch: every hand-measured gap is within 300 s (exit 0)', () => {
  const output = result(runHelper([fixture('closeout-intake-inbox.json')]), 'within', 'closeout intake inbox');
  const entry = dispatch(output, 'ctx_982ffebe0e6b');
  assert.equal(entry.signalCount, 8);
  assert.equal(entry.firstAt, '2026-10-10T06:32:57Z');
  assert.equal(entry.lastAt, '2026-10-10T06:44:45Z');
  assert.equal(entry.maxGapSeconds, 206);
  assert.deepEqual(entry.overGaps, []);
  assert.deepEqual(entry.questionWaits, []);
});

test('two inbox files with the same messages are merged by id', () => {
  const output = result(runHelper([fixture('closeout-rereview-inbox.json'), fixture('closeout-intake-inbox.json')]), 'over', 'two inboxes');
  assert.equal(dispatch(output, 'ctx_91581c5e133d').signalCount, 8, 'shared Dispatch is not doubled');
  assert.equal(dispatch(output, 'ctx_91581c5e133d').questionWaits.length, 1, 'shared question wait is not doubled');
  assert.equal(dispatch(output, 'ctx_a8a439c04dce').signalCount, 9);
  assert.equal(dispatch(output, 'ctx_982ffebe0e6b').signalCount, 8);
  assert.equal(dispatch(output, 'ctx_381dddcf1934').signalCount, 5);
});

test('check --json shape: the PR writing Sol has the recorded 343 s and 332 s gaps and its answered question', () => {
  const output = result(runHelper([fixture('pr1-writing-check.json')]), 'over', 'pr1 writing check');
  const entry = dispatch(output, 'ctx_b603f1bde17b');
  assert.equal(entry.maxGapSeconds, 343);
  assert.deepEqual(entry.overGaps, [
    gap(point('msg_813b8fe6d3e1', '2026-10-09T09:58:45Z', 'heartbeat'), point('msg_092e582ae3a5', '2026-10-09T10:04:28Z', 'heartbeat'), 343),
    gap(point('msg_4f92ca8f3bba', '2026-10-09T10:26:50Z', 'heartbeat'), point('msg_cbcd73127b85', '2026-10-09T10:32:22Z', 'heartbeat'), 332),
  ]);
  assert.equal(entry.questionWaits.length, 1);
  assert.equal(entry.questionWaits[0].from.id, 'msg_5f6887c37359');
  assert.equal(entry.questionWaits[0].replyId, 'msg_3e464481d320');
});

test('wait output with _keepalive lines and a message array: the TDD canon goal Gardener values', () => {
  const run = runHelper([fixture('tdd-canon-wait.txt'), fixture('tdd-canon-messages.json')]);
  const output = result(run, 'over', 'wait output + message array');
  const expected = {
    ctx_17955ac87a7e: [gap(point('msg_a85c87acd426', '2026-10-08T12:17:44Z', 'escalation'), point('msg_4460357cafce', '2026-10-08T12:25:13Z', 'heartbeat'), 449)],
    ctx_164a9f605c57: [
      gap(point('msg_e1eb68bd1a4a', '2026-10-08T12:34:23Z', 'heartbeat'), point('msg_b4e9ce0d9039', '2026-10-08T12:41:21Z', 'heartbeat'), 418),
      gap(point('msg_b4e9ce0d9039', '2026-10-08T12:41:21Z', 'heartbeat'), point('msg_d78c08dcc68e', '2026-10-08T12:47:34Z', 'heartbeat'), 373),
    ],
    ctx_7e5ebddbdd28: [gap(point('msg_851b394d7929', '2026-10-08T13:06:11Z', 'heartbeat'), point('msg_b7decc6f0784', '2026-10-08T13:13:02Z', 'heartbeat'), 411)],
    ctx_cb5ce542e50a: [gap(point('msg_0c70e7424cf1', '2026-10-08T14:13:08Z', 'heartbeat'), point('msg_fdb8f000ffde', '2026-10-08T14:19:11Z', 'heartbeat'), 363)],
    ctx_f1c3a890c258: [gap(point('msg_cf7326e63a9b', '2026-10-08T14:41:30Z', 'escalation'), point('msg_030bffe2cc6b', '2026-10-08T14:46:32Z', 'heartbeat'), 302)],
    ctx_a38458d832d0: [gap(point('msg_3e7470cba4ea', '2026-10-08T23:11:34Z', 'heartbeat'), point('msg_1623aa2c3916', '2026-10-08T23:17:55Z', 'heartbeat'), 381)],
    ctx_0b2d88fd401d: [],
  };
  for (const [id, overGaps] of Object.entries(expected)) {
    assert.deepEqual(dispatch(output, id).overGaps, overGaps, `${id} overGaps`);
  }
  assert.equal(dispatch(output, 'ctx_0b2d88fd401d').maxGapSeconds, 229);
  const writingSol = dispatch(output, 'ctx_c1e8312629fb');
  assert.equal(writingSol.maxGapSeconds, 774);
  // Only the 774 s gap is asserted: the question -> next signal gap before it has no reply in the
  // source and is not tested (liveness-sources.json, tddCanonGoal.notTested).
  const longest = gap(point('msg_d6abc781b567', '2026-10-08T11:46:08Z', 'heartbeat'), point('msg_75c99739db39', '2026-10-08T11:59:02Z', 'heartbeat'), 774);
  assert.ok(writingSol.overGaps.some(entry => isDeepStrictEqual(entry, longest)), `ctx_c1e8312629fb has the 774 s gap: ${JSON.stringify(writingSol.overGaps)}`);
  assert.deepEqual(writingSol.questionWaits, [], 'no reply in the input, so no question wait');
});

// Synthetic message lists written into a temporary folder for one test.
async function synthetic(t, files) {
  const folder = await mkdtemp(join(tmpdir(), 'check-liveness-'));
  t.after(() => rm(folder, { recursive: true, force: true }));
  const paths = [];
  for (const [name, content] of Object.entries(files)) {
    const path = join(folder, name);
    await writeFile(path, typeof content === 'string' ? content : `${JSON.stringify(content, null, 2)}\n`);
    paths.push(path);
  }
  return { folder, paths };
}

const DISPATCH = 'ctx_synthetic00001';
const signal = (id, type, at, extra = {}) => ({
  id, type, from_handle: 'term_synthetic-worker', to_handle: 'run:run_synthetic01', thread_id: null, created_at: at,
  subject: type === 'heartbeat' ? 'alive' : `[Rules Sol] ${type}`, payload: JSON.stringify({ taskId: 'task_synthetic01', dispatchId: DISPATCH }), ...extra,
});
const question = (id, at) => ({
  ...signal(id, 'question', at), from_handle: `dispatch:${DISPATCH}`, thread_id: id, subject: 'Question',
});
const reply = (id, questionId, at) => ({
  id, type: 'status', from_handle: 'run:run_synthetic01', to_handle: `dispatch:${DISPATCH}`, thread_id: questionId,
  created_at: at, subject: 'Re: Question', payload: null,
});

test('synthetic: exactly the threshold is within and one second more is over', async t => {
  const within = await synthetic(t, { 'within.json': [
    signal('msg_s1', 'heartbeat', '2026-10-10T00:00:00Z'), signal('msg_s2', 'worker_done', '2026-10-10T00:05:00Z'),
  ] });
  const withinEntry = dispatch(result(runHelper(within.paths), 'within', '300 s'), DISPATCH);
  assert.equal(withinEntry.maxGapSeconds, 300);
  assert.deepEqual(withinEntry.overGaps, []);

  const over = await synthetic(t, { 'over.json': [
    signal('msg_s1', 'heartbeat', '2026-10-10T00:00:00Z'), signal('msg_s2', 'worker_done', '2026-10-10T00:05:01Z'),
  ] });
  const overEntry = dispatch(result(runHelper(over.paths), 'over', '301 s'), DISPATCH);
  assert.deepEqual(overEntry.overGaps, [
    gap(point('msg_s1', '2026-10-10T00:00:00Z', 'heartbeat'), point('msg_s2', '2026-10-10T00:05:01Z', 'worker_done'), 301),
  ]);
});

test('synthetic: object payload, JSON string payload and a dispatch: from_handle group into one Dispatch', async t => {
  const { paths } = await synthetic(t, { 'mixed.json': [
    signal('msg_s1', 'heartbeat', '2026-10-10T00:00:00Z'),
    signal('msg_s2', 'status', '2026-10-10T00:01:00Z', { payload: { taskId: 'task_synthetic01', dispatchId: DISPATCH } }),
    signal('msg_s3', 'heartbeat', '2026-10-10T00:02:00Z', { from_handle: `dispatch:${DISPATCH}`, payload: null }),
    { id: 'msg_lead', type: 'status', from_handle: 'term_synthetic-lead', to_handle: 'term_synthetic-main', thread_id: null,
      created_at: '2026-10-10T00:02:30Z', subject: '[Rules 리드 Opus] 보고', payload: null },
    signal('msg_s4', 'worker_done', '2026-10-10T00:03:00Z'),
  ] });
  const output = result(runHelper(paths), 'within', 'mixed payload forms');
  assert.deepEqual(output.dispatches.map(entry => entry.dispatchId), [DISPATCH], 'a message without a Dispatch is not a Dispatch');
  const entry = dispatch(output, DISPATCH);
  assert.equal(entry.signalCount, 4);
  assert.equal(entry.maxGapSeconds, 60);
});

test('synthetic: rule B counts the silence after the reply as an ordinary gap that starts at the reply', async t => {
  const { paths } = await synthetic(t, { 'after-reply.json': [
    signal('msg_s1', 'heartbeat', '2026-10-10T00:00:00Z'),
    question('msg_q1', '2026-10-10T00:00:10Z'),
    reply('msg_r1', 'msg_q1', '2026-10-10T00:01:40Z'),
    signal('msg_s2', 'heartbeat', '2026-10-10T00:08:40Z'),
    signal('msg_s3', 'worker_done', '2026-10-10T00:09:00Z'),
  ] });
  const entry = dispatch(result(runHelper(paths), 'over', 'silence after the reply'), DISPATCH);
  assert.deepEqual(entry.questionWaits, [{
    from: point('msg_q1', '2026-10-10T00:00:10Z', 'question'), to: point('msg_r1', '2026-10-10T00:01:40Z', 'status'), seconds: 90, replyId: 'msg_r1',
  }]);
  assert.deepEqual(entry.overGaps, [
    gap(point('msg_r1', '2026-10-10T00:01:40Z', 'status'), point('msg_s2', '2026-10-10T00:08:40Z', 'heartbeat'), 420),
  ]);
  assert.equal(entry.maxGapSeconds, 420);
  assert.equal(entry.signalCount, 4);
});

test('synthetic: a long question wait is reported at any length and kept out of maxGapSeconds and overGaps', async t => {
  const { paths } = await synthetic(t, { 'long-wait.json': [
    signal('msg_s1', 'heartbeat', '2026-10-10T00:00:00Z'),
    question('msg_q1', '2026-10-10T00:00:10Z'),
    reply('msg_r1', 'msg_q1', '2026-10-10T00:15:10Z'),
    signal('msg_s2', 'heartbeat', '2026-10-10T00:16:50Z'),
    signal('msg_s3', 'worker_done', '2026-10-10T00:18:20Z'),
  ] });
  const entry = dispatch(result(runHelper(paths), 'within', 'long question wait'), DISPATCH);
  assert.deepEqual(entry.questionWaits, [{
    from: point('msg_q1', '2026-10-10T00:00:10Z', 'question'), to: point('msg_r1', '2026-10-10T00:15:10Z', 'status'), seconds: 900, replyId: 'msg_r1',
  }]);
  assert.deepEqual(entry.overGaps, []);
  assert.equal(entry.maxGapSeconds, 100);
});

test('synthetic: a reply at the same second as the next signal still makes a question wait', async t => {
  const { paths } = await synthetic(t, { 'same-second.json': [
    question('msg_q1', '2026-10-10T00:00:00Z'),
    reply('msg_r1', 'msg_q1', '2026-10-10T00:06:00Z'),
    signal('msg_s1', 'worker_done', '2026-10-10T00:06:00Z'),
  ] });
  const entry = dispatch(result(runHelper(paths), 'within', 'reply at the next signal second'), DISPATCH);
  assert.equal(entry.questionWaits.length, 1);
  assert.equal(entry.questionWaits[0].seconds, 360);
  assert.deepEqual(entry.overGaps, []);
});

test('synthetic: a reply after the next signal leaves question -> next signal an ordinary gap', async t => {
  const { paths } = await synthetic(t, { 'late-reply.json': [
    question('msg_q1', '2026-10-10T00:00:00Z'),
    signal('msg_s1', 'heartbeat', '2026-10-10T00:06:40Z'),
    reply('msg_r1', 'msg_q1', '2026-10-10T00:07:30Z'),
    signal('msg_s2', 'worker_done', '2026-10-10T00:08:00Z'),
  ] });
  const entry = dispatch(result(runHelper(paths), 'over', 'late reply'), DISPATCH);
  assert.deepEqual(entry.questionWaits, []);
  assert.deepEqual(entry.overGaps, [
    gap(point('msg_q1', '2026-10-10T00:00:00Z', 'question'), point('msg_s1', '2026-10-10T00:06:40Z', 'heartbeat'), 400),
  ]);
});

test('synthetic: with several replies the earliest one ends the question wait', async t => {
  const { paths } = await synthetic(t, { 'two-replies.json': [
    question('msg_q1', '2026-10-10T00:00:00Z'),
    reply('msg_r2', 'msg_q1', '2026-10-10T00:02:00Z'),
    reply('msg_r1', 'msg_q1', '2026-10-10T00:01:00Z'),
    signal('msg_s1', 'worker_done', '2026-10-10T00:03:00Z'),
  ] });
  const entry = dispatch(result(runHelper(paths), 'within', 'two replies'), DISPATCH);
  assert.equal(entry.questionWaits.length, 1);
  assert.equal(entry.questionWaits[0].replyId, 'msg_r1');
  assert.equal(entry.questionWaits[0].seconds, 60);
  assert.equal(entry.maxGapSeconds, 120);
});

test('synthetic: equal created_at orders by sequence, else by input order (supplement 2)', async t => {
  const withSequence = await synthetic(t, { 'sequence.json': [
    signal('msg_a', 'status', '2026-10-10T00:00:00Z', { sequence: 2 }),
    signal('msg_b', 'heartbeat', '2026-10-10T00:00:00Z', { sequence: 1 }),
    signal('msg_c', 'worker_done', '2026-10-10T00:06:40Z', { sequence: 3 }),
  ] });
  const bySequence = dispatch(result(runHelper(withSequence.paths), 'over', 'with sequence'), DISPATCH);
  assert.deepEqual(bySequence.overGaps.map(entry => [entry.from.id, entry.to.id, entry.seconds]), [['msg_a', 'msg_c', 400]]);

  const withoutSequence = await synthetic(t, {
    'first.json': [signal('msg_a', 'status', '2026-10-10T00:00:00Z')],
    'second.json': [signal('msg_b', 'heartbeat', '2026-10-10T00:00:00Z'), signal('msg_c', 'worker_done', '2026-10-10T00:06:40Z')],
  });
  const byInput = dispatch(result(runHelper(withoutSequence.paths), 'over', 'without sequence'), DISPATCH);
  assert.deepEqual(byInput.overGaps.map(entry => [entry.from.id, entry.to.id, entry.seconds]), [['msg_b', 'msg_c', 400]]);
});

test('synthetic: leading _keepalive lines of a wait output are skipped', async t => {
  const keepalive = '{"_keepalive":true,"_heartbeat":true,"elapsedMs":15008,"deadlineMs":900000}';
  const check = { id: 'synthetic', ok: true, result: { messages: [
    signal('msg_s1', 'heartbeat', '2026-10-10T00:00:00Z'), signal('msg_s2', 'worker_done', '2026-10-10T00:01:00Z'),
  ], count: 2 } };
  const { paths } = await synthetic(t, { 'wait.txt': `${keepalive}\n${keepalive}\n${keepalive}\n${JSON.stringify(check, null, 2)}\n` });
  assert.equal(dispatch(result(runHelper(paths), 'within', 'wait output'), DISPATCH).signalCount, 2);
});

const inputErrors = [
  ['no file argument', () => [], null],
  ['a missing file', ({ folder }) => [join(folder, 'missing.json')], 'missing.json'],
  ['a file that is not JSON', ({ paths }) => [paths[0]], 'broken.txt'],
  ['JSON of an unknown shape', ({ paths }) => [paths[1]], 'unknown.json'],
  ['a threshold that is not a number', ({ paths }) => [paths[2], '--threshold-seconds', 'abc'], null],
  ['a readable input without any Dispatch', ({ paths }) => [paths[3]], 'lead-only.json'],
  ['an empty message array', ({ paths }) => [paths[4]], 'empty.json'],
];

for (const [label, args, named] of inputErrors) {
  test(`synthetic: ${label} is an input error (exit 2) with diagnostics`, async t => {
    const files = await synthetic(t, {
      'broken.txt': '{"id": ',
      'unknown.json': { foo: 1 },
      'valid.json': [signal('msg_s1', 'heartbeat', '2026-10-10T00:00:00Z'), signal('msg_s2', 'worker_done', '2026-10-10T00:01:00Z')],
      'lead-only.json': [{ id: 'msg_lead', type: 'status', from_handle: 'term_synthetic-lead', to_handle: 'term_synthetic-main',
        thread_id: null, created_at: '2026-10-10T00:00:00Z', subject: '[Rules 리드 Opus] 보고', payload: null }],
      'empty.json': [],
    });
    const output = result(runHelper(args(files)), 'input-error', label);
    assertDiagnostics(output, label);
    if (named) {
      assert.ok(output.diagnostics.some(diagnostic => diagnostic.path.includes(named)), `${label}: a diagnostic names ${named}`);
    }
  });
}

test('the helper writes nothing next to its inputs or in its working folder', async t => {
  const folder = await mkdtemp(join(tmpdir(), 'check-liveness-'));
  t.after(() => rm(folder, { recursive: true, force: true }));
  const names = await readdir(fixturesDirectory);
  for (const name of names) await copyFile(fixture(name), join(folder, name));
  const before = await Promise.all(names.map(name => readFile(join(folder, name))));
  const run = runHelper(names.map(name => join(folder, name)), { cwd: folder });
  result(run, 'over', 'all fixtures');
  assert.deepEqual((await readdir(folder)).sort(), [...names].sort(), 'no new file');
  const after = await Promise.all(names.map(name => readFile(join(folder, name))));
  names.forEach((name, index) => assert.ok(before[index].equals(after[index]), `${name} unchanged`));
});

// Independent regressions for 99_Tools/Orca. Expected outcomes come from the approved rules, not
// from message-policy.mjs:
// - empty heartbeat exception: Main msg_8639aeaf7a12 and the boundary reply msg_9b26491a426e
//   (type heartbeat, three identities match, no content; exact `alive` only; null/absent/blank is
//   empty; non-string values and missing identities exit 2; real identity/tag mismatches exit 1);
// - the first four real heartbeats with a tagged subject and empty body did not match the tag
//   policy: Main msg_29e3012b0274;
// - official blocking ask `Question` and its three counterexamples: Main msg_09a19a463a74;
// - the ask evidence is the question receipt ID, the coordinator's CLI version and that CLI's
//   `ask --help` / `reply --help` stdout without `--subject`, replacing the 1.4.218 constant (fifth
//   decision of the previous Rules goal, relayed in msg_a73d4d5bbb3c); missing evidence, help with a
//   subject option and identity mismatch are rejected (Main msg_e6c8eb971f30 M2); outcomes and
//   diagnostic codes from the receive-helper test contract v1 Q1~Q6 (task_315c27807b1b);
// - result contract: exit 0 allowed, 1 policy violation, 2 input or tool failure; JSON stdout with
//   cause and repair; no writes, messages, ack or lifecycle changes (helper contract of the
//   operating-rules PR and the published ORCA `dispatch-message-policy` table).
// Inputs are real Orca messages and CLI help stdout copied with their provenance into
// observed-messages.json. Boundary cases change one field of a real message or evidence and name it.
//
// Environment: ORCA_TESTS_BASH (a POSIX bash for the workflow step; Windows default is Git Bash).
import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { existsSync } from 'node:fs';
import { copyFile, mkdir, mkdtemp, readFile, readdir, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { delimiter, dirname, join } from 'node:path';
import { after, before, test } from 'node:test';
import { fileURLToPath } from 'node:url';

import { evaluateMessage } from '../Orca/message-policy.mjs';

const repositoryRoot = fileURLToPath(new URL('../../', import.meta.url));
const cliPath = join(repositoryRoot, '99_Tools/Orca/check-message.mjs');
const workflowPath = join(repositoryRoot, '.github/workflows/code-rules.yml');
const fixture = JSON.parse(await readFile(new URL('./observed-messages.json', import.meta.url), 'utf8'));

// The published status / exit table (ORCA dispatch-message-policy, Tools README).
const documentedExit = { allowed: 0, 'policy-violation': 1, 'input-error': 2 };

const real = name => structuredClone(fixture.messages[name].message);
const solExpected = () => structuredClone(fixture.identities.operatingRulesSol.expected);
const resumeLinkExpected = () => structuredClone(fixture.identities.resumeLinkSol.expected);
const tddCanonExpected = () => structuredClone(fixture.identities.tddCanonSol.expected);
const parsedPayload = message => JSON.parse(message.payload);

// The same message with one field replaced; `undefined` removes the field.
function changed(message, changes) {
  const copy = structuredClone(message);
  for (const [key, value] of Object.entries(changes)) {
    if (value === undefined) delete copy[key];
    else copy[key] = value;
  }
  return copy;
}

function withPayload(message, changes) {
  return changed(message, { payload: JSON.stringify(changed(parsedPayload(message), changes)) });
}

function assertDecision(result, status, label) {
  assert.equal(result.status, status, `${label}: ${JSON.stringify(result)}`);
  assert.equal(result.exitCode, documentedExit[status], `${label}: exit code`);
  if (status === 'allowed') return;
  assert.ok(Array.isArray(result.diagnostics) && result.diagnostics.length > 0, `${label}: diagnostics`);
  for (const diagnostic of result.diagnostics) {
    for (const field of ['code', 'path', 'message', 'repair']) {
      assert.equal(typeof diagnostic[field], 'string', `${label}: diagnostic ${field}`);
      assert.ok(diagnostic[field].trim().length > 0, `${label}: empty diagnostic ${field}`);
    }
  }
}

function assertPlainAllowed(result, label) {
  assertDecision(result, 'allowed', label);
  assert.ok(!result.exception, `${label}: an ordinary tagged message is not an exception`);
}

function assertException(result, label) {
  assertDecision(result, 'allowed', label);
  assert.equal(typeof result.exception, 'string', `${label}: the exception must be shown`);
  assert.ok(result.exception.length > 0, `${label}: the exception must be named`);
  return result.exception;
}

const diagnosticPaths = result => result.diagnostics.map(diagnostic => diagnostic.path).join(' ');
const diagnosticCodes = result => result.diagnostics.map(diagnostic => diagnostic.code);

function assertRejected(result, status, code, label) {
  assertDecision(result, status, label);
  assert.ok(diagnosticCodes(result).includes(code), `${label}: diagnostic ${code}: ${JSON.stringify(result)}`);
}

test('real worker_done messages with JSON string payloads are accepted for the confirmed Dispatch', () => {
  assertPlainAllowed(evaluateMessage({ message: real('workerDoneStringPayload'), expected: resumeLinkExpected() }),
    'resume-link worker_done');
  assertPlainAllowed(evaluateMessage({ message: real('solWorkerDone'), expected: solExpected() }),
    'operating-rules worker_done');
  assertPlainAllowed(evaluateMessage({ message: real('solEscalation'), expected: solExpected() }),
    'tagged escalation');
});

test('an already parsed payload object gets the same decision as its JSON string', () => {
  const cases = [
    ['workerDoneStringPayload', resumeLinkExpected(), 'allowed'],
    ['heartbeatAlive', solExpected(), 'allowed'],
    ['heartbeatTaggedSubject1', solExpected(), 'policy-violation'],
  ];
  for (const [name, expected, status] of cases) {
    const message = real(name);
    const asObject = changed(message, { payload: parsedPayload(message) });
    assertDecision(evaluateMessage({ message, expected }), status, `${name} string payload`);
    assertDecision(evaluateMessage({ message: asObject, expected }), status, `${name} object payload`);
  }
});

test('the official preamble heartbeat (subject alive, empty body) is accepted without tags', () => {
  assertException(evaluateMessage({ message: real('heartbeatAlive'), expected: solExpected() }), 'msg_e36b193bbd62');
});

test('absent, null, empty and blank subject/body all count as an empty heartbeat', () => {
  const emptyValues = [['absent', undefined], ['null', null], ['empty', ''], ['blank', ' \t ']];
  for (const [subjectLabel, subject] of [...emptyValues, ['alive', 'alive']]) {
    for (const [bodyLabel, body] of emptyValues) {
      const message = changed(real('heartbeatAlive'), { subject, body });
      assertException(evaluateMessage({ message, expected: solExpected() }), `subject ${subjectLabel}, body ${bodyLabel}`);
    }
  }
});

test('the first four real heartbeats with a tagged subject and empty body are policy violations', () => {
  for (const name of ['heartbeatTaggedSubject1', 'heartbeatTaggedSubject2', 'heartbeatTaggedSubject3',
    'heartbeatTaggedSubject4']) {
    const result = evaluateMessage({ message: real(name), expected: solExpected() });
    assertDecision(result, 'policy-violation', name);
    assert.match(diagnosticPaths(result), /body/, `${name}: the diagnostic must point at the untagged body`);
  }
});

test('a heartbeat with content in its subject, body or payload must carry the tag', () => {
  const alive = real('heartbeatAlive');
  const cases = [
    ['untagged body with alive subject', changed(alive, { body: '18:02 진행: 링크 대조 중' }), 'policy-violation'],
    ['untagged body with empty subject', changed(alive, { subject: '', body: '진행 중' }), 'policy-violation'],
    ['subject other than exact alive', changed(alive, { subject: 'Alive' }), 'policy-violation'],
    ['alive with trailing text', changed(alive, { subject: 'alive - blocked' }), 'policy-violation'],
    ['extra payload content', withPayload(alive, { note: '막힘: 권한 확인' }), 'policy-violation'],
    ['tagged subject and tagged body', changed(alive, { subject: '[Rules Sol] 진행', body: '[Rules Sol] 링크 대조 중' }),
      'allowed'],
  ];
  for (const [label, message, status] of cases) {
    const result = evaluateMessage({ message, expected: solExpected() });
    if (status === 'allowed') assertPlainAllowed(result, label);
    else assertDecision(result, status, label);
  }
});

test('non-string heartbeat text or phase is an input error, never an empty heartbeat', () => {
  const alive = real('heartbeatAlive');
  const cases = [
    ['numeric phase', withPayload(alive, { phase: 3 })],
    ['object phase', withPayload(alive, { phase: { step: 'reviewing' } })],
    ['numeric body', changed(alive, { body: 0 })],
    ['array subject', changed(alive, { subject: ['alive'] })],
  ];
  for (const [label, message] of cases) {
    assertDecision(evaluateMessage({ message, expected: solExpected() }), 'input-error', label);
  }
});

test('every identity must equal the coordinator-confirmed Dispatch, even when the tag matches', () => {
  // ctx_4c94e3f19113 / task_206430c1244f were an earlier, settled Dispatch of the same Run.
  for (const name of ['heartbeatAlive', 'solWorkerDone']) {
    const message = real(name);
    const cases = [
      ['other sender', changed(message, { from_handle: 'term_2765654b-79f9-4a83-9c32-6e47e38fda47' })],
      ['other task', withPayload(message, { taskId: 'task_206430c1244f' })],
      ['stale Dispatch', withPayload(message, { dispatchId: 'ctx_4c94e3f19113' })],
    ];
    for (const [label, mismatched] of cases) {
      assertDecision(evaluateMessage({ message: mismatched, expected: solExpected() }), 'policy-violation',
        `${name}: ${label}`);
    }
  }
  const otherDispatch = { ...solExpected(), dispatchId: 'ctx_8fa3a4c5566c' };
  assertDecision(evaluateMessage({ message: real('solWorkerDone'), expected: otherDispatch }), 'policy-violation',
    'expected names a different active Dispatch');
});

test('missing or malformed identities and payloads are input errors, never accepted', () => {
  const alive = real('heartbeatAlive');
  const cases = [
    ['payload without taskId', withPayload(alive, { taskId: undefined })],
    ['payload without dispatchId', withPayload(alive, { dispatchId: undefined })],
    ['empty taskId', withPayload(alive, { taskId: '' })],
    ['payload absent', changed(alive, { payload: undefined })],
    ['payload null', changed(alive, { payload: null })],
    ['payload is not JSON', changed(alive, { payload: '{taskId:' })],
    ['payload is a JSON array', changed(alive, { payload: '[]' })],
    ['sender absent', changed(alive, { from_handle: undefined })],
    ['real Main status to the Run (no Dispatch payload)', real('mainStatusToRun')],
  ];
  for (const [label, message] of cases) {
    assertDecision(evaluateMessage({ message, expected: solExpected() }), 'input-error', label);
  }
  const padded = changed(alive, { from_handle: ` ${alive.from_handle}` });
  assert.notEqual(evaluateMessage({ message: padded, expected: solExpected() }).status, 'allowed',
    'a padded sender identity is not the confirmed identity');
});

test('expected identities are required input from the coordinator, not derived from the message', () => {
  const message = real('solWorkerDone');
  const cases = [
    ['expected absent', { message }],
    ['expected fromHandle absent', { message, expected: { ...solExpected(), fromHandle: undefined } }],
    ['expected taskId empty', { message, expected: { ...solExpected(), taskId: '' } }],
    ['expected dispatchId absent', { message, expected: { ...solExpected(), dispatchId: undefined } }],
    ['expected tag absent', { message, expected: { ...solExpected(), tag: undefined } }],
    ['message absent', { expected: solExpected() }],
    ['input is not an object', 'message'],
  ];
  for (const [label, input] of cases) {
    assertDecision(evaluateMessage(input), 'input-error', label);
  }
  const unbracketed = evaluateMessage({ message, expected: { ...solExpected(), tag: 'Rules Sol' } });
  assert.notEqual(unbracketed.status, 'allowed', 'a tag without brackets is not an assigned sender tag');
});

test('unsupported message types are not judged by this helper', () => {
  const message = changed(real('solWorkerDone'), { type: 'decision_gate' });
  assertDecision(evaluateMessage({ message, expected: solExpected() }), 'input-error', 'unknown type');
});

// Official ask evidence: the question receipt ID, the CLI version the coordinator ran and that CLI's
// `ask --help` / `reply --help` stdout (fixture officialAskHelp, keyed by the captured version).
const askProof = (version = '1.4.222') => ({
  messageId: 'msg_e9ea9a930108',
  cliVersion: version,
  askHelp: fixture.officialAskHelp[version].askHelp,
  replyHelp: fixture.officialAskHelp[version].replyHelp,
});

// The real 1.4.222 question has from_handle dispatch:<Dispatch>, thread_id = its id and payload.question = body.
const askExpected = (officialAsk = askProof()) => ({
  ...tddCanonExpected(),
  fromHandle: 'dispatch:ctx_c1e8312629fb',
  officialAsk,
});

// The 1.4.218 question with the evidence shape accepted before help evidence. Its help stdout is not in
// this fixture, so it serves only as the old evidence shape and as a Question without evidence.
const legacyAskExpected = () => ({
  ...solExpected(),
  fromHandle: 'dispatch:ctx_e99a28aa1f33',
  officialAsk: { messageId: 'msg_7634533175bb', cliVersion: fixture.officialAskEvidence.cliVersion },
});

test('the official ask Question is accepted with its receipt and ask/reply help evidence without --subject', () => {
  const ask = real('officialAskQuestion1422');
  // The CLI version is the one the coordinator ran, not a constant: the 1.4.222 capture, and the
  // 1.4.223 capture taken after the update for the same real question.
  for (const version of ['1.4.222', '1.4.223']) {
    const exception = assertException(evaluateMessage({ message: ask, expected: askExpected(askProof(version)) }),
      `real ask with ${version} help evidence`);
    assert.equal(exception, 'official-blocking-ask', `${version}: the ask exception name carries no CLI version`);
  }
  const heartbeatException = assertException(
    evaluateMessage({ message: real('heartbeatAlive'), expected: solExpected() }), 'heartbeat');
  assert.equal(heartbeatException, 'empty-heartbeat', 'the heartbeat exception stays distinguishable');

  for (const [name, expected] of [['officialAskQuestion', legacyAskExpected()], ['officialAskQuestion1422', askExpected()]]) {
    assertDecision(evaluateMessage({ message: real(name), expected: { ...expected, officialAsk: undefined } }),
      'policy-violation', `${name} without receipt evidence`);
  }
});

test('missing or malformed official ask evidence is an input error, never the exception', () => {
  const ask = real('officialAskQuestion1422');
  const proof = askProof();
  // Control: the complete real evidence opens the exception, so each case below fails by its changed field.
  assertException(evaluateMessage({ message: ask, expected: askExpected(proof) }), 'complete evidence');
  const cases = [
    ['messageId absent', { messageId: undefined }],
    ['messageId empty', { messageId: '' }],
    ['messageId padded', { messageId: ` ${proof.messageId}` }],
    ['cliVersion absent', { cliVersion: undefined }],
    ['cliVersion empty', { cliVersion: '' }],
    ['cliVersion not a string', { cliVersion: 1.4 }],
    ['askHelp absent', { askHelp: undefined }],
    ['askHelp empty', { askHelp: '' }],
    ['askHelp not a string', { askHelp: [proof.askHelp] }],
    ['reply help given as askHelp', { askHelp: proof.replyHelp }],
    ['replyHelp absent', { replyHelp: undefined }],
    ['replyHelp empty', { replyHelp: '' }],
    ['replyHelp not a string', { replyHelp: [proof.replyHelp] }],
    ['ask help given as replyHelp', { replyHelp: proof.askHelp }],
    ['ask and reply help swapped', { askHelp: proof.replyHelp, replyHelp: proof.askHelp }],
  ];
  for (const [label, fields] of cases) {
    assertRejected(evaluateMessage({ message: ask, expected: askExpected(changed(proof, fields)) }), 'input-error',
      'official-ask-proof', label);
  }

  // Evidence without help: the shape accepted before, and the lead's real 1.4.222 check input of this question.
  assertRejected(evaluateMessage({ message: real('officialAskQuestion'), expected: legacyAskExpected() }), 'input-error',
    'official-ask-proof', 'previous { messageId, cliVersion: 1.4.218 } shape');
  const leadCheckInput = askExpected({ messageId: proof.messageId, cliVersion: '1.4.222' });
  assertRejected(evaluateMessage({ message: ask, expected: leadCheckInput }), 'input-error', 'official-ask-proof',
    'lead check input lead-check/pr1-ask1-check-input.json');
});

// Boundaries the test contract left open; their outcome is the receive-helper implementation contract v1 I1
// (task_7845136f2c39): evidence that is null or not an object, or a blank cliVersion, is `official-ask-proof`.
test('official ask evidence that is null, not an object or has a blank cliVersion is an input error', () => {
  const ask = real('officialAskQuestion1422');
  const proof = askProof();
  // Control: the complete real evidence opens the exception, so each case below fails by its changed value.
  assertException(evaluateMessage({ message: ask, expected: askExpected(proof) }), 'complete evidence');
  const notAnObject = [
    ['officialAsk null', null],
    ['officialAsk true, a self-declaration', true],
    ['officialAsk only the receipt ID', proof.messageId],
    ['officialAsk an array holding the evidence', [proof]],
  ];
  for (const [label, officialAsk] of notAnObject) {
    assertRejected(evaluateMessage({ message: ask, expected: askExpected(officialAsk) }), 'input-error',
      'official-ask-proof', label);
  }
  for (const [label, cliVersion] of [['cliVersion a single space', ' '], ['cliVersion tabs and a newline', '\t\n\t']]) {
    const expected = askExpected(changed(proof, { cliVersion }));
    assertRejected(evaluateMessage({ message: ask, expected }), 'input-error', 'official-ask-proof', label);
  }
});

// The real help with `--subject` added right after an anchor text that the help must contain.
function withSubjectAfter(help, anchor, addition) {
  assert.ok(help.includes(anchor), `help anchor ${JSON.stringify(anchor)}`);
  return help.replace(anchor, `${anchor}${addition}`);
}

test('help evidence that shows a --subject option does not open the exception', () => {
  const ask = real('officialAskQuestion1422');
  const proof = askProof();
  assertException(evaluateMessage({ message: ask, expected: askExpected(proof) }), 'help without --subject');
  const cases = [
    ['ask help with a --subject option line', { askHelp: withSubjectAfter(proof.askHelp, '  --question\n', '  --subject\n') }],
    ['ask help with --subject in its usage line',
      { askHelp: withSubjectAfter(proof.askHelp, '[--options <csv>] ', '[--subject <text>] ') }],
    ['reply help with a --subject option line', { replyHelp: withSubjectAfter(proof.replyHelp, '  --body\n', '  --subject\n') }],
  ];
  for (const [label, fields] of cases) {
    assertRejected(evaluateMessage({ message: ask, expected: askExpected(changed(proof, fields)) }), 'input-error',
      'official-ask-subject-option', label);
  }
});

test('valid help evidence does not excuse a sender, task or receipt mismatch', () => {
  // ctx_cb5ce542e50a / task_df85be9762bd and its question msg_fcb32bda0afe are another real Dispatch of the
  // same Run (pr3-worker-start.json and session/all2.raw.json of the 2026-10-08 goal evidence folder).
  const ask = real('officialAskQuestion1422');
  assertException(evaluateMessage({ message: ask, expected: askExpected() }), 'matching question');
  const cases = [
    ['Question from another Dispatch', changed(ask, { from_handle: 'dispatch:ctx_cb5ce542e50a' }), askExpected(),
      'identity-mismatch'],
    ['payload taskId of another task', withPayload(ask, { taskId: 'task_df85be9762bd' }), askExpected(),
      'identity-mismatch'],
    ['evidence of another question', ask, askExpected({ ...askProof(), messageId: 'msg_fcb32bda0afe' }),
      'official-ask-mismatch'],
  ];
  for (const [label, message, expected, code] of cases) {
    assertRejected(evaluateMessage({ message, expected }), 'policy-violation', code, label);
  }
});

test('Main counterexamples: untagged body, other sender and a send imitating Question are rejected', () => {
  const ask = real('officialAskQuestion1422');
  const untaggedText = '빈 heartbeat 경계를 확인합니다.';
  const untaggedBody = withPayload(changed(ask, { body: untaggedText }), { question: untaggedText });
  assertDecision(evaluateMessage({ message: untaggedBody, expected: askExpected() }), 'policy-violation',
    'Question with an untagged body');

  const otherSender = changed(ask, { from_handle: 'dispatch:ctx_4c94e3f19113' });
  assertDecision(evaluateMessage({ message: otherSender, expected: askExpected() }), 'policy-violation',
    'Question from another Dispatch');
  const workerTerminal = tddCanonExpected().fromHandle;
  const termSender = changed(ask, { from_handle: workerTerminal });
  assertDecision(evaluateMessage({ message: termSender, expected: askExpected() }), 'policy-violation',
    'Question whose sender is not the confirmed ask sender');

  // send can choose subject, thread and payload, so only coordinator receipt evidence may open the exception.
  const sendImitation = changed(ask, {
    id: 'msg_000000000001',
    from_handle: workerTerminal,
    thread_id: 'msg_000000000001',
  });
  assertDecision(evaluateMessage({ message: sendImitation, expected: tddCanonExpected() }), 'policy-violation',
    'send imitating Question without evidence');
  assertDecision(evaluateMessage({ message: sendImitation, expected: { ...askExpected(), fromHandle: sendImitation.from_handle } }),
    'policy-violation', 'send imitating Question against the real ask receipt');
  const selfDeclared = withPayload(sendImitation, { official: true });
  assertDecision(evaluateMessage({ message: selfDeclared, expected: tddCanonExpected() }), 'policy-violation',
    'Question declaring itself official');
});

test('the ask exception is limited to type question', () => {
  const ask = real('officialAskQuestion1422');
  assertException(evaluateMessage({ message: ask, expected: askExpected() }), 'the same message as type question');
  const asStatus = changed(ask, { type: 'status' });
  assert.notEqual(evaluateMessage({ message: asStatus, expected: askExpected() }).status, 'allowed',
    'a status message cannot use the ask exception');
});

test('evaluation is pure: frozen input is accepted, left unchanged and decided the same way twice', () => {
  const deepFreeze = value => {
    if (value !== null && typeof value === 'object') {
      Object.values(value).forEach(deepFreeze);
      Object.freeze(value);
    }
    return value;
  };
  for (const [message, expected] of [[real('heartbeatAlive'), solExpected()], [real('officialAskQuestion1422'), askExpected()],
    [real('heartbeatTaggedSubject1'), solExpected()]]) {
    const input = deepFreeze({ message, expected });
    const before = JSON.stringify(input);
    const first = evaluateMessage(input);
    assert.equal(JSON.stringify(input), before, 'input must stay unchanged');
    assert.deepEqual(evaluateMessage(input), first, 'same input, same decision');
  }
});

let work;

before(async () => {
  work = await mkdtemp(join(process.env.ORCA_TESTS_WORK ?? tmpdir(), 'orca-message-policy-'));
});

after(async () => {
  if (work) await rm(work, { recursive: true, force: true });
});

// A child environment without the parent test runner's context, so nested `node --test` runs alone.
function childEnvironment(extra = {}) {
  const environment = { ...process.env, ...extra };
  delete environment.NODE_TEST_CONTEXT;
  return environment;
}

function runCli(args, options = {}) {
  const result = spawnSync(process.execPath, [...(options.nodeArgs ?? []), options.cli ?? cliPath, ...args], {
    cwd: options.cwd ?? work,
    encoding: 'utf8',
    env: childEnvironment(),
  });
  return { exit: result.status, stdout: result.stdout, stderr: result.stderr, error: result.error };
}

function parseSingleJsonLine(run, label) {
  assert.match(run.stdout, /^[^\n]+\n$/, `${label}: stdout must be one JSON line: ${run.stdout}${run.stderr}`);
  return JSON.parse(run.stdout);
}

async function inputFile(name, input) {
  const path = join(work, name);
  await mkdir(dirname(path), { recursive: true });
  await writeFile(path, typeof input === 'string' || Buffer.isBuffer(input) ? input : JSON.stringify(input));
  return path;
}

test('the CLI prints one JSON decision and exits 0, 1 or 2', async () => {
  const cases = [
    ['allowed.json', { message: real('solWorkerDone'), expected: solExpected() }, 'allowed'],
    ['violation.json', { message: real('heartbeatTaggedSubject1'), expected: solExpected() }, 'policy-violation'],
    ['input-error.json', { message: real('mainStatusToRun'), expected: solExpected() }, 'input-error'],
  ];
  for (const [name, input, status] of cases) {
    const run = runCli([await inputFile(name, input)]);
    assert.equal(run.exit, documentedExit[status], `${name}: ${run.stdout}${run.stderr}`);
    assert.equal(run.stderr, '', `${name}: stderr`);
    assertDecision(parseSingleJsonLine(run, name), status, name);
  }
});

test('the CLI accepts the real official ask with help evidence and rejects its check input without help', async () => {
  const ask = real('officialAskQuestion1422');
  const accepted = runCli([await inputFile('official-ask.json', { message: ask, expected: askExpected() })]);
  assert.equal(accepted.exit, 0, `${accepted.stdout}${accepted.stderr}`);
  assert.equal(accepted.stderr, '', 'stderr');
  assert.equal(assertException(parseSingleJsonLine(accepted, 'official ask'), 'official ask'), 'official-blocking-ask');

  const withoutHelp = askExpected({ messageId: 'msg_e9ea9a930108', cliVersion: '1.4.222' });
  const rejected = runCli([await inputFile('official-ask-without-help.json', { message: ask, expected: withoutHelp })]);
  assert.equal(rejected.exit, 2, `${rejected.stdout}${rejected.stderr}`);
  assertRejected(parseSingleJsonLine(rejected, 'without help'), 'input-error', 'official-ask-proof', 'without help');
});

test('argument, file, encoding and JSON failures are input errors with JSON repair guidance', async () => {
  const valid = JSON.stringify({ message: real('solWorkerDone'), expected: solExpected() });
  const cases = [
    ['no argument', []],
    ['two arguments', [await inputFile('a.json', valid), await inputFile('b.json', valid)]],
    ['missing file', [join(work, 'missing.json')]],
    ['directory', [work]],
    ['option instead of a path', ['--help']],
    ['invalid JSON', [await inputFile('broken.json', '{"message":')]],
    ['invalid UTF-8', [await inputFile('latin1.json', Buffer.from([0x7b, 0x22, 0xff, 0x22, 0x7d]))]],
    ['JSON null', [await inputFile('null.json', 'null')]],
  ];
  for (const [label, args] of cases) {
    const run = runCli(args);
    assert.equal(run.exit, 2, `${label}: ${run.stdout}${run.stderr}`);
    assertDecision(parseSingleJsonLine(run, label), 'input-error', label);
  }
});

test('the documented PowerShell use works: Korean path with a space and a UTF-8 BOM', async () => {
  // Windows PowerShell 5.1 `Set-Content -Encoding UTF8` writes a BOM; the README example path is Korean.
  const input = JSON.stringify({ message: real('solWorkerDone'), expected: solExpected() });
  const path = await inputFile(join('수신 입력', '수신입력.json'), Buffer.concat([Buffer.from([0xef, 0xbb, 0xbf]),
    Buffer.from(input, 'utf8')]));
  const run = runCli([path]);
  assert.equal(run.exit, 0, `${run.stdout}${run.stderr}`);
  assertPlainAllowed(parseSingleJsonLine(run, 'BOM input'), 'BOM input');
});

// Node's permission model denies file writes and child processes unless granted.
function permissionFlag() {
  if (process.allowedNodeEnvironmentFlags.has('--permission')) return '--permission';
  if (process.allowedNodeEnvironmentFlags.has('--experimental-permission')) return '--experimental-permission';
  throw new Error(`Node ${process.version} has no permission model to prove the CLI is read-only`);
}

test('the CLI only reads: it decides the same with writes and child processes denied', async () => {
  const cwd = join(work, 'read-only-cwd');
  await mkdir(cwd);
  const nodeArgs = [permissionFlag(), '--allow-fs-read=*'];
  const cases = [
    ['allowed.json', { message: real('heartbeatAlive'), expected: solExpected() }, 0],
    ['violation.json', { message: real('heartbeatTaggedSubject2'), expected: solExpected() }, 1],
    ['missing.json', null, 2],
  ];
  for (const [name, input, exit] of cases) {
    const path = input === null ? join(work, 'absent.json') : await inputFile(join('read-only', name), input);
    const run = runCli([path], { cwd, nodeArgs });
    assert.equal(run.exit, exit, `${name}: ${run.stdout}${run.stderr}`);
    parseSingleJsonLine(run, name);
  }
  assert.deepEqual(await readdir(cwd), [], 'the CLI must not create files in its working directory');
});

test('a policy module that cannot be loaded is a tool failure with exit 2, not a decision', async () => {
  const alone = join(work, 'cli-without-policy');
  await mkdir(alone);
  const copiedCli = join(alone, 'check-message.mjs');
  await copyFile(cliPath, copiedCli);
  const input = await inputFile('tool-failure.json', { message: real('solWorkerDone'), expected: solExpected() });
  const run = runCli([input], { cli: copiedCli });
  assert.equal(run.exit, 2, `${run.stdout}${run.stderr}`);
  assertDecision(parseSingleJsonLine(run, 'missing module'), 'input-error', 'missing module');
});

// Workflow step bodies as GitHub hands them to `bash --noprofile --norc -eo pipefail {0}`
// (workflow syntax reference). Lines are read as checked out on Linux (LF).
const readWorkflowLines = async () => (await readFile(workflowPath, 'utf8')).replace(/\r\n/g, '\n').split('\n');
const indentOf = line => line.length - line.trimStart().length;

function stepKeys(lines, name) {
  const step = lines.findIndex(line => line.trim() === `- name: ${name}`);
  if (step < 0) throw new Error(`Workflow step not found: ${name}`);
  const keyIndent = indentOf(lines[step]) + 2;
  const keys = [];
  for (let index = step + 1; index < lines.length; index += 1) {
    const line = lines[index];
    if (line.trim() === '' || line.trimStart().startsWith('#')) continue;
    if (indentOf(line) < keyIndent) break;
    if (indentOf(line) === keyIndent) keys.push(index);
  }
  return { step, keyIndent, keys };
}

function bashStepBody(lines, name, allowedKeys) {
  const { step, keyIndent, keys } = stepKeys(lines, name);
  const keyText = keys.map(index => lines[index].trim());
  const unexpected = keyText.filter(text => !allowedKeys.includes(text));
  if (unexpected.length > 0 || !keyText.includes('run: |')) {
    throw new Error(`Unsupported form of step "${name}" at line ${step + 1}: ${keyText.join(' / ')}`);
  }
  const body = [];
  for (let index = keys[keyText.indexOf('run: |')] + 1; index < lines.length; index += 1) {
    if (lines[index].trim() !== '' && indentOf(lines[index]) <= keyIndent) break;
    body.push(lines[index]);
  }
  while (body.length > 0 && body.at(-1).trim() === '') body.pop();
  const bodyIndent = indentOf(body.find(line => line.trim() !== '') ?? '');
  return `${body.map(line => line.slice(bodyIndent)).join('\n')}\n`;
}

function bashExecutable() {
  if (process.env.ORCA_TESTS_BASH) return process.env.ORCA_TESTS_BASH;
  if (process.platform !== 'win32') return 'bash';
  // On Windows, `bash` on PATH may be the WSL launcher, which does not see these Windows paths.
  const gitBash = 'C:\\Program Files\\Git\\bin\\bash.exe';
  if (existsSync(gitBash)) return gitBash;
  throw new Error('Set ORCA_TESTS_BASH to a POSIX bash; the workflow step cannot be run otherwise');
}

const slashPath = path => path.replace(/\\/g, '/');
const orcaStep = 'Run independent Orca message policy regressions';

async function runOrcaStep(label, testFileText) {
  const workspace = join(work, `workflow-${label}`);
  const runnerTemp = join(workspace, 'runner-temp');
  await mkdir(join(workspace, '99_Tools', 'Orca.Tests'), { recursive: true });
  await mkdir(runnerTemp, { recursive: true });
  if (testFileText !== null) {
    await writeFile(join(workspace, '99_Tools', 'Orca.Tests', 'message-policy.test.mjs'), testFileText);
  }
  const lines = await readWorkflowLines();
  const githubEnv = join(runnerTemp, 'set_env');
  await writeFile(githubEnv, '');
  await writeFile(join(workspace, 'initializer.sh'),
    bashStepBody(lines, 'Initialize job-local paths', ['shell: bash', 'run: |']));
  await writeFile(join(workspace, 'orca-step.sh'),
    bashStepBody(lines, orcaStep, ['if: always()', 'shell: bash', 'run: |']));

  const bash = bashExecutable();
  const path = `${dirname(process.execPath)}${delimiter}${process.env.PATH ?? ''}`;
  const runStep = (script, extra) => spawnSync(bash, ['--noprofile', '--norc', '-eo', 'pipefail', script], {
    cwd: workspace,
    encoding: 'utf8',
    env: childEnvironment({ PATH: path, ...extra }),
  });
  const initializer = runStep('initializer.sh', { RUNNER_TEMP: slashPath(runnerTemp), GITHUB_ENV: slashPath(githubEnv) });
  assert.equal(initializer.status, 0, `initializer: ${initializer.stderr}${initializer.error ?? ''}`);
  const handedOver = Object.fromEntries((await readFile(githubEnv, 'utf8')).split('\n').filter(Boolean)
    .map(line => [line.slice(0, line.indexOf('=')), line.slice(line.indexOf('=') + 1)]));
  const step = runStep('orca-step.sh', handedOver);
  const results = join(runnerTemp, 'code-rules-results', 'orca-independent-tests');
  const evidence = {};
  for (const name of ['command.txt', 'stdout.txt', 'stderr.txt', 'exit.txt']) {
    evidence[name] = existsSync(join(results, name)) ? await readFile(join(results, name), 'utf8') : null;
  }
  return { exit: step.status, stdout: step.stdout, stderr: step.stderr, error: step.error, evidence };
}

const passingSuite = "import { test } from 'node:test';\ntest('passes', () => {});\n";
const failingSuite = "import assert from 'node:assert/strict';\nimport { test } from 'node:test';\n" +
  "test('fails', () => assert.equal(1, 2));\n";
const unloadableSuite = "import { test } from 'node:test';\ntest('broken', () => {\n";

test('the Orca workflow step fails for a missing, unloadable or failing suite and keeps its evidence', async () => {
  const missing = await runOrcaStep('missing', null);
  assert.notEqual(missing.exit, 0, 'a missing entry point must fail the step');
  assert.equal(missing.evidence['exit.txt'], '1\n');
  assert.match(missing.evidence['stderr.txt'] ?? '', /99_Tools\/Orca\.Tests\/message-policy\.test\.mjs/);

  for (const [label, text] of [['unloadable', unloadableSuite], ['failing', failingSuite]]) {
    const run = await runOrcaStep(label, text);
    assert.notEqual(run.exit, 0, `${label}: ${run.stdout}${run.stderr}`);
    assert.ok(run.evidence['exit.txt'] !== null && run.evidence['exit.txt'].trim() !== '0', `${label}: exit.txt`);
    assert.equal(String(run.exit), run.evidence['exit.txt'].trim(), `${label}: kept exit equals step exit`);
    assert.ok(run.evidence['stdout.txt'] !== null && run.evidence['stderr.txt'] !== null, `${label}: raw output kept`);
  }

  const passing = await runOrcaStep('passing', passingSuite);
  assert.equal(passing.exit, 0, `${passing.stdout}${passing.stderr}${passing.error ?? ''}`);
  assert.equal(passing.evidence['exit.txt'], '0\n');
  // The reporter is spec or TAP depending on the terminal; both print the pass count.
  assert.match(passing.evidence['stdout.txt'], /\bpass 1\b/);
  assert.match(passing.evidence['command.txt'], /node --test 99_Tools\/Orca\.Tests\/message-policy\.test\.mjs/);
});

test('the Orca step always runs before the artifact upload of the same results folder', async () => {
  const lines = await readWorkflowLines();
  const orca = stepKeys(lines, orcaStep);
  const upload = stepKeys(lines, 'Preserve results even on failure');
  assert.ok(orca.keys.some(index => lines[index].trim() === 'if: always()'), 'an earlier failure must not skip it');
  assert.ok(orca.step < upload.step, 'evidence must exist before the upload step');
  assert.ok(upload.keys.some(index => lines[index].trim() === 'if: always()'));
  const uploadLines = lines.slice(upload.step, upload.step + 10).map(line => line.trim());
  assert.ok(uploadLines.includes('path: ${{ runner.temp }}/code-rules-results/'),
    'the uploaded folder is the job-local results folder');
});

// Independent regressions on the real session write incidents that motivated the guard, and on the
// allowed shapes named next to them. The inputs come from observed-sources.json, which says for
// every entry whether the command is an original or a reconstruction and where it came from.
// Expected decisions come from goal 「설계 / 세션 쓰기 가드」
// (01_Phases/goals/2026-10-10-operating-tool-guards/goal.md at 852f88ca), not from the hook:
// - line 88: one call reports the first of rule 1 -> 2 -> 3 only;
// - rule 1, lines 95-96: `orca orchestration <sub>` except the read-only subcommands, `check` with
//   `--peek`/`--all` and `--help`; stdout to /dev/null or NUL, a lone `&` after it, or a pipe to a
//   next command that is not the pipe's last `tee`;
// - rule 2, lines 101-104: redirect and tee destinations, `NAME=` of the same command before the
//   hook environment, relative to cwd; outside CLAUDE_PROJECT_DIR is write-outside-checkout; a
//   Write path under a temp root is temp-write;
// - rule 3, lines 110-113: before this session's memo the first write must be the memo, or memo-first;
// - line 90: the main checkout marker turns rules 2 and 3 off and keeps rule 1;
// - line 132 「허용 모양」 and the lead's contract v1 list of allowed shapes.
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { join } from 'node:path';
import { test } from 'node:test';

import {
  EVIDENCE, UTC, assertDeny, assertNoDecision, bashInput, createWorkspace, fileToolInput, readState, runHook, toSlash,
  writeMemo,
} from './guard-fixture.mjs';

const sources = JSON.parse(await readFile(new URL('./observed-sources.json', import.meta.url), 'utf8'));
const allowed = sources.allowedShapes;

const fill = (text, ws) => text.split('{TEMP}').join(toSlash(ws.temp)).split('{EVIDENCE}').join(EVIDENCE);

function incidentInput(ws, name, options) {
  const incident = sources.incidents[name];
  if (incident.tool === 'Bash') return bashInput(ws, fill(incident.command, ws), options);
  return fileToolInput(ws, incident.tool, fill(incident.filePath, ws), options);
}

// Expected result code per incident and the goal lines that decide it.
const incidentExpectations = {
  leadScratchpadPeek: ['write-outside-checkout',
    'SP= of the same command puts the destination under TEMP, outside the project (102-103); a --peek check is not rule 1 (95); rule 2 before rule 3 (88)'],
  pr217FilesTmp: ['write-outside-checkout', 'a git command with a redirect to /tmp writes outside the project (101, 103, 113)'],
  parentFolderTerminalRead: ['write-outside-checkout',
    '`orca terminal` is not orchestration (95); ../ from cwd leaves the project (102-103); 2>/dev/null is not a file (101)'],
  reentryTempReceipt: ['write-outside-checkout', '$TEMP comes from the hook environment and is outside the project (102-103); run-current is read-only for rule 1 (95)'],
  waitBackgroundDevNull: ['mailbox-output-loss', '`check --wait` with stdout to /dev/null and a lone & (96 가·나)'],
  ackDevNull: ['mailbox-output-loss', '`check --ack` without --peek/--all with stdout to /dev/null (95, 96 가)'],
  sendTeeHead: ['mailbox-output-loss', 'send output piped to tee that is not the last command of the pipe (96 다)'],
  sendTeeGrep: ['mailbox-output-loss', 'the grep -m variant of the same pipe (96 다)'],
  codemapScratchpadReceipt: ['write-outside-checkout', 'redirect into the scratchpad under TEMP, outside the project (103)'],
  codemapScratchpadWrite: ['temp-write', 'Write file_path under TEMP (104), before rule 3 (88)'],
  receiptBeforeMemo: ['memo-first', 'an evidence folder receipt is the first write and is not the memo (111-112)'],
};

test('every recorded incident has an expected decision in this file', () => {
  assert.deepEqual(Object.keys(sources.incidents).sort(), Object.keys(incidentExpectations).sort());
  for (const [name, incident] of Object.entries(sources.incidents)) {
    assert.ok(['original', 'reconstruction'].includes(incident.kind), `${name}: kind`);
    assert.ok(incident.source?.path && incident.source?.note, `${name}: source path and note`);
  }
});

for (const [name, [code, why]] of Object.entries(incidentExpectations)) {
  test(`incident ${name} (${sources.incidents[name].kind}) is blocked as ${code}: ${why}`, async t => {
    const ws = await createWorkspace(t);
    assertDeny(runHook(ws, incidentInput(ws, name)), code, name);
  });
}

test('the scratchpad incidents stay blocked after this session wrote its memo (rule 2 does not depend on the memo)', async t => {
  const ws = await createWorkspace(t);
  writeMemo(ws);
  assertDeny(runHook(ws, incidentInput(ws, 'leadScratchpadPeek')), 'write-outside-checkout', 'leadScratchpadPeek after memo');
  assertDeny(runHook(ws, incidentInput(ws, 'codemapScratchpadWrite')), 'temp-write', 'codemapScratchpadWrite after memo');
});

test('the receipt incident is recorded as a memo-first denial and leaves the memo unrecorded (line 110, 116)', async t => {
  const ws = await createWorkspace(t);
  assertDeny(runHook(ws, incidentInput(ws, 'receiptBeforeMemo')), 'memo-first', 'receiptBeforeMemo');
  const state = await readState(ws.project);
  assert.ok(state, 'a denial adds to the session state file');
  assert.equal(state.memoWrittenAt, null, 'no memo was written');
  assert.equal(state.denials.length, 1, 'one denial');
  assert.equal(state.denials[0].code, 'memo-first');
  assert.equal(state.denials[0].tool, 'Bash');
  assert.match(state.denials[0].at, UTC);
});

test('allowed: the same evidence folder receipt passes after this session wrote its memo', async t => {
  const ws = await createWorkspace(t);
  writeMemo(ws);
  assertNoDecision(runHook(ws, bashInput(ws, fill(allowed.receiptAfterMemo, ws))), 'receiptAfterMemo');
});

test('allowed: the memo is the first write of the session and is recorded (line 112)', async t => {
  const ws = await createWorkspace(t);
  writeMemo(ws);
  const state = await readState(ws.project);
  assert.ok(state, 'the memo write is recorded in the session state');
  assert.match(String(state.memoWrittenAt), UTC, 'memoWrittenAt is a UTC time');
});

test('allowed: a check --wait opened with run_in_background and no trailing & passes before the memo', async t => {
  const ws = await createWorkspace(t);
  assertNoDecision(runHook(ws, bashInput(ws, allowed.backgroundWait, { runInBackground: true })), 'backgroundWait');
});

test('allowed: check --peek redirected into the evidence folder passes after the memo and is a memo-first write before it', async t => {
  const ws = await createWorkspace(t);
  const input = bashInput(ws, fill(allowed.peekToEvidence, ws));
  assertDeny(runHook(ws, input), 'memo-first', 'peekToEvidence before memo');
  writeMemo(ws);
  assertNoDecision(runHook(ws, input), 'peekToEvidence after memo');
});

test('allowed: a read-only orchestration subcommand piped to node passes before the memo', async t => {
  const ws = await createWorkspace(t);
  assertNoDecision(runHook(ws, bashInput(ws, allowed.readOnlyPipe)), 'readOnlyPipe');
});

test('allowed: commands with only 2>/dev/null pass before the memo', async t => {
  const ws = await createWorkspace(t);
  assertNoDecision(runHook(ws, bashInput(ws, allowed.stderrOnlyWait, { runInBackground: true })), 'stderrOnlyWait');
  assertNoDecision(runHook(ws, bashInput(ws, allowed.stderrOnlyGit)), 'stderrOnlyGit');
});

test('allowed: send output piped to a last tee into the evidence folder passes after the memo', async t => {
  const ws = await createWorkspace(t);
  writeMemo(ws);
  assertNoDecision(runHook(ws, bashInput(ws, fill(allowed.lastTee, ws))), 'lastTee');
});

test('allowed: git commands without a redirect pass before the memo', async t => {
  const ws = await createWorkspace(t);
  for (const command of allowed.gitCommands) assertNoDecision(runHook(ws, bashInput(ws, command)), command);
});

test('allowed: with the main checkout marker, TEMP writes pass without a memo and rule 1 still blocks (line 90)', async t => {
  const ws = await createWorkspace(t, { marker: true });
  assertNoDecision(runHook(ws, bashInput(ws, allowed.mainCheckoutTempRedirect)), 'main checkout $TEMP redirect');
  assertNoDecision(runHook(ws, fileToolInput(ws, 'Write', join(ws.temp, 'notes.md'))), 'main checkout Write under TEMP');
  assertNoDecision(runHook(ws, incidentInput(ws, 'codemapScratchpadReceipt')), 'main checkout scratchpad redirect');
  assertNoDecision(runHook(ws, incidentInput(ws, 'receiptBeforeMemo')), 'main checkout receipt without memo');
  assertDeny(runHook(ws, bashInput(ws, allowed.mainCheckoutWaitLoss)), 'mailbox-output-loss', 'main checkout wait output loss');
});

// Independent regressions for the hook contract shared by the three rules. Every input here is a
// synthetic boundary case. Expected outcomes come from goal 「설계 / 세션 쓰기 가드」
// (01_Phases/goals/2026-10-10-operating-tool-guards/goal.md lines 86-91, 110 and 116 at 852f88ca)
// and the lead's supplement 1 item 8 (E/tdd/work/ask1-answer.txt), not from the hook:
// - output: one deny line only when blocking, empty stdout otherwise, never allow or ask, exit 0;
// - one call reports the first of rule 1 -> 2 -> 3;
// - Bash has rules 1-3, Write/Edit/MultiEdit/NotebookEdit rules 2 (temp) and 3, other tools no
//   decision;
// - input errors and internal failures give no decision;
// - state: `.claude/state/session-guard/<session_id>.json` in the form
//   {"version":1,"memoWrittenAt":"<UTC>"|null,"denials":[{"at","tool","code","target"}],"errors":[…]},
//   saved through a temporary file in the same folder and a rename; session_id is accepted only as
//   one file name segment; every denial is appended, and a failed save keeps the decision;
//   a denial target is a string for rules 2 and 3 and a string or null for rule 1.
import assert from 'node:assert/strict';
import { existsSync } from 'node:fs';
import { mkdir, writeFile } from 'node:fs/promises';
import { join } from 'node:path';
import { test } from 'node:test';

import {
  EVIDENCE, SESSION, UTC, assertDeny, assertNoDecision, bashInput, createWorkspace, fileToolInput, listFiles, readState, runHook,
  runReport, stateDirectory, toSlash, toolInput, writeMemo,
} from './guard-fixture.mjs';

const WAIT_LOST = 'orca orchestration check --wait --json > /dev/null';

test('the hook reports only the first rule of 1 -> 2 -> 3 that a call breaks (line 88)', async t => {
  const ws = await createWorkspace(t);
  const outside = `${toSlash(ws.outside)}/y.txt`;
  assertDeny(runHook(ws, bashInput(ws, `${WAIT_LOST}; echo x > "${outside}"`)), 'mailbox-output-loss', 'rules 1 and 2');
  assertDeny(runHook(ws, bashInput(ws, `echo x > ${EVIDENCE}/a.txt; ${WAIT_LOST}`)), 'mailbox-output-loss', 'rules 3 and 1');
  assertDeny(runHook(ws, bashInput(ws, `echo x > ${EVIDENCE}/a.txt; echo x > "${outside}"`)), 'write-outside-checkout', 'rules 3 and 2');
  assertDeny(runHook(ws, fileToolInput(ws, 'Write', join(ws.temp, 'x.md'))), 'temp-write', 'Write under TEMP before the memo');
});

const otherTools = [
  ['Read', { file_path: '/tmp/x.md' }],
  ['Grep', { pattern: 'x', path: '/tmp' }],
  ['Glob', { pattern: '**/*.md', path: '/tmp' }],
  ['WebFetch', { url: 'https://example.invalid/', prompt: 'x' }],
  ['Agent', { description: 'x', prompt: 'echo x > /tmp/y', subagent_type: 'general-purpose' }],
  ['TodoWrite', { todos: [] }],
];

for (const [toolName, input] of otherTools) {
  test(`the hook gives no decision for ${toolName} (line 89)`, async t => {
    const ws = await createWorkspace(t);
    assertNoDecision(runHook(ws, toolInput(ws, toolName, input)), toolName);
  });
}

const invalidInputs = [
  ['', 'empty stdin'],
  ['not json', 'text that is not JSON'],
  ['{"session_id":', 'truncated JSON'],
  ['[]', 'a JSON array'],
  ['"text"', 'a JSON string'],
  ['null', 'JSON null'],
  ['{}', 'an empty object'],
  [JSON.stringify({ session_id: SESSION, hook_event_name: 'PreToolUse', tool_name: 'Bash' }), 'Bash without tool_input'],
  [JSON.stringify({ session_id: SESSION, hook_event_name: 'PreToolUse', tool_name: 'Bash', tool_input: { command: 7 } }), 'command is not a string'],
];

for (const [raw, label] of invalidInputs) {
  test(`the hook gives no decision for an input error: ${label} (line 91)`, async t => {
    const ws = await createWorkspace(t);
    assertNoDecision(runHook(ws, null, { raw }), label);
  });
}

test('the hook never writes a state file outside its folder for a session_id that is not one file name segment', async t => {
  const ws = await createWorkspace(t);
  for (const session of ['../escape', 'a/b', 'a\\b', '..', '']) {
    const run = runHook(ws, bashInput(ws, `echo x > ${EVIDENCE}/a.txt`, { session }));
    assert.equal(run.exit, 0, runReport(run, `session_id ${JSON.stringify(session)}: exit 0`));
    assert.ok(!/"permissionDecision":"(allow|ask)"/.test(run.stdout), runReport(run, `session_id ${JSON.stringify(session)}: no allow or ask`));
  }
  assert.ok(!existsSync(join(ws.project, '.claude', 'state', 'escape.json')), 'no state file above the session-guard folder');
  assert.ok(!existsSync(join(ws.root, 'escape.json')), 'no state file outside the project');
  for (const file of await listFiles(join(ws.project, '.claude'))) {
    assert.ok(file.startsWith('state/session-guard/'), `unexpected file ${file}`);
  }
});

test('a denial is saved in the documented state format (lines 110, 116)', async t => {
  const ws = await createWorkspace(t);
  assertDeny(runHook(ws, bashInput(ws, `echo x > "${toSlash(ws.outside)}/y.txt"`)), 'write-outside-checkout', 'denial');
  const state = await readState(ws.project);
  assert.ok(state, 'state file exists after a denial');
  assert.deepEqual(Object.keys(state).sort(), ['denials', 'errors', 'memoWrittenAt', 'version']);
  assert.equal(state.version, 1);
  assert.equal(state.memoWrittenAt, null);
  assert.ok(Array.isArray(state.errors), 'errors is an array');
  assert.equal(state.denials.length, 1);
  const [denial] = state.denials;
  assert.deepEqual(Object.keys(denial).sort(), ['at', 'code', 'target', 'tool']);
  assert.match(denial.at, UTC);
  assert.equal(denial.tool, 'Bash');
  assert.equal(denial.code, 'write-outside-checkout');
  assert.equal(typeof denial.target, 'string', 'a rule 2 target is a string');
});

test('every denial is appended in order with its tool, code and target kind', async t => {
  const ws = await createWorkspace(t);
  assertDeny(runHook(ws, bashInput(ws, WAIT_LOST)), 'mailbox-output-loss', 'first');
  assertDeny(runHook(ws, fileToolInput(ws, 'Write', join(ws.temp, 'x.md'))), 'temp-write', 'second');
  assertDeny(runHook(ws, fileToolInput(ws, 'Write', join(ws.evidence, 'notes.md'))), 'memo-first', 'third');
  const state = await readState(ws.project);
  assert.deepEqual(state.denials.map(denial => [denial.tool, denial.code]),
    [['Bash', 'mailbox-output-loss'], ['Write', 'temp-write'], ['Write', 'memo-first']]);
  assert.ok(state.denials[0].target === null || typeof state.denials[0].target === 'string', 'rule 1 target is a string or null');
  assert.equal(typeof state.denials[1].target, 'string', 'rule 2 target is a string');
  assert.equal(typeof state.denials[2].target, 'string', 'rule 3 target is a string');
  for (const denial of state.denials) assert.match(denial.at, UTC);
});

test('the state folder keeps only session files after saves (temporary file + rename)', async t => {
  const ws = await createWorkspace(t);
  assertDeny(runHook(ws, bashInput(ws, WAIT_LOST)), 'mailbox-output-loss', 'denial');
  writeMemo(ws);
  assertDeny(runHook(ws, bashInput(ws, `echo x > "${toSlash(ws.outside)}/y.txt"`)), 'write-outside-checkout', 'denial after memo');
  assert.deepEqual(await listFiles(stateDirectory(ws.project)), [`${SESSION}.json`]);
});

test('a failed state save keeps the decision (line 116)', async t => {
  const ws = await createWorkspace(t);
  await mkdir(join(ws.project, '.claude', 'state'), { recursive: true });
  await writeFile(stateDirectory(ws.project), 'not a folder');
  assertDeny(runHook(ws, bashInput(ws, `echo x > "${toSlash(ws.outside)}/y.txt"`)), 'write-outside-checkout', 'rule 2 with an unwritable state');
  assertDeny(runHook(ws, bashInput(ws, WAIT_LOST)), 'mailbox-output-loss', 'rule 1 with an unwritable state');
  assertDeny(runHook(ws, fileToolInput(ws, 'Write', join(ws.evidence, 'notes.md'))), 'memo-first', 'rule 3 with an unwritable state');
});

test('a subagent call is judged by the same rules (supplement 1 item 4)', async t => {
  const ws = await createWorkspace(t);
  assertDeny(runHook(ws, bashInput(ws, WAIT_LOST, { agent: true })), 'mailbox-output-loss', 'subagent rule 1');
  assertDeny(runHook(ws, bashInput(ws, `echo x > "${toSlash(ws.outside)}/y.txt"`, { agent: true })), 'write-outside-checkout',
    'subagent rule 2');
  assertDeny(runHook(ws, fileToolInput(ws, 'Write', join(ws.temp, 'x.md'), { agent: true })), 'temp-write', 'subagent temp-write');
});

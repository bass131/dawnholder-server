// Independent verifier's regressions for two hook boundaries the first suite leaves open. Expected
// outcomes come from goal 「설계 / 세션 쓰기 가드」
// (01_Phases/goals/2026-10-10-operating-tool-guards/goal.md at 484d74c7), not from the hook:
// - line 93: an internal failure gives no decision and is kept in the session state errors when
//   that is possible; a session file the hook cannot read still has a valid session and project;
// - line 99 with lines 114 and 134: the rule 1 repair (wait output saved to an evidence file,
//   opened with run_in_background) is not a rule 1 shape; the file is a write for rule 3.
import assert from 'node:assert/strict';
import { mkdir, writeFile } from 'node:fs/promises';
import { test } from 'node:test';

import {
  EVIDENCE, UTC, assertDeny, assertNoDecision, bashInput, createWorkspace, readState, runHook, stateDirectory, statePath,
  toSlash, writeMemo,
} from './guard-fixture.mjs';

test('an unreadable session state gives no decision and keeps the failure in errors (line 93)', async t => {
  const ws = await createWorkspace(t);
  await mkdir(stateDirectory(ws.project), { recursive: true });
  await writeFile(statePath(ws.project), '{');
  const outside = `${toSlash(ws.outside)}/x.txt`;

  assertNoDecision(runHook(ws, bashInput(ws, `echo x > "${outside}"`)), 'Bash write outside with a broken state file');
  const state = await readState(ws.project);
  assert.equal(state.version, 1, 'the state file is in the documented form again (line 112)');
  assert.ok(state.errors.length >= 1, 'the failure is kept in errors (line 93)');
  for (const error of state.errors) {
    assert.match(error.at, UTC, 'an error has a UTC time (line 112)');
    assert.equal(typeof error.message, 'string', 'an error has a message (line 112)');
    assert.notEqual(error.message, '', 'an error message is not empty');
  }
});

test('the rule 1 repair passes rule 1 and leaves its evidence file to rule 3 (lines 99, 114, 134)', async t => {
  const ws = await createWorkspace(t);
  const wait = `orca orchestration check --run run_x --wait --timeout-ms 1000 --json > ${EVIDENCE}/wait.json`;

  assertDeny(runHook(ws, bashInput(ws, wait, { runInBackground: true })), 'memo-first', 'saved wait output before the memo');
  writeMemo(ws);
  assertNoDecision(runHook(ws, bashInput(ws, wait, { runInBackground: true })), 'saved wait output after the memo');
});

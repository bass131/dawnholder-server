// Independent regressions for rule 3, memo order. Every command and path here is a synthetic
// boundary case. Expected decisions come from goal 「설계 / 세션 쓰기 가드」 규칙 3
// (01_Phases/goals/2026-10-10-operating-tool-guards/goal.md lines 110-114 at 852f88ca), the scope
// widening of Main msg_27aefcdfcd45 quoted in goal line 43 (before this session's memo no file but
// the memo may be written explicitly, git and harness saves excluded) and the lead's supplement 1
// items 3 and 4 (E/tdd/work/ask1-answer.txt), not from the hook:
// - the session state lives in `$CLAUDE_PROJECT_DIR/.claude/state/session-guard/<session_id>.json`
//   and carries `memoWrittenAt`;
// - a memo path is a file under `.backups/verification/` of an allowed root whose file name
//   contains `context` and ends in `.md`, ignoring case; under a `.backups` junction the real
//   target path `<target>/verification/…context….md` is a memo too (supplement 1 item 3);
// - the writes of one call are read in text order: Write tools have their path, Bash has the rule 2
//   destinations including unresolved ones and without non-files; with no memo recorded, a call
//   whose first write is a memo path records the memo and passes, a call with another write
//   before the memo is memo-first; after the memo, rule 3 passes; editing an existing memo is a
//   memo write;
// - not counted: git, mkdir, cp, mv and scripts without a redirect or tee;
// - the main checkout marker turns rule 3 off (line 90);
// - a subagent call is judged the same way and shares the session file (supplement 1 item 4).
import assert from 'node:assert/strict';
import { join } from 'node:path';
import { test } from 'node:test';

import {
  EVIDENCE, MEMO, OTHER_SESSION, UNSET_VARIABLE, UTC, assertDeny, assertNoDecision, bashInput, createWorkspace, fileToolInput,
  notTempFolder, readState, runHook, toSlash, writeMemo,
} from './guard-fixture.mjs';

const memoState = async (ws, session) => (await readState(ws.project, session))?.memoWrittenAt ?? null;

test('rule 3 carries the memo across calls of one session: blocked, memo, then other writes pass', async t => {
  const ws = await createWorkspace(t);
  const notes = join(ws.evidence, 'notes.md');
  assertDeny(runHook(ws, fileToolInput(ws, 'Write', notes)), 'memo-first', 'write before the memo');
  assert.equal(await memoState(ws), null, 'no memo recorded after the blocked write');
  writeMemo(ws);
  assert.match(String(await memoState(ws)), UTC, 'memoWrittenAt is recorded as UTC');
  assertNoDecision(runHook(ws, fileToolInput(ws, 'Write', notes)), 'Write after the memo');
  assertNoDecision(runHook(ws, bashInput(ws, `echo x > ${EVIDENCE}/after.txt`)), 'redirect after the memo');
});

test('rule 3 keeps sessions apart: the memo of one session does not open another', async t => {
  const ws = await createWorkspace(t);
  writeMemo(ws);
  assertDeny(runHook(ws, bashInput(ws, `echo x > ${EVIDENCE}/other.txt`, { session: OTHER_SESSION })), 'memo-first',
    'other session before its memo');
  assert.equal(await memoState(ws, OTHER_SESSION), null, 'other session has no memo');
  writeMemo(ws, { session: OTHER_SESSION, path: join(ws.evidence, 'other-context.md') });
  assertNoDecision(runHook(ws, bashInput(ws, `echo x > ${EVIDENCE}/other.txt`, { session: OTHER_SESSION })), 'other session after its memo');
});

const memoPaths = [
  `${EVIDENCE}/lead-context.md`,
  `${EVIDENCE}/pr1-lead-context.md`,
  `${EVIDENCE}/session/reentry/lead-context-reentry.md`,
  `${EVIDENCE}/tdd/Context.MD`,
  `${EVIDENCE}/review/CONTEXT-notes.md`,
];

for (const path of memoPaths) {
  test(`rule 3 records ${path} as a memo`, async t => {
    const ws = await createWorkspace(t);
    writeMemo(ws, { path: join(ws.project, ...path.split('/')) });
    assert.match(String(await memoState(ws)), UTC, 'memo recorded');
  });
}

const notMemoPaths = [
  [`${EVIDENCE}/contract.md`, 'no context in the file name'],
  [`${EVIDENCE}/context.txt`, 'not .md'],
  [`${EVIDENCE}/context/notes.md`, 'context only in a folder name'],
  ['.backups/other/context.md', 'not under .backups/verification/'],
  ['docs/context.md', 'not under .backups'],
  [`${EVIDENCE}/clock-memo.txt`, 'the clock file the goal names (line 114)'],
];

for (const [path, why] of notMemoPaths) {
  test(`rule 3 blocks ${path} as the first write (${why})`, async t => {
    const ws = await createWorkspace(t);
    assertDeny(runHook(ws, fileToolInput(ws, 'Write', join(ws.project, ...path.split('/')))), 'memo-first', path);
    assert.equal(await memoState(ws), null, 'no memo recorded');
  });
}

test('rule 3 counts an Edit of an existing memo as the memo write (a resumed session)', async t => {
  const ws = await createWorkspace(t);
  assertNoDecision(runHook(ws, fileToolInput(ws, 'Edit', join(ws.project, ...MEMO.split('/')))), 'Edit of the memo');
  assert.match(String(await memoState(ws)), UTC, 'memo recorded by Edit');
  assertNoDecision(runHook(ws, fileToolInput(ws, 'MultiEdit', join(ws.evidence, 'notes.md'))), 'MultiEdit after the memo');
});

test('rule 3 reads the writes of one Bash call in text order', async t => {
  const clockFirst = await createWorkspace(t);
  assertDeny(runHook(clockFirst, bashInput(clockFirst, `date -u > ${EVIDENCE}/clock.txt; printf '%s\\n' '# memo' > ${MEMO}`)),
    'memo-first', 'clock file before the memo in one call');
  assert.equal(await memoState(clockFirst), null, 'the blocked call records no memo');

  const memoFirst = await createWorkspace(t);
  assertNoDecision(runHook(memoFirst, bashInput(memoFirst, `printf '%s\\n' '# memo' > ${MEMO}; date -u > ${EVIDENCE}/clock.txt`)),
    'memo then clock file in one call');
  assert.match(String(await memoState(memoFirst)), UTC, 'the memo is recorded');
});

test('rule 3 accepts a heredoc memo as the first write (supplement 1 item 7)', async t => {
  const ws = await createWorkspace(t);
  assertNoDecision(runHook(ws, bashInput(ws, `cat > ${MEMO} <<'EOF'\n# 작업 전 맥락\n> quoted line\nEOF`)), 'heredoc memo');
  assert.match(String(await memoState(ws)), UTC, 'memo recorded');
});

test('rule 3 counts redirects of git and unresolved destinations, not non-files', async t => {
  const ws = await createWorkspace(t);
  assertDeny(runHook(ws, bashInput(ws, `git diff --stat > ${EVIDENCE}/diff.txt`)), 'memo-first', 'git with a redirect');
  assertDeny(runHook(ws, bashInput(ws, `echo x > "$${UNSET_VARIABLE}/y.txt"`)), 'memo-first', 'unresolved destination');
  assertDeny(runHook(ws, bashInput(ws, `echo x | tee ${EVIDENCE}/t.txt`)), 'memo-first', 'tee file');
  assertNoDecision(runHook(ws, bashInput(ws, 'echo x > /dev/null 2>&1')), 'non-file destinations');
});

const notCounted = [
  'git status --short', 'git add -A', 'git log --oneline -3', `mkdir -p ${EVIDENCE}/work`, `cp a.txt ${EVIDENCE}/b.txt`,
  `mv a.txt ${EVIDENCE}/b.txt`, 'node 99_Tools/Backlog/check-candidates.mjs --json', 'sha256sum AGENTS.md',
];

for (const command of notCounted) {
  test(`rule 3 does not count \`${command}\` as a write before the memo`, async t => {
    const ws = await createWorkspace(t);
    assertNoDecision(runHook(ws, bashInput(ws, command)), command);
  });
}

test('rule 3 sees write tool paths outside the checkout that rule 2 leaves alone', async t => {
  const ws = await createWorkspace(t);
  const elsewhere = join(notTempFolder, 'memory', 'notes.md');
  assertDeny(runHook(ws, fileToolInput(ws, 'Write', elsewhere)), 'memo-first', 'Write outside before the memo');
  writeMemo(ws);
  assertNoDecision(runHook(ws, fileToolInput(ws, 'Write', elsewhere)), 'Write outside after the memo');
});

test('rule 3 is off with the main checkout marker (line 90)', async t => {
  const ws = await createWorkspace(t, { marker: true });
  assertNoDecision(runHook(ws, fileToolInput(ws, 'Write', join(ws.evidence, 'notes.md'))), 'Write without a memo');
  assertNoDecision(runHook(ws, bashInput(ws, `echo x > ${EVIDENCE}/r.txt`)), 'redirect without a memo');
});

test('rule 3 reports memo-first for a TEMP inside the project before the memo (rule 2 lets the allowed root win)', async t => {
  const ws = await createWorkspace(t, { tempInsideProject: true });
  assertDeny(runHook(ws, fileToolInput(ws, 'Write', join(ws.temp, 'claude', 'scratchpad', 'x.md'))), 'memo-first',
    'Write under the project TEMP before the memo');
});

test('rule 3 records a memo written at the real target of a .backups junction (supplement 1 item 3)', async t => {
  const ws = await createWorkspace(t, { junction: true });
  const realMemo = `${toSlash(ws.linkTarget)}/verification/2026-10-10-operating-tool-guards/tdd/context.md`;
  writeMemo(ws, { path: realMemo });
  assert.match(String(await memoState(ws)), UTC, 'memo recorded through the real target');
  assertNoDecision(runHook(ws, bashInput(ws, `echo x > ${EVIDENCE}/after.txt`)), 'redirect after the memo');
});

test('rule 3 records a memo written through the junction path (core-active)', async t => {
  const ws = await createWorkspace(t, { junction: true });
  writeMemo(ws);
  assert.match(String(await memoState(ws)), UTC, 'memo recorded through the link');
});

test('rule 3 judges a subagent call like the session and shares its state file (supplement 1 item 4)', async t => {
  const ws = await createWorkspace(t);
  assertDeny(runHook(ws, bashInput(ws, `echo x > ${EVIDENCE}/a.txt`, { agent: true })), 'memo-first', 'subagent before the memo');
  writeMemo(ws, { agent: true });
  assert.match(String(await memoState(ws)), UTC, 'the subagent memo is in the session file');
  assertNoDecision(runHook(ws, bashInput(ws, `echo x > ${EVIDENCE}/a.txt`)), 'the session after the subagent memo');
});

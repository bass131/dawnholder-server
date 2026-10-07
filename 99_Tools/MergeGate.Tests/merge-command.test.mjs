// Independent regressions for the `gh pr merge` pass conditions of the merge gate (PreToolUse
// Bash). Expected outcomes come from the approved rules, not from 99_Tools/MergeGate:
// - behavior contract v2 (E/merge-gate-behavior-spec.md, SHA256 d16918f6…7c9a) §2 output and
//   block codes, §4 「`gh pr merge`의 통과 조건」 checked in order 1–11 with the first failing
//   condition naming the code, 30-minute validity from createdAt (§1), and the record update on a
//   pass (usedAt now, usedCommand the original command);
// - goal 「관찰 가능한 완료조건」 1: forms (single, `&&`, heredoc, `bash -c`, `--auto`, `--admin`)
//   and counterexamples (no record, other session, other PR, other head, short head, no
//   `--match-head-commit`, expired, second use, not the main checkout);
// - goal 「실측 뒤 설계」 and design probe r5: a subagent call (agent_id present) never passes.
// Each block case changes one condition of the passing base case and expects that condition's
// code; the record file is checked separately from the decision.
//
// Environment: node only. Each test owns a temporary project passed as CLAUDE_PROJECT_DIR.
import assert from 'node:assert/strict';
import { chmod } from 'node:fs/promises';
import { test } from 'node:test';

import {
  approval, approvalsDirectory, approvalsFile, assertAllow, assertDeny, assertPromptNotBlocked, bashInput, createProject,
  HEAD_A, HEAD_B, listFiles, MINUTE, OTHER_PR, OTHER_SESSION, PR, promptInput, readApprovals, runHook, runReport,
  SESSION, writeApprovals,
} from './hook-fixture.mjs';

const mergeCommand = (method = '--squash', head = HEAD_A, pr = PR) => `gh pr merge ${pr} ${method} --match-head-commit ${head}`;
const isoUtc = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?Z$/;

// The passing base case of §4: main marker, an unused one-minute-old record of this session for
// PR 12 at HEAD_A, and the documented single command. `change` alters exactly one of these.
async function attempt(t, change = {}) {
  const fixture = await createProject(t, { marker: change.marker ?? true });
  const entries = change.approvals ?? [approval()];
  const written = entries.length > 0 ? await writeApprovals(fixture.project, entries, change.recordSession ?? SESSION) : null;
  const command = change.command ?? mergeCommand();
  const start = Date.now();
  const run = runHook(fixture, bashInput(fixture.project, command, { agent: change.agent ?? false }));
  const end = Date.now();
  const after = await readApprovals(fixture.project, change.recordSession ?? SESSION);
  return { ...fixture, command, run, written, after, window: { start, end } };
}

function assertUsedNow(entry, command, window, label) {
  assert.match(String(entry.usedAt), isoUtc, `${label}: usedAt is ISO 8601 UTC`);
  const used = Date.parse(entry.usedAt);
  assert.ok(used >= window.start - 1000 && used <= window.end + 1000, `${label}: usedAt is the pass time`);
  assert.equal(entry.usedCommand, command, `${label}: usedCommand is the original command`);
}

async function assertBlocked(t, change, code, label) {
  const result = await attempt(t, change);
  assertDeny(result.run, code, label);
  assert.deepEqual(result.after, result.written, runReport(result.run, `${label}: a blocked attempt leaves the record unchanged`));
}

test('§4 condition 11: an approval line then the single command passes and marks the record used', async t => {
  const fixture = await createProject(t);
  assertPromptNotBlocked(runHook(fixture, promptInput(fixture.project, `병합 승인: PR${PR} head ${HEAD_A}`)), 'approval');
  const before = await readApprovals(fixture.project);
  assert.ok(before, 'the approval line created a record (§3)');
  const command = mergeCommand('--squash');
  const start = Date.now();
  const run = runHook(fixture, bashInput(fixture.project, command));
  const end = Date.now();
  assertAllow(run, PR, 'approved merge');
  const [entry] = (await readApprovals(fixture.project)).approvals;
  assert.equal(entry.pr, PR, 'pr kept');
  assert.equal(entry.head, HEAD_A, 'head kept');
  assert.equal(entry.createdAt, before.approvals[0].createdAt, 'createdAt kept');
  assertUsedNow(entry, command, { start, end }, 'approved merge');
  assert.deepEqual(await listFiles(approvalsDirectory(fixture.project)), [`${SESSION}.json`], 'no temp file is left (§1)');
});

test('§4 condition 4: each merge method passes alone, also with the flags in another order', async t => {
  const commands = [
    mergeCommand('--merge'),
    mergeCommand('--squash'),
    mergeCommand('--rebase'),
    `gh pr merge ${PR} --match-head-commit ${HEAD_A} --merge`,
    `gh pr merge ${PR} --match-head-commit ${HEAD_A} --rebase`,
  ];
  for (const command of commands) {
    const result = await attempt(t, { command });
    assertAllow(result.run, PR, command);
    assertUsedNow(result.after.approvals[0], command, result.window, command);
  }
});

test('§4 condition 11: a pass updates only the record of the merged PR', async t => {
  const other = approval({ pr: OTHER_PR, head: HEAD_B });
  const result = await attempt(t, { approvals: [approval(), other] });
  assertAllow(result.run, PR, 'two records');
  assertUsedNow(result.after.approvals.find(entry => entry.pr === PR), result.command, result.window, 'merged PR');
  assert.deepEqual(result.after.approvals.find(entry => entry.pr === OTHER_PR), other, 'the other PR record is untouched');
});

test('§4 condition 9: a record younger than 30 minutes still passes', async t => {
  const result = await attempt(t, { approvals: [approval({ createdAgo: 25 * MINUTE })] });
  assertAllow(result.run, PR, '25 minutes old');
});

test('§4 condition 1 compound-command: a correct record does not let a compound form pass', async t => {
  const merge = mergeCommand();
  const forms = [
    ['&&', `git fetch origin && ${merge}`],
    [';', `${merge}; echo done`],
    ['|', `${merge} | cat`],
    ['&', `${merge} &`],
    ['>', `${merge} > merge.txt`],
    ['line break', `${merge}\necho done`],
    ['heredoc', `bash <<'EOF'\n${merge}\nEOF`],
    ['bash -c', `bash -c "${merge}"`],
    ['$( )', `echo $(${merge})`],
    ['backquote', `echo \`${merge}\``],
  ];
  for (const [label, command] of forms) {
    await assertBlocked(t, { command }, 'compound-command', label);
  }
});

test('§4 condition 2 forbidden-flag: --auto and --admin are blocked even with a correct record', async t => {
  await assertBlocked(t, { command: `gh pr merge ${PR} --squash --auto --match-head-commit ${HEAD_A}` }, 'forbidden-flag', '--auto');
  await assertBlocked(t, { command: `gh pr merge ${PR} --squash --admin --match-head-commit ${HEAD_A}` }, 'forbidden-flag', '--admin');
});

test('§4 condition 3 missing-match-head-commit: the head guard is required', async t => {
  await assertBlocked(t, { command: `gh pr merge ${PR} --squash` }, 'missing-match-head-commit', 'no head guard');
});

test('§4 condition 4 bad-form: only `gh pr merge <number> <one method> --match-head-commit <40 hex>` is accepted', async t => {
  const forms = [
    ['short head (7)', mergeCommand('--squash', HEAD_A.slice(0, 7))],
    ['short head (39)', mergeCommand('--squash', HEAD_A.slice(0, 39))],
    ['another argument', `gh pr merge ${PR} --squash --delete-branch --match-head-commit ${HEAD_A}`],
    ['no method', `gh pr merge ${PR} --match-head-commit ${HEAD_A}`],
    ['two methods', `gh pr merge ${PR} --squash --merge --match-head-commit ${HEAD_A}`],
    ['three methods', `gh pr merge ${PR} --merge --squash --rebase --match-head-commit ${HEAD_A}`],
    ['no PR number', `gh pr merge --squash --match-head-commit ${HEAD_A}`],
    ['branch instead of number', `gh pr merge feat/merge-gate --squash --match-head-commit ${HEAD_A}`],
  ];
  for (const [label, command] of forms) {
    await assertBlocked(t, { command }, 'bad-form', label);
  }
});

test('§4 condition 5 not-main-checkout: without the marker a correct record does not pass', async t => {
  await assertBlocked(t, { marker: false }, 'not-main-checkout', 'no marker');
});

test('§4 condition 6 subagent: a call with agent_id does not pass', async t => {
  await assertBlocked(t, { agent: true }, 'subagent', 'agent_id present');
});

test('§4 condition 7 no-approval: no record, another PR or another session', async t => {
  await assertBlocked(t, { approvals: [] }, 'no-approval', 'no record file');
  await assertBlocked(t, { approvals: [approval({ pr: OTHER_PR })] }, 'no-approval', 'record of another PR');
  await assertBlocked(t, { command: mergeCommand('--squash', HEAD_A, OTHER_PR) }, 'no-approval', 'command for another PR');
  await assertBlocked(t, { recordSession: OTHER_SESSION }, 'no-approval', 'record of another session');
});

test('§4 condition 8 head-mismatch: the record head must equal --match-head-commit', async t => {
  await assertBlocked(t, { command: mergeCommand('--squash', HEAD_B) }, 'head-mismatch', 'other head');
});

test('§4 condition 9 expired: a record older than 30 minutes does not pass', async t => {
  await assertBlocked(t, { approvals: [approval({ createdAgo: 31 * MINUTE })] }, 'expired', '31 minutes old');
});

test('§4 condition 10 already-used: a used record does not pass again', async t => {
  const used = approval({ usedAgo: 10 * 1000, usedCommand: mergeCommand() });
  await assertBlocked(t, { approvals: [used] }, 'already-used', 'usedAt set');

  const fixture = await createProject(t);
  await writeApprovals(fixture.project, [approval()]);
  assertAllow(runHook(fixture, bashInput(fixture.project, mergeCommand())), PR, 'first use');
  const afterFirst = await readApprovals(fixture.project);
  const second = runHook(fixture, bashInput(fixture.project, mergeCommand()));
  assertDeny(second, 'already-used', 'second use');
  assert.deepEqual(await readApprovals(fixture.project), afterFirst, runReport(second, 'second use: the record keeps the first use'));
});

test('§4 order: the first failing condition names the code', async t => {
  await assertBlocked(t, { marker: false, command: `${mergeCommand()} | cat` }, 'compound-command', '1 before 5');
  await assertBlocked(t, { approvals: [], command: `gh pr merge ${PR} --squash --admin` }, 'forbidden-flag', '2 before 3');
  await assertBlocked(t, { marker: false, command: `gh pr merge ${PR} --squash` }, 'missing-match-head-commit', '3 before 5');
  await assertBlocked(t, { approvals: [], command: mergeCommand('--squash', HEAD_A.slice(0, 7)) }, 'bad-form', '4 before 7');
  await assertBlocked(t, { marker: false, agent: true }, 'not-main-checkout', '5 before 6');
  await assertBlocked(t, { agent: true, approvals: [] }, 'subagent', '6 before 7');
  await assertBlocked(t, { approvals: [approval({ createdAgo: 31 * MINUTE })], command: mergeCommand('--squash', HEAD_B) },
    'head-mismatch', '8 before 9');
  await assertBlocked(t, { approvals: [approval({ createdAgo: 31 * MINUTE, usedAgo: 30 * MINUTE, usedCommand: mergeCommand() })] },
    'expired', '9 before 10');
});

// Linux: a read-only approvals folder stops the temp file of §1. Windows ignores folder modes, but
// a read-only target stops the rename. Both are set; root ignores both, so the test is skipped.
const runsAsRoot = typeof process.getuid === 'function' && process.getuid() === 0;
test('§4 condition 11 state-write-failed: a pass whose record update fails is blocked',
  { skip: runsAsRoot ? 'running as root: file modes do not stop writes, so the failure cannot be produced' : false },
  async t => {
    const fixture = await createProject(t);
    const written = await writeApprovals(fixture.project, [approval()]);
    await chmod(approvalsFile(fixture.project), 0o444);
    await chmod(approvalsDirectory(fixture.project), 0o555);
    let run;
    try {
      run = runHook(fixture, bashInput(fixture.project, mergeCommand()));
    } finally {
      await chmod(approvalsDirectory(fixture.project), 0o755);
      await chmod(approvalsFile(fixture.project), 0o644);
    }
    assertDeny(run, 'state-write-failed', 'read-only record');
    assert.deepEqual(await readApprovals(fixture.project), written, runReport(run, 'read-only record: still unused'));
  });

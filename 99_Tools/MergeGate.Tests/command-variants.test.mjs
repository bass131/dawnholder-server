// Independent regressions for spellings of merge and push commands that the earlier regressions do
// not cover. Expected outcomes come from the approved rules, not from 99_Tools/MergeGate:
// - behavior contract v2 (E/merge-gate-behavior-spec.md, SHA256 d16918f6…7c9a) §4 Bash 3 (a push
//   whose command string carries a refspec to main is always push-main), §4 「`gh pr merge`의 통과
//   조건」 1 (whitespace around the whole command is ignored), 4 (only
//   `gh pr merge <number> <method> --match-head-commit <40 hex>` passes) and 11 (usedCommand is the
//   original command), §5 (usedCommand must equal the input command exactly);
// - the lead's answer msg_6e39f24eb8d9 kept in E/impl-contract.md 「구현 기준」: every refspec argument
//   is checked and its destination is the part after the last `:` without a leading `+`.
// Where the contract fixes only that a form must not pass but not which code it gets, the test
// accepts the codes the contract allows for that form and checks that the record stays unused.
//
// Environment: node only. Each test owns a temporary project passed as CLAUDE_PROJECT_DIR.
import assert from 'node:assert/strict';
import { test } from 'node:test';

import {
  approval, assertAllow, assertDeny, assertNoDecision, assertPermissionAllow, bashInput, createProject, HEAD_A,
  permissionInput, PR, readApprovals, runHook, runReport, writeApprovals,
} from './hook-fixture.mjs';

const mergeCommand = `gh pr merge ${PR} --merge --match-head-commit ${HEAD_A}`;

function denyCode(run) {
  const reason = JSON.parse(run.stdout || '{}').hookSpecificOutput?.permissionDecisionReason ?? '';
  return reason.split(' ')[0].replace(/^merge-gate:/, '');
}

test('§4 conditions 1 and 11: whitespace around the single command passes and usedCommand keeps it', async t => {
  const command = `  ${mergeCommand}\t `;
  const fixture = await createProject(t);
  await writeApprovals(fixture.project, [approval()]);
  assertAllow(runHook(fixture, bashInput(fixture.project, command)), PR, 'surrounding whitespace');
  const [entry] = (await readApprovals(fixture.project)).approvals;
  assert.equal(entry.usedCommand, command, 'usedCommand is the original command, whitespace included');
  assertPermissionAllow(runHook(fixture, permissionInput(fixture.project, command)), 'same original command');
  assertNoDecision(runHook(fixture, permissionInput(fixture.project, mergeCommand)), 'trimmed command is another string');
});

test('§4 condition 4: forms outside the single documented shape never pass and leave the record unused', async t => {
  const forms = [
    ['--match-head-commit=<40 hex>', `gh pr merge ${PR} --merge --match-head-commit=${HEAD_A}`, ['missing-match-head-commit', 'bad-form']],
    ['sh -c wrapper', `sh -c "${mergeCommand}"`, ['compound-command']],
    ['environment prefix', `GH_REPO=owner/repo ${mergeCommand}`, ['compound-command', 'bad-form']],
    ['--auto with a value', `gh pr merge ${PR} --merge --auto=false --match-head-commit ${HEAD_A}`, ['forbidden-flag', 'bad-form']],
    ['--repo after merge', `gh pr merge ${PR} --merge --repo owner/repo --match-head-commit ${HEAD_A}`, ['bad-form']],
    ['PR URL instead of number', `gh pr merge https://github.com/owner/repo/pull/${PR} --merge --match-head-commit ${HEAD_A}`, ['bad-form']],
  ];
  for (const [label, command, codes] of forms) {
    const fixture = await createProject(t);
    const written = await writeApprovals(fixture.project, [approval()]);
    const run = runHook(fixture, bashInput(fixture.project, command));
    assertDeny(run, denyCode(run), label);
    assert.ok(codes.includes(denyCode(run)), runReport(run, `${label}: code is one of ${codes.join(', ')}`));
    assert.deepEqual(await readApprovals(fixture.project), written, runReport(run, `${label}: the record stays unused`));
  }
});

test('§4 Bash 3 push-main: a refspec to main is found next to options, in compound commands and for subagents', async t => {
  const fixture = await createProject(t);
  const commands = [
    'git push --force origin main',
    'git push origin main --force-with-lease',
    'git push -u origin HEAD:main',
    'git push --set-upstream origin main',
    'git push -o ci.skip origin main',
    'git push origin --delete main',
    'git push origin feat/x:refs/heads/main',
    'git add -A && git commit -m wip && git push origin HEAD:main',
    'bash -c "git push origin main"',
    'git push origin feat/x main',
  ];
  for (const command of commands) {
    assertDeny(runHook(fixture, bashInput(fixture.project, command)), 'push-main', command);
    assertDeny(runHook(fixture, bashInput(fixture.project, command, { agent: true })), 'push-main', `${command} (subagent)`);
  }
});

test('§4 Bash 3: pushes of other branches next to options get no decision', async t => {
  const fixture = await createProject(t);
  for (const command of ['git push -u origin feat/merge-gate', 'git push --force-with-lease origin feat/x:feat/x', 'git push origin main-backup']) {
    assertNoDecision(runHook(fixture, bashInput(fixture.project, command)), command);
  }
});

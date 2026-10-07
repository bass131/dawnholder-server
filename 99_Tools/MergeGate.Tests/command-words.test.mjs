// Independent regressions for the command words, subcommands and file paths that behavior
// contract v2.1 added (E/merge-gate-behavior-spec-v2.1.md, SHA256 ce113621…e016). Every expected
// decision and code below is read from that contract, not from 99_Tools/MergeGate:
// - §4 「명령 낱말과 하위 명령」: a gh or git command word is any whitespace word whose name after the
//   last `/` or `\` is gh, gh.exe, git or git.exe in any case, with surrounding quotes removed. Option
//   words, and the next word after -R/--repo (gh) or -C/-c/--git-dir/--work-tree/--namespace/
//   --super-prefix/--config-env (git), are skipped before the subcommand words are compared.
// - §4 「병합 시도 판정」 3: a git push with a refspec to main or with --all/--mirror is push-main;
//   a refspec of HEAD or @ (or none) looks up the branch of the input cwd. The refspec destination
//   rule is the lead's answer msg_6e39f24eb8d9 written into the same item.
// - §4 「통과 조건」, checked in order: 1 a merge attempt that does not start with the leading gh
//   command word is compound-command; 2 --auto/--admin is forbidden-flag; 3 a missing or `=` form
//   --match-head-commit is missing-match-head-commit with a reason that says `=` is not accepted;
//   4 only the lower-case `gh pr merge <n> <method> --match-head-commit <40 hex>` with nothing
//   between gh, pr and merge passes the form check, else bad-form; 6 an agent_id is subagent.
// - §4 「Write·Edit·MultiEdit·NotebookEdit」: the path is normalised first (relative to the input
//   cwd, `.`/`..` and repeated separators folded, `/` separators, case ignored).
// - §5: PermissionRequest allows only the exact standalone form.
// Forms the contract does not settle are kept out of these tests and recorded by the re-verifier's
// observation harness instead.
//
// Environment: node only. Each test owns a temporary project passed as CLAUDE_PROJECT_DIR.
import assert from 'node:assert/strict';
import { join } from 'node:path';
import { test } from 'node:test';

import {
  approval, approvalsFile, assertAllow, assertDeny, assertNoDecision, assertPermissionAllow, bashInput, createProject,
  createRepository, fileToolInput, HEAD_A, permissionInput, PR, readApprovals, runHook, runReport, SESSION,
  writeApprovals,
} from './hook-fixture.mjs';

const exactMerge = `gh pr merge ${PR} --merge --match-head-commit ${HEAD_A}`;
const tail = `${PR} --merge --match-head-commit ${HEAD_A}`;

test('§4 command words: merge attempts with words between gh, pr and merge are blocked before any record is used', async t => {
  const forms = [
    ['-R between pr and merge', `gh pr -R owner/repo merge ${tail}`, 'bad-form'],
    ['--repo between pr and merge', `gh pr --repo owner/repo merge ${tail}`, 'bad-form'],
    ['-R between gh and pr', `gh -R owner/repo pr merge ${tail}`, 'bad-form'],
    ['--repo=<value> with --admin', `gh pr --repo=owner/repo merge ${PR} --merge --admin`, 'forbidden-flag'],
    ['upper-case command word', `GH pr merge ${tail}`, 'bad-form'],
    ['.exe command word', `gh.exe pr merge ${tail}`, 'bad-form'],
    ['POSIX path command word', `/usr/bin/gh pr merge ${tail}`, 'bad-form'],
    ['Windows path command word', `C:/tools/gh.exe pr merge ${tail}`, 'bad-form'],
  ];
  for (const [label, command, code] of forms) {
    const fixture = await createProject(t);
    const written = await writeApprovals(fixture.project, [approval()]);
    assertDeny(runHook(fixture, bashInput(fixture.project, command)), code, label);
    assert.deepEqual(await readApprovals(fixture.project), written, `${label}: the matching record stays unused`);
    assertDeny(runHook(fixture, bashInput(fixture.project, command, { agent: true })), code, `${label} (subagent)`);
  }
});

test('§4 condition 1: a merge after another command word or an environment assignment is compound-command', async t => {
  for (const command of [`GH_REPO=owner/repo ${exactMerge}`, `env ${exactMerge}`, `command ${exactMerge}`, `cd x && gh pr -R owner/repo merge ${tail}`]) {
    const fixture = await createProject(t);
    const written = await writeApprovals(fixture.project, [approval()]);
    assertDeny(runHook(fixture, bashInput(fixture.project, command)), 'compound-command', command);
    assert.deepEqual(await readApprovals(fixture.project), written, `${command}: the record stays unused`);
  }
});

test('§4 condition 3: the = form of --match-head-commit is missing-match-head-commit and the reason says so', async t => {
  const fixture = await createProject(t);
  const written = await writeApprovals(fixture.project, [approval()]);
  const run = runHook(fixture, bashInput(fixture.project, `gh pr merge ${PR} --merge --match-head-commit=${HEAD_A}`));
  assertDeny(run, 'missing-match-head-commit', '= form');
  const reason = JSON.parse(run.stdout).hookSpecificOutput.permissionDecisionReason;
  assert.ok(reason.slice(reason.indexOf(' ')).includes('='), runReport(run, 'the reason text mentions the = form'));
  assert.deepEqual(await readApprovals(fixture.project), written, 'the record stays unused');
});

test('§4 and §5: the exact standalone merge still passes once, and widened spellings never get confirmation', async t => {
  const fixture = await createProject(t);
  await writeApprovals(fixture.project, [approval()]);
  assertAllow(runHook(fixture, bashInput(fixture.project, exactMerge)), PR, 'exact standalone merge');
  const [entry] = (await readApprovals(fixture.project)).approvals;
  assert.equal(entry.usedCommand, exactMerge, 'usedCommand is the command text (§4 condition 11)');
  assert.notEqual(entry.usedAt, null, 'usedAt is written (§4 condition 11)');
  assertPermissionAllow(runHook(fixture, permissionInput(fixture.project, exactMerge)), 'same command right after the pass');
  assertDeny(runHook(fixture, bashInput(fixture.project, exactMerge)), 'already-used', 'second use (§4 condition 10)');

  // A record whose usedCommand is a widened spelling confers nothing at PermissionRequest (§5).
  for (const command of [`GH pr merge ${tail}`, `gh pr -R owner/repo merge ${tail}`, `gh.exe pr merge ${tail}`]) {
    const forged = await createProject(t);
    await writeApprovals(forged.project, [approval({ usedAgo: 1000, usedCommand: command })]);
    assertNoDecision(runHook(forged, permissionInput(forged.project, command)), `PermissionRequest for ${command}`);
  }
});

test('§4 command words: git pushes to main behind global options or other spellings are push-main', async t => {
  const fixture = await createProject(t);
  const commands = [
    'git -C . push origin main',
    'git -C ../other push origin HEAD:main',
    'git -c push.default=simple push origin main',
    'git --no-pager push origin main',
    'git --git-dir=.git push origin main',
    'git --git-dir .git --work-tree . push origin main',
    'git --namespace ns push origin refs/heads/main',
    'git --super-prefix sub/ push origin main',
    'git --config-env core.editor=EDITOR push origin main',
    'git.exe push origin main',
    'GIT push origin main',
    '/usr/bin/git push origin +main',
    'C:/tools/git.exe -C . push origin feat/x:main',
    '"git" push origin main',
    'git -C . push --all',
    'git -C . push --mirror origin',
    'cd repo && git -C . push origin main',
  ];
  for (const command of commands) {
    assertDeny(runHook(fixture, bashInput(fixture.project, command)), 'push-main', command);
    assertDeny(runHook(fixture, bashInput(fixture.project, command, { agent: true })), 'push-main', `${command} (subagent)`);
  }
});

test('§4 boundaries: other branches, non-push git commands and gh pr commands that are not merge get no decision unless the last net words meet', async t => {
  const fixture = await createProject(t);
  const commands = [
    'git push origin feat/x',
    'git -C . push origin feat/x',
    'git -c push.default=current push -u origin feat/merge-gate',
    'git push origin main-backup',
    'git stash push',
    'git stash push -m wip',
    'git log --grep push',
    'gh pr view 12',
    'gh pr -R owner/repo view 12',
    'gh pr list --state merged',
    'gh pr checks 12',
    'gh repo view',
  ];
  for (const command of commands) assertNoDecision(runHook(fixture, bashInput(fixture.project, command)), command);
  // Behavior contract v2.3 「마지막 그물」 (SHA256 2fcfe94c…0e10) accepts these false positives: push with
  // the word main, and a gh command whose title has the word merge.
  for (const command of ['git -C . log --grep push origin main', 'gh pr create --title merge --body text']) {
    assertDeny(runHook(fixture, bashInput(fixture.project, command)), 'suspect-words', command);
  }
});

test('§4 Bash 3 (v2.1 @): a push of @ looks up the branch of the input cwd', async t => {
  const fixture = await createProject(t);
  const onMain = await createRepository(fixture.root, 'repo-main', 'main');
  const onFeature = await createRepository(fixture.root, 'repo-feature', 'feat/x');
  const notRepository = join(fixture.root, 'project');
  assertDeny(runHook(fixture, bashInput(fixture.project, 'git push origin @', { cwd: onMain })), 'push-main', '@ on main');
  assertNoDecision(runHook(fixture, bashInput(fixture.project, 'git push origin @', { cwd: onFeature })), '@ on a feature branch');
  assertDeny(runHook(fixture, bashInput(fixture.project, 'git push origin @', { cwd: notRepository })),
    'branch-lookup-failed', '@ where the branch cannot be read');
});

test('§4 file tools (v2.1): paths are normalised against the input cwd before the state folder check', async t => {
  const fixture = await createProject(t);
  const { project } = fixture;
  const state = join(project, '.claude', 'state', 'merge-gate');
  const protectedCases = [
    ['./ prefix', './.claude/state/merge-gate/main-checkout', project],
    ['. segment', '.claude/state/./merge-gate/main-checkout', project],
    ['repeated separator', '.claude//state/merge-gate/main-checkout', project],
    ['.. segment', '.claude/state/x/../merge-gate/main-checkout', project],
    ['.. before the folder', `sub/../.claude/state/merge-gate/approvals/${SESSION}.json`, project],
    ['absolute with . / .. / //', `${project}/.claude/./state//merge-gate/x/../main-checkout`, project],
    ['upper case', '.CLAUDE/State/MERGE-GATE/main-checkout', project],
    ['backslash separators', '.claude\\state\\merge-gate\\main-checkout', project],
    ['relative to a cwd inside the folder', '../main-checkout', join(state, 'approvals')],
    ['relative to the state parent', 'merge-gate/main-checkout', join(project, '.claude', 'state')],
  ];
  const outsideCases = [
    ['.. out of the folder', '.claude/state/merge-gate/../x', project],
    ['similar folder name', '.claude/state/merge-gate-old/main-checkout', project],
    ['same relative path from the project root', 'merge-gate/main-checkout', project],
  ];
  for (const tool of ['Write', 'Edit', 'MultiEdit', 'NotebookEdit']) {
    for (const [label, path, cwd] of protectedCases) {
      assertDeny(runHook(fixture, fileToolInput(project, tool, path, { cwd })), 'protected-path', `${tool} ${label}: ${path}`);
    }
    for (const [label, path, cwd] of outsideCases) {
      assertNoDecision(runHook(fixture, fileToolInput(project, tool, path, { cwd })), `${tool} ${label}: ${path}`);
    }
  }
  assert.equal(approvalsFile(project).startsWith(state), true, 'fixture record path lies under the state folder');
});

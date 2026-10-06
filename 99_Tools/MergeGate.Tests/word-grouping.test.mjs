// Independent regressions for the two shell readings and the quote grouping that behavior contract
// v2.3 added (E/merge-gate-behavior-spec-v2.3.md, SHA256 2fcfe94c…0e10). Every expected decision and
// code below is read from that contract, not from 99_Tools/MergeGate:
// - §4 「단순 명령 목록」: the simple commands are those of the boundary reading together with those
//   of the substitution reading, which first replaces $(…), ${…} and a backtick pair inside a word
//   by one placeholder word, innermost first; finding a subcommand or a push in either reading
//   counts, so adding a reading never removes a block.
// - §4 「건너뛰는 낱말의 따옴표 묶음」: a skipped option or value word whose quote is still open runs
//   to the word where it closes, read character by character from the raw word; a quote that never
//   closes before the end of the simple command skips that one word only.
// - §4 「gh 하위 명령」 and 「git push」 with Bash 3: a push whose refspec destination is main or
//   refs/heads/main, or with --all, is push-main without a branch lookup.
// - §4 「통과 조건」 1 and 4: a merge with $( or a backtick is compound-command; words between gh, pr
//   and merge, or a number or head that is not literal, is bad-form.
// - §4 Monitor: a merge attempt is always non-bash-merge; everything else gets the Bash judgement.
// The forms come from the second re-verification's rows V and Q (E/reverify2/observe.jsonl), the
// S2/S3 examples of v2.3 and the =-form option word the third re-verification contract names. The
// push forms run with the input cwd on a feature branch, so a block cannot come from a branch lookup.
//
// Environment: node and git. Each test owns a temporary project passed as CLAUDE_PROJECT_DIR.
import assert from 'node:assert/strict';
import { join } from 'node:path';
import { test } from 'node:test';

import {
  approval, assertDeny, assertNoDecision, bashInput, createProject, createRepository, HEAD_A, PR, readApprovals,
  runHook, writeApprovals,
} from './hook-fixture.mjs';

const tail = `${PR} --merge --match-head-commit ${HEAD_A}`;

// The shared fixture builds Bash inputs only; Monitor keeps the same fields with its own tool input.
const monitorInput = (project, command, options) => ({
  ...bashInput(project, command, options), tool_name: 'Monitor',
  tool_input: { command, description: 'merge gate regression input', timeout_ms: 60000 },
});

// Both shell tools, from a feature branch cwd: the expected code needs no branch lookup.
async function assertPushMain(t, commands) {
  const fixture = await createProject(t);
  const onFeature = await createRepository(fixture.root, 'repo-feature', 'feat/x');
  for (const command of commands) {
    assertDeny(runHook(fixture, bashInput(fixture.project, command, { cwd: onFeature })), 'push-main', `Bash ${command}`);
    assertDeny(runHook(fixture, monitorInput(fixture.project, command, { cwd: onFeature })), 'push-main', `Monitor ${command}`);
  }
}

// A main checkout with a valid unused record, so only the command form can block a Bash merge.
async function assertMergeBlocked(t, cases) {
  for (const [command, code] of cases) {
    const fixture = await createProject(t);
    const written = await writeApprovals(fixture.project, [approval()]);
    assertDeny(runHook(fixture, bashInput(fixture.project, command)), code, `Bash ${command}`);
    assertDeny(runHook(fixture, monitorInput(fixture.project, command)), 'non-bash-merge', `Monitor ${command}`);
    assert.deepEqual(await readApprovals(fixture.project), written, `${command}: the record stays unused`);
  }
}

test('§4 substitution reading: main pushes with ${…}, $(…) or backticks inside a word are push-main', async t => {
  await assertPushMain(t, [
    'git -C ${REPO} push origin main',
    'git -C "${REPO}" push origin main',
    'git -C "${REPO}" push origin HEAD:main',
    'git -C "${REPO}" push --all',
    'git push origin ${SHA}:main',
    'git push origin "${LOCAL}:refs/heads/main"',
    'git push origin $(git rev-parse HEAD):main',
    'git push origin $(echo $(git rev-parse HEAD)):main',
    'git -C "$(git rev-parse --show-toplevel)" push origin main',
    'git push ${REMOTE} main',
    'git push --repo=${URL} main',
    'git -C `pwd` push origin main',
    'git push origin `git rev-parse HEAD`:main',
  ]);
});

test('§4 substitution reading: merges with ${…}, $(…) or backticks inside a word are merge attempts', async t => {
  await assertMergeBlocked(t, [
    [`gh -R \${REPO} pr merge ${tail}`, 'bad-form'],
    [`gh pr -R "\${REPO}" merge ${tail}`, 'bad-form'],
    [`gh pr merge \${PR} --merge --match-head-commit ${HEAD_A}`, 'bad-form'],
    [`gh pr merge ${PR} --merge --match-head-commit \${HEAD}`, 'bad-form'],
    [`gh pr --repo $(cat repo.txt) merge ${PR} --merge --admin`, 'compound-command'],
    [`gh -R \`cat repo.txt\` pr merge ${tail}`, 'compound-command'],
  ]);
});

test('§4 substitution reading: a gh api merge path with ${…} inside a word is api-merge', async t => {
  const fixture = await createProject(t);
  const command = 'gh api -X PUT repos/${OWNER}/r/pulls/12/merge';
  assertDeny(runHook(fixture, bashInput(fixture.project, command)), 'api-merge', `Bash ${command}`);
  assertDeny(runHook(fixture, monitorInput(fixture.project, command)), 'api-merge', `Monitor ${command}`);
});

test('§4 quote grouping: a quote that closes inside the skipped word ends the skip there', async t => {
  await assertPushMain(t, [
    'git -C "C:/repo"/sub push origin main',
    'git -C "$HOME"/repo push origin main',
    "git -C 'C:/repo'/sub push origin HEAD:main",
    'git push -o "ci"skip origin main',
  ]);
  await assertMergeBlocked(t, [
    [`gh pr -R "o/r"x merge ${tail}`, 'bad-form'],
    [`gh -R "o/r"x pr merge ${tail}`, 'bad-form'],
  ]);
});

test('§4 quote grouping: a quote opened inside a value or an =-form option word runs to the word that closes it', async t => {
  await assertPushMain(t, [
    'git -c user.name="A B" push origin main',
    'git -c core.sshCommand="ssh -i k" push origin main',
    'git --git-dir="C:/My Repo/.git" push origin main',
    'git --git-dir="C:/My Repo/.git" --work-tree="C:/My Repo" push origin HEAD:main',
    'git -C "C:/My Repo"/sub push origin main',
    'git push --push-option="ci skip" origin main',
    'git push -o key="a b" origin main',
  ]);
  await assertMergeBlocked(t, [
    [`gh --repo="o/My Repo" pr merge ${tail}`, 'bad-form'],
    [`gh pr --repo="o/r x" merge ${tail}`, 'bad-form'],
  ]);
});

test('§4 quote grouping: a quote that never closes skips only its own word', async t => {
  await assertPushMain(t, [
    'git -C "C:/repo push origin main',
    'git push -o "ci origin main',
  ]);
  await assertMergeBlocked(t, [[`gh pr -R "o/r merge ${tail}`, 'bad-form']]);
  // No refspec is left after the one skipped word, so Bash 3 looks up the branch of the input cwd.
  const fixture = await createProject(t);
  const onMain = await createRepository(fixture.root, 'repo-main', 'main');
  const onFeature = await createRepository(fixture.root, 'repo-feature', 'feat/x');
  const notRepository = join(fixture.root, 'project');
  const command = 'git -C "C:/repo push origin';
  assertDeny(runHook(fixture, bashInput(fixture.project, command, { cwd: onMain })), 'push-main', `${command} on main`);
  assertNoDecision(runHook(fixture, bashInput(fixture.project, command, { cwd: onFeature })), `${command} on a feature branch`);
  assertDeny(runHook(fixture, bashInput(fixture.project, command, { cwd: notRepository })), 'branch-lookup-failed',
    `${command} where the branch cannot be read`);
});

test('§4 both readings and quote grouping leave pushes to other branches without a decision', async t => {
  const fixture = await createProject(t);
  const onFeature = await createRepository(fixture.root, 'repo-feature', 'feat/x');
  const commands = [
    'git -C "${REPO}" push origin feat/x',
    'git push origin $(git rev-parse HEAD):feat/x',
    'git -c user.name="A B" push origin feat/x',
    'git --git-dir="C:/My Repo/.git" push origin feat/x',
    'git -C "$HOME"/repo push -u origin feat/x',
  ];
  for (const command of commands) {
    assertNoDecision(runHook(fixture, bashInput(fixture.project, command, { cwd: onFeature })), `Bash ${command}`);
    assertNoDecision(runHook(fixture, monitorInput(fixture.project, command, { cwd: onFeature })), `Monitor ${command}`);
  }
});

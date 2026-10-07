// Independent regressions for the shell command reading that behavior contract v2.2 gathered into
// one rule (E/merge-gate-behavior-spec-v2.2.md, SHA256 67d63f4b…0868). Every expected decision and
// code below is read from that contract, not from 99_Tools/MergeGate:
// - §4 「셸 명령 읽기」: a backslash-newline folds to one space before judging; simple commands split
//   at ; & | ` < > ( ) { } and newline, and subcommand search and push arguments stay inside one
//   simple command; words split on whitespace with surrounding quotes removed; after an option that
//   takes the next word as its value, a value that starts with a quote runs to the word that ends
//   with the same quote; gh/git command words ignore case, a path before the last / or \ and .exe;
//   the git global options taking a separate value include --attr-source.
// - §4 Bash 2: a gh api call (by the command-word rule) whose command text names a PR merge call is
//   always api-merge.
// - §4 Bash 3: a push whose refspec destination is main or refs/heads/main, or with --all/--mirror,
//   is push-main; push value options (-o and others) skip their value; a push of HEAD or with no
//   refspec looks up the branch of the input cwd.
// - §4 Bash 5: the state folder path in a command is protected-path after folding \ to /, repeated
//   / and /./ segments, ignoring case.
// - §4 condition 1: a merge attempt that does not start with the leading gh command word, for
//   example after a parenthesis, is compound-command.
// The command forms come from the re-verification's observation rows (E/reverify/observe.jsonl,
// groups P, H1, GA and B). Forms the contract does not settle stay in the second re-verifier's
// observation harness (E/reverify2/observe.mjs) instead of here.
//
// Environment: node only. Each test owns a temporary project passed as CLAUDE_PROJECT_DIR.
import assert from 'node:assert/strict';
import { join } from 'node:path';
import { test } from 'node:test';

import {
  approval, assertDeny, assertNoDecision, bashInput, createProject, createRepository, HEAD_A, PR, readApprovals,
  runHook, writeApprovals,
} from './hook-fixture.mjs';

const tail = `${PR} --merge --match-head-commit ${HEAD_A}`;

test('§4 simple commands: main pushes inside parentheses, braces, substitutions and after line continuations are push-main', async t => {
  const fixture = await createProject(t);
  const commands = [
    '(git push origin main)',
    '(cd r && git push origin main)',
    'out=$(git push origin main)',
    '(git push origin HEAD:main)',
    '(git push --all)',
    '{ git push origin main; }',
    '{ git push origin refs/heads/main }',
    'git push \\\n  origin main',
    'git push origin \\\n  HEAD:main',
    'git push \\\n  origin \\\n  +HEAD:refs/heads/main',
    'echo start; (git -C . push origin feat/x:main)',
  ];
  for (const command of commands) {
    assertDeny(runHook(fixture, bashInput(fixture.project, command)), 'push-main', JSON.stringify(command));
    assertDeny(runHook(fixture, bashInput(fixture.project, command, { agent: true })), 'push-main', `${JSON.stringify(command)} (subagent)`);
  }
});

test('§4 Bash 3: a parenthesised push of HEAD looks up the branch of the input cwd', async t => {
  const fixture = await createProject(t);
  const onMain = await createRepository(fixture.root, 'repo-main', 'main');
  const onFeature = await createRepository(fixture.root, 'repo-feature', 'feat/x');
  const notRepository = join(fixture.root, 'project');
  for (const command of ['(git push origin HEAD)', '(cd . && git push)', '{ git push origin @; }']) {
    assertDeny(runHook(fixture, bashInput(fixture.project, command, { cwd: onMain })), 'push-main', `${command} on main`);
    assertNoDecision(runHook(fixture, bashInput(fixture.project, command, { cwd: onFeature })), `${command} on a feature branch`);
    assertDeny(runHook(fixture, bashInput(fixture.project, command, { cwd: notRepository })), 'branch-lookup-failed',
      `${command} where the branch cannot be read`);
  }
});

test('§4 option values: quoted values with spaces and --attr-source are skipped before push', async t => {
  const fixture = await createProject(t);
  const commands = [
    'git -C "C:/Program Files/repo" push origin main',
    "git -C 'C:/My Repo' push origin HEAD:main",
    'git -c "user.name=A B" push origin main',
    'bash -c "git -C \'C:/My Repo\' push origin main"',
    'git --git-dir "C:/My Repo/.git" --work-tree "C:/My Repo" push origin main',
    'git --attr-source HEAD push origin main',
    'git --attr-source=HEAD push origin main',
    'git push -o "ci skip please" origin main',
    "git push --push-option 'a b' origin HEAD:main",
  ];
  for (const command of commands) {
    assertDeny(runHook(fixture, bashInput(fixture.project, command)), 'push-main', command);
  }
});

test('§4 Bash 2: gh api merge calls are api-merge for every gh command word spelling', async t => {
  const fixture = await createProject(t);
  const commands = [
    'GH api -X PUT repos/o/r/pulls/12/merge',
    'gh.exe api -X PUT repos/o/r/pulls/12/merge',
    'C:/bin/gh.exe api -X PUT repos/o/r/pulls/12/merge',
    'C:\\bin\\GH.EXE api -X PUT repos/o/r/pulls/12/merge',
    '"gh" api -X PUT repos/o/r/pulls/12/merge',
    'gh -R o/r api -X PUT repos/o/r/pulls/12/merge',
    '(gh api -X PUT repos/o/r/pulls/12/merge)',
    "GH api graphql -f query='mutation { mergePullRequest(input: {pullRequestId: \"x\"}) { clientMutationId } }'",
    "gh.exe api graphql -f query='mutation { enablePullRequestAutoMerge(input: {pullRequestId: \"x\"}) { clientMutationId } }'",
  ];
  for (const command of commands) {
    assertDeny(runHook(fixture, bashInput(fixture.project, command)), 'api-merge', command);
    assertDeny(runHook(fixture, bashInput(fixture.project, command, { agent: true })), 'api-merge', `${command} (subagent)`);
  }
});

test('§4 Bash 5: state folder paths with repeated or dot segments are protected-path', async t => {
  const fixture = await createProject(t);
  const commands = [
    'cat .claude//state/merge-gate/main-checkout',
    'ls .claude/state/./merge-gate',
    'ls .claude/./state//merge-gate/approvals',
    'rm -f .claude///state/././merge-gate/approvals/x.json',
    'type .claude\\\\state\\.\\merge-gate\\main-checkout',
    'ls .CLAUDE//State/./MERGE-GATE',
  ];
  for (const command of commands) {
    assertDeny(runHook(fixture, bashInput(fixture.project, command)), 'protected-path', command);
  }
});

test('§4 condition 1: merges behind a parenthesis, a brace or a substitution are compound-command and leave the record unused', async t => {
  const commands = [
    `(gh pr merge ${tail})`,
    `{ gh pr merge ${tail} }`,
    `out=$(gh pr merge ${tail})`,
  ];
  for (const command of commands) {
    const fixture = await createProject(t);
    const written = await writeApprovals(fixture.project, [approval()]);
    assertDeny(runHook(fixture, bashInput(fixture.project, command)), 'compound-command', command);
    assert.deepEqual(await readApprovals(fixture.project), written, `${command}: the record stays unused`);
  }
});

test('§4 boundaries: other branches, quoted values on other branches and non-merge gh commands get no decision unless the last net words meet', async t => {
  const fixture = await createProject(t);
  const commands = [
    '(git push origin feat/x)',
    '{ git push origin main-backup; }',
    'out=$(git log --grep push)',
    'git -C "C:/My Repo" push origin feat/x',
    'git --attr-source HEAD push origin feat/main-fix',
    'git stash push',
    '(gh pr view 12)',
    'GH api repos/o/r/pulls/12',
    'gh.exe api repos/o/r/issues/12/comments',
    'ls .claude/state/x',
  ];
  for (const command of commands) assertNoDecision(runHook(fixture, bashInput(fixture.project, command)), command);
  // Behavior contract v2.3 「마지막 그물」 (SHA256 2fcfe94c…0e10) accepts these false positives: push with
  // the word main, and a gh command whose title has the word merge.
  for (const command of ['git push -o "main" origin feat/x', 'gh pr create --title merge --body text']) {
    assertDeny(runHook(fixture, bashInput(fixture.project, command)), 'suspect-words', command);
  }
});

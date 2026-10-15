// Independent review tests for the bounded grammar of user decision 7. Expected outcomes come from
// the approved texts, never from 99_Tools/CodexGuard:
// - user decision msg_5c404e04e508 (goal.md 「적용 중인 사용자 결정」 7, completion condition 4): only
//   one command, && chains and a literal cd before push are judged precisely; any other form with
//   merge/push words is denied with 「단순하게 다시 써라」 guidance; a command without those words is
//   kept even when it cannot be parsed; false denies count only inside the supported forms;
// - implementation contract E/guard-redesign-02-contract-v1.md: S1~S3, S1 data and the unsupported
//   row (lines 51~57), the protected families (lines 65~70), the missing remote HEAD rule (line 72)
//   and 「판정/오류/안내」 1~5 (lines 76~80): a protected action inside S1~S3 is a Policy deny, an
//   unsupported form with a candidate is an Unsupported form deny, an envelope/I/O/internal failure
//   is an Error deny, and the input budgets never leave the hook running on.
// Reasons are told apart by the result names the contract gives (Policy deny / Unsupported form /
// Error deny); unsupported-form.mjs checks the guidance text.
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { test } from 'node:test';

import {
  assertBlocked, assertKept, assertRemotesUntouched, bashInput, blockReason, caseName, createFixture,
  createRepository, createSubdirectory, expectBlocked, expectKept, hooksConfigPath, PR, runHook, runReport,
} from './hook-fixture.mjs';
import { assertUnsupported, expectUnsupported } from './unsupported-form.mjs';

async function repositories(t) {
  const fixture = await createFixture(t);
  const onMain = await createRepository(fixture, { name: 'main-repo', current: 'main' });
  const feature = await createRepository(fixture, { name: 'feature-repo' });
  const spaceMain = await createRepository(fixture, { name: 'space main', current: 'main' });
  const spaceFeature = await createRepository(fixture, { name: 'space feature' });
  const mainDeep = await createSubdirectory(onMain.directory, 'tools', 'deep dir');
  return { fixture, onMain, feature, spaceMain, spaceFeature, mainDeep };
}

// Contract 「판정/오류/안내」 1: a protected action in a supported form is a policy match.
function assertPolicy(fixture, run, label) {
  assertBlocked(fixture, run, label);
  const reason = blockReason(run);
  if (!/^\s*Policy deny/.test(reason)) assert.fail(runReport(run, `${label}: expected a Policy deny`));
}

async function expectPolicy(t, fixture, cwd, commands) {
  for (const command of commands) {
    await t.test(caseName(command), () => assertPolicy(fixture, runHook(fixture, bashInput(cwd, command)), caseName(command)));
  }
}

test('decision 7 S1~S3: supported normal commands, paths and data are kept (no false deny)', async t => {
  const { fixture, onMain, feature, spaceFeature } = await repositories(t);
  // S1: literal or quoted/absolute executable, .exe, global options, local merge, reads.
  await expectKept(t, fixture, feature.directory, [
    'git push origin feat/guard',
    'git push',
    'git.exe push origin feat/guard',
    '"C:/Program Files/Git/cmd/git.exe" push origin feat/guard',
    '& "C:\\Program Files\\Git\\cmd\\git.exe" push origin feat/guard',
    'git -c core.quotepath=false push origin feat/guard',
    'git merge main',
    `gh -R owner/repo pr view ${PR}`,
    `gh --repo=owner/repo pr checks ${PR}`,
    `gh pr create --title "Guard" --body "gh pr merge ${PR} after approval"`,
    `gh api repos/owner/repo/pulls/${PR}/merge`,
    `gh api -X GET repos/owner/repo/pulls/${PR}/merge`,
    "gh api graphql -f query='query { viewer { login } }'",
    "gh api graphql -f query='mutation { disablePullRequestAutoMerge(input: {pullRequestId: \"x\"}) { clientMutationId } }'",
    "gh api graphql -f query='query { search(query: \"mergePullRequest(\", type: ISSUE) { issueCount } }'",
  ]);
  // S2: && chains, with the newline after && used as white space (LF and CRLF).
  await expectKept(t, fixture, feature.directory, [
    'git fetch origin main && git push origin feat/guard',
    'git merge origin/main && git push origin feat/guard',
    'git status &&\n  git push origin feat/guard',
    'git status &&\r\n  git push origin feat/guard',
  ]);
  // S3: literal cd/Set-Location (quoted space, relative, absolute, sub path) and push's own git -C.
  await expectKept(t, fixture, onMain.directory, [
    'cd ../feature-repo && git push',
    'Set-Location ../feature-repo && git push',
    'cd "../space feature" && git push origin feat/guard',
    `cd "${spaceFeature.directory}" && git push`,
    `Set-Location -Path "${spaceFeature.directory}" && git push`,
    `git -C "${spaceFeature.directory}" push origin feat/guard`,
    'git -C ../feature-repo push',
  ]);
  // S1 data: quoted arguments, comments, heredoc/here-string bodies without execution substitution,
  // apostrophes, parentheses and the PowerShell quote escape (#18's two commit samples).
  await expectKept(t, fixture, feature.directory, [
    'git commit -m "Say `"hi`" now"',
    'git commit -m "Use `"git push`" only on work branches"',
    `git commit -m "Don't run (gh pr merge ${PR}) yet"`,
    `git push origin feat/guard # never gh pr merge ${PR}`,
    `<# gh pr merge ${PR} #> git status`,
    `git commit -F - <<'EOF'\ngh pr merge ${PR}\n(git push origin main)\nEOF`,
    `Set-Content notes.md -Value @'\ngh pr merge ${PR}\ngit push origin main\n'@`,
    `Set-Content notes.md -Value @"\ngh pr merge ${PR}\n"@`,
  ]);
  // Empty, white space and comment-only commands.
  await expectKept(t, fixture, feature.directory, ['', '   ', '\n', `# gh pr merge ${PR}`]);
  assertRemotesUntouched(fixture, feature, 'feature-repo');
  assertRemotesUntouched(fixture, onMain, 'main-repo');
});

test('decision 7 S1~S3: protected actions inside the supported forms are Policy denies', async t => {
  const { fixture, onMain, feature, spaceMain, mainDeep } = await repositories(t);
  await expectPolicy(t, fixture, feature.directory, [
    'git push origin main',
    'git.exe push origin HEAD:main',
    '"C:/Program Files/Git/cmd/git.exe" push origin heads/main',
    '& git push origin refs/heads/main',
    'git -c core.quotepath=false push --force origin main',
    `gh pr merge ${PR}`,
    `gh -R owner/repo pr merge ${PR} --auto`,
    `gh --repo=owner/repo pr merge --disable-auto ${PR}`,
    `"C:\\Program Files\\GitHub CLI\\gh.exe" pr merge ${PR}`,
    `GH pr merge ${PR}`,
    `gh api -XPUT repos/owner/repo/pulls/${PR}/merge`,
    `gh api --method=PUT "https://api.github.com/repos/owner/repo/pulls/${PR}/merge?x=1"`,
    `gh api repos/owner/repo/pulls/${PR}/merge -f merge_method=squash`,
    "gh api graphql -f query='mutation { mergePullRequest(input: {pullRequestId: \"x\"}) { clientMutationId } }'",
    "gh api graphql --raw-field=query='mutation{m: enqueuePullRequest(input:{pullRequestId:\"x\"}){clientMutationId}}'",
    // Conditions are not evaluated: every explicit command of an && chain is checked.
    `false && gh pr merge ${PR}`,
    `git status &&\n  gh pr merge ${PR}`,
  ]);
  // S3 moves the push into a repository on main: quoted space, absolute, sub path, git -C.
  await expectPolicy(t, fixture, feature.directory, [
    'cd ../main-repo && git push',
    'cd "../space main" && git push',
    `Set-Location -Path "${spaceMain.directory}" && git push`,
    `cd "${mainDeep}" && git push`,
    'cd ../main-repo && git status &&\n  git push',
    `git -C "${spaceMain.directory}" push`,
  ]);
  assertRemotesUntouched(fixture, feature, 'feature-repo');
  assertRemotesUntouched(fixture, onMain, 'main-repo');
  assertRemotesUntouched(fixture, spaceMain, 'space main');
});

test('decision 7: an unsupported form that names a protected command is an Unsupported form deny', async t => {
  const { fixture, feature } = await repositories(t);
  await expectUnsupported(t, fixture, feature.directory, [
    'cd ../feature-repo; git push origin feat/guard',
    'git status\ngit push origin feat/guard',
    'cd ../feature-repo || exit 1; git push',
    'git push origin feat/guard | Out-Null',
    'git push origin feat/guard &',
    '(git push origin feat/guard)',
    '{ git push origin feat/guard; }',
    'if true; then git push origin feat/guard; fi',
    'foreach ($b in @("x")) { git push origin feat/guard }',
    'bash -c "git push origin feat/guard"',
    `pwsh -Command "gh pr view ${PR}; gh pr merge ${PR}"`,
    `cmd /c "gh pr merge ${PR}"`,
    'env GIT_TRACE=1 git push origin feat/guard',
    `command gh pr merge ${PR}`,
    `eval "gh pr merge ${PR}"`,
    `iex -Command:'gh pr merge ${PR}'`,
    'git push origin "$branch"',
    'git push origin $(git branch --show-current)',
    'cd $HOME && git push origin feat/guard',
    `bash -c 'gh --repo owner/repo pr merge ${PR}'`,
    `bash -c "gh api -X PUT repos/owner/repo/pulls/${PR}/merge"`,
    `gh api -X PUT repos/owner/repo/pulls/${PR}/merge | Out-Null`,
    "gh api graphql -f query='mutation { mergePullRequest(input: {}) { clientMutationId } }' | Out-Null",
  ]);
  // Contract 「판정/오류/안내」 2: an original ban such as an explicit main stays forbidden after rewriting.
  const run = runHook(fixture, bashInput(feature.directory, 'cd ../main-repo; git push origin main'));
  assertUnsupported(fixture, run, 'unsupported explicit main');
  assert.match(blockReason(run), /\bmain\b/, runReport(run, 'the guidance says main stays protected'));
  assert.match(blockReason(run), /forbidden|금지/i, runReport(run, 'the guidance says the ban remains after rewriting'));
  assertRemotesUntouched(fixture, feature, 'feature-repo');
});

test('decision 7: a command without a protected candidate is kept even outside the supported forms', async t => {
  const { fixture, feature } = await repositories(t);
  await expectKept(t, fixture, feature.directory, [
    'git status; git log -1',
    'echo "unterminated',
    'git commit -m "unterminated',
    'git log --oneline | Select-String guard',
    `bash -c "gh pr view ${PR}"`,
    'git merge main; git status',
    'docker push example/image:1; npm publish',
    `gh api repos/owner/repo/pulls/${PR}/merge | Out-Null`,
    "gh api graphql -f query='query { viewer { login } }' | Out-Null",
    'gh api -X POST repos/owner/repo/issues -f title=guard | Out-Null',
    'gh api graphql -F query=@mutation.graphql | Out-Null',
  ]);
  assertRemotesUntouched(fixture, feature, 'feature-repo');
});

test('decision 7: input and Git read failures are Error denies, distinct from the unsupported form', async t => {
  const { fixture, onMain } = await repositories(t);
  const plain = await createSubdirectory(fixture.root, 'plain folder');
  const noHead = await createRepository(fixture, { name: 'no-head', remoteDefault: null });
  for (const [cwd, command, label] of [
    [plain, 'git push origin feat/guard', 'push outside a repository'],
    [onMain.directory, 'cd "../plain folder" && git push origin feat/guard', 'cd into a folder without a repository'],
    [noHead.directory, 'git push origin feat/guard', 'remote HEAD unknown'],
  ]) {
    const run = runHook(fixture, bashInput(cwd, command));
    assertBlocked(fixture, run, label);
    assert.match(blockReason(run), /^\s*Error deny/, runReport(run, `${label}: expected an Error deny`));
    assert.doesNotMatch(blockReason(run), /Unsupported form/, runReport(run, `${label}: not an unsupported form`));
  }
  // Contract line 72: an explicit main needs no remote HEAD evidence and stays a Policy deny.
  assertPolicy(fixture, runHook(fixture, bashInput(noHead.directory, 'git push origin main')), 'explicit main without remote HEAD');
  const envelope = runHook(fixture, null, { raw: '{"hook_event_name":"PreToolUse","tool_name":"Bash"', cwd: plain });
  assertBlocked(fixture, envelope, 'truncated JSON');
  assert.match(blockReason(envelope), /^\s*Error deny/, runReport(envelope, 'a broken envelope is an Error deny'));
});

// Defect #19 (this review): PowerShell's dot-source operator `.` runs the command after it in the
// session like the call operator `&` (about_Operators "Dot sourcing operator ."). The contract's S1
// is judged by the literal executable (line 51), which here is gh or git, not `.`.
test('defect #19: a PowerShell dot-source call of gh or git is judged like the & call', async t => {
  const { fixture, feature } = await repositories(t);
  await expectBlocked(t, fixture, feature.directory, [
    `. gh pr merge ${PR}`,
    '. git push origin main',
    `. "C:\\Program Files\\GitHub CLI\\gh.exe" pr merge ${PR}`,
  ]);
  assertRemotesUntouched(fixture, feature, 'feature-repo');
});

// Defect #20 (this review): command runners execute the command written after them, like the
// env/command/exec prefixes the contract lists as unsupported (line 55). The protected command is
// visible in the original text, so this is not the hidden-command limit of line 57; decision 7
// denies a form outside S1~S3 that names a push or merge.
test('defect #20: a protected command after a command runner is not passed as argument data', async t => {
  const { fixture, feature } = await repositories(t);
  await expectBlocked(t, fixture, feature.directory, [
    'nohup git push origin main',
    'timeout 60 git push origin main',
    `sudo gh pr merge ${PR}`,
    'nice -n 10 git push origin main',
    'xargs gh pr merge',
    'wsl git push origin main',
    `stdbuf -o0 gh pr merge ${PR}`,
    `start gh pr merge ${PR}`,
  ]);
  assertRemotesUntouched(fixture, feature, 'feature-repo');
});

// Defect #22 (this review): PowerShell's built-in aliases sl and chdir run Set-Location
// (Get-Alias in pwsh 7.6.6), and bash's `builtin cd` runs cd. S3 judges a push by the location
// a literal cd/Set-Location reaches (contract line 53), so a push after these moves into the
// repository on main and must not be judged in the starting work repository.
test('defect #22: a location change written as sl, chdir or builtin cd does not hide a push from main', async t => {
  const { fixture, feature } = await repositories(t);
  await expectBlocked(t, fixture, feature.directory, [
    'sl ../main-repo && git push',
    'chdir ../main-repo && git push',
    'builtin cd ../main-repo && git push',
  ]);
  assertRemotesUntouched(fixture, feature, 'feature-repo');
});

// Defect #21 (this review): contract 「판정/오류/안내」 4 combines the input budgets with the candidate
// check so they never leave the hook running on; main msg_8365f4261987 kept the timeout limit. A
// decision must arrive within the registered handler timeout. Each input stays under the 1 MiB
// envelope; a hook process killed by the timeout delivers no decision.
test('defect #21: repeated option words reach a decision within the registered handler timeout', async t => {
  const { fixture, feature } = await repositories(t);
  const registration = JSON.parse(readFileSync(hooksConfigPath, 'utf8'));
  const handlerTimeoutMs = registration.hooks.PreToolUse[0].hooks[0].timeout * 1000;
  const repeat = (unit, bytes) => unit.repeat(Math.floor(bytes / unit.length));
  for (const [label, command, expected] of [
    ['unclosed quote and attached git options', `echo "unterminated${repeat(' -x=git', 120000)} gh pr merge ${PR}`, 'unsupported'],
    ['slash-named git options after ;', `a; x${repeat(' -/git', 450000)}`, 'kept'],
  ]) {
    await t.test(label, () => {
      const started = Date.now();
      const run = runHook(fixture, bashInput(feature.directory, command));
      const elapsedMs = Date.now() - started;
      if (expected === 'unsupported') assertUnsupported(fixture, run, label);
      else assertKept(fixture, run, label);
      assert.ok(elapsedMs < handlerTimeoutMs,
        runReport(run, `${label}: decided in ${elapsedMs} ms, not within the ${handlerTimeoutMs} ms handler timeout`));
    });
  }
});

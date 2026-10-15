// Requirement tests for shell compounds, string wrappers and data quotes around the blocked
// commands. Expected outcomes come from the approved texts, not from 99_Tools/CodexGuard:
// - contract E/guard-pretest-contract-v1.md (SHA256 7fea9bfb…14d2) requirement 4: explicit
//   shell compounds and string wrappers are covered; a blocked word that only appears in a data
//   quote, commit message or PR title is not a reason to block;
// - scope draft v1 63행: the covered text is explicit commands and interpretable PowerShell or
//   POSIX shell compounds/wrappers (Codex on Windows runs PowerShell); 61행: a work branch push
//   after `git fetch origin main` is kept;
// - the lead's design supplement 1 (E/guard-pretest-design-supplement-01.md, Q5, reply to
//   msg_cff106ac428f): an explicit cd changes the cwd for what follows, a command substitution
//   that runs is covered, and so is eval / Invoke-Expression given one literal string; the same
//   text in a plain data quote is kept. The body of PowerShell -EncodedCommand is excluded.
// Dynamic execution, aliases, external script contents, reused shell stdin and other clients are
// outside the requirement and listed with reasons in the requirement table.
//
// User decision 7 (msg_5c404e04e508, goal.md 「적용 중인 사용자 결정」 7) narrowed the precise forms
// to one command, && chains and a literal cd before push. Three samples change, citing
// E/guard-redesign-02-contract-v1.md: `Set-Location <dir>; git push` (`;`, line 53),
// `git commit -m "$(cat <<'EOF' … EOF)"` (a heredoc inside an execution substitution, line 54) and
// `bash -c "echo '…merge…'"` (a shell host naming a merge, lines 55 and 70) are now an intended
// Unsupported form deny. Single-command data quotes, comments and a heredoc without an execution
// substitution stay kept (S1 data, line 54).
import { test } from 'node:test';

import {
  assertRemotesUntouched, createFixture, createRepository, createSubdirectory, expectBlocked, expectKept, PR,
} from './hook-fixture.mjs';
import { expectUnsupported } from './unsupported-form.mjs';

test('requirement 4: blocked commands inside POSIX and PowerShell compounds are blocked', async t => {
  const fixture = await createFixture(t);
  const repository = await createRepository(fixture);
  await expectBlocked(t, fixture, repository.directory, [
    `git status && gh pr merge ${PR}`,
    `gh pr checks ${PR} && gh pr merge ${PR} --squash`,
    `gh pr view ${PR}; gh pr merge ${PR}`,
    `gh pr checks ${PR} || gh pr merge ${PR} --auto`,
    `echo y | gh pr merge ${PR}`,
    `gh pr view ${PR}\ngh pr merge ${PR}`,
    `(gh pr merge ${PR})`,
    `{ gh pr merge ${PR}; }`,
    'gh pr create --fill && gh pr merge --auto --squash',
    `if gh pr checks ${PR}; then gh pr merge ${PR} --squash; fi`,
    'for pr in 12 13; do gh pr merge "$pr" --merge; done',
    'git add -A && git commit -m "wip" && git push origin HEAD:main',
    'git fetch origin && git push origin main',
    'git push origin feat/guard && git push origin main',
    'git status; git push --force origin main',
    'cd tools && git push origin main',
    `gh pr checks ${PR}; if ($LASTEXITCODE -eq 0) { gh pr merge ${PR} --squash }`,
    'git status | Out-Null; git push origin main',
    `$env:GH_REPO = 'owner/repo'; gh pr merge ${PR}`,
    `& gh pr merge ${PR}`,
    `& "C:\\Program Files\\GitHub CLI\\gh.exe" pr merge ${PR}`,
    "& 'C:\\Program Files\\Git\\cmd\\git.exe' push origin main",
  ]);
  assertRemotesUntouched(fixture, repository, 'after the compounds');
});

test('requirement 4: string wrappers and command prefixes are blocked', async t => {
  const fixture = await createFixture(t);
  const repository = await createRepository(fixture);
  await expectBlocked(t, fixture, repository.directory, [
    `bash -c "gh pr merge ${PR}"`,
    `bash -lc 'gh pr merge ${PR} --auto'`,
    "sh -c 'git push origin HEAD:main'",
    '/bin/bash -c "git push origin main"',
    `bash -c "git status && gh pr merge ${PR}"`,
    `bash -c "sh -c 'gh pr merge ${PR}'"`,
    `pwsh -NoProfile -Command "gh pr merge ${PR}"`,
    "pwsh -c 'git push origin main'",
    `powershell.exe -NoProfile -Command "gh pr merge ${PR} --squash"`,
    `pwsh -NoProfile -Command "& gh pr merge ${PR}"`,
    `cmd /c gh pr merge ${PR}`,
    'cmd.exe /c "git push origin main"',
    `env GH_REPO=owner/repo gh pr merge ${PR}`,
    `GH_REPO=owner/repo gh pr merge ${PR}`,
    'GIT_TRACE=1 git push origin main',
    `command gh pr merge ${PR}`,
    'exec git push origin main',
  ]);
  assertRemotesUntouched(fixture, repository, 'after the wrappers');
});

test('supplement 1 Q5: cd before an implicit push, executed substitutions and a single literal eval are covered', async t => {
  const fixture = await createFixture(t);
  const onMain = await createRepository(fixture, { name: 'main-repo', current: 'main' });
  const onFeature = await createRepository(fixture, { name: 'feature-repo' });
  await createSubdirectory(onMain.directory, 'tools');

  await t.test('cd into the repository on main', async t => {
    await expectBlocked(t, fixture, onFeature.directory, [
      'cd ../main-repo && git push',
      'cd ../main-repo; git push origin HEAD',
      'cd ../main-repo/tools && git push',
      'Set-Location ../main-repo; git push',
    ]);
  });
  await t.test('cd into the repository on a work branch', async t => {
    await expectKept(t, fixture, onMain.directory, ['cd ../feature-repo && git push']);
    await expectUnsupported(t, fixture, onMain.directory, ['Set-Location ../feature-repo; git push']);
  });
  await t.test('command substitution that runs the command', async t => {
    await expectBlocked(t, fixture, onFeature.directory, [
      `echo "$(gh pr merge ${PR})"`,
      `result=$(gh pr merge ${PR} --squash)`,
      'echo `git push origin main`',
      `Write-Output "result: $(gh pr merge ${PR})"`,
    ]);
  });
  await t.test('one literal string given to eval or Invoke-Expression', async t => {
    await expectBlocked(t, fixture, onFeature.directory, [
      `eval 'gh pr merge ${PR}'`,
      'eval "git push origin main"',
      `Invoke-Expression 'gh pr merge ${PR}'`,
      'iex "git push origin HEAD:main"',
    ]);
  });
  await t.test('the same text as single-quoted data', async t => {
    await expectKept(t, fixture, onFeature.directory, [
      `echo '$(gh pr merge ${PR})'`,
      "echo 'eval git push origin main'",
      `Write-Output '$(gh pr merge ${PR})'`,
    ]);
  });
  assertRemotesUntouched(fixture, onMain, 'main-repo');
  assertRemotesUntouched(fixture, onFeature, 'feature-repo');
});

test('requirement 4: the words inside data quotes, commit messages, titles and comments are kept', async t => {
  const fixture = await createFixture(t);
  const repository = await createRepository(fixture);
  await expectKept(t, fixture, repository.directory, [
    'git commit -m "Block gh pr merge and git push origin main"',
    "git commit -m 'docs: gh pr merge --auto stays blocked' -m 'git push --force origin main too'",
    "git commit -F - <<'EOF'\nBlock gh pr merge\n\ngit push origin main stays blocked\nEOF",
    `cat > notes.md <<'EOF'\ngh pr merge ${PR}\ngit push origin main\nEOF`,
    `echo "gh pr merge ${PR}"`,
    "echo 'git push origin main'",
    'printf \'%s\\n\' "git push --force origin main"',
    `Write-Output 'gh pr merge ${PR}'`,
    'grep -rn "gh pr merge" 99_Tools',
    'rg -n "git push origin main" 00_Document',
    "Select-String -Path README.md -Pattern 'gh pr merge'",
    'git log --grep "gh pr merge" --oneline',
    'gh pr create --title "Guard: block gh pr merge" --body "git push origin main is blocked"',
    `gh issue comment 5 --body "run gh pr merge ${PR} after approval"`,
    'git push origin feat/guard # never git push origin main',
    'git fetch origin main && git push origin feat/guard',
  ]);
  // Decision 7: an execution substitution around a heredoc and a shell host naming a merge are
  // outside S1 data, so the merge/push words in them are denied with rewrite guidance.
  await expectUnsupported(t, fixture, repository.directory, [
    "git commit -m \"$(cat <<'EOF'\nBlock gh pr merge --auto\n\ngit push origin HEAD:main stays blocked\nEOF\n)\"",
    `bash -c "echo 'gh pr merge ${PR}'"`,
  ]);
  assertRemotesUntouched(fixture, repository, 'after the data quotes');
});

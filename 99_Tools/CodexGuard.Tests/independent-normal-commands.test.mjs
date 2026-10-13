// Independent review tests for ordinary commands the guard must leave to Codex. Expected outcomes
// come from the approved texts, not from 99_Tools/CodexGuard:
// - goal 01_Phases/goals/2026-10-13-astra-lead-mailbox-hotfix/goal.md completion condition 4: a
//   false block of a normal command does not pass;
// - review contract E/guard-review-contract-v1.md requirement 4: quoted data, commit messages and
//   heredoc text are not blocked for their words; requirement 5: an error deny is for a needed
//   input or Git read the guard cannot judge, while calls outside the guard keep Codex's normal
//   permission handling.
// Each command below is valid in the shell it is written for and contains no gh call and no
// git push, so nothing in it is a guarded action. The heredoc forms are the usual way to pass a
// multi-line commit message (POSIX `$(cat <<'EOF' … EOF)`, PowerShell here-strings); apostrophes,
// list markers like "1)" and inner double quotes are ordinary text inside them. A cd whose target
// is computed only matters when a push follows; that case stays blocked below.
import { test } from 'node:test';

import { createFixture, createRepository, expectBlocked, expectKept, PR } from './hook-fixture.mjs';

test('goal completion 4: commit messages in heredocs and here-strings are kept whatever their text', async t => {
  const fixture = await createFixture(t);
  const repository = await createRepository(fixture);
  await expectKept(t, fixture, repository.directory, [
    "git commit -m \"$(cat <<'EOF'\nDon't merge yet\nEOF\n)\"",
    "git commit -m \"$(cat <<'EOF'\nfix: two steps\n\n1) first\n2) second\nEOF\n)\"",
    "git commit -m @'\nDon't merge yet\n'@",
    'git commit -m @"\nSay "hi" before merging\n"@',
    `git commit -m @'\nIt's not gh pr merge ${PR}\n'@`,
  ]);
});

test('goal completion 4: a computed cd without a push is kept', async t => {
  const fixture = await createFixture(t);
  const repository = await createRepository(fixture);
  await expectKept(t, fixture, repository.directory, [
    'cd $env:TEMP; Get-ChildItem',
    'cd "$HOME" && ls',
    'Set-Location (Join-Path $PWD sub); Get-ChildItem',
    'cd "$(git rev-parse --show-toplevel)" && git status',
  ]);
});

test('requirement 3: a computed cd before a push is still not kept', async t => {
  const fixture = await createFixture(t);
  const repository = await createRepository(fixture);
  await expectBlocked(t, fixture, repository.directory, [
    'cd $env:TEMP; git push',
    'cd "$(git rev-parse --show-toplevel)/../main-repo" && git push origin HEAD',
  ]);
});

test('goal completion 4: process substitution in a read-only command is kept', async t => {
  const fixture = await createFixture(t);
  const repository = await createRepository(fixture);
  await expectKept(t, fixture, repository.directory, [
    'diff <(git show HEAD:a) <(git show HEAD~1:a)',
  ]);
});

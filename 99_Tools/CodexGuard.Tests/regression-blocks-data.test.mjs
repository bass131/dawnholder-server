// Re-check tests for PowerShell blocks written against a brace and for command text that is data
// (first-fix review, defect #2/#8 lineage). Expected outcomes come from the approved texts and from
// measured PowerShell behavior, never from 99_Tools/CodexGuard:
// - implementation contract requirement 5: basic PowerShell control forms are inspected, while
//   quoted data, commit messages and heredoc/here-string text are not commands by themselves;
// - re-check contract #2 (catch{, catch [Type]{, finally/else, quoted data) and #8 (heredoc and
//   here-string data, executed substitutions and process substitutions);
// - goal completion condition 4: a valid normal command must not be denied.
// Measured facts (E/guard-recheck-01/probe-01/result.json "statements", Windows PowerShell 5.1 and
// pwsh 7.6.6): a catch naming two exception types with no space before its brace runs its body.
// A PowerShell double-quoted string uses ` to escape a quote, so "Say `"hi`" now" is one valid
// argument (PowerShell language: backtick escape inside expandable strings).
import { test } from 'node:test';

import { createFixture, createRepository, expectBlocked, expectKept, PR } from './hook-fixture.mjs';

async function workRepository(t) {
  const fixture = await createFixture(t);
  const feature = await createRepository(fixture, { name: 'feature-repo' });
  return { fixture, cwd: feature.directory };
}

test('#2 re-check: catch blocks against a brace, including a type list, are commands', async t => {
  const { fixture, cwd } = await workRepository(t);
  await expectBlocked(t, fixture, cwd, [
    `try { throw 'x' } catch [System.Management.Automation.RuntimeException],[System.Exception]{ gh pr merge ${PR} }`,
    `try{git status}catch [System.IO.IOException],[System.Exception]{gh pr merge ${PR}}`,
    `try { git status } catch{ gh pr merge ${PR} }`,
    `try { git status } catch [System.Exception]{ gh pr merge ${PR} }`,
    `try { git status } catch { } finally{ gh pr merge ${PR} }`,
    `if (Test-Path x) { git status }else{ gh pr merge ${PR} }`,
  ]);
  await expectKept(t, fixture, cwd, [
    `try { git status } catch [System.IO.IOException],[System.Exception]{ gh pr view ${PR} }`,
    `Write-Output 'catch [System.Exception]{ gh pr merge ${PR} }'`,
    `git commit -m "try{x}catch{gh pr merge ${PR}}"`,
  ]);
});

test('#8 re-check: valid PowerShell escapes inside a commit message are kept', async t => {
  const { fixture, cwd } = await workRepository(t);
  await expectKept(t, fixture, cwd, [
    'git commit -m "Say `"hi`" now"',
    'git commit -m "Use `"git push`" only on work branches"',
    'git commit -m "line1`nline2`n"',
  ]);
});

test('#8 re-check: heredoc and here-string data stays data and executed substitutions stay inspected', async t => {
  const { fixture, cwd } = await workRepository(t);
  await expectKept(t, fixture, cwd, [
    `git commit -F - <<'EOF'\nDon't run gh pr merge ${PR} (later)\nEOF`,
    `git commit -m "$(cat <<'EOF'\nfix: (a) don't "merge" yet\nEOF\n)"`,
    `git commit -m @'\nDon't "merge" (yet)\n'@`,
    'diff <(git log -1) <(git log -2)',
  ]);
  await expectBlocked(t, fixture, cwd, [
    `git commit -m "$(cat <<EOF\n$(gh pr merge ${PR})\nEOF\n)"`,
    `git commit -m @"\nDon't $(gh pr merge ${PR})\n"@`,
    `diff <(gh pr merge ${PR}) <(git status)`,
    `echo "$(gh pr merge ${PR})"`,
  ]);
});

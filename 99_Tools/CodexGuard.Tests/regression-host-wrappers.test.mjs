// Re-check tests for shell wrappers and literal evaluators (first-fix review, defect #3/#4 lineage).
// Expected outcomes come from the approved texts and from measurements of the installed hosts,
// never from 99_Tools/CodexGuard:
// - implementation contract requirement 5: `pwsh/powershell -Command`, `cmd /c` and one literal
//   string given to eval/Invoke-Expression/iex are inspected (lead supplement 1 Q5);
// - re-check contract #3: options, abbreviations, case, value consumption and the Command/File/
//   Encoded boundary of the installed powershell.exe and pwsh.exe; #4: -Command and positional iex.
// Measured host facts (E/guard-recheck-01/probe-01/result.json and cmd-tail-01.json; Windows
// PowerShell 5.1.26100.9444, pwsh 7.6.6, Windows cmd.exe), each run with a Write-Output/echo
// sentinel only:
// - powershell.exe runs its first positional argument as a command ("positional first argument",
//   "positional unquoted words"); pwsh treats it as a script file (exit 64).
// - powershell.exe still runs the sentinel after a bare -ex/-exec/-ep/-executionpolicy/-wd/-wo/
//   -workingdirectory followed by -Command (host "consumption" rows, bareRan true).
// - Both hosts join every word after -Command into one script ("-Command then a second argument"
//   printed both A and B); cmd /c joins the rest of the line the same way ("quoted first word then a
//   second word" printed "A B").
// - pwsh runs the string given to -CommandWithArgs and its alias -cwa.
// - Both hosts run a literal given to iex/Invoke-Expression through an abbreviated -C/-Com, the
//   -Command: form, or the pipeline ("statements" rows).
import { test } from 'node:test';

import { createFixture, createRepository, expectBlocked, expectKept, PR } from './hook-fixture.mjs';

async function workRepository(t) {
  const fixture = await createFixture(t);
  const feature = await createRepository(fixture, { name: 'feature-repo' });
  return { fixture, cwd: feature.directory };
}

test('#3 re-check: Windows PowerShell runs a positional first argument as a command', async t => {
  const { fixture, cwd } = await workRepository(t);
  await expectBlocked(t, fixture, cwd, [
    `powershell "gh pr merge ${PR}"`,
    `powershell gh pr merge ${PR}`,
    `powershell -NoProfile "gh pr merge ${PR}"`,
    `powershell.exe -NoLogo -NonInteractive "git push origin main"`,
  ]);
  await expectKept(t, fixture, cwd, [
    `powershell "gh pr view ${PR}"`,
    `powershell -NoProfile "Write-Output 'gh pr merge ${PR}'"`,
  ]);
});

test('#3 re-check: a bare value option before -Command still lets Windows PowerShell run the command', async t => {
  const { fixture, cwd } = await workRepository(t);
  await expectBlocked(t, fixture, cwd, ['-ex', '-exec', '-ep', '-executionpolicy', '-wd', '-wo', '-workingdirectory']
    .map(option => `powershell ${option} -Command "gh pr merge ${PR}"`));
});

test('#3 re-check: every word after -Command or /c belongs to the command text', async t => {
  const { fixture, cwd } = await workRepository(t);
  await expectBlocked(t, fixture, cwd, [
    `pwsh -NoProfile -Command "gh pr merge ${PR}" --squash`,
    `powershell -Command "gh pr" "merge ${PR}"`,
    `pwsh -c "gh pr view ${PR};" "gh pr merge ${PR}"`,
    `cmd /c "gh pr merge ${PR}" --squash`,
  ]);
  await expectKept(t, fixture, cwd, [
    `pwsh -NoProfile -Command "gh pr view ${PR}" --web`,
    `cmd /c "gh pr checks ${PR}" --watch`,
  ]);
});

test('#3 re-check: pwsh -CommandWithArgs and -cwa run their command text', async t => {
  const { fixture, cwd } = await workRepository(t);
  await expectBlocked(t, fixture, cwd, [
    `pwsh -CommandWithArgs "gh pr merge ${PR}"`,
    `pwsh -cwa "gh pr merge ${PR}"`,
    `pwsh -NoProfile -cwa 'git push origin main'`,
  ]);
  await expectKept(t, fixture, cwd, [`pwsh -cwa "gh pr view ${PR}"`]);
});

test('#3 re-check: the forms fixed by the first fix stay inspected and kept where they should', async t => {
  const { fixture, cwd } = await workRepository(t);
  await expectBlocked(t, fixture, cwd, [
    `powershell -exec bypass -command "gh pr merge ${PR}"`,
    `pwsh -EXeC bypass -CoMm "gh pr merge ${PR}"`,
    `powershell -in text -co "gh pr merge ${PR}"`,
    `pwsh -inp text -co "gh pr merge ${PR}"`,
    `pwsh -noninteractive -noprofile -c "gh pr merge ${PR}"`,
    `pwsh -WorkingDirectory . -Command "gh pr merge ${PR}"`,
  ]);
  await expectKept(t, fixture, cwd, [
    `pwsh -File script.ps1 -Command "gh pr merge ${PR}"`,
    `pwsh -NoProfile -Command "Write-Output 'gh pr merge ${PR}'"`,
  ]);
});

test('#4 re-check: one literal string given to iex/Invoke-Expression is inspected however it is bound', async t => {
  const { fixture, cwd } = await workRepository(t);
  await expectBlocked(t, fixture, cwd, [
    `iex -C 'gh pr merge ${PR}'`,
    `Invoke-Expression -Com 'gh pr merge ${PR}'`,
    `iex -Command:'gh pr merge ${PR}'`,
    `'gh pr merge ${PR}' | iex`,
    `"git push origin main" | Invoke-Expression`,
  ]);
  await expectKept(t, fixture, cwd, [
    `'gh pr view ${PR}' | iex`,
    `iex -C 'Write-Output "gh pr merge ${PR}"'`,
    `Invoke-Expression -Command 'gh pr checks ${PR}'`,
  ]);
});

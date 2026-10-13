// Re-check tests for the execution location of an implicit push (first-fix review, defect #1
// lineage). Expected outcomes come from the approved texts and from measured shell behavior, never
// from 99_Tools/CodexGuard:
// - implementation contract requirement 3 (decide a push by its actual destination, including the
//   cwd after an explicit cd) and requirement 5 (&&, ;, ||, pipes, newlines and the basic POSIX and
//   PowerShell control forms);
// - re-check contract E/guard-recheck-01-contract-v1.md #1: chains, group pipes, plain sequence
//   versus an explicit cd failure boundary, and both a lost main path and a false deny when every
//   path ends on a work branch;
// - goal completion condition 4: a normal command must not be denied.
// Rule: if any path the shell can take runs the push from the repository on main, block; if every
// path that reaches the push runs it on a work branch, keep. A plain `cd <dir>;` is taken to move
// the shell (the plain-sequence boundary named in the re-check contract). Where an operator or
// control form makes a later command depend on cd failing (`||`, `!`, `if !`, `until`, `while !`,
// catch after `-ErrorAction Stop`), that failure path is reachable; `exit` ends the path that runs it.
// Conditions are never evaluated, so `false`, `test`, `[ -f … ]` and Test-Path count as either way.
// Measured location facts (E/guard-recheck-01/probe-01/result.json, Git Bash 5.3.9, pwsh 7.6.6):
// - `cd <dir> & …`: Git Bash runs the backgrounded cd in a subshell and pwsh runs it as a job, so
//   the next command stays in the starting folder (posix "background cd…", statements "background &").
// - `! cd <missing> && …`, `if ! cd …; then …`, `until cd …; do …`, `while ! cd …; do …`: the body
//   runs exactly when cd failed, still in the starting folder (posix rows of the same names).
// - `{ cd <dir>; } | cat; …` keeps the parent folder; commands inside the piped group run in the
//   group's folder (posix "group piped…" rows).
import { test } from 'node:test';

import { createFixture, createRepository, expectBlocked, expectKept } from './hook-fixture.mjs';

async function repositories(t) {
  const fixture = await createFixture(t);
  const onMain = await createRepository(fixture, { name: 'main-repo', current: 'main' });
  const feature = await createRepository(fixture, { name: 'feature-repo' });
  const featureTwo = await createRepository(fixture, { name: 'feature-two' });
  return { fixture, onMain, feature, featureTwo };
}

test('#1 re-check: a backgrounded cd does not move the shell, so the push can run from main', async t => {
  const { fixture, onMain } = await repositories(t);
  await expectBlocked(t, fixture, onMain.directory, [
    'cd ../feature-repo & git push',
    'cd ../feature-repo &\ngit push',
    'Set-Location ../feature-repo & git push',
  ]);
});

test('#1 re-check: a body that runs only when cd failed stays in the starting repository on main', async t => {
  const { fixture, onMain } = await repositories(t);
  await expectBlocked(t, fixture, onMain.directory, [
    '! cd ../feature-repo && git push',
    'if ! cd ../feature-repo; then git push; fi',
    'until cd ../feature-repo; do git push; done',
    'while ! cd ../feature-repo; do git push; done',
  ]);
});

test('#1 re-check: an explicit cd failure boundary that exits keeps a push whose every path is a work branch', async t => {
  const { fixture, onMain } = await repositories(t);
  await expectKept(t, fixture, onMain.directory, [
    'cd ../feature-repo || exit 1; git push',
    'cd ../feature-repo || exit; git push',
    'cd ../feature-repo || { echo missing; exit 1; }; git push',
    'Set-Location ../feature-repo || exit 1; git push',
    'try { Set-Location ../feature-repo -ErrorAction Stop } catch { exit 1 }; git push',
  ]);
});

test('#1 re-check: the same boundary that does not exit still reaches main', async t => {
  const { fixture, onMain } = await repositories(t);
  await expectBlocked(t, fixture, onMain.directory, [
    'cd ../feature-repo || echo missing; git push',
    'cd ../feature-repo || true; git push',
    'try { Set-Location ../feature-repo -ErrorAction Stop } catch { Write-Output missing }; git push',
  ]);
});

test('#1 re-check: group pipes keep the parent folder and run the group body in its own folder', async t => {
  const { fixture, onMain, feature } = await repositories(t);
  await t.test('from the repository on main', async t => {
    await expectBlocked(t, fixture, onMain.directory, [
      '{ cd ../feature-repo; } | cat; git push',
      'echo | { cd ../feature-repo; }; git push',
    ]);
    await expectKept(t, fixture, onMain.directory, [
      '{ cd ../feature-repo; git push; } | cat',
      'cd ../feature-repo; { cd ../feature-two; } | cat; git push',
      'cd ../feature-repo && { git status; } | cat && git push',
    ]);
  });
  await t.test('from a work branch', async t => {
    await expectBlocked(t, fixture, feature.directory, [
      '{ cd ../main-repo; git push; } | cat',
      'echo | { cd ../main-repo; git push; }',
      'cd ../main-repo; { cd ../feature-two; } | cat; git push',
    ]);
  });
});

test('#1 re-check: && and || chains keep every reachable location and drop unreachable ones', async t => {
  const { fixture, onMain, feature } = await repositories(t);
  await expectBlocked(t, fixture, onMain.directory, [
    'cd ../feature-repo && git status; git push',
    'cd ../feature-repo && git push || git push',
    'false || cd ../feature-repo; git push',
  ]);
  await expectKept(t, fixture, onMain.directory, [
    'cd ../feature-repo && git push',
    'cd ../feature-repo || cd ../feature-two && git push',
    'cd ../feature-repo && cd ../feature-two && git push',
  ]);
  await expectBlocked(t, fixture, feature.directory, [
    'cd ../main-repo || cd ../feature-two && git push',
    'cd ../feature-two && cd ../main-repo && git push',
  ]);
  await expectKept(t, fixture, feature.directory, [
    'false && cd ../main-repo || git push',
    'cd ../main-repo || git push',
  ]);
});

test('#1 re-check: try/catch/finally keeps exception paths and drops what a catch-all consumed', async t => {
  const { fixture, onMain, feature } = await repositories(t);
  await expectBlocked(t, fixture, feature.directory, [
    'try { cd ../main-repo; cd ../feature-two } catch { git push }',
    'try { cd ../main-repo; cd ../feature-two } finally { git push }',
    'try { cd ../main-repo } catch [System.IO.IOException] { cd ../feature-two } finally { git push }',
    'try{cd ../main-repo}catch{cd ../feature-two};git push',
  ]);
  await expectKept(t, fixture, onMain.directory, [
    'try { cd ../feature-repo; git push } catch { Write-Output failed }',
    'try{cd ../main-repo}catch{cd ../main-repo}finally{cd ../feature-repo};git push',
  ]);
});

test('#1 re-check: if/else forms written with and without spaces keep their reachable locations', async t => {
  const { fixture, onMain, feature } = await repositories(t);
  await expectBlocked(t, fixture, feature.directory, [
    'if [ -f x ]; then cd ../main-repo; fi; git push',
    'if (Test-Path x){cd ../feature-two}else{cd ../main-repo};git push',
    'if [ -f a ]; then if [ -f b ]; then cd ../feature-two; else cd ../main-repo; fi; fi; git push',
  ]);
  await expectKept(t, fixture, onMain.directory, [
    'if (Test-Path x){cd ../feature-repo}else{cd ../feature-two};git push',
    'if [ -f a ]; then cd ../feature-repo; else cd ../feature-two; fi && git push',
  ]);
});

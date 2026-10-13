// Independent review tests for control flow and the execution location of an implicit push.
// Expected outcomes come from the approved texts, not from 99_Tools/CodexGuard:
// - review contract E/guard-review-contract-v1.md requirement 3 (decide a push by its actual
//   destination, including cwd) and requirement 4 (explicit compounds, cd/Set-Location);
// - implementation contract E/guard-impl-contract-v1.md requirement 5: &&, ;, ||, pipes, newlines
//   and the basic POSIX/PowerShell control forms are covered together with cd/Set-Location;
// - required checkpoint E/guard-review-required-checkpoint.md: a POSIX branch that changes the cwd,
//   an implicit push after the branch and a PowerShell else written without spaces must not lose
//   a protected destination, and commands whose every path is a work branch stay kept.
// Rule used for the expectations: when any path the shell can take runs the push from a repository
// whose implicit destination is main, the command is blocked; it is kept only when every path ends
// on a work branch. Which path a condition takes at run time is not known to a hook, so `false`,
// `test -d …`, try/catch and pipelines count as either way. A POSIX pipeline runs cd in a subshell
// while PowerShell runs it in the session, so both locations are possible for the same text.
import { test } from 'node:test';

import { createFixture, createRepository, expectBlocked, expectKept, PR } from './hook-fixture.mjs';

async function repositories(t) {
  const fixture = await createFixture(t);
  const onMain = await createRepository(fixture, { name: 'main-repo', current: 'main' });
  const feature = await createRepository(fixture, { name: 'feature-repo' });
  const featureTwo = await createRepository(fixture, { name: 'feature-two' });
  return { fixture, onMain, feature, featureTwo };
}

test('required checkpoint: a branch that cd-s into a repository on main keeps the implicit push blocked', async t => {
  const { fixture, feature } = await repositories(t);
  await expectBlocked(t, fixture, feature.directory, [
    'if [ -f marker ]; then cd ../main-repo; else cd ../feature-two; fi; git push',
    'if [ -f marker ]; then cd ../feature-two; else cd ../main-repo; fi; git push',
    'if [ -f a ]; then cd ../feature-two; elif [ -f b ]; then cd ../main-repo; else cd .; fi; git push origin HEAD',
    'if [ -f a ]; then if [ -f b ]; then cd ../main-repo; fi; fi\ngit push',
    'if (Test-Path marker) { cd ../feature-two }else{ cd ../main-repo }; git push',
    'if (Test-Path marker){cd ../main-repo}else{cd ../feature-two}; git push',
    'if (Test-Path a) { cd ../feature-two } elseif (Test-Path b) { cd ../main-repo } else { cd . }; git push',
  ]);
});

test('requirement 4: cd under && or || keeps every location the shell can reach', async t => {
  const { fixture, onMain, feature } = await repositories(t);
  await t.test('from the repository on main, a cd that may be skipped', async t => {
    await expectBlocked(t, fixture, onMain.directory, [
      'false && cd ../feature-repo; git push',
      'test -d ../missing && cd ../feature-repo; git push origin HEAD',
      '[ -f marker ] && cd ../feature-repo\ngit push',
    ]);
  });
  await t.test('from a work branch, the first cd of an || chain succeeds', async t => {
    await expectBlocked(t, fixture, feature.directory, [
      'cd ../main-repo || cd ../feature-two; git push',
      'Set-Location ../main-repo || Set-Location ../feature-two; git push',
    ]);
  });
});

test('requirement 4: PowerShell try/catch and pipelines keep every location the shell can reach', async t => {
  const { fixture, onMain, feature } = await repositories(t);
  await t.test('try runs the cd into the repository on main; catch does not run', async t => {
    await expectBlocked(t, fixture, feature.directory, [
      'try { cd ../main-repo } catch { cd ../feature-two }; git push',
      'try { Set-Location ../main-repo } catch { Set-Location ../feature-two } finally { }; git push',
    ]);
  });
  await t.test('a POSIX pipeline runs cd in a subshell, so the push stays in the repository on main', async t => {
    await expectBlocked(t, fixture, onMain.directory, ['cd ../feature-repo | cat; git push']);
  });
  await t.test('PowerShell runs Set-Location in the session, so the push moves to the repository on main', async t => {
    await expectBlocked(t, fixture, feature.directory, ['Set-Location ../main-repo | Out-Null; git push']);
  });
});

test('requirement 4: PowerShell catch and finally blocks written without a space are commands too', async t => {
  const { fixture, feature } = await repositories(t);
  await expectBlocked(t, fixture, feature.directory, [
    `try { git status } catch{ gh pr merge ${PR} }`,
    `try{git status}catch{gh pr merge ${PR}}`,
    `try { gh pr view ${PR} } catch [System.Exception]{ gh pr merge ${PR} }`,
    'try{git status}finally{git push origin main}',
  ]);
});

test('requirement 3 preservation: when every path ends on a work branch the implicit push is kept', async t => {
  const { fixture, onMain, feature } = await repositories(t);
  await expectKept(t, fixture, onMain.directory, [
    'if [ -f marker ]; then cd ../feature-repo; else cd ../feature-two; fi; git push',
    'if (Test-Path marker) { cd ../feature-repo }else{ cd ../feature-two }; git push',
    'cd ../feature-repo && git push',
  ]);
  await expectKept(t, fixture, feature.directory, [
    'if [ -f marker ]; then cd ../feature-two; fi; git push',
    'cd ../feature-two || cd .; git push',
    `try { gh pr view ${PR} } catch{ gh pr checks ${PR} }`,
  ]);
});

test('requirement 4 preservation: the same control words inside data are not commands', async t => {
  const { fixture, feature } = await repositories(t);
  await expectKept(t, fixture, feature.directory, [
    'git commit -m "if [ -f x ]; then cd ../main-repo; fi; git push"',
    `echo 'try{git status}catch{gh pr merge ${PR}}'`,
    `Write-Output "}else{ gh pr merge ${PR} }"`,
    'git log --grep "false && cd ../main-repo; git push"',
  ]);
});

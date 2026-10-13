// Re-check tests for push destinations, computed cd, read-only local Git and the inspection budget
// (first-fix review, defect #5/#6/#7/#9 lineage and requirement 8). Expected outcomes come from the
// approved texts and Git's documented refspec rules, never from 99_Tools/CodexGuard:
// - implementation contract requirement 3 (actual remote destination, heads/ refspecs, force/delete,
//   --all/--branches with --prune, every remote), requirement 4 (unknown remote default branch is an
//   error deny with a human recovery naming the remote; a named remote keeps its own HEAD evidence
//   even with a separate pushurl) and requirement 6/8 (caught errors are error denies; Git reads are
//   read-only and bounded);
// - re-check contract #5~#7 and #9 (computed cd only matters when a push needs the location);
// - git-push(1) "<refspec>": a <dst> such as heads/main is matched against the remote's refs, so it
//   names refs/heads/main; `--prune` deletes remote branches without a local counterpart.
import assert from 'node:assert/strict';
import { join } from 'node:path';
import { test } from 'node:test';

import {
  assertBlocked, assertKept, bashInput, blockReason, createFixture, createRepository, createSubdirectory,
  expectBlocked, expectKept, git, PR, runHook,
} from './hook-fixture.mjs';

// Refs and configuration of a repository, to show that a judgement wrote neither.
const localState = (fixture, directory) => [
  git(fixture, directory, ['for-each-ref', '--format=%(refname) %(objectname) %(symref)']),
  git(fixture, directory, ['config', '--list', '--local']),
].join('\n');

test('#5 re-check: heads/ destinations name the protected branch; look-alike names stay work branches', async t => {
  const fixture = await createFixture(t);
  const feature = await createRepository(fixture, { name: 'feature-repo' });
  const develop = await createRepository(fixture, { name: 'develop-repo', branches: ['main', 'develop', 'feat/guard'],
    remoteDefault: 'develop' });
  await expectBlocked(t, fixture, feature.directory, [
    'git push origin @:heads/main',
    'git push -f origin HEAD:heads/main',
    'git push origin -d heads/main',
    'git push origin feat/guard:heads/main feat/guard',
  ]);
  await expectBlocked(t, fixture, develop.directory, ['git push origin +HEAD:heads/develop', 'git push origin :heads/develop']);
  await expectKept(t, fixture, feature.directory, [
    'git push origin HEAD:feat/heads/main',
    'git push origin HEAD:heads-main',
    'git push origin HEAD:refs/heads/main-fix',
  ]);
  await expectKept(t, fixture, develop.directory, ['git push origin HEAD:heads/feat/guard']);
});

test('#6 re-check: --all/--branches with --prune can delete main; plain --all without a local main cannot', async t => {
  const fixture = await createFixture(t);
  const noMain = await createRepository(fixture, { name: 'no-main', branches: ['feat/guard'], current: 'feat/guard' });
  git(fixture, noMain.directory, ['branch', '-D', 'main']);
  await expectBlocked(t, fixture, noMain.directory, [
    'git push --prune --all origin',
    'git push origin --branches --prune',
    'git push --mirror origin',
  ]);
  await expectKept(t, fixture, noMain.directory, ['git push --all origin', 'git push --branches origin']);
});

test('#7 re-check: a named remote with a separate pushurl keeps its HEAD evidence and its name in the recovery', async t => {
  const fixture = await createFixture(t);
  const feature = await createRepository(fixture, { name: 'feature-repo', remotes: ['upstream', 'backup'] });
  const pushOnly = join(fixture.root, 'push-only.git');
  git(fixture, feature.directory, ['config', 'remote.upstream.pushurl', pushOnly]);
  await expectKept(t, fixture, feature.directory, [
    'git push upstream feat/guard',
    'git push',
    `git push "${pushOnly}" feat/guard`,
  ]);
  await expectBlocked(t, fixture, feature.directory, ['git push upstream HEAD:heads/main', 'git push backup HEAD:main']);

  git(fixture, feature.directory, ['symbolic-ref', '--delete', 'refs/remotes/backup/HEAD']);
  const before = localState(fixture, feature.directory);
  const run = runHook(fixture, bashInput(feature.directory, 'git push backup feat/guard'));
  assertBlocked(fixture, run, 'unknown backup default branch');
  const reason = blockReason(run);
  assert.match(reason, /error/i, 'an unknown default branch is an error deny, not a policy match');
  assert.match(reason, /backup/, 'the recovery names the actual remote');
  assert.doesNotMatch(reason, /set-head\s+"?git@|set-head\s+"?[A-Za-z]:[\\/]|set-head\s+"?\//,
    'the recovery does not put a URL or path where the remote name goes');
  assert.equal(localState(fixture, feature.directory), before, 'the guard repaired or changed local refs/config');
});

test('#9 re-check: a computed cd matters only when a later push needs the location', async t => {
  const fixture = await createFixture(t);
  const feature = await createRepository(fixture, { name: 'feature-repo' });
  const sub = await createSubdirectory(feature.directory, 'sub dir');
  await expectKept(t, fixture, feature.directory, [
    'cd $env:TEMP; Get-ChildItem',
    'cd "$(git rev-parse --show-toplevel)" && git status',
    `Set-Location (Join-Path $PWD 'sub dir'); git log -1`,
    `cd $env:TEMP; git -C "${feature.directory}" push origin feat/guard`,
  ]);
  await expectBlocked(t, fixture, feature.directory, [
    'cd $env:TEMP; git push',
    `cd "$(git rev-parse --show-toplevel)"; gh pr merge ${PR}`,
    'Set-Location $HOME; git push origin main',
  ]);
  const run = runHook(fixture, bashInput(sub, 'cd $env:TEMP; git push'));
  assertBlocked(fixture, run, 'computed cd before an implicit push');
  assert.match(blockReason(run), /error/i, 'an unknown push location is an error deny');
});

test('requirement 8: oversized or deeply nested input ends in an error deny instead of running on', async t => {
  const fixture = await createFixture(t);
  const feature = await createRepository(fixture, { name: 'feature-repo' });
  for (const command of [`${'git status; '.repeat(2100)}git log -1`, `${'('.repeat(40)}git status${')'.repeat(40)}`]) {
    const run = runHook(fixture, bashInput(feature.directory, command));
    assertBlocked(fixture, run, `budget: ${command.slice(0, 30)}`);
    assert.match(blockReason(run), /error/i, 'a budget stop is an error deny');
  }
  assertKept(fixture, runHook(fixture, bashInput(feature.directory, `${'git status; '.repeat(50)}git log -1`)), 'small chain');
});

// Requirement tests for git push to the remote main or remote default branch. Expected outcomes
// come from the approved texts and from git's documented push behaviour, not from
// 99_Tools/CodexGuard:
// - contract E/guard-pretest-contract-v1.md (SHA256 7fea9bfb…14d2) requirement 3: the actual
//   destination decides, through explicit refspecs, force and delete, several refspecs, options
//   and executable paths, the default destination, remote selection, push configuration and
//   --all/--mirror; local git merge, work branch pushes and ordinary reads are kept; a sub cwd,
//   git -C and the fixture's remote HEAD/default settings are inputs;
// - scope draft v1 59행: the default branch is identified from local remote HEAD/settings;
//   61행: a work branch push after `git fetch origin main` is kept;
// - contract 「위 요구는 …」: the protected targets are main and the actual default branch, so a
//   work branch is never blocked as a whole;
// - the lead's design supplement 1 (E/guard-pretest-design-supplement-01.md, Q2, reply to
//   msg_cff106ac428f): main on every remote and, on each remote, the default branch that local
//   refs show for that remote;
// - the lead's design supplement 2 (E/guard-pretest-design-supplement-02.json, SHA256
//   d91f3b21…ef34c, the main session's Q1 answer msg_1915fbc7b085 relayed in msg_e484b0bcd921,
//   read by this worker at 2026-10-13T14:20:25Z): with no remote HEAD and no other local evidence the push is
//   refused as an error that says the default branch could not be confirmed and how a person can
//   restore it locally, for the actual target remote; the guard neither queries the network nor
//   changes settings; a work branch push to a remote whose HEAD is known stays allowed.
// Git facts used to derive the samples (git-push documentation): with no refspec git pushes by
// remote.<name>.push, remote.<name>.mirror or push.default (simple by default, which pushes the
// current branch to the same name); `+` and --force only force; `:dst` and --delete delete dst;
// --all/--branches push every local branch and --mirror every ref.
// Each repository's remotes are empty bare repositories in the fixture; nothing is ever pushed.
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { test } from 'node:test';

import {
  assertBlocked, assertRemotesUntouched, bashInput, blockReason, createFixture, createRepository, createSubdirectory,
  expectBlocked, expectKept, git, runHook, runReport,
} from './hook-fixture.mjs';

const featureBranches = ['main', 'feat/guard', 'main-backup', 'feat/main'];

test('requirement 3: explicit refspecs, force and delete to main are blocked', async t => {
  const fixture = await createFixture(t);
  const repository = await createRepository(fixture, { branches: featureBranches });
  await expectBlocked(t, fixture, repository.directory, [
    'git push origin main',
    'git push origin HEAD:main',
    'git push origin @:main',
    'git push origin HEAD~0:main',
    'git push origin HEAD:refs/heads/main',
    'git push origin feat/guard:main',
    'git push origin refs/heads/feat/guard:refs/heads/main',
    'git push origin refs/heads/main',
    'git push origin main:main',
    'git push origin +HEAD:main',
    'git push origin +main',
    'git push -f origin main',
    'git push --force origin main',
    'git push origin main --force-with-lease',
    'git push --force-with-lease=main origin HEAD:main',
    'git push --force-if-includes --force-with-lease origin main',
    'git push origin --delete main',
    'git push --delete origin main',
    'git push -d origin main',
    'git push origin :main',
    'git push origin :refs/heads/main',
    'git push origin feat/guard main',
    'git push origin feat/guard HEAD:main',
    'git push --atomic origin feat/guard main',
    'git push -u origin HEAD:main',
    'git push --set-upstream origin main',
  ]);
  assertRemotesUntouched(fixture, repository, 'after the explicit refspecs');
});

test('requirement 3: options, executable paths, git global options and quoting do not hide main', async t => {
  const fixture = await createFixture(t);
  const repository = await createRepository(fixture, { branches: featureBranches });
  await expectBlocked(t, fixture, repository.directory, [
    'git push --no-verify origin main',
    'git push -o ci.skip origin main',
    'git push --push-option=ci.skip origin main',
    'git push --quiet origin main',
    'git push --verbose --progress origin main',
    'git push --tags origin main',
    'git push --follow-tags origin HEAD:main',
    '/usr/bin/git push origin main',
    'git.exe push origin main',
    '"C:\\Program Files\\Git\\cmd\\git.exe" push origin main',
    'git --no-pager push origin main',
    'git -c core.askPass=true push origin main',
    'git -C . push origin main',
    'git --git-dir=.git push origin main',
    'git push "origin" "main"',
    'git push origin "HEAD:main"',
    "git push 'origin' 'main'",
    'git  push   origin   main',
    'git\tpush\torigin\tmain',
  ]);
  assertRemotesUntouched(fixture, repository, 'after the spellings');
});

test('requirement 3: --all, --branches and --mirror reach main and are blocked', async t => {
  const fixture = await createFixture(t);
  const repository = await createRepository(fixture, { branches: featureBranches });
  await expectBlocked(t, fixture, repository.directory, [
    'git push --all origin',
    'git push --all',
    'git push --branches origin',
    'git push --mirror origin',
    'git push --mirror',
    'git push --all --force origin',
  ]);
  assertRemotesUntouched(fixture, repository, 'after --all and --mirror');
});

test('requirement 3: work branch pushes and look-alike names are kept', async t => {
  const fixture = await createFixture(t);
  const repository = await createRepository(fixture, { branches: featureBranches });
  await expectKept(t, fixture, repository.directory, [
    'git push origin feat/guard',
    'git push -u origin feat/guard',
    'git push --set-upstream origin feat/guard',
    'git push origin HEAD',
    'git push -u origin HEAD',
    'git push origin HEAD:feat/guard',
    'git push origin feat/guard:feat/guard',
    'git push origin HEAD:refs/heads/feat/guard',
    'git push --force-with-lease origin feat/guard',
    'git push -f origin feat/guard',
    'git push origin +feat/guard',
    'git push origin --delete feat/old',
    'git push origin :feat/old',
    'git push origin main:feat/guard',
    'git push origin main-backup',
    'git push origin feat/main',
    'git push origin HEAD:mainline',
    'git push origin HEAD:refs/heads/main-backup',
    'git push --dry-run origin feat/guard',
    'git push -o ci.skip origin feat/guard',
    'git push --no-verify origin feat/guard --force-with-lease=feat/guard',
  ]);
});

test('requirement 3: with no refspec the current branch decides', async t => {
  const fixture = await createFixture(t);
  const onMain = await createRepository(fixture, { name: 'on-main', current: 'main' });
  const onFeature = await createRepository(fixture, { name: 'on-feature', current: 'feat/guard' });

  await t.test('on main tracking origin/main', async t => {
    await expectBlocked(t, fixture, onMain.directory, [
      'git push',
      'git push origin',
      'git push origin HEAD',
      'git push -u origin HEAD',
      'git push --force',
      'git push -f',
      'git push --force-with-lease',
      'git push --no-verify',
      'git push -q',
    ]);
  });
  await t.test('on a work branch tracking the same name', async t => {
    await expectKept(t, fixture, onFeature.directory, [
      'git push',
      'git push origin',
      'git push --force-with-lease',
      'git push -f',
      'git push -u origin HEAD',
    ]);
  });
  assertRemotesUntouched(fixture, onMain, 'on-main');
  assertRemotesUntouched(fixture, onFeature, 'on-feature');
});

test('requirement 3: push configuration that sends a work branch to main is blocked', async t => {
  const fixture = await createFixture(t);
  const cases = [
    ['push.default=upstream with the work branch tracking main',
      [['push.default', 'upstream'], ['branch.feat/guard.merge', 'refs/heads/main']], ['git push', 'git push origin']],
    ['remote.origin.push=HEAD:refs/heads/main', [['remote.origin.push', 'HEAD:refs/heads/main']], ['git push', 'git push origin']],
    ['remote.origin.push=refs/heads/*:refs/heads/*', [['remote.origin.push', 'refs/heads/*:refs/heads/*']], ['git push', 'git push origin']],
    ['remote.origin.mirror=true', [['remote.origin.mirror', 'true']], ['git push', 'git push origin']],
    ['push.default=matching with main on both sides', [['push.default', 'matching']], ['git push', 'git push origin']],
  ];
  for (const [index, [label, config, commands]] of cases.entries()) {
    const repository = await createRepository(fixture, { name: `config-${index}`, config });
    await t.test(label, async t => {
      await expectBlocked(t, fixture, repository.directory, commands);
    });
    assertRemotesUntouched(fixture, repository, label);
  }

  const onMainCurrent = await createRepository(fixture, {
    name: 'current-on-main',
    current: 'main',
    config: [['push.default', 'current']],
  });
  await t.test('push.default=current on main', async t => {
    await expectBlocked(t, fixture, onMainCurrent.directory, ['git push']);
  });
  const onFeatureCurrent = await createRepository(fixture, { name: 'current-on-feature', config: [['push.default', 'current']] });
  await t.test('push.default=current on a work branch', async t => {
    await expectKept(t, fixture, onFeatureCurrent.directory, ['git push']);
  });
});

test('requirement 3: configuration given on the command line counts the same', async t => {
  const fixture = await createFixture(t);
  const repository = await createRepository(fixture);
  await t.test('sending to main', async t => {
    await expectBlocked(t, fixture, repository.directory, [
      'git -c push.default=matching push',
      'git -c remote.origin.push=HEAD:refs/heads/main push origin',
      'git -c remote.origin.mirror=true push origin',
    ]);
  });
  await t.test('sending the work branch to its own name', async t => {
    await expectKept(t, fixture, repository.directory, ['git -c push.default=current push']);
  });
  assertRemotesUntouched(fixture, repository, 'after command-line configuration');
});

test('requirement 3: a remote default branch other than main is protected as well as main', async t => {
  const fixture = await createFixture(t);
  const branches = ['main', 'trunk', 'feat/guard'];
  const onFeature = await createRepository(fixture, { name: 'trunk-feature', branches, remoteDefault: 'trunk' });
  const onTrunk = await createRepository(fixture, { name: 'trunk-current', branches, remoteDefault: 'trunk', current: 'trunk' });

  await t.test('pushes to trunk or main', async t => {
    await expectBlocked(t, fixture, onFeature.directory, [
      'git push origin trunk',
      'git push origin HEAD:trunk',
      'git push origin HEAD:refs/heads/trunk',
      'git push origin --delete trunk',
      'git push -f origin feat/guard:trunk',
      'git push origin main',
    ]);
  });
  await t.test('implicit push while on trunk', async t => {
    await expectBlocked(t, fixture, onTrunk.directory, ['git push', 'git push origin HEAD']);
  });
  await t.test('work branch pushes', async t => {
    await expectKept(t, fixture, onFeature.directory, ['git push origin feat/guard', 'git push origin HEAD', 'git push']);
  });
  assertRemotesUntouched(fixture, onFeature, 'trunk-feature');
  assertRemotesUntouched(fixture, onTrunk, 'trunk-current');
});

test('supplement 1 Q2: main on every remote and each remote\'s own default branch are protected', async t => {
  const fixture = await createFixture(t);
  const shape = {
    branches: ['main', 'develop', 'feat/guard'],
    remotes: ['origin', 'upstream'],
    remoteDefaults: { upstream: 'develop' },
  };
  const onFeature = await createRepository(fixture, { ...shape, name: 'two-remotes' });
  const onDevelop = await createRepository(fixture, { ...shape, name: 'two-remotes-develop', current: 'develop' });
  const pushDefault = await createRepository(fixture, {
    ...shape, name: 'push-default-upstream', current: 'develop', config: [['remote.pushDefault', 'upstream']],
  });
  const pushRemote = await createRepository(fixture, {
    ...shape, name: 'branch-push-remote', current: 'develop', config: [['branch.develop.pushRemote', 'upstream']],
  });

  await t.test('main anywhere, develop on upstream whose default it is', async t => {
    await expectBlocked(t, fixture, onFeature.directory, [
      'git push upstream main',
      'git push upstream HEAD:main',
      'git push upstream --delete main',
      'git push upstream develop',
      'git push upstream HEAD:develop',
      'git push -f upstream feat/guard:develop',
      'git push origin main',
      'git push --all upstream',
      'git push --mirror upstream',
    ]);
  });
  await t.test('develop on origin, whose default is main, and work branches anywhere', async t => {
    await expectKept(t, fixture, onFeature.directory, [
      'git push origin develop',
      'git push upstream feat/guard',
      'git push origin feat/guard',
    ]);
  });
  await t.test('on develop, pushing to origin', async t => {
    await expectKept(t, fixture, onDevelop.directory, ['git push', 'git push origin']);
  });
  await t.test('on develop, pushing to upstream', async t => {
    await expectBlocked(t, fixture, onDevelop.directory, ['git push upstream', 'git push upstream HEAD']);
  });
  await t.test('on develop with remote.pushDefault=upstream', async t => {
    await expectBlocked(t, fixture, pushDefault.directory, ['git push']);
  });
  await t.test('on develop with branch.develop.pushRemote=upstream', async t => {
    await expectBlocked(t, fixture, pushRemote.directory, ['git push']);
  });
  for (const repository of [onFeature, onDevelop, pushDefault, pushRemote]) {
    assertRemotesUntouched(fixture, repository, repository.directory);
  }
});

test('requirement 3: when origin/HEAD is unknown, main is still protected', async t => {
  const fixture = await createFixture(t);
  const repository = await createRepository(fixture, { name: 'no-origin-head', remoteDefault: null });
  await expectBlocked(t, fixture, repository.directory, [
    'git push origin main',
    'git push origin HEAD:main',
    'git push --all origin',
  ]);
  assertRemotesUntouched(fixture, repository, 'no-origin-head');
});

test('supplement 2 Q1: with no local evidence of the default branch, a work branch push is an error naming the remote', async t => {
  const fixture = await createFixture(t);
  const repository = await createRepository(fixture, { name: 'never-fetched', remotes: ['origin', 'upstream'], remoteRefs: false });
  const cwd = repository.directory;
  const configPath = join(cwd, '.git', 'config');
  const configBefore = readFileSync(configPath, 'utf8');
  const assertNothingRepaired = (run, label) => {
    assert.equal(readFileSync(configPath, 'utf8'), configBefore, runReport(run, `${label}: the repository configuration is unchanged`));
    const written = git(fixture, cwd, ['for-each-ref', '--format=%(refname)', 'refs/remotes']).trim();
    assert.equal(written, '', runReport(run, `${label}: the guard wrote no remote-tracking ref or remote HEAD`));
  };

  let policyReason = '';
  await t.test('main is blocked whatever the default branch', () => {
    const run = runHook(fixture, bashInput(cwd, 'git push origin main'));
    assertBlocked(fixture, run, 'git push origin main');
    assertNothingRepaired(run, 'git push origin main');
    policyReason = blockReason(run);
  });
  const cases = [
    ['git push origin feat/guard', 'origin'],
    ['git push -u origin HEAD', 'origin'],
    ['git push', 'origin'],
    ['git push upstream feat/guard', 'upstream'],
    ['git push upstream HEAD:feat/guard', 'upstream'],
  ];
  for (const [command, remote] of cases) {
    await t.test(command, () => {
      const run = runHook(fixture, bashInput(cwd, command));
      assertBlocked(fixture, run, command);
      assertNothingRepaired(run, command);
      const reason = blockReason(run);
      assert.notEqual(reason, policyReason, runReport(run, `${command}: the reason reports an error, not the policy match`));
      assert.ok(reason.includes(remote), runReport(run, `${command}: the reason names the target remote "${remote}"`));
    });
  }
  assertRemotesUntouched(fixture, repository, 'never-fetched');
});

test('requirement 3: a sub cwd and git -C select the repository whose branch decides', async t => {
  const fixture = await createFixture(t);
  const onMain = await createRepository(fixture, { name: 'main-repo', current: 'main' });
  const onFeature = await createRepository(fixture, { name: 'feature-repo' });
  const mainNested = await createSubdirectory(onMain.directory, 'tools', 'nested');
  const featureNested = await createSubdirectory(onFeature.directory, 'tools', 'nested');
  const plain = await createSubdirectory(fixture.root, 'plain');
  const slash = path => path.split('\\').join('/');

  await t.test('cwd inside a repository on main', async t => {
    await expectBlocked(t, fixture, mainNested, ['git push', 'git push origin HEAD']);
  });
  await t.test('cwd inside a repository on a work branch', async t => {
    await expectKept(t, fixture, featureNested, ['git push']);
  });
  await t.test('git -C into the repository on main, from the work branch repository', async t => {
    await expectBlocked(t, fixture, onFeature.directory, [
      'git -C ../main-repo push',
      'git -C ../main-repo push origin HEAD',
      `git -C "${slash(onMain.directory)}" push`,
    ]);
  });
  await t.test('git -C into the repository on main, from a folder outside any repository', async t => {
    await expectBlocked(t, fixture, plain, [`git -C "${slash(onMain.directory)}" push`, 'git -C ../main-repo/tools push']);
  });
  await t.test('git -C into the repository on a work branch, from the main repository', async t => {
    await expectKept(t, fixture, onMain.directory, [
      'git -C ../feature-repo push',
      `git -C "${slash(onFeature.directory)}" push origin HEAD`,
    ]);
  });
  await t.test('git -C into the repository on a work branch, from a folder outside any repository', async t => {
    await expectKept(t, fixture, plain, ['git -C ../feature-repo push']);
  });
  assertRemotesUntouched(fixture, onMain, 'main-repo');
  assertRemotesUntouched(fixture, onFeature, 'feature-repo');
});

test('requirement 3: local merges, fetches and reads are kept', async t => {
  const fixture = await createFixture(t);
  const repository = await createRepository(fixture, { branches: featureBranches });
  await expectKept(t, fixture, repository.directory, [
    'git merge origin/main',
    'git merge --no-ff main',
    'git merge --squash feat/main',
    'git fetch origin main',
    'git fetch origin main && git push origin feat/guard',
    'git pull origin main',
    'git pull --rebase origin main',
    'git rebase origin/main',
    'git log --oneline origin/main..HEAD',
    'git log --merges --grep "Merge pull request"',
    'git diff main...HEAD',
    'git merge-base HEAD origin/main',
    'git status',
    'git switch main',
    'git checkout main',
    'git branch -d feat/main',
    'git remote -v',
    'git ls-remote origin main',
    'git show-ref refs/remotes/origin/main',
  ]);
  assertRemotesUntouched(fixture, repository, 'after the kept commands');
});

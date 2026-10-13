// Requirement tests that the hook judges a command without running it. Expected outcomes come
// from the approved texts, not from 99_Tools/CodexGuard:
// - contract E/guard-pretest-contract-v1.md (SHA256 7fea9bfb…14d2) requirement 7: the hook never
//   runs the command it receives; local git reads in the fixture are allowed, network access and
//   remote changes are not; requirement 6: a kept call is neither rewritten nor run by the hook;
// - scope draft v1 57행: the guard runs no command on the caller's behalf.
// Every command below would leave a trace if the hook ran it: a ref in the empty bare remote, a
// line in the fake gh call log, a new local branch or a moved HEAD. Each case also checks the
// decision, so a missing hook cannot pass by doing nothing.
import assert from 'node:assert/strict';
import { test } from 'node:test';

import {
  assertBlocked, assertKept, assertRemotesUntouched, bashInput, createFixture, createRepository, git, PR, runHook,
  runReport,
} from './hook-fixture.mjs';

function localState(fixture, directory) {
  return {
    head: git(fixture, directory, ['symbolic-ref', '-q', 'HEAD']).trim(),
    branches: git(fixture, directory, ['for-each-ref', '--format=%(refname)', 'refs/heads']).trim(),
  };
}

test('requirement 7: judged commands leave no trace in remotes, gh or the local repository', async t => {
  const fixture = await createFixture(t);
  const repository = await createRepository(fixture);
  const cwd = repository.directory;
  const before = localState(fixture, cwd);

  const blocked = [
    'git push origin HEAD:main',
    'git push --mirror origin',
    `gh pr merge ${PR} --squash`,
    `gh api -X PUT repos/owner/repo/pulls/${PR}/merge`,
    'git checkout -q -b executed-before-block && git push origin HEAD:main',
    'bash -c "git branch executed-in-wrapper && git push origin main"',
  ];
  const kept = [
    'git push origin feat/guard',
    `gh pr view ${PR}`,
    'git checkout -q -b executed-by-hook',
    'git switch main',
    'bash -c "git branch executed-in-kept-wrapper"',
  ];

  for (const command of blocked) {
    await t.test(`blocked: ${command}`, () => {
      const run = runHook(fixture, bashInput(cwd, command));
      assertRemotesUntouched(fixture, repository, command);
      assert.deepEqual(localState(fixture, cwd), before, runReport(run, `${command}: local HEAD and branches are unchanged`));
      assertBlocked(fixture, run, command);
    });
  }
  for (const command of kept) {
    await t.test(`kept: ${command}`, () => {
      const run = runHook(fixture, bashInput(cwd, command));
      assertRemotesUntouched(fixture, repository, command);
      assert.deepEqual(localState(fixture, cwd), before, runReport(run, `${command}: local HEAD and branches are unchanged`));
      assertKept(fixture, run, command);
    });
  }
});

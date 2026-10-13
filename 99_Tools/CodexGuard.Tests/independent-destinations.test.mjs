// Independent review tests for push destinations. Expected outcomes come from the approved texts and
// from git's documented refspec rules, not from 99_Tools/CodexGuard:
// - review contract E/guard-review-contract-v1.md requirement 3: a push is judged by its actual
//   destination (refspec, delete, all, prune, remote selection); main and each remote's default
//   branch are protected; a work branch whose default-branch evidence exists locally is kept;
// - implementation contract E/guard-impl-contract-v1.md requirement 4 (main msg_1915fbc7b085): the
//   error for a missing default branch names the remote and a person's local recovery, and a work
//   branch push whose evidence is confirmed is kept.
// git push documentation (<refspec>…, "If <dst> doesn't start with refs/ … it is matched against
// the remote's refs" with the same rules as rev-parse): a destination such as heads/main resolves
// to refs/heads/main on the remote. --prune removes remote branches that have no local counterpart
// under the pushed refspecs, and --all pushes refs/heads/*, so --all --prune deletes a remote main
// the repository no longer has locally.
// A remote's pushurl only changes the URL git sends to; the remote keeps its name, its
// remote-tracking refs and its refs/remotes/<name>/HEAD.
import assert from 'node:assert/strict';
import { join } from 'node:path';
import { test } from 'node:test';

import {
  assertBlocked, assertKept, bashInput, blockReason, createFixture, createRepository, expectBlocked, expectKept,
  git, runHook, runReport,
} from './hook-fixture.mjs';

test('requirement 3: destinations spelled heads/<branch> resolve to the protected branch', async t => {
  const fixture = await createFixture(t);
  const repository = await createRepository(fixture);
  await expectBlocked(t, fixture, repository.directory, [
    'git push origin HEAD:heads/main',
    'git push origin heads/main',
    'git push origin +HEAD:heads/main',
    'git push origin --delete heads/main',
    'git push origin :heads/main',
  ]);
  await expectKept(t, fixture, repository.directory, [
    'git push origin HEAD:feat/heads/main',
    'git push origin HEAD:heads-main',
  ]);
});

test('requirement 3: heads/<default> reaches a default branch other than main', async t => {
  const fixture = await createFixture(t);
  const repository = await createRepository(fixture, {
    branches: ['main', 'develop', 'feat/guard'], remoteDefault: 'develop',
  });
  await expectBlocked(t, fixture, repository.directory, ['git push origin HEAD:heads/develop']);
});

test('requirement 3: --all --prune deletes a remote main that has no local branch', async t => {
  const fixture = await createFixture(t);
  const repository = await createRepository(fixture);
  git(fixture, repository.directory, ['branch', '-D', 'main']);
  await expectBlocked(t, fixture, repository.directory, [
    'git push --all --prune origin',
    'git push --prune --branches origin',
  ]);
  await expectKept(t, fixture, repository.directory, ['git push --all origin']);
});

test('requirement 3 preservation: a remote with a separate pushurl keeps its own default-branch evidence', async t => {
  const fixture = await createFixture(t);
  const repository = await createRepository(fixture);
  git(fixture, repository.directory, ['config', 'remote.origin.pushurl', join(fixture.root, 'push-only.git')]);
  await expectKept(t, fixture, repository.directory, [
    'git push origin feat/guard',
    'git push origin HEAD',
    'git push',
  ]);
  await expectBlocked(t, fixture, repository.directory, ['git push origin main', 'git push origin HEAD:main']);
});

// Required checkpoint (Windows URL separators): a remote named by its local path, written with the
// other separator or (on Windows, whose paths ignore case) another letter case, is still the
// configured remote, so its local default-branch evidence applies.
test('required checkpoint: a push to a configured remote written as a path with other separators', async t => {
  const fixture = await createFixture(t);
  const repository = await createRepository(fixture, {
    branches: ['main', 'develop', 'feat/guard'], remoteDefault: 'develop',
  });
  const bare = repository.bares.origin;
  const spellings = [bare, bare.split('\\').join('/')];
  if (process.platform === 'win32') spellings.push(bare.split('\\').join('/').toUpperCase());
  await expectKept(t, fixture, repository.directory, spellings.map(path => `git push "${path}" feat/guard`));
  await expectBlocked(t, fixture, repository.directory, spellings.map(path => `git push "${path}" HEAD:develop`));
});

test('implementation contract 4: the missing-default error names the remote and its recovery works', async t => {
  const fixture = await createFixture(t);
  const repository = await createRepository(fixture, { remoteDefault: null });
  const command = 'git push origin feat/guard';

  const before = runHook(fixture, bashInput(repository.directory, command));
  assertBlocked(fixture, before, `${command} while origin/HEAD is unknown`);
  const reason = blockReason(before);
  assert.match(reason, /\borigin\b/, runReport(before, 'the error names the remote origin'));
  assert.doesNotMatch(reason, /^Policy/i, runReport(before, 'the error is not presented as a policy match'));

  // A person who verified that origin's default branch is main restores the local symbolic ref.
  git(fixture, repository.directory, ['remote', 'set-head', 'origin', 'main']);
  const after = runHook(fixture, bashInput(repository.directory, command));
  assertKept(fixture, after, `${command} after git remote set-head origin main`);
});

// Independent review tests for the public stdin boundary and what a block says. Expected outcomes
// come from the approved texts, not from 99_Tools/CodexGuard:
// - review contract E/guard-review-contract-v1.md requirement 5: malformed input and caught errors
//   end in a deny, and a policy deny and an error deny are told apart by what they mean, not only
//   by being different strings;
// - implementation contract E/guard-impl-contract-v1.md requirement 4: an explicit main update is
//   a policy deny whatever the local default-branch evidence; requirement 6: an error deny says it
//   is an error.
// "Says it is an error" is read as: the reason contains the word "error" and does not present
// itself as a policy match. "Names the protected target" is read as: the reason contains the branch.
import assert from 'node:assert/strict';
import { appendFile } from 'node:fs/promises';
import { join } from 'node:path';
import { test } from 'node:test';

import {
  assertBlocked, bashInput, blockReason, createFixture, createRepository, PR, runHook, runReport,
} from './hook-fixture.mjs';

test('requirement 5: stdin that is not valid UTF-8 JSON text is an error deny', async t => {
  const fixture = await createFixture(t);
  const repository = await createRepository(fixture);
  const json = Buffer.from(JSON.stringify(bashInput(repository.directory, 'echo ok')));
  const invalidUtf8 = Buffer.concat([json.subarray(0, json.length - 3), Buffer.from([0xff, 0xfe]), json.subarray(json.length - 3)]);
  const run = runHook(fixture, null, { raw: invalidUtf8, cwd: repository.directory });
  assertBlocked(fixture, run, 'stdin with invalid UTF-8 bytes');
  assert.match(blockReason(run), /error/i, runReport(run, 'the reason says it is an error'));
});

test('requirement 5: a policy deny names what it protects, an error deny says it is an error', async t => {
  const fixture = await createFixture(t);
  const repository = await createRepository(fixture, { current: 'main' });
  const broken = await createRepository(fixture, { name: 'broken-config', current: 'main' });
  await appendFile(join(broken.directory, '.git', 'config'), '\n[broken\n');

  await t.test('explicit main in a readable repository: policy deny naming main', () => {
    const run = runHook(fixture, bashInput(repository.directory, 'git push origin HEAD:main'));
    assertBlocked(fixture, run, 'explicit main');
    assert.match(blockReason(run), /\bmain\b/, runReport(run, 'the policy reason names main'));
    assert.doesNotMatch(blockReason(run), /error/i, runReport(run, 'a policy match is not called an error'));
  });
  await t.test('explicit main in a repository git cannot read: still a policy deny naming main', () => {
    const run = runHook(fixture, bashInput(broken.directory, 'git push origin HEAD:main'));
    assertBlocked(fixture, run, 'explicit main, broken config');
    assert.match(blockReason(run), /\bmain\b/, runReport(run, 'the policy reason names main'));
    assert.doesNotMatch(blockReason(run), /error/i, runReport(run, 'the explicit main match does not depend on the read'));
  });
  await t.test('implicit push in a repository git cannot read: error deny, not a policy match', () => {
    const run = runHook(fixture, bashInput(broken.directory, 'git push'));
    assertBlocked(fixture, run, 'implicit push, broken config');
    assert.match(blockReason(run), /error/i, runReport(run, 'the reason says it is an error'));
    assert.doesNotMatch(blockReason(run), /^policy/i, runReport(run, 'the reason does not present a policy match'));
  });
  await t.test('gh pr merge: policy deny naming the merge', () => {
    const run = runHook(fixture, bashInput(repository.directory, `gh pr merge ${PR}`));
    assertBlocked(fixture, run, 'gh pr merge');
    assert.match(blockReason(run), /merge/i, runReport(run, 'the policy reason names the merge'));
    assert.doesNotMatch(blockReason(run), /error/i, runReport(run, 'a policy match is not called an error'));
  });
});

// Independent review tests for the shell wrappers and literal evaluation the implementation
// contract names. Expected outcomes come from the approved texts, not from 99_Tools/CodexGuard:
// - implementation contract E/guard-impl-contract-v1.md requirement 5: pwsh/powershell -Command and
//   literal one-string eval/Invoke-Expression/iex are covered;
// - the lead's design supplement 1 (E/guard-pretest-design-supplement-01.md, Q5): one literal string
//   given to eval/Invoke-Expression is covered; the body of -EncodedCommand is excluded.
// PowerShell parameter names are case-insensitive and may be shortened to any unique prefix
// (-exec, -executionpolicy and -ExecutionPolicy are the same parameter), and Invoke-Expression takes
// its one string through the named parameter -Command as well as by position.
//
// User decision 7 (msg_5c404e04e508, goal.md 「적용 중인 사용자 결정」 7) makes every shell host and
// evaluator form unsupported. A wrapped command whose text names a push or merge is an intended
// Unsupported form deny even when the destination looks like a work branch or the words sit in a
// nested data quote (E/guard-redesign-02-contract-v1.md lines 55 and 70: host option meaning is
// not simulated to allow it). Wrapped commands without such words stay kept.
import { test } from 'node:test';

import { createFixture, createRepository, expectBlocked, expectKept, PR } from './hook-fixture.mjs';
import { expectUnsupported } from './unsupported-form.mjs';

test('implementation contract 5: powershell and pwsh -Command after other parameters in any case', async t => {
  const fixture = await createFixture(t);
  const repository = await createRepository(fixture);
  await expectBlocked(t, fixture, repository.directory, [
    `powershell -ExecutionPolicy Bypass -Command "gh pr merge ${PR}"`,
    `powershell -exec bypass -command "gh pr merge ${PR}"`,
    'powershell -executionpolicy bypass -c "git push origin main"',
    `pwsh -noninteractive -command "gh pr merge ${PR}"`,
    `pwsh -NoLogo -NonInteractive -ExecutionPolicy RemoteSigned -Command "gh pr merge ${PR}"`,
  ]);
  await expectKept(t, fixture, repository.directory, [`powershell -exec bypass -command "gh pr view ${PR}"`]);
  await expectUnsupported(t, fixture, repository.directory, ['pwsh -noninteractive -command "git push origin feat/guard"']);
});

test('implementation contract 5: one literal string given to Invoke-Expression by its -Command parameter', async t => {
  const fixture = await createFixture(t);
  const repository = await createRepository(fixture);
  await expectBlocked(t, fixture, repository.directory, [
    `Invoke-Expression -Command 'gh pr merge ${PR}'`,
    'iex -Command "git push origin main"',
  ]);
  await expectKept(t, fixture, repository.directory, [`Invoke-Expression -Command 'gh pr view ${PR}'`]);
  await expectUnsupported(t, fixture, repository.directory, [`Invoke-Expression 'echo "gh pr merge ${PR}"'`]);
});

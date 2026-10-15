// Assertions for the intended rejection of an unsupported form (goal user decision 7). Like
// hook-fixture.mjs it holds no judgement of which command is unsupported; each test file lists its
// commands from the requirement text. The required parts of the reason are quoted from the
// approved texts, never taken from 99_Tools/CodexGuard:
// - user decision msg_5c404e04e508 (goal.md 「적용 중인 사용자 결정」 7): outside the simple forms a
//   command with merge/push words is denied with the guidance 「단순하게 다시 써라」;
// - main's instruction in the same message and the implementation contract
//   E/guard-redesign-02-contract-v1.md 「판정/오류/안내」 2: the reason names the unsupported form
//   ("Unsupported form 등의 명확한 이유") and gives the rewrite example
//   `cd <경로> && git push origin <branch>`;
// - the same contract 「판정/오류/안내」 4: an unsupported or unparsable command is a different result
//   from an input envelope, I/O or internal failure (Error deny) and from a policy match.
import assert from 'node:assert/strict';

import { assertBlocked, bashInput, blockReason, caseName, runHook, runReport } from './hook-fixture.mjs';

export const rewriteExample = 'cd <경로> && git push origin <branch>';

export function unsupportedProblems(reason) {
  const problems = [];
  if (!/Unsupported form/.test(reason)) problems.push('the reason does not name the unsupported form');
  if (!reason.includes('단순하게 다시 써라')) problems.push('the reason lacks the 「단순하게 다시 써라」 guidance');
  if (!reason.includes(rewriteExample)) problems.push(`the reason lacks the rewrite example ${rewriteExample}`);
  if (/^\s*Error deny/i.test(reason)) problems.push('the unsupported form is presented as an input/I/O error');
  if (/^\s*Policy deny/i.test(reason)) problems.push('the unsupported form is presented as a policy match');
  return problems;
}

export function assertUnsupported(fixture, run, label) {
  assertBlocked(fixture, run, label);
  const problems = unsupportedProblems(blockReason(run));
  if (problems.length > 0) {
    assert.fail(runReport(run, `${label}: expected an Unsupported form deny with rewrite guidance`, problems));
  }
}

// One subtest per command, all judged in the same cwd.
export async function expectUnsupported(t, fixture, cwd, commands, overrides) {
  for (const command of commands) {
    await t.test(caseName(command), () => {
      assertUnsupported(fixture, runHook(fixture, bashInput(cwd, command, overrides)), caseName(command));
    });
  }
}

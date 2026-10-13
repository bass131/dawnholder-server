// Requirement tests for inputs the guard cannot judge. Expected outcomes come from the approved
// texts, not from 99_Tools/CodexGuard:
// - contract E/guard-pretest-contract-v1.md (SHA256 7fea9bfb…14d2) requirement 6: input errors
//   and internal failures are not disguised as a normal deny or as real protection;
// - the lead's design supplement 1 (E/guard-pretest-design-supplement-01.md, Q3 B, reply to
//   msg_cff106ac428f): (a) stdin that is not a JSON object, (b) a Bash call whose
//   tool_input.command is missing or not a string, (c) a caught internal exception and (d) a
//   failed git read needed for an implicit push destination all end in a deny that says it is an
//   error, and the diagnosis tells a policy match apart from an input or judgement failure.
// Normal events and tools outside the guard are not input errors (entry-contract.test.mjs). A hook
// that never ran, timed out, was killed or lost its output decides nothing; that is not claimed
// as fail-closed and not tested here.
// The wording of an error is not fixed: each case checks that the call is blocked and that the
// reason differs from the reasons the hook gives for policy matches in the same fixture. Case (c)
// cannot be caused through the entry without a test seam in the product; it is left to the
// implementation review and listed in the requirement table.
import assert from 'node:assert/strict';
import { appendFile } from 'node:fs/promises';
import { join } from 'node:path';
import { test } from 'node:test';

import {
  assertBlocked, bashInput, blockReason, createFixture, createRepository, createSubdirectory, PR, runHook, runReport,
} from './hook-fixture.mjs';

function withoutField(input, field) {
  const copy = { ...input };
  delete copy[field];
  return copy;
}

test('supplement 1 Q3 (a)(b)(d): inputs and git reads the guard cannot judge end in a block that says it is an error', async t => {
  const fixture = await createFixture(t);
  const repository = await createRepository(fixture, { current: 'main' });
  const broken = await createRepository(fixture, { name: 'broken-config', current: 'main' });
  await appendFile(join(broken.directory, '.git', 'config'), '\n[broken\n');
  const plain = await createSubdirectory(fixture.root, 'plain');
  const cwd = repository.directory;

  const policyReasons = [];
  await t.test('policy matches give the reference reasons', () => {
    for (const command of [`gh pr merge ${PR}`, 'git push origin main']) {
      const run = runHook(fixture, bashInput(cwd, command));
      assertBlocked(fixture, run, `${command} (policy reference)`);
      policyReasons.push(blockReason(run));
    }
  });

  const bash = bashInput(cwd, `gh pr merge ${PR}`);
  const cases = [
    ['(a) stdin is cut-off JSON', { raw: '{"hook_event_name": "PreToolUse", "tool_name": "Bash", ', cwd }],
    ['(a) stdin is empty', { raw: '', cwd }],
    ['(a) stdin is a JSON array', { raw: JSON.stringify([bash]), cwd }],
    ['(a) stdin is a JSON string', { raw: JSON.stringify(`gh pr merge ${PR}`), cwd }],
    ['(b) tool_input is missing', { input: withoutField(bash, 'tool_input') }],
    ['(b) tool_input.command is missing', { input: { ...bash, tool_input: {} } }],
    ['(b) tool_input.command is null', { input: { ...bash, tool_input: { command: null } } }],
    ['(b) tool_input.command is a number', { input: { ...bash, tool_input: { command: PR } } }],
    ['(b) tool_input.command is an argument array', { input: { ...bash, tool_input: { command: ['gh', 'pr', 'merge', String(PR)] } } }],
    ['(d) implicit push from a folder outside any repository', { input: bashInput(plain, 'git push') }],
    ['(d) implicit push into a missing git -C folder', { input: bashInput(cwd, 'git -C ../missing-repo push') }],
    ['(d) implicit push in a repository whose config git cannot read', { input: bashInput(broken.directory, 'git push') }],
  ];
  for (const [label, { input, raw, cwd: processCwd }] of cases) {
    await t.test(label, () => {
      const run = runHook(fixture, input, { raw, cwd: processCwd ?? input?.cwd });
      assertBlocked(fixture, run, label);
      const reason = blockReason(run);
      assert.ok(!policyReasons.includes(reason),
        runReport(run, `${label}: the reason differs from the policy-match reasons (${policyReasons.join(' | ')})`));
    });
  }
});

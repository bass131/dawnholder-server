// Independent regressions for the merge gate's PermissionRequest decision. Expected outcomes come
// from the approved rules, not from 99_Tools/MergeGate:
// - behavior contract v2 (E/merge-gate-behavior-spec.md, SHA256 d16918f6…7c9a) §1 the 120-second
//   window from usedAt, §2 the exact PermissionRequest allow output, §5 allow only for Bash with
//   the marker, no agent_id, and a record of this session whose usedCommand equals the command and
//   whose usedAt is within 120 seconds; otherwise, and for input errors, decision none; never an
//   allow for an ask command that is not a merge (for example `gh pr create`);
// - user decision A 「hook이 대신 승인한다」 relayed by Main msg_af033fe88521;
// - 리드 답 msg_93f732ba921f item 2 (reply msg_eaa7a91f2cee): no decision for any command that is
//   not the exact single merge form of §4 condition 4, shown with forged records;
// - design probe r6 (E/design-probe/raw/hooklog/r6.jsonl line 3): the observed PermissionRequest
//   input, which has no tool_use_id.
// A stdin that is not JSON cannot be told apart from a PreToolUse input, so input errors here are
// JSON inputs of the PermissionRequest event with a missing or wrong field.
//
// Environment: node only. Each test owns a temporary project passed as CLAUDE_PROJECT_DIR.
import { test } from 'node:test';

import {
  approval, assertAllow, assertNoDecision, assertPermissionAllow, bashInput, createProject, HEAD_A, MINUTE,
  OTHER_SESSION, PR, permissionInput, runHook, writeApprovals,
} from './hook-fixture.mjs';

const merge = `gh pr merge ${PR} --squash --match-head-commit ${HEAD_A}`;
const SECOND = 1000;

// A record whose use was `usedCommand`, `usedAgo` milliseconds ago.
async function usedRecord(t, { marker = true, usedCommand = merge, usedAgo = 30 * SECOND, session } = {}) {
  const fixture = await createProject(t, { marker });
  await writeApprovals(fixture.project, [approval({ createdAgo: 2 * MINUTE, usedAgo, usedCommand })], session);
  return fixture;
}

test('§5 the command that just passed PreToolUse is allowed without a confirmation', async t => {
  const fixture = await createProject(t);
  await writeApprovals(fixture.project, [approval()]);
  assertAllow(runHook(fixture, bashInput(fixture.project, merge)), PR, 'PreToolUse pass');
  assertPermissionAllow(runHook(fixture, permissionInput(fixture.project, merge)), 'same command');
});

test('§5 a record used 30 seconds ago by the same command is allowed', async t => {
  const fixture = await usedRecord(t);
  assertPermissionAllow(runHook(fixture, permissionInput(fixture.project, merge)), 'used 30 s ago');
});

test('§5 decision none: another command, no pass, an old pass, a subagent, no marker, another session', async t => {
  const other = await usedRecord(t);
  assertNoDecision(runHook(other, permissionInput(other.project, `gh pr merge ${PR} --merge --match-head-commit ${HEAD_A}`)),
    'another command');
  assertNoDecision(runHook(other, permissionInput(other.project, ` ${merge}`)), 'not exactly the same string');

  const unused = await createProject(t);
  await writeApprovals(unused.project, [approval()]);
  assertNoDecision(runHook(unused, permissionInput(unused.project, merge)), 'record never used');

  const empty = await createProject(t);
  assertNoDecision(runHook(empty, permissionInput(empty.project, merge)), 'no record at all');

  const old = await usedRecord(t, { usedAgo: 3 * MINUTE });
  assertNoDecision(runHook(old, permissionInput(old.project, merge)), 'used 3 minutes ago');

  const subagent = await usedRecord(t);
  assertNoDecision(runHook(subagent, permissionInput(subagent.project, merge, { agent: true })), 'agent_id present');

  const unmarked = await usedRecord(t, { marker: false });
  assertNoDecision(runHook(unmarked, permissionInput(unmarked.project, merge)), 'no marker');

  const otherSession = await usedRecord(t, { session: OTHER_SESSION });
  assertNoDecision(runHook(otherSession, permissionInput(otherSession.project, merge)), 'record of another session');
});

test('§5 an ask command that is not the single merge form is never allowed, even with a forged record', async t => {
  const plain = await createProject(t);
  assertNoDecision(runHook(plain, permissionInput(plain.project, 'gh pr create --fill')), 'gh pr create');

  const forged = [
    'gh pr create --fill',
    'git push origin main',
    `git fetch origin && ${merge}`,
    `gh pr merge ${PR} --squash --auto --match-head-commit ${HEAD_A}`,
  ];
  for (const command of forged) {
    const fixture = await usedRecord(t, { usedCommand: command });
    assertNoDecision(runHook(fixture, permissionInput(fixture.project, command)), `forged record for: ${command}`);
  }
});

test('§5 input errors and other tools get no decision', async t => {
  const fixture = await usedRecord(t);
  const cases = [
    ['missing session_id', input => { delete input.session_id; }],
    ['numeric command', input => { input.tool_input.command = 123; }],
    ['missing command', input => { delete input.tool_input.command; }],
    ['tool Write', input => { input.tool_name = 'Write'; }],
    ['missing tool_name', input => { delete input.tool_name; }],
  ];
  for (const [label, change] of cases) {
    const input = permissionInput(fixture.project, merge);
    change(input);
    assertNoDecision(runHook(fixture, input), label);
  }
});

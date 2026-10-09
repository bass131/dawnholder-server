// Independent regressions for approval records the hook cannot trust, the edges of its two time
// windows and failures inside the entry. Expected outcomes come from the approved rules, not from
// 99_Tools/MergeGate:
// - behavior contract v2 (E/merge-gate-behavior-spec.md, SHA256 d16918f6…7c9a) §1 record format
//   and the 30-minute / 120-second constants, §3 「기록 쓰기가 실패해도 prompt를 막지 않는다」,
//   §4 condition 9 and input errors, §5 「usedAt이 120초 안」 and 「입력 오류도 결정 없음」;
// - the lead's supplement in the implementation contract (E/impl-contract.md 「구현 기준」):
//   an unreadable or malformed record counts as no record (PreToolUse no-approval, PermissionRequest
//   decision none, UserPromptSubmit replaces the file with one new record); an unexpected failure in
//   the entry blocks PreToolUse with invalid-input and gives no decision for the other two events;
//   Bash commands that are not merge attempts end with no decision without reading the record.
// Exact boundaries (exactly 30:00 or 120 s) are not settled by the contract, so the time cases keep
// a ten-second margin on each side.
//
// Environment: node only. Each test owns a temporary project passed as CLAUDE_PROJECT_DIR.
import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { existsSync } from 'node:fs';
import { mkdir, readFile, writeFile } from 'node:fs/promises';
import { test } from 'node:test';

import {
  approval, approvalsDirectory, approvalsFile, assertAllow, assertDeny, assertNoDecision, assertPermissionAllow,
  assertPromptNotBlocked, bashInput, createProject, HEAD_A, hookPath, MINUTE, permissionInput, PR, promptInput,
  readApprovals, runHook, runReport, writeApprovals,
} from './hook-fixture.mjs';

const SECOND = 1000;
const mergeCommand = `gh pr merge ${PR} --merge --match-head-commit ${HEAD_A}`;
const approvalLine = `병합 승인: PR${PR} head ${HEAD_A}`;

const recordBytes = async project => (existsSync(approvalsFile(project)) ? readFile(approvalsFile(project)) : null);

async function writeRecordText(project, content) {
  await mkdir(approvalsDirectory(project), { recursive: true });
  await writeFile(approvalsFile(project), content);
}

// Runs the real entry like hook-fixture's runHook, but with CLAUDE_PROJECT_DIR removed so the entry
// fails while resolving the state folder (an unexpected failure in the entry).
function runHookWithoutProjectDirectory({ root, project }, input) {
  const environment = { ...process.env, GIT_CEILING_DIRECTORIES: root };
  delete environment.NODE_TEST_CONTEXT;
  delete environment.CLAUDE_PROJECT_DIR;
  const result = spawnSync(process.execPath, [hookPath], {
    cwd: project, input: JSON.stringify(input), encoding: 'utf8', env: environment, timeout: 30000,
  });
  return { exit: result.status, stdout: result.stdout ?? '', stderr: result.stderr ?? '', error: result.error };
}

// Records whose content breaks the §1 format or cannot be read. Each one would otherwise grant PR 12.
const valid = approval();
const malformedRecords = [
  ['not JSON', 'not json\n'],
  ['empty file', ''],
  ['JSON array', '[]\n'],
  ['version 2', `${JSON.stringify({ version: 2, approvals: [valid] })}\n`],
  ['approvals missing', `${JSON.stringify({ version: 1 })}\n`],
  ['approvals not an array', `${JSON.stringify({ version: 1, approvals: { 0: valid } })}\n`],
  ['pr as a string', `${JSON.stringify({ version: 1, approvals: [{ ...valid, pr: String(PR) }] })}\n`],
  ['head in upper case', `${JSON.stringify({ version: 1, approvals: [{ ...valid, head: HEAD_A.toUpperCase() }] })}\n`],
  ['head of 39 characters', `${JSON.stringify({ version: 1, approvals: [{ ...valid, head: HEAD_A.slice(0, 39) }] })}\n`],
  ['createdAt not ISO 8601', `${JSON.stringify({ version: 1, approvals: [{ ...valid, createdAt: 'yesterday' }] })}\n`],
  ['invalid UTF-8 bytes', Buffer.from([0x7b, 0xff, 0xfe, 0x7d, 0x0a])],
];

test('lead supplement: a malformed or unreadable record counts as no record for PreToolUse (no-approval)', async t => {
  for (const [label, content] of malformedRecords) {
    const fixture = await createProject(t);
    await writeRecordText(fixture.project, content);
    const before = await recordBytes(fixture.project);
    const run = runHook(fixture, bashInput(fixture.project, mergeCommand));
    assertDeny(run, 'no-approval', label);
    assert.deepEqual(await recordBytes(fixture.project), before, runReport(run, `${label}: the record file is left as it was`));
  }

  const fixture = await createProject(t);
  await mkdir(approvalsFile(fixture.project), { recursive: true });
  assertDeny(runHook(fixture, bashInput(fixture.project, mergeCommand)), 'no-approval', 'record path is a folder');
});

test('lead supplement: a malformed record gives PermissionRequest no decision, even with a matching recent use', async t => {
  const used = approval({ usedAgo: 10 * SECOND, usedCommand: mergeCommand });
  const forged = [
    ['version 2', `${JSON.stringify({ version: 2, approvals: [used] })}\n`],
    ['head in upper case', `${JSON.stringify({ version: 1, approvals: [{ ...used, head: HEAD_A.toUpperCase() }] })}\n`],
    ['pr as a string', `${JSON.stringify({ version: 1, approvals: [{ ...used, pr: String(PR) }] })}\n`],
  ];
  for (const [label, content] of forged) {
    const fixture = await createProject(t);
    await writeRecordText(fixture.project, content);
    assertNoDecision(runHook(fixture, permissionInput(fixture.project, mergeCommand)), label);
  }
});

test('lead supplement: an approval line replaces a malformed record with one new record', async t => {
  for (const [label, content] of malformedRecords) {
    const fixture = await createProject(t);
    await writeRecordText(fixture.project, content);
    const start = Date.now();
    const run = runHook(fixture, promptInput(fixture.project, approvalLine));
    assertPromptNotBlocked(run, label);
    const record = await readApprovals(fixture.project);
    assert.equal(record?.version, 1, runReport(run, `${label}: version 1`));
    assert.equal(record.approvals.length, 1, runReport(run, `${label}: exactly one approval`));
    const [entry] = record.approvals;
    assert.deepEqual(Object.keys(entry).sort(), ['createdAt', 'head', 'pr', 'usedAt', 'usedCommand'], `${label}: five fields`);
    assert.equal(entry.pr, PR, `${label}: pr`);
    assert.equal(entry.head, HEAD_A, `${label}: head`);
    assert.ok(Date.parse(entry.createdAt) >= start - SECOND, `${label}: createdAt is new`);
    assert.equal(entry.usedAt, null, `${label}: usedAt null`);
    assert.equal(entry.usedCommand, null, `${label}: usedCommand null`);
    assertAllow(runHook(fixture, bashInput(fixture.project, mergeCommand)), PR, `${label}: the new record grants the merge`);
  }
});

test('§3 and §4: after a used record, a new approval line of the same PR lets the merge pass again', async t => {
  const fixture = await createProject(t);
  await writeApprovals(fixture.project, [approval({ usedAgo: 5 * MINUTE, usedCommand: mergeCommand })]);
  assertDeny(runHook(fixture, bashInput(fixture.project, mergeCommand)), 'already-used', 'before the new approval');
  assertPromptNotBlocked(runHook(fixture, promptInput(fixture.project, approvalLine)), 'new approval line');
  assertAllow(runHook(fixture, bashInput(fixture.project, mergeCommand)), PR, 'after the new approval');
});

test('§4 condition 9: the 30-minute window holds just inside and ends just after', async t => {
  const inside = await createProject(t);
  await writeApprovals(inside.project, [approval({ createdAgo: 30 * MINUTE - 10 * SECOND })]);
  assertAllow(runHook(inside, bashInput(inside.project, mergeCommand)), PR, '29 min 50 s old');

  const after = await createProject(t);
  const written = await writeApprovals(after.project, [approval({ createdAgo: 30 * MINUTE + 10 * SECOND })]);
  const run = runHook(after, bashInput(after.project, mergeCommand));
  assertDeny(run, 'expired', '30 min 10 s old');
  assert.deepEqual(await readApprovals(after.project), written, runReport(run, 'an expired record stays unused'));
});

test('§5: the 120-second window holds just inside and ends just after', async t => {
  const inside = await createProject(t);
  await writeApprovals(inside.project, [approval({ usedAgo: 110 * SECOND, usedCommand: mergeCommand })]);
  assertPermissionAllow(runHook(inside, permissionInput(inside.project, mergeCommand)), 'used 110 s ago');

  const after = await createProject(t);
  await writeApprovals(after.project, [approval({ usedAgo: 130 * SECOND, usedCommand: mergeCommand })]);
  assertNoDecision(runHook(after, permissionInput(after.project, mergeCommand)), 'used 130 s ago');
});

test('lead supplement: a failure inside the entry blocks a merge attempt with invalid-input and nothing else', async t => {
  const fixture = await createProject(t);
  await writeApprovals(fixture.project, [approval({ usedAgo: 10 * SECOND, usedCommand: mergeCommand })]);
  const before = await recordBytes(fixture.project);

  assertDeny(runHookWithoutProjectDirectory(fixture, bashInput(fixture.project, mergeCommand)), 'invalid-input',
    'PreToolUse merge without CLAUDE_PROJECT_DIR');
  assertNoDecision(runHookWithoutProjectDirectory(fixture, permissionInput(fixture.project, mergeCommand)),
    'PermissionRequest without CLAUDE_PROJECT_DIR');
  assertPromptNotBlocked(runHookWithoutProjectDirectory(fixture, promptInput(fixture.project, approvalLine)),
    'UserPromptSubmit without CLAUDE_PROJECT_DIR');
  for (const command of ['git status', 'gh pr view 12', 'ls -la']) {
    assertNoDecision(runHookWithoutProjectDirectory(fixture, bashInput(fixture.project, command)), `${command} without CLAUDE_PROJECT_DIR`);
  }
  assert.deepEqual(await recordBytes(fixture.project), before, 'no record is touched');
});

test('§4 input errors invalid-input: stdin that is not a hook object', async t => {
  const fixture = await createProject(t);
  for (const [label, raw] of [['invalid UTF-8', Buffer.from([0x7b, 0xff, 0xfe, 0x7d])], ['null', 'null'], ['array', '[]']]) {
    assertDeny(runHook(fixture, null, { raw }), 'invalid-input', label);
  }
});

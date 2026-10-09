// Independent regressions on the two real cases that motivated the candidate check. The inputs are
// verbatim `git show` copies recorded in fixtures/observed-sources.json, plus one reconstruction
// marked as such. Expected outcomes come from the Rules lead's test contract v1 (task_4e23d12d8aee,
// F1 and F2) and the records it cites, not from the module:
// - F1, 65 versus 68: BACKLOG at 2227161168446d117a853b2732464f8cd8401bb7 has 68 data rows and no
//   duplicate (hook goal record, line 400 of 01_Phases/goals/2026-10-07-hook-friction-helper-session/goal.md),
//   including the plain IDs on lines 134-136; the 「항목 계약」 field table gives no `ID` ID;
// - F2a, the merge gate goal's two missing rows: the real 「다음 계획 후보」 section at 5caa5dfe has four
//   candidates (source lines 590, 591, 592, 596) without a reference, no citation, against a BACKLOG of
//   44 data rows;
// - F2b, reconstruction: the same two items written as cited candidates are unknown IDs (lines 5, 6)
//   against that BACKLOG and allowed against 338effb0's BACKLOG of 48 data rows (merge gate goal line 552).
import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { readFile, readdir } from 'node:fs/promises';
import { join } from 'node:path';
import { test } from 'node:test';

import {
  assertDiagnostics, assertMessagesName, assertResult, fixturesDirectory, loadPolicy, parseCliResult, runCli,
  slashPath,
} from './candidate-fixture.mjs';

const sources = JSON.parse(await readFile(join(fixturesDirectory, 'observed-sources.json'), 'utf8'));
const fixturePath = name => join(fixturesDirectory, name);
const fixtureText = name => readFile(fixturePath(name), 'utf8');
const everyGoalExists = () => true;

const PLAIN_ID_BACKLOG = 'backlog-with-plain-ids.md';
const BACKLOG_BEFORE = 'backlog-before-merge-gate-rows.md';
const BACKLOG_AFTER = 'backlog-after-merge-gate-rows.md';
const GOAL_CANDIDATES = 'merge-gate-goal-candidates.md';
const GOAL_CITED = 'merge-gate-goal-cited-rows.md';

// The copy line of a source line in an excerpt, from the source ranges recorded for that copy.
function copyLine(name, sourceLine) {
  let copied = 0;
  for (const { from, to } of sources.files[name].sourceLines) {
    if (sourceLine >= from && sourceLine <= to) return copied + sourceLine - from + 1;
    copied += to - from + 1;
  }
  throw new Error(`${name} does not hold source line ${sourceLine}`);
}

test('every fixture is listed with its source, and its bytes match the recorded hash', async () => {
  const onDisk = (await readdir(fixturesDirectory)).filter(name => name !== 'observed-sources.json').sort();
  assert.deepEqual(onDisk, Object.keys(sources.files).sort(), 'fixtures and source records must match');
  for (const [name, record] of Object.entries(sources.files)) {
    const bytes = await readFile(fixturePath(name));
    assert.equal(createHash('sha256').update(bytes).digest('hex'), record.copySha256, `${name}: copy hash`);
    assert.equal(record.real, !record.reconstructed, `${name}: real or reconstructed, never both`);
    if (record.reconstructed) {
      assert.equal(record.commit, null, `${name}: a reconstruction names no source commit`);
      continue;
    }
    assert.match(record.commit, /^[0-9a-f]{40}$/, `${name}: 40-character source commit`);
    assert.match(record.sourceSha256, /^[0-9a-f]{64}$/, `${name}: source hash`);
    if (record.sourceLines === 'all') {
      assert.equal(record.copySha256, record.sourceSha256, `${name}: a whole copy has the source bytes`);
    } else {
      const copiedLines = record.sourceLines.reduce((sum, { from, to }) => sum + to - from + 1, 0);
      assert.equal(bytes.toString('utf8').split('\n').length - 1, copiedLines, `${name}: excerpt line count`);
    }
  }
});

test('F1: all 68 rows are extracted, including the three IDs without backticks', async () => {
  const { extractBacklogIds } = await loadPolicy();
  const entries = extractBacklogIds(await fixtureText(PLAIN_ID_BACKLOG));
  assert.equal(entries.length, 68);
  for (const [id, line] of [['management-launcher-real-run', 134], ['remote-play-check', 135],
    ['system-map-3d', 136]]) {
    assert.ok(entries.some(entry => entry.id === id && entry.line === line), `${id} on line ${line}`);
  }
  assert.ok(!entries.some(entry => entry.id === 'ID'), 'the field table row `ID` is not an ID');
  assert.ok(!entries.some(entry => entry.id.includes('`')), 'IDs are given without their backticks');
});

test('F1: the BACKLOG alone is allowed with 68 rows and no duplicate, in the module and the CLI', async () => {
  const { checkCandidates } = await loadPolicy();
  const expectedCounts = {
    backlogIds: 68, duplicateIds: 0, candidates: null, candidatesWithoutReference: null, unknownIds: null,
  };
  const backlogPath = sources.files[PLAIN_ID_BACKLOG].path;
  const result = checkCandidates({ backlogText: await fixtureText(PLAIN_ID_BACKLOG), backlogPath });
  assertResult(result, 'allowed', 'F1 module');
  assert.deepEqual(result.counts, expectedCounts, 'F1 module');

  const run = runCli(['--backlog', fixturePath(PLAIN_ID_BACKLOG)]);
  const cliResult = parseCliResult(run, 'F1 CLI');
  assertResult(cliResult, 'allowed', 'F1 CLI');
  assert.equal(run.exit, 0);
  assert.deepEqual(cliResult.counts, expectedCounts, 'F1 CLI');
});

test('F2a: the four real candidates without a BACKLOG ID or goal link are each reported', async () => {
  const { checkCandidates } = await loadPolicy();
  const missingLines = [590, 591, 592, 596].map(line => copyLine(GOAL_CANDIDATES, line));
  const expectedCounts = {
    backlogIds: 44, duplicateIds: 0, candidates: 4, candidatesWithoutReference: 4, unknownIds: 0,
  };

  const goalPath = sources.files[GOAL_CANDIDATES].path;
  const result = checkCandidates({
    backlogText: await fixtureText(BACKLOG_BEFORE),
    backlogPath: sources.files[BACKLOG_BEFORE].path,
    goalText: await fixtureText(GOAL_CANDIDATES),
    goalPath,
    isExistingGoal: everyGoalExists,
  });
  assertResult(result, 'policy-violation', 'F2a module');
  assertDiagnostics(result, missingLines.map(line => ['missing-reference', goalPath, line]), 'F2a module');
  assert.deepEqual(result.counts, expectedCounts, 'F2a module');

  const cliGoal = fixturePath(GOAL_CANDIDATES);
  const run = runCli(['--backlog', fixturePath(BACKLOG_BEFORE), '--goal', cliGoal]);
  const cliResult = parseCliResult(run, 'F2a CLI');
  assertResult(cliResult, 'policy-violation', 'F2a CLI');
  assert.equal(run.exit, 1);
  assertDiagnostics(cliResult, missingLines.map(line => ['missing-reference', slashPath(cliGoal), line]), 'F2a CLI');
  assert.deepEqual(cliResult.counts, expectedCounts, 'F2a CLI');
});

test('F2b (reconstruction): the cited missing rows are unknown before the BACKLOG addition', async () => {
  const { checkCandidates } = await loadPolicy();
  const goalPath = slashPath(fixturePath(GOAL_CITED));
  const unknown = [[5, 'report-folder-unification'], [6, 'management-feature-map-entry']];
  const expectedCounts = {
    backlogIds: 44, duplicateIds: 0, candidates: 2, candidatesWithoutReference: 0, unknownIds: 2,
  };

  const result = checkCandidates({
    backlogText: await fixtureText(BACKLOG_BEFORE),
    backlogPath: sources.files[BACKLOG_BEFORE].path,
    goalText: await fixtureText(GOAL_CITED),
    goalPath,
    isExistingGoal: everyGoalExists,
  });
  assertResult(result, 'policy-violation', 'F2b before module');
  assertDiagnostics(result, unknown.map(([line]) => ['unknown-backlog-id', goalPath, line]), 'F2b before module');
  assertMessagesName(result, 'unknown-backlog-id', unknown, 'F2b before module');
  assert.deepEqual(result.counts, expectedCounts, 'F2b before module');

  const run = runCli(['--backlog', fixturePath(BACKLOG_BEFORE), '--goal', fixturePath(GOAL_CITED)]);
  const cliResult = parseCliResult(run, 'F2b before CLI');
  assertResult(cliResult, 'policy-violation', 'F2b before CLI');
  assert.equal(run.exit, 1);
  assertDiagnostics(cliResult, unknown.map(([line]) => ['unknown-backlog-id', goalPath, line]), 'F2b before CLI');
  assertMessagesName(cliResult, 'unknown-backlog-id', unknown, 'F2b before CLI');
});

test('F2b (reconstruction): the same candidates arrive in the BACKLOG of 48 rows after the addition', async () => {
  const { checkCandidates } = await loadPolicy();
  const expectedCounts = {
    backlogIds: 48, duplicateIds: 0, candidates: 2, candidatesWithoutReference: 0, unknownIds: 0,
  };
  const result = checkCandidates({
    backlogText: await fixtureText(BACKLOG_AFTER),
    backlogPath: sources.files[BACKLOG_AFTER].path,
    goalText: await fixtureText(GOAL_CITED),
    goalPath: slashPath(fixturePath(GOAL_CITED)),
    isExistingGoal: everyGoalExists,
  });
  assertResult(result, 'allowed', 'F2b after module');
  assert.deepEqual(result.counts, expectedCounts, 'F2b after module');

  const run = runCli(['--backlog', fixturePath(BACKLOG_AFTER), '--goal', fixturePath(GOAL_CITED)]);
  const cliResult = parseCliResult(run, 'F2b after CLI');
  assertResult(cliResult, 'allowed', 'F2b after CLI');
  assert.equal(run.exit, 0);
  assert.deepEqual(cliResult.counts, expectedCounts, 'F2b after CLI');
});

// Independent regressions for 99_Tools/Backlog/check-candidates.mjs, run as a black box. Expected
// outcomes come from the Rules lead's test contract v1 (task_4e23d12d8aee), not from the CLI:
// - `--backlog <path> [--goal <path>]`, one JSON value on stdout and the process exit equal to its
//   exitCode (interface and result contract);
// - goal links count only for a goal file that exists, resolved from the goal's folder (R5);
// - usage and unreadable-file input errors are still JSON (R7), with the path / line values of the
//   lead's answer msg_f79fa2b5abb2 (usage: null / null; unreadable-file: the path with `/`, line null;
//   every reported path has `\` turned into `/`);
// - the CLI creates or changes no file (R8).
// Inputs are synthetic files written to a throwaway folder; the real cases are in observed-cases.test.mjs.
import assert from 'node:assert/strict';
import { mkdir, readFile, readdir } from 'node:fs/promises';
import { join } from 'node:path';
import { test } from 'node:test';

import {
  assertDiagnostics, assertResult, createWork, lines, parseCliResult, runCli, slashPath, withCrlf, writeText,
} from './candidate-fixture.mjs';

// Known IDs: alpha (backticked) and beta (plain).
const backlogText = lines(
  '# 목표 전 후보',
  '',
  '| ID | 제목 | 상태 |',
  '|---|---|---|',
  '| `alpha` | 백틱 후보 | 대기 |',
  '| beta | 백틱 없는 후보 | 대기 |',
);
const goalWith = (...candidateLines) => lines('# 표본 goal', '', '## 다음 계획 후보', '', ...candidateLines);

async function writeInputs(work) {
  return {
    backlog: await writeText(work, 'operations/BACKLOG.md', backlogText),
    allowedGoal: await writeText(work, 'goals/allowed-goal/goal.md', goalWith('- BACKLOG `alpha`·`beta`')),
    violatingGoal: await writeText(work, 'goals/violating-goal/goal.md', goalWith('- 참조 없는 후보')), // 5
    sectionlessGoal: await writeText(work, 'goals/sectionless-goal/goal.md', lines('# 표본 goal', '', '## 범위')),
  };
}

test('the CLI prints one JSON result and exits with its exitCode for allowed, violation and input error', async t => {
  const work = await createWork(t, 'cli-results');
  const files = await writeInputs(work);

  const backlogOnly = parseCliResult(runCli(['--backlog', files.backlog], { cwd: work }), 'BACKLOG only');
  assertResult(backlogOnly, 'allowed', 'BACKLOG only');
  assert.equal(backlogOnly.backlog, slashPath(files.backlog));
  assert.equal(backlogOnly.goal, null);
  assert.deepEqual(backlogOnly.counts, {
    backlogIds: 2, duplicateIds: 0, candidates: null, candidatesWithoutReference: null, unknownIds: null,
  });

  const allowed = parseCliResult(runCli(['--backlog', files.backlog, '--goal', files.allowedGoal], { cwd: work }),
    'allowed goal');
  assertResult(allowed, 'allowed', 'allowed goal');
  assert.equal(allowed.goal, slashPath(files.allowedGoal));
  assert.deepEqual(allowed.counts, {
    backlogIds: 2, duplicateIds: 0, candidates: 1, candidatesWithoutReference: 0, unknownIds: 0,
  });

  // Option order is free.
  const violation = parseCliResult(runCli(['--goal', files.violatingGoal, '--backlog', files.backlog], { cwd: work }),
    'violating goal');
  assertResult(violation, 'policy-violation', 'violating goal');
  assertDiagnostics(violation, [['missing-reference', slashPath(files.violatingGoal), 5]], 'violating goal');

  const inputError = parseCliResult(runCli(['--backlog', files.backlog, '--goal', files.sectionlessGoal],
    { cwd: work }), 'sectionless goal');
  assertResult(inputError, 'input-error', 'sectionless goal');
  assertDiagnostics(inputError, [['missing-section', slashPath(files.sectionlessGoal), null]], 'sectionless goal');
  assert.equal(inputError.backlog, slashPath(files.backlog));
  assert.equal(inputError.goal, slashPath(files.sectionlessGoal));
});

test('the CLI reads CRLF files with the same lines as LF', async t => {
  const work = await createWork(t, 'cli-crlf');
  const backlog = await writeText(work, 'BACKLOG.md', withCrlf(backlogText));
  const goal = await writeText(work, 'goals/crlf-goal/goal.md', withCrlf(goalWith(
    '- 알려진 ID: BACKLOG `beta`', // 5
    '- 없는 ID: BACKLOG `ghost`', // 6
    '- 참조 없는 후보', // 7
  )));
  const result = parseCliResult(runCli(['--backlog', backlog, '--goal', goal], { cwd: work }), 'CRLF');
  assertResult(result, 'policy-violation', 'CRLF');
  assertDiagnostics(result, [
    ['unknown-backlog-id', slashPath(goal), 6],
    ['missing-reference', slashPath(goal), 7],
  ], 'CRLF');
});

test('a goal link counts only when that goal file exists, resolved from the goal folder', async t => {
  const work = await createWork(t, 'cli-links');
  const backlog = await writeText(work, 'operations/BACKLOG.md', backlogText);
  await writeText(work, 'goals/planned-goal/goal.md', lines('# 예정 goal'));
  // A decoy beside the working directory: line 8 would find it if links were resolved from cwd.
  await writeText(work, 'planned-goal/goal.md', lines('# 작업 폴더 기준 미끼'));
  await mkdir(join(work, 'goals', 'empty-goal'));
  const goal = await writeText(work, 'goals/sample-goal/goal.md', goalWith(
    '- 있는 goal: [예정 goal](../planned-goal/goal.md#착수)', // 5
    '- 없는 goal: [사라진 goal](../absent-goal/goal.md)', // 6
    '- goal.md가 없는 폴더: [빈 폴더](../empty-goal/goal.md)', // 7
    '- 같은 이름의 다른 위치: [다른 위치](planned-goal/goal.md)', // 8
  ));
  const result = parseCliResult(runCli(['--backlog', backlog, '--goal', goal], { cwd: work }), 'links');
  assertResult(result, 'policy-violation', 'links');
  assertDiagnostics(result, [6, 7, 8].map(line => ['missing-reference', slashPath(goal), line]), 'links');
  assert.equal(result.counts.candidates, 4);
  assert.equal(result.counts.candidatesWithoutReference, 3);
});

test('argument errors are usage input errors reported as JSON with no path or line', async t => {
  const work = await createWork(t, 'cli-usage');
  const files = await writeInputs(work);
  const cases = [
    ['no argument', []],
    ['goal without backlog', ['--goal', files.allowedGoal]],
    ['unknown option', ['--backlog', files.backlog, '--json']],
    ['backlog without a value', ['--backlog']],
    ['goal without a value', ['--backlog', files.backlog, '--goal']],
    ['backlog twice', ['--backlog', files.backlog, '--backlog', files.backlog]],
    ['goal twice', ['--backlog', files.backlog, '--goal', files.allowedGoal, '--goal', files.allowedGoal]],
  ];
  for (const [label, args] of cases) {
    const result = parseCliResult(runCli(args, { cwd: work }), label);
    assertResult(result, 'input-error', label);
    assertDiagnostics(result, [['usage', null, null]], label);
  }
});

test('a file that cannot be read is an input error naming that file', async t => {
  const work = await createWork(t, 'cli-unreadable');
  const files = await writeInputs(work);
  const absentBacklog = join(work, 'operations', 'absent-BACKLOG.md');
  const absentGoal = join(work, 'goals', 'absent-goal', 'goal.md');
  const folder = join(work, 'goals');
  const cases = [
    ['absent BACKLOG', ['--backlog', absentBacklog], absentBacklog],
    ['folder as BACKLOG', ['--backlog', folder], folder],
    ['absent goal', ['--backlog', files.backlog, '--goal', absentGoal], absentGoal],
    ['folder as goal', ['--backlog', files.backlog, '--goal', folder], folder],
  ];
  for (const [label, args, unreadable] of cases) {
    const result = parseCliResult(runCli(args, { cwd: work }), label);
    assertResult(result, 'input-error', label);
    assertDiagnostics(result, [['unreadable-file', slashPath(unreadable), null]], label);
  }
});

// Node's permission model denies file writes and child processes unless granted.
function permissionFlag() {
  if (process.allowedNodeEnvironmentFlags.has('--permission')) return '--permission';
  if (process.allowedNodeEnvironmentFlags.has('--experimental-permission')) return '--experimental-permission';
  throw new Error(`Node ${process.version} has no permission model to prove the CLI is read-only`);
}

test('the CLI only reads: same results with writes and child processes denied, inputs and cwd unchanged', async t => {
  const work = await createWork(t, 'cli-read-only');
  const files = await writeInputs(work);
  const cwd = join(work, 'read-only-cwd');
  await mkdir(cwd);
  const nodeArgs = [permissionFlag(), '--allow-fs-read=*'];
  const inputs = Object.values(files);
  const before = await Promise.all(inputs.map(path => readFile(path)));
  const cases = [
    ['allowed', ['--backlog', files.backlog, '--goal', files.allowedGoal], 'allowed'],
    ['violation', ['--backlog', files.backlog, '--goal', files.violatingGoal], 'policy-violation'],
    ['unreadable', ['--backlog', join(work, 'absent.md')], 'input-error'],
  ];
  for (const [label, args, status] of cases) {
    assertResult(parseCliResult(runCli(args, { cwd, nodeArgs }), label), status, label);
  }
  assert.deepEqual(await readdir(cwd), [], 'the CLI must not create files in its working directory');
  const after = await Promise.all(inputs.map(path => readFile(path)));
  inputs.forEach((path, index) => assert.ok(before[index].equals(after[index]), `${path} must stay unchanged`));
});

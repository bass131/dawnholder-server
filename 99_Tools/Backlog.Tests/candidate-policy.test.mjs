// Independent regressions for 99_Tools/Backlog/candidate-policy.mjs (the pure module). Expected
// outcomes come from the approved rules, not from the module:
// - the candidate rule: goal-loop SKILL 「기준과 상태」 (a candidate leaves a BACKLOG stable ID or an
//   existing goal link) and the approved scope draft v1 「PR2 후보 도착 검사」 (ID tables are the
//   tables whose header cell is ID, backticks or not; `BACKLOG` followed by a backtick ID);
// - the interface, R1~R6, R8 and the result contract (status/exit 0·1·2, counts, code·path·line
//   diagnostics with message and repair): the Rules lead's test contract v1 (task_4e23d12d8aee);
// - the values the contract left open, answered by the lead in msg_f79fa2b5abb2: file-level
//   diagnostics have line null, duplicate-section points at the second heading, an empty ID row is
//   extracted as '' and counted but never a duplicate.
// Every input here is synthetic and written for one rule; the real cases are in observed-cases.test.mjs.
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { dirname, join } from 'node:path';
import { test } from 'node:test';

import {
  assertDiagnostics, assertMessagesName, assertResult, fixturesDirectory, lines, loadPolicy, policyPath,
  withCrlf,
} from './candidate-fixture.mjs';

const BACKLOG = '00_Document/operations/BACKLOG.md';
const GOAL = '01_Phases/goals/sample-goal/goal.md';
const everyGoalExists = () => true;
const noGoalExists = () => false;

// Synthetic BACKLOG: a field table like 「항목 계약」 (its `ID` row is data, not a header) and two ID
// tables, one with backticked and plain IDs, one with spaces around the cells. Known IDs: alpha, beta, gamma.
const backlogText = lines(
  '# 목표 전 후보', // 1
  '',
  '| 필드 | 기록 기준 |', // 3
  '|---|---|',
  '| ID | 목적을 나타내는 문자열 |', // 5
  '',
  '| ID | 제목 | 상태 |', // 7
  '|---|---|---|',
  '| `alpha` | 백틱 후보 | 대기 |', // 9
  '| beta | 백틱 없는 후보 | 대기 |', // 10
  '',
  '## 보류',
  '',
  '|  ID  | 제목 |', // 14
  '| :--- | --- |',
  '|  `gamma`  | 둘레 공백 |', // 16
);

const goalWith = (...candidateLines) => lines('# 표본 goal', '', '## 다음 계획 후보', '', ...candidateLines);

test('IDs come from every table headed ID, with or without backticks, in file order with 1-based lines', async () => {
  const { extractBacklogIds } = await loadPolicy();
  assert.deepEqual(extractBacklogIds(backlogText), [
    { id: 'alpha', line: 9 },
    { id: 'beta', line: 10 },
    { id: 'gamma', line: 16 },
  ]);
});

test('only a first header cell of exactly ID followed by a separator row makes an ID table', async () => {
  const { extractBacklogIds } = await loadPolicy();
  const text = lines(
    '| id | 제목 |', // 1: lower case
    '|---|---|',
    '| lower-case-header | x |',
    '',
    '| IDs | 제목 |', // 5: another word
    '|---|---|',
    '| plural-header | x |',
    '',
    '| ID | 제목 |', // 9: no separator row follows
    '| no-separator | x |',
    '',
    '| 제목 | ID |', // 12: ID is not the first cell
    '|---|---|',
    '| second-column | x |',
    '',
    '| ID | 제목 |', // 16
    '|---|---|',
    '| kept | x |', // 18
  );
  assert.deepEqual(extractBacklogIds(text), [{ id: 'kept', line: 18 }]);
});

test('CRLF text gives the same IDs and lines as LF', async () => {
  const { extractBacklogIds } = await loadPolicy();
  assert.deepEqual(extractBacklogIds(withCrlf(backlogText)), extractBacklogIds(backlogText));
  assert.deepEqual(extractBacklogIds(withCrlf(backlogText)).map(entry => entry.id), ['alpha', 'beta', 'gamma']);
});

test('extraction keeps duplicates in order and an empty first cell as the empty ID', async () => {
  const { extractBacklogIds } = await loadPolicy();
  const text = lines(
    '| ID | 제목 |',
    '|---|---|',
    '| `alpha` | x |', // 3
    '| alpha | x |', // 4
    '|  | 빈 칸 |', // 5
    '| beta | x |', // 6
  );
  assert.deepEqual(extractBacklogIds(text), [
    { id: 'alpha', line: 3 },
    { id: 'alpha', line: 4 },
    { id: '', line: 5 },
    { id: 'beta', line: 6 },
  ]);
});

test('a BACKLOG alone is allowed with its row count and no candidate counts', async () => {
  const { checkCandidates } = await loadPolicy();
  const result = checkCandidates({ backlogText, backlogPath: BACKLOG });
  assertResult(result, 'allowed', 'BACKLOG only');
  assert.equal(result.backlog, BACKLOG);
  assert.equal(result.goal, null);
  assert.deepEqual(result.counts, {
    backlogIds: 3, duplicateIds: 0, candidates: null, candidatesWithoutReference: null, unknownIds: null,
  });
});

test('every second and later row of the same ID is a duplicate, across tables and backtick forms', async () => {
  const { checkCandidates } = await loadPolicy();
  const text = lines(
    '| ID | 제목 |',
    '|---|---|',
    '| alpha | x |', // 3
    '| `alpha` | x |', // 4
    '| beta | x |', // 5
    '',
    '| ID | 제목 |',
    '|---|---|',
    '| beta | x |', // 9
    '| `beta` | x |', // 10
    '| gamma | x |', // 11
  );
  const result = checkCandidates({ backlogText: text, backlogPath: BACKLOG });
  assertResult(result, 'policy-violation', 'duplicates');
  assertDiagnostics(result, [
    ['duplicate-backlog-id', BACKLOG, 4],
    ['duplicate-backlog-id', BACKLOG, 9],
    ['duplicate-backlog-id', BACKLOG, 10],
  ], 'duplicates');
  assertMessagesName(result, 'duplicate-backlog-id', [[4, 'alpha'], [9, 'beta'], [10, 'beta']], 'duplicates');
  assert.equal(result.counts.backlogIds, 6);
  assert.equal(result.counts.duplicateIds, 2);
});

test('each empty ID row is reported on its line and empty rows are never duplicates', async () => {
  const { checkCandidates } = await loadPolicy();
  const text = lines(
    '| ID | 제목 |',
    '|---|---|',
    '|  | 첫 빈 칸 |', // 3
    '| alpha | x |',
    '|   | 둘째 빈 칸 |', // 5
  );
  const result = checkCandidates({ backlogText: text, backlogPath: BACKLOG });
  assertResult(result, 'policy-violation', 'empty IDs');
  assertDiagnostics(result, [['empty-backlog-id', BACKLOG, 3], ['empty-backlog-id', BACKLOG, 5]], 'empty IDs');
  assert.equal(result.counts.backlogIds, 3);
  assert.equal(result.counts.duplicateIds, 0);
});

test('a BACKLOG without an ID table is an input error without counts, with or without a goal', async () => {
  const { checkCandidates } = await loadPolicy();
  const fieldTableOnly = lines(
    '| 필드 | 기록 기준 |',
    '|---|---|',
    '| ID | 목적을 나타내는 문자열 |',
    '| 제목 | 이름 |',
  );
  const goalText = goalWith('- 참조 없는 후보', '- 없는 ID: BACKLOG `ghost`');
  const inputs = [
    { backlogText: fieldTableOnly, backlogPath: BACKLOG },
    { backlogText: fieldTableOnly, backlogPath: BACKLOG, goalText, goalPath: GOAL, isExistingGoal: everyGoalExists },
  ];
  for (const input of inputs) {
    const result = checkCandidates(input);
    assertResult(result, 'input-error', 'no ID table');
    assertDiagnostics(result, [['no-id-table', BACKLOG, null]], 'no ID table');
  }
});

test('a goal without a line exactly `## 다음 계획 후보` is an input error', async () => {
  const { checkCandidates } = await loadPolicy();
  const goalText = lines(
    '# 표본 goal',
    '',
    '## 다음 계획 후보들',
    '',
    '- BACKLOG `alpha`',
    '',
    '### 다음 계획 후보',
    '',
    '- BACKLOG `beta`',
    '',
    '## 다음 계획 후보 (초안)',
    '',
    '- BACKLOG `ghost`',
  );
  const result = checkCandidates({
    backlogText, backlogPath: BACKLOG, goalText, goalPath: GOAL, isExistingGoal: everyGoalExists,
  });
  assertResult(result, 'input-error', 'missing section');
  assertDiagnostics(result, [['missing-section', GOAL, null]], 'missing section');
  assert.equal(result.goal, GOAL);
});

test('trailing spaces and CR after the section heading are ignored; CRLF files give the same lines', async () => {
  const { checkCandidates } = await loadPolicy();
  const goalText = lines(
    '# 표본 goal',
    '',
    '## 다음 계획 후보   ', // 3
    '',
    '- 참조 없는 후보', // 5
    '- 알려진 ID: BACKLOG `alpha`', // 6
  );
  for (const [label, goal, backlog] of [['LF', goalText, backlogText],
    ['CRLF', withCrlf(goalText), withCrlf(backlogText)]]) {
    const result = checkCandidates({
      backlogText: backlog, backlogPath: BACKLOG, goalText: goal, goalPath: GOAL, isExistingGoal: everyGoalExists,
    });
    assertResult(result, 'policy-violation', label);
    assertDiagnostics(result, [['missing-reference', GOAL, 5]], label);
    assert.deepEqual(result.counts, {
      backlogIds: 3, duplicateIds: 0, candidates: 2, candidatesWithoutReference: 1, unknownIds: 0,
    }, label);
  }
});

test('two candidate sections are an input error at the second heading, with no candidate diagnostics', async () => {
  const { checkCandidates } = await loadPolicy();
  const goalText = lines(
    '# 표본 goal',
    '',
    '## 다음 계획 후보',
    '',
    '- 참조 없는 후보',
    '',
    '## 다음 계획 후보', // 7
    '',
    '- 없는 ID: BACKLOG `ghost`',
  );
  const result = checkCandidates({
    backlogText, backlogPath: BACKLOG, goalText, goalPath: GOAL, isExistingGoal: everyGoalExists,
  });
  assertResult(result, 'input-error', 'duplicate section');
  assertDiagnostics(result, [['duplicate-section', GOAL, 7]], 'duplicate section');
});

test('an input error hides the policy diagnostics of the other file', async () => {
  const { checkCandidates } = await loadPolicy();
  const duplicatedBacklog = lines('| ID | 제목 |', '|---|---|', '| alpha | x |', '| alpha | x |');
  const result = checkCandidates({
    backlogText: duplicatedBacklog,
    backlogPath: BACKLOG,
    goalText: lines('# 표본 goal', '', '## 범위', '', '- BACKLOG `ghost`'),
    goalPath: GOAL,
    isExistingGoal: everyGoalExists,
  });
  assertResult(result, 'input-error', 'missing section with a duplicate');
  assertDiagnostics(result, [['missing-section', GOAL, null]], 'missing section with a duplicate');
});

test('candidates are column-0 list lines of the section, which ends at the next # or ## heading', async () => {
  const { checkCandidates } = await loadPolicy();
  for (const ending of ['## 다음 절', '# 새 큰 제목']) {
    const goalText = lines(
      '# 표본 goal',
      '',
      '## 범위',
      '',
      '- 절 앞 목록: BACKLOG `before-section`', // 5
      '',
      '## 다음 계획 후보', // 7
      '',
      '절 소개 문단은 후보가 아니다: BACKLOG `intro-paragraph`.', // 9
      '',
      '- 대시 후보: BACKLOG `alpha`', // 11
      '* 별표 후보: BACKLOG `beta`', // 12
      '+ 더하기 후보: BACKLOG `gamma`', // 13
      '1. 번호 후보: BACKLOG `alpha`', // 14
      '2. 둘째 번호 후보', // 15
      '  - 하위 목록은 둘째 번호 후보에 속한다: BACKLOG `beta`', // 16
      '',
      '### 하위 제목은 절을 끝내지 않는다', // 18
      '',
      '- 하위 제목 뒤 후보', // 20
      '',
      ending, // 22
      '',
      '- 절 뒤 목록: BACKLOG `after-section`', // 24
    );
    const result = checkCandidates({
      backlogText, backlogPath: BACKLOG, goalText, goalPath: GOAL, isExistingGoal: everyGoalExists,
    });
    assertResult(result, 'policy-violation', ending);
    assertDiagnostics(result, [['missing-reference', GOAL, 20]], ending);
    assert.deepEqual(result.counts, {
      backlogIds: 3, duplicateIds: 0, candidates: 6, candidatesWithoutReference: 1, unknownIds: 0,
    }, ending);
  }
});

test('indented and blank lines belong to a candidate; a paragraph, heading or <a> line ends it', async () => {
  const { checkCandidates } = await loadPolicy();
  const goalText = goalWith(
    '- 첫 후보: 참조는 들여쓴 줄에 있다', // 5
    '',
    '  이어지는 들여쓴 문단: BACKLOG `alpha`', // 7
    '- 둘째 후보: 참조 없음', // 8
    '<a id="after-second"></a>', // 9
    '  앵커 뒤 들여쓴 줄: BACKLOG `alpha`', // 10
    '- 셋째 후보: 참조 없음', // 11
    '문단 줄이 셋째 후보를 끝낸다.', // 12
    '  문단 뒤 들여쓴 줄: BACKLOG `beta`', // 13
    '- 넷째 후보: 참조 없음', // 14
    '### 하위 제목이 넷째 후보를 끝낸다', // 15
    '  제목 뒤 들여쓴 줄: BACKLOG `gamma`', // 16
    '**참고**: 굵은 글씨 문단은 목록이 아니다. BACKLOG `ghost`', // 17
  );
  const result = checkCandidates({
    backlogText, backlogPath: BACKLOG, goalText, goalPath: GOAL, isExistingGoal: everyGoalExists,
  });
  assertResult(result, 'policy-violation', 'candidate span');
  assertDiagnostics(result, [
    ['missing-reference', GOAL, 8],
    ['missing-reference', GOAL, 11],
    ['missing-reference', GOAL, 14],
  ], 'candidate span');
  assert.deepEqual(result.counts, {
    backlogIds: 3, duplicateIds: 0, candidates: 4, candidatesWithoutReference: 3, unknownIds: 0,
  });
});

test('BACKLOG then backtick spans chained by ·, comma or / (spaces allowed) are each cited', async () => {
  const { checkCandidates } = await loadPolicy();
  const goalText = goalWith(
    '- 이어진 인용: BACKLOG `one`·`two`, `three` / `four`', // 5
    '- 둘레 공백: BACKLOG  `five` · `six` ,`seven`/ `eight`', // 6
    '- 괄호 안 낱말: (BACKLOG `nine`)', // 7
    '- 끊긴 사슬: BACKLOG `alpha` 및 `not-cited-one`, `not-cited-two`', // 8
  );
  const result = checkCandidates({
    backlogText, backlogPath: BACKLOG, goalText, goalPath: GOAL, isExistingGoal: everyGoalExists,
  });
  assertResult(result, 'policy-violation', 'citation chains');
  const unknown = [[5, 'one'], [5, 'two'], [5, 'three'], [5, 'four'], [6, 'five'], [6, 'six'], [6, 'seven'],
    [6, 'eight'], [7, 'nine']];
  assertDiagnostics(result, unknown.map(([line]) => ['unknown-backlog-id', GOAL, line]), 'citation chains');
  assertMessagesName(result, 'unknown-backlog-id', unknown, 'citation chains');
  assert.deepEqual(result.counts, {
    backlogIds: 3, duplicateIds: 0, candidates: 4, candidatesWithoutReference: 0, unknownIds: 9,
  });
});

test('a backtick span that is not right after a free-standing BACKLOG is no citation, even of a real ID', async () => {
  const { checkCandidates } = await loadPolicy();
  const goalText = goalWith(
    '- 다른 자리의 백틱: `alpha`는 BACKLOG에 있다', // 5
    '- 조사가 붙음: BACKLOG는 `alpha`를 쓴다', // 6
    '- 앞에 영문자: XBACKLOG `alpha`', // 7
    '- 뒤에 영문자: BACKLOGS `alpha`', // 8
    '- 앞에 숫자: 2BACKLOG `alpha`', // 9
    '- 링크 글자: [BACKLOG](../../../00_Document/operations/BACKLOG.md) `alpha`', // 10
  );
  const result = checkCandidates({
    backlogText, backlogPath: BACKLOG, goalText, goalPath: GOAL, isExistingGoal: everyGoalExists,
  });
  assertResult(result, 'policy-violation', 'not citations');
  assertDiagnostics(result, [5, 6, 7, 8, 9, 10].map(line => ['missing-reference', GOAL, line]), 'not citations');
  assert.equal(result.counts.unknownIds, 0);
});

test('each citation of an ID missing from BACKLOG is reported on its own line and replaces missing-reference', async () => {
  const { checkCandidates } = await loadPolicy();
  const goalText = goalWith(
    '- 첫 후보: BACKLOG `ghost`', // 5
    '  - 다시 인용: BACKLOG `ghost`·`alpha`', // 6
    '- 둘째 후보: BACKLOG `phantom`', // 7
  );
  const result = checkCandidates({
    backlogText, backlogPath: BACKLOG, goalText, goalPath: GOAL, isExistingGoal: noGoalExists,
  });
  assertResult(result, 'policy-violation', 'unknown IDs');
  assertDiagnostics(result, [
    ['unknown-backlog-id', GOAL, 5],
    ['unknown-backlog-id', GOAL, 6],
    ['unknown-backlog-id', GOAL, 7],
  ], 'unknown IDs');
  assertMessagesName(result, 'unknown-backlog-id', [[5, 'ghost'], [6, 'ghost'], [7, 'phantom']], 'unknown IDs');
  assert.deepEqual(result.counts, {
    backlogIds: 3, duplicateIds: 0, candidates: 2, candidatesWithoutReference: 0, unknownIds: 3,
  });
});

test('an existing goal link counts; isExistingGoal gets the link resolved from the goal folder with /', async () => {
  const { checkCandidates } = await loadPolicy();
  const existing = new Set([
    '01_Phases/goals/planned-goal/goal.md',
    '01_Phases/goals/other-goal/goal.md',
    '01_Phases/goals/third-goal/goal.md',
    '01_Phases/goals/fourth-goal/goal.md',
  ]);
  const asked = [];
  const isExistingGoal = path => {
    asked.push(path);
    return existing.has(path);
  };
  const goalText = goalWith(
    '- 예정 goal: [다음 goal](../planned-goal/goal.md#착수)',
    '- 돌아 들어간 경로: [다른 goal](./nested/../../other-goal/goal.md)',
    '- 저장소 루트까지 올라감: [셋째 goal](../../../01_Phases/goals/third-goal/goal.md)',
    '- 하위 줄의 링크',
    '  - [넷째 `goal`](../fourth-goal/goal.md)',
  );
  const result = checkCandidates({ backlogText, backlogPath: BACKLOG, goalText, goalPath: GOAL, isExistingGoal });
  assertResult(result, 'allowed', 'goal links');
  assert.deepEqual(result.counts, {
    backlogIds: 3, duplicateIds: 0, candidates: 4, candidatesWithoutReference: 0, unknownIds: 0,
  });
  for (const path of existing) assert.ok(asked.includes(path), `isExistingGoal was not asked ${path}: ${asked}`);
});

test('missing goals, other files, web links and links to the goal itself do not count', async () => {
  const { checkCandidates } = await loadPolicy();
  const isExistingGoal = path => path !== '01_Phases/goals/missing-goal/goal.md';
  const goalText = goalWith(
    '- 없는 goal: [사라진 goal](../missing-goal/goal.md)', // 5
    '- 다른 파일: [안내](../planned-goal/README.md)', // 6
    '- 이름 끝만 같음: [비슷한 이름](../planned-goal/notgoal.md)', // 7
    '- https: [GitHub](https://github.com/owner/repo/blob/main/01_Phases/goals/planned-goal/goal.md)', // 8
    '- http: [옛 주소](http://example.com/01_Phases/goals/planned-goal/goal.md)', // 9
    '- 자기 자신: [현재 결과](goal.md#현재-결과)', // 10
    '- 자기 자신 점 경로: [처음](./goal.md)', // 11
    '- 자기 자신 돌아온 경로: [이 goal](../sample-goal/goal.md)', // 12
  );
  const result = checkCandidates({ backlogText, backlogPath: BACKLOG, goalText, goalPath: GOAL, isExistingGoal });
  assertResult(result, 'policy-violation', 'links not counted');
  assertDiagnostics(result, [5, 6, 7, 8, 9, 10, 11, 12].map(line => ['missing-reference', GOAL, line]),
    'links not counted');
  assert.equal(result.counts.candidatesWithoutReference, 8);
});

test('a section without candidates is allowed with zero candidate counts', async () => {
  const { checkCandidates } = await loadPolicy();
  const goalText = goalWith('이 goal 밖으로 둔 일이 없다. BACKLOG `ghost`는 문단 안에 있다.');
  const result = checkCandidates({
    backlogText, backlogPath: BACKLOG, goalText, goalPath: GOAL, isExistingGoal: everyGoalExists,
  });
  assertResult(result, 'allowed', 'no candidates');
  assert.equal(result.goal, GOAL);
  assert.deepEqual(result.counts, {
    backlogIds: 3, duplicateIds: 0, candidates: 0, candidatesWithoutReference: 0, unknownIds: 0,
  });
});

test('BACKLOG violations are reported with the goal check, each on its own file', async () => {
  const { checkCandidates } = await loadPolicy();
  const duplicatedBacklog = lines('| ID | 제목 |', '|---|---|', '| alpha | x |', '| `alpha` | x |'); // 4
  const goalText = goalWith('- 알려진 ID: BACKLOG `alpha`', '- 참조 없는 후보'); // 6
  const result = checkCandidates({
    backlogText: duplicatedBacklog, backlogPath: BACKLOG, goalText, goalPath: GOAL, isExistingGoal: everyGoalExists,
  });
  assertResult(result, 'policy-violation', 'both files');
  assertDiagnostics(result, [['duplicate-backlog-id', BACKLOG, 4], ['missing-reference', GOAL, 6]], 'both files');
  assert.deepEqual(result.counts, {
    backlogIds: 2, duplicateIds: 1, candidates: 2, candidatesWithoutReference: 1, unknownIds: 0,
  });
});

test('the module judges only the given text and predicate, not the files the paths name', async () => {
  const { checkCandidates } = await loadPolicy();
  // Both paths name real files: a 68-row BACKLOG copy and a goal folder whose sibling goal exists.
  const realBacklogPath = join(fixturesDirectory, 'backlog-with-plain-ids.md');
  const realGoalPath = '01_Phases/goals/2026-10-09-plan-boundary-and-junction/goal.md';
  const goalText = goalWith('- 실제로 있는 goal: [병합 관문](../2026-10-06-merge-gate-canon-refresh/goal.md)'); // 5
  const result = checkCandidates({
    backlogText, backlogPath: realBacklogPath, goalText, goalPath: realGoalPath, isExistingGoal: noGoalExists,
  });
  assertResult(result, 'policy-violation', 'given text only');
  assertDiagnostics(result, [['missing-reference', realGoalPath, 5]], 'given text only');
  assert.equal(result.counts.backlogIds, 3);
});

test('evaluation is pure: frozen input is accepted, left unchanged and decided the same way twice', async () => {
  const { checkCandidates } = await loadPolicy();
  const goalText = goalWith('- BACKLOG `ghost`', '- [다음 goal](../planned-goal/goal.md)', '- 참조 없는 후보');
  const input = Object.freeze({
    backlogText, backlogPath: BACKLOG, goalText, goalPath: GOAL, isExistingGoal: everyGoalExists,
  });
  const first = checkCandidates(input);
  assert.deepEqual(checkCandidates(input), first, 'same input, same decision');
  assert.equal(input.goalText, goalText);
  assertResult(first, 'policy-violation', 'frozen input');
});

// Built-in modules that reach files, processes or the network (contract R8).
const deniedBuiltins = new Set(['fs', 'fs/promises', 'child_process', 'net', 'tls', 'dgram', 'dns', 'dns/promises',
  'http', 'https', 'http2']);
const literalSpecifiers = [
  /\bimport\s+(?:[\w$*{}\s,]+?\s+from\s+)?['"]([^'"]+)['"]/g,
  /\bexport\s+(?:\*|\{[^}]*\})\s*(?:as\s+[\w$]+\s+)?from\s+['"]([^'"]+)['"]/g,
  /\bimport\s*\(\s*['"]([^'"]+)['"]\s*\)/g,
  /\brequire\s*\(\s*['"]([^'"]+)['"]\s*\)/g,
];
// Ways to load a module or reach the network that a literal specifier list cannot see.
const hiddenAccess = [
  [/\bimport\s*\(\s*[^'"\s)]/, 'a dynamic import with a computed specifier'],
  [/\brequire\s*\(\s*[^'"\s)]/, 'a require with a computed specifier'],
  [/\bcreateRequire\b/, 'createRequire'],
  [/\bgetBuiltinModule\b/, 'process.getBuiltinModule'],
  [/\bprocess\.binding\b/, 'process.binding'],
  [/\bfetch\s*\(/, 'fetch'],
];

test('the module and the local modules it imports load no file, process or network module', async () => {
  const seen = new Set();
  const pending = [policyPath];
  while (pending.length > 0) {
    const path = pending.pop();
    if (seen.has(path)) continue;
    seen.add(path);
    const source = await readFile(path, 'utf8');
    for (const [pattern, name] of hiddenAccess) assert.doesNotMatch(source, pattern, `${path} uses ${name}`);
    for (const pattern of literalSpecifiers) {
      for (const [, specifier] of source.matchAll(pattern)) {
        assert.ok(!deniedBuiltins.has(specifier.replace(/^node:/, '')), `${path} imports ${specifier}`);
        if (specifier.startsWith('.')) pending.push(join(dirname(path), specifier));
      }
    }
  }
});

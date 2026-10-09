// Independent boundary regressions for 99_Tools/Backlog/candidate-policy.mjs and check-candidates.mjs,
// written by the independent verifier after the implementation (verifies task_e37ef1fb4f20). They cover
// edges the pre-tests leave open. Two kinds of test live here:
// - `contract R#:` tests take every expected value from the Rules lead's test contract v1
//   (task_4e23d12d8aee, R1~R8) and its answer msg_f79fa2b5abb2 (paths with `/`, line null for
//   file-level diagnostics);
// - `observed (contract open):` tests pin what the tool does where the contract sets no value (fenced
//   code, Markdown link forms, span shapes, table ends, byte encodings). They are observations, not
//   requirements: change one only with the reason and the decision that changed the behaviour.
// Every input is synthetic and every expected value is a constant counted by hand from that input.
import assert from 'node:assert/strict';
import { join } from 'node:path';
import { test } from 'node:test';

import {
  assertDiagnostics, assertMessagesName, assertResult, createWork, lines, loadPolicy, parseCliResult, runCli,
  slashPath, writeText,
} from './candidate-fixture.mjs';

const BACKLOG = '00_Document/operations/BACKLOG.md';
const GOAL = '01_Phases/goals/sample-goal/goal.md';
const everyGoalExists = () => true;

// Known IDs: alpha (backticked), beta and gamma (plain).
const backlogText = lines(
  '| ID | 제목 |',
  '|---|---|',
  '| `alpha` | x |',
  '| beta | x |',
  '| gamma | x |',
);

// Candidate rows start on line 5.
const goalWith = (...candidateLines) => lines('# 표본 goal', '', '## 다음 계획 후보', '', ...candidateLines);

async function check(goalText, { goalPath = GOAL, isExistingGoal = everyGoalExists, backlog = backlogText } = {}) {
  const { checkCandidates } = await loadPolicy();
  return checkCandidates({ backlogText: backlog, backlogPath: BACKLOG, goalText, goalPath, isExistingGoal });
}

test('contract R1: only an exact `ID` header followed by a |/-/:/space row makes an ID table', async () => {
  const { extractBacklogIds } = await loadPolicy();
  const text = lines(
    '| `ID` | 제목 |', // 1: a backticked header is not exactly ID
    '|---|---|',
    '| backticked-header | x |',
    '',
    '| ID | 제목 |', // 5: the next row holds a character outside | - : space
    '|---|x|',
    '| bad-separator | x |',
    '',
    '| ID | 제목 |', // 9
    '|:-:|-:|',
    '| colon-separator | x |', // 11
  );
  assert.deepEqual(extractBacklogIds(text), [{ id: 'colon-separator', line: 11 }]);
});

test('contract R1: the last row is read without a final newline, in LF, CRLF and mixed line ends', async () => {
  const { extractBacklogIds } = await loadPolicy();
  const expected = [{ id: 'alpha', line: 3 }, { id: 'last-row', line: 4 }];
  const variants = [
    ['LF', '| ID | 제목 |\n|---|---|\n| `alpha` | x |\n| last-row | x |'],
    ['CRLF', '| ID | 제목 |\r\n|---|---|\r\n| `alpha` | x |\r\n| last-row | x |'],
    ['mixed', '| ID | 제목 |\r\n|---|---|\n| `alpha` | x |\r\n| last-row | x |\n'],
  ];
  for (const [label, text] of variants) assert.deepEqual(extractBacklogIds(text), expected, label);
});

test('contract R2: the heading must be exactly `## 다음 계획 후보` apart from trailing space', async () => {
  const variants = [
    ' ## 다음 계획 후보', // leading space
    '##  다음 계획 후보', // two spaces after ##
    '## 다음  계획 후보', // two spaces inside
    '## 다음 계획 후보 ##', // closing hashes
    '# 다음 계획 후보', // another level
  ];
  for (const heading of variants) {
    const result = await check(lines('# 표본 goal', '', heading, '', '- BACKLOG `ghost`'));
    assertResult(result, 'input-error', heading);
    assertDiagnostics(result, [['missing-section', GOAL, null]], heading);
  }
});

test('contract R2·R3: only `# ` and `## ` lines end the section; other # lines only end a candidate', async () => {
  const result = await check(lines(
    '# 표본 goal',
    '',
    '## 다음 계획 후보', // 3
    '',
    '- 첫 후보: BACKLOG `alpha`', // 5
    '#태그처럼 공백 없는 줄', // 6: no heading, ends candidate 5
    '- 둘째 후보', // 7
    '#### 깊은 제목', // 8: no section end, ends candidate 7
    '- 셋째 후보', // 9
    '##다음 절처럼 붙은 줄', // 10: no section end
    '- 넷째 후보: BACKLOG `ghost`', // 11
    '## 다음 절', // 12: section end
    '- 절 밖: BACKLOG `phantom`', // 13
  ));
  assertResult(result, 'policy-violation', 'section end');
  assertDiagnostics(result, [
    ['missing-reference', GOAL, 7],
    ['missing-reference', GOAL, 9],
    ['unknown-backlog-id', GOAL, 11],
  ], 'section end');
  assertMessagesName(result, 'unknown-backlog-id', [[11, 'ghost']], 'section end');
  assert.deepEqual(result.counts, {
    backlogIds: 3, duplicateIds: 0, candidates: 4, candidatesWithoutReference: 2, unknownIds: 1,
  });
});

test('contract R2·R3: a section at the end of the file keeps its last candidate without a final newline', async () => {
  const goalText = '# 표본 goal\n\n## 다음 계획 후보\n\n- BACKLOG `alpha`\n- 마지막 후보'; // 6, no newline
  const result = await check(goalText);
  assertResult(result, 'policy-violation', 'end of file');
  assertDiagnostics(result, [['missing-reference', GOAL, 6]], 'end of file');
  assert.equal(result.counts.candidates, 2);
});

test('contract R3: `- ` `* ` `+ ` and number-dot candidates; `1)`, `-x`, table and quote lines are not', async () => {
  const result = await check(goalWith(
    '- 대시 후보: BACKLOG `alpha`', // 5
    '10. 두 자리 번호 후보', // 6
    '1) 괄호 번호는 후보가 아니다: BACKLOG `ghost`', // 7: ends candidate 6, not a candidate
    '-붙은 대시는 후보가 아니다: BACKLOG `ghost`', // 8
    '* 별표 후보', // 9
    '| 표 줄 | BACKLOG `ghost` |', // 10: ends candidate 9
    '> 인용 줄 BACKLOG `ghost`', // 11
    '+ 더하기 후보: BACKLOG `beta`', // 12
  ));
  assertResult(result, 'policy-violation', 'list forms');
  assertDiagnostics(result, [['missing-reference', GOAL, 6], ['missing-reference', GOAL, 9]], 'list forms');
  assert.deepEqual(result.counts, {
    backlogIds: 3, duplicateIds: 0, candidates: 4, candidatesWithoutReference: 2, unknownIds: 0,
  });
});

test('contract R3: tab-indented lines belong to the candidate above them', async () => {
  const result = await check(goalWith(
    '- 첫 후보', // 5
    '\t탭으로 들여쓴 줄: BACKLOG `alpha`',
    '- 둘째 후보', // 7
    '\t- 탭 하위 목록: [예정 goal](../planned-goal/goal.md)',
  ));
  assertResult(result, 'allowed', 'tab indent');
  assert.deepEqual(result.counts, {
    backlogIds: 3, duplicateIds: 0, candidates: 2, candidatesWithoutReference: 0, unknownIds: 0,
  });
});

test('contract R4: BACKLOG must be the free-standing word right before the span; every citation counts', async () => {
  const result = await check(goalWith(
    '- 뒤에 숫자: BACKLOG2 `alpha`', // 5
    '- 쌍점: BACKLOG:`alpha`', // 6
    '- 링크 글자 안: [BACKLOG `ghost`](../../../00_Document/operations/BACKLOG.md)', // 7
    '- 한 줄 두 인용: BACKLOG `alpha` 그리고 BACKLOG `ghost`', // 8
    '- 같은 ID 두 번: BACKLOG `ghost`·`ghost`', // 9
  ));
  assertResult(result, 'policy-violation', 'citation word');
  assertDiagnostics(result, [
    ['missing-reference', GOAL, 5],
    ['missing-reference', GOAL, 6],
    ['unknown-backlog-id', GOAL, 7],
    ['unknown-backlog-id', GOAL, 8],
    ['unknown-backlog-id', GOAL, 9],
    ['unknown-backlog-id', GOAL, 9],
  ], 'citation word');
  assertMessagesName(result, 'unknown-backlog-id', [[7, 'ghost'], [8, 'ghost'], [9, 'ghost'], [9, 'ghost']],
    'citation word');
  assert.deepEqual(result.counts, {
    backlogIds: 3, duplicateIds: 0, candidates: 5, candidatesWithoutReference: 2, unknownIds: 4,
  });
});

test('contract R5: the file name must be exactly goal.md; anchor-only and empty targets do not count', async () => {
  const result = await check(goalWith(
    '- 대문자 첫 글자: [x](../planned-goal/Goal.md)', // 5
    '- 전부 대문자: [x](../planned-goal/GOAL.md)', // 6
    '- 뒤에 확장자: [x](../planned-goal/goal.md.bak)', // 7
    '- anchor만: [x](#다음-계획-후보)', // 8
    '- 빈 대상: [x]()', // 9
  ));
  assertResult(result, 'policy-violation', 'file name');
  assertDiagnostics(result, [5, 6, 7, 8, 9].map(line => ['missing-reference', GOAL, line]), 'file name');
});

test('contract R5: isExistingGoal gets the link resolved from the goal folder and normalized with /', async () => {
  const cases = [
    // [label, goalPath, link, the one path isExistingGoal must be asked]
    ['above the start folder', GOAL, '../../../../../goal.md', '../../goal.md'],
    ['drive path', 'C:/repo/01_Phases/goals/sample-goal/goal.md', '../other-goal/goal.md#착수',
      'C:/repo/01_Phases/goals/other-goal/goal.md'],
    ['backslash goal path', 'C:\\repo\\01_Phases\\goals\\sample-goal\\goal.md', '../third-goal/goal.md',
      'C:/repo/01_Phases/goals/third-goal/goal.md'],
  ];
  for (const [label, goalPath, link, wanted] of cases) {
    const asked = [];
    const isExistingGoal = path => {
      asked.push(path);
      return path === wanted;
    };
    const result = await check(goalWith(`- 링크 후보: [x](${link})`), { goalPath, isExistingGoal });
    assertResult(result, 'allowed', label);
    assert.deepEqual(asked, [wanted], label);
  }
});

test('contract R5 and answer item 2: self links are excluded however the goal path is written', async () => {
  const { checkCandidates } = await loadPolicy();
  const cases = [
    // [received goal path, the same path as reported, self link]
    ['C:\\repo\\01_Phases\\goals\\sample-goal\\goal.md', 'C:/repo/01_Phases/goals/sample-goal/goal.md', 'goal.md'],
    ['./01_Phases/goals/sample-goal/goal.md', './01_Phases/goals/sample-goal/goal.md', '../sample-goal/goal.md'],
  ];
  for (const [goalPath, reported, link] of cases) {
    const result = checkCandidates({
      backlogText,
      backlogPath: '00_Document\\operations\\BACKLOG.md',
      goalText: goalWith(`- 자기 자신: [이 goal](${link})`), // 5
      goalPath,
      isExistingGoal: everyGoalExists,
    });
    assertResult(result, 'policy-violation', goalPath);
    assert.equal(result.backlog, BACKLOG, goalPath);
    assert.equal(result.goal, reported, goalPath);
    assertDiagnostics(result, [['missing-reference', reported, 5]], goalPath);
  }
});

test('contract R6: a citation of a missing ID is reported even when the candidate also links a goal', async () => {
  const result = await check(goalWith(
    '- BACKLOG `ghost`와 [예정 goal](../planned-goal/goal.md)', // 5
    '- [예정 goal](../planned-goal/goal.md) 그리고 BACKLOG `alpha`', // 6
  ));
  assertResult(result, 'policy-violation', 'citation with link');
  assertDiagnostics(result, [['unknown-backlog-id', GOAL, 5]], 'citation with link');
  assert.deepEqual(result.counts, {
    backlogIds: 3, duplicateIds: 0, candidates: 2, candidatesWithoutReference: 0, unknownIds: 1,
  });
});

test('contract R7: option spellings other than --backlog and --goal are usage errors', async t => {
  const work = await createWork(t, 'boundary-usage');
  const backlog = await writeText(work, 'BACKLOG.md', backlogText);
  const cases = [
    ['equals form', [`--backlog=${backlog}`]],
    ['upper case', ['--BACKLOG', backlog]],
    ['short form', ['-b', backlog]],
  ];
  for (const [label, args] of cases) {
    const result = parseCliResult(runCli(args, { cwd: work }), label);
    assertResult(result, 'input-error', label);
    assertDiagnostics(result, [['usage', null, null]], label);
  }
});

test('observed (contract open): lines inside fenced code are read as plain lines', async () => {
  // A heading line in a code block counts as a second candidate section.
  const quoted = await check(lines(
    '# 표본 goal', '', '## 다음 계획 후보', '', '- BACKLOG `alpha`', '', '```markdown',
    '## 다음 계획 후보', // 8
    '```',
  ));
  assertResult(quoted, 'input-error', 'heading in code');
  assertDiagnostics(quoted, [['duplicate-section', GOAL, 8]], 'heading in code');

  // A column-0 `# ` line in a code block ends the section, so later candidates are not checked.
  const commented = await check(goalWith(
    '- 첫 후보: BACKLOG `alpha`', // 5
    '```bash',
    '# 주석 줄',
    '```',
    '- 코드 뒤 후보', // 9: not seen
  ));
  assertResult(commented, 'allowed', 'comment in code');
  assert.equal(commented.counts.candidates, 1);
});

test('observed (contract open): the space after BACKLOG and inside a chain may be a line break', async () => {
  const result = await check(goalWith(
    '- 줄 끝 낱말 BACKLOG', // 5
    '  `ghost`', // 6
    '- 사슬이 줄을 넘음: BACKLOG `alpha`·', // 7
    '  `phantom`', // 8
  ));
  assertResult(result, 'policy-violation', 'line breaks');
  assertDiagnostics(result, [['unknown-backlog-id', GOAL, 6], ['unknown-backlog-id', GOAL, 8]], 'line breaks');
  assertMessagesName(result, 'unknown-backlog-id', [[6, 'ghost'], [8, 'phantom']], 'line breaks');
  assert.equal(result.counts.candidatesWithoutReference, 0);
});

test('observed (contract open): span text is used as written; `` and ``x`` give the empty ID', async () => {
  const result = await check(goalWith(
    '- 빈 span: BACKLOG ``', // 5
    '- 두 겹 백틱: BACKLOG ``alpha``', // 6
    '- 안쪽 공백: BACKLOG ` alpha `', // 7
  ));
  assertResult(result, 'policy-violation', 'span shapes');
  assertDiagnostics(result, [5, 6, 7].map(line => ['unknown-backlog-id', GOAL, line]), 'span shapes');
  assertMessagesName(result, 'unknown-backlog-id', [[5, '""'], [6, '""'], [7, '" alpha "']], 'span shapes');
  assert.equal(result.counts.candidatesWithoutReference, 0);
});

// A tab counts as the space after BACKLOG.
test('observed (contract open): BACKLOG is case-sensitive and binds only to ASCII letters and digits', async () => {
  const result = await check(goalWith(
    '- 한글 앞: 후보BACKLOG `alpha`', // 5
    '- 밑줄 앞: _BACKLOG `ghost`', // 6
    '- 탭 뒤: BACKLOG\t`beta`', // 7
    '- 소문자: backlog `alpha`', // 8
  ));
  assertResult(result, 'policy-violation', 'word edges');
  assertDiagnostics(result, [['unknown-backlog-id', GOAL, 6], ['missing-reference', GOAL, 8]], 'word edges');
  assert.equal(result.counts.candidatesWithoutReference, 1);
});

// Only `[text](target)` with no `]` in the text and a bare target counts; upper-case web schemes stay excluded.
test('observed (contract open): titled, angle, reference-style and bracketed-text links do not count', async () => {
  const asked = [];
  const isExistingGoal = path => {
    asked.push(path);
    return true;
  };
  const result = await check(goalWith(
    '- 제목 붙은 링크: [x](../planned-goal/goal.md "예정")', // 5
    '- 꺾쇠 링크: [x](<../planned-goal/goal.md>)', // 6
    '- 대문자 scheme: [x](HTTPS://example.com/01_Phases/goals/planned-goal/goal.md)', // 7
    '- 역슬래시 경로: [x](..\\planned-goal\\goal.md)', // 8: counted
    '- 참조식 링크: [x][planned]', // 9
    '- 글자에 대괄호: [R-2 [표본]](../planned-goal/goal.md)', // 10
    '',
    '[planned]: ../planned-goal/goal.md', // 12: ends candidate 10
  ), { isExistingGoal });
  assertResult(result, 'policy-violation', 'link forms');
  assertDiagnostics(result, [5, 6, 7, 9, 10].map(line => ['missing-reference', GOAL, line]), 'link forms');
  assert.deepEqual(asked, ['01_Phases/goals/planned-goal/goal.md']);
});

test('observed (contract open): input errors of both files are reported together', async () => {
  const { checkCandidates } = await loadPolicy();
  const result = checkCandidates({
    backlogText: lines('| 필드 | 기록 기준 |', '|---|---|', '| ID | 문자열 |'),
    backlogPath: BACKLOG,
    goalText: lines('# 표본 goal', '', '## 범위'),
    goalPath: GOAL,
    isExistingGoal: everyGoalExists,
  });
  assertResult(result, 'input-error', 'both inputs');
  assertDiagnostics(result, [['no-id-table', BACKLOG, null], ['missing-section', GOAL, null]], 'both inputs');
});

test('observed (contract open): table ends, a header without a leading pipe and a separator without dashes', async () => {
  const { extractBacklogIds } = await loadPolicy();
  const text = lines(
    '| ID | 제목 |',
    '|---|---|',
    '| alpha | x |', // 3
    '',
    '| orphan-after-blank | x |', // 5: a pipe row after the blank line has no header
    '',
    'ID | 제목', // 7
    '---|---',
    'no-leading-pipe | x', // 9
    '',
    '| ID | 제목 |',
    '|---|---|',
    '| beta | x |', // 13
    '파이프 없는 줄', // 14: GFM would still read this as a row; the tool ends the table here
    '| after-plain-line | x |', // 15
    '',
    '| ID | 제목 |',
    '| | |', // 18: only pipes and spaces, no dash
    '| pipes-only-separator | x |', // 19
  );
  assert.deepEqual(extractBacklogIds(text), [
    { id: 'alpha', line: 3 },
    { id: 'no-leading-pipe', line: 9 },
    { id: 'beta', line: 13 },
    { id: 'pipes-only-separator', line: 19 },
  ]);
});

test('observed (contract open): CLI byte encodings, two unreadable files and stray arguments', async t => {
  const work = await createWork(t, 'boundary-cli');
  const backlog = await writeText(work, 'BACKLOG.md', backlogText);
  const withBom = await writeText(work, 'BACKLOG-bom.md', `\uFEFF${backlogText}`);
  const invalidUtf8 = await writeText(work, 'BACKLOG-latin1.md', Buffer.from([0x7c, 0x20, 0xff, 0x20, 0x7c, 0x0a]));
  const noTable = await writeText(work, 'BACKLOG-no-table.md', lines('# 목표 전 후보'));
  const absentBacklog = join(work, 'absent-BACKLOG.md');
  const absentGoal = join(work, 'goals', 'absent-goal', 'goal.md');

  const bom = parseCliResult(runCli(['--backlog', withBom], { cwd: work }), 'BOM');
  assertResult(bom, 'allowed', 'BOM');
  assert.equal(bom.counts.backlogIds, 3);

  const latin1 = parseCliResult(runCli(['--backlog', invalidUtf8], { cwd: work }), 'invalid UTF-8');
  assertResult(latin1, 'input-error', 'invalid UTF-8');
  assertDiagnostics(latin1, [['unreadable-file', slashPath(invalidUtf8), null]], 'invalid UTF-8');

  const bothAbsent = parseCliResult(runCli(['--backlog', absentBacklog, '--goal', absentGoal], { cwd: work }),
    'both absent');
  assertResult(bothAbsent, 'input-error', 'both absent');
  assertDiagnostics(bothAbsent, [
    ['unreadable-file', slashPath(absentBacklog), null],
    ['unreadable-file', slashPath(absentGoal), null],
  ], 'both absent');

  // An unreadable goal hides the BACKLOG structure check.
  const unreadableFirst = parseCliResult(runCli(['--backlog', noTable, '--goal', absentGoal], { cwd: work }),
    'unreadable before structure');
  assertResult(unreadableFirst, 'input-error', 'unreadable before structure');
  assertDiagnostics(unreadableFirst, [['unreadable-file', slashPath(absentGoal), null]], 'unreadable before structure');

  for (const [label, args] of [['stray argument', ['--backlog', backlog, 'extra']],
    ['value starting with -', ['--backlog', '-dash.md']]]) {
    const result = parseCliResult(runCli(args, { cwd: work }), label);
    assertResult(result, 'input-error', label);
    assertDiagnostics(result, [['usage', null, null]], label);
  }
});

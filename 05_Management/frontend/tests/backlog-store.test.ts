// @vitest-environment node
// Requirement: goal 「만들 것」 6 and backlog-menu-design.md 「공유 백로그 판정」·「두 단계」: the backlog
// judgement moves out of record-index-check.ts into createBacklogStore({ repositoryRoot }).read()
// without changing what it finds — the five existing codes, their order, BACKLOG.md lines and
// sentences, and the missing·load failures. Every case here keeps the same expectation after the
// behaviour step (no fence, junction, letter case, undecodable or outside-root link), so this file
// passes once the structure step is done. Rows are checked field by field because the behaviour
// step adds `group`. The cause·fix sentences are copied from record-index-check.ts and the
// table-format causes from backlog-table.ts at 6ab9cc7a; nothing is computed by the product.
import { rmSync } from 'node:fs';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { createBacklogStore } from '../electron/backlog-store';
import { BACKLOG_PATH, CANDIDATE_TABLE_HEADER, backlogDocument, candidateRow } from './record-sources/backlog-fixture';
import { createOwnedTempRepository, type OwnedTempRepository } from './record-sources/owned-temp-repository';

const INDEXED_GOAL = '01_Phases/goals/2026-10-01-indexed/goal.md';
const MANAGED_GOAL = '05_Management/goals/2026-10-02-managed/goal.md';

const ID_FORMAT = { code: 'BACKLOG_ID_FORMAT', cause: '후보 ID가 소문자 단어를 하이픈으로 잇는 형식이 아닙니다.', fix: '128자 이하의 안정적인 소문자 ID를 쓰세요.' };
const PROMOTION_LINK_MISSING = { code: 'BACKLOG_PROMOTION_LINK_MISSING', cause: 'goal 승격 행에 goal 상대 링크가 없습니다.', fix: '승격한 goal의 원문 파일로 가는 상대 링크를 상태에 추가하세요.' };
const GOAL_LINK_MISSING = { code: 'BACKLOG_GOAL_LINK_MISSING', cause: '상대 goal 링크의 대상 파일이 없습니다.', fix: 'BACKLOG.md 기준 상대경로와 대상 goal 파일의 존재를 확인하세요.' };
const TABLE_FORMAT_FIX = 'ID 표의 머리글·구분 행·자료 행의 열 수를 맞추세요.';
const duplicate = (id: string) => ({ code: 'BACKLOG_ID_DUPLICATE', cause: `후보 ID ${id}가 중복됩니다.`, fix: '후보마다 서로 다른 안정적인 ID를 지정하세요.' });

let repository: OwnedTempRepository;
beforeEach(() => {
  repository = createOwnedTempRepository();
  repository.write(INDEXED_GOAL, '# 목표\n');
  repository.write(MANAGED_GOAL, '# 목표\n');
});
afterEach(() => repository.remove());

function writeBacklog(lines: string[]) {
  const document = backlogDocument(lines);
  repository.write(BACKLOG_PATH, document.text);
  return document;
}
const read = () => createBacklogStore({ repositoryRoot: repository.root }).read();

describe('candidate rows (structure step keeps the current reading)', () => {
  it('returns every row of the ID tables with its BACKLOG.md line and cells, no issue and no unchecked link for a clean file', async () => {
    const clean = '| `clean-candidate` | 깨끗한 후보 | 반복 확인을 줄인다 | 메인 2026-10-07 msg_0123456789ab | 없음 | Management | 대기 |';
    const promoted = `| \`promoted-candidate\` | 승격 후보 | 기록을 한곳에 둔다 | 메인 전달 | PR2 병합 | Rules | goal 승격 → [색인된 goal](../../${INDEXED_GOAL}#결과와-열린-사항) |`;
    const managed = `| \`managed-candidate\` | 관리 후보 | 이유 | 출처 | 없음 | Core | goal 승격 → [관리 goal](../../${MANAGED_GOAL}) |`;
    const document = writeBacklog([
      '# 목표 전 후보',
      '',
      '## 항목 계약',
      '',
      '| 필드 | 뜻 |',
      '|---|---|',
      '| ID | 목적을 나타내는 안정적인 소문자 문자열 |',
      '',
      '## 후보',
      '',
      ...CANDIDATE_TABLE_HEADER,
      clean,
      promoted,
      '',
      '## 다른 후보',
      '',
      ...CANDIDATE_TABLE_HEADER,
      managed,
    ]);

    const result = await read();
    const rows = result.ok ? result.rows : [];
    expect(result).toMatchObject({ ok: true, issues: [], uncheckedLinks: [] });
    expect(rows).toHaveLength(3);
    expect(rows[0]).toMatchObject({
      line: document.lineOf(clean), id: 'clean-candidate', title: '깨끗한 후보', reason: '반복 확인을 줄인다',
      source: '메인 2026-10-07 msg_0123456789ab', prerequisite: '없음', owner: 'Management', status: '대기',
    });
    expect(rows[1]).toMatchObject({
      line: document.lineOf(promoted), id: 'promoted-candidate', title: '승격 후보', reason: '기록을 한곳에 둔다',
      source: '메인 전달', prerequisite: 'PR2 병합', owner: 'Rules',
      status: `goal 승격 → [색인된 goal](../../${INDEXED_GOAL}#결과와-열린-사항)`,
    });
    expect(rows[2]).toMatchObject({ line: document.lineOf(managed), id: 'managed-candidate', owner: 'Core' });
  });

  it('reads BACKLOG.md again on every call, so a changed file applies to the next read', async () => {
    writeBacklog(['## 후보', ...CANDIDATE_TABLE_HEADER, candidateRow('first-version')]);
    const store = createBacklogStore({ repositoryRoot: repository.root });
    const first = await store.read();
    writeBacklog(['## 후보', ...CANDIDATE_TABLE_HEADER, candidateRow('second-version')]);
    const second = await store.read();
    const ids = (result: typeof first) => (result.ok ? result.rows.map(row => row.id) : []);
    expect({ first: ids(first), second: ids(second) }).toEqual({ first: ['first-version'], second: ['second-version'] });
  });
});

describe('issues of the five existing codes (structure step keeps code, order, line and sentences)', () => {
  it('lists table-format problems first in file order, then per row ID shape, duplicate, promotion link and goal links in cell order', async () => {
    const badShape = candidateRow('Bad_ID', { title: '모양', status: 'goal 승격' });
    const duplicateWithLinks = candidateRow('first-candidate', {
      title: '중복',
      source: '[없는 goal](../../01_Phases/goals/2026-10-09-none/goal.md)',
      status: `goal 승격 → [색인된 goal](../../${INDEXED_GOAL}) [없는 다른 goal](../../05_Management/goals/2026-10-09-gone/goal.md)`,
    });
    const shortRow = '| `short-row` | 열 부족 | 이유 | 출처 | 없음 | Rules |';
    const badShapeAgain = candidateRow('Bad_ID', { title: '둘째' });
    const brokenHeader = '| ID | 제목 | 상태 |';
    const document = writeBacklog([
      '## 후보',
      ...CANDIDATE_TABLE_HEADER,
      candidateRow('first-candidate'),
      badShape,
      duplicateWithLinks,
      shortRow,
      badShapeAgain,
      '',
      '## 깨진 표',
      brokenHeader,
      '',
    ]);

    const result = await read();
    const at = (line: string) => document.lineOf(line);
    expect(result).toEqual({
      ok: true,
      rows: expect.any(Array),
      uncheckedLinks: [],
      issues: [
        { code: 'BACKLOG_TABLE_FORMAT', line: at(shortRow), cause: '표의 열 수가 머리글 7개와 다릅니다.', fix: TABLE_FORMAT_FIX },
        { code: 'BACKLOG_TABLE_FORMAT', line: at(brokenHeader), cause: 'ID 표의 구분 행 또는 열 수가 올바르지 않습니다.', fix: TABLE_FORMAT_FIX },
        { ...ID_FORMAT, line: at(badShape) },
        { ...PROMOTION_LINK_MISSING, line: at(badShape) },
        { ...duplicate('first-candidate'), line: at(duplicateWithLinks) },
        { ...GOAL_LINK_MISSING, line: at(duplicateWithLinks) },
        { ...GOAL_LINK_MISSING, line: at(duplicateWithLinks) },
        { ...ID_FORMAT, line: at(badShapeAgain) },
        { ...duplicate('Bad_ID'), line: at(badShapeAgain) },
      ],
    });
    expect(result.ok ? result.rows.map(row => row.id) : []).toEqual(['first-candidate', 'Bad_ID', 'first-candidate', 'short-row', 'Bad_ID']);
  });
});

describe('BACKLOG.md that cannot be read', () => {
  it('returns missing with the fixed sentence when BACKLOG.md or its folder does not exist', async () => {
    rmSync(repository.path('00_Document'), { recursive: true, force: true });
    const withoutFolder = await read();
    repository.mkdir('00_Document/operations');
    const withoutFile = await read();
    const missing = { ok: false, code: 'missing', message: 'BACKLOG.md를 찾지 못했습니다.' };
    expect({ withoutFolder, withoutFile }).toEqual({ withoutFolder: missing, withoutFile: missing });
  });

  it('returns load with the fixed sentence when BACKLOG.md is a directory', async () => {
    repository.mkdir(BACKLOG_PATH);
    expect(await read()).toEqual({ ok: false, code: 'load', message: 'BACKLOG.md를 읽지 못했습니다. 다시 읽으세요.' });
  });
});

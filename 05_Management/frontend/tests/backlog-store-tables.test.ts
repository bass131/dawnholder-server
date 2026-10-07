// @vitest-environment node
// Requirement: backlog-menu-design.md 「두 단계」 behaviour rows for the table reading: a missing ID
// table is BACKLOG_TABLE_FORMAT with line null and the fixed sentences, an ID table with no rows is
// not; a backtick fence whose info string contains a backtick does not open (the 「fence 판정」 rule
// of the source section reader), and every row gets `group`, the nearest `## ` heading outside a
// fence before it (null before any). These fail until the behaviour step. Expected rows and groups
// are written here from the fixed input, through the real createBacklogStore on an owned TEMP root.
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { createBacklogStore } from '../electron/backlog-store';
import { BACKLOG_PATH, CANDIDATE_TABLE_HEADER, backlogDocument, candidateRow } from './record-sources/backlog-fixture';
import { createOwnedTempRepository, type OwnedTempRepository } from './record-sources/owned-temp-repository';

const NO_ID_TABLE = {
  code: 'BACKLOG_TABLE_FORMAT', line: null, cause: '첫 열이 ID인 후보 표가 없습니다.', fix: '머리글 첫 열이 ID인 후보 표와 구분 행을 두세요.',
};

let repository: OwnedTempRepository;
beforeEach(() => { repository = createOwnedTempRepository(); });
afterEach(() => repository.remove());

async function readLines(lines: string[]) {
  const document = backlogDocument(lines);
  repository.write(BACKLOG_PATH, document.text);
  return { document, result: await createBacklogStore({ repositoryRoot: repository.root }).read() };
}

describe('ID table presence (behaviour step)', () => {
  it('reports a missing ID table as BACKLOG_TABLE_FORMAT with line null, but not an ID table that has no rows', async () => {
    const noTable = await readLines(['# 목표 전 후보', '', '아직 후보 표가 없다.']);
    const onlyOtherTables = await readLines([
      '# 목표 전 후보',
      '| 필드 | 뜻 |',
      '|---|---|',
      '| ID | 안정적인 소문자 문자열 |',
      '',
      '```md',
      ...CANDIDATE_TABLE_HEADER,
      candidateRow('fenced-example'),
      '```',
    ]);
    const emptyTable = await readLines(['## 후보', ...CANDIDATE_TABLE_HEADER, '']);

    const missingTable = { ok: true, rows: [], issues: [NO_ID_TABLE], uncheckedLinks: [] };
    expect(noTable.result, 'no table').toEqual(missingTable);
    expect(onlyOtherTables.result, 'only a field table and a fenced ID table').toEqual(missingTable);
    expect(emptyTable.result, 'an ID table with no rows').toEqual({ ok: true, rows: [], issues: [], uncheckedLinks: [] });
  });
});

describe('fence rule shared with the source section reader (behaviour step)', () => {
  it('reads the candidate table after a backtick line whose info string has a backtick, and skips tables inside tilde and backtick fences', async () => {
    const afterBacktickLine = candidateRow('after-backtick-line');
    const { document, result } = await readLines([
      '## 후보',
      '```js`is not a fence',
      ...CANDIDATE_TABLE_HEADER,
      afterBacktickLine,
      '',
      '~~~ tilde`fence',
      ...CANDIDATE_TABLE_HEADER,
      candidateRow('inside-tilde-fence'),
      '~~~',
      '',
      '```text',
      ...CANDIDATE_TABLE_HEADER,
      candidateRow('inside-backtick-fence'),
      '```',
    ]);
    const rows = result.ok ? result.rows.map(row => ({ id: row.id, line: row.line })) : [];
    expect(result).toMatchObject({ ok: true, issues: [], uncheckedLinks: [] });
    expect(rows).toEqual([{ id: 'after-backtick-line', line: document.lineOf(afterBacktickLine) }]);
  });
});

describe('row group (behaviour step)', () => {
  it('gives each row the nearest ## heading text outside fences before it, and null before any ## heading', async () => {
    const { result } = await readLines([
      '# 목표 전 후보',
      ...CANDIDATE_TABLE_HEADER,
      candidateRow('before-any-heading'),
      '',
      '## 첫 묶음',
      '',
      ...CANDIDATE_TABLE_HEADER,
      candidateRow('first-group-row'),
      '',
      '```md',
      '## 펜스 안 제목',
      '```',
      '',
      ...CANDIDATE_TABLE_HEADER,
      candidateRow('still-first-group'),
      '',
      '## 둘째 묶음',
      ...CANDIDATE_TABLE_HEADER,
      candidateRow('second-group-row'),
    ]);
    const groups = result.ok ? result.rows.map(row => ({ id: row.id, group: row.group })) : [];
    expect(result.ok).toBe(true);
    expect(groups).toEqual([
      { id: 'before-any-heading', group: null },
      { id: 'first-group-row', group: '첫 묶음' },
      { id: 'still-first-group', group: '첫 묶음' },
      { id: 'second-group-row', group: '둘째 묶음' },
    ]);
  });
});

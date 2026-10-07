// Requirement: goal 「만들 것」 6 and 완료조건 5 (BACKLOG candidates shown read only, ID·goal link
// mismatches as warnings and an unreadable table as a format error) and backlog-menu-design.md
// 「화면」: the summary line, candidates grouped by `group` in file order with their fields and
// BACKLOG.md:<line>, each row's problems by kind name with cause and 「고치는 방법: <fix>」, unchecked
// links, the 「표 형식 오류」 area for problems not on a candidate row, the state sentences, the previous
// candidates kept after a failed re-read, one read at a time, a late earlier answer dropped, and no
// input, selection, editing or link element. window.systemBacklog is a fake bridge; its results are
// written here in the design's BacklogResult shape and the expected texts are the design's fixed
// wording. Fails until the behaviour step adds src/DevelopmentBacklog.tsx (default export, no
// required props, like DevelopmentRecords.tsx).
import '@testing-library/jest-dom/vitest';
import { act, cleanup, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { StrictMode } from 'react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import DevelopmentBacklog from './DevelopmentBacklog';

interface CandidateFields {
  line: number;
  id: string;
  title: string;
  reason: string;
  source: string;
  prerequisite: string;
  owner: string;
  status: string;
  group: string | null;
}

function row(fields: CandidateFields) {
  const cells = [`\`${fields.id}\``, fields.title, fields.reason, fields.source, fields.prerequisite, fields.owner, fields.status];
  return { ...fields, cells, goalLinks: [] };
}

const MENU: CandidateFields = {
  line: 23, id: 'menu-candidate', title: '백로그 메뉴 **굵게** 후보', reason: '다음 일을 한곳에서 본다',
  source: '메인 2026-10-07 msg_0123456789ab', prerequisite: '[PR2](../../05_Management/goals/2026-10-06-record-source-unification/goal.md) 병합',
  owner: 'Management', status: '진행 → [확인할 goal](../../01_Phases/goals/2026-10-07-pending/goal.md)', group: '후보',
};
const SHAPE: CandidateFields = {
  line: 24, id: 'Bad_ID', title: '모양이 틀린 후보', reason: '이유 B', source: '출처 B', prerequisite: '없음',
  owner: 'Rules', status: 'goal 승격 → [없는 goal](../../01_Phases/goals/2026-10-09-none/goal.md)', group: '후보',
};
const LATE: CandidateFields = {
  line: 107, id: 'late-candidate', title: '마감 뒤 후보 하나', reason: '이유 C',
  source: '[밖 goal](../../../01_Phases/goals/2026-10-01-outside/goal.md)', prerequisite: '없음', owner: 'Core', status: '대기', group: '마감 뒤 후보',
};
const SHORT: CandidateFields = {
  line: 108, id: 'short-row', title: '열이 모자란 후보', reason: '이유 D', source: '출처 D', prerequisite: '없음',
  owner: 'Rules', status: '', group: '마감 뒤 후보',
};
const CANDIDATES = [MENU, SHAPE, LATE, SHORT];

const TABLE_FORMAT_FIX = 'ID 표의 머리글·구분 행·자료 행의 열 수를 맞추세요.';
const HEADER_ISSUE = { code: 'BACKLOG_TABLE_FORMAT', line: 60, cause: 'ID 표의 구분 행 또는 열 수가 올바르지 않습니다.', fix: TABLE_FORMAT_FIX };
const SHORT_ROW_ISSUE = { code: 'BACKLOG_TABLE_FORMAT', line: 108, cause: '표의 열 수가 머리글 7개와 다릅니다.', fix: TABLE_FORMAT_FIX };
const ID_FORMAT_ISSUE = { code: 'BACKLOG_ID_FORMAT', line: 24, cause: '후보 ID가 소문자 단어를 하이픈으로 잇는 형식이 아닙니다.', fix: '128자 이하의 안정적인 소문자 ID를 쓰세요.' };
const LINK_MISSING_ISSUE = { code: 'BACKLOG_GOAL_LINK_MISSING', line: 24, cause: '상대 goal 링크의 대상 파일이 없습니다.', fix: 'BACKLOG.md 기준 상대경로와 대상 goal 파일의 존재를 확인하세요.' };
const LINK_REJECTED_ISSUE = {
  code: 'BACKLOG_GOAL_LINK_REJECTED', line: 107, cause: '상대 goal 링크가 저장소 경로 규칙에 맞지 않습니다 (parent).',
  fix: 'BACKLOG.md 기준 상대경로로 저장소 안의 goal 파일을 대소문자까지 같게 가리키고 링크·junction을 거치지 마세요.',
};
const UNCHECKED_LINK = { line: 23, link: '../../01_Phases/goals/2026-10-07-pending/goal.md' };
const RESULT = {
  ok: true,
  rows: CANDIDATES.map(row),
  issues: [HEADER_ISSUE, SHORT_ROW_ISSUE, ID_FORMAT_ISSUE, LINK_MISSING_ISSUE, LINK_REJECTED_ISSUE],
  uncheckedLinks: [UNCHECKED_LINK],
};
const SUMMARY = '후보 4개 · 형식 오류 2 · 어긋남 3 · 확인 불가 1';

const STATE = {
  noBridge: '백로그 연결을 사용할 수 없습니다. 데스크톱 앱에서 실행하세요.',
  noAnswer: '백로그 연결에서 응답을 받지 못했습니다.',
  refreshFailed: '갱신 실패: 이전에 읽은 백로그를 표시합니다.',
  recheck: '다시 읽기로 다시 확인하세요.',
};

const readBacklog = vi.fn<() => Promise<unknown>>();
beforeEach(() => {
  readBacklog.mockReset().mockResolvedValue(RESULT);
  vi.stubGlobal('systemBacklog', { readBacklog });
});
afterEach(() => { cleanup(); vi.unstubAllGlobals(); });

// Text as a person reads it: whitespace collapsed, element boundaries ignored.
const textOf = (element: Element) => (element.textContent ?? '').replace(/\s+/g, ' ').trim();

// The innermost elements under root whose text contains every part.
function innermostContaining(parts: string[], root: Element = document.body): Element[] {
  const holds = (element: Element) => parts.every(part => textOf(element).includes(part));
  return [root, ...root.querySelectorAll('*')].filter(element => holds(element) && !Array.from(element.children).some(holds));
}

// The innermost elements whose whole text is exactly `text`.
function exactly(text: string): Element[] {
  return [...document.body.querySelectorAll('*')].filter(element => textOf(element) === text
    && !Array.from(element.children).some(child => textOf(child) === text));
}

// One candidate's place on the screen: the innermost element showing all its fields, widened to
// the largest ancestor that holds no other candidate, so problems shown beside the fields count.
function candidateBlock(candidate: CandidateFields, all: CandidateFields[] = CANDIDATES): Element {
  const fields = [candidate.id, candidate.title, candidate.reason, candidate.source, candidate.prerequisite, candidate.owner, candidate.status, `BACKLOG.md:${candidate.line}`];
  const shown = innermostContaining(fields.filter(field => field !== ''));
  expect(shown, `${candidate.id}: one element shows every field`).toHaveLength(1);
  const others = all.filter(other => other.id !== candidate.id).map(other => other.id);
  let block = shown[0]!;
  while (block.parentElement && !others.some(id => textOf(block.parentElement!).includes(id))) block = block.parentElement;
  return block;
}

const follows = (earlier: Element, later: Element) => (earlier.compareDocumentPosition(later) & Node.DOCUMENT_POSITION_FOLLOWING) !== 0;

async function renderLoaded() {
  const user = userEvent.setup();
  const view = render(<DevelopmentBacklog />);
  await waitFor(() => expect(exactly(SUMMARY)).toHaveLength(1));
  return { user, container: view.container };
}

describe('summary and candidates', () => {
  it('shows the summary line 후보 N개 · 형식 오류 a · 어긋남 b · 확인 불가 c counted from the result', async () => {
    await renderLoaded();
    expect(exactly(SUMMARY)[0]).toBeVisible();
    expect(readBacklog).toHaveBeenCalledOnce();
  });

  it('groups candidates under their group title in file order', async () => {
    await renderLoaded();
    const summary = exactly(SUMMARY)[0]!;
    const groupTitle = (title: string) => {
      const found = exactly(title).filter(element => !summary.contains(element));
      expect(found.length, `group title ${title}`).toBeGreaterThan(0);
      return found[0]!;
    };
    const order = [groupTitle('후보'), candidateBlock(MENU), candidateBlock(SHAPE), groupTitle('마감 뒤 후보'), candidateBlock(LATE), candidateBlock(SHORT)];
    const outOfOrder = order.slice(1).flatMap((element, index) => (follows(order[index]!, element) ? [] : [index + 1]));
    expect(outOfOrder, 'positions not after the previous one').toEqual([]);
  });

  it('shows ID, title, status, owner, reason, source, prerequisite and BACKLOG.md:<line> of every candidate in one place', async () => {
    await renderLoaded();
    for (const candidate of CANDIDATES) expect(candidateBlock(candidate), candidate.id).toBeVisible();
  });

  it('shows a candidate that has no ## heading before it', async () => {
    const ungrouped: CandidateFields = { ...MENU, line: 5, id: 'ungrouped-candidate', title: '제목 앞 후보', group: null };
    readBacklog.mockResolvedValue({ ok: true, rows: [row(ungrouped)], issues: [], uncheckedLinks: [] });
    render(<DevelopmentBacklog />);
    await waitFor(() => expect(exactly('후보 1개 · 형식 오류 0 · 어긋남 0 · 확인 불가 0')).toHaveLength(1));
    expect(candidateBlock(ungrouped, [ungrouped])).toBeVisible();
  });
});

describe('problems by kind', () => {
  it('shows a row mismatch as 어긋남 with its cause and 고치는 방법: <fix> on that candidate', async () => {
    await renderLoaded();
    const shape = textOf(candidateBlock(SHAPE));
    const late = textOf(candidateBlock(LATE));
    for (const issue of [ID_FORMAT_ISSUE, LINK_MISSING_ISSUE]) {
      expect(shape, issue.code).toContain(issue.cause);
      expect(shape, issue.code).toContain(`고치는 방법: ${issue.fix}`);
    }
    expect(late).toContain(LINK_REJECTED_ISSUE.cause);
    expect(late).toContain(`고치는 방법: ${LINK_REJECTED_ISSUE.fix}`);
    const kindNames = (text: string) => ({ mismatch: text.includes('어긋남'), format: text.includes('형식 오류'), unchecked: text.includes('확인 불가') });
    expect({ shape: kindNames(shape), late: kindNames(late) }).toEqual({
      shape: { mismatch: true, format: false, unchecked: false },
      late: { mismatch: true, format: false, unchecked: false },
    });
  });

  it('shows a table-format problem on a candidate row as 형식 오류 with its cause and fix on that candidate', async () => {
    await renderLoaded();
    const short = textOf(candidateBlock(SHORT));
    expect(short).toContain(SHORT_ROW_ISSUE.cause);
    expect(short).toContain(`고치는 방법: ${TABLE_FORMAT_FIX}`);
    expect({ format: short.includes('형식 오류'), mismatch: short.includes('어긋남') }).toEqual({ format: true, mismatch: false });
  });

  it('shows an unchecked link as 확인 불가 with the link and 다시 읽기로 다시 확인하세요. on that candidate', async () => {
    await renderLoaded();
    const block = candidateBlock(MENU);
    const notice = innermostContaining(['확인 불가', STATE.recheck], block);
    expect(notice, 'one 확인 불가 notice on the candidate').toHaveLength(1);
    expect(textOf(notice[0]!)).toContain(UNCHECKED_LINK.link);
    expect({ mismatch: textOf(block).includes('어긋남'), format: textOf(block).includes('형식 오류') }).toEqual({ mismatch: false, format: false });
  });

  it('shows a table-format problem off the candidate rows in the 표 형식 오류 area above the candidates', async () => {
    await renderLoaded();
    const area = innermostContaining(['표 형식 오류', HEADER_ISSUE.cause, `고치는 방법: ${TABLE_FORMAT_FIX}`]);
    expect(area, '표 형식 오류 area').toHaveLength(1);
    const areaText = textOf(area[0]!);
    expect(CANDIDATES.filter(candidate => areaText.includes(candidate.id)).map(candidate => candidate.id), 'candidates inside the area').toEqual([]);
    expect(follows(area[0]!, candidateBlock(MENU)), 'area above the first candidate').toBe(true);
    const blocksWithHeaderIssue = CANDIDATES.filter(candidate => textOf(candidateBlock(candidate)).includes(HEADER_ISSUE.cause));
    expect(blocksWithHeaderIssue.map(candidate => candidate.id)).toEqual([]);
  });

  it('shows a problem with no line in the 표 형식 오류 area', async () => {
    const noTable = { code: 'BACKLOG_TABLE_FORMAT', line: null, cause: '첫 열이 ID인 후보 표가 없습니다.', fix: '머리글 첫 열이 ID인 후보 표와 구분 행을 두세요.' };
    readBacklog.mockResolvedValue({ ok: true, rows: [], issues: [noTable], uncheckedLinks: [] });
    render(<DevelopmentBacklog />);
    await waitFor(() => expect(exactly('후보 0개 · 형식 오류 1 · 어긋남 0 · 확인 불가 0')).toHaveLength(1));
    expect(innermostContaining(['표 형식 오류', noTable.cause, `고치는 방법: ${noTable.fix}`])).toHaveLength(1);
  });
});

describe('state sentences', () => {
  async function shownSentence(sentence: string) {
    await waitFor(() => expect(innermostContaining([sentence]).length).toBeGreaterThan(0));
    const [element] = innermostContaining([sentence]);
    expect(element).toBeVisible();
  }

  it('reports a missing bridge without reading', async () => {
    vi.stubGlobal('systemBacklog', undefined);
    render(<DevelopmentBacklog />);
    await shownSentence(STATE.noBridge);
  });

  it('reports a rejected invoke without showing the raw error', async () => {
    readBacklog.mockRejectedValue(new Error('ipc channel closed'));
    render(<DevelopmentBacklog />);
    await shownSentence(STATE.noAnswer);
    expect(textOf(document.body)).not.toContain('ipc channel closed');
  });

  it('shows the message of an ok: false result, without a refresh-failed note when nothing was read before', async () => {
    readBacklog.mockResolvedValue({ ok: false, code: 'too-large', message: 'BACKLOG.md가 읽기 크기 상한을 넘습니다.' });
    render(<DevelopmentBacklog />);
    await shownSentence('BACKLOG.md가 읽기 크기 상한을 넘습니다.');
    expect(textOf(document.body)).not.toContain(STATE.refreshFailed);
    expect(screen.getByRole('button', { name: '다시 읽기' })).toBeVisible();
  });

  it('keeps the previous candidates with 갱신 실패 when a re-read returns ok: false', async () => {
    const { user } = await renderLoaded();
    readBacklog.mockResolvedValue({ ok: false, code: 'invalid-encoding', message: 'BACKLOG.md가 UTF-8 문서가 아닙니다.' });
    await user.click(screen.getByRole('button', { name: '다시 읽기' }));
    await shownSentence(STATE.refreshFailed);
    await shownSentence('BACKLOG.md가 UTF-8 문서가 아닙니다.');
    for (const candidate of CANDIDATES) expect(candidateBlock(candidate), candidate.id).toBeVisible();
  });

  it('keeps the previous candidates with 갱신 실패 when a re-read invoke is rejected', async () => {
    const { user } = await renderLoaded();
    readBacklog.mockRejectedValue(new Error('ipc channel closed'));
    await user.click(screen.getByRole('button', { name: '다시 읽기' }));
    await shownSentence(STATE.refreshFailed);
    await shownSentence(STATE.noAnswer);
    for (const candidate of CANDIDATES) expect(candidateBlock(candidate), candidate.id).toBeVisible();
  });
});

describe('reading', () => {
  it('does not start another read while one is in progress, and reads again with 다시 읽기 afterwards', async () => {
    let answerFirst: (value: unknown) => void = () => {};
    readBacklog.mockReset()
      .mockImplementationOnce(() => new Promise(resolve => { answerFirst = resolve; }))
      .mockResolvedValue(RESULT);
    const user = userEvent.setup();
    render(<DevelopmentBacklog />);
    await waitFor(() => expect(readBacklog).toHaveBeenCalledTimes(1));
    await user.click(screen.getByRole('button', { name: '다시 읽기' }));
    const callsWhileReading = readBacklog.mock.calls.length;
    await act(async () => { answerFirst(RESULT); });
    await waitFor(() => expect(exactly(SUMMARY)).toHaveLength(1));
    await user.click(screen.getByRole('button', { name: '다시 읽기' }));
    await waitFor(() => expect(readBacklog).toHaveBeenCalledTimes(2));
    expect(callsWhileReading).toBe(1);
  });

  it('shows the latest read when an earlier read answers late (StrictMode re-runs the mount read)', async () => {
    const pending: ((value: unknown) => void)[] = [];
    readBacklog.mockReset().mockImplementation(() => new Promise(resolve => { pending.push(resolve); }));
    const earlier: CandidateFields = { ...MENU, id: 'earlier-answer', title: '먼저 부른 읽기' };
    const latest: CandidateFields = { ...MENU, id: 'latest-answer', title: '나중에 부른 읽기' };
    render(<StrictMode><DevelopmentBacklog /></StrictMode>);
    await waitFor(() => expect(readBacklog).toHaveBeenCalledTimes(2));
    await act(async () => { pending[1]!({ ok: true, rows: [row(latest)], issues: [], uncheckedLinks: [] }); });
    await act(async () => { pending[0]!({ ok: true, rows: [row(earlier)], issues: [], uncheckedLinks: [] }); });
    const shown = textOf(document.body);
    expect({ latest: shown.includes('latest-answer'), earlier: shown.includes('earlier-answer') }).toEqual({ latest: true, earlier: false });
  });
});

describe('read only', () => {
  it('has only the 다시 읽기 control: no input, selection, editing, disclosure or link element', async () => {
    const { container } = await renderLoaded();
    const editable = container.querySelectorAll('input, select, textarea, a, details, [contenteditable], [role="checkbox"], [role="link"], [role="textbox"]');
    expect([...editable].map(element => element.outerHTML)).toEqual([]);
    expect(screen.getAllByRole('button').map(button => textOf(button))).toEqual(['다시 읽기']);
  });

  it('shows cell Markdown as the literal text, without emphasis or link elements', async () => {
    const { container } = await renderLoaded();
    expect(container.querySelector('strong, em, a')).toBeNull();
    const shown = textOf(container);
    expect(shown).toContain(MENU.title);
    expect(shown).toContain(MENU.prerequisite);
    expect(shown).toContain(MENU.status);
  });
});

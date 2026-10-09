// Requirement: backlog-menu-design.md 「화면」 글자색 and 「시험」 글자 대비 회귀 시험 (main msg_6bae84d02fa1,
// PR3 verdict defect #1): every readable text on the 「이후 작업」 tab reaches WCAG 2 contrast 4.5:1, or
// 3:1 for large text, against its actual background, and every text kind the tab shows is measured.
// The DOM is the real App rendered here with the tab opened and window.systemBacklog faked with a
// fixed BacklogResult. The cascade, var() and colors are computed by a real Chromium renderer
// (tests/renderer-contrast/): jsdom does not resolve this app's cascade and var() like the
// renderer, so no contrast is read from jsdom.
//
// Environment: BACKLOG_CONTRAST_FRONTEND_ROOT takes the stylesheets (src/main.tsx and its CSS) from
// another frontend root, such as a TEMP copy with a candidate fix; the DOM is always this
// checkout's App. BACKLOG_CONTRAST_RAW_FILE (absolute) receives a copy of every measured row as JSON.
import '@testing-library/jest-dom/vitest';
import { cleanup, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { createElement, StrictMode } from 'react';
import { dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { describe, expect, it, vi } from 'vitest';
import type { BacklogBridge, BacklogIssue, BacklogResult } from '../electron/backlog-contract';
import type { BacklogRow } from '../electron/backlog-table';
import App from '../src/App';
import { measureTextContrast, type ContrastPage, type ContrastRow, type ContrastScene } from './renderer-contrast/text-contrast';

type CandidateFields = Omit<BacklogRow, 'cells' | 'goalLinks'>;

// cells and goalLinks are the table parser's view of the same row; the screen shows the named fields.
function candidate(fields: CandidateFields): BacklogRow {
  return { ...fields, cells: [`\`${fields.id}\``, fields.title, fields.reason, fields.source, fields.prerequisite, fields.owner, fields.status], goalLinks: [] };
}

// A candidate from a table before the first ## heading (group null) is shown first, without a title.
const HEADINGLESS = candidate({
  line: 6, id: 'headingless-candidate', title: '첫 제목 앞 표의 후보', reason: '첫 묶음 제목 앞 표에 있다',
  source: '사용자 요청 2026-10-01', prerequisite: '없음', owner: 'Management', status: '대기', group: null,
});
const SHORT_ROW = candidate({
  line: 14, id: 'short-row-candidate', title: '열이 모자란 후보', reason: '표 열 수를 확인한다',
  source: '메인 결정 msg_000000000001', prerequisite: '없음', owner: 'Rules', status: '대기', group: '마감 전 후보',
});
const MISSING_GOAL = candidate({
  line: 15, id: 'missing-goal-candidate', title: '없는 goal을 가리키는 후보', reason: '승격 링크를 확인한다',
  source: 'Rules 보고', prerequisite: 'PR3 병합', owner: 'Core', status: 'goal 승격 → [없는 goal](../../01_Phases/goals/2026-10-09-none/goal.md)', group: '마감 전 후보',
});
const UNCHECKED_ROW = candidate({
  line: 22, id: 'unchecked-link-candidate', title: '확인하지 못한 링크의 후보', reason: '링크 확인 중 오류가 났다',
  source: 'Management 관측', prerequisite: '없음', owner: 'Management', status: '진행 → [확인할 goal](../../01_Phases/goals/2026-10-07-pending/goal.md)', group: '마감 뒤 후보',
});
const CANDIDATES = [HEADINGLESS, SHORT_ROW, MISSING_GOAL, UNCHECKED_ROW];
const GROUP_TITLES = ['마감 전 후보', '마감 뒤 후보'];

// Line 10 is not a candidate row, so this problem is shown in the 「표 형식 오류」 area. The two row
// problems give one of each kind name: a table-format code shows 「형식 오류」, a goal-link code 「어긋남」.
const TABLE_AREA_ISSUE: BacklogIssue = { code: 'BACKLOG_TABLE_FORMAT', line: 10, cause: '머리글 아래 구분 행의 열 수가 머리글과 다릅니다.', fix: 'ID 표의 머리글과 구분 행의 열 수를 맞추세요.' };
const ROW_FORMAT_ISSUE: BacklogIssue = { code: 'BACKLOG_TABLE_FORMAT', line: 14, cause: '표의 열 수가 머리글 7개보다 적습니다.', fix: '빠진 열을 채우세요.' };
const ROW_MISMATCH_ISSUE: BacklogIssue = { code: 'BACKLOG_GOAL_LINK_MISSING', line: 15, cause: '상대 goal 링크의 대상 파일이 없습니다.', fix: 'BACKLOG.md 기준 상대경로와 대상 goal 파일을 확인하세요.' };
const UNCHECKED_LINK = { line: 22, link: '../../01_Phases/goals/2026-10-07-pending/goal.md' };

const LOADED: BacklogResult = { ok: true, rows: CANDIDATES, issues: [TABLE_AREA_ISSUE, ROW_FORMAT_ISSUE, ROW_MISMATCH_ISSUE], uncheckedLinks: [UNCHECKED_LINK] };
const FAILED_READ: BacklogResult = { ok: false, code: 'load', message: 'BACKLOG.md를 읽지 못했습니다. 다시 읽으세요.' };

// Fixed sentences of the design's 「화면」 section.
const READ_NOTICE = '백로그 파일을 읽었습니다. 자동 갱신은 하지 않습니다.';
const REFRESH_FAILED_NOTICE = '갱신 실패: 이전에 읽은 백로그를 표시합니다.';
const RECHECK_NOTICE = '다시 읽기로 다시 확인하세요.';
const FIX_PREFIX = '고치는 방법: ';

type SceneName = 'loaded' | 'refresh-failed';
const BOTH: readonly SceneName[] = ['loaded', 'refresh-failed'];

interface TextKind {
  name: string;
  scenes: readonly SceneName[];
  matches(row: { selector: string; text: string }): boolean;
}

const tagOf = (row: { selector: string }) => row.selector.split('.')[0];

// Every text kind the tab shows. A row takes the first kind that matches, so specific kinds come
// before the general ones (the headingless candidate before 후보 ID/제목, table-area problem before 원인).
const TEXT_KINDS: readonly TextKind[] = [
  { name: '읽음 안내', scenes: ['loaded'], matches: row => row.text === READ_NOTICE },
  { name: '읽기 실패 문장', scenes: ['refresh-failed'], matches: row => !FAILED_READ.ok && row.text === FAILED_READ.message },
  { name: '「갱신 실패」 안내', scenes: ['refresh-failed'], matches: row => row.text === REFRESH_FAILED_NOTICE },
  { name: '「다시 읽기」 버튼', scenes: BOTH, matches: row => tagOf(row) === 'button' && row.text === '다시 읽기' },
  { name: '요약', scenes: BOTH, matches: row => /^후보 \d+개 · 형식 오류 \d+ · 어긋남 \d+ · 확인 불가 \d+$/.test(row.text) },
  { name: '「표 형식 오류」 영역 제목 h3', scenes: BOTH, matches: row => tagOf(row) === 'h3' && row.text === '표 형식 오류' },
  { name: '묶음 제목 h3', scenes: BOTH, matches: row => tagOf(row) === 'h3' && GROUP_TITLES.includes(row.text) },
  { name: '「표 형식 오류」 영역의 문제', scenes: BOTH, matches: row => row.text === TABLE_AREA_ISSUE.cause || row.text === FIX_PREFIX + TABLE_AREA_ISSUE.fix },
  { name: 'group 없는 후보', scenes: BOTH, matches: row => row.text === HEADINGLESS.id || row.text === HEADINGLESS.title },
  { name: '후보 ID', scenes: BOTH, matches: row => CANDIDATES.some(item => item.id === row.text) },
  { name: '후보 제목 h4', scenes: BOTH, matches: row => tagOf(row) === 'h4' && CANDIDATES.some(item => item.title === row.text) },
  { name: '라벨 dt', scenes: BOTH, matches: row => tagOf(row) === 'dt' },
  { name: '위치 dd', scenes: BOTH, matches: row => tagOf(row) === 'dd' && /^BACKLOG\.md:\d+$/.test(row.text) },
  { name: '값 dd', scenes: BOTH, matches: row => tagOf(row) === 'dd' },
  { name: '문제 종류 「형식 오류」', scenes: BOTH, matches: row => row.text === '형식 오류' },
  { name: '문제 종류 「어긋남」', scenes: BOTH, matches: row => row.text === '어긋남' },
  { name: '「확인 불가」 표시', scenes: BOTH, matches: row => row.text === '확인 불가' },
  { name: '원인', scenes: BOTH, matches: row => row.text === ROW_FORMAT_ISSUE.cause || row.text === ROW_MISMATCH_ISSUE.cause },
  { name: '「고치는 방법」', scenes: BOTH, matches: row => row.text.startsWith(FIX_PREFIX) },
  { name: '확인 불가 링크', scenes: BOTH, matches: row => row.text === UNCHECKED_LINK.link },
  { name: '다시 읽기 안내', scenes: BOTH, matches: row => row.text === RECHECK_NOTICE },
];

const kindOf = (row: { selector: string; text: string }) => TEXT_KINDS.find(kind => kind.matches(row))?.name ?? null;

// The reading has settled when the notice is shown and 「다시 읽기」 is enabled again (a disabled
// button is drawn at half opacity, which is not the state a person reads).
async function settledOn(notice: string) {
  await waitFor(() => {
    expect(screen.getByText(notice)).toBeVisible();
    expect(screen.getByRole('button', { name: '다시 읽기' })).toBeEnabled();
  });
  return screen.getByRole('button', { name: '다시 읽기' });
}

// Renders App as src/main.tsx does (inside StrictMode), opens 개발 현황 → 이후 작업, and keeps the
// markup of two scenes: the loaded backlog, then a failed re-read that keeps the previous candidates.
async function backlogScenes(): Promise<ContrastPage[]> {
  const readBacklog = vi.fn<BacklogBridge['readBacklog']>().mockResolvedValue(LOADED);
  vi.stubGlobal('systemBacklog', { readBacklog } satisfies BacklogBridge);
  try {
    const user = userEvent.setup();
    const view = render(createElement(StrictMode, null, createElement(App)));
    await user.click(within(screen.getByRole('navigation', { name: '관리 영역' })).getByRole('button', { name: /개발 현황/ }));
    await user.click(screen.getByRole('button', { name: '이후 작업' }));
    const reread = await settledOn(READ_NOTICE);
    const loaded = view.container.innerHTML;
    readBacklog.mockResolvedValue(FAILED_READ);
    await user.click(reread);
    await settledOn(REFRESH_FAILED_NOTICE);
    const refreshFailed = view.container.innerHTML;
    return [{ name: 'loaded', rootHtml: loaded }, { name: 'refresh-failed', rootHtml: refreshFailed }];
  } finally {
    cleanup();
    vi.unstubAllGlobals();
  }
}

// Joined from the file path: under jsdom the global URL resolves relative URLs against http://localhost.
const FRONTEND_ROOT = dirname(dirname(fileURLToPath(import.meta.url)));

// Both tests read one measurement. A launch or page failure rejects it, so each test fails with
// the 「실행 못 함」 message instead of being skipped or passing. Each test allows 120 s: the jsdom
// render plus the 60 s Electron limit.
let measurement: Promise<ContrastScene[]> | undefined;
function measuredScenes(): Promise<ContrastScene[]> {
  measurement ??= backlogScenes().then(pages => measureTextContrast({
    frontendRoot: process.env.BACKLOG_CONTRAST_FRONTEND_ROOT ?? FRONTEND_ROOT,
    pages,
    scopeSelector: '.backlog-browser',
    timeoutMs: 60_000,
    classify: kindOf,
    rawCopyFile: process.env.BACKLOG_CONTRAST_RAW_FILE,
  }));
  return measurement;
}

// Floor, not round, so a failing 4.496 is not printed as 4.50.
const shownRatio = (ratio: number) => (Math.floor(ratio * 100) / 100).toFixed(2);

function describeFailure(row: ContrastRow): string {
  const where = `${row.scene} · ${row.selector} · "${row.text.slice(0, 40)}"`;
  if (row.ratio === null) return `${where} · 측정 불가: ${row.problem}`;
  return `${where} · 글자 ${row.color} · 배경 ${row.background} · ${shownRatio(row.ratio)}:1 < ${row.required}:1 · ${row.fontSizePx}px/${row.fontWeight}`;
}

const CONTRAST_FIX = '읽는 글자는 실제 배경 위에서 4.5:1(큰 글자 3:1) 이상이어야 한다. 고정 색 대신 밝은 테마 변수(--text·--muted·--warning)를 쓴다(backlog-menu-design.md 「화면」 글자색).';

describe('「이후 작업」 tab text contrast in the Chromium renderer', () => {
  it('measures at least one visible element for every text kind, in the loaded and the failed re-read scenes', async () => {
    const scenes = await measuredScenes();
    const missingKinds = scenes.flatMap(scene => TEXT_KINDS
      .filter(kind => kind.scenes.includes(scene.name as SceneName))
      .filter(kind => !scene.rows.some(row => row.kind === kind.name))
      .map(kind => `${scene.name}: ${kind.name}`));
    expect(scenes.map(scene => scene.name), 'measured scenes').toEqual(BOTH);
    expect(missingKinds, '측정 대상이 0개인 글자 종류').toEqual([]);
  }, 120_000);

  it('keeps every visible text at 4.5:1 or more over its composited background (3:1 for large text)', async () => {
    const scenes = await measuredScenes();
    const unmeasuredScenes = scenes.filter(scene => !scene.scopeVisible || scene.rows.length === 0).map(scene => scene.name);
    const failingRows = scenes.flatMap(scene => scene.rows.filter(row => !row.passes)).map(describeFailure);
    expect(unmeasuredScenes, '탭이 보이지 않거나 측정한 글자가 0개인 장면').toEqual([]);
    expect(failingRows, CONTRAST_FIX).toEqual([]);
  }, 120_000);
});

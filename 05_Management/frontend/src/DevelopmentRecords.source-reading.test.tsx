// Requirement: goal 「만들 것」 2·3 and 완료조건 2·3 (read-only records tab: source sections read in
// the app, the read checkout shown, broken links never shown as current fact, no editing) and
// index-v2-design.md 「화면」 with Astra msg_28ce06a18c04 Q4-Q6 (tab 「개발 기록」, view 「출처 N」,
// alert = 「끊긴 링크: 」 + the failure message). The bridge is a test double with readCatalog,
// readSection and readCheckout; the index is the v2 screen fixture, not the real catalog.json.
// Expected strings are the fixed wording of the design, written out here.
import '@testing-library/jest-dom/vitest';
import { act, cleanup, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { MERGE_COMMIT, SCREEN_COMMIT_B, screenCatalog } from '../tests/record-sources/catalog-v2-fixture';
import App from './App';
import DevelopmentRecords from './DevelopmentRecords';

const catalog = screenCatalog();
const VERSION = '9a8b7c6d5e4f3a2b1c0d9e8f7a6b5c4d3e2f1a0b9c8d7e6f5a4b3c2d1e0f9a8b';
const HEAD = '0123456789abcdef0123456789abcdef01234567';
const DESIGN_TEXT = '# 기록 설계\n**굵게** 표시와 `코드`는 글자 그대로다.\n- 목록 줄\n';
const COMBAT_TEXT = '## 전투 흐름\n즉시 피해 뒤 사망을 처리한다.\n';

function section(sourceId: string, path: string, heading: string | null, text: string) {
  return { ok: true, sourceId, path, heading, text, bytes: new TextEncoder().encode(text).byteLength };
}
const SECTIONS: Record<string, unknown> = {
  'records-design': section('records-design', 'docs/records.md', null, DESIGN_TEXT),
  'combat-guide': section('combat-guide', 'docs/combat.md', '전투 흐름', COMBAT_TEXT),
};

const readCatalog = vi.fn<() => Promise<unknown>>();
const readSection = vi.fn<(sourceId: unknown) => Promise<unknown>>();
const readCheckout = vi.fn<() => Promise<unknown>>();

beforeEach(() => {
  readCatalog.mockReset().mockResolvedValue({ ok: true, catalog, version: VERSION });
  readSection.mockReset().mockImplementation(async sourceId => SECTIONS[String(sourceId)]);
  readCheckout.mockReset().mockResolvedValue({ ok: true, checkout: { state: 'known', branch: 'feat/record-index', head: HEAD } });
  vi.stubGlobal('systemRecords', { readCatalog, readSection, readCheckout });
  Element.prototype.scrollIntoView = vi.fn();
});
afterEach(() => { cleanup(); vi.unstubAllGlobals(); });

async function openRecordsInApp() {
  const user = userEvent.setup();
  render(<App />);
  await user.click(screen.getByRole('button', { name: '3 개발 현황' }));
  await user.click(screen.getByRole('button', { name: '개발 기록' }));
  await screen.findByRole('searchbox', { name: '검색' });
  return user;
}

async function renderRecords() {
  const user = userEvent.setup();
  const view = render(<DevelopmentRecords />);
  await screen.findByRole('searchbox', { name: '검색' });
  return { user, container: view.container };
}

// The innermost element whose text starts with `prefix`, so a line split into spans still reads whole.
function lineStartingWith(prefix: string): string {
  const matches = screen.getAllByText((_content, element) => {
    const text = element?.textContent ?? '';
    return text.startsWith(prefix) && !Array.from(element?.children ?? []).some(child => (child.textContent ?? '').startsWith(prefix));
  });
  expect(matches).toHaveLength(1);
  return (matches[0]?.textContent ?? '').replace(/\s+/g, ' ').trim();
}

function listButton(listName: string, title: string): HTMLElement {
  const item = within(screen.getByRole('list', { name: listName })).getByText(title, { exact: true }).closest('button');
  if (!item) throw new Error(`${title} is not a list button`);
  return item;
}

function viewButton(label: '시스템' | '변경·결정·검증·계획' | '출처') {
  return screen.getByRole('button', { name: new RegExp(`^${label}\\s*\\d+$`) });
}

describe('read-only records tab', () => {
  it('names the development view tab 개발 기록', async () => {
    await openRecordsInApp();
    expect(screen.getByRole('button', { name: '개발 기록' })).toHaveAttribute('aria-pressed', 'true');
    expect(screen.queryByRole('button', { name: /기록 편집/ })).not.toBeInTheDocument();
  });

  it('has no editing, import, validation, save or draft controls, no text box and no file input', async () => {
    const { container } = await renderRecords();
    const buttonNames = screen.queryAllByRole('button').map(button => button.textContent ?? '');
    expect(buttonNames.filter(name => /편집|불러오기|초안|저장|취소/.test(name))).toEqual([]);
    expect(screen.queryByRole('textbox')).not.toBeInTheDocument();
    expect(container.querySelector('textarea')).toBeNull();
    expect(container.querySelector('input[type="file"]')).toBeNull();
    expect(screen.queryByText(/^기록 편집/)).not.toBeInTheDocument();
  });
});

describe('top bar and snapshot line', () => {
  it('shows the read checkout as branch and the first 12 HEAD digits', async () => {
    await renderRecords();
    await waitFor(() => expect(lineStartingWith('읽은 checkout: ')).toBe('읽은 checkout: feat/record-index · HEAD 0123456789ab'));
    expect(readCheckout).toHaveBeenCalled();
    expect(screen.getByRole('button', { name: '기록 새로고침' })).toBeVisible();
  });

  it('shows a detached HEAD as 분리된 HEAD', async () => {
    readCheckout.mockResolvedValue({ ok: true, checkout: { state: 'known', branch: null, head: HEAD } });
    await renderRecords();
    await waitFor(() => expect(lineStartingWith('읽은 checkout: ')).toBe('읽은 checkout: 분리된 HEAD · HEAD 0123456789ab'));
  });

  it('shows 알 수 없음 when the checkout is unknown or cannot be read', async () => {
    for (const result of [{ ok: true, checkout: { state: 'unknown', reason: 'no-git' } }, { ok: false, code: 'denied', message: '권한 없음' }]) {
      readCheckout.mockResolvedValue(result);
      await renderRecords();
      await waitFor(() => expect(lineStartingWith('읽은 checkout: ')).toBe('읽은 checkout: 알 수 없음'));
      cleanup();
    }
  });

  it('shows the index version as 기록 색인 버전 with the first 12 hash digits and no date, commit or scope note', async () => {
    await renderRecords();
    expect(lineStartingWith('기록 색인 버전 ')).toBe(`기록 색인 버전 ${VERSION.slice(0, 12)}`);
    expect(screen.queryByText(/기준일/)).not.toBeInTheDocument();
    expect(screen.queryByText(/소스 commit/)).not.toBeInTheDocument();
  });
});

describe('lists without status wording', () => {
  it('lists systems by area and title and records by type and title only', async () => {
    const { user } = await renderRecords();
    const rest = (button: HTMLElement, parts: string[]) => parts.reduce((text, part) => text.replace(part, ''), button.textContent ?? '').replace(/[\s·|—-]/g, '');
    for (const system of catalog.systems) expect(rest(listButton('시스템 목록', system.title), [system.area, system.title]), system.id).toBe('');
    await user.click(viewButton('변경·결정·검증·계획'));
    for (const record of catalog.records) expect(rest(listButton('기록 목록', record.title), [record.type, record.title]), record.id).toBe('');
  });

  it('shows each source with title, path and section, readable ones as 원문 읽기 buttons and the rest as text', async () => {
    const { user, container } = await renderRecords();
    await user.click(viewButton('출처'));
    const browser = container;
    for (const source of catalog.sources) {
      expect(browser, source.id).toHaveTextContent(source.title);
      expect(browser, source.id).toHaveTextContent(source.locator);
      if (source.section) expect(browser, source.id).toHaveTextContent(source.section);
    }
    expect(screen.getByRole('button', { name: '전투 흐름 안내 원문 읽기' })).toBeVisible();
    expect(screen.getByRole('button', { name: '기록 설계 전체 원문 읽기' })).toBeVisible();
    expect(browser).toHaveTextContent('전투 맵 코드 · 앱에서 읽지 않음(Markdown 아님)');
    expect(browser).toHaveTextContent('로컬 실행 기록 · 앱에서 읽지 않음(로컬 전용)');
    expect(browser).toHaveTextContent('메인 결정 메시지 · 앱에서 읽지 않음(전달 메시지)');
    for (const title of ['전투 맵 코드', '로컬 실행 기록', '메인 결정 메시지']) {
      expect(screen.queryByRole('button', { name: new RegExp(`^${title}`) }), title).not.toBeInTheDocument();
    }
  });
});

describe('search targets', () => {
  async function search(user: ReturnType<typeof userEvent.setup>, query: string) {
    const box = screen.getByRole('searchbox', { name: '검색' });
    await user.clear(box);
    await user.type(box, query);
  }
  const titlesIn = (listName: string) => within(screen.getByRole('list', { name: listName })).queryAllByRole('button').map(button => button.textContent ?? '');

  it('finds systems by ID, title, area and linked source title', async () => {
    const { user } = await renderRecords();
    const expectations: [string, string[]][] = [
      ['management-records', ['개발 기록 열람']],
      ['피해·사망', ['전투·피해·사망 처리']],
      ['게임 플레이', ['전투·피해·사망 처리']],
      ['전투 흐름 안내', ['전투·피해·사망 처리', 'DB·캐릭터 보관과 복원']],
    ];
    for (const [query, titles] of expectations) {
      await search(user, query);
      const shown = titlesIn('시스템 목록');
      expect(titles.map(title => shown.some(text => text.includes(title))), query).toEqual(titles.map(() => true));
      expect(shown, query).toHaveLength(titles.length);
    }
  });

  it('finds records by ID, title, #PR number and linked source title', async () => {
    const { user } = await renderRecords();
    await user.click(viewButton('변경·결정·검증·계획'));
    const expectations: [string, string][] = [
      ['combat-change', '즉시 피해 처리 통합'],
      ['백로그 메뉴', '백로그 메뉴 계획'],
      ['#197', '색인 형식 전환'],
      ['메인 결정 메시지', '색인 형식 전환'],
    ];
    for (const [query, title] of expectations) {
      await search(user, query);
      const shown = titlesIn('기록 목록');
      expect(shown, query).toHaveLength(1);
      expect(shown[0], query).toContain(title);
    }
  });

  it('finds sources by ID, title, path and section', async () => {
    const { user } = await renderRecords();
    await user.click(viewButton('출처'));
    const expectations: [string, string][] = [
      ['records-run-log', '로컬 실행 기록'],
      ['기록 설계', '기록 설계 전체'],
      ['GameMap.cs', '전투 맵 코드'],
      ['전투 흐름', '전투 흐름 안내'],
    ];
    for (const [query, title] of expectations) {
      await search(user, query);
      expect(screen.getByRole('searchbox', { name: '검색' })).toHaveValue(query);
      const others = catalog.sources.filter(item => item.title !== title).map(item => item.title);
      const browser = document.body;
      expect(browser, query).toHaveTextContent(title);
      expect(others.filter(other => (browser.textContent ?? '').includes(other)), query).toEqual([]);
    }
  });
});

describe('details', () => {
  it('shows a system with area, related systems, linked records and an always-open 원문 list', async () => {
    const { user } = await renderRecords();
    await user.click(listButton('시스템 목록', '개발 기록 열람'));
    const detail = screen.getByRole('region', { name: '개발 기록 열람' });
    for (const label of ['Management', '관련 시스템', '연결 기록', '원문']) expect(within(detail).getAllByText(label, { exact: true })[0], label).toBeVisible();
    expect(within(detail).getByRole('button', { name: /^전투·피해·사망 처리/ })).toBeVisible();
    expect(within(detail).getByRole('button', { name: /^색인 형식 전환/ })).toBeVisible();
    expect(within(detail).getByRole('button', { name: /^백로그 메뉴 계획/ })).toBeVisible();
    expect(within(detail).getByRole('button', { name: '기록 설계 전체 원문 읽기' })).toBeVisible();
    expect(detail).toHaveTextContent('로컬 실행 기록 · 앱에서 읽지 않음(로컬 전용)');
    expect(detail).toHaveTextContent('.backups/run/log.md');
    expect(detail).toHaveTextContent('메인 결정 메시지 · 앱에서 읽지 않음(전달 메시지)');
    expect(detail).toHaveTextContent('msg_0123456789ab');
    expect(within(detail).queryByText(/^근거\s*\d+개$/)).not.toBeInTheDocument();
    expect(detail.querySelector('details')).toBeNull();
    for (const removed of ['구현', '통합', '책임', '동작', '검증 한계', '다음 일']) {
      expect(within(detail).queryByText(removed, { exact: true }), removed).not.toBeInTheDocument();
    }
  });

  it('marks a non-Markdown git source as not read in the app, with its path', async () => {
    const { user } = await renderRecords();
    await user.click(listButton('시스템 목록', '전투·피해·사망 처리'));
    const detail = screen.getByRole('region', { name: '전투·피해·사망 처리' });
    expect(detail).toHaveTextContent('전투 맵 코드 · 앱에서 읽지 않음(Markdown 아님)');
    expect(detail).toHaveTextContent('02_Server/GameServer/Maps/GameMap.cs');
    expect(within(detail).getByRole('button', { name: '전투 흐름 안내 원문 읽기' })).toBeVisible();
  });

  it('shows a record with type, one PR line per pull request, linked systems and 원문', async () => {
    const { user } = await renderRecords();
    await user.click(viewButton('변경·결정·검증·계획'));
    await user.click(listButton('기록 목록', '색인 형식 전환'));
    const detail = screen.getByRole('region', { name: '색인 형식 전환' });
    expect(within(detail).getAllByText('변경', { exact: true })[0]).toBeVisible();
    expect(detail).toHaveTextContent(`PR #196 · 병합 ${MERGE_COMMIT.slice(0, 7)}`);
    expect(detail).toHaveTextContent(`PR #197 · 병합 ${SCREEN_COMMIT_B.slice(0, 7)}`);
    expect(within(detail).getAllByText('연결 시스템', { exact: true })[0]).toBeVisible();
    expect(within(detail).getByRole('button', { name: /^개발 기록 열람/ })).toBeVisible();
    expect(within(detail).getByRole('button', { name: '기록 설계 전체 원문 읽기' })).toBeVisible();
  });

  it('shows no PR line for a record without pull requests', async () => {
    const { user } = await renderRecords();
    await user.click(viewButton('변경·결정·검증·계획'));
    await user.click(listButton('기록 목록', '백로그 메뉴 계획'));
    expect(screen.getByRole('region', { name: '백로그 메뉴 계획' })).not.toHaveTextContent('PR #');
  });
});

describe('reading a source section', () => {
  it('goes list → system → source and back, restoring focus at each step', async () => {
    const user = await openRecordsInApp();
    await user.click(listButton('시스템 목록', '개발 기록 열람'));
    const open = screen.getByRole('button', { name: '기록 설계 전체 원문 읽기' });
    await user.click(open);
    const body = await screen.findByLabelText('원문 구간');
    expect(readSection.mock.calls).toEqual([['records-design']]);
    expect(screen.getByRole('heading', { name: '기록 설계 전체' })).toBeVisible();
    expect(metaContains('docs/records.md')).toBe(true);
    expect(metaContains('파일 처음부터')).toBe(true);
    expect(body.tagName).toBe('PRE');
    expect(body.textContent).toBe(DESIGN_TEXT);
    expect(body.children).toHaveLength(0);

    await user.click(screen.getByRole('button', { name: '← 뒤로' }));
    expect(screen.getByRole('region', { name: '개발 기록 열람' })).toBeVisible();
    expect(screen.getByRole('button', { name: '기록 설계 전체 원문 읽기' })).toHaveFocus();
    await user.click(screen.getByRole('button', { name: '← 뒤로' }));
    expect(listButton('시스템 목록', '개발 기록 열람')).toHaveFocus();
  });

  it('shows the section heading as meta for a source with a section, without turning Markdown into HTML', async () => {
    const { user } = await renderRecords();
    await user.click(listButton('시스템 목록', '전투·피해·사망 처리'));
    await user.click(screen.getByRole('button', { name: '전투 흐름 안내 원문 읽기' }));
    const body = await screen.findByLabelText('원문 구간');
    expect(metaContains('docs/combat.md')).toBe(true);
    expect(metaContains('전투 흐름')).toBe(true);
    expect(body.textContent).toBe(COMBAT_TEXT);
    expect(body.children).toHaveLength(0);
  });

  it('opens the same source screen from the 출처 list view and returns to it', async () => {
    const { user } = await renderRecords();
    await user.click(viewButton('출처'));
    await user.click(screen.getByRole('button', { name: '기록 설계 전체 원문 읽기' }));
    expect((await screen.findByLabelText('원문 구간')).textContent).toBe(DESIGN_TEXT);
    expect(screen.getByRole('heading', { name: '기록 설계 전체' })).toBeVisible();
    expect(readSection).toHaveBeenLastCalledWith('records-design');
    await user.click(screen.getByRole('button', { name: '← 뒤로' }));
    expect(screen.getByRole('button', { name: '기록 설계 전체 원문 읽기' })).toHaveFocus();
  });

  it('shows a failed read as 끊긴 링크 with the failure message and never the previously read body', async () => {
    readSection.mockImplementation(async sourceId => sourceId === 'combat-guide'
      ? { ok: false, code: 'missing', reason: null, message: '원문 파일을 찾을 수 없습니다.' }
      : SECTIONS[String(sourceId)]);
    const { user } = await renderRecords();
    await user.click(listButton('시스템 목록', '개발 기록 열람'));
    await user.click(screen.getByRole('button', { name: '기록 설계 전체 원문 읽기' }));
    await screen.findByLabelText('원문 구간');
    await user.click(screen.getByRole('button', { name: '← 뒤로' }));
    await user.click(screen.getByRole('button', { name: '← 뒤로' }));
    await user.click(listButton('시스템 목록', '전투·피해·사망 처리'));
    await user.click(screen.getByRole('button', { name: '전투 흐름 안내 원문 읽기' }));

    const alert = await screen.findByRole('alert');
    expect(alert.textContent?.replace(/\s+/g, ' ').trim()).toBe('끊긴 링크: 원문 파일을 찾을 수 없습니다.');
    expect(screen.queryByLabelText('원문 구간')).not.toBeInTheDocument();
    expect(document.body.textContent).not.toContain('글자 그대로다');
  });

  it('ignores a late result of an earlier request on the current source screen', async () => {
    const pending: ((value: unknown) => void)[] = [];
    readSection.mockImplementation(sourceId => sourceId === 'records-design'
      ? new Promise(resolve => { pending.push(resolve); })
      : Promise.resolve(SECTIONS[String(sourceId)]));
    const { user } = await renderRecords();
    await user.click(listButton('시스템 목록', '개발 기록 열람'));
    await user.click(screen.getByRole('button', { name: '기록 설계 전체 원문 읽기' }));
    await user.click(screen.getByRole('button', { name: '← 뒤로' }));
    await user.click(screen.getByRole('button', { name: '← 뒤로' }));
    await user.click(listButton('시스템 목록', '전투·피해·사망 처리'));
    await user.click(screen.getByRole('button', { name: '전투 흐름 안내 원문 읽기' }));
    expect((await screen.findByLabelText('원문 구간')).textContent).toBe(COMBAT_TEXT);

    await act(async () => { pending.forEach(resolve => resolve(SECTIONS['records-design'])); });
    expect(screen.getByLabelText('원문 구간').textContent).toBe(COMBAT_TEXT);
    expect(screen.getByRole('heading', { name: '전투 흐름 안내' })).toBeVisible();
    expect(document.body.textContent).not.toContain('글자 그대로다');
  });

  it('does not show a late result after leaving the source screen', async () => {
    const pending: ((value: unknown) => void)[] = [];
    readSection.mockImplementation(() => new Promise(resolve => { pending.push(resolve); }));
    const { user } = await renderRecords();
    await user.click(listButton('시스템 목록', '개발 기록 열람'));
    await user.click(screen.getByRole('button', { name: '기록 설계 전체 원문 읽기' }));
    await user.click(screen.getByRole('button', { name: '← 뒤로' }));
    await act(async () => { pending.forEach(resolve => resolve(SECTIONS['records-design'])); });
    expect(screen.getByRole('region', { name: '개발 기록 열람' })).toBeVisible();
    expect(screen.queryByLabelText('원문 구간')).not.toBeInTheDocument();
  });
});

// True when an element outside headings and the section body shows the text (path and section meta).
function metaContains(text: string): boolean {
  return screen.queryAllByText((_content, element) => {
    if (!element || element.closest('pre, h1, h2, h3, h4, h5, h6')) return false;
    const own = element.textContent ?? '';
    return own.includes(text) && !Array.from(element.children).some(child => (child.textContent ?? '').includes(text));
  }).length > 0;
}

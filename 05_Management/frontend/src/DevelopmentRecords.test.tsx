// Requirement: the development records tab loads through the runtime bridge, filters and opens
// details by keyboard, and reports a missing bridge instead of claiming records loaded. Since the
// record index v2 (goal 2026-10-06-record-source-unification 「만들 것」 1·3, index-v2-design.md
// 「화면」), the tab is read only and named 「개발 기록」, sources are listed as 「원문」 entries, and the
// index fixture is the v2 screen fixture instead of the real catalog.json. The draft editing tests
// were removed with the editor (「만들 것」 3).
import '@testing-library/jest-dom/vitest';
import { cleanup, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { screenCatalog } from '../tests/record-sources/catalog-v2-fixture';
import App from './App';

const data = screenCatalog();
const initial = { ok: true, catalog: data, version: 'a'.repeat(64) };
const read = vi.fn<() => Promise<unknown>>();
const readSection = vi.fn<(sourceId: unknown) => Promise<unknown>>();
const readCheckout = vi.fn<() => Promise<unknown>>();
beforeEach(() => {
  read.mockReset().mockResolvedValue(initial);
  readSection.mockReset().mockResolvedValue({ ok: false, code: 'missing', reason: null, message: '원문 파일을 찾을 수 없습니다.' });
  readCheckout.mockReset().mockResolvedValue({ ok: true, checkout: { state: 'unknown', reason: 'no-git' } });
  vi.stubGlobal('systemRecords', { readCatalog: read, readSection, readCheckout });
  Element.prototype.scrollIntoView = vi.fn();
});
afterEach(() => { cleanup(); vi.unstubAllGlobals(); });
async function enter() {
  const user = userEvent.setup(); render(<App />);
  await user.click(screen.getByRole('button', { name: '3 개발 현황' }));
  // 개발 현황은 시스템 카드 탭으로 열린다. 개발 기록은 그 옆 탭을 열어야 보인다.
  await user.click(screen.getByRole('button', { name: '개발 기록' }));
  await screen.findByRole('searchbox', { name: '검색' });
  return user;
}

describe('system discovery and evidence', () => {
  it('loads through the runtime bridge and filters with empty results and keyboard activation', async () => {
    const user = await enter();
    // App은 시스템 카드와 개발 기록 화면을 모두 마운트해 두고, 두 화면이 각각 한 번씩 기록을 읽는다.
    expect(read).toHaveBeenCalledTimes(2);
    expect(screen.getByText('읽은 스냅샷 · 게임 실시간 연동 아님')).toBeVisible();
    const search = screen.getByRole('searchbox', { name: '검색' });
    await user.type(search, '피해·사망');
    expect(screen.getByText('1개 결과')).toBeVisible();
    const card = screen.getByRole('button', { name: /전투·피해·사망 처리/ });
    card.focus(); await user.keyboard('{Enter}');
    const detail = screen.getByRole('region', { name: '전투·피해·사망 처리' });
    const heading = within(detail).getByRole('heading', { name: '전투·피해·사망 처리' });
    expect(heading).toBeVisible();
    await waitFor(() => expect(heading).toHaveFocus());
    expect(within(detail).getByRole('button', { name: '전투 흐름 안내 원문 읽기' })).toBeVisible();
    await user.click(screen.getByRole('button', { name: '목록으로' }));
    await user.clear(screen.getByRole('searchbox', { name: '검색' }));
    await user.type(screen.getByRole('searchbox', { name: '검색' }), 'no-such-system-xyz');
    expect(screen.getByText('검색 결과가 없습니다.')).toBeVisible();
    await user.click(screen.getByRole('button', { name: '필터 초기화' }));
    await user.selectOptions(screen.getByRole('combobox', { name: '분야' }), 'Management');
    expect(screen.getByText('1개 결과')).toBeVisible();
    expect(screen.queryByRole('button', { name: /전투·피해·사망 처리/ })).not.toBeInTheDocument();
  });
  it('navigates shared records, related systems and fixed 원문 entries while separating planned work', async () => {
    const user = await enter();
    await user.click(screen.getByRole('button', { name: /^변경·결정·검증·계획/ }));
    await user.selectOptions(screen.getByRole('combobox', { name: '기록 종류' }), '계획');
    await user.click(screen.getByText('백로그 메뉴 계획'));
    const plan = screen.getByRole('region', { name: '백로그 메뉴 계획' });
    expect(within(plan).getAllByText('계획', { exact: true })[0]).toBeVisible();
    expect(plan).not.toHaveTextContent('PR #');
    await user.click(within(plan).getByRole('button', { name: /^개발 기록 열람/ }));
    const detail = screen.getByRole('region', { name: '개발 기록 열람' });
    expect(detail).toHaveTextContent('.backups/run/log.md');
    expect(within(detail).getAllByText(/로컬 전용/).some(element => element.checkVisibility?.() ?? true)).toBe(true);
    await user.click(screen.getByRole('button', { name: /^출처\s*\d/ }));
    await user.type(screen.getByRole('searchbox'), 'msg_0123456789ab');
    expect(screen.getByText('1개 결과')).toBeVisible();
    expect(screen.getAllByText(/msg_0123456789ab/)[0]).toBeVisible();
  });
});

describe('missing bridge', () => {
  it('reports missing runtime authority instead of claiming that records loaded', async () => {
    vi.stubGlobal('systemRecords', undefined); render(<App />);
    await userEvent.click(screen.getByRole('button', { name: '3 개발 현황' }));
    await userEvent.click(screen.getByRole('button', { name: '개발 기록' }));
    expect(screen.getByText(/기록 연결을 사용할 수 없습니다/)).toBeVisible();
    expect(screen.queryByRole('searchbox')).not.toBeInTheDocument();
  });
});

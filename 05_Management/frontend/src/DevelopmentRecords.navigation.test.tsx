// Requirements: main msg_1da9a73ad0d2 relaying the user's B choice — development records open from a
// concise list into one full-page detail and return to the previous screen (goal completion 1–4).
// Since the record index v2 (goal 2026-10-06-record-source-unification 「만들 것」 1·2·3,
// index-v2-design.md 「화면」), lists show only area or type and title, details show links and 「원문」
// entries instead of narrative and status fields, the tab is 「개발 기록」, the view is 「출처」, and the
// fixture is the v2 screen fixture. The unsaved-draft navigation test left with the editor.
// Expected values are fixture fields; filter results are observed on screen, never recomputed.
import '@testing-library/jest-dom/vitest';
import { act, cleanup, render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { screenCatalog, type RecordFixture, type SystemFixture } from '../tests/record-sources/catalog-v2-fixture';
import App from './App';
import DevelopmentRecords from './DevelopmentRecords';

const data = screenCatalog();
const initial = { ok: true, catalog: data, version: 'a'.repeat(64) };
const read = vi.fn<() => Promise<unknown>>();
const readSection = vi.fn<(sourceId: unknown) => Promise<unknown>>();
const readCheckout = vi.fn<() => Promise<unknown>>();

function system(id: string): SystemFixture {
  const found = data.systems.find(item => item.id === id);
  if (!found) throw new Error(`fixture system ${id} missing`);
  return found;
}

function record(id: string): RecordFixture {
  const found = data.records.find(item => item.id === id);
  if (!found) throw new Error(`fixture record ${id} missing`);
  return found;
}

beforeEach(() => {
  read.mockReset().mockResolvedValue(initial);
  readSection.mockReset().mockResolvedValue({ ok: false, code: 'missing', reason: null, message: '원문 파일을 찾을 수 없습니다.' });
  readCheckout.mockReset().mockResolvedValue({ ok: true, checkout: { state: 'unknown', reason: 'no-git' } });
  vi.stubGlobal('systemRecords', { readCatalog: read, readSection, readCheckout });
  Element.prototype.scrollIntoView = vi.fn();
});
afterEach(() => { cleanup(); vi.unstubAllGlobals(); });

async function openRecords() {
  const user = userEvent.setup();
  render(<App />);
  await user.click(screen.getByRole('button', { name: '3 개발 현황' }));
  await user.click(screen.getByRole('button', { name: '개발 기록' }));
  await screen.findByRole('searchbox', { name: '검색' });
  return user;
}

function listButton(listName: string, title: string): HTMLElement {
  const item = within(screen.getByRole('list', { name: listName })).getByText(title, { exact: true }).closest('button');
  if (!item) throw new Error(`${title} is not a list button`);
  return item;
}

function detailHeading(title: string): HTMLElement {
  return within(screen.getByRole('region', { name: title })).getByRole('heading', { level: 3, name: title });
}

function linkButton(container: HTMLElement, title: string): HTMLElement {
  return within(container).getByRole('button', { name: new RegExp(`^${title}(?! 원문 읽기)`) });
}

// A readable git Markdown source is a 원문 읽기 button; any other source is shown as text.
function expectSourceEntry(container: HTMLElement, sourceId: string) {
  const source = data.sources.find(item => item.id === sourceId);
  if (!source) throw new Error(`fixture source ${sourceId} missing`);
  if (source.kind === 'git' && source.locator.endsWith('.md')) {
    expect(within(container).getByRole('button', { name: `${source.title} 원문 읽기` }), sourceId).toBeVisible();
  } else {
    expect(container, sourceId).toHaveTextContent(`${source.title} · 앱에서 읽지 않음`);
    expect(container, sourceId).toHaveTextContent(source.locator);
  }
}

describe('concise lists', () => {
  it('lists only area or type with the title while linked source titles stay searchable', async () => {
    const user = await openRecords();
    const systemList = screen.getByRole('list', { name: '시스템 목록' });
    expect(within(systemList).getAllByRole('listitem')).toHaveLength(data.systems.length);
    for (const item of data.systems) {
      const button = listButton('시스템 목록', item.title);
      expect(button).toHaveTextContent(item.area);
    }
    // combat's source title is not shown in the list, yet the search still reaches it.
    await user.type(screen.getByRole('searchbox', { name: '검색' }), '전투 흐름 안내');
    expect(listButton('시스템 목록', system('combat').title)).toBeVisible();
    expect(screen.getByRole('list', { name: '시스템 목록' })).not.toHaveTextContent('전투 흐름 안내');

    await user.clear(screen.getByRole('searchbox', { name: '검색' }));
    await user.click(screen.getByRole('button', { name: /^변경·결정·검증·계획/ }));
    const recordList = screen.getByRole('list', { name: '기록 목록' });
    expect(within(recordList).getAllByRole('listitem')).toHaveLength(data.records.length);
    for (const item of data.records) expect(listButton('기록 목록', item.title)).toHaveTextContent(item.type);

    await user.type(screen.getByRole('searchbox', { name: '검색' }), '메인 결정 메시지');
    expect(listButton('기록 목록', record('index-change').title)).toBeVisible();
    expect(screen.getByRole('list', { name: '기록 목록' })).not.toHaveTextContent('메인 결정 메시지');
  });
});

describe('full-page detail', () => {
  it('opens the same single detail by mouse and keyboard and moves focus to its title', async () => {
    const user = await openRecords();
    const combat = system('combat');

    await user.click(listButton('시스템 목록', combat.title));
    expect(detailHeading(combat.title)).toHaveFocus();
    expect(screen.queryByRole('list', { name: '시스템 목록' })).not.toBeInTheDocument();
    expect(screen.queryByRole('searchbox', { name: '검색' })).not.toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: '목록으로' }));
    expect(listButton('시스템 목록', combat.title)).toHaveFocus();

    // Keyboard only: Enter opens, Shift+Tab reaches the list return, Space opens again.
    await user.keyboard('{Enter}');
    expect(detailHeading(combat.title)).toHaveFocus();
    await user.tab({ shift: true });
    expect(screen.getByRole('button', { name: '목록으로' })).toHaveFocus();
    await user.keyboard('{Enter}');
    expect(listButton('시스템 목록', combat.title)).toHaveFocus();
    await user.keyboard(' ');
    expect(detailHeading(combat.title)).toHaveFocus();
  });

  it('keeps the area, every linked system and record and every 원문 entry in the system detail', async () => {
    const user = await openRecords();
    const records = system('management-records');
    await user.click(listButton('시스템 목록', records.title));
    const detail = screen.getByRole('region', { name: records.title });

    expect(within(detail).getAllByText(records.area, { exact: true })[0]).toBeVisible();
    for (const id of records.relatedSystemIds) expect(linkButton(detail, system(id).title)).toBeVisible();
    for (const id of records.recordIds) expect(linkButton(detail, record(id).title)).toBeVisible();
    for (const id of records.sourceIds) expectSourceEntry(detail, id);
    expect(records.sourceIds.some(id => data.sources.find(item => item.id === id)?.availability === 'local-only')).toBe(true);
  });

  it('keeps the type, every PR, linked system and 원문 entry in the record detail', async () => {
    const user = await openRecords();
    await user.click(screen.getByRole('button', { name: /^변경·결정·검증·계획/ }));
    const change = record('index-change');
    await user.click(listButton('기록 목록', change.title));
    const detail = screen.getByRole('region', { name: change.title });

    expect(within(detail).getAllByText(change.type, { exact: true })[0]).toBeVisible();
    for (const pull of change.pullRequests) expect(detail).toHaveTextContent(`PR #${pull.number} · 병합 ${pull.mergeCommit.slice(0, 7)}`);
    for (const id of change.systemIds) expect(linkButton(detail, system(id).title)).toBeVisible();
    for (const id of change.sourceIds) expectSourceEntry(detail, id);
  });
});

describe('returning to the previous screen', () => {
  it('goes back through linked details to the original filtered list and restores its focus', async () => {
    const user = await openRecords();
    const plan = record('index-plan');
    const records = system('management-records');
    const combat = system('combat');
    expect(plan.systemIds).toContain(records.id);
    expect(records.relatedSystemIds).toContain(combat.id);
    // combat belongs to another area, so the Management area filter alone would hide it.
    expect(combat.area).not.toBe('Management');

    await user.click(screen.getByRole('button', { name: /^변경·결정·검증·계획/ }));
    await user.selectOptions(screen.getByRole('combobox', { name: '기록 종류' }), '계획');
    await user.selectOptions(screen.getByRole('combobox', { name: '분야' }), 'Management');
    await user.type(screen.getByRole('searchbox', { name: '검색' }), '백로그');
    await user.click(listButton('기록 목록', plan.title));
    expect(detailHeading(plan.title)).toHaveFocus();

    await user.click(linkButton(screen.getByRole('region', { name: plan.title }), records.title));
    expect(detailHeading(records.title)).toHaveFocus();
    await user.click(linkButton(screen.getByRole('region', { name: records.title }), combat.title));
    expect(detailHeading(combat.title)).toHaveFocus();

    await user.click(screen.getByRole('button', { name: '← 뒤로' }));
    expect(detailHeading(records.title)).toBeVisible();
    expect(linkButton(screen.getByRole('region', { name: records.title }), combat.title)).toHaveFocus();
    await user.click(screen.getByRole('button', { name: '← 뒤로' }));
    expect(detailHeading(plan.title)).toBeVisible();
    expect(linkButton(screen.getByRole('region', { name: plan.title }), records.title)).toHaveFocus();
    await user.click(screen.getByRole('button', { name: '← 뒤로' }));

    expect(screen.getByRole('searchbox', { name: '검색' })).toHaveValue('백로그');
    expect(screen.getByRole('combobox', { name: '분야' })).toHaveValue('Management');
    expect(screen.getByRole('combobox', { name: '기록 종류' })).toHaveValue('계획');
    expect(screen.getByRole('button', { name: /^변경·결정·검증·계획/ })).toHaveAttribute('aria-pressed', 'true');
    expect(listButton('기록 목록', plan.title)).toHaveFocus();

    // The list return skips every intermediate detail.
    await user.click(listButton('기록 목록', plan.title));
    await user.click(linkButton(screen.getByRole('region', { name: plan.title }), records.title));
    await user.click(screen.getByRole('button', { name: '목록으로' }));
    expect(screen.getByRole('searchbox', { name: '검색' })).toHaveValue('백로그');
    expect(screen.getByRole('combobox', { name: '분야' })).toHaveValue('Management');
    expect(screen.getByRole('combobox', { name: '기록 종류' })).toHaveValue('계획');
    expect(listButton('기록 목록', plan.title)).toHaveFocus();
  });

  it('explains a selected item that disappeared after refresh and offers a safe way back', async () => {
    const user = await openRecords();
    const combat = system('combat');
    await user.click(listButton('시스템 목록', combat.title));
    const without = { ...data, systems: data.systems.filter(item => item.id !== combat.id) };
    read.mockResolvedValue({ ok: true, catalog: without, version: 'b'.repeat(64) });

    const refresh = screen.getByRole('button', { name: '기록 새로고침' });
    await user.click(refresh);
    await screen.findByRole('heading', { level: 3, name: '선택한 항목을 찾을 수 없습니다.' });
    expect(refresh).toHaveFocus();
    expect(screen.getByText(`새로 읽은 기록에 시스템 ID “${combat.id}”가 없습니다.`)).toBeVisible();
    expect(screen.queryByRole('button', { name: '전투 흐름 안내 원문 읽기' })).not.toBeInTheDocument();
    expect(screen.queryByText('관련 시스템', { exact: true })).not.toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: '목록으로' }));
    expect(within(screen.getByRole('list', { name: '시스템 목록' })).getAllByRole('listitem')).toHaveLength(without.systems.length);
    expect(screen.queryByText(combat.title, { exact: true })).not.toBeInTheDocument();
  });
});

describe('hidden view and focus ownership', () => {
  it('does not take focus when records load while the records view is hidden', async () => {
    const pending: ((value: unknown) => void)[] = [];
    read.mockImplementation(() => new Promise(resolve => { pending.push(resolve); }));
    const user = userEvent.setup();
    render(<App />);
    const development = screen.getByRole('button', { name: '3 개발 현황' });
    await user.click(development);
    expect(development).toHaveFocus();
    expect(pending.length).toBeGreaterThan(0);

    await act(async () => { pending.forEach(resolve => resolve(initial)); });
    expect(development).toHaveFocus();

    const recordsTab = screen.getByRole('button', { name: '개발 기록' });
    await user.click(recordsTab);
    expect(await screen.findByRole('searchbox', { name: '검색' })).toBeVisible();
    expect(recordsTab).toHaveFocus();
  });

  it('keeps the open detail but leaves focus on the tab when switching away and back', async () => {
    const user = await openRecords();
    const combat = system('combat');
    await user.click(listButton('시스템 목록', combat.title));
    const cardsTab = screen.getByRole('button', { name: '시스템 카드' });
    await user.click(cardsTab);
    expect(cardsTab).toHaveFocus();
    const recordsTab = screen.getByRole('button', { name: '개발 기록' });
    await user.click(recordsTab);
    expect(recordsTab).toHaveFocus();
    expect(detailHeading(combat.title)).toBeVisible();
  });

  it('reads the catalog once when mounted alone and not again while navigating', async () => {
    const user = userEvent.setup();
    render(<DevelopmentRecords />);
    await screen.findByRole('searchbox', { name: '검색' });
    expect(read).toHaveBeenCalledOnce();
    await user.click(listButton('시스템 목록', system('combat').title));
    await user.click(screen.getByRole('button', { name: '목록으로' }));
    await user.type(screen.getByRole('searchbox', { name: '검색' }), '기록');
    await user.click(screen.getByRole('button', { name: /^출처\s*\d/ }));
    expect(read).toHaveBeenCalledOnce();
  });
});

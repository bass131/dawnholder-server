// Requirements: main msg_1da9a73ad0d2 relaying the user's B choice — development records open from a
// concise list into one full-page detail and return to the previous screen (goal completion 1–4).
// Expected values are original catalog fields; filter results are observed on screen, never recomputed.
import '@testing-library/jest-dom/vitest';
import { act, cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import fixture from '../../records/catalog.json';
import { readCatalog, type CatalogResult, type DevelopmentRecord, type SystemRecord } from './recordCatalog';
import App from './App';
import DevelopmentRecords from './DevelopmentRecords';

const parsed = readCatalog(fixture);
if (!parsed) throw new Error('Invalid fixture');
const data = parsed;
const initial: CatalogResult = { ok: true, catalog: data, text: JSON.stringify(data), version: 'a'.repeat(64) };
const read = vi.fn<() => Promise<CatalogResult>>();
const save = vi.fn<(_input: { text: string; expectedVersion: string | null }) => Promise<CatalogResult>>();

function system(id: string): SystemRecord {
  const found = data.systems.find(item => item.id === id);
  if (!found) throw new Error(`fixture system ${id} missing`);
  return found;
}

function record(id: string): DevelopmentRecord {
  const found = data.records.find(item => item.id === id);
  if (!found) throw new Error(`fixture record ${id} missing`);
  return found;
}

beforeEach(() => {
  read.mockReset().mockResolvedValue(initial);
  save.mockReset();
  vi.stubGlobal('systemRecords', { readCatalog: read, saveCatalog: save });
  Element.prototype.scrollIntoView = vi.fn();
});
afterEach(() => { cleanup(); vi.unstubAllGlobals(); });

async function openRecords() {
  const user = userEvent.setup();
  render(<App />);
  await user.click(screen.getByRole('button', { name: '3 개발 현황' }));
  await user.click(screen.getByRole('button', { name: '개발 기록 · 기록 편집' }));
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

function expectShown(container: HTMLElement, text: string) {
  expect(within(container).getAllByText(text, { exact: true })[0]).toBeVisible();
}

describe('concise lists', () => {
  it('lists only title, area or type and the original status while hidden body text stays searchable', async () => {
    const user = await openRecords();
    const systemList = screen.getByRole('list', { name: '시스템 목록' });
    expect(within(systemList).getAllByRole('listitem')).toHaveLength(data.systems.length);
    for (const item of data.systems) {
      const button = listButton('시스템 목록', item.title);
      expect(button).toHaveTextContent(item.area);
      expect(button).toHaveTextContent(item.implementationStatus);
      expect(systemList).not.toHaveTextContent(item.summary);
      expect(systemList).not.toHaveTextContent(item.responsibility);
    }

    // persistence's responsibility is not shown in the list, yet the search still reaches it.
    const persistence = system('persistence');
    await user.type(screen.getByRole('searchbox', { name: '검색' }), 'repository·authority');
    expect(listButton('시스템 목록', persistence.title)).toBeVisible();
    expect(screen.getByRole('list', { name: '시스템 목록' })).not.toHaveTextContent(persistence.responsibility);

    await user.clear(screen.getByRole('searchbox', { name: '검색' }));
    await user.click(screen.getByRole('button', { name: /^변경·결정·검증·계획/ }));
    const recordList = screen.getByRole('list', { name: '기록 목록' });
    expect(within(recordList).getAllByRole('listitem')).toHaveLength(data.records.length);
    for (const item of data.records) {
      const button = listButton('기록 목록', item.title);
      expect(button).toHaveTextContent(item.type);
      expect(button).toHaveTextContent(item.status);
      expect(recordList).not.toHaveTextContent(item.summary);
      expect(recordList).not.toHaveTextContent(item.reason);
    }

    const plan = record('plan-contracts');
    await user.type(screen.getByRole('searchbox', { name: '검색' }), '유지보수 계약');
    expect(listButton('기록 목록', plan.title)).toBeVisible();
    expect(screen.getByRole('list', { name: '기록 목록' })).not.toHaveTextContent(plan.reason);
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

  it('keeps every system field, linked item and evidence path, version and local-only mark', async () => {
    const user = await openRecords();
    const persistence = system('persistence');
    await user.click(listButton('시스템 목록', persistence.title));
    const detail = screen.getByRole('region', { name: persistence.title });

    for (const text of [
      persistence.area, persistence.summary, persistence.responsibility,
      persistence.implementationStatus, persistence.integrationStatus, persistence.verificationStatus,
      ...persistence.behavior, ...persistence.limitations, ...persistence.nextSteps,
      '구현', '통합', '검증', '책임', '동작', '검증 한계', '다음 일', '관련 시스템', '연결 기록 · 이유와 검증',
    ]) {
      expectShown(detail, text);
    }
    for (const id of persistence.relatedSystemIds) {
      expect(within(detail).getByRole('button', { name: `${system(id).title} →` })).toBeVisible();
    }
    for (const id of persistence.recordIds) {
      expect(within(detail).getByRole('button', { name: `${record(id).title} →` })).toBeVisible();
    }

    await user.click(within(detail).getByText(`근거 ${persistence.sourceIds.length}개`));
    for (const id of persistence.sourceIds) {
      const source = data.sources.find(item => item.id === id);
      if (!source) throw new Error(`fixture source ${id} missing`);
      const article = within(detail).getByRole('heading', { level: 4, name: source.title }).closest('article');
      if (!article) throw new Error(`${source.title} is not an evidence article`);
      expect(article).toBeVisible();
      expect(article).toHaveTextContent(source.locator);
      expect(article).toHaveTextContent(source.revision || '미확인');
      expect(article).toHaveTextContent(source.section || '미지정');
      expect(article).toHaveTextContent(source.note);
      expect(article).toHaveTextContent(source.availability === 'local-only'
        ? `로컬 전용 · 다른 환경에서는 열람 불가할 수 있음 · ${source.kind}`
        : `버전 관리 근거 · ${source.kind}`);
    }
    expect(persistence.sourceIds.some(id => data.sources.find(item => item.id === id)?.availability === 'local-only')).toBe(true);
  });

  it('keeps every record field, linked system and evidence in the record detail', async () => {
    const user = await openRecords();
    await user.click(screen.getByRole('button', { name: /^변경·결정·검증·계획/ }));
    const plan = record('plan-contracts');
    await user.click(listButton('기록 목록', plan.title));
    const detail = screen.getByRole('region', { name: plan.title });

    for (const text of [
      plan.type, plan.status, plan.summary, plan.reason,
      ...plan.details, ...plan.limitations, ...plan.nextSteps,
      '이유', '상세', '검증 한계', '다음 일', '연결 시스템',
    ]) {
      expectShown(detail, text);
    }
    for (const id of plan.systemIds) {
      expect(within(detail).getByRole('button', { name: `${system(id).title} →` })).toBeVisible();
    }
    await user.click(within(detail).getByText(`근거 ${plan.sourceIds.length}개`));
    for (const id of plan.sourceIds) {
      const source = data.sources.find(item => item.id === id);
      if (!source) throw new Error(`fixture source ${id} missing`);
      expect(within(detail).getByRole('heading', { level: 4, name: source.title })).toBeVisible();
    }
  });
});

describe('returning to the previous screen', () => {
  it('goes back through linked details to the original filtered list and restores its focus', async () => {
    const user = await openRecords();
    const plan = record('plan-contracts');
    const persistence = system('persistence');
    const linked = record('decision-d0');
    expect(plan.systemIds).toContain(persistence.id);
    expect(persistence.recordIds).toContain(linked.id);
    // persistence belongs to another area, so the Management area filter alone would hide it.
    expect(persistence.area).not.toBe('Management');

    await user.click(screen.getByRole('button', { name: /^변경·결정·검증·계획/ }));
    await user.selectOptions(screen.getByRole('combobox', { name: '기록 종류' }), '계획');
    await user.selectOptions(screen.getByRole('combobox', { name: '분야' }), 'Management');
    await user.type(screen.getByRole('searchbox', { name: '검색' }), '유지보수 계약');
    await user.click(listButton('기록 목록', plan.title));
    expect(detailHeading(plan.title)).toHaveFocus();

    await user.click(screen.getByRole('button', { name: `${persistence.title} →` }));
    expect(detailHeading(persistence.title)).toHaveFocus();
    expectShown(screen.getByRole('region', { name: persistence.title }), persistence.summary);
    await user.click(screen.getByRole('button', { name: `${linked.title} →` }));
    expect(detailHeading(linked.title)).toHaveFocus();

    await user.click(screen.getByRole('button', { name: '← 뒤로' }));
    expect(detailHeading(persistence.title)).toBeVisible();
    expect(screen.getByRole('button', { name: `${linked.title} →` })).toHaveFocus();
    await user.click(screen.getByRole('button', { name: '← 뒤로' }));
    expect(detailHeading(plan.title)).toBeVisible();
    expect(screen.getByRole('button', { name: `${persistence.title} →` })).toHaveFocus();
    await user.click(screen.getByRole('button', { name: '← 뒤로' }));

    expect(screen.getByRole('searchbox', { name: '검색' })).toHaveValue('유지보수 계약');
    expect(screen.getByRole('combobox', { name: '분야' })).toHaveValue('Management');
    expect(screen.getByRole('combobox', { name: '기록 종류' })).toHaveValue('계획');
    expect(screen.getByRole('button', { name: /^변경·결정·검증·계획/ })).toHaveAttribute('aria-pressed', 'true');
    expect(listButton('기록 목록', plan.title)).toHaveFocus();

    // The list return skips every intermediate detail.
    await user.click(listButton('기록 목록', plan.title));
    await user.click(screen.getByRole('button', { name: `${persistence.title} →` }));
    await user.click(screen.getByRole('button', { name: '목록으로' }));
    expect(screen.getByRole('searchbox', { name: '검색' })).toHaveValue('유지보수 계약');
    expect(screen.getByRole('combobox', { name: '분야' })).toHaveValue('Management');
    expect(screen.getByRole('combobox', { name: '기록 종류' })).toHaveValue('계획');
    expect(listButton('기록 목록', plan.title)).toHaveFocus();
  });

  it('explains a selected item that disappeared after refresh and offers a safe way back', async () => {
    const user = await openRecords();
    const persistence = system('persistence');
    await user.click(listButton('시스템 목록', persistence.title));
    const without = { ...data, systems: data.systems.filter(item => item.id !== persistence.id) };
    read.mockResolvedValue({ ok: true, catalog: without, text: JSON.stringify(without), version: 'b'.repeat(64) });

    const refresh = screen.getByRole('button', { name: '기록 새로고침' });
    await user.click(refresh);
    await screen.findByRole('heading', { level: 3, name: '선택한 항목을 찾을 수 없습니다.' });
    expect(refresh).toHaveFocus();
    expect(screen.getByText(`새로 읽은 기록에 시스템 ID “${persistence.id}”가 없습니다.`)).toBeVisible();
    expect(screen.queryByText(persistence.summary)).not.toBeInTheDocument();
    expect(screen.queryByRole('heading', { level: 4, name: '책임' })).not.toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: '목록으로' }));
    expect(within(screen.getByRole('list', { name: '시스템 목록' })).getAllByRole('listitem')).toHaveLength(without.systems.length);
    expect(screen.queryByText(persistence.title, { exact: true })).not.toBeInTheDocument();
  });
});

describe('hidden view and focus ownership', () => {
  it('does not take focus when records load while the records view is hidden', async () => {
    const pending: ((value: CatalogResult) => void)[] = [];
    read.mockImplementation(() => new Promise(resolve => { pending.push(resolve); }));
    const user = userEvent.setup();
    render(<App />);
    const development = screen.getByRole('button', { name: '3 개발 현황' });
    await user.click(development);
    expect(development).toHaveFocus();
    expect(pending.length).toBeGreaterThan(0);

    await act(async () => { pending.forEach(resolve => resolve(initial)); });
    expect(development).toHaveFocus();

    const recordsTab = screen.getByRole('button', { name: '개발 기록 · 기록 편집' });
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
    const recordsTab = screen.getByRole('button', { name: '개발 기록 · 기록 편집' });
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
    await user.type(screen.getByRole('searchbox', { name: '검색' }), '저장');
    await user.click(screen.getByRole('button', { name: /^근거\s*\d/ }));
    expect(read).toHaveBeenCalledOnce();
  });
});

describe('unsaved draft across navigation', () => {
  it('keeps the draft through tabs, sections, details, search and refresh and saves only on request', async () => {
    const user = await openRecords();
    await user.click(screen.getByText(/^기록 편집$/));
    const field = screen.getByRole('textbox', { name: '카탈로그 JSON' });
    const draft = JSON.stringify({ ...data, revision: 'draft-A' });
    fireEvent.change(field, { target: { value: draft } });
    expect(screen.getByText('저장하지 않은 편집 초안 있음')).toBeVisible();

    await user.click(listButton('시스템 목록', system('persistence').title));
    await user.click(screen.getByRole('button', { name: `${record('plan-contracts').title} →` }));
    await user.click(screen.getByRole('button', { name: '← 뒤로' }));
    await user.click(screen.getByRole('button', { name: '목록으로' }));
    await user.type(screen.getByRole('searchbox', { name: '검색' }), '저장');
    await user.click(screen.getByRole('button', { name: /^변경·결정·검증·계획/ }));
    await user.click(screen.getByRole('button', { name: /^근거\s*\d/ }));
    await user.click(screen.getByRole('button', { name: '시스템 카드' }));
    await user.click(screen.getByRole('button', { name: '1 서버 운영' }));
    await user.click(screen.getByRole('button', { name: '3 개발 현황' }));
    await user.click(screen.getByRole('button', { name: '개발 기록 · 기록 편집' }));
    expect(field).toHaveValue(draft);

    const external = { ...data, revision: 'external-B' };
    read.mockResolvedValue({ ok: true, catalog: external, text: JSON.stringify(external), version: 'b'.repeat(64) });
    await user.click(screen.getByRole('button', { name: '기록 새로고침' }));
    expect(await screen.findByText(/편집 초안은 보존했습니다/)).toBeVisible();
    expect(screen.getByText(/기록 버전 external-B/)).toBeVisible();
    expect(field).toHaveValue(draft);
    expect(save).not.toHaveBeenCalled();

    save.mockResolvedValue({ ok: false, code: 'conflict', message: '다른 작업에서 기록을 변경했습니다.' });
    await user.click(screen.getByRole('button', { name: '검증 후 기록 저장' }));
    expect(save).toHaveBeenCalledOnce();
    expect(save).toHaveBeenLastCalledWith({ text: draft, expectedVersion: 'a'.repeat(64) });
    await waitFor(() => expect(screen.getByText(/다른 작업에서 기록을 변경했습니다/)).toBeVisible());
    expect(field).toHaveValue(draft);
  });
});

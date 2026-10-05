import '@testing-library/jest-dom/vitest';
import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import fixture from '../../records/catalog.json';
import { readCatalog, type CatalogResult } from './recordCatalog';
import App from './App';

const parsed = readCatalog(fixture);
if (!parsed) throw new Error('Invalid fixture');
const data = parsed;
const text = JSON.stringify(data);
const initial: CatalogResult = { ok: true, catalog: data, text, version: 'a'.repeat(64) };
const read = vi.fn<() => Promise<CatalogResult>>();
const save = vi.fn<(_input: { text: string; expectedVersion: string | null }) => Promise<CatalogResult>>();
beforeEach(() => {
  read.mockReset().mockResolvedValue(initial); save.mockReset();
  vi.stubGlobal('systemRecords', { readCatalog: read, saveCatalog: save });
  Element.prototype.scrollIntoView = vi.fn();
});
afterEach(() => { cleanup(); vi.unstubAllGlobals(); });
async function enter() {
  const user = userEvent.setup(); render(<App />);
  await user.click(screen.getByRole('button', { name: '3 개발 현황' }));
  await screen.findByRole('searchbox', { name: '검색' });
  return user;
}
async function editor() {
  const user = await enter();
  await user.click(screen.getByText(/^기록 편집$/));
  return { user, field: screen.getByRole('textbox', { name: '카탈로그 JSON' }) };
}

describe('system discovery and evidence', () => {
  it('loads through the runtime bridge and filters with empty results and keyboard activation', async () => {
    const user = await enter();
    expect(read).toHaveBeenCalledOnce();
    expect(screen.getByText('읽은 스냅샷 · 게임 실시간 연동 아님')).toBeVisible();
    const search = screen.getByRole('searchbox', { name: '검색' });
    await user.type(search, 'GameMap 즉시');
    expect(screen.getByText('1개 결과')).toBeVisible();
    const card = screen.getByRole('button', { name: /게임 플레이 전투·피해·사망 처리/ });
    card.focus(); await user.keyboard('{Enter}');
    const detail = screen.getByRole('region', { name: '선택한 시스템 상세' });
    expect(within(detail).getByRole('heading', { name: '전투·피해·사망 처리' })).toBeVisible();
    await waitFor(() => expect(detail).toHaveFocus());
    expect(within(detail).getByText(/PR140 MERGED/)).toBeVisible();
    await user.clear(search); await user.type(search, 'no-such-system-xyz');
    expect(screen.getByText('검색 결과가 없습니다.')).toBeVisible();
    await user.click(screen.getByRole('button', { name: '필터 초기화' }));
    await user.selectOptions(screen.getByRole('combobox', { name: '분야' }), 'Management');
    expect(screen.getByText('3개 결과')).toBeVisible();
    expect(screen.queryByRole('button', { name: /게임 플레이 전투·피해/ })).not.toBeInTheDocument();
  });
  it('navigates shared records, related systems and fixed evidence while separating planned work', async () => {
    const user = await enter();
    await user.click(screen.getByRole('button', { name: /^변경·결정·검증·계획/ }));
    await user.selectOptions(screen.getByRole('combobox', { name: '기록 종류' }), '계획');
    await user.click(screen.getByText('기준선·계약·평가 준비와 클라이언트·영속성 및 운영툴·서버 계약 후속 계획'));
    expect(screen.getByText(/^기준선·계약·평가 진행·완료 아님 \/ 클라이언트\/UI 적용부터 저장·복원 종합 검증까지의 후속 단계 미착수$/)).toBeVisible();
    await user.click(screen.getByRole('button', { name: 'DB·캐릭터 저장과 복원 →' }));
    const detail = screen.getByRole('region', { name: '선택한 시스템 상세' });
    expect(within(detail).getByText('작은 범위의 DB 연동 설계와 구현 분할 완료 / GameServer 저장·복원 연동 미완료')).toBeVisible();
    const evidence = detail.querySelector(':scope > .record-detail-body > .record-evidence > summary');
    expect(evidence).not.toBeNull(); await user.click(evidence!);
    expect(within(evidence!.parentElement!).getByText('01_Phases/goals/2026-09-29-persistence-design/goal.md')).toBeVisible();
    expect(within(detail).getAllByText(/로컬 전용/).some(element => element.checkVisibility?.() ?? true)).toBe(true);
    await user.click(screen.getByRole('button', { name: /^근거\s*\d/ }));
    await user.type(screen.getByRole('searchbox'), 'msg_b5283836b43f');
    expect(screen.getByText('1개 결과')).toBeVisible();
    expect(screen.getByText('message:msg_b5283836b43f')).toBeVisible();
  });
});

describe('draft editing without accidental data loss', () => {
  it('retains a draft across area switches and saves explicitly through the narrow bridge', async () => {
    const { user, field } = await editor();
    const next = { ...data, revision: 'ui-edited' }; const nextText = JSON.stringify(next);
    fireEvent.change(field, { target: { value: nextText } });
    expect(save).not.toHaveBeenCalled();
    await user.click(screen.getByRole('button', { name: '1 서버 운영' }));
    await user.click(screen.getByRole('button', { name: '3 개발 현황' }));
    expect(field).toHaveValue(nextText);
    save.mockResolvedValue({ ok: true, catalog: next, text: nextText, version: 'b'.repeat(64) });
    await user.click(screen.getByRole('button', { name: '검증 후 기록 저장' }));
    expect(save).toHaveBeenCalledWith({ text: nextText, expectedVersion: 'a'.repeat(64) });
    expect(await screen.findByText(/기록을 저장하고 화면에 반영했습니다/)).toBeVisible();
    expect(screen.getByText(/기준일 .*ui-edited/)).toBeVisible();
  });
  it('preserves an unsaved draft and its original version when reload observes another writer', async () => {
    const { user, field } = await editor();
    const draft = JSON.stringify({ ...data, revision: 'draft-A' });
    fireEvent.change(field, { target: { value: draft } });
    const external = { ...data, revision: 'external-B' }; const externalText = JSON.stringify(external);
    read.mockResolvedValue({ ok: true, catalog: external, text: externalText, version: 'b'.repeat(64) });
    await user.click(screen.getByRole('button', { name: '기록 새로고침' }));
    expect(field).toHaveValue(draft);
    save.mockResolvedValue({ ok: false, code: 'conflict', message: '다른 작업에서 기록을 변경했습니다.' });
    await user.click(screen.getByRole('button', { name: '검증 후 기록 저장' }));
    expect(save).toHaveBeenLastCalledWith({ text: draft, expectedVersion: 'a'.repeat(64) });
    expect(field).toHaveValue(draft);
    await user.click(screen.getByRole('button', { name: '편집 취소 · 초안 버리기' }));
    expect(field).toHaveValue(externalText);
    fireEvent.change(field, { target: { value: draft } });
    await user.click(screen.getByRole('button', { name: '검증 후 기록 저장' }));
    expect(save).toHaveBeenLastCalledWith({ text: draft, expectedVersion: 'b'.repeat(64) });
  });
  it('rejects malformed and dangling drafts locally and preserves the draft on write/bridge failures', async () => {
    const { user, field } = await editor();
    for (const draft of ['{', JSON.stringify({ ...data, systems: [{ ...data?.systems[0], sourceIds: ['unknown-source'] }] })]) {
      fireEvent.change(field, { target: { value: draft } });
      await user.click(screen.getByRole('button', { name: '검증 후 기록 저장' }));
      expect(screen.getByRole('alert')).toBeVisible(); expect(field).toHaveValue(draft);
      expect(save).not.toHaveBeenCalled();
    }
    const valid = text + '\n'; fireEvent.change(field, { target: { value: valid } });
    save.mockResolvedValueOnce({ ok: false, code: 'write', message: '백업 저장 실패' }).mockRejectedValueOnce(new Error('bridge failure'));
    await user.click(screen.getByRole('button', { name: '검증 후 기록 저장' }));
    expect(screen.getByText(/백업 저장 실패/)).toBeVisible(); expect(field).toHaveValue(valid);
    await user.click(screen.getByRole('button', { name: '검증 후 기록 저장' }));
    expect(screen.getByText(/저장 응답을 받지 못했습니다/)).toBeVisible(); expect(field).toHaveValue(valid);
  });
  it('imports only a draft, prevents overwriting a dirty draft and preserves prior view after a failed reload', async () => {
    const { user, field } = await editor();
    const imported = JSON.stringify({ ...data, revision: 'import-only' });
    const file = new File([imported], 'incoming.json', { type: 'application/json' });
    Object.defineProperty(file, 'text', { value: async () => imported });
    await user.upload(screen.getByLabelText('JSON 파일 불러오기'), file);
    expect(field).toHaveValue(imported); expect(save).not.toHaveBeenCalled();
    expect(screen.getByLabelText('JSON 파일 불러오기')).toBeDisabled();
    read.mockResolvedValue({ ok: false, code: 'invalid', message: '미확인 참조: missing', text: '{}', version: 'c'.repeat(64) });
    await user.click(screen.getByRole('button', { name: '기록 새로고침' }));
    expect(screen.getByText(/이전에 읽은 기록을 표시합니다/)).toBeVisible();
    expect(screen.getByText(/미확인 참조: missing/)).toBeVisible();
    expect(field).toHaveValue(imported);
  });
  it('reports missing runtime authority instead of claiming that records loaded', async () => {
    vi.stubGlobal('systemRecords', undefined); render(<App />);
    await userEvent.click(screen.getByRole('button', { name: '3 개발 현황' }));
    expect(screen.getByText(/기록 연결을 사용할 수 없습니다/)).toBeVisible();
    expect(screen.queryByRole('searchbox')).not.toBeInTheDocument();
  });
});

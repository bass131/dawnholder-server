// Requirement: screen-design.md 「화면」 — 「연결 안내: 연결 전·실패 때 「관리 기능 미연결」과 이유를 보이고 … 연결되면 「관리 기능
// 연결됨」으로 바꾼다. 하단 상태 줄도 같은 상태를 보인다」 — and the PR2 fix contract (E/contracts/pr2-fix1-task.md 「고칠 것」 1):
// splitting App.tsx's notice and footer lines keeps 「동작·문구·DOM 구조·접근성 이름」. The expected texts are the strings of the
// unsplit lines at 65bbdcb2 (src/App.tsx:88 and :104), which the fix had to keep.
//
// Independent re-verification test. Without the desktop bridge every section shows the App notice. With a connected bridge the
// 서버 운영 screen draws its own notice instead and the other sections show 관리 기능 연결됨. window.serverOperations is a fake
// whose reads other than the connection fail, so only the connection state reaches App.
import '@testing-library/jest-dom/vitest';
import { cleanup, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import App from './App';

const UNCONNECTED = '관리 기능 미연결';
const CONNECTED = '관리 기능 연결됨';
const NOTICE_DEFAULT = '실제 상태와 이력은 확인할 수 없습니다.';
const NOTICE_DEVELOPMENT = '시스템 기록은 로컬 파일에서 읽습니다. 서버의 실시간 상태는 미연결입니다.';
const NOTICE_CONNECTED = '서버 운영 화면에서 상태를 확인할 수 있습니다.';
const FOOTER_UNCONNECTED = '관리 기능 미연결 · 서버 실시간 상태 미연결';
const FOOTER_CONNECTED = '관리 기능 연결됨';
const REFRESH_LOCAL = '로컬 자료 · 자동 갱신 안 됨';
const REFRESH_OPERATIONS = '서버 상태 · 화면이 보이는 동안 자동 갱신';

// App's own notices: the 연결 상태 asides placed directly in the section, not the 서버 운영 screen's notice.
function appNotices(): HTMLElement[] {
  const notices = screen.queryAllByRole('complementary', { name: '연결 상태' });
  return notices.filter(element => element.parentElement?.id === 'management-section');
}

function elementShape(element: Element) {
  return {
    tag: element.tagName,
    className: element.className,
    nodeCount: element.childNodes.length,
    children: [...element.children].map(child => ({ tag: child.tagName, text: child.textContent })),
  };
}

function noticeShape(title: string, text: string) {
  return {
    tag: 'ASIDE',
    className: 'connection-notice',
    nodeCount: 2,
    children: [{ tag: 'STRONG', text: title }, { tag: 'P', text }],
  };
}

function footerShape(connection: string, refresh: string) {
  return {
    tag: 'FOOTER',
    className: 'app-status',
    nodeCount: 2,
    children: [{ tag: 'SPAN', text: connection }, { tag: 'SPAN', text: refresh }],
  };
}

function onlyAppNotice(): HTMLElement {
  const notices = appNotices();
  if (notices.length !== 1) throw new Error(`expected one App notice, found ${notices.length}`);
  return notices[0] as HTMLElement;
}

const footer = () => screen.getByRole('contentinfo');
const sectionButton = (title: string) => screen.getByRole('button', { name: new RegExp(title) });

function installConnectedBridge() {
  const failure = { ok: false, code: 'operationFailed', message: '시험 대역은 이 값을 주지 않습니다.' };
  const connected = { ok: true, connection: { state: 'connected' } };
  const bridge = {
    readConnection: vi.fn(async () => connected),
    connect: vi.fn(async () => connected),
    readStatus: vi.fn(async () => failure),
    readReleaseCandidate: vi.fn(async () => failure),
    readLogs: vi.fn(async () => failure),
    startServer: vi.fn(async () => failure),
    stopServer: vi.fn(async () => failure),
    forceStopServer: vi.fn(async () => failure),
    buildRelease: vi.fn(async () => failure),
    selectRelease: vi.fn(async () => failure),
  };
  vi.stubGlobal('serverOperations', bridge);
  return bridge;
}

beforeEach(() => {
  Object.defineProperty(document, 'visibilityState', { configurable: true, get: () => 'visible' });
  Object.defineProperty(document, 'hidden', { configurable: true, get: () => false });
});

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
  Reflect.deleteProperty(document, 'visibilityState');
  Reflect.deleteProperty(document, 'hidden');
});

describe('App connection notice and status line after the line split (independent re-verification)', () => {
  it('without the desktop bridge keeps each section notice, the two footer texts and their elements', async () => {
    const user = userEvent.setup();
    render(<App />);
    const operations = elementShape(onlyAppNotice());
    const operationsFooter = elementShape(footer());
    await user.click(sectionButton('유저 관리'));
    const users = elementShape(onlyAppNotice());
    await user.click(sectionButton('개발 현황'));
    const development = elementShape(onlyAppNotice());
    const developmentFooter = elementShape(footer());

    expect(operations).toEqual(noticeShape(UNCONNECTED, NOTICE_DEFAULT));
    expect(users).toEqual(noticeShape(UNCONNECTED, NOTICE_DEFAULT));
    expect(development).toEqual(noticeShape(UNCONNECTED, NOTICE_DEVELOPMENT));
    expect(operationsFooter).toEqual(footerShape(FOOTER_UNCONNECTED, REFRESH_LOCAL));
    expect(developmentFooter).toEqual(footerShape(FOOTER_UNCONNECTED, REFRESH_LOCAL));
  });

  it('with a connected bridge hides the App notice on 서버 운영 and shows 관리 기능 연결됨 in the other sections', async () => {
    const user = userEvent.setup();
    const bridge = installConnectedBridge();
    render(<App />);
    await waitFor(() => expect(footer().firstElementChild).toHaveTextContent(FOOTER_CONNECTED));
    const operationsNotices = appNotices().length;
    const operationsFooter = elementShape(footer());
    await user.click(sectionButton('유저 관리'));
    const users = elementShape(onlyAppNotice());
    const usersFooter = elementShape(footer());
    await user.click(sectionButton('개발 현황'));
    const development = elementShape(onlyAppNotice());

    expect(bridge.readConnection).toHaveBeenCalled();
    expect(operationsNotices).toBe(0);
    expect(operationsFooter).toEqual(footerShape(FOOTER_CONNECTED, REFRESH_OPERATIONS));
    expect(users).toEqual(noticeShape(CONNECTED, NOTICE_CONNECTED));
    expect(usersFooter).toEqual(footerShape(FOOTER_CONNECTED, REFRESH_LOCAL));
    expect(development).toEqual(noticeShape(CONNECTED, NOTICE_CONNECTED));
  });
});

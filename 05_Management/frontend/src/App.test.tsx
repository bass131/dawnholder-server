import '@testing-library/jest-dom/vitest';
import { cleanup, render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';
import App from './App';

const originalBeacon = Object.getOwnPropertyDescriptor(navigator, 'sendBeacon');

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
  if (originalBeacon) Object.defineProperty(navigator, 'sendBeacon', originalBeacon);
  else Reflect.deleteProperty(navigator, 'sendBeacon');
});

function expectUnobservedState() {
  expect(screen.getByRole('complementary', { name: '연결 상태' })).toHaveTextContent(/미연결/);
  expect(screen.getByRole('main')).toHaveTextContent(/미연결/);
  if (screen.getByRole('heading', { level: 2 }).textContent !== '개발 현황') {
    const operationalView = screen.getByRole('main').cloneNode(true) as HTMLElement;
    operationalView.querySelectorAll('[hidden]').forEach(element => element.remove());
    expect(operationalView).not.toHaveTextContent(/(?:정상|가동|실행|중지)\s*중|접속(?:자|\s*인원)?\s*[:：]?\s*0|저장\s*완료/);
  }
}

describe('unconnected management foundation', () => {
  it('starts with operations and exposes unknown state rather than invented telemetry', () => {
    render(<App />);

    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Dawnholder Management');
    expect(screen.getByRole('heading', { level: 2 })).toHaveTextContent('서버 운영');
    expectUnobservedState();
    expect(screen.getByRole('complementary', { name: '연결 상태' })).toHaveTextContent(/실제 상태와 이력은 확인할 수 없/);
    const navigation = within(screen.getByRole('navigation', { name: '관리 영역' }));
    expect(navigation.getAllByRole('button').map((button) => button.textContent?.trim()))
      .toEqual(['1 서버 운영', '2 유저 관리', '3 개발 현황']);
  });

  it('keeps disconnection clear across keyboard navigation and returning to operations', async () => {
    const user = userEvent.setup();
    render(<App />);
    const navigation = within(screen.getByRole('navigation', { name: '관리 영역' }));

    for (const label of ['유저 관리', '개발 현황', '서버 운영']) {
      const button = navigation.getByRole('button', { name: new RegExp(label) });
      button.focus();
      await user.keyboard('{Enter}');

      expect(button).toHaveAttribute('aria-pressed', 'true');
      expect(navigation.getAllByRole('button', { pressed: true })).toHaveLength(1);
      expect(screen.getByRole('heading', { level: 2 })).toHaveTextContent(label);
      expect(document.getElementById(button.getAttribute('aria-controls') ?? '')).toContainElement(screen.getByRole('heading', { level: 2 }));
      expectUnobservedState();
    }
  });

  it('explains unknown summary values accessibly instead of displaying numeric telemetry', () => {
    render(<App />);
    const summaries = screen.getAllByRole('definition');
    expect(summaries.length).toBeGreaterThan(0);
    for (const summary of summaries) {
      expect(summary).toHaveTextContent(/미수집|연결 후|확인할 수 없/);
      expect(summary).not.toHaveTextContent(/\d/);
      expect(within(summary).getByLabelText(/미수집|확인.*불가/)).toBeInTheDocument();
    }
  });

  it('cannot issue operating commands or call services during the available interaction flow', async () => {
    const fetchRequest = vi.fn(() => Promise.reject(new Error('Unexpected service request')));
    const xhrRequest = vi.spyOn(XMLHttpRequest.prototype, 'send');
    const socketConnection = vi.fn();
    const eventConnection = vi.fn();
    const beaconRequest = vi.fn();
    vi.stubGlobal('fetch', fetchRequest);
    vi.stubGlobal('WebSocket', socketConnection);
    vi.stubGlobal('EventSource', eventConnection);
    Object.defineProperty(navigator, 'sendBeacon', { configurable: true, value: beaconRequest });
    const user = userEvent.setup();
    render(<App />);

    for (const label of ['서버 운영', '유저 관리', '개발 현황', '서버 운영']) {
      await user.click(screen.getByRole('button', { name: new RegExp(`^\\d ${label}$`) }));
      for (const control of label === '서버 운영' ? within(screen.getByRole('main')).queryAllByRole('button') : []) {
        expect(control).toBeDisabled();
        expect(control).toHaveAccessibleDescription(/연결/);
        await user.click(control);
      }
      expect(screen.queryByRole('form')).not.toBeInTheDocument();
      expect(screen.queryByRole('link')).not.toBeInTheDocument();
      expectUnobservedState();
    }

    expect(screen.getByRole('button', { name: '서버 시작' })).toBeDisabled();
    expect(screen.getByRole('button', { name: '서버 종료' })).toBeDisabled();
    for (const request of [fetchRequest, xhrRequest, socketConnection, eventConnection, beaconRequest]) {
      expect(request).not.toHaveBeenCalled();
    }
  });
});

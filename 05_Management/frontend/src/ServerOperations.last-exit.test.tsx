// Requirement: screen-design.md 「화면」 운영 제어 — 「마지막 종료 종류(정상·강제·비정상·시작 실패·결과 모름)와 종료 코드를
// 보인다」, with backend-design.md 「상태 응답」 lastExit { kind: graceful | forced | abnormal | startFailed | unknown,
// exitCode, signal } or null, and the window-side implementation contract (E/contracts/pr2-frontend-impl-task.md
// 「마지막 종료 표시」): a null exitCode with a signal shows the signal, and a null lastExit says there is no exit record.
//
// Independent verifier test (PR2 verification). The expected words are the design's own words for the five kinds;
// nothing is computed with the screen's code. The status reaches the screen through a fake window.serverOperations,
// and 「창이 다시 보일 때는 바로 다시 읽는다」 (screen-design.md 「화면」 상태 갱신) is used to read a changed answer.
import '@testing-library/jest-dom/vitest';
import { act, cleanup, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import App from './App';

type Json = Record<string, unknown>;

const SERVER: Json = {
  serverId: 'server-1', displayName: 'Dawnholder 라이브 서버',
  state: 'stopped', pid: null, runId: null, startedAt: null, release: null,
  stopRequestedAt: null, stopTimedOut: false,
  port: 41777, portListening: false, portOwner: 'none', portOwnerDetail: null, portLockHeldByOther: false,
};

function statusWith(lastExit: Json | null): Json {
  return { backend: { startedAt: '2026-10-10T00:00:00Z', pid: 501 }, server: SERVER, currentRelease: null, lastExit };
}

function exitRecord(kind: string, exitCode: number | null, signal: number | null): Json {
  return { runId: 'run-7', endedAt: '2026-10-10T04:05:06Z', kind, exitCode, signal };
}

function installBridge(first: Json) {
  const world = { status: first };
  const done = async (): Promise<Json> => ({ ok: true });
  const bridge = {
    readConnection: vi.fn(async (): Promise<Json> => ({ ok: true, connection: { state: 'connected' } })),
    connect: vi.fn(async (): Promise<Json> => ({ ok: true, connection: { state: 'connected' } })),
    readStatus: vi.fn(async (): Promise<Json> => ({ ok: true, status: world.status })),
    startServer: vi.fn(done),
    stopServer: vi.fn(done),
    forceStopServer: vi.fn(done),
    readLogs: vi.fn(async (_query: unknown): Promise<Json> => ({ ok: false, code: 'notConnected', message: '관리 백엔드에 연결되지 않았습니다.' })),
    readReleaseCandidate: vi.fn(async (): Promise<Json> => ({
      ok: true, checkout: { state: 'known', branch: 'main', head: '0123456789abcdef0123456789abcdef01234567' }, currentRelease: null,
    })),
    buildRelease: vi.fn(done),
    selectRelease: vi.fn(done),
  };
  vi.stubGlobal('serverOperations', bridge);
  return { world, bridge };
}

// The design's five kind names. 「정상」 is a part of 「비정상」, so it is matched only when 비 does not precede it.
const KIND_WORDS: Record<string, RegExp> = {
  정상: /(?<!비)정상/,
  강제: /강제/,
  비정상: /비정상/,
  '시작 실패': /시작 실패/,
  '결과 모름': /결과 모름/,
};

function controlText(): string {
  const region = screen.getByRole('region', { name: '운영 제어' });
  return (region.textContent ?? '').replace(/\s+/g, ' ').trim();
}

function kindsShown(text: string): Record<string, boolean> {
  return Object.fromEntries(Object.entries(KIND_WORDS).map(([word, pattern]) => [word, pattern.test(text)]));
}

function onlyKind(word: string | null): Record<string, boolean> {
  return Object.fromEntries(Object.keys(KIND_WORDS).map(name => [name, name === word]));
}

function exitCodeShown(text: string, code: number): boolean {
  return new RegExp(`종료 코드\\s*:?\\s*${code}(?!\\d)`).test(text);
}

function anyExitCodeShown(text: string): boolean {
  return /종료 코드\s*:?\s*-?\d/.test(text);
}

function signalShown(text: string, signal: number): boolean {
  return new RegExp(`(signal|신호)\\s*:?\\s*${signal}(?!\\d)`, 'i').test(text);
}

async function readAgain() {
  // screen-design.md 「화면」: the screen reads the status at once when the window shows again.
  await act(async () => {
    document.dispatchEvent(new Event('visibilitychange'));
  });
}

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});

describe('last exit on the 서버 운영 screen (independent verifier)', () => {
  it.each([
    { kind: 'graceful', word: '정상', exitCode: 0, signal: null },
    { kind: 'forced', word: '강제', exitCode: 137, signal: 9 },
    { kind: 'abnormal', word: '비정상', exitCode: 134, signal: null },
    { kind: 'startFailed', word: '시작 실패', exitCode: 150, signal: null },
  ])('shows $kind as 「$word」 with its exit code $exitCode', async ({ kind, word, exitCode, signal }) => {
    installBridge(statusWith(exitRecord(kind, exitCode, signal)));
    render(<App />);

    await waitFor(() => expect(kindsShown(controlText())[word]).toBe(true));
    const text = controlText();

    expect(kindsShown(text)).toEqual(onlyKind(word));
    expect(exitCodeShown(text, exitCode)).toBe(true);
  });

  it('shows unknown as 「결과 모름」 when neither an exit code nor a signal was recorded', async () => {
    installBridge(statusWith(exitRecord('unknown', null, null)));
    render(<App />);

    await waitFor(() => expect(kindsShown(controlText())['결과 모름']).toBe(true));
    const text = controlText();

    expect(kindsShown(text)).toEqual(onlyKind('결과 모름'));
    expect(anyExitCodeShown(text)).toBe(false);
  });

  it('shows the signal when the exit code is null, and follows a changed answer', async () => {
    const { world } = installBridge(statusWith(exitRecord('forced', 137, 9)));
    render(<App />);
    await waitFor(() => expect(exitCodeShown(controlText(), 137)).toBe(true));

    world.status = statusWith(exitRecord('abnormal', null, 11));
    await readAgain();
    await waitFor(() => expect(kindsShown(controlText())['비정상']).toBe(true));
    const text = controlText();

    expect(kindsShown(text)).toEqual(onlyKind('비정상'));
    expect(signalShown(text, 11)).toBe(true);
    expect(anyExitCodeShown(text)).toBe(false);
  });

  it('says there is no exit record when lastExit becomes null after a recorded exit', async () => {
    const { world, bridge } = installBridge(statusWith(exitRecord('graceful', 0, null)));
    render(<App />);
    await waitFor(() => expect(kindsShown(controlText())['정상']).toBe(true));
    const readsBefore = bridge.readStatus.mock.calls.length;

    world.status = statusWith(null);
    await readAgain();
    await waitFor(() => expect(kindsShown(controlText())['정상']).toBe(false));
    const text = controlText();

    expect(bridge.readStatus.mock.calls.length).toBeGreaterThan(readsBefore);
    expect(kindsShown(text)).toEqual(onlyKind(null));
    expect(anyExitCodeShown(text)).toBe(false);
    expect(text).toMatch(/종료 기록/);
    expect(text).toMatch(/없/);
  });
});

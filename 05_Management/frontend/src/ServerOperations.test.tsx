// Requirement: screen-design.md 「화면」 (연결 안내, 요약 줄, 운영 제어, 운영 버전 올리기, 서버 로그, 상태 갱신,
// 종료 중 표시) and 「IPC 계약」 (window.serverOperations actions with results { ok: true, … } |
// { ok: false, code, message }), goal 「관찰 가능한 완료조건」 1·2·3·7·8, and the lead's S11 answer
// (E/pr2-frontend-tests/raw/ask-2-answer.txt): a periodic status request waits 2 s after the previous
// one ends, so periodic requests never overlap; after a command, 다시 연결 or the window showing again the
// screen reads the status at once, and an older answer that arrives later is dropped.
//
// The App is rendered so the 연결 상태 notice and the bottom status line are checked with the screen.
// window.serverOperations is a fake bridge whose answers use the backend-design.md shapes. The window
// process has already made failure messages Korean, so the screen shows a failed result's message as
// it is. Screen contract beyond the names the design quotes: period buttons 10분·30분·60분 with
// aria-pressed, a textbox named 찾을 낱말, quick buttons 오류·경고, a progressbar while a release builds,
// and the 강제 종료 confirmation as an in-page dialog or alertdialog with a 취소 button. Times on screen
// are not fixed to a format: a shown time is checked by changing the value and seeing the screen change.
// Fails until the implementation step draws App.tsx's Operations with src/ServerOperations.tsx.
import '@testing-library/jest-dom/vitest';
import { act, cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { StrictMode } from 'react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import App from './App';

type Json = Record<string, unknown>;
interface ConnectionView { state: string; reason?: string; exitCode?: number | null }
interface LogQuery { minutes?: unknown; contains?: unknown; limit?: unknown }

const HEAD = '0123456789abcdef0123456789abcdef01234567';
const BRANCH = 'feat/server-operations-screen';
const RUNNING_RELEASE = { commit: 'abcdef0123456789abcdef0123456789abcdef01', builtAt: '2026-10-10T01:00:00Z' };
const NEXT_RELEASE = { commit: '13579bdf13579bdf13579bdf13579bdf13579bdf', builtAt: '2026-10-10T02:00:00Z' };

const STOPPED_SERVER: Json = {
  serverId: 'server-1', displayName: 'Dawnholder 라이브 서버',
  state: 'stopped', pid: null, runId: null, startedAt: null, release: null,
  stopRequestedAt: null, stopTimedOut: false,
  port: 41777, portListening: false, portOwner: 'none', portOwnerDetail: null, portLockHeldByOther: false,
};
const RUNNING_SERVER: Json = {
  ...STOPPED_SERVER,
  state: 'running', pid: 4321, runId: 'run-1', startedAt: '2026-10-10T03:04:05Z', release: RUNNING_RELEASE,
  portListening: true, portOwner: 'self',
};
const STOPPING_SERVER: Json = { ...RUNNING_SERVER, state: 'stopping', stopRequestedAt: '2026-10-10T03:10:00Z', portListening: false, portOwner: 'unknown' };

// The next start uses NEXT_RELEASE, so RUNNING_RELEASE can only reach the screen as the running release.
function status(server: Json = {}, rest: Json = {}): Json {
  return {
    backend: { startedAt: '2026-10-10T00:00:00Z', pid: 501 },
    server: { ...STOPPED_SERVER, ...server },
    currentRelease: NEXT_RELEASE,
    lastExit: null,
    ...rest,
  };
}
const STOPPED = status();
const RUNNING = status(RUNNING_SERVER);
const STOPPING = status(STOPPING_SERVER);
const STOP_TIMED_OUT = status({ ...STOPPING_SERVER, stopTimedOut: true, portListening: true, portOwner: 'self' });

function line(seq: number, stream: string, text: string, collectedAt: string, textTruncated = false): Json {
  return { runId: 'run-1', seq, collectedAt, stream, text, textTruncated };
}
const STARTED_LINE = line(1, 'stdout', 'Server started', '2026-10-10T03:04:05.120Z');
const WARNING_LINE = line(2, 'stderr', 'warning: tick took 120 ms', '2026-10-10T03:04:06.500Z');
function logs(change: Json = {}): Json {
  return {
    serverId: 'server-1', from: '2026-10-10T02:54:05Z', to: '2026-10-10T03:04:05Z',
    lines: [STARTED_LINE, WARNING_LINE],
    truncated: false,
    retention: { oldestCollectedAt: '2026-10-03T03:04:05Z', totalBytes: 2048, recentDeletions: [] },
    ...change,
  };
}

const NOT_CONNECTED = { ok: false, code: 'notConnected', message: '관리 백엔드에 연결되지 않았습니다.' };

function installBridge(initial: { connection?: ConnectionView; status?: Json; logs?: Json } = {}) {
  const world: { connection: ConnectionView; status: Json; logs: Json } = {
    connection: initial.connection ?? { state: 'connected' },
    status: initial.status ?? STOPPED,
    logs: initial.logs ?? logs(),
  };
  const connected = () => world.connection.state === 'connected';
  const bridge = {
    readConnection: vi.fn(async (): Promise<Json> => ({ ok: true, connection: world.connection })),
    connect: vi.fn(async (): Promise<Json> => {
      world.connection = { state: 'connected' };
      return { ok: true, connection: world.connection };
    }),
    readStatus: vi.fn(async (): Promise<Json> => (connected() ? { ok: true, status: world.status } : NOT_CONNECTED)),
    startServer: vi.fn(async (): Promise<Json> => ({ ok: true })),
    stopServer: vi.fn(async (): Promise<Json> => ({ ok: true })),
    forceStopServer: vi.fn(async (): Promise<Json> => ({ ok: true })),
    readLogs: vi.fn(async (_query: unknown): Promise<Json> => (connected() ? { ok: true, logs: world.logs } : NOT_CONNECTED)),
    readReleaseCandidate: vi.fn(async (): Promise<Json> => ({
      ok: true, checkout: { state: 'known', branch: BRANCH, head: HEAD }, currentRelease: world.status.currentRelease ?? null,
    })),
    buildRelease: vi.fn(async (_input: unknown): Promise<Json> => ({ ok: true })),
    selectRelease: vi.fn(async (_input: unknown): Promise<Json> => ({ ok: true })),
  };
  vi.stubGlobal('serverOperations', bridge);
  return { bridge, world };
}

type Bridge = ReturnType<typeof installBridge>['bridge'];

// Text as a person reads it: whitespace collapsed, element boundaries ignored.
const textOf = (element: Element) => (element.textContent ?? '').replace(/\s+/g, ' ').trim();
const mainText = () => textOf(screen.getByRole('main'));
const notice = () => screen.getByRole('complementary', { name: '연결 상태' });
const button = (name: string | RegExp) => screen.getByRole('button', { name });
const queries = (bridge: Bridge) => bridge.readLogs.mock.calls.map(call => (call[0] ?? {}) as LogQuery);
const lastQuery = (bridge: Bridge): LogQuery => queries(bridge).at(-1) ?? {};
const nonEmptyWords = (words: unknown) => Array.isArray(words) && words.length > 0 && words.every(word => typeof word === 'string' && word.length > 0);

// The one table row holding a log line's text; a cut mark beside the text in the same cell still matches.
function rowText(text: string): string {
  const rows = screen.queryAllByRole('row').filter(row => textOf(row).includes(text));
  if (rows.length !== 1) throw new Error(`expected one row with "${text}", found ${rows.length}`);
  return textOf(rows[0] as HTMLElement);
}
const lineShown = (text: string) => screen.queryAllByRole('row').some(row => textOf(row).includes(text));
// 다시 읽기 once the screen has its connection, so the click is not lost on a still-disabled button.
async function clickReread(user: ReturnType<typeof userEvent.setup>) {
  const reread = await screen.findByRole('button', { name: '다시 읽기' });
  await waitFor(() => expect(reread).toBeEnabled());
  await user.click(reread);
}

// An open in-page confirmation; a closed <dialog> element does not count.
function openConfirmation(): HTMLElement | null {
  const candidates = [...screen.queryAllByRole('alertdialog'), ...screen.queryAllByRole('dialog')];
  return candidates.find(element => element.tagName !== 'DIALOG' || element.hasAttribute('open')) ?? null;
}
function confirmation(): HTMLElement {
  const element = openConfirmation();
  if (!element) throw new Error('no in-screen 강제 종료 confirmation is open');
  return element;
}

function setWindowVisible(visible: boolean) {
  Object.defineProperty(document, 'visibilityState', { configurable: true, get: () => (visible ? 'visible' : 'hidden') });
  Object.defineProperty(document, 'hidden', { configurable: true, get: () => !visible });
  document.dispatchEvent(new Event('visibilitychange'));
}

// For the periodic tests: all timers fake, moved inside act so React applies the answers.
function useFullFakeTimers() {
  vi.useRealTimers();
  vi.useFakeTimers();
  vi.setSystemTime(new Date('2026-10-10T04:00:00Z'));
}
async function advance(ms: number) {
  await act(async () => { await vi.advanceTimersByTimeAsync(ms); });
}
async function clickNow(element: HTMLElement) {
  await act(async () => { fireEvent.click(element); });
}

const originalBeacon = Object.getOwnPropertyDescriptor(navigator, 'sendBeacon');

beforeEach(() => {
  // Shown times depend on the clock; a fixed Date keeps two renders comparable. Other timers stay real.
  vi.useFakeTimers({ toFake: ['Date'] });
  vi.setSystemTime(new Date('2026-10-10T04:00:00Z'));
});

afterEach(() => {
  cleanup();
  vi.useRealTimers();
  vi.unstubAllGlobals();
  Reflect.deleteProperty(document, 'visibilityState');
  Reflect.deleteProperty(document, 'hidden');
  if (originalBeacon) Object.defineProperty(navigator, 'sendBeacon', originalBeacon);
  else Reflect.deleteProperty(navigator, 'sendBeacon');
});

describe('S1 without the desktop bridge', () => {
  it('S1 keeps the unconnected screen: every operations control disabled and 관리 기능 미연결', () => {
    render(<App />);
    expect(notice()).toHaveTextContent('관리 기능 미연결');
    const controls = within(screen.getByRole('main')).getAllByRole('button');
    expect(controls.map(control => [textOf(control), control.hasAttribute('disabled')])).toEqual(controls.map(control => [textOf(control), true]));
    expect(button('서버 시작')).toBeDisabled();
    expect(button('서버 종료')).toBeDisabled();
  });
});

describe('S2 connection notice', () => {
  it('S2 disconnected: 관리 기능 미연결, 서버 시작 disabled, and 다시 연결 calls connect and then reads the status', async () => {
    const { bridge } = installBridge({ connection: { state: 'disconnected', reason: 'startTimeout' } });
    const user = userEvent.setup();
    render(<App />);
    const reconnect = await screen.findByRole('button', { name: '다시 연결' });
    expect(notice()).toHaveTextContent('관리 기능 미연결');
    expect(button('서버 시작')).toBeDisabled();
    const statusReads = bridge.readStatus.mock.calls.length;
    await user.click(reconnect);
    expect(bridge.connect).toHaveBeenCalledOnce();
    await waitFor(() => expect(notice()).toHaveTextContent('관리 기능 연결됨'));
    expect(screen.getByRole('contentinfo')).toHaveTextContent('관리 기능 연결됨');
    await waitFor(() => expect(bridge.readStatus.mock.calls.length).toBeGreaterThan(statusReads));
  });

  it('S2 shows the reason: a start timeout and an exited backend read differently', async () => {
    const shown: string[] = [];
    for (const connection of [{ state: 'disconnected', reason: 'startTimeout' }, { state: 'disconnected', reason: 'backendExited', exitCode: 3 }]) {
      installBridge({ connection });
      render(<App />);
      await screen.findByRole('button', { name: '다시 연결' });
      shown.push(mainText());
      cleanup();
    }
    expect({ bothUnconnected: shown.every(text => text.includes('관리 기능 미연결')), differ: shown[0] !== shown[1] }).toEqual({ bothUnconnected: true, differ: true });
  });
});

describe('S3·S4 operations control and summary', () => {
  it('S3 connected and stopped: 서버 시작 enabled, 서버 종료 disabled; starting calls startServer and blocks both until it ends', async () => {
    const { bridge } = installBridge({ status: STOPPED });
    let finish: (value: Json) => void = () => {};
    bridge.startServer.mockImplementationOnce(() => new Promise<Json>(resolve => { finish = resolve; }));
    const user = userEvent.setup();
    render(<App />);
    await waitFor(() => expect(button('서버 시작')).toBeEnabled());
    const start = button('서버 시작');
    const stop = button('서버 종료');
    expect(stop).toBeDisabled();
    await user.click(start);
    expect(bridge.startServer).toHaveBeenCalledOnce();
    expect({ start: start.hasAttribute('disabled'), stop: stop.hasAttribute('disabled') }).toEqual({ start: true, stop: true });
    await user.click(start);
    expect(bridge.startServer).toHaveBeenCalledOnce();
    await act(async () => { finish({ ok: true }); });
  });

  it('S4 running: 실행 중, the pid, the running release and the different next release; 서버 종료 enabled, 서버 시작 disabled', async () => {
    installBridge({ status: RUNNING });
    render(<App />);
    await waitFor(() => expect(button('서버 종료')).toBeEnabled());
    expect(button('서버 시작')).toBeDisabled();
    const shown = mainText();
    expect({
      running: shown.includes('실행 중'),
      pid: shown.includes('4321'),
      runningRelease: shown.includes(RUNNING_RELEASE.commit.slice(0, 8)),
      nextRelease: shown.includes(NEXT_RELEASE.commit.slice(0, 8)),
    }).toEqual({ running: true, pid: true, runningRelease: true, nextRelease: true });
  });

  it('S4 shows the start time: another startedAt changes the screen', async () => {
    const shown: string[] = [];
    for (const startedAt of ['2026-10-10T03:04:05Z', '2026-10-10T08:41:27Z']) {
      installBridge({ status: status({ ...RUNNING_SERVER, startedAt }) });
      render(<App />);
      await waitFor(() => expect(button('서버 종료')).toBeEnabled());
      shown.push(mainText());
      cleanup();
    }
    expect(shown[0]).not.toBe(shown[1]);
  });
});

describe('S5·S6·S7 stopping and the port', () => {
  it('S5 a stop past its time limit shows 강제 종료, which calls forceStopServer only after an in-screen confirmation', async () => {
    const { bridge } = installBridge({ status: STOP_TIMED_OUT });
    const browserConfirm = vi.fn(() => true);
    vi.stubGlobal('confirm', browserConfirm);
    const user = userEvent.setup();
    render(<App />);
    await user.click(await screen.findByRole('button', { name: '강제 종료' }));
    expect(bridge.forceStopServer).not.toHaveBeenCalled();
    await user.click(within(confirmation()).getByRole('button', { name: '취소' }));
    expect({ forced: bridge.forceStopServer.mock.calls.length, open: openConfirmation() }).toEqual({ forced: 0, open: null });
    await user.click(button('강제 종료'));
    await user.click(within(confirmation()).getByRole('button', { name: /강제 종료/ }));
    await waitFor(() => expect(bridge.forceStopServer).toHaveBeenCalledOnce());
    expect(browserConfirm).not.toHaveBeenCalled();
  });

  it('S5 has no 강제 종료 while running or while a stop is still within its time limit', async () => {
    const seen: unknown[] = [];
    for (const [name, current] of [['running', RUNNING], ['stopping', STOPPING]] as const) {
      installBridge({ status: current });
      render(<App />);
      await waitFor(() => expect(mainText()).toMatch(/실행 중|종료 중/));
      seen.push({ name, forceStop: screen.queryByRole('button', { name: '강제 종료' }) !== null });
      cleanup();
    }
    expect(seen).toEqual([{ name: 'running', forceStop: false }, { name: 'stopping', forceStop: false }]);
  });

  it('S6 says another run holds the port lock, with 남의 실행은 끄지 않습니다', async () => {
    installBridge({ status: status({ portListening: true, portOwner: 'other', portOwnerDetail: 'pid 4242 dotnet', portLockHeldByOther: true }) });
    render(<App />);
    await waitFor(() => expect(mainText()).toContain('남의 실행은 끄지 않습니다'));
    expect(mainText()).toMatch(/다른 실행/);
  });

  it('S7 stopping with portOwner unknown shows 종료 중 without a warning', async () => {
    installBridge({ status: STOPPING });
    render(<App />);
    await waitFor(() => expect(mainText()).toContain('종료 중'));
    const shown = mainText();
    expect({ otherRun: /다른 실행/.test(shown), notTheirs: shown.includes('남의 실행은 끄지 않습니다'), alerts: screen.queryAllByRole('alert').length })
      .toEqual({ otherRun: false, notTheirs: false, alerts: 0 });
  });
});

describe('S8 failures', () => {
  it('S8 shows the Korean message of a failed command', async () => {
    const { bridge } = installBridge({ status: STOPPED });
    bridge.startServer.mockResolvedValueOnce({ ok: false, code: 'portBusy', message: '시험 문구: 서버 포트를 다른 실행이 쓰고 있습니다.' });
    const user = userEvent.setup();
    render(<App />);
    await waitFor(() => expect(button('서버 시작')).toBeEnabled());
    await user.click(button('서버 시작'));
    expect(await screen.findByText(/시험 문구: 서버 포트를 다른 실행이 쓰고 있습니다\./)).toBeVisible();
  });
});

describe('S9 operating version', () => {
  it('S9 shows the checkout branch and HEAD with the notes on uncommitted changes and the next start', async () => {
    installBridge();
    render(<App />);
    await waitFor(() => expect(mainText()).toContain(BRANCH));
    const shown = mainText();
    expect({ head: shown.includes(HEAD.slice(0, 8)), uncommitted: /커밋하지 않은/.test(shown), nextStart: /다음 시작부터/.test(shown) })
      .toEqual({ head: true, uncommitted: true, nextStart: true });
  });

  it('S9 builds the HEAD commit with a progress indicator, then selects that same commit', async () => {
    const { bridge } = installBridge();
    let finishBuild: (value: Json) => void = () => {};
    bridge.buildRelease.mockImplementationOnce(() => new Promise<Json>(resolve => { finishBuild = resolve; }));
    const user = userEvent.setup();
    render(<App />);
    const build = await screen.findByRole('button', { name: '이 commit으로 운영 버전 만들기' });
    await waitFor(() => expect(build).toBeEnabled());
    const selectBefore = screen.queryByRole('button', { name: '현재 운영 버전으로 지정' });
    expect(selectBefore === null || selectBefore.hasAttribute('disabled')).toBe(true);
    await user.click(build);
    expect(bridge.buildRelease.mock.calls).toEqual([[{ commit: HEAD }]]);
    expect({ progress: screen.queryAllByRole('progressbar').length > 0, buildBlocked: build.hasAttribute('disabled') || !build.isConnected })
      .toEqual({ progress: true, buildBlocked: true });
    await act(async () => { finishBuild({ ok: true }); });
    const select = await screen.findByRole('button', { name: '현재 운영 버전으로 지정' });
    await waitFor(() => expect(select).toBeEnabled());
    expect(screen.queryAllByRole('progressbar')).toHaveLength(0);
    await user.click(select);
    await waitFor(() => expect(bridge.selectRelease.mock.calls).toEqual([[{ commit: HEAD }]]));
  });
});

describe('S10 server log', () => {
  it('S10 reads 10 minutes by default; the period buttons, the words box and 다시 읽기 decide the next read', async () => {
    const { bridge } = installBridge();
    const user = userEvent.setup();
    render(<App />);
    const reread = await screen.findByRole('button', { name: '다시 읽기' });
    await waitFor(() => expect(reread).toBeEnabled());
    await user.click(reread);
    await waitFor(() => expect(bridge.readLogs).toHaveBeenCalled());
    const first = lastQuery(bridge);
    const limit = first.limit;
    expect({
      minutes: first.minutes,
      contains: first.contains,
      limitInRange: typeof limit === 'number' && Number.isInteger(limit) && limit >= 1 && limit <= 5000,
      tenPressed: button('10분').getAttribute('aria-pressed'),
    }).toEqual({ minutes: 10, contains: [], limitInRange: true, tenPressed: 'true' });
    await user.click(button('30분'));
    await user.type(screen.getByRole('textbox', { name: /찾을 낱말/ }), 'error, timeout');
    await user.click(button('다시 읽기'));
    await waitFor(() => expect(lastQuery(bridge)).toMatchObject({ minutes: 30, contains: ['error', 'timeout'] }));
    expect(button('30분')).toHaveAttribute('aria-pressed', 'true');
  });

  it('S10 refuses a sixth word on the screen', async () => {
    const { bridge } = installBridge();
    const user = userEvent.setup();
    render(<App />);
    const words = await screen.findByRole('textbox', { name: /찾을 낱말/ });
    await waitFor(() => expect(button('다시 읽기')).toBeEnabled());
    await user.type(words, 'a, b, c, d, e, f');
    await user.click(button('다시 읽기'));
    await act(async () => { await new Promise(resolve => { setTimeout(resolve, 50); }); });
    const sent = queries(bridge).map(query => query.contains);
    expect(sent.filter(contains => !Array.isArray(contains) || contains.length > 5 || contains.includes('f'))).toEqual([]);
    // Five words still go out, so the refusal above comes from the sixth word and not from a dead button.
    await user.clear(words);
    await user.type(words, 'a, b, c, d, e');
    await user.click(button('다시 읽기'));
    await waitFor(() => expect(lastQuery(bridge).contains).toEqual(['a', 'b', 'c', 'd', 'e']));
  });

  it('S10 the 오류 and 경고 quick buttons each read with their own nonempty words', async () => {
    const picked: unknown[] = [];
    for (const quick of ['오류', '경고']) {
      const { bridge } = installBridge();
      const user = userEvent.setup();
      render(<App />);
      await user.click(await screen.findByRole('button', { name: quick }));
      await waitFor(() => expect(button('다시 읽기')).toBeEnabled());
      await user.click(button('다시 읽기'));
      await waitFor(() => expect(bridge.readLogs).toHaveBeenCalled());
      picked.push(lastQuery(bridge).contains);
      cleanup();
    }
    expect({ errorWords: nonEmptyWords(picked[0]), warningWords: nonEmptyWords(picked[1]), differ: JSON.stringify(picked[0]) !== JSON.stringify(picked[1]) })
      .toEqual({ errorWords: true, warningWords: true, differ: true });
  });

  it('S10 shows each line under 수집 시각 in local time, with its stream and its text', async () => {
    installBridge();
    const user = userEvent.setup();
    render(<App />);
    await clickReread(user);
    expect(await screen.findByRole('columnheader', { name: '수집 시각' })).toBeVisible();
    await waitFor(() => expect(lineShown('Server started')).toBe(true));
    const started = rowText('Server started');
    const warning = rowText('warning: tick took 120 ms');
    expect({
      startedStream: started.includes('stdout'),
      warningStream: warning.includes('stderr'),
      rawUtcShown: started.includes(String(STARTED_LINE.collectedAt)),
      clockTime: /\d{1,2}:\d{2}/.test(started),
    }).toEqual({ startedStream: true, warningStream: true, rawUtcShown: false, clockTime: true });
  });

  it('S10 shows that the result was cut, that a line was cut, and the retention information', async () => {
    async function shownFor(answer: Json) {
      installBridge({ logs: answer });
      const user = userEvent.setup();
      render(<App />);
      await clickReread(user);
      await waitFor(() => expect(lineShown('Server started')).toBe(true));
      const shown = { all: mainText(), started: rowText('Server started'), warning: rowText('warning: tick took 120 ms') };
      cleanup();
      return shown;
    }
    const plain = await shownFor(logs());
    const resultCut = await shownFor(logs({ truncated: true }));
    const lineCut = await shownFor(logs({ lines: [STARTED_LINE, { ...WARNING_LINE, textTruncated: true }] }));
    const retention = await shownFor(logs({
      retention: { oldestCollectedAt: '2026-10-05T09:30:00Z', totalBytes: 734003200, recentDeletions: [{ from: '2026-10-01T00:00:00Z', to: '2026-10-02T00:00:00Z', bytes: 4096, reason: 'size' }] },
    }));
    expect({
      resultCutShown: resultCut.all !== plain.all,
      lineCutShown: lineCut.warning !== plain.warning,
      otherLineUnchanged: lineCut.started === plain.started,
      retentionShown: retention.all !== plain.all,
    }).toEqual({ resultCutShown: true, lineCutShown: true, otherLineUnchanged: true, retentionShown: true });
  });

  it('S10 reads the logs again every 5 s only while the server runs, the window is shown and 서버 운영 is open', async () => {
    useFullFakeTimers();
    const { bridge, world } = installBridge({ status: RUNNING });
    render(<App />);
    await advance(1_000);
    const reads = () => bridge.readLogs.mock.calls.length;
    let base = reads();
    await advance(20_000);
    const whileRunning = reads() - base;
    setWindowVisible(false);
    await advance(1_000);
    base = reads();
    await advance(20_000);
    const whileHidden = reads() - base;
    setWindowVisible(true);
    await advance(1_000);
    await clickNow(button(/^\d 유저 관리$/));
    await advance(1_000);
    base = reads();
    await advance(20_000);
    const elsewhere = reads() - base;
    world.status = STOPPED;
    await clickNow(button(/^\d 서버 운영$/));
    await advance(6_000);
    base = reads();
    await advance(20_000);
    const whileStopped = reads() - base;
    expect({ runningAbout4: whileRunning >= 3 && whileRunning <= 5, whileHidden, elsewhere, whileStopped })
      .toEqual({ runningAbout4: true, whileHidden: 0, elsewhere: 0, whileStopped: 0 });
  });
});

describe('S11 status updates', () => {
  it('S11 asks for the status every 2 s while 서버 운영 and the window are shown, stops when hidden and reads at once when shown again', async () => {
    useFullFakeTimers();
    const { bridge } = installBridge();
    render(<App />);
    await advance(500);
    const reads = () => bridge.readStatus.mock.calls.length;
    let base = reads();
    await advance(10_000);
    const whileShown = reads() - base;
    setWindowVisible(false);
    await advance(500);
    base = reads();
    await advance(10_000);
    const whileHidden = reads() - base;
    base = reads();
    setWindowVisible(true);
    await advance(100);
    const onShow = reads() - base;
    await clickNow(button(/^\d 유저 관리$/));
    await advance(500);
    base = reads();
    await advance(10_000);
    const elsewhere = reads() - base;
    expect({ about5: whileShown >= 4 && whileShown <= 6, whileHidden, readOnShow: onShow >= 1, elsewhere })
      .toEqual({ about5: true, whileHidden: 0, readOnShow: true, elsewhere: 0 });
  });

  it('S11 sends no other periodic status request while one is unanswered, and the next one 2 s after its answer', async () => {
    useFullFakeTimers();
    const { bridge } = installBridge();
    render(<App />);
    await advance(500);
    const pending: Array<(value: Json) => void> = [];
    bridge.readStatus.mockImplementation(() => new Promise<Json>(resolve => { pending.push(resolve); }));
    await advance(2_500);
    const held = bridge.readStatus.mock.calls.length;
    await advance(10_000);
    const whileUnanswered = bridge.readStatus.mock.calls.length - held;
    bridge.readStatus.mockImplementation(async () => ({ ok: true, status: STOPPED }));
    await act(async () => { pending[0]?.({ ok: true, status: STOPPED }); });
    await advance(1_900);
    const before2s = bridge.readStatus.mock.calls.length - held;
    await advance(300);
    const after2s = bridge.readStatus.mock.calls.length - held;
    expect({ heldRequests: pending.length, whileUnanswered, before2s, after2s }).toEqual({ heldRequests: 1, whileUnanswered: 0, before2s: 0, after2s: 1 });
  });

  it('S11 keeps the newer status when an older periodic answer arrives after the read that follows 서버 시작', async () => {
    useFullFakeTimers();
    const { bridge } = installBridge({ status: STOPPED });
    render(<App />);
    await advance(500);
    const pending: Array<(value: Json) => void> = [];
    bridge.readStatus.mockImplementation(() => new Promise<Json>(resolve => { pending.push(resolve); }));
    // The next periodic read starts and stays unanswered.
    await advance(2_500);
    await clickNow(button('서버 시작'));
    await advance(100);
    const runningAs = (pid: number) => ({ ok: true, status: status({ ...RUNNING_SERVER, pid }) });
    await act(async () => { pending[1]?.(runningAs(2222)); });
    await act(async () => { pending[0]?.(runningAs(1111)); });
    await advance(100);
    const shown = mainText();
    expect({ started: bridge.startServer.mock.calls.length, twoReads: pending.length >= 2, newer: shown.includes('2222'), older: shown.includes('1111') })
      .toEqual({ started: 1, twoReads: true, newer: true, older: false });
  });

  it('S11 reads the status at once after 서버 종료, 강제 종료 and 현재 운영 버전으로 지정, before the next periodic read', async () => {
    useFullFakeTimers();
    const seen: unknown[] = [];
    for (const [action, current] of [['서버 종료', RUNNING], ['강제 종료', STOP_TIMED_OUT], ['현재 운영 버전으로 지정', STOPPED]] as const) {
      const { bridge } = installBridge({ status: current });
      render(<App />);
      await advance(500);
      if (action === '현재 운영 버전으로 지정') {
        await clickNow(button('이 commit으로 운영 버전 만들기'));
        await advance(100);
      }
      // The periodic read after the first one is still more than 1 s away.
      const base = bridge.readStatus.mock.calls.length;
      await clickNow(button(action));
      if (action === '강제 종료') await clickNow(within(confirmation()).getByRole('button', { name: /강제 종료/ }));
      await advance(100);
      const sent = { '서버 종료': bridge.stopServer, '강제 종료': bridge.forceStopServer, '현재 운영 버전으로 지정': bridge.selectRelease }[action].mock.calls.length;
      seen.push({ action, sent, readAtOnce: bridge.readStatus.mock.calls.length > base });
      cleanup();
    }
    expect(seen).toEqual([
      { action: '서버 종료', sent: 1, readAtOnce: true },
      { action: '강제 종료', sent: 1, readAtOnce: true },
      { action: '현재 운영 버전으로 지정', sent: 1, readAtOnce: true },
    ]);
  });

  it('S11 keeps the last good status with 마지막 확인 시각 when a status request fails', async () => {
    useFullFakeTimers();
    const { bridge } = installBridge({ status: RUNNING });
    render(<App />);
    await advance(500);
    bridge.readStatus.mockResolvedValue({ ok: false, code: 'backendUnreachable', message: '관리 백엔드에 닿지 않습니다.' });
    await advance(2_500);
    const shown = mainText();
    expect({ failedReadMade: bridge.readStatus.mock.calls.length >= 2, pid: shown.includes('4321'), running: shown.includes('실행 중'), lastChecked: shown.includes('마지막 확인 시각') })
      .toEqual({ failedReadMade: true, pid: true, running: true, lastChecked: true });
  });

  it('S11 keeps the newer status when the first mount read answers late (StrictMode runs the mount effect twice)', async () => {
    const { bridge } = installBridge();
    const pending: Array<(value: Json) => void> = [];
    bridge.readStatus.mockImplementation(() => new Promise<Json>(resolve => { pending.push(resolve); }));
    render(<StrictMode><App /></StrictMode>);
    await waitFor(() => expect(pending.length).toBeGreaterThanOrEqual(1));
    await act(async () => { await new Promise(resolve => { setTimeout(resolve, 50); }); });
    const runningAs = (pid: number) => ({ ok: true, status: status({ ...RUNNING_SERVER, pid }) });
    await act(async () => { pending.at(-1)?.(runningAs(2222)); });
    for (const answer of pending.slice(0, -1)) await act(async () => { answer(runningAs(1111)); });
    const shown = mainText();
    expect({ newer: shown.includes('2222'), older: shown.includes('1111') }).toEqual({ newer: true, older: false });
  });
});

describe('S12·S13 scope', () => {
  it('S12 still shows 접속 데이터 미수집 for connections while connected and running', async () => {
    installBridge({ status: RUNNING });
    render(<App />);
    await waitFor(() => expect(mainText()).toContain('4321'));
    expect(mainText()).toContain('접속 데이터 미수집');
  });

  it('S13 uses only the bridge: no fetch, XMLHttpRequest, WebSocket, EventSource or sendBeacon through start, logs and a release build', async () => {
    const fetchRequest = vi.fn(() => Promise.reject(new Error('Unexpected service request')));
    const xhrRequest = vi.spyOn(XMLHttpRequest.prototype, 'send');
    const socketConnection = vi.fn();
    const eventConnection = vi.fn();
    const beaconRequest = vi.fn();
    vi.stubGlobal('fetch', fetchRequest);
    vi.stubGlobal('WebSocket', socketConnection);
    vi.stubGlobal('EventSource', eventConnection);
    Object.defineProperty(navigator, 'sendBeacon', { configurable: true, value: beaconRequest });
    const { bridge } = installBridge({ status: STOPPED });
    const user = userEvent.setup();
    render(<App />);
    await waitFor(() => expect(button('서버 시작')).toBeEnabled());
    await user.click(button('서버 시작'));
    await user.click(await screen.findByRole('button', { name: '다시 읽기' }));
    await user.click(await screen.findByRole('button', { name: '이 commit으로 운영 버전 만들기' }));
    await waitFor(() => expect(bridge.buildRelease).toHaveBeenCalled());
    expect({ start: bridge.startServer.mock.calls.length, logs: bridge.readLogs.mock.calls.length > 0 }).toEqual({ start: 1, logs: true });
    for (const request of [fetchRequest, xhrRequest, socketConnection, eventConnection, beaconRequest]) expect(request).not.toHaveBeenCalled();
  });
});

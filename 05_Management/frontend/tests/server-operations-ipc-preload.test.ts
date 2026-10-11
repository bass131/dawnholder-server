// @vitest-environment node
// Requirement: screen-design.md 「IPC 계약」 (ten server-operations:<action> channels, trustedSender
// first, results { ok: true, … } | { ok: false, code, message }, logs input filtered in the window
// process, release-build and release-select only for a 40-character lower-case commit equal to the
// checkout HEAD), 「모듈 배치」 (main.ts creates the connection object and starts it when the app is
// ready; preload.cts exposes serverOperations), 「시작과 붙기」 1 and 「앱 종료(트레이 「종료」) — 잠정」
// (ask while the server runs, cancel keeps everything, otherwise shut the connection down and then
// quit; X only hides). Electron is a test double (the tests/records-ipc-preload.test.ts approach), and
// so are the connection object (tests/server-operations-fixtures.ts, the shape tests/backend-connection.test.ts
// fixes) and the checkout store, so no wsl.exe, window or git process starts.
//
// Outside contract for the implementer beyond the design: the connection handler returns
// { ok: true, connection: connectionState() }, connect returns { ok: true, connection: <connect()
// result> }, release-candidate returns { ok: true, checkout: <checkout store result>, currentRelease },
// window-side refusals use a code other than denied with a Korean message, and the tray 「종료」 reads
// connection.readStatus() and asks with dialog.showMessageBox(…, { buttons: [two], cancelId, message })
// before connection.shutdown() and app.quit(). The preload bridge methods are readConnection, connect,
// readStatus, startServer, stopServer, forceStopServer, readLogs, readReleaseCandidate, buildRelease and
// selectRelease. Fails until the implementation step.
import { EventEmitter } from 'node:events';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { transformWithOxc } from 'vite';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { LOGS, RELEASES, STATUS_RUNNING, STATUS_STOPPED, type CreatedConnection } from './server-operations-fixtures';

vi.mock('node:fs', async original => ({ ...await original<typeof import('node:fs')>(), mkdirSync: vi.fn() }));

const backend = vi.hoisted(() => ({ created: [] as CreatedConnection[] }));
vi.mock('../electron/backend-connection.js', async () => (await import('./server-operations-fixtures')).backendConnectionModule(backend.created));

const checkout = vi.hoisted(() => ({ read: vi.fn() }));
vi.mock('../electron/checkout-store.js', () => ({ createCheckoutStore: vi.fn(() => ({ read: checkout.read })) }));

const MAIN_URL = new URL('../electron/main.ts', import.meta.url);
const HEAD = '0123456789abcdef0123456789abcdef01234567';
const OTHER_COMMIT = 'fedcba9876543210fedcba9876543210fedcba98';
const CHECKOUT = { state: 'known', branch: 'feat/server-operations-screen', head: HEAD };
const HANGUL = /[가-힣]/;
const EXISTING_CHANNELS = ['system-backlog:read', 'system-guide:read', 'system-records:read', 'system-records:read-checkout', 'system-records:read-section'];
const SERVER_CHANNELS = ['connection', 'connect', 'status', 'start', 'stop', 'force-stop', 'logs', 'release-candidate', 'release-build', 'release-select']
  .map(action => `server-operations:${action}`);

type Handler = (event: unknown, input?: unknown) => Promise<unknown>;
type MenuItem = { label?: string; type?: string; click?: () => unknown };
type MessageBoxOptions = { buttons?: string[]; cancelId?: number; message?: string; detail?: string };

function deferred<T>() {
  let resolve: (value: T) => void = () => {};
  const promise = new Promise<T>(settle => { resolve = settle; });
  return { promise, resolve };
}

// Only what electron/main.ts touches; whenReady waits for the test so readiness can be observed.
function makeHost() {
  const mainFrame = { url: new URL('../dist/index.html', MAIN_URL).href };
  const contents = Object.assign(new EventEmitter(), { mainFrame, setWindowOpenHandler: vi.fn(), setZoomFactor: vi.fn() });
  const window = Object.assign(new EventEmitter(), {
    webContents: contents, removeMenu: vi.fn(), loadFile: vi.fn(async () => undefined), isMinimized: vi.fn(() => false),
    isVisible: vi.fn(() => true), isDestroyed: vi.fn(() => false), restore: vi.fn(), show: vi.fn(), focus: vi.fn(), hide: vi.fn(), destroy: vi.fn(),
  });
  const tray = Object.assign(new EventEmitter(), { setToolTip: vi.fn(), setContextMenu: vi.fn(), destroy: vi.fn() });
  const ready = deferred<void>();
  const app = Object.assign(new EventEmitter(), { whenReady: vi.fn(() => ready.promise), setPath: vi.fn(), setName: vi.fn(), exit: vi.fn(), quit: vi.fn() });
  // Electron's quit lifecycle as tests/desktop-main.test.ts simulates it, with the cancellable close event.
  app.quit.mockImplementation(() => {
    const closeEvent = { preventDefault: vi.fn() };
    app.emit('before-quit');
    window.emit('close', closeEvent);
    if (closeEvent.preventDefault.mock.calls.length === 0) {
      window.emit('closed');
      app.emit('will-quit');
    }
  });
  const dialog = { showMessageBox: vi.fn(async (..._args: unknown[]) => ({ response: 0, checkboxChecked: false })) };
  return {
    window, contents, mainFrame, app, tray, dialog, ready,
    BrowserWindow: vi.fn(function () { return window; }),
    Tray: vi.fn(function () { return tray; }),
    Menu: { buildFromTemplate: vi.fn((items: MenuItem[]) => items) },
    nativeImage: { createFromPath: vi.fn(() => ({ isEmpty: () => false })) },
    session: { defaultSession: { setPermissionRequestHandler: vi.fn(), setPermissionCheckHandler: vi.fn(), on: vi.fn() } },
    protocol: { registerSchemesAsPrivileged: vi.fn(), handle: vi.fn() },
    ipcMain: { handle: vi.fn() },
  };
}

let host: ReturnType<typeof makeHost>;

beforeEach(() => {
  vi.resetModules();
  host = makeHost();
  vi.doMock('electron', () => host);
  backend.created.length = 0;
  checkout.read.mockReset().mockResolvedValue(CHECKOUT);
});

async function start(): Promise<Map<string, Handler>> {
  await import('../electron/main.js');
  host.ready.resolve();
  await vi.waitFor(() => expect(host.window.loadFile).toHaveBeenCalledOnce());
  return new Map(host.ipcMain.handle.mock.calls.map(call => [call[0] as string, call[1] as Handler]));
}

function connection() {
  const entry = backend.created[0];
  if (!entry) throw new Error('main.ts did not create the backend connection');
  return entry.connection;
}

function trusted() {
  return { sender: host.contents, senderFrame: host.mainFrame };
}

function untrusted() {
  return [
    { name: 'other sender', event: { ...trusted(), sender: {} } },
    { name: 'subframe', event: { ...trusted(), senderFrame: { url: host.mainFrame.url } } },
    { name: 'other page', event: { sender: host.contents, senderFrame: { ...host.mainFrame, url: 'file:///untrusted.html' } } },
  ];
}

// A window-side refusal: not the sender denial, and a Korean message for the screen.
function refusal(result: unknown) {
  const { ok, code, message } = (result ?? {}) as { ok?: unknown; code?: unknown; message?: unknown };
  return { ok, codeIsNotDenied: typeof code === 'string' && code !== '' && code !== 'denied', korean: typeof message === 'string' && HANGUL.test(message) };
}
const REFUSED = { ok: false, codeIsNotDenied: true, korean: true };

const pause = () => new Promise(resolve => { setTimeout(resolve, 20); });

describe('server-operations IPC channels', () => {
  it('M1 registers exactly the five existing channels and the ten server-operations channels', async () => {
    const handlers = await start();
    expect([...handlers.keys()].sort()).toEqual([...EXISTING_CHANNELS, ...SERVER_CHANNELS].sort());
  });

  it('M2 denies every server-operations channel to an untrusted sender, subframe, other page or destroyed window without using the connection', async () => {
    const handlers = await start();
    const callCounts = () => Object.fromEntries(Object.entries(connection()).map(([name, method]) => [name, method.mock.calls.length]));
    const before = callCounts();
    const inputs: Record<string, unknown> = {
      'server-operations:logs': { minutes: 10, contains: [], limit: 1000 },
      'server-operations:release-build': { commit: HEAD },
      'server-operations:release-select': { commit: HEAD },
    };
    const seen: Array<[string, string, boolean]> = [];
    const denied = (result: unknown) => (result as { ok?: unknown; code?: unknown } | undefined)?.ok === false && (result as { code?: unknown }).code === 'denied';
    for (const channel of SERVER_CHANNELS) {
      for (const { name, event } of untrusted()) seen.push([channel, name, denied(await handlers.get(channel)?.(event, inputs[channel]))]);
    }
    host.window.isDestroyed.mockReturnValue(true);
    for (const channel of SERVER_CHANNELS) seen.push([channel, 'destroyed window', denied(await handlers.get(channel)?.(trusted(), inputs[channel]))]);
    expect(seen).toEqual(seen.map(([channel, name]) => [channel, name, true]));
    expect(callCounts()).toEqual(before);
    expect(checkout.read).not.toHaveBeenCalled();
  });

  it('M2 hands a trusted sender to the connection: status, start, stop and force-stop call their method without renderer input and return its result', async () => {
    const handlers = await start();
    const double = connection();
    const pairs = [
      ['status', double.readStatus],
      ['start', double.startServer],
      ['stop', double.stopServer],
      ['force-stop', double.forceStopServer],
    ] as const;
    const seen: unknown[] = [];
    for (const [action, method] of pairs) {
      const answer = { ok: true, marker: action };
      method.mockResolvedValueOnce(answer);
      const result = await handlers.get(`server-operations:${action}`)?.(trusted(), '../renderer-input');
      seen.push({ action, sameResult: result === answer, calls: method.mock.calls });
    }
    expect(seen).toEqual(pairs.map(([action]) => ({ action, sameResult: true, calls: [[]] })));
  });

  it('M2 connection returns the connection state, and connect runs the connection start and attach again', async () => {
    const handlers = await start();
    const double = connection();
    const state = { state: 'disconnected', reason: 'startTimeout' };
    double.connectionState.mockReturnValue(state);
    expect(await handlers.get('server-operations:connection')?.(trusted())).toEqual({ ok: true, connection: state });
    const connectsBefore = double.connect.mock.calls.length;
    double.connect.mockResolvedValueOnce({ state: 'connected' });
    expect(await handlers.get('server-operations:connect')?.(trusted())).toEqual({ ok: true, connection: { state: 'connected' } });
    expect(double.connect.mock.calls.length - connectsBefore).toBe(1);
  });

  it('M2 release-candidate returns the checkout branch and HEAD with the current release', async () => {
    const handlers = await start();
    expect(await handlers.get('server-operations:release-candidate')?.(trusted())).toEqual({ ok: true, checkout: CHECKOUT, currentRelease: RELEASES.currentRelease });
  });

  it('M3 refuses malformed logs input before the connection and passes a valid query on', async () => {
    const handle = (await start()).get('server-operations:logs');
    const double = connection();
    const rejected: unknown[] = [
      null,
      'minutes=10',
      [10, [], 1000],
      { minutes: 0, contains: [], limit: 1000 },
      { minutes: '10', contains: [], limit: 1000 },
      { minutes: 10, contains: ['a', 'b', 'c', 'd', 'e', 'f'], limit: 1000 },
      { minutes: 10, contains: ['a'.repeat(65)], limit: 1000 },
      { minutes: 10, contains: [], limit: 5001 },
    ];
    const seen: unknown[] = [];
    for (const input of rejected) seen.push(refusal(await handle?.(trusted(), input)));
    expect(seen).toEqual(rejected.map(() => REFUSED));
    expect(double.readLogs).not.toHaveBeenCalled();
    const answer = { ok: true, logs: LOGS };
    double.readLogs.mockResolvedValueOnce(answer);
    const result = await handle?.(trusted(), { minutes: 30, contains: ['error', 'warn'], limit: 500 });
    expect({ sameResult: result === answer, calls: double.readLogs.mock.calls }).toEqual({ sameResult: true, calls: [[{ minutes: 30, contains: ['error', 'warn'], limit: 500 }]] });
  });

  for (const [action, method] of [['release-build', 'buildRelease'], ['release-select', 'selectRelease']] as const) {
    it(`M3 ${action} calls the connection only with a commit equal to the checkout HEAD`, async () => {
      const handle = (await start()).get(`server-operations:${action}`);
      const double = connection();
      const rejected: unknown[] = [
        { commit: OTHER_COMMIT },
        { commit: HEAD.toUpperCase() },
        { commit: HEAD.slice(1) },
        { commit: `${HEAD}0` },
        HEAD,
        null,
        {},
      ];
      const seen: unknown[] = [];
      for (const input of rejected) seen.push(refusal(await handle?.(trusted(), input)));
      checkout.read.mockResolvedValueOnce({ state: 'unknown', reason: 'no-git' });
      seen.push(refusal(await handle?.(trusted(), { commit: HEAD })));
      expect(seen).toEqual([...rejected, 'unknown checkout'].map(() => REFUSED));
      expect(double[method]).not.toHaveBeenCalled();
      const answer = { ok: true };
      double[method].mockResolvedValueOnce(answer);
      const result = await handle?.(trusted(), { commit: HEAD });
      expect({ sameResult: result === answer, calls: double[method].mock.calls }).toEqual({ sameResult: true, calls: [[HEAD]] });
    });
  }
});

describe('backend connection lifetime in the main process', () => {
  it('M4 creates the backend connection from the checkout root and starts it once, after the app is ready', async () => {
    await import('../electron/main.js');
    await pause();
    const connectsBeforeReady = backend.created.reduce((sum, entry) => sum + entry.connection.connect.mock.calls.length, 0);
    host.ready.resolve();
    await vi.waitFor(() => expect(host.window.loadFile).toHaveBeenCalledOnce());
    await vi.waitFor(() => expect(connection().connect).toHaveBeenCalled());
    await pause();
    expect({ created: backend.created.length, connectsBeforeReady, connects: connection().connect.mock.calls.length })
      .toEqual({ created: 1, connectsBeforeReady: 0, connects: 1 });
    expect(backend.created[0]?.options).toMatchObject({ repositoryRoot: fileURLToPath(new URL('../../../', MAIN_URL)) });
  });

  function quitFromTray() {
    const menu = host.Menu.buildFromTemplate.mock.calls[0]?.[0];
    menu?.find(item => item.label === '종료')?.click?.();
  }
  const askedOptions = () => host.dialog.showMessageBox.mock.calls[0]?.at(-1) as MessageBoxOptions | undefined;

  it('M5 asks before quitting while the server runs, and cancelling keeps the app and the connection', async () => {
    await start();
    const double = connection();
    double.readStatus.mockResolvedValue({ ok: true, status: STATUS_RUNNING });
    host.dialog.showMessageBox.mockImplementation(async (...args: unknown[]) => ({ response: (args.at(-1) as MessageBoxOptions).cancelId ?? -1, checkboxChecked: false }));
    quitFromTray();
    await vi.waitFor(() => expect(host.dialog.showMessageBox).toHaveBeenCalledOnce());
    await pause();
    const options = askedOptions();
    expect({
      buttons: options?.buttons?.length,
      cancelIsAButton: options?.cancelId === 0 || options?.cancelId === 1,
      mentionsServer: /서버/.test(`${options?.message ?? ''} ${options?.detail ?? ''}`),
    }).toEqual({ buttons: 2, cancelIsAButton: true, mentionsServer: true });
    expect({ shutdown: double.shutdown.mock.calls.length, quit: host.app.quit.mock.calls.length, exit: host.app.exit.mock.calls.length })
      .toEqual({ shutdown: 0, quit: 0, exit: 0 });
  });

  it('M5 after the confirmation, quits the app only once the connection has shut down', async () => {
    await start();
    const double = connection();
    double.readStatus.mockResolvedValue({ ok: true, status: STATUS_RUNNING });
    const answer = deferred<{ response: number; checkboxChecked: boolean }>();
    host.dialog.showMessageBox.mockImplementation(() => answer.promise);
    const shutdown = deferred<void>();
    double.shutdown.mockImplementation(() => shutdown.promise);
    quitFromTray();
    await vi.waitFor(() => expect(host.dialog.showMessageBox).toHaveBeenCalledOnce());
    await pause();
    const shutdownWhileAsking = double.shutdown.mock.calls.length;
    const cancelId = askedOptions()?.cancelId ?? 1;
    answer.resolve({ response: 1 - cancelId, checkboxChecked: false });
    await vi.waitFor(() => expect(double.shutdown).toHaveBeenCalledOnce());
    await pause();
    const quitWhileShuttingDown = host.app.quit.mock.calls.length;
    shutdown.resolve();
    await vi.waitFor(() => expect(host.app.quit).toHaveBeenCalledOnce());
    expect({ shutdownWhileAsking, quitWhileShuttingDown }).toEqual({ shutdownWhileAsking: 0, quitWhileShuttingDown: 0 });
  });

  it('M5 quits without asking, after shutting the connection down, when the server is not running', async () => {
    await start();
    const double = connection();
    double.readStatus.mockResolvedValue({ ok: true, status: STATUS_STOPPED });
    const shutdown = deferred<void>();
    double.shutdown.mockImplementation(() => shutdown.promise);
    quitFromTray();
    await vi.waitFor(() => expect(double.shutdown).toHaveBeenCalledOnce());
    await pause();
    const quitWhileShuttingDown = host.app.quit.mock.calls.length;
    shutdown.resolve();
    await vi.waitFor(() => expect(host.app.quit).toHaveBeenCalledOnce());
    expect({ asked: host.dialog.showMessageBox.mock.calls.length, quitWhileShuttingDown }).toEqual({ asked: 0, quitWhileShuttingDown: 0 });
  });

  it('M5 X close still only hides the window and leaves the connection running', async () => {
    await start();
    const event = { preventDefault: vi.fn() };
    host.window.emit('close', event);
    await pause();
    expect({ prevented: event.preventDefault.mock.calls.length, hidden: host.window.hide.mock.calls.length, shutdown: connection().shutdown.mock.calls.length, quit: host.app.quit.mock.calls.length })
      .toEqual({ prevented: 1, hidden: 1, shutdown: 0, quit: 0 });
  });
});

describe('serverOperations preload bridge', () => {
  const BRIDGE = [
    ['readConnection', 'server-operations:connection', 'none'],
    ['connect', 'server-operations:connect', 'none'],
    ['readStatus', 'server-operations:status', 'none'],
    ['startServer', 'server-operations:start', 'none'],
    ['stopServer', 'server-operations:stop', 'none'],
    ['forceStopServer', 'server-operations:force-stop', 'none'],
    ['readLogs', 'server-operations:logs', 'input'],
    ['readReleaseCandidate', 'server-operations:release-candidate', 'none'],
    ['buildRelease', 'server-operations:release-build', 'input'],
    ['selectRelease', 'server-operations:release-select', 'input'],
  ] as const;

  // preload.cts is CommonJS TypeScript; it is converted here and run with a fake require so the
  // real file decides what reaches the renderer. Any require other than electron fails the test.
  async function loadPreload() {
    const source = readFileSync(new URL('../electron/preload.cts', import.meta.url), 'utf8');
    const { code } = await transformWithOxc(source, 'preload.cts', { lang: 'ts' });
    const exposed = new Map<string, Record<string, (...args: unknown[]) => unknown>>();
    const invoke = vi.fn(async (..._args: unknown[]) => undefined);
    const electron = {
      contextBridge: { exposeInMainWorld: (name: string, api: Record<string, (...args: unknown[]) => unknown>) => { exposed.set(name, api); } },
      ipcRenderer: { invoke },
    };
    const requireModule = (id: string) => {
      if (id === 'electron') return electron;
      throw new Error(`preload must not require ${id}`);
    };
    new Function('require', 'module', 'exports', code)(requireModule, { exports: {} }, {});
    return { exposed, invoke };
  }

  it('P1 exposes serverOperations with exactly the ten actions and keeps the existing bridges as they are', async () => {
    const { exposed } = await loadPreload();
    expect([...exposed.keys()].sort()).toEqual(['serverOperations', 'systemBacklog', 'systemGuide', 'systemRecords']);
    expect(Object.keys(exposed.get('serverOperations') ?? {}).sort()).toEqual(BRIDGE.map(([method]) => method).sort());
    expect(Object.keys(exposed.get('systemRecords') ?? {}).sort()).toEqual(['readCatalog', 'readCheckout', 'readSection']);
    expect(Object.keys(exposed.get('systemGuide') ?? {})).toEqual(['readGuide']);
    expect(Object.keys(exposed.get('systemBacklog') ?? {})).toEqual(['readBacklog']);
  });

  it('P1 maps each action to its channel: input actions pass the renderer value unchecked, the others pass nothing', async () => {
    const { exposed, invoke } = await loadPreload();
    const api = exposed.get('serverOperations') ?? {};
    // An object and a non-object: the preload neither checks nor replaces either one.
    const raws: unknown[] = [{ minutes: '10', contains: 'error', commit: 'NOT-A-COMMIT' }, 'not an object'];
    const seen: unknown[] = [];
    for (const [method] of BRIDGE) {
      for (const raw of raws) {
        invoke.mockClear();
        await api[method]?.(raw, 'extra');
        const call = invoke.mock.calls[0] ?? [];
        const passed = call.length > 1 ? (call[1] === raw ? 'same value' : 'other value') : 'nothing';
        seen.push({ method, calls: invoke.mock.calls.length, channel: call[0], passed, extra: call.length > 2 });
      }
    }
    expect(seen).toEqual(BRIDGE.flatMap(([method, channel, input]) => raws.map(() => ({ method, calls: 1, channel, passed: input === 'input' ? 'same value' : 'nothing', extra: false }))));
  });
});

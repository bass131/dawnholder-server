// @vitest-environment node
// Requirement: backlog-menu-design.md 「Electron 경계」: main.ts builds createBacklogStore with the
// existing fixed repositoryRoot and registers system-backlog:read, which hands a trusted sender the
// store result and gives an untrusted sender, frame, page or destroyed window the existing denied
// result; there is no backlog write channel. preload exposes only systemBacklog.readBacklog, which
// invokes the fixed channel without passing anything from the renderer. Electron and the backlog
// store are test doubles (the records-ipc-preload.test.ts approach); the other stores are real and
// only constructed. node:fs mkdirSync is stubbed so the profile folder is not created.
// Fails until the behaviour step.
import { EventEmitter } from 'node:events';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { transformWithOxc } from 'vite';
import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('node:fs', async original => ({ ...await original<typeof import('node:fs')>(), mkdirSync: vi.fn() }));

const backlog = vi.hoisted(() => {
  // A result no real BACKLOG.md produces, so returning it proves the handler passed the store's own result.
  const storeResult = { ok: true, rows: [], issues: [], uncheckedLinks: [{ line: 7, link: 'store-result-marker' }] };
  return { storeResult, read: vi.fn(), createBacklogStore: vi.fn() };
});
vi.mock('../electron/backlog-store.js', () => ({ createBacklogStore: backlog.createBacklogStore }));
// screen-design.md 「시작과 붙기」 1: main.ts starts the backend connection when the app is ready. This
// double (the shape tests/backend-connection.test.ts fixes) keeps the test from starting wsl.exe.
vi.mock('../electron/backend-connection.js', async () => (await import('./server-operations-fixtures')).backendConnectionModule());

const DENIED = { ok: false, code: 'denied', message: '이 창에는 기록 접근 권한이 없습니다.' };
const MAIN_URL = new URL('../electron/main.ts', import.meta.url);

// Only what electron/main.ts touches at start-up; behaviour of each double is irrelevant here.
function makeHost() {
  const mainFrame = { url: new URL('../dist/index.html', MAIN_URL).href };
  const contents = Object.assign(new EventEmitter(), { mainFrame, setWindowOpenHandler: vi.fn(), setZoomFactor: vi.fn() });
  const window = Object.assign(new EventEmitter(), {
    webContents: contents, removeMenu: vi.fn(), loadFile: vi.fn(async () => undefined), isMinimized: vi.fn(() => false),
    isVisible: vi.fn(() => true), isDestroyed: vi.fn(() => false), restore: vi.fn(), show: vi.fn(), focus: vi.fn(), hide: vi.fn(),
  });
  const tray = Object.assign(new EventEmitter(), { setToolTip: vi.fn(), setContextMenu: vi.fn(), destroy: vi.fn() });
  const app = Object.assign(new EventEmitter(), { whenReady: vi.fn(async () => undefined), setPath: vi.fn(), setName: vi.fn(), exit: vi.fn(), quit: vi.fn() });
  return {
    window, contents, mainFrame, app,
    BrowserWindow: vi.fn(function () { return window; }),
    Tray: vi.fn(function () { return tray; }),
    Menu: { buildFromTemplate: vi.fn((items: unknown) => items) },
    nativeImage: { createFromPath: vi.fn(() => ({ isEmpty: () => false })) },
    session: { defaultSession: { setPermissionRequestHandler: vi.fn(), setPermissionCheckHandler: vi.fn(), on: vi.fn() } },
    protocol: { registerSchemesAsPrivileged: vi.fn(), handle: vi.fn() },
    ipcMain: { handle: vi.fn() },
  };
}

type Handler = (event: unknown, input?: unknown) => Promise<unknown>;
let host: ReturnType<typeof makeHost>;

beforeEach(() => {
  vi.resetModules();
  host = makeHost();
  vi.doMock('electron', () => host);
  backlog.read.mockReset().mockResolvedValue(backlog.storeResult);
  backlog.createBacklogStore.mockReset().mockReturnValue({ read: backlog.read });
});

async function start(): Promise<Map<string, Handler>> {
  await import('../electron/main.js');
  await vi.waitFor(() => expect(host.window.loadFile).toHaveBeenCalledOnce());
  return new Map(host.ipcMain.handle.mock.calls.map(call => [call[0] as string, call[1] as Handler]));
}

function trusted() {
  return { sender: host.contents, senderFrame: host.mainFrame };
}

describe('system-backlog:read in the main process', () => {
  it('builds the backlog store once from the existing fixed repository root', async () => {
    await start();
    const repositoryRoot = fileURLToPath(new URL('../../../', MAIN_URL));
    expect(backlog.createBacklogStore.mock.calls).toEqual([[{ repositoryRoot }]]);
  });

  it('registers system-backlog:read as the only backlog channel, so there is no backlog write channel', async () => {
    const channels = [...(await start()).keys()];
    expect(channels.filter(channel => /backlog/i.test(channel))).toEqual(['system-backlog:read']);
  });

  it('hands a trusted sender the store result and passes nothing from the renderer to the store', async () => {
    const handle = (await start()).get('system-backlog:read');
    const plain = await handle?.(trusted());
    const withInput = await handle?.(trusted(), '../outside/BACKLOG.md');
    expect({ plainIsStoreResult: plain === backlog.storeResult, inputIgnored: withInput === backlog.storeResult }).toEqual({ plainIsStoreResult: true, inputIgnored: true });
    expect(backlog.read.mock.calls).toEqual([[], []]);
  });

  it('gives an untrusted sender, subframe, other page or destroyed window the existing denied result without reading', async () => {
    const handle = (await start()).get('system-backlog:read');
    const untrusted = [
      { name: 'other sender', event: { ...trusted(), sender: {} } },
      { name: 'subframe', event: { ...trusted(), senderFrame: { url: host.mainFrame.url } } },
      { name: 'other page', event: { sender: host.contents, senderFrame: { ...host.mainFrame, url: 'file:///untrusted.html' } } },
    ];
    for (const { name, event } of untrusted) expect(await handle?.(event), name).toEqual(DENIED);
    host.window.isDestroyed.mockReturnValue(true);
    expect(await handle?.(trusted()), 'destroyed window').toEqual(DENIED);
    expect(backlog.read).not.toHaveBeenCalled();
  });
});

describe('systemBacklog preload bridge', () => {
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

  it('exposes systemBacklog with readBacklog only', async () => {
    const { exposed } = await loadPreload();
    expect(Object.keys(exposed.get('systemBacklog') ?? {})).toEqual(['readBacklog']);
  });

  it('invokes the fixed system-backlog:read channel without arguments, whatever the renderer passes', async () => {
    const { exposed, invoke } = await loadPreload();
    const readBacklog = exposed.get('systemBacklog')?.readBacklog;
    await readBacklog?.();
    await readBacklog?.('../outside/BACKLOG.md', 42);
    expect(invoke.mock.calls).toEqual([['system-backlog:read'], ['system-backlog:read']]);
  });
});

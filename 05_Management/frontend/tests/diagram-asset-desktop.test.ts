// @vitest-environment node
// Independent tests for how the desktop main process wires the fixed diagram protocol (R-14, D-11, design-spec 8.3).
// Electron is a test double here: these check registration, handler wiring and navigation decisions, not Chromium.
import { EventEmitter } from 'node:events';
import { existsSync, readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('node:fs', async (original) => ({ ...await original<typeof import('node:fs')>(), mkdirSync: vi.fn() }));
// Record index v2 (index-v2-design.md 「Electron 경계」): the catalog store only reads.
vi.mock('../electron/catalog-store.js', () => ({ createCatalogStore: vi.fn(() => ({ read: vi.fn(async () => ({ ok: true })) })) }));
vi.mock('../electron/system-guide-store.js', () => ({ createSystemGuideStore: vi.fn(() => ({ read: vi.fn(async () => ({ ok: true })) })) }));

const documentUrl = 'dh-diagram://renderer/diagram-renderer.html';
type Frame = { url: string; parent: Frame | null };
type NavigationEvent = { url: string; isMainFrame: boolean; frame: Frame | null | undefined; preventDefault: ReturnType<typeof vi.fn> };

function makeHost() {
  const order: string[] = [];
  let ready = false;
  const mainFrame: Frame = { url: new URL('../dist/index.html', new URL('../electron/main.ts', import.meta.url)).href, parent: null };
  const contents = Object.assign(new EventEmitter(), { mainFrame, setWindowOpenHandler: vi.fn(), setZoomFactor: vi.fn() });
  const window = Object.assign(new EventEmitter(), {
    webContents: contents, removeMenu: vi.fn(), loadFile: vi.fn(async () => undefined), isMinimized: vi.fn(() => false),
    isVisible: vi.fn(() => true), isDestroyed: vi.fn(() => false), restore: vi.fn(), show: vi.fn(), focus: vi.fn(), hide: vi.fn(),
  });
  const defaultSession = Object.assign(new EventEmitter(), { setPermissionRequestHandler: vi.fn(), setPermissionCheckHandler: vi.fn() });
  const protocol = {
    registerSchemesAsPrivileged: vi.fn((_schemes: unknown) => { order.push(ready ? 'register-after-ready' : 'register-before-ready'); }),
    handle: vi.fn((_scheme: string, _handler: unknown) => { order.push('handle'); }),
  };
  const app = Object.assign(new EventEmitter(), {
    whenReady: vi.fn(async () => { ready = true; order.push('ready'); }),
    setPath: vi.fn(), setName: vi.fn(), exit: vi.fn(), quit: vi.fn(),
  });
  const BrowserWindow = vi.fn(function () { order.push('window'); return window; });
  const ipcMain = { handle: vi.fn() };
  return {
    order, mainFrame, contents, window, protocol, app, BrowserWindow, ipcMain,
    session: { defaultSession },
    Tray: vi.fn(function () { return Object.assign(new EventEmitter(), { setToolTip: vi.fn(), setContextMenu: vi.fn(), destroy: vi.fn() }); }),
    Menu: { buildFromTemplate: vi.fn((items: unknown) => items) },
    nativeImage: { createFromPath: vi.fn(() => ({ isEmpty: () => false })) },
  };
}

let host: ReturnType<typeof makeHost>;
beforeEach(() => {
  vi.resetModules();
  host = makeHost();
  vi.doMock('electron', () => host);
});
async function start() {
  await import('../electron/main.js');
  await vi.waitFor(() => expect(host.window.loadFile).toHaveBeenCalledOnce());
}
function navigate(partial: Partial<NavigationEvent>): NavigationEvent {
  const event: NavigationEvent = { url: documentUrl, isMainFrame: false, frame: { url: '', parent: host.mainFrame }, preventDefault: vi.fn(), ...partial };
  host.contents.emit('will-frame-navigate', event);
  return event;
}

describe('diagram scheme registration and handler wiring', () => {
  it('registers only dh-diagram before ready with exactly standard and secure privileges', async () => {
    await start();
    expect(host.protocol.registerSchemesAsPrivileged).toHaveBeenCalledOnce();
    expect(host.protocol.registerSchemesAsPrivileged.mock.calls[0]).toEqual([[{ scheme: 'dh-diagram', privileges: { standard: true, secure: true } }]]);
    expect(host.order.indexOf('register-before-ready')).toBe(0);
    expect(host.order).not.toContain('register-after-ready');
  });

  it('installs one dh-diagram handler after ready and before the window can request a frame', async () => {
    await start();
    expect(host.protocol.handle).toHaveBeenCalledOnce();
    expect(host.protocol.handle.mock.calls[0]?.[0]).toBe('dh-diagram');
    expect(host.order.indexOf('ready')).toBeLessThan(host.order.indexOf('handle'));
    expect(host.order.indexOf('handle')).toBeLessThan(host.order.indexOf('window'));
  });

  it('wires the handler to the fixed asset contract and the module-relative dist directory', async () => {
    await start();
    const handler = host.protocol.handle.mock.calls[0]?.[1] as (request: { url: string; method: string }) => Promise<Response>;
    const post = await handler({ url: documentUrl, method: 'POST' });
    expect(post.status).toBe(405);
    const other = await handler({ url: 'dh-diagram://renderer/index.html', method: 'GET' });
    expect([other.status, other.headers.get('x-diagram-error')]).toEqual([404, 'asset-url-denied']);
    // The real build output may or may not exist when unit tests run; either outcome must be the fixed file or asset-missing.
    const builtDocument = fileURLToPath(new URL('../dist/diagram-renderer.html', import.meta.url));
    const response = await handler({ url: documentUrl, method: 'GET' });
    if (existsSync(builtDocument)) {
      expect(response.status).toBe(200);
      expect(await response.text()).toBe(readFileSync(builtDocument, 'utf8'));
    } else {
      expect([response.status, response.headers.get('x-diagram-error')]).toEqual([404, 'asset-missing']);
    }
  });

  it('cancels every download on the default session', async () => {
    await start();
    const listeners = host.session.defaultSession.listeners('will-download');
    expect(listeners).toHaveLength(1);
    const event = { preventDefault: vi.fn() };
    host.session.defaultSession.emit('will-download', event, { getURL: () => documentUrl }, host.contents);
    expect(event.preventDefault).toHaveBeenCalledOnce();
  });

  it('keeps the main window without Node, subframe Node, webview or relaxed web security', async () => {
    await start();
    const options = (host.BrowserWindow.mock.calls[0] as unknown[] | undefined)?.[0] as import('electron').BrowserWindowConstructorOptions;
    expect(options.webPreferences).toMatchObject({ nodeIntegration: false, nodeIntegrationInSubFrames: false, contextIsolation: true, sandbox: true, webSecurity: true, webviewTag: false });
    expect(options.webPreferences).not.toHaveProperty('allowRunningInsecureContent', true);
  });
});

describe('diagram frame entry and navigation decisions', () => {
  it('allows only the first entry of a fresh direct child frame into the exact renderer document', async () => {
    await start();
    const frame: Frame = { url: 'about:blank', parent: host.mainFrame };
    expect(navigate({ frame }).preventDefault).not.toHaveBeenCalled();
    frame.url = documentUrl;
    expect(navigate({ frame }).preventDefault).toHaveBeenCalledOnce();
    // A frame that somehow reports a blank URL again is still the same entered frame.
    frame.url = '';
    expect(navigate({ frame }).preventDefault).toHaveBeenCalledOnce();
    expect(navigate({ frame: { url: '', parent: host.mainFrame } }).preventDefault).not.toHaveBeenCalled();
  });

  it('prevents any other URL, nested frame, non-blank frame, main frame or frameless entry', async () => {
    await start();
    const nested: Frame = { url: '', parent: { url: documentUrl, parent: host.mainFrame } };
    const cases: Partial<NavigationEvent>[] = [
      { url: `${documentUrl}?x=1` }, { url: `${documentUrl}#x` }, { url: 'dh-diagram://renderer/diagram-renderer.js' },
      { url: 'dh-diagram://renderer:99/diagram-renderer.html' }, { url: 'dh-diagram://user@renderer/diagram-renderer.html' },
      { url: new URL('../dist/diagram-renderer.html', new URL('../electron/main.ts', import.meta.url)).href },
      { url: 'https://example.invalid/' }, { url: 'about:blank' },
      { frame: nested }, { frame: { url: 'file:///C:/other.html', parent: host.mainFrame } },
      { isMainFrame: true, frame: host.mainFrame }, { frame: null }, { frame: undefined },
    ];
    for (const partial of cases) expect(navigate(partial).preventDefault, JSON.stringify({ url: partial.url, isMainFrame: partial.isMainFrame, frame: partial.frame?.url })).toHaveBeenCalledOnce();
  });

  it('prevents main navigation and redirects and denies new windows', async () => {
    await start();
    for (const name of ['will-navigate', 'will-redirect']) {
      const event = { url: documentUrl, preventDefault: vi.fn() };
      host.contents.emit(name, event);
      expect(event.preventDefault, name).toHaveBeenCalledOnce();
    }
    const open = host.contents.setWindowOpenHandler.mock.calls[0]?.[0] as (details: { url: string }) => unknown;
    expect(open({ url: documentUrl })).toEqual({ action: 'deny' });
  });

  it('denies every IPC channel to a diagram subframe even inside the owned window', async () => {
    await start();
    const diagramFrame: Frame = { url: documentUrl, parent: host.mainFrame };
    const handlers = host.ipcMain.handle.mock.calls as unknown as [string, (event: unknown, input?: unknown) => Promise<unknown>][];
    expect(handlers.map(([channel]) => channel).sort()).toEqual(['system-backlog:read', 'system-guide:read', 'system-records:read', 'system-records:read-checkout', 'system-records:read-section']);
    for (const [channel, handle] of handlers) {
      expect(await handle({ sender: host.contents, senderFrame: diagramFrame }, {}), channel).toMatchObject({ ok: false, code: 'denied' });
      // The owned main frame passes the sender check; what each store then returns is tested elsewhere.
      expect(await handle({ sender: host.contents, senderFrame: host.mainFrame }, {}), channel).not.toMatchObject({ code: 'denied' });
    }
  });
});

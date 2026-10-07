// @vitest-environment node
import { EventEmitter } from 'node:events';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { beforeEach, describe, expect, it, vi } from 'vitest';

// Record index v2 (goal 2026-10-06-record-source-unification 「만들 것」 3, index-v2-design.md
// 「Electron 경계」): the catalog store only reads, so the double has no save.
const store = vi.hoisted(() => ({ read: vi.fn(async () => ({ ok: false, code: 'missing' })) }));
vi.mock('../electron/catalog-store.js', () => ({ createCatalogStore: vi.fn(() => store) }));

vi.mock('node:fs', async (original) => ({
  ...await original<typeof import('node:fs')>(),
  mkdirSync: vi.fn(),
}));

function makeElectronHost() {
  const contents = Object.assign(new EventEmitter(), { setWindowOpenHandler: vi.fn(), mainFrame: { url: new URL('../dist/index.html', new URL('../electron/main.ts', import.meta.url)).href } });
  const window = Object.assign(new EventEmitter(), {
    webContents: contents,
    removeMenu: vi.fn(),
    loadFile: vi.fn(async (_path: string) => undefined),
    isMinimized: vi.fn(() => false),
    isVisible: vi.fn(() => true),
    isDestroyed: vi.fn(() => false),
    restore: vi.fn(), show: vi.fn(), focus: vi.fn(), hide: vi.fn(), destroy: vi.fn(),
  });
  const tray = Object.assign(new EventEmitter(), {
    setToolTip: vi.fn(), setContextMenu: vi.fn(), destroy: vi.fn(),
  });
  const closeEvent = { preventDefault: vi.fn() };
  const app = Object.assign(new EventEmitter(), {
    whenReady: vi.fn(async () => undefined),
    setPath: vi.fn(), setName: vi.fn(), exit: vi.fn(), quit: vi.fn(),
  });
  // Simulate Electron's documented quit lifecycle, including the cancellable close event.
  app.quit.mockImplementation(() => {
    app.emit('before-quit');
    window.emit('close', closeEvent);
    if (!closeEvent.preventDefault.mock.calls.length) {
      window.emit('closed');
      app.emit('will-quit');
    }
  });
  const image = { isEmpty: vi.fn(() => false) };
  const session = { defaultSession: {
    setPermissionRequestHandler: vi.fn(), setPermissionCheckHandler: vi.fn(), on: vi.fn(),
  } };
  // R-14 local asset delivery: main now registers the fixed diagram scheme. Its contract is tested in diagram-asset-desktop.test.ts.
  const protocol = { registerSchemesAsPrivileged: vi.fn(), handle: vi.fn() };
  const BrowserWindow = vi.fn(function (_options: import('electron').BrowserWindowConstructorOptions) { return window; });
  const Tray = vi.fn(function () { return tray; });
  const Menu = { buildFromTemplate: vi.fn((items: Array<{ label?: string; type?: string; click?: () => void }>) => items) };
  const nativeImage = { createFromPath: vi.fn(() => image) };
  const ipcMain = { handle: vi.fn() };
  return { app, window, tray, image, closeEvent, BrowserWindow, Tray, Menu, nativeImage, session, ipcMain, protocol };
}

let host: ReturnType<typeof makeElectronHost>;

beforeEach(() => {
  vi.resetModules();
  host = makeElectronHost();
  vi.doMock('electron', () => host);
});

async function start() {
  await import('../electron/main.js');
  await vi.waitFor(() => expect(host.window.loadFile).toHaveBeenCalledOnce());
}

describe('desktop shell authority and lifetime contracts', () => {
  it('uses the agreed 1280 by 720 outer bounds for the next newly created window', async () => {
    await start();
    expect(host.BrowserWindow.mock.calls[0]?.[0]).toMatchObject({ width: 1280, height: 720 });
    expect(host.BrowserWindow.mock.calls[0]?.[0]).not.toHaveProperty('useContentSize', true);
  });

  it('loads the bundled UI with an isolated narrow preload and no Node, webview or permissions', async () => {
    await start();
    const options = host.BrowserWindow.mock.calls[0]?.[0];
    expect(options).toMatchObject({ webPreferences: {
      nodeIntegration: false, contextIsolation: true, sandbox: true, webSecurity: true, webviewTag: false,
    } });
    expect(options?.webPreferences?.preload?.replaceAll('\\', '/')).toMatch(/\/electron\/preload\.cjs$/);
    expect(host.window.loadFile.mock.calls[0]?.[0].replaceAll('\\', '/')).toMatch(/\/frontend\/dist\/index\.html$/);
    const deny = vi.fn();
    const request = host.session.defaultSession.setPermissionRequestHandler.mock.calls[0]?.[0];
    request(undefined, 'notifications', deny);
    expect(deny).toHaveBeenCalledWith(false);
    const check = host.session.defaultSession.setPermissionCheckHandler.mock.calls[0]?.[0];
    expect(check()).toBe(false);
  });

  it('allows only the owned main frame to reach the fixed catalog store', async () => {
    await start();
    // index-v2-design.md 「Electron 경계」: three read channels and the guide channel, no save channel.
    expect(host.ipcMain.handle.mock.calls.map(call => call[0]).sort()).toEqual([
      'system-backlog:read', 'system-guide:read', 'system-records:read', 'system-records:read-checkout', 'system-records:read-section',
    ]);
    const read = host.ipcMain.handle.mock.calls.find(call => call[0] === 'system-records:read')?.[1];
    const event = { sender: host.window.webContents, senderFrame: host.window.webContents.mainFrame };
    await read(event);
    expect(store.read).toHaveBeenCalledOnce();
    for (const other of [{ ...event, sender: {} }, { ...event, senderFrame: { url: event.senderFrame.url } }]) {
      expect(await read(other)).toMatchObject({ ok: false, code: 'denied' });
    }
    const trustedUrl = event.senderFrame.url;
    event.senderFrame.url = 'file:///untrusted.html';
    expect(await read(event)).toMatchObject({ code: 'denied' });
    event.senderFrame.url = trustedUrl;
    host.window.isDestroyed.mockReturnValue(true);
    expect(await read(event)).toMatchObject({ code: 'denied' });
    expect(store.read).toHaveBeenCalledOnce();
  });

  it('builds the store only from the module-relative 05 catalog path, with no backup path', async () => {
    // V3: electron/ and desktop-dist/ are siblings, so this also fixes the built entry's paths.
    // index-v2-design.md 「Electron 경계」 removes the .verification backup path argument.
    await start();
    const { createCatalogStore } = await import('../electron/catalog-store.js');
    expect(vi.mocked(createCatalogStore).mock.calls).toEqual([[
      fileURLToPath(new URL('../../records/catalog.json', import.meta.url)),
    ]]);
  });
  it('rejects navigation, redirects, subframe navigation and new windows', async () => {
    await start();
    for (const name of ['will-navigate', 'will-frame-navigate', 'will-redirect']) {
      const event = { preventDefault: vi.fn() };
      host.window.webContents.emit(name, event, 'https://example.invalid/');
      expect(event.preventDefault).toHaveBeenCalledOnce();
    }
    const handler = host.window.webContents.setWindowOpenHandler.mock.calls[0]?.[0];
    expect(handler({ url: 'https://example.invalid/' })).toEqual({ action: 'deny' });
  });

  it('keeps the same app/window alive on X and restores it through tray actions', async () => {
    await start();
    const event = { preventDefault: vi.fn() };
    host.window.emit('close', event);
    expect(event.preventDefault).toHaveBeenCalledOnce();
    expect(host.window.hide).toHaveBeenCalledOnce();
    expect(host.window.destroy).not.toHaveBeenCalled();
    expect(host.app.quit).not.toHaveBeenCalled();
    expect(host.tray.destroy).not.toHaveBeenCalled();

    host.window.isMinimized.mockReturnValue(true);
    const menu = host.Menu.buildFromTemplate.mock.calls[0]?.[0];
    menu?.find((item) => item.label === '열기')?.click?.();
    expect(host.window.restore).toHaveBeenCalledOnce();
    expect(host.window.show).toHaveBeenCalledOnce();
    expect(host.window.focus).toHaveBeenCalledOnce();
    host.tray.emit('double-click');
    expect(host.window.show).toHaveBeenCalledTimes(2);
    expect(host.BrowserWindow).toHaveBeenCalledOnce();
    expect(host.Tray).toHaveBeenCalledOnce();
  });

  it('lets explicit quit pass the close guard and destroys tray resources once', async () => {
    await start();
    const menu = host.Menu.buildFromTemplate.mock.calls[0]?.[0];
    menu?.find((item) => item.label === '종료')?.click?.();
    expect(host.app.quit).toHaveBeenCalledOnce();
    expect(host.closeEvent.preventDefault).not.toHaveBeenCalled();
    expect(host.window.hide).not.toHaveBeenCalled();
    expect(host.tray.destroy).toHaveBeenCalledOnce();
    host.app.emit('will-quit');
    expect(host.tray.destroy).toHaveBeenCalledOnce();
    host.tray.emit('double-click');
    expect(host.window.show).not.toHaveBeenCalled();
  });

  it('uses a real nonempty PNG tray image and fails startup if the image cannot load', async () => {
    const png = readFileSync(fileURLToPath(new URL('../electron/assets/tray.png', import.meta.url)));
    expect([...png.subarray(0, 8)]).toEqual([137, 80, 78, 71, 13, 10, 26, 10]);
    expect(png.readUInt32BE(16)).toBeGreaterThan(0);
    expect(png.readUInt32BE(20)).toBeGreaterThan(0);
    host.image.isEmpty.mockReturnValue(true);
    await import('../electron/main.js');
    await vi.waitFor(() => expect(host.app.exit).toHaveBeenCalledWith(1));
    expect(host.window.loadFile).not.toHaveBeenCalled();
    expect(host.Tray).not.toHaveBeenCalled();
  });

  it('reports failure rather than showing success when the local bundle cannot load', async () => {
    host.window.loadFile.mockRejectedValue(new Error('test: missing local bundle'));
    await import('../electron/main.js');
    await vi.waitFor(() => expect(host.app.exit).toHaveBeenCalledWith(1));
    expect(host.window.show).not.toHaveBeenCalled();
  });
});

// @vitest-environment node
// Requirement: goal 「만들 것」 2·3 and 완료조건 3 (no write IPC; the app reads registered sources by
// ID only) and index-v2-design.md 「Electron 경계」: the main process registers exactly
// system-records:read, system-records:read-section, system-records:read-checkout and
// system-guide:read, plus system-backlog:read from backlog-menu-design.md 「Electron 경계」; every
// handler checks trustedSender first; read-section hands its raw input to the source-section reader;
// preload exposes only systemRecords.readCatalog·readSection·readCheckout, systemGuide.readGuide and
// systemBacklog.readBacklog (its own tests are in backlog-ipc-preload.test.ts). Electron is a test
// double (the tests/desktop-main.test.ts approach); the stores are the real ones on the real
// repository, read only. node:fs mkdirSync is stubbed so the profile folder is not created.
// Expected checkout values come from the git CLI.
import { spawnSync } from 'node:child_process';
import { EventEmitter } from 'node:events';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { transformWithOxc } from 'vite';
import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('node:fs', async original => ({ ...await original<typeof import('node:fs')>(), mkdirSync: vi.fn() }));

const RECORD_CHANNELS = ['system-backlog:read', 'system-guide:read', 'system-records:read', 'system-records:read-checkout', 'system-records:read-section'];

// Only what electron/main.ts touches at start-up; behaviour of each double is irrelevant here.
function makeHost() {
  const mainFrame = { url: new URL('../dist/index.html', new URL('../electron/main.ts', import.meta.url)).href };
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

type Handler = (event: unknown, input?: unknown) => Promise<Record<string, unknown>>;
let host: ReturnType<typeof makeHost>;

beforeEach(() => {
  vi.resetModules();
  host = makeHost();
  vi.doMock('electron', () => host);
});

async function start(): Promise<Map<string, Handler>> {
  await import('../electron/main.js');
  await vi.waitFor(() => expect(host.window.loadFile).toHaveBeenCalledOnce());
  return new Map(host.ipcMain.handle.mock.calls.map(call => [call[0] as string, call[1] as Handler]));
}

function trusted() {
  return { sender: host.contents, senderFrame: host.mainFrame };
}

describe('records IPC channels', () => {
  it('registers exactly the three read channels, the guide channel and the backlog channel, with no save channel', async () => {
    const handlers = await start();
    expect([...handlers.keys()].sort()).toEqual(RECORD_CHANNELS);
    expect(handlers.has('system-records:save')).toBe(false);
  });

  it('denies every channel to an untrusted sender, frame, page or a destroyed window', async () => {
    const handlers = await start();
    const untrusted = [
      { name: 'other sender', event: { ...trusted(), sender: {} } },
      { name: 'subframe', event: { ...trusted(), senderFrame: { url: host.mainFrame.url } } },
      { name: 'other page', event: { sender: host.contents, senderFrame: { ...host.mainFrame, url: 'file:///untrusted.html' } } },
    ];
    for (const [channel, handle] of handlers) {
      for (const { name, event } of untrusted) {
        expect(await handle(event, 'guide-overview'), `${channel} ${name}`).toMatchObject({ ok: false, code: 'denied' });
      }
    }
    host.window.isDestroyed.mockReturnValue(true);
    for (const [channel, handle] of handlers) {
      expect(await handle(trusted(), 'guide-overview'), `${channel} destroyed window`).toMatchObject({ ok: false, code: 'denied' });
    }
  });

  it('read-section hands non-string and path-like input to the reader, which rejects it as invalid-request', async () => {
    const handle = (await start()).get('system-records:read-section');
    for (const input of [42, null, { sourceId: 'guide-overview' }, ['guide-overview'], 'docs/guide.md', '../outside.md']) {
      const result = await handle?.(trusted(), input);
      expect(result, JSON.stringify(input)).toMatchObject({ ok: false, code: 'invalid-request' });
    }
  });

  it('read-section looks up the real repository index: no-such-source is unknown-source (needs the v2 data conversion of step 3)', async () => {
    const handle = (await start()).get('system-records:read-section');
    expect(await handle?.(trusted(), 'no-such-source')).toMatchObject({ ok: false, code: 'unknown-source' });
  });

  it('read returns the index result without editable text', async () => {
    const handle = (await start()).get('system-records:read');
    const result = await handle?.(trusted()) ?? {};
    const hasEditableText = Object.hasOwn(result, 'text');
    const knownOutcome = result.ok === true || ['missing', 'invalid', 'load'].includes(String(result.code));
    expect({ hasEditableText, knownOutcome }).toEqual({ hasEditableText: false, knownOutcome: true });
  });

  it('read-checkout returns the branch and HEAD of the running checkout as the git CLI reports them', async () => {
    const root = fileURLToPath(new URL('../../../', import.meta.url));
    const git = (args: string[]) => spawnSync('git', args, { cwd: root, encoding: 'utf8' });
    const symbolic = git(['symbolic-ref', '--quiet', '--short', 'HEAD']);
    const expected = {
      ok: true,
      checkout: { state: 'known', branch: symbolic.status === 0 ? symbolic.stdout.trim() : null, head: git(['rev-parse', 'HEAD']).stdout.trim() },
    };
    const handle = (await start()).get('system-records:read-checkout');
    expect(await handle?.(trusted())).toEqual(expected);
  });
});

describe('preload bridge', () => {
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

  it('exposes only systemRecords.readCatalog·readSection·readCheckout, systemGuide.readGuide and systemBacklog', async () => {
    const { exposed } = await loadPreload();
    expect([...exposed.keys()].sort()).toEqual(['systemBacklog', 'systemGuide', 'systemRecords']);
    expect(Object.keys(exposed.get('systemRecords') ?? {}).sort()).toEqual(['readCatalog', 'readCheckout', 'readSection']);
    expect(Object.keys(exposed.get('systemGuide') ?? {})).toEqual(['readGuide']);
  });

  it('maps each bridge call to its fixed channel and passes only the source ID', async () => {
    const { exposed, invoke } = await loadPreload();
    const records = exposed.get('systemRecords');
    const guide = exposed.get('systemGuide');
    await records?.readCatalog?.();
    await records?.readSection?.('guide-overview');
    await records?.readCheckout?.();
    await guide?.readGuide?.();
    expect(invoke.mock.calls).toEqual([
      ['system-records:read'],
      ['system-records:read-section', 'guide-overview'],
      ['system-records:read-checkout'],
      ['system-guide:read'],
    ]);
  });
});

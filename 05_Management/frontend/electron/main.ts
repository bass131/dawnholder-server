import { mkdirSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { app, BrowserWindow, dialog, ipcMain, Menu, nativeImage, protocol, session, Tray, type IpcMainInvokeEvent } from 'electron';
import { createCatalogStore } from './catalog-store.js';
import { createSystemGuideStore } from './system-guide-store.js';
import { createSourceSectionReader, createSourceSectionStore } from './source-section-store.js';
import { sourceSectionFailure } from './source-section-contract.js';
import { createCheckoutStore } from './checkout-store.js';
import { createBacklogStore } from './backlog-store.js';
import { diagramAssets, diagramScheme } from './diagram-asset-contract.js';
import { createDiagramAssetHandler } from './diagram-asset-handler.js';
import { createBackendConnection } from './backend-connection.js';
import { operationFailure, parseCommitInput, parseLogQuery } from './server-operations-contract.js';

// Electron requires this before ready. Standard resolves the renderer's local
// relative assets; secure marks the local document as a trustworthy scheme.
// Leave CSP bypass, fetch/CORS and worker privileges off: only the fixed GET
// mapping is exposed, and the iframe remains opaque with allow-scripts alone.
protocol.registerSchemesAsPrivileged([{ scheme: diagramScheme, privileges: { standard: true, secure: true } }]);

const profilePath = fileURLToPath(new URL('../.verification/desktop-profile', import.meta.url));
mkdirSync(profilePath, { recursive: true });
app.setPath('userData', profilePath);
app.setName('Dawnholder Management');

let mainWindow: BrowserWindow | null = null;
let tray: Tray | null = null;
let isQuitting = false;
const indexPath = fileURLToPath(new URL('../dist/index.html', import.meta.url));
const indexUrl = new URL('../dist/index.html', import.meta.url).href;
const guideStore = createSystemGuideStore(fileURLToPath(new URL('../../records/system-guide.json', import.meta.url)));
const recordsStore = createCatalogStore(fileURLToPath(new URL('../../records/catalog.json', import.meta.url)));
const repositoryRoot = fileURLToPath(new URL('../../../', import.meta.url));
const sectionReader = createSourceSectionReader({
  readCatalog: () => recordsStore.read(),
  store: createSourceSectionStore({ repositoryRoot }),
});
const checkoutStore = createCheckoutStore({ repositoryRoot });
const backlogStore = createBacklogStore({ repositoryRoot });
const backendConnection = createBackendConnection({ repositoryRoot, env: process.env });
function trustedSender(event: IpcMainInvokeEvent): boolean {
  return !!mainWindow && !mainWindow.isDestroyed() && event.sender === mainWindow.webContents && event.senderFrame === mainWindow.webContents.mainFrame && event.senderFrame.url === indexUrl;
}
const denied = { ok: false, code: 'denied', message: '이 창에는 기록 접근 권한이 없습니다.' } as const;
ipcMain.handle('system-records:read', event => trustedSender(event) ? recordsStore.read() : denied);
ipcMain.handle('system-records:read-section', (event, input: unknown) => {
  if (!trustedSender(event)) return sourceSectionFailure('denied');
  return sectionReader.read(input);
});
ipcMain.handle('system-records:read-checkout', async event => {
  if (!trustedSender(event)) return denied;
  return { ok: true, checkout: await checkoutStore.read() };
});
ipcMain.handle('system-guide:read', event => trustedSender(event) ? guideStore.read() : denied);
ipcMain.handle('system-backlog:read', event => trustedSender(event) ? backlogStore.read() : denied);

ipcMain.handle('server-operations:connection', event => {
  if (!trustedSender(event)) return denied;
  return { ok: true, connection: backendConnection.connectionState() };
});
ipcMain.handle('server-operations:connect', async event => {
  if (!trustedSender(event)) return denied;
  return { ok: true, connection: await backendConnection.connect() };
});
for (const [action, call] of [
  ['status', backendConnection.readStatus],
  ['start', backendConnection.startServer],
  ['stop', backendConnection.stopServer],
  ['force-stop', backendConnection.forceStopServer],
] as const) {
  ipcMain.handle(`server-operations:${action}`, event => {
    if (!trustedSender(event)) return denied;
    return call();
  });
}
ipcMain.handle('server-operations:logs', (event, input: unknown) => {
  if (!trustedSender(event)) return denied;
  const query = parseLogQuery(input);
  return query ? backendConnection.readLogs(query) : operationFailure('invalidQuery');
});
ipcMain.handle('server-operations:release-candidate', async event => {
  if (!trustedSender(event)) return denied;
  const checkout = await checkoutStore.read();
  const result = await backendConnection.readReleases();
  if (!result.ok) return result;
  return { ok: true, checkout, currentRelease: result.releases.currentRelease };
});
for (const [action, call] of [
  ['release-build', backendConnection.buildRelease],
  ['release-select', backendConnection.selectRelease],
] as const) {
  ipcMain.handle(`server-operations:${action}`, async (event, input: unknown) => {
    if (!trustedSender(event)) return denied;
    const commit = parseCommitInput(input);
    if (commit === null) return operationFailure('invalidCommit');
    const checkout = await checkoutStore.read();
    if (checkout.state !== 'known' || checkout.head !== commit) return operationFailure('headChanged');
    return call(commit);
  });
}

let quitPending = false;
async function quitFromTray() {
  if (quitPending) return;
  quitPending = true;
  try {
    const result = await backendConnection.readStatus();
    if (result.ok && result.status.server.state !== 'stopped') {
      const options = {
        type: 'question' as const,
        buttons: ['종료', '취소'],
        defaultId: 1,
        cancelId: 1,
        message: '서버가 실행 중입니다. 종료하면 서버도 정상 종료합니다.',
      };
      const answer = mainWindow && !mainWindow.isDestroyed()
        ? await dialog.showMessageBox(mainWindow, options)
        : await dialog.showMessageBox(options);
      if (answer.response === options.cancelId) return;
    }
    // Keep the close guard and tray alive until backend termination is done.
    await backendConnection.shutdown();
    app.quit();
  } catch {
    console.error('[desktop] 관리 백엔드를 종료하지 못했습니다. 다시 종료하세요.');
  } finally {
    quitPending = false;
  }
}

app.whenReady().then(async () => {
  void backendConnection.connect();
  protocol.handle(diagramScheme, createDiagramAssetHandler(new URL('../dist/', import.meta.url)));
  session.defaultSession.setPermissionRequestHandler((_contents, _permission, callback) => callback(false));
  session.defaultSession.setPermissionCheckHandler(() => false);
  session.defaultSession.on('will-download', event => event.preventDefault());

  const window = new BrowserWindow({
    width: 1600,
    height: 900,
    useContentSize: true,
    title: 'Dawnholder Management',
    backgroundColor: '#e9dfc9',
    show: false,
    webPreferences: {
      nodeIntegration: false,
      nodeIntegrationInSubFrames: false,
      contextIsolation: true,
      sandbox: true,
      webSecurity: true,
      webviewTag: false,
      preload: fileURLToPath(new URL('./preload.cjs', import.meta.url)),
    },
  });
  mainWindow = window;
  window.webContents.on('did-finish-load', () => window.webContents.setZoomFactor(1.25));
  const trayImage = nativeImage.createFromPath(fileURLToPath(new URL('../electron/assets/tray.png', import.meta.url)));
  if (trayImage.isEmpty()) {
    throw new Error('Management 트레이 아이콘을 읽지 못했습니다.');
  }
  tray = new Tray(trayImage);
  tray.setToolTip('Dawnholder Management');
  const openWindow = () => {
    if (mainWindow?.isMinimized()) mainWindow.restore();
    mainWindow?.show();
    mainWindow?.focus();
  };
  tray.setContextMenu(Menu.buildFromTemplate([
    { label: '열기', click: openWindow },
    { type: 'separator' },
    { label: '종료', click: () => { void quitFromTray(); } },
  ]));
  tray.on('double-click', openWindow);
  window.removeMenu();
  window.webContents.on('will-navigate', (event) => event.preventDefault());
  const enteredFrames = new WeakSet<object>();
  window.webContents.on('will-frame-navigate', event => {
    // Only the exact renderer document's initial entry is allowed. Subframes never receive IPC authority.
    if (!event.isMainFrame && event.url === diagramAssets.document.url && event.frame && !enteredFrames.has(event.frame) &&
      event.frame.parent === window.webContents.mainFrame && (event.frame.url === '' || event.frame.url === 'about:blank')) {
      enteredFrames.add(event.frame);
      return;
    }
    event.preventDefault();
  });
  window.webContents.on('will-redirect', (event) => event.preventDefault());
  window.webContents.setWindowOpenHandler(() => ({ action: 'deny' }));
  window.once('ready-to-show', () => {
    mainWindow?.show();
    console.info(`[desktop] window ready, visible=${mainWindow?.isVisible()}, electron=${process.versions.electron}`);
  });
  window.on('closed', () => { mainWindow = null; });
  window.on('close', (event) => {
    if (!isQuitting) {
      event.preventDefault();
      window.hide();
      console.info('[desktop] window hidden; tray remains active');
    }
  });

  await window.loadFile(indexPath);
  console.info('[desktop] local Management bundle loaded');
}).catch((error: unknown) => {
  console.error('[desktop] startup failed', error);
  app.exit(1);
});

// The tray path waits for shutdown before app.quit opens the existing close guard.
app.on('before-quit', () => { isQuitting = true; });
app.on('will-quit', () => {
  tray?.destroy();
  tray = null;
  mainWindow = null;
});

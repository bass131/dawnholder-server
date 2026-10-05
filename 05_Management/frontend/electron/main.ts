import { mkdirSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { app, BrowserWindow, ipcMain, Menu, nativeImage, protocol, session, Tray, type IpcMainInvokeEvent } from 'electron';
import { createCatalogStore } from './catalog-store.js';
import { createSystemGuideStore } from './system-guide-store.js';
import { diagramAssets, diagramScheme } from './diagram-asset-contract.js';
import { createDiagramAssetHandler } from './diagram-asset-handler.js';

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
const recordsStore = createCatalogStore(
  fileURLToPath(new URL('../../records/catalog.json', import.meta.url)),
  fileURLToPath(new URL('../../.verification/system-records-last-good.json', import.meta.url)),
);
function trustedSender(event: IpcMainInvokeEvent): boolean {
  return !!mainWindow && !mainWindow.isDestroyed() && event.sender === mainWindow.webContents && event.senderFrame === mainWindow.webContents.mainFrame && event.senderFrame.url === indexUrl;
}
const denied = { ok: false, code: 'denied', message: '이 창에는 기록 접근 권한이 없습니다.' } as const;
ipcMain.handle('system-records:read', event => trustedSender(event) ? recordsStore.read() : denied);
ipcMain.handle('system-records:save', (event, input: unknown) => trustedSender(event) ? recordsStore.save(input) : denied);
ipcMain.handle('system-guide:read', event => trustedSender(event) ? guideStore.read() : denied);

app.whenReady().then(async () => {
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
    { label: '종료', click: () => app.quit() },
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

// 명시 종료는 이 앱만 닫는다. 미연결 WSL/서버에는 종료를 전달하지 않는다.
app.on('before-quit', () => { isQuitting = true; });
app.on('will-quit', () => {
  tray?.destroy();
  tray = null;
  mainWindow = null;
});

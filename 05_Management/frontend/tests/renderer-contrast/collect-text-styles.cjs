// Electron main process started by text-contrast.ts with the repository's electron dev dependency.
// It loads each owned TEMP page in one hidden window and returns, for every visible element with
// its own text inside the scope, the computed values the contrast formula needs. The formula stays
// in text-contrast.ts so the assertion and its arithmetic are read in one place.
'use strict';
const { app, BrowserWindow } = require('electron');
const { readFileSync, writeFileSync } = require('node:fs');

// An uncaught main-process error would open Electron's error dialog, a visible window. Exit with a
// message instead; the launcher reports a missing result as "not run".
process.on('uncaughtException', error => {
  process.stderr.write(`${error?.stack ?? error}\n`);
  app.exit(1);
});

// Runs in the page (serialized with toString), so it must not use anything from this file.
async function collectVisibleText(scopeSelector) {
  await document.fonts.ready;
  const describe = element => element.tagName.toLowerCase() + [...element.classList].map(name => `.${name}`).join('');
  // A failed <link> leaves sheet null; without the CSS every text would be the default black on
  // white and pass, so the caller treats a missing sheet as "not run", never as a measurement.
  const stylesheets = [...document.querySelectorAll('link[rel="stylesheet"]')].map(link => ({ href: link.href, applied: link.sheet !== null }));
  const scope = document.querySelector(scopeSelector);
  const visible = element => element.checkVisibility({ visibilityProperty: true, opacityProperty: true }) && element.getClientRects().length > 0;
  const scopeVisible = scope !== null && visible(scope);
  const rows = [];
  for (const element of scopeVisible ? [scope, ...scope.querySelectorAll('*')] : []) {
    const text = [...element.childNodes]
      .filter(node => node.nodeType === Node.TEXT_NODE)
      .map(node => node.textContent)
      .join('')
      .replace(/\s+/g, ' ')
      .trim();
    if (!text || !visible(element)) continue;
    const style = getComputedStyle(element);
    // Background layers from the element up to <html>; the caller composites them until opaque.
    const layers = [];
    let opacity = 1;
    for (let node = element; node; node = node.parentElement) {
      const nodeStyle = getComputedStyle(node);
      layers.push({ color: nodeStyle.backgroundColor, image: nodeStyle.backgroundImage !== 'none' });
      opacity *= Number(nodeStyle.opacity);
    }
    const path = [];
    for (let node = element; node && node !== scope.parentElement; node = node.parentElement) path.unshift(describe(node));
    rows.push({ selector: describe(element), path: path.join(' > '), text, color: style.color, layers, fontSize: style.fontSize, fontWeight: style.fontWeight, opacity });
  }
  return { stylesheets, fontsStatus: document.fonts.status, scopeVisible, rows };
}

async function measurePages(request) {
  const window = new BrowserWindow({
    show: false,
    focusable: false,
    skipTaskbar: true,
    width: request.window.width,
    height: request.window.height,
    useContentSize: true,
    webPreferences: { contextIsolation: true, nodeIntegration: false, sandbox: true, spellcheck: false, backgroundThrottling: false },
  });
  window.webContents.setWindowOpenHandler(() => ({ action: 'deny' }));
  const scenes = [];
  for (const page of request.pages) {
    await window.loadFile(page.file);
    const collected = await window.webContents.executeJavaScript(`(${collectVisibleText})(${JSON.stringify(request.scopeSelector)})`, false);
    scenes.push({ name: page.name, ...collected });
  }
  // Recorded as evidence that the measurement never showed or focused a window.
  const windowState = { visible: window.isVisible(), focused: window.isFocused() };
  window.destroy();
  return { scenes, windowState };
}

function main() {
  const request = JSON.parse(readFileSync(process.env.RENDERER_CONTRAST_REQUEST ?? '', 'utf8'));
  const writeResult = value => writeFileSync(request.resultFile, `${JSON.stringify(value)}\n`, { flag: 'wx' });
  // The launcher also passes --user-data-dir. Setting the path before 'ready' keeps every profile
  // write in the owned TEMP folder, never in %APPDATA%\Electron, which another app may own.
  app.setPath('userData', request.profileDir);
  // Computed styles need no GPU; a hidden window never paints, and CI runners often have no GPU.
  app.disableHardwareAcceleration();
  app.whenReady()
    .then(() => measurePages(request))
    .then(({ scenes, windowState }) => {
      writeResult({ ok: true, userData: app.getPath('userData'), versions: { electron: process.versions.electron, chrome: process.versions.chrome }, windowState, scenes });
      app.exit(0);
    })
    .catch(error => {
      try { writeResult({ ok: false, error: String(error?.stack ?? error) }); } finally { app.exit(1); }
    });
}

try {
  main();
} catch (error) {
  process.stderr.write(`${error?.stack ?? error}\n`);
  app.exit(1);
}

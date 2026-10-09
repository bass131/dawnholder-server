// Text contrast measured in a real Chromium renderer. The repository's electron dev dependency
// loads pages that this helper writes into an owned TEMP folder; the frontend stylesheets are
// linked by file URL from their own location in src/main.tsx import order, so relative font and
// image URLs resolve as in the app. collect-text-styles.cjs returns computed values and the WCAG 2
// arithmetic below turns them into ratios.
// Ported from the PR3 verifier's measureContrast (.backups/verification/
// 2026-10-06-record-source-unification/pr3-v1/contrast/contrast-observe.cjs). Changes: the
// composite stops at the first opaque layer or the canvas, visibility uses checkVisibility, and a
// color the formula cannot read fails the row instead of passing.
//
// Ownership: measureTextContrast creates the TEMP folder (pages, request, Electron profile, raw
// result), starts one Electron process, and removes the folder on every path. On timeout it ends
// only the process tree rooted at that child.
import { spawn, spawnSync, type ChildProcess } from 'node:child_process';
import { constants, copyFileSync, existsSync, mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { createRequire } from 'node:module';
import { tmpdir } from 'node:os';
import { dirname, isAbsolute, join, resolve } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';

// WCAG 2 SC 1.4.3 minimums. Constants on purpose: never derived from product CSS or code.
const NORMAL_TEXT_MINIMUM = 4.5;
const LARGE_TEXT_MINIMUM = 3;
// Large text is at least 18pt (24px), or 14pt (18.66px) when bold; bold is a computed weight of 700+.
const LARGE_TEXT_PX = 24;
const LARGE_BOLD_TEXT_PX = 18.66;
const BOLD_WEIGHT = 700;

// Wide enough that no narrow-width media rule of the app applies; contrast does not depend on it.
const WINDOW = { width: 1600, height: 900 };
// Paths are joined, not built with new URL(): the jsdom environment replaces the global URL and
// resolves relative URLs against http://localhost.
const COLLECTOR = join(dirname(fileURLToPath(import.meta.url)), 'collect-text-styles.cjs');

// Launch, page and stylesheet failures are not contrast results. They fail with this message so a
// broken harness is never read as a violation or as a pass.
class RendererNotRunError extends Error {
  constructor(reason: string) {
    super(`실행 못 함: ${reason}`);
    this.name = 'RendererNotRunError';
  }
}

export interface ContrastPage {
  name: string;
  // The app's root markup, placed in <div id="root"> as in index.html.
  rootHtml: string;
}

export interface ContrastRow {
  scene: string;
  kind: string | null;
  selector: string;
  path: string;
  text: string;
  // Composited #rrggbb text and background colors; null when the formula could not read a color.
  color: string | null;
  background: string | null;
  ratio: number | null;
  required: number;
  passes: boolean;
  fontSizePx: number;
  fontWeight: number;
  problem: string | null;
  computed: { color: string; layers: Layer[]; opacity: number; reachedCanvas: boolean; backgroundImage: boolean };
}

export interface ContrastScene {
  name: string;
  scopeVisible: boolean;
  rows: ContrastRow[];
}

export interface MeasureOptions {
  // Frontend root whose src/main.tsx names the stylesheets.
  frontendRoot: string;
  pages: ContrastPage[];
  scopeSelector: string;
  timeoutMs: number;
  // Labels each measured row in the result and the raw JSON (for example the text kind).
  classify: (row: { selector: string; text: string }) => string | null;
  // Absolute path that receives a copy of the raw JSON; the original stays in the owned TEMP.
  rawCopyFile: string | undefined;
}

interface Rgba { r: number; g: number; b: number; a: number }
interface Layer { color: string; image: boolean }
interface CollectedRow { selector: string; path: string; text: string; color: string; layers: Layer[]; fontSize: string; fontWeight: string; opacity: number }
interface CollectedScene { name: string; scopeVisible: boolean; fontsStatus: string; stylesheets: { href: string; applied: boolean }[]; rows: CollectedRow[] }
interface Collected { userData: string; versions: { electron: string; chrome: string }; windowState: { visible: boolean; focused: boolean }; scenes: CollectedScene[] }

// Chromium serializes computed sRGB colors as rgb()/rgba(), and as color(srgb …) for colors
// written in that space. Anything else is reported as unreadable, not guessed.
function parseCssColor(value: string): Rgba | null {
  const match = /^(rgba?|color)\((.*)\)$/.exec(value.trim());
  if (!match) return null;
  const parts = (match[2] ?? '').split(/[\s,/]+/).filter(Boolean);
  const space = match[1] === 'color' ? parts.shift() : 'legacy';
  if ((space !== 'legacy' && space !== 'srgb') || parts.length < 3 || parts.length > 4) return null;
  const numbers = parts.map(Number);
  if (!numbers.every(Number.isFinite)) return null;
  const scale = space === 'srgb' ? 255 : 1;
  const [r = 0, g = 0, b = 0, a = 1] = numbers;
  return { r: r * scale, g: g * scale, b: b * scale, a };
}

const over = (top: Rgba, bottom: Rgba): Rgba => ({
  r: top.r * top.a + bottom.r * (1 - top.a),
  g: top.g * top.a + bottom.g * (1 - top.a),
  b: top.b * top.a + bottom.b * (1 - top.a),
  a: 1,
});

// With nothing opaque up to <html>, Chromium paints the light-scheme canvas white.
const CANVAS: Rgba = { r: 255, g: 255, b: 255, a: 1 };

// Stacks the element's and its ancestors' background colors from the first opaque one (or the
// canvas) up to the element, as the renderer paints them. Background images are not composited;
// the row records whether one was in the stack.
function compositeBackground(layers: Layer[]) {
  const painted: Rgba[] = [];
  let backgroundImage = false;
  for (const layer of layers) {
    const color = parseCssColor(layer.color);
    if (!color) return { color: null, problem: `배경색을 해석하지 못함: ${layer.color}`, reachedCanvas: false, backgroundImage };
    backgroundImage ||= layer.image;
    if (color.a === 0) continue;
    painted.push(color);
    if (color.a >= 1) break;
  }
  const opaque = painted.at(-1);
  const reachedCanvas = opaque === undefined || opaque.a < 1;
  let color = reachedCanvas ? CANVAS : painted.pop()!;
  for (const layer of painted.reverse()) color = over(layer, color);
  return { color, problem: null, reachedCanvas, backgroundImage };
}

// WCAG 2 relative luminance. 0.04045 is the sRGB breakpoint; WCAG's older 0.03928 differs only for
// channel values between 10.02 and 10.31 of 255.
function relativeLuminance(color: Rgba): number {
  const linear = (channel: number) => {
    const value = channel / 255;
    return value <= 0.04045 ? value / 12.92 : ((value + 0.055) / 1.055) ** 2.4;
  };
  return 0.2126 * linear(color.r) + 0.7152 * linear(color.g) + 0.0722 * linear(color.b);
}

function contrastRatio(first: Rgba, second: Rgba): number {
  const [lighter, darker] = [relativeLuminance(first), relativeLuminance(second)].sort((a, b) => b - a);
  return (lighter! + 0.05) / (darker! + 0.05);
}

function requiredRatio(fontSizePx: number, fontWeight: number): number {
  const large = fontSizePx >= LARGE_TEXT_PX || (fontSizePx >= LARGE_BOLD_TEXT_PX && fontWeight >= BOLD_WEIGHT);
  return large ? LARGE_TEXT_MINIMUM : NORMAL_TEXT_MINIMUM;
}

const hex = (color: Rgba) => `#${[color.r, color.g, color.b].map(value => Math.round(value).toString(16).padStart(2, '0')).join('')}`;

function contrastRow(scene: string, row: CollectedRow, classify: MeasureOptions['classify']): ContrastRow {
  const fontSizePx = Number.parseFloat(row.fontSize);
  const fontWeight = Number(row.fontWeight);
  const required = requiredRatio(fontSizePx, fontWeight);
  const textColor = parseCssColor(row.color);
  const background = compositeBackground(row.layers);
  const base = {
    scene, kind: classify(row), selector: row.selector, path: row.path, text: row.text, required, fontSizePx, fontWeight,
    computed: { color: row.color, layers: row.layers, opacity: row.opacity, reachedCanvas: background.reachedCanvas, backgroundImage: background.backgroundImage },
  };
  const problem = background.problem ?? (textColor ? null : `글자색을 해석하지 못함: ${row.color}`);
  if (problem || !textColor || !background.color) {
    return { ...base, color: null, background: background.color ? hex(background.color) : null, ratio: null, passes: false, problem };
  }
  // A translucent text color is seen over the background it sits on.
  const shown = over(textColor, background.color);
  const ratio = contrastRatio(shown, background.color);
  return { ...base, color: hex(shown), background: hex(background.color), ratio, passes: ratio >= required, problem: null };
}

// The stylesheet list and order come from src/main.tsx, the app's entry, so they are not restated here.
function entryStylesheets(frontendRoot: string): string[] {
  const entry = join(frontendRoot, 'src', 'main.tsx');
  let source: string;
  try {
    source = readFileSync(entry, 'utf8');
  } catch (error) {
    throw new RendererNotRunError(`앱 진입점을 읽지 못했다 (${entry}: ${String(error)})`);
  }
  const stylesheets = [...source.matchAll(/^import\s+['"](\.{1,2}\/[^'"]+\.css)['"];?\s*$/gm)].map(match => resolve(dirname(entry), match[1] ?? ''));
  if (stylesheets.length === 0) throw new RendererNotRunError(`${entry}에서 CSS import를 찾지 못했다`);
  const missing = stylesheets.filter(path => !existsSync(path));
  if (missing.length > 0) throw new RendererNotRunError(`main.tsx가 import하는 CSS가 없다: ${missing.join(', ')}`);
  return stylesheets;
}

function pageHtml(rootHtml: string, stylesheets: string[], frontendRoot: string): string {
  const links = stylesheets.map(path => `<link rel="stylesheet" href="${pathToFileURL(path).href}">`).join('\n');
  // Under Vite and Vitest, module assets (ThemeImage's PNGs) are /src/... URLs; point them at the same files.
  const markup = rootHtml.replaceAll('src="/src/', `src="${pathToFileURL(join(frontendRoot, 'src')).href}/`);
  return `<!doctype html>\n<html lang="ko">\n<head>\n<meta charset="utf-8">\n${links}\n</head>\n<body>\n<div id="root">${markup}</div>\n</body>\n</html>\n`;
}

function electronExecutable(): string {
  let located: unknown;
  try {
    located = createRequire(import.meta.url)('electron');
  } catch (error) {
    throw new RendererNotRunError(`electron 개발 의존성을 불러오지 못했다 (${String(error)})`);
  }
  if (typeof located !== 'string' || !existsSync(located)) throw new RendererNotRunError(`electron 실행 파일이 없다 (${String(located)})`);
  return located;
}

// Ends only the tree rooted at the child this helper started (Electron's GPU, renderer and utility
// processes are its descendants). Other Electron processes on the machine are never touched.
function stopOwnTree(child: ChildProcess) {
  if (child.pid === undefined || child.exitCode !== null || child.signalCode !== null) return;
  if (process.platform === 'win32') spawnSync('taskkill', ['/PID', String(child.pid), '/T', '/F'], { windowsHide: true });
  else child.kill('SIGKILL');
}

interface ElectronExit { code: number | null; signal: string | null; timedOut: boolean; launchError: string | null; stderr: string; ms: number }

function runElectron(executable: string, requestFile: string, profileDir: string, timeoutMs: number): Promise<ElectronExit> {
  const env: NodeJS.ProcessEnv = { ...process.env, RENDERER_CONTRAST_REQUEST: requestFile };
  // With ELECTRON_RUN_AS_NODE set (some tool shells set it) electron.exe would run as plain Node.
  delete env.ELECTRON_RUN_AS_NODE;
  const started = Date.now();
  const child = spawn(executable, [COLLECTOR, `--user-data-dir=${profileDir}`], { env, stdio: ['ignore', 'ignore', 'pipe'], windowsHide: true });
  let stderr = '';
  child.stderr?.on('data', (chunk: Buffer) => { stderr = (stderr + chunk.toString('utf8')).slice(-4000); });
  return new Promise(settle => {
    let timedOut = false;
    let launchError: string | null = null;
    const finish = (code: number | null, signal: string | null) => {
      clearTimeout(limit);
      clearTimeout(lastResort);
      settle({ code, signal, timedOut, launchError, stderr, ms: Date.now() - started });
    };
    const limit = setTimeout(() => { timedOut = true; stopOwnTree(child); }, timeoutMs);
    // If the stopped child never reports its exit, still settle so the TEMP folder is removed.
    const lastResort = setTimeout(() => finish(null, null), timeoutMs + 15_000);
    child.once('error', error => { launchError = String(error); finish(null, null); });
    child.once('exit', (code, signal) => finish(code, signal));
  });
}

const isRecord = (value: unknown): value is Record<string, unknown> => typeof value === 'object' && value !== null && !Array.isArray(value);
const isString = (value: unknown): value is string => typeof value === 'string';

function readLayer(value: unknown): Layer | null {
  return isRecord(value) && isString(value.color) && typeof value.image === 'boolean' ? { color: value.color, image: value.image } : null;
}

function readRow(value: unknown): CollectedRow | null {
  if (!isRecord(value) || !Array.isArray(value.layers) || typeof value.opacity !== 'number') return null;
  const layers = value.layers.map(readLayer);
  const { selector, path, text, color, fontSize, fontWeight } = value;
  if (!isString(selector) || !isString(path) || !isString(text) || !isString(color) || !isString(fontSize) || !isString(fontWeight)) return null;
  if (layers.some(layer => layer === null)) return null;
  return { selector, path, text, color, layers: layers as Layer[], fontSize, fontWeight, opacity: value.opacity };
}

function readScene(value: unknown): CollectedScene | null {
  if (!isRecord(value) || !isString(value.name) || typeof value.scopeVisible !== 'boolean' || !isString(value.fontsStatus)) return null;
  if (!Array.isArray(value.stylesheets) || !Array.isArray(value.rows)) return null;
  const stylesheets = value.stylesheets.map(item => (isRecord(item) && isString(item.href) && typeof item.applied === 'boolean' ? { href: item.href, applied: item.applied } : null));
  const rows = value.rows.map(readRow);
  if (stylesheets.some(item => item === null) || rows.some(row => row === null)) return null;
  return { name: value.name, scopeVisible: value.scopeVisible, fontsStatus: value.fontsStatus, stylesheets: stylesheets as CollectedScene['stylesheets'], rows: rows as CollectedRow[] };
}

// The collector's result file is outside input to this process; read it from unknown.
function readCollected(resultFile: string): Collected {
  const value: unknown = JSON.parse(readFileSync(resultFile, 'utf8'));
  if (!isRecord(value)) throw new RendererNotRunError('Electron 결과가 객체가 아니다');
  if (value.ok !== true) throw new RendererNotRunError(`Electron이 페이지를 읽지 못했다: ${isString(value.error) ? value.error : JSON.stringify(value)}`);
  const { versions, windowState } = value;
  if (!isString(value.userData) || !isRecord(versions) || !isString(versions.electron) || !isString(versions.chrome) || !Array.isArray(value.scenes)) {
    throw new RendererNotRunError('Electron 결과의 형식이 다르다');
  }
  if (!isRecord(windowState) || typeof windowState.visible !== 'boolean' || typeof windowState.focused !== 'boolean') {
    throw new RendererNotRunError('Electron 결과에 창 상태가 없다');
  }
  const scenes = value.scenes.map(readScene);
  if (scenes.some(scene => scene === null)) throw new RendererNotRunError('Electron 결과의 장면 형식이 다르다');
  return {
    userData: value.userData,
    versions: { electron: versions.electron, chrome: versions.chrome },
    windowState: { visible: windowState.visible, focused: windowState.focused },
    scenes: scenes as CollectedScene[],
  };
}

// The run's other facts (stylesheets, versions, profile, window state, timing, pages) are kept in
// the raw JSON rather than returned; the test asserts on the scenes only.
export async function measureTextContrast(options: MeasureOptions): Promise<ContrastScene[]> {
  if (options.rawCopyFile !== undefined && !isAbsolute(options.rawCopyFile)) throw new RendererNotRunError(`원시 복사 경로는 절대 경로여야 한다: ${options.rawCopyFile}`);
  const stylesheets = entryStylesheets(options.frontendRoot);
  const executable = electronExecutable();
  const owned = mkdtempSync(join(tmpdir(), 'dh-renderer-contrast-'));
  const profileDir = join(owned, 'profile');
  const resultFile = join(owned, 'result.json');
  const requestFile = join(owned, 'request.json');
  const raw: Record<string, unknown> = { startedAt: new Date().toISOString(), frontendRoot: options.frontendRoot, stylesheets, executable, scopeSelector: options.scopeSelector, window: WINDOW };
  try {
    mkdirSync(join(owned, 'pages'));
    const pages = options.pages.map((page, index) => {
      const file = join(owned, 'pages', `${index + 1}-${page.name.replace(/[^a-z0-9-]/gi, '_')}.html`);
      const html = pageHtml(page.rootHtml, stylesheets, options.frontendRoot);
      writeFileSync(file, html, { flag: 'wx' });
      return { name: page.name, file, html };
    });
    raw.pages = pages.map(page => ({ name: page.name, html: page.html }));
    const request = { pages: pages.map(({ name, file }) => ({ name, file })), scopeSelector: options.scopeSelector, resultFile, profileDir, window: WINDOW };
    writeFileSync(requestFile, JSON.stringify(request), { flag: 'wx' });

    const exit = await runElectron(executable, requestFile, profileDir, options.timeoutMs);
    raw.electron = exit;
    if (exit.launchError) throw new RendererNotRunError(`Electron을 띄우지 못했다: ${exit.launchError}`);
    if (exit.timedOut) throw new RendererNotRunError(`Electron이 ${options.timeoutMs}ms 안에 끝나지 않아 자기 프로세스를 끝냈다. stderr: ${exit.stderr}`);
    if (!existsSync(resultFile)) throw new RendererNotRunError(`Electron이 결과 없이 끝났다 (exit ${exit.code}, signal ${exit.signal}). stderr: ${exit.stderr}`);
    const collected = readCollected(resultFile);
    raw.collected = {
      userData: collected.userData,
      versions: collected.versions,
      windowState: collected.windowState,
      scenes: collected.scenes.map(scene => ({ name: scene.name, fontsStatus: scene.fontsStatus, stylesheets: scene.stylesheets })),
    };
    if (resolve(collected.userData).toLowerCase() !== resolve(profileDir).toLowerCase()) {
      throw new RendererNotRunError(`Electron 프로필이 소유 TEMP 밖이다: ${collected.userData}`);
    }
    const unapplied = collected.scenes.flatMap(scene => scene.stylesheets.filter(sheet => !sheet.applied).map(sheet => `${scene.name}: ${sheet.href}`));
    if (unapplied.length > 0) throw new RendererNotRunError(`CSS가 페이지에 붙지 않았다: ${unapplied.join(', ')}`);

    const scenes = collected.scenes.map(scene => ({
      name: scene.name,
      scopeVisible: scene.scopeVisible,
      rows: scene.rows.map(row => contrastRow(scene.name, row, options.classify)),
    }));
    raw.scenes = scenes;
    return scenes;
  } catch (error) {
    raw.notRun = String(error);
    throw error;
  } finally {
    try {
      raw.finishedAt = new Date().toISOString();
      const rawFile = join(owned, 'measurements.json');
      writeFileSync(rawFile, `${JSON.stringify(raw, null, 2)}\n`);
      // COPYFILE_EXCL: an existing evidence file is never overwritten.
      if (options.rawCopyFile !== undefined) copyFileSync(rawFile, options.rawCopyFile, constants.COPYFILE_EXCL);
    } finally {
      // Retries cover Windows releasing the profile's file handles shortly after Electron exits.
      rmSync(owned, { recursive: true, force: true, maxRetries: 10, retryDelay: 200 });
    }
  }
}

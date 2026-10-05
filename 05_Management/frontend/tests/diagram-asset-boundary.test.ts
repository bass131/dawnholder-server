// @vitest-environment node
// Independent boundary tests for the fixed diagram asset protocol (R-14 local asset delivery, design-spec 8.3).
// These exercise the handler at the exact input it receives. Browser-side URL normalization is a separate
// runtime observation and is not inferred from these results.
import { mkdir, mkdtemp, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { pathToFileURL } from 'node:url';
import { afterAll, beforeAll, describe, expect, it } from 'vitest';
import { checkDiagramAssetRequest, diagramAssets, diagramOrigin, diagramScheme } from '../electron/diagram-asset-contract';
import { createDiagramAssetHandler } from '../electron/diagram-asset-handler';

const fixed = [
  { url: 'dh-diagram://renderer/diagram-renderer.html', fileName: 'diagram-renderer.html', mime: /^text\/html\b/ },
  { url: 'dh-diagram://renderer/diagram-renderer.js', fileName: 'diagram-renderer.js', mime: /^text\/javascript\b/ },
  { url: 'dh-diagram://renderer/diagram-renderer.css', fileName: 'diagram-renderer.css', mime: /^text\/css\b/ },
];

// Spellings a browser may or may not normalize before a handler sees them. At the handler they are all received input.
const deniedUrls = [
  'dh-diagram://renderer/diagram-renderer.html?x=1',
  'dh-diagram://renderer/diagram-renderer.html?',
  'dh-diagram://renderer/diagram-renderer.html#top',
  'dh-diagram://renderer/diagram-renderer.html#',
  'dh-diagram://user@renderer/diagram-renderer.html',
  'dh-diagram://user:pass@renderer/diagram-renderer.js',
  'dh-diagram://renderer:99/diagram-renderer.html',
  'dh-diagram://renderer:/diagram-renderer.html',
  'dh-diagram://renderer/./diagram-renderer.html',
  'dh-diagram://renderer/x/../diagram-renderer.html',
  'dh-diagram://renderer/%64iagram-renderer.html',
  'dh-diagram://renderer/diagram-renderer%2Ehtml',
  'dh-diagram://renderer/..%2Fdist%2Findex.html',
  'dh-diagram://renderer/../index.html',
  'dh-diagram://renderer/index.html',
  'dh-diagram://renderer/diagram-build-graph.json',
  'dh-diagram://renderer/assets/index.js',
  'dh-diagram://renderer/',
  'dh-diagram://renderer',
  'dh-diagram://renderer/diagram-renderer.html/',
  'dh-diagram://renderer//diagram-renderer.html',
  'dh-diagram://renderer/DIAGRAM-RENDERER.HTML',
  'DH-DIAGRAM://RENDERER/diagram-renderer.html',
  'dh-diagram://Renderer/diagram-renderer.html',
  'dh-diagram://renderer\\diagram-renderer.html',
  'dh-diagram://renderer/diagram-renderer.html%00',
  ' dh-diagram://renderer/diagram-renderer.html',
  'dh-diagram://renderer/diagram-renderer.html ',
  'dh-diagram://other/diagram-renderer.html',
  'dh-diagram:renderer/diagram-renderer.html',
  'file:///C:/diagram-renderer.html',
  'https://renderer/diagram-renderer.html',
  '',
];

describe('fixed diagram asset contract at the received request', () => {
  it('names one scheme and origin, and maps exactly three GET URLs to fixed files and explicit MIME types', () => {
    expect(diagramScheme).toBe('dh-diagram');
    expect(diagramOrigin).toBe('dh-diagram://renderer');
    expect(Object.values(diagramAssets).map(asset => asset.url).sort()).toEqual(fixed.map(item => item.url).sort());
    for (const item of fixed) {
      const result = checkDiagramAssetRequest(item.url, 'GET');
      expect(result.ok).toBe(true);
      if (!result.ok) continue;
      expect(result.asset.fileName).toBe(item.fileName);
      expect(result.asset.mime).toMatch(item.mime);
      expect(result.asset.mime).toMatch(/charset=utf-8/i);
    }
  });

  it('denies every other spelling with 404, including ones a URL parser would turn into an allowed route', () => {
    for (const url of deniedUrls) expect(checkDiagramAssetRequest(url, 'GET'), url).toEqual({ ok: false, status: 404, code: 'asset-url-denied' });
  });

  it('denies non-GET methods with 405 before considering the URL', () => {
    for (const method of ['POST', 'HEAD', 'PUT', 'DELETE', 'OPTIONS', 'PATCH', 'get', 'Get', '']) {
      for (const url of [fixed[0]!.url, 'dh-diagram://renderer/index.html']) {
        expect(checkDiagramAssetRequest(url, method), `${method} ${url}`).toEqual({ ok: false, status: 405, code: 'method-denied' });
      }
    }
  });
});

describe('fixed diagram asset handler with a real directory', () => {
  let root = '';
  let assetDir = '';
  const bodies: Record<string, string> = {
    'diagram-renderer.html': '<!doctype html><title>fixture-html</title>',
    'diagram-renderer.js': 'globalThis.fixtureScript = 1;',
    'diagram-renderer.css': 'body { color: #123456; }',
  };

  beforeAll(async () => {
    // Test-owned fixture: an asset directory with decoys that must never be reachable, plus a sibling secret.
    root = await mkdtemp(join(tmpdir(), 'dh-diagram-asset-test-'));
    assetDir = join(root, 'dist');
    await mkdir(assetDir);
    for (const [name, body] of Object.entries(bodies)) await writeFile(join(assetDir, name), body, 'utf8');
    await writeFile(join(assetDir, 'index.html'), 'parent-index-must-not-be-served', 'utf8');
    await writeFile(join(assetDir, 'diagram-build-graph.json'), '{"decoy":true}', 'utf8');
    await mkdir(join(assetDir, 'assets'));
    await writeFile(join(assetDir, 'assets', 'index.js'), 'decoy', 'utf8');
    await writeFile(join(root, 'secret.txt'), 'sibling-secret', 'utf8');
  });
  afterAll(async () => { if (root) await rm(root, { recursive: true, force: true }); });

  const handlerFor = (directory: string) => createDiagramAssetHandler(pathToFileURL(directory + '/'));

  it('serves the exact bytes of each fixed file with explicit MIME, nosniff and no-store', async () => {
    const handle = handlerFor(assetDir);
    for (const item of fixed) {
      const response = await handle({ url: item.url, method: 'GET' });
      expect(response.status, item.url).toBe(200);
      expect(response.headers.get('content-type')).toMatch(item.mime);
      expect(response.headers.get('x-content-type-options')).toBe('nosniff');
      expect(response.headers.get('cache-control')).toBe('no-store');
      expect(await response.text()).toBe(bodies[item.fileName]);
    }
  });

  it('never turns a denied request into a filesystem read, even when a file of that name exists', async () => {
    const handle = handlerFor(assetDir);
    const decoys = ['parent-index-must-not-be-served', '{"decoy":true}', 'decoy', 'sibling-secret', ...Object.values(bodies)];
    for (const url of deniedUrls) {
      const response = await handle({ url, method: 'GET' });
      const body = await response.text();
      expect(response.status, url).toBe(404);
      expect(response.headers.get('x-diagram-error')).toBe('asset-url-denied');
      expect(response.headers.get('content-type')).toMatch(/^text\/plain/);
      expect(response.headers.get('x-content-type-options')).toBe('nosniff');
      expect(decoys.includes(body), `${url} leaked ${body}`).toBe(false);
    }
  });

  it('answers non-GET with 405 and Allow: GET without returning the asset body', async () => {
    const handle = handlerFor(assetDir);
    for (const method of ['POST', 'HEAD', 'PUT', 'OPTIONS']) {
      const response = await handle({ url: fixed[1]!.url, method });
      expect(response.status, method).toBe(405);
      expect(response.headers.get('allow')).toBe('GET');
      expect(response.headers.get('x-diagram-error')).toBe('method-denied');
      expect(await response.text()).not.toBe(bodies['diagram-renderer.js']);
    }
  });

  it('reports a missing fixed file as 404 asset-missing and a read failure as 500 asset-read-failed', async () => {
    const broken = join(root, 'broken');
    await mkdir(broken);
    await writeFile(join(broken, 'diagram-renderer.html'), bodies['diagram-renderer.html']!, 'utf8');
    await mkdir(join(broken, 'diagram-renderer.css')); // a directory where a file is expected: a real read error
    const handle = handlerFor(broken);
    const missing = await handle({ url: fixed[1]!.url, method: 'GET' });
    expect(missing.status).toBe(404);
    expect(missing.headers.get('x-diagram-error')).toBe('asset-missing');
    expect(missing.headers.get('x-content-type-options')).toBe('nosniff');
    const unreadable = await handle({ url: fixed[2]!.url, method: 'GET' });
    expect(unreadable.status).toBe(500);
    expect(unreadable.headers.get('x-diagram-error')).toBe('asset-read-failed');
    expect(await unreadable.text()).toBe('asset-read-failed');
    expect((await handle({ url: fixed[0]!.url, method: 'GET' })).status).toBe(200);
  });

  it('reads the file at request time, so a file removed after startup becomes asset-missing rather than a stale success', async () => {
    const later = join(root, 'later');
    await mkdir(later);
    for (const [name, body] of Object.entries(bodies)) await writeFile(join(later, name), body, 'utf8');
    const handle = handlerFor(later);
    expect((await handle({ url: fixed[1]!.url, method: 'GET' })).status).toBe(200);
    await rm(join(later, 'diagram-renderer.js'));
    const response = await handle({ url: fixed[1]!.url, method: 'GET' });
    expect(response.status).toBe(404);
    expect(response.headers.get('x-diagram-error')).toBe('asset-missing');
  });
});

// @vitest-environment node
// Independent test for the approved URL condition (메인 msg_f9952b4413ea, design-spec 8.3): the response file is chosen
// only from the fixed table, and no filesystem path is ever built from the request URL's path, query or encoding.
// File reads are recorded at the fs boundary, so the property holds for every received spelling, not only for decoys.
import { pathToFileURL, fileURLToPath } from 'node:url';
import { join } from 'node:path';
import { tmpdir } from 'node:os';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const reads = vi.hoisted(() => [] as string[]);
vi.mock('node:fs/promises', () => ({
  readFile: vi.fn(async (path: string) => { reads.push(String(path)); return new TextEncoder().encode(`body:${String(path)}`); }),
}));
const { diagramAssets } = await import('../electron/diagram-asset-contract');
const { createDiagramAssetHandler } = await import('../electron/diagram-asset-handler');

const directoryUrl = pathToFileURL(join(tmpdir(), 'dh-fixed-table', 'dist') + '/');
const fixedPaths = Object.values(diagramAssets).map(asset => fileURLToPath(new URL(asset.fileName, directoryUrl)));

// Received spellings, including ones that name other files, traverse, encode, or carry a query/fragment.
const names = ['diagram-renderer.html', 'diagram-renderer.js', 'diagram-renderer.css', 'index.html', 'secret.txt', 'assets/index.js', '..', '../secret.txt', '%2e%2e/secret.txt', '..%2Fsecret.txt', 'diagram-renderer.html%00.txt', 'DIAGRAM-RENDERER.JS', 'C:/Windows/win.ini', '\\\\server\\share\\x'];
const decorations = ['', '?file=../secret.txt', '?', '#x', '/', '/../secret.txt', ';x'];
const hosts = ['renderer', 'RENDERER', 'renderer:1', 'user@renderer', 'other', ''];
const received = [...new Set(hosts.flatMap(host => names.flatMap(name => decorations.map(tail => `dh-diagram://${host}/${name}${tail}`))))];

describe('fixed asset table at the filesystem boundary', () => {
  beforeEach(() => { reads.length = 0; });

  it('reads only the three fixed paths, and only for the three exact URLs with GET', async () => {
    const handle = createDiagramAssetHandler(directoryUrl);
    const exact = new Set<string>(Object.values(diagramAssets).map(asset => asset.url));
    for (const method of ['GET', 'POST', 'HEAD']) {
      for (const url of [...received, ...exact]) {
        const before = reads.length;
        const response = await handle({ url, method });
        const readsHere = reads.slice(before);
        if (method === 'GET' && exact.has(url)) {
          expect(response.status, url).toBe(200);
          expect(readsHere, url).toEqual([fixedPaths[Object.values(diagramAssets).findIndex(asset => asset.url === url)]]);
        } else {
          expect(readsHere, `${method} ${url}`).toEqual([]);
          expect(response.status, `${method} ${url}`).toBe(method === 'GET' ? 404 : 405);
        }
      }
    }
    expect(new Set(reads).size).toBeLessThanOrEqual(3);
    for (const path of reads) expect(fixedPaths).toContain(path);
  });

  it('resolves the fixed file names against the directory URL, which must end with a slash', async () => {
    await createDiagramAssetHandler(directoryUrl)({ url: diagramAssets.script.url, method: 'GET' });
    expect(reads).toEqual([join(tmpdir(), 'dh-fixed-table', 'dist', 'diagram-renderer.js')]);
    reads.length = 0;
    // Without the trailing slash the last segment is replaced: the documented precondition of the handler.
    await createDiagramAssetHandler(new URL(directoryUrl.href.slice(0, -1)))({ url: diagramAssets.script.url, method: 'GET' });
    expect(reads).toEqual([join(tmpdir(), 'dh-fixed-table', 'diagram-renderer.js')]);
  });
});

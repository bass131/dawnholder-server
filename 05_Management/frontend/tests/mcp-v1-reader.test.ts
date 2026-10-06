// @vitest-environment node
// V1: snapshot reader I/O boundary with injected, pausable file operations, the native
// bounded read, error mapping without leaks, cancellation/finally close, and the hash
// shared with the UI store. No sleeps: every interleaving is driven by explicit gates.
import { mkdir, mkdtemp, readFile, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { createCatalogStore } from '../electron/catalog-store';
import { CatalogReadError } from '../mcp/catalog-errors';
import { createCatalogReader, nodeCatalogFileOperations, type CatalogFileOperations, type CatalogSnapshot } from '../mcp/catalog-reader';
import {
  ControlledCatalogFile, FIXTURE_PATH, MAX_CATALOG, abortError, catalogBytes, catalogOfExactSize, decodedTextHash, linkedCatalog,
  makeCatalog, makeRecord, makeSystem, osError, sha256, type FileOp,
} from './mcp-fixtures';

const live = () => new AbortController().signal;
const LEAKS = ['SENTINEL', 'C:\\', 'v1-fixture', 'EACCES', 'ENOENT', 'errno', '{', 'syscall'];

async function readFailure(promise: Promise<CatalogSnapshot>): Promise<CatalogReadError> {
  let caught: unknown;
  let resolved: CatalogSnapshot | undefined;
  try { resolved = await promise; } catch (error) { caught = error; }
  expect(resolved).toBeUndefined();
  expect(caught).toBeInstanceOf(CatalogReadError);
  const error = caught as CatalogReadError;
  for (const leak of LEAKS) expect(error.message.includes(leak), `message leaks ${leak}`).toBe(false);
  expect(error.cause).toBeUndefined();
  return error;
}

function deepFrozen(value: unknown, seen = new Set<unknown>()): boolean {
  if (typeof value !== 'object' || value === null || seen.has(value)) return true;
  seen.add(value);
  return Object.isFrozen(value) && Object.values(value).every(child => deepFrozen(child, seen));
}

let pending: Array<Promise<unknown>> = [];
afterEach(async () => { await Promise.allSettled(pending.splice(0)); });

describe('snapshot reader with injected I/O', () => {
  it('opens the fixed path once and reads open → stat → bounded read → stat → close, returning one deeply frozen snapshot', async () => {
    const catalog = linkedCatalog();
    const bytes = catalogBytes(catalog);
    const file = new ControlledCatalogFile(bytes);
    const snapshot = await file.reader().readSnapshot(live());
    expect(file.log).toEqual(['open', 'stat', 'read', 'stat', 'close']);
    expect(file.openedPaths).toEqual([FIXTURE_PATH]);
    expect(file.readRequests).toEqual([MAX_CATALOG + 1]);
    expect(file.openHandles.size).toBe(0);
    expect(snapshot.metadata).toEqual({ hash: decodedTextHash(bytes), revision: catalog.revision, asOf: catalog.asOf, sourceCommit: catalog.sourceCommit });
    expect(snapshot.metadata.hash).toBe(sha256(bytes));
    expect(snapshot.catalog).toEqual(catalog);
    expect(deepFrozen(snapshot)).toBe(true);
    const systems = snapshot.catalog.systems as unknown as Array<Record<string, unknown>>;
    expect(() => { systems.push({}); }).toThrow(TypeError);
    expect(() => { (systems[0] as Record<string, unknown>).title = 'mutated'; }).toThrow(TypeError);
    expect(() => { (snapshot.metadata as unknown as Record<string, unknown>).hash = 'mutated'; }).toThrow(TypeError);
    // Later file changes never reach an already returned snapshot.
    file.replace(catalogBytes(makeCatalog({ revision: 'changed' })));
    expect(snapshot.metadata.revision).toBe(catalog.revision);
  });

  it('releases the handle before producing any parse/validation outcome', async () => {
    for (const bytes of [catalogBytes(linkedCatalog()), new TextEncoder().encode('{ not json SENTINEL_RAW_JSON')]) {
      const file = new ControlledCatalogFile(bytes);
      const close = file.pause('close');
      let settled = false;
      const read = file.reader().readSnapshot(live()).then(() => { settled = true; }, () => { settled = true; });
      pending.push(read);
      await close.reached;
      await new Promise<void>(done => setImmediate(done));
      expect(settled).toBe(false);
      close.release();
      await read;
      expect(settled).toBe(true);
      expect(file.openHandles.size).toBe(0);
    }
  });

  describe('change detection between the two stats', () => {
    const cases: Array<[string, (file: ControlledCatalogFile) => void]> = [
      ['size and mtime change while read is pending', file => file.replace(catalogBytes(makeCatalog({ revision: 'r2-longer-revision' })))],
      ['same-size rewrite with mtime change', file => file.replace(catalogBytes(makeCatalog({ revision: 'fixture-r2' })))],
      ['growth without timestamp change', file => file.replace(new Uint8Array([...file.bytes, 0x20, 0x20]), false)],
      ['shrink without timestamp change', file => file.replace(file.bytes.slice(0, file.bytes.byteLength - 1), false)],
      ['atomic replacement (inode/file id change)', file => { file.ino += 1; }],
      ['ctime only change', file => { file.ctimeMs += 5; }],
      ['volume change', file => { file.dev += 1; }],
    ];
    for (const [label, mutate] of cases) {
      it(`${label} → CATALOG_CHANGED_DURING_READ, no partial data, handle closed`, async () => {
        const file = new ControlledCatalogFile(catalogBytes(makeCatalog()));
        const read = file.pause('read');
        const result = file.reader().readSnapshot(live());
        pending.push(result.catch(() => undefined));
        await read.reached;
        mutate(file);
        read.release();
        expect((await readFailure(result)).code).toBe('CATALOG_CHANGED_DURING_READ');
        expect(file.opened).toBe(1);
        expect(file.closed).toBe(1);
      });
    }

    it('copies the first stat even if the platform reuses one mutable stat object', async () => {
      const file = new ControlledCatalogFile(catalogBytes(makeCatalog()));
      file.statMode = 'shared';
      const read = file.pause('read');
      const result = file.reader().readSnapshot(live());
      pending.push(result.catch(() => undefined));
      await read.reached;
      file.replace(catalogBytes(makeCatalog({ revision: 'r2-longer-revision' })));
      read.release();
      expect((await readFailure(result)).code).toBe('CATALOG_CHANGED_DURING_READ');
    });

    it('detects bytes longer than the stable stat size (read returns more than stat reported)', async () => {
      const bytes = catalogBytes(makeCatalog());
      const longer = new Uint8Array([...bytes, 0x20]);
      let opened = 0;
      let closed = 0;
      const operations: CatalogFileOperations<object> = {
        open: async () => { opened += 1; return {}; },
        stat: async () => ({ size: bytes.byteLength, mtimeMs: 1, ctimeMs: 1, dev: 1, ino: 1 }),
        read: async (_handle, maxBytes) => longer.slice(0, maxBytes),
        close: async () => { closed += 1; },
      };
      const error = await readFailure(createCatalogReader({ catalogPath: FIXTURE_PATH, fileOperations: operations }).readSnapshot(live()));
      expect(error.code).toBe('CATALOG_CHANGED_DURING_READ');
      expect([opened, closed]).toEqual([1, 1]);
    });
  });

  describe('2 MiB source limit', () => {
    it('accepts exactly 2,097,152 bytes and rejects 2,097,153 by stat without reading', async () => {
      const exact = new ControlledCatalogFile(catalogOfExactSize(MAX_CATALOG));
      expect((await exact.reader().readSnapshot(live())).catalog.schemaVersion).toBe(1);
      const over = new ControlledCatalogFile(catalogOfExactSize(MAX_CATALOG + 1));
      expect((await readFailure(over.reader().readSnapshot(live()))).code).toBe('CATALOG_TOO_LARGE');
      expect(over.log).toEqual(['open', 'stat', 'close']);
    });

    it('a file growing past the limit during the bounded read fails without partial data', async () => {
      const file = new ControlledCatalogFile(catalogBytes(makeCatalog()));
      const read = file.pause('read');
      const result = file.reader().readSnapshot(live());
      pending.push(result.catch(() => undefined));
      await read.reached;
      file.replace(new Uint8Array(3 * MAX_CATALOG).fill(0x20));
      read.release();
      const error = await readFailure(result);
      // Either explicit code is acceptable: it is larger than the limit and it changed.
      expect(['CATALOG_TOO_LARGE', 'CATALOG_CHANGED_DURING_READ']).toContain(error.code);
      expect(file.readRequests).toEqual([MAX_CATALOG + 1]);
      expect(file.openHandles.size).toBe(0);
      console.info(`[V1-MEASURE] growth-past-limit code=${error.code}`);
    });
  });

  describe('error mapping without leaking paths, OS text or raw JSON', () => {
    const ioCases: Array<[string, FileOp, unknown, string]> = [
      ['open ENOENT', 'open', osError('ENOENT'), 'CATALOG_MISSING'],
      ['open EACCES', 'open', osError('EACCES'), 'CATALOG_UNREADABLE'],
      ['open EPERM (rename window)', 'open', osError('EPERM'), 'CATALOG_UNREADABLE'],
      ['open EBUSY', 'open', osError('EBUSY'), 'CATALOG_UNREADABLE'],
      ['open plain Error', 'open', new Error('SENTINEL_PLAIN C:\\v1-fixture'), 'CATALOG_UNREADABLE'],
      ['open non-Error throw', 'open', 'SENTINEL_STRING_THROW', 'CATALOG_UNREADABLE'],
      ['stat ENOENT after open', 'stat', osError('ENOENT'), 'CATALOG_UNREADABLE'],
      ['read EIO', 'read', osError('EIO'), 'CATALOG_UNREADABLE'],
      ['close EBADF', 'close', osError('EBADF'), 'CATALOG_UNREADABLE'],
    ];
    for (const [label, op, failure, code] of ioCases) {
      it(`${label} → ${code}`, async () => {
        const file = new ControlledCatalogFile(catalogBytes(makeCatalog()));
        file.failNext(op, failure);
        const error = await readFailure(file.reader().readSnapshot(live()));
        expect(error.code).toBe(code);
        expect(error.details).toBeUndefined();
        if (op !== 'close') expect(file.openHandles.size).toBe(0);
        // close is attempted exactly once whenever open succeeded (a failing close is still attempted).
        expect(file.log.filter(step => step === 'close')).toHaveLength(op === 'open' ? 0 : 1);
      });
    }

    const contentCases: Array<[string, Uint8Array, string]> = [
      ['invalid JSON with raw sentinel', new TextEncoder().encode('{"revision": "SENTINEL_RAW_JSON", '), 'CATALOG_INVALID'],
      ['empty file', new Uint8Array(), 'CATALOG_INVALID'],
      ['UTF-8 BOM before JSON', new Uint8Array([0xef, 0xbb, 0xbf, ...catalogBytes(makeCatalog())]), 'CATALOG_INVALID'],
      ['schemaVersion 2', catalogBytes({ ...makeCatalog(), schemaVersion: 2 }), 'CATALOG_INVALID'],
      ['missing systems array', catalogBytes({ ...makeCatalog(), systems: undefined }), 'CATALOG_INVALID'],
      ['invalid record type', catalogBytes(makeCatalog({ records: [{ ...makeRecord('r'), type: 'SENTINEL_TYPE' as '변경' }] })), 'CATALOG_INVALID'],
      ['non-string list member', catalogBytes(makeCatalog({ systems: [{ ...makeSystem('s'), behavior: [1 as unknown as string] }] })), 'CATALOG_INVALID'],
      ['duplicate system id', catalogBytes(makeCatalog({ systems: [makeSystem('SENTINEL_DUP'), makeSystem('SENTINEL_DUP')] })), 'CATALOG_INVALID'],
      ['blank id', catalogBytes(makeCatalog({ records: [makeRecord('   ')] })), 'CATALOG_INVALID'],
      ['top-level array', catalogBytes([makeCatalog()]), 'CATALOG_INVALID'],
    ];
    for (const [label, bytes, code] of contentCases) {
      it(`${label} → ${code}`, async () => {
        const file = new ControlledCatalogFile(bytes);
        const error = await readFailure(file.reader().readSnapshot(live()));
        expect(error.code).toBe(code);
        expect(error.details).toBeUndefined();
        expect(file.openHandles.size).toBe(0);
      });
    }

    it('broken references report the total and at most five examples of at most 128 units', async () => {
      const longMissing = `missing-${'😀'.repeat(100)}`;
      const catalog = makeCatalog({
        systems: [makeSystem('s1', { sourceIds: ['no-src-1'], relatedSystemIds: ['no-sys'], recordIds: ['no-rec'] })],
        records: [makeRecord('r1', { systemIds: ['s1', longMissing], sourceIds: ['no-src-2', 'no-src-3', 'no-src-4'] })],
      });
      const error = await readFailure(new ControlledCatalogFile(catalogBytes(catalog)).reader().readSnapshot(live()));
      expect(error.code).toBe('CATALOG_REFERENCE_BROKEN');
      expect(error.details?.count).toBe(7);
      const examples = error.details?.examples as string[];
      expect(examples).toHaveLength(5);
      for (const example of examples) {
        expect(example.length).toBeLessThanOrEqual(128);
        expect(/[\uD800-\uDBFF]$/.test(example)).toBe(false);
      }
    });
  });

  describe('cancellation at every I/O boundary', () => {
    it('an already aborted signal never opens the file', async () => {
      const file = new ControlledCatalogFile(catalogBytes(makeCatalog()));
      const controller = new AbortController();
      controller.abort();
      expect((await readFailure(file.reader().readSnapshot(controller.signal))).code).toBe('REQUEST_CANCELLED');
      expect(file.opened).toBe(0);
    });

    const steps: Array<[FileOp, number]> = [['open', 0], ['stat', 0], ['read', 0], ['stat', 1], ['close', 0]];
    for (const [op, skip] of steps) {
      for (const abortAware of [false, true]) {
        it(`abort while ${op}${skip ? ' (second)' : ''} is pending (${abortAware ? 'I/O observes signal' : 'I/O ignores signal'}) → REQUEST_CANCELLED, no data, handle closed`, async () => {
          const file = new ControlledCatalogFile(catalogBytes(linkedCatalog()));
          for (let index = 0; index < skip; index += 1) file.pause(op).release();
          const gate = file.pause(op, { abortAware: abortAware && op !== 'close' });
          const controller = new AbortController();
          const result = file.reader().readSnapshot(controller.signal);
          pending.push(result.catch(() => undefined));
          await gate.reached;
          controller.abort();
          gate.release();
          expect((await readFailure(result)).code).toBe('REQUEST_CANCELLED');
          expect(file.openHandles.size).toBe(0);
          expect(file.closed).toBe(file.opened);
        });
      }
    }

    it('cancellation that races a real I/O failure still reports cancellation and closes the handle', async () => {
      const file = new ControlledCatalogFile(catalogBytes(makeCatalog()));
      const gate = file.pause('read');
      file.failNext('read', abortError());
      const controller = new AbortController();
      const result = file.reader().readSnapshot(controller.signal);
      await gate.reached;
      controller.abort();
      gate.release();
      expect((await readFailure(result)).code).toBe('REQUEST_CANCELLED');
      expect(file.openHandles.size).toBe(0);
    });
  });

  it('re-reads on every request: no cache, no fallback to an earlier good snapshot', async () => {
    const file = new ControlledCatalogFile(catalogBytes(makeCatalog({ revision: 'first' })));
    const reader = file.reader();
    const first = await reader.readSnapshot(live());
    file.replace(new TextEncoder().encode('{ broken'));
    expect((await readFailure(reader.readSnapshot(live()))).code).toBe('CATALOG_INVALID');
    file.replace(catalogBytes(makeCatalog({ revision: 'third' })));
    const third = await reader.readSnapshot(live());
    expect([first.metadata.revision, third.metadata.revision]).toEqual(['first', 'third']);
    expect(third.metadata.hash).not.toBe(first.metadata.hash);
    expect(file.opened).toBe(3);
    expect(file.closed).toBe(3);
  });
});

describe('native file operations', () => {
  // A FileHandle stand-in that never reaches EOF, as a file growing without bound would.
  function endlessHandle(chunk = 65_536, onRead?: (calls: number) => void) {
    let calls = 0;
    let delivered = 0;
    let overrun = false;
    const handle = {
      async read(buffer: Buffer, offset: number, length: number) {
        calls += 1;
        onRead?.(calls);
        if (offset + length > buffer.byteLength) overrun = true;
        const bytesRead = Math.min(length, chunk);
        buffer.fill(0x20, offset, offset + bytesRead);
        delivered += bytesRead;
        return { bytesRead, buffer };
      },
    };
    return { handle, stats: () => ({ calls, delivered, overrun }) };
  }
  const nativeRead = nodeCatalogFileOperations.read as unknown as (handle: unknown, maxBytes: number, signal: AbortSignal) => Promise<Uint8Array>;

  it('stops at maxBytes on an endlessly growing handle', async () => {
    const endless = endlessHandle();
    const bytes = await nativeRead(endless.handle, MAX_CATALOG + 1, live());
    expect(bytes.byteLength).toBe(MAX_CATALOG + 1);
    expect(endless.stats()).toEqual({ calls: Math.ceil((MAX_CATALOG + 1) / 65_536), delivered: MAX_CATALOG + 1, overrun: false });
  });

  it('observes abort between chunks', async () => {
    const controller = new AbortController();
    const endless = endlessHandle(1024, calls => { if (calls === 2) controller.abort(); });
    const error = await nativeRead(endless.handle, MAX_CATALOG + 1, controller.signal).then(() => undefined, (caught: unknown) => caught);
    expect(error).toBeInstanceOf(CatalogReadError);
    expect((error as CatalogReadError).code).toBe('REQUEST_CANCELLED');
    expect(endless.stats().calls).toBe(2);
  });

  describe('real temporary files', () => {
    let root: string;
    beforeEach(async () => { root = await mkdtemp(join(tmpdir(), 'dawnholder-v1-reader-')); });
    afterEach(async () => {
      expect(resolve(root).startsWith(join(resolve(tmpdir()), 'dawnholder-v1-reader-'))).toBe(true);
      await rm(root, { recursive: true, force: true });
    });
    const nativeReader = (path: string) => createCatalogReader({ catalogPath: path, fileOperations: nodeCatalogFileOperations });

    it('missing file → MISSING, directory → UNREADABLE, oversize → TOO_LARGE, exact limit → ok', async () => {
      expect((await readFailure(nativeReader(join(root, 'absent.json')).readSnapshot(live()))).code).toBe('CATALOG_MISSING');
      await mkdir(join(root, 'dir.json'));
      expect((await readFailure(nativeReader(join(root, 'dir.json')).readSnapshot(live()))).code).toBe('CATALOG_UNREADABLE');
      await writeFile(join(root, 'over.json'), catalogOfExactSize(MAX_CATALOG + 1));
      expect((await readFailure(nativeReader(join(root, 'over.json')).readSnapshot(live()))).code).toBe('CATALOG_TOO_LARGE');
      await writeFile(join(root, 'huge.json'), new Uint8Array(3 * MAX_CATALOG).fill(0x20));
      expect((await readFailure(nativeReader(join(root, 'huge.json')).readSnapshot(live()))).code).toBe('CATALOG_TOO_LARGE');
      await writeFile(join(root, 'exact.json'), catalogOfExactSize(MAX_CATALOG));
      expect((await nativeReader(join(root, 'exact.json')).readSnapshot(live())).catalog.schemaVersion).toBe(1);
    });

    it('MCP snapshot hash equals the UI store version on the same bytes, including invalid UTF-8', async () => {
      const valid = catalogBytes(linkedCatalog());
      const text = new TextDecoder().decode(catalogBytes(makeCatalog({ scopeNote: 'INVALID_UTF8_MARK' })));
      const [head, tail] = text.split('INVALID_UTF8_MARK') as [string, string];
      const invalid = new Uint8Array([...new TextEncoder().encode(head), 0xff, 0xfe, 0xc3, ...new TextEncoder().encode(tail)]);
      for (const [name, bytes] of [['valid.json', valid], ['invalid-utf8.json', invalid]] as const) {
        const path = join(root, name);
        await writeFile(path, bytes);
        const snapshot = await nativeReader(path).readSnapshot(live());
        // Record index v2 (index-v2-design.md 「Electron 경계」): the store takes only the catalog path.
        const ui = await createCatalogStore(path).read();
        expect(ui.ok).toBe(true);
        expect(snapshot.metadata.hash).toBe(ui.ok ? ui.version : '');
        expect(snapshot.metadata.hash).toBe(decodedTextHash(bytes));
        if (name === 'valid.json') expect(snapshot.metadata.hash).toBe(sha256(bytes));
        else expect(snapshot.metadata.hash).not.toBe(sha256(bytes));
        expect(Buffer.from(await readFile(path)).equals(Buffer.from(bytes))).toBe(true);
      }
    });
  });

  it('canonical catalog: read-only hash parity with the UI store and unchanged bytes', async () => {
    const canonical = fileURLToPath(new URL('../../records/catalog.json', import.meta.url));
    const before = await readFile(canonical);
    const snapshot = await createCatalogReader({ catalogPath: canonical, fileOperations: nodeCatalogFileOperations }).readSnapshot(live());
    // Record index v2 (index-v2-design.md 「Electron 경계」): the store takes only the catalog path.
    const ui = await createCatalogStore(canonical).read();
    expect(ui.ok && ui.version).toBe(snapshot.metadata.hash);
    expect(snapshot.metadata.hash).toBe(sha256(before));
    const parsed = JSON.parse(before.toString('utf8')) as { revision: string; asOf: string; sourceCommit: string };
    expect(snapshot.metadata).toMatchObject({ revision: parsed.revision, asOf: parsed.asOf, sourceCommit: parsed.sourceCommit });
    expect((await readFile(canonical)).equals(before)).toBe(true);
    console.info(`[V1-MEASURE] canonical bytes=${before.byteLength} hash=${snapshot.metadata.hash} revision=${snapshot.metadata.revision}`);
  });
});

import { open, type FileHandle } from 'node:fs/promises';
import { MAX_CATALOG_BYTES, catalogReferenceErrors, readCatalog, type RecordCatalog } from '../electron/catalog-contract.js';
import { catalogHash } from '../electron/catalog-hash.js';
import { previewText } from '../electron/catalog-query.js';
import { CatalogReadError, checkCancelled } from './catalog-errors.js';
import { catalogDetails } from './catalog-dto.js';

export interface SnapshotMetadata {
  readonly hash: string;
  readonly revision: string;
  readonly asOf: string;
  readonly sourceCommit: string;
}

export interface CatalogSnapshot {
  readonly metadata: SnapshotMetadata;
  readonly catalog: RecordCatalog;
}

export interface CatalogFileStat {
  readonly size: number;
  readonly mtimeMs: number;
  readonly ctimeMs: number;
  readonly dev: number | bigint;
  readonly ino: number | bigint;
}

// Tests can suspend any individual operation; production has no selector or delay hook.
export interface CatalogFileOperations<Handle> {
  open(path: string, signal: AbortSignal): Promise<Handle>;
  stat(handle: Handle, signal: AbortSignal): Promise<CatalogFileStat>;
  read(handle: Handle, maxBytes: number, signal: AbortSignal): Promise<Uint8Array>;
  close(handle: Handle): Promise<void>;
}

export const nodeCatalogFileOperations: CatalogFileOperations<FileHandle> = {
  async open(path, signal) { checkCancelled(signal); return open(path, 'r'); },
  async stat(handle, signal) { checkCancelled(signal); return handle.stat(); },
  async read(handle, maxBytes, signal) {
    const bytes = Buffer.alloc(maxBytes);
    let length = 0;
    while (length < maxBytes) {
      checkCancelled(signal);
      const result = await handle.read(bytes, length, maxBytes - length, length);
      checkCancelled(signal);
      if (result.bytesRead === 0) break;
      length += result.bytesRead;
    }
    return bytes.subarray(0, length);
  },
  async close(handle) { await handle.close(); },
};

function freezeTree<T>(value: T): T {
  if (typeof value === 'object' && value !== null) {
    for (const child of Object.values(value)) freezeTree(child);
    Object.freeze(value);
  }
  return value;
}

function unchanged(a: CatalogFileStat, b: CatalogFileStat): boolean {
  return a.size === b.size && a.mtimeMs === b.mtimeMs && a.ctimeMs === b.ctimeMs && a.dev === b.dev && a.ino === b.ino;
}

function hasCode(error: unknown, code: string): boolean {
  return typeof error === 'object' && error !== null && 'code' in error && error.code === code;
}

export function createCatalogReader<Handle>(options: { catalogPath: string; fileOperations: CatalogFileOperations<Handle> }) {
  const { catalogPath, fileOperations } = options;
  async function readSnapshot(signal: AbortSignal): Promise<CatalogSnapshot> {
    let opened = false;
    try {
      checkCancelled(signal);
      // Keep the handle's lifetime wholly inside I/O, before parsing/query/serialization.
      const handle = await fileOperations.open(catalogPath, signal);
      opened = true;
      let bytes: Uint8Array;
      try {
        checkCancelled(signal);
        const before = { ...await fileOperations.stat(handle, signal) };
        checkCancelled(signal);
        if (before.size > MAX_CATALOG_BYTES) throw new CatalogReadError('CATALOG_TOO_LARGE');
        bytes = await fileOperations.read(handle, MAX_CATALOG_BYTES + 1, signal);
        checkCancelled(signal);
        if (bytes.byteLength > MAX_CATALOG_BYTES) throw new CatalogReadError('CATALOG_TOO_LARGE');
        const after = await fileOperations.stat(handle, signal);
        checkCancelled(signal);
        if (after.size > MAX_CATALOG_BYTES) throw new CatalogReadError('CATALOG_TOO_LARGE');
        if (!unchanged(before, after) || bytes.byteLength !== before.size) throw new CatalogReadError('CATALOG_CHANGED_DURING_READ');
      } finally {
        await fileOperations.close(handle);
      }
      checkCancelled(signal);
      const text = Buffer.from(bytes.buffer, bytes.byteOffset, bytes.byteLength).toString('utf8');
      let parsed: unknown;
      try { parsed = JSON.parse(text); } catch { throw new CatalogReadError('CATALOG_INVALID'); }
      const catalog = readCatalog(parsed);
      if (!catalog) throw new CatalogReadError('CATALOG_INVALID');
      const references = catalogReferenceErrors(catalog);
      if (references.length) throw new CatalogReadError('CATALOG_REFERENCE_BROKEN', { count: references.length, examples: references.slice(0, 5).map(item => previewText(item, 128)) });
      checkCancelled(signal);
      return freezeTree({ metadata: { hash: catalogHash(text), revision: catalog.revision, asOf: catalog.asOf, sourceCommit: catalog.sourceCommit }, catalog: catalogDetails(catalog) });
    } catch (error) {
      checkCancelled(signal);
      if (error instanceof CatalogReadError) throw error;
      throw new CatalogReadError(!opened && hasCode(error, 'ENOENT') ? 'CATALOG_MISSING' : 'CATALOG_UNREADABLE');
    }
  }
  return { readSnapshot };
}

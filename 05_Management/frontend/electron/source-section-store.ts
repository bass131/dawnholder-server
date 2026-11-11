import { realpath as realpathCallback } from 'node:fs';
import type { Stats } from 'node:fs';
import { lstat, open } from 'node:fs/promises';
import { join, resolve } from 'node:path';
import { promisify } from 'node:util';
import { isRecordId } from './catalog-contract.js';
import type { CatalogResult, RecordSource } from './catalog-contract.js';
import {
  extractSourceSection,
  MAX_SOURCE_FILE_BYTES,
  sourcePathParts,
  sourceReadability,
  sourceSectionFailure,
} from './source-section-contract.js';
import type { SourceSectionFailure, SourceSectionResult } from './source-section-contract.js';

const nativeRealpath = promisify(realpathCallback.native);
type SourceFile = { ok: true; path: string; stat: Stats } | SourceSectionFailure;

function missingError(error: unknown): boolean {
  return typeof error === 'object' && error !== null && 'code' in error
    && (error.code === 'ENOENT' || error.code === 'ENOTDIR');
}

function sameFile(a: Stats, b: Stats): boolean {
  return a.dev === b.dev && a.ino === b.ino;
}

function sameSnapshot(a: Stats, b: Stats): boolean {
  return sameFile(a, b) && a.size === b.size && a.mtimeMs === b.mtimeMs && a.ctimeMs === b.ctimeMs;
}

async function resolveSourceFile(repositoryRoot: string, parts: string[]): Promise<SourceFile> {
  try {
    const root = resolve(repositoryRoot);
    if ((await lstat(root)).isSymbolicLink()) return sourceSectionFailure('path-rejected', 'link');
    const canonicalRoot = await nativeRealpath(root);
    let path = root;
    let checked: Stats | null = null;
    // Walk from the fixed root before resolving the target: realpath alone
    // would conceal a junction, including one that points back inside the root.
    for (const part of parts) {
      path = join(path, part);
      checked = await lstat(path);
      if (checked.isSymbolicLink()) return sourceSectionFailure('path-rejected', 'link');
    }
    const canonicalPath = await nativeRealpath(path);
    if (canonicalPath !== join(canonicalRoot, ...parts)) return sourceSectionFailure('path-rejected', 'case');
    if (!checked?.isFile()) return sourceSectionFailure('not-readable', 'not-file');
    return { ok: true, path, stat: checked };
  } catch (error) {
    return sourceSectionFailure(missingError(error) ? 'missing' : 'load');
  }
}

// The index checker also verifies non-Markdown git locators, without reading
// their content. It shares both path classification and filesystem checks.
export async function inspectSourceFile({ repositoryRoot, locator }: { repositoryRoot: string; locator: string }): Promise<SourceFile> {
  const path = sourcePathParts(locator);
  return path.ok ? resolveSourceFile(repositoryRoot, path.parts) : path;
}

export type WholeSourceFileResult = { ok: true; text: string } | SourceSectionFailure;

// The source reading boundary shared by section reads and whole-file reads:
// source kind and path shape, the checked path, the opened handle's identity,
// the 1 MiB file limit, read snapshots and strict UTF-8.
async function readSourceFileText(repositoryRoot: string, source: RecordSource): Promise<WholeSourceFileResult> {
  const unreadableReason = sourceReadability(source);
  if (unreadableReason === 'local-only' || unreadableReason === 'handoff') {
    return sourceSectionFailure('not-readable', unreadableReason);
  }
  const parts = sourcePathParts(source.locator);
  if (!parts.ok) return parts;
  // Invalid paths take priority over a git source's unsupported extension.
  if (unreadableReason === 'extension') {
    return sourceSectionFailure('not-readable', unreadableReason);
  }
  const file = await resolveSourceFile(repositoryRoot, parts.parts);
  if (!file.ok) return file;

  try {
    const handle = await open(file.path, 'r');
    let buffer: Buffer;
    try {
      const before = await handle.stat();
      // The pathname check and open are separate operations. The opened
      // handle must still identify the file checked above before any read.
      if (!before.isFile() || !sameFile(file.stat, before)) return sourceSectionFailure('changed');
      if (before.size > MAX_SOURCE_FILE_BYTES) return sourceSectionFailure('too-large', 'file');
      buffer = Buffer.alloc(MAX_SOURCE_FILE_BYTES + 1);
      let length = 0;
      while (length < buffer.length) {
        const result = await handle.read(buffer, length, buffer.length - length, length);
        if (result.bytesRead === 0) break;
        length += result.bytesRead;
      }
      const after = await handle.stat();
      if (length > MAX_SOURCE_FILE_BYTES || after.size > MAX_SOURCE_FILE_BYTES) {
        return sourceSectionFailure('too-large', 'file');
      }
      let current: Stats;
      try {
        current = await lstat(file.path);
      } catch (error) {
        if (missingError(error)) return sourceSectionFailure('changed');
        throw error;
      }
      if (!sameSnapshot(before, after) || !sameSnapshot(after, current) || length !== before.size) {
        return sourceSectionFailure('changed');
      }
      buffer = buffer.subarray(0, length);
    } finally {
      await handle.close();
    }
    try {
      // The decoder's default consumes one leading BOM. Both entry points then
      // remove one more, as extractSourceSection does.
      return { ok: true, text: new TextDecoder('utf-8', { fatal: true }).decode(buffer) };
    } catch {
      return sourceSectionFailure('invalid-encoding');
    }
  } catch (error) {
    return sourceSectionFailure(missingError(error) ? 'changed' : 'load');
  }
}

// Reads a source file whole under the 1 MiB file limit only. The 256 KiB
// section limit bounds the text that source views and MCP pass on; the
// BACKLOG list (backlog-store.ts) needs every row of the file instead.
export async function readWholeSourceFile({ repositoryRoot, source }: { repositoryRoot: string; source: RecordSource }): Promise<WholeSourceFileResult> {
  const file = await readSourceFileText(repositoryRoot, source);
  if (!file.ok) return file;
  // Keep this the same as the BOM step of extractSourceSection, so the text
  // equals a section-null read of the same file under the section limit.
  const text = file.text.charCodeAt(0) === 0xfeff ? file.text.slice(1) : file.text;
  return { ok: true, text };
}

export function createSourceSectionStore({ repositoryRoot }: { repositoryRoot: string }) {
  async function read(source: RecordSource): Promise<SourceSectionResult> {
    const file = await readSourceFileText(repositoryRoot, source);
    return file.ok ? extractSourceSection(source, file.text) : file;
  }
  return { read };
}

export function createSourceSectionReader({ readCatalog, store }: {
  readCatalog(): Promise<CatalogResult>;
  store: { read(source: RecordSource): Promise<SourceSectionResult> };
}) {
  async function read(input: unknown): Promise<SourceSectionResult> {
    if (!isRecordId(input)) return sourceSectionFailure('invalid-request');
    let index: CatalogResult;
    try {
      index = await readCatalog();
    } catch {
      return sourceSectionFailure('index-unavailable');
    }
    if (!index.ok) return sourceSectionFailure('index-unavailable');
    const source = index.catalog.sources.find(item => item.id === input);
    if (!source) return sourceSectionFailure('unknown-source');
    return store.read(source);
  }
  return { read };
}

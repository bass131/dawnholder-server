import { open } from 'node:fs/promises';
import { MAX_CATALOG_BYTES, catalogReferenceErrors, readCatalog } from './catalog-contract.js';
import type { CatalogResult } from './catalog-contract.js';
import { catalogHash } from './catalog-hash.js';

const invalid = { ok: false, code: 'invalid', message: '기록 색인의 JSON·형식·ID·참조를 확인하세요.' } as const;
const tooLarge = { ok: false, code: 'invalid', message: '기록 파일은 2 MiB 이하이어야 합니다.' } as const;
type CatalogFileResult = { ok: true; text: string } | { ok: false; code: 'missing' | 'load' | 'invalid'; message: string };

// Internal file inspection is shared with the CLI; editable text never crosses
// the CatalogResult boundary exposed to the renderer.
export async function readCatalogFile(path: string): Promise<CatalogFileResult> {
  try {
    const handle = await open(path, 'r');
    let buffer: Buffer;
    try {
      const before = await handle.stat();
      if (!before.isFile()) return { ok: false, code: 'load', message: '기록 파일을 읽지 못했습니다. 파일 접근 상태를 확인하세요.' };
      if (before.size > MAX_CATALOG_BYTES) return tooLarge;
      buffer = Buffer.alloc(MAX_CATALOG_BYTES + 1);
      let length = 0;
      while (length < buffer.length) {
        const result = await handle.read(buffer, length, buffer.length - length, length);
        if (result.bytesRead === 0) break;
        length += result.bytesRead;
      }
      if (length > MAX_CATALOG_BYTES) return tooLarge;
      buffer = buffer.subarray(0, length);
    } finally {
      await handle.close();
    }
    return { ok: true, text: buffer.toString('utf8') };
  } catch (error) {
    const missing = typeof error === 'object' && error !== null && 'code' in error && error.code === 'ENOENT';
    return {
      ok: false,
      code: missing ? 'missing' : 'load',
      message: missing ? '기록 파일이 없습니다.' : '기록 파일을 읽지 못했습니다. 파일 접근 상태를 확인하세요.',
    };
  }
}

export function createCatalogStore(path: string) {
  async function read(): Promise<CatalogResult> {
    const file = await readCatalogFile(path);
    if (!file.ok) return file;
    let value: unknown;
    try {
      value = JSON.parse(file.text);
    } catch {
      return invalid;
    }
    const catalog = readCatalog(value);
    if (!catalog || catalogReferenceErrors(catalog).length > 0) return invalid;
    return { ok: true, catalog, version: catalogHash(file.text) };
  }
  return { read };
}

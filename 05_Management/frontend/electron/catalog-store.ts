import { randomUUID } from 'node:crypto';
import { mkdir, open, readFile, rename, unlink } from 'node:fs/promises';
import { dirname, join } from 'node:path';
import { MAX_CATALOG_BYTES, catalogReferenceErrors, readCatalog, type CatalogResult } from './catalog-contract.js';
import { catalogHash } from './catalog-hash.js';
import { createCatalogRenamer, type CatalogRenameOptions } from './catalog-rename.js';

function code(error: unknown): string | undefined { return typeof error === 'object' && error !== null && 'code' in error ? String(error.code) : undefined; }
function parse(text: string): CatalogResult {
  if (Buffer.byteLength(text, 'utf8') > MAX_CATALOG_BYTES) return { ok: false, code: 'invalid', message: '기록 파일은 2 MiB 이하이어야 합니다.' };
  try {
    const catalog = readCatalog(JSON.parse(text));
    if (!catalog) return { ok: false, code: 'invalid', message: '카탈로그 스키마 또는 중복 ID를 확인하세요.' };
    const errors = catalogReferenceErrors(catalog);
    if (errors.length) return { ok: false, code: 'invalid', message: errors.slice(0, 5).join('\n') };
    return { ok: true, catalog, text, version: catalogHash(text) };
  } catch { return { ok: false, code: 'invalid', message: '유효한 JSON 기록을 읽을 수 없습니다.' }; }
}
export function createCatalogStore(path: string, backupPath: string, renameOptions: CatalogRenameOptions = {}) {
  const renameCatalog = createCatalogRenamer(renameOptions);
  async function read(): Promise<CatalogResult> {
    try {
      const handle = await open(path, 'r');
      try {
        if ((await handle.stat()).size > MAX_CATALOG_BYTES) return { ok: false, code: 'invalid', message: '기록 파일은 2 MiB 이하이어야 합니다.' };
        const text = await handle.readFile('utf8');
        const result = parse(text);
        return result.ok ? result : { ...result, text, version: catalogHash(text) };
      } finally { await handle.close(); }
    } catch (error) {
      return { ok: false, code: code(error) === 'ENOENT' ? 'missing' : 'load', message: code(error) === 'ENOENT' ? '기록 파일이 없습니다. JSON을 불러온 뒤 저장하여 초기화할 수 있습니다.' : '기록 파일을 읽지 못했습니다. 파일 접근 상태를 확인하세요.' };
    }
  }
  async function save(input: unknown): Promise<CatalogResult> {
    if (typeof input !== 'object' || input === null || !('text' in input) || typeof input.text !== 'string' || !('expectedVersion' in input) || (input.expectedVersion !== null && (typeof input.expectedVersion !== 'string' || !/^[a-f0-9]{64}$/.test(input.expectedVersion)))) return { ok: false, code: 'invalid', message: '저장 요청 형식이 올바르지 않습니다.' };
    const next = parse(input.text);
    if (!next.ok) return next;
    const lockPath = join(dirname(path), '.catalog.lock');
    const temporaryPath = join(dirname(path), `.catalog-${randomUUID()}.tmp`);
    let locked = false;
    let temporaryCreated = false;
    try {
      await mkdir(dirname(path), { recursive: true });
      const lock = await open(lockPath, 'wx');
      locked = true;
      await lock.close();
      let previousText: string | null = null;
      try { previousText = await readFile(path, 'utf8'); } catch (error) { if (code(error) !== 'ENOENT') throw error; }
      const previousVersion = previousText === null ? null : catalogHash(previousText);
      if (previousVersion !== input.expectedVersion) return { ok: false, code: 'conflict', message: '다른 작업에서 기록을 변경했습니다. 초안을 보존하고 최신 기록을 다시 읽어 비교하세요.' };
      if (previousText !== null && parse(previousText).ok) {
        await mkdir(dirname(backupPath), { recursive: true });
        const backupTemporary = `${backupPath}.${randomUUID()}.tmp`;
        let backupCreated = false;
        try {
          const backup = await open(backupTemporary, 'wx'); backupCreated = true;
          try { await backup.writeFile(previousText, 'utf8'); await backup.sync(); } finally { await backup.close(); }
          await rename(backupTemporary, backupPath); backupCreated = false;
        } finally { if (backupCreated) await unlink(backupTemporary).catch(() => undefined); }
      }
      const temporary = await open(temporaryPath, 'wx'); temporaryCreated = true;
      try { await temporary.writeFile(next.text, 'utf8'); await temporary.sync(); } finally { await temporary.close(); }
      // Keep the existing lock and prepared backup throughout the bounded
      // Windows sharing-violation retry; never truncate or unlink the target.
      await renameCatalog(temporaryPath, path); temporaryCreated = false;
      return next;
    } catch (error) {
      return { ok: false, code: code(error) === 'EEXIST' && !locked ? 'busy' : 'write', message: code(error) === 'EEXIST' && !locked ? '다른 앱이 저장 중이거나 기록 잠금이 남아 있습니다. 잠금은 자동 삭제하지 않습니다.' : '기록 또는 복구용 백업을 저장하지 못했습니다. 현재 기록과 초안을 보존합니다.' };
    } finally {
      if (temporaryCreated) await unlink(temporaryPath).catch(() => undefined);
      if (locked) await unlink(lockPath).catch(() => undefined);
    }
  }
  return { read, save };
}

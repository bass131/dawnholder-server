import { open, stat } from 'node:fs/promises';
import { catalogHash } from './catalog-hash.js';
import { MAX_GUIDE_BYTES, readSystemGuide, type GuideResult } from './system-guide-contract.js';

function same(a: Awaited<ReturnType<typeof stat>>, b: Awaited<ReturnType<typeof stat>>): boolean {
  return a.size === b.size && a.mtimeMs === b.mtimeMs && a.ctimeMs === b.ctimeMs && a.dev === b.dev && a.ino === b.ino;
}
const invalid = { ok: false, code: 'invalid', message: '카드 자료의 UTF-8·JSON·ID·부모·문서·commit·매핑 참조를 확인하세요. 일부 자료는 표시하지 않습니다.' } as const;
const tooLarge = { ok: false, code: 'too-large', message: '카드 자료는 2 MiB 이하이어야 합니다.' } as const;

export function createSystemGuideStore(path: string) {
  let reading = false;
  async function read(): Promise<GuideResult> {
    if (reading) return { ok: false, code: 'busy', message: '카드 자료를 읽는 중입니다. 완료 후 다시 읽으세요.' };
    reading = true;
    try {
      const handle = await open(path, 'r');
      let buffer: Buffer;
      try {
        const before = await handle.stat();
        if (!before.isFile()) return invalid;
        if (before.size > MAX_GUIDE_BYTES) return tooLarge;
        buffer = Buffer.alloc(MAX_GUIDE_BYTES + 1);
        let length = 0;
        while (length < buffer.length) {
          const result = await handle.read(buffer, length, buffer.length - length, length);
          if (result.bytesRead === 0) break;
          length += result.bytesRead;
        }
        const after = await handle.stat();
        const current = await stat(path);
        if (length > MAX_GUIDE_BYTES || after.size > MAX_GUIDE_BYTES) return tooLarge;
        if (!same(before, after) || !same(after, current) || length !== before.size) return { ok: false, code: 'changed', message: '읽는 동안 카드 자료가 변경되었습니다. 다시 읽으세요.' };
        buffer = buffer.subarray(0, length);
      } finally { await handle.close(); }
      let parsed: unknown;
      let source: string;
      try { source = new TextDecoder('utf-8', { fatal: true, ignoreBOM: true }).decode(buffer); parsed = JSON.parse(source); } catch { return invalid; }
      const guide = readSystemGuide(parsed);
      if (!guide) return invalid;
      return { ok: true, guide, version: catalogHash(source), bytes: buffer.length };
    } catch (error) {
      const missing = typeof error === 'object' && error !== null && 'code' in error && error.code === 'ENOENT';
      return { ok: false, code: missing ? 'missing' : 'load', message: missing ? '05_Management/records/system-guide.json이 없습니다.' : '카드 자료를 읽지 못했습니다. 파일 접근 상태를 확인하세요.' };
    } finally { reading = false; }
  }
  return { read };
}

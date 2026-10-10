import { randomUUID } from 'node:crypto';
import { mkdir, readFile, rename, rm, writeFile } from 'node:fs/promises';
import { join } from 'node:path';

import { isSessionId } from '../MergeGate/merge-policy.mjs';

export function emptySessionState() {
  return { version: 1, memoWrittenAt: null, denials: [], errors: [] };
}

export function sessionStore(project, sessionId) {
  if (typeof project !== 'string' || !project.trim() || !isSessionId(sessionId)) throw new Error('상태 파일 경로가 유효하지 않다.');
  const directory = join(project, '.claude', 'state', 'session-guard');
  const recordPath = join(directory, `${sessionId}.json`);
  return {
    async read() {
      let bytes;
      try {
        bytes = await readFile(recordPath);
      } catch (error) {
        if (error.code === 'ENOENT' || error.code === 'ENOTDIR') return emptySessionState();
        throw error;
      }
      const record = JSON.parse(new TextDecoder('utf-8', { fatal: true }).decode(bytes));
      if (record?.version !== 1 || !Array.isArray(record.denials) || !Array.isArray(record.errors) ||
          (record.memoWrittenAt !== null && (typeof record.memoWrittenAt !== 'string' || !Number.isFinite(Date.parse(record.memoWrittenAt))))) {
        throw new Error('세션 상태 형식이 유효하지 않다.');
      }
      return record;
    },
    async write(record) {
      await mkdir(directory, { recursive: true });
      const temporary = join(directory, `${sessionId}.${randomUUID()}.tmp`);
      try {
        await writeFile(temporary, `${JSON.stringify(record)}\n`, { encoding: 'utf8', flag: 'wx' });
        await rename(temporary, recordPath);
      } finally {
        await rm(temporary, { force: true });
      }
    },
  };
}

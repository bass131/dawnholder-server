import { randomUUID } from 'node:crypto';
import { mkdir, readFile, rename, rm, stat, writeFile } from 'node:fs/promises';
import { join } from 'node:path';

import { isApprovalRecord, isSessionId } from './merge-policy.mjs';

export function approvalStore(projectDirectory, sessionId) {
  if (typeof projectDirectory !== 'string' || projectDirectory.trim().length === 0 || !isSessionId(sessionId)) {
    throw new Error('CLAUDE_PROJECT_DIR 또는 session_id가 유효하지 않다.');
  }
  const stateDirectory = join(projectDirectory, '.claude', 'state', 'merge-gate');
  const approvalsDirectory = join(stateDirectory, 'approvals');
  const recordPath = join(approvalsDirectory, `${sessionId}.json`);

  return {
    async hasMarker() {
      const marker = await stat(join(stateDirectory, 'main-checkout')).catch(() => null);
      return marker?.isFile() ?? false;
    },
    async read() {
      try {
        const bytes = await readFile(recordPath);
        const record = JSON.parse(new TextDecoder('utf-8', { fatal: true }).decode(bytes));
        return isApprovalRecord(record) ? record : null;
      } catch {
        // Missing, unreadable and malformed records confer no approval.
        return null;
      }
    },
    async write(record) {
      if (!isApprovalRecord(record)) throw new Error('저장할 승인 기록 형식이 유효하지 않다.');
      await mkdir(approvalsDirectory, { recursive: true });
      const temporaryPath = join(approvalsDirectory, `${sessionId}.${randomUUID()}.tmp`);
      try {
        await writeFile(temporaryPath, `${JSON.stringify(record)}\n`, { encoding: 'utf8', flag: 'wx' });
        // Same-directory rename commits one complete record before a merge can be allowed.
        // Sequential hooks are assumed: overlapping merges, or a prompt and merge, can duplicate
        // a pass or restore a consumed approval. Only the originally approved PR/head can return.
        // No lock is added because a crash could strand it.
        await rename(temporaryPath, recordPath);
      } finally {
        await rm(temporaryPath, { force: true });
      }
    },
  };
}

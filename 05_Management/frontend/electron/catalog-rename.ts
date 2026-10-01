import { rename as nativeRename } from 'node:fs/promises';
import { setTimeout } from 'node:timers/promises';

export interface CatalogRenameObservation {
  attempt: number;
  elapsedMs: number;
  outcome: 'succeeded' | 'failed';
  code?: string;
}
export interface CatalogRenameOptions {
  rename?: (source: string, target: string) => Promise<void>;
  wait?: (milliseconds: number) => Promise<void>;
  now?: () => number;
  onAttempt?: (observation: CatalogRenameObservation) => void;
}

export const CATALOG_RENAME_RETRY_MS = 1000;

export function createCatalogRenamer(options: CatalogRenameOptions = {}) {
  const rename = options.rename ?? nativeRename;
  const wait = options.wait ?? (async (milliseconds: number) => { await setTimeout(milliseconds); });
  const now = options.now ?? (() => performance.now());
  return async (source: string, target: string): Promise<void> => {
    const started = now();
    let attempt = 0;
    let backoffMs = 10;
    while (true) {
      attempt += 1;
      try {
        await rename(source, target);
        options.onAttempt?.({ attempt, elapsedMs: now() - started, outcome: 'succeeded' });
        return;
      } catch (error) {
        const code = typeof error === 'object' && error !== null && 'code' in error && typeof error.code === 'string' ? error.code : undefined;
        const elapsedMs = now() - started;
        options.onAttempt?.({ attempt, elapsedMs, outcome: 'failed', ...(code ? { code } : {}) });
        if (!code || !['EPERM', 'EACCES', 'EBUSY'].includes(code) || elapsedMs >= CATALOG_RENAME_RETRY_MS) throw error;
        await wait(Math.min(backoffMs, CATALOG_RENAME_RETRY_MS - elapsedMs));
        if (now() - started >= CATALOG_RENAME_RETRY_MS) throw error;
        backoffMs = Math.min(40, backoffMs * 2);
      }
    }
  };
}

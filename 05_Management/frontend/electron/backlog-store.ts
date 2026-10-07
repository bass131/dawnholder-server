import { lstat, readFile } from 'node:fs/promises';
import { dirname, isAbsolute, join, relative, resolve } from 'node:path';
import { backlogFormatIssues, backlogReadFailure, backlogRowIssues, missingBacklogGoalIssue } from './backlog-contract.js';
import type { BacklogResult } from './backlog-contract.js';
import { parseBacklogTables } from './backlog-table.js';

const backlogPath = '00_Document/operations/BACKLOG.md';

function isMissing(error: unknown): boolean {
  return typeof error === 'object' && error !== null && 'code' in error && error.code === 'ENOENT';
}

export function createBacklogStore({ repositoryRoot }: { repositoryRoot: string }) {
  async function read(): Promise<BacklogResult> {
    let text: string;
    try {
      text = await readFile(join(repositoryRoot, backlogPath), 'utf8');
    } catch (error) {
      return backlogReadFailure(isMissing(error) ? 'missing' : 'load');
    }

    const table = parseBacklogTables(text);
    const result: Extract<BacklogResult, { ok: true }> = {
      ok: true,
      rows: table.rows,
      issues: backlogFormatIssues(table),
      uncheckedLinks: [],
    };
    const ids = new Set<string>();
    for (const row of table.rows) {
      result.issues.push(...backlogRowIssues(row, ids));
      ids.add(row.id);
      for (const link of row.goalLinks) {
        let exists = false;
        try {
          const path = resolve(repositoryRoot, dirname(backlogPath), decodeURIComponent(link));
          const within = relative(resolve(repositoryRoot), path);
          if (!isAbsolute(within) && within !== '..' && !within.startsWith(`..${process.platform === 'win32' ? '\\' : '/'}`)) {
            exists = (await lstat(path)).isFile();
          }
        } catch (error) {
          if (!isMissing(error) && !(error instanceof URIError)) {
            // Keep the earlier warnings, while stopping an ordered inspection
            // whose next link could not be checked. This is not a violation.
            result.uncheckedLinks.push({ line: row.line, link });
            return result;
          }
        }
        if (!exists) result.issues.push(missingBacklogGoalIssue(row.line));
      }
    }
    return result;
  }
  return { read };
}

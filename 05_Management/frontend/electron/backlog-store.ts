import { dirname, isAbsolute, relative, resolve, sep } from 'node:path';
import {
  backlogFormatIssues,
  backlogReadFailure,
  backlogRowIssues,
  missingBacklogGoalIssue,
  rejectedBacklogGoalIssue,
} from './backlog-contract.js';
import type { BacklogResult } from './backlog-contract.js';
import { parseBacklogTables } from './backlog-table.js';
import type { RecordSource } from './catalog-contract.js';
import { inspectSourceFile, readWholeSourceFile } from './source-section-store.js';

type InspectedGoalFile = Awaited<ReturnType<typeof inspectSourceFile>>;

const backlogPath = '00_Document/operations/BACKLOG.md';
const backlogSource: RecordSource = {
  id: 'development-backlog',
  title: '이후 작업',
  kind: 'git',
  locator: backlogPath,
  section: null,
  availability: 'versioned',
};

export function createBacklogStore({ repositoryRoot }: { repositoryRoot: string }) {
  async function read(): Promise<BacklogResult> {
    // The fixed source keeps both callers on the source reading boundary: path,
    // file identity, the 1 MiB file limit, strict UTF-8 and read snapshots.
    // The list needs every row, so the 256 KiB section limit does not apply.
    // Renderer input never chooses the BACKLOG path.
    const source = await readWholeSourceFile({ repositoryRoot, source: backlogSource });
    if (!source.ok) {
      switch (source.code) {
        case 'missing':
        case 'too-large':
        case 'invalid-encoding':
        case 'changed':
          return backlogReadFailure(source.code);
        default:
          return backlogReadFailure('load');
      }
    }

    const table = parseBacklogTables(source.text);
    const result: Extract<BacklogResult, { ok: true }> = {
      ok: true,
      rows: table.rows,
      issues: backlogFormatIssues(table),
      uncheckedLinks: [],
    };
    const ids = new Set<string>();
    // One check per repository-relative locator serves every occurrence, and only
    // within this read: files can change between reads, so each read starts empty.
    const inspectedByLocator = new Map<string, InspectedGoalFile>();
    for (const row of table.rows) {
      result.issues.push(...backlogRowIssues(row, ids));
      ids.add(row.id);
      for (const link of row.goalLinks) {
        let decoded: string;
        try {
          decoded = decodeURIComponent(link);
        } catch {
          result.issues.push(rejectedBacklogGoalIssue(row.line, 'invalid'));
          continue;
        }
        // Reject decoded backslashes before OS path resolution so they cannot
        // become separators on Windows while being literal text elsewhere.
        if (decoded.includes('\\')) {
          result.issues.push(rejectedBacklogGoalIssue(row.line, 'invalid'));
          continue;
        }
        const path = resolve(repositoryRoot, dirname(backlogPath), decoded);
        const within = relative(resolve(repositoryRoot), path);
        if (isAbsolute(within) || within === '..' || within.startsWith(`..${sep}`)) {
          result.issues.push(rejectedBacklogGoalIssue(row.line, 'parent'));
          continue;
        }
        const locator = within.split(sep).join('/');
        let inspected = inspectedByLocator.get(locator);
        if (inspected === undefined) {
          inspected = await inspectSourceFile({ repositoryRoot, locator });
          inspectedByLocator.set(locator, inspected);
        }
        if (inspected.ok) continue;
        if (inspected.code === 'path-rejected') {
          result.issues.push(rejectedBacklogGoalIssue(row.line, inspected.reason ?? 'invalid'));
        } else if (inspected.code === 'missing' || inspected.code === 'not-readable') {
          result.issues.push(missingBacklogGoalIssue(row.line));
        } else {
          // An I/O failure says nothing about this link's validity. Keep it
          // unchecked and continue so later links and rows still get judged.
          result.uncheckedLinks.push({ line: row.line, link });
        }
      }
    }
    return result;
  }
  return { read };
}

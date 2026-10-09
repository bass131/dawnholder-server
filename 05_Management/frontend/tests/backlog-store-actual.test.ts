// @vitest-environment node
// Requirement: backlog-menu-design.md 「시험」 실제 자료: the store reads the real
// 00_Document/operations/BACKLOG.md successfully, with no BACKLOG_TABLE_FORMAT and a group on every
// candidate. Values Rules changes (row count, mismatch issues) are not fixed: mismatches are printed
// for the report and not asserted, because BACKLOG is Rules data and is reported, not edited.
// Fails until the behaviour step adds `group`. Reads the real repository, read only.
import { fileURLToPath } from 'node:url';
import { describe, expect, it } from 'vitest';
import { createBacklogStore } from '../electron/backlog-store';

const repositoryRoot = fileURLToPath(new URL('../../../', import.meta.url));

describe('the real BACKLOG.md', () => {
  it('reads with no table-format issue and a group on every candidate', async () => {
    const result = await createBacklogStore({ repositoryRoot }).read();
    const rows = result.ok ? result.rows : [];
    const issues = result.ok ? result.issues : [];
    const mismatches = issues.filter(issue => issue.code !== 'BACKLOG_TABLE_FORMAT');
    if (mismatches.length > 0) console.info(`[backlog actual] mismatches: ${JSON.stringify(mismatches)}`);

    const outcome = {
      ok: result.ok,
      hasCandidates: rows.length > 0,
      tableFormatIssues: issues.filter(issue => issue.code === 'BACKLOG_TABLE_FORMAT'),
      rowsWithoutGroup: rows.filter(row => typeof row.group !== 'string' || row.group.trim() === '').map(row => `${row.line} ${row.id}`),
    };
    expect(outcome).toEqual({ ok: true, hasCandidates: true, tableFormatIssues: [], rowsWithoutGroup: [] });
  });
});

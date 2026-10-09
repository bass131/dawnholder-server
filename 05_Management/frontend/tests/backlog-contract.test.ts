// @vitest-environment node
// Requirement: backlog-menu-design.md 「공유 백로그 판정」: backlogIssueKind is the one place that sorts
// a backlog code into the screen's 「형식 오류」(format) or 「어긋남」(mismatch); only
// BACKLOG_TABLE_FORMAT is format. The screen uses it instead of comparing code strings.
// Fails until backlog-contract.ts exports it; the screen needs it in the behaviour step, and the
// structure step may already add it with the contract. Expected kinds are written from the design.
import { describe, expect, it } from 'vitest';
import { backlogIssueKind } from '../electron/backlog-contract';

describe('backlogIssueKind', () => {
  it('classifies only BACKLOG_TABLE_FORMAT as format and every other backlog code as mismatch', () => {
    const codes = [
      'BACKLOG_TABLE_FORMAT', 'BACKLOG_ID_FORMAT', 'BACKLOG_ID_DUPLICATE',
      'BACKLOG_PROMOTION_LINK_MISSING', 'BACKLOG_GOAL_LINK_MISSING', 'BACKLOG_GOAL_LINK_REJECTED',
    ] as const;
    expect(Object.fromEntries(codes.map(code => [code, backlogIssueKind(code)]))).toEqual({
      BACKLOG_TABLE_FORMAT: 'format',
      BACKLOG_ID_FORMAT: 'mismatch',
      BACKLOG_ID_DUPLICATE: 'mismatch',
      BACKLOG_PROMOTION_LINK_MISSING: 'mismatch',
      BACKLOG_GOAL_LINK_MISSING: 'mismatch',
      BACKLOG_GOAL_LINK_REJECTED: 'mismatch',
    });
  });
});

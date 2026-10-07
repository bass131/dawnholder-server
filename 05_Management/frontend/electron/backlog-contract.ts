import type { BacklogRow, BacklogTableResult } from './backlog-table.js';
import { isRecordId } from './catalog-contract.js';

export type BacklogIssueCode =
  | 'BACKLOG_TABLE_FORMAT' | 'BACKLOG_ID_FORMAT' | 'BACKLOG_ID_DUPLICATE'
  | 'BACKLOG_PROMOTION_LINK_MISSING' | 'BACKLOG_GOAL_LINK_MISSING' | 'BACKLOG_GOAL_LINK_REJECTED';

export interface BacklogIssue {
  code: BacklogIssueCode;
  line: number | null;
  cause: string;
  fix: string;
}

export type BacklogResult =
  | {
    ok: true;
    rows: BacklogRow[];
    issues: BacklogIssue[];
    uncheckedLinks: { line: number; link: string }[];
  }
  | {
    ok: false;
    code: 'missing' | 'too-large' | 'invalid-encoding' | 'changed' | 'load' | 'denied';
    message: string;
  };

export interface BacklogBridge {
  readBacklog(): Promise<BacklogResult>;
}

export function backlogFormatIssues(table: BacklogTableResult): BacklogIssue[] {
  return table.formatIssues.map(issue => ({
    code: 'BACKLOG_TABLE_FORMAT',
    line: issue.line,
    cause: issue.cause,
    fix: 'ID 표의 머리글·구분 행·자료 행의 열 수를 맞추세요.',
  }));
}

export function backlogRowIssues(row: BacklogRow, previousIds: ReadonlySet<string>): BacklogIssue[] {
  const issues: BacklogIssue[] = [];
  if (!isRecordId(row.id)) {
    issues.push({
      code: 'BACKLOG_ID_FORMAT',
      line: row.line,
      cause: '후보 ID가 소문자 단어를 하이픈으로 잇는 형식이 아닙니다.',
      fix: '128자 이하의 안정적인 소문자 ID를 쓰세요.',
    });
  }
  if (previousIds.has(row.id)) {
    issues.push({
      code: 'BACKLOG_ID_DUPLICATE',
      line: row.line,
      cause: `후보 ID ${row.id}가 중복됩니다.`,
      fix: '후보마다 서로 다른 안정적인 ID를 지정하세요.',
    });
  }
  if (row.status.includes('goal 승격') && row.goalLinks.length === 0) {
    issues.push({
      code: 'BACKLOG_PROMOTION_LINK_MISSING',
      line: row.line,
      cause: 'goal 승격 행에 goal 상대 링크가 없습니다.',
      fix: '승격한 goal의 원문 파일로 가는 상대 링크를 상태에 추가하세요.',
    });
  }
  return issues;
}

export function missingBacklogGoalIssue(line: number): BacklogIssue {
  return {
    code: 'BACKLOG_GOAL_LINK_MISSING',
    line,
    cause: '상대 goal 링크의 대상 파일이 없습니다.',
    fix: 'BACKLOG.md 기준 상대경로와 대상 goal 파일의 존재를 확인하세요.',
  };
}

export function backlogReadFailure(code: 'missing' | 'load'): Extract<BacklogResult, { ok: false }> {
  return {
    ok: false,
    code,
    message: code === 'missing'
      ? 'BACKLOG.md를 찾지 못했습니다.'
      : 'BACKLOG.md를 읽지 못했습니다. 다시 읽으세요.',
  };
}

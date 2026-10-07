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

export function backlogIssueKind(code: BacklogIssueCode): 'format' | 'mismatch' {
  return code === 'BACKLOG_TABLE_FORMAT' ? 'format' : 'mismatch';
}

export function backlogFormatIssues(table: BacklogTableResult): BacklogIssue[] {
  const issues: BacklogIssue[] = table.formatIssues.map(issue => ({
    code: 'BACKLOG_TABLE_FORMAT',
    line: issue.line,
    cause: issue.cause,
    fix: 'ID 표의 머리글·구분 행·자료 행의 열 수를 맞추세요.',
  }));
  if (!table.hasIdTable) {
    issues.push({
      code: 'BACKLOG_TABLE_FORMAT',
      line: null,
      cause: '첫 열이 ID인 후보 표가 없습니다.',
      fix: '머리글 첫 열이 ID인 후보 표와 구분 행을 두세요.',
    });
  }
  return issues;
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

export function rejectedBacklogGoalIssue(line: number, reason: string): BacklogIssue {
  return {
    code: 'BACKLOG_GOAL_LINK_REJECTED',
    line,
    cause: `상대 goal 링크가 저장소 경로 규칙에 맞지 않습니다 (${reason}).`,
    fix: 'BACKLOG.md 기준 상대경로로 저장소 안의 goal 파일을 대소문자까지 같게 가리키고 링크·junction을 거치지 마세요.',
  };
}

type BacklogReadFailureCode = Exclude<Extract<BacklogResult, { ok: false }>['code'], 'denied'>;
const readFailureMessages: Record<BacklogReadFailureCode, string> = {
  missing: 'BACKLOG.md를 찾지 못했습니다.',
  'too-large': 'BACKLOG.md가 읽기 크기 상한을 넘습니다.',
  'invalid-encoding': 'BACKLOG.md가 UTF-8 문서가 아닙니다.',
  changed: '읽는 동안 BACKLOG.md가 바뀌었습니다. 다시 읽으세요.',
  load: 'BACKLOG.md를 읽지 못했습니다. 다시 읽으세요.',
};

export function backlogReadFailure(code: BacklogReadFailureCode): Extract<BacklogResult, { ok: false }> {
  return { ok: false, code, message: readFailureMessages[code] };
}

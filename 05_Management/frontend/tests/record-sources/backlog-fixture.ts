// BACKLOG.md text for the backlog store tests. A test writes the whole document as lines and asks
// for the 1-based line of a row by its text, so expected `line` values are never counted by hand
// or by the product parser.

export const BACKLOG_PATH = '00_Document/operations/BACKLOG.md';
export const CANDIDATE_TABLE_HEADER = [
  '| ID | 제목 | 이유 | 출처 | 선행 조건 | 담당 후보 | 상태 |',
  '|---|---|---|---|---|---|---|',
];

export interface BacklogDocument {
  text: string;
  lineOf(line: string): number;
}

export function backlogDocument(lines: string[]): BacklogDocument {
  return {
    text: `${lines.join('\n')}\n`,
    lineOf(line) {
      const index = lines.indexOf(line);
      if (index < 0 || lines.indexOf(line, index + 1) >= 0) throw new Error(`not a unique BACKLOG line: ${line}`);
      return index + 1;
    },
  };
}

// A seven-column candidate row; only the cells a test is about need to be given.
export function candidateRow(id: string, cells: { title?: string; source?: string; prerequisite?: string; status?: string } = {}): string {
  const { title = `${id} 제목`, source = '메인 전달', prerequisite = '없음', status = '대기' } = cells;
  return `| \`${id}\` | ${title} | 이유 | ${source} | ${prerequisite} | Management | ${status} |`;
}

export interface BacklogRow {
  line: number;
  id: string;
  title: string;
  reason: string;
  source: string;
  prerequisite: string;
  owner: string;
  status: string;
  group: string | null;
  cells: string[];
  goalLinks: string[];
}

export interface BacklogTableResult {
  rows: BacklogRow[];
  formatIssues: { line: number; cause: string }[];
  hasIdTable: boolean;
}

function tableCells(line: string): string[] | null {
  const trimmed = line.trim();
  if (!trimmed.startsWith('|')) return null;
  const cells: string[] = [];
  let cell = '';
  let escaped = false;
  for (const character of trimmed.slice(1)) {
    if (escaped) {
      cell += character;
      escaped = false;
    } else if (character === '\\') {
      escaped = true;
    } else if (character === '|') {
      cells.push(cell.trim());
      cell = '';
    } else {
      cell += character;
    }
  }
  if (escaped) cell += '\\';
  if (cell.length > 0 || !trimmed.endsWith('|')) cells.push(cell.trim());
  return cells;
}

export function backlogGoalLinks(text: string): string[] {
  const links: string[] = [];
  for (const match of text.matchAll(/\[[^\]]*\]\(<?([^\s)>]+)>?(?:\s+"[^"]*")?\)/g)) {
    const target = match[1];
    if (!target || /^(?:[a-z][a-z0-9+.-]*:|\/|#)/i.test(target)) continue;
    const path = target.split('#')[0] ?? '';
    if (/(?:^|\/)(?:01_Phases|05_Management)\/goals\//.test(path)) links.push(path);
  }
  return links;
}

// Only ID-first tables contain candidates. The preceding field-description
// table is documentation, and must not appear in either the checker or menu.
export function parseBacklogTables(text: string): BacklogTableResult {
  const rows: BacklogRow[] = [];
  const formatIssues: BacklogTableResult['formatIssues'] = [];
  const lines = text.replace(/^\uFEFF/, '').split(/\r\n|\r|\n/);
  let headers: string[] | null = null;
  let group: string | null = null;
  let hasIdTable = false;
  let fence: { marker: string; length: number } | null = null;
  for (let index = 0; index < lines.length; index++) {
    const line = lines[index] ?? '';
    const marker = /^ {0,3}(`{3,}|~{3,})(.*)$/.exec(line);
    if (fence) {
      if (marker?.[1]?.[0] === fence.marker && marker[1].length >= fence.length && /^[ \t]*$/.test(marker[2] ?? '')) fence = null;
      continue;
    }
    if (marker?.[1]) {
      const character = marker[1][0] ?? '';
      // Match the source reader's opening rule without changing its shared
      // module: backticks in a backtick fence's info string prevent opening.
      if (character !== '`' || !marker[2]?.includes('`')) {
        fence = { marker: character, length: marker[1].length };
        headers = null;
        continue;
      }
    }
    const heading = /^ {0,3}##[ \t]+(.*)$/.exec(line);
    if (heading) {
      group = (heading[1] ?? '').replace(/(?:[ \t]+|^)#+[ \t]*$/, '').trim();
      headers = null;
      continue;
    }
    const cells = tableCells(line);
    if (!cells) {
      headers = null;
      continue;
    }
    if (!headers) {
      const separator = tableCells(lines[index + 1] ?? '');
      const validSeparator = separator !== null && separator.length === cells.length
        && separator.every(cell => /^:?-{3,}:?$/.test(cell));
      if (validSeparator) {
        // Remember other tables too, so an ID-valued documentation row cannot
        // be mistaken for the header of a new candidate table.
        headers = cells;
        if (headers[0] === 'ID') hasIdTable = true;
        index += 1;
      } else if (cells[0] === 'ID') {
        formatIssues.push({ line: index + 1, cause: 'ID 표의 구분 행 또는 열 수가 올바르지 않습니다.' });
      }
      continue;
    }
    if (headers[0] !== 'ID') continue;
    if (cells.length !== headers.length) {
      formatIssues.push({ line: index + 1, cause: `표의 열 수가 머리글 ${headers.length}개와 다릅니다.` });
    }
    const statusColumn = headers.findIndex(header => header === '상태');
    rows.push({
      line: index + 1,
      id: (cells[0] ?? '').replace(/^`+|`+$/g, '').trim(),
      title: cells[1] ?? '',
      reason: cells[2] ?? '',
      source: cells[3] ?? '',
      prerequisite: cells[4] ?? '',
      owner: cells[5] ?? '',
      status: cells[statusColumn >= 0 ? statusColumn : headers.length - 1] ?? '',
      group,
      cells,
      goalLinks: backlogGoalLinks(cells.join(' ')),
    });
  }
  return { rows, formatIssues, hasIdTable };
}

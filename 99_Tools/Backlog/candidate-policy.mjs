import { posix } from 'node:path';

// Policy and public input format: ../README.md#후보-도착-검사.
// File existence belongs to the caller; this module only reads text and path strings.
function slashPath(path) {
  return path == null ? null : path.replace(/\\/g, '/');
}

function diagnostic(code, path, line, message, repair) {
  return { code, path: slashPath(path), line, message, repair };
}

function result(status, backlogPath, goalPath, counts, diagnostics) {
  const exitCode = status === 'allowed' ? 0 : status === 'policy-violation' ? 1 : 2;
  return {
    status,
    exitCode,
    backlog: slashPath(backlogPath),
    goal: slashPath(goalPath),
    counts,
    diagnostics,
  };
}

// Shared by input parsing in the CLI and structural input checks below.
export function inputErrorResult(backlogPath, goalPath, diagnostics) {
  return result('input-error', backlogPath, goalPath, null, diagnostics);
}

function firstCell(line) {
  if (!line.includes('|')) return null;
  return line.trim().replace(/^\|/, '').split('|')[0].trim();
}

function readBacklog(text) {
  const lines = text.split(/\r?\n/);
  const entries = [];
  let hasTable = false;
  let inTable = false;
  for (let index = 0; index < lines.length; index += 1) {
    const cell = firstCell(lines[index]);
    const nextLine = lines[index + 1] ?? '';
    if (cell === 'ID' && nextLine.trim() !== '' && /^[|\-:\s]+$/.test(nextLine)) {
      hasTable = true;
      inTable = true;
      index += 1;
      continue;
    }
    if (cell === null) {
      inTable = false;
    } else if (inTable) {
      const id = cell.startsWith('`') && cell.endsWith('`') && cell.length >= 2
        ? cell.slice(1, -1) : cell;
      entries.push({ id, line: index + 1 });
    }
  }
  return { hasTable, entries };
}

export function extractBacklogIds(text) {
  return readBacklog(text).entries;
}

function readCandidateSection(text, goalPath) {
  const lines = text.split(/\r?\n/);
  const headings = [];
  for (let index = 0; index < lines.length; index += 1) {
    if (lines[index].trimEnd() === '## 다음 계획 후보') headings.push(index);
  }
  if (headings.length === 0) {
    return { diagnostics: [diagnostic('missing-section', goalPath, null,
      'The goal has no exact heading "## 다음 계획 후보".',
      'Add one "## 다음 계획 후보" section and list the candidates there.')] };
  }
  if (headings.length > 1) {
    return { diagnostics: [diagnostic('duplicate-section', goalPath, headings[1] + 1,
      'The goal has more than one "## 다음 계획 후보" section.',
      'Combine the candidate lists under a single "## 다음 계획 후보" heading.')] };
  }

  const candidates = [];
  let current = null;
  for (let index = headings[0] + 1; index < lines.length; index += 1) {
    const line = lines[index];
    if (/^#{1,2} /.test(line)) break;
    if (/^(?:[-*+] |\d+\. )/.test(line)) {
      current = { line: index + 1, lines: [line] };
      candidates.push(current);
    } else if (line.trim() === '' || /^[ \t]/.test(line)) {
      if (current) current.lines.push(line);
    } else {
      current = null;
    }
  }
  return { diagnostics: [], candidates };
}

function citations(candidate) {
  const text = candidate.lines.join('\n');
  const entries = [];
  // Only the BACKLOG word starts a chain. Unrelated backticks never become references.
  const chains = /(?<![A-Za-z0-9])BACKLOG\s*(`[^`\r\n]*`(?:\s*[·,/]\s*`[^`\r\n]*`)*)/g;
  for (const chain of text.matchAll(chains)) {
    const chainStart = chain.index + chain[0].indexOf('`');
    for (const span of chain[1].matchAll(/`([^`\r\n]*)`/g)) {
      const offset = chainStart + span.index;
      const line = candidate.line + text.slice(0, offset).split('\n').length - 1;
      entries.push({ id: span[1], line });
    }
  }
  return entries;
}

function hasExistingGoal(candidate, goalPath, isExistingGoal) {
  const currentPath = posix.normalize(goalPath);
  const text = candidate.lines.join('\n');
  for (const match of text.matchAll(/\[[^\]\r\n]*\]\(([^)\r\n]*)\)/g)) {
    const target = slashPath(match[1].split('#')[0]);
    if (/^https?:\/\//i.test(target) || posix.basename(target) !== 'goal.md') continue;
    const absolute = posix.isAbsolute(target) || /^[A-Za-z]:\//.test(target);
    const resolved = absolute ? posix.normalize(target) : posix.join(posix.dirname(currentPath), target);
    if (resolved !== currentPath && isExistingGoal(resolved)) return true;
  }
  return false;
}

function checkBacklog(entries, backlogPath) {
  const ids = new Set();
  const duplicates = new Set();
  const diagnostics = [];
  for (const { id, line } of entries) {
    if (id === '') {
      diagnostics.push(diagnostic('empty-backlog-id', backlogPath, line,
        'The BACKLOG row has an empty ID.',
        'Give this row a stable ID in its first cell.'));
      continue;
    }
    if (ids.has(id)) {
      duplicates.add(id);
      diagnostics.push(diagnostic('duplicate-backlog-id', backlogPath, line,
        `BACKLOG ID "${id}" is repeated.`,
        'Keep one row per stable ID; reconcile the duplicate rows and their source records.'));
    }
    ids.add(id);
  }
  return { ids, duplicateIds: duplicates.size, diagnostics };
}

export function checkCandidates({ backlogText, backlogPath, goalText, goalPath, isExistingGoal = () => false }) {
  const backlog = slashPath(backlogPath);
  const goal = slashPath(goalPath);
  const table = readBacklog(backlogText);
  const inputDiagnostics = [];
  if (!table.hasTable) {
    inputDiagnostics.push(diagnostic('no-id-table', backlog, null,
      'The BACKLOG has no table whose first header cell is ID.',
      'Use a table headed ID with a separator row immediately below its header.'));
  }
  const section = goalText == null ? null : readCandidateSection(goalText, goal);
  if (section) inputDiagnostics.push(...section.diagnostics);
  // Input errors suppress policy findings and counts from either file.
  if (inputDiagnostics.length > 0) return inputErrorResult(backlog, goal, inputDiagnostics);

  const checked = checkBacklog(table.entries, backlog);
  const diagnostics = checked.diagnostics;
  const counts = {
    backlogIds: table.entries.length,
    duplicateIds: checked.duplicateIds,
    candidates: section ? section.candidates.length : null,
    candidatesWithoutReference: section ? 0 : null,
    unknownIds: section ? 0 : null,
  };
  for (const candidate of section?.candidates ?? []) {
    const references = citations(candidate);
    if (references.length === 0 && !hasExistingGoal(candidate, goal, isExistingGoal)) {
      counts.candidatesWithoutReference += 1;
      diagnostics.push(diagnostic('missing-reference', goal, candidate.line,
        'The candidate has neither a BACKLOG ID citation nor an existing goal link.',
        'Add BACKLOG `<id>` or a link to an existing other goal.md within this candidate.'));
    }
    for (const { id, line } of references) {
      if (checked.ids.has(id)) continue;
      counts.unknownIds += 1;
      diagnostics.push(diagnostic('unknown-backlog-id', goal, line,
        `Cited BACKLOG ID "${id}" is absent from the BACKLOG ID tables.`,
        'Register the candidate under that stable ID in BACKLOG, or correct the citation to its existing ID.'));
    }
  }
  const status = diagnostics.length === 0 ? 'allowed' : 'policy-violation';
  return result(status, backlog, goal, counts, diagnostics);
}

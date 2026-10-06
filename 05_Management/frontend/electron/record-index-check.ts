import { lstat, readFile, readdir } from 'node:fs/promises';
import { dirname, isAbsolute, join, relative, resolve } from 'node:path';
import { parseBacklogTables } from './backlog-table.js';
import { catalogReferenceIssues, isRecordId, validateCatalog } from './catalog-contract.js';
import type { RecordCatalog, RecordSource } from './catalog-contract.js';
import { readCatalogFile } from './catalog-store.js';
import { sourcePathParts } from './source-section-contract.js';
import type { SourceSectionCode, SourceSectionFailure } from './source-section-contract.js';
import { createSourceSectionStore, inspectSourceFile } from './source-section-store.js';

export type RecordIndexDiagnosticCode =
  | 'CATALOG_INVALID' | 'REFERENCE_BROKEN' | 'LOCATOR_INVALID'
  | 'SOURCE_PATH_REJECTED' | 'SOURCE_NOT_READABLE' | 'SOURCE_MISSING'
  | 'SOURCE_TOO_LARGE' | 'SOURCE_INVALID_ENCODING' | 'SECTION_MISSING' | 'SECTION_AMBIGUOUS'
  | 'GOAL_NOT_INDEXED' | 'BACKLOG_ID_FORMAT' | 'BACKLOG_ID_DUPLICATE'
  | 'BACKLOG_GOAL_LINK_MISSING' | 'BACKLOG_PROMOTION_LINK_MISSING' | 'BACKLOG_TABLE_FORMAT';

export interface RecordIndexDiagnostic {
  severity: 'error' | 'warning';
  code: RecordIndexDiagnosticCode;
  location: string;
  cause: string;
  fix: string;
}

export interface RecordIndexCheckResult {
  diagnostics: RecordIndexDiagnostic[];
  groups: { group: 'index' | 'goals' | 'backlog'; ran: boolean }[];
  exitCode: 0 | 1 | 2;
}

const catalogPath = '05_Management/records/catalog.json';
const backlogPath = '00_Document/operations/BACKLOG.md';
const goalRoots = ['01_Phases/goals', '05_Management/goals'];
const sourceCodes: Partial<Record<SourceSectionCode, RecordIndexDiagnosticCode>> = {
  'path-rejected': 'SOURCE_PATH_REJECTED',
  'not-readable': 'SOURCE_NOT_READABLE',
  missing: 'SOURCE_MISSING',
  'too-large': 'SOURCE_TOO_LARGE',
  'invalid-encoding': 'SOURCE_INVALID_ENCODING',
  'section-missing': 'SECTION_MISSING',
  'section-ambiguous': 'SECTION_AMBIGUOUS',
};

function isMissing(error: unknown): boolean {
  return typeof error === 'object' && error !== null && 'code' in error && error.code === 'ENOENT';
}

export async function checkRecordIndex({ repositoryRoot }: { repositoryRoot: string }): Promise<RecordIndexCheckResult> {
  const diagnostics: RecordIndexDiagnostic[] = [];
  const groups: RecordIndexCheckResult['groups'] = [
    { group: 'index', ran: true },
    { group: 'goals', ran: true },
    { group: 'backlog', ran: true },
  ];
  function failed(group: 'index' | 'goals' | 'backlog') {
    const state = groups.find(item => item.group === group);
    if (state) state.ran = false;
  }
  function add(severity: RecordIndexDiagnostic['severity'], code: RecordIndexDiagnosticCode, location: string, cause: string, fix: string) {
    diagnostics.push({ severity, code, location, cause, fix });
  }

  let catalog: RecordCatalog | null = null;
  let indexAvailable = true;
  const file = await readCatalogFile(join(repositoryRoot, catalogPath));
  if (!file.ok) {
    if (file.code === 'invalid') {
      add('error', 'CATALOG_INVALID', 'catalog.json', file.message, '색인 파일을 2 MiB 이하의 schemaVersion 2 자료로 정리하세요.');
    } else {
      indexAvailable = false;
      failed('index');
    }
  } else {
    let value: unknown;
    try {
      value = JSON.parse(file.text);
    } catch {
      add('error', 'CATALOG_INVALID', 'catalog.json', '색인 파일이 유효한 JSON이 아닙니다.', 'catalog.json의 JSON 문법을 고치세요.');
    }
    if (value !== undefined) {
      const parsed = validateCatalog(value);
      if (!parsed.ok) {
        add('error', 'CATALOG_INVALID', `catalog.json:${parsed.location}`, '색인의 버전·필드·ID 규칙을 위반했습니다.', '허용 키와 schemaVersion 2 형식, 종류별로 유일한 ID를 사용하세요.');
      } else {
        catalog = parsed.catalog;
      }
    }
  }

  function sourceFailure(source: RecordSource, result: SourceSectionFailure) {
    // A transient read failure is inability to run, not evidence that a
    // repository policy was violated. Exit 2 takes precedence over errors.
    if (result.code === 'load' || result.code === 'changed') {
      failed('index');
      return;
    }
    const code = sourceCodes[result.code];
    if (!code) {
      failed('index');
      return;
    }
    const section = result.code.startsWith('section-') || result.reason === 'section';
    const key = section ? 'section' : 'locator';
    const cause = result.reason === null ? result.message : `${result.message} (${result.reason})`;
    add('error', code, `catalog.json:sources[${source.id}].${key}`, cause,
      section ? '이 파일의 유일한 정확한 제목을 지정하고 구간을 크기 상한 안으로 줄이세요.' : '출처의 저장소 상대경로·대소문자·파일 종류·내용과 크기를 확인하세요.');
  }

  if (catalog) {
    for (const issue of catalogReferenceIssues(catalog)) {
      add('error', 'REFERENCE_BROKEN', `catalog.json:${issue.owner}.${issue.key}`,
        `참조 ID ${issue.target}가 색인에 없습니다.`, '해당 종류의 등록된 ID로 참조를 고치세요.');
    }
    const sections = createSourceSectionStore({ repositoryRoot });
    for (const source of catalog.sources) {
      if (source.kind === 'local') {
        const path = sourcePathParts(source.locator);
        if (!path.ok || source.section !== null) {
          add('error', 'LOCATOR_INVALID', `catalog.json:sources[${source.id}].locator`,
            '로컬 출처의 상대경로 또는 null 구간 규칙을 위반했습니다.', '로컬 경로를 저장소 루트 기준 상대경로로 쓰고 section을 null로 두세요.');
        }
        continue;
      }
      if (source.kind === 'handoff') {
        if (!/^msg_[a-f0-9]{12}$/.test(source.locator) || source.section !== null) {
          add('error', 'LOCATOR_INVALID', `catalog.json:sources[${source.id}].locator`,
            '전달 메시지 ID 또는 null 구간 규칙을 위반했습니다.', 'locator에 msg_와 소문자 12자리 hex만 쓰고 section을 null로 두세요.');
        }
        continue;
      }
      if (source.locator.endsWith('.md')) {
        const result = await sections.read(source);
        if (!result.ok) sourceFailure(source, result);
      } else {
        const parts = sourcePathParts(source.locator);
        if (!parts.ok) {
          sourceFailure(source, parts);
          continue;
        }
        if (source.section !== null) {
          add('error', 'SOURCE_NOT_READABLE', `catalog.json:sources[${source.id}].section`,
            'Markdown이 아닌 출처에 section을 지정했습니다. (extension)', '소문자 .md 출처를 지정하거나 section을 null로 두세요.');
          continue;
        }
        const result = await inspectSourceFile({ repositoryRoot, locator: source.locator });
        if (!result.ok) sourceFailure(source, result);
      }
    }
  }

  if (!indexAvailable) {
    failed('goals');
  } else {
    const locators = catalog?.sources.filter(source => source.kind === 'git').map(source => source.locator) ?? [];
    const unindexed: string[] = [];
    try {
      for (const root of goalRoots) {
        const entries = await readdir(join(repositoryRoot, root), { withFileTypes: true });
        for (const entry of entries.sort((a, b) => a.name.localeCompare(b.name))) {
          if (!entry.isDirectory()) continue;
          const folder = `${root}/${entry.name}`;
          try {
            const goal = await lstat(join(repositoryRoot, folder, 'goal.md'));
            if (goal.isFile() && !locators.some(locator => locator.startsWith(`${folder}/`))) unindexed.push(folder);
          } catch (error) {
            if (!isMissing(error)) throw error;
          }
        }
      }
      for (const folder of unindexed) {
        add('warning', 'GOAL_NOT_INDEXED', folder, '이 goal 폴더 아래의 git 출처가 색인에 없습니다.', 'goal 폴더의 원문을 가리키는 git 출처를 색인에 추가하세요.');
      }
    } catch {
      failed('goals');
    }
  }

  try {
    const backlog = parseBacklogTables(await readFile(join(repositoryRoot, backlogPath), 'utf8'));
    for (const issue of backlog.formatIssues) {
      add('warning', 'BACKLOG_TABLE_FORMAT', `BACKLOG.md:${issue.line}`, issue.cause, 'ID 표의 머리글·구분 행·자료 행의 열 수를 맞추세요.');
    }
    const ids = new Set<string>();
    for (const row of backlog.rows) {
      const location = `BACKLOG.md:${row.line}`;
      if (!isRecordId(row.id)) {
        add('warning', 'BACKLOG_ID_FORMAT', location, '후보 ID가 소문자 단어를 하이픈으로 잇는 형식이 아닙니다.', '128자 이하의 안정적인 소문자 ID를 쓰세요.');
      }
      if (ids.has(row.id)) {
        add('warning', 'BACKLOG_ID_DUPLICATE', location, `후보 ID ${row.id}가 중복됩니다.`, '후보마다 서로 다른 안정적인 ID를 지정하세요.');
      }
      ids.add(row.id);
      if (row.status.includes('goal 승격') && row.goalLinks.length === 0) {
        add('warning', 'BACKLOG_PROMOTION_LINK_MISSING', location, 'goal 승격 행에 goal 상대 링크가 없습니다.', '승격한 goal의 원문 파일로 가는 상대 링크를 상태에 추가하세요.');
      }
      for (const link of row.goalLinks) {
        let exists = false;
        try {
          const path = resolve(repositoryRoot, dirname(backlogPath), decodeURIComponent(link));
          const within = relative(resolve(repositoryRoot), path);
          if (!isAbsolute(within) && within !== '..' && !within.startsWith(`..${process.platform === 'win32' ? '\\' : '/'}`)) {
            exists = (await lstat(path)).isFile();
          }
        } catch (error) {
          if (!isMissing(error) && !(error instanceof URIError)) throw error;
        }
        if (!exists) {
          add('warning', 'BACKLOG_GOAL_LINK_MISSING', location, '상대 goal 링크의 대상 파일이 없습니다.', 'BACKLOG.md 기준 상대경로와 대상 goal 파일의 존재를 확인하세요.');
        }
      }
    }
  } catch {
    failed('backlog');
  }

  const couldNotRun = groups.some(group => !group.ran);
  const hasErrors = diagnostics.some(item => item.severity === 'error');
  return { diagnostics, groups, exitCode: couldNotRun ? 2 : hasErrors ? 1 : 0 };
}

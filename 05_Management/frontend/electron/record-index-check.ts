import { lstat, readdir } from 'node:fs/promises';
import { join } from 'node:path';
import type { BacklogIssueCode } from './backlog-contract.js';
import { createBacklogStore } from './backlog-store.js';
import { catalogReferenceIssues, validateCatalog } from './catalog-contract.js';
import type { RecordCatalog, RecordSource } from './catalog-contract.js';
import { readCatalogFile } from './catalog-store.js';
import { sourcePathParts, sourceReadability } from './source-section-contract.js';
import type { SourceSectionCode, SourceSectionFailure } from './source-section-contract.js';
import { createSourceSectionStore, inspectSourceFile } from './source-section-store.js';

export type RecordIndexDiagnosticCode =
  | 'CATALOG_INVALID' | 'REFERENCE_BROKEN' | 'LOCATOR_INVALID'
  | 'SOURCE_PATH_REJECTED' | 'SOURCE_NOT_READABLE' | 'SOURCE_MISSING'
  | 'SOURCE_TOO_LARGE' | 'SOURCE_INVALID_ENCODING' | 'SECTION_MISSING' | 'SECTION_AMBIGUOUS'
  | 'GOAL_NOT_INDEXED' | BacklogIssueCode;

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
      if (sourceReadability(source) === null) {
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
    const backlog = await createBacklogStore({ repositoryRoot }).read();
    if (!backlog.ok) {
      failed('backlog');
    } else {
      for (const issue of backlog.issues) {
        const location = issue.line === null ? 'BACKLOG.md' : `BACKLOG.md:${issue.line}`;
        add('warning', issue.code, location, issue.cause, issue.fix);
      }
      if (backlog.uncheckedLinks.length > 0) failed('backlog');
    }
  } catch {
    failed('backlog');
  }

  const couldNotRun = groups.some(group => !group.ran);
  const hasErrors = diagnostics.some(item => item.severity === 'error');
  return { diagnostics, groups, exitCode: couldNotRun ? 2 : hasErrors ? 1 : 0 };
}

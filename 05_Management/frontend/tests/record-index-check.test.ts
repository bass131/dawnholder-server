// @vitest-environment node
// Requirement: goal 「만들 것」 5 and 완료조건 1, index-v2-design.md 「색인 검사」 (fixed codes,
// three groups with their own ran state, exit 2 > 1 > 0, location·cause·fix) and the harness
// principles 1·3·4 (a check that could not run is never reported as a violation or a pass).
// checkRecordIndex runs on owned TEMP repositories laid out like this repository. The CLI part runs
// the real script from 05_Management/frontend on the real repository (read only).
import { spawnSync } from 'node:child_process';
import { readFileSync, rmSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { checkRecordIndex } from '../electron/record-index-check';
import {
  catalogText, gitSource, handoffSource, localSource, recordFixture, systemFixture, type CatalogFixture,
} from './record-sources/catalog-v2-fixture';
import { createOwnedTempRepository, type OwnedTempRepository } from './record-sources/owned-temp-repository';

const CATALOG = '05_Management/records/catalog.json';
const BACKLOG = '00_Document/operations/BACKLOG.md';
const GOAL_A = '01_Phases/goals/2026-10-01-indexed';
const GOAL_B = '05_Management/goals/2026-10-02-indexed-too';
const GOAL_TEXT = '# 목표\n\n## 결과와 열린 사항\n결과\n';
const BACKLOG_HEADER = [
  '# 목표 전 후보',
  '',
  '| ID | 제목 | 이유 | 출처 | 선행 조건 | 담당 후보 | 상태 |',
  '|---|---|---|---|---|---|---|',
];
const CLEAN_ROWS = [
  '| `clean-candidate` | 깨끗한 후보 | 이유 | 메인 전달 | 없음 | Management | 대기 |',
  `| \`promoted-candidate\` | 승격 후보 | 이유 | 메인 전달 | 없음 | Management | goal 승격 → [색인된 goal](../../${GOAL_A}/goal.md) |`,
];

function cleanCatalog(): CatalogFixture {
  return {
    schemaVersion: 2,
    sources: [
      gitSource('guide-overview', 'docs/guide.md', '개요'),
      gitSource('program-code', 'src/Program.cs', null),
      gitSource('indexed-goal', `${GOAL_A}/goal.md`, '결과와 열린 사항'),
      gitSource('indexed-too-goal', `${GOAL_B}/goal.md`, '결과와 열린 사항'),
      // The local file does not exist in this checkout; the check must not look for it.
      localSource('local-run-log', '.backups/run/log.md'),
      handoffSource('main-decision', 'msg_0123456789ab'),
    ],
    systems: [systemFixture({ id: 'records-view', sourceIds: ['guide-overview', 'program-code'], recordIds: ['indexed'] })],
    records: [
      recordFixture({ id: 'indexed', systemIds: ['records-view'], sourceIds: ['indexed-goal', 'local-run-log', 'main-decision'] }),
      recordFixture({ id: 'indexed-too', type: '결정', sourceIds: ['indexed-too-goal'] }),
    ],
  };
}

let repository: OwnedTempRepository;
beforeEach(() => {
  repository = createOwnedTempRepository();
  repository.write('docs/guide.md', '# 안내\n\n## 개요\n본문\n');
  repository.write('src/Program.cs', 'class Program {}\n');
  repository.write(`${GOAL_A}/goal.md`, GOAL_TEXT);
  repository.write(`${GOAL_B}/goal.md`, GOAL_TEXT);
  writeCatalog(cleanCatalog());
  writeBacklog(CLEAN_ROWS);
});
afterEach(() => repository.remove());

function writeCatalog(catalog: unknown) {
  repository.write(CATALOG, typeof catalog === 'string' ? catalog : catalogText(catalog));
}
// Returns the 1-based line number of each given row in the written BACKLOG.md.
function writeBacklog(rows: string[]): number[] {
  const lines = [...BACKLOG_HEADER, ...rows, ''];
  repository.write(BACKLOG, lines.join('\n'));
  return rows.map(row => lines.indexOf(row) + 1);
}
function withCatalog(change: (catalog: CatalogFixture) => void) {
  const catalog = cleanCatalog();
  change(catalog);
  writeCatalog(catalog);
}

const check = () => checkRecordIndex({ repositoryRoot: repository.root });
const codes = (result: Awaited<ReturnType<typeof check>>) => result.diagnostics.map(item => `${item.severity} ${item.code}`);
function only(result: Awaited<ReturnType<typeof check>>, code: string) {
  const found = result.diagnostics.filter(item => item.code === code);
  expect(found, `diagnostics: ${codes(result).join(', ')}`).toHaveLength(1);
  return found[0]!;
}
function groupStates(result: Awaited<ReturnType<typeof check>>) {
  return Object.fromEntries(result.groups.map(item => [item.group, item.ran]));
}

describe('a clean repository', () => {
  it('reports no diagnostics, all three groups ran and exit code 0, without looking for local-only files', async () => {
    const result = await check();
    expect(result.diagnostics).toEqual([]);
    expect(groupStates(result)).toEqual({ index: true, goals: true, backlog: true });
    expect(result.exitCode).toBe(0);
  });
});

describe('index errors (broken links fail)', () => {
  it('CATALOG_INVALID for malformed JSON, pointing at catalog.json', async () => {
    writeCatalog('{');
    const result = await check();
    const diagnostic = only(result, 'CATALOG_INVALID');
    expect(diagnostic).toMatchObject({ severity: 'error' });
    expect(diagnostic.location).toContain('catalog.json');
    expect(groupStates(result).index).toBe(true);
    expect(result.exitCode).toBe(1);
  });

  it('CATALOG_INVALID for a key outside the fixed set, located by the object ID and the key', async () => {
    withCatalog(catalog => { Object.assign(catalog.systems[0]!, { summary: '다시 들어온 서술' }); });
    const diagnostic = only(await check(), 'CATALOG_INVALID');
    expect(diagnostic.location).toContain('catalog.json');
    expect(diagnostic.location).toContain('records-view');
    expect(diagnostic.location).toContain('summary');
  });

  it('CATALOG_INVALID for a duplicate ID, located by that ID', async () => {
    withCatalog(catalog => { catalog.records.push(recordFixture({ id: 'indexed-too' })); });
    const diagnostic = only(await check(), 'CATALOG_INVALID');
    expect(diagnostic.location).toContain('indexed-too');
  });

  it('REFERENCE_BROKEN for a reference to a missing ID, located by the referring object', async () => {
    withCatalog(catalog => { catalog.records[1]!.sourceIds.push('no-such-source'); });
    const result = await check();
    const diagnostic = only(result, 'REFERENCE_BROKEN');
    expect(diagnostic).toMatchObject({ severity: 'error' });
    expect(diagnostic.location).toContain('indexed-too');
    expect(result.exitCode).toBe(1);
  });

  it('LOCATOR_INVALID for a local locator that is not repository-relative and a handoff that is not a message ID', async () => {
    withCatalog(catalog => {
      catalog.sources.push(localSource('absolute-local', 'C:/Dev/DawnHolder_Project/.backups/a.md'), handoffSource('bad-handoff', 'message:msg_0123456789ab'));
    });
    const result = await check();
    const found = result.diagnostics.filter(item => item.code === 'LOCATOR_INVALID');
    expect(found.map(item => item.severity)).toEqual(['error', 'error']);
    expect(found.some(item => item.location.includes('absolute-local'))).toBe(true);
    expect(found.some(item => item.location.includes('bad-handoff'))).toBe(true);
  });

  it('SOURCE_PATH_REJECTED names the reader reason in its cause', async () => {
    withCatalog(catalog => { catalog.sources.push(gitSource('parent-path', '../outside.md', null), gitSource('drive-path', 'C:/outside.md', null)); });
    const found = (await check()).diagnostics.filter(item => item.code === 'SOURCE_PATH_REJECTED');
    const byId = Object.fromEntries(found.map(item => [item.location.includes('parent-path') ? 'parent' : 'drive', item.cause]));
    expect(found).toHaveLength(2);
    expect(byId.parent).toContain('parent');
    expect(byId.drive).toContain('drive');
  });

  it('SOURCE_NOT_READABLE for a section on a non-Markdown git source and for a directory named .md', async () => {
    repository.mkdir('docs/folder.md');
    withCatalog(catalog => {
      catalog.sources.push(gitSource('code-with-section', 'src/Program.cs', 'Main'), gitSource('folder-source', 'docs/folder.md', null));
    });
    const found = (await check()).diagnostics.filter(item => item.code === 'SOURCE_NOT_READABLE');
    expect(found.map(item => item.severity)).toEqual(['error', 'error']);
    expect(found.some(item => item.location.includes('code-with-section'))).toBe(true);
    expect(found.some(item => item.location.includes('folder-source'))).toBe(true);
  });

  it('SOURCE_MISSING for a git source whose file is absent, Markdown or not', async () => {
    withCatalog(catalog => { catalog.sources.push(gitSource('absent-md', 'docs/none.md', null), gitSource('absent-code', 'src/None.cs', null)); });
    const found = (await check()).diagnostics.filter(item => item.code === 'SOURCE_MISSING');
    expect(found).toHaveLength(2);
  });

  it('SOURCE_TOO_LARGE for a file over 1 MiB and for a section over 256 KiB', async () => {
    repository.write('docs/big-file.md', `# 큰 파일\n${'y'.repeat(1024 * 1024)}\n`);
    repository.write('docs/big-section.md', `# 큰 구간\n${'x'.repeat(256 * 1024)}\n# 다음\n`);
    withCatalog(catalog => { catalog.sources.push(gitSource('big-file', 'docs/big-file.md', null), gitSource('big-section', 'docs/big-section.md', '큰 구간')); });
    const found = (await check()).diagnostics.filter(item => item.code === 'SOURCE_TOO_LARGE');
    expect(found).toHaveLength(2);
  });

  it('SOURCE_INVALID_ENCODING for bytes that are not strict UTF-8', async () => {
    repository.write('docs/bad.md', new Uint8Array([0x23, 0x20, 0x41, 0x0a, 0xff, 0xfe, 0x0a]));
    withCatalog(catalog => { catalog.sources.push(gitSource('bad-encoding', 'docs/bad.md', null)); });
    expect(only(await check(), 'SOURCE_INVALID_ENCODING').location).toContain('bad-encoding');
  });

  it('SECTION_MISSING and SECTION_AMBIGUOUS for headings that are absent or repeated', async () => {
    repository.write('docs/twice.md', '## 같은 제목\n가\n## 같은 제목\n나\n');
    withCatalog(catalog => { catalog.sources.push(gitSource('no-heading', 'docs/guide.md', '없는 제목'), gitSource('twice', 'docs/twice.md', '같은 제목')); });
    const result = await check();
    expect(only(result, 'SECTION_MISSING').location).toContain('no-heading');
    expect(only(result, 'SECTION_AMBIGUOUS').location).toContain('twice');
    expect(result.exitCode).toBe(1);
  });
});

describe('goal warnings (pilot)', () => {
  it('GOAL_NOT_INDEXED for a goal.md folder that no git locator starts with, located by the goal folder', async () => {
    repository.write('05_Management/goals/2026-10-03-unindexed/goal.md', GOAL_TEXT);
    repository.write('01_Phases/goals/2026-10-04-only-local/goal.md', GOAL_TEXT);
    repository.mkdir('01_Phases/goals/2026-10-05-no-goal-file');
    withCatalog(catalog => { catalog.sources.push(localSource('only-local', '01_Phases/goals/2026-10-04-only-local/goal.md')); });
    const result = await check();
    const found = result.diagnostics.filter(item => item.code === 'GOAL_NOT_INDEXED');
    expect(found.map(item => item.severity)).toEqual(['warning', 'warning']);
    expect(found.map(item => item.location).sort()).toEqual([
      expect.stringContaining('01_Phases/goals/2026-10-04-only-local'),
      expect.stringContaining('05_Management/goals/2026-10-03-unindexed'),
    ]);
    expect(result.exitCode).toBe(0);
  });

  it('counts any git locator under the goal folder, not only goal.md', async () => {
    repository.write('01_Phases/goals/2026-10-06-design-only/goal.md', GOAL_TEXT);
    repository.write('01_Phases/goals/2026-10-06-design-only/design.md', '# 설계\n');
    withCatalog(catalog => { catalog.sources.push(gitSource('design-only', '01_Phases/goals/2026-10-06-design-only/design.md', null)); });
    expect((await check()).diagnostics.filter(item => item.code === 'GOAL_NOT_INDEXED')).toEqual([]);
  });
});

describe('backlog warnings (pilot)', () => {
  it('reports ID shape, duplicate, missing goal link target, promotion without link and column count, located by BACKLOG.md line', async () => {
    const rows = [
      ...CLEAN_ROWS,
      '| `Bad_ID` | 모양 | 이유 | 출처 | 없음 | Rules | 대기 |',
      '| `dup-id` | 첫째 | 이유 | 출처 | 없음 | Rules | 대기 |',
      '| `dup-id` | 둘째 | 이유 | 출처 | 없음 | Rules | 대기 |',
      '| `dead-link` | 끊긴 링크 | 이유 | 출처 | 없음 | Rules | goal 승격 → [없는 goal](../../01_Phases/goals/2026-10-09-none/goal.md) |',
      '| `no-link` | 링크 없음 | 이유 | 출처 | 없음 | Rules | goal 승격 |',
      '| `short-row` | 열 부족 | 이유 | 출처 | 없음 | Rules |',
    ];
    const lines = writeBacklog(rows);
    const at = (row: number) => `BACKLOG.md:${lines[row]}`;
    const result = await check();
    const byCode = (code: string) => result.diagnostics.filter(item => item.code === code);
    expect(byCode('BACKLOG_ID_FORMAT').map(item => item.location)).toEqual([expect.stringContaining(at(2))]);
    expect(byCode('BACKLOG_ID_DUPLICATE').some(item => item.location.includes(at(4)) || item.location.includes(at(3)))).toBe(true);
    expect(byCode('BACKLOG_GOAL_LINK_MISSING').map(item => item.location)).toEqual([expect.stringContaining(at(5))]);
    expect(byCode('BACKLOG_PROMOTION_LINK_MISSING').map(item => item.location)).toEqual([expect.stringContaining(at(6))]);
    expect(byCode('BACKLOG_TABLE_FORMAT').map(item => item.location)).toEqual([expect.stringContaining(at(7))]);
    expect(result.diagnostics.filter(item => item.code.startsWith('BACKLOG_')).every(item => item.severity === 'warning')).toBe(true);
    expect(result.exitCode).toBe(0);
  });
});

describe('a group that could not run', () => {
  it('a missing index file marks the index group not run with exit code 2 and no policy diagnostic', async () => {
    rmSync(repository.path(CATALOG));
    const result = await check();
    // Whether the goals group can run without index locators is not fixed by the design.
    expect(groupStates(result)).toMatchObject({ index: false, backlog: true });
    expect(result.exitCode).toBe(2);
    expect(result.diagnostics.filter(item => item.severity === 'error')).toEqual([]);
  });

  it('a missing BACKLOG.md marks the backlog group not run with exit code 2, even with index errors', async () => {
    rmSync(repository.path(BACKLOG));
    withCatalog(catalog => { catalog.sources.push(gitSource('absent-md', 'docs/none.md', null)); });
    const result = await check();
    expect(groupStates(result)).toEqual({ index: true, goals: true, backlog: false });
    expect(result.diagnostics.some(item => item.code === 'SOURCE_MISSING')).toBe(true);
    expect(result.exitCode).toBe(2);
  });

  it('a goals folder that cannot be listed marks the goals group not run with exit code 2', async () => {
    rmSync(repository.path('05_Management/goals'), { recursive: true });
    repository.write('05_Management/goals', 'not a directory');
    const result = await check();
    expect(groupStates(result).goals).toBe(false);
    expect(result.exitCode).toBe(2);
  });
});

describe('diagnostic form', () => {
  it('gives every diagnostic a known severity, a code and non-empty location, cause and fix', async () => {
    repository.write('05_Management/goals/2026-10-03-unindexed/goal.md', GOAL_TEXT);
    writeBacklog([...CLEAN_ROWS, '| `Bad_ID` | 모양 | 이유 | 출처 | 없음 | Rules | 대기 |']);
    withCatalog(catalog => {
      catalog.records[1]!.sourceIds.push('no-such-source');
      catalog.sources.push(gitSource('absent-md', 'docs/none.md', null));
    });
    const result = await check();
    expect(result.diagnostics.length).toBeGreaterThanOrEqual(4);
    for (const diagnostic of result.diagnostics) {
      const shape = {
        severity: ['error', 'warning'].includes(diagnostic.severity),
        code: /^[A-Z][A-Z_]+$/.test(diagnostic.code),
        location: diagnostic.location.trim().length > 0,
        cause: diagnostic.cause.trim().length > 0,
        fix: diagnostic.fix.trim().length > 0,
      };
      expect(shape, diagnostic.code).toEqual({ severity: true, code: true, location: true, cause: true, fix: true });
    }
  });
});

// Requirement: backlog-menu-design.md 「색인 검사」 and 「두 단계」 behaviour rows: the check moves the
// shared store's issues over as warnings (a null line is located at BACKLOG.md alone) and a BACKLOG.md
// the store cannot read leaves the backlog group not run instead of becoming a diagnostic. The
// REJECTED, null-line, over-limit and encoding cases fail until the behaviour step; the missing case
// already holds. Sentences are copied from the design tables.
describe('backlog group through the shared backlog store', () => {
  const REJECTED_FIX = 'BACKLOG.md 기준 상대경로로 저장소 안의 goal 파일을 대소문자까지 같게 가리키고 링크·junction을 거치지 마세요.';
  const backlogDiagnostics = (result: Awaited<ReturnType<typeof check>>) => result.diagnostics.filter(item => item.code.startsWith('BACKLOG_'));

  it('reports BACKLOG_GOAL_LINK_REJECTED as a warning at BACKLOG.md:<line> with the reason in its cause', async () => {
    const outsideRow = `| \`outside-link\` | 밖 링크 | 이유 | 출처 | 없음 | Rules | goal 승격 → [밖](../../../${GOAL_A}/goal.md) |`;
    const lines = writeBacklog([...CLEAN_ROWS, outsideRow]);
    const result = await check();
    expect(backlogDiagnostics(result)).toEqual([{
      severity: 'warning',
      code: 'BACKLOG_GOAL_LINK_REJECTED',
      location: `BACKLOG.md:${lines[2]}`,
      cause: '상대 goal 링크가 저장소 경로 규칙에 맞지 않습니다 (parent).',
      fix: REJECTED_FIX,
    }]);
    expect(groupStates(result)).toEqual({ index: true, goals: true, backlog: true });
    expect(result.exitCode).toBe(0);
  });

  it('locates a missing ID table at BACKLOG.md without a line number', async () => {
    repository.write(BACKLOG, '# 목표 전 후보\n\n아직 후보 표가 없다.\n');
    const result = await check();
    expect(backlogDiagnostics(result)).toEqual([{
      severity: 'warning',
      code: 'BACKLOG_TABLE_FORMAT',
      location: 'BACKLOG.md',
      cause: '첫 열이 ID인 후보 표가 없습니다.',
      fix: '머리글 첫 열이 ID인 후보 표와 구분 행을 두세요.',
    }]);
    expect(result.exitCode).toBe(0);
  });

  // The readable text holds a row that would be BACKLOG_ID_FORMAT, so a partial read would show.
  const readableText = `${[...BACKLOG_HEADER, ...CLEAN_ROWS, '| `Bad_ID` | 모양 | 이유 | 출처 | 없음 | Rules | 대기 |'].join('\n')}\n`;
  const unreadable = [
    { state: 'missing', prepare: () => rmSync(repository.path(BACKLOG)) },
    { state: 'over 1 MiB', prepare: () => repository.write(BACKLOG, `${readableText}${'y'.repeat(1024 * 1024)}\n`) },
    { state: 'not strict UTF-8', prepare: () => repository.write(BACKLOG, Buffer.concat([Buffer.from(readableText), Buffer.from([0xff, 0xfe, 0x0a])])) },
  ];
  it.each(unreadable)('marks the backlog group not run with exit code 2 and no BACKLOG_ diagnostic when BACKLOG.md is $state', async ({ prepare }) => {
    prepare();
    const result = await check();
    expect(groupStates(result)).toEqual({ index: true, goals: true, backlog: false });
    expect(backlogDiagnostics(result)).toEqual([]);
    expect(result.exitCode).toBe(2);
  });
});

describe('records:check CLI on this repository', () => {
  const frontend = fileURLToPath(new URL('../', import.meta.url));

  it('is the npm records:check script', () => {
    const pkg = JSON.parse(readFileSync(new URL('../package.json', import.meta.url), 'utf8')) as { scripts: Record<string, string> };
    expect(pkg.scripts['records:check'] ?? '(no records:check script)').toContain('scripts/check-record-index.mjs');
  });

  it('exits 0 with diagnostic lines and a final summary line (needs the v2 data conversion of step 3)', () => {
    const run = spawnSync(process.execPath, ['scripts/check-record-index.mjs'], { cwd: frontend, encoding: 'utf8', timeout: 120_000 });
    const lines = (run.stdout ?? '').split(/\r?\n/).filter(line => line.trim());
    const summary = lines.at(-1) ?? '';
    const diagnosticLines = lines.slice(0, -1);
    expect({ status: run.status, stderr: run.stderr }).toMatchObject({ status: 0 });
    expect(summary).toMatch(/^records:check index=ran goals=ran backlog=ran errors=0 warnings=\d+$/);
    for (const line of diagnosticLines) expect(line).toMatch(/^warning [A-Z_]+ \S.* — .+ — 고치는 방법: .+$/);
  }, 120_000);
});

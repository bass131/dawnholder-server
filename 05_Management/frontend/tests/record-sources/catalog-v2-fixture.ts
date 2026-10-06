// Record index v2 fixture builders (design index-v2-design.md 「색인 형식」).
// Plain literals on purpose: tests describe the file format themselves and never build it with
// product types or functions. tests/mcp-fixtures.ts is the v1 MCP fixture owned by the MCP step.

export interface SourceFixture {
  id: string;
  title: string;
  kind: 'git' | 'local' | 'handoff';
  locator: string;
  section: string | null;
  availability: 'versioned' | 'local-only';
}

export interface SystemFixture {
  id: string;
  title: string;
  area: string;
  sourceIds: string[];
  relatedSystemIds: string[];
  recordIds: string[];
}

export interface PullRequestFixture {
  number: number;
  mergeCommit: string;
}

export interface RecordFixture {
  id: string;
  type: '변경' | '결정' | '검증' | '계획';
  title: string;
  systemIds: string[];
  sourceIds: string[];
  pullRequests: PullRequestFixture[];
}

export interface CatalogFixture {
  schemaVersion: 2;
  sources: SourceFixture[];
  systems: SystemFixture[];
  records: RecordFixture[];
}

export const MERGE_COMMIT = 'a47a02765c87d9794933f461a9c71ac7d4369d51';

export function gitSource(id: string, locator: string, section: string | null, title = `${id} 문서`): SourceFixture {
  return { id, title, kind: 'git', locator, section, availability: 'versioned' };
}

export function localSource(id: string, locator: string, title = `${id} 로컬 기록`): SourceFixture {
  return { id, title, kind: 'local', locator, section: null, availability: 'local-only' };
}

export function handoffSource(id: string, locator: string, title = `${id} 전달 메시지`): SourceFixture {
  return { id, title, kind: 'handoff', locator, section: null, availability: 'local-only' };
}

export function systemFixture(overrides: Partial<SystemFixture> & Pick<SystemFixture, 'id'>): SystemFixture {
  return { title: `${overrides.id} 시스템`, area: 'Management', sourceIds: [], relatedSystemIds: [], recordIds: [], ...overrides };
}

export function recordFixture(overrides: Partial<RecordFixture> & Pick<RecordFixture, 'id'>): RecordFixture {
  return { type: '변경', title: `${overrides.id} 기록`, systemIds: [], sourceIds: [], pullRequests: [], ...overrides };
}

// One system and one record linked both ways, three sources of every kind.
export function linkedCatalog(): CatalogFixture {
  return {
    schemaVersion: 2,
    sources: [
      gitSource('guide-overview', 'docs/guide.md', '개요', '안내 문서 개요'),
      localSource('local-run-log', '.backups/run/log.md', '로컬 실행 기록'),
      handoffSource('main-decision', 'msg_0123456789ab', '메인 결정 메시지'),
    ],
    systems: [systemFixture({ id: 'records-view', title: '기록 보기', sourceIds: ['guide-overview'], recordIds: ['index-change'] })],
    records: [recordFixture({
      id: 'index-change',
      title: '색인 형식 전환',
      systemIds: ['records-view'],
      sourceIds: ['guide-overview', 'local-run-log', 'main-decision'],
      pullRequests: [{ number: 196, mergeCommit: MERGE_COMMIT }],
    })],
  };
}

// The screen tests' index: readable Markdown with and without a section, non-Markdown git, local
// and handoff sources; systems in two areas linked both ways; records with zero, one or two PRs.
export const SCREEN_COMMIT_A = 'c27b03e888986f2ec8c593cd6c626a9c515595e1';
export const SCREEN_COMMIT_B = '0bd4f1e2a3c4d5e6f708192a3b4c5d6e7f809123';
export function screenCatalog(): CatalogFixture {
  return {
    schemaVersion: 2,
    sources: [
      gitSource('combat-guide', 'docs/combat.md', '전투 흐름', '전투 흐름 안내'),
      gitSource('combat-code', '02_Server/GameServer/Maps/GameMap.cs', null, '전투 맵 코드'),
      gitSource('records-design', 'docs/records.md', null, '기록 설계 전체'),
      localSource('records-run-log', '.backups/run/log.md', '로컬 실행 기록'),
      handoffSource('records-decision', 'msg_0123456789ab', '메인 결정 메시지'),
    ],
    systems: [
      systemFixture({
        id: 'combat', title: '전투·피해·사망 처리', area: '게임 플레이',
        sourceIds: ['combat-guide', 'combat-code'], relatedSystemIds: ['management-records'], recordIds: ['combat-change'],
      }),
      systemFixture({
        id: 'management-records', title: '개발 기록 열람', area: 'Management',
        sourceIds: ['records-design', 'records-run-log', 'records-decision'], relatedSystemIds: ['combat'], recordIds: ['index-change', 'index-plan'],
      }),
      systemFixture({ id: 'persistence', title: 'DB·캐릭터 보관과 복원', area: '서버', sourceIds: ['combat-guide'] }),
    ],
    records: [
      recordFixture({ id: 'combat-change', title: '즉시 피해 처리 통합', systemIds: ['combat'], sourceIds: ['combat-guide'], pullRequests: [{ number: 140, mergeCommit: SCREEN_COMMIT_A }] }),
      recordFixture({
        id: 'index-change', title: '색인 형식 전환', systemIds: ['management-records'], sourceIds: ['records-design', 'records-decision'],
        pullRequests: [{ number: 196, mergeCommit: MERGE_COMMIT }, { number: 197, mergeCommit: SCREEN_COMMIT_B }],
      }),
      recordFixture({ id: 'index-plan', type: '계획', title: '백로그 메뉴 계획', systemIds: ['management-records'], sourceIds: ['records-run-log'] }),
    ],
  };
}

export function catalogText(catalog: unknown): string {
  return `${JSON.stringify(catalog, null, 2)}\n`;
}

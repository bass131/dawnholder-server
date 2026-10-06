// @vitest-environment node
// Requirement: goal 완료조건 1 (no narrative or status field in the catalog, the index check ends
// with zero broken links, goals merged since 9-30 are in the index) and index-v2-design.md
// 「데이터 전환」. These read the real 05_Management/records/catalog.json and are expected to fail
// until the step-3 data conversion. The goal list and system IDs are fixed values: the 42 goal
// folders are E/pr2-goals-since-0930-at-a47a0276.txt (SHA-256 185fa722…, from
// `git ls-tree -r --name-only a47a0276 -- 01_Phases/goals 05_Management/goals`), and the 18 system
// IDs are those of catalog.json at a47a0276. Neither is recomputed from the current tree.
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { describe, expect, it } from 'vitest';
import { readCatalog } from '../electron/catalog-contract';
import { checkRecordIndex } from '../electron/record-index-check';

const GOALS_SINCE_0930 = [
  '01_Phases/goals/2026-09-30-contracts-baseline',
  '01_Phases/goals/2026-09-30-generator-exit-and-rollout-report',
  '01_Phases/goals/2026-09-30-immediate-enemy-hit',
  '01_Phases/goals/2026-09-30-maintainability-standards',
  '01_Phases/goals/2026-09-30-party-invite-command',
  '01_Phases/goals/2026-09-30-refactor-party-quest',
  '01_Phases/goals/2026-09-30-refactor-player-state',
  '01_Phases/goals/2026-09-30-remote-interpolation',
  '01_Phases/goals/2026-09-30-server-packet-publication',
  '01_Phases/goals/2026-09-30-session-disconnect-cleanup',
  '01_Phases/goals/2026-10-01-hierarchical-routing-pilot',
  '01_Phases/goals/2026-10-01-native-component-null-audit',
  '01_Phases/goals/2026-10-01-operations-rules',
  '01_Phases/goals/2026-10-01-persistence-technical-design',
  '01_Phases/goals/2026-10-01-readability-baseline',
  '01_Phases/goals/2026-10-01-readability-format-ci',
  '01_Phases/goals/2026-10-01-refactor-record-corrections',
  '01_Phases/goals/2026-10-02-agent-rule-context',
  '01_Phases/goals/2026-10-02-architecture-extractor-comparison',
  '01_Phases/goals/2026-10-02-architecture-part-rules',
  '01_Phases/goals/2026-10-02-persistence-repository',
  '01_Phases/goals/2026-10-03-codegraph-adapter-cleanup',
  '01_Phases/goals/2026-10-03-harness-principles',
  '01_Phases/goals/2026-10-04-persistence-integration',
  '01_Phases/goals/2026-10-04-teammate-onboarding',
  '01_Phases/goals/2026-10-05-architecture-tests-ci',
  '01_Phases/goals/2026-10-05-ci-warning-operating-followup',
  '01_Phases/goals/2026-10-05-items-inventory-currency',
  '01_Phases/goals/2026-10-05-module-boundary-warning',
  '01_Phases/goals/2026-10-05-operating-canon',
  '05_Management/goals/2026-09-30-console-refinement',
  '05_Management/goals/2026-09-30-desktop-shell',
  '05_Management/goals/2026-09-30-foundation',
  '05_Management/goals/2026-09-30-launcher',
  '05_Management/goals/2026-09-30-system-records',
  '05_Management/goals/2026-10-01-context-corrections',
  '05_Management/goals/2026-10-01-routing-adoption',
  '05_Management/goals/2026-10-01-session-closeout',
  '05_Management/goals/2026-10-01-shared-read-mcp',
  '05_Management/goals/2026-10-02-system-cards',
  '05_Management/goals/2026-10-05-development-record-navigation',
  '05_Management/goals/2026-10-06-record-source-unification',
];

const SYSTEM_IDS_AT_BASE = [
  'connection', 'movement', 'combat', 'skills', 'enemy-ai', 'character-state', 'map-entry', 'party', 'quest',
  'remote-rendering', 'packet-publication', 'transport', 'protocol', 'persistence', 'engineering',
  'management-desktop', 'management-operations', 'management-records',
];

// The fixed key sets of design 「색인 형식」, checked on the raw JSON independently of readCatalog.
const ALLOWED_KEYS = {
  catalog: ['records', 'schemaVersion', 'sources', 'systems'],
  source: ['availability', 'id', 'kind', 'locator', 'section', 'title'],
  system: ['area', 'id', 'recordIds', 'relatedSystemIds', 'sourceIds', 'title'],
  record: ['id', 'pullRequests', 'sourceIds', 'systemIds', 'title', 'type'],
};

interface RawSource { id: string; kind: string; locator: string }
interface RawCatalog { sources: RawSource[]; systems: { id: string; sourceIds: string[] }[]; records: object[] }

const repositoryRoot = fileURLToPath(new URL('../../../', import.meta.url));
const raw = JSON.parse(readFileSync(new URL('../../records/catalog.json', import.meta.url), 'utf8')) as RawCatalog & Record<string, unknown>;
const guide = JSON.parse(readFileSync(new URL('../../records/system-guide.json', import.meta.url), 'utf8')) as { cards: { id: string; relatedSystemIds: string[] }[] };

describe('the real record index after the data conversion', () => {
  it('is accepted by readCatalog', () => {
    expect(readCatalog(raw)).not.toBeNull();
  });

  it('has no key outside the fixed sets, so no narrative or status field remains', () => {
    const extra = [
      ...Object.keys(raw).filter(key => !ALLOWED_KEYS.catalog.includes(key)).map(key => `/${key}`),
      ...raw.sources.flatMap(item => Object.keys(item).filter(key => !ALLOWED_KEYS.source.includes(key)).map(key => `source ${item.id}/${key}`)),
      ...raw.systems.flatMap(item => Object.keys(item).filter(key => !ALLOWED_KEYS.system.includes(key)).map(key => `system ${item.id}/${key}`)),
      ...raw.records.flatMap(item => Object.keys(item).filter(key => !ALLOWED_KEYS.record.includes(key)).map(key => `record ${(item as { id: string }).id}/${key}`)),
    ];
    expect(extra).toEqual([]);
  });

  it('ends the index check with no error and every group run', async () => {
    const result = await checkRecordIndex({ repositoryRoot });
    const errors = result.diagnostics.filter(item => item.severity === 'error').map(item => `${item.code} ${item.location}`);
    expect(errors).toEqual([]);
    expect(result.groups.map(item => [item.group, item.ran]).sort()).toEqual([['backlog', true], ['goals', true], ['index', true]]);
  });

  it('indexes each of the 42 goals since 2026-09-30 as the prefix of a git source locator', () => {
    expect(GOALS_SINCE_0930).toHaveLength(42);
    const gitLocators = raw.sources.filter(item => item.kind === 'git').map(item => item.locator);
    const unindexed = GOALS_SINCE_0930.filter(goal => !gitLocators.some(locator => locator.startsWith(`${goal}/`)));
    expect(unindexed).toEqual([]);
  });

  it('links every system to at least one git Markdown source', () => {
    const markdown = new Set(raw.sources.filter(item => item.kind === 'git' && item.locator.endsWith('.md')).map(item => item.id));
    const without = raw.systems.filter(system => !system.sourceIds.some(id => markdown.has(id))).map(system => system.id);
    expect(without).toEqual([]);
  });

  it('keeps the 18 system IDs of a47a0276', () => {
    expect(raw.systems.map(system => system.id).sort()).toEqual([...SYSTEM_IDS_AT_BASE].sort());
  });

  it('keeps every relatedSystemIds of system-guide.json pointing at an index system', () => {
    const systems = new Set(raw.systems.map(system => system.id));
    const dangling = guide.cards.flatMap(card => card.relatedSystemIds.filter(id => !systems.has(id)).map(id => `${card.id} → ${id}`));
    expect(dangling).toEqual([]);
  });
});

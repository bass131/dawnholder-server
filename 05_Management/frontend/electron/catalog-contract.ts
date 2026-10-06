export const RECORD_TYPES = ['변경', '결정', '검증', '계획'] as const;
export const SOURCE_KINDS = ['git', 'local', 'handoff'] as const;
export const SOURCE_AVAILABILITY = ['versioned', 'local-only'] as const;
export const MAX_CATALOG_BYTES = 2 * 1024 * 1024;

export interface RecordSource {
  id: string;
  title: string;
  kind: typeof SOURCE_KINDS[number];
  locator: string;
  section: string | null;
  availability: typeof SOURCE_AVAILABILITY[number];
}

export interface SystemRecord {
  id: string;
  title: string;
  area: string;
  sourceIds: string[];
  relatedSystemIds: string[];
  recordIds: string[];
}

export interface RecordPullRequest {
  number: number;
  mergeCommit: string;
}

export interface DevelopmentRecord {
  id: string;
  type: typeof RECORD_TYPES[number];
  title: string;
  systemIds: string[];
  sourceIds: string[];
  pullRequests: RecordPullRequest[];
}

export interface RecordCatalog {
  schemaVersion: 2;
  sources: RecordSource[];
  systems: SystemRecord[];
  records: DevelopmentRecord[];
}

export function isRecordId(value: unknown): value is string {
  return typeof value === 'string' && value.length <= 128
    && /^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(value);
}

function isObject(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function isName(value: unknown): value is string {
  return typeof value === 'string' && value.length > 0 && value === value.trim();
}

function isIdList(value: unknown): value is string[] {
  return Array.isArray(value) && value.every(isRecordId);
}

function isLocator(value: unknown): value is string {
  return typeof value === 'string' && value.length > 0 && value.length <= 512
    && !/[\u0000-\u001f\u007f]/.test(value);
}

function isSection(value: unknown): value is string | null {
  return value === null || typeof value === 'string';
}

type FieldRules<T> = { [Key in keyof T]: (value: unknown) => value is T[Key] };

// Schema validation also owns its diagnostic locations, so the checker and the
// application reject the same fields, including every unrecognised key.
function invalidField<T>(value: unknown, rules: FieldRules<T>): string | null {
  if (!isObject(value)) return '(object)';
  for (const key of Object.keys(value)) {
    if (!Object.hasOwn(rules, key)) return key;
  }
  for (const key in rules) {
    if (!Object.hasOwn(value, key) || !rules[key](value[key])) return key;
  }
  return null;
}

function hasFields<T>(value: unknown, rules: FieldRules<T>): value is T {
  return invalidField(value, rules) === null;
}

const pullRequestRules: FieldRules<RecordPullRequest> = {
  number: (value): value is number => typeof value === 'number' && Number.isInteger(value) && value > 0,
  mergeCommit: (value): value is string => typeof value === 'string' && /^[a-f0-9]{40}$/.test(value),
};

function isPullRequests(value: unknown): value is RecordPullRequest[] {
  return Array.isArray(value)
    && value.every(item => hasFields(item, pullRequestRules))
    && new Set(value.map(item => item.number)).size === value.length;
}

const sourceRules: FieldRules<RecordSource> = {
  id: isRecordId,
  title: isName,
  kind: (value): value is RecordSource['kind'] => value === 'git' || value === 'local' || value === 'handoff',
  locator: isLocator,
  section: isSection,
  availability: (value): value is RecordSource['availability'] => value === 'versioned' || value === 'local-only',
};
const systemRules: FieldRules<SystemRecord> = {
  id: isRecordId,
  title: isName,
  area: isName,
  sourceIds: isIdList,
  relatedSystemIds: isIdList,
  recordIds: isIdList,
};
const recordRules: FieldRules<DevelopmentRecord> = {
  id: isRecordId,
  type: (value): value is DevelopmentRecord['type'] => value === '변경' || value === '결정' || value === '검증' || value === '계획',
  title: isName,
  systemIds: isIdList,
  sourceIds: isIdList,
  pullRequests: isPullRequests,
};

type InvalidCatalog = { ok: false; location: string };
type CatalogArray<T> = { ok: true; values: T[] } | InvalidCatalog;

function catalogArray<T extends { id: string }>(value: unknown, name: string, rules: FieldRules<T>): CatalogArray<T> {
  if (!Array.isArray(value)) return { ok: false, location: name };
  const values: T[] = [];
  const ids = new Set<string>();
  for (const [index, item] of value.entries()) {
    const label = isObject(item) && typeof item.id === 'string' ? item.id : String(index);
    const location = `${name}[${label}]`;
    if (!hasFields(item, rules)) {
      return { ok: false, location: `${location}.${invalidField(item, rules)}` };
    }
    if (ids.has(item.id)) return { ok: false, location: `${location}.id` };
    ids.add(item.id);
    values.push(item);
  }
  return { ok: true, values };
}

export function validateCatalog(value: unknown): { ok: true; catalog: RecordCatalog } | InvalidCatalog {
  if (!isObject(value)) return { ok: false, location: '(object)' };
  const keys = ['schemaVersion', 'sources', 'systems', 'records'];
  const extra = Object.keys(value).find(key => !keys.includes(key));
  if (extra) return { ok: false, location: extra };
  const missing = keys.find(key => !Object.hasOwn(value, key));
  if (missing) return { ok: false, location: missing };
  if (value.schemaVersion !== 2) return { ok: false, location: 'schemaVersion' };

  const sources = catalogArray(value.sources, 'sources', sourceRules);
  if (!sources.ok) return sources;
  for (const source of sources.values) {
    const expected = source.kind === 'git' ? 'versioned' : 'local-only';
    if (source.availability !== expected) {
      return { ok: false, location: `sources[${source.id}].availability` };
    }
  }
  const systems = catalogArray(value.systems, 'systems', systemRules);
  if (!systems.ok) return systems;
  const records = catalogArray(value.records, 'records', recordRules);
  if (!records.ok) return records;
  return { ok: true, catalog: { schemaVersion: 2, sources: sources.values, systems: systems.values, records: records.values } };
}

export function readCatalog(value: unknown): RecordCatalog | null {
  const result = validateCatalog(value);
  return result.ok ? result.catalog : null;
}

export interface CatalogReferenceIssue {
  owner: string;
  key: 'sourceIds' | 'relatedSystemIds' | 'recordIds' | 'systemIds';
  target: string;
}

export function catalogReferenceIssues(catalog: RecordCatalog): CatalogReferenceIssue[] {
  const systemIds = new Set(catalog.systems.map(item => item.id));
  const sourceIds = new Set(catalog.sources.map(item => item.id));
  const recordIds = new Set(catalog.records.map(item => item.id));
  const issues: CatalogReferenceIssue[] = [];
  function check(owner: string, key: CatalogReferenceIssue['key'], ids: string[], known: Set<string>) {
    for (const target of ids) {
      if (!known.has(target)) issues.push({ owner, key, target });
    }
  }
  for (const system of catalog.systems) {
    check(system.id, 'sourceIds', system.sourceIds, sourceIds);
    check(system.id, 'relatedSystemIds', system.relatedSystemIds, systemIds);
    check(system.id, 'recordIds', system.recordIds, recordIds);
  }
  for (const record of catalog.records) {
    check(record.id, 'systemIds', record.systemIds, systemIds);
    check(record.id, 'sourceIds', record.sourceIds, sourceIds);
  }
  return issues;
}

export function catalogReferenceErrors(catalog: RecordCatalog): string[] {
  return catalogReferenceIssues(catalog).map(issue => `${issue.owner}: 미확인 참조 ${issue.target}`);
}

export type CatalogResult =
  | { ok: true; catalog: RecordCatalog; version: string }
  | { ok: false; code: 'missing' | 'invalid' | 'load' | 'denied'; message: string };

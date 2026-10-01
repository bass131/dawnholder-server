export const RECORD_TYPES = ['변경', '결정', '검증', '계획'] as const;
export const SOURCE_KINDS = ['git', 'local', 'handoff'] as const;
export const SOURCE_AVAILABILITY = ['versioned', 'local-only'] as const;

export interface RecordSource {
  id: string; title: string; kind: typeof SOURCE_KINDS[number]; locator: string;
  revision: string; section: string; availability: typeof SOURCE_AVAILABILITY[number]; note: string;
}
export interface SystemRecord {
  id: string; title: string; area: string; summary: string; responsibility: string;
  behavior: string[]; implementationStatus: string; integrationStatus: string; verificationStatus: string;
  limitations: string[]; nextSteps: string[]; sourceIds: string[]; relatedSystemIds: string[]; recordIds: string[];
}
export interface DevelopmentRecord {
  id: string; type: typeof RECORD_TYPES[number]; title: string; summary: string;
  reason: string; status: string; systemIds: string[]; sourceIds: string[];
  details: string[]; limitations: string[]; nextSteps: string[];
}
export interface RecordCatalog {
  schemaVersion: 1; revision: string; asOf: string; sourceCommit: string; scopeNote: string;
  sources: RecordSource[]; systems: SystemRecord[]; records: DevelopmentRecord[];
}

const object = (value: unknown): value is Record<string, unknown> => typeof value === 'object' && value !== null && !Array.isArray(value);
const strings = (value: unknown): value is string[] => Array.isArray(value) && value.every(item => typeof item === 'string');
const enumValue = (values: readonly string[], value: unknown): boolean => values.includes(String(value));
function fields(value: unknown, text: string[], lists: string[] = []): value is Record<string, unknown> {
  return object(value) && text.every(key => typeof value[key] === 'string') && lists.every(key => strings(value[key]));
}
function uniqueIds(values: unknown[]): boolean {
  const ids = values.map(value => object(value) ? value.id : undefined);
  return ids.every(id => typeof id === 'string' && id.trim().length > 0) && new Set(ids).size === ids.length;
}
export function readCatalog(value: unknown): RecordCatalog | null {
  if (!fields(value, ['revision', 'asOf', 'sourceCommit', 'scopeNote']) || value.schemaVersion !== 1) return null;
  const { sources, systems, records } = value;
  if (!Array.isArray(sources) || !Array.isArray(systems) || !Array.isArray(records)) return null;
  if (![sources, systems, records].every(uniqueIds)) return null;
  if (!sources.every(source => fields(source, ['id', 'title', 'kind', 'locator', 'revision', 'section', 'availability', 'note']) && enumValue(SOURCE_KINDS, source.kind) && enumValue(SOURCE_AVAILABILITY, source.availability))) return null;
  if (!systems.every(system => fields(system, ['id', 'title', 'area', 'summary', 'responsibility', 'implementationStatus', 'integrationStatus', 'verificationStatus'], ['behavior', 'limitations', 'nextSteps', 'sourceIds', 'relatedSystemIds', 'recordIds']))) return null;
  if (!records.every(record => fields(record, ['id', 'type', 'title', 'summary', 'reason', 'status'], ['systemIds', 'sourceIds', 'details', 'limitations', 'nextSteps']) && enumValue(RECORD_TYPES, record.type))) return null;
  return value as unknown as RecordCatalog;
}
export function catalogReferenceErrors(catalog: RecordCatalog): string[] {
  const systemIds = new Set(catalog.systems.map(item => item.id));
  const sourceIds = new Set(catalog.sources.map(item => item.id));
  const recordIds = new Set(catalog.records.map(item => item.id));
  const errors: string[] = [];
  function check(owner: string, ids: string[], known: Set<string>) {
    ids.filter(id => !known.has(id)).forEach(id => errors.push(`${owner}: 미확인 참조 ${id}`));
  }
  catalog.systems.forEach(system => {
    check(system.id, system.sourceIds, sourceIds); check(system.id, system.relatedSystemIds, systemIds); check(system.id, system.recordIds, recordIds);
  });
  catalog.records.forEach(record => { check(record.id, record.systemIds, systemIds); check(record.id, record.sourceIds, sourceIds); });
  return errors;
}
export type CatalogResult =
  | { ok: true; catalog: RecordCatalog; text: string; version: string }
  | { ok: false; code: 'missing' | 'invalid' | 'load' | 'write' | 'busy' | 'conflict' | 'denied'; message: string; version?: string; text?: string };
export interface CatalogBridge {
  readCatalog(): Promise<CatalogResult>;
  saveCatalog(input: { text: string; expectedVersion: string | null }): Promise<CatalogResult>;
}
export const MAX_CATALOG_BYTES = 2 * 1024 * 1024;

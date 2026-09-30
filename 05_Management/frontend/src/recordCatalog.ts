import type { RecordCatalog, SystemRecord, DevelopmentRecord } from '../electron/catalog-contract';
export * from '../electron/catalog-contract';
export function matchesQuery(query: string, values: string[]): boolean {
  const terms = query.trim().toLocaleLowerCase().split(/\s+/).filter(Boolean);
  const text = values.join(' ').toLocaleLowerCase();
  return terms.every(term => text.includes(term));
}
export function filterSystems(catalog: RecordCatalog, query: string, area: string): SystemRecord[] {
  return catalog.systems.filter(system => (!area || system.area === area) && matchesQuery(query, [system.id, system.title, system.area, system.summary, system.responsibility, ...system.behavior]));
}
export function filterRecords(catalog: RecordCatalog, query: string, area: string, type: string): DevelopmentRecord[] {
  return catalog.records.filter(record => (!type || record.type === type) && (!area || record.systemIds.some(id => catalog.systems.some(system => system.id === id && system.area === area))) && matchesQuery(query, [record.id, record.title, record.summary, record.reason, ...record.details]));
}

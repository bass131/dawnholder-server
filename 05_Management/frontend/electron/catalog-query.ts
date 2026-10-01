import type { RecordCatalog, SystemRecord, DevelopmentRecord } from './catalog-contract.js';

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

export function compareCatalogIds(a: { id: string }, b: { id: string }): number {
  return a.id < b.id ? -1 : a.id > b.id ? 1 : 0;
}

export function previewText(text: string, limit: number): string {
  if (text.length <= limit) return text;
  const last = text.charCodeAt(limit - 1);
  const next = text.charCodeAt(limit);
  const end = last >= 0xd800 && last <= 0xdbff && next >= 0xdc00 && next <= 0xdfff ? limit - 1 : limit;
  return text.slice(0, end);
}

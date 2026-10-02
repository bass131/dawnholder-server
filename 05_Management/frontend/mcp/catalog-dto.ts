import type { DevelopmentRecord, RecordCatalog, RecordSource, SystemRecord } from '../electron/catalog-contract.js';
import { previewText } from '../electron/catalog-query.js';

export function systemDetails(item: SystemRecord): SystemRecord {
  return {
    id: item.id, title: item.title, area: item.area, summary: item.summary, responsibility: item.responsibility,
    behavior: [...item.behavior], implementationStatus: item.implementationStatus, integrationStatus: item.integrationStatus,
    verificationStatus: item.verificationStatus, limitations: [...item.limitations], nextSteps: [...item.nextSteps],
    sourceIds: [...item.sourceIds], relatedSystemIds: [...item.relatedSystemIds], recordIds: [...item.recordIds],
  };
}

export function recordDetails(item: DevelopmentRecord): DevelopmentRecord {
  return {
    id: item.id, type: item.type, title: item.title, summary: item.summary, reason: item.reason, status: item.status,
    systemIds: [...item.systemIds], sourceIds: [...item.sourceIds], details: [...item.details], limitations: [...item.limitations], nextSteps: [...item.nextSteps],
  };
}

export function sourceDetails(item: RecordSource): RecordSource {
  return { id: item.id, title: item.title, kind: item.kind, locator: item.locator, revision: item.revision, section: item.section, availability: item.availability, note: item.note };
}

export function catalogDetails(catalog: RecordCatalog): RecordCatalog {
  return {
    schemaVersion: catalog.schemaVersion, revision: catalog.revision, asOf: catalog.asOf, sourceCommit: catalog.sourceCommit, scopeNote: catalog.scopeNote,
    systems: catalog.systems.map(systemDetails), records: catalog.records.map(recordDetails), sources: catalog.sources.map(sourceDetails),
  };
}

function previews<T extends Record<string, string>>(fields: T): { values: T; truncatedFields: string[] } {
  const truncatedFields: string[] = [];
  const values = Object.fromEntries(Object.entries(fields).map(([key, text]) => {
    const value = previewText(text, key === 'summary' ? 160 : 80);
    if (value.length !== text.length) truncatedFields.push(key);
    return [key, value];
  })) as T;
  return { values, truncatedFields };
}

export function systemSummary(item: SystemRecord) {
  const preview = previews({ title: item.title, summary: item.summary, implementationStatus: item.implementationStatus, integrationStatus: item.integrationStatus, verificationStatus: item.verificationStatus });
  return { id: item.id, area: item.area, ...preview.values, lookupSupported: item.id.length <= 128, truncatedFields: preview.truncatedFields };
}

export function recordSummary(item: DevelopmentRecord) {
  const preview = previews({ title: item.title, summary: item.summary, status: item.status });
  return { id: item.id, type: item.type, ...preview.values, systemIds: [...item.systemIds], lookupSupported: item.id.length <= 128, truncatedFields: preview.truncatedFields };
}

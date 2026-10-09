import type { DevelopmentRecord, RecordCatalog, RecordSource, SystemRecord } from '../electron/catalog-contract.js';
import { previewText } from '../electron/catalog-query.js';
import type { SystemCard } from '../electron/system-guide-contract.js';

export function systemDetails(item: SystemRecord): SystemRecord {
  return {
    id: item.id,
    title: item.title,
    area: item.area,
    sourceIds: [...item.sourceIds],
    relatedSystemIds: [...item.relatedSystemIds],
    recordIds: [...item.recordIds],
  };
}

export function recordDetails(item: DevelopmentRecord): DevelopmentRecord {
  return {
    id: item.id,
    type: item.type,
    title: item.title,
    systemIds: [...item.systemIds],
    sourceIds: [...item.sourceIds],
    pullRequests: item.pullRequests.map(pr => ({ number: pr.number, mergeCommit: pr.mergeCommit })),
  };
}

export function sourceDetails(item: RecordSource): RecordSource {
  return {
    id: item.id,
    title: item.title,
    kind: item.kind,
    locator: item.locator,
    section: item.section,
    availability: item.availability,
  };
}

export function catalogDetails(catalog: RecordCatalog): RecordCatalog {
  return {
    schemaVersion: catalog.schemaVersion,
    systems: catalog.systems.map(systemDetails),
    records: catalog.records.map(recordDetails),
    sources: catalog.sources.map(sourceDetails),
  };
}

function titlePreview(text: string) {
  const title = previewText(text, 80);
  return { title, truncatedFields: title.length === text.length ? [] : ['title'] };
}

export function systemSummary(item: SystemRecord) {
  return {
    id: item.id,
    area: item.area,
    ...titlePreview(item.title),
    lookupSupported: item.id.length <= 128,
  };
}

export function recordSummary(item: DevelopmentRecord) {
  return {
    id: item.id,
    type: item.type,
    ...titlePreview(item.title),
    systemIds: [...item.systemIds],
    lookupSupported: item.id.length <= 128,
  };
}

export function guideCardSummary(item: SystemCard) {
  return {
    id: item.id,
    parentId: item.parentId,
    ...titlePreview(item.title),
    relatedSystemIds: [...item.relatedSystemIds],
    documentId: item.documentId ?? null,
    lookupSupported: item.id.length <= 128,
  };
}

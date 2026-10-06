import { compareCatalogIds, filterRecords, filterSystems } from '../electron/catalog-query.js';
import { recordDetails, recordSummary, sourceDetails, systemDetails, systemSummary } from './catalog-dto.js';
import type { CatalogSnapshot } from './catalog-reader.js';
import { DEFAULT_LIST_LIMIT, failure, listResponse, success } from './catalog-response.js';
import type { CatalogResponse } from './catalog-response.js';
import type { RecordToolName, ToolArguments } from './catalog-schemas.js';

// These five tools query only the immutable index snapshot supplied by the server.
export function queryRecordTools(name: RecordToolName, args: ToolArguments, snapshot: CatalogSnapshot): CatalogResponse {
  const { metadata, catalog } = snapshot;
  if (name === 'list_systems') {
    const items = filterSystems(catalog, args.query ?? '', args.area ?? '')
      .sort(compareCatalogIds)
      .map(systemSummary);
    return listResponse(metadata, items, args.offset ?? 0, args.limit ?? DEFAULT_LIST_LIMIT);
  }
  if (name === 'search_records') {
    if (args.systemId !== undefined && !catalog.systems.some(item => item.id === args.systemId)) {
      return failure('NOT_FOUND', metadata);
    }
    const items = filterRecords(catalog, args.query ?? '', args.area ?? '', args.type ?? '')
      .filter(item => args.systemId === undefined || item.systemIds.includes(args.systemId))
      .sort(compareCatalogIds)
      .map(recordSummary);
    return listResponse(metadata, items, args.offset ?? 0, args.limit ?? DEFAULT_LIST_LIMIT);
  }
  if (name === 'get_system') {
    const item = catalog.systems.find(system => system.id === args.id);
    return item ? success(metadata, { system: systemDetails(item) }) : failure('NOT_FOUND', metadata);
  }
  if (name === 'get_record') {
    const item = catalog.records.find(record => record.id === args.id);
    return item ? success(metadata, { record: recordDetails(item) }) : failure('NOT_FOUND', metadata);
  }
  const item = catalog.sources.find(source => source.id === args.id);
  if (!item) return failure('NOT_FOUND', metadata);
  return success(metadata, {
    source: sourceDetails(item),
    evidenceRead: false,
    availabilityVerified: false,
  });
}

import type { McpServer } from '@modelcontextprotocol/server';
import { compareCatalogIds, filterRecords, filterSystems } from '../electron/catalog-query.js';
import { recordDetails, recordSummary, sourceDetails, systemDetails, systemSummary } from './catalog-dto.js';
import { CatalogReadError, checkCancelled } from './catalog-errors.js';
import type { CatalogSnapshot } from './catalog-reader.js';
import { failure, listResponse, success, type CatalogResponse } from './catalog-response.js';
import { inputSchemas, outputSchemas, type CatalogToolName, type ToolArguments } from './catalog-schemas.js';
import { CatalogMcpServer } from './catalog-tool-name-transport.js';

export interface CatalogServerOptions {
  readSnapshot(signal: AbortSignal): Promise<CatalogSnapshot>;
  version: string;
  now?: () => number;
  // Internal measurement seam: called only after SDK input validation, before
  // admission/semantic checks. Production neither supplies nor exposes it.
  onToolHandlerEntered?: (toolName: CatalogToolName) => void;
}

function querySnapshot(name: CatalogToolName, args: ToolArguments, snapshot: CatalogSnapshot): CatalogResponse {
  const { metadata, catalog } = snapshot;
  if (args.expectedHash !== undefined && args.expectedHash !== metadata.hash) return failure('VERSION_CONFLICT', metadata, { expectedHash: args.expectedHash });
  if (name === 'list_systems') {
    const items = filterSystems(catalog, args.query ?? '', args.area ?? '').sort(compareCatalogIds).map(systemSummary);
    return listResponse(metadata, items, args.offset ?? 0, args.limit ?? 10);
  }
  if (name === 'search_records') {
    if (args.systemId !== undefined && !catalog.systems.some(item => item.id === args.systemId)) return failure('NOT_FOUND', metadata);
    const items = filterRecords(catalog, args.query ?? '', args.area ?? '', args.type ?? '')
      .filter(item => args.systemId === undefined || item.systemIds.includes(args.systemId)).sort(compareCatalogIds).map(recordSummary);
    return listResponse(metadata, items, args.offset ?? 0, args.limit ?? 10);
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
  return item ? success(metadata, { source: sourceDetails(item), evidenceRead: false, availabilityVerified: false }) : failure('NOT_FOUND', metadata);
}

export function createCatalogServer(options: CatalogServerOptions): McpServer {
  const server = new CatalogMcpServer({ name: 'dawnholder-catalog', version: options.version });
  const now = options.now ?? (() => performance.now());
  let active = 0;
  let tokens = 10;
  let lastRefill = now();

  async function handle(name: CatalogToolName, args: ToolArguments, signal: AbortSignal): Promise<CatalogResponse> {
    options.onToolHandlerEntered?.(name);
    if (signal.aborted) return failure('REQUEST_CANCELLED');
    const current = Math.max(lastRefill, now());
    tokens = Math.min(10, tokens + (current - lastRefill) / 100);
    lastRefill = current;
    if (active >= 4 || tokens < 1) return failure('RATE_LIMITED', null, { retryAfterMs: active >= 4 ? 100 : Math.min(1000, Math.max(1, Math.ceil((1 - tokens) * 100))) });
    tokens -= 1;
    active += 1;
    try {
      checkCancelled(signal);
      // The SDK already rejects malformed/unknown inputs. This semantic check
      // precedes I/O, so later pages can never silently switch snapshots.
      if ((name === 'list_systems' || name === 'search_records') && (args.offset ?? 0) > 0 && args.expectedHash === undefined) return failure('VERSION_REQUIRED');
      const snapshot = await options.readSnapshot(signal);
      checkCancelled(signal);
      const result = querySnapshot(name, args, snapshot);
      checkCancelled(signal);
      return result;
    } catch (error) {
      if (signal.aborted) return failure('REQUEST_CANCELLED');
      if (error instanceof CatalogReadError) return failure(error.code, null, error.details);
      return failure('CATALOG_UNREADABLE');
    } finally {
      active -= 1;
    }
  }

  const annotations = { readOnlyHint: true, destructiveHint: false, idempotentHint: true, openWorldHint: false };
  server.registerTool('list_systems', {
    description: 'List system previews from the catalog. Use expectedHash for subsequent pages; catalog metadata describes recorded history.',
    inputSchema: inputSchemas.list_systems, outputSchema: outputSchemas.list_systems, annotations,
  }, (args, context) => handle('list_systems', args, context.mcpReq.signal));
  server.registerTool('search_records', {
    description: 'Search recorded changes, decisions, verification and plans with AND filters. Use expectedHash for subsequent pages.',
    inputSchema: inputSchemas.search_records, outputSchema: outputSchemas.search_records, annotations,
  }, (args, context) => handle('search_records', args, context.mcpReq.signal));
  server.registerTool('get_system', {
    description: 'Read the complete catalog system by ID, optionally fixed to expectedHash.',
    inputSchema: inputSchemas.get_system, outputSchema: outputSchemas.get_system, annotations,
  }, (args, context) => handle('get_system', args, context.mcpReq.signal));
  server.registerTool('get_record', {
    description: 'Read the complete catalog development record by ID, optionally fixed to expectedHash.',
    inputSchema: inputSchemas.get_record, outputSchema: outputSchemas.get_record, annotations,
  }, (args, context) => handle('get_record', args, context.mcpReq.signal));
  server.registerTool('get_source', {
    description: 'Read catalog source metadata only. The locator is not opened; evidenceRead and availabilityVerified are false.',
    inputSchema: inputSchemas.get_source, outputSchema: outputSchemas.get_source, annotations,
  }, (args, context) => handle('get_source', args, context.mcpReq.signal));
  return server;
}

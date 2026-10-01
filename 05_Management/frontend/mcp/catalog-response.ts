import { ERROR_MESSAGES, errorIsRetryable, type CatalogErrorCode, type ErrorDetails } from './catalog-errors.js';
import type { SnapshotMetadata } from './catalog-reader.js';

export const MAX_RESPONSE_BYTES = 16384;
export const DEFAULT_LIST_BYTES = 8192;

export type CatalogEnvelope =
  | { ok: true; snapshot: SnapshotMetadata; data: Record<string, unknown> }
  | { ok: false; snapshot: SnapshotMetadata | null; error: { code: CatalogErrorCode; message: string; retryable: boolean; details?: ErrorDetails } };

export function serializeEnvelope(envelope: CatalogEnvelope) {
  return { content: [{ type: 'text' as const, text: JSON.stringify(envelope) }], structuredContent: envelope, isError: !envelope.ok };
}
export type CatalogResponse = ReturnType<typeof serializeEnvelope>;

export function responseBytes(result: CatalogResponse): number {
  return Buffer.byteLength(JSON.stringify(result), 'utf8');
}

export function failure(code: CatalogErrorCode, snapshot: SnapshotMetadata | null = null, details?: ErrorDetails): CatalogResponse {
  const error = { code, message: ERROR_MESSAGES[code], retryable: errorIsRetryable(code), ...(details ? { details } : {}) };
  let result = serializeEnvelope({ ok: false, snapshot, error });
  if (responseBytes(result) <= MAX_RESPONSE_BYTES) return result;
  // Preserve the cause but drop unbounded catalog metadata before emitting it.
  result = serializeEnvelope({ ok: false, snapshot: null, error: { ...error, details: { ...details, metadataOmitted: true } } });
  if (responseBytes(result) <= MAX_RESPONSE_BYTES) return result;
  return serializeEnvelope({ ok: false, snapshot: null, error: { ...error, details: { metadataOmitted: true } } });
}

export function success(snapshot: SnapshotMetadata, data: Record<string, unknown>): CatalogResponse {
  const result = serializeEnvelope({ ok: true, snapshot, data });
  return responseBytes(result) <= MAX_RESPONSE_BYTES ? result : failure('RESPONSE_TOO_LARGE', snapshot);
}

export function listResponse(snapshot: SnapshotMetadata, items: unknown[], offset: number, limit: number): CatalogResponse {
  const total = items.length;
  const selected: unknown[] = [];
  function candidate() {
    const returned = selected.length;
    return serializeEnvelope({ ok: true, snapshot, data: { items: selected, paging: { total, offset, limit, returned, nextOffset: offset + returned < total ? offset + returned : null } } });
  }
  if (offset >= total) return success(snapshot, { items: [], paging: { total, offset, limit, returned: 0, nextOffset: null } });
  const budget = limit === 10 ? DEFAULT_LIST_BYTES : MAX_RESPONSE_BYTES;
  for (const item of items.slice(offset, offset + limit)) {
    selected.push(item);
    const bytes = responseBytes(candidate());
    if (bytes > MAX_RESPONSE_BYTES) {
      selected.pop();
      if (!selected.length) return failure('RESPONSE_TOO_LARGE', snapshot);
      break;
    }
    if (bytes > budget) {
      if (selected.length > 1) selected.pop();
      break;
    }
  }
  const result = candidate();
  return responseBytes(result) <= MAX_RESPONSE_BYTES ? result : failure('RESPONSE_TOO_LARGE', snapshot);
}

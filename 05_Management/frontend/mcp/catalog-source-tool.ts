import type { RecordSource } from '../electron/catalog-contract.js';
import { catalogHash } from '../electron/catalog-hash.js';
import type { CheckoutInfo } from '../electron/checkout-contract.js';
import type { SourceSectionFailure, SourceSectionResult } from '../electron/source-section-contract.js';
import { sourceDetails } from './catalog-dto.js';
import { checkCancelled } from './catalog-errors.js';
import type { CatalogErrorCode } from './catalog-errors.js';
import type { CatalogSnapshot, SnapshotMetadata } from './catalog-reader.js';
import { MAX_RESPONSE_BYTES, failure, responseBytes, serializeEnvelope, success } from './catalog-response.js';
import type { CatalogResponse } from './catalog-response.js';
import type { ToolArguments } from './catalog-schemas.js';

export interface SourceToolReaders {
  readSourceSection(source: RecordSource, signal: AbortSignal): Promise<SourceSectionResult>;
  readCheckout(signal: AbortSignal): Promise<CheckoutInfo>;
}

interface SectionPageFields {
  source: RecordSource;
  path: string;
  heading: string | null;
  sectionHash: string;
  checkout: CheckoutInfo;
}

function sourceErrorCode(result: SourceSectionFailure): CatalogErrorCode {
  switch (result.code) {
    case 'not-readable': return 'SOURCE_NOT_READABLE';
    case 'path-rejected': return 'SOURCE_PATH_REJECTED';
    case 'missing': return 'SOURCE_MISSING';
    case 'too-large': return 'SOURCE_TOO_LARGE';
    case 'changed': return 'SOURCE_CHANGED_DURING_READ';
    case 'invalid-encoding': return 'SOURCE_INVALID_ENCODING';
    case 'load': return 'SOURCE_UNREADABLE';
    case 'section-missing': return 'SECTION_MISSING';
    case 'section-ambiguous': return 'SECTION_AMBIGUOUS';
    // Input and ID lookup belong to the MCP boundary; these reader/IPC-only
    // failures must reach the server's fixed diagnostic rather than leak text.
    default: throw new Error('Unexpected source result.');
  }
}

function splitsSurrogatePair(text: string, offset: number): boolean {
  const before = text.charCodeAt(offset - 1);
  const after = text.charCodeAt(offset);
  return before >= 0xd800 && before <= 0xdbff && after >= 0xdc00 && after <= 0xdfff;
}

function sectionPage(metadata: SnapshotMetadata, fields: SectionPageFields, text: string, offset: number): CatalogResponse {
  const total = text.length;
  if (offset >= total) {
    return success(metadata, {
      ...fields,
      text: '',
      paging: { offset, returned: 0, total, nextOffset: null },
    });
  }
  function safeLength(length: number): number {
    return splitsSurrogatePair(text, offset + length) ? length - 1 : length;
  }
  function candidate(returned: number): CatalogResponse {
    const end = offset + returned;
    return serializeEnvelope({
      ok: true,
      snapshot: metadata,
      data: {
        ...fields,
        text: text.slice(offset, end),
        paging: { offset, returned, total, nextOffset: end < total ? end : null },
      },
    });
  }
  const remaining = total - offset;
  const whole = candidate(remaining);
  // The last page's null nextOffset can be smaller than an earlier numeric
  // cursor. Check it first, then search only pair-safe, monotonically growing
  // partial fragments, including both copies of the envelope in the byte cost.
  if (responseBytes(whole) <= MAX_RESPONSE_BYTES) return whole;
  let lower = 0;
  let upper = remaining;
  while (lower < upper) {
    const middle = Math.ceil((lower + upper) / 2);
    if (responseBytes(candidate(safeLength(middle))) <= MAX_RESPONSE_BYTES) {
      lower = middle;
    } else {
      upper = middle - 1;
    }
  }
  const returned = safeLength(lower);
  if (returned === 0) return failure('RESPONSE_TOO_LARGE', metadata);
  return candidate(returned);
}

// The app store owns path checks, UTF-8 decoding, extraction and its handles.
// This module only maps its result and pages the successful plain text for MCP.
export async function querySourceTool(
  args: ToolArguments,
  snapshot: CatalogSnapshot,
  readers: SourceToolReaders,
  signal: AbortSignal,
): Promise<CatalogResponse> {
  const { metadata, catalog } = snapshot;
  const source = catalog.sources.find(item => item.id === args.id);
  if (!source) return failure('NOT_FOUND', metadata);
  const result = await readers.readSourceSection(source, signal);
  checkCancelled(signal);
  if (!result.ok) {
    const code = sourceErrorCode(result);
    return failure(code, metadata, result.reason === null ? undefined : { reason: result.reason });
  }
  const sectionHash = catalogHash(result.text);
  const offset = args.offset ?? 0;
  if (offset > 0 && args.expectedSectionHash !== undefined && args.expectedSectionHash !== sectionHash) {
    return failure('VERSION_CONFLICT', metadata, { expectedSectionHash: args.expectedSectionHash });
  }
  if (splitsSurrogatePair(result.text, offset)) return failure('INVALID_ARGUMENT', metadata);
  checkCancelled(signal);
  // Read checkout only after the section/version/offset checks: a broken link
  // must not cause unrelated Git metadata I/O or hide its original error.
  const checkout = await readers.readCheckout(signal);
  checkCancelled(signal);
  return sectionPage(metadata, {
    source: sourceDetails(source),
    path: source.locator,
    heading: result.heading,
    sectionHash,
    checkout,
  }, result.text, offset);
}

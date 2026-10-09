import { matchesQuery } from '../electron/catalog-query.js';
import type { GuideResult } from '../electron/system-guide-contract.js';
import { guideCardSummary } from './catalog-dto.js';
import type { CatalogErrorCode } from './catalog-errors.js';
import { DEFAULT_LIST_LIMIT, failure, listResponse, success } from './catalog-response.js';
import type { CatalogResponse } from './catalog-response.js';
import type { GuideToolName, ToolArguments } from './catalog-schemas.js';

function guideErrorCode(result: Extract<GuideResult, { ok: false }>): CatalogErrorCode {
  switch (result.code) {
    case 'missing': return 'GUIDE_MISSING';
    case 'load': return 'GUIDE_UNREADABLE';
    case 'too-large': return 'GUIDE_TOO_LARGE';
    case 'invalid': return 'GUIDE_INVALID';
    case 'changed': return 'GUIDE_CHANGED_DURING_READ';
    // Each production request owns a new store, so busy/denied is an internal
    // wiring failure diagnosed by the server's fixed catch-all, never an echo.
    default: throw new Error('Unexpected guide result.');
  }
}

// GuideResult already owns the app's validation and version; do not re-parse it
// or read the unrelated record index to serve these two tools.
export function queryGuideTools(name: GuideToolName, args: ToolArguments, result: GuideResult): CatalogResponse {
  if (!result.ok) return failure(guideErrorCode(result));
  const metadata = { hash: result.version };
  if (args.expectedHash !== undefined && args.expectedHash !== metadata.hash) {
    return failure('VERSION_CONFLICT', metadata, { expectedHash: args.expectedHash });
  }
  const { guide } = result;
  if (name === 'list_guide_cards') {
    const items = guide.cards
      .filter(card => matchesQuery(args.query ?? '', [card.id, card.title, card.summary]))
      .map(guideCardSummary);
    return listResponse(metadata, items, args.offset ?? 0, args.limit ?? DEFAULT_LIST_LIMIT);
  }
  const card = guide.cards.find(item => item.id === args.id);
  if (!card) return failure('NOT_FOUND', metadata);
  const document = guide.documents.find(item => item.id === card.documentId) ?? null;
  return success(metadata, { card, document });
}

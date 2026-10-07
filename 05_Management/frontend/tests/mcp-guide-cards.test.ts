// @vitest-environment node
// Requirement: goal 「만들 것」 4 (MCP serves the system cards) and index-v2-design.md 「MCP」 tool
// table, 「도구 규칙」 (list_guide_cards, get_guide_card, 스냅샷) and 오류 코드 표 (카드 rows), with
// Astra 보충 v1.1 (msg_6dd9f66e2ca7) Q2·Q3·Q5. Card tools read through the injected readGuide; the
// version parity cases inject the real createSystemGuideStore like main.ts will. Expected previews
// are cut here by definition and expected hashes come from node:crypto.
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { afterAll, afterEach, beforeAll, describe, expect, it, vi } from 'vitest';
import type { GuideResult, SystemCard, SystemGuide } from '../electron/system-guide-contract';
import { createSystemGuideStore } from '../electron/system-guide-store';
import {
  DEFAULT_PAGE_TARGET, MAX_RESULT, authoredBytes, baselineMatchesQuery, connectHarness, expectError, expectSuccess, guideBytes,
  guideResultOf, linkedCatalog, linkedGuide, makeCard, makeGuide, previewByDefinition, sha256, staticSnapshot, walkPages,
  type ErrorCode, type Harness, type Paging, type ReadGuide,
} from './mcp-fixtures';
import { createOwnedTempRepository, type OwnedTempRepository } from './record-sources/owned-temp-repository';

const REAL_GUIDE_PATH = fileURLToPath(new URL('../../records/system-guide.json', import.meta.url));

let harness: Harness | undefined;
afterEach(async () => {
  await harness?.close();
  harness = undefined;
  vi.restoreAllMocks();
});

async function connectCards(readGuide: ReadGuide) {
  harness = await connectHarness({ readSnapshot: async () => staticSnapshot(linkedCatalog()), readGuide });
  return harness;
}
const connectGuide = (guide: SystemGuide) => connectCards(async () => guideResultOf(guide));

// Design 「도구 규칙」 list_guide_cards preview, computed from the card by definition.
function cardPreview(card: SystemCard) {
  const title = previewByDefinition(card.title);
  return {
    id: card.id, parentId: card.parentId, title, relatedSystemIds: card.relatedSystemIds, documentId: card.documentId ?? null,
    lookupSupported: card.id.length <= 128, truncatedFields: title === card.title ? [] : ['title'],
  };
}

interface CardPage { items: Array<ReturnType<typeof cardPreview>>; paging: Paging }

describe('list_guide_cards previews (design 「도구 규칙」 list_guide_cards)', () => {
  it('returns { id, parentId, title, relatedSystemIds, documentId, lookupSupported, truncatedFields } in card array order', async () => {
    const guide = linkedGuide();
    const { data } = expectSuccess<CardPage>('list_guide_cards', await (await connectGuide(guide)).call('list_guide_cards', { limit: 50 }));
    expect(data.items).toEqual(guide.cards.map(cardPreview));
    expect(data.items.map(item => item.id)).toEqual(['server', 'client', 'server.network', 'automation']);
  });

  it('a card without documentId previews documentId: null', async () => {
    const { data } = expectSuccess<CardPage>('list_guide_cards', await (await connectGuide(linkedGuide())).call('list_guide_cards', {}));
    expect(data.items.find(item => item.id === 'client')?.documentId).toBeNull();
    expect(data.items.find(item => item.id === 'server.network')?.documentId).toBe('server.network.receive');
  });

  it('cuts the title at 80 UTF-16 code units without splitting a surrogate pair (보충 v1.1 Q2)', async () => {
    const guide = makeGuide({
      cards: [
        makeCard('exact', { title: 't'.repeat(80) }),
        makeCard('over', { title: 'o'.repeat(81) }),
        makeCard('pair', { title: `${'p'.repeat(79)}😀끝` }),
      ],
    });
    const { data } = expectSuccess<CardPage>('list_guide_cards', await (await connectGuide(guide)).call('list_guide_cards', {}));
    expect(data.items.map(item => [item.id, item.title, item.truncatedFields])).toEqual([
      ['exact', 't'.repeat(80), []],
      ['over', 'o'.repeat(80), ['title']],
      ['pair', 'p'.repeat(79), ['title']],
    ]);
  });

  it('lookupSupported is false only for a card ID longer than 128 characters', async () => {
    const guide = makeGuide({ cards: [makeCard('a'.repeat(128)), makeCard('b'.repeat(129))] });
    const { data } = expectSuccess<CardPage>('list_guide_cards', await (await connectGuide(guide)).call('list_guide_cards', {}));
    expect(data.items.map(item => item.lookupSupported)).toEqual([true, false]);
  });
});

describe('list_guide_cards query searches card id·title·summary only (design 「도구 규칙」)', () => {
  const queries = ['server', '수신', 'UNITY', '서버 server', 'istanbul', 'sys-alpha', 'receive', '없는검색어'];
  for (const query of queries) {
    it(`query "${query}" matches the same cards as the id·title·summary oracle, in array order`, async () => {
      const guide = linkedGuide();
      const expected = guide.cards.filter(card => baselineMatchesQuery(query, [card.id, card.title, card.summary])).map(card => card.id);
      const { data } = expectSuccess<CardPage>('list_guide_cards', await (await connectGuide(guide)).call('list_guide_cards', { query, limit: 50 }));
      expect(data.items.map(item => item.id)).toEqual(expected);
    });
  }

  it('relatedSystemIds and documentId are not search targets', async () => {
    const server = await connectGuide(linkedGuide());
    for (const query of ['sys-alpha', 'receive']) {
      expect(expectSuccess<CardPage>('list_guide_cards', await server.call('list_guide_cards', { query })).data.items, query).toEqual([]);
    }
  });
});

describe('list_guide_cards budget and pages (design 「도구 규칙」 목록 예산)', () => {
  // Long relatedSystemIds are not cut, so each preview is large enough to meet the budgets.
  const manyCards = () => makeGuide({
    cards: Array.from({ length: 45 }, (_, index) => makeCard(`card-${String(index).padStart(2, '0')}`, {
      relatedSystemIds: Array.from({ length: 12 }, (_, related) => `related-system-${index}-${related}-${'r'.repeat(20)}`),
    })),
  });

  it('the default page stays within 8 KiB, explicit limit 10 matches it, and one more preview would not fit', async () => {
    const guide = manyCards();
    const server = await connectGuide(guide);
    const defaultOutcome = await server.call('list_guide_cards', {});
    const defaultPage = expectSuccess<CardPage>('list_guide_cards', defaultOutcome).data;
    const explicitPage = expectSuccess<CardPage>('list_guide_cards', await server.call('list_guide_cards', { limit: 10 })).data;

    expect(defaultPage.paging.returned).toBeGreaterThan(1);
    expect(defaultPage.paging.returned).toBeLessThan(10);
    expect(authoredBytes(defaultOutcome)).toBeLessThanOrEqual(DEFAULT_PAGE_TARGET);
    expect(explicitPage.items).toEqual(defaultPage.items);

    const observed = defaultOutcome.authored?.structuredContent as { ok: true; snapshot: unknown; data: CardPage };
    const next = cardPreview(guide.cards[defaultPage.paging.returned] as SystemCard);
    const returned = defaultPage.paging.returned + 1;
    const longer = { ...observed, data: { items: [...observed.data.items, next], paging: { ...observed.data.paging, returned, nextOffset: returned < guide.cards.length ? returned : null } } };
    const longerBytes = Buffer.byteLength(JSON.stringify({ content: [{ type: 'text', text: JSON.stringify(longer) }], structuredContent: longer, isError: false }), 'utf8');
    expect(longerBytes, 'one more preview exceeds the 8 KiB default budget').toBeGreaterThan(DEFAULT_PAGE_TARGET);
  });

  it('limit other than 10 uses the 16 KiB budget, and walking pages covers every card once in order', async () => {
    const guide = manyCards();
    const server = await connectGuide(guide);
    const walked = await walkPages(server, 'list_guide_cards', { limit: 50 });
    expect(walked.error).toBeUndefined();
    expect(walked.ids).toEqual(guide.cards.map(card => card.id));
    for (const page of walked.pages) expect(page.bytes).toBeLessThanOrEqual(MAX_RESULT);
    expect(walked.pages.some(page => page.bytes > DEFAULT_PAGE_TARGET), 'a 16 KiB page is used').toBe(true);
  });

  it('offset at or past the end returns no items and nextOffset null', async () => {
    const guide = linkedGuide();
    const server = await connectGuide(guide);
    const hash = guideResultOf(guide).version;
    const { data } = expectSuccess<CardPage>('list_guide_cards', await server.call('list_guide_cards', { offset: 4, expectedHash: hash }));
    expect(data.items).toEqual([]);
    expect(data.paging).toMatchObject({ total: 4, offset: 4, returned: 0, nextOffset: null });
  });
});

describe('card tool versions and IDs (design 「도구 규칙」 스냅샷·판정 순서)', () => {
  it('list_guide_cards offset > 0 without expectedHash is VERSION_REQUIRED before reading the guide', async () => {
    const server = await connectGuide(linkedGuide());
    const envelope = expectError('list_guide_cards', await server.call('list_guide_cards', { offset: 1 }), 'VERSION_REQUIRED');
    expect(envelope.snapshot).toBeNull();
    expect(server.injected.guide).toBe(0);
  });

  it('a stale expectedHash is VERSION_CONFLICT with details.expectedHash and the guide { hash } on both card tools', async () => {
    const guide = linkedGuide();
    const server = await connectGuide(guide);
    const stale = 'a'.repeat(64);
    for (const [tool, args] of [['list_guide_cards', { offset: 1, expectedHash: stale }], ['get_guide_card', { id: 'server', expectedHash: stale }]] as const) {
      const envelope = expectError(tool, await server.call(tool, args), 'VERSION_CONFLICT');
      expect(envelope.error.details, tool).toEqual({ expectedHash: stale });
      expect(envelope.snapshot, tool).toEqual({ hash: guideResultOf(guide).version });
    }
  });

  it('get_guide_card returns { card, document } with the whole card and its whole document', async () => {
    const guide = linkedGuide();
    const { data } = expectSuccess('get_guide_card', await (await connectGuide(guide)).call('get_guide_card', { id: 'server.network' }));
    expect(data).toEqual({ card: guide.cards[2], document: guide.documents[0] });
  });

  it('get_guide_card returns document: null for a card without a document', async () => {
    const guide = linkedGuide();
    const { data } = expectSuccess('get_guide_card', await (await connectGuide(guide)).call('get_guide_card', { id: 'client' }));
    expect(data).toEqual({ card: guide.cards[1], document: null });
  });

  it('an unknown card ID is NOT_FOUND with the guide { hash }', async () => {
    const guide = linkedGuide();
    const envelope = expectError('get_guide_card', await (await connectGuide(guide)).call('get_guide_card', { id: 'no-such-card' }), 'NOT_FOUND');
    expect(envelope.snapshot).toEqual({ hash: guideResultOf(guide).version });
  });

  it('card tools read the guide only, never the record index (보충 v1.1 Q3)', async () => {
    const server = await connectGuide(linkedGuide());
    expectSuccess('list_guide_cards', await server.call('list_guide_cards', {}));
    expectSuccess('get_guide_card', await server.call('get_guide_card', { id: 'server' }));
    expectError('get_guide_card', await server.call('get_guide_card', { id: 'no-such-card' }), 'NOT_FOUND');
    expect(server.injected.guide).toBe(3);
    expect(server.injected.snapshot).toBe(0);
    expect(server.injected.section).toEqual([]);
    expect(server.injected.checkout).toBe(0);
  });

  it('card tools still answer when the record index is broken (보충 v1.1 Q3 reason)', async () => {
    harness = await connectHarness({
      readSnapshot: async () => { throw new Error('SENTINEL_INDEX_BROKEN'); },
      readGuide: async () => guideResultOf(linkedGuide()),
    });
    expectSuccess('list_guide_cards', await harness.call('list_guide_cards', {}));
  });
});

describe('card tool snapshot equals the app guide version of the same bytes (design 「MCP」 스냅샷)', () => {
  let repository: OwnedTempRepository;
  beforeAll(() => { repository = createOwnedTempRepository(); });
  afterAll(() => repository?.remove());

  for (const [index, space] of [2, 0].entries()) {
    it(`both card tools report { hash } = node:crypto SHA-256 = createSystemGuideStore version (JSON indent ${space})`, async () => {
      const bytes = guideBytes(linkedGuide(), space);
      const path = repository.write(`records/system-guide-${index}.json`, bytes);
      const app = await createSystemGuideStore(path).read();
      expect(app.ok).toBe(true);
      const appVersion = app.ok ? app.version : '';

      // main.ts wiring per design 「MCP 서버 주입 지점」: a new store per request.
      const server = await connectCards(() => createSystemGuideStore(path).read());
      for (const [tool, args] of [['list_guide_cards', {}], ['get_guide_card', { id: 'server' }]] as const) {
        const { snapshot } = expectSuccess(tool, await server.call(tool, args));
        expect(Object.keys(snapshot), tool).toEqual(['hash']);
        expect(snapshot.hash, tool).toBe(sha256(bytes));
        expect(snapshot.hash, tool).toBe(appVersion);
      }
    });
  }
});

describe('the real system-guide.json through the card tools (read only)', () => {
  it('lists every real card in file order with the file SHA-256 and opens the card that has a document', async () => {
    const bytes = readFileSync(REAL_GUIDE_PATH);
    const real = JSON.parse(bytes.toString('utf8')) as SystemGuide;
    const server = await connectCards(() => createSystemGuideStore(REAL_GUIDE_PATH).read());
    const walked = await walkPages(server, 'list_guide_cards', { limit: 50 });
    expect(walked.error).toBeUndefined();
    expect(walked.ids).toEqual(real.cards.map(card => card.id));

    const withDocument = real.cards.find(card => card.documentId);
    expect(withDocument, 'the real guide has a card with a document').toBeDefined();
    const opened = expectSuccess<{ card: SystemCard; document: { id: string } | null }>('get_guide_card', await server.call('get_guide_card', { id: withDocument?.id }));
    expect(opened.snapshot).toEqual({ hash: sha256(bytes) });
    expect(opened.data.card).toEqual(withDocument);
    expect(opened.data.document?.id).toBe(withDocument?.documentId);
  });
});

describe('guide results map to GUIDE_* codes (design 「MCP」 오류 코드 표 카드 rows, 보충 v1.1 Q5)', () => {
  const SENTINEL = 'SENTINEL_GUIDE_MESSAGE C:\\secret\\SENTINEL_PATH\\system-guide.json';
  const failureOf = (code: string) => ({ ok: false, code, message: SENTINEL }) as GuideResult;
  const rows: Array<{ code: string; mcp: ErrorCode; retryable: boolean }> = [
    { code: 'missing', mcp: 'GUIDE_MISSING', retryable: true },
    { code: 'load', mcp: 'GUIDE_UNREADABLE', retryable: true },
    { code: 'too-large', mcp: 'GUIDE_TOO_LARGE', retryable: false },
    { code: 'invalid', mcp: 'GUIDE_INVALID', retryable: false },
    { code: 'changed', mcp: 'GUIDE_CHANGED_DURING_READ', retryable: true },
  ];
  for (const { code, mcp, retryable } of rows) {
    it(`${code} → ${mcp} (retryable ${retryable}) with snapshot null on both card tools`, async () => {
      const server = await connectCards(async () => failureOf(code));
      const messages = new Set<string>();
      for (const [tool, args] of [['list_guide_cards', {}], ['get_guide_card', { id: 'server' }], ['get_guide_card', { id: 'client' }]] as const) {
        const outcome = await server.call(tool, args);
        const envelope = expectError(tool, outcome, mcp);
        expect(envelope.error.retryable).toBe(retryable);
        expect(envelope.snapshot).toBeNull();
        expect(JSON.stringify(outcome.result).includes('SENTINEL'), 'guide message or path leaked').toBe(false);
        messages.add(envelope.error.message);
      }
      expect(messages.size, 'one fixed message per code').toBe(1);
    });
  }

  for (const code of ['busy', 'denied']) {
    it(`${code} cannot occur on the MCP path and becomes CATALOG_UNREADABLE with one fixed diagnostic line`, async () => {
      const writes: string[] = [];
      vi.spyOn(process.stderr, 'write').mockImplementation(((chunk: string | Uint8Array) => { writes.push(String(chunk)); return true; }) as typeof process.stderr.write);
      const server = await connectCards(async () => failureOf(code));
      const outcome = await server.call('list_guide_cards', {});
      expectError('list_guide_cards', outcome, 'CATALOG_UNREADABLE');
      expect(writes).toHaveLength(1);
      expect(writes[0]?.includes('SENTINEL')).toBe(false);
      expect(JSON.stringify(outcome.result).includes('SENTINEL')).toBe(false);
    });
  }
});

// @vitest-environment node
// Requirement: goal 「만들 것」 4 (MCP reads registered source sections through the app's boundary
// module) and index-v2-design.md 「MCP」 tool table, 「도구 규칙」 (read_source_section, 페이지,
// 판정 순서) and 오류 코드 표, with Astra 보충 v1.1 (msg_6dd9f66e2ca7) Q4·Q5.
// readSourceSection and readCheckout are injected stubs here so every source result code can be
// produced; the owned-TEMP real-store cases live in mcp-source-section-rejections.test.ts.
// Expected hashes use node:crypto; page boundaries are checked against the design's response-size
// definition computed in this file, never against product paging code.
import { afterEach, describe, expect, it, vi } from 'vitest';
import type { RecordCatalog, RecordSource } from '../electron/catalog-contract';
import type { CheckoutInfo } from '../electron/checkout-contract';
import type { SourceSectionResult } from '../electron/source-section-contract';
import {
  ControlledCatalogFile, KNOWN_CHECKOUT, MAX_RESULT, authoredBytes, connectHarness, expectEnvelope, expectError, expectSuccess,
  hasLoneSurrogate, makeCatalog, makeSource, makeSystem, sectionResultOf, sha256, staticSnapshot,
  type CallOutcome, type ErrorCode, type Harness, type ReadCheckout, type ReadSourceSection, type SectionPaging,
} from './mcp-fixtures';

const GUIDE = makeSource('guide', { title: '안내 문서', locator: 'docs/guide.md', section: '개요' });
const WHOLE = makeSource('whole', { title: '파일 처음부터', locator: 'docs/whole.md', section: null });
const LOCAL = makeSource('local-note', { title: '로컬 메모', kind: 'local', availability: 'local-only', locator: '.backups/note.md', section: null });

function sectionCatalog(...extra: RecordSource[]): RecordCatalog {
  return makeCatalog({ sources: [GUIDE, WHOLE, LOCAL, ...extra], systems: [makeSystem('records-view', { sourceIds: ['guide'] })] });
}

let harness: Harness | undefined;
afterEach(async () => {
  await harness?.close();
  harness = undefined;
  vi.restoreAllMocks();
});

interface SectionData {
  source: RecordSource; path: string; heading: string | null; text: string; sectionHash: string;
  checkout: CheckoutInfo; paging: SectionPaging;
}

async function connectSections(options: {
  catalog?: RecordCatalog;
  text?: string | ((source: RecordSource) => string);
  readSourceSection?: ReadSourceSection;
  readCheckout?: ReadCheckout;
  now?: () => number;
} = {}) {
  const catalog = options.catalog ?? sectionCatalog();
  const snapshot = staticSnapshot(catalog);
  const textOf = (source: RecordSource) => typeof options.text === 'function' ? options.text(source) : options.text ?? `# ${source.section ?? source.title}\n본문\n`;
  harness = await connectHarness({
    readSnapshot: async () => snapshot,
    readSourceSection: options.readSourceSection ?? (async source => sectionResultOf(source, textOf(source))),
    ...(options.readCheckout ? { readCheckout: options.readCheckout } : {}),
    ...(options.now ? { now: options.now } : {}),
  });
  return { harness, catalogHash: snapshot.metadata.hash };
}

const sectionData = (outcome: CallOutcome) => expectSuccess<SectionData>('read_source_section', outcome);

// ---------------------------------------------------------------- response size by definition

// Design 「도구 규칙」 페이지: the whole response measured like the existing responseBytes —
// the serialized `{ content: [{ type: 'text', text: JSON(envelope) }], structuredContent, isError }`.
function responseSize(envelope: unknown): number {
  return Buffer.byteLength(JSON.stringify({ content: [{ type: 'text', text: JSON.stringify(envelope) }], structuredContent: envelope, isError: false }), 'utf8');
}

function envelopeWithFragment(observed: { ok: true; snapshot: unknown; data: SectionData }, full: string, offset: number, length: number) {
  const end = offset + length;
  return {
    ...observed,
    data: { ...observed.data, text: full.slice(offset, end), paging: { offset, returned: length, total: full.length, nextOffset: end < full.length ? end : null } },
  };
}

const isHigh = (code: number) => code >= 0xd800 && code <= 0xdbff;
const isLow = (code: number) => code >= 0xdc00 && code <= 0xdfff;
// Code units of the next whole character after `end` (a surrogate pair counts as two).
function nextCharacterLength(full: string, end: number): number {
  return isHigh(full.charCodeAt(end)) && isLow(full.charCodeAt(end + 1)) ? 2 : 1;
}

// Walks every fragment with the first page's sectionHash and checks each page against the
// design: ≤ 16 KiB, no split pair, and the longest fragment (one more character would not fit).
async function walkSection(server: Harness, id: string, full: string) {
  const fragments: string[] = [];
  let offset = 0;
  let expectedSectionHash: string | undefined;
  for (let page = 0; page < 1_000; page += 1) {
    const outcome = await server.call('read_source_section', { id, offset, ...(expectedSectionHash ? { expectedSectionHash } : {}) });
    const { data } = sectionData(outcome);
    const observed = outcome.authored?.structuredContent as { ok: true; snapshot: unknown; data: SectionData };
    expect(responseSize(observed), 'size definition matches the observed response').toBe(authoredBytes(outcome));
    expect(data.paging.offset).toBe(offset);
    expect(data.paging.total).toBe(full.length);
    expect(data.text).toBe(full.slice(offset, offset + data.paging.returned));
    expect(hasLoneSurrogate(data.text), `page ${page} splits a surrogate pair`).toBe(false);
    expectedSectionHash ??= data.sectionHash;
    expect(data.sectionHash).toBe(expectedSectionHash);
    fragments.push(data.text);
    if (data.paging.nextOffset === null) {
      expect(offset + data.paging.returned).toBe(full.length);
      return { fragments, pages: page + 1, sectionHash: expectedSectionHash };
    }
    expect(data.paging.returned, `page ${page} made progress`).toBeGreaterThan(0);
    const longer = data.paging.returned + nextCharacterLength(full, offset + data.paging.returned);
    expect(responseSize(envelopeWithFragment(observed, full, offset, longer)), `page ${page} is the longest fitting fragment`).toBeGreaterThan(MAX_RESULT);
    offset = data.paging.nextOffset;
  }
  throw new Error('section paging did not terminate');
}

// ---------------------------------------------------------------- success shape

describe('read_source_section success result (design 「MCP」 tool table)', () => {
  it('returns { source, path, heading, text, sectionHash, checkout, paging } for a short section', async () => {
    const text = '# 개요\n짧은 본문 😀\n';
    const { harness: server } = await connectSections({ text });
    const { data } = sectionData(await server.call('read_source_section', { id: 'guide' }));
    expect(data).toEqual({
      source: GUIDE, path: GUIDE.locator, heading: '개요', text,
      sectionHash: sha256(Buffer.from(text, 'utf8')), checkout: KNOWN_CHECKOUT,
      paging: { offset: 0, returned: text.length, total: text.length, nextOffset: null },
    });
  });

  it('passes a null heading through for a source whose section is null (file from the start)', async () => {
    const text = '파일 첫 줄\n# 제목\n본문\n';
    const { harness: server } = await connectSections({ text });
    const { data } = sectionData(await server.call('read_source_section', { id: 'whole' }));
    expect(data.heading).toBeNull();
    expect(data.path).toBe(WHOLE.locator);
    expect(data.text).toBe(text);
  });

  it('returns the same source object as get_source', async () => {
    const { harness: server } = await connectSections();
    const viaSource = expectSuccess<{ source: RecordSource }>('get_source', await server.call('get_source', { id: 'guide' })).data.source;
    expect(sectionData(await server.call('read_source_section', { id: 'guide' })).data.source).toEqual(viaSource);
  });

  it('passes the readSourceSection source argument from the snapshot, found by ID', async () => {
    const { harness: server } = await connectSections();
    await server.call('read_source_section', { id: 'whole' });
    expect(server.injected.section).toEqual([WHOLE]);
  });

  it('copies readCheckout results as they are: known branch, detached HEAD and unknown reasons', async () => {
    const checkouts: CheckoutInfo[] = [
      { state: 'known', branch: 'feat/records', head: 'a'.repeat(40) },
      { state: 'known', branch: null, head: 'b'.repeat(64) },
      { state: 'unknown', reason: 'backlink-mismatch' },
      { state: 'unknown', reason: 'no-git' },
    ];
    for (const checkout of checkouts) {
      const { harness: server } = await connectSections({ readCheckout: async () => checkout });
      expect(sectionData(await server.call('read_source_section', { id: 'guide' })).data.checkout).toEqual(checkout);
      await harness?.close();
      harness = undefined;
    }
  });

  it('sectionHash is the UTF-8 SHA-256 of the whole section text on every page', async () => {
    const full = `# 개요\n${'가나다라 😀 '.repeat(4_000)}\n`;
    const { harness: server } = await connectSections({ text: full });
    const walked = await walkSection(server, 'guide', full);
    expect(walked.pages).toBeGreaterThan(1);
    expect(walked.sectionHash).toBe(sha256(Buffer.from(full, 'utf8')));
  });
});

// ---------------------------------------------------------------- paging

describe('read_source_section paging in UTF-16 code units (design 「도구 규칙」 페이지)', () => {
  const texts: Array<{ label: string; full: string }> = [
    { label: 'ASCII text', full: `# 개요\n${'a'.repeat(40_000)}\n` },
    { label: 'surrogate pairs at shifting positions', full: `# 개요\n${'a😀'.repeat(9_000)}b\n` },
    { label: 'only surrogate pairs', full: '😀'.repeat(12_001) },
    { label: 'escape-heavy text', full: `# 개요\n${'"\\\n\t</script>\u2028'.repeat(3_000)}` },
  ];
  for (const { label, full } of texts) {
    it(`walks every fragment within 16 KiB, longest-first and pair-safe: ${label}`, async () => {
      const { harness: server } = await connectSections({ text: full });
      const walked = await walkSection(server, 'guide', full);
      expect(walked.fragments.join('')).toBe(full);
      expect(walked.pages).toBeGreaterThan(1);
    });
  }

  it('refuses an offset inside a surrogate pair with INVALID_ARGUMENT, the index { hash } and no checkout read (보충 v1.2 Q7)', async () => {
    const full = `ab😀${'c'.repeat(10)}`;
    const { harness: server, catalogHash } = await connectSections({ text: full });
    const expectedSectionHash = sha256(Buffer.from(full, 'utf8'));
    const refused = expectError('read_source_section', await server.call('read_source_section', { id: 'guide', offset: 3, expectedSectionHash }), 'INVALID_ARGUMENT');
    expect(refused.snapshot).toEqual({ hash: catalogHash });
    expect(server.injected.checkout).toBe(0);

    const atPairStart = sectionData(await server.call('read_source_section', { id: 'guide', offset: 2, expectedSectionHash })).data;
    expect(atPairStart.text).toBe(full.slice(2));
  });

  it('compares expectedSectionHash before the surrogate offset check (보충 v1.2 Q7 order)', async () => {
    const full = `ab😀${'c'.repeat(10)}`;
    const { harness: server } = await connectSections({ text: full });
    const stale = 'c'.repeat(64);
    const envelope = expectError('read_source_section', await server.call('read_source_section', { id: 'guide', offset: 3, expectedSectionHash: stale }), 'VERSION_CONFLICT');
    expect(envelope.error.details).toEqual({ expectedSectionHash: stale });
  });

  it('returns an empty last fragment when offset is at or past the end', async () => {
    const full = '# 개요\n본문\n';
    const { harness: server } = await connectSections({ text: full });
    const expectedSectionHash = sha256(Buffer.from(full, 'utf8'));
    for (const offset of [full.length, full.length + 5]) {
      const { data } = sectionData(await server.call('read_source_section', { id: 'guide', offset, expectedSectionHash }));
      expect(data.text).toBe('');
      expect(data.paging).toEqual({ offset, returned: 0, total: full.length, nextOffset: null });
    }
  });

  it('returns RESPONSE_TOO_LARGE when even an empty fragment exceeds 16 KiB', async () => {
    const huge = makeSource('huge-title', { title: 'T'.repeat(9_000), locator: 'docs/huge.md', section: '개요' });
    const { harness: server } = await connectSections({ catalog: sectionCatalog(huge), text: '# 개요\n본문\n' });
    expectError('read_source_section', await server.call('read_source_section', { id: 'huge-title' }), 'RESPONSE_TOO_LARGE');
  });

  it('returns RESPONSE_TOO_LARGE when offset < total but not one character fits', async () => {
    const full = '😀😀';
    const sectionHash = sha256(Buffer.from(full, 'utf8'));
    // Find a title length whose empty fragment fits but whose first character does not, by the
    // design's size definition (checkout and snapshot hash have fixed sizes in this fixture).
    const envelopeFor = (title: string, text: string) => {
      const source = makeSource('tight', { title, locator: 'docs/tight.md', section: '개요' });
      return {
        ok: true, snapshot: { hash: 'f'.repeat(64) },
        data: {
          source, path: source.locator, heading: '개요', text, sectionHash, checkout: KNOWN_CHECKOUT,
          paging: { offset: 0, returned: text.length, total: full.length, nextOffset: text.length < full.length ? text.length : null },
        },
      };
    };
    let titleLength = 0;
    for (let length = 9_000; length > 0; length -= 1) {
      const title = 'T'.repeat(length);
      if (responseSize(envelopeFor(title, '')) <= MAX_RESULT && responseSize(envelopeFor(title, '😀')) > MAX_RESULT) { titleLength = length; break; }
    }
    expect(titleLength, 'a tight title length exists').toBeGreaterThan(0);
    const tight = makeSource('tight', { title: 'T'.repeat(titleLength), locator: 'docs/tight.md', section: '개요' });
    const { harness: server } = await connectSections({ catalog: sectionCatalog(tight), text: full });
    expectError('read_source_section', await server.call('read_source_section', { id: 'tight' }), 'RESPONSE_TOO_LARGE');
  });
});

// ---------------------------------------------------------------- versions and IDs

describe('read_source_section versions and IDs (design 「도구 규칙」)', () => {
  it('offset > 0 without expectedSectionHash is VERSION_REQUIRED before any read, even with expectedHash', async () => {
    const { harness: server, catalogHash } = await connectSections();
    for (const args of [{ id: 'guide', offset: 1 }, { id: 'guide', offset: 1, expectedHash: catalogHash }]) {
      const envelope = expectError('read_source_section', await server.call('read_source_section', args), 'VERSION_REQUIRED');
      expect(envelope.snapshot).toBeNull();
    }
    expect(server.injected.snapshot).toBe(0);
    expect(server.injected.section).toEqual([]);
    expect(server.injected.checkout).toBe(0);
  });

  it('offset 0 needs no expectedSectionHash', async () => {
    const { harness: server } = await connectSections();
    sectionData(await server.call('read_source_section', { id: 'guide', offset: 0 }));
  });

  it('a different expectedSectionHash is VERSION_CONFLICT with details.expectedSectionHash, the index { hash } and no checkout read', async () => {
    const { harness: server, catalogHash } = await connectSections({ text: `# 개요\n${'x'.repeat(100)}\n` });
    const stale = 'e'.repeat(64);
    const envelope = expectError('read_source_section', await server.call('read_source_section', { id: 'guide', offset: 5, expectedSectionHash: stale }), 'VERSION_CONFLICT');
    expect(envelope.error.details).toEqual({ expectedSectionHash: stale });
    expect(envelope.snapshot).toEqual({ hash: catalogHash });
    expect(server.injected.section).toHaveLength(1);
    expect(server.injected.checkout).toBe(0);
  });

  it('a different index expectedHash is VERSION_CONFLICT with details.expectedHash before the section is read', async () => {
    const { harness: server, catalogHash } = await connectSections();
    const stale = 'd'.repeat(64);
    const envelope = expectError('read_source_section', await server.call('read_source_section', { id: 'guide', expectedHash: stale }), 'VERSION_CONFLICT');
    expect(envelope.error.details).toEqual({ expectedHash: stale });
    expect(envelope.snapshot).toEqual({ hash: catalogHash });
    expect(server.injected.section).toEqual([]);
  });

  it('the matching index expectedHash reads normally', async () => {
    const { harness: server, catalogHash } = await connectSections();
    const { snapshot } = sectionData(await server.call('read_source_section', { id: 'guide', expectedHash: catalogHash }));
    expect(snapshot).toEqual({ hash: catalogHash });
  });

  it('an unregistered source ID is NOT_FOUND without reading a section', async () => {
    const { harness: server, catalogHash } = await connectSections();
    const envelope = expectError('read_source_section', await server.call('read_source_section', { id: 'no-such-source' }), 'NOT_FOUND');
    expect(envelope.snapshot).toEqual({ hash: catalogHash });
    expect(server.injected.section).toEqual([]);
    expect(server.injected.checkout).toBe(0);
  });
});

// ---------------------------------------------------------------- source result mapping

const SENTINEL_MESSAGE = 'SENTINEL_STORE_MESSAGE C:\\secret\\SENTINEL_PATH\\guide.md';
const failureOf = (code: string, reason: string | null): SourceSectionResult =>
  ({ ok: false, code, reason, message: SENTINEL_MESSAGE }) as SourceSectionResult;

// Design 「MCP」 오류 코드 표, rows for 원문 results (code → MCP code, reason copied to details.reason).
const mappedResults: Array<{ code: string; reasons: Array<string | null>; mcp: ErrorCode; retryable: boolean }> = [
  { code: 'not-readable', reasons: ['local-only', 'handoff', 'extension', 'not-file'], mcp: 'SOURCE_NOT_READABLE', retryable: false },
  { code: 'path-rejected', reasons: ['drive', 'absolute', 'parent', 'invalid', 'link', 'case'], mcp: 'SOURCE_PATH_REJECTED', retryable: false },
  { code: 'missing', reasons: [null], mcp: 'SOURCE_MISSING', retryable: false },
  { code: 'too-large', reasons: ['file', 'section'], mcp: 'SOURCE_TOO_LARGE', retryable: false },
  { code: 'changed', reasons: [null], mcp: 'SOURCE_CHANGED_DURING_READ', retryable: true },
  { code: 'invalid-encoding', reasons: [null], mcp: 'SOURCE_INVALID_ENCODING', retryable: false },
  { code: 'section-missing', reasons: [null], mcp: 'SECTION_MISSING', retryable: false },
  { code: 'section-ambiguous', reasons: [null], mcp: 'SECTION_AMBIGUOUS', retryable: false },
  // Astra 보충 v1.2 Q6 (msg_e854e8f17079): unexpected source I/O failure.
  { code: 'load', reasons: [null], mcp: 'SOURCE_UNREADABLE', retryable: true },
];

describe('source results map to the design error table (design 「MCP」 오류 코드 표, 보충 v1.1 Q4·Q5)', () => {
  for (const { code, reasons, mcp, retryable } of mappedResults) {
    for (const reason of reasons) {
      it(`${code}${reason ? `·${reason}` : ''} → ${mcp} (retryable ${retryable})`, async () => {
        const { harness: server, catalogHash } = await connectSections({ readSourceSection: async () => failureOf(code, reason) });
        const outcome = await server.call('read_source_section', { id: 'guide' });
        const envelope = expectError('read_source_section', outcome, mcp);
        const wire = JSON.stringify(outcome.result);

        expect(envelope.error.retryable).toBe(retryable);
        expect(envelope.snapshot).toEqual({ hash: catalogHash });
        if (reason === null) expect('details' in envelope.error).toBe(false);
        else expect(envelope.error.details).toEqual({ reason });
        expect(wire.includes('SENTINEL'), 'store message, path or locator leaked').toBe(false);
        expect(wire.includes(GUIDE.locator)).toBe(false);
        expect(server.injected.checkout, 'checkout is read only for a successful response').toBe(0);
      });
    }
  }

  it('each new source code has one fixed message whatever the reason, source or input', async () => {
    for (const { code, reasons, mcp } of mappedResults) {
      const messages = new Set<string>();
      for (const reason of reasons) {
        for (const id of ['guide', 'whole', 'local-note']) {
          const { harness: server } = await connectSections({ readSourceSection: async () => failureOf(code, reason) });
          messages.add(expectError('read_source_section', await server.call('read_source_section', { id }), mcp).error.message);
          await harness?.close();
          harness = undefined;
        }
      }
      expect(messages.size, mcp).toBe(1);
    }
  });
});

describe('source results that cannot occur on the MCP path (design 「MCP」 오류 코드 표 아래 문단)', () => {
  for (const code of ['invalid-request', 'index-unavailable', 'unknown-source', 'denied']) {
    it(`${code} becomes CATALOG_UNREADABLE with one fixed diagnostic line and no echo`, async () => {
      const writes: string[] = [];
      vi.spyOn(process.stderr, 'write').mockImplementation(((chunk: string | Uint8Array) => { writes.push(String(chunk)); return true; }) as typeof process.stderr.write);
      const { harness: server } = await connectSections({ readSourceSection: async () => failureOf(code, null) });
      const outcome = await server.call('read_source_section', { id: 'guide' });
      expectError('read_source_section', outcome, 'CATALOG_UNREADABLE');

      expect(writes).toHaveLength(1);
      expect(writes[0]?.includes('SENTINEL')).toBe(false);
      expect(writes[0]?.includes(GUIDE.locator)).toBe(false);
      expect(JSON.stringify(outcome.result).includes('SENTINEL')).toBe(false);
    });
  }
});

// ---------------------------------------------------------------- decision order

describe('read_source_section decision order (design 「도구 규칙」 판정 순서)', () => {
  it('admission precedes the version check: the 11th call in a frozen clock is RATE_LIMITED, not VERSION_REQUIRED', async () => {
    const { harness: server } = await connectSections({ now: () => 0 });
    for (let call = 0; call < 10; call += 1) sectionData(await server.call('read_source_section', { id: 'guide' }));
    expectError('read_source_section', await server.call('read_source_section', { id: 'guide', offset: 4 }), 'RATE_LIMITED');
  });

  it('the version check precedes the data read: a broken index still answers VERSION_REQUIRED', async () => {
    const file = new ControlledCatalogFile(new TextEncoder().encode('{ not json'));
    const reader = file.reader();
    harness = await connectHarness({ readSnapshot: signal => reader.readSnapshot(signal) });
    expectError('read_source_section', await harness.call('read_source_section', { id: 'guide', offset: 4 }), 'VERSION_REQUIRED');
    expect(file.opened).toBe(0);
  });

  it('the data read precedes expectedHash: a broken index answers its read error, not VERSION_CONFLICT', async () => {
    const file = new ControlledCatalogFile(new TextEncoder().encode('{ not json'));
    const reader = file.reader();
    harness = await connectHarness({ readSnapshot: signal => reader.readSnapshot(signal) });
    expectError('read_source_section', await harness.call('read_source_section', { id: 'guide', expectedHash: 'a'.repeat(64) }), 'CATALOG_INVALID');
    expect(harness.injected.section).toEqual([]);
  });

  it('expectedHash precedes the ID lookup: an unknown ID with a stale hash is VERSION_CONFLICT', async () => {
    const { harness: server } = await connectSections();
    expectError('read_source_section', await server.call('read_source_section', { id: 'no-such-source', expectedHash: 'b'.repeat(64) }), 'VERSION_CONFLICT');
  });

  it('the ID lookup precedes the tool step: NOT_FOUND never calls readSourceSection', async () => {
    const readSourceSection = vi.fn<ReadSourceSection>(async source => sectionResultOf(source, 'x'));
    const { harness: server } = await connectSections({ readSourceSection });
    expectError('read_source_section', await server.call('read_source_section', { id: 'no-such-source' }), 'NOT_FOUND');
    expect(readSourceSection).not.toHaveBeenCalled();
  });

  it('a successful read calls readSourceSection before readCheckout, once each', async () => {
    const { harness: server } = await connectSections();
    sectionData(await server.call('read_source_section', { id: 'guide' }));
    expect(server.injected.order).toEqual(['snapshot', 'section', 'checkout']);
  });

  it('the source tool never reads the guide (보충 v1.1 Q3)', async () => {
    const { harness: server } = await connectSections();
    sectionData(await server.call('read_source_section', { id: 'guide' }));
    expectError('read_source_section', await server.call('read_source_section', { id: 'no-such-source' }), 'NOT_FOUND');
    expect(server.injected.guide).toBe(0);
  });

  it('every source result keeps the envelope contract', async () => {
    const { harness: server } = await connectSections();
    expectEnvelope('read_source_section', await server.call('read_source_section', { id: 'local-note' }));
  });
});

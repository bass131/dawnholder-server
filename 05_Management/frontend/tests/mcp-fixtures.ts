// Independent fixtures and harness for the shared-read MCP verification.
// Contract values come from the record index v2 design (index-v2-design.md 「MCP」,
// 「색인 형식」, 「화면」 검색) and goal 「만들 것」 4, not from the product's own
// schemas, so a product drift fails here instead of being mirrored.
import { createHash } from 'node:crypto';
import { expect } from 'vitest';
import { z } from 'zod';
import { Client } from '@modelcontextprotocol/client';
import { InMemoryTransport, type McpServer } from '@modelcontextprotocol/server';
import { serveStdio } from '@modelcontextprotocol/server/stdio';
import type { DevelopmentRecord, RecordCatalog, RecordSource, SystemRecord } from '../electron/catalog-contract';
import type { CheckoutInfo } from '../electron/checkout-contract';
import type { SourceSectionResult } from '../electron/source-section-contract';
import type { GuideResult, ImplementationDocument, SystemCard, SystemGuide } from '../electron/system-guide-contract';
import { createCatalogReader, type CatalogFileOperations, type CatalogFileStat, type CatalogSnapshot } from '../mcp/catalog-reader';
import { createCatalogServer } from '../mcp/catalog-server';

export const MAX_CATALOG = 2 * 1024 * 1024;
export const MAX_RESULT = 16 * 1024;
export const DEFAULT_PAGE_TARGET = 8 * 1024;
export const MAX_REFUSAL = 1024;
export const MAX_SECTION_OFFSET = 262_144;
export const PREVIEW_TITLE_LIMIT = 80;
export const RECORD_TOOL_NAMES = ['list_systems', 'search_records', 'get_system', 'get_record', 'get_source'] as const;
export const GUIDE_TOOL_NAMES = ['list_guide_cards', 'get_guide_card'] as const;
// Design 「MCP」 tool table: the five record tools plus three new tools.
export const TOOL_NAMES = [...RECORD_TOOL_NAMES, 'read_source_section', ...GUIDE_TOOL_NAMES] as const;
export type ToolName = typeof TOOL_NAMES[number];
export type ListToolName = 'list_systems' | 'search_records' | 'list_guide_cards';
export const FIXTURE_PATH = 'C:\\v1-fixture\\SENTINEL_PATH\\catalog.json';

// ---------------------------------------------------------------- catalog builders (index v2)

export const MERGE_COMMIT = '0123456789abcdef0123456789abcdef01234567';

export function makeSource(id: string, extra: Partial<RecordSource> = {}): RecordSource {
  return { id, title: `출처 ${id}`, kind: 'git', locator: `docs/${id}.md`, section: `구간 ${id}`, availability: 'versioned', ...extra };
}

export function makeSystem(id: string, extra: Partial<SystemRecord> = {}): SystemRecord {
  return { id, title: `시스템 ${id}`, area: '게임 기반', sourceIds: [], relatedSystemIds: [], recordIds: [], ...extra };
}

export function makeRecord(id: string, extra: Partial<DevelopmentRecord> = {}): DevelopmentRecord {
  return { id, type: '변경', title: `기록 ${id}`, systemIds: [], sourceIds: [], pullRequests: [], ...extra };
}

export function makeCatalog(parts: Partial<RecordCatalog> = {}): RecordCatalog {
  return { schemaVersion: 2, sources: [], systems: [], records: [], ...parts };
}

// A small linked v2 catalog. Valid IDs only (design 「색인 형식」); Korean, emoji,
// escaping and locale-sensitive text live in titles and locators instead. U+2028/U+2029
// stay inside titles because the v2 contract rejects titles that trim() would change.
export function linkedCatalog(): RecordCatalog {
  return makeCatalog({
    sources: [
      makeSource('src-a', { title: 'Alpha 근거 "따옴표" \\ 역슬래시', locator: 'docs/SENTINEL_LOCATOR/evidence.md', section: '판정 😀' }),
      makeSource('src-b', { title: 'Beta 전달', kind: 'handoff', availability: 'local-only', locator: 'msg_0123456789ab', section: null }),
      makeSource('src-esc', { title: '</script>\u2028\u2029 로컬', kind: 'local', availability: 'local-only', locator: '.backups/SENTINEL_LOCAL/notes.md', section: null }),
    ],
    systems: [
      makeSystem('sys-alpha', { title: 'Alpha 전투 "엔진" 😀', area: '게임 기반', sourceIds: ['src-a'], relatedSystemIds: ['sys-beta'], recordIds: ['rec-1', 'rec-2'] }),
      makeSystem('sys-beta', { title: 'Beta 네트워크', area: '서버 플랫폼', sourceIds: ['src-b', 'src-esc'], relatedSystemIds: ['sys-alpha'], recordIds: ['rec-2', 'rec-3'] }),
      makeSystem('sys-gamma', { title: 'İstanbul GAMMA', area: 'Management', recordIds: ['rec-10'] }),
      makeSystem('sys-emoji', { title: 'Emoji 😀 시스템', area: '게임 기반' }),
    ],
    records: [
      makeRecord('rec-1', { type: '변경', title: 'Alpha \u2028 변경 </script>', systemIds: ['sys-alpha'], sourceIds: ['src-a'], pullRequests: [{ number: 101, mergeCommit: MERGE_COMMIT }] }),
      makeRecord('rec-2', { type: '결정', title: '동기화 결정 Alpha Beta', systemIds: ['sys-alpha', 'sys-beta'], sourceIds: ['src-b'], pullRequests: [{ number: 7, mergeCommit: 'a'.repeat(40) }, { number: 102, mergeCommit: 'b'.repeat(40) }] }),
      makeRecord('rec-3', { type: '검증', title: 'Beta 패킷 검증', systemIds: ['sys-beta'] }),
      makeRecord('rec-4', { type: '계획', title: '독립 계획', systemIds: [] }),
      makeRecord('rec-10', { type: '변경', title: 'Gamma 변경 istanbul', systemIds: ['sys-gamma'] }),
    ],
  });
}

export function catalogBytes(catalog: unknown, space = 2): Uint8Array {
  return new TextEncoder().encode(JSON.stringify(catalog, null, space));
}

// Appends one padding source whose ASCII title makes the encoded catalog exactly
// `size` bytes. v2 has no free-text top-level field, so a source title carries it.
export function catalogOfExactSize(size: number, base: RecordCatalog = makeCatalog()): Uint8Array {
  const padded = (length: number) => catalogBytes({
    ...base,
    sources: [...base.sources, makeSource('pad', { title: 'x'.repeat(length), section: null })],
  });
  const smallest = padded(1).byteLength;
  if (size < smallest) throw new Error(`fixture size ${size} < smallest ${smallest}`);
  const bytes = padded(1 + size - smallest);
  if (bytes.byteLength !== size) throw new Error(`fixture size ${bytes.byteLength} != ${size}`);
  return bytes;
}

export const sha256 = (bytes: Uint8Array | string) => createHash('sha256').update(bytes).digest('hex');
// Design 「MCP」 스냅샷: decode as UTF-8 text, re-encode as UTF-8, then SHA-256 (independent oracle).
export const decodedTextHash = (bytes: Uint8Array) => sha256(Buffer.from(Buffer.from(bytes).toString('utf8'), 'utf8'));

// Independent UTF-16 code unit comparison (does not reuse the product comparator).
export function codeUnitCompare(a: string, b: string): number {
  const length = Math.min(a.length, b.length);
  for (let index = 0; index < length; index += 1) {
    const diff = a.charCodeAt(index) - b.charCodeAt(index);
    if (diff !== 0) return diff;
  }
  return a.length - b.length;
}

// Preview cut by definition (design 「도구 규칙」, Astra 보충 v1.1 Q2): 80 UTF-16 code
// units, one fewer when the cut would split a surrogate pair.
export function previewByDefinition(text: string, limit = PREVIEW_TITLE_LIMIT): string {
  if (text.length <= limit) return text;
  const splitsPair = /[\uD800-\uDBFF]/.test(text[limit - 1] ?? '') && /[\uDC00-\uDFFF]/.test(text[limit] ?? '');
  return text.slice(0, splitsPair ? limit - 1 : limit);
}

// Verbatim baseline term matching from `git show 18c8ca6:05_Management/frontend/src/recordCatalog.ts`.
export function baselineMatchesQuery(query: string, values: string[]): boolean {
  const terms = query.trim().toLocaleLowerCase().split(/\s+/).filter(Boolean);
  const text = values.join(' ').toLocaleLowerCase();
  return terms.every(term => text.includes(term));
}
function linkedSourceTitles(catalog: RecordCatalog, ids: string[]): string[] {
  return ids.flatMap(id => catalog.sources.filter(source => source.id === id).map(source => source.title));
}
// Design 「화면」 검색: systems by ID·title·area·linked source titles; records by
// ID·title·`#PR번호`·linked source titles. MCP search keeps parity with the screen.
export function screenFilterSystems(catalog: RecordCatalog, query: string, area: string): SystemRecord[] {
  return catalog.systems.filter(system => (!area || system.area === area)
    && baselineMatchesQuery(query, [system.id, system.title, system.area, ...linkedSourceTitles(catalog, system.sourceIds)]));
}
export function screenFilterRecords(catalog: RecordCatalog, query: string, area: string, type: string): DevelopmentRecord[] {
  return catalog.records.filter(record => (!type || record.type === type)
    && (!area || record.systemIds.some(id => catalog.systems.some(system => system.id === id && system.area === area)))
    && baselineMatchesQuery(query, [record.id, record.title, ...record.pullRequests.map(pr => `#${pr.number}`), ...linkedSourceTitles(catalog, record.sourceIds)]));
}

// ---------------------------------------------------------------- guide and source fixtures

export function makeCard(id: string, extra: Partial<SystemCard> = {}): SystemCard {
  return { id, parentId: null, title: `카드 ${id}`, summary: `요약 ${id}`, relatedSystemIds: [], ...extra };
}

export function makeDocument(id: string, cardId: string, extra: Partial<ImplementationDocument> = {}): ImplementationDocument {
  return {
    id, cardId, title: `문서 ${id}`, sourceCommit: MERGE_COMMIT, sourceRefs: [{ path: '00_Server/Program.cs', symbol: 'Main' }],
    sections: [{ id: 'flow', title: '흐름', blocks: [{ type: 'paragraph', text: `문서 ${id} 본문` }] }], ...extra,
  };
}

export function makeGuide(parts: Partial<SystemGuide> = {}): SystemGuide {
  return {
    schemaVersion: 1, revision: 'guide-fixture-r1', asOf: '2026-10-01', coverage: 'representative-diagrams',
    coverageNote: '시험용 카드 자료', cards: [], documents: [], ...parts,
  };
}

// Cards in a deliberately non-sorted array order (design: 순서는 카드 자료의 배열 순서). Valid for
// the app's readSystemGuide: the child card has a status, a document and a covering code reference.
export function linkedGuide(): SystemGuide {
  return makeGuide({
    cards: [
      makeCard('server', { title: '서버 Server', summary: '게임 서버 프로세스', relatedSystemIds: ['sys-alpha'] }),
      makeCard('client', { title: '클라이언트', summary: 'Unity 화면과 입력 😀' }),
      makeCard('server.network', {
        parentId: 'server', title: '네트워크 수신', summary: '패킷 수신 경로', relatedSystemIds: ['sys-beta'], status: 'code-present', documentId: 'server.network.receive',
        codeReference: { commitSha: MERGE_COMMIT, mappings: [{ path: '00_Server/Program.cs', kind: 'file', role: '수신 진입점' }] },
      }),
      makeCard('automation', { title: 'İstanbul 자동화', summary: '운영 스크립트' }),
    ],
    documents: [makeDocument('server.network.receive', 'server.network')],
  });
}

export function guideBytes(guide: unknown, space = 2): Uint8Array {
  return new TextEncoder().encode(JSON.stringify(guide, null, space));
}

// A successful GuideResult for in-memory tests. `version` follows the design rule
// (UTF-8 text SHA-256 of the bytes the app store would have read).
export function guideResultOf(guide: SystemGuide): Extract<GuideResult, { ok: true }> {
  const bytes = guideBytes(guide);
  return { ok: true, guide, version: sha256(bytes), bytes: bytes.byteLength };
}

export const KNOWN_CHECKOUT: CheckoutInfo = { state: 'known', branch: 'feat/fixture-branch', head: 'c0ffee00'.repeat(5) };

export function sectionResultOf(source: RecordSource, text: string, heading: string | null = source.section): SourceSectionResult {
  return { ok: true, sourceId: source.id, path: source.locator, heading, text, bytes: Buffer.byteLength(text, 'utf8') };
}

// ---------------------------------------------------------------- controlled file I/O

export type FileOp = 'open' | 'stat' | 'read' | 'close';
export interface FakeHandle { readonly id: number }
export interface Pause { readonly reached: Promise<void>; release(): void }
interface QueuedPause { abortAware: boolean; markReached(): void; wait: Promise<void> }

function deferred() {
  let resolve!: () => void;
  const promise = new Promise<void>(done => { resolve = done; });
  return { promise, resolve };
}

export function abortError(): Error {
  return Object.assign(new Error('aborted at C:\\v1-fixture\\SENTINEL_OS_ABORT'), { name: 'AbortError', code: 'ABORT_ERR' });
}

export function osError(code: string): Error {
  return Object.assign(new Error(`${code}: SENTINEL_OS_MESSAGE open 'C:\\v1-fixture\\SENTINEL_PATH\\catalog.json'`), { code, errno: -1, syscall: 'open', path: FIXTURE_PATH });
}

// An in-memory catalog file whose open/stat/read/close can be paused, failed or
// mutated at a chosen step. Mutations made while an op is paused are visible to it.
export class ControlledCatalogFile {
  bytes: Uint8Array;
  mtimeMs = 1_000;
  ctimeMs = 1_000;
  dev = 7;
  ino = 42;
  // 'shared' returns one mutable stat object, to prove the reader copies the first stat.
  statMode: 'fresh' | 'shared' = 'fresh';
  readonly log: string[] = [];
  readonly openedPaths: string[] = [];
  readonly readRequests: number[] = [];
  readonly openHandles = new Set<number>();
  opened = 0;
  closed = 0;
  private nextHandle = 1;
  private sharedStat: { size: number; mtimeMs: number; ctimeMs: number; dev: number; ino: number } | undefined;
  private readonly pauses = new Map<FileOp, QueuedPause[]>();
  private readonly failures = new Map<FileOp, unknown[]>();
  private readonly hooks = new Map<FileOp, Array<() => void>>();

  constructor(bytes: Uint8Array) { this.bytes = bytes; }

  replace(bytes: Uint8Array, touch = true): void {
    this.bytes = bytes;
    if (touch) { this.mtimeMs += 1; this.ctimeMs += 1; }
  }

  pause(op: FileOp, options: { abortAware?: boolean } = {}): Pause {
    const reached = deferred();
    const gate = deferred();
    const queue = this.pauses.get(op) ?? [];
    queue.push({ abortAware: options.abortAware ?? false, markReached: reached.resolve, wait: gate.promise });
    this.pauses.set(op, queue);
    return { reached: reached.promise, release: gate.resolve };
  }

  failNext(op: FileOp, error: unknown): void {
    this.failures.set(op, [...this.failures.get(op) ?? [], error]);
  }

  // Runs synchronously when the op resumes, before it produces its result.
  onNext(op: FileOp, hook: () => void): void {
    this.hooks.set(op, [...this.hooks.get(op) ?? [], hook]);
  }

  private currentStat(): CatalogFileStat {
    const values = { size: this.bytes.byteLength, mtimeMs: this.mtimeMs, ctimeMs: this.ctimeMs, dev: this.dev, ino: this.ino };
    if (this.statMode === 'fresh') return values;
    this.sharedStat ??= { ...values };
    return Object.assign(this.sharedStat, values);
  }

  private async gate(op: FileOp, signal?: AbortSignal): Promise<void> {
    this.log.push(op);
    const pause = this.pauses.get(op)?.shift();
    if (pause) {
      pause.markReached();
      if (pause.abortAware && signal) {
        const aborted = new Promise<'aborted'>(done => {
          if (signal.aborted) done('aborted'); else signal.addEventListener('abort', () => done('aborted'), { once: true });
        });
        if (await Promise.race([pause.wait.then(() => 'released' as const), aborted]) === 'aborted') throw abortError();
      } else {
        await pause.wait;
      }
    }
    this.hooks.get(op)?.shift()?.();
    const failure = this.failures.get(op)?.shift();
    if (failure !== undefined) throw failure;
  }

  readonly operations: CatalogFileOperations<FakeHandle> = {
    open: async (path, signal) => {
      this.openedPaths.push(path);
      await this.gate('open', signal);
      const handle = { id: this.nextHandle++ };
      this.openHandles.add(handle.id);
      this.opened += 1;
      return handle;
    },
    stat: async (handle, signal) => {
      if (!this.openHandles.has(handle.id)) throw new Error('stat on closed handle');
      await this.gate('stat', signal);
      return this.currentStat();
    },
    read: async (handle, maxBytes, signal) => {
      if (!this.openHandles.has(handle.id)) throw new Error('read on closed handle');
      this.readRequests.push(maxBytes);
      await this.gate('read', signal);
      return this.bytes.slice(0, Math.min(maxBytes, this.bytes.byteLength));
    },
    close: async handle => {
      await this.gate('close');
      this.openHandles.delete(handle.id);
      this.closed += 1;
    },
  };

  reader(path = FIXTURE_PATH) {
    return createCatalogReader({ catalogPath: path, fileOperations: this.operations });
  }
}

// ---------------------------------------------------------------- SDK harness

export interface WireEntry { direction: 'client' | 'server'; message: Record<string, unknown> }
export interface Authored { content: unknown; structuredContent?: unknown; isError?: unknown }
export interface CallOutcome {
  result?: Record<string, unknown>;
  thrown?: { name?: string | undefined; code?: unknown; message?: string | undefined; data?: unknown };
  // Server-authored `{ content, structuredContent, isError }` picked from the wire result.
  authored?: Authored;
  // Full JSON-RPC response observed on the wire (result or error).
  response?: Record<string, unknown>;
}

// Calls made to the design 「MCP 서버 주입 지점」 functions, in arrival order.
export interface InjectionCalls {
  snapshot: number;
  guide: number;
  section: RecordSource[];
  checkout: number;
  order: Array<'snapshot' | 'guide' | 'section' | 'checkout'>;
}

export interface Harness {
  client: Client;
  servers: McpServer[];
  entered: string[];
  wire: WireEntry[];
  injected: InjectionCalls;
  call(name: string, args?: unknown, options?: { signal?: AbortSignal }): Promise<CallOutcome>;
  close(): Promise<void>;
}

export type ReadSnapshot = (signal: AbortSignal) => Promise<CatalogSnapshot>;
export type ReadGuide = (signal: AbortSignal) => Promise<GuideResult>;
export type ReadSourceSection = (source: RecordSource, signal: AbortSignal) => Promise<SourceSectionResult>;
export type ReadCheckout = (signal: AbortSignal) => Promise<CheckoutInfo>;

// Every call to this clock advances 1 s, so the token bucket never limits
// functional tests. Rate tests inject their own clock.
export function advancingClock(): () => number {
  let time = 0;
  return () => (time += 1_000);
}

export const defaultReadGuide: ReadGuide = async () => guideResultOf(linkedGuide());
export const defaultReadSourceSection: ReadSourceSection = async source => sectionResultOf(source, `# ${source.section ?? source.title}\n\n시험 구간 본문\n`);
export const defaultReadCheckout: ReadCheckout = async () => KNOWN_CHECKOUT;

export async function connectHarness(options: {
  readSnapshot: ReadSnapshot;
  readGuide?: ReadGuide;
  readSourceSection?: ReadSourceSection;
  readCheckout?: ReadCheckout;
  era?: 'legacy' | 'modern';
  now?: () => number;
  version?: string;
}): Promise<Harness> {
  const [clientSide, serverSide] = InMemoryTransport.createLinkedPair();
  const wire: WireEntry[] = [];
  const capture = (direction: WireEntry['direction'], transport: InMemoryTransport) => {
    const send = transport.send.bind(transport);
    transport.send = async (message, sendOptions) => {
      wire.push({ direction, message: structuredClone(message) as Record<string, unknown> });
      return send(message, sendOptions);
    };
  };
  capture('client', clientSide);
  capture('server', serverSide);
  const servers: McpServer[] = [];
  const entered: string[] = [];
  const injected: InjectionCalls = { snapshot: 0, guide: 0, section: [], checkout: 0, order: [] };
  const readGuide = options.readGuide ?? defaultReadGuide;
  const readSourceSection = options.readSourceSection ?? defaultReadSourceSection;
  const readCheckout = options.readCheckout ?? defaultReadCheckout;
  const connection = serveStdio(() => {
    const server = createCatalogServer({
      readSnapshot: signal => { injected.snapshot += 1; injected.order.push('snapshot'); return options.readSnapshot(signal); },
      readGuide: signal => { injected.guide += 1; injected.order.push('guide'); return readGuide(signal); },
      readSourceSection: (source, signal) => { injected.section.push(source); injected.order.push('section'); return readSourceSection(source, signal); },
      readCheckout: signal => { injected.checkout += 1; injected.order.push('checkout'); return readCheckout(signal); },
      version: options.version ?? 'v1-fixture', now: options.now ?? advancingClock(),
      onToolHandlerEntered: name => { entered.push(name); },
    });
    servers.push(server);
    return server;
  }, { transport: serverSide });
  const client = new Client({ name: 'v1-verifier', version: '0.0.0' },
    options.era === 'modern' ? { versionNegotiation: { mode: { pin: '2026-07-28' } } } : { supportedProtocolVersions: ['2025-11-25'] });
  await client.connect(clientSide);
  const expected = options.era === 'modern' ? '2026-07-28' : '2025-11-25';
  if (client.getNegotiatedProtocolVersion() !== expected) throw new Error(`negotiated ${client.getNegotiatedProtocolVersion()} != ${expected}`);

  async function call(name: string, args: unknown = {}, callOptions: { signal?: AbortSignal } = {}): Promise<CallOutcome> {
    const start = wire.length;
    const outcome: CallOutcome = {};
    try {
      outcome.result = await client.callTool({ name, arguments: args as Record<string, unknown> }, callOptions) as Record<string, unknown>;
    } catch (error) {
      const value = error as { name?: string; code?: unknown; message?: string; data?: unknown };
      outcome.thrown = { name: value.name, code: value.code, message: value.message, data: value.data };
    }
    const request = wire.slice(start).find(entry => entry.direction === 'client' && entry.message.method === 'tools/call');
    const response = request && wire.find(entry => entry.direction === 'server' && entry.message.id === request.message.id && ('result' in entry.message || 'error' in entry.message));
    if (response) {
      outcome.response = response.message;
      const result = response.message.result as Record<string, unknown> | undefined;
      if (result) {
        const authored: Authored = { content: result.content };
        if ('structuredContent' in result) authored.structuredContent = result.structuredContent;
        if ('isError' in result) authored.isError = result.isError;
        outcome.authored = authored;
      }
    }
    return outcome;
  }

  return {
    client, servers, entered, wire, injected, call,
    async close() { await client.close(); await connection.close(); },
  };
}

// Design 「MCP」 스냅샷: the snapshot metadata is `{ hash }` only (Astra 보충 v1.1 Q1).
export function staticSnapshot(catalog: RecordCatalog, hashValue = sha256(JSON.stringify(catalog))): CatalogSnapshot {
  return { metadata: { hash: hashValue }, catalog };
}

// Reads the fixture through the real reader with a fresh controlled file per call.
export function fileBacked(catalog: unknown) {
  const file = new ControlledCatalogFile(catalogBytes(catalog));
  const reader = file.reader();
  let reads = 0;
  return { file, get reads() { return reads; }, readSnapshot: (signal: AbortSignal) => { reads += 1; return reader.readSnapshot(signal); } };
}

// ---------------------------------------------------------------- independent envelope contract

const hex64 = /^[a-f0-9]{64}$/;
const text = z.string();
const strings = z.array(text);
const snapshotSchema = z.strictObject({ hash: z.string().regex(hex64) });
const listPagingSchema = z.strictObject({
  total: z.number().int().min(0), offset: z.number().int().min(0).max(100_000), limit: z.number().int().min(1).max(50),
  returned: z.number().int().min(0), nextOffset: z.number().int().min(0).nullable(),
});
const sectionPagingSchema = z.strictObject({
  offset: z.number().int().min(0).max(MAX_SECTION_OFFSET), returned: z.number().int().min(0),
  total: z.number().int().min(0), nextOffset: z.number().int().min(0).nullable(),
});
const truncatedTitle = z.array(z.literal('title'));
const recordType = z.enum(['변경', '결정', '검증', '계획']);
const systemPreviewSchema = z.strictObject({ id: text, area: text, title: text, lookupSupported: z.boolean(), truncatedFields: truncatedTitle });
const recordPreviewSchema = z.strictObject({ id: text, type: recordType, title: text, systemIds: strings, lookupSupported: z.boolean(), truncatedFields: truncatedTitle });
const systemSchema = z.strictObject({ id: text, title: text, area: text, sourceIds: strings, relatedSystemIds: strings, recordIds: strings });
const pullRequestSchema = z.strictObject({ number: z.number().int().positive(), mergeCommit: z.string().regex(/^[a-f0-9]{40}$/) });
const recordSchema = z.strictObject({ id: text, type: recordType, title: text, systemIds: strings, sourceIds: strings, pullRequests: z.array(pullRequestSchema) });
const sourceSchema = z.strictObject({
  id: text, title: text, kind: z.enum(['git', 'local', 'handoff']), locator: text, section: text.nullable(),
  availability: z.enum(['versioned', 'local-only']),
});
const checkoutSchema = z.union([
  z.strictObject({ state: z.literal('known'), branch: text.nullable(), head: text }),
  z.strictObject({ state: z.literal('unknown'), reason: z.enum(['no-git', 'link', 'pointer-invalid', 'backlink-mismatch', 'head-invalid', 'ref-invalid', 'ref-missing', 'too-large', 'load']) }),
]);
const cardPreviewSchema = z.strictObject({
  id: text, parentId: text.nullable(), title: text, relatedSystemIds: strings, documentId: text.nullable(),
  lookupSupported: z.boolean(), truncatedFields: truncatedTitle,
});
const cardSchema = z.strictObject({
  id: text, parentId: text.nullable(), title: text, summary: text, relatedSystemIds: strings,
  status: z.literal('code-present').optional(), documentId: text.optional(),
  codeReference: z.strictObject({
    commitSha: text,
    mappings: z.array(z.strictObject({ path: text, kind: z.enum(['file', 'directory']), namespace: text.optional(), role: text })),
  }).optional(),
});
const guideBlockSchema = z.union([
  z.strictObject({ type: z.literal('paragraph'), text }),
  z.strictObject({ type: z.literal('diagram'), id: text, format: z.literal('mermaid'), source: text, title: text, description: text }),
]);
const documentSchema = z.strictObject({
  id: text, cardId: text, title: text, sourceCommit: text, sourceRefs: z.array(z.strictObject({ path: text, symbol: text })),
  sections: z.array(z.strictObject({ id: text, title: text, blocks: z.array(guideBlockSchema) })),
});
const dataSchemas: Record<ToolName, z.ZodType> = {
  list_systems: z.strictObject({ items: z.array(systemPreviewSchema), paging: listPagingSchema }),
  search_records: z.strictObject({ items: z.array(recordPreviewSchema), paging: listPagingSchema }),
  get_system: z.strictObject({ system: systemSchema }),
  get_record: z.strictObject({ record: recordSchema }),
  get_source: z.strictObject({ source: sourceSchema, evidenceRead: z.literal(false), availabilityVerified: z.literal(false) }),
  read_source_section: z.strictObject({
    source: sourceSchema, path: text, heading: text.nullable(), text, sectionHash: z.string().regex(hex64),
    checkout: checkoutSchema, paging: sectionPagingSchema,
  }),
  list_guide_cards: z.strictObject({ items: z.array(cardPreviewSchema), paging: listPagingSchema }),
  get_guide_card: z.strictObject({ card: cardSchema, document: documentSchema.nullable() }),
};
export const LEGACY_ERROR_CODES = [
  'INVALID_ARGUMENT', 'CATALOG_MISSING', 'CATALOG_UNREADABLE', 'CATALOG_TOO_LARGE', 'CATALOG_INVALID', 'CATALOG_REFERENCE_BROKEN',
  'CATALOG_CHANGED_DURING_READ', 'NOT_FOUND', 'VERSION_CONFLICT', 'VERSION_REQUIRED', 'RESPONSE_TOO_LARGE', 'RATE_LIMITED', 'REQUEST_CANCELLED',
] as const;
// Design 「MCP」 새 오류 코드.
export const SOURCE_ERROR_CODES = [
  'SOURCE_NOT_READABLE', 'SOURCE_PATH_REJECTED', 'SOURCE_MISSING', 'SOURCE_TOO_LARGE', 'SOURCE_CHANGED_DURING_READ',
  'SOURCE_INVALID_ENCODING', 'SECTION_MISSING', 'SECTION_AMBIGUOUS',
  // Astra 보충 v1.2 Q6 (msg_e854e8f17079): the source store's unexpected I/O failure 'load'.
  'SOURCE_UNREADABLE',
] as const;
export const GUIDE_ERROR_CODES = ['GUIDE_MISSING', 'GUIDE_UNREADABLE', 'GUIDE_TOO_LARGE', 'GUIDE_INVALID', 'GUIDE_CHANGED_DURING_READ'] as const;
export const ERROR_CODES = [...LEGACY_ERROR_CODES, ...SOURCE_ERROR_CODES, ...GUIDE_ERROR_CODES] as const;
export type ErrorCode = typeof ERROR_CODES[number];
// Design 「MCP」 오류 코드 표 「재시도 가능」 column plus the unchanged legacy set.
export const RETRYABLE = new Set<ErrorCode>([
  'CATALOG_MISSING', 'CATALOG_UNREADABLE', 'CATALOG_CHANGED_DURING_READ', 'RATE_LIMITED',
  'SOURCE_CHANGED_DURING_READ', 'SOURCE_UNREADABLE', 'GUIDE_MISSING', 'GUIDE_UNREADABLE', 'GUIDE_CHANGED_DURING_READ',
]);
const errorSchema = z.strictObject({
  ok: z.literal(false), snapshot: snapshotSchema.nullable(),
  error: z.strictObject({
    code: z.enum(ERROR_CODES), message: z.string().min(1).max(256), retryable: z.boolean(),
    details: z.record(z.string(), z.union([z.string(), z.number(), z.boolean(), z.array(z.string())])).optional(),
  }),
});

export type Snapshot = z.infer<typeof snapshotSchema>;
export type Paging = z.infer<typeof listPagingSchema>;
export type SectionPaging = z.infer<typeof sectionPagingSchema>;
export interface SuccessEnvelope { ok: true; snapshot: Snapshot; data: Record<string, unknown> }
export type ErrorEnvelope = z.infer<typeof errorSchema>;
export type Envelope = SuccessEnvelope | ErrorEnvelope;

export function authoredBytes(outcome: CallOutcome): number {
  if (!outcome.authored) throw new Error('no server-authored result observed');
  return Buffer.byteLength(JSON.stringify(outcome.authored), 'utf8');
}

// Asserts the design 「MCP」 domain envelope on one tool result and returns it.
export function expectEnvelope(tool: ToolName, outcome: CallOutcome): Envelope {
  expect(outcome.thrown, JSON.stringify(outcome.thrown)).toBeUndefined();
  const authored = outcome.authored;
  if (!authored) throw new Error('no server-authored result observed');
  expect(Object.keys(authored).sort()).toEqual(['content', 'isError', 'structuredContent']);
  const content = authored.content as Array<{ type: string; text: string }>;
  expect(content).toHaveLength(1);
  expect(content[0]?.type).toBe('text');
  expect(content[0]?.text).toBe(JSON.stringify(authored.structuredContent));
  expect(JSON.parse(content[0]?.text ?? 'null')).toEqual(authored.structuredContent);
  // The client-visible content/structuredContent equal what the server wrote.
  expect(outcome.result?.content).toEqual(authored.content);
  expect(outcome.result?.structuredContent).toEqual(authored.structuredContent);
  expect(authoredBytes(outcome)).toBeLessThanOrEqual(MAX_RESULT);
  const envelope = authored.structuredContent as Envelope;
  expect(authored.isError).toBe(envelope.ok === false);
  if (envelope.ok) {
    const parsed = z.strictObject({ ok: z.literal(true), snapshot: snapshotSchema, data: dataSchemas[tool] }).safeParse(envelope);
    expect(parsed.success, parsed.success ? '' : JSON.stringify(parsed.error.issues).slice(0, 800)).toBe(true);
    if (tool === 'read_source_section') {
      const { text: fragment, paging } = envelope.data as { text: string; paging: SectionPaging };
      expect(paging.returned).toBe(fragment.length);
      expect(paging.nextOffset).toBe(paging.offset + paging.returned < paging.total ? paging.offset + paging.returned : null);
    } else if ('paging' in envelope.data) {
      const { items, paging } = envelope.data as { items: unknown[]; paging: Paging };
      expect(paging.returned).toBe(items.length);
      expect(paging.nextOffset).toBe(paging.offset + paging.returned < paging.total ? paging.offset + paging.returned : null);
      expect(paging.returned).toBeLessThanOrEqual(paging.limit);
    }
  } else {
    const parsed = errorSchema.safeParse(envelope);
    expect(parsed.success, parsed.success ? '' : JSON.stringify(parsed.error.issues).slice(0, 800)).toBe(true);
    expect('data' in envelope).toBe(false);
    expect(envelope.error.retryable).toBe(RETRYABLE.has(envelope.error.code));
  }
  return envelope;
}

export function expectError(tool: ToolName, outcome: CallOutcome, code: ErrorCode): ErrorEnvelope {
  const envelope = expectEnvelope(tool, outcome);
  expect(envelope.ok, JSON.stringify(envelope).slice(0, 400)).toBe(false);
  if (envelope.ok) throw new Error('unreachable');
  expect(envelope.error.code).toBe(code);
  return envelope;
}

export function expectSuccess<T = Record<string, unknown>>(tool: ToolName, outcome: CallOutcome): { snapshot: Snapshot; data: T } {
  const envelope = expectEnvelope(tool, outcome);
  expect(envelope.ok, JSON.stringify(envelope).slice(0, 400)).toBe(true);
  if (!envelope.ok) throw new Error('unreachable');
  return { snapshot: envelope.snapshot, data: envelope.data as T };
}

// Observation for an input refusal on either SDK channel (tool isError result or
// JSON-RPC error). Returns the client-observed JSON byte size for the 1 KiB criterion.
export function refusalObservation(outcome: CallOutcome): { channel: 'tool-result' | 'protocol-error'; json: string; bytes: number } {
  if (outcome.thrown) {
    const json = JSON.stringify({ code: outcome.thrown.code, message: outcome.thrown.message, data: outcome.thrown.data });
    return { channel: 'protocol-error', json, bytes: Buffer.byteLength(json, 'utf8') };
  }
  const json = JSON.stringify(outcome.result);
  return { channel: 'tool-result', json, bytes: Buffer.byteLength(json, 'utf8') };
}

export function expectRefusal(outcome: CallOutcome, sentinels: string[]): { channel: string; bytes: number } {
  const observed = refusalObservation(outcome);
  if (observed.channel === 'tool-result') {
    expect(outcome.result?.isError).toBe(true);
    const structured = outcome.result?.structuredContent as { ok?: unknown } | undefined;
    expect(structured?.ok === true).toBe(false);
  }
  for (const sentinel of sentinels) expect(observed.json.includes(sentinel), `reflected ${sentinel.slice(0, 40)}`).toBe(false);
  expect(observed.bytes).toBeLessThanOrEqual(MAX_REFUSAL);
  return observed;
}

// Walks every page with the first page's hash; fails on repetition or runaway loops.
export async function walkPages(harness: Harness, tool: ListToolName, args: Record<string, unknown>, maxPages = 10_000) {
  const ids: string[] = [];
  const pages: Array<{ paging: Paging; bytes: number; returned: number }> = [];
  let offset = 0;
  let hashValue: string | undefined;
  for (let page = 0; page < maxPages; page += 1) {
    const outcome = await harness.call(tool, { ...args, offset, ...(hashValue ? { expectedHash: hashValue } : {}) });
    const envelope = expectEnvelope(tool, outcome);
    if (!envelope.ok) return { ids, pages, error: envelope };
    hashValue ??= envelope.snapshot.hash;
    expect(envelope.snapshot.hash).toBe(hashValue);
    const { items, paging } = envelope.data as { items: Array<{ id: string }>; paging: Paging };
    expect(paging.offset).toBe(offset);
    ids.push(...items.map(item => item.id));
    pages.push({ paging, bytes: authoredBytes(outcome), returned: items.length });
    if (paging.nextOffset === null) return { ids, pages, error: undefined };
    expect(paging.nextOffset).toBeGreaterThan(offset);
    offset = paging.nextOffset;
  }
  throw new Error('paging did not terminate');
}

export function hasLoneSurrogate(value: string): boolean {
  return /[\uD800-\uDBFF](?![\uDC00-\uDFFF])|(?<![\uD800-\uDBFF])[\uDC00-\uDFFF]/.test(value);
}

export async function flushMicrotasks(rounds = 20): Promise<void> {
  for (let round = 0; round < rounds; round += 1) await new Promise<void>(resolve => setImmediate(resolve));
}

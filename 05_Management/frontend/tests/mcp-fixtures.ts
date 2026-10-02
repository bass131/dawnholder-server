// Independent fixtures and harness for the shared-read MCP verification (V1 owner).
// Contract values come from goal D2/D3, not from the product's own schemas, so a
// product drift fails here instead of being mirrored. V2 may import these helpers.
import { createHash } from 'node:crypto';
import { expect } from 'vitest';
import { z } from 'zod';
import { Client } from '@modelcontextprotocol/client';
import { InMemoryTransport, type McpServer } from '@modelcontextprotocol/server';
import { serveStdio } from '@modelcontextprotocol/server/stdio';
import type { DevelopmentRecord, RecordCatalog, RecordSource, SystemRecord } from '../electron/catalog-contract';
import { createCatalogReader, type CatalogFileOperations, type CatalogFileStat, type CatalogSnapshot } from '../mcp/catalog-reader';
import { createCatalogServer } from '../mcp/catalog-server';

export const MAX_CATALOG = 2 * 1024 * 1024;
export const MAX_RESULT = 16 * 1024;
export const DEFAULT_PAGE_TARGET = 8 * 1024;
export const MAX_REFUSAL = 1024;
export const TOOL_NAMES = ['list_systems', 'search_records', 'get_system', 'get_record', 'get_source'] as const;
export type ToolName = typeof TOOL_NAMES[number];
export const FIXTURE_PATH = 'C:\\v1-fixture\\SENTINEL_PATH\\catalog.json';

// ---------------------------------------------------------------- catalog builders

export function makeSource(id: string, extra: Partial<RecordSource> = {}): RecordSource {
  return { id, title: `출처 ${id}`, kind: 'git', locator: `docs/${id}.md`, revision: 'rev-1', section: '§1', availability: 'versioned', note: '', ...extra };
}

export function makeSystem(id: string, extra: Partial<SystemRecord> = {}): SystemRecord {
  return {
    id, title: `시스템 ${id}`, area: '게임 기반', summary: `요약 ${id}`, responsibility: `책임 ${id}`, behavior: [],
    implementationStatus: '구현', integrationStatus: '연결', verificationStatus: '검증',
    limitations: [], nextSteps: [], sourceIds: [], relatedSystemIds: [], recordIds: [], ...extra,
  };
}

export function makeRecord(id: string, extra: Partial<DevelopmentRecord> = {}): DevelopmentRecord {
  return { id, type: '변경', title: `기록 ${id}`, summary: `요약 ${id}`, reason: '', status: '완료', systemIds: [], sourceIds: [], details: [], limitations: [], nextSteps: [], ...extra };
}

export function makeCatalog(parts: Partial<RecordCatalog> = {}): RecordCatalog {
  return {
    schemaVersion: 1, revision: 'fixture-r1', asOf: '2020-01-02T03:04:05Z', sourceCommit: '0123456789abcdef0123456789abcdef01234567',
    scopeNote: 'V1 독립 fixture', sources: [], systems: [], records: [], ...parts,
  };
}

// A small linked catalog with Korean, emoji, escaping and locale-sensitive text.
export function linkedCatalog(): RecordCatalog {
  return makeCatalog({
    revision: 'r-ä-"quoted"', asOf: '2019-12-31T23:59:59Z', sourceCommit: 'deadbeefdeadbeefdeadbeefdeadbeefdeadbeef',
    sources: [
      makeSource('src-a', { locator: 'C:\\v1-fixture\\SENTINEL_LOCATOR\\evidence.md', note: 'line1\nline2 "q" \\ back' }),
      makeSource('src-b', { kind: 'handoff', availability: 'local-only', locator: 'https://example.invalid/SENTINEL_URL', section: '' }),
      makeSource('src-"esc\\', { kind: 'local', title: '</script>\u2028\u2029' }),
    ],
    systems: [
      makeSystem('sys-alpha', {
        title: 'Alpha 전투 "엔진"', area: '게임 기반', summary: '타격 판정과 😀 이모지', responsibility: '전투 책임',
        behavior: ['타격 판정', 'line\nbreak', 'Tab\tchar'], sourceIds: ['src-a'], relatedSystemIds: ['sys-beta'], recordIds: ['rec-1', 'rec-2'],
        limitations: ['한계 1'], nextSteps: ['다음 1', '다음 2'],
      }),
      makeSystem('sys-beta', { title: 'Beta 네트워크', area: '서버 플랫폼', summary: '서버 동기화', behavior: ['패킷'], sourceIds: ['src-b', 'src-"esc\\'], relatedSystemIds: ['sys-alpha'], recordIds: ['rec-2', 'rec-3'] }),
      makeSystem('sys-gamma', { title: 'İstanbul GAMMA', area: 'Management', summary: 'Locale 검색', recordIds: ['rec-10'] }),
      makeSystem('sys-😀', { title: 'Emoji 시스템', area: '게임 기반', summary: '이모지 ID' }),
    ],
    records: [
      makeRecord('rec-1', { type: '변경', title: 'Alpha 변경', summary: '타격 범위 변경', reason: '밸런스', systemIds: ['sys-alpha'], sourceIds: ['src-a'], details: ['\u2028 separator', '</script>', 'quote " and \\'] }),
      makeRecord('rec-2', { type: '결정', title: '동기화 결정', summary: 'Alpha Beta 공통', systemIds: ['sys-alpha', 'sys-beta'], sourceIds: ['src-b'] }),
      makeRecord('rec-3', { type: '검증', title: 'Beta 검증', summary: '패킷 검증', systemIds: ['sys-beta'] }),
      makeRecord('rec-4', { type: '계획', title: '독립 계획', summary: '시스템 없음', systemIds: [] }),
      makeRecord('rec-10', { type: '변경', title: 'Gamma 변경', summary: 'istanbul 소문자', systemIds: ['sys-gamma'] }),
    ],
  });
}

export function catalogBytes(catalog: unknown, space = 2): Uint8Array {
  return new TextEncoder().encode(JSON.stringify(catalog, null, space));
}

// Pads scopeNote so the encoded catalog has exactly `size` bytes (ASCII padding).
export function catalogOfExactSize(size: number, base: RecordCatalog = makeCatalog()): Uint8Array {
  const empty = catalogBytes({ ...base, scopeNote: '' }).byteLength;
  const bytes = catalogBytes({ ...base, scopeNote: 'x'.repeat(size - empty) });
  if (bytes.byteLength !== size) throw new Error(`fixture size ${bytes.byteLength} != ${size}`);
  return bytes;
}

export const sha256 = (bytes: Uint8Array | string) => createHash('sha256').update(bytes).digest('hex');
// Goal D3: decode as UTF-8 text, re-encode as UTF-8, then SHA-256 (independent oracle).
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

// Verbatim baseline UI search from `git show 18c8ca6:05_Management/frontend/src/recordCatalog.ts`.
export function baselineMatchesQuery(query: string, values: string[]): boolean {
  const terms = query.trim().toLocaleLowerCase().split(/\s+/).filter(Boolean);
  const text = values.join(' ').toLocaleLowerCase();
  return terms.every(term => text.includes(term));
}
export function baselineFilterSystems(catalog: RecordCatalog, query: string, area: string): SystemRecord[] {
  return catalog.systems.filter(system => (!area || system.area === area) && baselineMatchesQuery(query, [system.id, system.title, system.area, system.summary, system.responsibility, ...system.behavior]));
}
export function baselineFilterRecords(catalog: RecordCatalog, query: string, area: string, type: string): DevelopmentRecord[] {
  return catalog.records.filter(record => (!type || record.type === type) && (!area || record.systemIds.some(id => catalog.systems.some(system => system.id === id && system.area === area))) && baselineMatchesQuery(query, [record.id, record.title, record.summary, record.reason, ...record.details]));
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

export interface Harness {
  client: Client;
  servers: McpServer[];
  entered: string[];
  wire: WireEntry[];
  call(name: string, args?: unknown, options?: { signal?: AbortSignal }): Promise<CallOutcome>;
  close(): Promise<void>;
}

// Every call to this clock advances 1 s, so the token bucket never limits
// functional tests. Rate tests inject their own clock.
export function advancingClock(): () => number {
  let time = 0;
  return () => (time += 1_000);
}

export async function connectHarness(options: {
  readSnapshot: (signal: AbortSignal) => Promise<CatalogSnapshot>;
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
  const connection = serveStdio(() => {
    const server = createCatalogServer({
      readSnapshot: options.readSnapshot, version: options.version ?? 'v1-fixture', now: options.now ?? advancingClock(),
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
    client, servers, entered, wire, call,
    async close() { await client.close(); await connection.close(); },
  };
}

export function staticSnapshot(catalog: RecordCatalog, hashValue = sha256(JSON.stringify(catalog))): CatalogSnapshot {
  return { metadata: { hash: hashValue, revision: catalog.revision, asOf: catalog.asOf, sourceCommit: catalog.sourceCommit }, catalog };
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
const snapshotSchema = z.strictObject({ hash: z.string().regex(hex64), revision: text, asOf: text, sourceCommit: text });
const pagingSchema = z.strictObject({
  total: z.number().int().min(0), offset: z.number().int().min(0).max(100_000), limit: z.number().int().min(1).max(50),
  returned: z.number().int().min(0), nextOffset: z.number().int().min(0).nullable(),
});
const systemSummarySchema = z.strictObject({
  id: text, title: text, area: text, summary: text, implementationStatus: text, integrationStatus: text, verificationStatus: text,
  lookupSupported: z.boolean(), truncatedFields: z.array(z.enum(['title', 'summary', 'implementationStatus', 'integrationStatus', 'verificationStatus'])),
});
const recordType = z.enum(['변경', '결정', '검증', '계획']);
const recordSummarySchema = z.strictObject({
  id: text, type: recordType, title: text, summary: text, status: text, systemIds: strings,
  lookupSupported: z.boolean(), truncatedFields: z.array(z.enum(['title', 'summary', 'status'])),
});
const systemSchema = z.strictObject({
  id: text, title: text, area: text, summary: text, responsibility: text, behavior: strings, implementationStatus: text, integrationStatus: text,
  verificationStatus: text, limitations: strings, nextSteps: strings, sourceIds: strings, relatedSystemIds: strings, recordIds: strings,
});
const recordSchema = z.strictObject({
  id: text, type: recordType, title: text, summary: text, reason: text, status: text, systemIds: strings, sourceIds: strings,
  details: strings, limitations: strings, nextSteps: strings,
});
const sourceSchema = z.strictObject({
  id: text, title: text, kind: z.enum(['git', 'local', 'handoff']), locator: text, revision: text, section: text,
  availability: z.enum(['versioned', 'local-only']), note: text,
});
const dataSchemas: Record<ToolName, z.ZodType> = {
  list_systems: z.strictObject({ items: z.array(systemSummarySchema), paging: pagingSchema }),
  search_records: z.strictObject({ items: z.array(recordSummarySchema), paging: pagingSchema }),
  get_system: z.strictObject({ system: systemSchema }),
  get_record: z.strictObject({ record: recordSchema }),
  get_source: z.strictObject({ source: sourceSchema, evidenceRead: z.literal(false), availabilityVerified: z.literal(false) }),
};
export const ERROR_CODES = [
  'INVALID_ARGUMENT', 'CATALOG_MISSING', 'CATALOG_UNREADABLE', 'CATALOG_TOO_LARGE', 'CATALOG_INVALID', 'CATALOG_REFERENCE_BROKEN',
  'CATALOG_CHANGED_DURING_READ', 'NOT_FOUND', 'VERSION_CONFLICT', 'VERSION_REQUIRED', 'RESPONSE_TOO_LARGE', 'RATE_LIMITED', 'REQUEST_CANCELLED',
] as const;
export type ErrorCode = typeof ERROR_CODES[number];
const RETRYABLE = new Set<ErrorCode>(['CATALOG_MISSING', 'CATALOG_UNREADABLE', 'CATALOG_CHANGED_DURING_READ', 'RATE_LIMITED']);
const errorSchema = z.strictObject({
  ok: z.literal(false), snapshot: snapshotSchema.nullable(),
  error: z.strictObject({
    code: z.enum(ERROR_CODES), message: z.string().min(1).max(256), retryable: z.boolean(),
    details: z.record(z.string(), z.union([z.string(), z.number(), z.boolean(), z.array(z.string())])).optional(),
  }),
});

export type Snapshot = z.infer<typeof snapshotSchema>;
export type Paging = z.infer<typeof pagingSchema>;
export interface SuccessEnvelope { ok: true; snapshot: Snapshot; data: Record<string, unknown> }
export type ErrorEnvelope = z.infer<typeof errorSchema>;
export type Envelope = SuccessEnvelope | ErrorEnvelope;

export function authoredBytes(outcome: CallOutcome): number {
  if (!outcome.authored) throw new Error('no server-authored result observed');
  return Buffer.byteLength(JSON.stringify(outcome.authored), 'utf8');
}

// Asserts the D2/D3 domain envelope on one tool result and returns it.
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
    if ('paging' in envelope.data) {
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
export async function walkPages(harness: Harness, tool: 'list_systems' | 'search_records', args: Record<string, unknown>, maxPages = 10_000) {
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

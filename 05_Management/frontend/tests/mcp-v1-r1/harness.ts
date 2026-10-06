// V1-R1 (V1-01 re-verification) harness, owned by the V1-R1 verifier. It reuses the V1
// fixtures read-only and adds what the V1 harness does not expose: raw JSON-RPC requests
// (so non-string tool names reach the server), per-response wire measurement, and a
// recording Transport for the public Transport/connect contract.
// The connection is the real SDK path: Client ↔ in-memory wire ↔ public serveStdio ↔
// createCatalogServer. Nothing here touches SDK private members.
import { Client } from '@modelcontextprotocol/client';
import { InMemoryTransport, type McpServer, type Transport } from '@modelcontextprotocol/server';
import { serveStdio } from '@modelcontextprotocol/server/stdio';
import type { CatalogSnapshot } from '../../mcp/catalog-reader';
import { createCatalogServer } from '../../mcp/catalog-server';
import {
  advancingClock, defaultReadCheckout, defaultReadGuide, defaultReadSourceSection, flushMicrotasks, type CallOutcome,
} from '../mcp-fixtures';

export type Era = 'legacy' | 'modern';
export const ERAS: readonly Era[] = ['legacy', 'modern'];
export const REVISION: Record<Era, string> = { legacy: '2025-11-25', modern: '2026-07-28' };
export type JsonObject = Record<string, unknown>;
export interface WireMessage { direction: 'client' | 'server'; message: JsonObject }

export const bytesOf = (value: unknown) => Buffer.byteLength(JSON.stringify(value), 'utf8');

// One request/response pair as both the client API and the wire saw it.
export interface Exchange {
  request?: JsonObject;
  response?: JsonObject;
  result?: JsonObject;
  thrown?: { code?: unknown; message?: unknown; data?: unknown };
}

// The goal's "client-observed" refusal: the error object the client API surfaces (or the
// result object), JSON UTF-8 bytes. The wire size is the whole JSON-RPC response object
// (newline excluded) and is recorded separately.
export interface Observation {
  channel: 'protocol-error' | 'tool-result' | 'unanswered';
  clientJson: string;
  clientBytes: number;
  wireBytes: number | null;
}

export function observe(exchange: Exchange): Observation {
  const wireBytes = exchange.response ? bytesOf(exchange.response) : null;
  if (exchange.thrown) {
    const clientJson = JSON.stringify({ code: exchange.thrown.code, message: exchange.thrown.message, data: exchange.thrown.data });
    return { channel: 'protocol-error', clientJson, clientBytes: Buffer.byteLength(clientJson, 'utf8'), wireBytes };
  }
  if (exchange.result) {
    const clientJson = JSON.stringify(exchange.result);
    return { channel: 'tool-result', clientJson, clientBytes: Buffer.byteLength(clientJson, 'utf8'), wireBytes };
  }
  const response = exchange.response;
  if (response && 'error' in response) {
    const clientJson = JSON.stringify(response.error);
    return { channel: 'protocol-error', clientJson, clientBytes: Buffer.byteLength(clientJson, 'utf8'), wireBytes };
  }
  if (response && 'result' in response) {
    const clientJson = JSON.stringify(response.result);
    return { channel: 'tool-result', clientJson, clientBytes: Buffer.byteLength(clientJson, 'utf8'), wireBytes };
  }
  return { channel: 'unanswered', clientJson: '', clientBytes: 0, wireBytes };
}

// Adapts an exchange to the V1 fixture shape so the V1 envelope oracle (expectEnvelope)
// judges normal results unchanged.
export function toOutcome(exchange: Exchange): CallOutcome {
  const outcome: CallOutcome = {};
  if (exchange.result) outcome.result = exchange.result;
  if (exchange.thrown) {
    outcome.thrown = { code: exchange.thrown.code, data: exchange.thrown.data };
    if (typeof exchange.thrown.message === 'string') outcome.thrown.message = exchange.thrown.message;
  }
  if (exchange.response) {
    outcome.response = exchange.response;
    const result = exchange.response.result as JsonObject | undefined;
    if (result) {
      outcome.authored = { content: result.content };
      if ('structuredContent' in result) outcome.authored.structuredContent = result.structuredContent;
      if ('isError' in result) outcome.authored.isError = result.isError;
    }
  }
  return outcome;
}

export interface R1Harness {
  era: Era;
  client: Client;
  servers: McpServer[];
  wire: WireMessage[];
  // Product handler first-line seam (createCatalogServer onToolHandlerEntered).
  entered: string[];
  callTool(name: string, args?: unknown, options?: { signal?: AbortSignal }): Promise<Exchange>;
  // Sends a raw tools/call whose params are replaced; the modern per-request envelope
  // (`params._meta`) is cloned from a real client tools/call so only `params` differs.
  raw(params: unknown, options?: { keepMeta?: boolean; method?: string }): Promise<Exchange>;
  close(): Promise<void>;
}

function responseFor(wire: WireMessage[], id: unknown): JsonObject | undefined {
  return wire.find(entry => entry.direction === 'server' && entry.message.id === id && ('result' in entry.message || 'error' in entry.message))?.message;
}

export async function connectR1(options: {
  era: Era;
  readSnapshot: (signal: AbortSignal) => Promise<CatalogSnapshot>;
  now?: () => number;
}): Promise<R1Harness> {
  const [clientSide, serverSide] = InMemoryTransport.createLinkedPair();
  const wire: WireMessage[] = [];
  for (const [direction, transport] of [['client', clientSide], ['server', serverSide]] as const) {
    const send = transport.send.bind(transport);
    transport.send = async (message, sendOptions) => {
      wire.push({ direction, message: structuredClone(message) as JsonObject });
      return send(message, sendOptions);
    };
  }
  const servers: McpServer[] = [];
  const entered: string[] = [];
  const connection = serveStdio(() => {
    // Design 「MCP 서버 주입 지점」: the new tools read through injected functions (fixture defaults).
    const server = createCatalogServer({
      readSnapshot: options.readSnapshot, readGuide: defaultReadGuide, readSourceSection: defaultReadSourceSection, readCheckout: defaultReadCheckout,
      version: 'v1-r1-fixture', now: options.now ?? advancingClock(),
      onToolHandlerEntered: name => { entered.push(name); },
    });
    servers.push(server);
    return server;
  }, { transport: serverSide });
  const client = new Client({ name: 'v1-r1-verifier', version: '0.0.0' },
    options.era === 'modern' ? { versionNegotiation: { mode: { pin: '2026-07-28' } } } : { supportedProtocolVersions: ['2025-11-25'] });
  await client.connect(clientSide);
  if (client.getNegotiatedProtocolVersion() !== REVISION[options.era]) throw new Error(`negotiated ${client.getNegotiatedProtocolVersion()}`);

  let template: JsonObject | undefined;
  let rawId = 0;

  async function callTool(name: string, args: unknown = {}, callOptions: { signal?: AbortSignal } = {}): Promise<Exchange> {
    const start = wire.length;
    const exchange: Exchange = {};
    try {
      exchange.result = await client.callTool({ name, arguments: args as Record<string, unknown> }, callOptions) as JsonObject;
    } catch (error) {
      const value = error as { code?: unknown; message?: unknown; data?: unknown };
      exchange.thrown = { code: value.code, message: value.message, data: value.data };
    }
    await flushMicrotasks(2);
    const request = wire.slice(start).find(entry => entry.direction === 'client' && entry.message.method === 'tools/call')?.message;
    if (request) {
      template ??= request;
      exchange.request = request;
      const response = responseFor(wire, request.id);
      if (response) exchange.response = response;
    }
    return exchange;
  }

  async function raw(params: unknown, rawOptions: { keepMeta?: boolean; method?: string } = {}): Promise<Exchange> {
    if (!template) await callTool('r1_template_probe');
    const base = template as JsonObject;
    const meta = (base.params as JsonObject | undefined)?._meta;
    let finalParams = params;
    if ((rawOptions.keepMeta ?? true) && meta !== undefined && params !== null && typeof params === 'object' && !Array.isArray(params)) {
      finalParams = { _meta: structuredClone(meta), ...params as JsonObject };
    }
    const id = `r1-raw-${(rawId += 1)}`;
    const request: JsonObject = { jsonrpc: '2.0', id, method: rawOptions.method ?? 'tools/call', params: finalParams };
    await clientSide.send(request as Parameters<Transport['send']>[0]);
    await flushMicrotasks(5);
    const exchange: Exchange = { request };
    const response = responseFor(wire, id);
    if (response) exchange.response = response;
    return exchange;
  }

  return {
    era: options.era, client, servers, wire, entered, callTool, raw,
    async close() { await client.close(); await connection.close(); },
  };
}

// A test-owned Transport implementing only the public Transport interface. It delegates
// to an in-memory wire and records what the product forwards to it.
export class RecordingTransport implements Transport {
  onclose: Transport['onclose'];
  onerror: Transport['onerror'];
  onmessage: Transport['onmessage'];
  readonly sessionId = 'r1-session-id';
  readonly sends: Array<{ message: JsonObject; options: Parameters<Transport['send']>[1] }> = [];
  readonly protocolVersions: string[] = [];
  readonly supportedVersions: string[][] = [];
  starts = 0;
  closes = 0;
  failSend: ((message: JsonObject) => boolean) | undefined;

  constructor(private readonly inner: InMemoryTransport) {}

  async start(): Promise<void> {
    this.starts += 1;
    this.inner.onmessage = (message, extra) => this.onmessage?.(message, extra);
    this.inner.onclose = () => this.onclose?.();
    this.inner.onerror = error => this.onerror?.(error);
    await this.inner.start();
  }

  async send(...[message, options]: Parameters<Transport['send']>): Promise<void> {
    const copy = structuredClone(message) as JsonObject;
    this.sends.push({ message: copy, options });
    if (this.failSend?.(copy)) throw new Error('R1 injected send failure');
    // InMemoryTransport declares a narrower options type; the runtime object is passed as is.
    await this.inner.send(message, options as Parameters<InMemoryTransport['send']>[1]);
  }

  async close(): Promise<void> {
    this.closes += 1;
    await this.inner.close();
  }

  setProtocolVersion(version: string): void { this.protocolVersions.push(version); }
  setSupportedProtocolVersions(versions: string[]): void { this.supportedVersions.push([...versions]); }
}

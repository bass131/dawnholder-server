// V2 harness (verifier-owned): real child processes over real stdio pipes. A test-owned
// client Transport writes newline-delimited JSON-RPC to the child's stdin and records every
// raw stdout line, so wire bytes, stdout purity, stderr and exit are observed directly.
// Children are either the BUILT production entry (mcp-dist/mcp/main.js, canonical catalog,
// no seams) or the V2 fixture entry (tests/mcp-v2/fixture-entry.mjs) that assembles the
// built product factories with a fixture path and reports counters over an IPC side channel.
import { spawn, type ChildProcess } from 'node:child_process';
import { createHash, randomUUID } from 'node:crypto';
import { mkdirSync, readFileSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, resolve, sep } from 'node:path';
import { fileURLToPath } from 'node:url';
import { Client } from '@modelcontextprotocol/client';
import type { Transport } from '@modelcontextprotocol/server';

export type Era = 'legacy' | 'modern';
export const ERAS: readonly Era[] = ['legacy', 'modern'];
export const REVISION: Record<Era, string> = { legacy: '2025-11-25', modern: '2026-07-28' };
export type JsonObject = Record<string, unknown>;

export const FRONTEND = fileURLToPath(new URL('../../', import.meta.url));
export const PRODUCTION_ENTRY = join(FRONTEND, 'mcp-dist', 'mcp', 'main.js');
export const FIXTURE_ENTRY = join(FRONTEND, 'tests', 'mcp-v2', 'fixture-entry.mjs');
export const CANONICAL_CATALOG = resolve(FRONTEND, '..', 'records', 'catalog.json');
export const EXIT_DEADLINE_MS = 5_000;

export const bytesOf = (value: unknown) => Buffer.byteLength(JSON.stringify(value), 'utf8');
export const sha256 = (bytes: Uint8Array | string) => createHash('sha256').update(bytes).digest('hex');
// Goal D3 oracle: UTF-8 decode, re-encode, SHA-256.
export const textHash = (bytes: Uint8Array) => sha256(Buffer.from(Buffer.from(bytes).toString('utf8'), 'utf8'));
export const sleep = (ms: number) => new Promise<void>(done => setTimeout(done, ms));

export async function waitFor(condition: () => boolean | Promise<boolean>, timeoutMs = 5_000, label = 'condition'): Promise<void> {
  const deadline = performance.now() + timeoutMs;
  while (!(await condition())) {
    if (performance.now() > deadline) throw new Error(`timed out waiting for ${label}`);
    await sleep(5);
  }
}

// ------------------------------------------------------------------ TEMP fixture dirs
const ownedRoots: string[] = [];
export function tempRoot(label: string): string {
  const root = join(tmpdir(), `dawnholder-v2-${label}-${randomUUID()}`);
  mkdirSync(root, { recursive: true });
  ownedRoots.push(root);
  return root;
}
export function removeTempRoots(): number {
  let removed = 0;
  for (const root of ownedRoots.splice(0)) {
    const absolute = resolve(root);
    // Only remove what this harness created under the OS temp dir.
    if (!absolute.startsWith(resolve(tmpdir()) + sep) || !absolute.includes(`${sep}dawnholder-v2-`)) throw new Error(`refusing to remove ${absolute}`);
    rmSync(absolute, { recursive: true, force: true });
    removed += 1;
  }
  return removed;
}

// ------------------------------------------------------------------ client transport
export interface Line { at: number; bytes: number; text: string; json?: JsonObject }

class PipeTransport implements Transport {
  onclose: Transport['onclose'];
  onerror: Transport['onerror'];
  onmessage: Transport['onmessage'];
  readonly lines: Line[] = [];
  readonly nonJson: string[] = [];
  readonly outgoing: Array<{ at: number; message: JsonObject }> = [];
  eofAt: number | undefined;
  private buffer = Buffer.alloc(0);
  private closed = false;

  constructor(private readonly child: ChildProcess) {}

  async start(): Promise<void> {
    this.child.stdout?.on('data', (chunk: Buffer) => {
      this.buffer = Buffer.concat([this.buffer, chunk]);
      for (let index = this.buffer.indexOf(0x0a); index >= 0; index = this.buffer.indexOf(0x0a)) {
        let raw = this.buffer.subarray(0, index);
        this.buffer = this.buffer.subarray(index + 1);
        if (raw.at(-1) === 0x0d) raw = raw.subarray(0, -1);
        if (raw.byteLength === 0) continue;
        const text = raw.toString('utf8');
        const line: Line = { at: performance.now(), bytes: raw.byteLength, text };
        this.lines.push(line);
        try { line.json = JSON.parse(text) as JsonObject; } catch { this.nonJson.push(text.slice(0, 200)); continue; }
        this.onmessage?.(line.json as Parameters<NonNullable<Transport['onmessage']>>[0]);
      }
    });
    this.child.stdout?.on('end', () => { if (this.buffer.byteLength) this.nonJson.push(this.buffer.toString('utf8').slice(0, 200)); });
  }

  async send(message: Parameters<Transport['send']>[0]): Promise<void> {
    this.outgoing.push({ at: performance.now(), message: structuredClone(message) as JsonObject });
    this.writeLine(JSON.stringify(message));
  }

  writeLine(text: string): void {
    if (this.closed) throw new Error('transport closed');
    this.child.stdin?.write(`${text}\n`);
  }

  async close(): Promise<void> {
    if (this.closed) return;
    this.closed = true;
    this.eofAt = performance.now();
    this.child.stdin?.end();
    this.onclose?.();
  }
}

// ------------------------------------------------------------------ one child process
export interface Exchange {
  request?: JsonObject;
  responseLine?: Line;
  result?: JsonObject;
  thrown?: { code?: unknown; message?: unknown; data?: unknown };
  // Server-authored {content, structuredContent, isError} as on the wire (goal D2 measure).
  appBytes?: number;
  // What the client API surfaced: result object, or {code,message,data} of the error.
  clientJson: string;
  clientBytes: number;
  // Whole JSON-RPC response line (newline excluded).
  wireBytes: number | null;
}

export interface ExitInfo { code: number | null; signal: NodeJS.Signals | null; msAfterEof: number | null; forcedKill: boolean }

export interface Counters {
  factoryCalls: number; factoryEras: string[]; entered: number; enteredByTool: Record<string, number>;
  readSnapshotCalls: number; readSnapshotOk: number; readSnapshotErrors: Record<string, number>;
  openAttempts: number; openFailures: Record<string, number>; opens: number; closes: number; openedPaths: string[]; openAtSettle: number; maxConcurrentReads: number;
  openHandlesNow: number; inMemoryOpen: number;
}

export interface SpawnOptions {
  era: Era;
  label: string;
  entry?: 'production' | 'fixture';
  // fixture entry only
  mode?: 'real' | 'controlled';
  catalog?: string;
  clock?: 'real' | 'frozen' | 'advancing';
  version?: string;
  // production probes
  extraArgs?: string[];
  cwd?: string;
  env?: NodeJS.ProcessEnv;
  roots?: Array<{ uri: string; name?: string }>;
}

export class StdioProcess {
  readonly child: ChildProcess;
  readonly transport: PipeTransport;
  readonly client: Client;
  readonly events: JsonObject[] = [];
  readonly stderr: Buffer[] = [];
  readonly exited: Promise<{ code: number | null; signal: NodeJS.Signals | null; at: number }>;
  exitInfo: ExitInfo | undefined;
  rootsRequests = 0;
  negotiated: string | undefined;
  serverVersion: string | undefined;
  private claimed = new Set<unknown>();
  private rawId = 0;

  private constructor(readonly options: SpawnOptions) {
    const fixture = options.entry === 'fixture';
    const args = fixture
      ? [FIXTURE_ENTRY, '--mode', options.mode ?? 'real', '--catalog', options.catalog ?? '', '--clock', options.clock ?? 'advancing', '--version', options.version ?? 'v2-fixture']
      : [PRODUCTION_ENTRY, ...options.extraArgs ?? []];
    this.child = spawn(process.execPath, args, {
      cwd: options.cwd ?? FRONTEND, env: options.env ?? process.env, windowsHide: true,
      stdio: fixture ? ['pipe', 'pipe', 'pipe', 'ipc'] : ['pipe', 'pipe', 'pipe'],
    });
    this.child.stderr?.on('data', (chunk: Buffer) => { this.stderr.push(chunk); });
    this.child.on('message', message => { this.events.push(message as JsonObject); });
    this.exited = new Promise(done => this.child.once('exit', (code, signal) => done({ code, signal, at: performance.now() })));
    this.transport = new PipeTransport(this.child);
    const capabilities = options.roots ? { capabilities: { roots: { listChanged: true } } } : {};
    this.client = new Client({ name: `v2-${options.label}`, version: '0.0.0' }, options.era === 'modern'
      ? { versionNegotiation: { mode: { pin: '2026-07-28' } }, ...capabilities }
      : { supportedProtocolVersions: ['2025-11-25'], ...capabilities });
    if (options.roots) {
      const roots = options.roots;
      this.client.setRequestHandler('roots/list', async () => { this.rootsRequests += 1; return { roots }; });
    }
  }

  static async start(options: SpawnOptions): Promise<StdioProcess> {
    const proc = new StdioProcess(options);
    if (options.entry === 'fixture') await waitFor(() => proc.events.some(event => event.t === 'ready'), 10_000, `${options.label} ready`);
    await proc.client.connect(proc.transport);
    proc.negotiated = proc.client.getNegotiatedProtocolVersion();
    proc.serverVersion = proc.client.getServerVersion()?.version;
    if (proc.negotiated !== REVISION[options.era]) throw new Error(`${options.label}: negotiated ${proc.negotiated} != ${REVISION[options.era]}`);
    return proc;
  }

  get lines(): Line[] { return this.transport.lines; }
  get nonJson(): string[] { return this.transport.nonJson; }
  stderrBytes(): number { return this.stderr.reduce((sum, chunk) => sum + chunk.byteLength, 0); }
  stderrText(): string { return Buffer.concat(this.stderr).toString('utf8'); }

  private responseFor(id: unknown): Line | undefined {
    return this.transport.lines.find(line => line.json && line.json.id === id && ('result' in line.json || 'error' in line.json));
  }

  async call(name: string, args: unknown = {}, options: { signal?: AbortSignal; timeout?: number } = {}): Promise<Exchange> {
    let result: JsonObject | undefined;
    let thrown: Exchange['thrown'];
    const requestOptions = { ...(options.signal ? { signal: options.signal } : {}), ...(options.timeout ? { timeout: options.timeout } : {}) };
    try {
      result = await this.client.callTool({ name, arguments: args as Record<string, unknown> }, requestOptions) as JsonObject;
    } catch (error) {
      const value = error as { code?: unknown; message?: unknown; data?: unknown };
      thrown = { code: value.code, message: value.message, data: value.data };
    }
    const argsJson = JSON.stringify(args);
    const request = this.transport.outgoing.find(entry => entry.message.method === 'tools/call' && !this.claimed.has(entry.message.id)
      && (entry.message.params as JsonObject | undefined)?.name === name
      && JSON.stringify((entry.message.params as JsonObject | undefined)?.arguments) === argsJson)?.message;
    if (request) this.claimed.add(request.id);
    return this.exchange(request, result, thrown);
  }

  private exchange(request: JsonObject | undefined, result: JsonObject | undefined, thrown: Exchange['thrown']): Exchange {
    const responseLine = request ? this.responseFor(request.id) : undefined;
    let clientJson = '';
    if (thrown) clientJson = JSON.stringify({ code: thrown.code, message: thrown.message, data: thrown.data });
    else if (result) clientJson = JSON.stringify(result);
    else if (responseLine?.json && 'error' in responseLine.json) clientJson = JSON.stringify(responseLine.json.error);
    else if (responseLine?.json && 'result' in responseLine.json) clientJson = JSON.stringify(responseLine.json.result);
    const exchange: Exchange = { clientJson, clientBytes: Buffer.byteLength(clientJson, 'utf8'), wireBytes: responseLine?.bytes ?? null };
    if (request) exchange.request = request;
    if (responseLine) exchange.responseLine = responseLine;
    if (result) exchange.result = result;
    if (thrown) exchange.thrown = thrown;
    const wireResult = responseLine?.json?.result as JsonObject | undefined;
    if (wireResult) exchange.appBytes = bytesOf({ content: wireResult.content, structuredContent: wireResult.structuredContent, isError: wireResult.isError });
    return exchange;
  }

  // Raw tools/call (params replaced). The modern per-request `_meta` envelope is cloned
  // from a real client tools/call so only the tested part differs.
  async raw(params: unknown, options: { method?: string; timeoutMs?: number } = {}): Promise<Exchange> {
    const template = this.transport.outgoing.find(entry => entry.message.method === 'tools/call')?.message;
    if (!template) throw new Error('raw() needs one prior client tools/call as envelope template');
    const meta = (template.params as JsonObject | undefined)?._meta;
    const finalParams = meta !== undefined && params !== null && typeof params === 'object' && !Array.isArray(params)
      ? { _meta: structuredClone(meta), ...params as JsonObject } : params;
    const id = `v2-raw-${(this.rawId += 1)}`;
    const request: JsonObject = { jsonrpc: '2.0', id, method: options.method ?? 'tools/call', params: finalParams };
    this.transport.writeLine(JSON.stringify(request));
    await waitFor(() => this.responseFor(id) !== undefined, options.timeoutMs ?? 5_000, `raw ${id}`);
    return this.exchange(request, undefined, undefined);
  }

  // Writes several raw lines in ONE stdin write (pipelined burst).
  writeBurst(requests: JsonObject[]): void {
    this.transport.writeLine(requests.map(request => JSON.stringify(request)).join('\n'));
  }
  envelopeMeta(): unknown {
    const template = this.transport.outgoing.find(entry => entry.message.method === 'tools/call')?.message;
    return (template?.params as JsonObject | undefined)?._meta;
  }
  responseLineFor(id: unknown): Line | undefined { return this.responseFor(id); }

  command(message: JsonObject): void {
    if (!this.child.send) throw new Error('no IPC channel (production entry has none)');
    this.child.send(message);
  }

  async counters(): Promise<Counters> {
    const before = this.events.length;
    this.command({ c: 'counters' });
    await waitFor(() => this.events.slice(before).some(event => event.t === 'counters'), 5_000, 'counters');
    const event = this.events.slice(before).find(item => item.t === 'counters') as unknown as Counters & { t: string };
    return event;
  }

  async waitEvent(predicate: (event: JsonObject) => boolean, from: number, timeoutMs = 5_000, label = 'event'): Promise<JsonObject> {
    await waitFor(() => this.events.slice(from).some(predicate), timeoutMs, label);
    return this.events.slice(from).find(predicate) as JsonObject;
  }

  // Closes the client (stdin EOF) and waits for a natural exit. A kill after the
  // deadline is recorded as forcedKill (a failure cleanup, never a pass).
  async close(): Promise<ExitInfo> {
    if (this.exitInfo) return this.exitInfo;
    await this.client.close().catch(() => undefined);
    if (this.transport.eofAt === undefined) await this.transport.close();
    let forcedKill = false;
    const timer = setTimeout(() => { forcedKill = true; this.child.kill(); }, EXIT_DEADLINE_MS);
    const exit = await this.exited;
    clearTimeout(timer);
    this.exitInfo = { code: exit.code, signal: exit.signal, msAfterEof: this.transport.eofAt === undefined ? null : Math.round(exit.at - this.transport.eofAt), forcedKill };
    return this.exitInfo;
  }

  // Failure cleanup only.
  kill(): void { if (this.child.exitCode === null && this.child.signalCode === null) this.child.kill(); }

  summary(): JsonObject {
    return {
      label: this.options.label, era: this.options.era, entry: this.options.entry ?? 'production', negotiated: this.negotiated, serverVersion: this.serverVersion,
      stdoutLines: this.lines.length, nonJsonStdoutLines: this.nonJson.length, stdoutBytes: this.lines.reduce((sum, line) => sum + line.bytes + 1, 0),
      stderrBytes: this.stderrBytes(), exit: this.exitInfo,
    };
  }
}

// ------------------------------------------------------------------ assertions helpers
export function isJsonRpc(line: Line): boolean {
  const json = line.json;
  if (!json || json.jsonrpc !== '2.0') return false;
  return typeof json.method === 'string' || 'result' in json || 'error' in json;
}

export function structured(exchange: Exchange): JsonObject | undefined {
  return exchange.result?.structuredContent as JsonObject | undefined;
}

export function canonicalBytes(): Buffer { return readFileSync(CANONICAL_CATALOG); }

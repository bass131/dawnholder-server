// V3-R1 (verifier-owned): one stdio child for an arbitrary verifier entry. The V2 harness
// (tests/mcp-v2/harness.ts, read-only for this round) only spawns its two fixed entries, so this
// keeps the same idea in a minimal form: the client Transport writes newline-delimited JSON-RPC to
// stdin and records every raw stdout line, so stdout purity and stderr are observed directly.
import { spawn, type ChildProcess } from 'node:child_process';
import { Client } from '@modelcontextprotocol/client';
import type { Transport } from '@modelcontextprotocol/server';
import type { CallOutcome } from '../mcp-fixtures';

type JsonObject = Record<string, unknown>;

class LineTransport implements Transport {
  onclose: Transport['onclose'];
  onerror: Transport['onerror'];
  onmessage: Transport['onmessage'];
  readonly lines: string[] = [];
  readonly nonJson: string[] = [];
  private buffer = Buffer.alloc(0);

  constructor(private readonly child: ChildProcess) {}

  async start(): Promise<void> {
    this.child.stdout?.on('data', (chunk: Buffer) => {
      this.buffer = Buffer.concat([this.buffer, chunk]);
      for (let index = this.buffer.indexOf(0x0a); index >= 0; index = this.buffer.indexOf(0x0a)) {
        const text = this.buffer.subarray(0, index).toString('utf8').replace(/\r$/, '');
        this.buffer = this.buffer.subarray(index + 1);
        if (!text) continue;
        this.lines.push(text);
        let message: JsonObject;
        try { message = JSON.parse(text) as JsonObject; } catch { this.nonJson.push(text.slice(0, 200)); continue; }
        this.onmessage?.(message as Parameters<NonNullable<Transport['onmessage']>>[0]);
      }
    });
  }

  async send(message: Parameters<Transport['send']>[0]): Promise<void> {
    this.child.stdin?.write(`${JSON.stringify(message)}\n`);
  }

  async close(): Promise<void> {
    this.child.stdin?.end();
    this.onclose?.();
  }
}

export interface RawChild {
  client: Client;
  transport: LineTransport;
  stderr(): string;
  call(name: string, args: JsonObject): Promise<CallOutcome>;
  close(): Promise<{ code: number | null; signal: NodeJS.Signals | null; forcedKill: boolean }>;
}

export async function startRawChild(entry: string, args: string[], era: 'legacy' | 'modern', cwd: string): Promise<RawChild> {
  const child = spawn(process.execPath, [entry, ...args], { cwd, windowsHide: true, stdio: ['pipe', 'pipe', 'pipe'] });
  const stderr: Buffer[] = [];
  child.stderr?.on('data', (chunk: Buffer) => { stderr.push(chunk); });
  const exited = new Promise<{ code: number | null; signal: NodeJS.Signals | null }>(done => child.once('exit', (code, signal) => done({ code, signal })));
  const transport = new LineTransport(child);
  const client = new Client({ name: `v3-r1-${era}`, version: '0.0.0' }, era === 'modern' ? { versionNegotiation: { mode: { pin: '2026-07-28' } } } : { supportedProtocolVersions: ['2025-11-25'] });
  await client.connect(transport);
  return {
    client, transport,
    stderr: () => Buffer.concat(stderr).toString('utf8'),
    async call(name, toolArgs) {
      const result = await client.callTool({ name, arguments: toolArgs }) as JsonObject;
      return { result, authored: { content: result.content, structuredContent: result.structuredContent, isError: result.isError } };
    },
    async close() {
      await client.close().catch(() => undefined);
      let forcedKill = false;
      const timer = setTimeout(() => { forcedKill = true; child.kill(); }, 5_000);
      const exit = await exited;
      clearTimeout(timer);
      return { ...exit, forcedKill };
    },
  };
}

// V2 shared assertions over the stdio harness (verifier-owned). Uses vitest expect, so it is
// imported only by test files; the child fixture entry never imports it.
import { renameSync, writeFileSync } from 'node:fs';
import { expect } from 'vitest';
import { catalogBytes, type CallOutcome } from '../mcp-fixtures';
import { EXIT_DEADLINE_MS, StdioProcess, isJsonRpc, textHash, type Exchange, type ExitInfo, type JsonObject, type SpawnOptions } from './harness';

// Adapts a stdio exchange to the V1 envelope oracle (expectEnvelope/expectSuccess/expectError).
export function outcome(exchange: Exchange): CallOutcome {
  const value: CallOutcome = {};
  if (exchange.result) value.result = exchange.result;
  if (exchange.thrown) value.thrown = { code: exchange.thrown.code, data: exchange.thrown.data, ...(typeof exchange.thrown.message === 'string' ? { message: exchange.thrown.message } : {}) };
  const wire = exchange.responseLine?.json;
  if (wire) {
    value.response = wire;
    const result = wire.result as JsonObject | undefined;
    if (result) value.authored = { content: result.content, structuredContent: result.structuredContent, isError: result.isError };
  }
  return value;
}

// stdout purity, stderr, natural EOF exit within the deadline (no forced kill).
export async function closeAndCheck(proc: StdioProcess, options: { stderrEmpty?: boolean } = {}): Promise<ExitInfo> {
  const exit = await proc.close();
  expect(proc.nonJson, `${proc.options.label} non-JSON stdout`).toEqual([]);
  for (const line of proc.lines) expect(isJsonRpc(line), line.text.slice(0, 120)).toBe(true);
  expect(exit.forcedKill, `${proc.options.label} forced kill`).toBe(false);
  expect(exit.code).toBe(0);
  expect(exit.signal).toBeNull();
  expect(exit.msAfterEof).not.toBeNull();
  expect(exit.msAfterEof as number).toBeLessThan(EXIT_DEADLINE_MS);
  if (options.stderrEmpty ?? true) expect(proc.stderrText()).toBe('');
  console.info(`[V2-MEASURE] exit ${JSON.stringify(proc.summary())}`);
  return exit;
}

// Tracks started processes so afterEach can close them; a forced kill fails the test.
export function processPool() {
  const procs: StdioProcess[] = [];
  return {
    async start(options: SpawnOptions) {
      const proc = await StdioProcess.start(options);
      procs.push(proc);
      return proc;
    },
    async closeAll() {
      for (const proc of procs.splice(0)) {
        if (!proc.exitInfo) {
          const exit = await proc.close();
          expect(exit.forcedKill, `${proc.options.label} needed a forced kill (failure cleanup)`).toBe(false);
        }
      }
    },
  };
}

// Atomic fixture replacement by the test (write temp + rename); returns the D3 text hash.
export function writeCatalog(path: string, catalog: unknown): string {
  const bytes = catalogBytes(catalog);
  const temporary = `${path}.${process.pid}.tmp`;
  writeFileSync(temporary, bytes);
  renameSync(temporary, path);
  return textHash(bytes);
}

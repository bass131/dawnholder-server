// @vitest-environment node
// V1: per-process admission (4 concurrent tool calls, no queue), the monotonic token
// bucket (capacity 10, 10/s), RATE_LIMITED details, slot release on every exit path, and
// cancellation (SDK-suppressed response vs domain REQUEST_CANCELLED). Deterministic:
// paused promises and an injected clock, never sleeps.
import type { McpServer } from '@modelcontextprotocol/server';
import { afterEach, describe, expect, it } from 'vitest';
import type { RecordCatalog } from '../electron/catalog-contract';
import { CatalogReadError } from '../mcp/catalog-errors';
import type { CatalogSnapshot } from '../mcp/catalog-reader';
import { createCatalogServer } from '../mcp/catalog-server';
import {
  ControlledCatalogFile, catalogBytes, connectHarness, expectEnvelope, expectError, expectSuccess, fileBacked, flushMicrotasks,
  linkedCatalog, makeSystem, staticSnapshot, type CallOutcome, type Harness, type ToolName,
} from './mcp-fixtures';

const harnesses: Harness[] = [];
async function open(readSnapshot: (signal: AbortSignal) => Promise<CatalogSnapshot>, now?: () => number) {
  const harness = await connectHarness(now ? { readSnapshot, now } : { readSnapshot });
  harnesses.push(harness);
  return harness;
}
afterEach(async () => { await Promise.all(harnesses.splice(0).map(harness => harness.close())); });

type Mode = 'pass' | 'pause' | 'domain-error' | 'unexpected-error';
// A readSnapshot whose behaviour is switched per phase; paused reads wait for release().
function gatedReader(catalog: RecordCatalog) {
  const snapshot = staticSnapshot(catalog);
  const waiting: Array<() => void> = [];
  const listeners: Array<() => void> = [];
  const state = { mode: 'pass' as Mode, reads: 0, inFlight: 0 };
  return {
    state,
    async readSnapshot(): Promise<CatalogSnapshot> {
      state.reads += 1;
      state.inFlight += 1;
      listeners.splice(0).forEach(listener => listener());
      try {
        if (state.mode === 'pause') await new Promise<void>(resolve => waiting.push(resolve));
        if (state.mode === 'domain-error') throw new CatalogReadError('CATALOG_MISSING');
        if (state.mode === 'unexpected-error') throw new Error('SENTINEL unexpected');
        return snapshot;
      } finally {
        state.inFlight -= 1;
      }
    },
    async waitForReads(count: number) {
      while (state.reads < count) await new Promise<void>(resolve => listeners.push(resolve));
    },
    releaseAll() { waiting.splice(0).forEach(resolve => resolve()); },
  };
}

function retryAfter(outcome: CallOutcome, tool: ToolName = 'list_systems'): number {
  const envelope = expectError(tool, outcome, 'RATE_LIMITED');
  expect(envelope.snapshot).toBeNull();
  expect(envelope.error.retryable).toBe(true);
  const value = envelope.error.details?.retryAfterMs;
  expect(typeof value).toBe('number');
  expect(value as number).toBeGreaterThanOrEqual(1);
  expect(value as number).toBeLessThanOrEqual(1000);
  return value as number;
}

// Occupies every slot with paused reads, proves a fifth call is refused without reading,
// then releases. Returns how many paused calls were admitted (must be exactly 4).
async function probeCapacity(harness: Harness, gated: ReturnType<typeof gatedReader>) {
  const previousMode = gated.state.mode;
  gated.state.mode = 'pause';
  const base = gated.state.reads;
  const admitted = Array.from({ length: 4 }, (_, index) => harness.call('list_systems', { query: `probe-${index}` }));
  await gated.waitForReads(base + 4);
  const extra = await harness.call('get_system', { id: 'sys-alpha' });
  retryAfter(extra, 'get_system');
  expect(gated.state.reads).toBe(base + 4);
  gated.state.mode = 'pass';
  gated.releaseAll();
  for (const outcome of await Promise.all(admitted)) expectSuccess('list_systems', outcome);
  gated.state.mode = previousMode;
  await flushMicrotasks();
  return gated.state.reads - base;
}

function directHandler(server: McpServer, name: ToolName) {
  // Test-tool limitation: the SDK suppresses responses to cancelled requests, so the domain
  // REQUEST_CANCELLED envelope is observed by calling the registered callback directly.
  const tools = (server as unknown as { _registeredTools: Record<string, { handler: (args: unknown, context: unknown) => Promise<Record<string, unknown>> }> })._registeredTools;
  const tool = tools[name];
  if (!tool) throw new Error(`tool ${name} not registered`);
  return (args: Record<string, unknown>, signal: AbortSignal) => tool.handler(args, { mcpReq: { signal } });
}
const asOutcome = (result: Record<string, unknown>): CallOutcome => ({ result, authored: { content: result.content, structuredContent: result.structuredContent, isError: result.isError } });

describe('concurrency: 4 slots, no queue', () => {
  it('a fifth concurrent call is RATE_LIMITED at once without reading the catalog; released slots are reusable', async () => {
    const gated = gatedReader(linkedCatalog());
    const harness = await open(gated.readSnapshot);
    gated.state.mode = 'pause';
    const four = Array.from({ length: 4 }, (_, index) => harness.call('search_records', { query: `q${index}` }));
    await gated.waitForReads(4);
    retryAfter(await harness.call('list_systems', { offset: 1 }));
    retryAfter(await harness.call('get_record', { id: 'rec-1' }), 'get_record');
    expect(gated.state.reads).toBe(4);
    expect(gated.state.inFlight).toBe(4);
    expect(harness.entered).toHaveLength(6);
    gated.state.mode = 'pass';
    gated.releaseAll();
    for (const outcome of await Promise.all(four)) expectSuccess('search_records', outcome);
    expect(await probeCapacity(harness, gated)).toBe(4);
  });

  it('every exit path returns its slot: success, read error, unexpected throw, VERSION_REQUIRED/CONFLICT, NOT_FOUND, RESPONSE_TOO_LARGE, cancellation', async () => {
    const catalog = linkedCatalog();
    catalog.systems.push(makeSystem('big', { behavior: Array.from({ length: 40 }, () => 'b'.repeat(1_000)) }));
    const gated = gatedReader(catalog);
    const harness = await open(gated.readSnapshot);
    const paths: Array<[string, Mode, ToolName, Record<string, unknown>, string]> = [
      ['success', 'pass', 'list_systems', {}, 'ok'],
      ['domain read error', 'domain-error', 'list_systems', {}, 'CATALOG_MISSING'],
      ['unexpected throw', 'unexpected-error', 'get_system', { id: 'sys-alpha' }, 'CATALOG_UNREADABLE'],
      ['VERSION_REQUIRED', 'pass', 'search_records', { offset: 2 }, 'VERSION_REQUIRED'],
      ['VERSION_CONFLICT', 'pass', 'get_record', { id: 'rec-1', expectedHash: 'e'.repeat(64) }, 'VERSION_CONFLICT'],
      ['NOT_FOUND', 'pass', 'get_source', { id: 'nope' }, 'NOT_FOUND'],
      ['RESPONSE_TOO_LARGE', 'pass', 'get_system', { id: 'big' }, 'RESPONSE_TOO_LARGE'],
    ];
    for (const [label, mode, tool, args, expected] of paths) {
      gated.state.mode = mode;
      for (const round of [1, 2]) {
        const outcomes = await Promise.all(Array.from({ length: 4 }, () => harness.call(tool, args)));
        for (const outcome of outcomes) {
          const envelope = expectEnvelope(tool, outcome);
          expect(envelope.ok ? 'ok' : envelope.error.code, `${label} round ${round}`).toBe(expected);
        }
      }
      gated.state.mode = 'pass';
      expect(await probeCapacity(harness, gated), label).toBe(4);
    }
    // Cancellation while the read is paused: the SDK aborts, the handler finishes later.
    gated.state.mode = 'pause';
    const base = gated.state.reads;
    const controllers = Array.from({ length: 4 }, () => new AbortController());
    const cancelled = controllers.map((controller, index) => harness.call('list_systems', { query: `c${index}` }, { signal: controller.signal }));
    await gated.waitForReads(base + 4);
    controllers.forEach(controller => controller.abort());
    gated.state.mode = 'pass';
    gated.releaseAll();
    for (const outcome of await Promise.all(cancelled)) expect(outcome.thrown).toBeDefined();
    await flushMicrotasks();
    expect(gated.state.inFlight).toBe(0);
    expect(await probeCapacity(harness, gated), 'cancellation').toBe(4);
  });
});

describe('token bucket: capacity 10, refill 10/s, monotonic', () => {
  it('limits bursts, refills at 10 per second, caps at 10, and ignores a clock that moves backwards', async () => {
    let now = 0;
    const backing = fileBacked(linkedCatalog());
    const harness = await open(backing.readSnapshot, () => now);
    const ok = async () => expectSuccess('list_systems', await harness.call('list_systems'));
    const limited = async () => retryAfter(await harness.call('list_systems'));
    for (let index = 0; index < 10; index += 1) await ok();
    const first = await limited();
    expect(backing.reads).toBe(10);
    now += 50;
    expect(await limited()).toBeLessThanOrEqual(first);
    now += 50;
    await ok();
    const wait = await limited();
    now += wait;
    await ok();
    // Long idle never exceeds capacity.
    now += 60_000;
    for (let index = 0; index < 10; index += 1) await ok();
    await limited();
    // A backwards jump grants nothing; returning to the high-water mark grants nothing either.
    const highWater = now;
    now -= 5_000;
    await limited();
    now = highWater;
    await limited();
    now += 100;
    await ok();
    await limited();
    console.info(`[V1-MEASURE] bucket firstRetryAfterMs=${first} reads=${backing.reads}`);
  });

  it('rate limiting precedes VERSION_REQUIRED and catalog faults; limited calls neither read nor consume tokens', async () => {
    let now = 0;
    let reads = 0;
    const harness = await open(async () => { reads += 1; throw new CatalogReadError('CATALOG_MISSING'); }, () => now);
    for (let index = 0; index < 10; index += 1) expectError('list_systems', await harness.call('list_systems'), 'CATALOG_MISSING');
    expect(reads).toBe(10);
    retryAfter(await harness.call('list_systems', { offset: 3 }));
    retryAfter(await harness.call('search_records', { offset: 3, systemId: 'x' }), 'search_records');
    for (let index = 0; index < 20; index += 1) retryAfter(await harness.call('get_system', { id: 'x' }), 'get_system');
    expect(reads).toBe(10);
    now += 100;
    expectError('list_systems', await harness.call('list_systems', { offset: 3 }), 'VERSION_REQUIRED');
    retryAfter(await harness.call('list_systems'));
  });

  it('each server instance (one per stdio connection) keeps its own bucket and slots', async () => {
    const now = () => 0;
    const one = await open(fileBacked(linkedCatalog()).readSnapshot, now);
    const two = await open(fileBacked(linkedCatalog()).readSnapshot, now);
    for (let index = 0; index < 10; index += 1) expectSuccess('list_systems', await one.call('list_systems'));
    retryAfter(await one.call('list_systems'));
    expectSuccess('list_systems', await two.call('list_systems'));
  });
});

describe('cancellation', () => {
  for (const abortAware of [false, true]) {
    it(`SDK cancellation (${abortAware ? 'I/O observes signal' : 'I/O ignores signal'}): no response for cancelled ids, handles closed, slots returned`, async () => {
      const file = new ControlledCatalogFile(catalogBytes(linkedCatalog()));
      const gates = Array.from({ length: 4 }, () => file.pause('read', { abortAware }));
      const reader = file.reader();
      const harness = await open(signal => reader.readSnapshot(signal));
      const controllers = Array.from({ length: 4 }, () => new AbortController());
      const calls = controllers.map((controller, index) => harness.call('search_records', { query: `alpha ${index}`.trim() }, { signal: controller.signal }));
      await Promise.all(gates.map(gate => gate.reached));
      controllers.forEach(controller => controller.abort());
      gates.forEach(gate => gate.release());
      const outcomes = await Promise.all(calls);
      await flushMicrotasks();
      for (const outcome of outcomes) {
        expect(outcome.thrown).toBeDefined();
        expect(outcome.result).toBeUndefined();
      }
      const ids = harness.wire.filter(entry => entry.direction === 'client' && entry.message.method === 'tools/call').map(entry => entry.message.id);
      expect(ids).toHaveLength(4);
      const cancelNotes = harness.wire.filter(entry => entry.direction === 'client' && entry.message.method === 'notifications/cancelled');
      expect(cancelNotes).toHaveLength(4);
      const responses = harness.wire.filter(entry => entry.direction === 'server' && ids.includes(entry.message.id));
      expect(responses.filter(entry => (entry.message.result as { structuredContent?: { ok?: unknown } } | undefined)?.structuredContent?.ok === true)).toEqual([]);
      console.info(`[V1-MEASURE] sdk-cancel abortAware=${abortAware} serverResponsesForCancelledIds=${responses.length}`);
      expect(file.openHandles.size).toBe(0);
      expect(file.closed).toBe(file.opened);
      // All four slots and the reader are usable again.
      const after = await Promise.all(Array.from({ length: 4 }, () => harness.call('list_systems')));
      for (const outcome of after) expectSuccess('list_systems', outcome);
    });
  }

  it('domain REQUEST_CANCELLED when the handler observes cancellation with the channel open (during read, after read, before entry)', async () => {
    const file = new ControlledCatalogFile(catalogBytes(linkedCatalog()));
    const reader = file.reader();
    let abortAfterRead: AbortController | undefined;
    let reads = 0;
    let now = 0;
    const server = createCatalogServer({
      version: 'v1-direct', now: () => now,
      readSnapshot: async signal => {
        reads += 1;
        const snapshot = await reader.readSnapshot(signal);
        abortAfterRead?.abort();
        return snapshot;
      },
    });
    const getSystem = directHandler(server, 'get_system');
    const check = (result: Record<string, unknown>) => {
      const envelope = expectError('get_system', asOutcome(result), 'REQUEST_CANCELLED');
      expect(envelope.snapshot).toBeNull();
      expect(envelope.error.retryable).toBe(false);
      expect(JSON.stringify(result).includes('Alpha')).toBe(false);
    };
    // 1. aborted while the read is paused
    const gate = file.pause('read');
    const during = new AbortController();
    const pending = getSystem({ id: 'sys-alpha' }, during.signal);
    await gate.reached;
    during.abort();
    gate.release();
    check(await pending);
    expect(file.openHandles.size).toBe(0);
    // 2. aborted after the snapshot was read, before the response is produced
    abortAfterRead = new AbortController();
    check(await getSystem({ id: 'sys-alpha' }, abortAfterRead.signal));
    abortAfterRead = undefined;
    // 3. already aborted at entry: no read, no token
    const readsBefore = reads;
    const pre = new AbortController();
    pre.abort();
    for (let index = 0; index < 12; index += 1) check(await getSystem({ id: 'sys-alpha' }, pre.signal));
    expect(reads).toBe(readsBefore);
    // Two tokens were consumed above (cases 1 and 2); eight remain at a frozen clock.
    for (let index = 0; index < 8; index += 1) expectSuccess('get_system', asOutcome(await getSystem({ id: 'sys-alpha' }, new AbortController().signal)));
    expect(expectEnvelope('get_system', asOutcome(await getSystem({ id: 'sys-alpha' }, new AbortController().signal))).ok).toBe(false);
    now += 100;
    expectSuccess('get_system', asOutcome(await getSystem({ id: 'sys-alpha' }, new AbortController().signal)));
  });
});

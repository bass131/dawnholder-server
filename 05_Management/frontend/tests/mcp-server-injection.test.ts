// @vitest-environment node
// Requirement: index-v2-design.md 「MCP 서버 주입 지점」 (createCatalogServer takes readGuide,
// readSourceSection and readCheckout; the signal is used for cancellation) and 「MCP」 (admission
// limits stay the same for the new tools), with Astra 보충 v1.1 (msg_6dd9f66e2ca7) Q3.
// The in-memory harness wraps each injected function to record its calls.
import type { McpServer } from '@modelcontextprotocol/server';
import { afterEach, describe, expect, it } from 'vitest';
import type { RecordSource } from '../electron/catalog-contract';
import type { GuideResult } from '../electron/system-guide-contract';
import type { SourceSectionResult } from '../electron/source-section-contract';
import {
  KNOWN_CHECKOUT, connectHarness, expectError, expectSuccess, flushMicrotasks, guideResultOf, linkedCatalog, linkedGuide,
  sectionResultOf, staticSnapshot, type CallOutcome, type Harness, type ToolName,
} from './mcp-fixtures';

let harness: Harness | undefined;
afterEach(async () => { await harness?.close(); harness = undefined; });

const snapshot = staticSnapshot(linkedCatalog());

function gate() {
  let release!: () => void;
  let reached!: () => void;
  const released = new Promise<void>(done => { release = done; });
  const arrived = new Promise<void>(done => { reached = done; });
  return { released, arrived, release: () => release(), reach: () => reached() };
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
const serverOf = (connected: Harness) => {
  const server = connected.servers[0];
  if (!server) throw new Error('no server created');
  return server;
};

describe('createCatalogServer calls the injected functions from tool calls (design 「MCP 서버 주입 지점」)', () => {
  it('list_guide_cards and get_guide_card call readGuide with an AbortSignal', async () => {
    const signals: unknown[] = [];
    harness = await connectHarness({ readSnapshot: async () => snapshot, readGuide: async signal => { signals.push(signal); return guideResultOf(linkedGuide()); } });
    expectSuccess('list_guide_cards', await harness.call('list_guide_cards', {}));
    expectSuccess('get_guide_card', await harness.call('get_guide_card', { id: 'server' }));
    expect(signals).toHaveLength(2);
    for (const signal of signals) expect(signal).toBeInstanceOf(AbortSignal);
  });

  it('read_source_section calls readSourceSection(source, signal) and readCheckout(signal)', async () => {
    const sectionArgs: Array<[RecordSource, unknown]> = [];
    const checkoutSignals: unknown[] = [];
    harness = await connectHarness({
      readSnapshot: async () => snapshot,
      readSourceSection: async (source, signal) => { sectionArgs.push([source, signal]); return sectionResultOf(source, '# 판정 😀\n본문\n'); },
      readCheckout: async signal => { checkoutSignals.push(signal); return KNOWN_CHECKOUT; },
    });
    expectSuccess('read_source_section', await harness.call('read_source_section', { id: 'src-a' }));
    expect(sectionArgs).toHaveLength(1);
    expect(sectionArgs[0]?.[0]).toEqual(snapshot.catalog.sources[0]);
    expect(sectionArgs[0]?.[1]).toBeInstanceOf(AbortSignal);
    expect(checkoutSignals).toHaveLength(1);
    expect(checkoutSignals[0]).toBeInstanceOf(AbortSignal);
  });

  it('the five record tools never call readGuide, readSourceSection or readCheckout (보충 v1.1 Q3)', async () => {
    harness = await connectHarness({ readSnapshot: async () => snapshot });
    expectSuccess('list_systems', await harness.call('list_systems', {}));
    expectSuccess('search_records', await harness.call('search_records', {}));
    expectSuccess('get_system', await harness.call('get_system', { id: 'sys-alpha' }));
    expectSuccess('get_record', await harness.call('get_record', { id: 'rec-1' }));
    expectSuccess('get_source', await harness.call('get_source', { id: 'src-a' }));
    expectError('get_record', await harness.call('get_record', { id: 'no-such-record' }), 'NOT_FOUND');
    expect(harness.injected.snapshot).toBe(6);
    expect(harness.injected.guide).toBe(0);
    expect(harness.injected.section).toEqual([]);
    expect(harness.injected.checkout).toBe(0);
  });

  it('record tools still answer when the guide is broken (보충 v1.1 Q3 reason)', async () => {
    harness = await connectHarness({ readSnapshot: async () => snapshot, readGuide: async () => { throw new Error('SENTINEL_GUIDE_BROKEN'); } });
    expectSuccess('list_systems', await harness.call('list_systems', {}));
    expectSuccess('get_source', await harness.call('get_source', { id: 'src-a' }));
  });
});

describe('cancelled requests do not call the injected functions (design 「MCP 서버 주입 지점」)', () => {
  const newToolCalls: Array<[ToolName, Record<string, unknown>]> = [
    ['read_source_section', { id: 'src-a' }],
    ['list_guide_cards', {}],
    ['get_guide_card', { id: 'server' }],
  ];
  for (const [tool, args] of newToolCalls) {
    it(`${tool} with an already aborted signal is REQUEST_CANCELLED and reads nothing`, async () => {
      harness = await connectHarness({ readSnapshot: async () => snapshot });
      const controller = new AbortController();
      controller.abort();
      const result = await directHandler(serverOf(harness), tool)(args, controller.signal);
      const envelope = expectError(tool, asOutcome(result), 'REQUEST_CANCELLED');
      expect(envelope.snapshot).toBeNull();
      expect(harness.injected.snapshot + harness.injected.guide + harness.injected.section.length + harness.injected.checkout).toBe(0);
    });
  }

  it('cancelling while readSourceSection runs answers REQUEST_CANCELLED and skips readCheckout', async () => {
    const paused = gate();
    harness = await connectHarness({
      readSnapshot: async () => snapshot,
      readSourceSection: async source => { paused.reach(); await paused.released; return sectionResultOf(source, 'x'); },
    });
    const controller = new AbortController();
    const pending = directHandler(serverOf(harness), 'read_source_section')({ id: 'src-a' }, controller.signal);
    await paused.arrived;
    controller.abort();
    paused.release();
    expectError('read_source_section', asOutcome(await pending), 'REQUEST_CANCELLED');
    expect(harness.injected.checkout).toBe(0);
  });

  it('cancelling while readGuide runs answers REQUEST_CANCELLED', async () => {
    const paused = gate();
    harness = await connectHarness({
      readSnapshot: async () => snapshot,
      readGuide: async () => { paused.reach(); await paused.released; return guideResultOf(linkedGuide()); },
    });
    const controller = new AbortController();
    const pending = directHandler(serverOf(harness), 'list_guide_cards')({}, controller.signal);
    await paused.arrived;
    controller.abort();
    paused.release();
    expectError('list_guide_cards', asOutcome(await pending), 'REQUEST_CANCELLED');
  });
});

describe('admission limits apply unchanged to the new tools (design 「MCP」 동시 4건·토큰 10개)', () => {
  it('a fifth concurrent request is RATE_LIMITED without reading, and slots return after the reads finish', async () => {
    const paused = gate();
    let waiting = 0;
    const arrivedAll = gate();
    const server = await connectHarness({
      readSnapshot: async () => snapshot,
      readGuide: async (): Promise<GuideResult> => {
        waiting += 1;
        if (waiting === 4) arrivedAll.reach();
        await paused.released;
        return guideResultOf(linkedGuide());
      },
    });
    harness = server;
    const four = Array.from({ length: 4 }, () => server.call('list_guide_cards', {}));
    // Fail fast instead of timing out when the four calls finish without being held in readGuide.
    await Promise.race([
      arrivedAll.arrived,
      Promise.all(four).then(() => { throw new Error('the four list_guide_cards calls finished before readGuide held them'); }),
    ]);
    const before = server.injected.section.length;
    expectError('read_source_section', await server.call('read_source_section', { id: 'src-a' }), 'RATE_LIMITED');
    expect(server.injected.section.length).toBe(before);
    paused.release();
    for (const outcome of await Promise.all(four)) expectSuccess('list_guide_cards', outcome);
    await flushMicrotasks();
    expectSuccess('read_source_section', await server.call('read_source_section', { id: 'src-a' }));
  });

  it('the eleventh call within one instant is RATE_LIMITED for card and source tools alike', async () => {
    harness = await connectHarness({ readSnapshot: async () => snapshot, now: () => 0 });
    for (let call = 0; call < 5; call += 1) expectSuccess('get_guide_card', await harness.call('get_guide_card', { id: 'server' }));
    for (let call = 0; call < 5; call += 1) expectSuccess('read_source_section', await harness.call('read_source_section', { id: 'src-a' }));
    const guideBefore = harness.injected.guide;
    expectError('list_guide_cards', await harness.call('list_guide_cards', {}), 'RATE_LIMITED');
    expect(harness.injected.guide).toBe(guideBefore);
  });

  it('every failure path of the new tools returns its admission slot', async () => {
    const sourceResults: SourceSectionResult[] = [];
    const held = gate();
    let holdSnapshots = false;
    let heldReads = 0;
    const server = await connectHarness({
      readSnapshot: async () => {
        if (holdSnapshots) { heldReads += 1; await held.released; }
        return snapshot;
      },
      readSourceSection: async source => sourceResults.shift() ?? sectionResultOf(source, 'x'),
      readGuide: async () => ({ ok: false, code: 'missing', message: 'x' }),
    });
    harness = server;
    for (let round = 0; round < 3; round += 1) {
      sourceResults.push({ ok: false, code: 'missing', reason: null, message: 'x' });
      expectError('read_source_section', await server.call('read_source_section', { id: 'src-a' }), 'SOURCE_MISSING');
      expectError('read_source_section', await server.call('read_source_section', { id: 'no-such-source' }), 'NOT_FOUND');
      expectError('list_guide_cards', await server.call('list_guide_cards', {}), 'GUIDE_MISSING');
      expectError('get_guide_card', await server.call('get_guide_card', { id: 'server' }), 'GUIDE_MISSING');
    }
    // After twelve finished calls, four held reads must all be admitted; a leaked slot would refuse one.
    holdSnapshots = true;
    const four = Array.from({ length: 4 }, () => server.call('get_source', { id: 'src-a' }));
    await flushMicrotasks();
    expect(heldReads).toBe(4);
    held.release();
    for (const outcome of await Promise.all(four)) expectSuccess('get_source', outcome);
  });
});

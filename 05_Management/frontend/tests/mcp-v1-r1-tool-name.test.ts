// @vitest-environment node
// V1-R1: independent re-verification of V1-01 (unknown tool names reflected in the SDK
// error and growing with input) after the fix. Requirement source, not the implementation:
// goal D3 — every input refusal on any SDK/handler path is an error, carries no normal data,
// does not echo input and stays ≤ 1,024 JSON UTF-8 bytes as the client observes it, in both
// 2025-11-25 and 2026-07-28; completion condition 4 — arbitrary path/URL/unknown inputs
// are errors without data. Handler entry is counted at the product's tool-handler first
// line (onToolHandlerEntered) and catalog access by reader calls and file opens.
// All calls go through the real Client ↔ public serveStdio ↔ server connection; no SDK
// private member is touched (unlike the V1 admission test that calls a registered handler).
import { InMemoryTransport } from '@modelcontextprotocol/server';
import { Client } from '@modelcontextprotocol/client';
import { afterEach, describe, expect, it } from 'vitest';
import type { CatalogSnapshot } from '../mcp/catalog-reader';
import { createCatalogServer } from '../mcp/catalog-server';
import {
  ControlledCatalogFile, MAX_REFUSAL, TOOL_NAMES, advancingClock, catalogBytes, expectEnvelope, expectError, expectSuccess,
  fileBacked, flushMicrotasks, linkedCatalog, type ToolName,
} from './mcp-fixtures';
import {
  ERAS, RecordingTransport, bytesOf, connectR1, observe, toOutcome, type Era, type Exchange, type JsonObject, type Observation, type R1Harness,
} from './mcp-v1-r1/harness';

const harnesses: R1Harness[] = [];
async function open(era: Era, readSnapshot: (signal: AbortSignal) => Promise<CatalogSnapshot>, now?: () => number) {
  const harness = await connectR1(now ? { era, readSnapshot, now } : { era, readSnapshot });
  harnesses.push(harness);
  return harness;
}
afterEach(async () => { await Promise.all(harnesses.splice(0).map(harness => harness.close().catch(() => undefined))); });

const INVALID_PARAMS = -32602;
const long = (prefix: string, unit: string, count: number) => `${prefix}${unit.repeat(count)}`;

// Short, long, path/URL-shaped, escape/Unicode, near-miss and prototype-key names.
const UNKNOWN_NAMES: Array<[string, string]> = [
  ['short', 'read_file'],
  ['one char', 'x'],
  ['empty', ''],
  ['trailing space', 'list_systems '],
  ['leading space', ' list_systems'],
  ['upper case', 'LIST_SYSTEMS'],
  ['hyphen', 'list-systems'],
  ['prefix of real', 'list_system'],
  ['NUL suffix', 'list_systems\u0000'],
  ['newline suffix', 'get_source\n'],
  ['fullwidth', 'ｌｉｓｔ_systems'],
  ['traversal', 'get_source/../list_systems'],
  ['__proto__', '__proto__'],
  ['constructor', 'constructor'],
  ['toString', 'toString'],
  ['hasOwnProperty', 'hasOwnProperty'],
  ['valueOf', 'valueOf'],
  ['__defineGetter__', '__defineGetter__'],
  ['windows path', 'C:\\Users\\SENTINEL_USER\\secret.txt'],
  ['relative catalog path', '..\\..\\records\\SENTINEL_catalog.json'],
  ['posix path', '/etc/SENTINEL_passwd'],
  ['UNC path', '\\\\SENTINEL-host\\share\\catalog.json'],
  ['file URL', 'file:///C:/SENTINEL_FILE/catalog.json'],
  ['https URL', 'https://SENTINEL.invalid/tool?x=1&y="2"#frag'],
  ['custom scheme', 'mcp://SENTINEL/tools/list_systems'],
  ['quote/backslash', '"SENTINEL_QUOTE"\\'],
  ['script tag', '</script><SENTINEL_TAG>'],
  ['line separators', 'SENTINEL_LS\u2028\u2029'],
  ['lone surrogate', 'SENTINEL_LONE\uD800'],
  ['emoji/Korean', 'SENTINEL_😀_한글_도구'],
  ['RTL override', 'SENTINEL_RTL\u202Egpj.exe'],
  ['zero width', 'SENTINEL\u200B_ZW'],
  ['control chars', '\u0001SENTINEL_CTRL\t\r\n\u001f'],
  ['template-ish', '${SENTINEL_TEMPLATE}%s%n{{x}}'],
  ['5,024 units', long('SENTINEL_TOOL_C:\\secret\\', 'x', 5_000)],
  ['100k units', long('SENTINEL_100K_', 'n', 100_000)],
  ['1M units', long('SENTINEL_1M_', 'm', 1_000_000)],
  ['150 KB Korean', long('SENTINEL_KO_', '한', 50_000)],
  ['emoji 30k pairs', long('SENTINEL_EMOJI_', '😀', 30_000)],
];

// Fragments that must never appear in a refusal (names ≥ 3 units, their JSON-escaped form
// and a prefix of long names are checked per call as well).
const SENTINELS = ['SENTINEL', 'C:\\', 'secret', 'https://', 'file:', '../', '..\\', '\\\\'];

function reflected(json: string, name: string): string[] {
  const needles = [...SENTINELS];
  if (name.length >= 3) {
    needles.push(name.slice(0, 64), JSON.stringify(name).slice(1, -1).slice(0, 64));
  }
  return needles.filter(needle => json.includes(needle));
}

// The fixed refusal for an unknown string tool name: a JSON-RPC protocol error the server
// answered, code -32602 (both revisions' "unknown tool" protocol error), no result/data,
// no reflection, ≤ 1 KiB as the client observes it.
function expectNameRefusal(exchange: Exchange, name: string): Observation {
  expect(exchange.request, 'request reached the wire').toBeDefined();
  expect(exchange.response, 'server answered').toBeDefined();
  expect(exchange.result).toBeUndefined();
  expect('result' in (exchange.response as JsonObject)).toBe(false);
  expect(exchange.thrown).toBeDefined();
  const error = (exchange.response as JsonObject).error as JsonObject;
  expect(error.code).toBe(INVALID_PARAMS);
  expect(exchange.thrown?.code).toBe(INVALID_PARAMS);
  const observed = observe(exchange);
  expect(observed.channel).toBe('protocol-error');
  expect(reflected(observed.clientJson, name)).toEqual([]);
  expect(reflected(JSON.stringify(exchange.response), name)).toEqual([]);
  expect(observed.clientBytes).toBeLessThanOrEqual(MAX_REFUSAL);
  return observed;
}

// Design 「MCP」: the five record tools and the three new tools.
const NORMAL_CALLS: Array<[ToolName, Record<string, unknown>]> = [
  ['list_systems', {}], ['search_records', { systemId: 'sys-alpha' }], ['get_system', { id: 'sys-alpha' }],
  ['get_record', { id: 'rec-1' }], ['get_source', { id: 'src-a' }],
  ['read_source_section', { id: 'src-a' }], ['list_guide_cards', {}], ['get_guide_card', { id: 'server' }],
];
// Card tools read the guide, not the index (Astra 보충 v1.1 Q3).
const INDEX_READING_CALLS = NORMAL_CALLS.filter(([tool]) => tool !== 'list_guide_cards' && tool !== 'get_guide_card').length;

for (const era of ERAS) {
  describe(`${era}: unknown tool names after the V1-01 fix`, () => {
    it('every unknown string name gets one fixed protocol error before the handler and the catalog, and the eight tools still work', async () => {
      const backing = fileBacked(linkedCatalog());
      const harness = await open(era, backing.readSnapshot);
      // An array, not an object: a '__proto__' label would set the prototype instead of a key.
      const observations: Array<{ label: string; units: number; clientBytes: number; wireBytes: number | null }> = [];
      const clientJsons = new Set<string>();
      const wireShapes = new Set<string>();
      for (const [label, name] of UNKNOWN_NAMES) {
        const exchange = await harness.callTool(name, {});
        const observed = expectNameRefusal(exchange, name);
        observations.push({ label, units: name.length, clientBytes: observed.clientBytes, wireBytes: observed.wireBytes });
        clientJsons.add(observed.clientJson);
        wireShapes.add(JSON.stringify({ ...(exchange.response as JsonObject), id: null }));
      }
      // Fixed: identical client-visible error and identical response apart from the id.
      expect([...clientJsons]).toHaveLength(1);
      expect([...wireShapes]).toHaveLength(1);
      expect(observations).toHaveLength(UNKNOWN_NAMES.length);
      expect(harness.entered).toEqual([]);
      expect(backing.reads).toBe(0);
      expect(backing.file.opened).toBe(0);
      console.info(`[R1-MEASURE] unknown-name ${era} fixed=${[...clientJsons][0]} ${JSON.stringify(observations)}`);

      for (const [tool, args] of NORMAL_CALLS) expectSuccess(tool, toOutcome(await harness.callTool(tool, args)));
      expect(harness.entered).toEqual(NORMAL_CALLS.map(([tool]) => tool));
      expect(backing.reads).toBe(INDEX_READING_CALLS);
    });

    it('unknown names with hostile arguments are refused the same way; arguments are not validated, echoed or read', async () => {
      const backing = fileBacked(linkedCatalog());
      const harness = await open(era, backing.readSnapshot);
      const fixed = observe(await harness.callTool('read_file', {})).clientJson;
      const hostile: unknown[] = [
        { path: 'C:\\SENTINEL_ARG_PATH\\secret.txt', url: 'https://SENTINEL_ARG.invalid/' },
        { [long('C:\\SENTINEL_ARG_KEY_', 'k', 30_000)]: 1 },
        Object.fromEntries(Array.from({ length: 300 }, (_, index) => [`SENTINEL_MANY_${index}`, index])),
        { query: long('SENTINEL_Q', 'q', 100_000) },
      ];
      for (const args of hostile) {
        for (const name of ['read_file', 'SENTINEL_TOOL_C:\\secret\\x', '__proto__']) {
          const exchange = await harness.callTool(name, args);
          expect(expectNameRefusal(exchange, name).clientJson).toBe(fixed);
        }
      }
      expect(harness.entered).toEqual([]);
      expect(backing.reads).toBe(0);
    });

    it('known names keep the SDK strict-argument channel, distinct from the name gate, before the handler', async () => {
      const backing = fileBacked(linkedCatalog());
      const harness = await open(era, backing.readSnapshot);
      const nameRefusal = observe(await harness.callTool('read_file', {})).clientJson;
      const strict: Array<[ToolName, unknown]> = [
        ['list_systems', { 'C:\\Users\\SENTINEL_PATH_KEY\\secret.txt': 'SENTINEL_VALUE' }],
        ['get_source', { id: 'src-a', url: 'https://SENTINEL_URL.invalid/' }],
        ['search_records', { [long('C:\\SENTINEL_KEY_', 'k', 30_000)]: 1 }],
        ['get_record', { id: long('SENTINEL_ID', 'i', 100_000) }],
        ['list_systems', ['SENTINEL_ARRAY_ARGS']],
      ];
      const sizes: number[] = [];
      for (const [tool, args] of strict) {
        const exchange = await harness.callTool(tool, args);
        expect(exchange.response, 'server answered').toBeDefined();
        const observed = observe(exchange);
        expect(observed.clientJson).not.toBe(nameRefusal);
        if (observed.channel === 'tool-result') {
          expect(exchange.result?.isError).toBe(true);
          expect((exchange.result?.structuredContent as { ok?: unknown } | undefined)?.ok === true).toBe(false);
        } else {
          expect(observed.channel).toBe('protocol-error');
        }
        expect(reflected(observed.clientJson, 'SENTINEL')).toEqual([]);
        expect(observed.clientBytes).toBeLessThanOrEqual(MAX_REFUSAL);
        sizes.push(observed.clientBytes);
      }
      expect(harness.entered).toEqual([]);
      expect(backing.reads).toBe(0);
      console.info(`[R1-MEASURE] strict-known ${era} clientBytes=${JSON.stringify(sizes)}`);
    });

    it('non-string, missing and raw tool names reach the server, are refused without reflection within 1 KiB, and never enter the handler', async () => {
      const backing = fileBacked(linkedCatalog());
      const harness = await open(era, backing.readSnapshot);
      await harness.callTool('r1_template_probe');
      const variants: Array<[string, unknown]> = [
        ['number', { name: 12345, arguments: {} }],
        ['null', { name: null, arguments: {} }],
        ['boolean', { name: true }],
        ['object', { name: { 'C:\\SENTINEL_OBJ_KEY': 'SENTINEL_OBJ_VALUE' }, arguments: {} }],
        ['array', { name: [long('SENTINEL_ARR_', 'x', 5_000)], arguments: {} }],
        ['missing', { arguments: { SENTINEL_ARG: long('SENTINEL_V', 'v', 5_000) } }],
        ['raw long string', { name: long('SENTINEL_RAW_C:\\secret\\', 'y', 5_000), arguments: {} }],
        ['raw prototype', { name: '__proto__', arguments: {} }],
        ['raw known name, hostile args', { name: 'list_systems', arguments: { 'C:\\SENTINEL_KEY': 'SENTINEL_V' } }],
      ];
      const observations: Array<Observation & { label: string; hasResult: boolean }> = [];
      for (const [label, params] of variants) {
        const exchange = await harness.raw(params);
        const observed = observe(exchange);
        const response = exchange.response;
        expect(response, `${label}: server answered`).toBeDefined();
        const result = response?.result as { isError?: unknown; structuredContent?: { ok?: unknown } } | undefined;
        if (result) {
          expect(result.isError, label).toBe(true);
          expect(result.structuredContent?.ok === true, label).toBe(false);
        }
        expect(reflected(observed.clientJson, 'SENTINEL'), label).toEqual([]);
        expect(reflected(JSON.stringify(response), 'SENTINEL'), label).toEqual([]);
        expect(observed.clientBytes, label).toBeLessThanOrEqual(MAX_REFUSAL);
        observations.push({ label, ...observed, hasResult: result !== undefined });
      }
      expect(harness.entered).toEqual([]);
      expect(backing.reads).toBe(0);
      expect(backing.file.opened).toBe(0);
      console.info(`[R1-MEASURE] raw-names ${era} ${JSON.stringify(observations)}`);
    });

    if (era === 'modern') {
      // Observation for the report: the name gate runs before the SDK's per-request envelope
      // check, so an unknown name without the 2026-07-28 envelope gets the name refusal.
      it('observation: name gate precedence over the per-request envelope check', async () => {
        const backing = fileBacked(linkedCatalog());
        const harness = await open(era, backing.readSnapshot);
        await harness.callTool('r1_template_probe');
        const unknown = await harness.raw({ name: 'SENTINEL_NO_ENVELOPE', arguments: {} }, { keepMeta: false });
        const known = await harness.raw({ name: 'list_systems', arguments: {} }, { keepMeta: false });
        const unknownObserved = observe(unknown);
        const knownObserved = observe(known);
        expect(unknown.response).toBeDefined();
        expect(known.response).toBeDefined();
        for (const observed of [unknownObserved, knownObserved]) {
          expect(reflected(observed.clientJson, 'SENTINEL')).toEqual([]);
          expect(observed.clientBytes).toBeLessThanOrEqual(MAX_REFUSAL);
        }
        expect(harness.entered).toEqual([]);
        expect(backing.reads).toBe(0);
        console.info(`[R1-MEASURE] envelope-precedence ${era} unknown=${unknownObserved.clientJson} known=${knownObserved.clientJson}`);
      });
    }

    // Outside V1-01: the SDK also interpolates prompt names, resource URIs and completion refs
    // in its own errors. Only tools are registered here, so these must stay unreflected too.
    it('other name/URI-carrying and unknown methods are refused without reflection within 1 KiB', async () => {
      const backing = fileBacked(linkedCatalog());
      const harness = await open(era, backing.readSnapshot);
      await harness.callTool('r1_template_probe');
      const requests: Array<[string, string, unknown]> = [
        ['prompts/get', 'prompts/get', { name: long('SENTINEL_PROMPT_C:\\secret\\', 'p', 5_000) }],
        ['resources/read', 'resources/read', { uri: `file:///C:/SENTINEL_RES/${'r'.repeat(5_000)}` }],
        ['resources/templates/list', 'resources/templates/list', {}],
        ['completion/complete prompt', 'completion/complete', { ref: { type: 'ref/prompt', name: long('SENTINEL_REF', 'c', 5_000) }, argument: { name: 'SENTINEL_ARG', value: 'SENTINEL_VALUE' } }],
        ['completion/complete resource', 'completion/complete', { ref: { type: 'ref/resource', uri: 'https://SENTINEL.invalid/x' }, argument: { name: 'a', value: 'b' } }],
        ['logging/setLevel', 'logging/setLevel', { level: long('SENTINEL_LEVEL', 'l', 5_000) }],
        ['unknown method', long('SENTINEL/method_', 'u', 5_000), {}],
      ];
      const observations: Array<Observation & { label: string }> = [];
      for (const [label, method, params] of requests) {
        const exchange = await harness.raw(params, { method });
        const observed = observe(exchange);
        expect(exchange.response, `${label}: server answered`).toBeDefined();
        expect(observed.channel, label).toBe('protocol-error');
        expect(reflected(observed.clientJson, 'SENTINEL'), label).toEqual([]);
        expect(reflected(JSON.stringify(exchange.response), 'SENTINEL'), label).toEqual([]);
        expect(observed.clientBytes, label).toBeLessThanOrEqual(MAX_REFUSAL);
        observations.push({ label, ...observed });
      }
      expect(harness.entered).toEqual([]);
      expect(backing.reads).toBe(0);
      console.info(`[R1-MEASURE] other-methods ${era} ${JSON.stringify(observations)}`);
    });

    it('unknown names do not consume tokens; the bucket still admits exactly 10 calls under a frozen clock', async () => {
      const backing = fileBacked(linkedCatalog());
      const harness = await open(era, backing.readSnapshot, () => 0);
      for (let index = 0; index < 15; index += 1) expectNameRefusal(await harness.callTool(`SENTINEL_unknown_${index}`, {}), `SENTINEL_unknown_${index}`);
      expect(harness.entered).toEqual([]);
      for (let index = 0; index < 10; index += 1) expectSuccess('list_systems', toOutcome(await harness.callTool('list_systems', {})));
      expectError('list_systems', toOutcome(await harness.callTool('list_systems', {})), 'RATE_LIMITED');
      expect(harness.entered).toHaveLength(11);
      expect(backing.reads).toBe(10);
    });

    it('with four calls in flight an unknown name is still refused immediately, takes no slot, and slots return after release', async () => {
      const file = new ControlledCatalogFile(catalogBytes(linkedCatalog()));
      const reader = file.reader();
      const pauses = Array.from({ length: 4 }, () => file.pause('read'));
      const harness = await open(era, signal => reader.readSnapshot(signal));
      const inFlight = Array.from({ length: 4 }, () => harness.callTool('list_systems', {}));
      await Promise.all(pauses.map(pause => pause.reached));
      expectNameRefusal(await harness.callTool('read_file', {}), 'read_file');
      expect(harness.entered).toHaveLength(4);
      const limited = expectError('list_systems', toOutcome(await harness.callTool('list_systems', {})), 'RATE_LIMITED');
      expect(limited.error.details?.retryAfterMs).toBe(100);
      pauses.forEach(pause => pause.release());
      for (const exchange of await Promise.all(inFlight)) expectSuccess('list_systems', toOutcome(exchange));
      const again = Array.from({ length: 4 }, () => file.pause('read'));
      const second = Array.from({ length: 4 }, () => harness.callTool('get_system', { id: 'sys-alpha' }));
      await Promise.all(again.map(pause => pause.reached));
      again.forEach(pause => pause.release());
      for (const exchange of await Promise.all(second)) expectSuccess('get_system', toOutcome(exchange));
      expect(file.opened).toBe(8);
      expect(file.closed).toBe(8);
      expect(harness.entered).toHaveLength(4 + 1 + 4);
    });

    it('SDK cancellation still reaches the handler through the wrapper: no response for the cancelled id, handle closed, slot returned', async () => {
      const file = new ControlledCatalogFile(catalogBytes(linkedCatalog()));
      const reader = file.reader();
      const pause = file.pause('read', { abortAware: true });
      const harness = await open(era, signal => reader.readSnapshot(signal));
      const controller = new AbortController();
      const cancelled = harness.callTool('list_systems', {}, { signal: controller.signal });
      await pause.reached;
      controller.abort();
      const exchange = await cancelled;
      expect(exchange.result).toBeUndefined();
      expect(exchange.thrown).toBeDefined();
      await flushMicrotasks();
      expect(harness.wire.some(entry => entry.direction === 'client' && entry.message.method === 'notifications/cancelled')).toBe(true);
      expect(exchange.request).toBeDefined();
      expect(harness.wire.filter(entry => entry.direction === 'server' && entry.message.id === exchange.request?.id)).toEqual([]);
      expect(file.closed).toBe(file.opened);
      expectNameRefusal(await harness.callTool('read_file', {}), 'read_file');
      const pauses = Array.from({ length: 4 }, () => file.pause('read'));
      const four = Array.from({ length: 4 }, () => harness.callTool('get_record', { id: 'rec-1' }));
      await Promise.all(pauses.map(item => item.reached));
      pauses.forEach(item => item.release());
      for (const result of await Promise.all(four)) expectSuccess('get_record', toOutcome(result));
      expect(file.closed).toBe(file.opened);
    });

    it('closing the connection propagates through the wrapper: the in-flight read is aborted and its handle closed, no success data', async () => {
      const file = new ControlledCatalogFile(catalogBytes(linkedCatalog()));
      const reader = file.reader();
      const pause = file.pause('read', { abortAware: true });
      const harness = await open(era, signal => reader.readSnapshot(signal));
      const pending = harness.callTool('get_system', { id: 'sys-alpha' });
      await pause.reached;
      harnesses.splice(harnesses.indexOf(harness), 1);
      await harness.close();
      const exchange = await pending;
      await flushMicrotasks();
      expect(exchange.result).toBeUndefined();
      expect(exchange.thrown).toBeDefined();
      expect(harness.wire.some(entry => entry.direction === 'server' && entry.message.id === exchange.request?.id && 'result' in entry.message)).toBe(false);
      expect(file.opened).toBe(1);
      expect(file.closed).toBe(1);
      expect(file.openHandles.size).toBe(0);
    });

    it('tools/list still lists exactly the eight strict tools with output schemas, and the name gate admits exactly that set', async () => {
      const backing = fileBacked(linkedCatalog());
      const harness = await open(era, backing.readSnapshot);
      const { tools } = await harness.client.listTools();
      expect(tools.map(tool => tool.name).sort()).toEqual([...TOOL_NAMES].sort());
      for (const tool of tools) {
        expect(tool.inputSchema.additionalProperties, tool.name).toBe(false);
        expect(tool.outputSchema, tool.name).toBeDefined();
        expect(tool.annotations?.readOnlyHint, tool.name).toBe(true);
      }
      // Every listed name passes the gate (reaches the handler); a refusal would be -32602.
      for (const tool of tools) {
        const args = ['list_systems', 'search_records', 'list_guide_cards'].includes(tool.name) ? {} : { id: 'sys-alpha' };
        const envelope = expectEnvelope(tool.name as ToolName, toOutcome(await harness.callTool(tool.name, args)));
        expect(envelope.ok || envelope.error.code === 'NOT_FOUND').toBe(true);
      }
      expect([...harness.entered].sort()).toEqual([...TOOL_NAMES].sort());
    });
  });
}

describe('public Transport contract of the wrapper (direct McpServer.connect, 2025-11-25)', () => {
  async function connectRecording() {
    const [clientSide, serverSide] = InMemoryTransport.createLinkedPair();
    const recording = new RecordingTransport(serverSide);
    const backing = fileBacked(linkedCatalog());
    const entered: string[] = [];
    const server = createCatalogServer({ readSnapshot: backing.readSnapshot, version: 'v1-r1-direct', now: advancingClock(), onToolHandlerEntered: name => { entered.push(name); } });
    const errors: Error[] = [];
    let serverClosed = 0;
    await server.connect(recording);
    server.server.onerror = error => { errors.push(error); };
    server.server.onclose = () => { serverClosed += 1; };
    const client = new Client({ name: 'v1-r1-direct', version: '0.0.0' }, { supportedProtocolVersions: ['2025-11-25'] });
    await client.connect(clientSide);
    return { client, server, recording, backing, entered, errors, get serverClosed() { return serverClosed; } };
  }

  it('forwards start/version/session members, answers unknown names with relatedRequestId, and reports refusal send failures via onerror', async () => {
    const setup = await connectRecording();
    const { client, server, recording, backing, entered, errors } = setup;
    expect(recording.starts).toBe(1);
    expect(recording.supportedVersions.length).toBeGreaterThanOrEqual(1);
    expect(recording.supportedVersions[0]).toContain('2025-11-25');
    expect(recording.protocolVersions).toContain('2025-11-25');
    expect(server.server.transport?.sessionId).toBe('r1-session-id');

    await expect(client.callTool({ name: 'SENTINEL_direct_C:\\secret', arguments: {} })).rejects.toMatchObject({ code: INVALID_PARAMS });
    const refusal = recording.sends.find(entry => 'error' in entry.message);
    expect(refusal).toBeDefined();
    expect(refusal?.options?.relatedRequestId).toBe(refusal?.message.id);
    expect(Object.keys(refusal?.message ?? {}).sort()).toEqual(['error', 'id', 'jsonrpc']);
    expect(JSON.stringify(refusal?.message)).not.toContain('SENTINEL');

    recording.failSend = message => 'error' in message;
    const controller = new AbortController();
    const hanging = client.callTool({ name: 'SENTINEL_send_failure', arguments: {} }, { signal: controller.signal });
    await flushMicrotasks();
    expect(errors).toHaveLength(1);
    expect(errors[0]).toBeInstanceOf(Error);
    expect(errors[0]?.message).toBe('R1 injected send failure');
    controller.abort();
    await expect(hanging).rejects.toBeDefined();
    recording.failSend = undefined;

    const ok = await client.callTool({ name: 'get_source', arguments: { id: 'src-a' } });
    expect((ok.structuredContent as { ok?: unknown }).ok).toBe(true);
    expect(entered).toEqual(['get_source']);
    expect(backing.reads).toBe(1);
    await client.close();
    await server.close();
  });

  it('peer close reaches the server through the wrapper and aborts the in-flight read; server close reaches the transport', async () => {
    const first = await connectRecording();
    const file = new ControlledCatalogFile(catalogBytes(linkedCatalog()));
    const reader = file.reader();
    const pause = file.pause('read', { abortAware: true });
    const [clientSide, serverSide] = InMemoryTransport.createLinkedPair();
    const recording = new RecordingTransport(serverSide);
    const server = createCatalogServer({ readSnapshot: signal => reader.readSnapshot(signal), version: 'v1-r1-direct', now: advancingClock() });
    let closed = 0;
    await server.connect(recording);
    server.server.onclose = () => { closed += 1; };
    const client = new Client({ name: 'v1-r1-direct', version: '0.0.0' }, { supportedProtocolVersions: ['2025-11-25'] });
    await client.connect(clientSide);
    const pending = client.callTool({ name: 'list_systems', arguments: {} });
    await pause.reached;
    await client.close();
    await expect(pending).rejects.toBeDefined();
    await flushMicrotasks();
    expect(closed).toBe(1);
    expect(file.opened).toBe(1);
    expect(file.closed).toBe(1);
    expect(recording.sends.some(entry => 'result' in entry.message && (entry.message.result as JsonObject).structuredContent !== undefined)).toBe(false);

    await first.server.close();
    expect(first.recording.closes).toBe(1);
    expect(first.serverClosed).toBe(1);
    await first.client.close();
  });
});

describe('V1-R1 measurement bookkeeping', () => {
  it('a refusal for a 1M-unit name has the same byte size as for a 1-unit name in both revisions', async () => {
    const sizes: Record<string, number[]> = {};
    for (const era of ERAS) {
      const harness = await open(era, fileBacked(linkedCatalog()).readSnapshot);
      sizes[era] = [];
      for (const name of ['x', long('SENTINEL_', 'z', 1_000_000)]) {
        const exchange = await harness.callTool(name, {});
        sizes[era].push(expectNameRefusal(exchange, name).clientBytes, bytesOf({ ...(exchange.response as JsonObject), id: 0 }));
      }
      expect(sizes[era]?.[0]).toBe(sizes[era]?.[2]);
      expect(sizes[era]?.[1]).toBe(sizes[era]?.[3]);
    }
    console.info(`[R1-MEASURE] size-independence ${JSON.stringify(sizes)}`);
  });
});

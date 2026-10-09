// @vitest-environment node
// V2: input refusal over real stdio in both revisions (goal D3, completion condition 4,
// main msg_3507e1989469, V1-01 hand-over). Criterion: error returned, no normal data, no
// reflection of the input key/value/name, client-observed result/error JSON (SDK fields
// included) <= 1,024 UTF-8 bytes regardless of input length. Handler entry is MEASURED with
// the product's first-line observer (onToolHandlerEntered) injected by the V2 fixture entry,
// plus readSnapshot and file-open counters. The production entry (no seam) is checked for
// the same refusals from the outside. JSON-RPC id/envelope overhead is recorded separately.
// All test data are SENTINEL strings.
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { afterAll, afterEach, describe, expect, it } from 'vitest';
import { MAX_REFUSAL, expectSuccess, linkedCatalog } from './mcp-fixtures';
import { CANONICAL_CATALOG, ERAS, StdioProcess, removeTempRoots, tempRoot, type Era, type Exchange, type JsonObject } from './mcp-v2/harness';
import { closeAndCheck, outcome, processPool, writeCatalog } from './mcp-v2/support';

const pool = processPool();
afterEach(async () => { await pool.closeAll(); });
afterAll(() => { removeTempRoots(); });

const BIG_KEY = `SENTINEL_BIGKEY_${'k'.repeat(27_648 - 16)}`;
const MANY_KEYS = Object.fromEntries(Array.from({ length: 300 }, (_, index) => [`SENTINEL_MANY_${index}`, `SENTINEL_MANYV_${index}`]));
interface ArgCase { label: string; tool: string; args: unknown; sentinels: string[]; raw?: boolean }
const ARG_CASES: ArgCase[] = [
  { label: 'unknown path key', tool: 'list_systems', args: { path: 'C:\\SENTINEL_PATH_VALUE\\catalog.json' }, sentinels: ['SENTINEL_PATH_VALUE', 'path'] },
  { label: 'unknown url key', tool: 'get_source', args: { id: 'src-a', url: 'https://SENTINEL_URL_VALUE.invalid/x' }, sentinels: ['SENTINEL_URL_VALUE', 'url'] },
  { label: 'unknown file/roots keys', tool: 'search_records', args: { file: 'file:///C:/SENTINEL_FILE_VALUE', roots: ['SENTINEL_ROOT_VALUE'] }, sentinels: ['SENTINEL_FILE_VALUE', 'SENTINEL_ROOT_VALUE', 'roots'] },
  { label: 'sensitive-shaped key/value', tool: 'get_system', args: { id: 'sys-alpha', 'C:\\Users\\SENTINEL_SECRET_USER\\.ssh\\id_rsa': 'SENTINEL_TOKEN_sk_live_0000' }, sentinels: ['SENTINEL_SECRET_USER', 'SENTINEL_TOKEN', 'id_rsa'] },
  { label: '27,648-unit unknown key', tool: 'list_systems', args: { [BIG_KEY]: 1 }, sentinels: ['SENTINEL_BIGKEY', 'kkkkkkkkkk'] },
  { label: '300 unknown keys', tool: 'list_systems', args: MANY_KEYS, sentinels: ['SENTINEL_MANY'] },
  { label: 'nested object value', tool: 'list_systems', args: { query: { SENTINEL_NESTED_KEY: 'SENTINEL_NESTED_VALUE' } }, sentinels: ['SENTINEL_NESTED'] },
  { label: 'nested array value', tool: 'search_records', args: { type: ['SENTINEL_ARRAY_ENUM'] }, sentinels: ['SENTINEL_ARRAY_ENUM'] },
  { label: 'query 257 units', tool: 'list_systems', args: { query: `SENTINEL_Q257_${'q'.repeat(257 - 14)}` }, sentinels: ['SENTINEL_Q257'] },
  { label: 'query 100,000 units', tool: 'search_records', args: { query: `SENTINEL_Q100K_${'q'.repeat(100_000 - 15)}` }, sentinels: ['SENTINEL_Q100K'] },
  { label: 'area 129 units', tool: 'list_systems', args: { area: `SENTINEL_AREA_${'a'.repeat(129 - 14)}` }, sentinels: ['SENTINEL_AREA'] },
  { label: 'invalid enum', tool: 'search_records', args: { type: 'SENTINEL_TYPE' }, sentinels: ['SENTINEL_TYPE'] },
  { label: 'empty type', tool: 'search_records', args: { type: '' }, sentinels: [] },
  { label: 'empty systemId', tool: 'search_records', args: { systemId: '' }, sentinels: [] },
  { label: 'systemId 129 units', tool: 'search_records', args: { systemId: `SENTINEL_SYS_${'s'.repeat(129 - 13)}` }, sentinels: ['SENTINEL_SYS'] },
  { label: 'limit 0', tool: 'list_systems', args: { limit: 0 }, sentinels: [] },
  { label: 'limit 51', tool: 'list_systems', args: { limit: 51 }, sentinels: [] },
  { label: 'limit 1.5', tool: 'search_records', args: { limit: 1.5 }, sentinels: [] },
  { label: 'limit -1', tool: 'search_records', args: { limit: -1 }, sentinels: [] },
  { label: 'limit string', tool: 'list_systems', args: { limit: 'SENTINEL_LIMIT' }, sentinels: ['SENTINEL_LIMIT'] },
  { label: 'limit null', tool: 'list_systems', args: { limit: null }, sentinels: [] },
  { label: 'offset -1', tool: 'list_systems', args: { offset: -1 }, sentinels: [] },
  { label: 'offset 100,001', tool: 'search_records', args: { offset: 100_001 }, sentinels: [] },
  { label: 'offset 1e300', tool: 'list_systems', args: { offset: 1e300 }, sentinels: [] },
  { label: 'expectedHash sentinel', tool: 'get_record', args: { id: 'rec-1', expectedHash: 'SENTINEL_HASH' }, sentinels: ['SENTINEL_HASH'] },
  { label: 'expectedHash uppercase', tool: 'get_record', args: { id: 'rec-1', expectedHash: 'A'.repeat(64) }, sentinels: ['AAAAAAAAAAAAAAAA'] },
  { label: 'expectedHash 65 hex', tool: 'list_systems', args: { expectedHash: 'a'.repeat(65) }, sentinels: ['aaaaaaaaaaaaaaaaaaaa'] },
  { label: 'empty id', tool: 'get_system', args: { id: '' }, sentinels: [] },
  { label: 'missing id', tool: 'get_record', args: {}, sentinels: [] },
  { label: 'id 129 units', tool: 'get_source', args: { id: `SENTINEL_ID_${'i'.repeat(129 - 12)}` }, sentinels: ['SENTINEL_ID'] },
  { label: 'id number', tool: 'get_system', args: { id: 12345 }, sentinels: [] },
  { label: 'tool-specific foreign arg', tool: 'get_system', args: { id: 'sys-alpha', limit: 5 }, sentinels: [] },
  { label: 'every field invalid + unknown key', tool: 'search_records', args: { query: 1e300, area: ['SENTINEL_AREA_ARR'], type: 'SENTINEL_T', systemId: '', limit: 1e300, offset: -1e300, expectedHash: 'SENTINEL_H', SENTINEL_EXTRA: 'SENTINEL_EXTRAV' }, sentinels: ['SENTINEL_'] },
  // Design 「도구 규칙」 input bounds of the three new tools.
  { label: 'section path key', tool: 'read_source_section', args: { id: 'src-a', path: 'C:\\SENTINEL_SECTION_PATH\\a.md' }, sentinels: ['SENTINEL_SECTION_PATH'] },
  { label: 'section offset 262,145', tool: 'read_source_section', args: { id: 'src-a', offset: 262_145 }, sentinels: [] },
  { label: 'section hash sentinel', tool: 'read_source_section', args: { id: 'src-a', offset: 1, expectedSectionHash: 'SENTINEL_SECTION_HASH' }, sentinels: ['SENTINEL_SECTION_HASH'] },
  { label: 'card list area key', tool: 'list_guide_cards', args: { area: 'SENTINEL_CARD_AREA' }, sentinels: ['SENTINEL_CARD_AREA'] },
  { label: 'card id 129 units', tool: 'get_guide_card', args: { id: `SENTINEL_CARD_${'c'.repeat(129 - 14)}` }, sentinels: ['SENTINEL_CARD'] },
  { label: 'arguments array (raw)', tool: 'list_systems', args: ['SENTINEL_ARGS_ARRAY'], sentinels: ['SENTINEL_ARGS_ARRAY'], raw: true },
  { label: 'arguments string (raw)', tool: 'list_systems', args: 'SENTINEL_ARGS_STRING', sentinels: ['SENTINEL_ARGS_STRING'], raw: true },
];

const longName = (prefix: string, count: number) => `${prefix}${'x'.repeat(count - prefix.length)}`;
const NAME_CASES: Array<{ label: string; name: string; sentinels: string[] }> = [
  { label: 'short', name: 'read_file', sentinels: ['read_file'] },
  { label: '5,024-unit path-shaped', name: longName('SENTINEL_TOOL_C:\\secret\\', 5_024), sentinels: ['SENTINEL_TOOL', 'secret'] },
  { label: '100,000-unit', name: longName('SENTINEL_TOOL_100K_', 100_000), sentinels: ['SENTINEL_TOOL_100K'] },
  { label: 'windows path', name: 'C:\\Users\\SENTINEL_NAME_USER\\secret.txt', sentinels: ['SENTINEL_NAME_USER', 'secret.txt'] },
  { label: 'relative path', name: '..\\..\\SENTINEL_REL\\catalog.json', sentinels: ['SENTINEL_REL'] },
  { label: 'file URL', name: 'file:///C:/SENTINEL_FILEURL/catalog.json', sentinels: ['SENTINEL_FILEURL', 'file:'] },
  { label: 'https URL', name: 'https://SENTINEL_HTTPS.invalid/tool?x=1&y="2"', sentinels: ['SENTINEL_HTTPS', 'https:'] },
  { label: '__proto__', name: '__proto__', sentinels: ['__proto__'] },
  { label: 'constructor', name: 'constructor', sentinels: ['constructor'] },
  { label: 'near-miss case', name: 'LIST_SYSTEMS', sentinels: ['LIST_SYSTEMS'] },
  { label: 'near-miss space', name: ' list_systems', sentinels: [' list_systems'] },
  { label: 'near-miss new tool', name: 'read_source', sentinels: ['read_source'] },
  { label: 'near-miss new tool case', name: 'GET_GUIDE_CARD', sentinels: ['GET_GUIDE_CARD'] },
];
const NON_STRING_NAMES: Array<{ label: string; params: unknown; sentinels: string[] }> = [
  { label: 'name number', params: { name: 424242, arguments: {} }, sentinels: ['424242'] },
  { label: 'name object', params: { name: { SENTINEL_NAME_OBJ: 'SENTINEL_NAME_OBJV' }, arguments: {} }, sentinels: ['SENTINEL_NAME_OBJ'] },
  { label: 'name array', params: { name: ['SENTINEL_NAME_ARR'], arguments: {} }, sentinels: ['SENTINEL_NAME_ARR'] },
  { label: 'name missing', params: { arguments: { SENTINEL_NONAME: 1 } }, sentinels: ['SENTINEL_NONAME'] },
];

interface Measured { label: string; channel: string; clientBytes: number; wireBytes: number | null; overhead: number | null; entered?: number; reads?: number; opens?: number; code?: unknown }

function noReflection(exchange: Exchange, sentinels: string[]) {
  const responseText = exchange.responseLine?.text ?? '';
  for (const sentinel of sentinels) {
    expect(exchange.clientJson.includes(sentinel), `client reflects ${sentinel.slice(0, 30)}`).toBe(false);
    expect(responseText.includes(sentinel), `wire reflects ${sentinel.slice(0, 30)}`).toBe(false);
  }
}

function refusalShape(exchange: Exchange): string {
  if (exchange.thrown) return 'protocol-error';
  if (exchange.result) {
    expect(exchange.result.isError).toBe(true);
    const structuredContent = exchange.result.structuredContent as { ok?: unknown } | undefined;
    expect(structuredContent?.ok === true).toBe(false);
    return 'tool-result';
  }
  const wire = exchange.responseLine?.json;
  expect(wire, 'server answered').toBeDefined();
  expect(wire && 'error' in wire).toBe(true);
  return 'protocol-error';
}

async function runCases(proc: StdioProcess, counted: boolean): Promise<{ args: Measured[]; names: Measured[]; nonString: Measured[] }> {
  // One real call first: envelope template for raw requests, and proof the seam counts.
  expectSuccess('list_systems', outcome(await proc.call('list_systems', { limit: 1 })));
  const baseline = counted ? await proc.counters() : undefined;
  if (baseline) expect(baseline.entered).toBe(1);
  const measure = async (label: string, exchange: Exchange, sentinels: string[]): Promise<Measured> => {
    const channel = refusalShape(exchange);
    noReflection(exchange, sentinels);
    expect(exchange.clientBytes, label).toBeLessThanOrEqual(MAX_REFUSAL);
    expect(exchange.clientBytes).toBeGreaterThan(0);
    const measured: Measured = { label, channel, clientBytes: exchange.clientBytes, wireBytes: exchange.wireBytes, overhead: exchange.wireBytes === null ? null : exchange.wireBytes - exchange.clientBytes };
    if (exchange.thrown) measured.code = exchange.thrown.code;
    else if (exchange.responseLine?.json && 'error' in exchange.responseLine.json) measured.code = (exchange.responseLine.json.error as JsonObject).code;
    if (counted && baseline) {
      const now = await proc.counters();
      measured.entered = now.entered - baseline.entered;
      measured.reads = now.readSnapshotCalls - baseline.readSnapshotCalls;
      measured.opens = now.opens - baseline.opens;
      expect(measured.entered, `${label}: handler entered`).toBe(0);
      expect(measured.reads, `${label}: readSnapshot`).toBe(0);
      expect(measured.opens, `${label}: file open`).toBe(0);
      expect(now.readGuideCalls - baseline.readGuideCalls, `${label}: readGuide`).toBe(0);
      expect(now.readSourceSectionCalls - baseline.readSourceSectionCalls, `${label}: readSourceSection`).toBe(0);
    }
    return measured;
  };
  const args: Measured[] = [];
  for (const item of ARG_CASES) {
    const exchange = item.raw ? await proc.raw({ name: item.tool, arguments: item.args }) : await proc.call(item.tool, item.args);
    // Every case names a registered tool: its refusal comes from the input schema, not the name gate.
    const message = exchange.thrown?.message ?? (exchange.responseLine?.json?.error as JsonObject | undefined)?.message;
    expect(message, `${item.label}: refused as an unknown tool name`).not.toBe('Unknown tool name.');
    args.push(await measure(item.label, exchange, item.sentinels));
  }
  const names: Measured[] = [];
  for (const item of NAME_CASES) {
    const exchange = await proc.call(item.name, {});
    const measured = await measure(item.label, exchange, item.sentinels);
    expect(exchange.thrown?.code).toBe(-32602);
    expect(exchange.thrown?.message).toBe('Unknown tool name.');
    names.push(measured);
  }
  const nonString: Measured[] = [];
  for (const item of NON_STRING_NAMES) nonString.push(await measure(item.label, await proc.raw(item.params), item.sentinels));
  // The seam still works after all refusals: exactly one more entry for one valid call.
  expectSuccess('get_system', outcome(await proc.call('get_system', { id: (counted ? 'sys-alpha' : await firstSystemId(proc)) })));
  if (counted && baseline) {
    const after = await proc.counters();
    expect(after.entered - baseline.entered).toBe(1);
    expect(after.readSnapshotCalls - baseline.readSnapshotCalls).toBe(1);
  }
  return { args, names, nonString };
}

async function firstSystemId(proc: StdioProcess): Promise<string> {
  const listed = expectSuccess('list_systems', outcome(await proc.call('list_systems', { limit: 1, query: '' })));
  return (listed.data as { items: Array<{ id: string }> }).items[0]?.id ?? '';
}

function summarize(era: Era, entry: string, result: { args: Measured[]; names: Measured[]; nonString: Measured[] }) {
  const all = [...result.args, ...result.names, ...result.nonString];
  const max = all.reduce((best, item) => (item.clientBytes > best.clientBytes ? item : best));
  console.info(`[V2-MEASURE] refusal ${entry} ${era} ${JSON.stringify({ cases: all.length, maxClient: { label: max.label, bytes: max.clientBytes, wire: max.wireBytes }, items: all })}`);
}

describe('strict input and unknown tool name refusal over real stdio', () => {
  it('fixture entry, both revisions concurrently: refused, unreflected, <= 1 KiB, handler/read/open counters 0 for every case', async () => {
    const root = tempRoot('refusal');
    const path = join(root, 'catalog.json');
    writeCatalog(path, linkedCatalog());
    const procs = await Promise.all(ERAS.map(era => pool.start({ era, label: `refusal-fx-${era}`, entry: 'fixture', catalog: path })));
    const results = await Promise.all(procs.map(proc => runCases(proc, true)));
    results.forEach((result, index) => summarize(ERAS[index] as Era, 'fixture', result));
    for (const proc of procs) {
      const all = proc.lines.map(line => line.text).join('\n');
      for (const sentinel of ['SENTINEL_', 'secret', 'id_rsa']) expect(all.includes(sentinel), `stdout contains ${sentinel}`).toBe(false);
      await closeAndCheck(proc);
    }
  }, 60_000);

  it('production entry, both revisions concurrently: same refusals from outside; refusals consume no tokens', async () => {
    const before = readFileSync(CANONICAL_CATALOG);
    const procs = await Promise.all(ERAS.map(era => pool.start({ era, label: `refusal-prod-${era}` })));
    const results = await Promise.all(procs.map(proc => runCases(proc, false)));
    results.forEach((result, index) => summarize(ERAS[index] as Era, 'production', result));
    // ~50 refusals arrived within well under a second; if any had been admitted to the
    // handler they would have drained the 10-token bucket. Five normal calls still succeed.
    for (const proc of procs) {
      for (let index = 0; index < 5; index += 1) expectSuccess('list_systems', outcome(await proc.call('list_systems', { query: `tokens-${index}` })));
      const all = proc.lines.map(line => line.text).join('\n');
      for (const sentinel of ['SENTINEL_', 'secret', 'id_rsa']) expect(all.includes(sentinel), `stdout contains ${sentinel}`).toBe(false);
      await closeAndCheck(proc);
    }
    expect(readFileSync(CANONICAL_CATALOG).equals(before)).toBe(true);
  }, 60_000);
});

describe('malformed stdio input lines (production entry)', () => {
  it('both revisions: garbage lines are not echoed; stdout stays JSON-RPC, stderr carries only a fixed diagnostic; service continues', async () => {
    const procs = await Promise.all(ERAS.map(era => pool.start({ era, label: `garbage-${era}` })));
    for (const proc of procs) {
      expectSuccess('list_systems', outcome(await proc.call('list_systems', { limit: 1 })));
      proc.transport.writeLine('{not json SENTINEL_GARBAGE C:\SENTINEL_GARBAGE_PATH');
      proc.transport.writeLine('[1,2,"SENTINEL_ARRAY_LINE"]');
      proc.transport.writeLine('"SENTINEL_STRING_LINE"');
      const unknownMethod = await proc.raw({ SENTINEL_PARAM: 'SENTINEL_PARAM_VALUE' }, { method: 'SENTINEL_METHOD/../../x' });
      expect((unknownMethod.responseLine?.json?.error as JsonObject | undefined)?.code).toBe(-32601);
      expectSuccess('list_systems', outcome(await proc.call('list_systems', { limit: 2 })));
      const stdout = proc.lines.map(line => line.text).join('\n');
      expect(stdout.includes('SENTINEL')).toBe(false);
      const stderr = proc.stderrText();
      expect(stderr.includes('SENTINEL')).toBe(false);
      expect(stderr.split('\n').filter(Boolean).every(line => line === 'Catalog MCP transport error.')).toBe(true);
      const exit = await closeAndCheck(proc, { stderrEmpty: false });
      console.info(`[V2-MEASURE] garbage-lines ${proc.options.era} ${JSON.stringify({ stderr, unknownMethodClientBytes: unknownMethod.clientBytes, unknownMethodWire: unknownMethod.wireBytes, exit })}`);
    }
  }, 30_000);

  it('a line over the SDK 10 MiB stdio buffer closes only that connection; the process exits 0 by itself without stdout pollution', async () => {
    const [victim, other] = await Promise.all([pool.start({ era: 'legacy', label: 'bigline-legacy' }), pool.start({ era: 'modern', label: 'bigline-other' })]);
    expectSuccess('list_systems', outcome(await victim.call('list_systems', { limit: 1 })));
    victim.child.stdin?.on('error', () => undefined);
    const started = performance.now();
    victim.child.stdin?.write(`"SENTINEL_BIG${'S'.repeat(11 * 1024 * 1024)}\n`);
    const exit = await Promise.race([victim.exited, new Promise<null>(done => setTimeout(() => done(null), 5_000))]);
    expect(exit, 'victim did not exit by itself within 5 s').not.toBeNull();
    expect(exit?.code).toBe(0);
    expect(victim.nonJson).toEqual([]);
    expect(victim.lines.map(line => line.text).join('\n').includes('SENTINEL')).toBe(false);
    expect(victim.stderrText().includes('SENTINEL')).toBe(false);
    // The other client process is unaffected.
    expectSuccess('list_systems', outcome(await other.call('list_systems', { limit: 1 })));
    console.info(`[V2-MEASURE] big-line ${JSON.stringify({ exitCode: exit?.code, signal: exit?.signal, msAfterWrite: exit ? Math.round(exit.at - started) : null, stderr: victim.stderrText() })}`);
    victim.exitInfo = { code: exit?.code ?? null, signal: exit?.signal ?? null, msAfterEof: null, forcedKill: false };
    await closeAndCheck(other);
  }, 30_000);
});

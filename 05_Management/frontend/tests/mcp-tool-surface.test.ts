// @vitest-environment node
// Requirement: index-v2-design.md 「MCP」 (eight tools; the new three are in inputSchemas and
// outputSchemas with read-only annotations; unknown names stay pre-refused) and 「도구 규칙」 input
// bounds, plus 오류 코드 표 아래 문단 (each new code has a fixed Korean message without input,
// locator, path or raw error) and Astra 보충 v1.2 (msg_e854e8f17079) Q6. Wording is not asserted.
import { afterEach, describe, expect, it } from 'vitest';
import type { GuideResult } from '../electron/system-guide-contract';
import type { SourceSectionResult } from '../electron/source-section-contract';
import {
  GUIDE_ERROR_CODES, MAX_SECTION_OFFSET, SOURCE_ERROR_CODES, TOOL_NAMES, connectHarness, expectError, expectRefusal, expectSuccess,
  linkedCatalog, makeSource, sectionResultOf, sha256, staticSnapshot, type ErrorCode, type Harness,
} from './mcp-fixtures';

const NEW_TOOLS = ['read_source_section', 'list_guide_cards', 'get_guide_card'] as const;
const READ_ONLY = { readOnlyHint: true, destructiveHint: false, idempotentHint: true, openWorldHint: false };
const HASH_PATTERN = '^[a-f0-9]{64}$';

let harness: Harness | undefined;
afterEach(async () => { await harness?.close(); harness = undefined; });

async function connect(options: Partial<Parameters<typeof connectHarness>[0]> = {}) {
  harness = await connectHarness({ readSnapshot: async () => staticSnapshot(linkedCatalog()), ...options });
  return harness;
}

describe('tool list (design 「MCP」)', () => {
  it.each(['legacy', 'modern'] as const)('%s: exactly eight tools are listed, the five record tools plus the three new tools', async era => {
    const server = await connect({ era });
    const { tools } = await server.client.listTools();
    expect(tools.map(tool => tool.name).sort()).toEqual([...TOOL_NAMES].sort());
    expect(tools).toHaveLength(8);
  });

  it('the three new tools carry the read-only annotations and an output schema', async () => {
    const { tools } = await (await connect()).client.listTools();
    for (const name of NEW_TOOLS) {
      const tool = tools.find(item => item.name === name);
      expect(tool?.annotations, name).toMatchObject(READ_ONLY);
      expect(tool?.outputSchema, name).toBeDefined();
      expect(tool?.inputSchema.additionalProperties, name).toBe(false);
    }
  });

  it('read_source_section input is { id, offset?, expectedSectionHash?, expectedHash? } with the design bounds and no path argument', async () => {
    const { tools } = await (await connect()).client.listTools();
    const schema = tools.find(item => item.name === 'read_source_section')?.inputSchema;
    expect(schema, 'read_source_section is listed').toBeDefined();
    const properties = schema?.properties as Record<string, Record<string, unknown>>;
    expect(Object.keys(properties).sort()).toEqual(['expectedHash', 'expectedSectionHash', 'id', 'offset']);
    expect(schema?.required).toEqual(['id']);
    expect(properties.id).toMatchObject({ type: 'string', minLength: 1, maxLength: 128 });
    expect(properties.offset).toMatchObject({ type: 'integer', minimum: 0, maximum: MAX_SECTION_OFFSET });
    expect(properties.expectedSectionHash).toMatchObject({ type: 'string', pattern: HASH_PATTERN });
    expect(properties.expectedHash).toMatchObject({ type: 'string', pattern: HASH_PATTERN });
  });

  it('list_guide_cards input is { query?, limit?, offset?, expectedHash? } and get_guide_card input is { id, expectedHash? }', async () => {
    const { tools } = await (await connect()).client.listTools();
    const list = tools.find(item => item.name === 'list_guide_cards')?.inputSchema;
    expect(list, 'list_guide_cards is listed').toBeDefined();
    const listProperties = list?.properties as Record<string, Record<string, unknown>>;
    expect(Object.keys(listProperties).sort()).toEqual(['expectedHash', 'limit', 'offset', 'query']);
    expect(list?.required ?? []).toEqual([]);
    expect(listProperties.query).toMatchObject({ type: 'string', maxLength: 256 });
    expect(listProperties.limit).toMatchObject({ type: 'integer', minimum: 1, maximum: 50 });
    expect(listProperties.offset).toMatchObject({ type: 'integer', minimum: 0, maximum: 100000 });
    expect(listProperties.expectedHash).toMatchObject({ type: 'string', pattern: HASH_PATTERN });

    const detail = tools.find(item => item.name === 'get_guide_card')?.inputSchema;
    const detailProperties = detail?.properties as Record<string, Record<string, unknown>>;
    expect(Object.keys(detailProperties).sort()).toEqual(['expectedHash', 'id']);
    expect(detail?.required).toEqual(['id']);
    expect(detailProperties.id).toMatchObject({ type: 'string', minLength: 1, maxLength: 128 });
  });
});

describe('the transport accepts the new tools and refuses out-of-range input before the handler (design 「도구 규칙」)', () => {
  const SENTINEL = 'SENTINEL_INPUT_VALUE';
  const refused: Array<[typeof NEW_TOOLS[number], Record<string, unknown>]> = [
    ['read_source_section', {}],
    ['read_source_section', { id: '' }],
    ['read_source_section', { id: `${SENTINEL}${'x'.repeat(129)}` }],
    ['read_source_section', { id: 'src-a', offset: -1 }],
    ['read_source_section', { id: 'src-a', offset: MAX_SECTION_OFFSET + 1 }],
    ['read_source_section', { id: 'src-a', offset: 1.5 }],
    ['read_source_section', { id: 'src-a', offset: '1' }],
    ['read_source_section', { id: 'src-a', expectedSectionHash: 'A'.repeat(64) }],
    ['read_source_section', { id: 'src-a', expectedSectionHash: 'a'.repeat(63) }],
    ['read_source_section', { id: 'src-a', expectedHash: `${SENTINEL}` }],
    ['read_source_section', { id: 'src-a', path: `C:\\${SENTINEL}\\guide.md` }],
    ['read_source_section', { id: 'src-a', locator: `docs/${SENTINEL}.md` }],
    ['list_guide_cards', { query: `${SENTINEL}${'q'.repeat(257)}` }],
    ['list_guide_cards', { limit: 0 }],
    ['list_guide_cards', { limit: 51 }],
    ['list_guide_cards', { limit: 1.5 }],
    ['list_guide_cards', { offset: -1 }],
    ['list_guide_cards', { offset: 100001 }],
    ['list_guide_cards', { expectedHash: 'F'.repeat(64) }],
    ['list_guide_cards', { area: SENTINEL }],
    ['get_guide_card', {}],
    ['get_guide_card', { id: '' }],
    ['get_guide_card', { id: 'c'.repeat(129) }],
    ['get_guide_card', { id: 'server', [SENTINEL]: true }],
  ];
  for (const [tool, args] of refused) {
    it(`${tool} refuses ${JSON.stringify(args).slice(0, 70)} without entering the handler or reading`, async () => {
      const server = await connect();
      const outcome = await server.call(tool, args);
      expectRefusal(outcome, [SENTINEL]);
      // The name gate admits the eight registered tools, so this refusal must come from the input
      // schema, not from the unknown-name refusal.
      expect(outcome.thrown?.message, 'refused as an unknown tool name').not.toBe('Unknown tool name.');
      expect(server.entered).toEqual([]);
      expect(server.injected.snapshot + server.injected.guide + server.injected.section.length + server.injected.checkout).toBe(0);
    });
  }

  it('accepts the boundary values: id of 128, offset 262,144, limit 1 and 50, list offset 100,000', async () => {
    const longId = 'a'.repeat(128);
    const text = '# 판정 😀\n본문\n';
    const server = await connect({ readSourceSection: async source => sectionResultOf(source, text) });
    expectError('read_source_section', await server.call('read_source_section', { id: longId }), 'NOT_FOUND');
    const atMax = expectSuccess<{ text: string }>('read_source_section', await server.call('read_source_section', {
      id: 'src-a', offset: MAX_SECTION_OFFSET, expectedSectionHash: sha256(Buffer.from(text, 'utf8')),
    })).data;
    expect(atMax.text).toBe('');
    expectError('get_guide_card', await server.call('get_guide_card', { id: longId }), 'NOT_FOUND');
    for (const limit of [1, 50]) expectSuccess('list_guide_cards', await server.call('list_guide_cards', { limit }));
    expectError('list_guide_cards', await server.call('list_guide_cards', { offset: 100000 }), 'VERSION_REQUIRED');
    expect(server.entered).toEqual(['read_source_section', 'read_source_section', 'get_guide_card', 'list_guide_cards', 'list_guide_cards', 'list_guide_cards']);
  });

  it('near-miss names of the new tools are refused by the name gate before any handler', async () => {
    const server = await connect();
    for (const name of ['read_source', 'Read_Source_Section', 'read_source_section ', 'list_guide_card', 'get_guide_cards', 'guide_cards']) {
      const outcome = await server.call(name, { id: 'src-a' });
      expect(outcome.thrown?.code, name).toBe(-32602);
      expectRefusal(outcome, ['src-a']);
    }
    expect(server.entered).toEqual([]);
  });
});

describe('each new error code has one fixed Korean message without input, locator, path or raw error (design 「MCP」 오류 코드 표 아래 문단)', () => {
  const SENTINEL = 'SENTINEL_RAW_ERROR C:\\secret\\SENTINEL_PATH';
  const sourceFailures: Record<typeof SOURCE_ERROR_CODES[number], [string, string | null]> = {
    SOURCE_NOT_READABLE: ['not-readable', 'extension'],
    SOURCE_PATH_REJECTED: ['path-rejected', 'parent'],
    SOURCE_MISSING: ['missing', null],
    SOURCE_TOO_LARGE: ['too-large', 'file'],
    SOURCE_CHANGED_DURING_READ: ['changed', null],
    SOURCE_INVALID_ENCODING: ['invalid-encoding', null],
    SECTION_MISSING: ['section-missing', null],
    SECTION_AMBIGUOUS: ['section-ambiguous', null],
    SOURCE_UNREADABLE: ['load', null],
  };
  const guideFailures: Record<typeof GUIDE_ERROR_CODES[number], string> = {
    GUIDE_MISSING: 'missing', GUIDE_UNREADABLE: 'load', GUIDE_TOO_LARGE: 'too-large', GUIDE_INVALID: 'invalid', GUIDE_CHANGED_DURING_READ: 'changed',
  };
  const secretSource = makeSource('secret-locator', { title: 'SENTINEL_TITLE 출처', locator: 'docs/SENTINEL_LOCATOR/secret.md', section: 'SENTINEL_SECTION' });

  function expectFixedKorean(code: ErrorCode, messages: Set<string>, wires: string[]) {
    expect(messages.size, `${code}: one fixed message`).toBe(1);
    const [message = ''] = [...messages];
    expect(/[가-힣]/.test(message), `${code}: Korean message`).toBe(true);
    for (const wire of wires) {
      expect(wire.includes('SENTINEL'), `${code}: input, locator, path or raw error leaked`).toBe(false);
      expect(wire.includes('secret-locator'), `${code}: source ID echoed`).toBe(false);
    }
  }

  for (const code of SOURCE_ERROR_CODES) {
    it(`${code}`, async () => {
      const [storeCode, reason] = sourceFailures[code];
      const failure = { ok: false, code: storeCode, reason, message: SENTINEL } as SourceSectionResult;
      const catalog = linkedCatalog();
      catalog.sources.push(secretSource);
      harness = await connectHarness({ readSnapshot: async () => staticSnapshot(catalog), readSourceSection: async () => failure });
      const messages = new Set<string>();
      const wires: string[] = [];
      for (const id of ['secret-locator', 'src-a']) {
        const outcome = await harness.call('read_source_section', { id });
        messages.add(expectError('read_source_section', outcome, code).error.message);
        if (id === 'secret-locator') wires.push(JSON.stringify(outcome.result));
      }
      expectFixedKorean(code, messages, wires);
    });
  }

  for (const code of GUIDE_ERROR_CODES) {
    it(`${code}`, async () => {
      const failure = { ok: false, code: guideFailures[code], message: SENTINEL } as GuideResult;
      const server = await connect({ readGuide: async () => failure });
      const messages = new Set<string>();
      const wires: string[] = [];
      for (const [tool, args] of [['list_guide_cards', { query: 'SENTINEL_QUERY' }], ['get_guide_card', { id: 'secret-locator' }]] as const) {
        const outcome = await server.call(tool, args);
        messages.add(expectError(tool, outcome, code).error.message);
        wires.push(JSON.stringify(outcome.result));
      }
      expectFixedKorean(code, messages, wires);
    });
  }
});

// @vitest-environment node
// Independent verifier test for the judgement order of the source section boundary.
// Requirement: index-v2-design.md 「원문 구간 읽기 경계」 steps 4-7 run in order and an earlier
// rejection stops the later steps (kind -> path shape -> extension -> file system), with the
// 「거절 사례 8종」 app·MCP columns and the 「색인 검사」 codes. The kind and extension checks come from
// one helper result (sourceReadability), so a git `.cs` locator with a bad path shape must still be
// rejected for its path, never for its extension, and a local `.cs` locator for its kind.
// All three sides run the real code on one owned TEMP repository: the app reader with the real
// catalog and source stores, the BUILT MCP entry (mcp-dist/mcp/main.js, whose fixed paths select the
// TEMP root) over stdio, and checkRecordIndex. Expected codes and reasons are copied from the
// design tables, never computed by the product.
import { cpSync, existsSync, readFileSync, rmdirSync, symlinkSync } from 'node:fs';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { Client } from '@modelcontextprotocol/client';
import { StdioClientTransport } from '@modelcontextprotocol/client/stdio';
import { afterAll, beforeAll, describe, expect, it } from 'vitest';
import { createCatalogStore } from '../electron/catalog-store';
import { checkRecordIndex } from '../electron/record-index-check';
import { createSourceSectionReader, createSourceSectionStore } from '../electron/source-section-store';
import { catalogText, gitSource, localSource, systemFixture, type SourceFixture } from './record-sources/catalog-v2-fixture';
import { createOwnedTempRepository, type OwnedTempRepository } from './record-sources/owned-temp-repository';

const FRONTEND = fileURLToPath(new URL('../', import.meta.url));
const CATALOG = '05_Management/records/catalog.json';
const GUIDE = '# 안내\n\n## 개요\n개요 본문\n';

const sources: SourceFixture[] = [];
const git = (id: string, locator: string) => {
  sources.push(gitSource(id, locator, null));
  return id;
};
const local = (id: string, locator: string) => {
  sources.push(localSource(id, locator));
  return id;
};
// Registered at module load so the fixture index contains them.
const ids = {
  markdown: git('guide', 'docs/guide.md'),
  parentCode: git('parent-code', '../a.cs'),
  driveCode: git('drive-code', 'C:/a.cs'),
  absoluteCode: git('absolute-code', '/a.cs'),
  backslashCode: git('backslash-code', 'docs\\a.cs'),
  code: git('code', 'docs/a.cs'),
  missingCode: git('missing-code', 'docs/none.cs'),
  localCode: local('local-code', 'docs/a.cs'),
  localParentCode: local('local-parent-code', '../a.cs'),
};

type Outcome = { ok: true } | { code: string; reason: string | null };
interface ReaderCase { step: string; id: string; app: Outcome; mcp: Outcome }
const READ: Outcome = { ok: true };
const rejected = (step: string, id: string, appCode: string, reason: string, mcpCode: string): ReaderCase => ({
  step, id, app: { code: appCode, reason }, mcp: { code: mcpCode, reason },
});
// Design 「원문 구간 읽기 경계」 steps 4-7 and 「거절 사례 8종」 (app column, MCP column).
const READER_CASES: ReaderCase[] = [
  { step: 'control: a git Markdown file is read', id: ids.markdown, app: READ, mcp: READ },
  rejected('5 parent before 6 extension', ids.parentCode, 'path-rejected', 'parent', 'SOURCE_PATH_REJECTED'),
  rejected('5 drive before 6 extension', ids.driveCode, 'path-rejected', 'drive', 'SOURCE_PATH_REJECTED'),
  rejected('5 absolute before 6 extension', ids.absoluteCode, 'path-rejected', 'absolute', 'SOURCE_PATH_REJECTED'),
  rejected('5 invalid before 6 extension', ids.backslashCode, 'path-rejected', 'invalid', 'SOURCE_PATH_REJECTED'),
  rejected('6 extension of an existing file', ids.code, 'not-readable', 'extension', 'SOURCE_NOT_READABLE'),
  rejected('6 extension before 7 file system', ids.missingCode, 'not-readable', 'extension', 'SOURCE_NOT_READABLE'),
  rejected('4 kind before 6 extension', ids.localCode, 'not-readable', 'local-only', 'SOURCE_NOT_READABLE'),
  rejected('4 kind before 5 path shape', ids.localParentCode, 'not-readable', 'local-only', 'SOURCE_NOT_READABLE'),
];

interface IndexCase { step: string; id: string; diagnostic: { code: string; reason: string | null } | null }
// Design 「색인 검사」: a git path rejection is SOURCE_PATH_REJECTED with the reader reason in its
// cause; a non-Markdown git file without a section is checked for existence only; a local file is
// never looked for, but a local locator outside the repository-relative shape is LOCATOR_INVALID.
const INDEX_CASES: IndexCase[] = [
  { step: 'control: a git Markdown section reads cleanly', id: ids.markdown, diagnostic: null },
  { step: 'git parent path', id: ids.parentCode, diagnostic: { code: 'SOURCE_PATH_REJECTED', reason: 'parent' } },
  { step: 'git drive path', id: ids.driveCode, diagnostic: { code: 'SOURCE_PATH_REJECTED', reason: 'drive' } },
  { step: 'git absolute path', id: ids.absoluteCode, diagnostic: { code: 'SOURCE_PATH_REJECTED', reason: 'absolute' } },
  { step: 'git invalid path', id: ids.backslashCode, diagnostic: { code: 'SOURCE_PATH_REJECTED', reason: 'invalid' } },
  { step: 'existing non-Markdown git file', id: ids.code, diagnostic: null },
  { step: 'missing non-Markdown git file', id: ids.missingCode, diagnostic: { code: 'SOURCE_MISSING', reason: null } },
  { step: 'local file is not looked for', id: ids.localCode, diagnostic: null },
  { step: 'local locator outside the repository shape', id: ids.localParentCode, diagnostic: { code: 'LOCATOR_INVALID', reason: null } },
];

let repository: OwnedTempRepository;
let dependencyLink = '';
let client: Client | undefined;

beforeAll(async () => {
  repository = createOwnedTempRepository();
  repository.write('docs/guide.md', GUIDE);
  repository.write('docs/a.cs', 'class A {}\n');
  repository.write(CATALOG, catalogText({
    schemaVersion: 2,
    sources,
    systems: [systemFixture({ id: 'records-view', sourceIds: [] })],
    records: [],
  }));
  repository.write('05_Management/records/system-guide.json', readFileSync(join(FRONTEND, '..', 'records', 'system-guide.json')));
  // The built entry, read-only copy; package.json keeps its ESM type; dependencies via a junction.
  const frontend = repository.path('05_Management/frontend');
  cpSync(join(FRONTEND, 'mcp-dist'), join(frontend, 'mcp-dist'), { recursive: true, errorOnExist: true, force: false });
  cpSync(join(FRONTEND, 'package.json'), join(frontend, 'package.json'), { errorOnExist: true, force: false });
  dependencyLink = join(frontend, 'node_modules');
  symlinkSync(join(FRONTEND, 'node_modules'), dependencyLink, 'junction');

  // cwd is the unrelated sibling folder: only the module-relative paths may select the root.
  const entry = join(frontend, 'mcp-dist', 'mcp', 'main.js');
  const transport = new StdioClientTransport({ command: process.execPath, args: [entry], cwd: repository.outside, stderr: 'pipe' });
  // Drain the server's diagnostics so a full pipe never stalls it; they are not part of the result.
  transport.stderr?.on('data', () => {});
  client = new Client({ name: 'source-section-order', version: '0.0.0' });
  await client.connect(transport);
}, 60_000);

afterAll(async () => {
  await client?.close();
  // Unlink the dependency junction itself (non-recursive) before removing the owned root.
  if (dependencyLink && existsSync(dependencyLink)) rmdirSync(dependencyLink);
  expect(existsSync(join(FRONTEND, 'node_modules', '@modelcontextprotocol'))).toBe(true);
  repository?.remove();
});

interface Envelope { ok: boolean; error?: { code: string; details?: { reason?: string } } }

async function readThroughMcp(id: string): Promise<Outcome> {
  for (let attempt = 0; attempt < 50; attempt += 1) {
    const result = await client!.callTool({ name: 'read_source_section', arguments: { id } });
    const envelope = result.structuredContent as unknown as Envelope;
    if (envelope.ok) return READ;
    const code = envelope.error?.code ?? 'no-error-code';
    if (code !== 'RATE_LIMITED') return { code, reason: envelope.error?.details?.reason ?? null };
    await new Promise(done => setTimeout(done, 150));
  }
  throw new Error('rate limit never cleared');
}

function appReader() {
  const catalogStore = createCatalogStore(repository.path(CATALOG));
  const store = createSourceSectionStore({ repositoryRoot: repository.root });
  return createSourceSectionReader({ readCatalog: () => catalogStore.read(), store });
}

describe('source section judgement order: kind, then path shape, then extension, then file system', () => {
  it('the app reader and the built MCP entry reject each source at the earliest design step', async () => {
    const reader = appReader();
    const observed: Array<{ step: string; id: string; app: Outcome; mcp: Outcome }> = [];
    for (const { step, id, app, mcp } of READER_CASES) {
      const appResult = await reader.read(id);
      const appSeen: Outcome = appResult.ok ? READ : { code: appResult.code, reason: appResult.reason };
      const mcpSeen = await readThroughMcp(id);
      observed.push({ step, id, app: appSeen, mcp: mcpSeen });
      expect(appSeen, `${step} ${id} app`).toEqual(app);
      expect(mcpSeen, `${step} ${id} mcp`).toEqual(mcp);
    }
    console.info(`[source-section-order] reader ${JSON.stringify(observed)}`);
  }, 60_000);

  it('the record index check reports a git path rejection for its path even when the extension is not Markdown', async () => {
    const result = await checkRecordIndex({ repositoryRoot: repository.root });
    const observed: Array<{ step: string; id: string; diagnostics: string[] }> = [];
    for (const { step, id, diagnostic } of INDEX_CASES) {
      const found = result.diagnostics.filter(item => item.location.includes(`sources[${id}]`));
      const seenCodes = found.map(item => `${item.severity} ${item.code}`);
      observed.push({ step, id, diagnostics: found.map(item => `${item.severity} ${item.code} ${item.cause}`) });
      expect(seenCodes, `${step} ${id}`).toEqual(diagnostic ? [`error ${diagnostic.code}`] : []);
      if (diagnostic?.reason) expect(found[0]?.cause, `${step} ${id} cause`).toContain(diagnostic.reason);
    }
    console.info(`[source-section-order] index ${JSON.stringify(observed)}`);
  });
});

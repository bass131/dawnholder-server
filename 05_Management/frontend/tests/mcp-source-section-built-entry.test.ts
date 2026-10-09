// @vitest-environment node
// Independent verifier test (PR2 step 6). Requirement: main decision msg_4c7e21aeead6 item 2 (the
// eight rejection cases on both the app and MCP sides), goal 완료조건 2·4 and index-v2-design.md
// 「거절 사례 8종」, 「MCP」 오류 코드 표 and 「MCP 서버 주입 지점」 (main.ts joins the real stores to
// the fixed root `mcp-dist/mcp/main.js` -> `../../../../`).
// The other rejection tests inject the stores into an in-memory server. This file runs the BUILT
// production entry unchanged from an owned TEMP layout, so the fixed-path wiring itself selects the
// fixture repository; the app reader reads the same files for a side-by-side table. Expected
// code·reason pairs are copied from the design table, never computed by the product.
import { cpSync, existsSync, readdirSync, readFileSync, rmdirSync, symlinkSync } from 'node:fs';
import { createHash } from 'node:crypto';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { Client } from '@modelcontextprotocol/client';
import { StdioClientTransport } from '@modelcontextprotocol/client/stdio';
import { afterAll, beforeAll, describe, expect, it } from 'vitest';
import { createCatalogStore } from '../electron/catalog-store';
import { createSourceSectionReader, createSourceSectionStore } from '../electron/source-section-store';
import { catalogText, gitSource, handoffSource, localSource, systemFixture, type SourceFixture } from './record-sources/catalog-v2-fixture';
import { createOwnedTempRepository, type OwnedTempRepository } from './record-sources/owned-temp-repository';

const FRONTEND = fileURLToPath(new URL('../', import.meta.url));
const MIB = 1024 * 1024;
const SECTION_LIMIT = 256 * 1024;
const GUIDE = '# 안내\n\n## 개요\n개요 본문\n\n## 다음\n끝\n';
const BRANCH = 'verify/built-entry';
const HEAD_HASH = '0123456789abcdef0123456789abcdef01234567';

function sectionOfBytes(heading: string, bytes: number): string {
  const head = `# ${heading}\n`;
  return `${head}${'x'.repeat(bytes - Buffer.byteLength(head) - 1)}\n`;
}
function fileOfBytes(bytes: number): string {
  const tail = '## 끝\n짧은 본문\n';
  return `${'y'.repeat(bytes - Buffer.byteLength(tail) - 1)}\n${tail}`;
}

const sources: SourceFixture[] = [];
const git = (id: string, locator: string, section: string | null = null) => {
  sources.push(gitSource(id, locator, section));
  return id;
};
const ids = {
  overview: git('guide-overview', 'docs/guide.md', '개요'),
  parent: git('parent', '../outside.md'),
  parentInside: git('parent-inside', 'docs/../../outside.md'),
  absolute: git('absolute', '/outside.md'),
  unc: git('unc', '//server/share/a.md'),
  drive: git('drive', 'C:/outside.md'),
  driveRelative: git('drive-relative', 'c:outside.md'),
  caseOnly: git('case-only', 'case/guide.md'),
  junction: git('junction', 'linked/secret.md'),
  junctionInside: git('junction-inside', 'linked-inside/guide.md'),
  extensionCs: git('extension-cs', 'docs/a.cs'),
  extensionUpper: git('extension-upper', 'docs/a.MD'),
  extensionDouble: git('extension-double', 'docs/a.md.txt'),
  fileOver: git('file-over', 'big/file-over.md', '끝'),
  sectionOver: git('section-over', 'big/section-over.md', '큰 구간'),
  missing: git('missing', 'docs/none.md'),
};
const LOCAL = 'local-run-log';
const HANDOFF = 'main-decision';

interface Expected { app: { code: string; reason: string | null }; mcp: { code: string; reason: string | null } }
const row = (appCode: string, reason: string | null, mcpCode: string): Expected => ({ app: { code: appCode, reason }, mcp: { code: mcpCode, reason } });
// Design 「거절 사례 8종」 table (app column, MCP column) and 「MCP」 오류 코드 표.
const TABLE: Array<[string, string, Expected]> = [
  ['1 parent', ids.parent, row('path-rejected', 'parent', 'SOURCE_PATH_REJECTED')],
  ['1 parent', ids.parentInside, row('path-rejected', 'parent', 'SOURCE_PATH_REJECTED')],
  ['2 absolute', ids.absolute, row('path-rejected', 'absolute', 'SOURCE_PATH_REJECTED')],
  ['2 absolute', ids.unc, row('path-rejected', 'absolute', 'SOURCE_PATH_REJECTED')],
  ['3 drive', ids.drive, row('path-rejected', 'drive', 'SOURCE_PATH_REJECTED')],
  ['3 drive', ids.driveRelative, row('path-rejected', 'drive', 'SOURCE_PATH_REJECTED')],
  ['5 link', ids.junction, row('path-rejected', 'link', 'SOURCE_PATH_REJECTED')],
  ['5 link', ids.junctionInside, row('path-rejected', 'link', 'SOURCE_PATH_REJECTED')],
  ['6 extension', ids.extensionCs, row('not-readable', 'extension', 'SOURCE_NOT_READABLE')],
  ['6 extension', ids.extensionUpper, row('not-readable', 'extension', 'SOURCE_NOT_READABLE')],
  ['6 extension', ids.extensionDouble, row('not-readable', 'extension', 'SOURCE_NOT_READABLE')],
  ['7 size', ids.fileOver, row('too-large', 'file', 'SOURCE_TOO_LARGE')],
  ['7 size', ids.sectionOver, row('too-large', 'section', 'SOURCE_TOO_LARGE')],
  ['8 unregistered', 'no-such-source', row('unknown-source', null, 'NOT_FOUND')],
  ['broken link', ids.missing, row('missing', null, 'SOURCE_MISSING')],
  ['local', LOCAL, row('not-readable', 'local-only', 'SOURCE_NOT_READABLE')],
  ['handoff', HANDOFF, row('not-readable', 'handoff', 'SOURCE_NOT_READABLE')],
];

let repository: OwnedTempRepository;
let dependencyLink = '';
let entry = '';
let catalogBytes: Buffer;
let caseInsensitive = false;
let client: Client | undefined;
let transport: StdioClientTransport | undefined;
const stderr: Buffer[] = [];

beforeAll(async () => {
  repository = createOwnedTempRepository();
  // Repository files the fixed root resolves to.
  repository.write('docs/guide.md', GUIDE);
  repository.write('case/Guide.md', GUIDE);
  caseInsensitive = existsSync(repository.path('case/guide.md'));
  for (const name of ['a.cs', 'a.MD', 'a.md.txt']) repository.write(`docs/${name}`, GUIDE);
  repository.write('../outside/secret.md', '# 비밀\n밖의 파일\n');
  symlinkSync(repository.outside, repository.path('linked'), 'junction');
  symlinkSync(repository.path('docs'), repository.path('linked-inside'), 'junction');
  repository.write('big/file-over.md', fileOfBytes(MIB + 1));
  repository.write('big/section-over.md', `${sectionOfBytes('큰 구간', SECTION_LIMIT + 1)}# 다음\n끝\n`);
  // A plain .git directory: the checkout store reads HEAD and the loose branch ref only.
  repository.write('.git/HEAD', `ref: refs/heads/${BRANCH}\n`);
  repository.write(`.git/refs/heads/${BRANCH}`, `${HEAD_HASH}\n`);
  catalogBytes = Buffer.from(catalogText({
    schemaVersion: 2,
    sources: [...sources, localSource(LOCAL, '.backups/run.log'), handoffSource(HANDOFF, 'msg_0123456789ab')],
    systems: [systemFixture({ id: 'records-view', sourceIds: [] })],
    records: [],
  }), 'utf8');
  repository.write('05_Management/records/catalog.json', catalogBytes);
  repository.write('05_Management/records/system-guide.json', readFileSync(join(FRONTEND, '..', 'records', 'system-guide.json')));
  // The built entry, read-only copy; package.json keeps its ESM type; dependencies via a junction.
  const frontend = repository.path('05_Management/frontend');
  cpSync(join(FRONTEND, 'mcp-dist'), join(frontend, 'mcp-dist'), { recursive: true, errorOnExist: true, force: false });
  cpSync(join(FRONTEND, 'package.json'), join(frontend, 'package.json'), { errorOnExist: true, force: false });
  dependencyLink = join(frontend, 'node_modules');
  symlinkSync(join(FRONTEND, 'node_modules'), dependencyLink, 'junction');
  entry = join(frontend, 'mcp-dist', 'mcp', 'main.js');

  // cwd is the unrelated sibling folder: only the module-relative paths may select the root.
  transport = new StdioClientTransport({ command: process.execPath, args: [entry], cwd: repository.outside, stderr: 'pipe' });
  transport.stderr?.on('data', (chunk: Buffer) => { stderr.push(chunk); });
  client = new Client({ name: 'pr2-verifier-built-entry', version: '0.0.0' });
  await client.connect(transport);
}, 60_000);

afterAll(async () => {
  await client?.close();
  // Unlink the dependency junction itself (non-recursive) before removing the owned root.
  if (dependencyLink && existsSync(dependencyLink)) rmdirSync(dependencyLink);
  expect(existsSync(join(FRONTEND, 'node_modules', '@modelcontextprotocol'))).toBe(true);
  repository?.remove();
});

interface Envelope { ok: boolean; snapshot: { hash: string } | null; data?: Record<string, unknown>; error?: { code: string; details?: { reason?: string } } }

async function readSection(id: string): Promise<Envelope> {
  for (let attempt = 0; attempt < 50; attempt += 1) {
    const result = await client!.callTool({ name: 'read_source_section', arguments: { id } });
    const envelope = result.structuredContent as unknown as Envelope;
    if (envelope.ok || envelope.error?.code !== 'RATE_LIMITED') return envelope;
    await new Promise(done => setTimeout(done, 150));
  }
  throw new Error('rate limit never cleared');
}

function appReader() {
  const catalogStore = createCatalogStore(repository.path('05_Management/records/catalog.json'));
  return createSourceSectionReader({ readCatalog: () => catalogStore.read(), store: createSourceSectionStore({ repositoryRoot: repository.root }) });
}

describe('built MCP entry from an owned TEMP layout: read_source_section through main.ts fixed paths', () => {
  it('reads a registered Markdown section under the fixed root and reports that checkout', async () => {
    const envelope = await readSection(ids.overview);
    expect(envelope.ok, JSON.stringify(envelope).slice(0, 300)).toBe(true);
    expect(envelope.snapshot).toEqual({ hash: createHash('sha256').update(catalogBytes).digest('hex') });
    expect(envelope.data).toMatchObject({
      path: 'docs/guide.md', heading: '개요', text: '## 개요\n개요 본문\n\n',
      checkout: { state: 'known', branch: BRANCH, head: HEAD_HASH },
      paging: { offset: 0, returned: '## 개요\n개요 본문\n\n'.length, total: '## 개요\n개요 본문\n\n'.length, nextOffset: null },
    });
  });

  it('answers the eight rejection cases (and broken, local, handoff links) with the design code·reason on both sides', async () => {
    const reader = appReader();
    const indexHash = createHash('sha256').update(catalogBytes).digest('hex');
    const observed: Array<{ label: string; id: string; app: unknown; mcp: unknown }> = [];
    const table = [...TABLE, ['4 case', ids.caseOnly, caseInsensitive
      ? row('path-rejected', 'case', 'SOURCE_PATH_REJECTED') : row('missing', null, 'SOURCE_MISSING')] as [string, string, Expected]];
    for (const [label, id, expected] of table) {
      const app = await reader.read(id);
      const mcp = await readSection(id);
      const appSeen = app.ok ? { ok: true } : { code: app.code, reason: app.reason };
      const mcpSeen = mcp.ok ? { ok: true } : { code: mcp.error?.code, reason: mcp.error?.details?.reason ?? null };
      observed.push({ label, id, app: appSeen, mcp: mcpSeen });
      expect(appSeen, `${label} ${id} app`).toEqual(expected.app);
      expect(mcpSeen, `${label} ${id} mcp`).toEqual(expected.mcp);
      // Design 「도구 규칙」: index-backed source errors and NOT_FOUND carry the index { hash }.
      expect(mcp.snapshot, `${label} ${id} snapshot`).toEqual({ hash: indexHash });
    }
    console.info(`[PR2-V1] built-entry rejection table caseInsensitive=${caseInsensitive} ${JSON.stringify(observed)}`);
  }, 60_000);

  it('never writes into the fixture repository root and keeps stderr free of locators', async () => {
    const before = readdirSync(repository.root).sort();
    for (const [, id] of TABLE) await readSection(id);
    expect(readdirSync(repository.root).sort()).toEqual(before);
    const text = Buffer.concat(stderr).toString('utf8');
    for (const locator of ['outside.md', 'secret.md', 'a.md.txt', 'file-over.md']) expect(text).not.toContain(locator);
  }, 60_000);
});

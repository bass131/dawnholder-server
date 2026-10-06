// @vitest-environment node
// Requirement: main decision msg_4c7e21aeead6 item 2 (the eight rejection cases, MCP column) and
// index-v2-design.md 「거절 사례 8종」 with 「MCP」 오류 코드 표: read_source_section answers each
// case with the listed code and `details.reason`. The first seven cases read files under an owned
// TEMP repository through the real createSourceSectionStore injected as readSourceSection; the
// eighth is an unregistered ID. Expected code·reason pairs are copied from the design table.
import { existsSync, symlinkSync } from 'node:fs';
import { afterAll, afterEach, beforeAll, describe, expect, it } from 'vitest';
import type { RecordCatalog, RecordSource } from '../electron/catalog-contract';
import { createCheckoutStore } from '../electron/checkout-store';
import { createSourceSectionStore } from '../electron/source-section-store';
import { connectHarness, expectEnvelope, expectSuccess, makeCatalog, makeSource, sha256, staticSnapshot, type Harness } from './mcp-fixtures';
import { createOwnedTempRepository, type OwnedTempRepository } from './record-sources/owned-temp-repository';

const MIB = 1024 * 1024;
const SECTION_LIMIT = 256 * 1024;
const GUIDE = '\uFEFF# 안내\n\n## 개요\n개요 본문\n\n## 다음\n끝\n';

// A section whose heading line through its last newline is exactly `bytes` UTF-8 bytes.
function sectionOfBytes(heading: string, bytes: number): string {
  const head = `# ${heading}\n`;
  return `${head}${'x'.repeat(bytes - Buffer.byteLength(head) - 1)}\n`;
}
// A file of exactly `bytes` bytes ending in a short section titled 끝.
function fileOfBytes(bytes: number): string {
  const tail = '## 끝\n짧은 본문\n';
  return `${'y'.repeat(bytes - Buffer.byteLength(tail) - 1)}\n${tail}`;
}

let repository: OwnedTempRepository;
let caseInsensitive = false;
let fileLinkError: NodeJS.ErrnoException | null = null;
let harness: Harness | undefined;

const sources: RecordSource[] = [];
const git = (id: string, locator: string, section: string | null = null) => {
  sources.push(makeSource(id, { locator, section }));
  return id;
};
// Registered at module load so the fixture index contains them.
const ids = {
  overview: git('guide-overview', 'docs/guide.md', '개요'),
  whole: git('guide-whole', 'docs/guide.md'),
  parent: git('parent', '../outside.md'),
  parentInside: git('parent-inside', 'docs/../../outside.md'),
  absolute: git('absolute', '/outside.md'),
  unc: git('unc', '//server/share/a.md'),
  drive: git('drive', 'C:/outside.md'),
  driveRelative: git('drive-relative', 'c:outside.md'),
  caseOnly: git('case-only', 'case/guide.md'),
  junction: git('junction', 'linked/secret.md'),
  junctionInside: git('junction-inside', 'linked-inside/guide.md'),
  fileLink: git('file-link', 'docs/file-link.md'),
  extensionCs: git('extension-cs', 'docs/a.cs'),
  extensionUpper: git('extension-upper', 'docs/a.MD'),
  extensionDouble: git('extension-double', 'docs/a.md.txt'),
  fileLimit: git('file-limit', 'big/file-limit.md', '끝'),
  fileOver: git('file-over', 'big/file-over.md', '끝'),
  sectionLimit: git('section-limit', 'big/section-limit.md', '큰 구간'),
  sectionOver: git('section-over', 'big/section-over.md', '큰 구간'),
};
const catalog: RecordCatalog = makeCatalog({ sources });

beforeAll(() => {
  repository = createOwnedTempRepository();
  repository.write('docs/guide.md', GUIDE);
  repository.write('case/Guide.md', GUIDE);
  caseInsensitive = existsSync(repository.path('case/guide.md'));
  for (const name of ['a.cs', 'a.MD', 'a.md.txt']) repository.write(`docs/${name}`, GUIDE);
  repository.write('../outside/secret.md', '# 비밀\n밖의 파일\n');
  symlinkSync(repository.outside, repository.path('linked'), 'junction');
  symlinkSync(repository.path('docs'), repository.path('linked-inside'), 'junction');
  try {
    symlinkSync(`${repository.outside}/secret.md`, repository.path('docs/file-link.md'), 'file');
  } catch (error) {
    fileLinkError = error as NodeJS.ErrnoException;
  }
  repository.write('big/file-limit.md', fileOfBytes(MIB));
  repository.write('big/file-over.md', fileOfBytes(MIB + 1));
  repository.write('big/section-limit.md', `${sectionOfBytes('큰 구간', SECTION_LIMIT)}# 다음\n끝\n`);
  repository.write('big/section-over.md', `${sectionOfBytes('큰 구간', SECTION_LIMIT + 1)}# 다음\n끝\n`);
});
afterAll(() => repository?.remove());
afterEach(async () => { await harness?.close(); harness = undefined; });

// The real boundary module reads the owned TEMP root; the index snapshot is the fixture above.
async function connectRealStore() {
  const store = createSourceSectionStore({ repositoryRoot: repository.root });
  const checkout = createCheckoutStore({ repositoryRoot: repository.root });
  harness = await connectHarness({
    readSnapshot: async () => staticSnapshot(catalog),
    readSourceSection: source => store.read(source),
    readCheckout: () => checkout.read(),
  });
  return harness;
}

// Observed error code and details of one read; details is null when the envelope omits it.
async function rejection(id: string) {
  const server = await connectRealStore();
  const envelope = expectEnvelope('read_source_section', await server.call('read_source_section', { id }));
  await harness?.close();
  harness = undefined;
  if (envelope.ok) return { ok: true };
  return { code: envelope.error.code, details: envelope.error.details ?? null };
}

describe('control: registered Markdown inside the owned root reads through the real store', () => {
  it('reads a heading section and a whole file (BOM removed) with the real checkout result of a root without .git', async () => {
    const server = await connectRealStore();
    const overview = expectSuccess<{ heading: string | null; text: string; sectionHash: string; checkout: unknown }>('read_source_section', await server.call('read_source_section', { id: ids.overview })).data;
    const whole = expectSuccess<{ heading: string | null; text: string }>('read_source_section', await server.call('read_source_section', { id: ids.whole })).data;

    expect(overview.heading).toBe('개요');
    expect(overview.text).toBe('## 개요\n개요 본문\n\n');
    expect(overview.sectionHash).toBe(sha256(Buffer.from('## 개요\n개요 본문\n\n', 'utf8')));
    expect(overview.checkout).toEqual({ state: 'unknown', reason: 'no-git' });
    expect(whole.heading).toBeNull();
    expect(whole.text).toBe(GUIDE.slice(1));
  });

  it('reads a file of exactly 1 MiB and a section of exactly 256 KiB', async () => {
    const server = await connectRealStore();
    expectSuccess('read_source_section', await server.call('read_source_section', { id: ids.fileLimit }));
    const limit = expectSuccess<{ paging: { total: number } }>('read_source_section', await server.call('read_source_section', { id: ids.sectionLimit })).data;
    expect(limit.paging.total).toBe(sectionOfBytes('큰 구간', SECTION_LIMIT).length);
  });
});

describe('the eight rejection cases through read_source_section (main decision msg_4c7e21aeead6 item 2)', () => {
  it('1 parent path: ../outside.md and docs/../../outside.md are SOURCE_PATH_REJECTED·parent', async () => {
    for (const id of [ids.parent, ids.parentInside]) {
      expect(await rejection(id), id).toEqual({ code: 'SOURCE_PATH_REJECTED', details: { reason: 'parent' } });
    }
  });

  it('2 absolute path: /outside.md and //server/share/a.md are SOURCE_PATH_REJECTED·absolute', async () => {
    for (const id of [ids.absolute, ids.unc]) {
      expect(await rejection(id), id).toEqual({ code: 'SOURCE_PATH_REJECTED', details: { reason: 'absolute' } });
    }
  });

  it('3 drive letter: C:/outside.md and c:outside.md are SOURCE_PATH_REJECTED·drive', async () => {
    for (const id of [ids.drive, ids.driveRelative]) {
      expect(await rejection(id), id).toEqual({ code: 'SOURCE_PATH_REJECTED', details: { reason: 'drive' } });
    }
  });

  it('4 case-only path: case/Guide.md registered as case/guide.md is ·case on a case-insensitive file system, else SOURCE_MISSING', async () => {
    const expected = caseInsensitive ? { code: 'SOURCE_PATH_REJECTED', details: { reason: 'case' } } : { code: 'SOURCE_MISSING', details: null };
    expect(await rejection(ids.caseOnly), `observed caseInsensitive=${caseInsensitive}`).toEqual(expected);
  });

  it('5 link: a .md under a junction (to outside or back inside the root) is SOURCE_PATH_REJECTED·link', async () => {
    for (const id of [ids.junction, ids.junctionInside]) {
      expect(await rejection(id), id).toEqual({ code: 'SOURCE_PATH_REJECTED', details: { reason: 'link' } });
    }
  });

  it('5 link: a file symbolic link is SOURCE_PATH_REJECTED·link when the OS allows creating one', async () => {
    if (fileLinkError) {
      // Design 「거절 사례 8종」: a permission refusal is recorded and the junction case decides.
      expect(['EPERM', 'EACCES']).toContain(fileLinkError.code);
      return;
    }
    expect(await rejection(ids.fileLink)).toEqual({ code: 'SOURCE_PATH_REJECTED', details: { reason: 'link' } });
  });

  it('6 extension: a.cs, a.MD and a.md.txt are SOURCE_NOT_READABLE·extension', async () => {
    for (const id of [ids.extensionCs, ids.extensionUpper, ids.extensionDouble]) {
      expect(await rejection(id), id).toEqual({ code: 'SOURCE_NOT_READABLE', details: { reason: 'extension' } });
    }
  });

  it('7 size: a file over 1 MiB is SOURCE_TOO_LARGE·file and a section over 256 KiB is SOURCE_TOO_LARGE·section', async () => {
    expect(await rejection(ids.fileOver)).toEqual({ code: 'SOURCE_TOO_LARGE', details: { reason: 'file' } });
    expect(await rejection(ids.sectionOver)).toEqual({ code: 'SOURCE_TOO_LARGE', details: { reason: 'section' } });
  });

  it('8 unregistered source ID: no-such-source is NOT_FOUND without details', async () => {
    expect(await rejection('no-such-source')).toEqual({ code: 'NOT_FOUND', details: null });
  });
});

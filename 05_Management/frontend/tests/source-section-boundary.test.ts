// @vitest-environment node
// Requirement: goal 「만들 것」 2 and 완료조건 2 (only registered Markdown inside the repository is
// read; missing files, sections and oversize show as broken links) with main decision
// msg_4c7e21aeead6 item 2 (the eight rejection cases), and index-v2-design.md
// 「원문 구간 읽기 경계」 (steps 1-9, result shapes) and 「거절 사례 8종」 (app column).
// Every case goes through createSourceSectionReader().read(id) with an owned TEMP repository root
// and a fixture v2 index read by the real catalog store. Expected code·reason values are copied
// from the design table; texts and byte counts are written here, never computed by the product.
import { existsSync, readdirSync, symlinkSync } from 'node:fs';
import { afterAll, beforeAll, describe, expect, it, vi } from 'vitest';
import { createCatalogStore } from '../electron/catalog-store';
import { createSourceSectionReader, createSourceSectionStore } from '../electron/source-section-store';
import {
  catalogText, gitSource, handoffSource, localSource, systemFixture, type SourceFixture,
} from './record-sources/catalog-v2-fixture';
import { createOwnedTempRepository, type OwnedTempRepository } from './record-sources/owned-temp-repository';

const MIB = 1024 * 1024;
const SECTION_LIMIT = 256 * 1024;
const GUIDE = '# 안내\n\n## 개요\n개요 본문\n\n## 다음\n끝\n';

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
const sources: SourceFixture[] = [];
const source = (id: string, locator: string, section: string | null = null) => {
  sources.push(gitSource(id, locator, section));
  return id;
};

beforeAll(() => {
  repository = createOwnedTempRepository();
  repository.write('docs/guide.md', GUIDE);
  repository.write('case/Guide.md', GUIDE);
  caseInsensitive = existsSync(repository.path('case/guide.md'));
  for (const name of ['a.cs', 'a.MD', 'a.md.txt', 'a.html']) repository.write(`docs/${name}`, GUIDE);
  repository.mkdir('dir.md');
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
  repository.write('enc/bad.md', new Uint8Array([0x23, 0x20, 0x41, 0x0a, 0xff, 0xfe, 0x0a]));
  repository.write('records/catalog.json', catalogText({
    schemaVersion: 2,
    sources: [
      ...sources,
      localSource('local-run-log', '../outside/secret.md'),
      handoffSource('main-decision', 'msg_0123456789ab'),
    ],
    systems: [systemFixture({ id: 'records-view', sourceIds: [] })],
    records: [],
  }));
});
afterAll(() => repository?.remove());

// Registered at module load so the fixture index above contains them.
const ids = {
  guide: source('guide-overview', 'docs/guide.md', '개요'),
  parent: source('parent', '../outside.md'),
  parentInside: source('parent-inside', 'docs/../../outside.md'),
  parentBack: source('parent-back', 'docs/../guide.md'),
  absolute: source('absolute', '/outside.md'),
  unc: source('unc', '//server/share/a.md'),
  drive: source('drive', 'C:/outside.md'),
  driveRelative: source('drive-relative', 'c:outside.md'),
  driveBackslash: source('drive-backslash', 'C:\\outside.md'),
  driveBeforeParent: source('drive-before-parent', 'C:/../outside.md'),
  absoluteBeforeParent: source('absolute-before-parent', '/../outside.md'),
  parentBeforeExtension: source('parent-before-extension', '../outside.MD'),
  caseFile: source('case-file', 'case/guide.md'),
  caseDirectory: source('case-directory', 'Case/Guide.md'),
  junction: source('junction', 'linked/secret.md'),
  junctionInside: source('junction-inside', 'linked-inside/guide.md'),
  junctionMissing: source('junction-missing', 'linked/none.md'),
  fileLink: source('file-link', 'docs/file-link.md'),
  extensionCs: source('extension-cs', 'docs/a.cs'),
  extensionUpper: source('extension-upper', 'docs/a.MD'),
  extensionDouble: source('extension-double', 'docs/a.md.txt'),
  extensionHtml: source('extension-html', 'docs/a.html'),
  extensionMissingFile: source('extension-missing-file', 'docs/none.cs'),
  fileLimit: source('file-limit', 'big/file-limit.md', '끝'),
  fileOver: source('file-over', 'big/file-over.md', '끝'),
  sectionLimit: source('section-limit', 'big/section-limit.md', '큰 구간'),
  sectionOver: source('section-over', 'big/section-over.md', '큰 구간'),
  missing: source('missing', 'docs/none.md'),
  missingOther: source('missing-other', 'other/none.md'),
  notFile: source('not-file', 'dir.md'),
  badEncoding: source('bad-encoding', 'enc/bad.md'),
  backslash: source('backslash', 'docs\\guide.md'),
  emptySegment: source('empty-segment', 'docs//guide.md'),
  dotSegment: source('dot-segment', './docs/guide.md'),
  trailingSlash: source('trailing-slash', 'docs/guide.md/'),
  star: source('star', 'docs/gu*ide.md'),
  question: source('question', 'docs/gu?ide.md'),
  colon: source('colon', 'docs/a:b.md'),
};

function createReader() {
  const catalogStore = createCatalogStore(repository.path('records/catalog.json'));
  const readCatalog = vi.fn(() => catalogStore.read());
  const store = createSourceSectionStore({ repositoryRoot: repository.root });
  const storeRead = vi.spyOn(store, 'read');
  return { reader: createSourceSectionReader({ readCatalog, store }), readCatalog, storeRead };
}

async function outcome(id: unknown) {
  const result = await createReader().reader.read(id);
  return result.ok ? { ok: true } : { ok: false, code: result.code, reason: result.reason };
}

describe('the eight rejection cases through createSourceSectionReader().read (main decision msg_4c7e21aeead6 item 2)', () => {
  it('reads a registered Markdown section inside the root (control case)', async () => {
    expect(await outcome(ids.guide)).toEqual({ ok: true });
  });

  it('1 parent path: ../outside.md and docs/../../outside.md are path-rejected·parent', async () => {
    for (const id of [ids.parent, ids.parentInside, ids.parentBack]) {
      expect(await outcome(id), id).toEqual({ ok: false, code: 'path-rejected', reason: 'parent' });
    }
  });

  it('2 absolute path: /outside.md and //server/share/a.md are path-rejected·absolute', async () => {
    for (const id of [ids.absolute, ids.unc]) {
      expect(await outcome(id), id).toEqual({ ok: false, code: 'path-rejected', reason: 'absolute' });
    }
  });

  it('3 drive letter: C:/outside.md and c:outside.md are path-rejected·drive', async () => {
    for (const id of [ids.drive, ids.driveRelative, ids.driveBackslash]) {
      expect(await outcome(id), id).toEqual({ ok: false, code: 'path-rejected', reason: 'drive' });
    }
  });

  it('4 case-only difference: case/guide.md for case/Guide.md is case on a case-insensitive file system, missing otherwise', async () => {
    // Observed on the running file system, not assumed (contract criterion 2).
    const expected = caseInsensitive
      ? { ok: false, code: 'path-rejected', reason: 'case' }
      : { ok: false, code: 'missing', reason: null };
    for (const id of [ids.caseFile, ids.caseDirectory]) expect(await outcome(id), `${id} caseInsensitive=${caseInsensitive}`).toEqual(expected);
  });

  it('5 links: a .md under a junction is path-rejected·link, even when the junction points inside the root', async () => {
    for (const id of [ids.junction, ids.junctionInside, ids.junctionMissing]) {
      expect(await outcome(id), id).toEqual({ ok: false, code: 'path-rejected', reason: 'link' });
    }
  });

  it('5 links: a file symbolic link is path-rejected·link when this machine allows creating one', async () => {
    if (fileLinkError) {
      // Creation was refused; the junction case above decides the link rule. Keep the raw error.
      console.warn(`[source-section] file symlink not created: ${fileLinkError.code} ${fileLinkError.message}`);
      expect(fileLinkError.code).toMatch(/^(EPERM|EACCES)$/);
      return;
    }
    expect(await outcome(ids.fileLink)).toEqual({ ok: false, code: 'path-rejected', reason: 'link' });
  });

  it('6 extension outside lowercase .md: a.cs, a.MD, a.md.txt and a.html are not-readable·extension', async () => {
    for (const id of [ids.extensionCs, ids.extensionUpper, ids.extensionDouble, ids.extensionHtml]) {
      expect(await outcome(id), id).toEqual({ ok: false, code: 'not-readable', reason: 'extension' });
    }
  });

  it('7 size limit: a file over 1 MiB is too-large·file and exactly 1 MiB is read', async () => {
    expect(await outcome(ids.fileOver)).toEqual({ ok: false, code: 'too-large', reason: 'file' });
    expect(await outcome(ids.fileLimit)).toEqual({ ok: true });
  });

  it('7 size limit: a section over 256 KiB is too-large·section and exactly 256 KiB is read', async () => {
    expect(await outcome(ids.sectionOver)).toEqual({ ok: false, code: 'too-large', reason: 'section' });
    expect(await outcome(ids.sectionLimit)).toEqual({ ok: true });
  });

  it('8 unregistered source ID: no-such-source is unknown-source', async () => {
    expect(await outcome('no-such-source')).toEqual({ ok: false, code: 'unknown-source', reason: null });
  });
});

describe('judgement order (design steps 1-9: an earlier rejection stops later steps)', () => {
  it('1 rejects a non-ID request as invalid-request before reading the index', async () => {
    const inputs: unknown[] = [42, null, undefined, {}, ['guide-overview'], '', 'Bad_ID', ' guide-overview', 'a'.repeat(129), 'a--b'];
    for (const input of inputs) {
      const { reader, readCatalog } = createReader();
      const result = await reader.read(input);
      expect(result.ok ? null : { code: result.code, reason: result.reason }, JSON.stringify(input) ?? 'undefined')
        .toEqual({ code: 'invalid-request', reason: null });
      expect(readCatalog, JSON.stringify(input) ?? 'undefined').not.toHaveBeenCalled();
    }
  });

  it('2 reports index-unavailable when the index cannot be read', async () => {
    for (const path of ['records/none.json', 'records/broken.json']) {
      if (path.endsWith('broken.json')) repository.write(path, '{');
      const catalogStore = createCatalogStore(repository.path(path));
      const reader = createSourceSectionReader({ readCatalog: () => catalogStore.read(), store: createSourceSectionStore({ repositoryRoot: repository.root }) });
      const result = await reader.read(ids.guide);
      expect(result.ok ? null : { code: result.code, reason: result.reason }, path).toEqual({ code: 'index-unavailable', reason: null });
    }
  });

  it('3 reports unknown-source without asking the store', async () => {
    const { reader, storeRead } = createReader();
    const result = await reader.read('no-such-source');
    expect(result.ok ? null : result.code).toBe('unknown-source');
    expect(storeRead).not.toHaveBeenCalled();
  });

  it('reads the index again on every request, so a changed index applies to the next read', async () => {
    const { reader, readCatalog } = createReader();
    await reader.read(ids.guide);
    await reader.read(ids.guide);
    expect(readCatalog).toHaveBeenCalledTimes(2);
  });

  it('4 rejects local and handoff sources as not-readable before checking their paths', async () => {
    expect(await outcome('local-run-log')).toEqual({ ok: false, code: 'not-readable', reason: 'local-only' });
    expect(await outcome('main-decision')).toEqual({ ok: false, code: 'not-readable', reason: 'handoff' });
  });

  it('5 orders path shapes as drive, absolute, parent, then extension (step 6)', async () => {
    expect(await outcome(ids.driveBeforeParent)).toEqual({ ok: false, code: 'path-rejected', reason: 'drive' });
    expect(await outcome(ids.absoluteBeforeParent)).toEqual({ ok: false, code: 'path-rejected', reason: 'absolute' });
    expect(await outcome(ids.parentBeforeExtension)).toEqual({ ok: false, code: 'path-rejected', reason: 'parent' });
  });

  it('5 rejects backslash, empty, dot, wildcard and colon segments as path-rejected·invalid', async () => {
    for (const id of [ids.backslash, ids.emptySegment, ids.dotSegment, ids.trailingSlash, ids.star, ids.question, ids.colon]) {
      expect(await outcome(id), id).toEqual({ ok: false, code: 'path-rejected', reason: 'invalid' });
    }
  });

  it('5 rejects a control character in the locator as path-rejected·invalid (store entry, since the index refuses it)', async () => {
    const store = createSourceSectionStore({ repositoryRoot: repository.root });
    const result = await store.read(gitSource('control', 'docs/gu\u0001ide.md', null));
    expect(result.ok ? null : { code: result.code, reason: result.reason }).toEqual({ code: 'path-rejected', reason: 'invalid' });
  });

  it('6 judges the extension before looking for the file', async () => {
    expect(await outcome(ids.extensionMissingFile)).toEqual({ ok: false, code: 'not-readable', reason: 'extension' });
  });

  it('7 reports missing for an absent file and not-readable·not-file for a directory named .md', async () => {
    expect(await outcome(ids.missing)).toEqual({ ok: false, code: 'missing', reason: null });
    expect(await outcome(ids.missingOther)).toEqual({ ok: false, code: 'missing', reason: null });
    expect(await outcome(ids.notFile)).toEqual({ ok: false, code: 'not-readable', reason: 'not-file' });
  });

  it('8 reports invalid-encoding for bytes that are not strict UTF-8', async () => {
    expect(await outcome(ids.badEncoding)).toEqual({ ok: false, code: 'invalid-encoding', reason: null });
  });
});

describe('result shapes', () => {
  it('returns the source ID, the locator as path, the heading, the section text and its UTF-8 byte count', async () => {
    const text = '## 개요\n개요 본문\n\n';
    const result = await createReader().reader.read(ids.guide);
    expect(result).toEqual({ ok: true, sourceId: ids.guide, path: 'docs/guide.md', heading: '개요', text, bytes: Buffer.byteLength(text, 'utf8') });
  });

  it('returns failures as ok, code, reason and a fixed Korean message without the input, locator, root or raw error', async () => {
    const failures = [];
    for (const id of [...Object.values(ids), 'no-such-source', 'local-run-log', 'main-decision', 42]) {
      const result = await createReader().reader.read(id);
      if (!result.ok) failures.push({ id, result });
    }
    expect(failures.length).toBeGreaterThan(20);
    const locators = new Map(sources.map(item => [item.id, item.locator]));
    for (const { id, result } of failures) {
      const label = String(id);
      expect(Object.keys(result).sort(), label).toEqual(['code', 'message', 'ok', 'reason']);
      expect(result.message, label).toMatch(/[가-힣]/);
      const leaks = [String(id), locators.get(String(id)) ?? '', repository.root, repository.base, 'ENOENT', 'EPERM', 'EISDIR', 'errno']
        .filter(value => value.length > 2 && result.message.includes(value));
      expect(leaks, label).toEqual([]);
    }
  });

  it('uses the same fixed message for the same failure code', async () => {
    const first = await createReader().reader.read(ids.missing);
    const second = await createReader().reader.read(ids.missingOther);
    expect(first.ok || second.ok).toBe(false);
    expect(!first.ok && !second.ok && first.message === second.message).toBe(true);
  });

  it('never writes into the repository root while reading', async () => {
    const before = readdirSync(repository.root).sort();
    for (const id of Object.values(ids)) await createReader().reader.read(id);
    expect(readdirSync(repository.root).sort()).toEqual(before);
  });
});

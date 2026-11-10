// @vitest-environment node
// Verifier probe for 05_Management/goals/2026-11-10-code-audit-management-fixes/goal.md 「만들 것」 3: the whole-file
// read and the shared source read go through one reading procedure and differ only in the 256 KiB section limit.
// Expected values come from the inputs built here and from the design documents, never from the product:
// - codes and reasons: index-v2-design.md 「원문 구간 읽기 경계」 steps 4~9;
// - text: the item 3 implementation contract supplement v1.1 decision 2. Under the section limit the whole-file text
//   equals the section-null text, and a leading BOM pair is removed on both, as the shared read does today.
// Every case uses an owned TEMP repository and the real store.
import { statSync, symlinkSync } from 'node:fs';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import type { RecordSource } from '../electron/catalog-contract';
import { createSourceSectionStore, readWholeSourceFile } from '../electron/source-section-store';
import { gitSource, handoffSource, localSource } from './record-sources/catalog-v2-fixture';
import { createOwnedTempRepository, type OwnedTempRepository } from './record-sources/owned-temp-repository';

const KIB = 1024;
const MIB = 1024 * KIB;
const SECTION_LIMIT = 256 * KIB;
const BOM_BYTES = [0xef, 0xbb, 0xbf];
const BOM_CHAR = String.fromCharCode(0xfeff);
const LOCATOR = 'docs/whole.md';
const WHOLE_SOURCE = gitSource('whole-source', LOCATOR, null);

let repository: OwnedTempRepository;
beforeEach(() => {
  repository = createOwnedTempRepository();
});
afterEach(() => repository.remove());

// The two entry points on the same source, whole-file read first.
async function bothReads(source: RecordSource) {
  const whole = await readWholeSourceFile({ repositoryRoot: repository.root, source });
  const shared = await createSourceSectionStore({ repositoryRoot: repository.root }).read(source);
  return { whole, shared };
}

function withBoms(count: number, body: string): Buffer {
  const prefix = Buffer.from(Array.from({ length: count }, () => BOM_BYTES).flat());
  return Buffer.concat([prefix, Buffer.from(body, 'utf8')]);
}

// Writes the file and returns its size on disk, so the size each case relies on is measured, not assumed.
function writeSource(locator: string, content: Buffer): number {
  return statSync(repository.write(locator, content)).size;
}

const outcome = (result: { ok: boolean; code?: string; reason?: string | null }) => (result.ok
  ? { ok: true }
  : { ok: false, code: result.code, reason: result.reason });

describe('goal 3 probe: the leading BOM step is the same on both entry points (supplement v1.1 decision 2)', () => {
  const cases: [string, number, string, string][] = [
    ['no BOM', 0, 'A\n', 'A\n'],
    ['one BOM', 1, 'A\n', 'A\n'],
    ['two BOMs', 2, 'A\n', 'A\n'],
    ['three BOMs keep the third', 3, 'A\n', `${BOM_CHAR}A\n`],
    ['a BOM alone', 1, '', ''],
    ['a BOM inside the text stays', 0, `A${BOM_CHAR}B\n`, `A${BOM_CHAR}B\n`],
  ];

  it.each(cases)('goal 3 probe: %s', async (_name, boms, body, expectedText) => {
    writeSource(LOCATOR, withBoms(boms, body));
    const { whole, shared } = await bothReads(WHOLE_SOURCE);
    expect(whole).toEqual({ ok: true, text: expectedText });
    expect(shared).toEqual({
      ok: true,
      sourceId: 'whole-source',
      path: LOCATOR,
      heading: null,
      text: expectedText,
      bytes: Buffer.byteLength(expectedText, 'utf8'),
    });
  });
});

describe('goal 3 probe: section null keeps the 256 KiB section limit, the whole-file read takes only the 1 MiB file limit', () => {
  // [name, BOM count, text after the BOMs, text bytes, section-null outcome]
  const hangul = '가';
  const cases: [string, number, string, number, { ok: boolean; code?: string; reason?: string }][] = [
    ['exactly 256 KiB', 0, 'x'.repeat(SECTION_LIMIT), SECTION_LIMIT, { ok: true }],
    ['one byte over 256 KiB', 0, 'x'.repeat(SECTION_LIMIT + 1), SECTION_LIMIT + 1, { ok: false, code: 'too-large', reason: 'section' }],
    ['two BOMs then exactly 256 KiB', 2, 'x'.repeat(SECTION_LIMIT), SECTION_LIMIT, { ok: true }],
    ['two BOMs then one byte over 256 KiB', 2, 'x'.repeat(SECTION_LIMIT + 1), SECTION_LIMIT + 1, { ok: false, code: 'too-large', reason: 'section' }],
    // 87381 three-byte characters are 262143 bytes, so the limit counts UTF-8 bytes, not characters.
    ['exactly 256 KiB of mostly three-byte characters', 0, `${hangul.repeat(87381)}x`, SECTION_LIMIT, { ok: true }],
    ['two bytes over 256 KiB of three-byte characters', 0, hangul.repeat(87382), SECTION_LIMIT + 2, { ok: false, code: 'too-large', reason: 'section' }],
  ];

  it.each(cases)('goal 3 probe: %s', async (_name, boms, text, textBytes, sectionNull) => {
    const fileBytes = writeSource(LOCATOR, withBoms(boms, text));
    const { whole, shared } = await bothReads(WHOLE_SOURCE);
    expect(Buffer.byteLength(text, 'utf8')).toBe(textBytes);
    expect(fileBytes).toBe(textBytes + 3 * boms);
    expect(outcome(shared)).toEqual(sectionNull);
    if (shared.ok) expect(shared.bytes).toBe(textBytes);
    expect({ ok: whole.ok, sameText: whole.ok && whole.text === text }).toEqual({ ok: true, sameText: true });
  });

  it('goal 3 probe: a file of exactly 1 MiB reads whole and stays too-large·section on the shared read', async () => {
    const text = 'x'.repeat(MIB);
    const fileBytes = writeSource(LOCATOR, withBoms(0, text));
    const { whole, shared } = await bothReads(WHOLE_SOURCE);
    expect(fileBytes).toBe(MIB);
    expect({ ok: whole.ok, sameText: whole.ok && whole.text === text }).toEqual({ ok: true, sameText: true });
    expect(outcome(shared)).toEqual({ ok: false, code: 'too-large', reason: 'section' });
  });

  it('goal 3 probe: the file limit counts the BOM, so a BOM and 1 MiB - 3 bytes of text is exactly 1 MiB and reads', async () => {
    const text = 'x'.repeat(MIB - 3);
    const fileBytes = writeSource(LOCATOR, withBoms(1, text));
    const { whole } = await bothReads(WHOLE_SOURCE);
    expect(fileBytes).toBe(MIB);
    expect({ ok: whole.ok, sameText: whole.ok && whole.text === text }).toEqual({ ok: true, sameText: true });
  });

  it.each([
    ['1 MiB + 1 bytes of text', 0, MIB + 1],
    ['a BOM and 1 MiB - 2 bytes of text', 1, MIB - 2],
  ])('goal 3 probe: %s is one byte over the file limit and gives the same too-large·file on both entry points', async (_name, boms, textBytes) => {
    const fileBytes = writeSource(LOCATOR, withBoms(boms, 'x'.repeat(textBytes)));
    const { whole, shared } = await bothReads(WHOLE_SOURCE);
    expect(fileBytes).toBe(MIB + 1);
    expect(outcome(whole)).toEqual({ ok: false, code: 'too-large', reason: 'file' });
    expect(whole).toEqual(shared);
  });
});

describe('goal 3 probe: the same failure on both entry points for steps 4~8 (index-v2-design.md)', () => {
  // [name, source, setup, code, reason]
  const cases: [string, RecordSource, () => void, string, string | null][] = [
    ['step 4 local source', localSource('local-source', LOCATOR), () => writeSource(LOCATOR, withBoms(0, 'A\n')), 'not-readable', 'local-only'],
    ['step 4 handoff source', handoffSource('handoff-source', LOCATOR), () => writeSource(LOCATOR, withBoms(0, 'A\n')), 'not-readable', 'handoff'],
    ['step 5 parent segment', gitSource('parent-source', '../outside.md', null), () => undefined, 'path-rejected', 'parent'],
    ['step 5 drive path', gitSource('drive-source', 'C:/docs/whole.md', null), () => undefined, 'path-rejected', 'drive'],
    ['step 6 upper-case extension', gitSource('upper-source', 'docs/whole.MD', null), () => writeSource('docs/whole.MD', withBoms(0, 'A\n')), 'not-readable', 'extension'],
    ['step 7 missing file', WHOLE_SOURCE, () => undefined, 'missing', null],
    ['step 7 folder named .md', WHOLE_SOURCE, () => repository.mkdir(LOCATOR), 'not-readable', 'not-file'],
    ['step 7 junction on the path', WHOLE_SOURCE, () => {
      repository.write('../outside/whole.md', withBoms(0, 'A\n'));
      symlinkSync(repository.outside, repository.path('docs'), 'junction');
    }, 'path-rejected', 'link'],
    ['step 8 bytes that are not strict UTF-8', WHOLE_SOURCE, () => writeSource(LOCATOR, Buffer.from([0x41, 0xff, 0xfe, 0x0a])), 'invalid-encoding', null],
  ];

  it.each(cases)('goal 3 probe: %s', async (_name, source, setup, code, reason) => {
    setup();
    const { whole, shared } = await bothReads(source);
    expect(outcome(whole)).toEqual({ ok: false, code, reason });
    expect(whole).toEqual(shared);
  });

  it.runIf(process.platform === 'win32')('goal 3 probe: step 7 case-only path difference on Windows', async () => {
    writeSource(LOCATOR, withBoms(0, 'A\n'));
    const { whole, shared } = await bothReads(gitSource('case-source', 'docs/WHOLE.md', null));
    expect(outcome(whole)).toEqual({ ok: false, code: 'path-rejected', reason: 'case' });
    expect(whole).toEqual(shared);
  });
});

describe('goal 3 probe: the shared read does not branch on the BACKLOG source (implementation contract condition 1)', () => {
  // A literal copy of the fixed source in electron/backlog-store.ts, so a branch on its id, locator or fields shows.
  const backlogSourceCopy: RecordSource = {
    id: 'development-backlog',
    title: '이후 작업',
    kind: 'git',
    locator: '00_Document/operations/BACKLOG.md',
    section: null,
    availability: 'versioned',
  };

  it('goal 3 probe: the shared read of the BACKLOG source over 256 KiB stays too-large·section; the whole-file read reads it', async () => {
    const text = 'x'.repeat(300 * KIB);
    const fileBytes = writeSource(backlogSourceCopy.locator, withBoms(0, text));
    const { whole, shared } = await bothReads(backlogSourceCopy);
    expect(fileBytes).toBe(300 * KIB);
    expect(outcome(shared)).toEqual({ ok: false, code: 'too-large', reason: 'section' });
    expect({ ok: whole.ok, sameText: whole.ok && whole.text === text }).toEqual({ ok: true, sameText: true });
  });
});

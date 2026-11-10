// @vitest-environment node
// Requirement: 05_Management/goals/2026-11-10-code-audit-management-fixes/goal.md 「만들 것」 3 and 완료조건 4: reading
// BACKLOG.md as a whole takes only the 1 MiB file limit, while source section reads keep their 256 KiB section limit.
// The sizes are fixed here on purpose. BACKLOG.md belongs to Rules and keeps changing, so no case depends on its size.
// Every case uses an owned TEMP repository and the real stores. Failure sentences are copied from
// backlog-menu-design.md, and byte lengths come from inputs built here, never from the product.
import { renameSync, symlinkSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { createBacklogStore } from '../electron/backlog-store';
import { createSourceSectionStore } from '../electron/source-section-store';
import { BACKLOG_PATH, CANDIDATE_TABLE_HEADER, candidateRow } from './record-sources/backlog-fixture';
import { gitSource } from './record-sources/catalog-v2-fixture';
import { createOwnedTempRepository, type OwnedTempRepository } from './record-sources/owned-temp-repository';

const KIB = 1024;
const MIB = 1024 * KIB;
const SECTION_LIMIT = 256 * KIB;
// Over the section limit and well over the size BACKLOG.md has grown to, under the file limit.
const FIXED_BYTES = 600 * KIB;
const TOO_LARGE = { ok: false, code: 'too-large', message: 'BACKLOG.md가 읽기 크기 상한을 넘습니다.' };
const INVALID_ENCODING = { ok: false, code: 'invalid-encoding', message: 'BACKLOG.md가 UTF-8 문서가 아닙니다.' };
const LOAD = { ok: false, code: 'load', message: 'BACKLOG.md를 읽지 못했습니다. 다시 읽으세요.' };
const CHANGED = { ok: false, code: 'changed', message: '읽는 동안 BACKLOG.md가 바뀌었습니다. 다시 읽으세요.' };

// Replaces the target right before the product opens it, the way tests/source-section-changed.test.ts does.
const replaceOnOpen = vi.hoisted(() => ({ target: '', replacement: '', replaced: 0 }));

vi.mock('node:fs/promises', async original => {
  const actual = await original<typeof import('node:fs/promises')>();
  const open = (async (path: Parameters<typeof actual.open>[0], ...rest: unknown[]) => {
    const requested = resolve(String(path)).toLowerCase();
    if (replaceOnOpen.target && requested === resolve(replaceOnOpen.target).toLowerCase()) {
      renameSync(replaceOnOpen.replacement, replaceOnOpen.target);
      replaceOnOpen.target = '';
      replaceOnOpen.replaced += 1;
    }
    return (actual.open as (...args: unknown[]) => ReturnType<typeof actual.open>)(path, ...rest);
  }) as typeof actual.open;
  return { ...actual, open, default: { ...actual, open } };
});

// A document of exactly `bytes` UTF-8 bytes: one candidate table before an ASCII filler line and one after it, so a
// reader that stops at the first 256 KiB loses the second table.
function documentOfBytes(bytes: number, prefix: Uint8Array = new Uint8Array()): Buffer {
  const before = ['## 앞', ...CANDIDATE_TABLE_HEADER, candidateRow('before-filler'), ''].join('\n');
  const after = ['## 뒤', ...CANDIDATE_TABLE_HEADER, candidateRow('after-filler'), ''].join('\n');
  const fixed = prefix.byteLength + Buffer.byteLength(`${before}\n${after}`, 'utf8');
  return Buffer.concat([prefix, Buffer.from(`${before}${'x'.repeat(bytes - fixed)}\n${after}`, 'utf8')]);
}

let repository: OwnedTempRepository;
beforeEach(() => {
  repository = createOwnedTempRepository();
  replaceOnOpen.target = '';
  replaceOnOpen.replaced = 0;
});
afterEach(() => repository.remove());

const readBacklog = () => createBacklogStore({ repositoryRoot: repository.root }).read();
const readSource = (section: string | null) => createSourceSectionStore({ repositoryRoot: repository.root })
  .read(gitSource('backlog-source', BACKLOG_PATH, section));

// Candidate IDs and groups, empty when the read failed, so a failed read shows beside the missing rows.
async function backlogOutcome() {
  const result = await readBacklog();
  return {
    failure: result.ok ? null : result.code,
    candidates: result.ok ? result.rows.map(row => ({ id: row.id, group: row.group })) : [],
  };
}

const BOTH_CANDIDATES = [{ id: 'before-filler', group: '앞' }, { id: 'after-filler', group: '뒤' }];

describe('goal 3: BACKLOG.md as a whole takes only the 1 MiB file limit', () => {
  // Control: proves the filler and the two tables parse as intended, so the failures below come from the size alone.
  it('goal 3 control: reads both candidates of a document under the section limit', async () => {
    repository.write(BACKLOG_PATH, documentOfBytes(4 * KIB));
    expect(await backlogOutcome()).toEqual({ failure: null, candidates: BOTH_CANDIDATES });
  });

  it('goal 3: reads a fixed 600 KiB BACKLOG.md whole, including the candidate past the first 256 KiB', async () => {
    const document = documentOfBytes(FIXED_BYTES);
    repository.write(BACKLOG_PATH, document);
    const outcome = await backlogOutcome();
    expect(document.byteLength).toBe(FIXED_BYTES);
    expect(outcome).toEqual({ failure: null, candidates: BOTH_CANDIDATES });
  });

  it('goal 3: reads a BACKLOG.md of exactly 1 MiB', async () => {
    const document = documentOfBytes(MIB);
    repository.write(BACKLOG_PATH, document);
    const outcome = await backlogOutcome();
    expect(document.byteLength).toBe(MIB);
    expect(outcome).toEqual({ failure: null, candidates: BOTH_CANDIDATES });
  });

  it('goal 3: returns too-large with the fixed sentence for a BACKLOG.md one byte over 1 MiB', async () => {
    const document = documentOfBytes(MIB + 1);
    repository.write(BACKLOG_PATH, document);
    const result = await readBacklog();
    expect(document.byteLength).toBe(MIB + 1);
    expect(result).toEqual(TOO_LARGE);
  });
});

describe('goal 3 regression: the whole-file read keeps the source reading boundary at a size over the section limit', () => {
  it('goal 3: ignores a leading BOM in a document under the section limit', async () => {
    repository.write(BACKLOG_PATH, documentOfBytes(4 * KIB, new Uint8Array([0xef, 0xbb, 0xbf])));
    expect(await backlogOutcome()).toEqual({ failure: null, candidates: BOTH_CANDIDATES });
  });

  it('goal 3: ignores a leading BOM in a fixed 600 KiB document', async () => {
    const document = documentOfBytes(FIXED_BYTES, new Uint8Array([0xef, 0xbb, 0xbf]));
    repository.write(BACKLOG_PATH, document);
    const outcome = await backlogOutcome();
    expect(document.byteLength).toBe(FIXED_BYTES);
    expect(outcome).toEqual({ failure: null, candidates: BOTH_CANDIDATES });
  });

  it('goal 3: returns invalid-encoding with the fixed sentence for bytes that are not strict UTF-8 in a fixed 600 KiB document', async () => {
    const valid = documentOfBytes(FIXED_BYTES - 3);
    const document = Buffer.concat([valid, Buffer.from([0xff, 0xfe, 0x0a])]);
    repository.write(BACKLOG_PATH, document);
    const result = await readBacklog();
    expect(document.byteLength).toBe(FIXED_BYTES);
    expect(result).toEqual(INVALID_ENCODING);
  });

  it('goal 3: returns load with the fixed sentence when a folder on the path of a fixed 600 KiB document is a junction', async () => {
    repository.write('../outside/operations/BACKLOG.md', documentOfBytes(FIXED_BYTES));
    repository.mkdir('00_Document');
    symlinkSync(join(repository.outside, 'operations'), repository.path('00_Document/operations'), 'junction');
    expect(await readBacklog()).toEqual(LOAD);
  });

  it('goal 3: returns changed with the fixed sentence, not the replacement rows, when a fixed 600 KiB document is replaced at open', async () => {
    const target = repository.write(BACKLOG_PATH, documentOfBytes(FIXED_BYTES));
    replaceOnOpen.replacement = repository.write('00_Document/operations/BACKLOG.replacement.tmp', documentOfBytes(FIXED_BYTES + 1));
    replaceOnOpen.target = target;
    const result = await readBacklog();
    const replacedBeforeRead = replaceOnOpen.replaced;
    expect(replacedBeforeRead, 'the product opened BACKLOG.md through node:fs/promises open').toBe(1);
    expect(result).toEqual(CHANGED);
  });
});

describe('goal 3 preservation: source section reads keep the 256 KiB section limit on the same file', () => {
  const failureOf = (result: Awaited<ReturnType<typeof readSource>>) => (result.ok ? { ok: true } : { ok: false, code: result.code, reason: result.reason });

  it('goal 3: a whole-file source read (section null) of a fixed 600 KiB BACKLOG.md stays too-large·section', async () => {
    repository.write(BACKLOG_PATH, documentOfBytes(FIXED_BYTES));
    expect(failureOf(await readSource(null))).toEqual({ ok: false, code: 'too-large', reason: 'section' });
  });

  it('goal 3: a named section one byte over 256 KiB in a file under 1 MiB stays too-large·section', async () => {
    const head = '# 큰 구간\n';
    const section = `${head}${'x'.repeat(SECTION_LIMIT + 1 - Buffer.byteLength(head, 'utf8') - 1)}\n`;
    const document = Buffer.from(`${section}# 다음\n끝\n`, 'utf8');
    repository.write(BACKLOG_PATH, document);
    const result = await readSource('큰 구간');
    expect(Buffer.byteLength(section, 'utf8')).toBe(SECTION_LIMIT + 1);
    expect(failureOf(result)).toEqual({ ok: false, code: 'too-large', reason: 'section' });
  });

  it('goal 3: a named section under 256 KiB in a fixed 600 KiB file still reads', async () => {
    repository.write(BACKLOG_PATH, documentOfBytes(FIXED_BYTES));
    const result = await readSource('뒤');
    expect(failureOf(result)).toEqual({ ok: true });
  });
});

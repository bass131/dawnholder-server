// @vitest-environment node
// Requirement: backlog-menu-design.md 「goal 링크 판정 순서」 (steps 1-4, the reason in the
// REJECTED cause, the fixed sentences) and the read-result table of 「두 단계」: in the behaviour step
// BACKLOG.md and every goal link go through the app's source reading boundary, so links, junctions,
// letter case, the 1 MiB file and 256 KiB content limits and strict UTF-8 are judged as the app's
// source reading judges them (index-v2-design.md 「원문 구간 읽기 경계」). These fail until the
// behaviour step. Each case uses an owned TEMP repository, real junctions and the real
// createBacklogStore; sentences are copied from the design tables, never computed by the product.
import { existsSync, symlinkSync } from 'node:fs';
import { join } from 'node:path';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { createBacklogStore } from '../electron/backlog-store';
import { BACKLOG_PATH, CANDIDATE_TABLE_HEADER, backlogDocument, candidateRow } from './record-sources/backlog-fixture';
import { createOwnedTempRepository, type OwnedTempRepository } from './record-sources/owned-temp-repository';

const MIB = 1024 * 1024;
const CONTENT_LIMIT = 256 * 1024;
const INDEXED_GOAL = '01_Phases/goals/2026-10-01-indexed/goal.md';
const MANAGED_FOLDER = '05_Management/goals/2026-10-02-managed';

const REJECTED_FIX = 'BACKLOG.md 기준 상대경로로 저장소 안의 goal 파일을 대소문자까지 같게 가리키고 링크·junction을 거치지 마세요.';
const rejected = (line: number, reason: string) => ({
  code: 'BACKLOG_GOAL_LINK_REJECTED', line, cause: `상대 goal 링크가 저장소 경로 규칙에 맞지 않습니다 (${reason}).`, fix: REJECTED_FIX,
});
const missing = (line: number) => ({
  code: 'BACKLOG_GOAL_LINK_MISSING', line, cause: '상대 goal 링크의 대상 파일이 없습니다.', fix: 'BACKLOG.md 기준 상대경로와 대상 goal 파일의 존재를 확인하세요.',
});
const READ_FAILURES = {
  tooLarge: { ok: false, code: 'too-large', message: 'BACKLOG.md가 읽기 크기 상한을 넘습니다.' },
  invalidEncoding: { ok: false, code: 'invalid-encoding', message: 'BACKLOG.md가 UTF-8 문서가 아닙니다.' },
  load: { ok: false, code: 'load', message: 'BACKLOG.md를 읽지 못했습니다. 다시 읽으세요.' },
};

let repository: OwnedTempRepository;
beforeEach(() => {
  repository = createOwnedTempRepository();
  repository.write(INDEXED_GOAL, '# 목표\n');
  repository.write(`${MANAGED_FOLDER}/goal.md`, '# 목표\n');
});
afterEach(() => repository.remove());

const read = () => createBacklogStore({ repositoryRoot: repository.root }).read();

// Writes one candidate row per status cell and returns the issues with the line of each row.
async function issuesForStatuses(statuses: string[]) {
  const rows = statuses.map((status, index) => candidateRow(`link-case-${index + 1}`, { status }));
  const document = backlogDocument(['## 후보', ...CANDIDATE_TABLE_HEADER, ...rows]);
  repository.write(BACKLOG_PATH, document.text);
  const result = await read();
  return {
    result,
    issues: result.ok ? result.issues : [],
    unchecked: result.ok ? result.uncheckedLinks : [],
    lines: rows.map(row => document.lineOf(row)),
  };
}

describe('goal link judgement order (behaviour step)', () => {
  it('rejects a link whose percent-encoding cannot be decoded as REJECTED (invalid)', async () => {
    const { result, issues, lines } = await issuesForStatuses([
      'goal 승격 → [깨진 인코딩](../../01_Phases/goals/2026-10-01-indexed/goal%E0%A4%A.md)',
    ]);
    expect(result.ok).toBe(true);
    expect(issues).toEqual([rejected(lines[0]!, 'invalid')]);
  });

  // Added by the independent verifier: step 2's decoded backslash rule (design supplement) had no repository test.
  // Windows would read `\` as a separator, so without the rule the first link would name the existing managed
  // goal.md and the second would leave the root (parent); both must stop at step 2 as invalid. A cell's `\\` is
  // the table escape for one literal `\`, which reaches the link as is.
  it('rejects a decoded backslash as REJECTED (invalid) before resolving it, where it would name an existing goal or leave the root', async () => {
    const { result, issues, unchecked, lines } = await issuesForStatuses([
      `goal 승격 → [역슬래시](../../${MANAGED_FOLDER}%5Cgoal.md)`,
      'goal 승격 → [역슬래시로 밖](../../01_Phases/goals/..%5C..%5C..%5C..%5Coutside/goal.md)',
      `goal 승격 → [셀의 역슬래시](../../${MANAGED_FOLDER}\\\\goal.md)`,
    ]);
    expect(result.ok).toBe(true);
    expect(unchecked).toEqual([]);
    expect(issues).toEqual([rejected(lines[0]!, 'invalid'), rejected(lines[1]!, 'invalid'), rejected(lines[2]!, 'invalid')]);
  });

  // Added by the independent verifier: step 4's path-rejected `invalid` from inspectSourceFile (the source path
  // rules of index-v2-design.md 「원문 구간 읽기 경계」 5: `:` and control characters), after a successful decode.
  it('rejects a decoded link that the source path rules call invalid (a colon, a control character) as REJECTED (invalid)', async () => {
    const { issues, lines } = await issuesForStatuses([
      `goal 승격 → [콜론](../../${MANAGED_FOLDER}/goal%3A.md)`,
      `goal 승격 → [제어 문자](../../${MANAGED_FOLDER}/goal%01.md)`,
    ]);
    expect(issues).toEqual([rejected(lines[0]!, 'invalid'), rejected(lines[1]!, 'invalid')]);
  });

  it('rejects a link that leaves the repository root as REJECTED (parent), even when a file exists there', async () => {
    repository.write(`../${INDEXED_GOAL}`, '# 저장소 밖 목표\n');
    const { issues, lines } = await issuesForStatuses([`goal 승격 → [밖](../../../${INDEXED_GOAL})`]);
    expect(issues).toEqual([rejected(lines[0]!, 'parent')]);
  });

  it('rejects a link through a junction in the middle of the path as REJECTED (link), wherever the junction points', async () => {
    repository.write('../outside/goal.md', '# 저장소 밖 목표\n');
    symlinkSync(repository.outside, repository.path('01_Phases/goals/linked-outside'), 'junction');
    symlinkSync(repository.path('01_Phases/goals/2026-10-01-indexed'), repository.path('01_Phases/goals/linked-inside'), 'junction');
    const { issues, lines } = await issuesForStatuses([
      'goal 승격 → [밖을 가리키는 정션](../../01_Phases/goals/linked-outside/goal.md)',
      'goal 승격 → [안을 가리키는 정션](../../01_Phases/goals/linked-inside/goal.md)',
    ]);
    expect(issues).toEqual([rejected(lines[0]!, 'link'), rejected(lines[1]!, 'link')]);
  });

  it('judges a link that differs only in letter case as REJECTED (case) on a case-insensitive file system, MISSING otherwise', async () => {
    // Observed on the running file system, not assumed (design 「시험」).
    const caseInsensitive = existsSync(repository.path('01_Phases/goals/2026-10-01-INDEXED/goal.md'));
    const { issues, lines } = await issuesForStatuses([
      'goal 승격 → [폴더 대소문자](../../01_Phases/goals/2026-10-01-Indexed/goal.md)',
      'goal 승격 → [파일 대소문자](../../01_Phases/goals/2026-10-01-indexed/Goal.md)',
    ]);
    const expected = caseInsensitive
      ? [rejected(lines[0]!, 'case'), rejected(lines[1]!, 'case')]
      : [missing(lines[0]!), missing(lines[1]!)];
    expect(issues, `caseInsensitive=${caseInsensitive}`).toEqual(expected);
  });

  it('lists the issues of several links in one row in cell order: REJECTED, nothing for an existing file, MISSING for an absent file or a directory', async () => {
    repository.mkdir('05_Management/goals/2026-10-03-folder/goal.md');
    const row = candidateRow('several-links', {
      source: `[밖](../../../${INDEXED_GOAL})`,
      prerequisite: `[있는 goal](../../${INDEXED_GOAL})`,
      status: [
        'goal 승격 → [없는 goal](../../01_Phases/goals/2026-10-09-none/goal.md)',
        `[goal 폴더](../../${MANAGED_FOLDER})`,
        '[goal.md 이름의 폴더](../../05_Management/goals/2026-10-03-folder/goal.md)',
        `[깨진 인코딩](../../${MANAGED_FOLDER}/goal%E0%A4%A.md)`,
      ].join(' '),
    });
    const document = backlogDocument(['## 후보', ...CANDIDATE_TABLE_HEADER, row]);
    repository.write(BACKLOG_PATH, document.text);
    const result = await read();
    const line = document.lineOf(row);
    expect(result).toEqual({
      ok: true,
      rows: expect.any(Array),
      uncheckedLinks: [],
      issues: [rejected(line, 'parent'), missing(line), missing(line), missing(line), rejected(line, 'invalid')],
    });
  });
});

describe('BACKLOG.md read through the source reading boundary (behaviour step)', () => {
  function candidatesWithPadding(padding: string): Uint8Array {
    const text = backlogDocument(['## 후보', ...CANDIDATE_TABLE_HEADER, candidateRow('padded-candidate'), '']).text;
    return Buffer.from(`${text}${padding}\n`, 'utf8');
  }

  it('returns too-large with the fixed sentence for a BACKLOG.md over 1 MiB', async () => {
    repository.write(BACKLOG_PATH, candidatesWithPadding('y'.repeat(MIB)));
    expect(await read()).toEqual(READ_FAILURES.tooLarge);
  });

  it('returns too-large for content over 256 KiB in a file under 1 MiB, and reads content of exactly 256 KiB', async () => {
    const head = candidatesWithPadding('').byteLength;
    // head already ends with the padding line's newline; the padding fills the rest of the limit.
    const atLimit = candidatesWithPadding('x'.repeat(CONTENT_LIMIT - head));
    const overLimit = candidatesWithPadding('x'.repeat(CONTENT_LIMIT - head + 1));
    expect([atLimit.byteLength, overLimit.byteLength]).toEqual([CONTENT_LIMIT, CONTENT_LIMIT + 1]);

    repository.write(BACKLOG_PATH, atLimit);
    const atLimitResult = await read();
    repository.write(BACKLOG_PATH, overLimit);
    const overLimitResult = await read();
    expect(atLimitResult).toMatchObject({ ok: true, issues: [], uncheckedLinks: [] });
    expect(overLimitResult).toEqual(READ_FAILURES.tooLarge);
  });

  it('returns invalid-encoding with the fixed sentence for bytes that are not strict UTF-8', async () => {
    const valid = candidatesWithPadding('');
    repository.write(BACKLOG_PATH, Buffer.concat([valid, Buffer.from([0xff, 0xfe, 0x0a])]));
    expect(await read()).toEqual(READ_FAILURES.invalidEncoding);
  });

  it('returns load with the fixed sentence when a folder on the BACKLOG.md path is a junction to outside the root', async () => {
    repository.write('../outside/operations/BACKLOG.md', candidatesWithPadding(''));
    repository.mkdir('00_Document');
    symlinkSync(join(repository.outside, 'operations'), repository.path('00_Document/operations'), 'junction');
    expect(await read()).toEqual(READ_FAILURES.load);
  });

  it('returns load with the fixed sentence when a folder on the BACKLOG.md path is a junction to inside the root', async () => {
    repository.write('docs-copy/operations/BACKLOG.md', candidatesWithPadding(''));
    symlinkSync(repository.path('docs-copy'), repository.path('00_Document'), 'junction');
    expect(await read()).toEqual(READ_FAILURES.load);
  });
});

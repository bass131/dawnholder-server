// @vitest-environment node
// Requirement: backlog-menu-design.md 「goal 링크 판정 순서」 4 (`load` → uncheckedLinks), 「두 단계」 behaviour row
// 「예상 밖 링크 오류」(only that link is unchecked; later links and rows are still judged), the read-result table row
// `changed`, and 「색인 검사」 (uncheckedLinks: the issues are moved, then the backlog group is not run; a BACKLOG.md
// read that returns ok: false leaves the group not run with no BACKLOG_ diagnostic).
// The design leaves these without tests because they are not reproducible on a real file system. This file makes
// them deterministic without a product hook, the way tests/source-section-changed.test.ts does: node:fs/promises is
// wrapped so that lstat of one chosen path fails with EACCES, or BACKLOG.md is replaced right before the product
// opens it. Premise: the source reading boundary reaches the file system through node:fs/promises lstat and open;
// if it stops doing so, the interception counters fail first. Sentences are copied from the design tables.
import { renameSync } from 'node:fs';
import { resolve } from 'node:path';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { createBacklogStore } from '../electron/backlog-store';
import { checkRecordIndex } from '../electron/record-index-check';
import { BACKLOG_PATH, CANDIDATE_TABLE_HEADER, backlogDocument, candidateRow } from './record-sources/backlog-fixture';
import { catalogText, gitSource, recordFixture } from './record-sources/catalog-v2-fixture';
import { createOwnedTempRepository, type OwnedTempRepository } from './record-sources/owned-temp-repository';

const io = vi.hoisted(() => ({ deniedPath: '', deniedCount: 0, replaceTarget: '', replacement: '', replacedCount: 0 }));

vi.mock('node:fs/promises', async original => {
  const actual = await original<typeof import('node:fs/promises')>();
  const same = (path: unknown, target: string) => target !== '' && resolve(String(path)).toLowerCase() === resolve(target).toLowerCase();
  const lstat = (async (path: Parameters<typeof actual.lstat>[0], ...rest: unknown[]) => {
    if (same(path, io.deniedPath)) {
      io.deniedCount += 1;
      throw Object.assign(new Error('EACCES: permission denied (test double)'), { code: 'EACCES' });
    }
    return (actual.lstat as (...args: unknown[]) => ReturnType<typeof actual.lstat>)(path, ...rest);
  }) as typeof actual.lstat;
  const open = (async (path: Parameters<typeof actual.open>[0], ...rest: unknown[]) => {
    if (same(path, io.replaceTarget)) {
      renameSync(io.replacement, io.replaceTarget);
      io.replaceTarget = '';
      io.replacedCount += 1;
    }
    return (actual.open as (...args: unknown[]) => ReturnType<typeof actual.open>)(path, ...rest);
  }) as typeof actual.open;
  return { ...actual, lstat, open, default: { ...actual, lstat, open } };
});

const INDEXED_GOAL = '01_Phases/goals/2026-10-01-indexed/goal.md';
const NOTES_GOAL_FOLDER = '01_Phases/goals/2026-10-04-notes';
// The denied folder sits below a goal folder, so the catalog's source check and the goal listing never lstat it;
// only the BACKLOG link that walks through it does.
const DENIED_FOLDER = `${NOTES_GOAL_FOLDER}/denied`;
const DENIED_LINK = `../../${DENIED_FOLDER}/goal.md`;
const GOAL_TEXT = '# 목표\n\n## 결과와 열린 사항\n결과\n';
const MISSING = { code: 'BACKLOG_GOAL_LINK_MISSING', cause: '상대 goal 링크의 대상 파일이 없습니다.', fix: 'BACKLOG.md 기준 상대경로와 대상 goal 파일의 존재를 확인하세요.' };
const ID_FORMAT = { code: 'BACKLOG_ID_FORMAT', cause: '후보 ID가 소문자 단어를 하이픈으로 잇는 형식이 아닙니다.', fix: '128자 이하의 안정적인 소문자 ID를 쓰세요.' };
const CHANGED = { ok: false, code: 'changed', message: '읽는 동안 BACKLOG.md가 바뀌었습니다. 다시 읽으세요.' };

let repository: OwnedTempRepository;
beforeEach(() => {
  repository = createOwnedTempRepository();
  repository.write(INDEXED_GOAL, GOAL_TEXT);
  repository.write(`${NOTES_GOAL_FOLDER}/goal.md`, GOAL_TEXT);
  repository.write(`${DENIED_FOLDER}/goal.md`, GOAL_TEXT);
  repository.mkdir('05_Management/goals');
  repository.write('05_Management/records/catalog.json', catalogText({
    schemaVersion: 2,
    sources: [gitSource('indexed-goal', INDEXED_GOAL, '결과와 열린 사항'), gitSource('notes-goal', `${NOTES_GOAL_FOLDER}/goal.md`, '결과와 열린 사항')],
    systems: [],
    records: [recordFixture({ id: 'indexed', sourceIds: ['indexed-goal', 'notes-goal'] })],
  }));
  Object.assign(io, { deniedPath: '', deniedCount: 0, replaceTarget: '', replacement: '', replacedCount: 0 });
});
afterEach(() => repository.remove());

// One row whose first link cannot be checked, followed in the same row by a missing goal, then a later row with
// an ID problem. A judgement that stopped at the unchecked link would lose the last two issues.
const firstRow = candidateRow('unchecked-first', {
  source: `[확인 못 함](${DENIED_LINK})`,
  status: '대기 → [없는 goal](../../01_Phases/goals/2026-10-09-none/goal.md)',
});
const laterRow = candidateRow('Later_ID', { status: `goal 승격 → [있는 goal](../../${INDEXED_GOAL})` });
function writeBacklog() {
  const document = backlogDocument(['## 후보', ...CANDIDATE_TABLE_HEADER, firstRow, laterRow]);
  repository.write(BACKLOG_PATH, document.text);
  return { first: document.lineOf(firstRow), later: document.lineOf(laterRow) };
}
const readStore = () => createBacklogStore({ repositoryRoot: repository.root }).read();
const check = () => checkRecordIndex({ repositoryRoot: repository.root });
const groupStates = (result: Awaited<ReturnType<typeof check>>) => Object.fromEntries(result.groups.map(item => [item.group, item.ran]));

describe('a goal link whose check fails with an unexpected I/O error (behaviour step)', () => {
  it('is listed only in uncheckedLinks, and the later link of that row and the later rows are still judged', async () => {
    const lines = writeBacklog();
    io.deniedPath = repository.path(DENIED_FOLDER);
    const result = await readStore();
    const intercepted = io.deniedCount;
    expect(intercepted, 'the product walked through the denied folder with node:fs/promises lstat').toBeGreaterThan(0);
    expect(result).toEqual({
      ok: true,
      rows: expect.any(Array),
      uncheckedLinks: [{ line: lines.first, link: DENIED_LINK }],
      issues: [{ ...MISSING, line: lines.first }, { ...ID_FORMAT, line: lines.later }],
    });
  });

  it('makes records:check move the issues as warnings, then mark only the backlog group not run with exit code 2', async () => {
    const lines = writeBacklog();
    const clean = await check();
    expect({ groups: groupStates(clean), exitCode: clean.exitCode }, 'the fixture repository before the denial').toEqual({
      groups: { index: true, goals: true, backlog: true }, exitCode: 0,
    });

    io.deniedPath = repository.path(DENIED_FOLDER);
    const denied = await check();
    const intercepted = io.deniedCount;
    expect(intercepted).toBeGreaterThan(0);
    expect(groupStates(denied)).toEqual({ index: true, goals: true, backlog: false });
    expect(denied.exitCode).toBe(2);
    expect(denied.diagnostics).toEqual([
      { severity: 'warning', code: MISSING.code, location: `BACKLOG.md:${lines.first}`, cause: MISSING.cause, fix: MISSING.fix },
      { severity: 'warning', code: ID_FORMAT.code, location: `BACKLOG.md:${lines.later}`, cause: ID_FORMAT.cause, fix: ID_FORMAT.fix },
    ]);
  });
});

describe('BACKLOG.md replaced between the path checks and the read (behaviour step)', () => {
  function replaceOnOpen() {
    writeBacklog();
    io.replacement = repository.write('00_Document/operations/BACKLOG.replacement.tmp', backlogDocument(['## 후보', ...CANDIDATE_TABLE_HEADER, candidateRow('replacement-row')]).text);
    io.replaceTarget = repository.path(BACKLOG_PATH);
  }

  it('is changed with the fixed sentence, not the replacement rows', async () => {
    replaceOnOpen();
    const result = await readStore();
    const replaced = io.replacedCount;
    expect(replaced, 'the product opened BACKLOG.md through node:fs/promises open').toBe(1);
    expect(result).toEqual(CHANGED);
  });

  it('leaves the backlog group of records:check not run with exit code 2 and no BACKLOG_ diagnostic', async () => {
    replaceOnOpen();
    const result = await check();
    const replaced = io.replacedCount;
    expect(replaced).toBe(1);
    expect(groupStates(result)).toEqual({ index: true, goals: true, backlog: false });
    expect(result.diagnostics.filter(item => item.code.startsWith('BACKLOG_'))).toEqual([]);
    expect(result.exitCode).toBe(2);
  });
});

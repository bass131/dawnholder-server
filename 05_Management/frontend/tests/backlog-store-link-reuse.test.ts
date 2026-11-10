// @vitest-environment node
// Requirement: goal 2026-11-10-code-audit-management-fixes, item 3, defect #1 (「3번 검증 결과와 결함 #1」, lead
// decision): within one createBacklogStore(...).read(), a goal link target that BACKLOG.md names several times — in
// several rows, twice in one row, or in spellings that resolve to the same repository-relative locator — is checked
// on the file system no more often than in a document that names it once. Issues and uncheckedLinks still come per
// occurrence, in occurrence order, with that occurrence's line and link text, and every read judges the targets afresh.
// Observation without a product hook, the way tests/backlog-store-unchecked.test.ts does it: node:fs/promises lstat is
// wrapped and every path it is called with is recorded. Premise: the path check of the source reading boundary
// (source-section-store.ts resolveSourceFile) walks a goal target with node:fs/promises lstat, one call per path part,
// so the target file's own path is lstat'ed once per check whether the file exists or not. If the check stops doing
// so, the premise assertion (the target was checked at least once) fails before any comparison. Expected counts are
// never constants: each test measures a document that names the target once and compares with it.
import { rmSync } from 'node:fs';
import { resolve } from 'node:path';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { BacklogResult } from '../electron/backlog-contract';
import { createBacklogStore } from '../electron/backlog-store';
import { BACKLOG_PATH, CANDIDATE_TABLE_HEADER, backlogDocument, candidateRow } from './record-sources/backlog-fixture';
import { createOwnedTempRepository, type OwnedTempRepository } from './record-sources/owned-temp-repository';

const io = vi.hoisted(() => ({ lstatPaths: [] as string[], deniedPath: '' }));

vi.mock('node:fs/promises', async original => {
  const actual = await original<typeof import('node:fs/promises')>();
  const lstat = (async (path: Parameters<typeof actual.lstat>[0], ...rest: unknown[]) => {
    // Recorded before the real call: an absent target's lstat throws ENOENT and still counts as a check.
    const called = resolve(String(path)).toLowerCase();
    io.lstatPaths.push(called);
    if (io.deniedPath !== '' && called === resolve(io.deniedPath).toLowerCase()) {
      throw Object.assign(new Error('EACCES: permission denied (test double)'), { code: 'EACCES' });
    }
    return (actual.lstat as (...args: unknown[]) => ReturnType<typeof actual.lstat>)(path, ...rest);
  }) as typeof actual.lstat;
  return { ...actual, lstat, default: { ...actual, lstat } };
});

const GOAL_TEXT = '# 목표\n';
const PRESENT_GOAL = '01_Phases/goals/2026-10-01-present/goal.md';
// The folder exists and only goal.md is absent, so the walk reaches the target file's own path.
const ABSENT_FOLDER = '05_Management/goals/2026-10-02-absent';
const ABSENT_GOAL = `${ABSENT_FOLDER}/goal.md`;
const DENIED_GOAL = '01_Phases/goals/2026-10-03-denied/goal.md';

// Links as written in BACKLOG.md (00_Document/operations/). Every spelling of one target resolves to the same
// repository-relative locator. `./` and percent-encoding stay away from `<top>/goals/`, which the link extraction
// needs to see literally.
const PRESENT_LINK = `../../${PRESENT_GOAL}`;
const PRESENT_DOT_LINK = './../../01_Phases/goals/./2026-10-01-present/goal.md';
const PRESENT_ENCODED_LINK = '../../01_Phases/goals/2026%2D10%2D01-present/goal%2Emd';
const ABSENT_LINK = `../../${ABSENT_GOAL}`;
const ABSENT_DOT_LINK = './../../05_Management/goals/./2026-10-02-absent/goal.md';
const DENIED_LINK = `../../${DENIED_GOAL}`;
const DENIED_DOT_LINK = './../../01_Phases/goals/./2026-10-03-denied/goal.md';
const OUTSIDE_LINK = `../../../${PRESENT_GOAL}`;

// Fixed sentences, never computed by the product: REJECTED is the backlog-menu-design.md table row, MISSING is the
// sentence tests/backlog-store-boundary.test.ts and tests/backlog-store-unchecked.test.ts already pin.
const missing = (line: number) => ({
  code: 'BACKLOG_GOAL_LINK_MISSING', line, cause: '상대 goal 링크의 대상 파일이 없습니다.', fix: 'BACKLOG.md 기준 상대경로와 대상 goal 파일의 존재를 확인하세요.',
});
const rejected = (line: number, reason: string) => ({
  code: 'BACKLOG_GOAL_LINK_REJECTED',
  line,
  cause: `상대 goal 링크가 저장소 경로 규칙에 맞지 않습니다 (${reason}).`,
  fix: 'BACKLOG.md 기준 상대경로로 저장소 안의 goal 파일을 대소문자까지 같게 가리키고 링크·junction을 거치지 마세요.',
});

type BacklogStore = ReturnType<typeof createBacklogStore>;
type BacklogRead = Extract<BacklogResult, { ok: true }>;

let repository: OwnedTempRepository;
beforeEach(() => {
  repository = createOwnedTempRepository();
  repository.write(PRESENT_GOAL, GOAL_TEXT);
  repository.write(DENIED_GOAL, GOAL_TEXT);
  repository.mkdir(ABSENT_FOLDER);
  io.lstatPaths = [];
  io.deniedPath = '';
});
afterEach(() => repository.remove());

const newStore = () => createBacklogStore({ repositoryRoot: repository.root });

// Writes one promoted candidate row per entry, its links in the status cell in the given order, and returns the
// 1-based line of each row.
function writeBacklog(linkRows: string[][]): number[] {
  const rows = linkRows.map((links, index) => {
    const linkText = links.map((link, position) => `[goal ${position + 1}](${link})`).join(' ');
    return candidateRow(`link-reuse-${index + 1}`, { status: `goal 승격 → ${linkText}` });
  });
  const document = backlogDocument(['## 후보', ...CANDIDATE_TABLE_HEADER, ...rows]);
  repository.write(BACKLOG_PATH, document.text);
  return rows.map(row => document.lineOf(row));
}

// One read, and how many of its lstat calls were on the target file's own path.
async function readCountingChecks(store: BacklogStore, locator: string): Promise<{ result: BacklogRead; checks: number }> {
  io.lstatPaths = [];
  const result = await store.read();
  if (!result.ok) throw new Error(`fixture BACKLOG.md was not read: ${result.code}`);
  const target = resolve(repository.path(locator)).toLowerCase();
  const checks = io.lstatPaths.filter(path => path === target).length;
  return { result, checks };
}

// The reference: a fresh store reading a document that names the target once, in the repository's current state.
async function checksForOneOccurrence(link: string, locator: string): Promise<number> {
  writeBacklog([[link]]);
  const { checks } = await readCountingChecks(newStore(), locator);
  return checks;
}

const goalLinkCounts = (result: BacklogRead) => result.rows.map(row => row.goalLinks.length);

describe('goal 3 defect #1: one read checks a repeated goal link target as often as a target named once', () => {
  it('a present target named twice in one row and once in two later rows', async () => {
    const singleChecks = await checksForOneOccurrence(PRESENT_LINK, PRESENT_GOAL);
    expect(singleChecks, 'premise: the target path is checked with node:fs/promises lstat').toBeGreaterThan(0);

    writeBacklog([[PRESENT_LINK, PRESENT_LINK], [PRESENT_LINK], [PRESENT_LINK]]);
    const { result, checks: repeatedChecks } = await readCountingChecks(newStore(), PRESENT_GOAL);
    expect(goalLinkCounts(result), 'every occurrence was extracted as a goal link').toEqual([2, 1, 1]);
    expect(result.issues).toEqual([]);
    expect(repeatedChecks, `checks of ${PRESENT_GOAL} for 4 occurrences vs 1 occurrence`).toBe(singleChecks);
  });

  it('an absent target (its folder exists) named twice in one row and once in two later rows', async () => {
    const singleChecks = await checksForOneOccurrence(ABSENT_LINK, ABSENT_GOAL);
    expect(singleChecks, 'premise: the target path is checked with node:fs/promises lstat').toBeGreaterThan(0);

    const lines = writeBacklog([[ABSENT_LINK, ABSENT_LINK], [ABSENT_LINK], [ABSENT_LINK]]);
    const { result, checks: repeatedChecks } = await readCountingChecks(newStore(), ABSENT_GOAL);
    expect(goalLinkCounts(result), 'every occurrence was extracted as a goal link').toEqual([2, 1, 1]);
    expect(result.issues).toEqual([missing(lines[0]!), missing(lines[0]!), missing(lines[1]!), missing(lines[2]!)]);
    expect(repeatedChecks, `checks of ${ABSENT_GOAL} for 4 occurrences vs 1 occurrence`).toBe(singleChecks);
  });

  // Each spelling appears exactly once, so only treating the spellings as one locator keeps the count.
  it('one present target in three spellings that resolve to the same locator (plain, `./` mixed in, percent-encoded)', async () => {
    const singleChecks = await checksForOneOccurrence(PRESENT_LINK, PRESENT_GOAL);
    expect(singleChecks, 'premise: the target path is checked with node:fs/promises lstat').toBeGreaterThan(0);

    writeBacklog([[PRESENT_LINK], [PRESENT_DOT_LINK], [PRESENT_ENCODED_LINK]]);
    const { result, checks: spelledChecks } = await readCountingChecks(newStore(), PRESENT_GOAL);
    expect(goalLinkCounts(result), 'every spelling was extracted as a goal link').toEqual([1, 1, 1]);
    expect(result.issues, 'every spelling names the present goal.md').toEqual([]);
    expect(spelledChecks, `checks of ${PRESENT_GOAL} for 3 spellings vs 1 occurrence`).toBe(singleChecks);
  });
});

describe('goal 3 defect #1: issues and unchecked links stay per occurrence, in occurrence order', () => {
  it('lists MISSING for every occurrence of an absent target, with the REJECTED (parent) issue between them in place', async () => {
    const lines = writeBacklog([[ABSENT_LINK, ABSENT_DOT_LINK], [OUTSIDE_LINK], [ABSENT_LINK]]);
    const { result } = await readCountingChecks(newStore(), ABSENT_GOAL);
    const { issues, uncheckedLinks } = result;
    expect(issues).toEqual([missing(lines[0]!), missing(lines[0]!), rejected(lines[1]!, 'parent'), missing(lines[2]!)]);
    expect(uncheckedLinks).toEqual([]);
  });

  it('lists an I/O-failed (EACCES) target in uncheckedLinks at each occurrence, with that occurrence line and link text', async () => {
    const lines = writeBacklog([[DENIED_LINK], [ABSENT_LINK], [DENIED_DOT_LINK]]);
    io.deniedPath = repository.path(DENIED_GOAL);
    const { result, checks: deniedChecks } = await readCountingChecks(newStore(), DENIED_GOAL);
    const { issues, uncheckedLinks } = result;
    expect(deniedChecks, 'premise: the denied target was reached through node:fs/promises lstat').toBeGreaterThan(0);
    expect(uncheckedLinks).toEqual([{ line: lines[0]!, link: DENIED_LINK }, { line: lines[2]!, link: DENIED_DOT_LINK }]);
    expect(issues).toEqual([missing(lines[1]!)]);
  });
});

describe('goal 3 defect #1: every read of the same store judges the targets afresh', () => {
  // The same store reads a document naming the target in two rows, the target changes, and the store reads again.
  async function readAcrossChange(link: string, locator: string, change: () => void) {
    const lines = writeBacklog([[link], [link]]);
    const store = newStore();
    const first = await readCountingChecks(store, locator);
    change();
    const second = await readCountingChecks(store, locator);
    return { lines, first: first.result, second: second.result, secondChecks: second.checks };
  }
  const createAbsentGoal = () => {
    repository.write(ABSENT_GOAL, GOAL_TEXT);
  };
  const removePresentGoal = () => {
    rmSync(repository.path(PRESENT_GOAL));
  };

  it('drops the MISSING issues on the second read after the absent target is created', async () => {
    const { lines, first, second } = await readAcrossChange(ABSENT_LINK, ABSENT_GOAL, createAbsentGoal);
    expect(first.issues).toEqual([missing(lines[0]!), missing(lines[1]!)]);
    expect(second.issues).toEqual([]);
  });

  it('lists MISSING for every occurrence on the second read after the present target is removed', async () => {
    const { lines, first, second } = await readAcrossChange(PRESENT_LINK, PRESENT_GOAL, removePresentGoal);
    expect(first.issues).toEqual([]);
    expect(second.issues).toEqual([missing(lines[0]!), missing(lines[1]!)]);
  });

  it('checks the created target on the second read as often as a fresh read naming it once', async () => {
    const { secondChecks } = await readAcrossChange(ABSENT_LINK, ABSENT_GOAL, createAbsentGoal);
    const singleChecks = await checksForOneOccurrence(ABSENT_LINK, ABSENT_GOAL);
    expect(singleChecks, 'premise: the target path is checked with node:fs/promises lstat').toBeGreaterThan(0);
    expect(secondChecks, `second-read checks of ${ABSENT_GOAL} for 2 occurrences vs 1 occurrence`).toBe(singleChecks);
  });

  it('checks the removed target on the second read as often as a fresh read naming it once', async () => {
    const { secondChecks } = await readAcrossChange(PRESENT_LINK, PRESENT_GOAL, removePresentGoal);
    const singleChecks = await checksForOneOccurrence(PRESENT_LINK, PRESENT_GOAL);
    expect(singleChecks, 'premise: the target path is checked with node:fs/promises lstat').toBeGreaterThan(0);
    expect(secondChecks, `second-read checks of ${PRESENT_GOAL} for 2 occurrences vs 1 occurrence`).toBe(singleChecks);
  });
});

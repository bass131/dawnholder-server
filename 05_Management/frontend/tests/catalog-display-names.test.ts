/// <reference path="../src/recordsBridge.d.ts" />
// Requirement (main msg_f5293de719b3 relaying the user's choice A, and the user rule of 2026-10-02):
// record and card sentences name the work itself instead of internal milestone/stage codes such as
// M1b, P0, D0 or S단계. A code may remain only as a trace in parentheses directly after a name.
// Exact values — ids, locators, revisions/hashes, PR numbers, D-/R- decision ids, analyzer ids — are
// not stage codes; their fields are listed below with the reason they stay exact.
import '@testing-library/jest-dom/vitest';
import { cleanup, render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { createElement } from 'react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import catalogFile from '../../records/catalog.json';
import guideFile from '../../records/system-guide.json';
import { catalogReferenceErrors, readCatalog } from '../electron/catalog-contract';
import DevelopmentRecords from '../src/DevelopmentRecords';

// An upper-case M/S/D/P with a number (optionally one lower-case suffix), or "<letter>단계".
// A preceding letter, digit, _, #, . or - means the text belongs to a hash, path, anchor or
// analyzer id (SHA256:D2649…, #s1, IDE0011), so it is not read as a stage code.
const STAGE_CODE = /(?<![A-Za-z0-9_#.\-])(?:[MSDP]\d+[a-z]?|[MSDP]단계)(?![A-Za-z0-9_])/gu;
const LONE_STAGE_CODE = new RegExp(`^${STAGE_CODE.source}$`, 'u');

interface Leaf { pointer: string; text: string }
interface Finding { pointer: string; code: string; text: string }

// "클라이언트 연결 수명 정리 (M1b)" keeps a trace after a name. "S2(S1 포함)" and "(M1b) PR132" do not.
function isTraceAfterName(text: string, index: number): boolean {
  const open = text.lastIndexOf('(', index);
  if (open < 0 || text.lastIndexOf(')', index) > open) return false;
  const head = text.slice(0, open).trimEnd();
  const lastWord = head.split(/\s+/).pop() ?? '';
  return /[가-힣]$/u.test(head) && !LONE_STAGE_CODE.test(lastWord);
}

function standaloneStageCodes(text: string): string[] {
  const codes: string[] = [];
  for (const match of text.matchAll(STAGE_CODE)) {
    if (match.index === undefined || isTraceAfterName(text, match.index)) continue;
    codes.push(match[0]);
  }
  return codes;
}

function stringLeaves(value: unknown, pointer = ''): Leaf[] {
  if (typeof value === 'string') return [{ pointer, text: value }];
  if (Array.isArray(value)) return value.flatMap((item, index) => stringLeaves(item, `${pointer}/${index}`));
  if (typeof value === 'object' && value !== null) {
    return Object.entries(value).flatMap(([key, item]) => stringLeaves(item, `${pointer}/${key}`));
  }
  return [];
}

function findings(leaves: Leaf[]): Finding[] {
  return leaves.flatMap(leaf => standaloneStageCodes(leaf.text).map(code => ({ pointer: leaf.pointer, code, text: leaf.text })));
}

function explain(found: Finding[]): string {
  return found.map(({ pointer, code, text }) => [
    `${pointer}: '${code}'가 작업 이름 없이 표시됩니다.`,
    `연결된 goal 제목·단계 표의 작업 내용 이름으로 바꾸고, 추적이 꼭 필요하면 '이름 (${code})'처럼 이름 뒤 괄호에 두세요.`,
    `문장: ${text}`,
  ].join(' ')).join('\n');
}

interface FieldRules { display: RegExp[]; exact: [RegExp, string][] }

function displayLeaves(value: unknown, rules: FieldRules): { display: Leaf[]; unclassified: string[] } {
  const display: Leaf[] = [];
  const unclassified: string[] = [];
  for (const leaf of stringLeaves(value)) {
    if (rules.display.some(pattern => pattern.test(leaf.pointer))) display.push(leaf);
    else if (!rules.exact.some(([pattern]) => pattern.test(leaf.pointer))) unclassified.push(leaf.pointer);
  }
  return { display, unclassified };
}

function explainUnclassified(pointers: string[]): string {
  return pointers.map(pointer => [
    `${pointer}: 표시 문장인지 정확 값인지 분류되지 않은 문자열 필드입니다.`,
    '화면에 문장으로 보이면 display 목록에, id·경로·버전처럼 바꾸면 안 되는 값이면 exact 목록에 이유와 함께 추가하세요.',
  ].join(' ')).join('\n');
}

// Sentences rendered by DevelopmentRecords.tsx (list and detail titles, areas and 원문 section headings).
// Record index v2 (goal 2026-10-06-record-source-unification 「만들 것」 1, index-v2-design.md 「색인 형식」):
// the narrative and status fields are gone, so only titles, areas and sections remain sentences.
const CATALOG_FIELDS: FieldRules = {
  display: [
    /^\/sources\/\d+\/(title|section)$/,
    /^\/systems\/\d+\/(title|area)$/,
    /^\/records\/\d+\/title$/,
  ],
  exact: [
    [/^\/sources\/\d+\/(id|locator)$/, 'source id and exact path or message id'],
    [/^\/sources\/\d+\/(kind|availability)$/, 'contract enum'],
    [/^\/(systems|records)\/\d+\/id$/, 'reference identity'],
    [/^\/systems\/\d+\/(sourceIds|relatedSystemIds|recordIds)\/\d+$/, 'reference id'],
    [/^\/records\/\d+\/(systemIds|sourceIds)\/\d+$/, 'reference id'],
    [/^\/records\/\d+\/type$/, 'contract enum'],
    [/^\/records\/\d+\/pullRequests\/\d+\/mergeCommit$/, 'merge commit hash'],
  ],
};

// Sentences rendered by SystemCardsView.tsx and ImplementationDocument.tsx. Diagram sources are
// included because their labels are drawn and the source text can be expanded on screen.
const GUIDE_FIELDS: FieldRules = {
  display: [
    /^\/coverageNote$/,
    /^\/cards\/\d+\/(title|summary)$/,
    /^\/cards\/\d+\/codeReference\/mappings\/\d+\/role$/,
    /^\/documents\/\d+\/(title|sections\/\d+\/title)$/,
    /^\/documents\/\d+\/sections\/\d+\/blocks\/\d+\/(text|title|description|source)$/,
  ],
  exact: [
    [/^\/(revision|asOf|coverage)$/, 'guide version, snapshot date and coverage id'],
    [/^\/cards\/\d+\/(id|parentId|status|documentId)$/, 'card identity, hierarchy and status enum'],
    [/^\/cards\/\d+\/relatedSystemIds\/\d+$/, 'catalog reference id'],
    [/^\/cards\/\d+\/codeReference\/(commitSha|mappings\/\d+\/(path|kind|namespace))$/, 'exact code reference'],
    [/^\/documents\/\d+\/(id|cardId|sourceCommit)$/, 'document identity and commit hash'],
    [/^\/documents\/\d+\/sourceRefs\/\d+\/(path|symbol)$/, 'exact code reference'],
    [/^\/documents\/\d+\/sections\/\d+\/id$/, 'section identity'],
    [/^\/documents\/\d+\/sections\/\d+\/blocks\/\d+\/(id|type|format)$/, 'block identity and enum'],
  ],
};

describe('stage-code detector', () => {
  it.each([
    ['M1b PR132 완료·병합', ['M1b']],
    ['P0는 정식 문서 준비 중이다', ['P0']],
    ['M1b가 연결 수명을 정리했다', ['M1b']],
    ['P1~P7 미착수', ['P1', 'P7']],
    ['M0→M3 비맹검 평가', ['M0', 'M3']],
    ['이번 S단계 범위가 아니다', ['S단계']],
    ['S2(S1 포함) 2e6b8c3', ['S2', 'S1']],
    ['(M1b) PR132 완료', ['M1b']],
    ['S1/S2의 생산 8파일', ['S1', 'S2']],
    ['P2 D1a 기술 명세', ['P2', 'D1a']],
  ])('flags a code standing in for a name: %s', (text, codes) => {
    expect(standaloneStageCodes(text)).toEqual(codes);
  });
  it.each([
    '클라이언트 연결 수명 정리 PR132 완료·병합',
    '클라이언트 연결 수명 정리 (M1b) PR132 완료·병합',
    '즉시 피해 처리 책임 통합 (#s1)',
    '01_Phases/goals/2026-09-29-persistence-design/goal.md',
    'D-08 결정과 R-2 요구',
    'UI3/UI4 검증 수량',
    'SA1201·SA1202·IDE0011 오류 적용',
    '.NET815 통과/전체820(기존skip5)',
    'SHA256:D2649FE0DBE9039E',
    'message:msg_b5283836b43f',
    'PR140~145 MERGED',
    'a3c4e15→2b533c7',
  ])('keeps names, traces after names and exact values: %s', text => {
    expect(standaloneStageCodes(text)).toEqual([]);
  });
});

describe('development records data', () => {
  const { display, unclassified } = displayLeaves(catalogFile, CATALOG_FIELDS);
  it('classifies every string field as a display sentence or an exact value', () => {
    expect(unclassified, explainUnclassified(unclassified)).toEqual([]);
    expect(display.length).toBeGreaterThan(0);
  });
  it('names the work instead of a stage code in every display sentence', () => {
    const found = findings(display);
    expect(found, explain(found)).toEqual([]);
  });
  it('remains a valid catalog with complete references', () => {
    const catalog = readCatalog(catalogFile);
    expect(catalog).not.toBeNull();
    expect(catalog && catalogReferenceErrors(catalog)).toEqual([]);
  });
});

describe('system guide data', () => {
  const { display, unclassified } = displayLeaves(guideFile, GUIDE_FIELDS);
  it('classifies every string field and names the work in every display sentence', () => {
    expect(unclassified, explainUnclassified(unclassified)).toEqual([]);
    expect(display.length).toBeGreaterThan(0);
    const found = findings(display);
    expect(found, explain(found)).toEqual([]);
  });
});

describe('rendered development records', () => {
  // Checked inside the test so an invalid catalog fails that test instead of the whole file.
  const catalog = readCatalog(catalogFile);
  beforeEach(() => {
    // Read-only bridge of index-v2-design.md 「Electron 경계」: no editable text, no save.
    vi.stubGlobal('systemRecords', {
      readCatalog: vi.fn(async () => ({ ok: true, catalog, version: 'a'.repeat(64) })),
      readSection: vi.fn(async () => ({ ok: false, code: 'missing', reason: null, message: '원문 파일을 찾을 수 없습니다.' })),
      readCheckout: vi.fn(async () => ({ ok: true, checkout: { state: 'unknown', reason: 'no-git' } })),
    });
    // jsdom has no layout; opening a detail scrolls its heading into view.
    Element.prototype.scrollIntoView = vi.fn();
  });
  afterEach(() => { cleanup(); vi.unstubAllGlobals(); });

  // Text nodes are scanned one by one so neighbouring elements cannot hide a code behind a letter.
  function renderedTexts(root: Element): Leaf[] {
    const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT);
    const texts: Leaf[] = [];
    for (let node = walker.nextNode(); node; node = walker.nextNode()) texts.push({ pointer: 'rendered text', text: node.textContent ?? '' });
    return texts;
  }

  // The test opens every system and record detail with two clicks, so its run time grows with the
  // item count and passed the 5 s default under a full parallel run. 250 ms per opened item leaves
  // about five times the per-item time of a run alone.
  const RENDER_TIMEOUT_MS = Math.max(5_000, (catalogFile.systems.length + catalogFile.records.length) * 250);
  it('shows systems, records and sources without stage codes on the records screen', async () => {
    if (!catalog) throw new Error('Invalid canonical catalog');
    // DevelopmentRecords renders every catalog sentence across its lists and full-page details;
    // App.tsx only switches the hidden tab around it.
    // Opening every system and record detail takes dozens of clicks, so no artificial delay runs between them.
    const user = userEvent.setup({ delay: null });
    const { container } = render(createElement(DevelopmentRecords));
    await screen.findByRole('searchbox', { name: '검색' });
    const browser = container.querySelector('.records-browser');
    if (!browser) throw new Error('Records browser was not rendered');
    // Lists show only areas, types and titles, so every other sentence is read by opening each item's detail.
    async function detailTexts(root: Element, listName: string): Promise<Leaf[]> {
      const texts: Leaf[] = [];
      const count = within(screen.getByRole('list', { name: listName })).getAllByRole('button').length;
      for (let index = 0; index < count; index++) {
        const item = within(screen.getByRole('list', { name: listName })).getAllByRole('button')[index];
        if (!item) throw new Error(`${listName} item ${index} disappeared`);
        await user.click(item);
        texts.push(...renderedTexts(root));
        await user.click(screen.getByRole('button', { name: '목록으로' }));
      }
      return texts;
    }
    const views: [RegExp, string | null, (texts: string[]) => void][] = [
      [/^시스템\s*\d/, '시스템 목록', texts => catalog.systems.forEach(system => expect(texts.join('\n')).toContain(system.title))],
      [/^변경·결정·검증·계획/, '기록 목록', texts => catalog.records.forEach(record => expect(texts.join('\n')).toContain(record.title))],
      [/^출처\s*\d/, null, texts => catalog.sources.forEach(source => expect(texts.join('\n')).toContain(source.title))],
    ];
    for (const [name, listName, covers] of views) {
      await user.click(screen.getByRole('button', { name }));
      const texts = renderedTexts(browser);
      if (listName) texts.push(...await detailTexts(browser, listName));
      covers(texts.map(leaf => leaf.text));
      const found = findings(texts);
      expect(found, explain(found)).toEqual([]);
    }
  }, RENDER_TIMEOUT_MS);
});

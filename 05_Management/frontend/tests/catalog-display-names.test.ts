/// <reference path="../src/recordsBridge.d.ts" />
// Requirement (main msg_f5293de719b3 relaying the user's choice A, and the user rule of 2026-10-02):
// record and card sentences name the work itself instead of internal milestone/stage codes such as
// M1b, P0, D0 or S단계. A code may remain only as a trace in parentheses directly after a name.
// Exact values — ids, locators, revisions/hashes, PR numbers, D-/R- decision ids, analyzer ids — are
// not stage codes; their fields are listed below with the reason they stay exact.
import '@testing-library/jest-dom/vitest';
import { cleanup, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { createElement } from 'react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import catalogFile from '../../records/catalog.json';
import guideFile from '../../records/system-guide.json';
import { catalogReferenceErrors, readCatalog, type CatalogResult } from '../electron/catalog-contract';
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

// Sentences rendered by DevelopmentRecords.tsx (snapshot note, Sources, Status, Items, card and entry text).
const CATALOG_FIELDS: FieldRules = {
  display: [
    /^\/scopeNote$/,
    /^\/sources\/\d+\/(title|section|note)$/,
    /^\/systems\/\d+\/(title|area|summary|responsibility|implementationStatus|integrationStatus|verificationStatus)$/,
    /^\/systems\/\d+\/(behavior|limitations|nextSteps)\/\d+$/,
    /^\/records\/\d+\/(title|summary|reason|status)$/,
    /^\/records\/\d+\/(details|limitations|nextSteps)\/\d+$/,
  ],
  exact: [
    [/^\/(revision|asOf|sourceCommit)$/, 'record version, snapshot time and commit hash'],
    [/^\/sources\/\d+\/(id|locator|revision)$/, 'evidence id, exact path and version/hash'],
    [/^\/sources\/\d+\/(kind|availability)$/, 'contract enum'],
    [/^\/(systems|records)\/\d+\/id$/, 'reference identity'],
    [/^\/systems\/\d+\/(sourceIds|relatedSystemIds|recordIds)\/\d+$/, 'reference id'],
    [/^\/records\/\d+\/(systemIds|sourceIds)\/\d+$/, 'reference id'],
    [/^\/records\/\d+\/type$/, 'contract enum'],
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
  it('reads the requirement example as a work name next to its PR number', () => {
    expect(display.some(leaf => leaf.text.includes('클라이언트 연결 수명 정리 PR132 완료·병합'))).toBe(true);
    expect(display.filter(leaf => leaf.text.includes('M1b')).map(leaf => leaf.pointer)).toEqual([]);
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
  const catalog = readCatalog(catalogFile);
  if (!catalog) throw new Error('Invalid canonical catalog');
  const loaded: CatalogResult = { ok: true, catalog, text: JSON.stringify(catalog), version: 'a'.repeat(64) };
  beforeEach(() => {
    vi.stubGlobal('systemRecords', { readCatalog: vi.fn(async () => loaded), saveCatalog: vi.fn() });
  });
  afterEach(() => { cleanup(); vi.unstubAllGlobals(); });

  // Text nodes are scanned one by one so neighbouring elements cannot hide a code behind a letter.
  function renderedTexts(root: Element): Leaf[] {
    const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT);
    const texts: Leaf[] = [];
    for (let node = walker.nextNode(); node; node = walker.nextNode()) texts.push({ pointer: 'rendered text', text: node.textContent ?? '' });
    return texts;
  }

  it('shows systems, records and evidence without stage codes on the records screen', async () => {
    // DevelopmentRecords renders every catalog sentence; App.tsx only switches the hidden tab around it.
    const user = userEvent.setup();
    const { container } = render(createElement(DevelopmentRecords));
    await screen.findByRole('searchbox', { name: '검색' });
    const browser = container.querySelector('.records-browser');
    if (!browser) throw new Error('Records browser was not rendered');
    const views: [RegExp, (texts: string[]) => void][] = [
      [/^시스템\s*\d/, texts => catalog.systems.forEach(system => expect(texts).toContain(system.summary))],
      [/^변경·결정·검증·계획/, texts => catalog.records.forEach(record => expect(texts).toContain(record.title))],
      [/^근거\s*\d/, texts => catalog.sources.forEach(source => expect(texts).toContain(source.note))],
    ];
    for (const [name, covers] of views) {
      await user.click(screen.getByRole('button', { name }));
      const texts = renderedTexts(browser);
      covers(texts.map(leaf => leaf.text));
      expect(texts.map(leaf => leaf.text)).toContain(catalog.scopeNote);
      const found = findings(texts);
      expect(found, explain(found)).toEqual([]);
    }
    await user.click(screen.getByRole('button', { name: /^시스템\s*\d/ }));
    expect(screen.getByText(/^클라이언트 연결 수명 정리 PR132 완료·병합/)).toBeVisible();
  });
});

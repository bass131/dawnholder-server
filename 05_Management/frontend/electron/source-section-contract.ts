import type { RecordSource } from './catalog-contract.js';

export const MAX_SOURCE_FILE_BYTES = 1024 * 1024;
export const MAX_SOURCE_SECTION_BYTES = 256 * 1024;

export type SourceSectionCode =
  | 'invalid-request' | 'index-unavailable' | 'unknown-source' | 'not-readable'
  | 'path-rejected' | 'missing' | 'too-large' | 'changed' | 'invalid-encoding'
  | 'section-missing' | 'section-ambiguous' | 'load' | 'denied';
export type SourcePathReason = 'drive' | 'absolute' | 'parent' | 'invalid' | 'link' | 'case';
export type SourceSectionReason = SourcePathReason | 'local-only' | 'handoff' | 'extension' | 'not-file' | 'file' | 'section' | null;

export interface SourceSectionFailure {
  ok: false;
  code: SourceSectionCode;
  reason: SourceSectionReason;
  message: string;
}

export type SourceSectionResult =
  | { ok: true; sourceId: string; path: string; heading: string | null; text: string; bytes: number }
  | SourceSectionFailure;

const messages: Record<SourceSectionCode, string> = {
  'invalid-request': '출처 ID 형식이 올바르지 않습니다.',
  'index-unavailable': '기록 색인을 읽을 수 없습니다.',
  'unknown-source': '등록된 출처를 찾을 수 없습니다.',
  'not-readable': '앱에서 읽을 수 없는 출처입니다.',
  'path-rejected': '허용되지 않은 출처 경로입니다.',
  missing: '출처 파일이 없습니다.',
  'too-large': '출처 파일 또는 원문 구간의 크기 상한을 넘었습니다.',
  changed: '읽는 동안 출처 파일이 변경되었습니다. 다시 읽으세요.',
  'invalid-encoding': '출처 파일의 UTF-8 인코딩이 올바르지 않습니다.',
  'section-missing': '출처의 제목 구간을 찾을 수 없습니다.',
  'section-ambiguous': '같은 제목 구간이 여러 개 있어 원문을 고를 수 없습니다.',
  load: '출처 파일을 읽지 못했습니다. 파일 접근 상태를 확인하세요.',
  denied: '이 창에는 기록 접근 권한이 없습니다.',
};

export function sourceSectionFailure(code: SourceSectionCode, reason: SourceSectionReason = null): SourceSectionFailure {
  return { ok: false, code, reason, message: messages[code] };
}

export function sourcePathParts(locator: string): { ok: true; parts: string[] } | SourceSectionFailure {
  if (/^[a-z]:/i.test(locator)) return sourceSectionFailure('path-rejected', 'drive');
  if (locator.startsWith('/')) return sourceSectionFailure('path-rejected', 'absolute');
  const parts = locator.split('/');
  if (parts.includes('..')) return sourceSectionFailure('path-rejected', 'parent');
  if (locator.length === 0 || locator.length > 512
    || /[\\\u0000-\u001f\u007f*?:<>"|]/.test(locator)
    || parts.some(part => part === '' || part === '.')) {
    return sourceSectionFailure('path-rejected', 'invalid');
  }
  return { ok: true, parts };
}

export function sourceReadability(source: RecordSource): 'local-only' | 'handoff' | 'extension' | null {
  if (source.kind === 'local') return 'local-only';
  if (source.kind === 'handoff') return 'handoff';
  return source.locator.endsWith('.md') ? null : 'extension';
}

interface Heading {
  text: string;
  level: number;
  offset: number;
}

function markdownHeadings(text: string): Heading[] {
  const headings: Heading[] = [];
  let fence: { marker: string; length: number } | null = null;
  const lines = text.matchAll(/([^\r\n]*)(?:\r\n|\r|\n|$)/g);
  for (const line of lines) {
    const content = line[1] ?? '';
    const fenceMatch = /^ {0,3}(`{3,}|~{3,})(.*)$/.exec(content);
    if (fence) {
      // A shorter or different fence cannot end the current fenced block.
      if (fenceMatch && fenceMatch[1]?.[0] === fence.marker
        && fenceMatch[1].length >= fence.length && /^[ \t]*$/.test(fenceMatch[2] ?? '')) {
        fence = null;
      }
      continue;
    }
    if (fenceMatch?.[1]) {
      const marker = fenceMatch[1][0] ?? '';
      if (marker !== '`' || !fenceMatch[2]?.includes('`')) {
        fence = { marker, length: fenceMatch[1].length };
        continue;
      }
    }
    const heading = /^ {0,3}(#{1,6})(?:[ \t]+(.*)|$)/.exec(content);
    if (!heading?.[1]) continue;
    const title = (heading[2] ?? '')
      .replace(/(?:[ \t]+|^)#+[ \t]*$/, '')
      .replace(/^[ \t]+|[ \t]+$/g, '');
    headings.push({ text: title, level: heading[1].length, offset: line.index });
  }
  return headings;
}

export function extractSourceSection(source: RecordSource, decoded: string): SourceSectionResult {
  const text = decoded.startsWith('\uFEFF') ? decoded.slice(1) : decoded;
  let sectionText = text;
  if (source.section !== null) {
    const headings = markdownHeadings(text);
    const matches = headings.filter(heading => heading.text === source.section);
    const selected = matches[0];
    if (!selected) return sourceSectionFailure('section-missing');
    if (matches.length > 1) return sourceSectionFailure('section-ambiguous');
    const next = headings.find(heading => heading.offset > selected.offset && heading.level <= selected.level);
    sectionText = text.slice(selected.offset, next?.offset ?? text.length);
  }
  const bytes = new TextEncoder().encode(sectionText).length;
  if (bytes > MAX_SOURCE_SECTION_BYTES) return sourceSectionFailure('too-large', 'section');
  return { ok: true, sourceId: source.id, path: source.locator, heading: source.section, text: sectionText, bytes };
}

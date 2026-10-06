// @vitest-environment node
// Requirement: index-v2-design.md 「색인 형식」 정확한 제목 문구·구간 and 「원문 구간 읽기 경계」
// step 9, with the section:null and result rules fixed by Astra msg_28ce06a18c04 (Q1·Q2).
// Exact heading text: an ATX line (0-3 leading spaces, 1-6 #) without the leading # and spaces
// and without a closing # run and spaces. Lines in ``` or ~~~ fences are not headings, Setext is
// not supported, a leading BOM is ignored. A section runs from its heading line to just before the
// next heading of the same or a higher level, keeping the original newlines.
// Each case writes one file into an owned TEMP root and reads it through createSourceSectionStore.
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { createSourceSectionStore } from '../electron/source-section-store';
import { gitSource } from './record-sources/catalog-v2-fixture';
import { createOwnedTempRepository, type OwnedTempRepository } from './record-sources/owned-temp-repository';

let repository: OwnedTempRepository;
beforeEach(() => { repository = createOwnedTempRepository(); });
afterEach(() => repository.remove());

async function readSection(content: string | Uint8Array, section: string | null) {
  repository.write('docs/page.md', content);
  return createSourceSectionStore({ repositoryRoot: repository.root }).read(gitSource('page', 'docs/page.md', section));
}

async function sectionText(content: string | Uint8Array, section: string | null): Promise<string | { code: string }> {
  const result = await readSection(content, section);
  return result.ok ? result.text : { code: result.code };
}

const NESTED = [
  '# 문서',
  '머리말',
  '## 첫 절',
  '첫 본문',
  '### 첫 절 세부',
  '세부 본문',
  '## 둘째 절',
  '둘째 본문',
  '# 다른 문서',
  '끝',
  '',
].join('\n');

describe('section boundaries', () => {
  it('ends a section just before the next heading of the same or a higher level and keeps deeper headings', async () => {
    expect(await sectionText(NESTED, '첫 절')).toBe('## 첫 절\n첫 본문\n### 첫 절 세부\n세부 본문\n');
    expect(await sectionText(NESTED, '첫 절 세부')).toBe('### 첫 절 세부\n세부 본문\n');
    expect(await sectionText(NESTED, '문서')).toBe('# 문서\n머리말\n## 첫 절\n첫 본문\n### 첫 절 세부\n세부 본문\n## 둘째 절\n둘째 본문\n');
  });

  it('runs the last section to the end of the file', async () => {
    expect(await sectionText(NESTED, '다른 문서')).toBe('# 다른 문서\n끝\n');
  });

  it('keeps CRLF newlines and matches the heading without the carriage return', async () => {
    const crlf = '# A\r\n본문\r\n# B\r\n다음\r\n';
    const result = await readSection(crlf, 'A');
    expect(result).toMatchObject({ ok: true, heading: 'A', text: '# A\r\n본문\r\n' });
  });

  it('reads the whole file without its BOM for section null, with heading null', async () => {
    const body = '서문\n# 첫 제목\n본문\n';
    const withBom = new Uint8Array([0xef, 0xbb, 0xbf, ...Buffer.from(body, 'utf8')]);
    const result = await readSection(withBom, null);
    expect(result).toEqual({ ok: true, sourceId: 'page', path: 'docs/page.md', heading: null, text: body, bytes: Buffer.byteLength(body, 'utf8') });
  });

  it('ignores a leading BOM when finding the first heading', async () => {
    const withBom = new Uint8Array([0xef, 0xbb, 0xbf, ...Buffer.from('# 첫 제목\n본문\n', 'utf8')]);
    expect(await sectionText(withBom, '첫 제목')).toBe('# 첫 제목\n본문\n');
  });

  it('counts bytes as UTF-8 bytes of the returned text', async () => {
    const result = await readSection('## 한글 제목\n가나다\n', '한글 제목');
    expect(result).toMatchObject({ ok: true, bytes: Buffer.byteLength('## 한글 제목\n가나다\n', 'utf8') });
  });
});

describe('exact heading text', () => {
  it('recognises ATX levels one to six and not seven #', async () => {
    const levels = ['# 하나', '## 둘', '### 셋', '#### 넷', '##### 다섯', '###### 여섯', '####### 일곱', ''].join('\n');
    for (const heading of ['하나', '둘', '셋', '넷', '다섯', '여섯']) {
      const result = await readSection(levels, heading);
      expect(result.ok ? result.heading : result.code, heading).toBe(heading);
    }
    expect(await sectionText(levels, '일곱')).toEqual({ code: 'section-missing' });
    expect(await sectionText(levels, '# 일곱')).toEqual({ code: 'section-missing' });
  });

  it('drops a closing # run and the spaces around it', async () => {
    const closing = '## 닫는 제목 ##\n본문\n### 공백 뒤 닫힘 #   \n본문\n';
    expect(await sectionText(closing, '닫는 제목')).toBe('## 닫는 제목 ##\n본문\n### 공백 뒤 닫힘 #   \n본문\n');
    expect(await sectionText(closing, '공백 뒤 닫힘')).toBe('### 공백 뒤 닫힘 #   \n본문\n');
    expect(await sectionText(closing, '닫는 제목 ##')).toEqual({ code: 'section-missing' });
  });

  it('accepts zero to three leading spaces and not four', async () => {
    const indented = '   ### 세 칸 들여쓰기\n본문\n    ### 네 칸 코드\n코드\n';
    expect(await sectionText(indented, '세 칸 들여쓰기')).toBe('   ### 세 칸 들여쓰기\n본문\n    ### 네 칸 코드\n코드\n');
    expect(await sectionText(indented, '네 칸 코드')).toEqual({ code: 'section-missing' });
  });

  it('compares bold and code marks as written', async () => {
    const marked = '## **굵게** `code` 제목\n본문\n';
    expect(await sectionText(marked, '**굵게** `code` 제목')).toBe(marked);
    expect(await sectionText(marked, '굵게 code 제목')).toEqual({ code: 'section-missing' });
  });

  it('does not see lines inside ``` or ~~~ fences as headings, so a fenced line neither matches nor ends a section', async () => {
    const fenced = [
      '## 바깥',
      '```',
      '## 펜스 안 백틱',
      '# 끝처럼 보임',
      '```',
      '~~~',
      '## 펜스 안 물결',
      '~~~',
      '바깥 계속',
      '## 다음',
      '',
    ].join('\n');
    expect(await sectionText(fenced, '펜스 안 백틱')).toEqual({ code: 'section-missing' });
    expect(await sectionText(fenced, '펜스 안 물결')).toEqual({ code: 'section-missing' });
    expect(await sectionText(fenced, '바깥')).toBe(fenced.slice(0, fenced.indexOf('## 다음')));
  });

  it('does not count a fenced copy of a heading toward ambiguity', async () => {
    const copy = '## 하나\n본문\n```\n## 하나\n```\n';
    expect(await sectionText(copy, '하나')).toBe(copy);
  });

  it('does not support Setext headings', async () => {
    expect(await sectionText('제목\n===\n본문\n', '제목')).toEqual({ code: 'section-missing' });
  });
});

describe('section failures', () => {
  it('reports section-missing with reason null when no heading has the text', async () => {
    const result = await readSection(NESTED, '없는 제목');
    expect(result.ok ? null : { code: result.code, reason: result.reason }).toEqual({ code: 'section-missing', reason: null });
  });

  it('reports section-ambiguous when two headings share the text, whatever their levels', async () => {
    for (const content of ['## 같은 제목\n가\n## 같은 제목\n나\n', '# 같은 제목\n가\n### 같은 제목\n나\n']) {
      const result = await readSection(content, '같은 제목');
      expect(result.ok ? null : { code: result.code, reason: result.reason }, content).toEqual({ code: 'section-ambiguous', reason: null });
    }
  });

  it('applies the 256 KiB section limit to section null as well', async () => {
    const whole = `# 전체\n${'x'.repeat(256 * 1024)}\n`;
    const result = await readSection(whole, null);
    expect(result.ok ? null : { code: result.code, reason: result.reason }).toEqual({ code: 'too-large', reason: 'section' });
  });
});

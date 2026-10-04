// Independent tests for the child's static-SVG adapter and for the parent gate on the string the child displays
// (R-14 result → gate → approve → shown; design-spec 2.8 "장식·애니메이션은 쓰지 않는다"; 8.3 "외부 참조·이벤트·
// foreignObject를 성공 SVG로 허용하지 않는다").
//
// Fixtures in ./diagram-static-svg/ are the unmodified `mermaid.render` output of Mermaid 12.0.0 for the three
// representative guide sources, as returned by the product child in the 2026-10-03 independent run before the
// adapter existed. They pin the library's output shape. Expectations are about what is drawn (elements, attributes,
// text, applied CSS) and what is refused, not about the adapter's removal steps.
/// <reference types="vite/client" />
import { describe, expect, it } from 'vitest';
import { staticDiagramSvg } from '../src/diagrams/static-svg';
import { svgError } from '../src/diagrams/svg-check';
import callSvg from './diagram-static-svg/mermaid-flowchart-call.svg?raw';
import packetSvg from './diagram-static-svg/mermaid-sequence-packet.svg?raw';
import stateSvg from './diagram-static-svg/mermaid-state-window.svg?raw';

const SVG = 'http://www.w3.org/2000/svg';
const fixtures: Record<string, string> = { 'mermaid-flowchart-call.svg': callSvg, 'mermaid-sequence-packet.svg': packetSvg, 'mermaid-state-window.svg': stateSvg };
const fixture = (file: string) => { const text = fixtures[file]; if (!text) throw new Error(`missing fixture ${file}`); return text; };
const representatives = [
  { kind: 'call flowchart', file: 'mermaid-flowchart-call.svg', labels: ['수신 버퍼 기록', '길이 헤더 검사', '완성 프레임 분리', '게임 패킷 처리', '처리 길이만 소비', '다음 수신 등록'] },
  { kind: 'packet sequence', file: 'mermaid-sequence-packet.svg', labels: ['Unity 클라이언트', '게임 서버', 'C_Handshake · clientVersion', '첫 패킷과 버전 검사', 'S_HandshakeResult · ok=true', '메인 스레드에서 성공 반영'] },
  { kind: 'window state', file: 'mermaid-state-window.svg', labels: ['표시 준비', '창 표시', '창 숨김', '앱 종료', '준비 완료', '닫기 버튼', '트레이 열기', '트레이 종료'] },
];

function parse(svg: string): Document {
  const doc = new DOMParser().parseFromString(svg, 'image/svg+xml');
  expect(doc.querySelector('parsererror'), 'XML parse').toBeNull();
  return doc;
}
const serialize = (doc: Document) => new XMLSerializer().serializeToString(doc.documentElement);
// A refusal is the adapter's own Error. A TypeError or RangeError is a crash that would only look like a refusal.
function childOutcome(input: string): 'accepted' | 'refused' | 'crashed' {
  try {
    staticDiagramSvg(input);
    return 'accepted';
  } catch (error) {
    return error instanceof TypeError || error instanceof RangeError ? 'crashed' : 'refused';
  }
}
const attributes = (element: Element) => Object.fromEntries(Array.from(element.attributes).map(attr => [attr.name, attr.value]));
const ownText = (element: Element) => Array.from(element.childNodes).filter(node => node.nodeType === 3 || node.nodeType === 4).map(node => node.nodeValue).join('');
const labels = (doc: Document) => Array.from(doc.getElementsByTagName('text')).map(text => (text.textContent ?? '').replace(/\s+/g, ' ').trim()).filter(Boolean);
const styleText = (doc: Document) => Array.from(doc.getElementsByTagName('style')).map(style => style.textContent ?? '').join('');

// Top-level CSS blocks with balanced braces, so nested at-rule bodies stay inside their block.
function cssBlocks(css: string): { prelude: string; body: string }[] {
  const blocks: { prelude: string; body: string }[] = [];
  let depth = 0; let start = 0; let open = -1;
  for (let index = 0; index < css.length; index++) {
    if (css[index] === '{') { if (depth === 0) open = index; depth++; }
    else if (css[index] === '}') { depth--; if (depth === 0) { blocks.push({ prelude: css.slice(start, open).trim(), body: css.slice(open + 1, index) }); start = index + 1; } }
  }
  expect(depth, 'balanced CSS').toBe(0);
  return blocks;
}
const declarations = (body: string) => body.split(';').map(item => item.trim()).filter(Boolean);
const effect = /^-?(?:[a-z]+-)*(?:animation|transition|filter|box-shadow)(?:-[a-z-]+)?\s*:/i;
// The indexes, in `elements`, of what a selector list selects in `doc`. An unknown selector keeps its text as its
// meaning, so it can only match an unchanged copy of itself and must be kept.
function selectedIndexes(doc: Document, elements: Element[], prelude: string): string {
  const selected = new Set<number>();
  for (const selector of prelude.split(',')) {
    let found: Element[];
    try {
      found = Array.from(doc.querySelectorAll(selector));
    } catch {
      return `unparsed:${prelude}`;
    }
    for (const element of found) {
      const index = elements.indexOf(element);
      if (index >= 0) selected.add(index);
    }
  }
  return [...selected].sort((a, b) => a - b).join(',');
}

// The product never parses an approved string as HTML: the child moves the XML node it prepared (renderer.ts
// showDiagram; design-spec 8.3 격리/CSP). This helper is an adversarial check only. If anything ever re-reads an
// approved string with the HTML parser, it must still find only the same static SVG vocabulary.
const staticTags = new Set(['svg', 'g', 'defs', 'marker', 'path', 'rect', 'circle', 'ellipse', 'polygon', 'polyline', 'line', 'text', 'tspan', 'title', 'desc', 'style', 'clipPath', 'linearGradient', 'radialGradient', 'stop', 'use']);
function htmlDisplayProblems(svg: string): string[] {
  const host = document.createElement('div');
  host.innerHTML = svg;
  const problems: string[] = [];
  for (const element of Array.from(host.querySelectorAll('*'))) {
    if (element.namespaceURI !== SVG || !staticTags.has(element.localName)) problems.push(`element ${element.namespaceURI}|${element.localName}`);
    for (const attr of Array.from(element.attributes)) if (/^on/i.test(attr.name)) problems.push(`event ${element.localName}@${attr.name}`);
  }
  return problems;
}

function mutate(file: string, edit: (doc: Document, id: string) => void): string {
  const doc = parse(fixture(file));
  edit(doc, doc.documentElement.id);
  return serialize(doc);
}
function addElement(doc: Document, parent: Element, markup: string): void {
  const fragment = parse(`<svg xmlns="${SVG}" xmlns:xlink="http://www.w3.org/1999/xlink">${markup}</svg>`).documentElement;
  for (const node of Array.from(fragment.childNodes)) parent.append(doc.importNode(node, true));
}
const appendCss = (css: (id: string) => string) => (doc: Document, id: string) => { doc.querySelector('style')!.append(css(id)); };
const firstSymbol = (doc: Document) => doc.querySelector('symbol')!;
const PACKET = 'mermaid-sequence-packet.svg';
const CALL = 'mermaid-flowchart-call.svg';

describe('static adapter on real Mermaid 12 output of the three representative guides', () => {
  it.each(representatives)('$kind: keeps every drawn element, attribute, label and applied rule, and removes only decoration', ({ file, labels: expected }) => {
    const raw = fixture(file);
    const original = parse(raw);
    const result = staticDiagramSvg(raw);
    const output = parse(result);

    // Removed parts are decoration only: drop-shadow filters and library symbols nothing references.
    const filters = Array.from(original.getElementsByTagName('filter'));
    for (const filter of filters) expect(Array.from(filter.children).map(child => child.localName)).toEqual(['feDropShadow']);
    const references = (raw.match(/(?:href="#|url\(#)([\w-]+)/g) ?? []).map(ref => ref.replace(/^(?:href="#|url\(#)/, ''));
    for (const symbol of Array.from(original.getElementsByTagName('symbol'))) expect(references).not.toContain(symbol.id);
    const filterRefs = new Set(filters.map(filter => `url(#${filter.id})`));

    const drawn = Array.from(original.getElementsByTagName('*')).filter(element => !element.closest('filter, symbol'));
    const kept = Array.from(output.getElementsByTagName('*'));
    expect(kept.map(element => element.localName)).toEqual(drawn.map(element => element.localName));
    drawn.forEach((element, index) => {
      const want = attributes(element);
      if (want.filter && filterRefs.has(want.filter)) delete want.filter;
      expect(attributes(kept[index]!), `${element.localName} #${index}`).toEqual(want);
      if (element.localName !== 'style') expect(ownText(kept[index]!), `${element.localName} #${index} text`).toBe(ownText(element));
    });
    expect(labels(output)).toEqual(labels(original));
    // Wrapped rows join without a space, so compare label text without whitespace.
    const compact = labels(output).map(label => label.replace(/\s+/g, ''));
    for (const label of expected) expect(compact).toContain(label.replace(/\s+/g, ''));

    // Applied CSS keeps every declaration except visual effects; no rule or declaration is invented. A rule is compared
    // by its meaning — the drawn elements a selector engine finds plus its declarations — not by selector text, so the
    // approved svg-type normalization of raw `#id` selectors (msg_1e80f351e41a) is allowed while any change of target
    // fails. `drawn` and `kept` are the same elements in the same order (checked above), so indexes compare directly.
    const before = cssBlocks(styleText(original));
    const after = cssBlocks(styleText(output));
    const meaningIn = (doc: Document, elements: Element[]) => (block: { prelude: string; body: string }) => ({
      targets: selectedIndexes(doc, elements, block.prelude),
      declarations: declarations(block.body).filter(entry => !effect.test(entry)).join(';'),
    });
    const originalMeaning = meaningIn(original, drawn);
    const outputMeaning = meaningIn(output, kept);
    // The library font variable moves from :root onto the diagram root. A custom property draws nothing unless a
    // value reads it with var(), and no value in the output does.
    const onlyCustomProperties = (body: string) => declarations(body).every(entry => entry.startsWith('--'));
    expect(result).not.toContain('var(');
    for (const block of after) {
      expect(block.prelude.startsWith('@'), block.prelude).toBe(false);
      const now = outputMeaning(block);
      const source = before.some(item => {
        const was = originalMeaning(item);
        if (was.declarations !== now.declarations) return false;
        if (was.targets === now.targets) return true;
        return onlyCustomProperties(item.body) && now.targets === '0';
      });
      expect(source, `invented or altered rule ${block.prelude}{${block.body}}`).toBe(true);
    }
    for (const block of before) {
      if (block.prelude.startsWith('@')) continue;
      const was = originalMeaning(block);
      if (!was.targets || !was.declarations) continue;
      const keptWithSameMeaning = after.some(item => {
        const now = outputMeaning(item);
        return now.targets === was.targets && now.declarations === was.declarations;
      });
      expect(keptWithSameMeaning, `applied rule kept: ${block.prelude}`).toBe(true);
    }

    // Static: no animation, transition, filter or shadow anywhere, and the parent gate accepts the same string.
    expect(styleText(output)).not.toContain('@');
    for (const block of after) expect(declarations(block.body).filter(item => effect.test(item)), block.prelude).toEqual([]);
    expect(output.querySelectorAll('filter, feDropShadow, symbol, [filter], animate, set')).toHaveLength(0);
    for (const element of kept) expect(element.getAttribute('style') ?? '').not.toMatch(/animation|transition|filter|box-shadow/i);
    for (const marker of result.match(/marker-(?:start|end)="url\(#([\w-]+)\)"/g) ?? []) {
      expect(output.getElementById(marker.replace(/^.*#|\)"$/g, '')), marker).not.toBeNull();
    }
    expect(svgError(result)).toBeNull();
    expect(staticDiagramSvg(result)).toBe(result);
    expect(htmlDisplayProblems(result)).toEqual([]);
  });

  it('returns an already static minimal SVG unchanged and the gate accepts that exact string', () => {
    const minimal = `<svg xmlns="${SVG}" id="diagram-min" viewBox="0 0 100 40"><defs><marker id="diagram-min-arrow" viewBox="0 0 10 10"><path d="M0,0 L10,5 L0,10 z"/></marker></defs>` +
      `<style>svg#diagram-min .edge{stroke:#715333;fill:none}svg#diagram-min text{fill:#2a231b;font-size:16px}</style>` +
      `<path class="edge" d="M0,20 L90,20" marker-end="url(#diagram-min-arrow)"/><text x="4" y="16">시작 → 끝</text></svg>`;
    expect(staticDiagramSvg(minimal)).toBe(minimal);
    expect(svgError(minimal)).toBeNull();
    expect(htmlDisplayProblems(minimal)).toEqual([]);
  });
});

describe('static adapter refuses unknown or dangerous output instead of removing it into a success', () => {
  const cases: [string, string, (doc: Document, id: string) => void][] = [
    // Unknown at-rules and obfuscation, including near-misses of the library's own keyframes.
    ['@media', PACKET, appendCss(id => `@media screen{#${id} .actor{fill:#715333}}`)],
    ['@supports', PACKET, appendCss(id => `@supports (fill:red){#${id} .actor{fill:#715333}}`)],
    ['@import', PACKET, appendCss(() => '@import url(https://example.invalid/x.css);')],
    ['@font-face', PACKET, appendCss(() => '@font-face{font-family:x;src:local(x)}')],
    ['@namespace', PACKET, appendCss(() => '@namespace svg url(http://www.w3.org/2000/svg);')],
    ['@layer', PACKET, appendCss(id => `@layer base{#${id} text{fill:#715333}}`)],
    ['@property', PACKET, appendCss(() => "@property --x{syntax:'<color>';inherits:false;initial-value:red}")],
    ['unknown @keyframes', PACKET, appendCss(() => '@keyframes spin{to{transform:rotate(1turn)}}')],
    ['altered library keyframes body', PACKET, (doc, _id) => {
      const style = doc.querySelector('style')!; const block = cssBlocks(style.textContent!).find(item => item.prelude.startsWith('@keyframes'))!;
      style.textContent = style.textContent!.replace(`${block.prelude}{${block.body}}`, `${block.prelude}{${block.body.replace('0', '1')}}`);
    }],
    ['upper-case library keyframes', PACKET, doc => { const style = doc.querySelector('style')!; style.textContent = style.textContent!.replace('@keyframes', '@KEYFRAMES'); }],
    ['CSS escape', PACKET, appendCss(id => `#${id} .actor{fill:\\72 ed}`)],
    ['CSS comment', PACKET, appendCss(id => `#${id} .actor{fill:red/**/}`)],
    ['comment-split url', PACKET, appendCss(id => `#${id} .actor{fill:url(/**/https://example.invalid/x)}`)],
    ['CSS https url', PACKET, appendCss(id => `#${id} .actor{fill:url(https://example.invalid/p.svg#g)}`)],
    ['CSS data url', PACKET, appendCss(id => `#${id} .actor{fill:url(data:image/svg+xml,x)}`)],
    ['CSS javascript url', PACKET, appendCss(id => `#${id} .actor{fill:url(javascript:alert(1))}`)],
    ['CSS file url', PACKET, appendCss(id => `#${id} .actor{fill:url(file:///C:/x.svg#g)}`)],
    ['CSS protocol-relative url', PACKET, appendCss(id => `#${id} .actor{fill:url(//example.invalid/x)}`)],
    ['CSS quoted external url', PACKET, appendCss(id => `#${id} .actor{fill:url("https://example.invalid/x")}`)],
    // Danger inside parts the adapter would remove must be refused first.
    ['external url inside an unused neo gradient rule', PACKET, doc => {
      const style = doc.querySelector('style')!; const rule = cssBlocks(style.textContent!).find(item => item.body.includes('-gradient)'))!;
      style.textContent = style.textContent!.replace(`{${rule.body}}`, `{${rule.body}background:url(https://example.invalid/x);}`);
    }],
    ['@import next to the library keyframes', PACKET, doc => { const style = doc.querySelector('style')!; style.textContent = style.textContent!.replace('@keyframes', '@import url(https://example.invalid/x.css);@keyframes'); }],
    ['external url inside a library shadow declaration', PACKET, doc => { const style = doc.querySelector('style')!; style.textContent = style.textContent!.replace('drop-shadow(', 'drop-shadow(url(https://example.invalid/f) '); }],
    ['script inside an unused symbol', PACKET, doc => addElement(doc, firstSymbol(doc), '<script>parent.postMessage(1,"*")</script>')],
    ['foreignObject inside an unused symbol', PACKET, doc => addElement(doc, firstSymbol(doc), '<foreignObject width="1" height="1"/>')],
    ['event attribute inside an unused symbol', PACKET, doc => addElement(doc, firstSymbol(doc), '<rect width="1" height="1" onclick="alert(1)"/>')],
    ['external use inside an unused symbol', PACKET, doc => addElement(doc, firstSymbol(doc), '<use href="https://example.invalid/s.svg#x"/>')],
    ['@import style inside an unused symbol', PACKET, doc => addElement(doc, firstSymbol(doc), '<style>@import url(https://example.invalid/x.css);</style>')],
    ['image inside an unused symbol', PACKET, doc => addElement(doc, firstSymbol(doc), '<image href="#x"/>')],
    ['animation inside an unused symbol', PACKET, doc => addElement(doc, firstSymbol(doc), '<animate attributeName="opacity" to="0" dur="1s"/>')],
    ['inline animation inside an unused symbol', PACKET, doc => addElement(doc, firstSymbol(doc), '<rect width="1" height="1" style="animation:dash 1s infinite"/>')],
    ['nested symbol', PACKET, doc => addElement(doc, firstSymbol(doc), '<symbol id="nested-x"/>')],
    ['duplicate id inside an unused symbol', PACKET, (doc, id) => addElement(doc, firstSymbol(doc), `<rect id="${id}" width="1" height="1"/>`)],
    ['symbol referenced by use', PACKET, doc => addElement(doc, doc.documentElement, `<use href="#${firstSymbol(doc).id}"/>`)],
    ['symbol referenced by CSS url', PACKET, appendCss(id => `#${id} .actor{fill:url(#${id}-computer)}`)],
    ['symbol with an extra attribute', PACKET, doc => firstSymbol(doc).setAttribute('viewBox', '0 0 24 24')],
    ['filter with an extra feImage', PACKET, doc => addElement(doc, doc.querySelector('filter')!, '<feImage href="#x"/>')],
    ['filter primitive other than the library shadow', PACKET, doc => { const filter = doc.querySelector('filter')!; filter.replaceChildren(); addElement(doc, filter, '<feGaussianBlur stdDeviation="3"/>'); }],
    ['shadow primitive with an extra attribute', PACKET, doc => doc.querySelector('feDropShadow')!.setAttribute('result', 'x')],
    ['event attribute on the library filter', PACKET, doc => doc.querySelector('filter')!.setAttribute('onload', 'alert(1)')],
    ['filter with an unknown id', CALL, doc => { const filter = doc.querySelector('filter')!; filter.id = 'other-shadow'; }],
    ['library filter used from CSS', PACKET, appendCss(id => `#${id} .actor{filter:url(#${id}-drop-shadow)}`)],
    ['element inside style', PACKET, doc => addElement(doc, doc.querySelector('style')!, '<g/>')],
    ['used edge animation class', PACKET, doc => doc.querySelector('line')!.setAttribute('class', 'edge-animation-fast')],
    // Events, script, foreign content and namespaces.
    ['root onload', PACKET, doc => doc.documentElement.setAttribute('onload', 'alert(1)')],
    ['mixed-case event attribute', PACKET, doc => doc.querySelector('rect')!.setAttribute('OnMouseOver', 'alert(1)')],
    ['script element', PACKET, doc => addElement(doc, doc.documentElement, '<script>1</script>')],
    ['foreignObject', PACKET, doc => addElement(doc, doc.documentElement, '<foreignObject width="1" height="1"><div xmlns="http://www.w3.org/1999/xhtml">x</div></foreignObject>')],
    ['anchor', PACKET, doc => addElement(doc, doc.documentElement, '<a href="#x"><text>x</text></a>')],
    ['foreign-namespace element', PACKET, doc => doc.documentElement.append(doc.createElementNS('urn:example', 'x:widget'))],
    ['xml-events namespace declaration', PACKET, doc => doc.documentElement.setAttributeNS('http://www.w3.org/2000/xmlns/', 'xmlns:ev', 'http://www.w3.org/2001/xml-events')],
    ['xml:base', PACKET, doc => doc.documentElement.setAttributeNS('http://www.w3.org/XML/1998/namespace', 'xml:base', 'https://example.invalid/')],
    ['external use href', PACKET, doc => addElement(doc, doc.documentElement, '<use href="https://example.invalid/s.svg#x"/>')],
    ['external xlink:href', PACKET, doc => addElement(doc, doc.documentElement, '<use xlink:href="data:image/svg+xml,x#y"/>')],
    ['external url attribute', PACKET, doc => doc.querySelector('rect')!.setAttribute('fill', 'url(https://example.invalid/#p)')],
    ['javascript url in clip-path', PACKET, doc => doc.querySelector('rect')!.setAttribute('clip-path', 'url(javascript:alert(1))')],
    // Unsupported active or reference definitions.
    ['animate', PACKET, doc => addElement(doc, doc.documentElement, '<animate attributeName="opacity" to="0" dur="1s"/>')],
    ['set', PACKET, doc => addElement(doc, doc.documentElement, '<set attributeName="fill" to="red"/>')],
    ['animateTransform', PACKET, doc => addElement(doc, doc.documentElement, '<animateTransform attributeName="transform" type="rotate" to="90"/>')],
    ['animateMotion', PACKET, doc => addElement(doc, doc.documentElement, '<animateMotion path="M0,0 L1,1"/>')],
    ['pattern definition', PACKET, doc => addElement(doc, doc.documentElement, '<defs><pattern id="unused-pattern" width="1" height="1"/></defs>')],
    ['mask definition', PACKET, doc => addElement(doc, doc.documentElement, '<defs><mask id="unused-mask"/></defs>')],
    ['textPath', PACKET, doc => addElement(doc, doc.documentElement, '<text><textPath href="#x">x</textPath></text>')],
    ['switch', PACKET, doc => addElement(doc, doc.documentElement, '<switch><rect width="1" height="1"/></switch>')],
    // Inline effects (unknown inline filter, animation and transition) on drawn elements.
    ['inline filter function', PACKET, doc => doc.querySelector('rect')!.setAttribute('style', 'filter:blur(2px)')],
    ['inline animation', PACKET, doc => doc.querySelector('rect')!.setAttribute('style', 'animation:dash 1s infinite')],
    ['inline animation-name', PACKET, doc => doc.querySelector('rect')!.setAttribute('style', 'animation-name:dash')],
    ['inline transition', PACKET, doc => doc.querySelector('rect')!.setAttribute('style', 'transition:fill 1s')],
    ['inline transition-property', PACKET, doc => doc.querySelector('rect')!.setAttribute('style', 'transition-property:fill')],
    ['inline vendor animation', PACKET, doc => doc.querySelector('rect')!.setAttribute('style', '-webkit-animation:dash 1s')],
    ['inline box-shadow', PACKET, doc => doc.querySelector('rect')!.setAttribute('style', 'box-shadow:0 0 2px red')],
    ['inline effect on the root', PACKET, doc => doc.documentElement.setAttribute('style', 'max-width:100px;filter:blur(1px)')],
    ['stylesheet transition', PACKET, appendCss(id => `#${id} .actor{transition:fill 1s}`)],
    ['stylesheet filter function', PACKET, appendCss(id => `#${id} .actor{filter:blur(1px)}`)],
    ['stylesheet animation-name', PACKET, appendCss(id => `#${id} .actor{animation-name:dash}`)],
    ['stylesheet vendor filter', PACKET, appendCss(id => `#${id} .actor{-webkit-filter:blur(1px)}`)],
  ];

  it.each(cases)('%s', (_name, file, edit) => {
    const input = mutate(file, edit);
    expect(input).not.toBe(fixture(file));
    expect(childOutcome(input)).toBe('refused');
  });

  it.each([
    ['DOCTYPE', (svg: string) => `<!DOCTYPE svg>${svg}`],
    ['internal entity', (svg: string) => `<!DOCTYPE svg [<!ENTITY x "y">]>${svg}`],
    ['processing instruction', (svg: string) => `<?xml-stylesheet href="https://example.invalid/x.css"?>${svg}`],
    ['XML declaration', (svg: string) => `<?xml version="1.0"?>${svg}`],
    ['malformed', (svg: string) => svg.replace('</svg>', '<g></svg>')],
    ['wrong root', () => '<html xmlns="http://www.w3.org/1999/xhtml"><body/></html>'],
    ['wrong namespace', () => '<svg xmlns="http://example.invalid/ns"><rect/></svg>'],
    ['blank', () => '  '],
    ['over 256 KiB in UTF-8 bytes', (svg: string) => svg.replace('</svg>', `<desc>${'가'.repeat(Math.ceil((256 * 1024) / 3))}</desc></svg>`)],
  ])('%s', (_name, make) => {
    const input = make(fixture(PACKET));
    expect(input).not.toBe(fixture(PACKET));
    expect(childOutcome(input)).toBe('refused');
  });
});

describe('neo paint rules whose gradient definition is missing', () => {
  const neo = (markup: string) => (doc: Document) => addElement(doc, doc.documentElement, markup);
  it.each([
    ['a node descendant of the root', neo('<g data-look="neo" class="node"><rect width="1" height="1"/></g>')],
    ['a deeper descendant', neo('<g><g data-look="neo" class="cluster"><rect width="1" height="1"/></g></g>')],
    ['a compound icon selector', neo('<g data-look="neo" class="icon-shape"><path class="icon" d="M0,0"/></g>')],
    ['an inline style reference', (doc: Document, id: string) => doc.querySelector('rect')!.setAttribute('style', `stroke:url(#${id}-gradient)`)],
    ['a paint attribute reference', (doc: Document, id: string) => doc.querySelector('rect')!.setAttribute('fill', `url(#${id}-gradient)`)],
    ['a non-neo stylesheet rule', appendCss(id => `#${id} .actor{fill:url(#${id}-gradient)}`)],
  ])('refuses the output when the missing gradient is used by %s', (_name, edit) => {
    const input = mutate(PACKET, edit);
    expect(input).not.toBe(fixture(PACKET));
    expect(childOutcome(input)).toBe('refused');
  });

  it('drops only unused neo rules: a root that is itself marked neo is not its own descendant', () => {
    const svg = mutate(PACKET, doc => { doc.documentElement.setAttribute('data-look', 'neo'); doc.documentElement.setAttribute('class', 'node'); });
    const result = staticDiagramSvg(svg);
    expect(result).not.toMatch(/-gradient\)/);
    expect(svgError(result)).toBeNull();
    expect(parse(result).documentElement.getAttribute('data-look')).toBe('neo');
  });

  it('keeps neo rules untouched when the gradient exists', () => {
    const raw = fixture(CALL);
    expect(raw).toMatch(/linearGradient/);
    const id = parse(raw).documentElement.id;
    expect(staticDiagramSvg(raw)).toContain(`stroke:url(#${id}-gradient)`);
  });
});

describe('parent gate inspection copy keeps the original XML meaning', () => {
  const svg = (body: string, rootAttrs = '') => `<svg xmlns="${SVG}" xmlns:xlink="http://www.w3.org/1999/xlink" id="d"${rootAttrs}>${body}</svg>`;
  it.each([
    ['single-quoted style attribute', svg(`<rect width="1" height="1" style='fill:#715333'/>`)],
    ['style attribute with spaces around =', svg('<rect width="1" height="1" style = "fill:#715333"/>')],
    ['style attribute first and last', svg('<rect style="fill:#715333" width="1" height="1"/><rect width="1" height="1" style="stroke:#715333"/>')],
    ['style-like text in another attribute value', svg(`<text data-label="a > style='fill:red'" style="fill:#2a231b">style="x"</text>`)],
    ['attributes named like style', svg('<rect data-style="fill:red" xstyle="a" style-x="b" width="1" height="1"/>')],
    ['style element with CDATA', svg('<style><![CDATA[svg#d rect{fill:#715333}]]></style><rect width="1" height="1"/>')],
    ['style element next to a comment', svg('<style>svg#d rect{fill:#715333}<!-- note --></style><rect width="1" height="1"/>')],
    ['two style elements', svg('<style>svg#d rect{fill:#715333}</style><style>svg#d text{fill:#2a231b}</style><rect width="1" height="1"/>')],
    ['self-closing style element', svg('<style/><rect width="1" height="1"/>')],
    ['style element with a newline before >', svg('<style\n>svg#d rect{fill:#715333}</style\n><rect width="1" height="1"/>')],
    ['internal url in style attribute and target', svg('<defs><linearGradient id="g"><stop offset="0"/></linearGradient></defs><rect width="1" height="1" style="fill:url(#g)"/>')],
  ])('accepts %s', (_name, input) => {
    expect(svgError(input)).toBeNull();
  });

  it.each([
    ['reserved inspection name as element', svg('<dh-inspection-style>svg#d rect{fill:red}</dh-inspection-style>')],
    ['reserved inspection name as attribute', svg('<rect dh-inspection-style="fill:url(https://example.invalid/x)" width="1" height="1"/>')],
    ['reserved inspection name hiding an import', svg('<dh-inspection-style>@import url(https://example.invalid/x.css);</dh-inspection-style>')],
    ['prefixed style element in SVG namespace with an import', `<svg xmlns="${SVG}" xmlns:xlink="${SVG}" id="d"><xlink:style>@import url(https://example.invalid/x.css);</xlink:style></svg>`],
    ['prefixed style through another declared prefix', `<svg xmlns="${SVG}" xmlns:s="${SVG}" id="d"><s:style>svg#d rect{fill:red}</s:style></svg>`],
    ['upper-case STYLE element', svg('<STYLE>svg#d rect{fill:red}</STYLE>')],
    ['upper-case STYLE attribute with an external url', svg('<rect width="1" height="1" STYLE="fill:url(https://example.invalid/x)"/>')],
    ['single-quoted style hiding an external url', svg(`<rect width="1" height="1" style='fill:url("https://example.invalid/x")'/>`)],
    ['style value quoted with the other quote around a fake attribute', svg(`<rect width="1" height="1" data-a='x" style="fill:#715333' style="fill:url(https://example.invalid/x)"/>`)],
    ['decimal entity hiding @import', svg('<style>&#64;import url(https://example.invalid/x.css);</style>')],
    ['hex entity hiding a scheme', svg('<rect width="1" height="1" style="fill:url(&#x68;ttps://example.invalid/x)"/>')],
    ['CDATA hiding @import', svg('<style><![CDATA[@import url(https://example.invalid/x.css);]]></style>')],
    ['duplicate style attribute', svg('<rect width="1" height="1" style="fill:red" style="fill:blue"/>')],
    ['undeclared HTML entity', svg('<text>&nbsp;</text>')],
    ['mismatched style end tag', svg('<style>svg#d rect{fill:red}</x:style>')],
  ])('rejects %s', (_name, input) => {
    expect(svgError(input)).not.toBeNull();
  });

  it('does not mutate the string it approves', () => {
    const input = svg(`<style>svg#d rect{fill:#715333}</style><rect width="1" height="1" style='stroke:#715333'/>`);
    const copy = `${input}`;
    expect(svgError(input)).toBeNull();
    expect(input).toBe(copy);
  });
});

// ASSET-07: the gate validates the XML meaning and the child displays the prepared XML node, not an HTML parse. These
// cases still re-read approved strings with the HTML parser, where SVG title/desc are HTML integration points and
// markup that XML reads as text could become HTML elements, so an approved string stays safe even outside that path.
describe('approved strings stay static SVG even when re-read by an HTML parser (ASSET-07)', () => {
  const svg = (body: string) => `<svg xmlns="${SVG}" id="d" viewBox="0 0 10 10">${body}<rect width="1" height="1"/></svg>`;
  it.each([
    ['comment inside nested title', svg('<title><title><!--</title><img src="x" onerror="alert(1)">--></title></title>')],
    ['CDATA inside style inside title', svg('<title><style><![CDATA[</style><img src=x onerror=alert(1)>]]></style></title>')],
    ['comment inside style inside desc', svg('<desc><style><!--</style><img src=x onerror=alert(1)>--></style></desc>')],
  ])('%s is refused by the parent gate or displays only static SVG', (_name, input) => {
    const approved = svgError(input) === null;
    expect(approved ? htmlDisplayProblems(input) : []).toEqual([]);
    let child: string | null = null;
    try { child = staticDiagramSvg(input); } catch { child = null; }
    expect(child === null ? [] : htmlDisplayProblems(child)).toEqual([]);
  });
});

// ASSET-08: unknown visual effects must fail (design-spec 2.8, the final inline-effect hardening). XML keeps attribute
// case and treats `filter` as a plain attribute, while an HTML parse lower-cases names and applies presentation
// attributes. The product displays the XML node, so displayedEffects() re-reads the string with the HTML parser as an
// adversarial check that no spelling the adapter does not inspect could carry an effect into a displayed SVG.
describe('effects that reach the displayed SVG are refused (ASSET-08)', () => {
  function displayedEffects(svg: string): string[] {
    const host = document.createElement('div');
    host.innerHTML = svg;
    return Array.from(host.querySelectorAll('*')).flatMap(element => [
      ...(/animation|transition|filter|box-shadow/i.test(element.getAttribute('style') ?? '') ? [`${element.localName} style=${element.getAttribute('style')}`] : []),
      ...(element.hasAttribute('filter') && !/^url\(#[\w-]+\)$|^none$/.test(element.getAttribute('filter')!) ? [`${element.localName} filter=${element.getAttribute('filter')}`] : []),
    ]);
  }
  it.each([
    ['upper-case STYLE attribute with a filter function', (doc: Document) => doc.querySelector('rect')!.setAttribute('STYLE', 'filter:blur(2px)')],
    ['mixed-case Style attribute with an animation', (doc: Document) => doc.querySelector('rect')!.setAttribute('Style', 'animation:dash 1s infinite')],
    ['filter presentation attribute with a filter function', (doc: Document) => doc.querySelector('rect')!.setAttribute('filter', 'blur(2px)')],
  ])('%s', (_name, edit) => {
    const input = mutate(PACKET, edit);
    let child: string | null = null;
    try { child = staticDiagramSvg(input); } catch { child = null; }
    expect(child === null ? [] : displayedEffects(child)).toEqual([]);
  });
});

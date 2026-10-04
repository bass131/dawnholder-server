// Independent tests for the static-SVG contract that both gates apply: the parent's `svgError` on the string it
// approves, and the child's `prepareStaticDiagram` that keeps the XML node it later displays (R-14; design-spec 2.8
// "장식·애니메이션은 쓰지 않는다", 8.3 "외부 참조·이벤트·foreignObject를 성공 SVG로 허용하지 않는다"; goal
// "다음 보안 수리 설계": 설명 요소는 텍스트만, 모르는 표기를 조용히 버려 성공시키지 않는다).
//
// Expectations come from what an element, attribute or rule would draw, load or run, never from the product's allow
// tables. Each refusal is paired with an accepted control built from the same base, so a gate that refuses everything
// fails here. Inputs are built and pre-checked (well-formed or deliberately not) outside the assertion that judges them.
/// <reference types="vite/client" />
import { describe, expect, it } from 'vitest';
import { prepareStaticDiagram, staticDiagramSvg } from '../src/diagrams/static-svg';
import { svgError } from '../src/diagrams/svg-check';
import callSvg from './diagram-static-svg/mermaid-flowchart-call.svg?raw';
import packetSvg from './diagram-static-svg/mermaid-sequence-packet.svg?raw';
import stateSvg from './diagram-static-svg/mermaid-state-window.svg?raw';

const SVG = 'http://www.w3.org/2000/svg';
const XLINK = 'http://www.w3.org/1999/xlink';
const XHTML = 'http://www.w3.org/1999/xhtml';
const MAX_SVG_BYTES = 256 * 1024; // design-spec 8.3 "최대 256 KiB"

type Gate = 'accepted' | 'refused' | 'crashed';
type Verdict = { parent: Gate; child: Gate };

// A refusal is the product's own Error. A TypeError/RangeError would be a crash that only looks like a refusal.
function childGate(svg: string): Gate {
  try {
    prepareStaticDiagram(svg);
    return 'accepted';
  } catch (error) {
    return error instanceof TypeError || error instanceof RangeError ? 'crashed' : 'refused';
  }
}
function verdict(svg: string): Verdict {
  return { parent: svgError(svg) === null ? 'accepted' : 'refused', child: childGate(svg) };
}
const both = (gate: Gate): Verdict => ({ parent: gate, child: gate });

const parse = (svg: string) => new DOMParser().parseFromString(svg, 'image/svg+xml');
const wellFormed = (svg: string) => parse(svg).querySelector('parsererror') === null;
const byteLength = (value: string) => new TextEncoder().encode(value).byteLength;

// A small static diagram that uses every reference kind the contract allows: marker, paint server, clip path, use.
function base(body = '', rootAttributes = ''): string {
  return `<svg xmlns="${SVG}" xmlns:xlink="${XLINK}" id="d" viewBox="0 0 120 40"${rootAttributes}>` +
    '<defs>' +
    '<marker id="d-arrow" viewBox="0 0 10 10" refX="5" refY="5" markerWidth="6" markerHeight="6" orient="auto"><path d="M0,0 L10,5 L0,10 z"/></marker>' +
    '<linearGradient id="d-paint"><stop offset="0" stop-color="#f3e9d2"/><stop offset="1" stop-color="#715333"/></linearGradient>' +
    '<clipPath id="d-clip"><rect width="120" height="40"/></clipPath>' +
    '</defs>' +
    '<style>svg#d .edge{stroke:#715333;fill:none}</style>' +
    '<g id="d-node" class="node"><rect class="box" x="1" y="1" width="40" height="20" fill="#f3e9d2"/><text x="4" y="16">시작</text></g>' +
    '<path class="edge" d="M41,11 L80,11" marker-end="url(#d-arrow)"/>' +
    `${body}</svg>`;
}
const control = base();

// The fixtures are the unmodified Mermaid 12 output already used by diagram-static-svg.test.ts.
const fixtures = [
  { kind: 'call flowchart', svg: callSvg },
  { kind: 'packet sequence', svg: packetSvg },
  { kind: 'window state', svg: stateSvg },
];

describe('test inputs are what the cases claim', () => {
  it('loads the three real Mermaid fixtures as SVG documents with a diagram id', () => {
    for (const { kind, svg } of fixtures) {
      expect(svg.length, kind).toBeGreaterThan(1000);
      const doc = parse(svg);
      expect(doc.querySelector('parsererror'), kind).toBeNull();
      expect(doc.documentElement.namespaceURI, kind).toBe(SVG);
      expect(doc.documentElement.id, kind).toMatch(/^diagram-[\w-]+$/);
    }
  });

  it('accepts the shared base in both gates, so refusals below come from the change alone', () => {
    expect(wellFormed(control)).toBe(true);
    expect(verdict(control)).toEqual(both('accepted'));
  });
});

// Each body is appended to the accepted base. The base must still be accepted, the variant must differ from it.
function refusedVariants(cases: [string, string][]) {
  it.each(cases)('%s', (_name, body) => {
    const input = base(body);
    expect(input).not.toBe(control);
    expect(verdict(control)).toEqual(both('accepted'));
    expect(verdict(input)).toEqual(both('refused'));
  });
}

describe('description elements are text only, and HTML integration points never become markup', () => {
  it('accepts plain, escaped, CDATA and comment text in title and desc and keeps it as text in the kept node', () => {
    const body = '<title>패킷 흐름</title><desc>요청 &lt;b&gt;굵게&lt;/b&gt; 뒤 응답</desc>' +
      '<g><title><![CDATA[a>b<img src=x onerror=alert(1)>]]><!-- <img src=x onerror=alert(2)> --></title></g>';
    const input = base(body);
    expect(wellFormed(input)).toBe(true);
    expect(verdict(input)).toEqual(both('accepted'));
    const kept = prepareStaticDiagram(input).root;
    const titles = Array.from(kept.getElementsByTagNameNS(SVG, 'title'));
    expect(titles).toHaveLength(2);
    for (const title of titles) expect(title.children).toHaveLength(0);
    expect(titles[1]!.textContent).toBe('a>b<img src=x onerror=alert(1)>');
    expect(kept.getElementsByTagName('img')).toHaveLength(0);
  });

  refusedVariants([
    ['SVG element inside title', '<title>a<tspan>b</tspan></title>'],
    ['HTML element inside title', `<title><b xmlns="${XHTML}">굵게</b></title>`],
    ['HTML image with a handler inside desc', `<desc><img xmlns="${XHTML}" src="x" onerror="alert(1)"/></desc>`],
    ['title nested in title', '<title>a<title>b</title></title>'],
    ['style inside desc', '<desc><style>svg#d .box{fill:#715333}</style></desc>'],
    ['foreignObject inside desc', '<desc><foreignObject width="1" height="1"/></desc>'],
    ['MathML inside desc', '<desc><math xmlns="http://www.w3.org/1998/Math/MathML"><mi>x</mi></math></desc>'],
    ['foreignObject under the root', '<foreignObject width="10" height="10"/>'],
    ['foreignObject with only text', '<foreignObject width="10" height="10">x</foreignObject>'],
    ['foreignObject in a group', '<g><foreignObject width="10" height="10"/></g>'],
    ['foreignObject in a marker', '<defs><marker id="d-m2"><foreignObject width="1" height="1"/></marker></defs>'],
    ['foreignObject in a clip path', '<defs><clipPath id="d-c2"><foreignObject width="1" height="1"/></clipPath></defs>'],
    ['foreignObject in text', '<text x="1" y="1"><foreignObject width="1" height="1"/></text>'],
  ]);
});

describe('element names are exact: case, prefix and namespace variants are refused', () => {
  refusedVariants([
    ['Title', '<Title>x</Title>'],
    ['TITLE', '<TITLE>x</TITLE>'],
    ['Desc', '<Desc>x</Desc>'],
    ['foreignobject', '<foreignobject width="1" height="1"/>'],
    ['FOREIGNOBJECT', '<FOREIGNOBJECT width="1" height="1"/>'],
    ['Style element', '<Style>svg#d .box{fill:red}</Style>'],
    ['sCript', '<sCript>parent.postMessage(1,"*")</sCript>'],
    ['ANIMATE', '<ANIMATE attributeName="opacity" to="0" dur="1s"/>'],
    ['Rect', '<Rect width="1" height="1"/>'],
    ['prefixed title in the SVG namespace', `<s:title xmlns:s="${SVG}">x</s:title>`],
    ['prefixed rect in the SVG namespace', `<s:rect xmlns:s="${SVG}" width="1" height="1"/>`],
    ['group whose default namespace is XHTML', `<g xmlns="${XHTML}"><rect width="1" height="1"/></g>`],
    ['style element in the XHTML namespace', `<style xmlns="${XHTML}">svg#d .box{fill:red}</style>`],
    ['title in the XHTML namespace', `<title xmlns="${XHTML}">x</title>`],
    ['group in no namespace', '<g xmlns=""><rect width="1" height="1"/></g>'],
    ['rect in an unknown namespace', '<rect xmlns="urn:example" width="1" height="1"/>'],
  ]);

  it.each([
    ['upper-case root', control.replace('<svg ', '<SVG ').replace(/<\/svg>$/, '</SVG>')],
    ['prefixed root', control.replace(`<svg xmlns="${SVG}"`, `<svg:svg xmlns:svg="${SVG}"`).replace(/<\/svg>$/, '</svg:svg>')],
    ['root in the XHTML namespace', control.replace(`xmlns="${SVG}"`, `xmlns="${XHTML}"`)],
    ['root in no namespace', control.replace(` xmlns="${SVG}"`, '')],
  ])('%s', (_name, input) => {
    expect(input).not.toBe(control);
    expect(wellFormed(input)).toBe(true);
    expect(verdict(input)).toEqual(both('refused'));
  });
});

describe('CDATA, comments and character references are judged by their XML meaning', () => {
  it.each([
    ['a stylesheet split by CDATA and a comment', '<style><![CDATA[svg#d .box{fill:]]><!-- note -->#715333}</style>'],
    ['an internal reference written with a character reference', '<rect width="1" height="1" fill="url(&#35;d-paint)"/>'],
  ])('accepts %s', (_name, body) => {
    const input = base(body);
    expect(wellFormed(input)).toBe(true);
    expect(verdict(input)).toEqual(both('accepted'));
  });

  refusedVariants([
    ['effect property name split by a comment', '<style>svg#d .box{fi<!-- -->lter:blur(1px)}</style>'],
    ['at-rule split by an empty CDATA section', '<style>@im<![CDATA[]]>port url(https://example.invalid/x.css);</style>'],
    ['effect property written with a character reference in a style attribute', '<rect width="1" height="1" style="&#102;ilter:blur(1px)"/>'],
    ['effect property written with a character reference in a stylesheet', '<style>svg#d .box{&#x66;ilter:blur(1px)}</style>'],
    ['external scheme written with a character reference in use href', '<use href="&#104;ttps://example.invalid/s.svg#x"/>'],
    ['javascript scheme written with character references in use href', '<use href="&#x6A;avascript:alert(1)"/>'],
    ['external paint written with a character reference', '<rect width="1" height="1" fill="url(&#104;ttps://example.invalid/p)"/>'],
    ['reserved inspection name as text', '<text x="1" y="1">dh-inspection-style</text>'],
    ['reserved inspection name as a class', '<rect class="dh-inspection-style" width="1" height="1"/>'],
    ['reserved inspection name in a comment', '<!-- dh-inspection-style -->'],
  ]);

  it.each([
    ['unquoted attribute', '<rect width=1 height="1"/>'],
    ['duplicate attribute', '<rect width="1" width="2" height="1"/>'],
    ['undeclared prefix', '<x:rect width="1" height="1"/>'],
    ['mismatched end tag', '<g><rect width="1" height="1"/></x>'],
    ['unclosed CDATA in a stylesheet', '<style><![CDATA[svg#d .box{fill:#715333}</style>'],
    ['less-than sign in an attribute value', '<rect data-label="a<b" width="1" height="1"/>'],
    ['undefined entity', '<text x="1" y="1">&unknown;</text>'],
    ['unclosed comment', '<!-- open'],
  ])('refuses malformed XML: %s', (_name, body) => {
    const input = base(body);
    expect(wellFormed(input)).toBe(false);
    expect(verdict(input)).toEqual(both('refused'));
  });

  it.each([
    ['lower-case doctype', `<!doctype svg>${control}`],
    ['processing instruction between elements', control.replace('<defs>', '<?xml-stylesheet href="https://example.invalid/x.css"?><defs>')],
    ['second root element', `${control}<svg xmlns="${SVG}"/>`],
    ['text after the root', `${control}tail`],
  ])('refuses %s', (_name, input) => {
    expect(input).not.toBe(control);
    expect(verdict(input)).toEqual(both('refused'));
  });
});

describe('attribute names are exact and attribute order does not change the verdict', () => {
  it('accepts the same attributes in either order and keeps an equal node', () => {
    const forward = base('<rect class="box" style="fill:#715333" width="1" height="1" fill="url(#d-paint)" clip-path="url(#d-clip)"/>');
    const reverse = base('<rect clip-path="url(#d-clip)" fill="url(#d-paint)" height="1" width="1" style="fill:#715333" class="box"/>');
    expect(verdict(forward)).toEqual(both('accepted'));
    expect(verdict(reverse)).toEqual(both('accepted'));
    const forwardRect = prepareStaticDiagram(forward).root.lastElementChild!;
    const reverseRect = prepareStaticDiagram(reverse).root.lastElementChild!;
    expect(forwardRect.localName).toBe('rect');
    expect(forwardRect.isEqualNode(reverseRect)).toBe(true);
  });

  it('accepts internal use references through href and xlink:href', () => {
    const input = base('<use href="#d-node" x="50"/><use xlink:href="#d-node" y="20"/>');
    expect(verdict(input)).toEqual(both('accepted'));
  });

  refusedVariants([
    ['handler before the geometry', '<rect onclick="alert(1)" width="1" height="1"/>'],
    ['handler after the geometry', '<rect width="1" height="1" onclick="alert(1)"/>'],
    ['upper-case handler', '<rect width="1" height="1" ONCLICK="alert(1)"/>'],
    ['handler on a group', '<g onfocusin="alert(1)"><rect width="1" height="1"/></g>'],
    ['upper-case FILL', '<rect width="1" height="1" FILL="#715333"/>'],
    ['mixed-case Stroke', '<rect width="1" height="1" Stroke="#715333"/>'],
    ['upper-case D on a path', '<path D="M0,0 L1,1"/>'],
    ['upper-case TRANSFORM', '<g TRANSFORM="translate(1,1)"><rect width="1" height="1"/></g>'],
    ['upper-case HREF on use', '<use HREF="#d-node"/>'],
    ['xlink:href on a non-use element', '<rect xlink:href="https://example.invalid/x" width="1" height="1"/>'],
    ['XML Events attribute', '<rect xmlns:ev="http://www.w3.org/2001/xml-events" ev:event="click" width="1" height="1"/>'],
    ['src attribute', '<rect src="https://example.invalid/x" width="1" height="1"/>'],
  ]);

  it.each([
    ['ViewBox', ' ViewBox="0 0 1 1"'],
    ['onload', ' onload="alert(1)"'],
    ['OnLoad', ' OnLoad="alert(1)"'],
  ])('refuses the root attribute %s', (_name, attribute) => {
    const input = base('', attribute);
    expect(wellFormed(input)).toBe(true);
    expect(verdict(input)).toEqual(both('refused'));
  });

  // Harmless names outside the static vocabulary may be refused or kept, but never removed into a success.
  it.each([
    ['tabindex', '0'],
    ['systemLanguage', 'en'],
    ['requiredExtensions', 'urn:x'],
    ['data-unknown', '1'],
    ['mask', 'url(#d-clip)'],
  ])('does not silently drop the unknown attribute %s', (name, value) => {
    const input = base(`<rect id="d-probe" width="1" height="1" ${name}="${value}"/>`);
    expect(wellFormed(input)).toBe(true);
    const outcome = verdict(input);
    if (outcome.child === 'accepted') {
      expect(prepareStaticDiagram(input).root.querySelector('#d-probe')!.getAttribute(name)).toBe(value);
    } else {
      expect(outcome).toEqual(both('refused'));
    }
  });
});

describe('presentation attributes, style attributes and stylesheet rules follow one static-value rule', () => {
  it('accepts static presentation values and internal references of every allowed kind', () => {
    const input = base('<rect width="10" height="10" fill="url(#d-paint)" stroke="#715333" stroke-width="2" opacity="0.5" clip-path="url(#d-clip)"/>' +
      '<path d="M0,0 L10,0" stroke="currentColor" marker-start="url(#d-arrow)" marker-end="url(#d-arrow)"/>' +
      '<text x="1" y="1" style="font-size:16px;fill:#2a231b" text-anchor="middle">끝</text>');
    expect(verdict(input)).toEqual(both('accepted'));
  });

  refusedVariants([
    // Effects through every spelling (the class of the original ASSET-08 counterexamples).
    ['filter presentation attribute to an internal id', '<rect width="1" height="1" filter="url(#d-clip)"/>'],
    ['upper-case FILTER presentation attribute', '<rect width="1" height="1" FILTER="blur(2px)"/>'],
    ['upper-case property name in a style attribute', '<rect width="1" height="1" style="FILTER:blur(2px)"/>'],
    ['mixed-case animation property in a style attribute', '<rect width="1" height="1" style="Animation-Name:dash"/>'],
    ['vendor filter in a style attribute', '<rect width="1" height="1" style="-webkit-filter:blur(1px)"/>'],
    ['transition longhand in a style attribute', '<rect width="1" height="1" style="transition-duration:1s"/>'],
    ['motion path in a style attribute', `<rect width="1" height="1" style="offset-path:path('M0 0 L9 9')"/>`],
    ['backdrop filter in a style attribute', '<rect width="1" height="1" style="backdrop-filter:blur(1px)"/>'],
    ['upper-case effect in a stylesheet', '<style>svg#d .box{FILTER:blur(2px)}</style>'],
    ['mixed-case animation shorthand in a stylesheet', '<style>svg#d .box{Animation:dash 1s infinite}</style>'],
    ['library keyframes with changed spacing', '<style>@keyframes  dash{to{stroke-dashoffset:0;}}</style>'],
    // Loads through any presentation or style value.
    ['data paint', '<rect width="1" height="1" fill="url(data:image/svg+xml,x)"/>'],
    ['file stroke', '<rect width="1" height="1" stroke="url(file:///C:/x.svg#p)"/>'],
    ['external marker', '<path d="M0,0 L1,1" marker-end="url(https://example.invalid/m.svg#a)"/>'],
    ['protocol-relative marker', '<path d="M0,0 L1,1" marker-start="url(//example.invalid/m#a)"/>'],
    ['cursor with an external image', '<rect width="1" height="1" cursor="url(https://example.invalid/c.cur), auto"/>'],
    ['background image in a style attribute', '<rect width="1" height="1" style="background-image:url(#d-paint)"/>'],
    ['custom property carrying a value', '<rect width="1" height="1" style="--x:#715333;fill:var(--x)"/>'],
    ['generated content in a stylesheet', `<style>svg#d .box{content:'x'}</style>`],
    // Internal references must name an existing definition.
    ['paint reference to a missing id', '<rect width="1" height="1" fill="url(#d-missing)"/>'],
    ['marker reference to a missing id', '<path d="M0,0 L1,1" marker-end="url(#d-missing)"/>'],
    ['use reference to a missing id', '<use href="#d-missing"/>'],
    ['label reference to a missing id', '<g aria-labelledby="d-missing"><rect width="1" height="1"/></g>'],
  ]);

  // Static selectors only; an unsupported selector may not be deleted to turn the output into a success.
  it.each([
    ['hover', 'svg#d .box:hover{fill:#715333}'],
    ['pseudo-element', 'svg#d .box::before{fill:#715333}'],
    ['universal', 'svg#d *{fill:#715333}'],
    ['attribute presence', 'svg#d rect[class]{fill:#715333}'],
    ['negation', 'svg#d rect:not(.edge){fill:#715333}'],
  ])('does not silently delete a rule with a %s selector', (_name, rule) => {
    const input = base(`<style>${rule}</style>`);
    expect(wellFormed(input)).toBe(true);
    const outcome = verdict(input);
    if (outcome.child === 'accepted') {
      expect(prepareStaticDiagram(input).svg).toContain(rule);
      expect(outcome.parent).toBe('accepted');
    } else {
      expect(outcome).toEqual(both('refused'));
    }
  });
});

// The child document holds the diagram next to its own return button (design-spec 2.3 "도식의 문서로 돌아가기").
// SVG <style> rules apply to the whole child document, so an approved stylesheet must stay inside the diagram.
// jsdom does not apply SVG stylesheets, so the visible effect is checked in real Chromium; this pins the gate input.
describe('approved stylesheets cannot restyle the child document around the diagram', () => {
  // The approved design (msg_1e80f351e41a) keeps the library's :root font variable only as raw child input and moves it
  // onto the diagram root; the final string both gates judge never addresses the HTML root.
  const fontVariable = '--mermaid-font-family:"trebuchet ms",verdana,arial,sans-serif;';
  it('accepts the raw library font variable in the child and moves it onto the diagram root', () => {
    const raw = base(`<style>#d :root{${fontVariable}}:root{${fontVariable}}#d .box{fill:#715333}</style>`);
    expect(wellFormed(raw)).toBe(true);
    expect(verdict(raw)).toEqual({ parent: 'refused', child: 'accepted' });
    const output = prepareStaticDiagram(raw).svg;
    expect(output).not.toContain(':root');
    expect(output).toContain(`svg#d{${fontVariable}}`);
    expect(output).toContain('svg#d .box{fill:#715333}');
    expect(svgError(output)).toBeNull();
  });

  refusedVariants([
    ['a type selector for the return button', '<style>button{display:none}</style>'],
    ['the return button id', '<style>#return-to-document{visibility:hidden}</style>'],
    ['the display container id', '<style>#display{display:none}</style>'],
    ['the document root hidden', '<style>:root{visibility:hidden}</style>'],
    ['the body', '<style>body{display:none}</style>'],
  ]);
});

// Scope is judged by selector engines, not by the product grammar. Each rule of a string is parsed by a separate CSS
// parser (an HTML <style> sheet) and run with querySelectorAll on a document laid out like diagram-renderer.html
// (staging, display, return button) that also holds HTML elements colliding with the diagram's id and classes.
// The return button sits next to the diagram here, so a rule aimed at the root's siblings has something to reach.
// Requirement: every element an approved rule selects is the diagram root or inside it (design-spec 2.3 return
// button; ASSET-13 in goals/2026-10-02-system-cards/goal.md). Real Chromium repeats this check in the runtime evidence.
describe('every rule a gate approves selects only the diagram root or its descendants', () => {
  function childDocument(svg: string): { doc: Document; root: Element } {
    const doc = document.implementation.createHTMLDocument('child');
    doc.body.innerHTML = '<div id="staging"></div>' +
      '<div id="display"><button id="return-to-document" type="button">x</button></div>' +
      '<p id="d" class="box edge node"><span class="box edge">host</span><rect class="box"></rect></p>';
    const root = doc.importNode(parse(svg).documentElement, true);
    doc.getElementById('display')!.prepend(root);
    return { doc, root };
  }
  // jsdom builds a style sheet only in a document with a window, so the test document parses it and then drops it.
  function selectorsOf(svg: string): string[] {
    const css = Array.from(parse(svg).getElementsByTagNameNS(SVG, 'style')).map(style => style.textContent ?? '').join('\n');
    const style = document.createElement('style');
    style.textContent = css;
    document.head.append(style);
    try {
      const sheet = style.sheet;
      if (!sheet) throw new Error('the CSS parser did not build a sheet');
      const rules = Array.from(sheet.cssRules);
      return rules.flatMap(rule => ('selectorText' in rule ? String(rule.selectorText).split(',') : [rule.cssText]));
    } finally {
      style.remove();
    }
  }
  // Selectors the engine cannot parse select nothing in a browser, so they cannot leave the diagram.
  function escapes(svg: string): string[] {
    const { doc, root } = childDocument(svg);
    return selectorsOf(svg).filter(selector => {
      let selected: Element[];
      try {
        selected = Array.from(doc.querySelectorAll(selector));
      } catch {
        return false;
      }
      return selected.some(element => element !== root && !root.contains(element));
    });
  }
  function selectsInside(svg: string, selector: string): number {
    const { doc, root } = childDocument(svg);
    return Array.from(doc.querySelectorAll(selector)).filter(element => element === root || root.contains(element)).length;
  }
  const styled = (css: string, rootAttributes = '') => base(`<style>${css}</style>`, rootAttributes);

  it('the engines see the collision: an unqualified id rule reaches the HTML host, the svg-typed rule does not', () => {
    expect(escapes(styled('#d .box{fill:#715333}'))).toEqual(['#d .box']);
    expect(escapes(styled('svg#d .box{fill:#715333}'))).toEqual([]);
    expect(selectsInside(styled('svg#d .box{fill:#715333}'), 'svg#d .box')).toBeGreaterThan(0);
  });

  // Each accepted case selects at least one diagram element, so the scope check below is not vacuous.
  it.each([
    ['the root alone', 'svg#d{color:#715333}', 'svg#d', ''],
    ['a root compound with a class', 'svg#d.root .box{fill:#715333}', 'svg#d.root .box', ' class="root"'],
    ['a descendant class', 'svg#d .box{fill:#715333}', 'svg#d .box', ''],
    ['a child chain', 'svg#d > g > rect{fill:#715333}', 'svg#d > g > rect', ''],
    ['a general sibling after a descendant step', 'svg#d .node ~ .edge{stroke:#715333}', 'svg#d .node ~ .edge', ''],
    ['an adjacent sibling after a child step', 'svg#d > g + path{stroke:#715333}', 'svg#d > g + path', ''],
    ['the library attribute suffix form', 'svg#d [id$="-node"]{fill:#715333}', 'svg#d [id$="-node"]', ''],
    ['the library neo attribute form on the root', 'svg#d[data-look="neo"] .box{fill:#715333}', 'svg#d[data-look="neo"] .box', ' data-look="neo"'],
    ['a comma list of scoped items', 'svg#d .box,svg#d text{fill:#715333}', 'svg#d text', ''],
    ['tabs and newlines between steps', 'svg#d\t>\ng\n.box{fill:#715333}', 'svg#d > g .box', ''],
  ])('accepts %s in both gates and selects only inside the diagram', (_name, css, probe, rootAttributes) => {
    const input = styled(css, rootAttributes);
    expect(wellFormed(input)).toBe(true);
    expect(verdict(input)).toEqual(both('accepted'));
    expect(selectsInside(input, probe)).toBeGreaterThan(0);
    expect(escapes(input)).toEqual([]);
  });

  // Every one of these selects something outside the diagram in the child document.
  const escaping: [string, string][] = [
    ['an adjacent sibling of the root', 'svg#d + button{display:none}'],
    ['a general sibling of the root', 'svg#d ~ #return-to-document{visibility:hidden}'],
    ['a universal sibling of the root', 'svg#d ~ *{display:none}'],
    ['a second comma item outside the diagram', 'svg#d .box,button{display:none}'],
    ['a comma item that is the host id', 'svg#d .box,#display{display:none}'],
    ['a class shared with the host', '.box{fill:red}'],
    ['the universal selector', '*{display:none}'],
    ['the universal selector in any namespace', '*|*{display:none}'],
    ['an escaped type selector', '\\62 utton{display:none}'],
    ['the html element', 'html{visibility:hidden}'],
    ['the display container under its own type', 'div#display{display:none}'],
    ['a descendant of the host paragraph', 'p .box{fill:red}'],
  ];
  it.each(escaping)('refuses %s in both gates', (_name, css) => {
    const input = styled(css);
    expect(wellFormed(input)).toBe(true);
    expect(escapes(input).length, 'the engine confirms this rule leaves the diagram').toBeGreaterThan(0);
    expect(verdict(control)).toEqual(both('accepted'));
    expect(verdict(input)).toEqual(both('refused'));
  });

  // Mermaid's raw `#id` form is child input only. The parent judges strings, and a compromised child could send the
  // raw form, so the parent must refuse it; the child may keep it only as the svg-typed rule it then sends.
  it.each([
    ['an unqualified id that collides with a host element', '#d .box{fill:red}'],
    ['an unqualified root id rule', '#d{display:none}'],
  ])('refuses %s in the parent, and the child sends only an in-scope rule', (_name, css) => {
    const input = styled(css);
    expect(escapes(input).length, 'the engine confirms this rule leaves the diagram').toBeGreaterThan(0);
    expect(svgError(input)).not.toBeNull();
    expect(childGate(input)).toBe('accepted');
    const output = prepareStaticDiagram(input).svg;
    expect(escapes(output)).toEqual([]);
    expect(svgError(output)).toBeNull();
  });

  // Inputs that may be refused or kept: if a gate keeps a string, nothing it keeps may leave the diagram, and a
  // string the child keeps must be what the parent then accepts.
  const either: [string, string][] = [
    ['an upper-case type on the root compound', 'SVG#d .box{fill:#715333}'],
    ['an upper-case root id', 'svg#D .box{fill:#715333}'],
    ['an id that only shares a prefix with the root', 'svg#d-node{fill:#715333}'],
    ['a different root id', 'svg#e .box{fill:#715333}'],
    ['a pseudo-class after the root', 'svg#d :root{fill:#715333}'],
    ['a namespace prefix', 'svg|rect{fill:#715333}'],
    ['an escaped root id', 'svg#\\64  .box{fill:#715333}'],
    ['a trailing combinator', 'svg#d >{fill:#715333}'],
    ['a doubled combinator', 'svg#d > > rect{fill:#715333}'],
    ['an empty trailing comma item', 'svg#d .box,{fill:#715333}'],
    ['an empty leading comma item', ',svg#d .box{fill:#715333}'],
    ['two empty comma items', 'svg#d .box,,svg#d text{fill:#715333}'],
    ['a no-break space before the type', ' svg#d .box{fill:#715333}'],
    ['a no-break space after the root id', 'svg#d .box{fill:#715333}'],
    ['an HTML comment token before a host rule', 'svg#d .box{fill:#715333}<!--button{display:none}'],
    ['a raw library id rule', '#d .box{fill:#715333}'],
    ['a sibling escape in raw library form', '#d + button{display:none}'],
    ['a font variable glued to a class', `#d .a:root{--mermaid-font-family:"trebuchet ms",verdana,arial,sans-serif;}{fill:red}`],
  ];
  it.each([...escaping, ...either])('%s: whatever is kept stays inside the diagram', (_name, css) => {
    const input = styled(css);
    if (!wellFormed(input)) {
      // A malformed document is never approved, so nothing of it is kept.
      expect(verdict(input)).toEqual(both('refused'));
      return;
    }
    if (svgError(input) === null) expect(escapes(input)).toEqual([]);
    if (childGate(input) === 'accepted') {
      const output = prepareStaticDiagram(input).svg;
      expect(svgError(output)).toBeNull();
      expect(escapes(output)).toEqual([]);
    }
  });
});

// The child alone reads Mermaid's raw output. It may normalize only the library's exact forms, after checking the
// whole original (design msg_1e80f351e41a: exact root selectors and the exact :root font rule move onto svg#id).
describe('the child normalizes only exact library forms, after checking the original', () => {
  const fontRule = ':root{--mermaid-font-family:"trebuchet ms",verdana,arial,sans-serif;}';
  const styled = (css: string) => base(`<style>${css}</style>`);

  it('adds only the svg type to a raw id rule, and the parent refuses the raw form', () => {
    const raw = styled('#d .box{fill:#715333}');
    expect(verdict(raw)).toEqual({ parent: 'refused', child: 'accepted' });
    const output = prepareStaticDiagram(raw);
    const styles = Array.from(output.root.querySelectorAll('style'));
    expect(styles.at(-1)!.textContent).toBe('svg#d .box{fill:#715333}');
    expect(svgError(output.svg)).toBeNull();
  });

  it.each([
    ['a different font list', ':root{--mermaid-font-family:"trebuchet ms",verdana,arial;}'],
    ['a missing final semicolon', ':root{--mermaid-font-family:"trebuchet ms",verdana,arial,sans-serif}'],
    ['a space before the block', ':root {--mermaid-font-family:"trebuchet ms",verdana,arial,sans-serif;}'],
    ['another property on :root', ':root{fill:red}'],
    ['visibility on :root', ':root{visibility:hidden}'],
    ['the library rule under another id', `#e ${fontRule}`],
    ['the library rule with two spaces after the id', `#d  ${fontRule}`],
    ['a host rule after the exact font rule', `${fontRule}button{display:none}`],
    ['an import after the exact font rule', `${fontRule}@import url(https://example.invalid/x.css);`],
    ['an external url next to the exact font rule', `${fontRule}#d .box{fill:url(https://example.invalid/p)}`],
  ])('refuses %s in the child', (_name, css) => {
    const input = styled(css);
    expect(wellFormed(input)).toBe(true);
    expect(childGate(control)).toBe('accepted');
    expect(childGate(input)).toBe('refused');
  });
});

// The parent approves a string and the child displays a node. They must be the same output, and the parent's
// style-neutral inspection copy must stay inside the parent check.
describe('the approved string and the kept node are one output', () => {
  const cases = [
    ...fixtures.map(({ kind, svg }) => ({ kind, svg })),
    { kind: 'small static diagram', svg: control },
    { kind: 'stylesheet with CDATA and comment', svg: base('<style><![CDATA[svg#d .box{fill:]]><!-- note -->#715333}</style>') },
  ];

  it.each(cases)('$kind', ({ svg }) => {
    const prepared = prepareStaticDiagram(svg);
    expect(staticDiagramSvg(svg)).toBe(prepared.svg);
    expect(svgError(prepared.svg)).toBeNull();
    const reparsed = parse(prepared.svg);
    expect(reparsed.querySelector('parsererror')).toBeNull();
    expect(reparsed.documentElement.isEqualNode(prepared.root)).toBe(true);
    expect(prepared.root.namespaceURI).toBe(SVG);
    expect(prepared.root.ownerDocument).not.toBe(document);
    expect(prepared.root.ownerDocument.documentElement).toBe(prepared.root);
    const names = Array.from(prepared.root.querySelectorAll('*')).flatMap(element => [element.localName, ...element.getAttributeNames()]);
    expect(names.filter(name => name.includes('inspection'))).toEqual([]);
    expect(prepared.svg).not.toContain('dh-inspection-style');
  });

  it('gives each call its own node, so a displayed node cannot be shared with a later result', () => {
    const first = prepareStaticDiagram(control).root;
    const second = prepareStaticDiagram(control).root;
    expect(first).not.toBe(second);
    expect(first.isEqualNode(second)).toBe(true);
  });
});

describe('the library cleanup removes only unused ornament and checks it before removing', () => {
  const packetDoc = parse(packetSvg);
  const id = packetDoc.documentElement.id;
  const symbolId = `${id}-computer`;
  const editPacket = (edit: (doc: Document) => void) => {
    const doc = parse(packetSvg);
    edit(doc);
    return new XMLSerializer().serializeToString(doc.documentElement);
  };
  const append = (doc: Document, parent: Element, markup: string) => {
    const fragment = parse(`<svg xmlns="${SVG}" xmlns:xlink="${XLINK}">${markup}</svg>`);
    expect(fragment.querySelector('parsererror')).toBeNull();
    for (const node of Array.from(fragment.documentElement.childNodes)) parent.append(doc.importNode(node, true));
  };

  it('works on a packet fixture that has the library symbols, shadow filters and an actor class', () => {
    expect(packetDoc.getElementById(symbolId)?.localName).toBe('symbol');
    expect(packetDoc.getElementById(`${id}-drop-shadow`)?.localName).toBe('filter');
    expect(packetDoc.querySelector('rect.actor')).not.toBeNull();
    expect(childGate(packetSvg)).toBe('accepted');
  });

  it('keeps a used rule and only removes the library shadow declaration from it', () => {
    const rule = `#${id} .actor{fill:#f3e9d2;filter:drop-shadow( 1px 2px 2px rgba(185,185,185,1));}`;
    const input = editPacket(doc => { doc.querySelector('style')!.append(rule); });
    expect(input).toContain(rule);
    const output = prepareStaticDiagram(input).svg;
    expect(output).toContain(`#${id} .actor{fill:#f3e9d2;}`);
    expect(output).not.toContain('drop-shadow');
    expect(svgError(output)).toBeNull();
  });

  it.each([
    ['a library symbol named by aria-labelledby', (doc: Document) => doc.querySelector('rect.actor')!.setAttribute('aria-labelledby', symbolId)],
    ['a library shadow filter referenced by use', (doc: Document) => append(doc, doc.documentElement, `<use href="#${id}-drop-shadow"/>`)],
    ['a neo gradient rule with one used selector in its list', (doc: Document) => {
      append(doc, doc.documentElement, '<g data-look="neo" class="neo-used"><rect width="1" height="1"/></g>');
      doc.querySelector('style')!.append(`#${id} [data-look="neo"].neo-unused,#${id} [data-look="neo"].neo-used{stroke:url(#${id}-gradient);}`);
    }],
    ['a neo gradient rule written as a compound selector on the root', (doc: Document) => {
      doc.querySelector('style')!.append(`#${id}[data-look="neo"]{stroke:url(#${id}-gradient);}`);
    }],
    ['an upper-case Script inside an unused library symbol', (doc: Document) => append(doc, doc.getElementById(symbolId)!, '<Script>1</Script>')],
    ['a STYLE attribute effect inside an unused library symbol', (doc: Document) => append(doc, doc.getElementById(symbolId)!, '<rect width="1" height="1" STYLE="filter:blur(2px)"/>')],
    ['markup inside a title of an unused library symbol', (doc: Document) => append(doc, doc.getElementById(symbolId)!, '<title><tspan>x</tspan></title>')],
    ['a foreignObject inside a library shadow filter', (doc: Document) => append(doc, doc.getElementById(`${id}-drop-shadow`)!, '<foreignObject width="1" height="1"/>')],
    ['an external href on the library shadow primitive', (doc: Document) => doc.querySelector('feDropShadow')!.setAttribute('href', 'https://example.invalid/x')],
  ])('refuses %s instead of removing it', (_name, edit) => {
    const input = editPacket(edit);
    expect(input).not.toBe(packetSvg);
    expect(childGate(input)).toBe('refused');
  });
});

describe('size and time bounds', () => {
  // Pads a desc so the whole document is exactly `bytes` UTF-8 bytes.
  function sized(bytes: number): string {
    const without = base('<desc></desc>');
    const padding = bytes - byteLength(without);
    expect(padding).toBeGreaterThan(0);
    return base(`<desc>${'a'.repeat(padding)}</desc>`);
  }

  it('accepts exactly 256 KiB in both gates and refuses one byte more', () => {
    const atLimit = sized(MAX_SVG_BYTES);
    const overLimit = sized(MAX_SVG_BYTES + 1);
    expect(byteLength(atLimit)).toBe(MAX_SVG_BYTES);
    expect(byteLength(overLimit)).toBe(MAX_SVG_BYTES + 1);
    expect(verdict(atLimit)).toEqual(both('accepted'));
    expect(verdict(overLimit)).toEqual(both('refused'));
  });

  it('decides a near-limit diagram with thousands of elements well inside the five-second lifetime', () => {
    const shape = '<rect class="box" x="1" y="1" width="4" height="4" fill="#f3e9d2"/>';
    const count = Math.floor((MAX_SVG_BYTES - byteLength(control) - 64) / byteLength(shape));
    const input = base(`<g>${shape.repeat(count)}</g>`);
    expect(byteLength(input)).toBeLessThanOrEqual(MAX_SVG_BYTES);
    expect(count).toBeGreaterThan(3000);
    const started = performance.now();
    const outcome = verdict(input);
    const elapsed = performance.now() - started;
    expect(outcome).toEqual(both('accepted'));
    expect(elapsed).toBeLessThan(5000);
  });

  // A stylesheet selector is short input. Deciding it must not grow exponentially with its length, or the parent's
  // main thread stops (no timer can fire during a synchronous check) and the child misses its five-second ACK.
  const letters = 'a'.repeat(27);
  it.each([
    ['a long supported selector', `<style>svg#d ${letters}{fill:#715333}</style>`, 'accepted'],
    ['a long selector that ends in an unsupported pseudo-class', `<style>svg#d ${letters}:hover{fill:#715333}</style>`, 'any'],
  ] as const)('decides %s in bounded time in each gate', (_name, body, expected) => {
    const input = base(body);
    expect(wellFormed(input)).toBe(true);
    let started = performance.now();
    const parent = svgError(input) === null ? 'accepted' : 'refused';
    const parentMs = performance.now() - started;
    started = performance.now();
    const child = childGate(input);
    const childMs = performance.now() - started;
    if (expected === 'accepted') expect({ parent, child }).toEqual(both('accepted'));
    else expect(child).not.toBe('crashed');
    expect(parentMs, 'parent svgError milliseconds').toBeLessThan(250);
    expect(childMs, 'child prepareStaticDiagram milliseconds').toBeLessThan(250);
  });

  // Cost must follow input size: the parent decides a child result on its main thread, so its check has to stay
  // linear inside the 256 KiB cap (ASSET-12 in goals/2026-10-02-system-cards/goal.md). One timing proves little,
  // so each shape is timed at n and 4n characters inside one run and the growth is bounded. Linear work grows about
  // 4x, quadratic work about 16x, exponential work does not finish. Times under the floor count as the floor so that
  // timer noise on tiny inputs cannot fail a linear check. Real Chromium timings are kept in the runtime evidence.
  const floorMs = 5;
  function timedVerdict(input: string) {
    const parentRuns: number[] = [];
    const childRuns: number[] = [];
    let parent: Gate = 'crashed';
    let child: Gate = 'crashed';
    for (let run = 0; run < 3; run++) {
      let started = performance.now();
      parent = svgError(input) === null ? 'accepted' : 'refused';
      parentRuns.push(performance.now() - started);
      started = performance.now();
      child = childGate(input);
      childRuns.push(performance.now() - started);
    }
    const median = (values: number[]) => [...values].sort((a, b) => a - b)[1]!;
    return { parent, child, parentMs: median(parentRuns), childMs: median(childRuns) };
  }
  function expectLinearGrowth(name: string, make: (n: number) => string, expected: Gate, n: number) {
    const small = base(make(n));
    const large = base(make(4 * n));
    expect(byteLength(large)).toBeLessThanOrEqual(MAX_SVG_BYTES);
    const first = timedVerdict(small);
    const second = timedVerdict(large);
    console.info('[CSS-REVIEW-TIMING]', JSON.stringify({ name, n, bytes: [byteLength(small), byteLength(large)], first, second }));
    expect({ parent: second.parent, child: second.child }).toEqual(both(expected));
    // Soft, so a slow parent still reports the child's growth in the same run.
    expect.soft(second.parentMs / Math.max(first.parentMs, floorMs), 'parent growth for 4x input').toBeLessThan(8);
    expect.soft(second.childMs / Math.max(first.childMs, floorMs), 'child growth for 4x input').toBeLessThan(8);
  }

  const selectorShapes: [string, (n: number) => string, Gate][] = [
    ['one long identifier that ends in a pseudo-class', n => `<style>svg#d ${'a'.repeat(n)}:hover{fill:#715333}</style>`, 'refused'],
    ['many descendant steps', n => `<style>svg#d ${'a '.repeat(n / 2)}b{fill:#715333}</style>`, 'accepted'],
    ['many descendant steps that end in a pseudo-class', n => `<style>svg#d ${'a '.repeat(n / 2)}b:hover{fill:#715333}</style>`, 'refused'],
    ['a long compound of classes', n => `<style>svg#d ${'.a'.repeat(n / 2)}:hover{fill:#715333}</style>`, 'refused'],
    ['many comma items', n => `<style>${'svg#d a,'.repeat(n / 8)}svg#d a:hover{fill:#715333}</style>`, 'refused'],
    ['alternating combinators', n => `<style>svg#d a${'>a~a+a'.repeat(n / 6)}:hover{fill:#715333}</style>`, 'refused'],
    ['a long whitespace run inside a selector', n => `<style>svg#d${' '.repeat(n)}a:hover{fill:#715333}</style>`, 'refused'],
    ['many rules with the last one unsupported', n => `<style>${'svg#d a{fill:#715333}'.repeat(n / 21)}svg#d a:hover{fill:#715333}</style>`, 'refused'],
    ['a long attribute suffix value', n => `<style>svg#d [id$="-${'a'.repeat(n)}"]:hover{fill:#715333}</style>`, 'refused'],
  ];
  it.each(selectorShapes)('selector shape %s: decision cost grows linearly in each gate', (name, make, expected) => {
    expectLinearGrowth(name, make, expected, 8000);
  }, 60_000);

  it.each(selectorShapes)('selector shape %s: decided near the 256 KiB cap well inside the five-second lifetime', (_name, make, expected) => {
    const n = 230_000;
    const input = base(make(n));
    expect(byteLength(input)).toBeLessThanOrEqual(MAX_SVG_BYTES);
    expect(byteLength(input)).toBeGreaterThan(200 * 1024);
    const measured = timedVerdict(input);
    expect({ parent: measured.parent, child: measured.child }).toEqual(both(expected));
    expect(measured.parentMs, 'parent svgError milliseconds').toBeLessThan(2000);
    expect(measured.childMs, 'child prepareStaticDiagram milliseconds').toBeLessThan(2000);
  }, 60_000);

  // The same linear requirement holds for every part of the parent check, not only selectors. These shapes reach
  // declaration values (ASSET-15) and the parent's string scan before parsing (ASSET-16); they are refused, and must be
  // refused quickly.
  const valueShapes: [string, (n: number) => string][] = [
    ['a whitespace run inside a stylesheet declaration value', n => `<style>svg#d .box{fill:#715333${' '.repeat(n)}x}</style>`],
    ['a whitespace run inside a style attribute value', n => `<rect width="1" height="1" style="fill:#715333${' '.repeat(n)}x"/>`],
    ['an unterminated start tag with a long name', n => `<g${'b'.repeat(n)}`],
  ];
  it.each(valueShapes)('value shape %s: decision cost grows linearly in each gate', (name, make) => {
    expectLinearGrowth(name, make, 'refused', 8000);
  }, 120_000);

  // The child alone moves Mermaid's bare :root font rule onto svg#<root id> (design msg_1e80f351e41a). Every copy of
  // that rule then carries the whole root id, so a raw input with a long id and many copies must not make the child's
  // work grow with copies x id length inside the 256 KiB cap (ASSET-18; real Chromium took about 2 s at 256 KiB). Here
  // the id appears once and gets half of the bytes, which makes copies x id length the largest for a given size.
  const libraryRootFont = ':root{--mermaid-font-family:"trebuchet ms",verdana,arial,sans-serif;}';
  function rootFontInput(bytes: number, copies?: number): string {
    const head = (id: string) => `<svg xmlns="${SVG}" id="${id}" viewBox="0 0 120 40"><style>`;
    const tail = '</style><path d="M1,1 L80,11"/></svg>';
    const budget = bytes - byteLength(head('') + tail);
    const idLength = Math.floor(budget / 2);
    const count = copies ?? Math.floor((budget - idLength) / libraryRootFont.length);
    return `${head('a'.repeat(idLength))}${libraryRootFont.repeat(count)}${tail}`;
  }

  it('the bare root font rule in these inputs is the exact text Mermaid 12 writes', () => {
    for (const { svg } of fixtures) expect(svg).toContain(libraryRootFont);
  });

  it('a long root id with one library font rule stays a raw form the child accepts', () => {
    const input = rootFontInput(128 * 1024, 1);
    expect(wellFormed(input)).toBe(true);
    expect(verdict(input)).toEqual({ parent: 'refused', child: 'accepted' });
  });

  it('library font rule copies under a long root id: child cost grows linearly (ASSET-18)', () => {
    const small = rootFontInput(MAX_SVG_BYTES / 4);
    const large = rootFontInput(MAX_SVG_BYTES);
    expect(byteLength(large)).toBeLessThanOrEqual(MAX_SVG_BYTES);
    expect(large.split(libraryRootFont).length - 1).toBeGreaterThan(1500);
    const first = timedVerdict(small);
    const second = timedVerdict(large);
    console.info('[GATE-REVIEW-TIMING]', JSON.stringify({ name: 'library font rule copies under a long root id', bytes: [byteLength(small), byteLength(large)], first, second }));
    expect(second.parent).toBe('refused');
    expect(second.child).not.toBe('crashed');
    expect.soft(second.childMs / Math.max(first.childMs, floorMs), 'child growth for 4x input').toBeLessThan(8);
  }, 120_000);

  // The ASSET-18 repair charges expanded font text before the copy is made. That limit is an allocation budget, not a
  // new acceptance rule: an input the earlier child accepted and whose final output fits the cap must still be accepted
  // (goal 현재 단계 19:54 UTC, msg_e220bf03be69 "임의의 더좁은승인집합은 만들지 마십시오"). These inputs place the expanded text
  // where the adapter later removes it or where scoping trims it, and the final output is what the parent approves.
  function childOutput(input: string): string | null {
    try {
      return prepareStaticDiagram(input).svg;
    } catch {
      return null;
    }
  }

  // A raw library symbol may hold graphics, a g may hold a style, and an unused library symbol is removed whole.
  it.each([
    ['g', (css: string) => `<g><style>${css}</style></g>`],
    ['g and defs', (css: string) => `<g><defs><style>${css}</style></defs></g>`],
  ])('font copies in a style inside an unused library symbol (%s) stay accepted: the symbol is removed', (_path, wrap) => {
    const id = 'a'.repeat(1000);
    const input = `<svg xmlns="${SVG}" id="${id}" viewBox="0 0 120 40"><defs>` +
      `<symbol id="${id}-computer" width="24" height="24">${wrap(libraryRootFont.repeat(600))}<path d="M0,0 L1,1"/></symbol>` +
      '</defs><path d="M1,1 L80,11"/></svg>';
    expect(wellFormed(input)).toBe(true);
    // 600 copies x (1000-char id + 68) is about 640 000 characters if the removed style were expanded and counted.
    expect(byteLength(input)).toBeLessThan(64 * 1024);
    expect(verdict(input)).toEqual({ parent: 'refused', child: 'accepted' });
    const output = childOutput(input)!;
    expect(byteLength(output)).toBeLessThan(2048);
    expect(output).not.toContain('symbol');
    expect(svgError(output)).toBeNull();
  });

  it('selector whitespace that scoping trims pays for inserted root ids, so an output under the cap is still accepted', () => {
    // Each copy is preceded by as many spaces as the root id is long. The expanded text is then about twice the final
    // output, while the output (spaces trimmed, id inserted) stays under the cap.
    const id = 'a'.repeat(256);
    const copy = ' '.repeat(256) + libraryRootFont;
    const head = `<svg xmlns="${SVG}" id="${id}" viewBox="0 0 120 40"><style>`;
    const tail = '</style><path d="M1,1 L80,11"/></svg>';
    const copies = Math.floor((MAX_SVG_BYTES - byteLength(head + tail)) / copy.length);
    const input = `${head}${copy.repeat(copies)}${tail}`;
    expect(byteLength(input)).toBeLessThanOrEqual(MAX_SVG_BYTES);
    expect(copies * (256 + 256 + 68)).toBeGreaterThan(MAX_SVG_BYTES);
    expect(verdict(input)).toEqual({ parent: 'refused', child: 'accepted' });
    const output = childOutput(input)!;
    expect(byteLength(output)).toBeLessThanOrEqual(MAX_SVG_BYTES);
    expect(output.split(`svg#${id}{--mermaid-font-family:`).length - 1).toBe(copies);
    expect(output).not.toContain(':root');
    expect(svgError(output)).toBeNull();
  });

  // A two-character root id makes each moved font rule one character longer than the bare rule (design
  // msg_1e80f351e41a: :root moves onto svg#<id>), so the child output is one byte longer than its input.
  it.each([
    [MAX_SVG_BYTES - 2, 'accepted', MAX_SVG_BYTES - 1],
    [MAX_SVG_BYTES - 1, 'accepted', MAX_SVG_BYTES],
    [MAX_SVG_BYTES, 'refused', null],
  ] as const)('the final cap is judged on the child output: input %i bytes is %s', (bytes, expected, outputBytes) => {
    const head = `<svg xmlns="${SVG}" id="dd" viewBox="0 0 120 40"><style>${libraryRootFont}</style><desc>`;
    const tail = '</desc></svg>';
    const input = head + 'a'.repeat(bytes - byteLength(head + tail)) + tail;
    expect(byteLength(input)).toBe(bytes);
    expect(childGate(input)).toBe(expected);
    const output = childOutput(input);
    expect(output === null ? null : byteLength(output)).toBe(outputBytes);
  });

  // The new UTF-16 length check runs before trim/TextEncoder; it must not replace the UTF-8 byte cap.
  it('the UTF-8 byte cap still decides text whose UTF-16 length is far below it', () => {
    const head = `<svg xmlns="${SVG}" id="d" viewBox="0 0 120 40"><desc>`;
    const tail = '</desc></svg>';
    const room = MAX_SVG_BYTES - byteLength(head + tail);
    const atCap = head + '가'.repeat(Math.floor(room / 3)) + 'a'.repeat(room % 3) + tail;
    const overCap = head + '가'.repeat(Math.floor(room / 3) + 1) + tail;
    expect(byteLength(atCap)).toBe(MAX_SVG_BYTES);
    expect(byteLength(overCap)).toBeGreaterThan(MAX_SVG_BYTES);
    expect(overCap.length).toBeLessThan(MAX_SVG_BYTES / 2);
    expect(verdict(atCap)).toEqual(both('accepted'));
    expect(verdict(overCap)).toEqual(both('refused'));
  });

  // The budget is shared by the document: spreading copies over many style nodes must not reset it per node.
  it('library font rule copies spread over many style nodes under a long root id: child cost grows linearly', () => {
    function spread(bytes: number): string {
      const styles = 100;
      const id = 'a'.repeat(Math.floor(bytes / 2));
      const head = `<svg xmlns="${SVG}" id="${id}" viewBox="0 0 120 40">`;
      const tail = '<path d="M1,1 L80,11"/></svg>';
      const perStyle = Math.floor((bytes - byteLength(head + tail)) / styles / byteLength(libraryRootFont)) - 1;
      return head + `<style>${libraryRootFont.repeat(perStyle)}</style>`.repeat(styles) + tail;
    }
    const small = spread(MAX_SVG_BYTES / 4);
    const large = spread(MAX_SVG_BYTES);
    expect(byteLength(large)).toBeLessThanOrEqual(MAX_SVG_BYTES);
    expect(large.split(libraryRootFont).length - 1).toBeGreaterThan(1500);
    const first = timedVerdict(small);
    const second = timedVerdict(large);
    console.info('[COST-REVIEW-FOURTH-TIMING]', JSON.stringify({ name: 'font copies over many style nodes', bytes: [byteLength(small), byteLength(large)], first, second }));
    expect(second.parent).toBe('refused');
    expect(second.child).toBe('refused');
    expect.soft(second.childMs / Math.max(first.childMs, floorMs), 'child growth for 4x input').toBeLessThan(8);
  }, 120_000);
});

// Fifth cost repair (ASSET-19): the child must not charge or grow a style that leaves with an unused library symbol,
// and every check on that style must still run before the symbol goes (메인 msg_6b159abbb02f 4항 "시간 상한과 승인 보존을
// 둘 다 완료조건"; Astra msg_e220bf03be69 "임의의 더좁은승인집합은 만들지 마십시오"; this file's header for refusals). The
// raw content model lets a style reach a symbol only through a g (symbol -> graphics, g -> style/defs/marker, defs ->
// style/clipPath/raw symbol, marker/clipPath -> graphics). Expected outputs are written from the design, not computed
// by the adapter: the symbol is gone, the bare :root font rule moves onto svg#<id> and a raw #<id> selector gains the
// svg type (design msg_1e80f351e41a). The symbol shares a defs with a kept marker so its removal empties nothing.
describe('an unused library symbol is checked whole, then leaves without its styles being grown or charged (ASSET-19)', () => {
  const fontRule = ':root{--mermaid-font-family:"trebuchet ms",verdana,arial,sans-serif;}';
  const movedFontRule = (id: string) => `svg#${id}{--mermaid-font-family:"trebuchet ms",verdana,arial,sans-serif;}`;
  const childOutputOf = (input: string): string | null => {
    try {
      return prepareStaticDiagram(input).svg;
    } catch {
      return null;
    }
  };
  const keptStyle = (id: string, raw: boolean) => raw
    ? `<style>#${id} .node rect{stroke:#715333}${fontRule}</style>`
    : `<style>svg#${id} .node rect{stroke:#715333}${movedFontRule(id)}</style>`;
  const marker = (id: string) => `<marker id="${id}-arrow" viewBox="0 0 10 10" refX="5" refY="5" markerWidth="6" ` +
    'markerHeight="6" orient="auto"><path d="M0,0 L10,5 L0,10 z"/></marker>';
  const drawing = (id: string) => '<g class="node"><title>시작 노드</title><rect x="1" y="1" width="40" height="20" ' +
    `fill="#f3e9d2"/><text x="4" y="16">시작</text></g><path class="edge" d="M41,11 L80,11" marker-end="url(#${id}-arrow)"/>`;
  const librarySymbol = (id: string, body: string, name = 'computer') =>
    `<symbol id="${id}-${name}" width="24" height="24">${body}<path d="M0,0 L1,1"/></symbol>`;
  // raw: what the library writes, with the symbol beside the kept marker. final: the output the child must return.
  const libraryPage = (id: string, symbol: string, raw: boolean, extra = '') =>
    `<svg xmlns="${SVG}" id="${id}" viewBox="0 0 120 40">${keptStyle(id, raw)}<defs>${marker(id)}${raw ? symbol : ''}</defs>` +
    `${extra}${drawing(id)}</svg>`;

  const insideSymbol: [string, (css: string, id: string) => string][] = [
    ['g', css => `<g><style>${css}</style></g>`],
    ['g > defs', css => `<g><defs><style>${css}</style></defs></g>`],
    ['g > marker > g', (css, id) => `<g><marker id="${id}-inner-marker"><g><style>${css}</style></g></marker></g>`],
    ['g > defs > clipPath > g', (css, id) => `<g><defs><clipPath id="${id}-inner-clip"><g><style>${css}</style></g></clipPath></defs></g>`],
    ['g > defs > a second library symbol > g', (css, id) => `<g><defs>${librarySymbol(id, `<g><style>${css}</style></g>`, 'clock')}</defs></g>`],
  ];

  it('the base page is accepted and becomes exactly the final form written here', () => {
    const id = 'd';
    const input = libraryPage(id, '', true);
    expect(wellFormed(input)).toBe(true);
    expect(verdict(input)).toEqual({ parent: 'refused', child: 'accepted' });
    expect(childOutputOf(input)).toBe(libraryPage(id, '', false));
    expect(svgError(libraryPage(id, '', false))).toBeNull();
  });

  it.each(insideSymbol)('a style at symbol > %s is checked, then leaves with the symbol: the output is the page without it', (_path, wrap) => {
    const id = `d${'a'.repeat(999)}`;
    const input = libraryPage(id, librarySymbol(id, wrap(fontRule.repeat(600), id)), true);
    const expected = libraryPage(id, '', false);
    expect(wellFormed(input)).toBe(true);
    // 600 copies x (1000-character id + 68) is about 640 000 characters if the removed style were grown and charged.
    expect(byteLength(input)).toBeLessThan(64 * 1024);
    expect(verdict(input)).toEqual({ parent: 'refused', child: 'accepted' });
    expect(childOutputOf(input)).toBe(expected);
    expect(svgError(expected)).toBeNull();
  });

  it('a library symbol inside g > defs leaves the same way', () => {
    const id = `d${'a'.repeat(999)}`;
    const symbol = librarySymbol(id, `<g><style>${fontRule.repeat(600)}</style></g>`);
    const input = libraryPage(id, '', true, `<g><defs>${symbol}</defs></g>`);
    expect(wellFormed(input)).toBe(true);
    expect(verdict(input)).toEqual({ parent: 'refused', child: 'accepted' });
    expect(childOutputOf(input)).toBe(libraryPage(id, '', false, '<g><defs/></g>'));
  });

  it('copies kept outside the symbol are charged alone: removed copies cannot refuse an output under the cap', () => {
    const id = `d${'a'.repeat(1999)}`;
    const keptCopies = 100;
    const removedCopies = 600;
    const input = libraryPage(id, librarySymbol(id, `<g><style>${fontRule.repeat(removedCopies)}</style></g>`), true,
      `<style>${fontRule.repeat(keptCopies)}</style>`);
    const expected = libraryPage(id, '', false, `<style>${movedFontRule(id).repeat(keptCopies)}</style>`);
    expect(wellFormed(input)).toBe(true);
    expect(byteLength(expected)).toBeLessThanOrEqual(MAX_SVG_BYTES);
    // Kept and removed copies together would grow to more than twice the cap.
    expect((keptCopies + removedCopies) * (id.length + 68)).toBeGreaterThan(2 * MAX_SVG_BYTES);
    expect(verdict(input)).toEqual({ parent: 'refused', child: 'accepted' });
    expect(childOutputOf(input)).toBe(expected);
  });

  it.each(['the symbol first', 'the kept styles first'])('keeps style order and duplicate selectors with %s', order => {
    const id = `d${'a'.repeat(499)}`;
    const symbol = librarySymbol(id, `<g><style>#${id} .node rect{stroke:#000000}${fontRule.repeat(300)}</style></g>`);
    const later = (raw: boolean) => `<style>${raw ? '' : 'svg'}#${id} .node rect{stroke:#2a231b}</style>`;
    const page = (raw: boolean) => {
      const defs = `<defs>${marker(id)}${raw ? symbol : ''}</defs>`;
      const styles = keptStyle(id, raw) + later(raw);
      const body = order === 'the symbol first' ? defs + styles : styles + defs;
      return `<svg xmlns="${SVG}" id="${id}" viewBox="0 0 120 40">${body}${drawing(id)}</svg>`;
    };
    expect(wellFormed(page(true))).toBe(true);
    expect(verdict(page(true))).toEqual({ parent: 'refused', child: 'accepted' });
    expect(childOutputOf(page(true))).toBe(page(false));
  });

  it('keeps CDATA, escaping and whitespace of kept styles while a removed style holds the same forms', () => {
    const id = 'd';
    const unchanged = `<style><![CDATA[svg#${id} > g.node rect{stroke:#715333}]]></style>`;
    const rewritten = (raw: boolean) => `<style>${raw ? '' : 'svg'}#${id} &gt; g.node text{fill:#2a231b}</style>`;
    const removed = `<g><style><![CDATA[  ${fontRule}  ]]>#${id} &gt; g.node text{fill:#000000}   ${fontRule}</style></g>`;
    const input = libraryPage(id, librarySymbol(id, removed), true, unchanged + rewritten(true));
    expect(wellFormed(input)).toBe(true);
    expect(verdict(input)).toEqual({ parent: 'refused', child: 'accepted' });
    expect(childOutputOf(input)).toBe(libraryPage(id, '', false, unchanged + rewritten(false)));
  });

  // A check on a style decides the same way whether the style is kept or leaves with the symbol. Each refusal has an
  // accepted control in both places, so a gate that refuses everything fails here.
  type Place = 'kept' | 'removed';
  const placed = (id: string, css: string, place: Place, symbolBody = '<g/>', extra = '') => place === 'kept'
    ? libraryPage(id, librarySymbol(id, symbolBody), true, `<style>${css}</style>${extra}`)
    : libraryPage(id, librarySymbol(id, `<g><style>${css}</style></g>${symbolBody}`), true, extra);
  const animationRule = (id: string) => `#${id} .edge-animation-slow{stroke-dasharray:9,5!important;stroke-dashoffset:900;` +
    'animation:dash 50s linear infinite;stroke-linecap:round;}';
  const neoRule = (id: string, name: string) => `#${id} [data-look="neo"].${name} rect{stroke:url(#${id}-gradient);}`;
  const checks: [string, (id: string, place: Place) => string, (id: string, place: Place) => string][] = [
    ['a reference to the library symbol itself',
      (id, place) => placed(id, `#${id} .node rect{fill:url(#${id}-computer)}`, place),
      (id, place) => placed(id, `#${id} .node rect{fill:url(#${id}-arrow)}`, place)],
    ['the library animation rule while an element uses its class',
      (id, place) => placed(id, animationRule(id), place, '<g/>', '<path class="edge-animation-slow" d="M1,1 L2,2"/>'),
      (id, place) => placed(id, animationRule(id), place)],
    ['a neo rule without its gradient while a shape inside the symbol uses it',
      (id, place) => placed(id, neoRule(id, 'node'), place, '<g class="node" data-look="neo"><rect width="1" height="1"/></g>'),
      (id, place) => placed(id, neoRule(id, 'node'), place, '<g class="node"><rect width="1" height="1"/></g>')],
  ];
  it.each(checks)('refuses %s in a kept style and in a removed one', (_name, refused, accepted) => {
    const id = 'd';
    for (const place of ['kept', 'removed'] as const) {
      expect(wellFormed(refused(id, place)), place).toBe(true);
      expect(wellFormed(accepted(id, place)), place).toBe(true);
      expect(childGate(accepted(id, place)), `${place} control`).toBe('accepted');
      expect(childGate(refused(id, place)), place).toBe('refused');
    }
  });

  // Unused neo rules are looked up in the document before they are dropped, and every lookup draws on one document
  // work budget. Moving rules into a removed style must not take their lookups off that budget.
  it('charges the lookups of unused neo rules in a removed style to the same document budget as kept ones', () => {
    const id = 'd';
    const shapes = `<g>${'<rect width="1" height="1"/>'.repeat(1000)}</g>`;
    const rules = (from: number, count: number) => Array.from({ length: count },
      (_, index) => neoRule(id, `unused${String(from + index).padStart(40, '0')}`)).join('');
    // An empty style in the symbol keeps the tree depth equal between the kept-only and split pages.
    const keptOnly = (count: number) => placed(id, rules(0, count), 'kept', '<g><style></style></g>', shapes);
    const removedOnly = (count: number) => placed(id, rules(0, count), 'removed', '<g/>', shapes);
    expect(wellFormed(keptOnly(8))).toBe(true);
    expect(childGate(keptOnly(4)), 'kept 4').toBe('accepted');
    expect(childGate(removedOnly(4)), 'removed 4').toBe('accepted');
    expect(childGate(keptOnly(40)), 'kept 40').toBe('refused');
    expect(childGate(removedOnly(40)), 'removed 40').toBe('refused');
    // Four removed and four kept rules decide like eight kept ones.
    const split = libraryPage(id, librarySymbol(id, `<g><style>${rules(0, 4)}</style></g><g/>`), true, `<style>${rules(4, 4)}</style>${shapes}`);
    expect(wellFormed(split)).toBe(true);
    expect(childGate(keptOnly(8)), 'kept 8').toBe('refused');
    expect(childGate(split), 'four removed and four kept').toBe(childGate(keptOnly(8)));
  });

  const unsafe: [string, (id: string) => string][] = [
    ['an external URL', id => `#${id} .node rect{fill:url(https://example.invalid/x)}`],
    ['an import rule', () => '@import "https://example.invalid/x.css";'],
    ['a CSS comment', id => `#${id} .node rect{fill:#715333/* x */}`],
  ];
  it.each(unsafe)('refuses %s inside a removed style in both gates before anything is removed', (_name, css) => {
    const id = 'd';
    const input = placed(id, css(id), 'removed');
    expect(wellFormed(input)).toBe(true);
    expect(childGate(placed(id, `#${id} .node rect{fill:#715333}`, 'removed'))).toBe('accepted');
    expect(verdict(input)).toEqual(both('refused'));
  });

  // The removal set is exactly the symbol subtrees only while a style cannot sit directly in a symbol or in a filter.
  const shadow = (id: string, inner: string) => `<defs><filter id="${id}-drop-shadow" height="130%" width="130%">${inner}` +
    '<feDropShadow dx="4" dy="4" stdDeviation="0" flood-opacity="0.06" flood-color="#000000"/></filter></defs>';
  const misplaced: [string, (id: string) => string][] = [
    ['a style directly inside a library symbol', id => `<defs>${librarySymbol(id, `<style>${fontRule}</style>`)}</defs>`],
    ['a defs directly inside a library symbol', id => `<defs>${librarySymbol(id, `<defs><style>${fontRule}</style></defs>`)}</defs>`],
    ['a marker directly inside a library symbol', id => `<defs>${librarySymbol(id, `<marker id="${id}-m"><g><style>${fontRule}</style></g></marker>`)}</defs>`],
    ['a style inside a library shadow filter', id => shadow(id, `<style>${fontRule}</style>`)],
  ];
  it.each(misplaced)('refuses %s', (_name, extra) => {
    const id = 'd';
    expect(childGate(libraryPage(id, '', true, shadow(id, '')))).toBe('accepted');
    const input = libraryPage(id, '', true, extra(id));
    expect(wellFormed(input)).toBe(true);
    expect(verdict(input)).toEqual(both('refused'));
  });

  const notRemovable: [string, (id: string) => string, (id: string) => string][] = [
    ['an inner symbol with a name the library does not write', id => `<g><defs>${librarySymbol(id, '<g/>', 'other')}</defs></g>`, () => ''],
    ['an inner library symbol that the drawing uses', id => `<g><defs>${librarySymbol(id, '<g/>', 'clock')}</defs></g>`, id => `<use href="#${id}-clock"/>`],
    ['a shape inside the symbol that the drawing uses', id => `<g><path id="${id}-inner-shape" d="M0,0 L1,1"/></g>`, id => `<use href="#${id}-inner-shape"/>`],
  ];
  it.each(notRemovable)('refuses %s instead of removing it', (_name, body, user) => {
    const id = 'd';
    const use = user(id);
    const input = libraryPage(id, librarySymbol(id, `<g><style>${fontRule}</style></g>${body(id)}`), true, use);
    const control = libraryPage(id, librarySymbol(id, `<g><style>${fontRule}</style></g><g/>`), true, use ? `<use href="#${id}-arrow"/>` : '');
    expect(wellFormed(input)).toBe(true);
    expect(childGate(control)).toBe('accepted');
    expect(childGate(input)).toBe('refused');
  });

  // The cap is judged on the output alone. The input is exactly 256 KiB; the removed symbol holds about 5.8 million
  // characters of font copies if grown, while each kept copy under a 2001-character id adds 2000 bytes to the output.
  it.each([
    ['exactly the cap', MAX_SVG_BYTES, 'accepted'],
    ['one byte over the cap', MAX_SVG_BYTES + 1, 'refused'],
  ] as const)('an output of %s (%i bytes) next to a large removed symbol is %s', (_name, outputBytes, expected) => {
    const id = `d${'a'.repeat(2000)}`;
    const kept = (raw: boolean) => `<style>${(raw ? fontRule : movedFontRule(id)).repeat(100)}</style>`;
    const page = (raw: boolean, symbolPad: number, pad: number) => libraryPage(id,
      librarySymbol(id, `<g><style>${fontRule.repeat(2800)}</style></g><desc>${'s'.repeat(symbolPad)}</desc>`), raw,
      `${kept(raw)}<desc>${'p'.repeat(pad)}</desc>`);
    // The final page does not contain the symbol: pad it to the wanted size, then size the symbol so the raw page is
    // exactly the cap. Both pads count one byte per character.
    const pad = outputBytes - byteLength(page(false, 0, 0));
    const symbolPad = MAX_SVG_BYTES - byteLength(page(true, 0, pad));
    const input = page(true, symbolPad, pad);
    const output = page(false, symbolPad, pad);
    expect(Math.min(pad, symbolPad)).toBeGreaterThan(0);
    expect(byteLength(input)).toBe(MAX_SVG_BYTES);
    expect(byteLength(output)).toBe(outputBytes);
    expect(wellFormed(input)).toBe(true);
    expect(childGate(input)).toBe(expected);
    expect(childOutputOf(input)).toBe(expected === 'accepted' ? output : null);
  });

  // ASSET-19 inputs put half of the bytes in the root id and the rest in copies inside a removed symbol. The adapter
  // before the fourth repair grew them (about 14x child time per 4x input in Chromium) and the fourth repair refused
  // them. The child must accept them, and its work must follow the input size.
  it('copies in a removed library symbol under a long root id: child cost grows linearly and the input stays accepted', () => {
    function removedMax(bytes: number): string {
      const shell = libraryPage('', librarySymbol('', '<g><style></style></g>'), true);
      // The id is written five times: root, marker, marker reference, symbol and the kept rule.
      const id = `d${'a'.repeat(Math.floor((bytes - byteLength(shell)) / 2 / 5) - 1)}`;
      const without = byteLength(libraryPage(id, librarySymbol(id, '<g><style></style></g>'), true));
      const copies = Math.floor((bytes - without) / fontRule.length);
      return libraryPage(id, librarySymbol(id, `<g><style>${fontRule.repeat(copies)}</style></g>`), true);
    }
    const small = removedMax(MAX_SVG_BYTES / 4);
    const large = removedMax(MAX_SVG_BYTES);
    expect(byteLength(large)).toBeLessThanOrEqual(MAX_SVG_BYTES);
    expect(large.split(fontRule).length - 1).toBeGreaterThan(1500);
    const timed = (input: string) => {
      const runs: number[] = [];
      let gate: Gate = 'crashed';
      for (let run = 0; run < 3; run++) {
        const started = performance.now();
        gate = childGate(input);
        runs.push(performance.now() - started);
      }
      return { gate, ms: [...runs].sort((a, b) => a - b)[1]! };
    };
    const first = timed(small);
    const second = timed(large);
    console.info('[COST-REVIEW-FIFTH-TIMING]', JSON.stringify({ name: 'copies in a removed library symbol', bytes: [byteLength(small), byteLength(large)], first, second }));
    expect(first.gate).toBe('accepted');
    expect(second.gate).toBe('accepted');
    expect(svgError(childOutputOf(large)!)).toBeNull();
    expect.soft(second.ms / Math.max(first.ms, 5), 'child growth for 4x input').toBeLessThan(8);
  }, 120_000);
});

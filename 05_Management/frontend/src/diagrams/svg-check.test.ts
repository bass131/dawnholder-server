// Independent tests for the parent SVG gate (R-14, design-spec 8.3 "외부 참조·이벤트·foreignObject를 성공 SVG로 허용하지 않는다").
// These hold the security floor that any later repair of representative rendering must keep.
import { describe, expect, it } from 'vitest';
import { svgError } from './svg-check';

const wrap = (body: string, attrs = '') => `<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" viewBox="0 0 100 100"${attrs}>${body}</svg>`;

describe('parent SVG gate', () => {
  // Ordinary rules are anchored to the diagram root (svg#rootId): the approved design msg_1e80f351e41a and
  // design-spec 2.3 keep stylesheet effects inside the diagram, next to the child's return button.
  it('accepts a plain static SVG with internal markers, internal references and ordinary style rules', () => {
    const svg = wrap('<defs><marker id="arrow" viewBox="0 0 10 10"><path d="M0,0 L10,5 L0,10 z"/></marker></defs>' +
      '<style>svg#r .edge{stroke:#715333;stroke-width:2px;fill:none}svg#r text{font-family:"Segoe UI",sans-serif;font-size:16px}</style>' +
      '<g id="d"><path class="edge" d="M0,0 L50,50" marker-end="url(#arrow)"/><text x="5" y="20">시작 <tspan>끝</tspan></text></g>', ' id="r"');
    expect(svgError(svg)).toBeNull();
  });

  it.each([
    ['foreignObject', wrap('<foreignObject><div xmlns="http://www.w3.org/1999/xhtml">x</div></foreignObject>')],
    ['script', wrap('<script>parent.postMessage(1,"*")</script>')],
    ['image', wrap('<image href="https://example.invalid/x.png"/>')],
    ['anchor', wrap('<a href="https://example.invalid/"><text>x</text></a>')],
    ['event attribute', wrap('<rect width="1" height="1" onclick="alert(1)"/>')],
    ['event on root', wrap('<rect width="1" height="1"/>', ' onload="alert(1)"')],
    ['external href', wrap('<use href="https://example.invalid/s.svg#x"/>')],
    ['external xlink:href', wrap('<use xlink:href="file:///C:/x.svg#x"/>')],
    ['dangling internal reference', wrap('<use href="#missing"/>')],
    ['style @import', wrap('<style>@import url(https://example.invalid/x.css);</style>')],
    ['style @font-face', wrap('<style>@font-face{font-family:x;src:url(https://example.invalid/f.woff2)}</style>')],
    ['style external url()', wrap('<style>.a{fill:url(https://example.invalid/p.svg#g)}</style>')],
    ['style data url()', wrap('<style>.a{background:url(data:image/png;base64,AAAA)}</style>')],
    ['style escape obfuscation', wrap('<style>.a{background:u\\72l(https://example.invalid/)}</style>')],
    ['style comment obfuscation', wrap('<style>.a{background:url/**/(https://example.invalid/)}</style>')],
    ['attribute external url()', wrap('<rect width="1" height="1" fill="url(https://example.invalid/#p)"/>')],
    ['attribute style url()', wrap('<rect width="1" height="1" style="fill:url(javascript:alert(1))"/>')],
    ['duplicate id', wrap('<rect id="a" width="1" height="1"/><rect id="a" width="1" height="1"/>')],
    ['doctype', `<!DOCTYPE svg>${wrap('<rect width="1" height="1"/>')}`],
    ['entity', `<!DOCTYPE svg [<!ENTITY x "y">]>${wrap('<text>&x;</text>')}`],
    ['processing instruction', `<?xml-stylesheet href="https://example.invalid/x.css"?>${wrap('<rect width="1" height="1"/>')}`],
    ['not svg root', '<html xmlns="http://www.w3.org/1999/xhtml"><body/></html>'],
    ['malformed', '<svg xmlns="http://www.w3.org/2000/svg"><g></svg>'],
    ['wrong namespace', '<svg xmlns="http://example.invalid/ns"><rect/></svg>'],
    ['empty', '   '],
  ])('rejects %s', (_name, svg) => {
    expect(svgError(svg)).not.toBeNull();
  });

  it('enforces the 256 KiB byte ceiling with UTF-8 bytes, not characters', () => {
    const base = wrap('<text>X</text>');
    const room = 256 * 1024 - new TextEncoder().encode(base).byteLength;
    expect(svgError(base.replace('X', 'a'.repeat(room + 1)))).toBeNull();
    expect(svgError(base.replace('X', 'a'.repeat(room + 2)))).not.toBeNull();
    expect(svgError(base.replace('X', '가'.repeat(Math.ceil(room / 3) + 1)))).not.toBeNull();
  });
});

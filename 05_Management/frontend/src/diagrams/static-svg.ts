import {
  MERMAID_DECORATIONS as decorations,
  MERMAID_KEYFRAMES as keyframes,
  MERMAID_ROOT_FONT_RULE,
  mermaidAnimationRule,
  mermaidRootFontRule,
  mermaidStateStartCircle,
  svgCssError,
  svgDocumentError,
  svgInputError,
} from './svg-contract';
import { MAX_SVG_BYTES } from './policy';

export type StaticDiagram = { svg: string; root: Element };

function unsupported(): never {
  throw new Error('렌더 결과에 지원하지 않는 정적 SVG가 있습니다.');
}

function exactAttributes(node: Element, expected: Record<string, string>): boolean {
  return node.attributes.length === Object.keys(expected).length
    && Object.entries(expected).every(([name, value]) => node.getAttribute(name) === value);
}

function scopeLibraryStylesheet(
  css: string,
  rootFontRule: string,
  rootFont: string,
  reserveExpansion: (length: number) => void,
): string {
  // Called only after the shared raw check has inspected the whole original.
  // Exact library :root font configuration becomes SVG-local, while raw #id
  // selectors gain the SVG type so colliding HTML IDs cannot be their anchor.
  const canonicalFont = css.replaceAll(rootFontRule, rootFont);
  let copies = 0;
  let cursor = 0;
  while (cursor < canonicalFont.length) {
    const next = canonicalFont.indexOf(MERMAID_ROOT_FONT_RULE, cursor);
    if (next < 0) break;
    copies += 1;
    cursor = next + MERMAID_ROOT_FONT_RULE.length;
  }
  // A shared ID is inserted once per bare rule. Charge the whole expanded
  // string before replaceAll creates it, including across separate style nodes.
  const expandedLength = canonicalFont.length
    + copies * (rootFont.length - MERMAID_ROOT_FONT_RULE.length);
  reserveExpansion(expandedLength);
  const localFont = canonicalFont.replaceAll(MERMAID_ROOT_FONT_RULE, rootFont);
  return mapStylesheetRules(localFont, (rule, selectors) => {
    const open = rule.indexOf('{');
    const scoped = selectors.split(',').map(selector => {
      const part = selector.trim();
      return part.startsWith('#') ? `svg${part}` : part;
    });
    return scoped.join(',') + rule.slice(open);
  });
}

// The raw contract has already excluded nesting/braces in values. Consume each
// rule once; an unanchored [^{}]+ replacement retries each suffix on failure.
function mapStylesheetRules(css: string, map: (rule: string, selectors: string, declarations: string) => string): string {
  const pieces: string[] = [];
  let cursor = 0;
  while (cursor < css.length) {
    const open = css.indexOf('{', cursor);
    if (open < 0) {
      pieces.push(css.slice(cursor));
      break;
    }
    const close = css.indexOf('}', open + 1);
    if (close < 0) unsupported();
    pieces.push(map(css.slice(cursor, close + 1), css.slice(cursor, open), css.slice(open + 1, close)));
    cursor = close + 1;
  }
  return pieces.join('');
}

function documentElements(root: Element): {
  nodes: Element[];
  maxDepth: number;
  symbolSubtreeNodes: Set<Element>;
} {
  const walker = root.ownerDocument.createTreeWalker(root, NodeFilter.SHOW_ELEMENT);
  const nodes: Element[] = [];
  const depths = new Map<Element, number>();
  const symbolSubtreeNodes = new Set<Element>();
  let maxDepth = 1;
  let node: Element | null = root;
  while (node) {
    const depth = (node.parentElement ? depths.get(node.parentElement) ?? 0 : 0) + 1;
    depths.set(node, depth);
    // Raw SVG permits indirect styles through g/defs inside a symbol. Mark
    // the whole subtree in this parent-first walk; validation/removal stays below.
    if (node.localName === 'symbol' || (node.parentElement && symbolSubtreeNodes.has(node.parentElement))) {
      symbolSubtreeNodes.add(node);
    }
    maxDepth = Math.max(maxDepth, depth);
    nodes.push(node);
    node = walker.nextNode() as Element | null;
  }
  return { nodes, maxDepth, symbolSubtreeNodes };
}

function unusedRuleQuery(doc: Document, nodeCount: number, maxDepth: number): (selector: string) => Element | null {
  // This is a structural admission budget, not a claim about Chromium's
  // matching complexity or a timer that can interrupt a synchronous query.
  // Length bounds token count; nodes/depth charge ancestor/sibling searches.
  // Consume before EVERY library-rule query; the document shares 2M units.
  let remaining = 2_000_000;
  return selector => {
    const cost = selector.length * nodeCount * maxDepth;
    if (cost > remaining) throw new Error('SVG selector 검사 작업량 한도를 넘습니다.');
    remaining -= cost;
    return doc.querySelector(selector);
  };
}

// Mermaid 12 emits animation and neo decoration definitions for all diagrams.
// This is a narrow static-output adapter, not a sanitizer: unknown output fails,
// and referenced symbols or missing paint servers are never silently discarded.
export function prepareStaticDiagram(svg: string): StaticDiagram {
  if (svgInputError(svg)) unsupported();
  const doc = new DOMParser().parseFromString(svg, 'image/svg+xml');
  const root = doc.documentElement;
  if (svgDocumentError(doc, { mermaid: true })) unsupported();
  const id = root.id;
  const refs: string[] = [];
  const { nodes, maxDepth, symbolSubtreeNodes } = documentElements(root);
  const styles = nodes.filter(node => node.localName === 'style');
  const filters = nodes.filter(node => node.localName === 'filter');
  const symbols = nodes.filter(node => node.localName === 'symbol');
  const ids = new Set(nodes.map(node => node.id));
  const gradientReference = `url(#${id}-gradient)`;
  const hasGradient = ids.has(`${id}-gradient`);
  const queryUnusedRule = unusedRuleQuery(doc, nodes.length, maxDepth);
  // Only retained styles charge this budget. Later scoping trims at most
  // svg.length original whitespace; symbol styles do not survive removal.
  // The final UTF-8 cap is checked separately from this allocation allowance.
  let remainingExpansion = MAX_SVG_BYTES + svg.length;
  const reserveExpansion = (length: number): void => {
    if (length > remainingExpansion) unsupported();
    remainingExpansion -= length;
  };
  const animationRules = [mermaidAnimationRule(id, 'slow', '50'), mermaidAnimationRule(id, 'fast', '20')];
  const animationUsed = ['slow', 'fast'].map(speed => nodes.some(node => node.classList.contains(`edge-animation-${speed}`)));
  const rootFontRule = mermaidRootFontRule(id);
  const rootFont = `svg#${id}${MERMAID_ROOT_FONT_RULE.slice(':root'.length)}`;
  const neoPrefix = `#${id} `;
  let changed = false;

  // The shared raw contract already inspected every original node and value.
  // Collect references solely to decide whether library symbols are unused.
  for (const node of nodes) {
    // The complete raw document passed the shared contract above. These
    // library-only dimensions have no circle geometry effect in final SVG.
    if (mermaidStateStartCircle(node)) {
      node.removeAttribute('width');
      node.removeAttribute('height');
      changed = true;
    }
    for (const attr of Array.from(node.attributes)) {
      if (attr.localName === 'href') refs.push(attr.value.slice(1));
      else if (attr.name !== 'xmlns' && attr.name !== 'xmlns:xlink') svgCssError(attr.value, refs);
    }
  }

  for (const style of styles) {
    if (style.children.length) unsupported();
    let css = style.textContent ?? '';
    for (const frame of keyframes) css = css.replaceAll(frame, '');
    // Exact known keyframes have no input, references or executable content.
    // Everything else, including unused CSS, meets the parent's security floor.
    if (svgCssError(css, refs)) unsupported();
    for (const [index, rule] of animationRules.entries()) {
      if (css.includes(rule)) {
        if (animationUsed[index]) unsupported();
        css = css.replaceAll(rule, '');
      }
    }
    for (const decoration of decorations) css = css.replaceAll(decoration, '');

    // Sequence output carries generic neo rules without their gradient definition.
    // Drop only that paint declaration's whole rule when none of its selectors is used.
    css = mapStylesheetRules(css, (rule, selectors, declarations) => {
      if (hasGradient || !declarations.includes(gradientReference)) return rule;
      const parts = selectors.split(',');
      if (!parts.every(selector => selector.startsWith(neoPrefix) && selector.includes('[data-look="neo"]'))) unsupported();
      // Match against the document: the selector includes the root's own ID.
      // Element-scoped selector engines can omit that ancestor and miss real users.
      if (parts.some(selector => queryUnusedRule(selector))) unsupported();
      return '';
    });
    // Keep reference/use checks and query charges above for every style.
    // Symbol validation below either rejects or removes the entire subtree
    // and sets changed, so its styles need no expansion or text replacement.
    if (symbolSubtreeNodes.has(style)) continue;
    css = scopeLibraryStylesheet(css, rootFontRule, rootFont, reserveExpansion);
    if (css !== style.textContent) {
      style.textContent = css;
      changed = true;
    }
  }

  for (const filter of filters) {
    const small = filter.id === `${id}-drop-shadow-small`;
    const normal = filter.id === `${id}-drop-shadow`;
    const shadow = filter.firstElementChild;
    if ((!small && !normal) || !exactAttributes(filter, { id: filter.id, height: small ? '150%' : '130%', width: small ? '150%' : '130%' }) ||
      filter.children.length !== 1 || !shadow || shadow.localName !== 'feDropShadow' || shadow.children.length || filter.textContent?.trim()) unsupported();
    const flood = shadow.getAttribute('flood-color');
    if (!flood || !['#000000', '#FFFFFF'].includes(flood) || !exactAttributes(shadow, { dx: small ? '2' : '4', dy: small ? '2' : '4', stdDeviation: '0', 'flood-opacity': '0.06', 'flood-color': flood })) unsupported();
    const filterReference = `url(#${filter.id})`;
    for (const node of nodes) {
      if (node.getAttribute('filter') === filterReference) {
        node.removeAttribute('filter');
        changed = true;
      }
    }
    filter.remove(); changed = true;
  }

  const referenceIds = new Set(refs);
  for (const symbol of symbols) {
    const expected = symbol.id === `${id}-database`
      ? { id: symbol.id, 'fill-rule': 'evenodd', 'clip-rule': 'evenodd' }
      : { id: symbol.id, width: '24', height: '24' };
    if (!['computer', 'database', 'clock'].some(name => symbol.id === `${id}-${name}`) || referenceIds.has(symbol.id) ||
      !exactAttributes(symbol, expected)) unsupported();
    // Its descendants were validated before any decoration was removed.
    symbol.remove(); changed = true;
  }

  const result = changed ? new XMLSerializer().serializeToString(root) : svg;
  const error = svgInputError(result) ?? svgDocumentError(doc);
  if (error) throw new Error(error);
  // The string and retained node come from the same XML document. The opaque
  // child moves this node only after approval; it never HTML-parses the string.
  return { svg: result, root };
}

export function staticDiagramSvg(svg: string): string { return prepareStaticDiagram(svg).svg; }

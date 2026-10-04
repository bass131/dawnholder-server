import { byteLength, MAX_SVG_BYTES } from './policy';
import { scopedSelectorCheck } from './css-selectors';

export const SVG_NAMESPACE = 'http://www.w3.org/2000/svg';
export const INSPECTION_STYLE = 'dh-inspection-style';
const XLINK_NAMESPACE = 'http://www.w3.org/1999/xlink';
const XMLNS_NAMESPACE = 'http://www.w3.org/2000/xmlns/';
const identifier = /^[\w-]+$/;
const number = '[+-]?(?:\\d+(?:\\.\\d*)?|\\.\\d+)(?:[eE][+-]?\\d+)?';
const scalar = new RegExp(`^${number}$`);
const length = new RegExp(`^${number}(?:px|pt|em|%)?$`);
const numbers = new RegExp(`^${number}(?:[\\s,]+${number})*$`);
const lengths = new RegExp(`^${number}(?:px|pt|em|%)?(?:[\\s,]+${number}(?:px|pt|em|%)?)*$`);
const font = /^[\w\s,"'-]+$/;
type ValueCheck = (value: string, refs: string[]) => boolean;
function own<T>(table: Record<string, T>, key: string): T | undefined {
  return Object.hasOwn(table, key) ? table[key] : undefined;
}
const matches = (pattern: RegExp): ValueCheck => value => pattern.test(value);
const choices = (...values: string[]): ValueCheck => value => values.includes(value);
const reference: ValueCheck = (value, refs) => {
  const match = /^url\(\s*["']?#([\w-]+)["']?\s*\)$/.exec(value);
  if (!match) return false;
  refs.push(match[1]!);
  return true;
};
const color: ValueCheck = value =>
  /^(?:#[\da-fA-F]{3,8}|[a-zA-Z]+|(?:rgb|rgba|hsl|hsla)\([\d.%,\s/+\-]+\))$/.test(value);
const paint: ValueCheck = (value, refs) => reference(value, refs) || color(value, refs);
const unit: ValueCheck = value => scalar.test(value) && Number(value) >= 0 && Number(value) <= 1;
const size: ValueCheck = value => length.test(value) || ['auto', 'revert'].includes(value);
const transform: ValueCheck = value => {
  const source = value.trim();
  const operation = /(matrix|translate|scale|rotate|skewX|skewY)\(([^()]*)\)\s*/y;
  let cursor = 0;
  // Sticky matching advances one cursor rather than slicing/copying every
  // remaining suffix. Argument/separator scans cover disjoint input segments.
  while (cursor < source.length) {
    operation.lastIndex = cursor;
    const match = operation.exec(source);
    if (!match || !numbers.test(match[2]!.trim())) return false;
    const count = match[2]!.trim().split(/[\s,]+/).length;
    if (!transformArityValid(match[1]!, count)) return false;
    cursor = operation.lastIndex;
  }
  return true;
};

function transformArityValid(name: string, count: number): boolean {
  if (name === 'matrix') return count === 6;
  if (name === 'rotate') return count === 1 || count === 3;
  if (name.startsWith('skew')) return count === 1;
  return count === 1 || count === 2;
}

// This vocabulary covers the three bundled Mermaid outputs and the small static
// SVG contract. Unknown properties fail, including CSS variables used as values.
const properties: Record<string, ValueCheck> = {
  fill: paint, stroke: paint, color, 'background-color': color, background: color,
  'stop-color': color, opacity: unit, 'fill-opacity': unit, 'stroke-opacity': unit, 'stop-opacity': unit,
  'stroke-width': size, 'stroke-dasharray': (v, refs) => v === 'none' || matches(lengths)(v, refs),
  'stroke-dashoffset': matches(length), 'stroke-linecap': choices('butt', 'round', 'square'),
  'stroke-linejoin': choices('miter', 'round', 'bevel'), 'stroke-miterlimit': matches(scalar),
  'fill-rule': choices('nonzero', 'evenodd'), 'clip-rule': choices('nonzero', 'evenodd'),
  'clip-path': (v, refs) => v === 'none' || reference(v, refs),
  'marker-start': reference, 'marker-mid': reference, 'marker-end': reference,
  'font-family': matches(font), '--mermaid-font-family': matches(font),
  'font-size': matches(length), 'font-style': choices('normal', 'italic', 'oblique'),
  'font-weight': matches(/^(?:normal|bold|bolder|lighter|[1-9]00)$/),
  'text-anchor': choices('start', 'middle', 'end'), 'text-align': choices('left', 'right', 'center', 'start', 'end'),
  'dominant-baseline': choices(
    'auto', 'central', 'middle', 'alphabetic', 'hanging', 'text-before-edge', 'text-after-edge',
  ),
  'alignment-baseline': choices('auto', 'baseline', 'central', 'middle', 'alphabetic', 'hanging'),
  display: choices('none', 'inline', 'block', 'inline-block'), visibility: choices('visible', 'hidden', 'collapse'),
  overflow: choices('visible', 'hidden'), 'pointer-events': choices('none', 'auto', 'all', 'visiblePainted'),
  cursor: choices('auto', 'default', 'pointer'), position: choices('absolute', 'relative', 'static'),
  'z-index': matches(/^-?\d+$/), 'vertical-align': matches(length),
  width: size, height: size, 'max-width': size, 'max-height': size, rx: matches(length), ry: matches(length),
  margin: matches(lengths), padding: matches(lengths),
  'border-radius': matches(lengths), 'border-bottom': matches(length),
  border: (value, refs) => {
    const match = /^(\S+) solid (.+)$/.exec(value);
    return !!match && length.test(match[1]!) && color(match[2]!, refs);
  },
};

export const MERMAID_KEYFRAMES = [
  '@keyframes edge-animation-frame{from{stroke-dashoffset:0;}}',
  '@keyframes dash{to{stroke-dashoffset:0;}}',
];
export const MERMAID_ROOT_FONT_RULE = ':root{--mermaid-font-family:"trebuchet ms",verdana,arial,sans-serif;}';
export function mermaidRootFontRule(id: string): string {
  return `#${id} ${MERMAID_ROOT_FONT_RULE}`;
}
export const MERMAID_DECORATIONS = [
  'filter:drop-shadow( 1px 2px 2px rgba(185,185,185,1));',
  'filter:drop-shadow(3px 5px 2px rgb(0 0 0 / 0.4));',
  'box-shadow:0px 8px 16px 0px rgba(0,0,0,0.2);', 'filter:none;',
];
export function mermaidAnimationRule(id: string, speed: string, duration: string): string {
  return `#${id} .edge-animation-${speed}{`
    + 'stroke-dasharray:9,5!important;stroke-dashoffset:900;'
    + `animation:dash ${duration}s linear infinite;`
    + 'stroke-linecap:round;}';
}

export function svgCssError(value: string, refs: string[]): boolean {
  if (/\\|@|(?:https?|file|data|javascript):|\/\*/i.test(value)) return true;
  const withoutUrls = value.replace(/url\(\s*["']?#([\w-]+)["']?\s*\)/g, (_all, ref: string) => {
    refs.push(ref);
    return '';
  });
  return /url\s*\(|expression\s*\(|-moz-binding|behavior\s*:/i.test(withoutUrls);
}

function declarationsValid(css: string, refs: string[]): boolean {
  if (svgCssError(css, [])) return false;
  return css.split(';').every(entry => {
    const declaration = entry.trim();
    if (!declaration) return true;
    const colon = declaration.indexOf(':');
    if (colon < 0) return false;
    const name = declaration.slice(0, colon).trim();
    if (!identifier.test(name)) return false;
    const check = own(properties, name);
    let value = declaration.slice(colon + 1).trim();
    if (value.endsWith('!important')) value = value.slice(0, -10).trimEnd();
    return !!check && check(value, refs);
  });
}

function stylesheetValid(css: string, context: SvgContext): boolean {
  // Exact library-only exceptions are checked even in unused rules, before the
  // adapter removes anything. No other at-rule or effect value enters the DOM.
  if (context.raw) {
    for (const frame of MERMAID_KEYFRAMES) css = css.replaceAll(frame, '');
    if (svgCssError(css, [])) return false;
    for (const rule of context.animationRules) css = css.replaceAll(rule, '');
    for (const decoration of MERMAID_DECORATIONS) css = css.replaceAll(decoration, '');
    // Only this exact library setting has a raw :root exception. The adapter
    // moves it to svg#rootId; final SVG styles never address the HTML root.
    css = css.replaceAll(context.rootFontRule, '').replaceAll(MERMAID_ROOT_FONT_RULE, '');
  }
  if (svgCssError(css, [])) return false;
  let cursor = 0;
  while (cursor < css.length) {
    while (cursor < css.length && /\s/.test(css[cursor]!)) cursor += 1;
    if (cursor === css.length) return true;
    const open = css.indexOf('{', cursor);
    if (open < 0) return false;
    const close = css.indexOf('}', open + 1);
    if (close < 0) return false;
    const selectors = css.slice(cursor, open);
    const declarations = css.slice(open + 1, close);
    if (selectors.includes('}') || declarations.includes('{')) return false;
    if (!context.selectorsValid(selectors)) return false;
    if (!declarationsValid(declarations, context.refs)) return false;
    cursor = close + 1;
  }
  return true;
}

const graphics = [
  'g', 'path', 'rect', 'circle', 'ellipse', 'polygon', 'polyline', 'line', 'text', 'use', 'title', 'desc',
];
const children: Record<string, readonly string[]> = {
  svg: [...graphics, 'defs', 'style', 'marker', 'clipPath', 'linearGradient', 'radialGradient'],
  g: [...graphics, 'defs', 'style', 'marker'],
  defs: ['marker', 'clipPath', 'linearGradient', 'radialGradient', 'style'],
  marker: graphics, clipPath: graphics,
  linearGradient: ['stop'], radialGradient: ['stop'], text: ['tspan'], tspan: ['tspan'],
  title: [], desc: [], style: [], path: [], rect: [], circle: [], ellipse: [],
  polygon: [], polyline: [], line: [], stop: [], use: [],
};
const geometry: Record<string, readonly string[]> = {
  svg: ['width', 'height', 'viewBox', 'preserveAspectRatio'],
  marker: ['viewBox', 'refX', 'refY', 'markerUnits', 'markerWidth', 'markerHeight', 'orient'],
  path: ['d'], rect: ['x', 'y', 'width', 'height', 'rx', 'ry'],
  circle: ['cx', 'cy', 'r'], ellipse: ['cx', 'cy', 'rx', 'ry'],
  polygon: ['points'], polyline: ['points'], line: ['x1', 'x2', 'y1', 'y2'],
  text: ['x', 'y', 'dx', 'dy'], tspan: ['x', 'y', 'dx', 'dy'],
  use: ['x', 'y', 'width', 'height', 'href'], clipPath: ['clipPathUnits'],
  linearGradient: ['x1', 'x2', 'y1', 'y2', 'gradientUnits', 'gradientTransform', 'spreadMethod'],
  radialGradient: ['cx', 'cy', 'r', 'fx', 'fy', 'fr', 'gradientUnits', 'gradientTransform', 'spreadMethod'],
  stop: ['offset'],
  filter: ['width', 'height'], feDropShadow: ['dx', 'dy', 'stdDeviation', 'flood-color', 'flood-opacity'],
  symbol: ['width', 'height'],
};
const metadata = new Set([
  'data-id', 'data-look', 'data-et', 'data-type', 'data-edge', 'data-points', 'data-from', 'data-to',
  'data-label', 'data-style', 'data-a', 'xstyle', 'style-x', 'name',
]);
const textAttributes = new Set(['role', 'aria-roledescription', 'aria-label', 'aria-labelledby', 'aria-describedby']);
const specialValues: Record<string, ValueCheck> = {
  d: matches(/^[MmZzLlHhVvCcSsQqTtAa\d\s,.+eE-]+$/), points: matches(numbers),
  viewBox: value => numbers.test(value) && value.trim().split(/[\s,]+/).length === 4,
  preserveAspectRatio: matches(/^(?:none|x(?:Min|Mid|Max)Y(?:Min|Mid|Max)(?: (?:meet|slice))?)$/),
  markerUnits: choices('userSpaceOnUse', 'strokeWidth'),
  orient: (v, refs) => ['auto', 'auto-start-reverse'].includes(v) || matches(scalar)(v, refs),
  gradientUnits: choices('objectBoundingBox', 'userSpaceOnUse'),
  clipPathUnits: choices('objectBoundingBox', 'userSpaceOnUse'),
  gradientTransform: transform, spreadMethod: choices('pad', 'reflect', 'repeat'), href: (value, refs) => {
    if (!/^#[\w-]+$/.test(value)) return false;
    refs.push(value.slice(1));
    return true;
  },
  offset: matches(length), 'flood-color': color, 'flood-opacity': unit, stdDeviation: matches(numbers),
};

export function svgInputError(svg: string): string | null {
  // UTF-8 bytes are at least the UTF-16 length. Reject obviously oversized
  // input/results before trim or TextEncoder allocates another full copy.
  const invalid = svg.length > MAX_SVG_BYTES
    || !svg.trim()
    || byteLength(svg) > MAX_SVG_BYTES
    || /<!DOCTYPE|<!ENTITY|<\?/i.test(svg)
    || svg.includes(INSPECTION_STYLE);
  return invalid ? 'SVG 형식 또는 256 KiB 한도를 확인하세요.' : null;
}

type SvgOptions = { inspection?: boolean; mermaid?: boolean };
type SvgContext = {
  inspection: boolean;
  raw: boolean;
  rootId: string;
  ids: Set<string>;
  refs: string[];
  selectorsValid: (selectors: string) => boolean;
  animationRules: string[];
  rootFontRule: string;
};

function tagOf(node: Element, inspection: boolean): string {
  if (inspection && node.localName === INSPECTION_STYLE) return 'style';
  return node.localName;
}

function contentModel(tag: string, raw: boolean): readonly string[] | undefined {
  if (raw && tag === 'symbol') return graphics;
  if (raw && tag === 'filter') return ['feDropShadow'];
  if (raw && tag === 'feDropShadow') return [];
  return own(children, tag);
}

function contentError(node: Element, tag: string, model: readonly string[], context: SvgContext): string | null {
  for (const child of Array.from(node.childNodes)) {
    if (child.nodeType === 1) {
      const childTag = tagOf(child as Element, context.inspection);
      const rawDefinition = context.raw && tag === 'defs' && ['symbol', 'filter'].includes(childTag);
      if (!model.includes(childTag) && !rawDefinition) return `지원하지 않는 SVG 내용 모델: ${tag}`;
      continue;
    }
    const textOrComment = [3, 4, 8].includes(child.nodeType);
    const nonemptyText = child.nodeType !== 8 && !!child.textContent?.trim();
    const allowsText = ['text', 'tspan', 'title', 'desc', 'style'].includes(tag);
    if (!textOrComment || (nonemptyText && !allowsText)) return `지원하지 않는 SVG 텍스트: ${tag}`;
  }
  return null;
}

function metadataError(name: string, value: string, refs: string[]): string | null {
  // Explicit inert metadata cannot create a presentation or load effect.
  if (svgCssError(value, [])) return '지원하지 않는 SVG 메타데이터입니다.';
  if (name === 'aria-labelledby' || name === 'aria-describedby') {
    if (!/^[\w\s-]+$/.test(value)) return 'SVG 설명 참조를 확인하세요.';
    for (const ref of value.trim().split(/\s+/)) refs.push(ref);
  }
  return null;
}

const presentationAttribute = /^(?:fill|stroke|stop|font|text|dominant|alignment|clip|marker|opacity|color|display|visibility|overflow|pointer-events)(?:-|$)/;

function presentationValid(tag: string, name: string, value: string, refs: string[]): boolean {
  const actualName = name === 'xlink:href' ? 'href' : name;
  const isGeometry = own(geometry, tag)?.includes(actualName) ?? false;
  if (isGeometry) {
    const check = own(specialValues, actualName) ?? matches(length);
    return check(value, refs);
  }
  // CSS-only box/position/custom properties do not become SVG attributes.
  if (!presentationAttribute.test(actualName)) return false;
  const check = own(properties, actualName);
  return !!check && check(value, refs);
}

// Mermaid's initial marker carries inert box dimensions on a circle. Admit
// only its exact, canonical decimal diameter pair in raw library output.
export function mermaidStateStartCircle(node: Element): boolean {
  if (node.localName !== 'circle' || node.prefix || node.namespaceURI !== SVG_NAMESPACE) return false;
  const markerClass = node.getAttributeNode('class');
  const radius = node.getAttributeNode('r');
  const width = node.getAttributeNode('width');
  const height = node.getAttributeNode('height');
  if (!markerClass || markerClass.namespaceURI || markerClass.value !== 'state-start'
    || !radius || radius.namespaceURI || !width || width.namespaceURI || !height || height.namespaceURI) return false;
  if (!scalar.test(radius.value) || radius.value.includes('e') || radius.value.includes('E')) return false;
  const radiusNumber = Number(radius.value);
  const diameter = radiusNumber * 2;
  return Number.isFinite(radiusNumber) && radiusNumber > 0 && Number.isFinite(diameter)
    && radius.value === String(radiusNumber)
    && width.value === String(diameter) && !width.value.includes('e')
    && height.value === width.value;
}

function attributeError(attr: Attr, tag: string, context: SvgContext, stateStart: boolean): string | null {
  const name = context.inspection && attr.name === INSPECTION_STYLE ? 'style' : attr.name;
  const value = attr.value;
  const refs = context.refs;
  if (name === 'xmlns' || name === 'xmlns:xlink') {
    const expected = name === 'xmlns' ? SVG_NAMESPACE : XLINK_NAMESPACE;
    if (attr.namespaceURI !== XMLNS_NAMESPACE || value !== expected) return 'SVG namespace가 올바르지 않습니다.';
    return null;
  }
  const xlinkHref = attr.namespaceURI === XLINK_NAMESPACE && name === 'xlink:href' && tag === 'use';
  if (attr.namespaceURI && !xlinkHref) return '지원하지 않는 SVG 속성 namespace입니다.';

  if (name === 'id') {
    if (!identifier.test(value) || context.ids.has(value)) return 'SVG ID 형식을 확인하세요.';
    context.ids.add(value);
  } else if (name === 'class') {
    if (!/^[\w\s-]*$/.test(value)) return 'SVG class 형식을 확인하세요.';
  } else if (name === 'style') {
    if (!declarationsValid(value, refs)) return '지원하지 않는 정적 SVG style 속성입니다.';
  } else if (name === 'transform') {
    if (!transform(value, refs)) return 'SVG transform 형식을 확인하세요.';
  } else if (metadata.has(name) || textAttributes.has(name)) {
    return metadataError(name, value, refs);
  } else if (stateStart && (name === 'width' || name === 'height')) {
    return null;
  } else if (context.raw && name === 'filter') {
    if (!reference(value, refs)) return '지원하지 않는 SVG filter입니다.';
    const target = refs.at(-1);
    const knownShadow = target === `${context.rootId}-drop-shadow` || target === `${context.rootId}-drop-shadow-small`;
    if (!knownShadow) return '지원하지 않는 SVG filter입니다.';
  } else if (!presentationValid(tag, name, value, refs)) {
    return `지원하지 않는 정적 SVG 속성: ${name}`;
  }
  return null;
}

// Both callers share case/namespace, content, attribute and CSS checks. Parent
// style names are inspection-only. Raw library exceptions are never final SVG.
export function svgDocumentError(doc: Document, options: SvgOptions = {}): string | null {
  const root = doc.documentElement;
  const invalidRoot = doc.querySelector('parsererror')
    || root.localName !== 'svg'
    || root.prefix
    || root.namespaceURI !== SVG_NAMESPACE;
  if (invalidRoot) return '유효한 SVG 문서가 아닙니다.';

  const context: SvgContext = {
    inspection: options.inspection ?? false,
    raw: options.mermaid ?? false,
    rootId: root.id,
    ids: new Set<string>(),
    refs: [],
    selectorsValid: scopedSelectorCheck(root.id, options.mermaid ?? false),
    animationRules: [mermaidAnimationRule(root.id, 'slow', '50'), mermaidAnimationRule(root.id, 'fast', '20')],
    rootFontRule: mermaidRootFontRule(root.id),
  };
  const walker = doc.createTreeWalker(root, NodeFilter.SHOW_ELEMENT);
  let node: Element | null = root;
  while (node) {
    const tag = tagOf(node, context.inspection);
    const model = contentModel(tag, context.raw);
    if (!model || node.prefix || node.namespaceURI !== SVG_NAMESPACE) return `허용하지 않는 SVG 요소: ${tag}`;
    const childError = contentError(node, tag, model, context);
    if (childError) return childError;
    if (tag === 'style' && !stylesheetValid(node.textContent ?? '', context)) {
      return '렌더 결과에 외부 참조 또는 지원하지 않는 정적 CSS가 있습니다.';
    }
    const stateStart = context.raw && mermaidStateStartCircle(node);
    // Materializing a large NamedNodeMap pays for every Attr before an early refusal.
    for (let index = 0; index < node.attributes.length; index++) {
      const attr = node.attributes.item(index);
      if (!attr) continue;
      const error = attributeError(attr, tag, context, stateStart);
      if (error) return error;
    }
    node = walker.nextNode() as Element | null;
  }
  if (!context.raw && context.refs.some(ref => !context.ids.has(ref))) return 'SVG 내부 참조의 대상이 없습니다.';
  return null;
}

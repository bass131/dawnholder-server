// Independent tests for the child side of the diagram handshake (R-14, design-spec 8.3 render lifetime).
// Mermaid is replaced by a test double, so these check the child's message/display contract, not Mermaid output.
/// <reference types="vite/client" />
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import packetSvg from '../../tests/diagram-static-svg/mermaid-sequence-packet.svg?raw';
import stateSvg from '../../tests/diagram-static-svg/mermaid-state-window.svg?raw';
import { svgError } from './svg-check';

const mermaid = vi.hoisted(() => ({ initialize: vi.fn(), render: vi.fn() }));
vi.mock('mermaid', () => ({ default: mermaid }));

const identity = { requestId: 'request-1', generation: 'generation-1', token: 'token-1' };
const svg = '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 10 10"><rect width="10" height="10"/></svg>';
type Sent = Record<string, unknown> & { type: string };
let sent: Sent[] = [];

// The child document lives for one load. Each test imports a fresh copy, so listeners from earlier copies are removed.
let detach: (() => void)[] = [];
async function loadChild() {
  detach.forEach(remove => remove()); detach = [];
  document.body.innerHTML = '<div id="staging" aria-hidden="true"></div><div id="display" hidden></div><button id="return-to-document" type="button" hidden>문서로 돌아가기 (Esc)</button>';
  vi.resetModules();
  const targets = [window, document] as EventTarget[];
  const originals = targets.map(target => target.addEventListener);
  targets.forEach((target, index) => {
    target.addEventListener = function (this: EventTarget, type: string, listener: EventListenerOrEventListenerObject | null, options?: boolean | AddEventListenerOptions) {
      if (listener) detach.push(() => target.removeEventListener(type, listener, options));
      return originals[index]!.call(this, type, listener, options);
    };
  });
  try { await import('./renderer'); } finally { targets.forEach((target, index) => { target.addEventListener = originals[index]!; }); }
}
const fromParent = (data: unknown, source: Window | null = window) => window.dispatchEvent(new MessageEvent('message', { data, source }));
const settle = async () => { for (let index = 0; index < 10; index++) await Promise.resolve(); };
const display = () => document.getElementById('display')!;
const types = () => sent.map(message => message.type);

beforeEach(() => {
  sent = [];
  mermaid.render.mockReset();
  mermaid.render.mockResolvedValue({ svg });
  vi.spyOn(window, 'postMessage').mockImplementation(((message: Sent) => { sent.push(message); }) as typeof window.postMessage);
  vi.stubGlobal('requestAnimationFrame', (callback: FrameRequestCallback) => { callback(0); return 1; });
});
afterEach(() => { detach.forEach(remove => remove()); detach = []; vi.unstubAllGlobals(); document.body.replaceChildren(); });

describe('diagram child handshake', () => {
  it('initializes Mermaid with strict security, no HTML labels and no automatic start', async () => {
    await loadChild();
    expect(mermaid.initialize).toHaveBeenCalled();
    expect(mermaid.initialize.mock.calls.at(-1)?.[0]).toMatchObject({ startOnLoad: false, securityLevel: 'strict', htmlLabels: false, flowchart: { htmlLabels: false } });
  });

  it('answers only a well-formed init from its parent window and keeps that identity', async () => {
    await loadChild();
    fromParent({ ...identity, type: 'diagram:init' }, null);
    fromParent({ ...identity, type: 'diagram:init', extra: 1 });
    fromParent({ ...identity, token: 'BAD TOKEN', type: 'diagram:init' });
    expect(sent).toEqual([]);
    fromParent({ ...identity, type: 'diagram:init' });
    expect(sent).toEqual([{ ...identity, type: 'diagram:ready' }]);
    fromParent({ requestId: 'request-2', generation: 'generation-2', token: 'token-2', type: 'diagram:init' });
    expect(types()).toEqual(['diagram:ready']);
  });

  it('renders, returns the SVG without displaying it, and displays only after the parent approves', async () => {
    await loadChild();
    fromParent({ ...identity, type: 'diagram:init' });
    fromParent({ ...identity, type: 'diagram:approve' });
    expect(display().hidden).toBe(true);
    fromParent({ ...identity, type: 'diagram:render', source: 'flowchart TD\n  A["시작"] --> B["끝"]', title: '제목', description: '설명' });
    await settle();
    expect(mermaid.render).toHaveBeenCalledOnce();
    expect(mermaid.render.mock.calls[0]?.[1]).toBe('flowchart TD\n  A["시작"] --> B["끝"]');
    expect(sent.at(-1)).toEqual({ ...identity, type: 'diagram:result', svg });
    expect(display().hidden).toBe(true);
    expect(display().innerHTML).toBe('');
    fromParent({ ...identity, requestId: 'request-x', type: 'diagram:approve' });
    expect(display().hidden).toBe(true);
    fromParent({ ...identity, type: 'diagram:approve' });
    expect(display().hidden).toBe(false);
    expect(display().querySelector('svg')).not.toBeNull();
    const shown = sent.at(-1)!;
    expect(shown).toMatchObject({ ...identity, type: 'diagram:shown' });
    expect(shown.height).toBeGreaterThanOrEqual(120);
    expect(shown.height).toBeLessThanOrEqual(720);
    expect(document.getElementById('return-to-document')!.hidden).toBe(false);
  });

  it.each([
    ['a source outside the three allowed kinds', { source: 'graph TD\n  A --> B', description: '설명' }],
    ['a forbidden directive', { source: 'flowchart TD\n  A --> B\n  click A callback', description: '설명' }],
    ['a description over 2 KiB', { source: 'flowchart TD\n  A --> B', description: '가'.repeat(700) }],
  ])('fails %s without calling Mermaid', async (_name, input) => {
    await loadChild();
    fromParent({ ...identity, type: 'diagram:init' });
    fromParent({ ...identity, type: 'diagram:render', title: '제목', ...input });
    await settle();
    expect(mermaid.render).not.toHaveBeenCalled();
    expect(sent.at(-1)).toMatchObject({ ...identity, type: 'diagram:failed' });
  });

  it('reports a Mermaid failure and an oversized SVG as block failures without a result', async () => {
    for (const outcome of ['throw', 'oversize'] as const) {
      sent = [];
      if (outcome === 'throw') mermaid.render.mockRejectedValueOnce(new Error('parse'));
      else mermaid.render.mockResolvedValueOnce({ svg: svg.replace('</svg>', `<desc>${'a'.repeat(256 * 1024)}</desc></svg>`) });
      await loadChild();
      fromParent({ ...identity, type: 'diagram:init' });
      fromParent({ ...identity, type: 'diagram:render', source: 'flowchart TD\n  A --> B', title: '제목', description: '설명' });
      await settle();
      expect(types(), outcome).toEqual(['diagram:ready', 'diagram:failed']);
      fromParent({ ...identity, type: 'diagram:approve' });
      expect(display().hidden, outcome).toBe(true);
    }
  });

  it('asks the parent to return focus only while a diagram is shown', async () => {
    await loadChild();
    fromParent({ ...identity, type: 'diagram:init' });
    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
    expect(types()).toEqual(['diagram:ready']);
    fromParent({ ...identity, type: 'diagram:render', source: 'flowchart TD\n  A --> B', title: '제목', description: '설명' });
    await settle();
    fromParent({ ...identity, type: 'diagram:approve' });
    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
    document.getElementById('return-to-document')!.click();
    expect(types().filter(type => type === 'diagram:focus')).toHaveLength(2);
  });
});

// Independent tests for the static-output repair (ASSET-01/02) and the display ACK repair (ASSET-06).
// The Mermaid double returns real Mermaid 12 output captured from the product child (tests/diagram-static-svg/).
describe('static result and display ACK', () => {
  const start = async (rawSvg = svg) => {
    mermaid.render.mockResolvedValueOnce({ svg: rawSvg });
    await loadChild();
    fromParent({ ...identity, type: 'diagram:init' });
    fromParent({ ...identity, type: 'diagram:render', source: 'sequenceDiagram\n  participant C as 클라이언트\n  C->>C: 요청', title: '제목', description: '설명' });
    await settle();
  };
  const result = () => sent.find(message => message.type === 'diagram:result') as (Sent & { svg: string }) | undefined;
  let measuredHeight = 502;
  let measuredState: { hidden: boolean; svg: number }[] = [];
  beforeEach(() => {
    measuredHeight = 502; measuredState = [];
    // jsdom has no layout. The getter records what was displayed at the moment the child measured.
    Object.defineProperty(document.documentElement, 'scrollHeight', { configurable: true, get: () => {
      measuredState.push({ hidden: display().hidden !== false, svg: display().querySelectorAll('svg').length });
      return measuredHeight;
    } });
  });
  afterEach(() => { delete (document.documentElement as unknown as Record<string, unknown>).scrollHeight; });

  it('sends the parent a static form of real Mermaid output that the parent gate accepts', async () => {
    await start(packetSvg);
    const message = result();
    expect(message).toBeDefined();
    expect(message!.svg).not.toBe(packetSvg);
    expect(svgError(message!.svg)).toBeNull();
    expect(message!.svg).not.toMatch(/@keyframes|<filter|<symbol|drop-shadow/);
    expect(display().innerHTML).toBe('');
  });

  it('fails unsupported output as a block failure without a result and without blaming user CSS', async () => {
    await start(packetSvg.replace('@keyframes dash', '@keyframes spin'));
    expect(types()).toEqual(['diagram:ready', 'diagram:failed']);
    expect(String(sent.at(-1)!.message)).not.toMatch(/사용자 CSS/);
    fromParent({ ...identity, type: 'diagram:approve' });
    expect(display().hidden).toBe(true);
    expect(display().innerHTML).toBe('');
  });

  it('sends shown after approval even when animation frames never run (offscreen iframe)', async () => {
    vi.stubGlobal('requestAnimationFrame', () => 1);
    await start(packetSvg);
    expect(types()).toEqual(['diagram:ready', 'diagram:result']);
    fromParent({ ...identity, type: 'diagram:approve' });
    expect(types()).toEqual(['diagram:ready', 'diagram:result', 'diagram:shown']);
    fromParent({ ...identity, type: 'diagram:approve' });
    expect(types().filter(type => type === 'diagram:shown')).toHaveLength(1);
  });

  it.each([[502, 502], [60, 120], [5000, 720]])('measures height %i after the approved SVG is inserted and visible, reporting %i', async (height, reported) => {
    measuredHeight = height;
    await start(packetSvg);
    measuredState = [];
    fromParent({ ...identity, type: 'diagram:approve' });
    expect(measuredState.length).toBeGreaterThan(0);
    expect(measuredState.every(state => !state.hidden && state.svg === 1)).toBe(true);
    expect(sent.at(-1)).toEqual({ ...identity, type: 'diagram:shown', height: reported });
  });

  // The child moves the XML node it validated and never HTML-parses the approved string (design-spec 8.3; see
  // 'approved XML node ownership and lifetime' below). For this ordinary Mermaid result an HTML parse of the same string
  // serializes the same way, so that parse is only a reference here; strings whose XML and HTML readings differ are
  // judged in the ownership block.
  it('shows the approved XML node, which for an ordinary result serializes like an HTML parse of the approved string', async () => {
    await start(packetSvg);
    const approved = result()!.svg;
    fromParent({ ...identity, type: 'diagram:approve' });
    const expected = document.createElement('div');
    expected.innerHTML = approved;
    expect(display().innerHTML).toBe(expected.innerHTML);
    expect(display().querySelectorAll('svg')).toHaveLength(1);
  });
});

// Independent tests for the parser repair (ASSET-07/08 follow-up): the child keeps the XML node it validated and moves
// it into the display only after approval, and every way the render can end releases that node. The Mermaid double
// stands in for the library; the SVG strings are static inputs whose meaning differs between XML and HTML parsing.
describe('approved XML node ownership and lifetime', () => {
  const SVG = 'http://www.w3.org/2000/svg';
  // XML reads the CDATA as title text. The HTML parser reads `<![CDATA[` in a title (an HTML integration point) as a
  // bogus comment that ends at the first `>`, so the same string would create an <img> element through innerHTML.
  const cdataTitleSvg = `<svg xmlns="${SVG}" id="d" viewBox="0 0 10 10">` +
    '<title><![CDATA[a>b<img src=x onerror=alert(1)>]]></title>' +
    '<defs><marker id="d-arrow" viewBox="0 0 10 10" markerWidth="6" markerHeight="6" refX="5" refY="5"><path d="M0,0 L10,5 L0,10 z"/></marker></defs>' +
    '<style>svg#d .edge{stroke:#715333;fill:none}</style>' +
    '<path class="edge" d="M0,5 L9,5" marker-end="url(#d-arrow)" style="stroke-width:2px"/></svg>';
  const source = 'flowchart TD\n  A["시작"] --> B["끝"]';
  const result = () => sent.find(message => message.type === 'diagram:result') as (Sent & { svg: string }) | undefined;
  const staging = () => document.getElementById('staging')!;

  type Deferred = { resolve: (value: { svg: string }) => void; reject: (error: Error) => void };
  // Mermaid draws into the staging container while it renders; the double leaves a node there the same way.
  function deferRender(): Deferred {
    const deferred = {} as Deferred;
    mermaid.render.mockImplementationOnce((_id: string, _source: string, container: HTMLElement) => {
      container.append(document.createElement('div'));
      return new Promise((resolve, reject) => {
        deferred.resolve = resolve;
        deferred.reject = reject;
      });
    });
    return deferred;
  }
  async function startRender(rawSvg?: string) {
    if (rawSvg !== undefined) mermaid.render.mockResolvedValueOnce({ svg: rawSvg });
    await loadChild();
    fromParent({ ...identity, type: 'diagram:init' });
    fromParent({ ...identity, type: 'diagram:render', source, title: '제목', description: '설명' });
    await settle();
  }
  const pageHide = () => window.dispatchEvent(new Event('pagehide'));

  it('the HTML parser really reads the title CDATA as markup, so the next test can tell the two display paths apart', () => {
    const host = document.createElement('div');
    host.innerHTML = cdataTitleSvg;
    expect(host.querySelector('img')).not.toBeNull();
    expect(svgError(cdataTitleSvg)).toBeNull();
  });

  it('displays the validated XML node: title CDATA stays text, names keep their case and no HTML element appears', async () => {
    await startRender(cdataTitleSvg);
    const approved = result()?.svg;
    expect(approved).toBeDefined();
    fromParent({ ...identity, type: 'diagram:approve' });
    const shown = display().firstElementChild!;
    expect(display().children).toHaveLength(1);
    expect(shown.namespaceURI).toBe(SVG);
    expect(shown.ownerDocument).toBe(document);
    // The parent approved the string; the displayed node is that same output.
    const parsed = new DOMParser().parseFromString(approved!, 'image/svg+xml').documentElement;
    expect(shown.isEqualNode(parsed)).toBe(true);
    expect(document.querySelectorAll('img')).toHaveLength(0);
    const title = shown.querySelector('title')!;
    expect(title.namespaceURI).toBe(SVG);
    expect(title.children).toHaveLength(0);
    expect(title.textContent).toBe('a>b<img src=x onerror=alert(1)>');
    expect(shown.querySelector('marker')!.getAttribute('markerWidth')).toBe('6');
    expect(Array.from(shown.querySelectorAll('*')).every(element => element.namespaceURI === SVG)).toBe(true);
  });

  it('never shows the parent inspection names: the displayed style element and attribute keep their real names', async () => {
    await startRender(cdataTitleSvg);
    fromParent({ ...identity, type: 'diagram:approve' });
    const shown = display().firstElementChild!;
    expect(shown.querySelector('style')!.textContent).toBe('svg#d .edge{stroke:#715333;fill:none}');
    expect(shown.querySelector('path.edge')!.getAttribute('style')).toBe('stroke-width:2px');
    const names = Array.from(shown.querySelectorAll('*')).flatMap(element => [element.localName, ...element.getAttributeNames()]);
    expect(names.filter(name => name.includes('inspection'))).toEqual([]);
  });

  it('empties the staging area once the result is pending and shows nothing before approval', async () => {
    const render = deferRender();
    await startRender();
    expect(staging().childElementCount).toBe(1);
    render.resolve({ svg: cdataTitleSvg });
    await settle();
    expect(types()).toEqual(['diagram:ready', 'diagram:result']);
    expect(staging().childElementCount).toBe(0);
    expect(display().childElementCount).toBe(0);
    expect(display().hidden).toBe(true);
  });

  it('a second approval does not move, copy or replace the displayed node', async () => {
    await startRender(cdataTitleSvg);
    fromParent({ ...identity, type: 'diagram:approve' });
    const first = display().firstElementChild;
    fromParent({ ...identity, type: 'diagram:approve' });
    expect(display().children).toHaveLength(1);
    expect(display().firstElementChild).toBe(first);
    expect(types().filter(type => type === 'diagram:shown')).toHaveLength(1);
  });

  it('page teardown while Mermaid renders discards a late result without any message', async () => {
    const render = deferRender();
    await startRender();
    pageHide();
    render.resolve({ svg: cdataTitleSvg });
    await settle();
    expect(types()).toEqual(['diagram:ready']);
    expect(staging().childElementCount).toBe(0);
    expect(display().childElementCount).toBe(0);
    fromParent({ ...identity, type: 'diagram:approve' });
    expect(display().childElementCount).toBe(0);
    expect(types()).toEqual(['diagram:ready']);
  });

  it('page teardown while Mermaid renders discards a late failure without a failure message', async () => {
    const render = deferRender();
    await startRender();
    pageHide();
    render.reject(new Error('late parse error'));
    await settle();
    expect(types()).toEqual(['diagram:ready']);
    expect(staging().childElementCount).toBe(0);
  });

  it('page teardown after the result releases the pending node: a later approval shows nothing', async () => {
    await startRender(cdataTitleSvg);
    expect(types()).toEqual(['diagram:ready', 'diagram:result']);
    pageHide();
    fromParent({ ...identity, type: 'diagram:approve' });
    expect(display().childElementCount).toBe(0);
    expect(display().hidden).toBe(true);
    expect(types()).toEqual(['diagram:ready', 'diagram:result']);
  });

  it('page teardown after display clears the shown node and stops focus requests', async () => {
    await startRender(cdataTitleSvg);
    fromParent({ ...identity, type: 'diagram:approve' });
    expect(display().childElementCount).toBe(1);
    pageHide();
    expect(display().childElementCount).toBe(0);
    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
    document.getElementById('return-to-document')!.click();
    expect(types().filter(type => type === 'diagram:focus')).toHaveLength(0);
  });

  it('a refused render result keeps nothing: no result, empty staging and display, approval ignored', async () => {
    const refused = cdataTitleSvg.replace('<title>', '<title><tspan>x</tspan>');
    expect(svgError(refused)).not.toBeNull();
    const render = deferRender();
    await startRender();
    render.resolve({ svg: refused });
    await settle();
    expect(types()).toEqual(['diagram:ready', 'diagram:failed']);
    expect(staging().childElementCount).toBe(0);
    fromParent({ ...identity, type: 'diagram:approve' });
    expect(display().childElementCount).toBe(0);
    expect(types()).toEqual(['diagram:ready', 'diagram:failed']);
  });
});

// Design-spec 2.8: diagram text uses the system font at 16 CSS px or more, and no decoration, pixel font or animation.
// The configuration the child hands to Mermaid is the only place those choices enter the library's output.
describe('Mermaid configuration for the representative diagrams', () => {
  type Found = { path: string; value: unknown };
  function collect(value: unknown, keyPattern: RegExp, path = ''): Found[] {
    if (!value || typeof value !== 'object') return [];
    return Object.entries(value as Record<string, unknown>).flatMap(([key, child]) => {
      const here = path ? `${path}.${key}` : key;
      return [...(keyPattern.test(key) ? [{ path: here, value: child }] : []), ...collect(child, keyPattern, here)];
    });
  }

  it('sets every font size to at least 16 px and every font family to the system fonts', async () => {
    await loadChild();
    const config = mermaid.initialize.mock.calls.at(-1)?.[0] as unknown;
    const sizes = collect(config, /fontsize/i);
    const families = collect(config, /fontfamily/i);
    expect(sizes.length).toBeGreaterThan(0);
    expect(families.length).toBeGreaterThan(0);
    for (const { path, value } of sizes) {
      const pixels = typeof value === 'number' ? value : Number(/^(\d+(?:\.\d+)?)px$/.exec(String(value))?.[1]);
      expect(pixels, path).toBeGreaterThanOrEqual(16);
    }
    for (const { path, value } of families) {
      const names = String(value).split(',').map(name => name.trim().replace(/^["']|["']$/g, ''));
      expect(names[0], path).toBe('Segoe UI');
      expect(names, path).toContain('Malgun Gothic');
      expect(String(value), path).not.toMatch(/galmuri|pixel/i);
    }
  });

  it('chooses the plain look, not the hand-drawn or neo decoration', async () => {
    await loadChild();
    const config = mermaid.initialize.mock.calls.at(-1)?.[0] as { look?: unknown };
    expect(config.look).toBe('classic');
  });
});

// ASSET20 through the child flow: a state diagram whose Mermaid output carries the library initial marker
// <circle class="state-start" r="7" width="14" height="14"> (goal 2026-10-04 00:37) must reach the parent as a string
// the parent gate accepts, and the node shown after approval must be that same approved XML, with the marker drawn as
// a circle of radius 7 and no inert box. A marker the library never writes fails the block without a result.
describe('state diagram with the library initial marker in the child flow (ASSET20)', () => {
  const withMarker = (circle: string) => {
    const marked = stateSvg.replace('<g class="nodes">', `<g class="nodes"><g class="node default" id="mark-start" transform="translate(130.48, 16)">${circle}</g>`);
    expect(marked).not.toBe(stateSvg);
    return marked;
  };
  const start = async (rawSvg: string) => {
    mermaid.render.mockResolvedValueOnce({ svg: rawSvg });
    await loadChild();
    fromParent({ ...identity, type: 'diagram:init' });
    fromParent({ ...identity, type: 'diagram:render', source: 'stateDiagram-v2\n  [*] --> A\n  A --> [*]', title: '제목', description: '설명' });
    await settle();
  };

  it('sends a result the parent accepts and shows exactly that XML, with the marker as a plain circle', async () => {
    await start(withMarker('<circle class="state-start" r="7" width="14" height="14"></circle>'));
    expect(types()).toEqual(['diagram:ready', 'diagram:result']);
    const approved = String(sent.at(-1)!.svg);
    expect(svgError(approved)).toBeNull();
    fromParent({ ...identity, type: 'diagram:approve' });
    expect(types()).toEqual(['diagram:ready', 'diagram:result', 'diagram:shown']);
    const shown = display().querySelector('svg');
    expect(shown).not.toBeNull();
    expect(display().querySelectorAll('svg')).toHaveLength(1);
    expect(new XMLSerializer().serializeToString(shown!)).toBe(approved);
    const markers = Array.from(shown!.querySelectorAll('circle.state-start'));
    expect(markers.map(circle => circle.getAttributeNames().sort())).toEqual([['class', 'r']]);
    expect(markers[0]!.getAttribute('r')).toBe('7');
  });

  it.each([
    ['a box that is not twice the radius', '<circle class="state-start" r="7" width="14" height="15"></circle>'],
    ['the box on another class', '<circle class="state-end" r="7" width="14" height="14"></circle>'],
  ])('fails the block without a result for %s', async (_name, circle) => {
    await start(withMarker(circle));
    expect(types()).toEqual(['diagram:ready', 'diagram:failed']);
    fromParent({ ...identity, type: 'diagram:approve' });
    expect(display().hidden).toBe(true);
    expect(display().innerHTML).toBe('');
  });
});

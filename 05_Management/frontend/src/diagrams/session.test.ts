// Independent lifecycle tests for the parent side of diagram display (R-14 boundary, design-spec 8.3 render lifetime).
// jsdom never runs the sandboxed child: the test plays the child by posting from the iframe's own WindowProxy.
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { DiagramBlock } from '../../electron/system-guide-contract';
import { diagramAssets } from '../../electron/diagram-asset-contract';
import { startDiagramDocument, type DiagramState } from './session';

// The parent accepts only rules anchored to the diagram root (svg#rootId, msg_1e80f351e41a), so the stand-in result
// carries a root id like real Mermaid output.
const okSvg = '<svg xmlns="http://www.w3.org/2000/svg" id="d" viewBox="0 0 10 10"><style>svg#d .n{fill:#715333}</style><rect class="n" width="10" height="10"/></svg>';
type Sent = Record<string, unknown> & { type: string; requestId: string; generation: string; token: string };

function block(id: string, source = 'flowchart TD\n  A["시작"] --> B["끝"]'): DiagramBlock {
  return { type: 'diagram', id, format: 'mermaid', title: `${id} 제목`, description: `${id} 대체 설명`, source } as DiagramBlock;
}
function target(id: string, source?: string) {
  const host = document.createElement('div');
  document.body.append(host);
  const states: DiagramState[] = [];
  const focus = vi.fn();
  return { block: block(id, source), host, focus, states, update: (state: DiagramState) => { states.push(state); } };
}
type Target = ReturnType<typeof target>;
const last = (item: Target) => item.states.at(-1);
const flush = async () => { for (let index = 0; index < 10; index++) await Promise.resolve(); };

// Captures what the parent posts into one iframe and lets the test answer as that iframe's child document.
function child(item: Target) {
  const iframe = item.host.querySelector('iframe');
  if (!iframe?.contentWindow) throw new Error('no renderer iframe for this block');
  const sent: Sent[] = [];
  vi.spyOn(iframe.contentWindow, 'postMessage').mockImplementation(((message: Sent) => { sent.push(message); }) as typeof iframe.contentWindow.postMessage);
  const identity = () => { const first = sent[0]; if (!first) throw new Error('parent has not sent init'); return { requestId: first.requestId, generation: first.generation, token: first.token }; };
  const post = (data: unknown, source: Window | null = iframe.contentWindow) => window.dispatchEvent(new MessageEvent('message', { data, source }));
  return {
    iframe, sent,
    load() { iframe.dispatchEvent(new Event('load')); },
    identity,
    reply(type: string, extra: Record<string, unknown> = {}) { post({ ...identity(), type, ...extra }); },
    post,
    types: () => sent.map(message => message.type),
  };
}
async function drive(item: Target, svg = okSvg) {
  const c = child(item);
  c.load(); c.reply('diagram:ready'); c.reply('diagram:result', { svg }); c.reply('diagram:shown', { height: 333 });
  await flush();
  return c;
}

// jsdom has no HTMLIFrameElement.sandbox DOMTokenList. This shim only reflects the attribute so the test can read
// exactly what the parent asked for; real sandbox enforcement is observed in Electron, not here.
if (!('sandbox' in HTMLIFrameElement.prototype)) {
  Object.defineProperty(HTMLIFrameElement.prototype, 'sandbox', {
    configurable: true,
    get(this: HTMLIFrameElement) {
      const tokens = () => (this.getAttribute('sandbox') ?? '').split(/\s+/).filter(Boolean);
      return { add: (...added: string[]) => this.setAttribute('sandbox', [...new Set([...tokens(), ...added])].join(' ')) };
    },
  });
}

beforeEach(() => { vi.useFakeTimers(); vi.spyOn(console, 'info').mockImplementation(() => undefined); });
afterEach(() => { vi.useRealTimers(); document.body.replaceChildren(); });

describe('diagram parent lifecycle', () => {
  it('opens one sandboxed frame at the fixed renderer URL with only allow-scripts', () => {
    const item = target('a');
    const cancel = startDiagramDocument([item]);
    const iframe = item.host.querySelector('iframe')!;
    expect(item.host.children).toHaveLength(1);
    expect(iframe.getAttribute('src')).toBe(diagramAssets.document.url);
    expect(iframe.getAttribute('sandbox')).toBe('allow-scripts');
    expect(iframe.referrerPolicy).toBe('no-referrer');
    expect(iframe.hasAttribute('allow')).toBe(false);
    expect(last(item)?.status).toBe('loading');
    cancel();
  });

  it('succeeds only after result, parent SVG check, approval and the shown ACK, without putting SVG in the parent DOM', async () => {
    const item = target('a');
    const cancel = startDiagramDocument([item]);
    const c = child(item);
    c.load();
    expect(c.sent).toHaveLength(1);
    expect(c.sent[0]).toEqual({ type: 'diagram:init', ...c.identity() });
    for (const value of Object.values(c.identity())) expect(value).toMatch(/^[a-z0-9-]{1,64}$/);
    c.reply('diagram:ready');
    expect(c.sent[1]).toEqual({ ...c.identity(), type: 'diagram:render', source: item.block.source, title: item.block.title, description: item.block.description });
    c.reply('diagram:result', { svg: okSvg });
    expect(c.types()).toEqual(['diagram:init', 'diagram:render', 'diagram:approve']);
    expect(last(item)?.status).toBe('loading');
    expect(document.querySelectorAll('svg')).toHaveLength(0);
    c.reply('diagram:shown', { height: 333 });
    expect(last(item)).toMatchObject({ status: 'success' });
    expect(last(item)?.elapsedMs).toEqual(expect.any(Number));
    expect(item.host.querySelector('iframe')).toBe(c.iframe);
    const height = Number(c.iframe.height);
    expect(height).toBeGreaterThanOrEqual(333);
    expect(height).toBeLessThanOrEqual(720);
    expect(document.querySelectorAll('svg')).toHaveLength(0);
    cancel();
    expect(item.host.querySelector('iframe')).toBeNull();
  });

  it('ignores messages from another window, another identity, the wrong stage or with extra fields', async () => {
    const item = target('a');
    const cancel = startDiagramDocument([item]);
    const c = child(item);
    c.load();
    const id = c.identity();
    c.reply('diagram:result', { svg: okSvg });
    c.reply('diagram:shown', { height: 200 });
    c.post({ ...id, type: 'diagram:ready' }, window);
    c.post({ ...id, type: 'diagram:ready' }, null);
    c.post({ ...id, token: 'other-token', type: 'diagram:ready' });
    c.post({ ...id, requestId: 'other-request', type: 'diagram:ready' });
    c.post({ ...id, generation: 'other-generation', type: 'diagram:ready' });
    c.post({ ...id, type: 'diagram:ready', extra: true });
    c.post(JSON.stringify({ ...id, type: 'diagram:ready' }));
    expect(c.types()).toEqual(['diagram:init']);
    c.reply('diagram:ready');
    c.reply('diagram:ready');
    expect(c.types()).toEqual(['diagram:init', 'diagram:render']);
    c.reply('diagram:shown', { height: 200 });
    c.reply('diagram:result', { svg: okSvg, extra: 1 });
    c.reply('diagram:result', { svg: 7 });
    expect(c.types()).toEqual(['diagram:init', 'diagram:render']);
    c.reply('diagram:result', { svg: okSvg });
    for (const height of [119, 721, Number.NaN, '300']) c.reply('diagram:shown', { height });
    expect(last(item)?.status).toBe('loading');
    c.reply('diagram:shown', { height: 120 });
    expect(last(item)?.status).toBe('success');
    cancel();
  });

  it('rejects an SVG that fails the parent check without approval, removes the frame and continues with the next block', async () => {
    const first = target('a');
    const second = target('b');
    const cancel = startDiagramDocument([first, second]);
    const c = child(first);
    c.load(); c.reply('diagram:ready');
    c.reply('diagram:result', { svg: '<svg xmlns="http://www.w3.org/2000/svg"><foreignObject><div xmlns="http://www.w3.org/1999/xhtml">x</div></foreignObject></svg>' });
    expect(c.types()).not.toContain('diagram:approve');
    expect(last(first)?.status).toBe('failed');
    expect(first.host.querySelector('iframe')).toBeNull();
    await flush();
    expect(second.host.querySelector('iframe')).not.toBeNull();
    await drive(second);
    expect(last(second)?.status).toBe('success');
    cancel();
  });

  it('shows the child failure message for that block and continues with the next block', async () => {
    const first = target('a');
    const second = target('b');
    const cancel = startDiagramDocument([first, second]);
    const c = child(first);
    c.load(); c.reply('diagram:ready');
    c.reply('diagram:failed', { message: '도식 문법 실패' });
    expect(last(first)).toMatchObject({ status: 'failed', message: '도식 문법 실패' });
    expect(first.host.querySelector('iframe')).toBeNull();
    await flush();
    expect(second.host.querySelector('iframe')).not.toBeNull();
    cancel();
  });

  it('counts five seconds from load start to the final ACK, and a late ACK neither succeeds nor blocks the next block', async () => {
    const first = target('a');
    const second = target('b');
    const cancel = startDiagramDocument([first, second]);
    const c = child(first);
    vi.advanceTimersByTime(3000); c.load();
    vi.advanceTimersByTime(1000); c.reply('diagram:ready');
    vi.advanceTimersByTime(500); c.reply('diagram:result', { svg: okSvg });
    vi.advanceTimersByTime(499);
    expect(last(first)?.status).toBe('loading');
    vi.advanceTimersByTime(1);
    expect(last(first)?.status).toBe('failed');
    expect(last(first)?.message).toContain('5초');
    expect(first.host.querySelector('iframe')).toBeNull();
    c.reply('diagram:shown', { height: 200 });
    expect(last(first)?.status).toBe('failed');
    await flush();
    expect(second.host.querySelector('iframe')).not.toBeNull();
    expect(last(second)?.status).toBe('loading');
    cancel();
  });

  it('treats a renderer document that never becomes ready as a document failure and fails the rest of the generation at once', async () => {
    const items = [target('a'), target('b'), target('c')];
    const cancel = startDiagramDocument(items);
    const c = child(items[0]!);
    c.load();
    vi.advanceTimersByTime(4999);
    expect(items.map(item => last(item)?.status)).toEqual(['loading', undefined, undefined]);
    vi.advanceTimersByTime(1);
    await flush();
    expect(items.map(item => last(item)?.status)).toEqual(['failed', 'failed', 'failed']);
    expect(items.map(item => item.host.querySelector('iframe'))).toEqual([null, null, null]);
    vi.advanceTimersByTime(20_000);
    await flush();
    expect(items.map(item => item.states.length)).toEqual([2, 1, 1]);
    cancel();
  });

  it('treats a frame error event as a document failure for the rest of the generation', async () => {
    const items = [target('a'), target('b')];
    const cancel = startDiagramDocument(items);
    items[0]!.host.querySelector('iframe')!.dispatchEvent(new Event('error'));
    await flush();
    expect(items.map(item => last(item)?.status)).toEqual(['failed', 'failed']);
    expect(items[1]!.host.querySelector('iframe')).toBeNull();
    cancel();
  });

  it('cancel during rendering removes the frame, listener and timer, discards late results and starts nothing else', async () => {
    const first = target('a');
    const second = target('b');
    const cancel = startDiagramDocument([first, second]);
    const c = child(first);
    c.load(); c.reply('diagram:ready');
    const before = first.states.length;
    cancel();
    expect(first.host.querySelector('iframe')).toBeNull();
    c.reply('diagram:result', { svg: okSvg });
    c.reply('diagram:shown', { height: 200 });
    vi.advanceTimersByTime(10_000);
    await flush();
    expect(c.types()).toEqual(['diagram:init', 'diagram:render']);
    expect(first.states).toHaveLength(before);
    expect(second.states).toHaveLength(0);
    expect(second.host.querySelector('iframe')).toBeNull();
  });

  it('cancel after display removes shown frames, and the child focus request works only while shown', async () => {
    const item = target('a');
    const cancel = startDiagramDocument([item]);
    const c = child(item);
    c.load(); c.reply('diagram:ready');
    c.reply('diagram:focus');
    expect(item.focus).not.toHaveBeenCalled();
    c.reply('diagram:result', { svg: okSvg }); c.reply('diagram:shown', { height: 200 });
    c.reply('diagram:focus');
    expect(item.focus).toHaveBeenCalledOnce();
    cancel();
    expect(item.host.querySelector('iframe')).toBeNull();
    c.reply('diagram:focus');
    expect(item.focus).toHaveBeenCalledOnce();
  });

  it('fails a policy-rejected source without opening a renderer and continues', async () => {
    const first = target('a', 'graph TD\n  A --> B');
    const second = target('b');
    const cancel = startDiagramDocument([first, second]);
    expect(first.host.querySelector('iframe')).toBeNull();
    expect(last(first)?.status).toBe('failed');
    await flush();
    expect(second.host.querySelector('iframe')).not.toBeNull();
    cancel();
  });

  it('gives each generation a new identity and ignores messages addressed to the previous generation', async () => {
    const old = target('a');
    const cancelOld = startDiagramDocument([old]);
    const oldChild = child(old); oldChild.load();
    const oldIdentity = oldChild.identity();
    cancelOld();
    const next = target('a');
    const cancelNext = startDiagramDocument([next]);
    const nextChild = child(next); nextChild.load();
    expect(nextChild.identity().generation).not.toBe(oldIdentity.generation);
    expect(nextChild.identity().token).not.toBe(oldIdentity.token);
    nextChild.post({ ...oldIdentity, type: 'diagram:ready' });
    nextChild.post({ ...nextChild.identity(), generation: oldIdentity.generation, type: 'diagram:ready' });
    expect(nextChild.types()).toEqual(['diagram:init']);
    cancelNext();
  });
});

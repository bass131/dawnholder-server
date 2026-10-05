import type { DiagramBlock } from '../../electron/system-guide-contract';
import { diagramAssets } from '../../electron/diagram-asset-contract';
import { DIAGRAM_TIMEOUT_MS, diagramPolicyError } from './policy';
import { isChildMessage, type DiagramIdentity, type ParentMessage } from './protocol';
import { svgError } from './svg-check';

export type DiagramState = { status: 'waiting' | 'loading' | 'success' | 'failed'; message: string; elapsedMs?: number };
export interface DiagramTarget { block: DiagramBlock; host: HTMLElement; focus(): void; update(state: DiagramState): void; }
export const rendererUrl = diagramAssets.document.url;

// One owner per document generation. Cancel removes every listener, timer and iframe.
export function startDiagramDocument(targets: DiagramTarget[]): () => void {
  const generation = crypto.randomUUID();
  let cancelled = false;
  let currentCleanup: (() => void) | null = null;
  let shownCleanups: (() => void)[] = [];
  async function render(target: DiagramTarget): Promise<'done' | 'document-failed' | 'cancelled'> {
    const policy = diagramPolicyError(target.block.source);
    if (policy) { target.update({ status: 'failed', message: policy }); return 'done'; }
    const identity: DiagramIdentity = { generation, requestId: crypto.randomUUID(), token: crypto.randomUUID() };
    const iframe = document.createElement('iframe');
    iframe.sandbox.add('allow-scripts');
    iframe.title = `${target.block.title} · 도식`;
    iframe.className = 'diagram-frame';
    iframe.height = '120';
    iframe.referrerPolicy = 'no-referrer';
    const started = performance.now();
    target.update({ status: 'loading', message: '격리된 도식 문서에서 읽는 중입니다.' });
    return new Promise(resolve => {
      let stage: 'load' | 'ready' | 'render' | 'approve' | 'shown' = 'load';
      let settled = false;
      const send = (message: ParentMessage) => iframe.contentWindow?.postMessage(message, '*');
      function cleanup() {
        clearTimeout(timer); iframe.removeEventListener('load', loaded); iframe.removeEventListener('error', loadError); window.removeEventListener('message', receive); iframe.remove();
      }
      function finish(outcome: 'done' | 'document-failed' | 'cancelled', error?: string) {
        if (settled) return;
        settled = true; clearTimeout(timer); currentCleanup = null;
        if (error) target.update({ status: 'failed', message: error });
        if (stage !== 'shown') cleanup();
        resolve(outcome);
      }
      function loadError() { finish('document-failed', 'renderer 문서를 열지 못했습니다. 같은 문서의 남은 도식도 표시하지 않습니다.'); }
      function loaded() {
        if (stage !== 'load' || cancelled) return;
        stage = 'ready'; send({ ...identity, type: 'diagram:init' });
      }
      function receive(event: MessageEvent<unknown>) {
        if (cancelled || event.source !== iframe.contentWindow || !isChildMessage(event.data, identity)) return;
        const message = event.data;
        if (stage === 'shown' && message.type === 'diagram:focus') { target.focus(); return; }
        if (settled) return;
        if (stage === 'ready' && message.type === 'diagram:ready') {
          stage = 'render'; send({ ...identity, type: 'diagram:render', source: target.block.source, title: target.block.title, description: target.block.description });
        } else if (stage === 'render' && message.type === 'diagram:result') {
          const error = svgError(message.svg);
          if (error) { finish('done', error); return; }
          stage = 'approve'; send({ ...identity, type: 'diagram:approve' });
        } else if (stage === 'approve' && message.type === 'diagram:shown') {
          stage = 'shown';
          const height = Math.min(720, Math.max(120, Math.ceil(message.height / 8) * 8));
          iframe.height = String(height);
          const elapsedMs = performance.now() - started;
          target.update({ status: 'success', message: '도식 표시 완료', elapsedMs });
          console.info('[diagram] final-ack', JSON.stringify({ id: target.block.id, generation, requestId: identity.requestId, elapsedMs, height, sourceBytes: new TextEncoder().encode(target.block.source).byteLength }));
          shownCleanups.push(cleanup); finish('done');
        } else if ((stage === 'render' || stage === 'approve') && message.type === 'diagram:failed') finish('done', message.message);
      }
      const timer = window.setTimeout(() => finish(stage === 'load' || stage === 'ready' ? 'document-failed' : 'done', `5초 안에 ${stage === 'load' || stage === 'ready' ? 'renderer 문서의 준비' : '도식 표시 완료 ACK'}를 받지 못했습니다.`), DIAGRAM_TIMEOUT_MS);
      currentCleanup = () => { cleanup(); finish('cancelled'); };
      window.addEventListener('message', receive);
      iframe.addEventListener('load', loaded); iframe.addEventListener('error', loadError);
      console.info('[diagram] load-start', JSON.stringify({ id: target.block.id, ...identity, at: started }));
      iframe.src = rendererUrl; target.host.replaceChildren(iframe);
    });
  }
  void (async () => {
    for (let index = 0; index < targets.length; index++) {
      if (cancelled) break;
      const target = targets[index]!;
      const outcome = await render(target);
      if (outcome === 'document-failed') {
        targets.slice(index + 1).forEach(item => item.update({ status: 'failed', message: 'renderer 문서 자체의 로드 실패로 같은 세대의 나머지 도식을 표시하지 않습니다.' })); break;
      }
    }
  })();
  return () => { cancelled = true; currentCleanup?.(); currentCleanup = null; shownCleanups.forEach(cleanup => cleanup()); shownCleanups = []; };
}

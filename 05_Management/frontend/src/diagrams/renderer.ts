import mermaid from 'mermaid';
import { byteLength, diagramPolicyError, MAX_SVG_BYTES } from './policy';
import { isParentMessage } from './protocol';
import type { DiagramIdentity, ChildMessage } from './protocol';
import { prepareStaticDiagram } from './static-svg';
import type { StaticDiagram } from './static-svg';

const staging = document.getElementById('staging')!;
const display = document.getElementById('display')!;
const returnButton = document.getElementById('return-to-document') as HTMLButtonElement;
let identity: DiagramIdentity | undefined;
let phase: 'initial' | 'ready' | 'rendering' | 'pending' | 'shown' | 'failed' = 'initial';
let pendingDiagram: StaticDiagram | undefined;
const systemFont = 'Segoe UI, Malgun Gothic, sans-serif';
mermaid.initialize({
  startOnLoad: false,
  securityLevel: 'strict',
  htmlLabels: false,
  theme: 'base',
  look: 'classic',
  layout: 'dagre',
  maxTextSize: 4096,
  suppressErrorRendering: true,
  themeVariables: {
    fontFamily: systemFont, fontSize: '16px', background: '#fffaf0',
    primaryColor: '#f3e9d2', primaryTextColor: '#2a231b',
    primaryBorderColor: '#715333', lineColor: '#715333', secondaryColor: '#e5f0e9', tertiaryColor: '#fff3c4',
    edgeLabelBackground: '#fffaf0', actorBkg: '#f3e9d2', actorBorder: '#715333', actorTextColor: '#2a231b',
    signalColor: '#155c57', signalTextColor: '#2a231b',
    noteBkgColor: '#fff3c4', noteBorderColor: '#715333', noteTextColor: '#2a231b',
  },
  flowchart: {
    htmlLabels: false, nodeSpacing: 24, rankSpacing: 24, padding: 8,
    wrappingWidth: 280, curve: 'linear', useMaxWidth: true,
  },
  // A self-message has zero horizontal span: right alignment keeps its label
  // to the left of the actor lifeline without changing the message order.
  sequence: {
    useMaxWidth: true, actorMargin: 64, messageMargin: 32, mirrorActors: false,
    messageAlign: 'right', actorFontSize: 16, messageFontSize: 16, noteFontSize: 16,
    actorFontFamily: systemFont, messageFontFamily: systemFont, noteFontFamily: systemFont,
  },
  state: { useMaxWidth: true, padding: 8, rankSpacing: 32 },
});

function send(message: ChildMessage): void {
  parent.postMessage(message, '*');
}

function clearDiagram(): void {
  pendingDiagram = undefined;
  staging.replaceChildren();
  display.replaceChildren();
}

function failed(message: string) {
  phase = 'failed';
  clearDiagram();
  if (identity) send({ ...identity, type: 'diagram:failed', message });
}

async function renderDiagram(source: string, renderIdentity: DiagramIdentity): Promise<void> {
  try {
    const result = await mermaid.render(`diagram-${renderIdentity.requestId}`, source, staging);
    if (phase !== 'rendering' || identity !== renderIdentity) {
      clearDiagram();
      return;
    }
    if (byteLength(result.svg) > MAX_SVG_BYTES) {
      failed('SVG가 256 KiB를 넘습니다.');
      return;
    }
    try {
      pendingDiagram = prepareStaticDiagram(result.svg);
    } catch {
      failed('렌더 결과를 지원하는 정적 SVG로 확인하지 못했습니다. 원천과 대체 설명은 보존됩니다.');
      return;
    }
    phase = 'pending';
    staging.replaceChildren();
    send({ ...renderIdentity, type: 'diagram:result', svg: pendingDiagram.svg });
  } catch {
    if (phase === 'rendering' && identity === renderIdentity) {
      failed('도식 문법 또는 격리된 렌더에 실패했습니다. 원천과 대체 설명을 확인하세요.');
    } else {
      clearDiagram();
    }
  }
}

function showDiagram(approvedIdentity: DiagramIdentity): void {
  // The approved string and retained XML node are paired inside this opaque
  // child. Only this node is moved; no HTML parse or DOM postMessage occurs.
  if (!pendingDiagram) {
    failed('승인할 SVG 노드가 없습니다.');
    return;
  }
  display.replaceChildren(pendingDiagram.root);
  pendingDiagram = undefined;
  staging.replaceChildren();
  display.hidden = false;
  returnButton.hidden = false;
  phase = 'shown';
  // Offscreen iframes can suspend RAF. scrollHeight forces actual layout;
  // the parent still owns the five-second limit until it receives this ACK.
  const height = Math.min(720, Math.max(120, document.documentElement.scrollHeight));
  send({ ...approvedIdentity, type: 'diagram:shown', height });
}

// The parent cancels/replaces a generation by destroying its iframe. Release
// pending XML nodes on document teardown, and ignore an in-flight render later.
window.addEventListener('pagehide', () => {
  phase = 'failed';
  identity = undefined;
  clearDiagram();
});
returnButton.addEventListener('click', () => {
  if (phase === 'shown' && identity) send({ ...identity, type: 'diagram:focus' });
});
document.addEventListener('keydown', event => {
  if (event.key !== 'Escape' || phase !== 'shown' || !identity) return;
  event.preventDefault();
  send({ ...identity, type: 'diagram:focus' });
});

window.addEventListener('message', event => {
  if (event.source !== parent || !isParentMessage(event.data, identity)) return;
  const message = event.data;
  if (phase === 'initial' && message.type === 'diagram:init') {
    identity = {
      requestId: message.requestId,
      generation: message.generation,
      token: message.token,
    };
    phase = 'ready';
    send({ ...identity, type: 'diagram:ready' });
  } else if (phase === 'ready' && identity && message.type === 'diagram:render') {
    const policy = diagramPolicyError(message.source);
    if (policy || byteLength(message.description) > 2048) {
      failed(policy ?? '대체 설명이 한도를 넘습니다.');
      return;
    }
    phase = 'rendering';
    void renderDiagram(message.source, identity);
  } else if (phase === 'pending' && identity && message.type === 'diagram:approve') {
    showDiagram(identity);
  }
});

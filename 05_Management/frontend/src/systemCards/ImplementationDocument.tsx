import { useEffect, useRef, useState } from 'react';
import type { DiagramBlock, ImplementationDocument as DocumentData, SystemCard } from '../../electron/system-guide-contract';
import { startDiagramDocument, type DiagramState } from '../diagrams/session';

function Diagram({ block, host, heading, state }: { block: DiagramBlock; host: (node: HTMLDivElement | null) => void; heading: (node: HTMLHeadingElement | null) => void; state: DiagramState }) {
  const [copyMessage, setCopyMessage] = useState('');
  const sourceField = useRef<HTMLTextAreaElement>(null);
  async function copy() {
    const previousFocus = document.activeElement;
    sourceField.current?.focus(); sourceField.current?.select();
    const copied = document.execCommand('copy');
    if (previousFocus instanceof HTMLElement) previousFocus.focus();
    try { if (!copied) await navigator.clipboard.writeText(block.source); setCopyMessage('원천을 복사했습니다.'); }
    catch { setCopyMessage('복사하지 못했습니다. 원천 텍스트를 선택해 복사하세요.'); }
  }
  return <figure className="diagram-block" aria-busy={state.status === 'waiting' || state.status === 'loading'}>
    <h5 ref={heading} tabIndex={-1}>{block.title}</h5>
    <p className={`diagram-status ${state.status === 'failed' ? 'record-missing' : ''}`} role="status">{state.message}</p>
    <div className="diagram-host" ref={host} />
    <figcaption><strong>한국어 대체 설명</strong><p>{block.description}</p></figcaption>
    <details><summary>도식 원천 펼치기</summary><pre><code>{block.source}</code></pre><textarea ref={sourceField} value={block.source} readOnly aria-hidden="true" tabIndex={-1} className="visually-hidden" /><button type="button" onClick={() => void copy()}>원천 복사</button><p role="status">{copyMessage}</p></details>
  </figure>;
}

export default function ImplementationDocument({ document: doc, card, active }: { document: DocumentData; card: SystemCard; active: boolean }) {
  const hosts = useRef(new Map<string, HTMLElement>());
  const headings = useRef(new Map<string, HTMLHeadingElement>());
  const [states, setStates] = useState<Record<string, DiagramState>>({});
  useEffect(() => {
    setStates({});
    if (!active) return;
    const blocks = doc.sections.flatMap(section => section.blocks.filter((block): block is DiagramBlock => block.type === 'diagram'));
    return startDiagramDocument(blocks.flatMap(block => {
      const host = hosts.current.get(block.id);
      return host ? [{ block, host, focus: () => headings.current.get(block.id)?.focus(), update: (state: DiagramState) => setStates(previous => ({ ...previous, [block.id]: state })) }] : [];
    }));
  }, [doc, active]);
  return <article className="implementation-document ledger-page">
    <div className="document-note">구현 설명 · 원천을 읽은 해설이며 실행 검증 결과가 아닙니다.</div>
    <p className="document-summary">{card.summary}</p>
    <div className="document-baseline"><span>코드 있음 · 기능 완성이나 검증 통과를 뜻하지 않습니다.</span><details><summary>문서 기준 commit {doc.sourceCommit.slice(0, 7)} · 전체 SHA 보기</summary><code>{doc.sourceCommit}</code></details><span>자동 갱신 안 됨</span></div>
    <nav className="document-toc" aria-label="구현 문서 목차">{doc.sections.map(section => <a key={section.id} href={`#section-${section.id}`} onClick={event => { event.preventDefault(); const target = document.getElementById(`section-${section.id}`); target?.focus(); target?.scrollIntoView({ block: 'start' }); }}>{section.title}</a>)}</nav>
    {doc.sections.map(section => <section className="document-section" key={section.id} aria-labelledby={`section-${section.id}`}>
      <h4 id={`section-${section.id}`} tabIndex={-1}>{section.title}</h4>
      {section.blocks.map((block, index) => block.type === 'paragraph' ? <p key={index}>{block.text}</p> : <Diagram key={block.id} block={block}
        host={node => { if (node) hosts.current.set(block.id, node); else hosts.current.delete(block.id); }}
        heading={node => { if (node) headings.current.set(block.id, node); else headings.current.delete(block.id); }}
        state={states[block.id] ?? { status: 'waiting', message: '앞선 도식 표시를 기다리는 중입니다.' }} />)}
    </section>)}
    <section className="document-section"><h4>코드 매핑과 작성 원천</h4><p>코드 보기 기능은 후속 작업입니다. 아래 경로와 역할은 텍스트로 읽을 수 있습니다.</p>
      <table className="mapping-table"><thead><tr><th scope="col">경로</th><th scope="col">역할</th><th scope="col">종류</th></tr></thead><tbody>{card.codeReference?.mappings.map(mapping => <tr key={mapping.path}><td><code>{mapping.path}</code>{mapping.namespace && <p>{mapping.namespace}</p>}</td><td>{mapping.role}</td><td>{mapping.kind}</td></tr>)}</tbody></table>
      <h5>문서 sourceRefs</h5><ul>{doc.sourceRefs.map(ref => <li key={`${ref.path}:${ref.symbol}`}><code>{ref.path}</code><p>{ref.symbol}</p></li>)}</ul>
    </section>
  </article>;
}

import { useEffect, useRef, useState } from 'react';
import type { GuideResult, SystemCard } from '../../electron/system-guide-contract';
import { searchGuide } from './search';
import { parentLocation, rootLocation, type GuideLocation } from './navigation';
import ThemeImage from '../theme/ThemeImage';
import ImplementationDocument from './ImplementationDocument';

export default function SystemCardsView({ active, openRecords }: { active: boolean; openRecords(): void }) {
  const [result, setResult] = useState<GuideResult | null>(null);
  const [loading, setLoading] = useState(true);
  const [location, setLocation] = useState<GuideLocation>(rootLocation);
  const [catalogIds, setCatalogIds] = useState<Set<string> | null>(null);
  const [catalogVersion, setCatalogVersion] = useState('');
  const [history, setHistory] = useState<GuideLocation[]>([]);
  const request = useRef(0);
  const heading = useRef<HTMLHeadingElement>(null);
  const searchInput = useRef<HTMLInputElement>(null);
  const focusId = useRef<string | null>(null);

  async function reload() {
    const generation = ++request.current;
    setLoading(true); setResult(null);
    if (!window.systemGuide) { setResult({ ok: false, code: 'load', message: '이 화면에는 카드 자료의 Electron 파일 연결이 없습니다.' }); setLoading(false); return; }
    try {
      const next = await window.systemGuide.readGuide();
      if (request.current === generation) { setResult(next); setLoading(false); setLocation(rootLocation); setHistory([]); }
    } catch { if (request.current === generation) { setResult({ ok: false, code: 'load', message: '카드 자료 읽기 응답을 받지 못했습니다.' }); setLoading(false); } }
  }
  useEffect(() => {
    void reload();
    let alive = true;
    void window.systemRecords?.readCatalog().then(catalog => { if (alive) { setCatalogIds(catalog.ok ? new Set(catalog.catalog.systems.map(system => system.id)) : null); setCatalogVersion(catalog.ok ? catalog.version : ''); } }).catch(() => { if (alive) setCatalogIds(null); });
    return () => { alive = false; ++request.current; };
  }, []);
  const guide = !loading && result?.ok ? result.guide : null;
  const roots = guide?.cards.filter(card => card.parentId === null) ?? [];
  const parent = guide?.cards.find(card => card.id === location.parentId);
  const card = guide?.cards.find(item => item.id === location.cardId);
  const doc = guide?.documents.find(item => item.id === card?.documentId);
  const searching = !!location.query.trim() && !parent && !card;
  const cards = guide ? searching ? searchGuide(guide, location.query) : parent ? guide.cards.filter(item => item.parentId === parent.id) : roots : [];
  const title = doc?.title ?? (searching ? `“${location.query}” 검색 결과` : parent?.title ?? '전체 시스템');

  useEffect(() => {
    if (!active || loading || (document.activeElement === searchInput.current && focusId.current === null)) return;
    const id = focusId.current; focusId.current = null;
    const target = id ? document.getElementById(`card-${id}`) : heading.current;
    (target ?? heading.current)?.focus();
  }, [active, loading, location.parentId, location.cardId, searching]);
  function navigate(next: GuideLocation) { setHistory(previous => [...previous, location]); setLocation(next); }
  function up() { focusId.current = location.cardId ?? location.parentId; navigate(parentLocation(location)); }
  function back() { const previous = history.at(-1); if (!previous) return; focusId.current = location.cardId ?? location.parentId; setHistory(items => items.slice(0, -1)); setLocation(previous); }
  function select(item: SystemCard) { navigate({ ...location, parentId: item.parentId ?? item.id, cardId: item.parentId ? item.id : null }); }
  useEffect(() => {
    if (!active) return;
    function key(event: KeyboardEvent) {
      const input = event.target instanceof HTMLElement && (!!event.target.closest('input,textarea,select,[contenteditable]'));
      if (event.key === '/' && !input) { event.preventDefault(); searchInput.current?.focus(); }
      if (event.key === 'Escape') {
        if (event.target === searchInput.current && location.query) { event.preventDefault(); setLocation({ ...location, query: '' }); }
        else if (!input && (location.parentId || location.cardId)) { event.preventDefault(); up(); }
      }
    }
    window.addEventListener('keydown', key); return () => window.removeEventListener('keydown', key);
  }, [active, location]);

  function cardItem(item: SystemCard) {
    const children = guide?.cards.filter(child => child.parentId === item.id) ?? [];
    return <li key={item.id}><article className="quest-card"><span className="quest-pin" aria-hidden="true" /><ThemeImage name={`emblem-${item.parentId ?? item.id}`} className="card-emblem" />
      <h4><a id={`card-${item.id}`} href={`#${item.id}`} onClick={event => { event.preventDefault(); select(item); }}>{item.title}</a></h4><p>{item.summary}</p>
      {item.parentId === null ? <p className="quest-meta">{children.length ? `대표 하위 ${children.length}장 →` : '하위 설명 자료 작성 중 →'}</p> : <><code className="card-id">{item.id}</code><p className="quest-status"><ThemeImage name="stamp-present" className="small-stamp" />코드 있음</p><p className="quest-meta">코드 {item.codeReference?.mappings.length ?? 0}곳 · 기존 ID {item.relatedSystemIds.length}개 · 구현 문서 →</p></>}
    </article></li>;
  }
  return <div className="system-cards" aria-busy={loading}>
    <div className="guide-toolbar"><div className="guide-navigation"><button type="button" onClick={back} disabled={!history.length}>← 뒤로</button>{(parent || card) && <button type="button" onClick={up}>↑ {card ? parent?.title : '전체 시스템'}</button>}{(parent || card) && location.query.trim() && <button type="button" onClick={() => { focusId.current = location.cardId; navigate({ parentId: null, cardId: null, query: location.query }); }}>← 검색 결과로</button>}</div>
      <label className="guide-search">카드·문서 검색 <input ref={searchInput} type="search" maxLength={256} value={location.query} aria-keyshortcuts="/" placeholder="이름, 경로, 설명 검색 (/)" onChange={event => { setLocation({ parentId: null, cardId: null, query: event.target.value }); setHistory([]); }} /></label>
    </div>
    {guide && <><p className="coverage-note">{guide.coverageNote}</p>
      {(parent || card) && <nav className="system-index" aria-label="상위 시스템">{roots.map(root => <button type="button" key={root.id} aria-pressed={parent?.id === root.id} onClick={() => navigate({ ...location, parentId: root.id, cardId: null })}><ThemeImage name={`emblem-${root.id}`} className="index-emblem" /><span>{root.title}</span></button>)}</nav>}
      <h3 className={doc || searching ? 'guide-title' : 'board-title'} ref={heading} tabIndex={-1}>{title}</h3>
      {card && <div className="related-records"><span>기존 기록 ID (별도 catalog 기준)</span>{card.relatedSystemIds.map(id => <span key={id} className={catalogIds === null || !catalogIds.has(id) ? 'record-missing' : ''}>{catalogIds === null ? `제목 확인 불가: ${id}` : catalogIds.has(id) ? id : `연결 실패: ${id} — 개발 기록에 없는 ID`}</span>)}<button type="button" onClick={openRecords}>개발 기록 열기</button></div>}
      {doc && card ? <ImplementationDocument key={doc.id} document={doc} card={card} active={active} /> : searching ? <div className="ledger-page search-results"><p role="status">카드와 문서에서 {cards.length}개 결과를 찾았습니다.</p>{cards.length ? <ul>{cards.map(item => <li key={item.id}><a id={`card-${item.id}`} href={`#${item.id}`} onClick={event => { event.preventDefault(); select(item); }}>{item.title}</a><p>{item.summary}</p></li>)}</ul> : <><p>카드 이름·코드 경로·본문에서 찾았습니다. “수신”, “handshake”, “트레이” 같은 낱말로 찾아보세요.</p><button type="button" onClick={openRecords}>개발 기록 검색 열기</button><button type="button" onClick={() => setLocation(rootLocation)}>전체 시스템</button></>}</div> : <div className="quest-board"><ul aria-label={parent ? '하위 시스템' : '상위 시스템'} className="quest-list">{cards.map(cardItem)}</ul>{!parent && <aside className="board-note"><h4>대표 자료부터 읽기</h4><p>상위 {roots.length}개 · 대표 하위 {guide.cards.length - roots.length}개</p><p>카드 → 하위 카드 → 구현 설명 순서로 읽습니다.</p><div className="stamp-legend">{[['present', '코드 있음'], ['partial', '일부만 있음'], ['absent', '미완료'], ['disconnected', '미연결']].map(([stamp, label]) => <span key={stamp}><ThemeImage name={`stamp-${stamp}`} className="small-stamp" />{label}</span>)}</div></aside>}{parent && !cards.length && <aside className="board-note empty-guide"><h4>하위 설명 자료 작성 중</h4><p>이 분류의 하위 카드는 아직 작성하지 않았습니다. 코드 구현 여부를 판정한 표시가 아닙니다.</p></aside>}</div>}
      <details className="guide-snapshot"><summary>카드 자료 기준 · {guide.revision} · 자동 갱신 안 됨</summary><dl><dt>경로</dt><dd>05_Management/records/system-guide.json</dd><dt>기준일 / byte hash</dt><dd>{guide.asOf} / <code>{result?.ok ? result.version : ''}</code></dd><dt>개발 기록 (별도 자료)</dt><dd>{catalogIds ? `${catalogIds.size}개 ID · hash ${catalogVersion}` : '읽기 실패 · 기존 ID 제목 확인 불가'} · 05_Management/records/catalog.json</dd></dl></details>
    </>}
    {loading && <div className="guide-state" role="status"><h3>카드 자료 읽는 중</h3><p>자료 하나를 끝까지 읽고 검증한 뒤에만 표시합니다.</p></div>}
    {!loading && !guide && <div className="guide-state ledger-page" role="alert"><h3>카드 자료 {result && !result.ok && result.code === 'invalid' ? '손상·검증 실패' : '읽기 실패'}</h3><p>{result && !result.ok ? result.message : '알 수 없는 응답입니다.'}</p><p>05_Management/records/system-guide.json</p><p>부분 자료나 이전 정상 사본으로 바꾸지 않습니다. 개발 기록과 편집 초안은 별도로 유지합니다.</p><button type="button" onClick={openRecords}>개발 기록 열기</button></div>}
    <button className="guide-reload" type="button" disabled={loading} onClick={() => void reload()}>카드 자료 다시 읽기</button>
  </div>;
}

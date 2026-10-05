import { useRef, useState } from 'react';
import { filterRecords, filterSystems, matchesQuery, RECORD_TYPES, type RecordCatalog } from '../recordCatalog';
import { RecordItems, RecordSources, SystemStatus } from './RecordDetails';

type View = 'systems' | 'records' | 'sources';

export default function CatalogExplorer({ data }: { data: RecordCatalog }) {
  const [view, setView] = useState<View>('systems');
  const [query, setQuery] = useState('');
  const [area, setArea] = useState('');
  const [type, setType] = useState('');
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const detailRef = useRef<HTMLElement>(null);
  const systems = filterSystems(data, query, area);
  const records = filterRecords(data, query, area, type);
  const sources = data.sources.filter(source => matchesQuery(query, [source.id, source.title, source.locator, source.note, source.section]) && (!area || data.systems.some(system => system.area === area && system.sourceIds.includes(source.id)) || data.records.some(record => record.sourceIds.includes(source.id) && record.systemIds.some(id => data.systems.some(system => system.id === id && system.area === area)))));
  const selected = systems.find(system => system.id === selectedId);
  const areas = [...new Set(data.systems.map(system => system.area))];
  function navigateSystem(id: string) {
    setQuery(''); setArea(''); setView('systems'); setSelectedId(id);
    requestAnimationFrame(() => { detailRef.current?.focus(); detailRef.current?.scrollIntoView({ block: 'nearest' }); });
  }
  function systemLinks(ids: string[]) {
    return <div className="record-links">{ids.length === 0 && <span className="record-muted">등록된 연결 없음</span>}{ids.map(id => {
      const system = data.systems.find(item => item.id === id);
      return system ? <button type="button" className="record-link" key={id} onClick={() => navigateSystem(id)}>{system.title} →</button> : <span className="record-missing" key={id}>미확인 시스템: {id}</span>;
    })}</div>;
  }
  function recordDetails(id: string) {
    const record = data.records.find(item => item.id === id);
    return record ? <details className="record-entry" key={id}><summary><span className="record-type">{record.type}</span><span>{record.title}</span><span className="record-muted">{record.status}</span></summary><div className="record-entry-body"><p>{record.summary}</p><div className="record-detail-group"><h4>이유</h4><p>{record.reason}</p></div><RecordItems title="상세" items={record.details} /><RecordItems title="검증 한계" items={record.limitations} /><RecordItems title="다음 일" items={record.nextSteps} /><div className="record-detail-group"><h4>연결 시스템</h4>{systemLinks(record.systemIds)}</div><details className="record-evidence"><summary>근거 {record.sourceIds.length}개</summary><RecordSources data={data} ids={record.sourceIds} /></details></div></details> : <p className="record-missing" key={id}>미확인 기록: {id}</p>;
  }
  return <div className="records-browser">
    <div className="record-intro"><div><h3>시스템 기록</h3><p>시스템의 목적과 책임, 변경 이유와 검증 근거를 탐색합니다.</p></div><span className="snapshot-tag">읽은 스냅샷 · 게임 실시간 연동 아님</span></div>
    <details className="record-snapshot"><summary>기준일 {data.asOf} · 기록 버전 {data.revision}</summary><dl><div><dt>소스 commit</dt><dd className="record-path">{data.sourceCommit}</dd></div></dl><p>{data.scopeNote}</p><p>규칙은 GameDev 원문을 참조하세요. 여기의 구현·통합·검증 상태는 기록 상태이며 현재 게임 상태를 의미하지 않습니다. 자동 갱신되지 않습니다.</p></details>
    <div className="record-filters"><label>검색<input type="search" placeholder="시스템, 기록, 근거 검색" value={query} onChange={event => { setQuery(event.target.value); setSelectedId(null); }} /></label><label>분야<select value={area} onChange={event => { setArea(event.target.value); setSelectedId(null); }}><option value="">모든 분야</option>{areas.map(item => <option key={item}>{item}</option>)}</select></label>{view === 'records' && <label>기록 종류<select value={type} onChange={event => setType(event.target.value)}><option value="">모든 종류</option>{RECORD_TYPES.map(item => <option key={item}>{item}</option>)}</select></label>}{(query || area || type) && <button type="button" className="record-link" onClick={() => { setQuery(''); setArea(''); setType(''); setSelectedId(null); }}>필터 초기화</button>}</div>
    <div className="record-views" aria-label="기록 탐색 분류">{([['systems', '시스템', systems.length], ['records', '변경·결정·검증·계획', records.length], ['sources', '근거', sources.length]] as const).map(([key, title, count]) => <button type="button" key={key} aria-pressed={view === key} onClick={() => setView(key)}>{title}<span>{count}</span></button>)}</div>
    <p className="record-result" role="status">{view === 'systems' ? systems.length : view === 'records' ? records.length : sources.length}개 결과{selected ? ` · 선택: ${selected.title}` : ''}</p>
    {view === 'systems' && <div className="record-system-layout"><div className="record-cards">{systems.map(system => <button className="record-card" type="button" key={system.id} aria-pressed={selectedId === system.id} aria-controls="system-detail" onClick={() => { setSelectedId(system.id); requestAnimationFrame(() => detailRef.current?.focus()); }}><span className="record-area">{system.area}</span><h4>{system.title}</h4><p>{system.summary}</p><p className="record-responsibility">책임 · {system.responsibility}</p><SystemStatus system={system} /><span className="record-card-action">상세 보기 →</span></button>)}{!systems.length && <div className="record-empty"><h4>검색 결과가 없습니다.</h4><p>검색어나 분야 필터를 조정하세요.</p></div>}</div><section id="system-detail" className="record-detail work-panel" ref={detailRef} tabIndex={-1} aria-label="선택한 시스템 상세">{selected ? <><div className="panel-heading"><h3>{selected.title}</h3><button className="record-link" type="button" onClick={() => setSelectedId(null)}>선택 해제</button></div><div className="record-detail-body"><p>{selected.summary}</p><div className="record-detail-group"><h4>책임</h4><p>{selected.responsibility}</p></div><SystemStatus system={selected} /><RecordItems title="동작" items={selected.behavior} /><RecordItems title="검증 한계" items={selected.limitations} /><RecordItems title="다음 일" items={selected.nextSteps} /><div className="record-detail-group"><h4>관련 시스템</h4>{systemLinks(selected.relatedSystemIds)}</div><div className="record-detail-group"><h4>연결 기록 · 이유와 검증</h4>{selected.recordIds.length ? selected.recordIds.map(recordDetails) : <p className="record-muted">등록된 연결 기록 없음</p>}</div><details className="record-evidence"><summary>근거 {selected.sourceIds.length}개</summary><RecordSources data={data} ids={selected.sourceIds} /></details></div></> : <div className="record-empty"><h4>시스템을 선택하세요.</h4><p>목적과 상태를 비교한 뒤 동작, 연결 기록과 근거를 확인할 수 있습니다.</p></div>}</section></div>}
    {view === 'records' && <div className="record-list">{records.map(record => recordDetails(record.id))}{!records.length && <div className="record-empty"><h4>검색 결과가 없습니다.</h4><p>검색어, 분야 또는 기록 종류를 조정하세요.</p></div>}</div>}
    {view === 'sources' && (sources.length ? <RecordSources data={data} ids={sources.map(source => source.id)} /> : <div className="record-empty"><h4>검색 결과가 없습니다.</h4><p>검색어나 분야 필터를 조정하세요.</p></div>)}
  </div>;
}

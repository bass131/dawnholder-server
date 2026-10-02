import { useEffect, useRef, useState } from 'react';
import { filterRecords, filterSystems, matchesQuery, readCatalog, catalogReferenceErrors, MAX_CATALOG_BYTES, RECORD_TYPES, type RecordCatalog, type SystemRecord } from './recordCatalog';


type View = 'systems' | 'records' | 'sources';
function Items({ title, items }: { title: string; items: string[] }) {
  return <div className="record-detail-group"><h4>{title}</h4>{items.length ? <ul>{items.map((item, index) => <li key={index}>{item}</li>)}</ul> : <p className="record-muted">등록된 항목 없음</p>}</div>;
}
function Sources({ data, ids }: { data: RecordCatalog; ids: string[] }) {
  return <div className="record-sources">{ids.length === 0 && <p className="record-muted">등록된 근거 없음</p>}{ids.map(id => {
    const source = data.sources.find(item => item.id === id);
    return source ? <article className="record-source" key={id}><h4>{source.title}</h4><dl><div><dt>정확 경로</dt><dd className="record-path">{source.locator}</dd></div><div><dt>버전 / 해시</dt><dd className="record-path">{source.revision || '미확인'}</dd></div><div><dt>섹션</dt><dd>{source.section || '미지정'}</dd></div><div><dt>보관</dt><dd>{source.availability === 'local-only' ? '로컬 전용 · 다른 환경에서는 열람 불가할 수 있음' : '버전 관리 근거'} · {source.kind}</dd></div></dl><p>{source.note}</p></article> : <p className="record-missing" key={id}>미확인 근거: {id}</p>;
  })}</div>;
}
function Status({ system }: { system: SystemRecord }) {
  return <dl className="record-status"><div><dt>구현</dt><dd>{system.implementationStatus}</dd></div><div><dt>통합</dt><dd>{system.integrationStatus}</dd></div><div><dt>검증</dt><dd>{system.verificationStatus}</dd></div></dl>;
}
function CatalogExplorer({ data }: { data: RecordCatalog }) {
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
    return record ? <details className="record-entry" key={id}><summary><span className="record-type">{record.type}</span><span>{record.title}</span><span className="record-muted">{record.status}</span></summary><div className="record-entry-body"><p>{record.summary}</p><div className="record-detail-group"><h4>이유</h4><p>{record.reason}</p></div><Items title="상세" items={record.details} /><Items title="검증 한계" items={record.limitations} /><Items title="다음 일" items={record.nextSteps} /><div className="record-detail-group"><h4>연결 시스템</h4>{systemLinks(record.systemIds)}</div><details className="record-evidence"><summary>근거 {record.sourceIds.length}개</summary><Sources data={data} ids={record.sourceIds} /></details></div></details> : <p className="record-missing" key={id}>미확인 기록: {id}</p>;
  }
  return <div className="records-browser">
    <div className="record-intro"><div><h3>시스템 기록</h3><p>시스템의 목적과 책임, 변경 이유와 검증 근거를 탐색합니다.</p></div><span className="snapshot-tag">읽은 스냅샷 · 게임 실시간 연동 아님</span></div>
    <details className="record-snapshot"><summary>기준일 {data.asOf} · 기록 버전 {data.revision}</summary><dl><div><dt>소스 commit</dt><dd className="record-path">{data.sourceCommit}</dd></div></dl><p>{data.scopeNote}</p><p>규칙은 GameDev 원문을 참조하세요. 여기의 구현·통합·검증 상태는 기록 상태이며 현재 게임 상태를 의미하지 않습니다. 자동 갱신되지 않습니다.</p></details>
    <div className="record-filters"><label>검색<input type="search" placeholder="시스템, 기록, 근거 검색" value={query} onChange={event => { setQuery(event.target.value); setSelectedId(null); }} /></label><label>분야<select value={area} onChange={event => { setArea(event.target.value); setSelectedId(null); }}><option value="">모든 분야</option>{areas.map(item => <option key={item}>{item}</option>)}</select></label>{view === 'records' && <label>기록 종류<select value={type} onChange={event => setType(event.target.value)}><option value="">모든 종류</option>{RECORD_TYPES.map(item => <option key={item}>{item}</option>)}</select></label>}{(query || area || type) && <button type="button" className="record-link" onClick={() => { setQuery(''); setArea(''); setType(''); setSelectedId(null); }}>필터 초기화</button>}</div>
    <div className="record-views" aria-label="기록 탐색 분류">{([['systems', '시스템', systems.length], ['records', '변경·결정·검증·계획', records.length], ['sources', '근거', sources.length]] as const).map(([key, title, count]) => <button type="button" key={key} aria-pressed={view === key} onClick={() => setView(key)}>{title}<span>{count}</span></button>)}</div>
    <p className="record-result" role="status">{view === 'systems' ? systems.length : view === 'records' ? records.length : sources.length}개 결과{selected ? ` · 선택: ${selected.title}` : ''}</p>
    {view === 'systems' && <div className="record-system-layout"><div className="record-cards">{systems.map(system => <button className="record-card" type="button" key={system.id} aria-pressed={selectedId === system.id} aria-controls="system-detail" onClick={() => { setSelectedId(system.id); requestAnimationFrame(() => detailRef.current?.focus()); }}><span className="record-area">{system.area}</span><h4>{system.title}</h4><p>{system.summary}</p><p className="record-responsibility">책임 · {system.responsibility}</p><Status system={system} /><span className="record-card-action">상세 보기 →</span></button>)}{!systems.length && <div className="record-empty"><h4>검색 결과가 없습니다.</h4><p>검색어나 분야 필터를 조정하세요.</p></div>}</div><section id="system-detail" className="record-detail work-panel" ref={detailRef} tabIndex={-1} aria-label="선택한 시스템 상세">{selected ? <><div className="panel-heading"><h3>{selected.title}</h3><button className="record-link" type="button" onClick={() => setSelectedId(null)}>선택 해제</button></div><div className="record-detail-body"><p>{selected.summary}</p><div className="record-detail-group"><h4>책임</h4><p>{selected.responsibility}</p></div><Status system={selected} /><Items title="동작" items={selected.behavior} /><Items title="검증 한계" items={selected.limitations} /><Items title="다음 일" items={selected.nextSteps} /><div className="record-detail-group"><h4>관련 시스템</h4>{systemLinks(selected.relatedSystemIds)}</div><div className="record-detail-group"><h4>연결 기록 · 이유와 검증</h4>{selected.recordIds.length ? selected.recordIds.map(recordDetails) : <p className="record-muted">등록된 연결 기록 없음</p>}</div><details className="record-evidence"><summary>근거 {selected.sourceIds.length}개</summary><Sources data={data} ids={selected.sourceIds} /></details></div></> : <div className="record-empty"><h4>시스템을 선택하세요.</h4><p>목적과 상태를 비교한 뒤 동작, 연결 기록과 근거를 확인할 수 있습니다.</p></div>}</section></div>}
    {view === 'records' && <div className="record-list">{records.map(record => recordDetails(record.id))}{!records.length && <div className="record-empty"><h4>검색 결과가 없습니다.</h4><p>검색어, 분야 또는 기록 종류를 조정하세요.</p></div>}</div>}
    {view === 'sources' && (sources.length ? <Sources data={data} ids={sources.map(source => source.id)} /> : <div className="record-empty"><h4>검색 결과가 없습니다.</h4><p>검색어나 분야 필터를 조정하세요.</p></div>)}
  </div>;
}



export default function DevelopmentRecords() {
  const [catalog, setCatalog] = useState<RecordCatalog | null>(null);
  const [version, setVersion] = useState<string | null>(null);
  const [draftVersion, setDraftVersion] = useState<string | null>(null);
  const [loadedText, setLoadedText] = useState('');
  const [draft, setDraft] = useState('');
  const [dirty, setDirty] = useState(false);
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState('기록을 읽는 중입니다.');
  const [failure, setFailure] = useState(false);
  const [baselineAvailable, setBaselineAvailable] = useState(false);
  const [draftBaselineAvailable, setDraftBaselineAvailable] = useState(false);
  const [draftError, setDraftError] = useState('');
  const alive = useRef(true);
  const operation = useRef(false);
  const draftRef = useRef('');
  const dirtyRef = useRef(false);
  const inputRef = useRef<HTMLInputElement>(null);
  function updateDraft(text: string, changed: boolean) {
    draftRef.current = text; dirtyRef.current = changed;
    setDraft(text); setDirty(changed); setDraftError('');
  }
  async function reload() {
    if (operation.current) return;
    if (!window.systemRecords) { setNotice('기록 연결을 사용할 수 없습니다. 데스크톱 앱에서 실행하세요.'); setFailure(true); return; }
    operation.current = true; setBusy(true);
    try {
      const result = await window.systemRecords.readCatalog();
      if (!alive.current) return;
      if (result.ok) {
        setCatalog(result.catalog); setVersion(result.version); setLoadedText(result.text); setBaselineAvailable(true);
        if (!dirtyRef.current) {
          updateDraft(result.text, false);
          setDraftVersion(result.version); setDraftBaselineAvailable(true);
        }
        setFailure(false); setNotice(dirtyRef.current ? '최신 기록을 읽었습니다. 편집 초안은 보존했습니다. 저장 전에 변경 내용을 비교하세요.' : '기록 파일을 읽었습니다. 자동 갱신은 하지 않습니다.');
      } else {
        setFailure(true); setNotice(`${result.message}${catalog ? ' · 갱신 실패: 이전에 읽은 기록을 표시합니다.' : ''}`);
        if (result.code === 'missing' || result.version) {
          setVersion(result.version ?? null); setLoadedText(result.text ?? ''); setBaselineAvailable(true);
          if (!dirtyRef.current) {
            updateDraft(result.text ?? '', false);
            setDraftVersion(result.version ?? null); setDraftBaselineAvailable(true);
          }
        } else {
          setBaselineAvailable(false);
          if (!dirtyRef.current) setDraftBaselineAvailable(false);
        }
      }
    } catch { if (alive.current) { setFailure(true); setBaselineAvailable(false); if (!dirtyRef.current) setDraftBaselineAvailable(false); setNotice('기록 연결에서 응답을 받지 못했습니다. 초안은 보존합니다.'); } }
    finally { operation.current = false; if (alive.current) setBusy(false); }
  }
  useEffect(() => { alive.current = true; void reload(); return () => { alive.current = false; }; }, []);
  async function importFile(file: File | undefined) {
    if (!file || operation.current) return;
    if (dirtyRef.current) { setDraftError('편집 중인 초안이 있습니다. 초안을 보존하려면 먼저 복사하거나 저장하세요. 다른 파일을 불러오려면 편집 취소를 명시적으로 선택하세요.'); return; }
    if (file.size > MAX_CATALOG_BYTES) { setDraftError('불러올 파일은 2 MiB 이하이어야 합니다.'); return; }
    operation.current = true; setBusy(true);
    try {
      const text = await file.text();
      if (!alive.current) return;
      updateDraft(text, true); setDraftVersion(version); setDraftBaselineAvailable(baselineAvailable);
      setNotice(`${file.name}을 초안으로 불러왔습니다. 아직 기록 파일에 저장하지 않았습니다.`);
    } catch { setDraftError('선택한 파일을 읽지 못했습니다.'); }
    finally { operation.current = false; if (alive.current) setBusy(false); }
  }
  function validateDraft(): boolean {
    if (new TextEncoder().encode(draftRef.current).length > MAX_CATALOG_BYTES) { setDraftError('초안은 2 MiB 이하이어야 합니다.'); return false; }
    try {
      const parsed = readCatalog(JSON.parse(draftRef.current));
      if (!parsed) { setDraftError('카탈로그 스키마 또는 중복 ID를 확인하세요.'); return false; }
      const errors = catalogReferenceErrors(parsed);
      if (errors.length) { setDraftError(errors.slice(0, 5).join('\n')); return false; }
      setDraftError(''); setNotice('초안 형식과 ID 참조를 확인했습니다. 저장하면 기록 파일을 갱신합니다.'); return true;
    } catch { setDraftError('초안이 유효한 JSON이 아닙니다.'); return false; }
  }
  async function save() {
    if (operation.current || !draftBaselineAvailable || !window.systemRecords || !validateDraft()) return;
    operation.current = true; setBusy(true);
    try {
      const result = await window.systemRecords.saveCatalog({ text: draftRef.current, expectedVersion: draftVersion });
      if (!alive.current) return;
      if (result.ok) {
        setCatalog(result.catalog); setVersion(result.version); setLoadedText(result.text); updateDraft(result.text, false);
        setDraftVersion(result.version); setDraftBaselineAvailable(true); setBaselineAvailable(true);
        setFailure(false); setNotice('기록을 저장하고 화면에 반영했습니다. 재빌드는 필요하지 않습니다.');
      } else { setFailure(true); setNotice(`${result.message} · 편집 초안은 보존했습니다.`); }
    } catch { setFailure(true); setNotice('저장 응답을 받지 못했습니다. 초안을 보존합니다. 새로고침으로 기록을 확인하세요.'); }
    finally { operation.current = false; if (alive.current) setBusy(false); }
  }
  return <div>
    <div className="record-runtime-bar"><button className="record-link" type="button" disabled={busy} onClick={() => void reload()}>기록 새로고침</button><label className="record-file-label">JSON 파일 불러오기<input ref={inputRef} type="file" accept=".json,application/json" disabled={busy || dirty} onChange={event => { const file = event.target.files?.[0]; void importFile(file); event.target.value = ''; }} /></label><span>{dirty ? '저장하지 않은 편집 초안 있음' : '실행 중 읽기·편집 가능'}</span></div>
    <p className={failure ? 'record-runtime-notice record-missing' : 'record-runtime-notice'} role="status">{notice}</p>
    {catalog ? <CatalogExplorer data={catalog} /> : <div className="record-empty"><h3>시스템 기록을 읽을 수 없습니다.</h3><p>기록 새로고침을 시도하거나 아래 기록 편집에서 JSON을 불러와 저장하세요.</p></div>}
    <details className="record-editor"><summary>기록 편집 {dirty ? '· 미저장 초안' : ''}</summary><p>현재 화면은 마지막으로 읽은 기록입니다. JSON 파일 불러오기는 초안만 바꿉니다. 명시적으로 저장할 때 05_Management/records/catalog.json을 갱신합니다. 자동 갱신은 하지 않습니다. 저장한 내용은 같은 파일을 읽는 공동 조회 MCP의 다음 요청에 반영됩니다.</p><label>카탈로그 JSON<textarea aria-describedby="record-editor-help" value={draft} disabled={busy} spellCheck={false} onChange={event => updateDraft(event.target.value, event.target.value !== loadedText)} /></label><p id="record-editor-help">새로고침은 편집 초안을 보존합니다. 변경을 버리려면 편집 취소를 선택하세요. 직전 정상 기록 백업은 복구용이며 변경 근거가 아닙니다.</p>{draftError && <p className="record-missing" role="alert">{draftError}</p>}<div className="record-editor-actions"><button className="record-link" type="button" disabled={busy || !draft} onClick={validateDraft}>초안 검증</button><button className="record-link" type="button" disabled={busy || !dirty || !draftBaselineAvailable || !window.systemRecords} onClick={() => void save()}>검증 후 기록 저장</button><button className="record-link" type="button" disabled={busy || !dirty} onClick={() => { updateDraft(loadedText, false); setDraftVersion(version); setDraftBaselineAvailable(baselineAvailable); setNotice('편집 초안을 취소하고 마지막으로 읽은 원문으로 돌아갔습니다.'); }}>편집 취소 · 초안 버리기</button></div></details>
  </div>;
}

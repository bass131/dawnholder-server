import { useLayoutEffect, useRef, useState } from 'react';
import { filterRecords, filterSystems, matchesQuery, RECORD_TYPES, type RecordCatalog } from '../recordCatalog';
import { RecordDetail, RecordSources, SystemDetail } from './RecordDetails';
import {
  goBack,
  initialNavigation,
  navigateTo,
  returnToList,
  selectionFocusKey,
  updateFilters,
  type CatalogView,
  type ExplorerLocation,
  type RecordSelection,
  type ReturnPoint,
} from './navigation';

export default function CatalogExplorer({ data, active }: { data: RecordCatalog; active: boolean }) {
  const [navigation, setNavigation] = useState(initialNavigation);
  const browserRef = useRef<HTMLDivElement>(null);
  const headingRef = useRef<HTMLHeadingElement>(null);
  const pendingPosition = useRef<'heading' | 'return' | null>(null);
  const { location, returnPoint } = navigation.current;
  const { view, query, area, recordType, selection } = location;
  const systems = filterSystems(data, query, area);
  const records = filterRecords(data, query, area, recordType);
  const sources = data.sources.filter(source => {
    const matchesArea = !area
      || data.systems.some(system => system.area === area && system.sourceIds.includes(source.id))
      || data.records.some(record => record.sourceIds.includes(source.id)
        && record.systemIds.some(id => data.systems.some(system => system.id === id && system.area === area)));
    return matchesArea && matchesQuery(query, [source.id, source.title, source.locator, source.note, source.section]);
  });
  const areas = [...new Set(data.systems.map(system => system.area))];
  if (area && !areas.includes(area)) areas.push(area);

  // Linked details resolve against the whole snapshot, independently of the saved list filters.
  const selectedSystem = selection?.kind === 'system'
    ? data.systems.find(system => system.id === selection.id)
    : undefined;
  const selectedRecord = selection?.kind === 'record'
    ? data.records.find(record => record.id === selection.id)
    : undefined;
  const selected = selectedSystem ?? selectedRecord;
  const listTitle = view === 'systems' ? '시스템 목록' : view === 'records' ? '변경·결정·검증·계획 목록' : '근거';
  const title = selection ? selected?.title ?? '선택한 항목을 찾을 수 없습니다.' : listTitle;
  const resultCount = view === 'systems' ? systems.length : view === 'records' ? records.length : sources.length;
  const views = [
    ['systems', '시스템', systems.length],
    ['records', '변경·결정·검증·계획', records.length],
    ['sources', '근거', sources.length],
  ] as const;

  function scrollContainer() {
    return browserRef.current?.closest<HTMLElement>('main') ?? document.documentElement;
  }

  function captureReturnPoint(focusKey: string): ReturnPoint {
    const container = scrollContainer();
    return {
      scrollLeft: container.scrollLeft,
      scrollTop: container.scrollTop,
      focusKey,
      evidenceOpen: browserRef.current?.querySelector<HTMLDetailsElement>('.record-evidence')?.open ?? false,
    };
  }

  useLayoutEffect(() => {
    const browser = browserRef.current;
    const heading = headingRef.current;
    // Only an explicit navigation requests focus; background reads and inactive tabs never do.
    if (!active || !browser || !heading || browser.closest('[hidden]') || !pendingPosition.current) return;
    const position = pendingPosition.current;
    pendingPosition.current = null;
    if (position === 'return') {
      const point = navigation.current.returnPoint;
      const target = Array.from(browser.querySelectorAll<HTMLElement>('[data-record-focus]'))
        .find(element => element.dataset.recordFocus === point.focusKey);
      (target ?? heading).focus({ preventScroll: true });
      if (target) {
        // The ledger scrolls inside main, so restoring window.scrollY would lose its position.
        const container = scrollContainer();
        container.scrollLeft = point.scrollLeft;
        container.scrollTop = point.scrollTop;
      } else {
        heading.scrollIntoView({ block: 'start' });
      }
    } else {
      heading.focus({ preventScroll: true });
      heading.scrollIntoView({ block: 'start' });
    }
  }, [active, navigation]);

  function openSelection(next: RecordSelection) {
    const point = captureReturnPoint(selectionFocusKey(next));
    pendingPosition.current = 'heading';
    setNavigation(previous => navigateTo(previous, {
      ...previous.current.location,
      view: next.kind === 'system' ? 'systems' : 'records',
      selection: next,
    }, point));
  }

  function changeView(next: CatalogView) {
    if (!selection && view === next) return;
    const point = captureReturnPoint('view:' + next);
    pendingPosition.current = 'heading';
    setNavigation(previous => navigateTo(previous, {
      ...previous.current.location,
      view: next,
      selection: null,
    }, point));
  }

  function back() {
    if (!navigation.history.length) return;
    pendingPosition.current = 'return';
    setNavigation(goBack);
  }

  function list() {
    pendingPosition.current = 'return';
    setNavigation(returnToList);
  }

  function filter(filters: Partial<Pick<ExplorerLocation, 'query' | 'area' | 'recordType'>>) {
    setNavigation(previous => updateFilters(previous, filters));
  }

  return (
    <div className="records-browser" ref={browserRef}>
      <div className="record-intro">
        <div><h3>시스템 기록</h3><p>시스템의 목적과 책임, 변경 이유와 검증 근거를 탐색합니다.</p></div>
        <span className="snapshot-tag">읽은 스냅샷 · 게임 실시간 연동 아님</span>
      </div>
      <details className="record-snapshot">
        <summary>기준일 {data.asOf} · 기록 버전 {data.revision}</summary>
        <dl><div><dt>소스 commit</dt><dd className="record-path">{data.sourceCommit}</dd></div></dl>
        <p>{data.scopeNote}</p>
        <p>규칙은 GameDev 원문을 참조하세요. 여기의 구현·통합·검증 상태는 기록 상태이며 현재 게임 상태를 의미하지 않습니다. 자동 갱신되지 않습니다.</p>
      </details>
      {!selection && (
        <div className="record-filters">
          <label>
            검색
            <input
              type="search"
              placeholder="시스템, 기록, 근거 검색"
              value={query}
              data-record-focus="search"
              onChange={event => filter({ query: event.target.value })}
            />
          </label>
          <label>
            분야
            <select value={area} data-record-focus="area" onChange={event => filter({ area: event.target.value })}>
              <option value="">모든 분야</option>
              {areas.map(item => <option key={item}>{item}</option>)}
            </select>
          </label>
          {view === 'records' && (
            <label>
              기록 종류
              <select
                value={recordType}
                data-record-focus="record-type"
                onChange={event => filter({ recordType: event.target.value })}
              >
                <option value="">모든 종류</option>
                {RECORD_TYPES.map(item => <option key={item}>{item}</option>)}
              </select>
            </label>
          )}
          {(query || area || recordType) && (
            <button
              type="button"
              className="record-link"
              onClick={() => filter({ query: '', area: '', recordType: '' })}
            >
              필터 초기화
            </button>
          )}
        </div>
      )}
      <div className="record-views" aria-label="기록 탐색 분류">
        {views.map(([key, label, count]) => (
          <button
            type="button"
            key={key}
            aria-pressed={view === key}
            data-record-focus={'view:' + key}
            onClick={() => changeView(key)}
          >
            {label}<span>{count}</span>
          </button>
        ))}
      </div>
      {(selection || navigation.history.length > 0) && (
        <div className="record-navigation" aria-label="기록 화면 이동">
          <button className="record-link" type="button" disabled={!navigation.history.length} onClick={back}>
            ← 뒤로
          </button>
          {selection && <button className="record-link" type="button" onClick={list}>목록으로</button>}
        </div>
      )}
      {selection ? (
        <section className="record-detail work-panel" aria-labelledby="catalog-heading">
          <div className="record-detail-heading">
            <h3 id="catalog-heading" ref={headingRef} tabIndex={-1}>{title}</h3>
          </div>
          {selectedSystem ? (
            <SystemDetail
              key={selectionFocusKey(selection)}
              data={data}
              system={selectedSystem}
              evidenceOpen={returnPoint.evidenceOpen}
              onOpen={openSelection}
            />
          ) : selectedRecord ? (
            <RecordDetail
              key={selectionFocusKey(selection)}
              data={data}
              record={selectedRecord}
              evidenceOpen={returnPoint.evidenceOpen}
              onOpen={openSelection}
            />
          ) : (
            <div className="record-detail-body" role="status">
              <p>새로 읽은 기록에 {selection.kind === 'system' ? '시스템' : '기록'} ID “{selection.id}”가 없습니다.</p>
              <p>이전 화면이나 목록으로 돌아가 다시 선택하세요.</p>
            </div>
          )}
        </section>
      ) : (
        <>
          <h3 id="catalog-heading" className="record-list-heading" ref={headingRef} tabIndex={-1}>{title}</h3>
          <p className="record-result" role="status">{resultCount}개 결과</p>
          {view === 'systems' && (
            <ul className="record-cards" aria-label="시스템 목록">
              {systems.map(system => (
                <li key={system.id}>
                  <button
                    className="record-card"
                    type="button"
                    data-record-focus={selectionFocusKey({ kind: 'system', id: system.id })}
                    onClick={() => openSelection({ kind: 'system', id: system.id })}
                  >
                    <span className="record-area">{system.area}</span>
                    <span className="record-item-title">{system.title}</span>
                    <span className="record-item-status">{system.implementationStatus}</span>
                  </button>
                </li>
              ))}
            </ul>
          )}
          {view === 'records' && (
            <ul className="record-cards" aria-label="기록 목록">
              {records.map(record => (
                <li key={record.id}>
                  <button
                    className="record-card"
                    type="button"
                    data-record-focus={selectionFocusKey({ kind: 'record', id: record.id })}
                    onClick={() => openSelection({ kind: 'record', id: record.id })}
                  >
                    <span className="record-type">{record.type}</span>
                    <span className="record-item-title">{record.title}</span>
                    <span className="record-item-status">{record.status}</span>
                  </button>
                </li>
              ))}
            </ul>
          )}
          {view === 'sources' && sources.length > 0 && <RecordSources data={data} ids={sources.map(source => source.id)} />}
          {resultCount === 0 && (
            <div className="record-empty">
              <h4>검색 결과가 없습니다.</h4>
              <p>{view === 'records' ? '검색어, 분야 또는 기록 종류를 조정하세요.' : '검색어나 분야 필터를 조정하세요.'}</p>
            </div>
          )}
        </>
      )}
    </div>
  );
}

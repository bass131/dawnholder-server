import { sourceReadability } from '../../electron/source-section-contract';
import type { DevelopmentRecord, RecordCatalog, SystemRecord } from '../recordCatalog';
import { selectionFocusKey } from './navigation';
import type { RecordSelection } from './navigation';

interface DetailProps {
  data: RecordCatalog;
  onOpen(selection: RecordSelection): void;
}

export function RecordSources({ data, ids, onOpen }: DetailProps & { ids: string[] }) {
  return (
    <div className="record-sources">
      {ids.length === 0 && <p className="record-muted">등록된 출처 없음</p>}
      {ids.map(id => {
        const source = data.sources.find(item => item.id === id);
        if (!source) return <p className="record-missing" key={id}>미확인 출처: {id}</p>;
        const unreadable = sourceReadability(source);
        const reason = unreadable === 'local-only' ? '로컬 전용' : unreadable === 'handoff' ? '전달 메시지' : 'Markdown 아님';
        const selection: RecordSelection = { kind: 'source', id };
        return (
          <article className="record-source" key={id}>
            {unreadable ? (
              <h4>{source.title} · 앱에서 읽지 않음({reason})</h4>
            ) : (
              <button
                type="button"
                className="record-link"
                data-record-focus={selectionFocusKey(selection)}
                onClick={() => onOpen(selection)}
              >
                {source.title} 원문 읽기
              </button>
            )}
            <dl>
              <div><dt>종류</dt><dd>{source.kind}</dd></div>
              <div><dt>경로</dt><dd className="record-path">{source.locator}</dd></div>
              <div><dt>구간</dt><dd>{source.section ?? '파일 처음부터'}</dd></div>
            </dl>
          </article>
        );
      })}
    </div>
  );
}

function CatalogLinks({ data, kind, ids, onOpen }: DetailProps & {
  kind: 'system' | 'record';
  ids: string[];
}) {
  const items = kind === 'system' ? data.systems : data.records;
  return (
    <div className="record-links">
      {ids.length === 0 && <span className="record-muted">등록된 연결 없음</span>}
      {ids.map(id => {
        const item = items.find(candidate => candidate.id === id);
        if (!item) return <span className="record-missing" key={id}>미확인 {kind === 'system' ? '시스템' : '기록'}: {id}</span>;
        const selection: RecordSelection = { kind, id };
        return (
          <button
            type="button"
            className="record-link"
            key={id}
            data-record-focus={selectionFocusKey(selection)}
            onClick={() => onOpen(selection)}
          >
            {item.title} →
          </button>
        );
      })}
    </div>
  );
}

function OriginalSources({ data, ids, onOpen }: DetailProps & { ids: string[] }) {
  return (
    <div className="record-detail-group">
      <h4>원문</h4>
      <RecordSources data={data} ids={ids} onOpen={onOpen} />
    </div>
  );
}

export function SystemDetail({ data, system, onOpen }: DetailProps & { system: SystemRecord }) {
  return (
    <div className="record-detail-body">
      <p className="record-detail-meta">{system.area}</p>
      <div className="record-detail-group">
        <h4>관련 시스템</h4>
        <CatalogLinks data={data} kind="system" ids={system.relatedSystemIds} onOpen={onOpen} />
      </div>
      <div className="record-detail-group">
        <h4>연결 기록</h4>
        <CatalogLinks data={data} kind="record" ids={system.recordIds} onOpen={onOpen} />
      </div>
      <OriginalSources data={data} ids={system.sourceIds} onOpen={onOpen} />
    </div>
  );
}

export function RecordDetail({ data, record, onOpen }: DetailProps & { record: DevelopmentRecord }) {
  return (
    <div className="record-detail-body">
      <p className="record-detail-meta">{record.type}</p>
      {record.pullRequests.map(pr => <p className="record-detail-meta" key={pr.number}>PR #{pr.number} · 병합 {pr.mergeCommit.slice(0, 7)}</p>)}
      <div className="record-detail-group">
        <h4>연결 시스템</h4>
        <CatalogLinks data={data} kind="system" ids={record.systemIds} onOpen={onOpen} />
      </div>
      <OriginalSources data={data} ids={record.sourceIds} onOpen={onOpen} />
    </div>
  );
}

import type { DevelopmentRecord, RecordCatalog, SystemRecord } from '../recordCatalog';
import { selectionFocusKey, type RecordSelection } from './navigation';

export function RecordItems({ title, items }: { title: string; items: string[] }) {
  return (
    <div className="record-detail-group">
      <h4>{title}</h4>
      {items.length ? (
        <ul>{items.map((item, index) => <li key={index}>{item}</li>)}</ul>
      ) : (
        <p className="record-muted">등록된 항목 없음</p>
      )}
    </div>
  );
}

export function RecordSources({ data, ids }: { data: RecordCatalog; ids: string[] }) {
  return (
    <div className="record-sources">
      {ids.length === 0 && <p className="record-muted">등록된 근거 없음</p>}
      {ids.map(id => {
        const source = data.sources.find(item => item.id === id);
        if (!source) return <p className="record-missing" key={id}>미확인 근거: {id}</p>;
        return (
          <article className="record-source" key={id}>
            <h4>{source.title}</h4>
            <dl>
              <div><dt>정확 경로</dt><dd className="record-path">{source.locator}</dd></div>
              <div><dt>버전 / 해시</dt><dd className="record-path">{source.revision || '미확인'}</dd></div>
              <div><dt>섹션</dt><dd>{source.section || '미지정'}</dd></div>
              <div>
                <dt>보관</dt>
                <dd>
                  {source.availability === 'local-only'
                    ? '로컬 전용 · 다른 환경에서는 열람 불가할 수 있음'
                    : '버전 관리 근거'} · {source.kind}
                </dd>
              </div>
            </dl>
            <p>{source.note}</p>
          </article>
        );
      })}
    </div>
  );
}

function SystemStatus({ system }: { system: SystemRecord }) {
  return (
    <dl className="record-status">
      <div><dt>구현</dt><dd>{system.implementationStatus}</dd></div>
      <div><dt>통합</dt><dd>{system.integrationStatus}</dd></div>
      <div><dt>검증</dt><dd>{system.verificationStatus}</dd></div>
    </dl>
  );
}

interface DetailProps {
  data: RecordCatalog;
  evidenceOpen: boolean;
  onOpen(selection: RecordSelection): void;
}

function CatalogLinks({ data, kind, ids, onOpen }: {
  data: RecordCatalog;
  kind: RecordSelection['kind'];
  ids: string[];
  onOpen(selection: RecordSelection): void;
}) {
  const items = kind === 'system' ? data.systems : data.records;
  return (
    <div className="record-links">
      {ids.length === 0 && <span className="record-muted">등록된 연결 없음</span>}
      {ids.map(id => {
        const item = items.find(candidate => candidate.id === id);
        if (!item) {
          return <span className="record-missing" key={id}>미확인 {kind === 'system' ? '시스템' : '기록'}: {id}</span>;
        }
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

function Evidence({ data, ids, open }: { data: RecordCatalog; ids: string[]; open: boolean }) {
  return (
    <details className="record-evidence" open={open}>
      <summary data-record-focus="detail-evidence">근거 {ids.length}개</summary>
      <RecordSources data={data} ids={ids} />
    </details>
  );
}

export function SystemDetail({ data, system, evidenceOpen, onOpen }: DetailProps & { system: SystemRecord }) {
  return (
    <div className="record-detail-body">
      <p className="record-detail-meta">{system.area}</p>
      <p>{system.summary}</p>
      <div className="record-detail-group"><h4>책임</h4><p>{system.responsibility}</p></div>
      <SystemStatus system={system} />
      <RecordItems title="동작" items={system.behavior} />
      <RecordItems title="검증 한계" items={system.limitations} />
      <RecordItems title="다음 일" items={system.nextSteps} />
      <div className="record-detail-group">
        <h4>관련 시스템</h4>
        <CatalogLinks data={data} kind="system" ids={system.relatedSystemIds} onOpen={onOpen} />
      </div>
      <div className="record-detail-group">
        <h4>연결 기록 · 이유와 검증</h4>
        {system.recordIds.length ? (
          <CatalogLinks data={data} kind="record" ids={system.recordIds} onOpen={onOpen} />
        ) : (
          <p className="record-muted">등록된 연결 기록 없음</p>
        )}
      </div>
      <Evidence data={data} ids={system.sourceIds} open={evidenceOpen} />
    </div>
  );
}

export function RecordDetail({ data, record, evidenceOpen, onOpen }: DetailProps & { record: DevelopmentRecord }) {
  return (
    <div className="record-detail-body">
      <p className="record-detail-meta"><span>{record.type}</span><span>{record.status}</span></p>
      <p>{record.summary}</p>
      <div className="record-detail-group"><h4>이유</h4><p>{record.reason}</p></div>
      <RecordItems title="상세" items={record.details} />
      <RecordItems title="검증 한계" items={record.limitations} />
      <RecordItems title="다음 일" items={record.nextSteps} />
      <div className="record-detail-group">
        <h4>연결 시스템</h4>
        <CatalogLinks data={data} kind="system" ids={record.systemIds} onOpen={onOpen} />
      </div>
      <Evidence data={data} ids={record.sourceIds} open={evidenceOpen} />
    </div>
  );
}

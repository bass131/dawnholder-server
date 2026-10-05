import type { RecordCatalog, SystemRecord } from '../recordCatalog';

export function RecordItems({ title, items }: { title: string; items: string[] }) {
  return <div className="record-detail-group"><h4>{title}</h4>{items.length ? <ul>{items.map((item, index) => <li key={index}>{item}</li>)}</ul> : <p className="record-muted">등록된 항목 없음</p>}</div>;
}
export function RecordSources({ data, ids }: { data: RecordCatalog; ids: string[] }) {
  return <div className="record-sources">{ids.length === 0 && <p className="record-muted">등록된 근거 없음</p>}{ids.map(id => {
    const source = data.sources.find(item => item.id === id);
    return source ? <article className="record-source" key={id}><h4>{source.title}</h4><dl><div><dt>정확 경로</dt><dd className="record-path">{source.locator}</dd></div><div><dt>버전 / 해시</dt><dd className="record-path">{source.revision || '미확인'}</dd></div><div><dt>섹션</dt><dd>{source.section || '미지정'}</dd></div><div><dt>보관</dt><dd>{source.availability === 'local-only' ? '로컬 전용 · 다른 환경에서는 열람 불가할 수 있음' : '버전 관리 근거'} · {source.kind}</dd></div></dl><p>{source.note}</p></article> : <p className="record-missing" key={id}>미확인 근거: {id}</p>;
  })}</div>;
}
export function SystemStatus({ system }: { system: SystemRecord }) {
  return <dl className="record-status"><div><dt>구현</dt><dd>{system.implementationStatus}</dd></div><div><dt>통합</dt><dd>{system.integrationStatus}</dd></div><div><dt>검증</dt><dd>{system.verificationStatus}</dd></div></dl>;
}

import { useEffect, useRef, useState } from 'react';
import { backlogIssueKind } from '../electron/backlog-contract';
import type { BacklogIssue, BacklogResult } from '../electron/backlog-contract';
import type { BacklogRow } from '../electron/backlog-table';

type BacklogSnapshot = Extract<BacklogResult, { ok: true }>;

function candidateGroups(rows: BacklogRow[]) {
  const groups: { title: string | null; rows: BacklogRow[] }[] = [];
  const ungrouped = rows.filter(row => row.group === null);
  if (ungrouped.length > 0) groups.push({ title: null, rows: ungrouped });
  for (const row of rows) {
    if (row.group === null) continue;
    const previous = groups.at(-1);
    if (previous?.title === row.group) {
      previous.rows.push(row);
    } else {
      groups.push({ title: row.group, rows: [row] });
    }
  }
  return groups;
}

function BacklogProblem({ issue }: { issue: BacklogIssue }) {
  return (
    <div className="backlog-problem">
      <span className="backlog-problem-kind">{backlogIssueKind(issue.code) === 'format' ? '형식 오류' : '어긋남'}</span>
      <p>{issue.cause}</p>
      <p>고치는 방법: {issue.fix}</p>
    </div>
  );
}

export default function DevelopmentBacklog() {
  const [snapshot, setSnapshot] = useState<BacklogSnapshot | null>(null);
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState('백로그를 읽는 중입니다.');
  const [failure, setFailure] = useState(false);
  const mounted = useRef(false);
  const requestSequence = useRef(0);
  const operation = useRef<number | null>(null);

  async function reload() {
    if (operation.current !== null) return;
    const bridge = window.systemBacklog;
    if (!bridge) {
      setFailure(true);
      setNotice('백로그 연결을 사용할 수 없습니다. 데스크톱 앱에서 실행하세요.');
      return;
    }
    const request = ++requestSequence.current;
    operation.current = request;
    setBusy(true);
    setFailure(false);
    setNotice('백로그를 읽는 중입니다.');
    try {
      const result = await bridge.readBacklog();
      if (!mounted.current || request !== requestSequence.current) return;
      if (result.ok) {
        setSnapshot(result);
        setFailure(false);
        setNotice('백로그 파일을 읽었습니다. 자동 갱신은 하지 않습니다.');
      } else {
        // A failed refresh owns only the notice; the last readable snapshot
        // remains available instead of presenting an empty candidate list.
        setFailure(true);
        setNotice(result.message);
      }
    } catch {
      if (mounted.current && request === requestSequence.current) {
        setFailure(true);
        setNotice('백로그 연결에서 응답을 받지 못했습니다.');
      }
    } finally {
      if (operation.current === request) operation.current = null;
      if (mounted.current && request === requestSequence.current) setBusy(false);
    }
  }

  useEffect(() => {
    mounted.current = true;
    void reload();
    return () => {
      // A StrictMode remount gets its own request and refresh lock. An old
      // response cannot replace its snapshot or clear its operation.
      mounted.current = false;
      requestSequence.current += 1;
      operation.current = null;
    };
  }, []);

  const formatCount = snapshot?.issues.filter(issue => backlogIssueKind(issue.code) === 'format').length ?? 0;
  const mismatchCount = (snapshot?.issues.length ?? 0) - formatCount;
  const candidateLines = new Set(snapshot?.rows.map(row => row.line));
  const tableIssues = snapshot?.issues.filter(issue => issue.line === null || !candidateLines.has(issue.line)) ?? [];

  return (
    <div className="backlog-browser">
      <div className="record-runtime-bar">
        <button className="record-link" type="button" disabled={busy} onClick={() => void reload()}>다시 읽기</button>
      </div>
      <p className={failure ? 'record-runtime-notice record-missing' : 'record-runtime-notice'} role="status">{notice}</p>
      {failure && snapshot && <p className="record-runtime-notice record-missing">갱신 실패: 이전에 읽은 백로그를 표시합니다.</p>}
      {snapshot && (
        <>
          <p className="backlog-summary">후보 {snapshot.rows.length}개 · 형식 오류 {formatCount} · 어긋남 {mismatchCount} · 확인 불가 {snapshot.uncheckedLinks.length}</p>
          {tableIssues.length > 0 && (
            <section className="backlog-table-issues" aria-label="표 형식 오류">
              <h3>표 형식 오류</h3>
              {tableIssues.map((issue, index) => <BacklogProblem key={index} issue={issue} />)}
            </section>
          )}
          {candidateGroups(snapshot.rows).map((group, index) => (
            <section className="backlog-group" key={index}>
              {group.title !== null && <h3>{group.title}</h3>}
              <ul className="backlog-candidates">
                {group.rows.map(row => (
                  <li className="backlog-candidate" key={row.line}>
                    <p className="backlog-id">{row.id}</p>
                    <h4>{row.title}</h4>
                    <dl className="backlog-fields">
                      <div><dt>상태</dt><dd>{row.status}</dd></div>
                      <div><dt>담당 후보</dt><dd>{row.owner}</dd></div>
                      <div><dt>이유</dt><dd>{row.reason}</dd></div>
                      <div><dt>출처</dt><dd>{row.source}</dd></div>
                      <div><dt>선행 조건</dt><dd>{row.prerequisite}</dd></div>
                      <div><dt>위치</dt><dd className="record-path">BACKLOG.md:{row.line}</dd></div>
                    </dl>
                    {snapshot.issues.filter(issue => issue.line === row.line).map((issue, issueIndex) => (
                      <BacklogProblem key={issueIndex} issue={issue} />
                    ))}
                    {snapshot.uncheckedLinks.filter(link => link.line === row.line).map((link, linkIndex) => (
                      <div className="backlog-problem" key={linkIndex}>
                        <span className="backlog-problem-kind">확인 불가</span>
                        <p className="record-path">{link.link}</p>
                        <p>다시 읽기로 다시 확인하세요.</p>
                      </div>
                    ))}
                  </li>
                ))}
              </ul>
            </section>
          ))}
        </>
      )}
    </div>
  );
}

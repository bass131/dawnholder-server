import { useCallback, useEffect, useRef, useState } from 'react';
import { parseLogQuery, serverOperationMessage } from '../electron/server-operations-contract';
import type {
  ConnectionState, LogsResponse, OperationResult, ReleaseCandidate, ReleaseInfo, ServerStatus,
} from '../electron/server-operations-contract';

const serverStates = { stopped: '정지', starting: '시작 중', running: '실행 중', stopping: '종료 중' };
const exitKinds = { graceful: '정상', forced: '강제', abnormal: '비정상', startFailed: '시작 실패', unknown: '결과 모름' };
const localTime = (time: string | null) => time === null ? '없음' : new Date(time).toLocaleString('ko-KR');

function ReleaseLabel({ release }: { release: ReleaseInfo | null }) {
  return release
    ? <><code>{release.commit.slice(0, 8)}</code><span className="summary-reason">빌드 시각 {localTime(release.builtAt)}</span></>
    : <>지정된 운영 버전 없음</>;
}

function StatusSummary({ status }: { status: ServerStatus | null }) {
  const server = status?.server;
  const other = server?.portOwner === 'other' || server?.portLockHeldByOther;
  return (
    <dl className="summary-strip operations-summary" aria-label="서버 관측 정보">
      <div className="summary-item">
        <dt>실행 상태</dt>
        <dd><span className="summary-value">{server ? serverStates[server.state] : '상태 데이터 미수집'}</span>
          {server?.pid !== null && server?.pid !== undefined && <span className="summary-reason">pid {server.pid}</span>}
          {server?.startedAt && <span className="summary-reason">시작 시각 {localTime(server.startedAt)}</span>}
        </dd>
      </div>
      <div className="summary-item">
        <dt>7777 대기</dt>
        <dd><span className="summary-value">{server ? (server.portListening ? '대기 중' : '없음') : '미수집'}</span>
          {other && <p className="operations-warning">다른 실행이 쓰는 중입니다. {server?.portOwnerDetail} 남의 실행은 끄지 않습니다.</p>}
          {server?.portOwner === 'unknown' && server.state !== 'stopping' && <p>포트 소유자를 확인하지 못했습니다.</p>}
        </dd>
      </div>
      <div className="summary-item">
        <dt>운영 버전</dt>
        <dd>{status ? <>
          {server?.release ? <><span>실행 중 실행본 </span><ReleaseLabel release={server.release} /></> : <ReleaseLabel release={status.currentRelease} />}
          {server?.release && server.release.commit !== status.currentRelease?.commit && <p>다음 시작 <ReleaseLabel release={status.currentRelease} /></p>}
        </> : '버전 데이터 미수집'}</dd>
      </div>
      <div className="summary-item"><dt>접속 현황</dt><dd>접속 데이터 미수집</dd></div>
    </dl>
  );
}

function LastExit({ exit }: { exit: ServerStatus['lastExit'] | undefined }) {
  if (!exit) return <p>마지막 종료: 종료 기록이 없습니다.</p>;
  return <p>마지막 종료: {exitKinds[exit.kind]} · {exit.exitCode !== null
    ? `종료 코드 ${exit.exitCode}`
    : exit.signal !== null ? `signal ${exit.signal}` : '종료 코드 모름'} · {localTime(exit.endedAt)}</p>;
}

function LogTable({ logs }: { logs: LogsResponse | null }) {
  if (!logs) return <div className="log-empty"><h4>아직 읽은 로그가 없습니다.</h4><p>다시 읽기로 서버 로그를 확인하세요.</p></div>;
  return <>
    {logs.truncated && <p className="operations-warning">조회 결과가 잘렸습니다. 가장 최근 줄만 표시합니다.</p>}
    <div className="server-log-scroll">
      <table className="server-log-table">
        <thead><tr><th scope="col">수집 시각</th><th scope="col">출력</th><th scope="col">내용</th></tr></thead>
        <tbody>{logs.lines.map(line => <tr key={`${line.runId}:${line.seq}`}>
          <td>{localTime(line.collectedAt)}</td><td>{line.stream}</td>
          <td><code>{line.text}</code>{line.textTruncated && <span className="log-cut"> (줄 내용 잘림)</span>}</td>
        </tr>)}</tbody>
      </table>
      {logs.lines.length === 0 && <p className="operations-note">조건에 맞는 로그가 없습니다.</p>}
    </div>
    <div className="panel-footer log-retention">
      <p>가장 오래된 수집 시각: {localTime(logs.retention.oldestCollectedAt)} · 보존 크기 {logs.retention.totalBytes.toLocaleString('ko-KR')} 바이트</p>
      <p>최근 정리: {logs.retention.recentDeletions.length === 0 ? '없음' : ''}</p>
      {logs.retention.recentDeletions.map((entry, index) => <p key={`${entry.from}:${index}`}>
        {localTime(entry.from)} ~ {localTime(entry.to)} · {entry.bytes.toLocaleString('ko-KR')} 바이트 · {entry.reason === 'age' ? '보존 기간' : '보존 용량'}
      </p>)}
      <p>시각은 현지 수집 시각이며, 로그가 발생한 시각과 다를 수 있습니다.</p>
    </div>
  </>;
}

export default function ServerOperations({ onConnectionChange }: { onConnectionChange: (state: ConnectionState) => void }) {
  const bridge = window.serverOperations;
  const [connection, setConnection] = useState<ConnectionState>({ state: 'starting' });
  const [status, setStatus] = useState<ServerStatus | null>(null);
  const [lastChecked, setLastChecked] = useState<string | null>(null);
  const [statusError, setStatusError] = useState('');
  const [commandError, setCommandError] = useState('');
  const [busy, setBusy] = useState('');
  const [candidate, setCandidate] = useState<ReleaseCandidate | null>(null);
  const [candidateError, setCandidateError] = useState('');
  const [builtCommit, setBuiltCommit] = useState<string | null>(null);
  const [confirmForce, setConfirmForce] = useState(false);
  const [logs, setLogs] = useState<LogsResponse | null>(null);
  const [logError, setLogError] = useState('');
  const [logsBusy, setLogsBusy] = useState(false);
  const [minutes, setMinutes] = useState(10);
  const [words, setWords] = useState('');
  const [visible, setVisible] = useState(!document.hidden);
  const lifecycle = useRef(0);
  const mounted = useRef(false);
  const statusSequence = useRef(0);
  const candidateSequence = useRef(0);
  const logSequence = useRef(0);
  const commandPending = useRef(false);

  const refreshStatus = useCallback(async () => {
    if (!bridge) return;
    const sequence = ++statusSequence.current;
    const current = () => mounted.current && sequence === statusSequence.current;
    try {
      const [connectionResult, result] = await Promise.all([bridge.readConnection(), bridge.readStatus()]);
      if (!current()) return;
      if (connectionResult.ok) {
        setConnection(connectionResult.connection);
        onConnectionChange(connectionResult.connection);
      }
      if (result.ok) {
        setStatus(result.status);
        setLastChecked(new Date().toISOString());
        setStatusError('');
      } else {
        // A failed refresh changes the notice, not the last readable snapshot.
        setStatusError(result.message);
      }
    } catch {
      if (current()) setStatusError('관리 연결에서 상태 응답을 받지 못했습니다.');
    }
  }, [bridge, onConnectionChange]);

  const refreshCandidate = useCallback(async () => {
    if (!bridge) return;
    const sequence = ++candidateSequence.current;
    try {
      const result = await bridge.readReleaseCandidate();
      if (!mounted.current || sequence !== candidateSequence.current) return;
      if (result.ok) {
        setCandidate(result);
        setCandidateError('');
        setBuiltCommit(previous => result.checkout.state === 'known' && result.checkout.head === previous ? previous : null);
      } else {
        setCandidateError(result.message);
      }
    } catch {
      if (mounted.current && sequence === candidateSequence.current) setCandidateError('운영 버전 정보를 읽지 못했습니다.');
    }
  }, [bridge]);

  const refreshLogs = useCallback(async (nextWords = words) => {
    if (!bridge) return;
    const contains = nextWords.trim() === '' ? [] : nextWords.split(',').map(word => word.trim());
    const query = parseLogQuery({ minutes, contains, limit: 1000 });
    // A rejected query also invalidates an earlier result for another filter.
    const sequence = ++logSequence.current;
    if (!query) {
      setLogsBusy(false);
      setLogError(serverOperationMessage('invalidQuery'));
      return;
    }
    setLogsBusy(true);
    try {
      const result = await bridge.readLogs(query);
      if (!mounted.current || sequence !== logSequence.current) return;
      if (result.ok) {
        setLogs(result.logs);
        setLogError('');
      } else {
        setLogError(result.message);
      }
    } catch {
      if (mounted.current && sequence === logSequence.current) setLogError('서버 로그 응답을 받지 못했습니다.');
    } finally {
      if (mounted.current && sequence === logSequence.current) setLogsBusy(false);
    }
  }, [bridge, minutes, words]);

  useEffect(() => {
    mounted.current = true;
    lifecycle.current += 1;
    let active = true;
    let timer: ReturnType<typeof setTimeout> | undefined;
    let periodicPending = false;
    const periodic = async () => {
      if (!active || document.hidden || periodicPending) return;
      periodicPending = true;
      try {
        await refreshStatus();
      } finally {
        periodicPending = false;
        // The interval starts at settlement, not dispatch: slow reads never
        // overlap each other. Command/visibility reads have their own sequence.
        if (active && !document.hidden) timer = setTimeout(() => { void periodic(); }, 2000);
      }
    };
    const visibilityChanged = () => {
      setVisible(!document.hidden);
      clearTimeout(timer);
      if (!document.hidden) {
        if (periodicPending) void refreshStatus();
        else void periodic();
      }
    };
    document.addEventListener('visibilitychange', visibilityChanged);
    void periodic();
    return () => {
      active = false;
      mounted.current = false;
      lifecycle.current += 1;
      // StrictMode cleanup invalidates every pending result before remount.
      statusSequence.current += 1;
      candidateSequence.current += 1;
      logSequence.current += 1;
      commandPending.current = false;
      clearTimeout(timer);
      document.removeEventListener('visibilitychange', visibilityChanged);
    };
  }, [refreshStatus]);

  const connected = connection.state === 'connected';
  useEffect(() => {
    if (connected) void refreshCandidate();
  }, [connected, refreshCandidate]);

  useEffect(() => {
    if (!connected || !visible || status?.server.state !== 'running') return;
    let active = true;
    let timer: ReturnType<typeof setTimeout>;
    const poll = async () => {
      if (!active || document.hidden) return;
      await refreshLogs();
      if (active) timer = setTimeout(() => { void poll(); }, 5000);
    };
    timer = setTimeout(() => { void poll(); }, 5000);
    return () => {
      active = false;
      clearTimeout(timer);
    };
  }, [connected, visible, status?.server.state, refreshLogs]);

  async function runCommand(label: string, call: () => Promise<OperationResult>, success?: () => void) {
    if (commandPending.current) return;
    const current = lifecycle.current;
    commandPending.current = true;
    setBusy(label);
    setCommandError('');
    try {
      const result = await call();
      if (!mounted.current || lifecycle.current !== current) return;
      if (result.ok) success?.();
      else setCommandError(result.message);
    } catch {
      if (mounted.current && lifecycle.current === current) setCommandError('관리 요청의 응답을 받지 못했습니다.');
    } finally {
      if (mounted.current && lifecycle.current === current) {
        commandPending.current = false;
        setBusy('');
        void refreshStatus();
      }
    }
  }

  const head = candidate?.checkout.state === 'known' ? candidate.checkout.head : null;
  const controlsBlocked = !connected || busy !== '';
  const reason = connection.state === 'disconnected'
    ? `${serverOperationMessage(connection.reason)}${connection.exitCode !== undefined ? ` 종료 코드 ${connection.exitCode ?? '모름'}` : ''}`
    : connection.state === 'starting' ? '관리 백엔드를 시작하는 중입니다.'
      : connection.state === 'stopping' ? '관리 백엔드를 종료하는 중입니다.' : '관리 백엔드와 연결됐습니다.';

  return (
    <div className="server-operations">
      <aside className="connection-notice" aria-label="연결 상태">
        <strong>{connected ? '관리 기능 연결됨' : '관리 기능 미연결'}</strong><p>{reason}</p>
        {!connected && <button type="button" disabled={busy !== '' || connection.state !== 'disconnected'} onClick={() => {
          if (bridge) {
            void runCommand('다시 연결 중', () => bridge.connect(), () => { void refreshCandidate(); });
          }
        }}>다시 연결</button>}
      </aside>
      <StatusSummary status={status} />
      {lastChecked && <p className="operations-note">마지막 확인 시각: {localTime(lastChecked)}{statusError && ' · 이전 정상 값 표시 중'}</p>}
      {statusError && <p className="operations-warning" role="alert">{statusError}</p>}
      <section className="operations-bar" aria-labelledby="operations-control-heading">
        <div><h3 id="operations-control-heading">운영 제어</h3><LastExit exit={status?.lastExit} /></div>
        <div className="control-buttons">
          <button type="button" disabled={controlsBlocked || status?.server.state !== 'stopped'} onClick={() => {
            if (bridge) void runCommand('서버 시작 중', bridge.startServer);
          }}>서버 시작</button>
          <button type="button" disabled={controlsBlocked || status?.server.state !== 'running'} onClick={() => {
            if (bridge) void runCommand('서버 종료 요청 중', bridge.stopServer);
          }}>서버 종료</button>
          {status?.server.state === 'stopping' && status.server.stopTimedOut && <button type="button" disabled={controlsBlocked} onClick={() => setConfirmForce(true)}>강제 종료</button>}
        </div>
      </section>
      {busy && <p role="status">{busy}</p>}
      {commandError && <p className="operations-warning" role="alert">{commandError}</p>}
      {confirmForce && <div className="operations-dialog-backdrop">
        <div className="operations-dialog" role="alertdialog" aria-modal="true" aria-labelledby="force-stop-title" onKeyDown={event => {
          if (event.key === 'Escape') setConfirmForce(false);
          if (event.key !== 'Tab') return;
          const buttons = event.currentTarget.querySelectorAll<HTMLButtonElement>('button:not(:disabled)');
          const first = buttons[0];
          const last = buttons[buttons.length - 1];
          if (event.shiftKey && document.activeElement === first) {
            event.preventDefault();
            last?.focus();
          } else if (!event.shiftKey && document.activeElement === last) {
            event.preventDefault();
            first?.focus();
          }
        }}>
          <h3 id="force-stop-title">서버를 강제 종료할까요?</h3><p>정상 종료 시간 안에 끝나지 않았습니다. 진행 중인 작업이 중단될 수 있습니다.</p>
          <div className="control-buttons">
            <button type="button" autoFocus onClick={() => setConfirmForce(false)}>취소</button>
            <button type="button" disabled={controlsBlocked || !status?.server.stopTimedOut} onClick={() => {
              setConfirmForce(false);
              if (bridge) void runCommand('서버 강제 종료 중', bridge.forceStopServer);
            }}>강제 종료 실행</button>
          </div>
        </div>
      </div>}
      <section className="work-panel release-panel" aria-labelledby="release-heading">
        <div className="panel-heading"><h3 id="release-heading">운영 버전 올리기</h3></div>
        <div className="operations-panel-body">
          <p>checkout branch: {candidate?.checkout.state === 'known' ? candidate.checkout.branch ?? '분리된 HEAD' : '확인 중'}</p>
          <p>HEAD commit: <code className="release-commit">{head ?? '확인 불가'}</code></p>
          <p>커밋하지 않은 작업 트리 변경은 운영 버전에 들어가지 않습니다. 개발 checkout을 고쳐도 운영 버전은 이 버튼을 누르기 전에는 바뀌지 않습니다.</p>
          <p>지정한 운영 버전은 다음 시작부터 사용합니다.</p>
          {candidateError && <p className="operations-warning" role="alert">{candidateError}</p>}
          <div className="control-buttons">
            <button type="button" disabled={controlsBlocked} onClick={() => { void refreshCandidate(); }}>운영 버전 정보 다시 읽기</button>
            <button type="button" disabled={controlsBlocked || !head} onClick={() => {
              if (bridge && head) {
                void runCommand('운영 실행본 빌드 중 (수 분 걸릴 수 있습니다)', () => bridge.buildRelease({ commit: head }), () => setBuiltCommit(head));
              }
            }}>이 commit으로 운영 버전 만들기</button>
            <button type="button" disabled={controlsBlocked || !head || builtCommit !== head} onClick={() => {
              if (bridge && head) {
                void runCommand('현재 운영 버전 지정 중', () => bridge.selectRelease({ commit: head }), () => { void refreshCandidate(); });
              }
            }}>현재 운영 버전으로 지정</button>
          </div>
          {busy.startsWith('운영 실행본 빌드') && <progress aria-label="운영 실행본 빌드 중" />}
        </div>
      </section>
      <section className="work-panel log-panel" aria-labelledby="server-logs-heading">
        <div className="panel-heading"><h3 id="server-logs-heading">서버 로그</h3><span>원본 기록</span></div>
        <div className="server-log-filters">
          <div className="log-periods" aria-label="조회 기간">{[10, 30, 60].map(value => <button key={value} type="button" aria-pressed={minutes === value} onClick={() => setMinutes(value)}>{value}분</button>)}</div>
          <label>찾을 낱말 <input value={words} placeholder="최대 5개, 쉼표로 구분" onChange={event => setWords(event.target.value)} /></label>
          <div className="control-buttons">
            <button type="button" disabled={!connected} onClick={() => {
              setWords('error, 오류');
              void refreshLogs('error, 오류');
            }}>오류</button>
            <button type="button" disabled={!connected} onClick={() => {
              setWords('warn, 경고');
              void refreshLogs('warn, 경고');
            }}>경고</button>
            <button type="button" disabled={!connected || logsBusy} onClick={() => { void refreshLogs(); }}>다시 읽기</button>
          </div>
          <p>실행 중이고 이 화면이 보이는 동안 5초마다 읽습니다.</p>
        </div>
        {logsBusy && <p className="operations-note" role="status">서버 로그를 읽는 중입니다.</p>}
        {logError && <p className="operations-warning" role="alert">{logError}{logs && ' 이전 조회 결과를 표시합니다.'}</p>}
        <LogTable logs={logs} />
      </section>
    </div>
  );
}

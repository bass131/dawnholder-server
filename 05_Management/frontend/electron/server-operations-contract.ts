import type { CheckoutInfo } from './checkout-contract.js';

export type OperationResult<T extends object = Record<never, never>> =
  | ({ ok: true } & T)
  | { ok: false; code: string; message: string };

export type ConnectionState =
  | { state: 'starting' | 'connected' | 'stopping' }
  | { state: 'disconnected'; reason: string; exitCode?: number | null };

export interface ReleaseInfo { commit: string; builtAt: string }
export interface ServerStatus {
  backend: { startedAt: string; pid: number };
  server: {
    serverId: string;
    displayName: string;
    state: 'stopped' | 'starting' | 'running' | 'stopping';
    pid: number | null;
    runId: string | null;
    startedAt: string | null;
    release: ReleaseInfo | null;
    stopRequestedAt: string | null;
    stopTimedOut: boolean;
    port: number;
    portListening: boolean;
    portOwner: 'none' | 'self' | 'other' | 'unknown';
    portOwnerDetail: string | null;
    portLockHeldByOther: boolean;
  };
  currentRelease: ReleaseInfo | null;
  lastExit: {
    runId: string;
    endedAt: string;
    kind: 'graceful' | 'forced' | 'abnormal' | 'startFailed' | 'unknown';
    exitCode: number | null;
    signal: number | null;
  } | null;
}

export interface ReleasesResponse {
  releases: (ReleaseInfo & { sdkVersion: string; sourceRepository: string })[];
  currentRelease: ReleaseInfo | null;
}
export interface LogQuery { minutes: number; contains: string[]; limit: number }
export interface LogsResponse {
  serverId: string;
  from: string;
  to: string;
  lines: {
    runId: string;
    seq: number;
    collectedAt: string;
    stream: 'stdout' | 'stderr';
    text: string;
    textTruncated: boolean;
  }[];
  truncated: boolean;
  retention: {
    oldestCollectedAt: string | null;
    totalBytes: number;
    recentDeletions: { from: string; to: string; bytes: number; reason: 'age' | 'size' }[];
  };
}
export type ReleaseCandidate = { checkout: CheckoutInfo; currentRelease: ReleaseInfo | null };
export interface ServerOperationsBridge {
  readConnection(): Promise<OperationResult<{ connection: ConnectionState }>>;
  connect(): Promise<OperationResult<{ connection: ConnectionState }>>;
  readStatus(): Promise<OperationResult<{ status: ServerStatus }>>;
  startServer(): Promise<OperationResult>;
  stopServer(): Promise<OperationResult>;
  forceStopServer(): Promise<OperationResult>;
  readLogs(query: LogQuery): Promise<OperationResult<{ logs: LogsResponse }>>;
  readReleaseCandidate(): Promise<OperationResult<ReleaseCandidate>>;
  buildRelease(input: { commit: string }): Promise<OperationResult>;
  selectRelease(input: { commit: string }): Promise<OperationResult>;
}

function isObject(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}
function isInteger(value: unknown, min: number, max = Number.MAX_SAFE_INTEGER): value is number {
  return typeof value === 'number' && Number.isSafeInteger(value) && value >= min && value <= max;
}
function isText(value: unknown): value is string { return typeof value === 'string'; }
function isTime(value: unknown): value is string {
  return isText(value) && Number.isFinite(Date.parse(value));
}
function nullable<T>(value: unknown, check: (item: unknown) => item is T): value is T | null {
  return value === null || check(value);
}
function oneOf<T extends string>(value: unknown, values: readonly T[]): value is T {
  return values.some(item => item === value);
}
function isRelease(value: unknown): value is ReleaseInfo {
  return isObject(value) && isText(value.commit) && isTime(value.builtAt);
}
function releaseCopy(value: ReleaseInfo | null): ReleaseInfo | null {
  return value === null ? null : { commit: value.commit, builtAt: value.builtAt };
}

export function toWslPath(path: string): string | null {
  if (!/^[a-zA-Z]:[\\/]/.test(path) || /[\u0000-\u001f]/.test(path)) return null;
  const tail = path.slice(3).replaceAll('\\', '/').replace(/\/+$/, '');
  return `/mnt/${path[0]?.toLowerCase()}${tail ? `/${tail}` : ''}`;
}

export function parseLogQuery(input: unknown): LogQuery | null {
  if (!isObject(input) || !isInteger(input.minutes, 1, 1440) || !isInteger(input.limit, 1, 5000)) return null;
  if (!Array.isArray(input.contains) || input.contains.length > 5) return null;
  const contains: string[] = [];
  for (const word of input.contains) {
    if (!isText(word) || word.length < 1 || word.length > 64) return null;
    contains.push(word);
  }
  return { minutes: input.minutes, contains, limit: input.limit };
}

export function parseCommitInput(input: unknown): string | null {
  return isObject(input) && isText(input.commit) && /^[a-f0-9]{40}$/.test(input.commit) ? input.commit : null;
}

export function parseStatusResponse(body: unknown): ServerStatus | null {
  if (!isObject(body) || !isObject(body.backend) || !isObject(body.server)) return null;
  const backend = body.backend;
  const server = body.server;
  if (!isTime(backend.startedAt) || !isInteger(backend.pid, 1)
    || !isText(server.serverId) || !isText(server.displayName)
    || !oneOf(server.state, ['stopped', 'starting', 'running', 'stopping'] as const)
    || !nullable(server.pid, (value): value is number => isInteger(value, 1))
    || !nullable(server.runId, isText) || !nullable(server.startedAt, isTime)
    || !nullable(server.release, isRelease) || !nullable(server.stopRequestedAt, isTime)
    || typeof server.stopTimedOut !== 'boolean' || !isInteger(server.port, 1, 65535)
    || typeof server.portListening !== 'boolean'
    || !oneOf(server.portOwner, ['none', 'self', 'other', 'unknown'] as const)
    || !nullable(server.portOwnerDetail, isText) || typeof server.portLockHeldByOther !== 'boolean'
    || !nullable(body.currentRelease, isRelease)) return null;

  let lastExit: ServerStatus['lastExit'] = null;
  if (body.lastExit !== null) {
    const exit = body.lastExit;
    if (!isObject(exit) || !isText(exit.runId) || !isTime(exit.endedAt)
      || !oneOf(exit.kind, ['graceful', 'forced', 'abnormal', 'startFailed', 'unknown'] as const)
      || !nullable(exit.exitCode, (value): value is number => isInteger(value, -2147483648, 2147483647))
      || !nullable(exit.signal, (value): value is number => isInteger(value, 0))) return null;
    lastExit = { runId: exit.runId, endedAt: exit.endedAt, kind: exit.kind, exitCode: exit.exitCode, signal: exit.signal };
  }
  // Return only the public fields: unexpected backend fields never cross IPC.
  return {
    backend: { startedAt: backend.startedAt, pid: backend.pid },
    server: {
      serverId: server.serverId, displayName: server.displayName, state: server.state,
      pid: server.pid, runId: server.runId, startedAt: server.startedAt, release: releaseCopy(server.release),
      stopRequestedAt: server.stopRequestedAt, stopTimedOut: server.stopTimedOut,
      port: server.port, portListening: server.portListening, portOwner: server.portOwner,
      portOwnerDetail: server.portOwnerDetail, portLockHeldByOther: server.portLockHeldByOther,
    },
    currentRelease: releaseCopy(body.currentRelease), lastExit,
  };
}

export function parseReleasesResponse(body: unknown): ReleasesResponse | null {
  if (!isObject(body) || !Array.isArray(body.releases) || !nullable(body.currentRelease, isRelease)) return null;
  const releases: ReleasesResponse['releases'] = [];
  for (const item of body.releases) {
    if (!isObject(item) || !isRelease(item) || !isText(item.sdkVersion) || !isText(item.sourceRepository)) return null;
    releases.push({ commit: item.commit, builtAt: item.builtAt, sdkVersion: item.sdkVersion, sourceRepository: item.sourceRepository });
  }
  return { releases, currentRelease: releaseCopy(body.currentRelease) };
}

export function parseLogsResponse(body: unknown): LogsResponse | null {
  if (!isObject(body) || !isText(body.serverId) || !isTime(body.from) || !isTime(body.to)
    || !Array.isArray(body.lines) || typeof body.truncated !== 'boolean' || !isObject(body.retention)) return null;
  const retention = body.retention;
  if (!nullable(retention.oldestCollectedAt, isTime) || !isInteger(retention.totalBytes, 0)
    || !Array.isArray(retention.recentDeletions)) return null;
  const lines: LogsResponse['lines'] = [];
  for (const item of body.lines) {
    if (!isObject(item) || !isText(item.runId) || !isInteger(item.seq, 0) || !isTime(item.collectedAt)
      || !oneOf(item.stream, ['stdout', 'stderr'] as const) || !isText(item.text) || typeof item.textTruncated !== 'boolean') return null;
    lines.push({ runId: item.runId, seq: item.seq, collectedAt: item.collectedAt, stream: item.stream, text: item.text, textTruncated: item.textTruncated });
  }
  const recentDeletions: LogsResponse['retention']['recentDeletions'] = [];
  for (const item of retention.recentDeletions) {
    if (!isObject(item) || !isTime(item.from) || !isTime(item.to) || !isInteger(item.bytes, 0)
      || !oneOf(item.reason, ['age', 'size'] as const)) return null;
    recentDeletions.push({ from: item.from, to: item.to, bytes: item.bytes, reason: item.reason });
  }
  return {
    serverId: body.serverId, from: body.from, to: body.to, lines, truncated: body.truncated,
    retention: { oldestCollectedAt: retention.oldestCollectedAt, totalBytes: retention.totalBytes, recentDeletions },
  };
}

const messages: Readonly<Record<string, string>> = {
  busy: '다른 작업을 처리 중입니다. 끝난 뒤 다시 시도하세요.',
  alreadyRunning: '서버가 이미 실행 중입니다.',
  noCurrentRelease: '현재 운영 버전이 없습니다. 운영 버전을 만들고 지정하세요.',
  portBusy: '서버 포트를 다른 실행이 쓰고 있습니다. 남의 실행은 끄지 않습니다.',
  startFailed: '서버를 시작하지 못했습니다. 서버 로그를 확인하세요.',
  notRunning: '실행 중인 서버가 없습니다.',
  invalidCommit: '운영 버전 commit은 소문자 16진수 40자여야 합니다.',
  commitNotFound: '저장소에서 해당 commit을 찾지 못했습니다.',
  releaseNotFound: '해당 운영 실행본이 없습니다. 먼저 빌드하세요.',
  releaseSourceNotConfigured: '운영 실행본의 원천 저장소가 설정되지 않았습니다.',
  buildFailed: '운영 실행본을 빌드하지 못했습니다. 관리 백엔드 출력을 확인하세요.',
  invalidQuery: '로그 조회는 1~1440분, 낱말 최대 5개(각 1~64자), 최대 5000줄입니다.',
  logReadFailed: '서버 로그를 읽지 못했습니다.',
  operationFailed: '관리 백엔드가 작업을 완료하지 못했습니다.',
  invalidRequest: '요청 형식이 올바르지 않습니다.',
  bodyTooLarge: '요청 내용이 허용 크기를 넘었습니다.',
  notFound: '요청한 관리 기능을 찾지 못했습니다.',
  forbidden: '이 요청에는 관리 기능 접근 권한이 없습니다.',
  unauthorized: '관리 연결 인증이 맞지 않습니다. 다시 연결하세요.',
  invalidResponse: '관리 백엔드 응답 형식이 올바르지 않습니다.',
  notConnected: '관리 백엔드에 연결되지 않았습니다.',
  backendUnreachable: '관리 백엔드에 응답을 받지 못했습니다. 다시 연결하세요.',
  startTimeout: '관리 백엔드 시작 시간이 초과됐습니다. 기본 데이터 폴더를 바꿨는지도 확인하세요.',
  invalidOverride: '검증용 설정 경로와 데이터 폴더는 함께 지정해야 합니다.',
  backendExited: '관리 백엔드가 종료됐습니다.',
  invalidRepository: '이 checkout의 Windows 경로를 WSL 경로로 바꾸지 못했습니다.',
  headChanged: 'checkout HEAD와 commit이 다릅니다. 운영 버전 정보를 다시 읽으세요.',
};

export function serverOperationMessage(code: string): string {
  return Object.hasOwn(messages, code) ? messages[code] ?? '관리 요청을 완료하지 못했습니다.' : '관리 요청을 완료하지 못했습니다.';
}
export function operationFailure(code: string): Extract<OperationResult, { ok: false }> {
  // An unknown backend code is untrusted text, just like its message body.
  return { ok: false, code: Object.hasOwn(messages, code) ? code : 'unknown', message: serverOperationMessage(code) };
}

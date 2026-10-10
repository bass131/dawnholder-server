// Shared by the server operations tests (screen-design.md 「창 프로세스와 백엔드」·「IPC 계약」):
// backend answers written in the backend-design.md 「상태 응답」·「실행본 목록 응답」·「로그 조회」 shapes,
// and the double of electron/backend-connection.ts that every test starting electron/main.ts uses.
//
// The connection double is the outside contract the window-side implementation follows, the same one
// tests/backend-connection.test.ts drives on the real module: the module exports
// createBackendConnection(options) and the connection has connect, connectionState, readStatus,
// startServer, stopServer, forceStopServer, readLogs, readReleases, buildRelease, selectRelease and
// shutdown. Ports here are made-up values; no test listens on or connects to them.
import { vi } from 'vitest';

export const RUNNING_RELEASE = { commit: 'abcdef0123456789abcdef0123456789abcdef01', builtAt: '2026-10-10T01:00:00Z' };
export const NEXT_RELEASE = { commit: '13579bdf13579bdf13579bdf13579bdf13579bdf', builtAt: '2026-10-10T02:00:00Z' };

export const STATUS_STOPPED = {
  backend: { startedAt: '2026-10-10T00:00:00Z', pid: 501 },
  server: {
    serverId: '6f1c2e4a-0b7d-4c55-9a51-2f0f5d1c9e11', displayName: 'Dawnholder 라이브 서버',
    state: 'stopped', pid: null, runId: null, startedAt: null,
    release: null,
    stopRequestedAt: null, stopTimedOut: false,
    port: 41777, portListening: false,
    portOwner: 'none', portOwnerDetail: null,
    portLockHeldByOther: false,
  },
  currentRelease: RUNNING_RELEASE,
  lastExit: { runId: 'run-0', endedAt: '2026-10-10T00:30:00Z', kind: 'graceful', exitCode: 0, signal: null },
};

export const STATUS_RUNNING = {
  ...STATUS_STOPPED,
  server: {
    ...STATUS_STOPPED.server,
    state: 'running', pid: 4321, runId: 'run-1', startedAt: '2026-10-10T03:04:05Z',
    release: RUNNING_RELEASE,
    portListening: true, portOwner: 'self',
  },
};

export const RELEASES = {
  releases: [
    { commit: RUNNING_RELEASE.commit, builtAt: RUNNING_RELEASE.builtAt, sdkVersion: '10.0.301', sourceRepository: '/mnt/c/Dev/DawnHolder_Project' },
  ],
  currentRelease: RUNNING_RELEASE,
};

export const LOGS = {
  serverId: STATUS_STOPPED.server.serverId,
  from: '2026-10-10T02:54:05Z',
  to: '2026-10-10T03:04:05Z',
  lines: [
    { runId: 'run-1', seq: 1, collectedAt: '2026-10-10T03:04:05.120Z', stream: 'stdout', text: 'Server started', textTruncated: false },
    { runId: 'run-1', seq: 2, collectedAt: '2026-10-10T03:04:06.500Z', stream: 'stderr', text: 'warning: tick took 120 ms', textTruncated: false },
  ],
  truncated: false,
  retention: { oldestCollectedAt: '2026-10-03T03:04:05Z', totalBytes: 2048, recentDeletions: [] },
};

export function backendConnectionDouble() {
  return {
    connect: vi.fn(async (): Promise<Record<string, unknown>> => ({ state: 'connected' })),
    connectionState: vi.fn((): Record<string, unknown> => ({ state: 'connected' })),
    readStatus: vi.fn(async (): Promise<Record<string, unknown>> => ({ ok: true, status: STATUS_STOPPED })),
    startServer: vi.fn(async (): Promise<Record<string, unknown>> => ({ ok: true })),
    stopServer: vi.fn(async (): Promise<Record<string, unknown>> => ({ ok: true })),
    forceStopServer: vi.fn(async (): Promise<Record<string, unknown>> => ({ ok: true })),
    readLogs: vi.fn(async (_query: unknown): Promise<Record<string, unknown>> => ({ ok: true, logs: LOGS })),
    readReleases: vi.fn(async (): Promise<Record<string, unknown>> => ({ ok: true, releases: RELEASES })),
    buildRelease: vi.fn(async (_commit: unknown): Promise<Record<string, unknown>> => ({ ok: true })),
    selectRelease: vi.fn(async (_commit: unknown): Promise<Record<string, unknown>> => ({ ok: true })),
    shutdown: vi.fn(async (): Promise<void> => undefined),
  };
}

export type BackendConnectionDouble = ReturnType<typeof backendConnectionDouble>;
export interface CreatedConnection { options: unknown; connection: BackendConnectionDouble }

// The module object for vi.mock('../electron/backend-connection.js'): main.ts imports only the factory.
export function backendConnectionModule(created: CreatedConnection[] = []) {
  return {
    createBackendConnection: (options: unknown) => {
      const connection = backendConnectionDouble();
      created.push({ options, connection });
      return connection;
    },
  };
}

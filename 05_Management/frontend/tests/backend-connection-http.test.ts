// @vitest-environment node
// Requirement: screen-design.md 「HTTP 대화」 — 「창 프로세스의 Node HTTP로 `http://127.0.0.1:<포트>`에 요청한다.
// `Authorization: Bearer <비밀값>`을 붙이고 `Origin`을 보내지 않는다」 and 「요청마다 시간 상한을 둔다: 상태 … 5초」, with
// backend-design.md 「관리 접근 경계」 (the backend refuses a request whose Host is not 127.0.0.1:<port> or
// localhost:<port>, or that carries Origin).
//
// Independent verifier test (PR2 verification). The other connection tests replace request() with a double; this one
// keeps the module's own node:http request and answers from a real listener on 127.0.0.1 at a port the kernel picks
// (never 7777 or 14333). wsl.exe stays a double. Real time: the limit test waits about 5 s.
import { createServer } from 'node:http';
import type { IncomingHttpHeaders, Server } from 'node:http';
import type { AddressInfo } from 'node:net';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { STATUS_STOPPED } from './server-operations-fixtures';

const HOME = '/home/tester';
const CONNECTION_FILE = `${HOME}/.local/share/dawnholder/management/connection.json`;
const TOKEN = 'tok-real-http';
const STATUS_LIMIT_MS = 5_000;

interface Seen { method: string | undefined; url: string | undefined; headers: IncomingHttpHeaders; closedAt?: number }

let server: Server;
let port = 0;
let mode: 'answer' | 'hang' = 'answer';
let seen: Seen[] = [];

beforeEach(async () => {
  mode = 'answer';
  seen = [];
  server = createServer((request, response) => {
    const entry: Seen = { method: request.method, url: request.url, headers: { ...request.headers } };
    seen.push(entry);
    request.socket.once('close', () => { entry.closedAt = Date.now(); });
    if (mode === 'hang') return;
    response.writeHead(200, { 'Content-Type': 'application/json' });
    response.end(JSON.stringify(STATUS_STOPPED));
  });
  await new Promise<void>(resolve => server.listen(0, '127.0.0.1', resolve));
  port = (server.address() as AddressInfo).port;
});

afterEach(async () => {
  server.closeAllConnections();
  await new Promise<void>(resolve => server.close(() => resolve()));
});

async function attached() {
  const runProcess = vi.fn(async (_file: string, args: readonly string[]) => {
    const command = args.slice(3).join(' ');
    if (command === 'printenv HOME') return { exitCode: 0, stdout: `${HOME}\n`, stderr: '' };
    if (command === `cat ${CONNECTION_FILE}`) {
      return { exitCode: 0, stdout: JSON.stringify({ port, token: TOKEN, pid: 4242, startedAt: '2026-10-10T01:00:00Z' }), stderr: '' };
    }
    if (command === 'kill -0 4242') return { exitCode: 0, stdout: '', stderr: '' };
    return { exitCode: 1, stdout: '', stderr: 'unexpected command\n' };
  });
  const spawnProcess = vi.fn(() => {
    throw new Error('no child is expected: the recorded backend answers');
  });
  const { createBackendConnection } = await import('../electron/backend-connection.js');
  const connection = createBackendConnection({ repositoryRoot: 'C:\\Users\\tester\\checkout', env: {}, runProcess, spawnProcess, log: vi.fn() });
  const state = await connection.connect();
  return { connection, state };
}

describe('the connection object over real node:http (independent verifier)', () => {
  it('attaches and reads the status from 127.0.0.1 with the bearer secret, the loopback Host and no Origin', async () => {
    const { connection, state } = await attached();
    seen = [];

    const result = await connection.readStatus();
    const request = seen[0];

    expect(state).toMatchObject({ state: 'connected' });
    expect(result.ok).toBe(true);
    expect(seen).toHaveLength(1);
    expect({ method: request?.method, url: request?.url }).toEqual({ method: 'GET', url: '/api/status' });
    expect(request?.headers.authorization).toBe(`Bearer ${TOKEN}`);
    expect(request?.headers.host).toBe(`127.0.0.1:${port}`);
    expect(request?.headers.origin).toBeUndefined();
  });

  it('gives up a status request that gets no answer after 5 s and closes that connection', async () => {
    const { connection } = await attached();
    mode = 'hang';
    seen = [];

    const started = Date.now();
    const result = await connection.readStatus();
    const settledAfterMs = Date.now() - started;
    await vi.waitFor(() => expect(seen[0]?.closedAt).toBeTypeOf('number'), { timeout: 2_000, interval: 50 });
    const closedAfterMs = (seen[0]?.closedAt ?? Number.NaN) - started;

    expect(result.ok).toBe(false);
    expect(settledAfterMs).toBeGreaterThanOrEqual(STATUS_LIMIT_MS - 100);
    expect(settledAfterMs).toBeLessThan(STATUS_LIMIT_MS + 1_500);
    expect(closedAfterMs).toBeLessThan(STATUS_LIMIT_MS + 1_500);
    expect(connection.connectionState().state).toBe('connected');
  }, 20_000);
});

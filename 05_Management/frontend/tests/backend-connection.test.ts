// @vitest-environment node
// Requirement: screen-design.md 「단일 소유자」 (one connection object owns the backend child, the
// connection details and the connection state starting·connected·disconnected·stopping), 「시작과
// 붙기」 1~6, 「HTTP 대화」, 「백엔드가 죽거나 끊길 때」 and 「앱 종료(트레이 「종료」) — 잠정」, and
// backend-design.md 「관리 접근 경계」 (127.0.0.1, Authorization: Bearer, no Origin, connection file
// { port, token, pid, startedAt }).
//
// Outside contract for the implementer (electron/backend-connection.ts):
//   createBackendConnection({ repositoryRoot, env, spawnProcess, runProcess, request, clock, log })
//   - repositoryRoot: the Windows checkout path main.ts already has; env: process.env in main.ts.
//   - spawnProcess(file, args) starts a long-lived child and returns a Node ChildProcess-like object
//     (pid, stdout and stderr streams, kill(signal?), 'exit' and 'close' events with (code, signal)).
//   - runProcess(file, args) runs a command once → Promise<{ exitCode, stdout, stderr }>.
//   - request({ host, port, method, path, headers, body?, timeoutMs?, signal? }) → Promise<{ statusCode,
//     body }> with the raw body text; it rejects when the connection is refused or times out.
//   - clock: { now(), sleep(ms) }; log(line) receives console lines.
//   The connection has connect(), connectionState(), readStatus(), startServer(), stopServer(),
//   forceStopServer(), readLogs({ minutes, contains, limit }), readReleases(), buildRelease(commit),
//   selectRelease(commit) and shutdown(). Results are { ok: true, … } | { ok: false, code, message };
//   readStatus gives { ok: true, status }, readLogs { ok: true, logs }, readReleases { ok: true, releases }.
//   A disconnected state carries reason (startTimeout, invalidOverride, backendUnreachable,
//   backendExited) and, for backendExited, exitCode.
//
// Every I/O double is passed in, so nothing here starts wsl.exe, a process, a socket or a file. The
// doubles form one consistent fake WSL: a backend that is not alive refuses its port, and any command
// other than printenv, cat or kill -TERM/-KILL that names a pid is answered as a liveness check, so
// the tests do not depend on how the implementation checks a pid. Time is Vitest's fake clock. The
// module does not exist before the implementation step, so each test imports it and fails alone.
import { EventEmitter } from 'node:events';
import { PassThrough } from 'node:stream';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { LOGS, RELEASES, STATUS_RUNNING, STATUS_STOPPED } from './server-operations-fixtures';

const REPOSITORY_ROOT = 'C:\\Users\\bass1\\orca\\workspaces\\DawnHolder_Project\\management-active\\';
const BACKEND_SCRIPT = '/mnt/c/Users/bass1/orca/workspaces/DawnHolder_Project/management-active/05_Management/backend/backend-wsl.sh';
const WSL_PREFIX = ['-d', 'Ubuntu', '--exec'];
const RUN_ARGS = [...WSL_PREFIX, 'bash', BACKEND_SCRIPT, 'run'];
const HOME = '/home/tester';
const CONNECTION_FILE = `${HOME}/.local/share/dawnholder/management/connection.json`;
const COMMIT = '0123456789abcdef0123456789abcdef01234567';
const QUERY = { minutes: 10, contains: ['error', 'warn'], limit: 1000 };
const HANGUL = /[가-힣]/;
const STATUS_STOPPING = { ...STATUS_RUNNING, server: { ...STATUS_RUNNING.server, state: 'stopping', stopRequestedAt: '2026-10-10T03:10:00Z' } };

interface HttpRequest {
  host: string;
  port: number;
  method: string;
  path: string;
  headers: Record<string, string>;
  body?: string;
  timeoutMs?: number;
  signal?: AbortSignal;
}
interface Reply { status: number; body: unknown }
type Route = (request: HttpRequest) => Reply | 'hang';
type Result = { ok: boolean; code?: string; message?: string; [key: string]: unknown };

class FakeChild extends EventEmitter {
  readonly stdout = new PassThrough();
  readonly stderr = new PassThrough();
  exitCode: number | null = null;
  signalCode: NodeJS.Signals | null = null;
  private ending = false;
  readonly kill = vi.fn((signal?: NodeJS.Signals | number) => {
    this.end(null, typeof signal === 'string' ? signal : 'SIGTERM');
    return true;
  });

  constructor(readonly pid: number) {
    super();
  }

  // Ends the way a Node child process does: 'exit' then 'close', a moment after the cause.
  end(code: number | null, signal: NodeJS.Signals | null = null) {
    if (this.ending) return;
    this.ending = true;
    setTimeout(() => {
      this.exitCode = code;
      this.signalCode = signal;
      this.stdout.end();
      this.stderr.end();
      this.emit('exit', code, signal);
      this.emit('close', code, signal);
    }, 10);
  }
}

interface FakeBackend {
  pid: number;
  port: number;
  token: string;
  startedAt: string;
  file: string;
  alive: boolean;
  obeysTerm: boolean;
  refuse: boolean;
  child: FakeChild | null;
  routes: Map<string, Route>;
}

interface BackendSpec {
  pid: number;
  port: number;
  token: string;
  startedAt: string;
  file?: string;
  child?: FakeChild | null;
  obeysTerm?: boolean;
}

function header(request: HttpRequest | undefined, name: string): string | undefined {
  return Object.entries(request?.headers ?? {}).find(([key]) => key.toLowerCase() === name)?.[1];
}

function startsWith(args: readonly string[], prefix: readonly string[]): boolean {
  return prefix.every((part, index) => args[index] === part);
}

function defaultReply(request: HttpRequest): Reply {
  switch (`${request.method} ${request.path.split('?')[0]}`) {
    case 'GET /api/status': return { status: 200, body: STATUS_STOPPED };
    case 'GET /api/logs': return { status: 200, body: LOGS };
    case 'GET /api/releases': return { status: 200, body: RELEASES };
    case 'POST /api/server/stop': return { status: 200, body: STATUS_STOPPING };
    case 'POST /api/server/start':
    case 'POST /api/server/force-stop':
    case 'POST /api/releases':
    case 'PUT /api/releases/current':
      return { status: 200, body: {} };
    default: return { status: 404, body: { error: 'notFound', message: '관리 API 경로를 찾을 수 없습니다.' } };
  }
}

function createWorld() {
  const files = new Map<string, string>();
  const backends: FakeBackend[] = [];
  const spawned: Array<{ file: string; args: string[]; child: FakeChild }> = [];
  const runs: Array<{ file: string; args: string[] }> = [];
  const requests: HttpRequest[] = [];
  const logged: string[] = [];
  let nextChildPid = 9001;

  const result = (exitCode: number, stdout: string, stderr = '') => ({ exitCode, stdout, stderr });

  // A backend that ends removes its own connection file only when it ends gracefully.
  function endBackend(backend: FakeBackend, graceful: boolean) {
    backend.alive = false;
    const own = JSON.parse(files.get(backend.file) ?? 'null') as { pid?: number } | null;
    if (graceful && own?.pid === backend.pid) files.delete(backend.file);
    backend.child?.end(graceful ? 0 : 137);
  }

  function hanging(request: HttpRequest): Promise<never> {
    return new Promise((_resolve, reject) => {
      const fail = () => reject(Object.assign(new Error('request timed out'), { code: 'ETIMEDOUT' }));
      if (typeof request.timeoutMs === 'number') setTimeout(fail, request.timeoutMs);
      request.signal?.addEventListener('abort', fail, { once: true });
    });
  }

  const world = {
    files, backends, spawned, runs, requests, logged,
    onSpawn: (_child: FakeChild) => {},

    addBackend(spec: BackendSpec): FakeBackend {
      const backend: FakeBackend = {
        pid: spec.pid, port: spec.port, token: spec.token, startedAt: spec.startedAt,
        file: spec.file ?? CONNECTION_FILE,
        alive: true, obeysTerm: spec.obeysTerm ?? true, refuse: false,
        child: spec.child ?? null, routes: new Map(),
      };
      backends.push(backend);
      files.set(backend.file, JSON.stringify({ port: spec.port, token: spec.token, pid: spec.pid, startedAt: spec.startedAt }));
      // The wsl.exe child and the backend it started end together.
      backend.child?.once('exit', () => { backend.alive = false; });
      return backend;
    },

    io: {
      spawnProcess: vi.fn((file: string, args: readonly string[]) => {
        const child = new FakeChild(nextChildPid++);
        spawned.push({ file, args: [...args], child });
        world.onSpawn(child);
        return child;
      }),

      runProcess: vi.fn(async (file: string, args: readonly string[]) => {
        runs.push({ file, args: [...args] });
        if (file !== 'wsl.exe' || !startsWith(args, WSL_PREFIX)) return result(1, '', 'not a wsl.exe -d Ubuntu --exec command\n');
        const command = args.slice(WSL_PREFIX.length);
        const [name, first, second] = command;
        if (name === 'printenv' && first === 'HOME' && command.length === 2) return result(0, `${HOME}\n`);
        if (name === 'cat' && first !== undefined && command.length === 2) {
          const text = files.get(first);
          return text === undefined ? result(1, '', `cat: ${first}: No such file or directory\n`) : result(0, text);
        }
        if (name === 'kill' && (first === '-TERM' || first === '-KILL') && command.length === 3) {
          const backend = backends.find(item => item.alive && String(item.pid) === second);
          if (!backend) return result(1, '', `kill: (${second}) - No such process\n`);
          if (first === '-KILL') endBackend(backend, false);
          else if (backend.obeysTerm) setTimeout(() => endBackend(backend, true), 1_000);
          return result(0, '');
        }
        const numbers = command.join(' ').match(/\d+/g) ?? [];
        if (numbers.length > 0) return result(backends.some(item => item.alive && numbers.includes(String(item.pid))) ? 0 : 1, '');
        return result(127, '', 'unknown command\n');
      }),

      request: vi.fn((request: HttpRequest): Promise<{ statusCode: number; body: string }> => {
        requests.push({ ...request, headers: { ...request.headers } });
        const backend = backends.find(item => item.alive && !item.refuse && item.port === request.port);
        if (request.host !== '127.0.0.1' || !backend) {
          return Promise.reject(Object.assign(new Error(`connect ECONNREFUSED ${request.host}:${request.port}`), { code: 'ECONNREFUSED' }));
        }
        if (header(request, 'authorization') !== `Bearer ${backend.token}`) {
          return Promise.resolve({ statusCode: 401, body: JSON.stringify({ error: 'unauthorized', message: '관리 요청의 비밀값이 맞지 않습니다.' }) });
        }
        const route = backend.routes.get(`${request.method} ${request.path.split('?')[0]}`);
        const reply = route ? route(request) : defaultReply(request);
        if (reply === 'hang') return hanging(request);
        return Promise.resolve({ statusCode: reply.status, body: typeof reply.body === 'string' ? reply.body : JSON.stringify(reply.body) });
      }),

      clock: { now: () => Date.now(), sleep: (ms: number) => new Promise<void>(resolve => { setTimeout(resolve, ms); }) },
      log: vi.fn((line: string) => { logged.push(line); }),
    },
  };
  return world;
}

type World = ReturnType<typeof createWorld>;

async function connectionFor(world: World, env: Record<string, string | undefined> = {}) {
  const { createBackendConnection } = await import('../electron/backend-connection.js');
  return createBackendConnection({ repositoryRoot: REPOSITORY_ROOT, env, ...world.io });
}

type Connection = Awaited<ReturnType<typeof connectionFor>>;

function track<T>(promise: Promise<T>) {
  const outcome: { settled: boolean; value?: T } = { settled: false };
  promise.then(value => { outcome.settled = true; outcome.value = value; }, () => { outcome.settled = true; });
  return outcome;
}

// Moves fake time forward until the promise settles; fails if it is still pending after limitMs.
async function settle<T>(promise: Promise<T>, limitMs = 1_000, stepMs = 50): Promise<T> {
  const outcome = track(promise);
  for (let elapsed = 0; !outcome.settled && elapsed < limitMs; elapsed += stepMs) await vi.advanceTimersByTimeAsync(stepMs);
  if (!outcome.settled) throw new Error(`still pending after ${limitMs} ms of fake time`);
  return promise;
}

async function attached(options: { token?: string; obeysTerm?: boolean } = {}) {
  const world = createWorld();
  const backend = world.addBackend({ pid: 123, port: 40001, token: options.token ?? 'tok-attached', startedAt: '2026-10-10T01:00:00Z', obeysTerm: options.obeysTerm ?? true });
  const connection = await connectionFor(world);
  expect(await settle(connection.connect())).toMatchObject({ state: 'connected' });
  return { world, backend, connection };
}

// The backend appears 5 s after backend-wsl.sh run starts, as pid 222 on port 40002.
function startsBackendOnSpawn(world: World, spec: Partial<BackendSpec> = {}) {
  world.onSpawn = child => {
    setTimeout(() => world.addBackend({ pid: 222, port: 40002, token: 'tok-new', startedAt: '2026-10-10T02:00:00Z', child, ...spec }), 5_000);
  };
}

async function started(spec: Partial<BackendSpec> = {}) {
  const world = createWorld();
  startsBackendOnSpawn(world, spec);
  const connection = await connectionFor(world);
  expect(await settle(connection.connect(), 10_000)).toMatchObject({ state: 'connected' });
  const child = world.spawned[0]?.child;
  if (!child) throw new Error('no child was started');
  return { world, connection, child };
}

function lastRequest(world: World) {
  const request = world.requests.at(-1);
  return { port: request?.port, authorization: header(request, 'authorization') };
}

function catReads(world: World) {
  return world.runs.filter(run => run.args[WSL_PREFIX.length] === 'cat').map(run => run.args[WSL_PREFIX.length + 1]);
}

function killRuns(world: World) {
  return world.runs.filter(run => run.args[WSL_PREFIX.length] === 'kill' && ['-TERM', '-KILL'].includes(run.args[WSL_PREFIX.length + 1] ?? '')).map(run => run.args);
}

function nonWslRuns(world: World) {
  return world.runs.filter(run => run.file !== 'wsl.exe' || !startsWith(run.args, WSL_PREFIX));
}

beforeEach(() => {
  vi.useFakeTimers();
});

afterEach(() => {
  vi.useRealTimers();
});

describe('K1 attaching to a running backend', () => {
  it('K1 attaches when the connection file pid lives and its secret gets status 200, starting no child', async () => {
    const world = createWorld();
    world.addBackend({ pid: 123, port: 40001, token: 'tok-attached', startedAt: '2026-10-10T01:00:00Z' });
    const connection = await connectionFor(world);
    const returned = await settle(connection.connect());
    expect({ returned, current: connection.connectionState() }).toMatchObject({ returned: { state: 'connected' }, current: { state: 'connected' } });
    expect(world.io.spawnProcess).not.toHaveBeenCalled();
    expect(world.runs.map(run => run.args)).toContainEqual([...WSL_PREFIX, 'printenv', 'HOME']);
    expect(catReads(world)).toContain(CONNECTION_FILE);
    expect(nonWslRuns(world)).toEqual([]);
    expect(await settle(connection.readStatus())).toEqual({ ok: true, status: STATUS_STOPPED });
    expect(lastRequest(world)).toEqual({ port: 40001, authorization: 'Bearer tok-attached' });
  });
});

describe('K2 starting the backend', () => {
  it('K2 starts backend-wsl.sh run through wsl.exe with an argument array when there is no connection file', async () => {
    const world = createWorld();
    startsBackendOnSpawn(world);
    const connection = await connectionFor(world);
    expect(await settle(connection.connect(), 10_000)).toMatchObject({ state: 'connected' });
    expect(world.spawned.map(({ file, args }) => ({ file, args }))).toEqual([{ file: 'wsl.exe', args: RUN_ARGS }]);
    await settle(connection.readStatus());
    expect(lastRequest(world)).toEqual({ port: 40002, authorization: 'Bearer tok-new' });
    expect(nonWslRuns(world)).toEqual([]);
  });

  it('K2 starts a backend when the connection file names a pid that no longer lives', async () => {
    const world = createWorld();
    world.files.set(CONNECTION_FILE, JSON.stringify({ port: 40001, token: 'tok-old', pid: 111, startedAt: '2026-10-09T23:00:00Z' }));
    startsBackendOnSpawn(world);
    const connection = await connectionFor(world);
    expect(await settle(connection.connect(), 10_000)).toMatchObject({ state: 'connected' });
    expect(world.spawned.map(({ file, args }) => ({ file, args }))).toEqual([{ file: 'wsl.exe', args: RUN_ARGS }]);
    await settle(connection.readStatus());
    expect(lastRequest(world)).toEqual({ port: 40002, authorization: 'Bearer tok-new' });
  });

  it('K2 starts a backend when the recorded one fails its status, and connects only to a new file with another pid and startedAt', async () => {
    const world = createWorld();
    const old = world.addBackend({ pid: 123, port: 40001, token: 'tok-old', startedAt: '2026-10-10T01:00:00Z' });
    old.routes.set('GET /api/status', () => ({ status: 503, body: { error: 'operationFailed', message: '상태를 읽지 못했습니다.' } }));
    world.onSpawn = child => {
      // The old backend answers again a second later, while its old connection file is still there.
      setTimeout(() => { old.routes.delete('GET /api/status'); }, 1_000);
      setTimeout(() => world.addBackend({ pid: 222, port: 40002, token: 'tok-new', startedAt: '2026-10-10T02:00:00Z', child }), 5_000);
    };
    const connection = await connectionFor(world);
    const connecting = connection.connect();
    await vi.advanceTimersByTimeAsync(3_000);
    expect({ spawned: world.spawned.length, state: connection.connectionState().state }).toEqual({ spawned: 1, state: 'starting' });
    expect(await settle(connecting, 7_000)).toMatchObject({ state: 'connected' });
    await settle(connection.readStatus());
    expect(lastRequest(world)).toEqual({ port: 40002, authorization: 'Bearer tok-new' });
  });

  it('K2 connect() while starting or connected does not start another backend (IPC connect)', async () => {
    const world = createWorld();
    startsBackendOnSpawn(world);
    const connection = await connectionFor(world);
    const first = connection.connect();
    await vi.advanceTimersByTimeAsync(1_000);
    const second = connection.connect();
    await settle(Promise.all([first, second]), 10_000);
    expect(await settle(connection.connect())).toMatchObject({ state: 'connected' });
    expect(world.spawned).toHaveLength(1);
  });
});

describe('K3 start time limit', () => {
  it('K3 gives up 120 s after starting without a new connection file: ends the child and reports startTimeout', async () => {
    const world = createWorld();
    const connection = await connectionFor(world);
    const connecting = connection.connect();
    const outcome = track(connecting);
    await vi.advanceTimersByTimeAsync(119_000);
    const child = world.spawned[0]?.child;
    expect({ spawned: world.spawned.length, settled: outcome.settled, state: connection.connectionState().state, kills: child?.kill.mock.calls.length })
      .toEqual({ spawned: 1, settled: false, state: 'starting', kills: 0 });
    await vi.advanceTimersByTimeAsync(2_000);
    expect(outcome.settled).toBe(true);
    expect(await connecting).toMatchObject({ state: 'disconnected', reason: 'startTimeout' });
    expect(child?.kill).toHaveBeenCalled();
    // The ended child's exit arrives later and must not replace the reason.
    await vi.advanceTimersByTimeAsync(1_000);
    expect(connection.connectionState()).toMatchObject({ state: 'disconnected', reason: 'startTimeout' });
  });
});

describe('K4 verification override variables', () => {
  const CONFIG = '/tmp/dh-check/backend.json';
  const DATA = '/tmp/dh-check/data';

  it('K4 passes run --config and reads the connection file under the data directory when both are set', async () => {
    const world = createWorld();
    startsBackendOnSpawn(world, { file: `${DATA}/connection.json`, token: 'tok-check' });
    const env = { DAWNHOLDER_MANAGEMENT_BACKEND_CONFIG: CONFIG, DAWNHOLDER_MANAGEMENT_DATA_DIR: DATA };
    const connection = await connectionFor(world, env);
    expect(await settle(connection.connect(), 10_000)).toMatchObject({ state: 'connected' });
    expect(world.spawned.map(({ file, args }) => ({ file, args }))).toEqual([{ file: 'wsl.exe', args: [...RUN_ARGS, '--config', CONFIG] }]);
    const reads = catReads(world);
    expect({ dataDirectory: reads.includes(`${DATA}/connection.json`), defaultFile: reads.includes(CONNECTION_FILE) }).toEqual({ dataDirectory: true, defaultFile: false });
    await settle(connection.readStatus());
    expect(lastRequest(world)).toEqual({ port: 40002, authorization: 'Bearer tok-check' });
  });

  it('K4 neither starts nor attaches with only one of the two variables: invalidOverride', async () => {
    const seen: unknown[] = [];
    for (const env of [{ DAWNHOLDER_MANAGEMENT_BACKEND_CONFIG: CONFIG }, { DAWNHOLDER_MANAGEMENT_DATA_DIR: DATA }]) {
      const world = createWorld();
      // A live backend in the default place that it must not attach to either.
      world.addBackend({ pid: 123, port: 40001, token: 'tok-attached', startedAt: '2026-10-10T01:00:00Z' });
      const connection = await connectionFor(world, env);
      const state = await settle(connection.connect());
      seen.push({ state, spawned: world.spawned.length, requests: world.requests.length });
    }
    const refused = { state: expect.objectContaining({ state: 'disconnected', reason: 'invalidOverride' }), spawned: 0, requests: 0 };
    expect(seen).toEqual([refused, refused]);
  });
});

describe('K5 HTTP requests', () => {
  it('K5 sends each operation to 127.0.0.1 and the file port with the bearer secret and no Origin header', async () => {
    const { world, connection } = await attached();
    const operations = [
      { name: 'status', call: () => connection.readStatus(), method: 'GET', path: '/api/status' },
      { name: 'start', call: () => connection.startServer(), method: 'POST', path: '/api/server/start' },
      { name: 'stop', call: () => connection.stopServer(), method: 'POST', path: '/api/server/stop' },
      { name: 'force stop', call: () => connection.forceStopServer(), method: 'POST', path: '/api/server/force-stop' },
      { name: 'logs', call: () => connection.readLogs(QUERY), method: 'GET', path: '/api/logs' },
      { name: 'releases', call: () => connection.readReleases(), method: 'GET', path: '/api/releases' },
      { name: 'build', call: () => connection.buildRelease(COMMIT), method: 'POST', path: '/api/releases' },
      { name: 'select', call: () => connection.selectRelease(COMMIT), method: 'PUT', path: '/api/releases/current' },
    ];
    const seen: unknown[] = [];
    for (const operation of operations) {
      const before = world.requests.length;
      const result = await settle(operation.call() as Promise<Result>);
      const sent = world.requests.slice(before);
      seen.push({
        name: operation.name, ok: result.ok, count: sent.length,
        host: sent[0]?.host, port: sent[0]?.port, method: sent[0]?.method, path: sent[0]?.path.split('?')[0],
        authorization: header(sent[0], 'authorization'), origin: header(sent[0], 'origin') !== undefined,
      });
    }
    expect(seen).toEqual(operations.map(({ name, method, path }) => ({
      name, ok: true, count: 1, host: '127.0.0.1', port: 40001, method, path, authorization: 'Bearer tok-attached', origin: false,
    })));
    const logs = world.requests.find(request => request.path.startsWith('/api/logs'));
    const query = new URL(logs?.path ?? '/', 'http://127.0.0.1').searchParams;
    expect({ minutes: query.get('minutes'), contains: query.getAll('contains'), limit: query.get('limit') }).toEqual({ minutes: '10', contains: ['error', 'warn'], limit: '1000' });
    const bodies = world.requests.filter(request => request.path.startsWith('/api/releases') && request.method !== 'GET').map(request => JSON.parse(request.body ?? 'null') as unknown);
    expect(bodies).toEqual([{ commit: COMMIT }, { commit: COMMIT }]);
  });

  it('K5 returns the checked status, logs and release answers', async () => {
    const { connection } = await attached();
    expect(await settle(connection.readStatus())).toEqual({ ok: true, status: STATUS_STOPPED });
    expect(await settle(connection.readLogs(QUERY))).toEqual({ ok: true, logs: LOGS });
    expect(await settle(connection.readReleases())).toEqual({ ok: true, releases: RELEASES });
  });
});

describe('K6 request time limits', () => {
  it('K6 fails status and logs after 5 s, start, stop and force stop after 30 s and a release build after 660 s', async () => {
    const cases = [
      { name: 'status', route: 'GET /api/status', limitMs: 5_000, call: (connection: Connection) => connection.readStatus() },
      { name: 'logs', route: 'GET /api/logs', limitMs: 5_000, call: (connection: Connection) => connection.readLogs(QUERY) },
      { name: 'start', route: 'POST /api/server/start', limitMs: 30_000, call: (connection: Connection) => connection.startServer() },
      { name: 'stop', route: 'POST /api/server/stop', limitMs: 30_000, call: (connection: Connection) => connection.stopServer() },
      { name: 'force stop', route: 'POST /api/server/force-stop', limitMs: 30_000, call: (connection: Connection) => connection.forceStopServer() },
      { name: 'build', route: 'POST /api/releases', limitMs: 660_000, call: (connection: Connection) => connection.buildRelease(COMMIT) },
    ];
    const seen: unknown[] = [];
    for (const item of cases) {
      const { backend, connection } = await attached();
      backend.routes.set(item.route, () => 'hang');
      const outcome = track(item.call(connection) as Promise<Result>);
      await vi.advanceTimersByTimeAsync(item.limitMs - 100);
      const pendingBeforeLimit = !outcome.settled;
      await vi.advanceTimersByTimeAsync(200);
      seen.push({ name: item.name, pendingBeforeLimit, settledAfterLimit: outcome.settled, ok: outcome.value?.ok, state: connection.connectionState().state });
    }
    expect(seen).toEqual(cases.map(({ name }) => ({ name, pendingBeforeLimit: true, settledAfterLimit: true, ok: false, state: 'connected' })));
  });
});

describe('K7 bad answers and unreachable backends', () => {
  it('K7 fails only that request with invalidResponse when an answer has the wrong shape, staying connected', async () => {
    const { backend, connection } = await attached();
    const cases: Array<{ name: string; route: string; body: unknown; call: () => Promise<Result> }> = [
      { name: 'status {}', route: 'GET /api/status', body: {}, call: () => connection.readStatus() },
      { name: 'status not JSON', route: 'GET /api/status', body: 'not json', call: () => connection.readStatus() },
      { name: 'status unknown state', route: 'GET /api/status', body: { ...STATUS_STOPPED, server: { ...STATUS_STOPPED.server, state: 'paused' } }, call: () => connection.readStatus() },
      { name: 'logs lines as text', route: 'GET /api/logs', body: { ...LOGS, lines: 'Server started' }, call: () => connection.readLogs(QUERY) },
      { name: 'releases as text', route: 'GET /api/releases', body: { ...RELEASES, releases: 'none' }, call: () => connection.readReleases() },
    ];
    const seen: unknown[] = [];
    for (const item of cases) {
      backend.routes.set(item.route, () => ({ status: 200, body: item.body }));
      const result = await settle(item.call());
      backend.routes.delete(item.route);
      seen.push({ name: item.name, ok: result.ok, code: result.code, korean: HANGUL.test(result.message ?? ''), state: connection.connectionState().state });
    }
    expect(seen).toEqual(cases.map(({ name }) => ({ name, ok: false, code: 'invalidResponse', korean: true, state: 'connected' })));
  });

  it('K7 disconnects with backendUnreachable after three refused or timed-out requests in a row, and not after two', async () => {
    const { backend, connection } = await attached();
    const seen: unknown[] = [];
    const step = async (name: string, prepare: () => void) => {
      prepare();
      const result = await settle(connection.readStatus() as Promise<Result>, 6_000);
      seen.push({ name, ok: result.ok, state: connection.connectionState().state });
    };
    await step('refused 1', () => { backend.refuse = true; });
    await step('refused 2', () => {});
    await step('answered', () => { backend.refuse = false; });
    await step('refused 3', () => { backend.refuse = true; });
    await step('refused 4', () => {});
    await step('timed out', () => { backend.refuse = false; backend.routes.set('GET /api/status', () => 'hang'); });
    expect(seen).toEqual([
      { name: 'refused 1', ok: false, state: 'connected' },
      { name: 'refused 2', ok: false, state: 'connected' },
      { name: 'answered', ok: true, state: 'connected' },
      { name: 'refused 3', ok: false, state: 'connected' },
      { name: 'refused 4', ok: false, state: 'connected' },
      { name: 'timed out', ok: false, state: 'disconnected' },
    ]);
    expect(connection.connectionState()).toMatchObject({ state: 'disconnected', reason: 'backendUnreachable' });
  });
});

describe('K8 a refused secret', () => {
  it('K8 reads the connection file again once on 401 and retries with the new secret', async () => {
    const { world, backend, connection } = await attached({ token: 'tok-a' });
    backend.token = 'tok-b';
    world.files.set(CONNECTION_FILE, JSON.stringify({ port: 40001, token: 'tok-b', pid: 123, startedAt: backend.startedAt }));
    const readsBefore = catReads(world).length;
    const requestsBefore = world.requests.length;
    const result = await settle(connection.readStatus() as Promise<Result>);
    expect({
      ok: result.ok,
      extraReads: catReads(world).length - readsBefore,
      authorizations: world.requests.slice(requestsBefore).map(request => header(request, 'authorization')),
    }).toEqual({ ok: true, extraReads: 1, authorizations: ['Bearer tok-a', 'Bearer tok-b'] });
    await settle(connection.readStatus());
    expect(lastRequest(world)).toEqual({ port: 40001, authorization: 'Bearer tok-b' });
  });

  it('K8 gives up with unauthorized after that one re-read when the secret is still refused', async () => {
    const { world, backend, connection } = await attached({ token: 'tok-a' });
    backend.token = 'tok-z';
    const readsBefore = catReads(world).length;
    const requestsBefore = world.requests.length;
    const result = await settle(connection.readStatus() as Promise<Result>);
    const sent = world.requests.length - requestsBefore;
    expect({ ok: result.ok, code: result.code, extraReads: catReads(world).length - readsBefore, oneOrTwoRequests: sent >= 1 && sent <= 2 })
      .toEqual({ ok: false, code: 'unauthorized', extraReads: 1, oneOrTwoRequests: true });
  });
});

describe('K9 the backend child ending', () => {
  it('K9 reports backendExited with the exit code when the child ends, and does not start it again by itself', async () => {
    const { world, connection, child } = await started();
    child.end(3);
    await vi.advanceTimersByTimeAsync(100);
    expect(connection.connectionState()).toMatchObject({ state: 'disconnected', reason: 'backendExited', exitCode: 3 });
    await vi.advanceTimersByTimeAsync(600_000);
    expect(world.spawned).toHaveLength(1);
    // 「다시 연결」 runs the start and attach again.
    startsBackendOnSpawn(world, { pid: 333, port: 40003, token: 'tok-again', startedAt: '2026-10-10T03:00:00Z' });
    expect(await settle(connection.connect(), 10_000)).toMatchObject({ state: 'connected' });
    expect(world.spawned).toHaveLength(2);
  });

  it('K9 reports backendExited when the child ends before the backend is ready, without waiting for the 120 s limit', async () => {
    const world = createWorld();
    world.onSpawn = child => { setTimeout(() => child.end(1), 3_000); };
    const connection = await connectionFor(world);
    expect(await settle(connection.connect(), 5_000)).toMatchObject({ state: 'disconnected', reason: 'backendExited', exitCode: 1 });
    expect(world.spawned).toHaveLength(1);
  });
});

describe('K10 shutting the backend down', () => {
  const term = (pid: number) => [...WSL_PREFIX, 'kill', '-TERM', String(pid)];
  const kill = (pid: number) => [...WSL_PREFIX, 'kill', '-KILL', String(pid)];

  it('K10 sends kill -TERM to a started backend and waits for the child, with no kill -KILL when it ends in time', async () => {
    const { world, connection } = await started();
    const stopping = connection.shutdown();
    await vi.advanceTimersByTimeAsync(0);
    expect(connection.connectionState()).toMatchObject({ state: 'stopping' });
    await settle(stopping, 5_000);
    expect(killRuns(world)).toEqual([term(222)]);
  });

  it('K10 sends kill -KILL 30 s after TERM when the started backend does not end', async () => {
    const { world, connection } = await started({ obeysTerm: false });
    const stopping = connection.shutdown();
    const outcome = track(stopping);
    await vi.advanceTimersByTimeAsync(29_000);
    const before = { settled: outcome.settled, kills: killRuns(world) };
    await vi.advanceTimersByTimeAsync(3_000);
    await settle(stopping, 5_000);
    expect(before).toEqual({ settled: false, kills: [term(222)] });
    expect(killRuns(world)).toEqual([term(222), kill(222)]);
  });

  it('K10 stops an attached backend the same way: TERM, and KILL only when it is still alive after 30 s', async () => {
    const obeying = await attached();
    await settle(obeying.connection.shutdown(), 5_000);
    const ignoring = await attached({ obeysTerm: false });
    const outcome = track(ignoring.connection.shutdown());
    await vi.advanceTimersByTimeAsync(29_000);
    const before = { settled: outcome.settled, kills: killRuns(ignoring.world) };
    await vi.advanceTimersByTimeAsync(6_000);
    expect({
      obeying: killRuns(obeying.world),
      ignoringBefore30s: before,
      ignoringAfter: { settled: outcome.settled, kills: killRuns(ignoring.world) },
    }).toEqual({
      obeying: [term(123)],
      ignoringBefore30s: { settled: false, kills: [term(123)] },
      ignoringAfter: { settled: true, kills: [term(123), kill(123)] },
    });
  });
});

describe('K11 the connection secret', () => {
  it('K11 keeps the secret out of the console, the log sink, every result and the connection state', async () => {
    const SECRET = 'secret-token-0f9e8d7c6b5a';
    const ROTATED = 'secret-token-rotated-1a2b3c';
    const consoleSpies = (['log', 'info', 'warn', 'error', 'debug'] as const).map(name => vi.spyOn(console, name).mockImplementation(() => {}));
    const world = createWorld();
    world.onSpawn = child => {
      child.stdout.write('Building ManagementBackend...\n');
      child.stderr.write('warning: the first build can take a while\n');
      setTimeout(() => world.addBackend({ pid: 222, port: 40002, token: SECRET, startedAt: '2026-10-10T02:00:00Z', child }), 5_000);
    };
    const connection = await connectionFor(world);
    const seen: unknown[] = [];
    seen.push(await settle(connection.connect(), 10_000), connection.connectionState());
    const backend = world.backends.at(-1);
    if (!backend) throw new Error('no backend');
    backend.routes.set('GET /api/status', () => ({ status: 200, body: { broken: true } }));
    seen.push(await settle(connection.readStatus()), connection.connectionState());
    backend.routes.delete('GET /api/status');
    // A 401 that the re-read fixes, then one that it cannot fix.
    backend.token = ROTATED;
    world.files.set(CONNECTION_FILE, JSON.stringify({ port: 40002, token: ROTATED, pid: 222, startedAt: backend.startedAt }));
    seen.push(await settle(connection.readStatus()));
    backend.token = 'secret-token-not-in-any-file';
    seen.push(await settle(connection.readStatus()));
    backend.token = ROTATED;
    backend.refuse = true;
    for (let attempt = 0; attempt < 3; attempt += 1) seen.push(await settle(connection.readStatus(), 6_000));
    seen.push(connection.connectionState());
    backend.refuse = false;
    await settle(connection.shutdown(), 40_000);
    seen.push(connection.connectionState());
    const asText = (value: unknown) => (typeof value === 'string' ? value : value instanceof Error ? `${value.message}\n${value.stack ?? ''}` : JSON.stringify(value) ?? String(value));
    const written = [...consoleSpies.flatMap(spy => spy.mock.calls.flat()), ...world.logged, ...seen].map(asText);
    expect(written.filter(text => text.includes(SECRET) || text.includes(ROTATED))).toEqual([]);
  });
});

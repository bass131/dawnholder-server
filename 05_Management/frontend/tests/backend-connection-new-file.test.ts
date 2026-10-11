// @vitest-environment node
// Requirement: screen-design.md 「시작과 붙기」 4 — 「새 연결 파일이 생기고(이전 파일과 `startedAt`이 다름) `GET /api/status`가
// 200이 될 때까지 기다린다. `pid`로는 가르지 않는다. WSL이 유휴 종료 뒤 다시 켜지면 남은 이전 파일의 pid 번호를 새 백엔드가
// 다시 받을 수 있다(PR2 검증 관찰). 상한은 120초다(첫 빌드 포함). 넘으면 자식을 끝내고 `disconnected: startTimeout`」 — and
// 「앱 종료」 — 「끝낼 때는 `wsl.exe -d Ubuntu --exec kill -TERM <백엔드 pid>`를 보낸다」, here while the backend is still starting.
//
// Independent re-verification test for the PR2 fix of the first verdict's design observations (a)·(h). The previous file is
// the connection file connect() reads before it starts backend-wsl.sh run. Only a file whose startedAt differs from it belongs
// to the started backend; with no previous file the first file does. Every I/O is a double: no wsl.exe, process or socket is
// started, and time is Vitest's fake clock.
import { EventEmitter } from 'node:events';
import { PassThrough } from 'node:stream';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { STATUS_STOPPED } from './server-operations-fixtures';

const REPOSITORY_ROOT = 'C:\\Users\\tester\\checkout\\';
const HOME = '/home/tester';
const CONNECTION_FILE = `${HOME}/.local/share/dawnholder/management/connection.json`;
const WSL_PREFIX = ['-d', 'Ubuntu', '--exec'];
const START_LIMIT_MS = 120_000;
const STALE_STARTED_AT = '2026-10-10T01:00:00Z';
const NEW_STARTED_AT = '2026-10-10T05:00:00Z';

interface HttpInput {
  host: string;
  port: number;
  method: string;
  path: string;
  headers: Record<string, string>;
}

interface ConnectionRecord {
  pid: number;
  port: number;
  token: string;
  startedAt: string;
}

interface Backend extends ConnectionRecord {
  alive: boolean;
  answersStatus: boolean;
  child: Child | null;
}

class Child extends EventEmitter {
  readonly stdout = new PassThrough();
  readonly stderr = new PassThrough();
  readonly kill = vi.fn(() => {
    this.end();
    return true;
  });
  private ending = false;

  // Ends the way a Node child process does: 'exit' a moment after the cause.
  end() {
    if (this.ending) return;
    this.ending = true;
    setTimeout(() => this.emit('exit', 0, null), 10);
  }
}

function createWorld() {
  const files = new Map<string, string>();
  const backends: Backend[] = [];
  const runs: string[][] = [];
  const spawned: Child[] = [];
  const statusRequests: Array<{ port: number; authorization: string | undefined }> = [];
  const result = (exitCode: number, stdout = '', stderr = '') => ({ exitCode, stdout, stderr });
  const living = (pid: string | undefined) => backends.find(backend => backend.alive && String(backend.pid) === pid);

  // A backend that ends takes its wsl.exe child with it.
  function endBackend(backend: Backend) {
    backend.alive = false;
    backend.child?.end();
  }

  function answer(input: HttpInput) {
    const backend = backends.find(item => item.alive && item.port === input.port);
    if (input.host !== '127.0.0.1' || !backend) {
      return Promise.reject(Object.assign(new Error(`connect ECONNREFUSED 127.0.0.1:${input.port}`), { code: 'ECONNREFUSED' }));
    }
    const authorization = input.headers.Authorization;
    if (authorization !== `Bearer ${backend.token}`) {
      return Promise.resolve({ statusCode: 401, body: JSON.stringify({ error: 'unauthorized', message: '비밀값이 맞지 않습니다.' }) });
    }
    if (input.method !== 'GET' || input.path !== '/api/status') return Promise.resolve({ statusCode: 200, body: '{}' });
    statusRequests.push({ port: input.port, authorization });
    if (!backend.answersStatus) {
      return Promise.resolve({ statusCode: 503, body: JSON.stringify({ error: 'operationFailed', message: '상태를 읽지 못했습니다.' }) });
    }
    return Promise.resolve({ statusCode: 200, body: JSON.stringify(STATUS_STOPPED) });
  }

  return {
    backends, runs, spawned, statusRequests,

    // The connection file as the backend writes it: { port, token, pid, startedAt }.
    writeFile({ port, token, pid, startedAt }: ConnectionRecord) {
      files.set(CONNECTION_FILE, JSON.stringify({ port, token, pid, startedAt }));
    },

    addBackend(record: ConnectionRecord, extra: Partial<Pick<Backend, 'answersStatus' | 'child'>> = {}): Backend {
      const backend: Backend = { ...record, alive: true, answersStatus: true, child: null, ...extra };
      backends.push(backend);
      return backend;
    },

    io: {
      spawnProcess: vi.fn((_file: string, _args: readonly string[]) => {
        const child = new Child();
        spawned.push(child);
        return child;
      }),
      runProcess: vi.fn(async (file: string, args: readonly string[]) => {
        runs.push([file, ...args]);
        const [name, first, second] = args.slice(WSL_PREFIX.length);
        if (name === 'printenv' && first === 'HOME') return result(0, `${HOME}\n`);
        if (name === 'cat' && first === CONNECTION_FILE) {
          const text = files.get(CONNECTION_FILE);
          if (text === undefined) return result(1, '', `cat: ${CONNECTION_FILE}: No such file or directory\n`);
          return result(0, text);
        }
        if (name === 'kill' && first === '-0') return result(living(second) ? 0 : 1);
        if (name === 'kill' && (first === '-TERM' || first === '-KILL')) {
          const backend = living(second);
          if (!backend) return result(1, '', `kill: (${second}) - No such process\n`);
          // The backend stops its server and ends by itself a second after TERM.
          setTimeout(() => endBackend(backend), first === '-TERM' ? 1_000 : 0);
          return result(0);
        }
        return result(127, '', 'unexpected command\n');
      }),
      request: vi.fn(answer),
      clock: { now: () => Date.now(), sleep: (ms: number) => new Promise<void>(resolve => { setTimeout(resolve, ms); }) },
      log: vi.fn(),
    },
  };
}

type World = ReturnType<typeof createWorld>;

async function connectionFor(world: World) {
  const { createBackendConnection } = await import('../electron/backend-connection.js');
  return createBackendConnection({ repositoryRoot: REPOSITORY_ROOT, env: {}, ...world.io });
}

function track<T>(promise: Promise<T>) {
  const outcome = { settled: false };
  promise.then(() => { outcome.settled = true; }, () => { outcome.settled = true; });
  return outcome;
}

// Moves fake time forward until the promise settles; fails if it is still pending after limitMs.
async function settle<T>(promise: Promise<T>, limitMs: number): Promise<T> {
  const outcome = track(promise);
  for (let elapsed = 0; !outcome.settled && elapsed < limitMs; elapsed += 100) await vi.advanceTimersByTimeAsync(100);
  if (!outcome.settled) throw new Error(`still pending after ${limitMs} ms of fake time`);
  return promise;
}

function startedChild(world: World): Child {
  const child = world.spawned[0];
  if (!child) throw new Error('backend-wsl.sh run was not started');
  return child;
}

// kill -TERM / -KILL commands, without the wsl.exe -d Ubuntu --exec prefix.
function killCommands(world: World) {
  return world.runs
    .map(run => run.slice(1 + WSL_PREFIX.length))
    .filter(([name, signal]) => name === 'kill' && (signal === '-TERM' || signal === '-KILL'));
}

beforeEach(() => {
  vi.useFakeTimers();
});

afterEach(() => {
  vi.useRealTimers();
});

describe('waiting for the started backend: a new connection file has another startedAt (independent re-verification)', () => {
  it('connects to a new file that reuses the stale pid with another startedAt, long before the 120 s limit', async () => {
    const world = createWorld();
    world.writeFile({ pid: 4242, port: 40001, token: 'tok-stale', startedAt: STALE_STARTED_AT });
    const connection = await connectionFor(world);

    const outcome = track(connection.connect());
    await vi.advanceTimersByTimeAsync(1_000);
    const child = startedChild(world);
    // WSL started again: the new backend has the stale file's pid before it writes its own file.
    const started = world.addBackend({ pid: 4242, port: 40002, token: 'tok-new', startedAt: NEW_STARTED_AT }, { child });
    await vi.advanceTimersByTimeAsync(2_000);
    const stateBeforeNewFile = connection.connectionState().state;
    world.writeFile(started);
    await vi.advanceTimersByTimeAsync(2_000);
    const afterNewFile = { settled: outcome.settled, state: connection.connectionState() };
    await vi.advanceTimersByTimeAsync(START_LIMIT_MS);
    const afterStartLimit = connection.connectionState();
    const status = await settle(connection.readStatus(), 1_000);

    expect(stateBeforeNewFile).toBe('starting');
    expect(afterNewFile).toEqual({ settled: true, state: { state: 'connected' } });
    expect(afterStartLimit).toEqual({ state: 'connected' });
    expect(child.kill).not.toHaveBeenCalled();
    expect(status).toEqual({ ok: true, status: STATUS_STOPPED });
    expect(world.statusRequests.at(-1)).toEqual({ port: 40002, authorization: 'Bearer tok-new' });
  });

  it('does not attach to the backend behind the unchanged stale file when it answers again, and ends with startTimeout at 120 s', async () => {
    const world = createWorld();
    const staleRecord = { pid: 123, port: 40001, token: 'tok-stale', startedAt: STALE_STARTED_AT };
    const stale = world.addBackend(staleRecord, { answersStatus: false });
    world.writeFile(staleRecord);
    const connection = await connectionFor(world);

    const connecting = connection.connect();
    const outcome = track(connecting);
    await vi.advanceTimersByTimeAsync(1_000);
    const child = startedChild(world);
    // The recorded backend answers its status again while its file is still the only one.
    stale.answersStatus = true;
    const statesUntilLimit = new Set<string>();
    for (let second = 2; second < 120; second += 1) {
      await vi.advanceTimersByTimeAsync(1_000);
      statesUntilLimit.add(connection.connectionState().state);
    }
    const settledBeforeLimit = outcome.settled;
    await vi.advanceTimersByTimeAsync(2_000);

    expect(statesUntilLimit).toEqual(new Set(['starting']));
    expect(settledBeforeLimit).toBe(false);
    expect(await connecting).toMatchObject({ state: 'disconnected', reason: 'startTimeout' });
    expect(child.kill).toHaveBeenCalled();
    expect(killCommands(world)).toEqual([]);
    expect(stale.alive).toBe(true);
  });

  it('does not count a file with another pid but the stale startedAt as new: no connection, startTimeout at 120 s', async () => {
    const world = createWorld();
    world.writeFile({ pid: 111, port: 40001, token: 'tok-stale', startedAt: STALE_STARTED_AT });
    const connection = await connectionFor(world);

    const connecting = connection.connect();
    const outcome = track(connecting);
    await vi.advanceTimersByTimeAsync(1_000);
    const child = startedChild(world);
    const sameStartedAt = world.addBackend({ pid: 222, port: 40002, token: 'tok-other', startedAt: STALE_STARTED_AT }, { child });
    world.writeFile(sameStartedAt);
    await vi.advanceTimersByTimeAsync(START_LIMIT_MS - 2_000);
    const beforeLimit = { settled: outcome.settled, state: connection.connectionState().state };
    await vi.advanceTimersByTimeAsync(2_000);

    expect(beforeLimit).toEqual({ settled: false, state: 'starting' });
    expect(await connecting).toMatchObject({ state: 'disconnected', reason: 'startTimeout' });
  });
});

describe('shutdown while the backend is starting: only the new connection file names the backend to end (independent re-verification)', () => {
  it('sends kill -TERM to the pid of a new file even when it reuses the stale pid', async () => {
    const world = createWorld();
    world.writeFile({ pid: 4242, port: 40001, token: 'tok-stale', startedAt: STALE_STARTED_AT });
    const connection = await connectionFor(world);

    void connection.connect();
    await vi.advanceTimersByTimeAsync(1_100);
    const child = startedChild(world);
    const started = world.addBackend({ pid: 4242, port: 40002, token: 'tok-new', startedAt: NEW_STARTED_AT }, { child });
    world.writeFile(started);
    // The tray quit arrives before the wait reads the new file again.
    await settle(connection.shutdown(), 35_000);

    expect(killCommands(world)).toEqual([['kill', '-TERM', '4242']]);
    expect(started.alive).toBe(false);
  });

  it('sends no kill to the backend behind the unchanged stale file, even when it answers again', async () => {
    const world = createWorld();
    const staleRecord = { pid: 123, port: 40001, token: 'tok-stale', startedAt: STALE_STARTED_AT };
    const stale = world.addBackend(staleRecord, { answersStatus: false });
    world.writeFile(staleRecord);
    const connection = await connectionFor(world);

    void connection.connect();
    await vi.advanceTimersByTimeAsync(1_100);
    startedChild(world);
    stale.answersStatus = true;
    await settle(connection.shutdown(), 35_000);

    expect(killCommands(world)).toEqual([]);
    expect(stale.alive).toBe(true);
  });

  it('with no previous file, treats the first file as new and sends kill -TERM to its pid', async () => {
    const world = createWorld();
    const connection = await connectionFor(world);

    void connection.connect();
    await vi.advanceTimersByTimeAsync(1_100);
    const child = startedChild(world);
    const started = world.addBackend({ pid: 222, port: 40002, token: 'tok-new', startedAt: NEW_STARTED_AT }, { child });
    world.writeFile(started);
    await settle(connection.shutdown(), 35_000);

    expect(killCommands(world)).toEqual([['kill', '-TERM', '222']]);
    expect(started.alive).toBe(false);
  });
});

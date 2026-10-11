// @vitest-environment node
// Requirement: screen-design.md 「HTTP 대화」 — 「요청마다 시간 상한을 둔다: 상태·로그·실행본 목록 5초, 시작·종료·강제 종료·
// 현재 운영 버전 지정 30초, 실행본 빌드 660초」 and 「IPC 계약」 release-select (`PUT /api/releases/current`), with the
// window-side implementation contract (E/contracts/pr2-frontend-impl-task.md 「요청 시간 상한 둘」: 실행본 목록 5초,
// 현재 운영 버전 지정 30초). tests/backend-connection.test.ts K6 covers the other limits.
//
// Independent verifier test (PR2 verification). The limits are the design's numbers. A request that is still unanswered
// 100 ms before the limit must be pending, and settle as a failure 100 ms after it, as in K6; an answer that arrives
// before the limit must be used. Every I/O is a double and time is Vitest's fake clock.
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { RELEASES, STATUS_STOPPED } from './server-operations-fixtures';

const HOME = '/home/tester';
const CONNECTION_FILE = `${HOME}/.local/share/dawnholder/management/connection.json`;
const RECORDED = { port: 40123, token: 'tok-recorded', pid: 4242, startedAt: '2026-10-10T01:00:00Z' };
const COMMIT = '0123456789abcdef0123456789abcdef01234567';
const RELEASE_LIST_LIMIT_MS = 5_000;
const RELEASE_SELECT_LIMIT_MS = 30_000;

interface Request { method: string; path: string; body?: string; signal?: AbortSignal }
type Answer = { delayMs: number; statusCode: number; body: unknown } | 'never';

function createWorld() {
  const seen: Request[] = [];
  const answers = new Map<string, Answer>();
  const io = {
    runProcess: vi.fn(async (_file: string, args: readonly string[]) => {
      const command = args.slice(3).join(' ');
      if (command === 'printenv HOME') return { exitCode: 0, stdout: `${HOME}\n`, stderr: '' };
      if (command === `cat ${CONNECTION_FILE}`) return { exitCode: 0, stdout: JSON.stringify(RECORDED), stderr: '' };
      if (command === `kill -0 ${RECORDED.pid}`) return { exitCode: 0, stdout: '', stderr: '' };
      return { exitCode: 1, stdout: '', stderr: 'unexpected command\n' };
    }),
    spawnProcess: vi.fn(() => {
      throw new Error('no child is expected: the recorded backend is alive');
    }),
    request: vi.fn((input: Request) => {
      seen.push({ method: input.method, path: input.path, ...(input.body === undefined ? {} : { body: input.body }) });
      const answer = answers.get(`${input.method} ${input.path}`) ?? { delayMs: 0, statusCode: 200, body: STATUS_STOPPED };
      return new Promise<{ statusCode: number; body: string }>((resolve, reject) => {
        // Like node:http with a signal: an aborted request rejects.
        input.signal?.addEventListener('abort', () => reject(new Error('aborted')), { once: true });
        if (answer === 'never') return;
        const reply = () => resolve({ statusCode: answer.statusCode, body: JSON.stringify(answer.body) });
        // An immediate answer must not wait for fake time to move.
        if (answer.delayMs === 0) reply();
        else setTimeout(reply, answer.delayMs);
      });
    }),
    clock: { now: () => Date.now(), sleep: (ms: number) => new Promise<void>(resolve => { setTimeout(resolve, ms); }) },
    log: vi.fn(),
  };
  return { seen, answers, io };
}

async function attached() {
  const world = createWorld();
  const { createBackendConnection } = await import('../electron/backend-connection.js');
  const connection = createBackendConnection({ repositoryRoot: 'C:\\Users\\tester\\checkout', env: {}, ...world.io });
  expect(await connection.connect()).toMatchObject({ state: 'connected' });
  world.seen.length = 0;
  return { world, connection };
}

function track<T>(promise: Promise<T>) {
  const outcome: { settled: boolean; value?: T } = { settled: false };
  promise.then(value => {
    outcome.settled = true;
    outcome.value = value;
  }, () => {
    outcome.settled = true;
  });
  return outcome;
}

beforeEach(() => {
  vi.useFakeTimers();
});

afterEach(() => {
  vi.useRealTimers();
});

describe('release request limits (independent verifier)', () => {
  it('gives up on GET /api/releases 5 s after sending it and stays connected', async () => {
    const { world, connection } = await attached();
    world.answers.set('GET /api/releases', 'never');

    const outcome = track(connection.readReleases());
    await vi.advanceTimersByTimeAsync(RELEASE_LIST_LIMIT_MS - 100);
    const pendingBeforeLimit = !outcome.settled;
    await vi.advanceTimersByTimeAsync(200);

    expect(world.seen).toEqual([{ method: 'GET', path: '/api/releases' }]);
    expect(pendingBeforeLimit).toBe(true);
    expect(outcome.settled).toBe(true);
    expect(outcome.value?.ok).toBe(false);
    expect(connection.connectionState().state).toBe('connected');
  });

  it('uses a release list that arrives just before the 5 s limit', async () => {
    const { world, connection } = await attached();
    world.answers.set('GET /api/releases', { delayMs: RELEASE_LIST_LIMIT_MS - 200, statusCode: 200, body: RELEASES });

    const outcome = track(connection.readReleases());
    await vi.advanceTimersByTimeAsync(RELEASE_LIST_LIMIT_MS - 100);

    expect(outcome.settled).toBe(true);
    expect(outcome.value).toMatchObject({ ok: true, releases: { currentRelease: RELEASES.currentRelease } });
  });

  it('keeps PUT /api/releases/current open past 5 s and gives up 30 s after sending it, staying connected', async () => {
    const { world, connection } = await attached();
    world.answers.set('PUT /api/releases/current', 'never');

    const outcome = track(connection.selectRelease(COMMIT));
    await vi.advanceTimersByTimeAsync(RELEASE_LIST_LIMIT_MS + 100);
    const pendingPastListLimit = !outcome.settled;
    await vi.advanceTimersByTimeAsync(RELEASE_SELECT_LIMIT_MS - RELEASE_LIST_LIMIT_MS - 200);
    const pendingBeforeLimit = !outcome.settled;
    await vi.advanceTimersByTimeAsync(200);

    expect(world.seen).toHaveLength(1);
    expect(world.seen[0]).toMatchObject({ method: 'PUT', path: '/api/releases/current' });
    expect(JSON.parse(world.seen[0]?.body ?? 'null')).toEqual({ commit: COMMIT });
    expect(pendingPastListLimit).toBe(true);
    expect(pendingBeforeLimit).toBe(true);
    expect(outcome.settled).toBe(true);
    expect(outcome.value?.ok).toBe(false);
    expect(connection.connectionState().state).toBe('connected');
  });

  it('uses a release selection answer that arrives just before the 30 s limit', async () => {
    const { world, connection } = await attached();
    world.answers.set('PUT /api/releases/current', { delayMs: RELEASE_SELECT_LIMIT_MS - 200, statusCode: 200, body: {} });

    const outcome = track(connection.selectRelease(COMMIT));
    await vi.advanceTimersByTimeAsync(RELEASE_SELECT_LIMIT_MS - 100);

    expect(outcome.settled).toBe(true);
    expect(outcome.value?.ok).toBe(true);
  });
});

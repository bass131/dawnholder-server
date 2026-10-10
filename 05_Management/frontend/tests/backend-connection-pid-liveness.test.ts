// @vitest-environment node
// Requirement: screen-design.md 「시작과 붙기」 2 — 「그 pid가 살아 있고(`wsl.exe -d Ubuntu --exec kill -0 <pid>`가 0으로
// 끝남) 그 비밀값으로 `GET /api/status`가 200이면 그 백엔드에 붙는다. 이 경우 자식 프로세스는 없다」 — and 3 — 「없으면
// `wsl.exe -d Ubuntu --exec bash <checkout의 WSL 경로>/05_Management/backend/backend-wsl.sh run`을 자식으로 띄운다」, with the
// window-side implementation contract (E/contracts/pr2-frontend-impl-task.md 「pid 생존 확인」).
//
// Independent verifier test (PR2 verification). tests/backend-connection.test.ts answers every command that names a pid
// as a liveness check on purpose, so the exact command was not fixed there. Here the recorded backend always answers
// its status with 200, so only the exit code of the design's command can decide between attaching and starting a child.
// Every I/O is a double: no wsl.exe, process or socket is started.
import { EventEmitter } from 'node:events';
import { PassThrough } from 'node:stream';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { STATUS_STOPPED } from './server-operations-fixtures';

const REPOSITORY_ROOT = 'C:\\Users\\tester\\checkout\\';
const BACKEND_SCRIPT = '/mnt/c/Users/tester/checkout/05_Management/backend/backend-wsl.sh';
const HOME = '/home/tester';
const CONNECTION_FILE = `${HOME}/.local/share/dawnholder/management/connection.json`;

class Child extends EventEmitter {
  readonly stdout = new PassThrough();
  readonly stderr = new PassThrough();
  readonly kill = vi.fn(() => {
    setTimeout(() => this.emit('exit', null, 'SIGTERM'), 10);
    return true;
  });
}

type Liveness = number | 'wsl.exe fails';

function createWorld(recordedPid: number, liveness: Liveness) {
  const recorded = { port: 40123, token: 'tok-recorded', pid: recordedPid, startedAt: '2026-10-10T01:00:00Z' };
  const runs: Array<{ file: string; args: string[] }> = [];
  const spawned: Array<{ file: string; args: string[]; child: Child }> = [];
  const statusRequests: Array<{ port: number; authorization: string | undefined }> = [];
  const io = {
    runProcess: vi.fn(async (file: string, args: readonly string[]) => {
      runs.push({ file, args: [...args] });
      const command = args.slice(3).join(' ');
      if (command === 'printenv HOME') return { exitCode: 0, stdout: `${HOME}\n`, stderr: '' };
      if (command === `cat ${CONNECTION_FILE}`) return { exitCode: 0, stdout: JSON.stringify(recorded), stderr: '' };
      if (command === `kill -0 ${recordedPid}`) {
        if (liveness === 'wsl.exe fails') throw new Error('spawn wsl.exe ENOENT');
        return { exitCode: liveness, stdout: '', stderr: liveness === 0 ? '' : `kill: (${recordedPid}) - No such process\n` };
      }
      return { exitCode: 1, stdout: '', stderr: `unexpected command: ${command}\n` };
    }),
    spawnProcess: vi.fn((file: string, args: readonly string[]) => {
      const child = new Child();
      spawned.push({ file, args: [...args], child });
      return child;
    }),
    // The recorded backend answers every status request with 200.
    request: vi.fn(async (input: { port: number; method: string; path: string; headers: Record<string, string> }) => {
      if (input.method === 'GET' && input.path === '/api/status') {
        statusRequests.push({ port: input.port, authorization: input.headers.Authorization ?? input.headers.authorization });
      }
      return { statusCode: 200, body: JSON.stringify(STATUS_STOPPED) };
    }),
    clock: { now: () => Date.now(), sleep: (ms: number) => new Promise<void>(resolve => { setTimeout(resolve, ms); }) },
    log: vi.fn(),
  };
  return { recorded, runs, spawned, statusRequests, io };
}

async function connectionFor(world: ReturnType<typeof createWorld>) {
  const { createBackendConnection } = await import('../electron/backend-connection.js');
  return createBackendConnection({ repositoryRoot: REPOSITORY_ROOT, env: {}, ...world.io });
}

function livenessRuns(world: ReturnType<typeof createWorld>) {
  return world.runs.filter(run => run.args.includes('kill') && run.args.includes('-0'));
}

beforeEach(() => {
  vi.useFakeTimers();
});

afterEach(() => {
  vi.useRealTimers();
});

describe('connection file pid liveness through wsl.exe kill -0 (independent verifier)', () => {
  it.each([4242, 5150])('attaches when kill -0 for the recorded pid %i ends with 0, starting no child', async pid => {
    const world = createWorld(pid, 0);
    const connection = await connectionFor(world);

    const state = await connection.connect();

    expect(state).toMatchObject({ state: 'connected' });
    expect(livenessRuns(world)).toEqual([{ file: 'wsl.exe', args: ['-d', 'Ubuntu', '--exec', 'kill', '-0', String(pid)] }]);
    expect(world.spawned).toEqual([]);
    expect(world.statusRequests).toContainEqual({ port: world.recorded.port, authorization: `Bearer ${world.recorded.token}` });
  });

  it.each<Liveness>([1, 2, 255, 'wsl.exe fails'])(
    'does not attach when kill -0 ends with %s although the recorded secret gets status 200, and starts backend-wsl.sh run',
    async liveness => {
      const world = createWorld(4242, liveness);
      const connection = await connectionFor(world);

      const pending = connection.connect();
      await vi.waitFor(() => expect(world.spawned).toHaveLength(1));
      const stateWhileStarting = connection.connectionState();
      const livenessCommands = livenessRuns(world);
      const spawnedCommands = world.spawned.map(({ file, args }) => ({ file, args }));
      // End the started child so connect() settles; the design does not restart it by itself.
      world.spawned[0]?.child.emit('exit', 1, null);
      await vi.advanceTimersByTimeAsync(300);
      await pending;

      expect(livenessCommands).toEqual([{ file: 'wsl.exe', args: ['-d', 'Ubuntu', '--exec', 'kill', '-0', '4242'] }]);
      expect(spawnedCommands).toEqual([{ file: 'wsl.exe', args: ['-d', 'Ubuntu', '--exec', 'bash', BACKEND_SCRIPT, 'run'] }]);
      expect(stateWhileStarting.state).not.toBe('connected');
    },
  );
});

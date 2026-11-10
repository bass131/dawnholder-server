// @vitest-environment node
// Requirement: 05_Management/goals/2026-11-10-code-audit-management-fixes/goal.md 「만들 것」 2 and 완료조건 3, with
// contract supplement v1.1 decisions 1-2. The start time of the backend process is remembered when authentication
// succeeds and compared right before TERM and before KILL. A different or vanished process gets no signal. A process
// that cannot be confirmed gets no signal and the log receives a line with its pid. The same process still gets
// `kill -TERM <pid>` and, 30 s later, `kill -KILL <pid>`.
//
// The fake WSL answers `cat /proc/<pid>/stat` per process, so a reused pid has another start time. Only the exact
// command of the implementation contract is answered. Signals are recorded against whoever owns the pid at that moment.
// Every I/O is a double: no wsl.exe, process or socket is started, and time is Vitest's fake clock.
import { EventEmitter } from 'node:events';
import { PassThrough } from 'node:stream';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { procStatLine } from './backend-process-fixtures';
import { STATUS_STOPPED } from './server-operations-fixtures';

const REPOSITORY_ROOT = 'C:\\Users\\tester\\checkout\\';
const HOME = '/home/tester';
const CONNECTION_FILE = `${HOME}/.local/share/dawnholder/management/connection.json`;
const PID = 4242;
const PORT = 40123;
const START_TIME = 987654321;
const OTHER_START_TIME = 987659999;
const TERM_WAIT_MS = 30_000;
const SHUTDOWN_LIMIT_MS = 35_000;
// Spaces and closing parentheses inside the process name: only counting from the last `)` finds field 22.
const AWKWARD_NAME = 'a) (b c) d';

type StatFailure = 'wsl.exe throws' | 'exit 255' | 'empty output' | 'garbage' | 'too few fields' | 'non-numeric start time';
const STAT_FAILURES: StatFailure[] = ['wsl.exe throws', 'exit 255', 'empty output', 'garbage', 'too few fields', 'non-numeric start time'];

interface OsProcess {
  label: string;
  name: string;
  startTime: number;
  // Whether fields other than starttime move between reads.
  moving: boolean;
  statReads: number;
  obeysTerm: boolean;
  child: Child | null;
  statFailure: StatFailure | null;
  afterStatRead: (() => void) | null;
}

class Child extends EventEmitter {
  readonly stdout = new PassThrough();
  readonly stderr = new PassThrough();
  private ending = false;
  readonly kill = vi.fn(() => {
    this.end();
    return true;
  });

  // Ends the way a Node child process does: 'exit' a moment after the cause.
  end() {
    if (this.ending) return;
    this.ending = true;
    setTimeout(() => this.emit('exit', 0, null), 10);
  }
}

function createWorld() {
  const processes = new Map<number, OsProcess>();
  const signals: Array<{ signal: string; pid: number; receiver: string | null }> = [];
  const logged: string[] = [];
  const spawned: Child[] = [];
  let connection: { port: number; token: string; pid: number; startedAt: string } | null = null;
  let acceptedToken = '';
  const result = (exitCode: number, stdout = '', stderr = '') => ({ exitCode, stdout, stderr });

  function addProcess(pid: number, spec: Partial<OsProcess> = {}): OsProcess {
    const created: OsProcess = {
      label: 'backend', name: 'dotnet', startTime: START_TIME, moving: true, statReads: 0,
      obeysTerm: true, child: null, statFailure: null, afterStatRead: null, ...spec,
    };
    processes.set(pid, created);
    return created;
  }

  function answerStat(pid: number) {
    const owner = processes.get(pid);
    const path = `/proc/${pid}/stat`;
    if (!owner) return result(1, '', `cat: ${path}: No such file or directory\n`);
    const line = procStatLine({ pid, name: owner.name, startTime: owner.startTime, tick: owner.moving ? owner.statReads : 0 });
    owner.statReads += 1;
    const after = owner.afterStatRead;
    owner.afterStatRead = null;
    after?.();
    switch (owner.statFailure) {
      case null: return result(0, `${line}\n`);
      case 'wsl.exe throws': throw new Error('spawn wsl.exe ENOENT');
      case 'exit 255': return result(255, '', 'The Windows Subsystem for Linux instance has terminated.\n');
      case 'empty output': return result(0, '');
      case 'garbage': return result(0, 'not a stat line\n');
      case 'too few fields': return result(0, `${pid} (dotnet) S 1 2 3\n`);
      case 'non-numeric start time': return result(0, `${line.replace(String(owner.startTime), 'soon')}\n`);
    }
  }

  function sendSignal(signal: string, pid: number) {
    const owner = processes.get(pid);
    signals.push({ signal, pid, receiver: owner?.label ?? null });
    if (!owner) return result(1, '', `kill: (${pid}) - No such process\n`);
    if (owner.label === 'backend' && (signal === '-KILL' || owner.obeysTerm)) {
      setTimeout(() => {
        if (processes.get(pid) === owner) processes.delete(pid);
        owner.child?.end();
      }, signal === '-KILL' ? 0 : 1_000);
    }
    return result(0);
  }

  return {
    processes, signals, logged, spawned, addProcess,

    // The connection file names `pid` and the backend accepts only this token from now on.
    publish(pid: number, token: string, startedAt: string) {
      connection = { port: PORT, token, pid, startedAt };
      acceptedToken = token;
    },

    io: {
      spawnProcess: vi.fn((_file: string, _args: readonly string[]) => {
        const child = new Child();
        spawned.push(child);
        return child;
      }),
      runProcess: vi.fn(async (_file: string, args: readonly string[]) => {
        const [name, first, second] = args.slice(3);
        if (name === 'printenv' && first === 'HOME') return result(0, `${HOME}\n`);
        if (name === 'cat' && first === CONNECTION_FILE) return connection ? result(0, JSON.stringify(connection)) : result(1);
        const statPath = name === 'cat' ? /^\/proc\/(\d+)\/stat$/.exec(first ?? '') : null;
        if (statPath) return answerStat(Number(statPath[1]));
        if (name === 'kill' && first === '-0') return result(processes.has(Number(second)) ? 0 : 1);
        if (name === 'kill' && (first === '-TERM' || first === '-KILL')) return sendSignal(first, Number(second));
        return result(127, '', 'unexpected command\n');
      }),
      request: vi.fn(async (input: { port: number; method: string; path: string; headers: Record<string, string> }) => {
        if (!connection || input.port !== connection.port) throw new Error('connect ECONNREFUSED');
        if (input.headers.Authorization !== `Bearer ${acceptedToken}`) {
          return { statusCode: 401, body: JSON.stringify({ error: 'unauthorized', message: '비밀값이 맞지 않습니다.' }) };
        }
        return { statusCode: 200, body: JSON.stringify(STATUS_STOPPED) };
      }),
      clock: { now: () => Date.now(), sleep: (ms: number) => new Promise<void>(resolve => { setTimeout(resolve, ms); }) },
      log: vi.fn((line: string) => { logged.push(line); }),
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

async function settle<T>(promise: Promise<T>, limitMs: number): Promise<T> {
  const outcome = track(promise);
  for (let elapsed = 0; !outcome.settled && elapsed < limitMs; elapsed += 100) await vi.advanceTimersByTimeAsync(100);
  if (!outcome.settled) throw new Error(`still pending after ${limitMs} ms of fake time`);
  return promise;
}

async function attached(spec: Partial<OsProcess> = {}) {
  const world = createWorld();
  const backend = world.addProcess(PID, spec);
  world.publish(PID, 'tok-attached', '2026-11-10T01:00:00Z');
  const connection = await connectionFor(world);
  const state = await settle(connection.connect(), 1_000);
  return { world, backend, connection, state };
}

// A connection the app started itself: the backend appears as a child of the spawned wsl.exe.
async function started(spec: Partial<OsProcess> = {}) {
  const world = createWorld();
  const connection = await connectionFor(world);
  const connecting = connection.connect();
  await vi.advanceTimersByTimeAsync(1_000);
  const child = world.spawned[0];
  if (!child) throw new Error('backend-wsl.sh run was not started');
  const backend = world.addProcess(PID, { child, ...spec });
  world.publish(PID, 'tok-started', '2026-11-10T02:00:00Z');
  const state = await settle(connecting, 5_000);
  return { world, backend, connection, child, state };
}

function reuse(world: World, pid = PID, spec: Partial<OsProcess> = {}) {
  return world.addProcess(pid, { label: 'unrelated', startTime: OTHER_START_TIME, obeysTerm: false, ...spec });
}

function sent(world: World) {
  return world.signals.map(({ signal, pid }) => `${signal} ${pid}`);
}

function sentToOthers(world: World) {
  return world.signals.filter(({ receiver }) => receiver !== 'backend' && receiver !== null);
}

function logLinesNaming(world: World, pid: number, from: number) {
  return world.logged.slice(from).filter(line => new RegExp(`\\b${pid}\\b`).test(line));
}

beforeEach(() => {
  vi.useFakeTimers();
});

afterEach(() => {
  vi.useRealTimers();
});

describe('goal 2: an attached backend', () => {
  it('goal 2 control: the same process gets TERM and no KILL when it ends in time, although every other stat field moved', async () => {
    const { world, connection, state } = await attached();
    await settle(connection.shutdown(), SHUTDOWN_LIMIT_MS);
    expect(state).toEqual({ state: 'connected' });
    expect(sent(world)).toEqual([`-TERM ${PID}`]);
    expect(sentToOthers(world)).toEqual([]);
  });

  it('goal 2 control: the same process gets KILL only after 30 s when it ignores TERM', async () => {
    const { world, connection } = await attached({ obeysTerm: false });
    const stopping = connection.shutdown();
    await vi.advanceTimersByTimeAsync(TERM_WAIT_MS - 1_000);
    const before30s = sent(world);
    await settle(stopping, SHUTDOWN_LIMIT_MS);
    expect(before30s).toEqual([`-TERM ${PID}`]);
    expect(sent(world)).toEqual([`-TERM ${PID}`, `-KILL ${PID}`]);
  });

  it('goal 2: a pid that passed to another process after attaching gets neither TERM nor KILL', async () => {
    const { world, connection } = await attached();
    const unrelated = reuse(world);
    await settle(connection.shutdown(), SHUTDOWN_LIMIT_MS);
    expect(sent(world)).toEqual([]);
    expect(unrelated.statReads).toBeGreaterThan(0);
  });

  it('goal 2: a backend that vanished after attaching gets no signal', async () => {
    const { world, connection } = await attached();
    world.processes.delete(PID);
    await settle(connection.shutdown(), SHUTDOWN_LIMIT_MS);
    expect(sent(world)).toEqual([]);
  });

  it('goal 2: a pid that passes to another process while waiting after TERM gets no KILL', async () => {
    const { world, connection } = await attached({ obeysTerm: false });
    const stopping = connection.shutdown();
    await vi.advanceTimersByTimeAsync(5_000);
    const afterTerm = sent(world);
    reuse(world);
    await settle(stopping, SHUTDOWN_LIMIT_MS);
    expect(afterTerm).toEqual([`-TERM ${PID}`]);
    expect(sent(world)).toEqual([`-TERM ${PID}`]);
    expect(sentToOthers(world)).toEqual([]);
  });

  it.each(STAT_FAILURES)('goal 2: a stat read that fails at shutdown (%s) sends no signal and logs the pid', async failure => {
    const { world, backend, connection, state } = await attached();
    const logStart = world.logged.length;
    backend.statFailure = failure;
    await settle(connection.shutdown(), SHUTDOWN_LIMIT_MS);
    expect(state).toEqual({ state: 'connected' });
    expect(sent(world)).toEqual([]);
    expect(logLinesNaming(world, PID, logStart).length).toBeGreaterThan(0);
  });

  it.each(STAT_FAILURES)('goal 2: a stat read that failed when attaching (%s) still connects, then sends no signal and logs the pid', async failure => {
    const { world, backend, connection, state } = await attached({ statFailure: failure });
    const logStart = world.logged.length;
    backend.statFailure = null;
    await settle(connection.shutdown(), SHUTDOWN_LIMIT_MS);
    expect(state).toEqual({ state: 'connected' });
    expect(sent(world)).toEqual([]);
    expect(logLinesNaming(world, PID, logStart).length).toBeGreaterThan(0);
  });

  it('goal 2: a stat read that fails while waiting after TERM sends no KILL and logs the pid', async () => {
    const { world, backend, connection } = await attached({ obeysTerm: false });
    const logStart = world.logged.length;
    const stopping = connection.shutdown();
    await vi.advanceTimersByTimeAsync(5_000);
    const afterTerm = sent(world);
    backend.statFailure = 'wsl.exe throws';
    await settle(stopping, SHUTDOWN_LIMIT_MS);
    expect(afterTerm).toEqual([`-TERM ${PID}`]);
    expect(sent(world)).toEqual([`-TERM ${PID}`]);
    expect(logLinesNaming(world, PID, logStart).length).toBeGreaterThan(0);
  });
});

describe('goal 2: reading field 22 of a process name with spaces and parentheses', () => {
  it('goal 2: the same process with such a name still gets TERM when only its start time stayed equal', async () => {
    const { world, connection } = await attached({ name: AWKWARD_NAME });
    await settle(connection.shutdown(), SHUTDOWN_LIMIT_MS);
    expect(sent(world)).toEqual([`-TERM ${PID}`]);
  });

  it('goal 2: another process with such a name gets no signal when only its start time differs', async () => {
    const { world, connection } = await attached({ name: AWKWARD_NAME, moving: false });
    reuse(world, PID, { name: AWKWARD_NAME, moving: false });
    await settle(connection.shutdown(), SHUTDOWN_LIMIT_MS);
    expect(sent(world)).toEqual([]);
  });
});

describe('goal 2 and supplement decision 1: a credential re-read through a 401 sets the identity again', () => {
  const NEW_PID = 5151;

  async function reauthenticated(newPid: number, newStartTime: number, spec: Partial<OsProcess> = {}) {
    const attachment = await attached();
    const replacement = attachment.world.addProcess(newPid, { startTime: newStartTime, ...spec });
    attachment.world.publish(newPid, 'tok-rotated', '2026-11-10T03:00:00Z');
    const status = await settle(attachment.connection.readStatus(), 1_000);
    return { ...attachment, replacement, status };
  }

  it('goal 2: after a re-read names another pid, TERM goes to that pid', async () => {
    const { world, connection, status } = await reauthenticated(NEW_PID, OTHER_START_TIME);
    world.processes.delete(PID);
    await settle(connection.shutdown(), SHUTDOWN_LIMIT_MS);
    expect(status).toMatchObject({ ok: true });
    expect(sent(world)).toEqual([`-TERM ${NEW_PID}`]);
  });

  it('goal 2: after a re-read names the same pid with another start time, TERM still goes to it', async () => {
    const { world, connection, status } = await reauthenticated(PID, OTHER_START_TIME);
    await settle(connection.shutdown(), SHUTDOWN_LIMIT_MS);
    expect(status).toMatchObject({ ok: true });
    expect(sent(world)).toEqual([`-TERM ${PID}`]);
  });

  it('goal 2: a pid reused after the re-read gets no signal', async () => {
    const { world, connection } = await reauthenticated(NEW_PID, OTHER_START_TIME);
    world.processes.delete(PID);
    reuse(world, NEW_PID, { startTime: START_TIME + 7 });
    await settle(connection.shutdown(), SHUTDOWN_LIMIT_MS);
    expect(sent(world)).toEqual([]);
  });

  it('goal 2: a stat read that fails during the re-read sends no signal and logs the pid', async () => {
    const { world, connection, replacement, status } = await reauthenticated(NEW_PID, OTHER_START_TIME, { statFailure: 'wsl.exe throws' });
    const logStart = world.logged.length;
    world.processes.delete(PID);
    replacement.statFailure = null;
    await settle(connection.shutdown(), SHUTDOWN_LIMIT_MS);
    expect(status).toMatchObject({ ok: true });
    expect(sent(world)).toEqual([]);
    expect(logLinesNaming(world, NEW_PID, logStart).length).toBeGreaterThan(0);
  });
});

describe('goal 2: a backend the app started as its child passes the same check', () => {
  it('goal 2 control: the same process gets TERM', async () => {
    const { world, connection, state } = await started();
    await settle(connection.shutdown(), SHUTDOWN_LIMIT_MS);
    expect(state).toEqual({ state: 'connected' });
    expect(sent(world)).toEqual([`-TERM ${PID}`]);
  });

  it('goal 2 control: the same process that ignores TERM gets KILL after 30 s', async () => {
    const { world, connection } = await started({ obeysTerm: false });
    await settle(connection.shutdown(), SHUTDOWN_LIMIT_MS);
    expect(sent(world)).toEqual([`-TERM ${PID}`, `-KILL ${PID}`]);
  });

  it('goal 2: a pid that passed to another process gets neither signal, and the child is still ended', async () => {
    const { world, connection, child } = await started();
    reuse(world);
    await settle(connection.shutdown(), SHUTDOWN_LIMIT_MS);
    expect(sent(world)).toEqual([]);
    expect(child.kill).toHaveBeenCalled();
  });

  it('goal 2: a pid that passes to another process while waiting after TERM gets no KILL, and the child is still ended', async () => {
    const { world, connection, child } = await started({ obeysTerm: false });
    const stopping = connection.shutdown();
    await vi.advanceTimersByTimeAsync(5_000);
    const afterTerm = sent(world);
    reuse(world);
    await settle(stopping, SHUTDOWN_LIMIT_MS);
    expect(afterTerm).toEqual([`-TERM ${PID}`]);
    expect(sent(world)).toEqual([`-TERM ${PID}`]);
    expect(child.kill).toHaveBeenCalled();
  });

  it('goal 2: a stat read that fails at shutdown sends no signal, logs the pid and still ends the child', async () => {
    const { world, backend, connection, child } = await started();
    const logStart = world.logged.length;
    backend.statFailure = 'wsl.exe throws';
    await settle(connection.shutdown(), SHUTDOWN_LIMIT_MS);
    expect(sent(world)).toEqual([]);
    expect(logLinesNaming(world, PID, logStart).length).toBeGreaterThan(0);
    expect(child.kill).toHaveBeenCalled();
  });
});

describe('goal 2: shutdown while the started backend is still connecting', () => {
  // The tray quit arrives after the new connection file exists but before the wait reads it again.
  async function stoppingDuringStart(spec: Partial<OsProcess> = {}) {
    const world = createWorld();
    const connection = await connectionFor(world);
    void connection.connect();
    await vi.advanceTimersByTimeAsync(1_100);
    const child = world.spawned[0];
    if (!child) throw new Error('backend-wsl.sh run was not started');
    const backend = world.addProcess(PID, { child, ...spec });
    world.publish(PID, 'tok-started', '2026-11-10T02:00:00Z');
    return { world, connection, child, backend };
  }

  it('goal 2 control: the authenticated new backend gets TERM', async () => {
    const { world, connection } = await stoppingDuringStart();
    await settle(connection.shutdown(), SHUTDOWN_LIMIT_MS);
    expect(sent(world)).toEqual([`-TERM ${PID}`]);
  });

  it('goal 2: a pid that passes to another process right after its identity was read gets no TERM', async () => {
    const { world, connection, backend } = await stoppingDuringStart();
    backend.afterStatRead = () => { reuse(world); };
    await settle(connection.shutdown(), SHUTDOWN_LIMIT_MS);
    expect(sent(world)).toEqual([]);
    expect(sentToOthers(world)).toEqual([]);
  });

  it('goal 2: an unreadable identity sends no TERM, logs the pid and still ends the child', async () => {
    const { world, connection, child } = await stoppingDuringStart({ statFailure: 'wsl.exe throws' });
    const logStart = world.logged.length;
    await settle(connection.shutdown(), SHUTDOWN_LIMIT_MS);
    expect(sent(world)).toEqual([]);
    expect(logLinesNaming(world, PID, logStart).length).toBeGreaterThan(0);
    expect(child.kill).toHaveBeenCalled();
  });
});

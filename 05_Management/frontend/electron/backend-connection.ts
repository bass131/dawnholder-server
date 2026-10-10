import { execFile, spawn } from 'node:child_process';
import type { ChildProcess } from 'node:child_process';
import { request as httpRequest } from 'node:http';
import { StringDecoder } from 'node:string_decoder';
import {
  operationFailure, parseCommitInput, parseLogQuery, parseLogsResponse,
  parseReleasesResponse, parseStatusResponse, toWslPath,
} from './server-operations-contract.js';
import type {
  ConnectionState, LogQuery, LogsResponse, OperationResult, ReleasesResponse, ServerStatus,
} from './server-operations-contract.js';

interface ConnectionInfo { port: number; token: string; pid: number; startedAt: string }
interface ProcessResult { exitCode: number | null; stdout: string; stderr: string }
interface HttpInput {
  host: string;
  port: number;
  method: string;
  path: string;
  headers: Record<string, string>;
  body?: string;
  timeoutMs?: number;
  signal?: AbortSignal;
}
interface HttpResult { statusCode: number; body: string }
type BackendChild = Pick<ChildProcess, 'stdout' | 'stderr' | 'kill' | 'once'>;
interface ConnectionOptions {
  repositoryRoot: string;
  env?: Readonly<Record<string, string | undefined>>;
  spawnProcess?: (file: string, args: readonly string[]) => BackendChild;
  runProcess?: (file: string, args: readonly string[]) => Promise<ProcessResult>;
  request?: (input: HttpInput) => Promise<HttpResult>;
  clock?: { now(): number; sleep(ms: number): Promise<void> };
  log?: (line: string) => void;
}

function defaultRunProcess(file: string, args: readonly string[]): Promise<ProcessResult> {
  return new Promise(resolve => {
    execFile(file, [...args], { windowsHide: true, timeout: 5000, maxBuffer: 1024 * 1024, encoding: 'utf8' }, (error, stdout, stderr) => {
      let exitCode = 0;
      if (error) {
        exitCode = typeof error.code === 'number' ? error.code : 1;
      }
      resolve({ exitCode, stdout, stderr });
    });
  });
}

function defaultRequest(input: HttpInput): Promise<HttpResult> {
  return new Promise((resolve, reject) => {
    const request = httpRequest(input, response => {
      const chunks: Buffer[] = [];
      let bytes = 0;
      response.on('data', (chunk: Buffer) => {
        bytes += chunk.length;
        // The maximum log query contains 5000 lines. Bound a broken peer too.
        if (bytes > 64 * 1024 * 1024) {
          request.destroy(new Error('Response too large'));
          return;
        }
        chunks.push(chunk);
      });
      response.once('error', reject);
      response.once('aborted', () => reject(new Error('Response aborted')));
      response.once('end', () => resolve({ statusCode: response.statusCode ?? 0, body: Buffer.concat(chunks).toString('utf8') }));
    });
    request.once('error', reject);
    request.end(input.body);
  });
}

function parseConnection(text: string): ConnectionInfo | null {
  try {
    const value: unknown = JSON.parse(text);
    if (typeof value !== 'object' || value === null || Array.isArray(value)) return null;
    if (!('port' in value) || typeof value.port !== 'number' || !Number.isInteger(value.port) || value.port < 1 || value.port > 65535
      || !('pid' in value) || typeof value.pid !== 'number' || !Number.isSafeInteger(value.pid) || value.pid <= 0
      || !('token' in value) || typeof value.token !== 'string' || !/^[\w+/=.\-]{1,4096}$/.test(value.token)
      || !('startedAt' in value) || typeof value.startedAt !== 'string' || !Number.isFinite(Date.parse(value.startedAt))) return null;
    return { port: value.port, token: value.token, pid: value.pid, startedAt: value.startedAt };
  } catch {
    return null;
  }
}

function isFreshConnection(candidate: ConnectionInfo | null, previous: ConnectionInfo | null): candidate is ConnectionInfo {
  // WSL may reuse the old pid after restarting; startedAt identifies the new run.
  return candidate !== null && (previous === null || candidate.startedAt !== previous.startedAt);
}

export function createBackendConnection(options: ConnectionOptions) {
  const env = options.env ?? process.env;
  const spawnProcess = options.spawnProcess ?? ((file, args) => spawn(file, [...args], { windowsHide: true, stdio: ['ignore', 'pipe', 'pipe'] }));
  const runProcess = options.runProcess ?? defaultRunProcess;
  const request = options.request ?? defaultRequest;
  const clock = options.clock ?? { now: () => Date.now(), sleep: (ms: number) => new Promise<void>(resolve => setTimeout(resolve, ms)) };
  const log = options.log ?? ((line: string) => console.info(`[management backend] ${line}`));
  const wsl = (args: readonly string[]) => runProcess('wsl.exe', ['-d', 'Ubuntu', '--exec', ...args]);
  let state: ConnectionState = { state: 'disconnected', reason: 'notConnected' };
  let info: ConnectionInfo | null = null;
  let connectionFile: string | null = null;
  let home: string | null = null;
  let child: BackendChild | null = null;
  let launchPrevious: ConnectionInfo | null = null;
  let connecting: Promise<ConnectionState> | null = null;
  let shuttingDown: Promise<void> | null = null;
  let generation = 0;
  let failures = 0;
  const pending = new Set<AbortController>();
  const secrets = new Set<string>();

  function redact(text: string): string {
    let safe = text;
    for (const secret of secrets) safe = safe.replaceAll(secret, '[비밀값 숨김]');
    return safe;
  }

  function pipeLines(stream: BackendChild['stdout']) {
    if (!stream) return;
    const decoder = new StringDecoder('utf8');
    let buffered = '';
    let dropping = false;
    const emit = (line: string) => {
      // Neither cat output nor raw errors are logged. Also discard credential
      // records a child might print before its token has been read.
      if (!/"token"\s*:|authorization\s*:|bearer\s+/i.test(line)) log(redact(line));
    };
    stream.on('data', (data: Buffer | string) => {
      buffered += typeof data === 'string' ? data : decoder.write(data);
      let end = buffered.indexOf('\n');
      while (end >= 0) {
        if (!dropping) emit(buffered.slice(0, end).replace(/\r$/, ''));
        dropping = false;
        buffered = buffered.slice(end + 1);
        end = buffered.indexOf('\n');
      }
      if (buffered.length > 65536) {
        buffered = '';
        dropping = true;
      }
    });
    stream.once('end', () => {
      buffered += decoder.end();
      if (buffered && !dropping) emit(buffered);
      buffered = '';
    });
  }

  async function readConnectionFile(): Promise<ConnectionInfo | null> {
    if (!connectionFile) return null;
    try {
      const file = await wsl(['cat', connectionFile]);
      const found = file.exitCode === 0 ? parseConnection(file.stdout) : null;
      if (found) secrets.add(found.token);
      return found;
    } catch {
      return null;
    }
  }

  async function pidLives(pid: number): Promise<boolean> {
    try {
      return (await wsl(['kill', '-0', String(pid)])).exitCode === 0;
    } catch {
      return false;
    }
  }

  async function exchange(target: ConnectionInfo, method: string, path: string, timeoutMs: number, body?: string): Promise<HttpResult> {
    const controller = new AbortController();
    pending.add(controller);
    let timer: ReturnType<typeof setTimeout> | undefined;
    const cancelled = new Promise<never>((_resolve, reject) => {
      controller.signal.addEventListener('abort', () => reject(new Error('Request ended')), { once: true });
      timer = setTimeout(() => controller.abort(), timeoutMs);
    });
    const headers: Record<string, string> = { Authorization: `Bearer ${target.token}` };
    if (body !== undefined) headers['Content-Type'] = 'application/json';
    try {
      return await Promise.race([
        request({ host: '127.0.0.1', port: target.port, method, path, headers, timeoutMs, signal: controller.signal, ...(body === undefined ? {} : { body }) }),
        cancelled,
      ]);
    } finally {
      clearTimeout(timer);
      pending.delete(controller);
    }
  }

  async function ready(candidate: ConnectionInfo): Promise<boolean> {
    if (!await pidLives(candidate.pid)) return false;
    try {
      const response = await exchange(candidate, 'GET', '/api/status', 5000);
      return response.statusCode === 200;
    } catch {
      return false;
    }
  }

  async function beginConnection(): Promise<ConnectionState> {
    const current = ++generation;
    state = { state: 'starting' };
    failures = 0;
    const config = env.DAWNHOLDER_MANAGEMENT_BACKEND_CONFIG || null;
    const data = env.DAWNHOLDER_MANAGEMENT_DATA_DIR || null;
    if (Boolean(config) !== Boolean(data) || (config && !config.startsWith('/')) || (data && !data.startsWith('/'))) {
      state = { state: 'disconnected', reason: 'invalidOverride' };
      return state;
    }
    const repository = toWslPath(options.repositoryRoot);
    if (!repository) {
      state = { state: 'disconnected', reason: 'invalidRepository' };
      return state;
    }
    const active = () => generation === current && state.state === 'starting';
    try {
      if (!data && home === null) {
        const result = await wsl(['printenv', 'HOME']);
        if (!active()) return state;
        if (result.exitCode !== 0 || !result.stdout.trim().startsWith('/')) throw new Error('HOME unavailable');
        home = result.stdout.trim();
      }
      connectionFile = `${(data ?? `${home}/.local/share/dawnholder/management`).replace(/\/+$/, '')}/connection.json`;
      const previous = await readConnectionFile();
      if (!active()) return state;
      if (previous && await ready(previous)) {
        if (active()) {
          info = previous;
          state = { state: 'connected' };
        }
        return state;
      }
      if (!active()) return state;
      const oldChild = child;
      child = null;
      oldChild?.kill();
      info = null;
      launchPrevious = previous;
      const args = ['-d', 'Ubuntu', '--exec', 'bash', `${repository}/05_Management/backend/backend-wsl.sh`, 'run'];
      if (config) args.push('--config', config);
      const spawned = spawnProcess('wsl.exe', args);
      child = spawned;
      pipeLines(spawned.stdout);
      pipeLines(spawned.stderr);
      const ended = (exitCode: number | null) => {
        if (child !== spawned) return;
        child = null;
        info = null;
        if (state.state === 'starting' || state.state === 'connected') {
          state = { state: 'disconnected', reason: 'backendExited', exitCode };
          for (const controller of pending) controller.abort();
        }
      };
      spawned.once('exit', ended);
      spawned.once('error', () => ended(null));
      // Detach ownership before killing on timeout so a late exit cannot replace
      // startTimeout with backendExited. Shutdown similarly invalidates this run.
      const waitUntilReady = async () => {
        while (active()) {
          const candidate = await readConnectionFile();
          if (active() && isFreshConnection(candidate, previous) && await ready(candidate)) {
            if (active()) {
              info = candidate;
              state = { state: 'connected' };
            }
            break;
          }
          if (active()) await clock.sleep(250);
        }
      };
      let timer: ReturnType<typeof setTimeout> | undefined;
      const timeout = new Promise<void>(resolve => {
        timer = setTimeout(() => {
          if (active()) {
            state = { state: 'disconnected', reason: 'startTimeout' };
            child = null;
            spawned.kill();
            for (const controller of pending) controller.abort();
          }
          resolve();
        }, 120000);
      });
      try {
        // A slow cat command must not extend the 120-second start deadline.
        await Promise.race([waitUntilReady(), timeout]);
      } finally {
        clearTimeout(timer);
      }
    } catch {
      if (active()) state = { state: 'disconnected', reason: 'backendUnreachable' };
    }
    return state;
  }

  function connect(): Promise<ConnectionState> {
    if (state.state === 'connected' || state.state === 'stopping') return Promise.resolve({ ...state });
    if (connecting) return connecting;
    connecting = beginConnection().finally(() => { connecting = null; });
    return connecting;
  }

  async function operation<T extends object>(method: string, path: string, timeoutMs: number, parse: (body: unknown) => T | null, body?: string): Promise<OperationResult<T>> {
    if (state.state !== 'connected' || !info) return operationFailure('notConnected');
    const current = generation;
    const deadline = clock.now() + timeoutMs;
    let target = info;
    let expired = false;
    let timer: ReturnType<typeof setTimeout> | undefined;
    const timeLimit = new Promise<never>((_resolve, reject) => {
      timer = setTimeout(() => {
        expired = true;
        reject(new Error('Request timed out'));
      }, timeoutMs);
    });
    const perform = async (): Promise<OperationResult<T>> => {
      for (let attempt = 0; attempt < 2; attempt += 1) {
        const response = await exchange(target, method, path, Math.max(1, deadline - clock.now()), body);
        if (expired || current !== generation || state.state !== 'connected') return operationFailure('notConnected');
        failures = 0;
        if (response.statusCode === 401 && attempt === 0) {
          const refreshed = await readConnectionFile();
          if (expired || current !== generation || state.state !== 'connected') return operationFailure('notConnected');
          if (!refreshed) return operationFailure('unauthorized');
          info = refreshed;
          target = refreshed;
          continue;
        }
        let answer: unknown;
        try {
          answer = JSON.parse(redact(response.body));
        } catch {
          return operationFailure('invalidResponse');
        }
        if (response.statusCode < 200 || response.statusCode >= 300) {
          const code = typeof answer === 'object' && answer !== null && 'error' in answer && typeof answer.error === 'string'
            ? answer.error : 'operationFailed';
          return operationFailure(code);
        }
        const checked = parse(answer);
        return checked === null ? operationFailure('invalidResponse') : { ok: true, ...checked };
      }
      return operationFailure('unauthorized');
    };
    try {
      // The limit includes the one credential reread and retry, not just each
      // socket exchange. A late file read cannot update connection information.
      return await Promise.race([perform(), timeLimit]);
    } catch {
      if (current === generation && state.state === 'connected') {
        failures += 1;
        if (failures >= 3) state = { state: 'disconnected', reason: 'backendUnreachable' };
      }
      return operationFailure('backendUnreachable');
    } finally {
      expired = true;
      clearTimeout(timer);
    }
  }

  async function stopConnection(): Promise<void> {
    generation += 1;
    state = { state: 'stopping' };
    for (const controller of pending) controller.abort();
    const stoppingChild = child;
    let target = info;
    if (!target && stoppingChild) {
      const candidate = await readConnectionFile();
      // During startup an old file may still belong to a backend we could not
      // attach to. Only a fresh, authenticated backend is ours to terminate.
      if (isFreshConnection(candidate, launchPrevious) && await ready(candidate)) target = candidate;
    }
    try {
      if (target) {
        await wsl(['kill', '-TERM', String(target.pid)]);
        const deadline = clock.now() + 30000;
        while (clock.now() < deadline) {
          const alive = stoppingChild ? child === stoppingChild : await pidLives(target.pid);
          if (!alive) return;
          await clock.sleep(Math.min(250, deadline - clock.now()));
        }
        const alive = stoppingChild ? child === stoppingChild : await pidLives(target.pid);
        if (alive) await wsl(['kill', '-KILL', String(target.pid)]);
      }
    } finally {
      if (child === stoppingChild) {
        child = null;
        stoppingChild?.kill();
      }
      info = null;
    }
  }

  function readStatus(): Promise<OperationResult<{ status: ServerStatus }>> {
    return operation('GET', '/api/status', 5000, body => {
      const status = parseStatusResponse(body);
      return status ? { status } : null;
    });
  }
  function readLogs(input: LogQuery): Promise<OperationResult<{ logs: LogsResponse }>> {
    const query = parseLogQuery(input);
    if (!query) return Promise.resolve(operationFailure('invalidQuery'));
    const params = new URLSearchParams({ minutes: String(query.minutes), limit: String(query.limit) });
    query.contains.forEach(word => params.append('contains', word));
    return operation('GET', `/api/logs?${params}`, 5000, body => {
      const logs = parseLogsResponse(body);
      return logs ? { logs } : null;
    });
  }
  function readReleases(): Promise<OperationResult<{ releases: ReleasesResponse }>> {
    return operation('GET', '/api/releases', 5000, body => {
      const releases = parseReleasesResponse(body);
      return releases ? { releases } : null;
    });
  }
  function releaseOperation(commit: string, build: boolean): Promise<OperationResult> {
    if (parseCommitInput({ commit }) === null) return Promise.resolve(operationFailure('invalidCommit'));
    const body = JSON.stringify({ commit });
    if (build) {
      return operation('POST', '/api/releases', 660000, () => ({}), body);
    }
    return operation('PUT', '/api/releases/current', 30000, () => ({}), body);
  }

  return {
    connect,
    connectionState: (): ConnectionState => ({ ...state }),
    readStatus, readLogs, readReleases,
    startServer: () => operation('POST', '/api/server/start', 30000, () => ({})),
    stopServer: () => operation('POST', '/api/server/stop', 30000, body => parseStatusResponse(body) ? {} : null),
    forceStopServer: () => operation('POST', '/api/server/force-stop', 30000, () => ({})),
    buildRelease: (commit: string) => releaseOperation(commit, true),
    selectRelease: (commit: string) => releaseOperation(commit, false),
    shutdown: () => {
      shuttingDown ??= stopConnection();
      return shuttingDown;
    },
  };
}

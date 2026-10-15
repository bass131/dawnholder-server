import { execFile } from 'node:child_process';
import { createHash } from 'node:crypto';
import { constants } from 'node:fs';
import { access, open, realpath, stat } from 'node:fs/promises';
import { dirname, isAbsolute, join, resolve } from 'node:path';
import { promisify } from 'node:util';

const executeFile = promisify(execFile);
const REQUEST_LIMIT = 1024 * 1024;

export class LockError extends Error {
  constructor(status, code, message, repair) {
    super(message);
    this.status = status;
    this.code = code;
    this.repair = repair;
  }
}

export function fault(code, message, repair = 'Preserve the evidence and inspect the named state or OS failure.') {
  return new LockError('tool-error', code, message, repair);
}

export function inputError(message) {
  return new LockError('input-error', 'invalid-request', message,
    'Supply one UTF-8 JSON request matching the ExecutionLock README; no command was admitted.');
}

export function resultFields() {
  return {
    version: 1, status: null, exitCode: null, code: null, message: null, repair: null,
    requestId: null, repositoryCommonDir: null, repositoryKey: null, stateRoot: null,
    evidenceDir: null, executionId: null, commandExitCode: null,
    observationSource: null, observedAt: null,
  };
}

export function outcome(context, status, code, message, repair = 'See the execution evidence for observed scope.') {
  const exits = { observed: 0, succeeded: 0, blocked: 1, 'input-error': 2, 'tool-error': 2,
    'timed-out': 124, cancelled: 130 };
  const commandExit = context.commandExitCode;
  const exitCode = status === 'failed'
    ? (Number.isInteger(commandExit) && commandExit >= 1 && commandExit <= 255 ? commandExit : 1)
    : exits[status];
  return { ...context, status, exitCode, code, message, repair };
}

export const sha256 = bytes => createHash('sha256').update(bytes).digest('hex');
export const sleep = ms => new Promise(done => setTimeout(done, ms));
export const isoDate = value => typeof value === 'string' && /^\d{4}-\d\d-\d\dT.*Z$/.test(value)
  && Number.isFinite(Date.parse(value));
export const object = value => value !== null && typeof value === 'object' && !Array.isArray(value);

function keys(value, allowed, label) {
  if (!object(value) || Object.keys(value).some(key => !allowed.includes(key))) {
    throw inputError(`${label} must be an object containing only documented fields.`);
  }
}

function pathString(value, label) {
  if (typeof value !== 'string' || !isAbsolute(value) || value.includes('\0')) {
    throw inputError(`${label} must be an absolute path without NUL.`);
  }
}

async function existingPath(value, directory, label) {
  pathString(value, label);
  try {
    const information = await stat(value);
    if (directory ? !information.isDirectory() : !information.isFile()) throw new Error('wrong kind');
    await access(value, directory ? constants.R_OK : constants.R_OK | constants.X_OK);
    return await realpath(value);
  } catch {
    throw inputError(`${label} must be an existing accessible ${directory ? 'directory' : 'file'}: ${value}`);
  }
}

export async function canonicalFuturePath(value) {
  pathString(value, 'stateRoot');
  try {
    return await realpath(value);
  } catch (error) {
    if (error.code !== 'ENOENT') throw error;
    const parent = dirname(value);
    if (parent === value) throw error;
    return join(await canonicalFuturePath(parent), value.slice(parent.length).replace(/^[/\\]+/, ''));
  }
}

export async function readRequest(argv) {
  if (!Array.isArray(argv) || argv.length !== 1 || typeof argv[0] !== 'string'
    || !argv[0] || argv[0].startsWith('-') || argv[0].includes('\0')) {
    throw inputError('Expected exactly one JSON file path.');
  }
  let bytes;
  let handle;
  try {
    handle = await open(argv[0], 'r');
    const information = await handle.stat();
    if (!information.isFile() || information.size > REQUEST_LIMIT) throw new Error('size/type');
    // Bound the allocation/read even if a writer grows the file after stat.
    const buffer = Buffer.alloc(REQUEST_LIMIT + 1);
    let length = 0;
    while (length < buffer.length) {
      const { bytesRead } = await handle.read(buffer, length, buffer.length - length, null);
      if (bytesRead === 0) break;
      length += bytesRead;
    }
    if (length > REQUEST_LIMIT) throw new Error('size');
    bytes = buffer.subarray(0, length);
  } catch {
    throw inputError('The request must be a readable regular file of at most 1 MiB.');
  } finally {
    await handle?.close();
  }
  let request;
  try {
    request = JSON.parse(new TextDecoder('utf-8', { fatal: true }).decode(bytes));
  } catch {
    throw inputError('The request is not valid UTF-8 JSON.');
  }
  keys(request, ['version', 'action', 'repoRoot', 'evidenceDir', 'execution'], 'request');
  if (request.version !== 1 || !['inspect', 'run'].includes(request.action)) {
    throw inputError('Expected version 1 and action inspect or run.');
  }
  request.repoRoot = await existingPath(request.repoRoot, true, 'repoRoot');
  if (request.action === 'inspect') {
    if ('execution' in request) throw inputError('inspect does not accept execution.');
    if ('evidenceDir' in request) await existingPath(request.evidenceDir, true, 'evidenceDir');
  } else {
    request.evidenceDir = await existingPath(request.evidenceDir, true, 'evidenceDir');
    const execution = request.execution;
    keys(execution, ['kind', 'part', 'file', 'args', 'cwd', 'timeoutMs', 'waitTimeoutMs'], 'execution');
    if (!['unity', 'wsl'].includes(execution.kind) || typeof execution.part !== 'string'
      || execution.part.length < 1 || execution.part.length > 64 || /[\s\0]/u.test(execution.part)) {
      throw inputError('execution.kind or execution.part is invalid.');
    }
    if (!Array.isArray(execution.args) || execution.args.length > 256
      || execution.args.some(arg => typeof arg !== 'string' || arg.length > 32768 || arg.includes('\0'))) {
      throw inputError('execution.args must contain at most 256 strings, each at most 32768 characters without NUL.');
    }
    if (!Number.isInteger(execution.timeoutMs) || execution.timeoutMs < 1 || execution.timeoutMs > 3600000
      || ('waitTimeoutMs' in execution && (!Number.isInteger(execution.waitTimeoutMs)
        || execution.waitTimeoutMs < 0 || execution.waitTimeoutMs > 300000))) {
      throw inputError('execution timeoutMs/waitTimeoutMs is outside the documented integer bounds.');
    }
    execution.file = await existingPath(execution.file, false, 'execution.file');
    execution.cwd = await existingPath(execution.cwd, true, 'execution.cwd');
    execution.waitTimeoutMs ??= 60000;
  }
  return { request, bytes, requestPath: resolve(argv[0]) };
}

export async function repositoryLocation(repoRoot, injectedRoot) {
  let common;
  try {
    const environment = { ...process.env };
    for (const key of ['GIT_DIR', 'GIT_COMMON_DIR', 'GIT_WORK_TREE', 'GIT_INDEX_FILE', 'GIT_OBJECT_DIRECTORY']) {
      delete environment[key];
    }
    const { stdout } = await executeFile('git', ['-C', repoRoot, 'rev-parse', '--path-format=absolute',
      '--git-common-dir'], { env: environment, encoding: 'utf8', timeout: 10000,
      maxBuffer: 1024 * 1024, windowsHide: true });
    common = await realpath(stdout.trim());
    if (!(await stat(common)).isDirectory()) throw new Error('not directory');
  } catch {
    throw inputError(`repoRoot is not an accessible Git repository/worktree: ${repoRoot}`);
  }
  const identity = process.platform === 'win32' ? common.toLowerCase() : common;
  return {
    repositoryCommonDir: common,
    repositoryKey: sha256(identity),
    stateRoot: await canonicalFuturePath(injectedRoot ?? join(common, 'dawnholder-execution-lock')),
  };
}

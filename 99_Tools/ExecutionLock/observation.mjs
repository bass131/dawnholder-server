import { execFile } from 'node:child_process';
import { join, win32 } from 'node:path';
import { fileURLToPath } from 'node:url';
import { promisify } from 'node:util';
import { fault, isoDate, object } from './contract.mjs';

const executeFile = promisify(execFile);
const OBSERVATION_TIMEOUT_MS = 10000;

export function sameIdentity(left, right) {
  return left?.pid === right?.pid && left?.startedAt === right?.startedAt;
}

export function identity(processInfo) {
  return { pid: processInfo.pid, parentPid: processInfo.parentPid, startedAt: processInfo.startedAt };
}

export function processKind(processInfo) {
  const name = win32.basename(processInfo.name).toLowerCase();
  if (name === 'unity.exe') return 'unity';
  if (name === 'wsl.exe') return 'wsl';
  return null;
}

export function validateObservation(value) {
  if (!object(value) || value.complete !== true || !isoDate(value.observedAt)
    || !object(value.memory) || !Array.isArray(value.processes)) {
    throw fault('incomplete-observation', 'A complete timestamped memory/process observation is required.');
  }
  for (const field of ['physicalAvailableBytes', 'commitAvailableBytes']) {
    if (!Number.isSafeInteger(value.memory[field]) || value.memory[field] < 0) {
      throw fault('invalid-memory', `Observation memory.${field} is missing or not a nonnegative byte count.`);
    }
  }
  const seen = new Set();
  for (const entry of value.processes) {
    if (!object(entry) || !Number.isInteger(entry.pid) || entry.pid < 0
      || !Number.isInteger(entry.parentPid) || entry.parentPid < 0 || typeof entry.name !== 'string'
      || seen.has(entry.pid) || (entry.startedAt !== null && !isoDate(entry.startedAt))) {
      throw fault('invalid-process-observation', 'Invalid or duplicate PID/parent/start identity in observation.');
    }
    seen.add(entry.pid);
    // Some protected Windows system processes hide CreationDate. Missing identities remain explicit;
    // they cannot be used for an owner, a related child, or a Unity/WSL admission decision.
    if (processKind(entry) && !entry.startedAt) {
      throw fault('unknown-process-identity', `Cannot identify ${entry.name} PID ${entry.pid}.`);
    }
  }
  return value;
}

async function observeWindows() {
  if (process.platform !== 'win32') {
    throw fault('unsupported-platform', `Native observation is unsupported on ${process.platform}.`,
      'Use native Windows for real execution. Tests may explicitly inject an observation through runCli.');
  }
  const powershell = join(process.env.SystemRoot ?? 'C:\\Windows',
    'System32', 'WindowsPowerShell', 'v1.0', 'powershell.exe');
  const script = fileURLToPath(new URL('./observe-windows.ps1', import.meta.url));
  try {
    const { stdout, stderr } = await executeFile(powershell,
      ['-NoLogo', '-NoProfile', '-NonInteractive', '-File', script],
      { encoding: 'utf8', timeout: OBSERVATION_TIMEOUT_MS - 500, maxBuffer: 16 * 1024 * 1024, windowsHide: true });
    if (stderr.trim()) throw new Error(stderr.trim());
    return JSON.parse(stdout.replace(/^\uFEFF/, ''));
  } catch (error) {
    throw fault('windows-observation-failed', `Windows CIM observation failed: ${error.message}`,
      'Check PowerShell/CIM access and the observation script; do not replace unavailable values with estimates.');
  }
}

export function createObserver(dependencies, context, evidence) {
  const injected = dependencies.observe !== undefined || dependencies.stateRoot !== undefined;
  const provider = dependencies.observe ?? observeWindows;
  if (typeof provider !== 'function') throw fault('invalid-observer', 'dependencies.observe must be a function.');
  return async () => {
    context.observationSource = injected ? 'injected' : (process.platform === 'win32' ? 'windows' : null);
    let timer;
    try {
      const timeout = new Promise((_, reject) => {
        timer = setTimeout(() => reject(fault('observation-timeout', 'Observation exceeded 10000 ms.')),
          OBSERVATION_TIMEOUT_MS);
      });
      const raw = await Promise.race([Promise.resolve().then(provider), timeout]);
      await evidence?.event('observation', { source: context.observationSource, raw });
      const observed = validateObservation(raw);
      context.observedAt = observed.observedAt;
      return observed;
    } catch (error) {
      await evidence?.event('observation-failure', { source: context.observationSource, message: error.message });
      if (error.status) throw error;
      throw fault('observation-failed', `Observation failed: ${error.message}`);
    } finally {
      clearTimeout(timer);
    }
  };
}

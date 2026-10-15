import { spawn } from 'node:child_process';
import { randomUUID } from 'node:crypto';
import { admissionReason, findIdentityConflict, reconcile, refreshBundle } from './admission.mjs';
import { fault, outcome, sleep } from './contract.mjs';
import { identity, sameIdentity } from './observation.mjs';

const POLL_MS = 30;
const CLEANUP_LIMIT_MS = 5000;

function blocked(context, code) {
  return outcome(context, 'blocked', code, `Execution was not admitted: ${code}.`,
    'Inspect current owners, queued priorities and the recorded observation; retry only after the conflict is resolved.');
}

function cancelled(context) {
  return outcome(context, 'cancelled', 'cancelled', 'The request was cancelled.',
    'Inspect cleanup and retained bundle identities in the evidence before another execution.');
}

async function removeUnstarted(store, id) {
  await store.transaction(async transaction => {
    const state = await transaction.read();
    const entry = state.entries.find(item => item.id === id);
    if (entry && !entry.spawnAttempted && entry.root === null) {
      state.entries = state.entries.filter(item => item.id !== id);
      await transaction.write(state);
    }
  });
}

async function reserve({ execution, store, signal, context, evidence }) {
  const deadline = Date.now() + execution.waitTimeoutMs;
  const id = randomUUID();
  let registered = false;
  try {
    for (;;) {
      if (signal?.aborted) {
        if (registered) await removeUnstarted(store, id);
        return { result: cancelled(context) };
      }
      const attempt = await store.transaction(async transaction => {
        const state = await transaction.read();
        const observation = await transaction.observe();
        const conflict = findIdentityConflict(state, observation);
        if (conflict !== null) return { reason: 'process-identity-mismatch', frozen: true };
        reconcile(state, observation);
        let entry = state.entries.find(item => item.id === id);
        if (!entry) {
          const owner = observation.processes.find(item => item.pid === process.pid);
          if (!owner?.startedAt) {
            throw fault('unknown-runner-identity', `Current runner PID ${process.pid} has no observed start identity.`);
          }
          entry = {
            id, kind: execution.kind, part: execution.part, owner: identity(owner), phase: 'waiting',
            registeredAt: new Date().toISOString(), sequence: state.nextSequence++, root: null,
            members: [], rootExited: false, ownerFinished: false, spawnAttempted: false,
            evidenceDir: context.evidenceDir,
          };
          state.entries.push(entry);
        }
        if (signal?.aborted) {
          state.entries = state.entries.filter(item => item.id !== id);
          await transaction.write(state);
          return { aborted: true };
        }
        const reason = admissionReason(state, entry, observation);
        if (reason === null) entry.phase = 'reserved';
        await transaction.write(state);
        registered = true;
        return { reason, entry };
      });
      if (attempt.aborted) return { result: cancelled(context) };
      if (attempt.reason === null) {
        context.executionId = id;
        await evidence.event('reservation', { entry: attempt.entry });
        return { id };
      }
      if (attempt.frozen || Date.now() >= deadline) {
        if (registered) await removeUnstarted(store, id);
        return { result: blocked(context, attempt.reason) };
      }
      await sleep(Math.min(POLL_MS, Math.max(1, deadline - Date.now())));
    }
  } catch (error) {
    if (registered) {
      try {
        await removeUnstarted(store, id);
      } catch (cleanupError) {
        await evidence.event('waiting-cleanup-failure', { message: cleanupError.message });
      }
    }
    throw error;
  }
}

function spawnOwned(execution, evidence) {
  const child = {
    process: null, pid: null, exited: false, code: null, signal: null, error: null,
    requestedAt: new Date().toISOString(), startedMs: Date.now(),
  };
  try {
    child.process = spawn(execution.file, execution.args, {
      cwd: execution.cwd, shell: false, windowsHide: true,
      stdio: ['ignore', evidence.stdout.fd, evidence.stderr.fd],
    });
    child.pid = child.process.pid ?? null;
    child.process.once('error', error => {
      child.error = error.message;
      child.exited = true;
    });
    child.process.once('exit', (code, signal) => {
      child.code = code;
      child.signal = signal;
      child.exited = true;
    });
  } catch (error) {
    child.error = error.message;
    child.exited = true;
  }
  return child;
}

async function startReserved(store, id, execution, signal, evidence) {
  let child = null;
  try {
    await store.transaction(async transaction => {
      const state = await transaction.read();
      const entry = state.entries.find(item => item.id === id);
      if (!entry || entry.phase !== 'reserved' || entry.owner.pid !== process.pid) {
        throw fault('reservation-lost', 'The reserved execution no longer belongs to this runner.');
      }
      if (signal?.aborted) {
        state.entries = state.entries.filter(item => item.id !== id);
        await transaction.write(state);
        return;
      }
      // Commit the uncertain spawn window first. If this process disappears before storing a PID,
      // another runner cannot prove that no command was started and must retain the reservation.
      entry.spawnAttempted = true;
      entry.phase = 'uncertain';
      await transaction.write(state);
      child = spawnOwned(execution, evidence);
      if (child.pid === null) {
        state.entries = state.entries.filter(item => item.id !== id);
      } else {
        entry.phase = 'running';
        entry.root = { pid: child.pid, parentPid: process.pid, startedAt: null };
      }
      await transaction.write(state);
      if (child.pid !== null) {
        // Persist the receipt before observing, but keep the mutex until the live handle's first
        // identity (or its confirmed exit) is known. A missing sample is never a fabricated start.
        const identifyDeadline = Math.min(child.startedMs + execution.timeoutMs, Date.now() + 5000);
        do {
          const observation = await transaction.observe();
          if (findIdentityConflict(state, observation) !== null) {
            throw fault('spawn-identity-conflict', 'A recorded process identity changed during spawn.');
          }
          if (child.exited) entry.rootExited = true;
          refreshBundle(entry, observation, child);
          await transaction.write(state);
          if (entry.root.startedAt || child.exited || signal?.aborted) break;
          await sleep(POLL_MS);
        } while (Date.now() < identifyDeadline);
      }
    });
    return child;
  } catch (error) {
    // No PID after an interrupted state write is not proof of no spawn. Keep the state for review.
    if (child?.process) child.process.unref();
    throw error;
  }
}

async function updateExecution(store, id, child, { finished = false, terminate = false } = {}) {
  return store.transaction(async transaction => {
    const state = await transaction.read();
    const observation = await transaction.observe();
    const conflict = findIdentityConflict(state, observation);
    if (conflict !== null) return { conflict, released: false, terminated: false };
    const entry = state.entries.find(item => item.id === id);
    if (!entry) return { released: true, terminated: false };
    if (child.exited) entry.rootExited = true;
    const bundle = refreshBundle(entry, observation, child);
    let terminated = false;
    if (terminate && !child.exited && entry.root?.startedAt) {
      const current = observation.processes.find(item => item.pid === child.pid);
      if (sameIdentity(entry.root, current)) {
        // Kill only the exact ChildProcess handle this runner created. Arbitrary descendant PIDs
        // are never sent signals: verified surviving children retain the lease until they finish.
        terminated = child.process.kill('SIGKILL');
      }
    }
    if (finished) entry.ownerFinished = true;
    reconcile(state, observation, { id, child });
    await transaction.write(state);
    return { released: !state.entries.some(item => item.id === id), terminated,
      bundle, root: entry.root, members: entry.members };
  });
}

async function retainAfterFailure(store, id, evidence) {
  try {
    await store.transaction(async transaction => {
      const state = await transaction.read();
      const entry = state.entries.find(item => item.id === id);
      if (entry) {
        entry.ownerFinished = true;
        await transaction.write(state);
      }
    });
  } catch (error) {
    await evidence.event('retained-state-failure', { message: error.message });
  }
}

export async function runExecution(options) {
  const { execution, store, signal, context, evidence } = options;
  const reservation = await reserve(options);
  if (reservation.result) return reservation.result;
  const { id } = reservation;
  const child = await startReserved(store, id, execution, signal, evidence);
  if (child === null) return cancelled(context);
  await evidence.event('spawn-receipt', { pid: child.pid, requestedAt: child.requestedAt,
    file: execution.file, args: execution.args, cwd: execution.cwd, shell: false, error: child.error });
  if (child.pid === null) {
    // The asynchronous spawn error is normally delivered before the next timer tick.
    await sleep(0);
    return outcome(context, 'tool-error', 'spawn-failed', `Command could not start: ${child.error ?? 'no PID'}.`,
      'Check the executable and argv on this OS; the unstarted reservation was removed.');
  }
  const deadline = child.startedMs + execution.timeoutMs;
  let interrupted = null;
  try {
    for (;;) {
      if (signal?.aborted) interrupted = 'cancelled';
      else if (Date.now() >= deadline) interrupted = 'timed-out';
      if (interrupted) break;
      const current = await updateExecution(store, id, child);
      if (current.conflict !== undefined) {
        child.process.unref();
        return blocked(context, 'process-identity-mismatch');
      }
      if (current.released) {
        context.commandExitCode = child.code;
        await evidence.event('command-exit', { code: child.code, signal: child.signal, error: child.error,
          root: current.root, members: current.members, released: true });
        if (child.error) throw fault('command-error', child.error);
        return outcome(context, child.code === 0 ? 'succeeded' : 'failed',
          child.code === 0 ? 'command-succeeded' : 'command-failed',
          `Command exited with ${child.code === null ? `signal ${child.signal}` : child.code}; bundle ended.`);
      }
      await sleep(POLL_MS);
    }

    const cleanupDeadline = Date.now() + CLEANUP_LIMIT_MS;
    let cleanup;
    do {
      cleanup = await updateExecution(store, id, child, { finished: true, terminate: true });
      await evidence.event('cleanup', { requestedStatus: interrupted, ...cleanup });
      if (cleanup.released || cleanup.conflict !== undefined || child.exited) break;
      await sleep(POLL_MS);
    } while (Date.now() < cleanupDeadline);
    context.commandExitCode = child.code;
    child.process.unref();
    return outcome(context, interrupted, interrupted,
      `${interrupted === 'cancelled' ? 'Cancellation' : 'Execution timeout'}; `
        + (cleanup?.released ? 'bundle ended and lease released.' : 'remaining or uncertain bundle state retained.'),
      'Inspect cleanup observations and retained identities; surviving children are never treated as completed.');
  } catch (error) {
    await retainAfterFailure(store, id, evidence);
    child.process.unref();
    throw error;
  }
}

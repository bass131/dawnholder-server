import { randomUUID } from 'node:crypto';
import { mkdir, open, readdir, readFile, rename, rmdir, stat, unlink } from 'node:fs/promises';
import { join } from 'node:path';
import { findIdentityConflict, refreshBundle } from './admission.mjs';
import { LockError, fault, isoDate, object, sleep } from './contract.mjs';
import { identity, sameIdentity } from './observation.mjs';

const MUTEX_TIMEOUT_MS = 15000;
const MAX_STATE_BYTES = 8 * 1024 * 1024;
const SNAPSHOT_TIMEOUT_MS = 1000;

function validIdentity(value, pending = false) {
  return object(value) && Number.isInteger(value.pid) && value.pid > 0
    && Number.isInteger(value.parentPid) && value.parentPid >= 0
    && (isoDate(value.startedAt) || (pending && value.startedAt === null));
}

function validateState(state, repositoryKey) {
  if (!object(state) || state.version !== 1 || state.repositoryKey !== repositoryKey
    || !Number.isSafeInteger(state.nextSequence) || state.nextSequence < 0 || !Array.isArray(state.entries)) {
    throw new Error('Invalid state header or repository identity.');
  }
  const ids = new Set();
  for (const entry of state.entries) {
    if (!object(entry) || typeof entry.id !== 'string' || !entry.id || ids.has(entry.id)
      || !['unity', 'wsl'].includes(entry.kind) || typeof entry.part !== 'string'
      || entry.part.length < 1 || entry.part.length > 64 || /[\s\0]/u.test(entry.part)
      || !validIdentity(entry.owner) || !isoDate(entry.registeredAt)
      || !Number.isSafeInteger(entry.sequence) || entry.sequence < 0 || entry.sequence >= state.nextSequence
      || !['waiting', 'reserved', 'running', 'uncertain'].includes(entry.phase)
      || !(entry.root === null || validIdentity(entry.root, true))
      || !Array.isArray(entry.members) || entry.members.some(member => !validIdentity(member))
      || typeof entry.rootExited !== 'boolean' || typeof entry.ownerFinished !== 'boolean'
      || typeof entry.spawnAttempted !== 'boolean' || typeof entry.evidenceDir !== 'string') {
      throw new Error('Invalid or unknown execution entry.');
    }
    ids.add(entry.id);
    if (entry.phase === 'waiting' || entry.phase === 'reserved') {
      if (entry.root !== null || entry.spawnAttempted || entry.members.length || entry.rootExited) {
        throw new Error('Process facts in an unstarted entry.');
      }
    } else if (!entry.spawnAttempted || (entry.phase === 'running' && entry.root === null)) {
      throw new Error('Started entry without a spawn receipt.');
    }
    const lineage = [entry.root, ...entry.members].filter(Boolean);
    const pids = new Set(lineage.map(item => item.pid));
    if (pids.size !== lineage.length || pids.has(entry.owner.pid)
      || (entry.root && entry.root.parentPid !== entry.owner.pid)
      || entry.members.some(member => member.pid === member.parentPid || !pids.has(member.parentPid))) {
      throw new Error('Inconsistent process lineage.');
    }
  }
  return state;
}

export function createStore(root, repositoryKey, observe, evidence) {
  const statePath = join(root, 'state.json');
  const mutex = join(root, 'transaction');
  let runnerIdentity = null;

  async function sameOwnerRecord(record) {
    try {
      if (!(await readFile(record.ownerPath)).equals(record.ownerBytes)) {
        throw fault('state-lock-owner-changed', `Owner bytes changed at ${record.ownerPath}.`);
      }
      return true;
    } catch (error) {
      if (error.code === 'ENOENT') {
        await evidence?.event('transaction-generation-changed', { token: record.owner.token,
          ownerPath: record.ownerPath });
        return false;
      }
      throw error;
    }
  }

  async function retireOwned(token, destination, claimedRecovery = false) {
    const started = Date.now();
    const ownerName = `owner-${token}.json`;
    for (let attempt = 1; ; attempt += 1) {
      try {
        const names = await readdir(mutex);
        const expected = claimedRecovery ? ['barrier'] : ['barrier', ownerName];
        if (names.length !== expected.length || expected.some(name => !names.includes(name))
          || await readFile(join(mutex, 'barrier'), 'utf8') !== 'execution-lock-v1') {
          throw fault('state-lock-owner-changed', `Retirement ownership is unclear at ${mutex}.`);
        }
        if (!claimedRecovery) {
          const record = JSON.parse(await readFile(join(mutex, ownerName), 'utf8'));
          if (record.token !== token || !sameIdentity(record.owner, runnerIdentity)) {
            throw fault('state-lock-owner-changed', `Retirement owner changed at ${mutex}.`);
          }
        }
        // A live normal owner cannot be reclaimed. A recovery claimant already won the unique
        // owner-file unlink; its nonempty barrier prevents publication until this rename.
        // Thus no cooperating contender can replace this generation between check and rename.
        await rename(mutex, destination);
        await evidence?.event('transaction-retired', { token, destination, claimedRecovery,
          attempt, elapsedMs: Date.now() - started });
        return;
      } catch (error) {
        const elapsedMs = Date.now() - started;
        await evidence?.event('transaction-retire-failure', { token, destination, claimedRecovery,
          attempt, elapsedMs, budgetMs: MUTEX_TIMEOUT_MS, code: error.code ?? error.name,
          syscall: error.syscall ?? null, path: error.path ?? mutex, message: error.message });
        if (!['EPERM', 'EACCES', 'EBUSY'].includes(error.code) || elapsedMs >= MUTEX_TIMEOUT_MS) throw error;
        await sleep(25);
      }
    }
  }

  async function readOwner() {
    let names;
    try {
      names = await readdir(mutex);
    } catch (error) {
      if (['ENOENT', 'EPERM', 'EACCES', 'EBUSY'].includes(error.code)) {
        await evidence?.event('transaction-read-retry', { code: error.code, path: mutex, message: error.message });
        return null;
      }
      throw error;
    }
    const owners = names.filter(name => /^owner-[a-f\d-]+\.json$/.test(name));
    if (owners.length === 0 && names.length === 1 && names[0] === 'barrier') return null;
    if (owners.length !== 1 || names.length !== 2 || !names.includes('barrier')) {
      throw fault('unknown-transaction-owner', `Unknown transaction contents at ${mutex}.`);
    }
    const ownerPath = join(mutex, owners[0]);
    try {
      const bytes = await readFile(ownerPath);
      const owner = JSON.parse(bytes);
      if (!validIdentity(owner.owner) || owners[0] !== `owner-${owner.token}.json`
        || await readFile(join(mutex, 'barrier'), 'utf8') !== 'execution-lock-v1') {
        throw new Error('Invalid transaction identity/barrier.');
      }
      return { ownerPath, ownerBytes: bytes, owner };
    } catch (error) {
      // Windows may report a sharing/delete-pending error when the entire directory is retiring.
      // Retry only within the caller's finite lock budget; never interpret it as an unlocked state.
      if (['ENOENT', 'EPERM', 'EACCES', 'EBUSY'].includes(error.code)) {
        await evidence?.event('transaction-read-retry', { code: error.code, message: error.message });
        return null;
      }
      throw fault('invalid-transaction-owner', `Cannot read ${ownerPath}: ${error.code ?? error.name}: ${error.message}`);
    }
  }

  async function read({ locked = false, allowRedundantPending = false } = {}) {
    let names;
    try {
      names = await readdir(root);
    } catch (error) {
      if (error.code === 'ENOENT') return { version: 1, repositoryKey, nextSequence: 0, entries: [] };
      throw fault('state-read', `Cannot read state root ${root}: ${error.message}`);
    }
    const pending = names.filter(name => /^state-[a-f\d-]+\.pending$/.test(name));
    const inactive = names.filter(name => /^(candidate|retired)-[a-f\d-]+$/.test(name));
    for (const name of inactive) {
      try {
        if (!(await stat(join(root, name))).isDirectory()) throw new Error('Expected an inactive directory.');
      } catch (error) {
        if (error.code !== 'ENOENT') throw fault('invalid-staging-path', `${name}: ${error.message}`);
      }
    }
    // Partial/unknown writes are preserved. Recovery may read an exact, fully validated duplicate
    // of the committed bytes, but never promotes a different pending state to the committed state.
    if (names.some(name => name !== 'state.json' && name !== 'transaction'
      && !pending.includes(name) && !inactive.includes(name))
      || (pending.length && !allowRedundantPending)) {
      throw fault('incomplete-state-write', `Unknown or unfinished state files in ${root}.`);
    }
    let ownerRecord = null;
    if (!locked && names.includes('transaction')) {
      ownerRecord = await readOwner();
      if (!ownerRecord) {
        throw fault('state-snapshot-changing', 'Transaction ownership could not be sampled consistently.');
      }
      if (!names.includes('state.json')) {
        throw fault('state-transaction-active', 'The first state transaction has no committed snapshot yet.');
      }
    }
    if (!names.includes('state.json')) {
      if (pending.length) throw fault('incomplete-state-write', 'Pending state has no committed counterpart.');
      return { version: 1, repositoryKey, nextSequence: 0, entries: [] };
    }
    let handle;
    try {
      handle = await open(statePath, 'r');
      if ((await handle.stat()).size > MAX_STATE_BYTES) throw new Error('State exceeds the 8 MiB bound.');
      const bytes = await handle.readFile();
      if (bytes.length > MAX_STATE_BYTES) throw new Error('State exceeds the 8 MiB bound.');
      const state = validateState(JSON.parse(new TextDecoder('utf-8', { fatal: true }).decode(bytes)), repositoryKey);
      for (const name of pending) {
        const pendingBytes = await readFile(join(root, name));
        if (!pendingBytes.equals(bytes)) throw new Error(`Uncommitted or partial state differs: ${name}`);
      }
      if (!locked) {
        // The file handle reads one atomically committed generation. Check the surrounding
        // namespace again: pending bytes or an unconfirmed owner must never become a clean read.
        const snapshotNames = items => items.filter(name => !/^(candidate|retired)-[a-f\d-]+$/.test(name)).sort();
        const afterNames = await readdir(root);
        if (JSON.stringify(snapshotNames(names)) !== JSON.stringify(snapshotNames(afterNames))
          || (ownerRecord && !await sameOwnerRecord(ownerRecord))) {
          throw fault('state-snapshot-changing', 'The committed snapshot namespace changed during the read.');
        }
      }
      return state;
    } catch (error) {
      if (error.status) throw error;
      throw fault('invalid-state', `Cannot validate ${statePath}: ${error.message}`,
        'Preserve the original bytes and inspect recorded owners and process identities; no force-unlock is provided.');
    } finally {
      await handle?.close();
    }
  }

  async function readSnapshot() {
    const deadline = Date.now() + SNAPSHOT_TIMEOUT_MS;
    for (;;) {
      try {
        return await read();
      } catch (error) {
        // Normal write/rename/retirement windows can look partial to a lock-free reader.
        // Retry without creating a lock or repairing anything. A persistent partial/corrupt
        // state still returns its actual validation error, never a fabricated empty snapshot.
        if (Date.now() >= deadline) throw error;
        await sleep(10);
      }
    }
  }

  async function write(state) {
    validateState(state, repositoryKey);
    const bytes = JSON.stringify(state, null, 2);
    if (Buffer.byteLength(bytes) > MAX_STATE_BYTES) throw fault('state-limit', 'State exceeds the 8 MiB bound.');
    try {
      if (await readFile(statePath, 'utf8') === bytes) return;
    } catch (error) {
      if (error.code !== 'ENOENT') throw error;
    }
    const temporary = join(root, `state-${randomUUID()}.pending`);
    const handle = await open(temporary, 'wx');
    try {
      await handle.writeFile(bytes);
      await handle.sync();
    } finally {
      await handle.close();
    }
    const deadline = Date.now() + 1000;
    for (;;) {
      try {
        await rename(temporary, statePath);
        return;
      } catch (error) {
        if (!['EPERM', 'EACCES', 'EBUSY'].includes(error.code) || Date.now() >= deadline) throw error;
        await sleep(25);
      }
    }
  }

  function recoveryBlock(code, message) {
    return new LockError('blocked', code, message,
      'Retain the recorded state; inspect process start identities and wait for the owned bundle to end.');
  }

  async function recoverEndedTransaction() {
    const record = await readOwner();
    if (!record) return;
    const { ownerPath, ownerBytes, owner } = record;
    const observation = await observe();
    // Observation may outlive a normally completed transaction. Do not apply facts about that
    // retired owner to the new generation's state; the acquisition loop will start fresh.
    if (!await sameOwnerRecord(record)) return;
    const currentOwner = observation.processes.find(item => item.pid === owner.owner.pid);
    if (currentOwner) {
      if (!sameIdentity(owner.owner, currentOwner)) {
        throw recoveryBlock('process-identity-mismatch', 'Transaction owner PID has another start identity.');
      }
      return;
    }
    const state = await read({ locked: true, allowRedundantPending: true });
    if (findIdentityConflict(state, observation) !== null) {
      throw recoveryBlock('process-identity-mismatch', 'Recorded execution PID has another start identity.');
    }
    for (const entry of state.entries.filter(item => item.phase !== 'waiting')) {
      if (entry.phase === 'reserved' && !entry.spawnAttempted) continue;
      const bundle = refreshBundle(entry, observation);
      if (bundle.uncertain) throw recoveryBlock('unconfirmed-execution', 'A spawn or process identity is unconfirmed.');
      if (bundle.live.length) throw recoveryBlock(`${entry.kind}-busy`, 'The lost owner still has a live bundle.');
    }
    const pending = (await readdir(root)).filter(name => /^state-[a-f\d-]+\.pending$/.test(name));
    const preserved = [];
    for (const name of pending) preserved.push({ name, bytes: await readFile(join(root, name), 'utf8') });
    await evidence?.event('transaction-recovery-proof', { owner, observedAt: observation.observedAt,
      redundantPending: preserved, reason: 'owner absent, no PID reuse, all recorded bundles ended' });

    // The unique owner's filename is the claim: only one contender can unlink THIS generation.
    // A stale contender never rmdirs a newer owner's directory after an ABA replacement.
    try {
      if (!(await readFile(ownerPath)).equals(ownerBytes)) return;
      await unlink(ownerPath);
    } catch (error) {
      if (error.code === 'ENOENT') return;
      throw error;
    }
    // The permanent barrier keeps the directory NONEMPTY: on POSIX rename can replace an empty
    // directory. No contender may publish a new generation while recovery is finishing.
    for (const { name, bytes } of preserved) {
      if (await readFile(join(root, name), 'utf8') !== bytes) {
        throw fault('pending-state-changed', `Pending bytes changed during recovery: ${name}`);
      }
      await unlink(join(root, name));
    }
    const retired = join(root, `retired-${owner.token}`);
    await retireOwned(owner.token, retired, true);
    await unlink(join(retired, 'barrier'));
    await rmdir(retired);
  }

  async function transaction(operation) {
    if (runnerIdentity === null) {
      const observation = await observe();
      const current = observation.processes.find(item => item.pid === process.pid);
      if (!current?.startedAt) throw fault('unknown-runner-identity', 'Runner start identity is unavailable.');
      runnerIdentity = identity(current);
    }
    await mkdir(root, { recursive: true });
    const token = randomUUID();
    const candidate = join(root, `candidate-${token}`);
    const ownerName = `owner-${token}.json`;
    let published = false;
    await mkdir(candidate);
    try {
      // Publish a complete, immutable owner in one rename. Death before publication leaves only an
      // inactive candidate; death after it always leaves the PID/start identity needed for review.
      for (const [name, bytes] of [['barrier', 'execution-lock-v1'],
        [ownerName, JSON.stringify({ token, owner: runnerIdentity, at: new Date().toISOString() })]]) {
        const handle = await open(join(candidate, name), 'wx');
        try {
          await handle.writeFile(bytes);
          await handle.sync();
        } finally {
          await handle.close();
        }
      }
      const deadline = Date.now() + MUTEX_TIMEOUT_MS;
      for (;;) {
        try {
          // Existing empty/unknown directories are not overwritten, even on POSIX where rename
          // could replace them. Concurrent legitimate publications always contain the barrier.
          let present = false;
          try {
            await stat(mutex);
            present = true;
          } catch (error) {
            if (error.code !== 'ENOENT') throw error;
          }
          if (present) {
            await recoverEndedTransaction();
            if (Date.now() >= deadline) throw fault('state-lock-timeout', `Transaction remains occupied: ${mutex}`);
            await sleep(20);
            continue;
          }
          await rename(candidate, mutex);
          published = true;
          await evidence?.event('transaction-acquired', { token, owner: runnerIdentity,
            ownerPath: join(mutex, ownerName) });
          break;
        } catch (error) {
          if (!['EEXIST', 'ENOTEMPTY', 'EPERM', 'EACCES', 'EBUSY'].includes(error.code)) throw error;
          await recoverEndedTransaction();
          if (Date.now() >= deadline) {
            throw fault('state-lock-timeout', `Could not acquire ${mutex} within ${MUTEX_TIMEOUT_MS} ms.`,
              'Preserve the transaction and inspect the owner/OS sharing error; no force-unlock is provided.');
          }
          await sleep(20);
        }
      }
      return await operation({ read: () => read({ locked: true }), write, observe });
    } finally {
      let cleanup = candidate;
      if (published) {
        const owner = JSON.parse(await readFile(join(mutex, ownerName), 'utf8'));
        if (owner.token !== token) throw fault('state-lock-owner-changed', `Owner changed at ${mutex}.`);
        cleanup = join(root, `retired-${token}`);
        // Retire atomically, then delete only our private directory. A new owner can never be
        // mistaken for the old empty directory during release.
        await retireOwned(token, cleanup);
      }
      for (const name of [ownerName, 'barrier']) {
        try {
          await unlink(join(cleanup, name));
        } catch (error) {
          if (error.code !== 'ENOENT') throw error;
        }
      }
      await rmdir(cleanup);
    }
  }

  return { read: readSnapshot, transaction };
}

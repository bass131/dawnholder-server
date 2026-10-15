import { fault } from './contract.mjs';
import { identity, processKind, sameIdentity } from './observation.mjs';

// Identity mismatch is a separate veto, checked before changing even a waiting entry.
export function findIdentityConflict(state, observation) {
  const byPid = new Map(observation.processes.map(item => [item.pid, item]));
  for (const entry of state.entries) {
    for (const recorded of [entry.owner, entry.root, ...entry.members].filter(Boolean)) {
      const current = byPid.get(recorded.pid);
      if (current && recorded.startedAt && !sameIdentity(recorded, current)) return recorded.pid;
    }
  }
  return null;
}

// A parent PID without its generation is not an ancestry proof. Both bundle discovery and
// external-root counting use this edge rule so an older orphan cannot disappear between them.
function parentRelation(child, parent) {
  if (!parent || child.parentPid !== parent.pid) return false;
  if (!parent.startedAt || !child.startedAt) return null;
  return Date.parse(child.startedAt) >= Date.parse(parent.startedAt);
}

export function refreshBundle(entry, observation, ownChild = null) {
  const byPid = new Map(observation.processes.map(item => [item.pid, item]));
  const root = entry.root;
  if (!root) return { live: [], uncertain: entry.phase !== 'waiting' };
  const currentRoot = byPid.get(root.pid);
  if (!root.startedAt && currentRoot) {
    // Only the still-live ChildProcess receipt can authenticate a root not yet observed.
    // A PID found after that handle exits could already be a different incarnation.
    if (!ownChild || ownChild.exited || currentRoot.parentPid !== entry.owner.pid || !currentRoot.startedAt) {
      return { live: [], uncertain: true };
    }
    root.startedAt = currentRoot.startedAt;
    root.parentPid = currentRoot.parentPid;
  }
  const known = new Map([root, ...entry.members].map(item => [item.pid, item]));
  let uncertainAncestry = false;
  let changed = true;
  while (changed) {
    changed = false;
    for (const processInfo of observation.processes) {
      if (known.has(processInfo.pid) || processInfo.pid === entry.owner.pid) continue;
      const parent = known.get(processInfo.parentPid);
      if (!parent) continue;
      const relation = parentRelation(processInfo, parent);
      if (relation === false) continue;
      if (!processInfo.startedAt) {
        throw fault('unknown-child-identity', `Related child PID ${processInfo.pid} has no start identity.`);
      }
      const currentParent = byPid.get(parent.pid);
      if (relation === null || !currentParent || !sameIdentity(parent, currentParent)) {
        uncertainAncestry = true;
        continue;
      }
      // Parent PID is used only within the recorded lineage, never to merge same-name roots.
      const child = identity(processInfo);
      entry.members.push(child);
      known.set(child.pid, child);
      changed = true;
    }
  }
  const live = [...known.values()].filter(item => {
    const current = byPid.get(item.pid);
    return current && item.startedAt && sameIdentity(item, current);
  });
  const rootUnproven = !root.startedAt && !entry.rootExited;
  const receiptStillLive = ownChild && !ownChild.exited;
  return { live, uncertain: uncertainAncestry || rootUnproven || Boolean(receiptStillLive && !currentRoot) };
}

export function reconcile(state, observation, own = null) {
  const remaining = [];
  for (const entry of state.entries) {
    const ownerPresent = observation.processes.some(item => sameIdentity(entry.owner, item));
    if (entry.phase === 'waiting') {
      if (ownerPresent) remaining.push(entry);
      continue;
    }
    if (entry.phase === 'reserved' && !entry.spawnAttempted && !ownerPresent) continue;
    const child = own?.id === entry.id ? own.child : null;
    if (child?.exited) entry.rootExited = true;
    const bundle = refreshBundle(entry, observation, child);
    const mayFinish = !ownerPresent || entry.ownerFinished || (child?.exited === true);
    if (mayFinish && !bundle.uncertain && bundle.live.length === 0) continue;
    remaining.push(entry);
  }
  state.entries = remaining;
}

function priority(entry) {
  return (entry.kind === 'unity' && entry.part === 'Content') || (entry.kind === 'wsl' && entry.part === 'Core')
    ? 0 : 1;
}

function externalRoots(observation, active) {
  const owned = active.flatMap(entry => [entry.root, ...entry.members].filter(Boolean));
  const byPid = new Map(observation.processes.map(item => [item.pid, item]));
  return observation.processes.filter(item => {
    if (!processKind(item) || owned.some(recorded => recorded.startedAt && sameIdentity(recorded, item))) return false;
    const visited = new Set([item.pid]);
    let child = item;
    let parent = byPid.get(item.parentPid);
    while (parent && !visited.has(parent.pid)) {
      if (parentRelation(child, parent) !== true) break;
      if (processKind(parent) === processKind(item)) return false;
      visited.add(parent.pid);
      child = parent;
      parent = byPid.get(parent.parentPid);
    }
    return true;
  });
}

export function admissionReason(state, candidate, observation) {
  const active = state.entries.filter(entry => entry.phase !== 'waiting' && entry.id !== candidate.id);
  const external = externalRoots(observation, active);
  const unity = active.filter(entry => entry.kind === 'unity');
  const wsl = active.filter(entry => entry.kind === 'wsl');
  const externalUnity = external.filter(item => processKind(item) === 'unity');
  const externalWsl = external.filter(item => processKind(item) === 'wsl');
  if (externalWsl.length) return 'unknown-wsl-owner';
  if (candidate.kind === 'unity' && (unity.length || externalUnity.length)) return 'unity-busy';
  if (candidate.kind === 'wsl' && wsl.length) return 'wsl-busy';
  if (active.some(entry => entry.phase === 'uncertain' || entry.phase === 'reserved'
    || (entry.root && !entry.root.startedAt && !entry.rootExited))) return 'unconfirmed-execution';
  const opposite = candidate.kind === 'unity' ? wsl : [...unity, ...externalUnity];
  if (opposite.length) {
    const coreWsl = candidate.kind === 'wsl' ? candidate.part === 'Core' : wsl.every(entry => entry.part === 'Core');
    if (!coreWsl) return 'parallel-part-not-authorized';
    if (unity.length + externalUnity.length > 1) return 'multiple-unity-roots';
    if (observation.memory.physicalAvailableBytes < 6 * 1024 ** 3
      || observation.memory.commitAvailableBytes < 4 * 1024 ** 3) return 'parallel-memory';
  }
  const waiting = state.entries.filter(entry => entry.phase === 'waiting' && entry.kind === candidate.kind);
  waiting.sort((left, right) => priority(left) - priority(right) || left.sequence - right.sequence
    || left.id.localeCompare(right.id));
  if (waiting.length && waiting[0].id !== candidate.id) return 'waiting-priority';
  return null;
}

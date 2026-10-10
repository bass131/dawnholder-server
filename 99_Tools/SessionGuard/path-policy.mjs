import { posix } from 'node:path';

export function expandVariables(text, variables) {
  let unresolved = false;
  const value = text.replace(/\$(?:\{([A-Za-z_]\w*)\}|([A-Za-z_]\w*)|([^\s]*))/g, (source, braced, bare) => {
    const name = braced ?? bare;
    const replacement = name && Object.hasOwn(variables, name) ? variables[name] : undefined;
    if (replacement === undefined) {
      unresolved = true;
      return source;
    }
    if (typeof replacement === 'object') {
      unresolved ||= replacement.unresolved;
      return replacement.value;
    }
    return String(replacement);
  });
  return { value, unresolved };
}

function slashPath(value) {
  return value.replace(/\\/g, '/').replace(/^\/([A-Za-z])(?=\/|$)/, '$1:');
}

export function normalizePath(value, cwd) {
  let path = slashPath(value);
  if (/^[A-Za-z]:[^/]/.test(path)) return null;
  if (!/^(?:[A-Za-z]:\/|\/)/.test(path)) path = `${slashPath(cwd)}/${path}`;
  const drive = /^([A-Za-z]:)(\/.*)$/.exec(path);
  path = drive ? drive[1].toLowerCase() + posix.normalize(drive[2]).toLowerCase() : posix.normalize(path);
  return path.replace(/\/$/, '') || '/';
}

export function isWithin(path, root) {
  return path !== null && root !== null && (path === root || path.startsWith(root === '/' ? root : `${root}/`));
}

export function isNonFile(value) {
  return /^(?:\/dev\/(?:null|stdout|stderr|fd\/[^/]+)|nul)$/i.test(value);
}

export function isNullOutput(value) {
  return /^(?:\/dev\/null|nul)$/i.test(value);
}

export function resolveDestination(destination, cwd) {
  // An unresolved variable has no trustworthy root, but still counts as a write for memo order.
  return destination.unresolved ? null : normalizePath(destination.value, cwd);
}

export function isMemoPath(path, memoRoots) {
  return path !== null && /context[^/]*\.md$/i.test(posix.basename(path)) &&
    memoRoots.some(root => isWithin(path, root));
}

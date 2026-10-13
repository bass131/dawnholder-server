import { isAbsolute, resolve } from 'node:path';

import { evaluateGh } from './gh-policy.mjs';
import { evaluateGitPush } from './git-push-policy.mjs';
import { commandName, readShell } from './shell-command.mjs';

const controlPrefixes = new Set(['if', 'then', 'elif', 'else', 'do', 'while', 'until', '!', 'try', 'catch', 'finally']);
const literalEvaluators = new Set(['eval', 'invoke-expression', 'iex']);
const shells = new Set(['bash', 'sh', 'dash', 'zsh', 'pwsh', 'powershell', 'cmd']);
const conditionalStarts = new Set(['if', 'while', 'until', 'for', 'foreach', 'switch']);
const copyState = state => ({ directories: [...state.directories] });
function joinDirectories(groups) {
  const directories = [...new Set(groups.flat())];
  if (directories.length > 64) throw new Error('more than 64 possible execution directories');
  return directories;
}

// Each sequence owns one chain. && continues success paths and || continues failure paths;
// skipped outcomes rejoin at ;/newline. Pipes also retain the parent-shell location.
// No external command's status is guessed (including false/test). null means an unknown cwd.
function createLocationChain(state) {
  let skippedSuccess = [];
  let skippedFailure = [];
  let pipeline = [];
  let commandEntry = [...state.directories];
  let outcomes = { success: [...state.directories], failure: [...state.directories] };
  let conditional = false;
  return {
    start() {
      commandEntry = [...state.directories];
      state.failureDirectories = null;
    },
    complete() {
      outcomes = {
        success: joinDirectories([skippedSuccess, state.directories]),
        failure: joinDirectories([skippedFailure, state.failureDirectories ?? state.directories]),
      };
    },
    separate(operator) {
      if (operator === '&&') {
        skippedSuccess = [];
        skippedFailure = outcomes.failure;
        state.directories = [...outcomes.success];
        conditional = true;
        pipeline = [];
      } else if (operator === '||') {
        skippedSuccess = outcomes.success;
        skippedFailure = [];
        state.directories = [...outcomes.failure];
        conditional = true;
        pipeline = [];
      } else if (operator === '|' || operator === '|&') {
        pipeline = joinDirectories([pipeline, commandEntry, state.directories]);
        state.directories = [...pipeline];
      } else {
        // A plain explicit cd defines the next location. Within a conditional chain its
        // failure location is retained separately, so a failed cd does not run an && push.
        state.directories = conditional ? joinDirectories([outcomes.success, outcomes.failure, pipeline]) :
          joinDirectories([state.directories, pipeline]);
        state.failureDirectories = outcomes.failure;
        skippedSuccess = [];
        skippedFailure = [];
        pipeline = [];
        conditional = false;
      }
    },
  };
}

function groupEnd(tokens, start) {
  let level = 1;
  for (let index = start + 1; index < tokens.length; index++) {
    if (tokens[index].kind === 'open') level++;
    if (tokens[index].kind === 'close' && --level === 0) return index;
  }
  throw new Error('unclosed shell group');
}

function branchKeyword(words, state, branches) {
  const keyword = words[0]?.quoted ? null : words[0]?.value.toLowerCase();
  if (conditionalStarts.has(keyword)) {
    branches.push({ entry: [...state.directories], exits: [], hasElse: false,
      loop: keyword !== 'if', braced: false, pendingBody: true });
  } else if (['else', 'elif', 'elseif'].includes(keyword) && branches.length) {
    const branch = branches.at(-1);
    if (!branch.braced) branch.exits.push([...state.directories]);
    state.directories = [...branch.entry];
    branch.hasElse = keyword === 'else';
    branch.pendingBody = true;
    branch.braced = false;
  } else if (['fi', 'done'].includes(keyword) && branches.length) {
    const branch = branches.pop();
    if (!branch.braced) branch.exits.push([...state.directories]);
    state.directories = branchLocations(branch);
  }
}

function branchLocations(branch) {
  return joinDirectories([...branch.exits, ...(!branch.hasElse || branch.loop ? [branch.entry] : [])]);
}

function inspectTry(tokens, start, state, budget, depth) {
  const checkpoints = [[...state.directories]];
  const end = groupEnd(tokens, start + 1);
  const attempted = copyState(state);
  let reason = visit(tokens.slice(start + 2, end), attempted, budget, depth + 1,
    directories => checkpoints.push([...directories]));
  if (reason) return { reason, end };
  const exits = [[...attempted.directories]];
  let unhandled = [...checkpoints];
  let index = end + 1;
  let consumed = end;
  while (index < tokens.length) {
    while (tokens[index]?.kind === 'operator' && tokens[index].value === '\n') index++;
    const keyword = tokens[index]?.kind === 'word' && !tokens[index].quoted ?
      tokens[index].value.toLowerCase() : null;
    if (keyword !== 'catch' && keyword !== 'finally') break;
    let open = index + 1;
    // Optional PowerShell exception type belongs to catch, never to its executable body.
    while (tokens[open]?.kind === 'word') open++;
    if (tokens[open]?.kind !== 'open' || tokens[open].value !== '{') break;
    const close = groupEnd(tokens, open);
    // A catch-all consumes the try's exceptional exits. Typed catches may leave exceptions
    // unhandled; finally must still inspect those intermediate locations.
    const catchesAll = keyword === 'catch' && open === index + 1;
    const child = { directories: keyword === 'catch' ? joinDirectories(checkpoints) :
      joinDirectories([...exits, ...unhandled]) };
    const catchCheckpoints = [];
    reason = visit(tokens.slice(open + 1, close), child, budget, depth + 1,
      directories => { if (keyword === 'catch') catchCheckpoints.push([...directories]); });
    if (reason) return { reason, end: close };
    consumed = close;
    if (keyword === 'finally') {
      state.directories = child.directories;
      return { reason: null, end: consumed };
    }
    unhandled = [...(catchesAll ? [] : unhandled), ...catchCheckpoints];
    exits.push([...child.directories]);
    index = close + 1;
  }
  state.directories = joinDirectories(exits);
  return { reason: null, end: consumed };
}

function parameterPrefix(value, name, minimum) {
  return value.length >= minimum.length && value.startsWith(minimum) && name.startsWith(value);
}

function commandOption(words, shell) {
  const powerShell = shell === 'pwsh' || shell === 'powershell';
  // Host parameters consume their values before another option can be considered. In particular
  // -exec/-executionpolicy and -noninteractive are never POSIX combined flags containing "c".
  const valueParameters = [
    ['executionpolicy', 'ex'], ['inputformat', shell === 'powershell' ? 'in' : 'inp'], ['outputformat', 'o'],
    ['windowstyle', 'w'], ['workingdirectory', 'wo'], ['configurationname', 'config'],
    ['configurationfile', 'configurationf'], ['settingsfile', 'settings'], ['version', 'v'],
    ['custompipename', 'custom'], ['encodedarguments', 'encodeda'], ['psconsolefile', 'ps'],
  ];
  for (let index = 1; index < words.length; index++) {
    const argument = words[index].value;
    if (shell === 'cmd') {
      if (/^\/c$/i.test(argument)) return index;
      if (/^\/k$/i.test(argument) || !argument.startsWith('/')) return -1;
      continue;
    }
    if (!argument.startsWith('-')) return -1; // A script path ends host-option parsing.
    if (!powerShell) {
      if (/^-[a-z]*c[a-z]*$/.test(argument)) return index;
      if (['--rcfile', '--init-file', '-o', '-O'].includes(argument)) index++;
      continue;
    }
    const name = argument.slice(1).toLowerCase();
    if (parameterPrefix(name, 'command', 'c')) return index;
    if (parameterPrefix(name, 'encodedcommand', 'e') || name === 'ec' ||
        parameterPrefix(name, 'file', 'f')) return -1;
    if (['ep', 'if', 'of', 'wd', 'ea'].includes(name) ||
        valueParameters.some(([full, minimum]) => parameterPrefix(name, full, minimum))) index++;
  }
  return -1;
}

// The traversal owns execution location. Child shells/substitutions get a copy; literal
// eval and explicit cd update the current scope. No command from this tree is executed.
export function evaluateCommand(command, cwd) {
  const budget = { deadline: Date.now() + 12000, commands: 0 };
  return visit(readShell(command), { directories: [cwd] }, budget, 0);
}

function visit(tokens, state, budget, depth, observe = () => {}) {
  if (depth > 32) throw new Error('shell wrapper nesting exceeds 32 levels');
  let current = [];
  // All explicit branches are inspected. Joining their possible cwd values prevents an
  // else branch from erasing a protected push destination reached by an earlier branch.
  const branches = [];
  const chain = createLocationChain(state);
  const finish = () => {
    const words = current;
    current = [];
    if (!words.length) return null;
    chain.start();
    branchKeyword(words, state, branches);
    const reason = evaluateWords(words, state, budget, depth);
    chain.complete();
    observe(state.directories);
    return reason;
  };
  for (let index = 0; index < tokens.length; index++) {
    const token = tokens[index];
    if (token.kind === 'word') {
      if (!current.length && !token.quoted && token.value.toLowerCase() === 'try' &&
          tokens[index + 1]?.kind === 'open' && tokens[index + 1].value === '{') {
        chain.start();
        const result = inspectTry(tokens, index, state, budget, depth);
        if (result.reason) return result.reason;
        index = result.end;
        chain.complete();
        observe(state.directories);
        continue;
      }
      for (const substitution of token.substitutions) {
        const reason = visit(substitution, copyState(state), budget, depth + 1);
        if (reason) return reason;
      }
      current.push(token);
    } else if (token.kind === 'open') {
      const reason = finish();
      if (reason) return reason;
      chain.start();
      const start = index + 1;
      index = groupEnd(tokens, index);
      const branch = branches.at(-1);
      const branchBody = token.value === '{' && branch?.pendingBody;
      const child = token.value === '(' || branchBody ? copyState(state) : state;
      const nestedReason = visit(tokens.slice(start, index), child, budget, depth + 1, observe);
      if (nestedReason) return nestedReason;
      if (branchBody) {
        branch.exits.push([...child.directories]);
        branch.pendingBody = false;
        branch.braced = true;
        state.directories = branchLocations(branch);
      }
      chain.complete();
    } else if (token.kind === 'redirect') {
      const target = tokens[++index];
      if (target?.kind !== 'word') throw new Error('missing redirection target');
      for (const substitution of target.substitutions) {
        const reason = visit(substitution, copyState(state), budget, depth + 1);
        if (reason) return reason;
      }
    } else {
      const reason = finish();
      if (reason) return reason;
      chain.separate(token.value);
      observe(state.directories);
    }
  }
  const reason = finish();
  chain.separate(';');
  return reason;
}

function evaluateWords(words, state, budget, depth) {
  if (++budget.commands > 2048 || Date.now() > budget.deadline) {
    throw new Error('explicit command inspection budget exceeded');
  }
  while (words.length && controlPrefixes.has(words[0].value.toLowerCase())) words = words.slice(1);
  while (words.length && /^[A-Za-z_]\w*=/.test(words[0].value)) words = words.slice(1);
  if (!words.length) return null;
  let name = commandName(words[0].value);
  if (name === 'env') {
    let index = 1;
    while (index < words.length) {
      const value = words[index].value;
      if (value === '--') {
        index++;
        break;
      }
      if (value === '-u' || value === '--unset') index += 2;
      else if (value.startsWith('-') || /^[A-Za-z_]\w*=/.test(value)) index++;
      else break;
    }
    return evaluateWords(words.slice(index), state, budget, depth);
  }
  if (name === 'command' || name === 'exec') {
    let index = 1;
    if (name === 'command' && words.slice(1).some(word => ['-v', '-V'].includes(word.value))) return null;
    while (words[index]?.value.startsWith('-')) index++;
    return evaluateWords(words.slice(index), state, budget, depth);
  }
  if (name === 'cd' || name === 'set-location' || name === 'chdir' || name === 'sl') {
    const path = words.slice(1).find(word => !['--', '/d', '-path', '-literalpath'].includes(word.value.toLowerCase()));
    // Unknown location only matters at a push. Reads and explicit gh policy remain inspectable.
    state.failureDirectories = [...state.directories];
    state.directories = !path?.literal ? [null] : state.directories.map(cwd =>
      isAbsolute(path.value) ? resolve(path.value) : cwd === null ? null : resolve(cwd, path.value));
    return null;
  }
  if (literalEvaluators.has(name)) {
    const body = name !== 'eval' && /^-command$/i.test(words[1]?.value ?? '') ? words.slice(2) : words.slice(1);
    if (body.length === 1 && body[0].literal) return visit(readShell(body[0].value), state, budget, depth + 1);
    return null;
  }
  if (shells.has(name)) {
    const option = commandOption(words, name);
    if (option < 0) return null;
    const body = words.slice(option + 1);
    if (!body.length) return null;
    if (body.length === 1) {
      return visit(readShell(body[0].value), copyState(state), budget, depth + 1);
    }
    // cmd /c and PowerShell -Command also accept an unquoted command tail.
    if (['cmd', 'pwsh', 'powershell'].includes(name)) {
      return evaluateWords(body, copyState(state), budget, depth + 1);
    }
    return visit(readShell(body[0].value), copyState(state), budget, depth + 1);
  }
  const argv = words.slice(1).map(word => word.value);
  if (name === 'gh') return evaluateGh(argv);
  if (name === 'git') {
    for (const cwd of state.directories) {
      const reason = evaluateGitPush(argv, cwd, budget);
      if (reason) return reason;
    }
  }
  return null;
}

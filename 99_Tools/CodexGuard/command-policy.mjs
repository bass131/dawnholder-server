import { resolve } from 'node:path';

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

// The traversal owns execution location. Child shells/substitutions get a copy; literal
// eval and explicit cd update the current scope. No command from this tree is executed.
export function evaluateCommand(command, cwd) {
  const budget = { deadline: Date.now() + 12000, commands: 0 };
  return visit(readShell(command), { directories: [cwd] }, budget, 0);
}

function visit(tokens, state, budget, depth) {
  if (depth > 32) throw new Error('shell wrapper nesting exceeds 32 levels');
  let current = [];
  // All explicit branches are inspected. Joining their possible cwd values prevents an
  // else branch from erasing a protected push destination reached by an earlier branch.
  const branches = [];
  const finish = () => {
    const words = current;
    current = [];
    const keyword = words[0]?.value.toLowerCase();
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
      state.directories = joinDirectories([
        ...branch.exits, ...(!branch.hasElse || branch.loop ? [branch.entry] : []),
      ]);
    }
    return evaluateWords(words, state, budget, depth);
  };
  for (let index = 0; index < tokens.length; index++) {
    const token = tokens[index];
    if (token.kind === 'word') {
      for (const substitution of token.substitutions) {
        const reason = visit(substitution, copyState(state), budget, depth + 1);
        if (reason) return reason;
      }
      current.push(token);
    } else if (token.kind === 'open') {
      const reason = finish();
      if (reason) return reason;
      let level = 1;
      const start = ++index;
      for (; index < tokens.length; index++) {
        if (tokens[index].kind === 'open') level++;
        if (tokens[index].kind === 'close' && --level === 0) break;
      }
      if (level !== 0) throw new Error('unclosed shell group');
      const branch = branches.at(-1);
      const branchBody = token.value === '{' && branch?.pendingBody;
      const child = token.value === '(' || branchBody ? copyState(state) : state;
      const nestedReason = visit(tokens.slice(start, index), child, budget, depth + 1);
      if (nestedReason) return nestedReason;
      if (branchBody) {
        branch.exits.push([...child.directories]);
        branch.pendingBody = false;
        branch.braced = true;
        state.directories = joinDirectories([
          ...branch.exits, ...(!branch.hasElse || branch.loop ? [branch.entry] : []),
        ]);
      }
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
    }
  }
  return finish();
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
    const path = words.slice(1).find(word => !['--', '/d', '-Path', '-LiteralPath'].includes(word.value));
    if (!path?.literal) throw new Error('execution directory is not an explicit path');
    state.directories = state.directories.map(cwd => resolve(cwd, path.value));
    return null;
  }
  if (literalEvaluators.has(name)) {
    if (words.length === 2 && words[1].literal) return visit(readShell(words[1].value), state, budget, depth + 1);
    return null;
  }
  if (shells.has(name)) {
    const option = words.findIndex((word, index) => index > 0 &&
      (name === 'cmd' ? /^\/c$/i.test(word.value) :
        /^(?:-Command|-c)$/i.test(word.value) || /^-[a-z]*c[a-z]*$/.test(word.value)));
    if (option < 0 || words.slice(1, option).some(word => /^-(?:EncodedCommand|e|enc)$/i.test(word.value))) return null;
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

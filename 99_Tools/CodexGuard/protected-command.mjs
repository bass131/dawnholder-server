import { apiValues, optionAt, commandOptionLength, candidateApiEndpoint, hasMergeMutation } from './gh-policy.mjs';
import { inspectionSlice } from './inspection-budget.mjs';

const gitValues = new Set(['-C', '-c', '--git-dir', '--work-tree', '--namespace', '--config-env']);
const bodyOptions = new Set(['-f', '-F', '--raw-field', '--field', '--input']);
const queryOptions = new Set(['-f', '-F', '--raw-field', '--field']);

// This view exposes literal command text inside wrapper quotes and malformed syntax.
// It never calls the shell lexer or reparses a suffix. Quote pairs of each kind are
// disjoint: their decoded strings overlap the other kind at most twice (<= 2n).
function candidateTokens(source, work) {
  work.call('candidateLexer');
  const tokens = [];
  const quotes = new Map();
  const groups = [];
  const placeholder = /\{[A-Za-z_][A-Za-z0-9_]*\}(?=\/|[\s"'\x60;|&)]|$)/y;
  let start = -1;
  let argument = null;
  let quote = null;
  let literalSkip = 0;
  let braceDepth = 0;
  let embeddedBraces = 0;
  function flush(end) {
    if (start < 0) return;
    const value = inspectionSlice(source, start, end, work, 'candidateSubstringCharacters');
    work.record('candidateWordTokens');
    tokens.push({ value, start, end, boundary: false });
    start = -1;
  }
  function finishArgument(end) {
    if (!argument) return;
    flush(end);
    argument.value = argument.parts.join('');
    work.record('candidateArgumentAssemblyCharacters', argument.value.length);
    delete argument.parts;
    argument.end = end;
    argument.argumentEnd = end;
    argument.next = tokens.length;
    argument = null;
  }
  // One input traversal emits both visible atoms and their containing literal argv
  // slots. Slots own quoting/concatenation, including zero-length values; atoms still
  // expose explicit commands inside wrapper text. Summary transitions consume slots,
  // never rebuild a remaining argv. No shell parse or suffix lexer is called here.
  function literalCharacter(offset) {
    if (offset < literalSkip) return;
    const char = source[offset];
    if (!quote && /[\s;&|()<>]/.test(char)) {
      finishArgument(offset);
      return;
    }
    placeholder.lastIndex = offset;
    const literalPlaceholder = char === '{' ? placeholder.exec(source) : null;
    if (!quote && ((char === '{' && !literalPlaceholder) ||
        (char === '}' && braceDepth > 0 && embeddedBraces === 0))) {
      finishArgument(offset);
      braceDepth += char === '{' ? 1 : -1;
      return;
    }
    if (!quote && char === '{' && literalPlaceholder) embeddedBraces++;
    if (!quote && char === '}' && embeddedBraces > 0) embeddedBraces--;
    if (!argument && char === '#') {
      const end = source.indexOf('\n', offset);
      literalSkip = end < 0 ? source.length : end;
      return;
    }
    if (!argument) {
      argument = { value: '', start: offset, end: offset, boundary: false,
        argumentStart: offset, argumentGroup: true, rootArgument: true, parts: [] };
      tokens.push(argument);
      work.record('candidateWordTokens');
    }
    if (char === quote) {
      if (quote === "'" && source[offset + 1] === "'") {
        argument.parts.push("'");
        literalSkip = offset + 2;
      } else {
        quote = null;
      }
      return;
    }
    if (!quote && (char === '"' || char === "'")) {
      quote = char;
      return;
    }
    if (quote !== "'" && (char === '\\' || char === '\x60')) {
      const next = source[offset + 1];
      if (next === '\n' || (next === '\r' && source[offset + 2] === '\n')) {
        literalSkip = offset + (next === '\r' ? 3 : 2);
        return;
      }
      if (next !== undefined && /[\s"'`$\\;&|()]/.test(next)) {
        argument.parts.push(next);
        literalSkip = offset + 2;
        return;
      }
    }
    argument.parts.push(char);
  }
  for (let offset = 0; offset < source.length; offset++) {
    work.record('candidateCharacters');
    literalCharacter(offset);
    const char = source[offset];
    if ((char === '\\' || char === '\x60') && /[\s"'\x60$\\;&|()]/.test(source[offset + 1] ?? '')) {
      if (start < 0) start = offset;
      literalCharacter(offset + 1);
      offset++;
      work.record('candidateCharacters');
      continue;
    }
    if (char === '"' || char === "'") {
      flush(offset);
      const open = quotes.get(char);
      if (open) {
        if (open.first === tokens.length) {
          tokens.push({ value: '', start: open.start, end: offset + 1, boundary: false });
          work.record('candidateWordTokens');
        }
        groups.push({ ...open, end: offset, after: tokens.length });
        quotes.delete(char);
      } else {
        quotes.set(char, { start: offset, first: tokens.length,
          prefix: !tokens.at(-1)?.rootArgument && tokens.at(-1)?.end === offset ? tokens.length - 1 : null });
      }
      continue;
    }
    if (char === '#' && start < 0 && !quotes.size) {
      while (offset < source.length && source[offset] !== '\n') {
        offset++;
        work.record('candidateCharacters');
      }
    }
    if (/\s/.test(source[offset] ?? '')) {
      flush(offset);
      if (source[offset] === '\n') tokens.push({ boundary: true, start: offset, end: offset + 1 });
      continue;
    }
    if (char === '{') {
      placeholder.lastIndex = offset;
      const match = placeholder.exec(source);
      if (match) {
        if (start < 0) start = offset;
        for (let part = offset + 1; part < offset + match[0].length; part++) literalCharacter(part);
        offset += match[0].length - 1;
        work.record('candidateCharacters', match[0].length - 1);
        continue;
      }
    }
    if (/[;|&()<>\x60{}]/.test(char)) {
      flush(offset);
      tokens.push({ boundary: true, start: offset, end: offset + 1 });
      continue;
    }
    if (start < 0) start = offset;
  }
  flush(source.length);
  finishArgument(source.length);
  for (const [index, token] of tokens.entries()) {
    token.next ??= index + 1;
    token.argument = token.value;
    if (!token.boundary) {
      work.call('normalizeCandidateToken');
      token.lower = token.value.toLowerCase();
    }
    work.record('candidateNormalizedCharacters', token.lower?.length ?? 0);
  }
  for (const group of groups) {
    const raw = inspectionSlice(source, group.start + 1, group.end, work, 'candidateSubstringCharacters');
    work.record('candidateQuoteCharacters', raw.length);
    const value = raw.replace(/([\\\x60])(["'\x60$\\])/g, '$2');
    const first = tokens[group.first];
    // Only option values use the quote jump. Candidate heads and verbs use raw atoms.
    if (!first.rootArgument) {
      first.argument = value;
      first.next = group.after;
      first.argumentGroup = true;
      first.argumentStart = group.start;
      first.argumentEnd = group.end + 1;
    }
    if (group.prefix !== null) {
      const prefix = tokens[group.prefix];
      prefix.argument = prefix.value + value;
      prefix.next = group.after;
      prefix.argumentStart = prefix.start;
      prefix.argumentEnd = group.end + 1;
      prefix.attachedValue = true;
    }
  }
  // Adjacent literal fragments form one value, even inside wrapper text. Each
  // fragment is joined once; joined nodes never materialize all suffix strings.
  for (let index = 0; index < tokens.length; index++) {
    const token = tokens[index];
    if (token.rootArgument || token.joinedInto || (token.boundary && !token.argumentGroup)) continue;
    const parts = [token.argument];
    let end = token.argumentEnd ?? token.end;
    let next = token.next;
    while (next < tokens.length) {
      const fragment = tokens[next];
      if (fragment.rootArgument || fragment.joinedInto ||
          (fragment.boundary && !fragment.argumentGroup) ||
          (fragment.argumentStart ?? fragment.start) !== end) break;
      if (source[end - 1] === "'" && source[end] === "'") parts.push("'");
      parts.push(fragment.argument);
      fragment.joinedInto = true;
      end = fragment.argumentEnd ?? fragment.end;
      next = fragment.next;
      work.record('candidateFragmentJoins');
    }
    if (parts.length > 1) {
      token.argument = parts.join('');
      work.record('candidateArgumentAssemblyCharacters', token.argument.length);
      token.argumentEnd = end;
      token.next = next;
    }
  }
  work.record('candidateTokens', tokens.length);
  return tokens;
}

// One reverse pass memoizes option transitions and API suffix state. Candidates read
// O(1) fields; shared suffixes contain references, never copied argv or concatenated tails.
function summarize(tokens, work) {
  work.call('candidateSummary');
  const count = tokens.length;
  const gitWord = new Int32Array(count + 1).fill(count);
  const ghWord = new Int32Array(count + 1).fill(count);
  const end = new Int32Array(count + 1).fill(count);
  const api = new Array(count + 1);
  const empty = { endpoint: null, method: null, hasBody: false, mutation: false };
  api[count] = empty;
  for (let index = count - 1; index >= 0; index--) {
    work.record('candidateSummaryTokens');
    const token = tokens[index];
    if (token.boundary && !token.argumentGroup) {
      end[index] = index;
      api[index] = empty;
      continue;
    }
    // Root slots are values for a consuming option, not additional command words.
    // Raw heads remain visible across wrapper quotes ("gh pr" "merge 12").
    if (token.rootArgument) {
      gitWord[index] = gitWord[index + 1];
      ghWord[index] = ghWord[index + 1];
      api[index] = api[index + 1];
      end[index] = end[index + 1];
      continue;
    }
    const next = token.attachedValue ? token.next : index + 1;
    const argument = token.attachedValue ? token.argument : (token.value ?? token.argument);
    const valueBoundary = tokens[next]?.boundary && !tokens[next]?.argumentGroup;
    const value = valueBoundary ? undefined : tokens[next]?.argument;
    const afterValue = valueBoundary ? next : (tokens[next]?.next ?? count);
    end[index] = end[token.next];
    const isOption = argument.startsWith('-');
    gitWord[index] = isOption ? gitWord[gitValues.has(argument) ? afterValue : next] : index;
    ghWord[index] = isOption ? ghWord[commandOptionLength(argument, work) === 2 ? afterValue : next] : index;
    work.record('candidateArgumentCharacters', argument.length);
    if (!isOption) {
      api[index] = { ...api[token.next], endpoint: { value: token.argument, kinds: null } };
      continue;
    }
    const option = optionAt([argument, value], 0, apiValues, work);
    const tail = api[option.consumed === 2 ? afterValue : next];
    const isMethod = option.name === '-X' || option.name === '--method';
    const query = queryOptions.has(option.name) && option.value?.startsWith('query=') ?
      inspectionSlice(option.value, 6, undefined, work, 'querySubstringCharacters') : null;
    const inlineQuery = query && !query.startsWith('@');
    if (inlineQuery) work.record('candidateQueryCharacters', query.length);
    const mutation = Boolean(inlineQuery && hasMergeMutation(query));
    api[index] = {
      endpoint: tail.endpoint,
      method: tail.method ?? (isMethod ? option.value?.toUpperCase() : null),
      hasBody: tail.hasBody || bodyOptions.has(option.name),
      mutation: tail.mutation || mutation,
    };
  }
  return { gitWord, ghWord, end, api };
}

export function inspectProtectedCandidates(command, work) {
  const tokens = candidateTokens(command, work);
  const { gitWord, ghWord, end, api } = summarize(tokens, work);
  const positions = [];
  for (let index = 0; index < tokens.length; index++) {
    work.record('candidateVisits');
    const token = tokens[index];
    if (token.boundary || token.rootArgument) continue;
    const match = /(?:^|[/\\:=])((?:git|gh)(?:\.exe)?)$/i.exec(token.value);
    if (!match) continue;
    const name = match[1].replace(/\.exe$/i, '').toLowerCase();
    const first = (name === 'git' ? gitWord : ghWord)[index + 1];
    const verb = tokens[first]?.lower;
    const afterVerb = first < tokens.length ? first + 1 : tokens.length;
    let protectedCommand = name === 'git' && verb === 'push';
    if (name === 'gh' && verb === 'pr') {
      protectedCommand = tokens[ghWord[afterVerb]]?.lower === 'merge';
    }
    if (name === 'gh' && verb === 'api') {
      const state = api[afterVerb];
      // Normalize each shared endpoint once. Method/body/query states are constant-size.
      const endpoint = state.endpoint;
      if (endpoint && !endpoint.kinds) {
        work.record('candidateEndpointCharacters', endpoint.value.length);
        endpoint.kinds = candidateApiEndpoint(endpoint.value);
      }
      const kinds = endpoint?.kinds ?? { rest: false, graphql: false };
      protectedCommand = (kinds.rest && (state.method ?? (state.hasBody ? 'POST' : 'GET')) !== 'GET') ||
        (kinds.graphql && state.mutation);
    }
    if (protectedCommand) positions.push(token.start + token.value.length - match[1].length);
    if (name === 'gh' && (verb === 'api' || verb === 'pr')) {
      // Proven gh arguments/query data are not separate commands. Option quote jumps
      // cross delimiters inside data; a real separator ends this invocation.
      index = Math.max(index, end[afterVerb] - 1);
    }
  }
  work.metrics.candidateTokens = tokens.length;
  return { present: positions.length > 0, positions };
}


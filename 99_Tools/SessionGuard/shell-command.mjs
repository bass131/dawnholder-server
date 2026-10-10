import { expandVariables, isNonFile } from './path-policy.mjs';

const operatorPattern = /^(?:&>>|&>|\d*(?:<<<|<<-|<<|<&|<|>>|>\||>&|>)|&&|\|\||\|&|[;&|()\n])/;
const isPipe = operator => operator === '|' || operator === '|&';

function tokenize(source) {
  let offset = 0;

  function sequence(nested = false) {
    const tokens = [];
    const heredocs = [];
    let pendingHeredoc = null;
    while (offset < source.length) {
      if (source[offset] === ')' && nested) {
        offset++;
        return tokens;
      }
      if (/[\t \r]/.test(source[offset])) {
        offset++;
        continue;
      }
      if (source[offset] === '#') {
        while (offset < source.length && source[offset] !== '\n') offset++;
        continue;
      }
      const operator = operatorPattern.exec(source.slice(offset))?.[0];
      if (operator) {
        tokens.push({ type: /[<>]/.test(operator) ? 'redirect' : 'operator', value: operator, position: offset });
        offset += operator.length;
        if (/^\d*<<-?$/.test(operator)) pendingHeredoc = operator.endsWith('-');
        if (operator === '\n') {
          for (const heredoc of heredocs.splice(0)) {
            let found = false;
            while (offset < source.length) {
              const end = source.indexOf('\n', offset);
              const limit = end < 0 ? source.length : end;
              let line = source.slice(offset, limit).replace(/\r$/, '');
              if (heredoc.stripTabs) line = line.replace(/^\t+/, '');
              offset = end < 0 ? source.length : end + 1;
              if (line === heredoc.delimiter) {
                found = true;
                break;
              }
            }
            if (!found) throw new Error('heredoc 종료 표식이 없다.');
          }
        }
        continue;
      }
      const token = word();
      tokens.push(token);
      if (pendingHeredoc !== null) {
        heredocs.push({ delimiter: token.parts.map(part => part.text).join(''), stripTabs: pendingHeredoc });
        pendingHeredoc = null;
      }
    }
    if (nested || heredocs.length || pendingHeredoc !== null) throw new Error('셸 명령이 닫히지 않았다.');
    return tokens;
  }

  function word() {
    const position = offset;
    const parts = [];
    let quote = null;
    function append(text, expand) {
      const last = parts.at(-1);
      if (last && !last.nested && last.expand === expand) last.text += text;
      else parts.push({ text, expand });
    }
    while (offset < source.length) {
      const char = source[offset];
      if (!quote && (/\s/.test(char) || /[;&|()<>]/.test(char))) break;
      if (quote !== "'" && source.startsWith('$(', offset)) {
        offset += 2;
        parts.push({ text: '$()', nested: sequence(true) });
        continue;
      }
      if (char === quote) {
        quote = null;
        offset++;
        continue;
      }
      if (!quote && (char === "'" || char === '"')) {
        quote = char;
        offset++;
        continue;
      }
      if (char === '\\' && quote !== "'") {
        const next = source[offset + 1];
        if (next !== undefined && (!quote || /[$`"\\]/.test(next))) {
          append(next, false);
          offset += 2;
          continue;
        }
      }
      append(char, quote !== "'");
      offset++;
    }
    if (quote) throw new Error('셸 따옴표가 닫히지 않았다.');
    return { type: 'word', parts, position, tilde: source[position] === '~' };
  }

  return sequence();
}

export function commandName(value = '') {
  return value.replace(/\\/g, '/').split('/').at(-1).replace(/\.exe$/i, '').toLowerCase();
}

export function readShellCommand(source, environment) {
  const commands = [];
  const writes = [];
  const tokens = tokenize(source.replace(/\\\r?\n/g, ''));

  function analyze(sequence, inherited) {
    const variables = { ...inherited };
    let current = { argv: [], redirects: [], end: null, next: null };
    let previous = null;
    let exportAssignments = false;

    function finish(operator) {
      if (operator === '\n' && !current.argv.length && !current.redirects.length &&
          (isPipe(previous?.end) || previous?.end === '&&' || previous?.end === '||')) return;
      current.end = operator;
      if (previous && isPipe(previous.end)) previous.next = current;
      commands.push(current);
      previous = current;
      current = { argv: [], redirects: [], end: null, next: null };
      exportAssignments = false;
    }

    function expand(word) {
      let value = '';
      let unresolved = false;
      for (const part of word.parts) {
        if (part.nested) {
          analyze(part.nested, variables);
          value += part.text;
          unresolved = true;
        } else {
          const expanded = part.expand ? expandVariables(part.text, variables) : { value: part.text, unresolved: false };
          value += expanded.value;
          unresolved ||= expanded.unresolved;
        }
      }
      if (word.tilde && /^~(?:\/|$)/.test(value)) {
        const home = variables.HOME;
        if (home === undefined) unresolved = true;
        else {
          value = (typeof home === 'object' ? home.value : home) + value.slice(1);
          unresolved ||= typeof home === 'object' && home.unresolved;
        }
      }
      return { value, unresolved, position: word.position };
    }

    function addWrite(destination) {
      if (destination.unresolved || !isNonFile(destination.value)) writes.push(destination);
    }

    for (let index = 0; index < sequence.length; index++) {
      const token = sequence[index];
      if (token.type === 'operator') {
        finish(token.value);
        continue;
      }
      if (token.type === 'redirect') {
        const target = sequence[++index];
        if (target?.type !== 'word') throw new Error('리다이렉트 목적지가 없다.');
        const destination = expand(target);
        current.redirects.push({ operator: token.value, destination });
        if (/^(?:&>>?|\d*(?:>>?|>\|))$/.test(token.value)) addWrite(destination);
        continue;
      }
      const argument = expand(token);
      const assignment = /^([A-Za-z_]\w*)=(.*)$/s.exec(argument.value);
      if (assignment && (current.argv.length === 0 || exportAssignments)) {
        variables[assignment[1]] = { value: assignment[2], unresolved: argument.unresolved };
        continue;
      }
      current.argv.push(argument);
      if (current.argv.length === 1) exportAssignments = argument.value === 'export';
      if (current.argv.length > 1 && commandName(current.argv[0].value) === 'tee') {
        const afterOptions = current.argv.slice(1, -1).some(arg => arg.value === '--');
        if (afterOptions || !argument.value.startsWith('-')) addWrite(argument);
      }
    }
    finish(null);
  }

  analyze(tokens, environment);
  writes.sort((left, right) => left.position - right.position);
  return { commands, writes };
}

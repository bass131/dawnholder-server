// This is an explicit-command reader, not a shell evaluator. Quotes retain data boundaries;
// only executable groups/substitutions and literal wrappers are visited by command-policy.
export function commandName(value = '') {
  return value.replace(/\\/g, '/').split('/').at(-1).replace(/\.exe$/i, '').toLowerCase();
}

function readSubstitution(source, start, end, depth) {
  // Use the command lexer itself so heredoc/here-string data cannot close a substitution.
  const nested = readSequence(source.slice(start), depth + 1, end);
  return { offset: start + nested.offset, tokens: nested.tokens };
}

function heredocSubstitutions(body, depth, powerShell = false) {
  const substitutions = [];
  for (let index = 0; index < body.length; index++) {
    if (body[index] === (powerShell ? '`' : '\\')) {
      index++;
      continue;
    }
    const dollar = body.startsWith('$(', index);
    if (!dollar && (powerShell || body[index] !== '`')) continue;
    const nested = readSubstitution(body, index + (dollar ? 2 : 1), dollar ? ')' : '`', depth);
    substitutions.push(nested.tokens);
    index = nested.offset - 1;
  }
  return substitutions;
}

export function readShell(source, depth = 0) {
  return readSequence(source, depth).tokens;
}

// PowerShell permits these control keywords directly against a scriptblock brace.
const adjacentBlockKeywords = new Set(['else', 'try', 'catch', 'finally', 'do']);
const powerShellCatchType = /^\[[^\]\r\n]+\]$/;
// gh API placeholders are data, including a standalone {owner} or a path component.
const apiPlaceholder = /^\{[A-Za-z_][A-Za-z0-9_]*\}(?:\/|$)/;

function readSequence(source, depth, terminator = null) {
  if (depth > 32) throw new Error('shell nesting exceeds 32 levels');
  let offset = 0;
  const tokens = [];
  const heredocs = [];
  let braceDepth = 0;
  let parenthesisDepth = 0;
  let pendingHeredoc = null;
  const operators = /^(?:\d*(?:<<-|<<<|<<|>>|>&|<&|>|<)|&&|\|\||\|&|[;&|\n])/;

  function substitution(end) {
    const nested = readSubstitution(source, offset, end, depth);
    offset = nested.offset;
    return nested.tokens;
  }

  while (offset < source.length) {
    if (terminator && source[offset] === terminator && parenthesisDepth === 0) {
      if (heredocs.length || pendingHeredoc !== null) throw new Error('unclosed heredoc');
      return { tokens, offset: offset + 1 };
    }
    if (/[ \t\r]/.test(source[offset])) {
      offset++;
      continue;
    }
    if (source[offset] === '#') {
      while (offset < source.length && source[offset] !== '\n') offset++;
      continue;
    }
    if (source.startsWith('<#', offset)) {
      const end = source.indexOf('#>', offset + 2);
      if (end < 0) throw new Error('unclosed PowerShell comment');
      offset = end + 2;
      continue;
    }
    const char = source[offset];
    const processSubstitution = /^[<>]\(/.test(source.slice(offset));
    const placeholder = apiPlaceholder.test(source.slice(offset));
    if (char === '(' || (char === '{' && !placeholder)) {
      tokens.push({ kind: 'open', value: char });
      if (char === '{') braceDepth++;
      if (char === '(') parenthesisDepth++;
      offset++;
      continue;
    }
    if (char === ')' || (char === '}' && braceDepth > 0)) {
      tokens.push({ kind: 'close', value: char });
      if (char === '}') braceDepth--;
      if (char === ')') parenthesisDepth--;
      offset++;
      continue;
    }
    const operator = !processSubstitution && operators.exec(source.slice(offset))?.[0];
    if (operator) {
      offset += operator.length;
      tokens.push({ kind: /[<>]/.test(operator) ? 'redirect' : 'operator', value: operator });
      if (/^\d*<<-?$/.test(operator)) pendingHeredoc = operator.endsWith('-');
      if (operator === '\n') {
        for (const heredoc of heredocs.splice(0)) {
          let found = false;
          const body = [];
          while (offset < source.length) {
            let end = source.indexOf('\n', offset);
            if (end < 0) end = source.length;
            let line = source.slice(offset, end).replace(/\r$/, '');
            if (heredoc.stripTabs) line = line.replace(/^\t+/, '');
            offset = Math.min(source.length, end + 1);
            if (line === heredoc.delimiter) {
              found = true;
              break;
            }
            body.push(line);
          }
          if (!found) throw new Error('unclosed heredoc');
          if (!heredoc.quoted) {
            tokens.push({ kind: 'word', value: '', literal: false,
              substitutions: heredocSubstitutions(body.join('\n'), depth) });
            tokens.push({ kind: 'operator', value: '\n' });
          }
        }
      }
      continue;
    }

    const word = { kind: 'word', value: '', literal: true, quoted: false, substitutions: [] };
    let quote = null;
    let embeddedBraces = 0;
    while (offset < source.length) {
      const current = source[offset];
      if (!quote && terminator === '`' && current === '`') break;
      if (!quote && /^@["']\r?\n/.test(source.slice(offset))) {
        const quoteMark = source[offset + 1];
        const bodyStart = source.indexOf('\n', offset) + 1;
        const closing = new RegExp(`^${quoteMark}@`, 'm').exec(source.slice(bodyStart));
        if (!closing) throw new Error('unclosed PowerShell here-string');
        const body = source.slice(bodyStart, bodyStart + closing.index);
        word.value += body.replace(/\r?\n$/, '');
        word.quoted = true;
        if (quoteMark === '"') {
          word.substitutions.push(...heredocSubstitutions(body, depth, true));
          if (body.includes('$')) word.literal = false;
        }
        offset = bodyStart + closing.index + 2;
        continue;
      }
      // POSIX process substitutions are argument words whose contents execute in a child scope.
      if (!quote && /^[<>]\(/.test(source.slice(offset))) {
        offset += 2;
        word.substitutions.push(substitution(')'));
        word.literal = false;
        word.value += '<process-substitution>';
        continue;
      }
      if (!quote && (/[\s;&|()<>]/.test(current))) break;
      if (!quote && current === '{' && (adjacentBlockKeywords.has(word.value.toLowerCase()) ||
          powerShellCatchType.test(word.value))) break;
      if (!quote && current === '}' && braceDepth > 0 && embeddedBraces === 0) break;
      if (!quote && current === '{') embeddedBraces++;
      if (!quote && current === '}' && embeddedBraces > 0) embeddedBraces--;
      if (current === quote) {
        // PowerShell doubles single quotes inside single-quoted strings.
        if (quote === "'" && source[offset + 1] === "'") {
          word.value += "'";
          offset += 2;
          continue;
        }
        quote = null;
        offset++;
        continue;
      }
      if (!quote && (current === "'" || current === '"')) {
        word.quoted = true;
        quote = current;
        offset++;
        continue;
      }
      if (quote !== "'" && source.startsWith('$(', offset)) {
        offset += 2;
        word.substitutions.push(substitution(')'));
        word.literal = false;
        word.value += '$()';
        continue;
      }
      if (quote !== "'" && current === '`' && source.indexOf('`', offset + 1) >= 0) {
        // Paired backticks are POSIX command substitution; an unpaired one is a PS escape below.
        offset++;
        word.substitutions.push(substitution('`'));
        word.literal = false;
        word.value += '`...`';
        continue;
      }
      if (quote !== "'" && (current === '\\' || current === '`')) {
        const next = source[offset + 1];
        if (next === '\n' || (next === '\r' && source[offset + 2] === '\n')) {
          offset += next === '\r' ? 3 : 2;
          continue;
        }
        // Keep Windows path separators; consume only explicit shell escapes.
        if (next !== undefined && /[\s"'`$\\;&|()]/.test(next)) {
          word.quoted = true;
          word.value += next;
          offset += 2;
          continue;
        }
      }
      if (quote !== "'" && current === '$') word.literal = false;
      word.value += current;
      offset++;
    }
    if (quote) throw new Error('unclosed shell quote');
    tokens.push(word);
    if (pendingHeredoc !== null) {
      heredocs.push({ delimiter: word.value, stripTabs: pendingHeredoc, quoted: word.quoted });
      pendingHeredoc = null;
    }
  }
  if (heredocs.length || pendingHeredoc !== null) throw new Error('unclosed heredoc');
  if (terminator) throw new Error(`unclosed shell substitution (${terminator})`);
  return { tokens, offset };
}

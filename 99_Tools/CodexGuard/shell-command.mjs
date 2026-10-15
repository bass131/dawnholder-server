// Lexical data boundaries only: no host options, command status or shell control-flow semantics.
// Syntax/budget failures are distinct from internal and I/O errors at the policy boundary.
import { inspectionSlice } from './inspection-budget.mjs';
export class UnsupportedSyntax extends Error {}
export function commandName(value = '') {
  return value.replace(/\\/g, '/').split('/').at(-1).replace(/\.exe$/i, '').toLowerCase();
}

function readSubstitution(source, start, end, depth, work) {
  // Use the command lexer itself so heredoc/here-string data cannot close a substitution.
  // Keep absolute offsets into one source. Sequential substitutions must not allocate
  // a copy of the entire remaining suffix for every child, even at shallow depth.
  const nested = readSequence(source, depth + 1, end, work, start);
  return { offset: nested.offset, tokens: nested.tokens };
}

function heredocSubstitutions(body, depth, powerShell = false, work) {
  const substitutions = [];
  for (let index = 0; index < body.length; index++) {
    work?.step('shellBodyCharacters');
    if (body[index] === (powerShell ? '`' : '\\')) {
      index++;
      continue;
    }
    const dollar = body.startsWith('$(', index);
    if (!dollar && (powerShell || body[index] !== '`')) continue;
    const nested = readSubstitution(body, index + (dollar ? 2 : 1), dollar ? ')' : '`', depth, work);
    substitutions.push(nested.tokens);
    index = nested.offset - 1;
  }
  return substitutions;
}

export function readShell(source, depth = 0, work) {
  work?.call('shellLexer');
  work?.check();
  return readSequence(source, depth, null, work).tokens;
}

// PowerShell permits these control keywords directly against a scriptblock brace.
// gh API placeholders are data, including a standalone {owner} or a path component.
const apiPlaceholder = /\{[A-Za-z_][A-Za-z0-9_]*\}(?:\/|$)/y;
function isPlaceholder(source, offset) {
  if (source[offset] !== '{') return false;
  apiPlaceholder.lastIndex = offset;
  return apiPlaceholder.test(source);
}

function readSequence(source, depth, terminator = null, work, start = 0) {
  work?.call('shellSequence');
  if (depth > 32) throw new UnsupportedSyntax('shell nesting exceeds 32 levels');
  let offset = start;
  const tokens = [];
  const heredocs = [];
  let braceDepth = 0;
  let parenthesisDepth = 0;
  let pendingHeredoc = null;
  const operators = /(?:\d*(?:<<-|<<<|<<|>>|>&|<&|>|<)|&&|\|\||\|&|[;&|\n])/y;

  function substitution(end) {
    const nested = readSubstitution(source, offset, end, depth, work);
    offset = nested.offset;
    return nested.tokens;
  }

  while (offset < source.length) {
    work?.step('shellCharacters');
    if (terminator && source[offset] === terminator && parenthesisDepth === 0) {
      if (heredocs.length || pendingHeredoc !== null) throw new UnsupportedSyntax('unclosed heredoc');
      return { tokens, offset: offset + 1 };
    }
    if (/[ \t\r]/.test(source[offset])) {
      offset++;
      continue;
    }
    if (source[offset] === '#') {
      while (offset < source.length && source[offset] !== '\n') {
        offset++;
        work?.step('shellCommentCharacters');
      }
      continue;
    }
    if (source.startsWith('<#', offset)) {
      const end = source.indexOf('#>', offset + 2);
      if (end < 0) throw new UnsupportedSyntax('unclosed PowerShell comment');
      work?.step('shellCommentCharacters', end - offset);
      offset = end + 2;
      continue;
    }
    const char = source[offset];
    const processSubstitution = (char === '<' || char === '>') && source[offset + 1] === '(';
    const placeholder = isPlaceholder(source, offset);
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
    operators.lastIndex = offset;
    const operator = !processSubstitution && operators.exec(source)?.[0];
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
            let line = inspectionSlice(source, offset, end, work, 'shellSubstringCharacters').replace(/\r$/, '');
            work?.step('shellBodyCharacters', end - offset);
            if (heredoc.stripTabs) line = line.replace(/^\t+/, '');
            offset = Math.min(source.length, end + 1);
            if (line === heredoc.delimiter) {
              found = true;
              break;
            }
            body.push(line);
          }
          if (!found) throw new UnsupportedSyntax('unclosed heredoc');
          const value = body.join('\n');
          work?.step('shellValueCharacters', value.length);
          tokens.push({ kind: 'body', value,
            substitutions: heredoc.quoted ? [] : heredocSubstitutions(value, depth, false, work) });
        }
      }
      continue;
    }

    const word = { kind: 'word', start: offset, value: '', literal: true, quoted: false, substitutions: [] };
    const parts = [];
    let quote = null;
    let embeddedBraces = 0;
    while (offset < source.length) {
      work?.step('shellWordCharacters');
      const current = source[offset];
      if (!quote && terminator === '`' && current === '`') break;
      if (!quote && current === '@' && /["']/.test(source[offset + 1] ?? '') &&
          (source[offset + 2] === '\n' || (source[offset + 2] === '\r' && source[offset + 3] === '\n'))) {
        const quoteMark = source[offset + 1];
        const bodyStart = source.indexOf('\n', offset) + 1;
        const closingPattern = new RegExp(`^${quoteMark}@`, 'gm');
        closingPattern.lastIndex = bodyStart;
        const closing = closingPattern.exec(source);
        if (!closing) throw new UnsupportedSyntax('unclosed PowerShell here-string');
        const body = inspectionSlice(source, bodyStart, closing.index, work, 'shellSubstringCharacters');
        work?.step('shellBodyCharacters', body.length);
        parts.push(body.replace(/\r?\n$/, ''));
        word.quoted = true;
        if (quoteMark === '"') {
          word.substitutions.push(...heredocSubstitutions(body, depth, true, work));
          if (body.includes('$')) word.literal = false;
        }
        offset = closing.index + 2;
        continue;
      }
      // POSIX process substitutions are argument words whose contents execute in a child scope.
      if (!quote && (current === '<' || current === '>') && source[offset + 1] === '(') {
        offset += 2;
        word.substitutions.push(substitution(')'));
        word.literal = false;
        parts.push('<process-substitution>');
        continue;
      }
      if (!quote && (/[\s;&|()<>]/.test(current))) break;
      if (!quote && current === '{' && !isPlaceholder(source, offset)) break;
      if (!quote && current === '}' && braceDepth > 0 && embeddedBraces === 0) break;
      if (!quote && current === '{') embeddedBraces++;
      if (!quote && current === '}' && embeddedBraces > 0) embeddedBraces--;
      if (current === quote) {
        // PowerShell doubles single quotes inside single-quoted strings.
        if (quote === "'" && source[offset + 1] === "'") {
          parts.push("'");
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
        parts.push('$()');
        continue;
      }
      if (quote === '"' && current === '`' && source[offset + 1] === '"') {
        // PowerShell's escaped quote is argument data, including paired `" ... `".
        parts.push('"');
        offset += 2;
        continue;
      }
      if (quote !== "'" && current === '`' && source.indexOf('`', offset + 1) >= 0) {
        // Paired backticks are POSIX command substitution; an unpaired one is a PS escape below.
        offset++;
        word.substitutions.push(substitution('`'));
        word.literal = false;
        parts.push('`...`');
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
          parts.push(next);
          offset += 2;
          continue;
        }
      }
      if (quote !== "'" && current === '$') word.literal = false;
      parts.push(current);
      offset++;
    }
    if (quote) throw new UnsupportedSyntax('unclosed shell quote');
    word.value = parts.join('');
    work?.step('shellValueCharacters', word.value.length);
    word.end = offset;
    tokens.push(word);
    if (pendingHeredoc !== null) {
      heredocs.push({ delimiter: word.value, stripTabs: pendingHeredoc, quoted: word.quoted });
      pendingHeredoc = null;
    }
  }
  if (heredocs.length || pendingHeredoc !== null) throw new UnsupportedSyntax('unclosed heredoc');
  if (terminator) throw new UnsupportedSyntax(`unclosed shell substitution (${terminator})`);
  return { tokens, offset };
}

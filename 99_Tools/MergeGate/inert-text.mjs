// The main decision uses the broad runner list instead of tracking whether a written file is executed.
const runnerNames = new Set([
  'bash', 'sh', 'zsh', 'dash', 'ksh', 'fish', 'pwsh', 'powershell', 'cmd', 'wsl',
  'node', 'nodejs', 'deno', 'bun', 'bunx', 'npx', 'npm', 'pnpm', 'yarn', 'tsx',
  'python', 'python3', 'py', 'perl', 'ruby', 'php', 'lua', 'awk', 'gawk', 'java',
  'dotnet', 'cscript', 'wscript', 'mshta', 'make',
  'source', 'eval', 'exec', 'xargs', 'env', 'sudo', 'nohup', 'timeout', 'command',
  'builtin', 'start', 'call',
]);
const textPipeNames = new Set(['tee', 'cat', 'head', 'tail', 'wc', 'cut', 'grep', 'sort', 'uniq']);
const reservedWords = new Set(['!', '{', '}', 'if', 'then', 'elif', 'else', 'do', 'while', 'until', 'time']);

// This reader locates inert spans; it neither expands shell values nor classifies merge attempts.
class ShellTextReader {
  constructor(text) {
    this.text = text;
    this.index = 0;
    this.failed = false;
    this.lists = [];
    this.heredocs = [];
  }

  readList(stop = '', inSubstitution = false) {
    const commands = [];
    this.lists.push(commands);
    const pendingHeredocs = [];
    let current = { tokens: [], boundary: '' };
    let groupDepth = 0;
    const finish = boundary => {
      if (current.tokens.length === 0) return;
      current.boundary = boundary;
      commands.push(current);
      current = { tokens: [], boundary: '' };
    };
    const close = () => {
      finish('');
      if (pendingHeredocs.length > 0 || groupDepth > 0 ||
          ['&&', '||', '|', '|&'].includes(commands.at(-1)?.boundary)) this.failed = true;
    };

    while (this.index < this.text.length && !this.failed) {
      const character = this.text[this.index];
      if (stop && character === stop && groupDepth === 0) {
        this.index += 1;
        close();
        return;
      }
      if (character === '\n' || this.text.startsWith('\r\n', this.index)) {
        this.index += character === '\r' ? 2 : 1;
        finish('\n');
        this.readHeredocBodies(pendingHeredocs);
        pendingHeredocs.length = 0;
        continue;
      }
      if (/\s/.test(character)) {
        this.index += 1;
        continue;
      }
      if (character === '#') {
        while (this.index < this.text.length && this.text[this.index] !== '\n') this.index += 1;
        continue;
      }
      if (this.text.startsWith('<(', this.index) || this.text.startsWith('>(', this.index)) {
        this.failed = true;
        return;
      }

      const redirect = /^(?:[0-9]+)?(?:<<<|<<-|<<|>>|>&|<&|<>|>\||>|<)|^&(?:>>|>)/.exec(this.text.slice(this.index));
      if (redirect) {
        const operator = redirect[0].replace(/^[0-9]+/, '');
        this.index += redirect[0].length;
        while (this.index < this.text.length && /[ \t]/.test(this.text[this.index])) this.index += 1;
        const target = this.readWord(stop, inSubstitution);
        if (!target) {
          this.failed = true;
          return;
        }
        current.tokens.push({ kind: 'redirect', operator, target });
        if (operator === '<<' || operator === '<<-') {
          pendingHeredocs.push({
            delimiter: target.value, quoted: target.quotes.length > 0, stripTabs: operator === '<<-',
            command: current, inSubstitution,
          });
        }
        continue;
      }

      if (';&|'.includes(character)) {
        const pair = this.text.slice(this.index, this.index + 2);
        const boundary = ['&&', '||', '|&'].includes(pair) ? pair : character;
        this.index += boundary.length;
        finish(boundary);
        continue;
      }
      if (character === '(' || character === ')') {
        this.index += 1;
        finish(character);
        groupDepth += character === '(' ? 1 : -1;
        if (groupDepth < 0) this.failed = true;
        continue;
      }
      const word = this.readWord(stop, inSubstitution);
      if (!word) {
        this.failed = true;
        return;
      }
      current.tokens.push(word);
    }
    close();
    if (stop) this.failed = true;
  }

  readWord(stop, inSubstitution) {
    const start = this.index;
    let value = '';
    const quotes = [];
    while (this.index < this.text.length && !this.failed) {
      const character = this.text[this.index];
      if (/\s/.test(character) || ';&|()<>'.includes(character) || (stop === '`' && character === '`')) break;
      if (character === '\\') {
        this.index += 1;
        if (this.index === this.text.length) {
          this.failed = true;
          break;
        }
        if (this.text.startsWith('\r\n', this.index)) this.index += 2;
        else if (this.text[this.index] === '\n') this.index += 1;
        else value += this.text[this.index++];
      } else if (character === "'" || character === '"') {
        const quoted = this.readQuote(character, inSubstitution);
        if (!quoted) break;
        value += quoted.value;
        quotes.push(quoted);
      } else if (this.isExpansion()) {
        const expansion = this.readExpansion();
        value += this.text.slice(expansion.start, expansion.end);
      } else {
        value += character;
        this.index += 1;
      }
    }
    return this.index === start || this.failed ? null : {
      kind: 'word', start, end: this.index, value, quotes,
      assignment: /^[A-Za-z_][A-Za-z0-9_]*=/.test(this.text.slice(start)),
    };
  }

  isExpansion() {
    return this.text[this.index] === '`' || this.text.startsWith('$(', this.index) ||
      this.text.startsWith('${', this.index);
  }

  readQuote(quote, inSubstitution) {
    const start = this.index++;
    let value = '';
    const keep = [];
    while (this.index < this.text.length && !this.failed) {
      const character = this.text[this.index];
      if (character === quote) {
        this.index += 1;
        return { start, end: this.index, value, keep };
      }
      if (quote === '"' && character === '\\' &&
          ['"', '`', '$', '\\', '\n', '\r'].includes(this.text[this.index + 1])) {
        this.index += 1;
        if (this.text.startsWith('\r\n', this.index)) this.index += 2;
        else if (this.text[this.index] === '\n') this.index += 1;
        else value += this.text[this.index++];
      } else if (quote === '"' && this.isExpansion()) {
        const expansion = this.readExpansion();
        keep.push(expansion);
        value += this.text.slice(expansion.start, expansion.end);
      } else {
        value += character;
        this.index += 1;
      }
    }
    this.failed = true;
    return null;
  }

  readExpansion() {
    const start = this.index;
    if (this.text.startsWith('${', start)) {
      this.index += 2;
      this.readParameter();
    } else if (this.text[start] === '`') {
      this.index += 1;
      this.readList('`', true);
    } else {
      this.index += 2;
      this.readList(')', true);
    }
    return { start, end: this.index };
  }

  readParameter() {
    let depth = 1;
    while (this.index < this.text.length && !this.failed) {
      const character = this.text[this.index];
      if (character === '\\') {
        this.index += 2;
      } else if (character === "'" || character === '"') {
        this.readQuote(character, true);
      } else if (this.isExpansion()) {
        this.readExpansion();
      } else {
        this.index += 1;
        if (character === '{') depth += 1;
        if (character === '}') depth -= 1;
        if (depth === 0) return;
      }
    }
    this.failed = true;
  }

  readHeredocBodies(pending) {
    for (const heredoc of pending) {
      const start = this.index;
      let found = false;
      while (this.index <= this.text.length) {
        const newline = this.text.indexOf('\n', this.index);
        const end = newline < 0 ? this.text.length : newline;
        let line = this.text.slice(this.index, end).replace(/\r$/, '');
        if (heredoc.stripTabs) line = line.replace(/^\t+/, '');
        if (line === heredoc.delimiter) {
          this.heredocs.push({ ...heredoc, start, end: this.index });
          this.index = newline < 0 ? end : end + 1;
          found = true;
          break;
        }
        if (newline < 0) break;
        this.index = end + 1;
      }
      if (!found) {
        this.failed = true;
        return;
      }
    }
  }
}

function commandWords(command) {
  const words = command.tokens.filter(token => token.kind === 'word');
  const first = words.findIndex(word => !word.assignment &&
    !reservedWords.has(word.value));
  return first < 0 ? [] : words.slice(first);
}

function hasCodePipe(lists) {
  return lists.some(commands => commands.some((command, index) =>
    ['|', '|&'].includes(command.boundary) && !textPipeNames.has(commandWords(commands[index + 1] ?? { tokens: [] })[0]?.value)));
}

function quotedArguments(words, codePipe) {
  const [executable, ...args] = words.map(word => word.value);
  if ((executable === 'echo' || executable === 'printf') && !codePipe) return words.slice(1);
  let valueOptions;
  // Only the explicitly listed inline option forms can hide quoted values.
  let inlineOptions = new Set();
  let start;
  if (executable === 'git' && args[0] === 'commit') {
    valueOptions = new Set(['-m', '--message']);
    inlineOptions = new Set(['--message']);
    start = 2;
  } else if (executable === 'gh' && args[0] === 'pr' && ['create', 'edit'].includes(args[1])) {
    valueOptions = new Set(['-t', '--title', '-b', '--body']);
    inlineOptions = valueOptions;
    start = 3;
  } else if (executable === 'orca' && args[0] === 'orchestration' && ['send', 'reply'].includes(args[1])) {
    valueOptions = new Set(['--subject', '--body']);
    start = 3;
  } else return [];

  const selected = [];
  for (let index = start; index < words.length; index += 1) {
    const option = words[index].value;
    if (valueOptions.has(option)) {
      if (words[index + 1]) selected.push(words[++index]);
    } else if (inlineOptions.has(option.split('=')[0]) && option.includes('=')) selected.push(words[index]);
  }
  return selected;
}

function receivesTextHeredoc(heredoc) {
  if (!heredoc.quoted || heredoc.inSubstitution || ['|', '|&'].includes(heredoc.command.boundary)) return false;
  const words = commandWords(heredoc.command).map(word => word.value);
  if (words[0] === 'cat' || words[0] === 'tee') return true;
  return words[0] === 'git' && words[1] === 'commit' && words.some((word, index) =>
    word === '--file=-' || (['-F', '--file'].includes(word) && words[index + 1] === '-'));
}

function renderMasked(text, edits) {
  edits.sort((left, right) => left.start - right.start || right.end - left.end);
  const render = (start, end) => {
    let result = '';
    let cursor = start;
    for (const edit of edits) {
      if (edit.start < cursor || edit.end > end) continue;
      result += text.slice(cursor, edit.start);
      result += edit.keep === null ? '' : "''" + edit.keep.map(span => render(span.start, span.end)).join("''") +
        (edit.keep.length > 0 ? "''" : '');
      cursor = edit.end;
    }
    return result + text.slice(cursor, end);
  };
  return render(0, text.length);
}

function hasRunner(masked, lists) {
  const words = (masked.match(/[A-Za-z0-9_./:@+\-]+/g) ?? []).map(word => word.toLowerCase());
  if (words.some(word => runnerNames.has(word.split('/').at(-1).replace(/\.exe$/, '')) ||
      /^(?:\.\/|\.\.\/|~\/)/.test(word))) return true;
  return lists.some(commands => commands.some(command => {
    const name = commandWords(command)[0]?.value;
    return name === '.' || (typeof name === 'string' && /[\\/]/.test(name));
  }));
}

/** Mask only the two inert text positions of behavior contract v3.1; retain code and runner commands. */
export function maskInertText(command) {
  if (typeof command !== 'string') return command;
  try {
    const reader = new ShellTextReader(command);
    reader.readList();
    // An uncertain shell read must preserve the original blocking surface.
    if (reader.failed) return command;
    const codePipe = hasCodePipe(reader.lists);
    const edits = reader.heredocs.filter(receivesTextHeredoc).map(heredoc => ({
      start: heredoc.start, end: heredoc.end, keep: null,
    }));
    for (const commands of reader.lists) {
      for (const current of commands) {
        for (const word of quotedArguments(commandWords(current), codePipe)) {
          if (!word.assignment) edits.push(...word.quotes);
        }
      }
    }
    const masked = renderMasked(command, edits);
    return hasRunner(masked, reader.lists) ? command : masked;
  } catch {
    // Unexpected reader state must fail closed by restoring the entire original command.
    return command;
  }
}

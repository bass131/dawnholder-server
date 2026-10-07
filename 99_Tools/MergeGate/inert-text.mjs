// Behavior contract: 「글 가리기(v3)」 · 「실행기 찾기(v3.1·v3.2)」 · 「bash와 다르게 읽을 수 있는 꼴(v3.2)」.
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
    this.pipes = [];
    this.heredocs = [];
  }

  readList(stop = '', inSubstitution = false) {
    const commands = [];
    this.lists.push(commands);
    const pendingHeredocs = [];
    let current = { tokens: [], boundary: '' };
    const groups = [];
    let closedGroup = null;
    let pendingPipe = null;
    let needsCommand = false;
    const finish = boundary => {
      if (current.tokens.length === 0) return null;
      current.boundary = boundary;
      commands.push(current);
      const completed = current;
      current = { tokens: [], boundary: '' };
      return completed;
    };
    const openGroup = closing => {
      // Function/array/arithmetic syntax cannot be read as a plain command group.
      if (current.tokens.some(token => token.kind !== 'word' || token.assignment || !reservedWords.has(token.value))) {
        this.failed = true;
        return;
      }
      finish('(');
      if (pendingPipe) pendingPipe.intoGroup = true;
      pendingPipe = null;
      closedGroup = null;
      needsCommand = false;
      groups.push({ closing, start: commands.length });
    };
    const closeGroup = closing => {
      if (groups.at(-1)?.closing !== closing || needsCommand) {
        this.failed = true;
        return;
      }
      finish(closing);
      closedGroup = commands.slice(groups.pop().start);
      if (closedGroup.length === 0) this.failed = true;
    };
    const close = () => {
      finish('');
      if (pendingHeredocs.length > 0 || groups.length > 0 || needsCommand) this.failed = true;
    };

    while (this.index < this.text.length && !this.failed) {
      const character = this.text[this.index];
      if (stop && character === stop && groups.length === 0) {
        this.index += 1;
        close();
        return;
      }
      if (this.unsupportedUnquoted()) {
        this.failed = true;
        return;
      }
      if (character === '\n') {
        this.index += 1;
        finish('\n');
        closedGroup = null;
        this.readHeredocBodies(pendingHeredocs);
        pendingHeredocs.length = 0;
        continue;
      }
      if (character === ' ' || character === '\t') {
        this.index += 1;
        continue;
      }
      if (character === '#') {
        while (this.index < this.text.length && this.text[this.index] !== '\n') {
          if (this.unsupportedUnquoted()) {
            this.failed = true;
            return;
          }
          this.index += 1;
        }
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
        needsCommand = false;
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
        const completed = finish(boundary);
        if (boundary === '|' || boundary === '|&') {
          const sources = closedGroup ?? (completed ? [completed] : []);
          if (sources.length === 0 || needsCommand) {
            this.failed = true;
            return;
          }
          // Every nested command shares the group's stdout; its consumer is outside the group.
          for (const source of sources) source.piped = true;
          pendingPipe = { commands, nextIndex: commands.length, intoGroup: false };
          this.pipes.push(pendingPipe);
        } else pendingPipe = null;
        closedGroup = null;
        needsCommand = ['&&', '||', '|', '|&'].includes(boundary);
        continue;
      }
      if (character === '(' || character === ')') {
        this.index += 1;
        if (character === '(') openGroup(')');
        else closeGroup(')');
        continue;
      }
      const word = this.readWord(stop, inSubstitution);
      if (!word) {
        this.failed = true;
        return;
      }
      if (word.raw === '{' && commandWords(current).length === 0) openGroup('}');
      else if (word.raw === '}' && commandWords(current).length === 0) closeGroup('}');
      else {
        current.tokens.push(word);
        closedGroup = null;
        pendingPipe = null;
        needsCommand = false;
      }
    }
    close();
    if (stop) this.failed = true;
  }

  unsupportedUnquoted() {
    const character = this.text[this.index];
    return (/\s/.test(character) && ![' ', '\t', '\n'].includes(character)) ||
      this.text.startsWith("$'", this.index) || this.text.startsWith('$"', this.index);
  }

  readWord(stop, inSubstitution) {
    const start = this.index;
    let value = '';
    const quotes = [];
    while (this.index < this.text.length && !this.failed) {
      const character = this.text[this.index];
      if (this.unsupportedUnquoted()) {
        this.failed = true;
        break;
      }
      if ([' ', '\t', '\n'].includes(character) || ';&|()<>'.includes(character) || (stop === '`' && character === '`')) break;
      if (character === '\\') {
        this.index += 1;
        if (this.index === this.text.length) {
          this.failed = true;
          break;
        }
        if (/\s/.test(this.text[this.index]) && ![' ', '\t', '\n'].includes(this.text[this.index])) {
          this.failed = true;
          break;
        }
        if (this.text[this.index] === '\n') this.index += 1;
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
      kind: 'word', start, end: this.index, raw: this.text.slice(start, this.index), value, quotes,
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
          ['"', '`', '$', '\\', '\n'].includes(this.text[this.index + 1])) {
        this.index += 1;
        if (this.text[this.index] === '\n') this.index += 1;
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
      // Bash ends at the first unescaped backtick; quoting/escaping inside needs a different reader.
      const end = this.text.indexOf('`', start + 1);
      if (end < 0 || /['"\\]/.test(this.text.slice(start + 1, end))) {
        this.failed = true;
        return { start, end: start };
      }
      this.index += 1;
      this.readList('`', true);
      if (this.index !== end + 1) this.failed = true;
    } else if (this.text.startsWith('$((', start)) {
      // Arithmetic expansion has quoting/grouping rules outside this command-substitution reader.
      this.failed = true;
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
      if (this.unsupportedUnquoted()) {
        this.failed = true;
        return;
      }
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
        let line = this.text.slice(this.index, end);
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

function hasCodePipe(pipes) {
  return pipes.some(pipe => pipe.intoGroup ||
    !textPipeNames.has(commandWords(pipe.commands[pipe.nextIndex] ?? { tokens: [] })[0]?.value));
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
  if (!heredoc.quoted || heredoc.inSubstitution || heredoc.command.piped) return false;
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
      // Empty quotes preserve word boundaries while retained substitutions remain visible as code.
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
  // gh api can read the text written to a file or stdin by the same command.
  if (words.includes('api') && words.some(word => ['gh', 'gh.exe'].includes(word.split('/').at(-1)))) return true;
  return lists.some(commands => commands.some(command => {
    const word = commandWords(command)[0];
    return word && (word.value === '.' || /[\\/]/.test(word.value) || /[$`]/.test(word.raw));
  }));
}

/** Mask only the two inert text positions of behavior contract v3.2; retain code and runner commands. */
export function maskInertText(command) {
  if (typeof command !== 'string' || command.includes('\r')) return command;
  try {
    const reader = new ShellTextReader(command);
    reader.readList();
    // An uncertain shell read must preserve the original blocking surface.
    if (reader.failed) return command;
    const codePipe = hasCodePipe(reader.pipes);
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

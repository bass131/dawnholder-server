import { posix, win32 } from 'node:path';

const approvalLifetime = 30 * 60 * 1000;
const permissionWindow = 120 * 1000;
const mergeMethods = new Set(['--merge', '--squash', '--rebase']);
const fileTools = new Set(['Write', 'Edit', 'MultiEdit', 'NotebookEdit']);
const shellTools = new Set(['Bash', 'Monitor']);
const promptTools = new Set(['CronCreate', 'ScheduleWakeup', 'SendMessage', 'RemoteTrigger']);
// These options take their value from the next word, which is not a subcommand or refspec.
const ghValueOptions = new Set(['-R', '--repo']);
const gitValueOptions = new Set(['-C', '-c', '--git-dir', '--work-tree', '--namespace', '--super-prefix', '--config-env', '--attr-source']);
const pushValueOptions = new Set(['--repo', '--receive-pack', '--exec', '--push-option', '-o']);

const blockReasons = {
  'not-main-checkout': '메인 checkout 표식이 없다. 사용자가 준비한 메인 전용 checkout에서 병합하라.',
  subagent: '하위 에이전트 호출은 병합할 수 없다. 메인 세션이 승인 문장을 받은 뒤 직접 실행해야 한다.',
  'no-approval': '이 세션에 해당 PR의 승인 기록이 없다. 사용자가 메인 창에 「병합 승인: PR<번호> head <40자>」 한 줄을 제출해야 한다.',
  'head-mismatch': '승인 head와 명령의 head가 다르다. 현재 head를 다시 확인하고 사용자에게 새 승인 문장을 받아라.',
  expired: '승인 기록의 유효 시간 30분이 지났거나 생성 시각이 유효하지 않다. 사용자에게 새 승인 문장을 받아라.',
  'already-used': '승인 기록을 이미 사용했다. 실패한 병합도 재사용할 수 없으므로 새 승인 문장을 받아라.',
  'missing-match-head-commit': '병합 명령에 --match-head-commit <40자>가 없다. = 형태는 받지 않으므로 승인받은 전체 head를 별도 인자로 넣어라.',
  'forbidden-flag': '--auto 또는 --admin 병합은 금지된다. 자동 병합이나 관리자 우회 없이 단독 병합 명령을 사용하라.',
  'bad-form': '허용된 단독 병합 형태가 아니다. 판정된 낱말 사이 옵션·대문자·경로·.exe 명령 낱말은 받지 않는다. gh pr merge <번호> <방식 하나> --match-head-commit <40자 hex>만 사용하라.',
  'compound-command': '병합 시도가 복합 명령이나 감싼 명령 안에 있다. 다른 명령·리다이렉트·치환 없이 gh pr merge만 실행하라.',
  'non-bash-merge': 'Bash 밖 셸 도구의 병합 시도다. 병합은 Bash 도구의 단독 명령으로만 한다.',
  'api-merge': 'gh api를 통한 PR 병합·자동 병합, 브랜치 병합, main 참조 쓰기, 브랜치를 바꾸는 GraphQL mutation은 금지된다. 작업 브랜치를 PR로 올리고 메인 세션에서 승인받은 gh pr merge 단독 명령을 사용하라.',
  'push-main': 'main 목적지 또는 --all·--mirror push는 금지된다. 작업 브랜치를 push하고 PR로 통합하라.',
  'branch-lookup-failed': 'push가 가리키는 저장소의 현재 branch를 확인할 수 없다. 변수·치환이 든 경로, cd -·popd, 파이프·배경 실행 앞의 cd, GIT_DIR 같은 대입은 경로를 직접 적거나 목적지 branch를 명시해 다시 실행하고, 그 밖에는 hook 입력 cwd와 대상 저장소의 git 상태를 확인하라.',
  'protected-path': '명령·파일 도구가 보호된 병합 관문 상태 폴더를 가리킨다. 에이전트가 표식·기록을 읽거나 수정하지 마라.',
  'approval-injection': '다른 터미널이나 예약·전달 prompt에 병합 승인 문장을 넣는 입력이다. 승인 문장은 사용자가 메인 창에 직접 입력한다.',
  'suspect-words': '단독 병합 형태가 아닌 명령에 병합·main push 의심 낱말이 함께 있다. 병합은 단독 명령으로, 다른 명령은 나눠 실행하거나 문구를 파일로 넘겨라.',
  'invalid-input': 'hook 입력 또는 실행 환경을 해석하지 못했다. 유효한 hook JSON·session_id·tool_name·tool_input 객체와 Bash command 문자열을 확인하라.',
  'state-write-failed': '통과 직전 승인 사용 기록을 저장하지 못했다. 상태 폴더의 접근 권한·파일 상태를 확인한 뒤 다시 실행하라.',
};

function isObject(value) {
  return value !== null && typeof value === 'object' && !Array.isArray(value);
}

// A session is a single filename component, never a path supplied by stdin.
export function isSessionId(value) {
  return typeof value === 'string' && /^[A-Za-z0-9][A-Za-z0-9._-]{0,199}$/.test(value);
}

export function hasAgent(input) {
  return Object.hasOwn(input, 'agent_id');
}

export function denyOutput(code, detail = '') {
  return {
    hookSpecificOutput: {
      hookEventName: 'PreToolUse',
      permissionDecision: 'deny',
      permissionDecisionReason: `merge-gate:${code} ${blockReasons[code]}${detail ? ` (${detail})` : ''}`,
    },
  };
}

function blocked(code) {
  return { kind: 'decision', output: denyOutput(code) };
}

function protectedPath(value) {
  if (typeof value !== 'string') return false;
  // Commands are text, not one filesystem path: fold separators and dots without resolving '..'.
  const normalized = value.replaceAll('\\', '/').replace(/\/+/g, '/').replace(/\/(?:\.\/)+/g, '/');
  return /\.claude\/state\/merge-gate(?:\/|\b)/i.test(normalized);
}

function protectedFilePath(value, cwd) {
  if (typeof value !== 'string' || value.length === 0) return false;
  const filePath = value.replaceAll('\\', '/');
  const directory = typeof cwd === 'string' ? cwd.replaceAll('\\', '/') : '';
  const paths = /^(?:[a-z]:|\/\/)/i.test(filePath) || /^(?:[a-z]:|\/\/)/i.test(directory) ? win32 : posix;
  // Explicit join/normalize resolves relative paths without resolve() reading the process cwd.
  const normalized = (paths.isAbsolute(filePath) ? paths.normalize(filePath) : paths.join(directory, filePath))
    .replaceAll('\\', '/').toLowerCase();
  return /(?:^|\/)\.claude\/state\/merge-gate(?:\/|$)/.test(normalized);
}

function unquote(word) {
  return word.replace(/^['"]|['"]$/g, '');
}

function isCommandWord(word, name) {
  const filename = unquote(word).split(/[\\/]/).at(-1).toLowerCase();
  return filename === name || filename === name + '.exe';
}

// 읽기를 더해도 막는 형태가 줄지 않게 경계 읽기와 치환 묶음 읽기의 합으로 본다.
function shellReads(command) {
  const boundaryRead = command.replace(/\\\r?\n/g, ' ');
  let substitutionRead = boundaryRead;
  while (true) {
    const next = substitutionRead.replace(/\$\([^()]*\)|\$\{[^{}]*\}|`[^`]*`/g, () => '$SUB');
    if (next === substitutionRead) break;
    substitutionRead = next;
  }
  return [boundaryRead, substitutionRead];
}

// One read cut at ; & | ` < > ( ) { } and newline, in order: word lists and the separator tokens
// between them, so the push target reading can follow cd and parentheses. `text` keeps the part as
// written for the quote-aware pass of pushLocations.
function commandSteps(text) {
  return text.split(/([;&|`<>(){}\r\n][;&|`<>(){}\s]*)/).flatMap((part, index) => {
    if (index % 2 === 1) return [{ separators: part.match(/&&|\|\||[;&|`<>(){}\r\n]/g), hereDocument: /(?:^|[^<])<<(?!<)/.test(part) }];
    const words = part.trim().split(/\s+/).filter(Boolean).map(raw => ({ raw, value: unquote(raw) }));
    return words.length > 0 ? [{ words, text: part }] : [];
  });
}

function simpleCommands(command) {
  return shellReads(command).flatMap(commandSteps).filter(step => step.words).map(step => step.words);
}

// 닫히지 않는 따옴표는 처음 낱말만 건너뛰어 예상 밖 입력에서도 막는 쪽으로 기운다.
function quotedWordEnd(words, start) {
  let quote = null;
  for (let index = start; index < words.length; index += 1) {
    for (const character of words[index].raw) {
      if (character === quote) quote = null;
      else if (quote === null && (character === '"' || character === "'")) quote = character;
    }
    if (quote === null) return index;
  }
  return start;
}

// §4 command words: skip options and their separate values before comparing the first subcommand words.
function findSubcommands(words, executable, subcommands, valueOptions) {
  const found = [];
  for (let start = 0; start < words.length; start += 1) {
    if (!isCommandWord(words[start].value, executable)) continue;
    let part = 0;
    for (let index = start + 1; index < words.length; index += 1) {
      const word = words[index].value;
      if (word.startsWith('-')) {
        const optionEnd = quotedWordEnd(words, index);
        index = valueOptions.has(word) ? quotedWordEnd(words, optionEnd + 1) : optionEnd;
        continue;
      }
      if (word !== subcommands[part]) break;
      part += 1;
      if (part === subcommands.length) {
        found.push({ start, end: index });
        break;
      }
    }
  }
  return found;
}

function validToolInput(input) {
  return isObject(input) && isSessionId(input.session_id) &&
    typeof input.tool_name === 'string' && input.tool_name.trim().length > 0 &&
    isObject(input.tool_input) &&
    (input.tool_name !== 'Bash' || typeof input.tool_input.command === 'string');
}

// §4 conditions 1–4 precede checkout, agent and record checks.
function inspectMergeCommand(command) {
  const trimmed = command.trim();
  if (/[;&|`<>\r\n]/.test(trimmed) || trimmed.includes('$(') || /\bbash\s+-c\b/.test(trimmed)) {
    return blocked('compound-command');
  }
  const words = trimmed.split(/\s+/);
  // A merge wrapped in another command or an environment assignment cannot be a standalone invocation.
  if (!isCommandWord(words[0], 'gh')) return blocked('compound-command');
  if (words.some(word => /^--(?:auto|admin)(?:=|$)/.test(word))) return blocked('forbidden-flag');
  const headFlag = words.indexOf('--match-head-commit');
  if (headFlag < 0 || headFlag === words.length - 1) return blocked('missing-match-head-commit');
  if (words.length !== 7 || words[0] !== 'gh' || words[1] !== 'pr' || words[2] !== 'merge' ||
      !/^[1-9][0-9]*$/.test(words[3]) || !Number.isSafeInteger(Number(words[3]))) {
    return blocked('bad-form');
  }
  const flags = words.slice(4);
  const methodFirst = mergeMethods.has(flags[0]) && flags[1] === '--match-head-commit';
  const headFirst = flags[0] === '--match-head-commit' && mergeMethods.has(flags[2]);
  const head = methodFirst ? flags[2] : headFirst ? flags[1] : '';
  if (!/^[0-9a-fA-F]{40}$/.test(head)) return blocked('bad-form');
  return { kind: 'merge', pr: Number(words[3]), head: head.toLowerCase(), command };
}

// 'main' for a main destination, 'lookup' when the push takes the current branch, otherwise 'none'.
function pushDestination(words) {
  const positional = [];
  let remoteByOption = false;
  for (let index = 0; index < words.length; index += 1) {
    const word = words[index].value;
    if (word === '--all' || word === '--mirror') return 'main';
    if (word === '--repo' || word.startsWith('--repo=')) remoteByOption = true;
    if (word.startsWith('-')) {
      const optionEnd = quotedWordEnd(words, index);
      index = pushValueOptions.has(word) ? quotedWordEnd(words, optionEnd + 1) : optionEnd;
    } else positional.push(word);
  }
  const refspecs = remoteByOption ? positional : positional.slice(1);
  for (const refspec of refspecs) {
    const destination = refspec.replace(/^\+/, '').split(':').at(-1);
    if (destination === 'main' || destination === 'refs/heads/main') return 'main';
  }
  return refspecs.length === 0 || refspecs.some(refspec => /^\+?(?:HEAD|@)$/.test(refspec)) ? 'lookup' : 'none';
}

function quotesClosed(text) {
  let quote = null;
  for (const character of text) {
    if (character === quote) quote = null;
    else if (quote === null && (character === '"' || character === "'")) quote = character;
  }
  return quote === null;
}

// The shell word at `index` (one or more blank-split words when quoted). `path` is the value git can
// be handed as is, or null when the shell would still expand it; `open` marks a quote that never
// closes in this simple command.
function shellWord(words, index) {
  const end = quotedWordEnd(words, index);
  const raw = words.slice(index, end + 1).map(word => word.raw).join(' ');
  const path = raw.replace(/["']/g, '');
  const literal = path.length > 0 && !/[$`*?[\]]/.test(path) && !path.startsWith('~');
  return { path: literal ? path : null, end, open: !quotesClosed(raw) };
}

const repositoryOptions = new Set(['-C', '--git-dir', '--work-tree']);

// The git global options between `git` and `push` that pick the repository, in the order written.
// A value whose quote never closes is skipped like findSubcommands skips it: Bash would read the
// rest of the command into that value, so this push does not run as written.
function repositoryOptionsBefore(words) {
  const selection = [];
  for (let index = 0; index < words.length; index += 1) {
    const word = words[index].value;
    const [name, ...attached] = word.split('=');
    if (repositoryOptions.has(word)) {
      if (index + 1 >= words.length) return null;
      const value = shellWord(words, index + 1);
      if (value.open) {
        index += 1;
        continue;
      }
      if (!value.path) return null;
      selection.push({ option: word, path: value.path });
      index = value.end;
    } else if (name !== '-C' && repositoryOptions.has(name) && attached.length > 0) {
      const value = shellWord(words, index);
      if (value.open) continue;
      if (!value.path) return null;
      selection.push({ option: name, path: value.path.slice(name.length + 1) });
      index = value.end;
    } else {
      const optionEnd = quotedWordEnd(words, index);
      index = gitValueOptions.has(word) ? quotedWordEnd(words, optionEnd + 1) : optionEnd;
    }
  }
  return selection;
}

const directoryCommands = new Set(['cd', 'pushd', 'popd']);
const followedAfter = new Set(['&&', ';', '\n', '\r', ')']);
const unfollowedBefore = new Set(['|', '||', '<', '>']);
const listEnds = new Set([';', '\n', '\r']);
// A newline right after these continues the same list.
const joinsNextLine = new Set(['&&', '||', '|']);
// Bodies that may not run: a cd inside them never moves what comes after.
const compoundOpeners = new Set(['if', 'while', 'until', 'for', 'select', 'case']);
const compoundClosers = new Set(['fi', 'done', 'esac']);
// Separators after which an unquoted # starts a comment.
const commentAfter = new Set([';', '\n', '\r', '&', '|', '&&', '||', '(', ')']);

// The path one cd or pushd moves to, judged from its own words and the separators next to it:
// undefined without a directory word, null when the text cannot settle it (cd -, a bare cd, popd, an
// option or a second argument, a variable, substitution, glob or leading ~, a pipe, redirect or
// background next to it, a directory word that is not the first word).
function directoryChange(words, before, after) {
  const changeAt = words.findIndex(word => directoryCommands.has(word.value));
  if (changeAt < 0) return undefined;
  if (changeAt > 0 || words[0].value === 'popd' || unfollowedBefore.has(before) ||
      !(after === undefined || followedAfter.has(after))) return null;
  if (words.length < 2) return null;
  const target = shellWord(words, 1);
  if (target.open || !target.path || target.end !== words.length - 1 || /^[-+]/.test(target.path)) return null;
  return target.path;
}

// Bash quoting over one word part, from the quote it starts in: single quotes take every character,
// a backslash escapes the next character outside quotes and inside double quotes, and an unquoted #
// at a word start comments out the rest of the line. `escapes` marks a final backslash that escapes
// the separator right after the part.
function quoteAfter(text, quote, wordStart) {
  let escaped = false;
  for (const character of text) {
    const atWordStart = wordStart;
    wordStart = false;
    if (escaped) escaped = false;
    else if (quote === "'") quote = character === "'" ? null : quote;
    else if (character === '\\') escaped = true;
    else if (quote === '"') quote = character === '"' ? null : quote;
    else if (character === '"' || character === "'") quote = character;
    else if (character === '#' && atWordStart) return { quote, escapes: false, comment: true };
    else wordStart = /\s/.test(character);
  }
  return { quote, escapes: escaped, comment: false };
}

// Where each lookup push of one read runs. This is the one place that decides when cd and pushd give
// their location. Bash runs a list of commands joined by && and || from left to right, so:
// - a cd gives its path to the rest of its && chain, the commands that run only after it ran;
// - it gives the path past the end of its list (; or a newline) only when it always runs: the first
//   command of the list, or a literal cd after literal cds joined by &&, outside brace groups and
//   if/while/until/for/select/case bodies (a function body is a brace group). A cd that fails there
//   leaves the push where it was, but then the lookup of its path fails too;
// - otherwise, and after a || that follows a cd, where the push runs is unknown (null);
// - a list sent to the background with & is unknown after it when it changed directory;
// - after a here-document operator (<<), whose body lines read like commands here, every directory
//   change is unknown;
// - a subshell, $(…) or backquote scope returns to the location it started from.
// The quote-aware pass (`quotes`) reads Bash quoting too: separators inside quotes or escaped by a
// backslash are characters, a part that starts inside quotes continues the command before it and a
// directory word there makes the location unknown, and an unquoted # comments out the line. If that
// pass saw a directory word and its quotes do not close or a scope closes that never opened, it
// misread a quote (a here-document body, for one) and all its locations are unknown.
function pushLocations(steps, quotes) {
  const pushes = [];
  const scopes = [];
  let inBackquote = false;
  let underflow = false;
  let directoryWords = false;
  let quote = null;
  let escapes = false;
  let comment = false;
  let continued = false;
  let hereDocument = false;
  // chain: where the next command runs when it runs. after: where commands run once this list ends.
  // start: where the list began. leading: only literal cds joined by && so far. moved: a directory
  // change in this list. braces, compound: brace groups and compound bodies around the list.
  const listFrom = location => ({ chain: location, after: location, start: location, leading: true, moved: false });
  let state = { ...listFrom([]), braces: 0, compound: 0 };
  const unknown = () => Object.assign(state, { chain: null, after: null });
  const endList = background => Object.assign(state, listFrom(background && state.moved ? null : state.after));

  function separator(token, previous, next) {
    if (token === '(' || (token === '`' && !inBackquote)) {
      if (token === '`') inBackquote = true;
      scopes.push({ subshell: true, state });
      state = { ...listFrom(state.chain), braces: 0, compound: 0 };
    } else if (token === ')' || token === '`') {
      if (token === '`') inBackquote = false;
      // Brace groups still open inside the scope close with it.
      let entry = scopes.pop();
      while (entry && !entry.subshell) entry = scopes.pop();
      if (entry) state = { ...entry.state, leading: false };
      else {
        underflow = true;
        Object.assign(state, { chain: null, after: null, start: null });
      }
    } else if (token === '{') {
      scopes.push({ subshell: false, state });
      state = { ...listFrom(state.chain), braces: state.braces + 1, compound: state.compound };
    } else if (token === '}') {
      if (scopes.at(-1)?.subshell !== false) return;
      const inner = state;
      state = { ...scopes.pop().state, leading: false };
      // The group runs in this shell, so its end location carries out, but only as far as a cd that
      // may not have run: the rest of the && chain.
      if (JSON.stringify(inner.chain) !== JSON.stringify(state.chain)) Object.assign(state, { chain: inner.chain, after: null, moved: true });
    } else if (token === '||') {
      Object.assign(state, { chain: state.moved ? null : state.start, leading: false });
    } else if (token === '&') {
      // >&, <&, &> and |& are redirections or a pipe, not a background list.
      if (!['<', '>', '|'].includes(previous) && next !== '>') endList(true);
    } else if (listEnds.has(token)) endList(false);
  }

  function command(words, before, after) {
    const first = words[0].value;
    if (compoundOpeners.has(first)) state.compound += 1;
    else if (compoundClosers.has(first) && state.compound > 0) state.compound -= 1;
    let change = directoryChange(words, before, after);
    // Here-document body lines are text, but this reading cannot tell them from commands.
    if (change !== undefined && hereDocument) change = null;
    if (change === undefined) {
      state.leading = false;
      return;
    }
    state.moved = true;
    if (change === null || state.chain === null) {
      unknown();
      state.leading = false;
      return;
    }
    state.chain = [...state.chain, change];
    state.after = state.leading && state.braces === 0 && state.compound === 0 ? state.chain : null;
  }

  for (let index = 0; index < steps.length; index += 1) {
    const { words, text, separators } = steps[index];
    if (separators) {
      if (quote !== null) continue;
      hereDocument ||= steps[index].hereDocument;
      let tokens = separators;
      if (escapes) {
        tokens = tokens.slice(1);
        continued = tokens.length === 0;
        escapes = false;
      }
      if (comment) {
        // A comment ends at the newline only; a ; inside it is part of the comment.
        const lineEnd = tokens.findIndex(token => token === '\n' || token === '\r');
        if (lineEnd < 0) continue;
        tokens = tokens.slice(lineEnd);
        comment = false;
      }
      let joining = false;
      for (let at = 0; at < tokens.length; at += 1) {
        if (listEnds.has(tokens[at]) && joining) continue;
        joining = joinsNextLine.has(tokens[at]);
        separator(tokens[at], tokens[at - 1], tokens[at + 1]);
      }
      continue;
    }
    if (comment) continue;
    const before = steps[index - 1]?.separators.at(-1);
    const continues = quote !== null || continued;
    continued = false;
    if (quotes) ({ quote, escapes, comment } = quoteAfter(text, quote, !continues && (index === 0 || commentAfter.has(before))));
    for (const { start, end } of findSubcommands(words, 'git', ['push'], gitValueOptions)) {
      const destination = pushDestination(words.slice(end + 1));
      if (destination === 'main') return { main: true, pushes: [] };
      if (destination === 'lookup') pushes.push({ location: state.chain, options: words.slice(start + 1, end) });
    }
    const hasDirectoryWord = words.some(word => directoryCommands.has(word.value));
    directoryWords ||= hasDirectoryWord;
    if (!continues) command(words, before, steps[index + 1]?.separators[0]);
    else if (hasDirectoryWord) {
      state.moved = true;
      unknown();
    }
  }
  if (quotes && directoryWords && (quote !== null || underflow)) for (const push of pushes) push.location = null;
  return { main: false, pushes };
}

// Which repository each push reaches. A push with a main destination is { main: true }. A push that
// takes the current branch adds a lookup: the cd/pushd paths it runs under (pushLocations) as -C, then
// its own -C, --git-dir and --work-tree, in the order written. The hook hands this list to git,
// which joins relative paths itself. A lookup is null when the text does not tell the repository:
// the location is unknown, an option value would still be expanded, or GIT_DIR, GIT_WORK_TREE or
// GIT_COMMON_DIR is assigned. Each read is passed over twice, without and with quoting, and the
// lookups of all passes are kept: a quote the quote-aware pass misreads can only add lookups.
function inspectPushes(command) {
  const environmentSwitch = /(?:^|[^\w])GIT_(?:DIR|WORK_TREE|COMMON_DIR)=/.test(command);
  // Keyed by the selection: the passes usually find the same push, and each lookup starts a git process.
  const lookups = new Map();
  for (const text of shellReads(command)) {
    const steps = commandSteps(text);
    for (const quotes of [false, true]) {
      const read = pushLocations(steps, quotes);
      if (read.main) return { main: true, lookups: [] };
      for (const { location, options } of read.pushes) {
        const selection = environmentSwitch || location === null ? null : repositoryOptionsBefore(options);
        const repository = selection && [...location.map(path => ({ option: '-C', path })), ...selection];
        lookups.set(JSON.stringify(repository), repository);
      }
    }
  }
  return { main: false, lookups: [...lookups.values()] };
}

function otherCommandDecision(command) {
  if (/\borca\b/.test(command) && /\bterminal\b/.test(command) && /\bsend\b/.test(command) &&
      command.includes('병합 승인:')) return blocked('approval-injection');
  if (protectedPath(command)) return blocked('protected-path');
  return { kind: 'none' };
}

function containsApprovalText(value) {
  if (typeof value === 'string') return value.includes('병합 승인:');
  return value !== null && typeof value === 'object' && Object.values(value).some(containsApprovalText);
}

// gh api calls that move a branch without gh pr merge: the branch merge endpoint and writes to the
// main ref, plus the GraphQL mutations that merge into or rewrite a branch. A write is an explicit
// method other than GET, or fields or --input without a method, which gh sends as POST. The precise
// reading and the last net both call this on the whole command text, because the boundary read cuts
// GraphQL bodies and {owner}/{repo} placeholders apart. -f/-F of another command in the same text
// also count as fields and block. PR merge and auto-merge calls are matched apart, in preparePreToolUse.
function apiMovesBranch(command) {
  if (/\b(?:mergeBranch|updateRef|createCommitOnBranch)\b/.test(command)) return true;
  if (!/\/merges(?![\w.-])|\bgit\/refs\/heads\/main(?![\w./-])/.test(command)) return false;
  const methods = [...command.matchAll(/(?:^|[\s(])(?:-X(?:=|\s+)?|--method(?:=|\s+))["']?([A-Za-z]+)/g)]
    .map(match => match[1].toUpperCase());
  if (methods.some(method => method !== 'GET')) return true;
  // An explicit GET sends fields as query parameters, but only for its own call: in a compound
  // command the GET may belong to another call, so fields still count as a write.
  if (methods.length > 0 && !/[;&|`\r\n]|\$\(/.test(command)) return false;
  return /(?:^|[\s(])(?:-[fF]|--(?:raw-)?field(?![\w-])|--input(?![\w-]))/.test(command);
}

// 정밀 판정이 놓친 형태를 원문 낱말로 막는 안전망이며 오탐을 받아들인다.
function suspectWords(command) {
  const words = (command.match(/[A-Za-z0-9_./:@+\-]+/g) ?? []).map(word => word.toLowerCase());
  const hasGh = words.some(word => ['gh', 'gh.exe'].includes(word.split('/').at(-1)));
  if (hasGh && words.includes('merge')) return true;
  if (hasGh && words.includes('api') && (apiMovesBranch(command) || words.some(word => word.endsWith('/merge') ||
      word.includes('mergepullrequest') || word.includes('enablepullrequestautomerge')))) return true;
  return words.includes('push') && words.some(word => word === 'main' || word.endsWith(':main') ||
    word.endsWith('heads/main') || word === '--all' || word === '--mirror');
}

// Classify before touching state: ordinary commands never read approval files.
export function preparePreToolUse(input) {
  if (!validToolInput(input)) return blocked('invalid-input');
  if (fileTools.has(input.tool_name)) {
    const paths = [input.tool_input?.file_path, input.tool_input?.notebook_path];
    return paths.some(path => protectedFilePath(path, input.cwd)) ? blocked('protected-path') : { kind: 'none' };
  }
  // A scheduled or forwarded prompt can reach UserPromptSubmit, so protect every nested string value.
  if (promptTools.has(input.tool_name)) {
    return containsApprovalText(input.tool_input) ? blocked('approval-injection') : { kind: 'none' };
  }
  if (!shellTools.has(input.tool_name) || typeof input.tool_input.command !== 'string') return { kind: 'none' };
  const command = input.tool_input.command;
  const commands = simpleCommands(command);
  if (commands.some(words => findSubcommands(words, 'gh', ['pr', 'merge'], ghValueOptions).length > 0)) {
    // Only Bash has the standalone merge permission path; Monitor must never consume an approval.
    return input.tool_name === 'Monitor' ? blocked('non-bash-merge') : inspectMergeCommand(command);
  }
  if (commands.some(words => findSubcommands(words, 'gh', ['api'], ghValueOptions).length > 0) &&
      (/\bpulls\/[0-9]+\/merge\b/.test(command) || /\b(?:mergePullRequest|enablePullRequestAutoMerge)\b/.test(command) ||
       apiMovesBranch(command))) {
    return blocked('api-merge');
  }
  const push = inspectPushes(command);
  if (push.main) return blocked('push-main');
  const fallback = otherCommandDecision(command);
  if (fallback.kind === 'decision') return fallback;
  if (suspectWords(command)) return blocked('suspect-words');
  // The hook runs one lookup per entry of `lookups` (see inspectPushes) and passes the branches back.
  return push.lookups.length > 0 ? { kind: 'branch', lookups: push.lookups, fallback } : fallback;
}

// One branch per lookup, null for a lookup that failed or could not be made.
export function evaluatePushBranches(branches, fallback) {
  if (branches.includes('main')) return blocked('push-main');
  if (branches.some(branch => typeof branch !== 'string' || branch.length === 0)) return blocked('branch-lookup-failed');
  return fallback;
}

export function parseApprovalPrompt(prompt) {
  if (typeof prompt !== 'string') return null;
  const match = /^병합 승인: PR([1-9][0-9]*) head ([0-9a-fA-F]{40})$/.exec(prompt.trim());
  if (!match || !Number.isSafeInteger(Number(match[1]))) return null;
  return { pr: Number(match[1]), head: match[2].toLowerCase() };
}

function isUtcTimestamp(value) {
  return typeof value === 'string' && /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?Z$/.test(value) &&
    Number.isFinite(Date.parse(value));
}

export function isApprovalRecord(record) {
  if (!isObject(record) || record.version !== 1 || !Array.isArray(record.approvals)) return false;
  const seen = new Set();
  for (const entry of record.approvals) {
    if (!isObject(entry) || Object.keys(entry).length !== 5 ||
        !Number.isSafeInteger(entry.pr) || entry.pr <= 0 || seen.has(entry.pr) ||
        typeof entry.head !== 'string' || !/^[0-9a-f]{40}$/.test(entry.head) || !isUtcTimestamp(entry.createdAt) ||
        !(entry.usedAt === null || isUtcTimestamp(entry.usedAt)) ||
        !(entry.usedCommand === null || typeof entry.usedCommand === 'string') ||
        (entry.usedAt === null) !== (entry.usedCommand === null)) return false;
    seen.add(entry.pr);
  }
  return true;
}

export function recordApproval(record, approval, now) {
  const entries = isApprovalRecord(record) ? record.approvals : [];
  const fresh = { ...approval, createdAt: new Date(now).toISOString(), usedAt: null, usedCommand: null };
  return { version: 1, approvals: [...entries.filter(entry => entry.pr !== approval.pr), fresh] };
}

// §4 conditions 5–11. The caller must persist the returned record before printing allow.
export function evaluateMergeAttempt(attempt, { marker, subagent, record, now }) {
  if (!marker) return blocked('not-main-checkout');
  if (subagent) return blocked('subagent');
  const entry = isApprovalRecord(record) ? record.approvals.find(item => item.pr === attempt.pr) : null;
  if (!entry) return blocked('no-approval');
  if (entry.head !== attempt.head) return blocked('head-mismatch');
  const age = now - Date.parse(entry.createdAt);
  if (!Number.isFinite(age) || age < 0 || age >= approvalLifetime) return blocked('expired');
  if (entry.usedAt !== null) return blocked('already-used');
  const used = { ...entry, usedAt: new Date(now).toISOString(), usedCommand: attempt.command };
  return {
    kind: 'use',
    record: { version: 1, approvals: record.approvals.map(item => item.pr === attempt.pr ? used : item) },
    output: {
      hookSpecificOutput: {
        hookEventName: 'PreToolUse',
        permissionDecision: 'allow',
        permissionDecisionReason: `merge-gate:approved PR${attempt.pr}`,
      },
    },
  };
}

export function canRequestMergePermission(input) {
  return validToolInput(input) && input.tool_name === 'Bash' && !hasAgent(input) &&
    /\bgh\s+pr\s+merge\b/.test(input.tool_input.command) && inspectMergeCommand(input.tool_input.command).kind === 'merge';
}

export function evaluatePermissionRequest(input, record, now) {
  if (!canRequestMergePermission(input) || !isApprovalRecord(record)) return null;
  const passed = record.approvals.some(entry => {
    const age = entry.usedAt === null ? NaN : now - Date.parse(entry.usedAt);
    return entry.usedCommand === input.tool_input.command && age >= 0 && age <= permissionWindow;
  });
  // Exact command equality keeps this exception confined to the previous PreToolUse pass.
  return passed ? { hookSpecificOutput: { hookEventName: 'PermissionRequest', decision: { behavior: 'allow' } } } : null;
}

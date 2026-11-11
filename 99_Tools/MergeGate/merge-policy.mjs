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
  'push-main': 'main 목적지 또는 --all·--mirror push는 금지된다. 작업 브랜치를 push하고 PR로 통합하라. 다른 저장소에서 push하려던 것이면 cd <경로> && git push …나 git -C <경로> push …로 다시 친다. cd 뒤가 ;·줄바꿈·||이면 cd 전 위치도 조회한다.',
  'branch-lookup-failed': 'push가 가리키는 저장소의 현재 branch를 확인할 수 없다. 변수·치환이 든 경로, cd -·popd, ||·파이프·배경 실행과 엮인 cd, env -C, GIT_DIR 같은 대입은 경로를 직접 적거나 목적지 branch를 명시해 다시 실행하라. Windows에서 /tmp 같은 Git Bash 전용 경로와 이름 끝이 점·공백인 경로는 C:/… 꼴로 적는다. 따옴표·heredoc 문구가 든 commit과 push는 나눠 실행하고, push는 git -C <경로> push …나 cd <절대 경로 또는 ./경로> && git push …처럼 단순한 꼴로 친다. 공백이 이어지거나 탭이 든 경로는 cd "<경로>" && git push …로 친다. 그 밖에는 hook 입력 cwd와 대상 저장소의 git 상태를 확인하라.',
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

// The word reading of one read: cut at ; & | ` < > ( ) { } and newlines wherever they are, quotes and
// here-document bodies included, so it finds a merge or push word even where Bash reads text. Each
// word keeps the blanks before it (`gap`), so a quoted value split over words can be spelled again.
function wordLists(text) {
  return text.split(/[;&|`<>(){}\r\n]/)
    .map(part => [...part.matchAll(/(\s*)(\S+)/g)].map(([, gap, raw]) => ({ raw, value: unquote(raw), gap })))
    .filter(words => words.length > 0);
}

function simpleCommands(command) {
  return shellReads(command).flatMap(wordLists);
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

// The path a shell word hands to git, only when Bash passes it on unchanged: plain characters, '…', and
// "…" without $, ` or \. A word Bash would still expand, split or unescape (a variable, substitution,
// glob, ~, brace, backslash or separator outside quotes) or an empty one gives no certain path (null).
function certainPath(raw) {
  if (!/^(?:[^\s'"\\$`*?[\]{}~;&|<>()#!]|'[^']*'|"[^"\\$`]*")+$/.test(raw)) return null;
  const path = raw.replace(/'([^']*)'|"([^"]*)"/g, (_, single, double) => single ?? double);
  return path.length > 0 ? path : null;
}

// The shell word at `index` (one or more blank-split words when quoted). `path` is its certainPath;
// `open` marks a quote that never closes in this simple command. A quoted value over several words is
// certain only when the words give it back as written: one space between them, and no line
// continuation in the command (`folded`), which the word reading turned into a blank. Other blanks
// ("a  b", a tab) give no certain path; the word reading does not split like Bash to recover them.
function shellWord(words, index, folded) {
  const end = quotedWordEnd(words, index);
  const parts = words.slice(index, end + 1);
  const raw = parts.map(word => word.raw).join(' ');
  const asWritten = parts.length === 1 || (!folded && parts.slice(1).every(word => word.gap === ' '));
  return { path: asWritten ? certainPath(raw) : null, end, open: !quotesClosed(raw) };
}

const repositoryOptions = new Set(['-C', '--git-dir', '--work-tree']);

// The git global options between `git` and `push` that pick the repository, in the order written.
// A value whose quote never closes is skipped like findSubcommands skips it: Bash would read the
// rest of the command into that value, so this push does not run as written. `folded`: see shellWord.
function repositoryOptionsBefore(words, folded) {
  const selection = [];
  for (let index = 0; index < words.length; index += 1) {
    const word = words[index].value;
    const [name, ...attached] = word.split('=');
    if (repositoryOptions.has(word)) {
      if (index + 1 >= words.length) return null;
      const value = shellWord(words, index + 1, folded);
      if (value.open) {
        index += 1;
        continue;
      }
      if (!value.path) return null;
      selection.push({ option: word, path: value.path });
      index = value.end;
    } else if (name !== '-C' && repositoryOptions.has(name) && attached.length > 0) {
      const value = shellWord(words, index, folded);
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

// Where a lookup push runs. Without a cd, pushd, popd or env word the command cannot move, so every
// push runs at the input cwd, as before this gate read cd (mentionsLocationChange). With one, only the
// certain reading below gives locations, and only inside this closed grammar it reads like Bash:
//   words, '…' and "…" without $ ` \, the operators && || ; and newline, and ( … ) subshells;
//   cd or pushd with one path word; a push in a simple command whose first word is git.
// Anything else makes every location of the command unknown: here-documents and other redirections,
// # comments, $ ` \ outside quotes, { } braces and globs, single & or |, ((, reserved words and compound
// commands, functions, assignments, eval·builtin·export and similar commands (uncertainCommandWords),
// unbalanced parentheses. A push that another command starts (env -C, nice, xargs) is placed nowhere.
// Inside the grammar a push is looked up at every location it may run at (walkList):
// - a cd gives its path only to the rest of its && chain, the commands that run only if it succeeded;
// - past the end of its list (; or a newline) the shell is where a cd of the list's leading && chain
//   left it, or where the list started when a cd failed, so all of them are looked up. A cd later in
//   the list (after another command or a ||) makes the location past the list unknown;
// - after a || that follows a cd, and for a cd in a list that already had a ||, the location is unknown:
//   `true || cd x && git push` runs the push without the cd;
// - a relative path that CDPATH may redirect (CDPATH can be set in the shell, outside this text)
//   keeps its lookup, so a main there is still push-main, and adds an unknown location;
// - a subshell returns to the locations it started from.
// Which written paths Windows git reads as the same folder as Git Bash is the hook's (gitPath there).
const directoryCommands = new Set(['cd', 'pushd', 'popd']);

// How a word check reads the command: Bash drops a line continuation before it splits words, and
// quotes or backslashes inside a word still spell it ("c'd'", \cd). The text without dropping is kept
// too, so a backslash that only escapes another one (`a\\` before a newline) cannot hide a word.
function spelledTexts(command) {
  return [command, command.replace(/\\\r?\n/g, '')].map(text => text.replace(/['"\\]/g, ''));
}

// env is a trigger, not a walked command: `env -C <path> git push` moves the push, and walkList places
// a push behind env nowhere. Matching the word, not its options, covers every spelling of -C·--chdir;
// any case, since Windows finds env.exe for ENV too. cd stays lowercase: CD is not the builtin.
const locationWords = /(?<![\p{L}\p{N}_])(?:cd|pushd|popd|[Ee][Nn][Vv])(?![\p{L}\p{N}_])/u;
function mentionsLocationChange(command) {
  return spelledTexts(command).some(text => locationWords.test(text));
}

// Characters outside quotes that end the certain reading: expansions, escapes, comments, braces,
// globs, history, redirections and here-documents, and whitespace or controls other than space, tab
// and newline (Bash keeps them inside a word, the word reading splits at them).
const uncertainCharacter = /[\\$`#{}*?[!<>]|[^\S \t\n]|[\u0000-\u0008\u000e-\u001f\u007f]/u;

// Words ({ raw, value } with quotes removed, `from` where raw starts in the command) and operators, or
// null outside the grammar.
function certainTokens(command) {
  const tokens = [];
  let raw = '';
  let value = '';
  let from = 0;
  const endWord = () => {
    if (raw) tokens.push({ word: { raw, value, from } });
    raw = '';
    value = '';
  };
  for (let index = 0; index < command.length; index += 1) {
    const character = command[index];
    const next = command[index + 1];
    if (character === "'" || character === '"') {
      const close = command.indexOf(character, index + 1);
      if (close < 0) return null;
      const inside = command.slice(index + 1, close);
      if (character === '"' && /[\\$`]/.test(inside)) return null;
      if (!raw) from = index;
      raw += command.slice(index, close + 1);
      value += inside;
      index = close;
    } else if (character === ' ' || character === '\t') endWord();
    else if (character === '\n' || character === ';' || character === '(' || character === ')') {
      // ;; ;& end case items and (( starts arithmetic.
      if ((character === ';' && (next === ';' || next === '&')) || (character === '(' && next === '(')) return null;
      endWord();
      tokens.push({ operator: character });
    } else if (character === '&' || character === '|') {
      if (next !== character) return null;
      endWord();
      tokens.push({ operator: character + next });
      index += 1;
    } else if (uncertainCharacter.test(character)) return null;
    else {
      if (!raw) from = index;
      raw += character;
      value += character;
    }
  }
  endWord();
  return tokens;
}

// Command words that start a compound command or function, run text as code in this shell (so a cd
// inside quoted text could move it), wrap another builtin, or change how cd resolves a path.
const uncertainCommandWords = new Set([
  'if', 'then', 'else', 'elif', 'fi', 'while', 'until', 'for', 'select', 'do', 'done', 'case', 'esac', 'in',
  'function', 'coproc', 'eval', 'source', '.', 'trap', 'mapfile', 'readarray', 'fc', 'bind', 'time', 'builtin',
  'command', 'enable', 'alias', 'shopt', 'set', 'export', 'declare', 'typeset', 'local', 'readonly', 'unset',
]);
// Variables cd reads: a command that names one may set it (printf -v, read) before a relative cd.
const directoryVariables = /(?<![\w])(?:CDPATH|PWD|OLDPWD)(?![\w])/;

// program := list ((; | newline) list)*; list := command ((&& | ||) newline* command)*;
// command := words | ( program ). Null for anything else, Bash syntax errors included.
function certainProgram(tokens) {
  let at = 0;
  const isOperator = (token, ...names) => token?.operator !== undefined && names.includes(token.operator);
  const endsCommand = token => token === undefined || isOperator(token, ';', '\n', '&&', '||', ')');
  const skipNewlines = () => {
    while (isOperator(tokens[at], '\n')) at += 1;
  };

  function certainCommand(depth) {
    if (isOperator(tokens[at], '(')) {
      at += 1;
      const inner = program(depth + 1);
      if (!inner || !isOperator(tokens[at], ')')) return null;
      at += 1;
      return endsCommand(tokens[at]) ? { subshell: inner } : null;
    }
    const words = [];
    while (tokens[at]?.word) words.push(tokens[at++].word);
    if (words.length === 0 || uncertainCommandWords.has(words[0].value) ||
        /^[A-Za-z_][A-Za-z0-9_]*\+?=/.test(words[0].raw)) return null;
    return endsCommand(tokens[at]) ? { words } : null;
  }

  function list(depth) {
    const commands = [certainCommand(depth)];
    const operators = [];
    while (commands.at(-1) && isOperator(tokens[at], '&&', '||')) {
      operators.push(tokens[at].operator);
      at += 1;
      skipNewlines();
      commands.push(certainCommand(depth));
    }
    return commands.at(-1) ? { commands, operators } : null;
  }

  function program(depth) {
    if (depth > 32) return null;
    const lists = [];
    skipNewlines();
    while (tokens[at] && !isOperator(tokens[at], ')')) {
      const parsed = list(depth);
      if (!parsed) return null;
      lists.push(parsed);
      if (isOperator(tokens[at], ';', '\n')) {
        at += 1;
        skipNewlines();
      } else if (tokens[at] && !isOperator(tokens[at], ')')) return null;
    }
    return lists.length > 0 ? lists : null;
  }

  const lists = program(0);
  return lists && at === tokens.length ? lists : null;
}

// The single path of a cd or pushd; null for popd, a bare cd, an option (-, -P, +1), two arguments or
// a path certainPath does not settle.
function directoryTarget(words) {
  if (words[0].value === 'popd' || words.length !== 2) return null;
  const path = certainPath(words[1].raw);
  return path === null || /^[-+]/.test(path) ? null : path;
}

// Bash's cd searches CDPATH unless the path is absolute or starts with . or .. (absolute_pathname in
// bash); a drive path such as C:/x is absolute in Git Bash. Only the paths after the last absolute one
// matter, since that one fixes the location again.
const absolutePath = path => /^(?:\/|[A-Za-z]:)/.test(path);
const searchedInCdpath = path => !absolutePath(path) && !/^\.\.?(?:\/|$)/.test(path);
function cdpathMayMove(location) {
  return location.slice(location.findLastIndex(absolutePath) + 1).some(searchedInCdpath);
}

// A location is the cd and pushd paths that lead to it from the input cwd, in order; null is one the
// text does not tell. A push may run at a set of them. A set larger than maxLocations becomes unknown,
// which also bounds the git lookups the hook starts (one each).
const maxLocations = 8;
const unknownLocations = [null];
function locationSet(...groups) {
  const unique = new Map(groups.flat().map(location => [JSON.stringify(location), location]));
  return unique.size > maxLocations ? unknownLocations : [...unique.values()];
}

// The lists of a program run one after another, each from where the one before may leave the shell.
function walkProgram(lists, start, found) {
  lists.reduce((locations, list) => walkList(list, locations, found), start);
}

function walkList({ commands, operators }, start, found) {
  // run: where the next command may run when it runs. after: where the next list may start. leading:
  // only cds joined by && so far. moved: this list had a cd. orSeen: this list had a ||.
  let run = start;
  let after = start;
  let leading = true;
  let moved = false;
  let orSeen = false;
  commands.forEach((item, index) => {
    if (operators[index - 1] === '||') {
      if (moved) run = unknownLocations;
      orSeen = true;
      leading = false;
    }
    if (item.subshell) {
      walkProgram(item.subshell, run, found);
      leading = false;
      return;
    }
    const { words } = item;
    if (directoryCommands.has(words[0].value)) {
      moved = true;
      const target = orSeen ? null : directoryTarget(words);
      run = locationSet(run.map(location => (location === null || target === null ? null : [...location, target])));
      // A cd may fail and leave the shell where it was, so the next list keeps every earlier location.
      after = leading ? locationSet(after, run) : unknownLocations;
      return;
    }
    leading = false;
    for (const { start: gitAt, end } of findSubcommands(words, 'git', ['push'], gitValueOptions)) {
      found.pushes += 1;
      const destination = pushDestination(words.slice(end + 1));
      if (destination === 'main') found.main = true;
      if (destination !== 'lookup') continue;
      const placed = gitAt === 0 && words[0].value === 'git';
      if (placed) found.placedLookupWords.push(words[end]);
      const options = words.slice(gitAt + 1, end);
      for (const location of placed ? run : [...run, null]) {
        found.lookups.push({ location, options });
        if (location !== null && cdpathMayMove(location)) found.lookups.push({ location: null });
      }
    }
  });
  return after;
}

// The certain reading: null outside the grammar. Otherwise its lookup pushes with their locations,
// whether a push has a main destination, how many pushes it read, and the subcommand words of the
// lookup pushes it placed (for everyPushPlaced).
function certainLocations(command) {
  if ([command, ...spelledTexts(command)].some(text => directoryVariables.test(text))) return null;
  const tokens = certainTokens(command);
  const lists = tokens && certainProgram(tokens);
  if (!lists) return null;
  const found = { main: false, lookups: [], pushes: 0, placedLookupWords: [] };
  walkProgram(lists, [[]], found);
  return found;
}

// The destination of each push subcommand the word reading sees in one read, quoted text included.
function wordPushes(text) {
  return wordLists(text).flatMap(words => findSubcommands(words, 'git', ['push'], gitValueOptions)
    .map(({ end }) => pushDestination(words.slice(end + 1))));
}

// Whether the certain reading placed every lookup push the word reading finds. The counts must agree,
// and once the placed lookup pushes are blanked out the word reading must find no lookup push left: one
// left runs where the certain reading did not look (inside bash -c "…"), or the two readings disagree
// about its destination. Blanking keeps every other character where it was.
function everyPushPlaced(command, certain) {
  let rest = command;
  for (const { from, raw } of certain.placedLookupWords) {
    rest = rest.slice(0, from) + '_'.repeat(raw.length) + rest.slice(from + raw.length);
  }
  return shellReads(command).every(text => wordPushes(text).length === certain.pushes) &&
    shellReads(rest).every(text => !wordPushes(text).includes('lookup'));
}

// Which repository each push reaches. A push with a main destination is { main: true }. A push that
// takes the current branch adds one lookup per location it may run at: that location's cd/pushd paths
// as -C, then its own -C, --git-dir and --work-tree, in the order written. The hook hands this list to
// git, which joins relative paths itself. A lookup is null when the text does not tell the repository:
// the location is unknown, an option value is not a certain path, or GIT_DIR, GIT_WORK_TREE or
// GIT_COMMON_DIR is assigned.
// The word reading finds the pushes and, without a location word, looks them up at the input cwd as
// before this gate read cd. The one place that gives the input cwd up is the location-word branch
// below: the certain reading's locations replace it only when that reading placed every lookup push
// (everyPushPlaced). Otherwise a null is added. So for every lookup push the word reading sees, the
// list holds its input-cwd lookup, the certain reading's lookups of the same word, or a null: a command
// outside the grammar is never judged more loosely than at the input cwd.
function inspectPushes(command) {
  const environmentSwitch = /(?:^|[^\w])GIT_(?:DIR|WORK_TREE|COMMON_DIR)=/.test(command);
  const folded = /\\\r?\n/.test(command);
  let pushes = [];
  for (const words of simpleCommands(command)) {
    for (const { start, end } of findSubcommands(words, 'git', ['push'], gitValueOptions)) {
      const destination = pushDestination(words.slice(end + 1));
      if (destination === 'main') return { main: true, lookups: [] };
      if (destination === 'lookup') pushes.push({ location: [], options: words.slice(start + 1, end) });
    }
  }
  if (mentionsLocationChange(command)) {
    const certain = certainLocations(command);
    if (certain?.main) return { main: true, lookups: [] };
    const needed = pushes.length > 0 || certain?.lookups.length > 0;
    const placed = certain !== null && everyPushPlaced(command, certain);
    pushes = certain === null ? [] : certain.lookups;
    if (needed && !placed) pushes = [...pushes, { location: null }];
  }
  // Keyed by the selection: one push is often found twice, and each lookup starts a git process.
  const lookups = new Map();
  for (const { location, options } of pushes) {
    const selection = environmentSwitch || location === null ? null : repositoryOptionsBefore(options, folded);
    const repository = selection && [...location.map(path => ({ option: '-C', path })), ...selection];
    lookups.set(JSON.stringify(repository), repository);
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

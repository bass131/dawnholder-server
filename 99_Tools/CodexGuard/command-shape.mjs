import { commandName, readShell, UnsupportedSyntax } from './shell-command.mjs';
import { supportedCommandLimit } from './inspection-budget.mjs';

// These literal commands have known argument/data and caller-location boundaries.
// Unknown executables remain usable alone; explicit nested commands or a later protected
// command after an unknown location effect require the unsupported-form decision.
const ordinaryCommands = new Set([
  'git', 'gh', 'echo', 'printf', 'write-output', 'write-host', 'true', 'false',
  'test', '[', 'pwd', 'get-location', 'cd', 'set-location',
]);

const wrappers = new Set([
  'bash', 'sh', 'dash', 'zsh', 'ksh', 'fish', 'pwsh', 'powershell', 'cmd',
  'env', 'command', 'exec', 'eval', 'iex', 'invoke-expression',
]);
const controlWords = new Set([
  'if', 'then', 'else', 'elif', 'elseif', 'fi', 'for', 'foreach', 'while', 'until', 'do', 'done',
  'try', 'catch', 'finally', 'switch', 'function', '!', 'time', 'pushd', 'popd', 'push-location', 'pop-location',
]);

function locationArgument(words) {
  const argumentsLeft = words.slice(1);
  if (['--', '/d', '-path', '-literalpath'].includes(argumentsLeft[0]?.value.toLowerCase())) {
    argumentsLeft.shift();
  }
  if (argumentsLeft.length !== 1) return null;
  const path = argumentsLeft[0];
  if (!path.literal || !path.value || (!path.quoted && /[~*?\[]/.test(path.value)) || path.value === '-') return null;
  return path.value;
}

// S1/S2/S3 are a grammar whitelist. Every other form goes to the candidate detector;
// this module never decides whether an unsupported command would succeed or reach a branch.
export function classifyCommand(command, candidates, work) {
  let tokens;
  try {
    tokens = readShell(command, 0, work);
  } catch (error) {
    if (!(error instanceof UnsupportedSyntax)) throw error;
    return { supported: false, problem: error.message };
  }
  const commands = [];
  let words = [];
  let redirects = [];
  let bodies = 0;
  let ended = false;
  let afterAnd = false;
  let unknownLocation = false;
  let candidateIndex = 0;
  function candidateIn(word) {
    while (candidateIndex < candidates.positions.length && candidates.positions[candidateIndex] < word.start) {
      candidateIndex++;
    }
    return candidates.positions[candidateIndex] < word.end;
  }
  const unsupported = problem => ({ supported: false, problem });
  const finish = () => {
    if (!words.length) return 'missing command';
    const name = commandName(words[0].value);
    const rootCandidate = candidateIn(words[0]);
    let nestedCandidate = false;
    for (let index = 1; index < words.length; index++) {
      const word = words[index];
      const present = candidateIn(word);
      if (present && ['git', 'gh'].includes(commandName(word.value))) nestedCandidate = true;
    }
    if (!ordinaryCommands.has(name) && nestedCandidate) return 'unknown execution prefix with a protected command';
    if (unknownLocation && rootCandidate) return 'protected command after an unknown location effect';
    if (wrappers.has(name) || controlWords.has(name) || /^[A-Za-z_]\w*=/.test(words[0].value)) {
      return 'wrapper, prefix or control statement';
    }
    if (words.some(word => !word.literal || word.substitutions.length)) return 'nonliteral argument or substitution';
    let directory = null;
    if (name === 'cd' || name === 'set-location') {
      directory = locationArgument(words);
      if (directory === null) return 'cd/Set-Location requires one literal path';
    }
    if (!ordinaryCommands.has(name)) unknownLocation = true;
    commands.push({ name, argv: words.slice(1).map(word => word.value), directory });
    words = [];
    return null;
  };
  for (let index = 0; index < tokens.length; index++) {
    work?.step('shapeTokens');
    const token = tokens[index];
    if (token.kind === 'body') {
      bodies++;
      if (token.substitutions.length) return unsupported('heredoc execution substitution');
      continue;
    }
    if (token.kind === 'word') {
      if (ended) return unsupported('standalone command newline');
      words.push(token);
      afterAnd = false;
    } else if (token.kind === 'redirect') {
      const target = tokens[++index];
      if (target?.kind !== 'word' || !target.literal || target.substitutions.length) {
        return unsupported('nonliteral redirection');
      }
      redirects.push(token.value);
    } else if (token.kind === 'operator' && token.value === '&&') {
      if (ended || redirects.length) return unsupported('compound heredoc or newline');
      const problem = finish();
      if (problem) return unsupported(problem);
      if (commands.length >= supportedCommandLimit) return unsupported('command inspection budget exceeded');
      afterAnd = true;
    } else if (token.kind === 'operator' && token.value === '\n') {
      // Newlines after && are whitespace. Leading/trailing newlines do not add a command.
      if (words.length && !afterAnd) ended = true;
    } else if (token.kind === 'operator' && token.value === '&' && !words.length && !ended &&
        tokens[index + 1]?.kind === 'word') {
      // PowerShell's leading call operator invokes a literal executable (not a background job).
      continue;
    } else {
      return unsupported('operator, group or control statement outside S1/S2/S3');
    }
  }
  if (afterAnd) return unsupported('missing command after &&');
  if (redirects.length && (commands.length || !bodies || redirects.some(value => !/^(?:\d*<<-?|\d*>)$/.test(value)))) {
    return unsupported('redirection outside a single heredoc command');
  }
  if (words.length) {
    const problem = finish();
    if (problem) return unsupported(problem);
  }
  return { supported: true, commands };
}

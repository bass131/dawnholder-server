const globalValues = new Set(['-R', '--repo', '--hostname']);
const apiValues = new Set([
  ...globalValues, '-X', '--method', '-f', '--raw-field', '-F', '--field', '-H', '--header',
  '--input', '-q', '--jq', '-t', '--template', '--cache', '--preview', '-p',
]);

function optionAt(argv, index, valueOptions) {
  const argument = argv[index];
  const equal = argument.indexOf('=');
  const name = equal >= 0 ? argument.slice(0, equal) : argument;
  if (valueOptions.has(name)) {
    return { name, value: equal >= 0 ? argument.slice(equal + 1) : argv[index + 1],
      consumed: equal >= 0 ? 1 : 2 };
  }
  const short = argument.slice(0, 2);
  if (!argument.startsWith('--') && argument.length > 2 && valueOptions.has(short)) {
    return { name: short, value: argument.slice(2), consumed: 1 };
  }
  return { name: argument, consumed: 1 };
}

function commandWords(argv) {
  const words = [];
  for (let index = 0; index < argv.length; index++) {
    if (argv[index].startsWith('-')) index += optionAt(argv, index, globalValues).consumed - 1;
    else words.push({ value: argv[index], index });
    if (words.length === 2) break;
  }
  return words;
}

function hasMergeMutation(query) {
  // Strings/comments can name a mutation without executing it. Strip them before reading
  // GraphQL operation/field tokens (including aliased fields and fragment selections).
  const text = query.replace(/"""[\s\S]*?"""|"(?:\\.|[^"\\])*"|#[^\n\r]*/g, ' ');
  return /\bmutation\b/.test(text) &&
    /\b(?:mergePullRequest|enablePullRequestAutoMerge|enqueuePullRequest)\s*\(/.test(text);
}

export function evaluateGh(argv) {
  const words = commandWords(argv);
  if (words[0]?.value === 'pr' && words[1]?.value === 'merge') {
    return 'Policy deny: Codex cannot run gh pr merge (including auto-merge and --disable-auto).';
  }
  if (words[0]?.value !== 'api') return null;
  let method = null;
  let hasBody = false;
  let endpoint = null;
  const queries = [];
  for (let index = words[0].index + 1; index < argv.length; index++) {
    const argument = argv[index];
    if (!argument.startsWith('-')) {
      endpoint ??= argument;
      continue;
    }
    const option = optionAt(argv, index, apiValues);
    index += option.consumed - 1;
    if (option.name === '-X' || option.name === '--method') method = option.value?.toUpperCase();
    if (['-f', '-F', '--raw-field', '--field', '--input'].includes(option.name)) hasBody = true;
    if (['-f', '-F', '--raw-field', '--field'].includes(option.name) && option.value?.startsWith('query=')) {
      const query = option.value.slice(6);
      if (!query.startsWith('@')) queries.push(query);
    }
  }
  if (!endpoint) return null;
  let pathname = endpoint.replace(/^\//, '').split('?')[0];
  if (/^https?:\/\//.test(endpoint)) pathname = new URL(endpoint).pathname.replace(/^\//, '');
  if (/(?:^|\/)repos\/[^/]+\/[^/]+\/pulls\/[^/]+\/merge\/?$/.test(pathname) &&
      (method ?? (hasBody ? 'POST' : 'GET')) !== 'GET') {
    return 'Policy deny: Codex cannot change a pull request through the REST merge endpoint.';
  }
  if (/(?:^|\/)graphql\/?$/.test(pathname) && queries.some(hasMergeMutation)) {
    return 'Policy deny: Codex cannot merge, enable auto-merge, or enqueue a pull request through GraphQL.';
  }
  return null;
}

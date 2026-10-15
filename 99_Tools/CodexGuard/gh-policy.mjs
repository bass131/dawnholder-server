import { inspectionSlice } from './inspection-budget.mjs';

const globalValues = new Set(['-R', '--repo', '--hostname']);
export const apiValues = new Set([
  ...globalValues, '-X', '--method', '-f', '--raw-field', '-F', '--field', '-H', '--header',
  '--input', '-q', '--jq', '-t', '--template', '--cache', '--preview', '-p',
]);

export function optionAt(argv, index, valueOptions, work) {
  const argument = argv[index];
  const equal = argument.indexOf('=');
  const name = equal >= 0 ? inspectionSlice(argument, 0, equal, work, 'optionSubstringCharacters') : argument;
  if (valueOptions.has(name)) {
    return { name, value: equal >= 0 ? inspectionSlice(argument, equal + 1, undefined, work,
      'optionSubstringCharacters') : argv[index + 1],
      consumed: equal >= 0 ? 1 : 2 };
  }
  const short = inspectionSlice(argument, 0, 2, work, 'optionSubstringCharacters');
  if (!argument.startsWith('--') && argument.length > 2 && valueOptions.has(short)) {
    return { name: short, value: inspectionSlice(argument, 2, undefined, work, 'optionSubstringCharacters'), consumed: 1 };
  }
  return { name: argument, consumed: 1 };
}

export function commandOptionLength(argument, work) {
  return optionAt([argument], 0, globalValues, work).consumed;
}

// Shared with conservative candidate detection so interleaved global options have one owner.
export function commandWords(argv, startIndex = 0, work) {
  const words = [];
  for (let index = startIndex; index < argv.length; index++) {
    if (argv[index].startsWith('-')) index += commandOptionLength(argv[index], work) - 1;
    else words.push({ value: argv[index], index });
    if (words.length === 2) break;
  }
  return words;
}

export function hasMergeMutation(query) {
  // Strings/comments can name a mutation without executing it. Strip them before reading
  // GraphQL operation/field tokens (including aliased fields and fragment selections).
  const text = query.replace(/"""[\s\S]*?"""|"(?:\\.|[^"\\])*"|#[^\n\r]*/g, ' ');
  return /\bmutation\b/.test(text) &&
    /\b(?:mergePullRequest|enablePullRequestAutoMerge|enqueuePullRequest)\s*\(/.test(text);
}

function endpointKinds(pathname) {
  return {
    rest: /(?:^|\/)repos\/[^/]+\/[^/]+\/pulls\/[^/]+\/merge\/?$/.test(pathname),
    graphql: /(?:^|\/)graphql\/?$/.test(pathname),
  };
}

// The conservative source view cannot validate a URL that may only be argument data.
// Keep an explicitly written protected path even when its authority is malformed.
// Actual S1 gh calls still use URL validation in apiProtection below.
export function candidateApiEndpoint(endpoint = '') {
  const pathname = endpoint.replace(/^https?:\/\/[^/]*/, '').replace(/^\//, '').split('?')[0];
  return endpointKinds(pathname);
}

export function apiProtection(endpoint, method, hasBody, hasMutation) {
  if (!endpoint) return null;
  let pathname = endpoint.replace(/^\//, '').split('?')[0];
  if (/^https?:\/\//.test(endpoint)) pathname = new URL(endpoint).pathname.replace(/^\//, '');
  const kinds = endpointKinds(pathname);
  if (kinds.rest &&
      (method ?? (hasBody ? 'POST' : 'GET')) !== 'GET') {
    return 'Policy deny: Codex cannot change a pull request through the REST merge endpoint.';
  }
  if (kinds.graphql && hasMutation) {
    return 'Policy deny: Codex cannot merge, enable auto-merge, or enqueue a pull request through GraphQL.';
  }
  return null;
}

export function evaluateGh(argv, work) {
  const words = commandWords(argv, 0, work);
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
    const option = optionAt(argv, index, apiValues, work);
    index += option.consumed - 1;
    if (option.name === '-X' || option.name === '--method') method = option.value?.toUpperCase();
    if (['-f', '-F', '--raw-field', '--field', '--input'].includes(option.name)) hasBody = true;
    if (['-f', '-F', '--raw-field', '--field'].includes(option.name) && option.value?.startsWith('query=')) {
      const query = inspectionSlice(option.value, 6, undefined, work, 'querySubstringCharacters');
      if (!query.startsWith('@')) queries.push(query);
    }
  }
  return apiProtection(endpoint, method, hasBody, queries.some(hasMergeMutation));
}

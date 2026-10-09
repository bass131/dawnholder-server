import { readFileSync, statSync } from 'node:fs';
import { checkCandidates, inputErrorResult } from './candidate-policy.mjs';

function parseArguments(args) {
  const paths = { backlog: null, goal: null };
  for (let index = 0; index < args.length; index += 2) {
    const option = args[index];
    if (option !== '--backlog' && option !== '--goal') return null;
    const key = option.slice(2);
    const value = args[index + 1];
    if (paths[key] !== null || !value || value.startsWith('-')) return null;
    paths[key] = value.replace(/\\/g, '/');
  }
  return paths.backlog === null ? null : paths;
}

function isExistingGoal(path) {
  try {
    return statSync(path).isFile();
  } catch {
    return false;
  }
}

function main(args) {
  const paths = parseArguments(args);
  if (!paths) {
    return inputErrorResult(null, null, [{
      code: 'usage',
      path: null,
      line: null,
      message: 'Expected one --backlog path and an optional --goal path, each at most once.',
      repair: 'Run node 99_Tools/Backlog/check-candidates.mjs --backlog <path> [--goal <path>].',
    }]);
  }

  const texts = {};
  const diagnostics = [];
  for (const [key, path] of Object.entries(paths)) {
    if (path === null) continue;
    try {
      texts[key] = new TextDecoder('utf-8', { fatal: true }).decode(readFileSync(path));
    } catch (error) {
      diagnostics.push({
        code: 'unreadable-file',
        path,
        line: null,
        message: `Could not read the UTF-8 file (${error.code ?? 'unknown'}).`,
        repair: 'Supply an existing readable UTF-8 Markdown file, not a directory.',
      });
    }
  }
  if (diagnostics.length > 0) return inputErrorResult(paths.backlog, paths.goal, diagnostics);
  return checkCandidates({
    backlogText: texts.backlog,
    backlogPath: paths.backlog,
    goalText: texts.goal,
    goalPath: paths.goal,
    isExistingGoal,
  });
}

const decision = main(process.argv.slice(2));
process.stdout.write(`${JSON.stringify(decision)}\n`);
process.exitCode = decision.exitCode;

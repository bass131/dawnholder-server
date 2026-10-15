import { resolve } from 'node:path';
import { classifyCommand } from './command-shape.mjs';
import { inspectProtectedCandidates } from './protected-command.mjs';
import { createInspectionWork, InspectionLimit } from './inspection-budget.mjs';
import { evaluateGh } from './gh-policy.mjs';
import { evaluateGitPush } from './git-push-policy.mjs';

function unsupportedReason(problem) {
  return `Unsupported form: ${problem}. 보호 명령을 단순하게 다시 써라. ` +
    '예: cd <경로> && git push origin <branch>. ' +
    'This is guidance only; the guard does not rewrite or execute the command. ' +
    'Protected main/default-branch updates and PR merges remain forbidden after rewriting.';
}

export function evaluateCommand(command, cwd, diagnostics = {}) {
  const work = createInspectionWork(diagnostics.metrics, diagnostics.stepLimit);
  const candidates = inspectProtectedCandidates(command, work);
  try {
    const shape = classifyCommand(command, candidates, work);
    if (!shape.supported) return candidates.present ? unsupportedReason(shape.problem) : null;
    return evaluateSupported(shape.commands, cwd, work);
  } catch (error) {
    if (!(error instanceof InspectionLimit)) throw error;
    return candidates.present ? unsupportedReason(error.message) : null;
  }
}

function evaluateSupported(commands, cwd, work) {
  const budget = { deadline: Date.now() + 12000 };
  let directory = cwd;
  for (const instruction of commands) {
    work.step('policyArguments', instruction.argv.reduce((sum, value) => sum + value.length + 1, 0));
    // Every explicit command is a possible execution; no true/false/exit status is guessed.
    // Only an && successor receives a literal cd's successful location.
    if (instruction.directory !== null) {
      work.step('locationCharacters', directory.length + instruction.directory.length);
      directory = resolve(directory, instruction.directory);
      continue;
    }
    let reason = null;
    if (instruction.name === 'gh') reason = evaluateGh(instruction.argv, work);
    if (instruction.name === 'git') reason = evaluateGitPush(instruction.argv, directory, budget);
    work.check();
    if (reason) return reason;
  }
  return null;
}

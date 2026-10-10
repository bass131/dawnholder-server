import { readFile } from 'node:fs/promises';
import { evaluateLiveness, inputFailure } from './liveness-policy.mjs';

async function main(args) {
  const paths = [];
  let thresholdSeconds = 300;
  let thresholdSeen = false;
  for (let index = 0; index < args.length; index++) {
    const argument = args[index];
    if (argument === '--threshold-seconds') {
      const value = args[++index];
      if (thresholdSeen || !value?.trim() || !Number.isFinite(Number(value)) || Number(value) < 0) {
        return inputFailure(null, 'threshold', '--threshold-seconds', 'Expected one finite, nonnegative seconds value.',
          'Use --threshold-seconds 300 once, or omit it for the default of 300 seconds.');
      }
      thresholdSeen = true;
      thresholdSeconds = Number(value);
    } else if (!argument || argument.startsWith('-')) {
      return inputFailure(thresholdSeconds, 'arguments', argument || '$', 'Unknown option or empty file path.',
        'Run node 99_Tools/Orca/check-liveness.mjs <file>... [--threshold-seconds <n>].');
    } else {
      paths.push(argument);
    }
  }
  if (paths.length === 0) {
    return inputFailure(thresholdSeconds, 'arguments', '$', 'Expected at least one saved message file.',
      'Run node 99_Tools/Orca/check-liveness.mjs <file>... [--threshold-seconds <n>].');
  }
  const inputs = [];
  for (const path of paths) {
    let bytes;
    try {
      bytes = await readFile(path);
    } catch (error) {
      return inputFailure(thresholdSeconds, 'input-file', path, `Could not read the input file (${error.code ?? 'unknown'}).`,
        'Supply an existing readable file containing saved Orca JSON output.');
    }
    try {
      inputs.push({ path, text: new TextDecoder('utf-8', { fatal: true }).decode(bytes) });
    } catch {
      return inputFailure(thresholdSeconds, 'input-encoding', path, 'Input is not valid UTF-8.',
        'Save the original Orca JSON output with UTF-8 encoding.');
    }
  }
  return evaluateLiveness(inputs, thresholdSeconds);
}

let decision;
try {
  decision = await main(process.argv.slice(2));
} catch (error) {
  decision = inputFailure(null, 'tool-failure', '$', `The local liveness tool could not complete the evaluation: ${error.message}`,
    'Preserve the saved inputs and inspect the local tool/runtime failure.');
}
process.stdout.write(`${JSON.stringify(decision)}\n`);
process.exitCode = { within: 0, over: 1, 'input-error': 2 }[decision.status];

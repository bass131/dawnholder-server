import { readFile } from 'node:fs/promises';

function inputFailure(code, message, repair) {
  return {
    status: 'input-error',
    exitCode: 2,
    exception: null,
    diagnostics: [{ code, path: '$', message, repair }],
  };
}

async function main(args) {
  if (args.length !== 1 || !args[0] || args[0].startsWith('-')) {
    return inputFailure('arguments', 'Expected exactly one JSON input file path.',
      'Run node 99_Tools/Orca/check-message.mjs <input.json> from the repository root.');
  }

  let bytes;
  try {
    bytes = await readFile(args[0]);
  } catch (error) {
    return inputFailure('input-file', `Could not read the input file (${error.code ?? 'unknown'}).`,
      'Supply an existing readable JSON file containing one message and independently confirmed expected identities.');
  }

  let input;
  try {
    const text = new TextDecoder('utf-8', { fatal: true }).decode(bytes);
    input = JSON.parse(text);
  } catch {
    return inputFailure('input-json', 'Input is not valid UTF-8 JSON.',
      'Export valid UTF-8 JSON with message and expected objects; retain the original payload string or object.');
  }
  const { evaluateMessage } = await import('./message-policy.mjs');
  return evaluateMessage(input);
}

// Set the return status without forcing process termination or changing Orca lifecycle state.
let decision;
try {
  decision = await main(process.argv.slice(2));
} catch {
  decision = inputFailure('tool-failure', 'The local policy tool could not complete the evaluation.',
    'Preserve the input and inspect the local tool/runtime failure; use the current Orca CLI and a human source comparison.');
}
process.stdout.write(`${JSON.stringify(decision)}\n`);
process.exitCode = decision.exitCode;

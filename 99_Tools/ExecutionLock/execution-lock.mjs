import { randomUUID } from 'node:crypto';
import { realpath } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import { fault, outcome, readRequest, repositoryLocation, resultFields } from './contract.mjs';
import { createEvidence } from './evidence.mjs';
import { createObserver } from './observation.mjs';
import { runExecution } from './runner.mjs';
import { createStore } from './state-store.mjs';

// The only public execution entry. Dependencies are an in-process test seam, never request/env flags.
// Import has no state/observation/execution side effects; only the direct CLI prints JSON and sets exit.
export async function runCli(argv, dependencies = {}) {
  const context = resultFields();
  let evidence;
  let result;
  try {
    const input = await readRequest(argv);
    Object.assign(context, await repositoryLocation(input.request.repoRoot, dependencies.stateRoot));
    context.requestId = randomUUID();
    if (input.request.action === 'run') {
      evidence = await createEvidence(input.request.evidenceDir, context.requestId, input, argv, context);
    }
    const observe = createObserver(dependencies, context, evidence);
    const store = createStore(context.stateRoot, context.repositoryKey, observe, evidence);
    if (input.request.action === 'inspect') {
      const state = await store.read();
      const observation = await observe();
      result = outcome(context, 'observed', 'state-observed',
        'Read-only observation completed; this is not permission to execute.');
      result.state = state;
      result.observation = observation;
    } else {
      result = await runExecution({ execution: input.request.execution, store,
        signal: dependencies.signal, context, evidence });
    }
  } catch (error) {
    const failure = error.status ? error : fault('tool-failure', error.message ?? String(error));
    result = outcome(context, failure.status, failure.code, failure.message, failure.repair);
  }
  if (evidence) {
    try {
      await evidence.finish(result);
    } catch (error) {
      result = outcome(context, 'tool-error', 'evidence-write-failed', `Evidence finalization failed: ${error.message}`,
        'Preserve the execution directory and state; inspect which files were written and whether the bundle remains.');
    }
  }
  return result;
}

const directlyInvoked = process.argv[1]
  && await realpath(process.argv[1]).catch(() => null) === await realpath(fileURLToPath(import.meta.url));
if (directlyInvoked) {
  const controller = new AbortController();
  const cancel = () => controller.abort();
  process.on('SIGINT', cancel);
  process.on('SIGTERM', cancel);
  const result = await runCli(process.argv.slice(2), { signal: controller.signal });
  process.removeListener('SIGINT', cancel);
  process.removeListener('SIGTERM', cancel);
  process.stdout.write(`${JSON.stringify(result)}\n`);
  process.exitCode = result.exitCode;
}

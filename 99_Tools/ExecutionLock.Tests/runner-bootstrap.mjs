// Test-only process entry: `node runner-bootstrap.mjs <config.json> <request.json>`. It calls the
// public runCli of 99_Tools/ExecutionLock/execution-lock.mjs through the explicit dependencies
// seam (injected observation, isolated stateRoot, AbortSignal) so separate processes with their
// own cwd can contend for one state, and writes the returned object as one JSON line on stdout with
// its exitCode as the process exit, like the product's top-level CLI. It is never the product CLI
// and its observation is never a real Windows measurement.
//
// Config fields: label, recordDir, machinePath, stateRoot (null for the product default), barrier
// (a file whose creation releases a simultaneous start, or null) and cancelFile (its creation
// aborts the signal). The scenario's stop-all file also aborts, and a lifetime bound aborts a run
// that a broken product would otherwise keep alive.
import { existsSync, readFileSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { pathToFileURL } from 'node:url';

import { createObservation, productPath, recordEnd, recordSelf, sleep } from './lock-fixture.mjs';

const LIFETIME_MS = 150_000;
// Bootstrap failure exit, distinct from every status exit of the product contract.
const BOOTSTRAP_FAILURE = 70;

const [configPath, requestPath] = process.argv.slice(2);
const config = JSON.parse(readFileSync(configPath, 'utf8'));
recordSelf(config.recordDir, config.label);
process.on('exit', code => recordEnd(config.recordDir, config.label, code, 'bootstrap-exit'));

try {
  const { runCli } = await import(pathToFileURL(productPath).href);
  const observation = createObservation({
    recordDir: config.recordDir,
    machine: () => JSON.parse(readFileSync(config.machinePath, 'utf8')),
  });

  // The ready mark is not a process record, so the observation does not list this process twice.
  if (config.barrier) {
    writeFileSync(join(config.recordDir, `${config.label}.ready`), '');
    while (!existsSync(config.barrier)) await sleep(5);
  }

  const controller = new AbortController();
  const stopAll = join(config.recordDir, 'stop-all');
  const cancelWatch = setInterval(() => {
    if (existsSync(config.cancelFile) || existsSync(stopAll)) controller.abort();
  }, 20);
  const lifetime = setTimeout(() => controller.abort(), LIFETIME_MS);

  const dependencies = { observe: observation.observe, signal: controller.signal };
  if (config.stateRoot) dependencies.stateRoot = config.stateRoot;
  const result = await runCli([requestPath], dependencies);

  clearInterval(cancelWatch);
  clearTimeout(lifetime);
  process.stdout.write(`${JSON.stringify(result)}\n`);
  process.exitCode = result?.exitCode;
} catch (error) {
  process.stderr.write(`runner-bootstrap: ${error?.stack ?? error}\n`);
  process.exitCode = BOOTSTRAP_FAILURE;
}

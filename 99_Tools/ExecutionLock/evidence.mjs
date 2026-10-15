import { appendFile, mkdir, open, writeFile } from 'node:fs/promises';
import { join } from 'node:path';
import { sha256 } from './contract.mjs';

// One new directory per request. Open output files before admission so an unwritable destination
// cannot launch a command. Child output goes directly to files, never the wrapper's JSON stream.
export async function createEvidence(parent, requestId, input, argv, context) {
  const directory = join(parent, `run-${requestId}`);
  await mkdir(directory);
  context.evidenceDir = directory;
  await writeFile(join(directory, 'request.json'), input.bytes, { flag: 'wx' });
  await writeFile(join(directory, 'invocation.json'), JSON.stringify({
    argv, cwd: process.cwd(), requestPath: input.requestPath, requestSha256: sha256(input.bytes),
    startedAt: new Date().toISOString(), node: process.version, platform: process.platform,
    observationSource: null, repositoryCommonDir: context.repositoryCommonDir,
    repositoryKey: context.repositoryKey, stateRoot: context.stateRoot, command: input.request.execution,
  }, null, 2), { flag: 'wx' });
  let stdout;
  let stderr;
  try {
    stdout = await open(join(directory, 'stdout.txt'), 'wx');
    stderr = await open(join(directory, 'stderr.txt'), 'wx');
  } catch (error) {
    await stdout?.close();
    throw error;
  }
  return {
    directory,
    stdout,
    stderr,
    async event(type, value) {
      const row = { at: new Date().toISOString(), type, ...value };
      // Nonfinite injected facts must stay visibly invalid in raw evidence, not become JSON null.
      const text = JSON.stringify(row, (_key, item) => typeof item === 'number' && !Number.isFinite(item)
        ? { invalidNumber: String(item) } : item);
      await appendFile(join(directory, 'events.jsonl'), `${text}\n`);
    },
    async finish(result) {
      await stdout.close();
      await stderr.close();
      await writeFile(join(directory, 'result.json'), JSON.stringify(result, null, 2), { flag: 'wx' });
      await writeFile(join(directory, 'exit.txt'), `${result.exitCode}\n`, { flag: 'wx' });
    },
  };
}

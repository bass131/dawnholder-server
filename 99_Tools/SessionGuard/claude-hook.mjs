import { lstat, realpath, stat } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';

import { denyOutput, evaluateSessionCall, prepareCall } from './session-policy.mjs';
import { emptySessionState, sessionStore } from './session-store.mjs';

async function optionalStat(path, inspect) {
  try {
    return await inspect(path);
  } catch (error) {
    if (error.code === 'ENOENT' || error.code === 'ENOTDIR') return null;
    throw error;
  }
}

async function checkoutFacts(project) {
  const marker = await optionalStat(join(project, '.claude', 'state', 'merge-gate', 'main-checkout'), stat);
  const backups = join(project, '.backups');
  const link = await optionalStat(backups, lstat);
  return {
    mainCheckout: marker?.isFile() ?? false,
    backupTarget: link?.isSymbolicLink() ? await realpath(backups) : null,
  };
}

let store;
let record;
let output = null;
try {
  const chunks = [];
  for await (const chunk of process.stdin) chunks.push(chunk);
  const input = JSON.parse(new TextDecoder('utf-8', { fatal: true }).decode(Buffer.concat(chunks)));
  const call = prepareCall(input, process.env);
  if (call) {
    const project = process.env.CLAUDE_PROJECT_DIR;
    store = sessionStore(project, input.session_id);
    record = await store.read();
    const facts = await checkoutFacts(project);
    const decision = evaluateSessionCall(call, {
      ...facts,
      project,
      tempRoots: [tmpdir(), process.env.TEMP, process.env.TMP, process.env.TMPDIR, '/tmp'],
      memoWrittenAt: record.memoWrittenAt,
    });
    output = denyOutput(decision);
    if (decision.recordMemo) record.memoWrittenAt = new Date().toISOString();
    if (decision.code) {
      record.denials.push({ at: new Date().toISOString(), tool: call.tool, code: decision.code, target: decision.target });
    }
    if (decision.recordMemo || decision.code) {
      // A diagnostic save failure must not undo a policy decision already made.
      await store.write(record).catch(() => {});
    }
  }
} catch (error) {
  // This is a mistake guard, not a security boundary: a broken guard must not block every tool.
  output = null;
  if (store) {
    record ??= emptySessionState();
    record.errors.push({ at: new Date().toISOString(), message: String(error.message ?? error) });
    await store.write(record).catch(() => {});
  }
}
if (output) process.stdout.write(`${JSON.stringify(output)}\n`);
process.exitCode = 0;

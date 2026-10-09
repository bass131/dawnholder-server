import { execFileSync } from 'node:child_process';

import { approvalStore } from './approval-store.mjs';
import {
  canRequestMergePermission, denyOutput, evaluateMergeAttempt, evaluatePermissionRequest, evaluatePushBranch,
  hasAgent, isSessionId, parseApprovalPrompt, preparePreToolUse, recordApproval,
} from './merge-policy.mjs';

function currentBranch(cwd) {
  if (typeof cwd !== 'string' || cwd.trim().length === 0) return null;
  try {
    return execFileSync('git', ['symbolic-ref', '--quiet', '--short', 'HEAD'], {
      cwd, encoding: 'utf8', timeout: 5000, stdio: ['ignore', 'pipe', 'pipe'],
    }).trim() || null;
  } catch {
    return null;
  }
}

async function handleHook(input) {
  if (input.hook_event_name === 'UserPromptSubmit') {
    const approval = parseApprovalPrompt(input.prompt);
    if (!approval || !isSessionId(input.session_id)) return null;
    const store = approvalStore(process.env.CLAUDE_PROJECT_DIR, input.session_id);
    if (!await store.hasMarker()) return null;
    await store.write(recordApproval(await store.read(), approval, Date.now()));
    return null;
  }

  if (input.hook_event_name === 'PreToolUse') {
    const attempt = preparePreToolUse(input);
    if (attempt.kind === 'decision') return attempt.output;
    if (attempt.kind === 'none') return null;
    if (attempt.kind === 'branch') {
      return evaluatePushBranch(currentBranch(input.cwd), attempt.fallback).output ?? null;
    }
    const store = approvalStore(process.env.CLAUDE_PROJECT_DIR, input.session_id);
    const marker = await store.hasMarker();
    const subagent = hasAgent(input);
    const record = marker && !subagent ? await store.read() : null;
    const decision = evaluateMergeAttempt(attempt, { marker, subagent, record, now: Date.now() });
    if (decision.kind === 'use') {
      try {
        // Approval is consumed before execution and stays consumed if the command fails.
        await store.write(decision.record);
      } catch (error) {
        return denyOutput('state-write-failed', error.code ?? error.message);
      }
    }
    return decision.output;
  }

  if (input.hook_event_name === 'PermissionRequest') {
    // Avoid state I/O for unrelated commands; the pure evaluator repeats this guard for direct callers.
    if (!canRequestMergePermission(input)) return null;
    const store = approvalStore(process.env.CLAUDE_PROJECT_DIR, input.session_id);
    if (!await store.hasMarker()) return null;
    return evaluatePermissionRequest(input, await store.read(), Date.now());
  }
  return null;
}

let input;
let output;
try {
  const chunks = [];
  for await (const chunk of process.stdin) chunks.push(chunk);
  input = JSON.parse(new TextDecoder('utf-8', { fatal: true }).decode(Buffer.concat(chunks)));
  if (input === null || typeof input !== 'object' || Array.isArray(input)) throw new Error('hook JSON은 객체여야 한다.');
  output = await handleHook(input);
} catch (error) {
  // Prompts must continue; PermissionRequest falls back to the normal confirmation flow.
  const event = input?.hook_event_name;
  output = event === undefined || event === 'PreToolUse' ? denyOutput('invalid-input', error.code ?? error.message) : null;
}
if (output) process.stdout.write(`${JSON.stringify(output)}\n`);
process.exitCode = 0;

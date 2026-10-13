import { evaluateCommand } from './command-policy.mjs';

function object(value) {
  return value !== null && typeof value === 'object' && !Array.isArray(value);
}

function handleHook(input) {
  if (!object(input)) throw new Error('stdin JSON must be an object');
  if (typeof input.hook_event_name !== 'string') throw new Error('hook_event_name must be a string');
  if (input.hook_event_name !== 'PreToolUse') return null;
  if (typeof input.tool_name !== 'string') throw new Error('tool_name must be a string');
  if (input.tool_name !== 'Bash') return null;
  if (!object(input.tool_input) || typeof input.tool_input.command !== 'string') {
    throw new Error('Bash tool_input.command must be a string');
  }
  if (typeof input.cwd !== 'string' || !input.cwd.trim()) throw new Error('Bash cwd must be a nonempty string');
  return evaluateCommand(input.tool_input.command, input.cwd);
}

let reason;
try {
  const chunks = [];
  let bytes = 0;
  for await (const chunk of process.stdin) {
    bytes += chunk.length;
    if (bytes > 1024 * 1024) throw new Error('hook input exceeds 1 MiB');
    chunks.push(chunk);
  }
  const text = new TextDecoder('utf-8', { fatal: true }).decode(Buffer.concat(chunks));
  reason = handleHook(JSON.parse(text));
} catch (error) {
  reason = `Error deny: CodexGuard could not inspect this command: ${error.message}. ` +
    'Fix the input/local state and retry.';
}

// No decision on a kept call: Codex retains its normal permission handling. A caught error
// uses the same supported deny envelope, but never masquerades as a policy match.
if (reason) {
  process.stdout.write(`${JSON.stringify({ hookSpecificOutput: {
    hookEventName: 'PreToolUse', permissionDecision: 'deny', permissionDecisionReason: reason,
  } })}\n`);
}

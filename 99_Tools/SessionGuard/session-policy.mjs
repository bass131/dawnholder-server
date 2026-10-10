import { isSessionId } from '../MergeGate/merge-policy.mjs';
import {
  expandVariables, isMemoPath, isNullOutput, isWithin, normalizePath, resolveDestination,
} from './path-policy.mjs';
import { commandName, readShellCommand } from './shell-command.mjs';

const writeTools = new Set(['Write', 'Edit', 'MultiEdit', 'NotebookEdit']);
const readOnlyCommands = new Set([
  'inbox', 'run-current', 'run-list', 'run-show', 'task-list', 'worker-show', 'worker-read', 'worker-list',
  'dispatch-show', 'gate-list', 'request-show',
]);
const repairs = {
  'mailbox-output-loss': '우편함 명령의 출력이 버려지거나 잘릴 수 있다. 출력을 .backups/verification/<goal>/ 아래 파일로 저장하고 따로 읽는다. 대기는 Bash run_in_background로 연다.',
  'write-outside-checkout': '쓰기 목적지가 허용 뿌리 밖이다. .backups/verification/<goal>/ 아래 근거 파일로 쓴다.',
  'temp-write': '쓰기 목적지가 checkout 밖 임시 폴더다. .backups/verification/<goal>/ 아래 근거 파일로 쓴다.',
  'memo-first': '맥락 메모보다 앞선 파일 쓰기다. 이 세션의 맥락 메모(.backups/verification/<goal>/…context….md)를 먼저 쓴다. 메모의 시각은 시계 출력을 메모 안에 그대로 적는다.',
};

const isObject = value => value !== null && typeof value === 'object' && !Array.isArray(value);

export function prepareCall(input, environment) {
  if (!isObject(input) || input.hook_event_name !== 'PreToolUse') return null;
  if (input.tool_name !== 'Bash' && !writeTools.has(input.tool_name)) return null;
  if (!isSessionId(input.session_id) || !isObject(input.tool_input)) throw new Error('도구 입력 또는 session_id가 유효하지 않다.');
  const project = environment.CLAUDE_PROJECT_DIR;
  if (typeof project !== 'string' || !project.trim()) throw new Error('CLAUDE_PROJECT_DIR가 없다.');
  const cwd = input.cwd ?? project;
  if (typeof cwd !== 'string' || !cwd.trim()) throw new Error('cwd가 유효하지 않다.');
  if (input.tool_name === 'Bash') {
    if (typeof input.tool_input.command !== 'string') throw new Error('Bash command는 문자열이어야 한다.');
    return { ...readShellCommand(input.tool_input.command, environment), cwd, tool: input.tool_name };
  }
  const path = input.tool_name === 'NotebookEdit' ? input.tool_input.notebook_path : input.tool_input.file_path;
  if (typeof path !== 'string' || !path.trim()) throw new Error('쓰기 도구의 경로가 유효하지 않다.');
  return { commands: [], writes: [expandVariables(path, environment)], cwd, tool: input.tool_name };
}

function mailboxLoss(commands) {
  for (const command of commands) {
    const args = command.argv.map(arg => arg.value);
    if (commandName(args[0]) !== 'orca' || args[1] !== 'orchestration' || !args[2]) continue;
    if (readOnlyCommands.has(args[2]) || args.includes('--help')) continue;
    if (args[2] === 'check' && (args.includes('--peek') || args.includes('--all'))) continue;
    const discardsStdout = command.redirects.some(({ operator, destination }) =>
      /^(?:&>>?|1?(?:>>?|>\|))$/.test(operator) && !destination.unresolved && isNullOutput(destination.value));
    const piped = command.end === '|' || command.end === '|&';
    const next = command.next;
    const lastTee = next && commandName(next.argv[0]?.value) === 'tee' && next.end !== '|' && next.end !== '|&';
    let last = command;
    while (last.next) last = last.next;
    if (discardsStdout || last.end === '&' || (piped && !lastTee)) return args.join(' ');
  }
  return null;
}

export function evaluateSessionCall(call, { project, backupTarget, tempRoots, mainCheckout, memoWrittenAt }) {
  const lostCommand = mailboxLoss(call.commands);
  if (lostCommand !== null) return { code: 'mailbox-output-loss', target: lostCommand };
  if (mainCheckout) return {};

  const allowedRoots = [project, backupTarget].filter(Boolean).map(root => normalizePath(root, call.cwd));
  const temporaryRoots = tempRoots.filter(Boolean).map(root => normalizePath(root, call.cwd));
  const destinations = call.writes.map(write => ({ ...write, path: resolveDestination(write, call.cwd) }));
  for (const destination of destinations) {
    if (destination.path === null) continue;
    // R-5 puts TEMP inside the checkout: the allowed roots must win over temporary roots.
    if (allowedRoots.some(root => isWithin(destination.path, root))) continue;
    if (call.tool === 'Bash') return { code: 'write-outside-checkout', target: destination.value };
    if (temporaryRoots.some(root => isWithin(destination.path, root))) return { code: 'temp-write', target: destination.value };
  }

  const first = destinations[0];
  if (!memoWrittenAt && first) {
    const memoRoots = [`${project}/.backups/verification`];
    if (backupTarget) memoRoots.push(`${backupTarget}/verification`);
    if (isMemoPath(first.path, memoRoots.map(root => normalizePath(root, call.cwd)))) return { recordMemo: true };
    return { code: 'memo-first', target: first.value };
  }
  return {};
}

export function denyOutput(decision) {
  if (!decision.code) return null;
  return {
    hookSpecificOutput: {
      hookEventName: 'PreToolUse',
      permissionDecision: 'deny',
      permissionDecisionReason: `session-guard:${decision.code} ${repairs[decision.code]} 대상: ${decision.target ?? ''}`,
    },
  };
}

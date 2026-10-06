const approvalLifetime = 30 * 60 * 1000;
const permissionWindow = 120 * 1000;
const mergeMethods = new Set(['--merge', '--squash', '--rebase']);
const fileTools = new Set(['Write', 'Edit', 'MultiEdit', 'NotebookEdit']);

const blockReasons = {
  'not-main-checkout': '메인 checkout 표식이 없다. 사용자가 준비한 메인 전용 checkout에서 병합하라.',
  subagent: '하위 에이전트 호출은 병합할 수 없다. 메인 세션이 승인 문장을 받은 뒤 직접 실행해야 한다.',
  'no-approval': '이 세션에 해당 PR의 승인 기록이 없다. 사용자가 메인 창에 「병합 승인: PR<번호> head <40자>」 한 줄을 제출해야 한다.',
  'head-mismatch': '승인 head와 명령의 head가 다르다. 현재 head를 다시 확인하고 사용자에게 새 승인 문장을 받아라.',
  expired: '승인 기록의 유효 시간 30분이 지났거나 생성 시각이 유효하지 않다. 사용자에게 새 승인 문장을 받아라.',
  'already-used': '승인 기록을 이미 사용했다. 실패한 병합도 재사용할 수 없으므로 새 승인 문장을 받아라.',
  'missing-match-head-commit': '병합 명령에 --match-head-commit <40자>가 없다. 승인받은 전체 head를 인자로 넣어라.',
  'forbidden-flag': '--auto 또는 --admin 병합은 금지된다. 자동 병합이나 관리자 우회 없이 단독 병합 명령을 사용하라.',
  'bad-form': '허용된 단독 병합 형태가 아니다. gh pr merge <번호> <방식 하나> --match-head-commit <40자 hex>만 사용하라.',
  'compound-command': '병합 시도가 복합 명령이나 감싼 명령 안에 있다. 다른 명령·리다이렉트·치환 없이 gh pr merge만 실행하라.',
  'api-merge': 'gh api를 통한 PR 병합·자동 병합은 금지된다. 메인 세션에서 승인받은 gh pr merge 단독 명령을 사용하라.',
  'push-main': 'main 목적지 또는 --all·--mirror push는 금지된다. 작업 브랜치를 push하고 PR로 통합하라.',
  'branch-lookup-failed': 'push의 현재 branch를 확인할 수 없다. hook 입력 cwd의 저장소와 git 상태를 확인하라.',
  'protected-path': '명령·파일 도구가 보호된 병합 관문 상태 폴더를 가리킨다. 에이전트가 표식·기록을 읽거나 수정하지 마라.',
  'approval-injection': '다른 터미널에 병합 승인 문장을 넣는 명령이다. 사용자가 메인 창에 승인 문장을 직접 제출해야 한다.',
  'invalid-input': 'hook 입력 또는 실행 환경을 해석하지 못했다. 유효한 hook JSON·session_id·tool_name과 Bash command 문자열을 확인하라.',
  'state-write-failed': '통과 직전 승인 사용 기록을 저장하지 못했다. 상태 폴더의 접근 권한·파일 상태를 확인한 뒤 다시 실행하라.',
};

function isObject(value) {
  return value !== null && typeof value === 'object' && !Array.isArray(value);
}

// A session is a single filename component, never a path supplied by stdin.
export function isSessionId(value) {
  return typeof value === 'string' && /^[A-Za-z0-9][A-Za-z0-9._-]{0,199}$/.test(value);
}

export function hasAgent(input) {
  return Object.hasOwn(input, 'agent_id');
}

export function denyOutput(code, detail = '') {
  return {
    hookSpecificOutput: {
      hookEventName: 'PreToolUse',
      permissionDecision: 'deny',
      permissionDecisionReason: `merge-gate:${code} ${blockReasons[code]}${detail ? ` (${detail})` : ''}`,
    },
  };
}

function blocked(code) {
  return { kind: 'decision', output: denyOutput(code) };
}

function protectedPath(value) {
  return typeof value === 'string' && /\.claude\/state\/merge-gate(?:\/|\b)/i.test(value.replaceAll('\\', '/'));
}

function validToolInput(input) {
  return isObject(input) && isSessionId(input.session_id) &&
    typeof input.tool_name === 'string' && input.tool_name.trim().length > 0 &&
    (input.tool_name !== 'Bash' || (isObject(input.tool_input) && typeof input.tool_input.command === 'string'));
}

// §4 conditions 1–4 precede checkout, agent and record checks.
function inspectMergeCommand(command) {
  const trimmed = command.trim();
  if (/[;&|`<>\r\n]/.test(trimmed) || trimmed.includes('$(') ||
      !/^gh\s+pr\s+merge\b/.test(trimmed) || /\bbash\s+-c\b/.test(trimmed)) {
    return blocked('compound-command');
  }
  const words = trimmed.split(/\s+/);
  if (words.some(word => /^--(?:auto|admin)(?:=|$)/.test(word))) return blocked('forbidden-flag');
  const headFlag = words.indexOf('--match-head-commit');
  if (headFlag < 0 || headFlag === words.length - 1) return blocked('missing-match-head-commit');
  if (words.length !== 7 || !/^[1-9][0-9]*$/.test(words[3]) || !Number.isSafeInteger(Number(words[3]))) {
    return blocked('bad-form');
  }
  const flags = words.slice(4);
  const methodFirst = mergeMethods.has(flags[0]) && flags[1] === '--match-head-commit';
  const headFirst = flags[0] === '--match-head-commit' && mergeMethods.has(flags[2]);
  const head = methodFirst ? flags[2] : headFirst ? flags[1] : '';
  if (!/^[0-9a-fA-F]{40}$/.test(head)) return blocked('bad-form');
  return { kind: 'merge', pr: Number(words[3]), head: head.toLowerCase(), command };
}

function inspectPushes(command) {
  let branchNeeded = false;
  for (const match of command.matchAll(/\bgit\s+push\b/g)) {
    const tail = command.slice(match.index + match[0].length).split(/[;&|`<>\r\n]/, 1)[0];
    const words = tail.trim().split(/\s+/).filter(Boolean).map(word => word.replace(/^['"]|['"]$/g, ''));
    const positional = [];
    let remoteByOption = false;
    for (let index = 0; index < words.length; index += 1) {
      const word = words[index];
      if (word === '--all' || word === '--mirror') return 'main';
      if (word === '--repo' || word.startsWith('--repo=')) remoteByOption = true;
      if (['--repo', '--receive-pack', '--exec', '--push-option', '-o'].includes(word)) index += 1;
      else if (!word.startsWith('-')) positional.push(word);
    }
    const refspecs = remoteByOption ? positional : positional.slice(1);
    for (const refspec of refspecs) {
      const destination = refspec.replace(/^\+/, '').split(':').at(-1);
      if (destination === 'main' || destination === 'refs/heads/main') return 'main';
    }
    if (refspecs.length === 0 || refspecs.some(refspec => /^\+?HEAD$/.test(refspec))) branchNeeded = true;
  }
  return branchNeeded ? 'branch' : 'none';
}

function otherCommandDecision(command) {
  if (/\borca\b/.test(command) && /\bterminal\b/.test(command) && /\bsend\b/.test(command) &&
      command.includes('병합 승인:')) return blocked('approval-injection');
  if (protectedPath(command)) return blocked('protected-path');
  return { kind: 'none' };
}

// Classify before touching state: ordinary commands never read approval files.
export function preparePreToolUse(input) {
  if (!validToolInput(input)) return blocked('invalid-input');
  if (fileTools.has(input.tool_name)) {
    const paths = [input.tool_input?.file_path, input.tool_input?.notebook_path];
    return paths.some(protectedPath) ? blocked('protected-path') : { kind: 'none' };
  }
  if (input.tool_name !== 'Bash') return { kind: 'none' };
  const command = input.tool_input.command;
  if (/\bgh\s+pr\s+merge\b/.test(command)) return inspectMergeCommand(command);
  if (/\bgh\s+api\b/.test(command) &&
      (/\bpulls\/[0-9]+\/merge\b/.test(command) || /\b(?:mergePullRequest|enablePullRequestAutoMerge)\b/.test(command))) {
    return blocked('api-merge');
  }
  const push = inspectPushes(command);
  if (push === 'main') return blocked('push-main');
  const fallback = otherCommandDecision(command);
  return push === 'branch' ? { kind: 'branch', fallback } : fallback;
}

export function evaluatePushBranch(branch, fallback) {
  if (typeof branch !== 'string' || branch.length === 0) return blocked('branch-lookup-failed');
  return branch === 'main' ? blocked('push-main') : fallback;
}

export function parseApprovalPrompt(prompt) {
  if (typeof prompt !== 'string') return null;
  const match = /^병합 승인: PR([1-9][0-9]*) head ([0-9a-fA-F]{40})$/.exec(prompt.trim());
  if (!match || !Number.isSafeInteger(Number(match[1]))) return null;
  return { pr: Number(match[1]), head: match[2].toLowerCase() };
}

function isUtcTimestamp(value) {
  return typeof value === 'string' && /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?Z$/.test(value) &&
    Number.isFinite(Date.parse(value));
}

export function isApprovalRecord(record) {
  if (!isObject(record) || record.version !== 1 || !Array.isArray(record.approvals)) return false;
  const seen = new Set();
  for (const entry of record.approvals) {
    if (!isObject(entry) || Object.keys(entry).length !== 5 ||
        !Number.isSafeInteger(entry.pr) || entry.pr <= 0 || seen.has(entry.pr) ||
        typeof entry.head !== 'string' || !/^[0-9a-f]{40}$/.test(entry.head) || !isUtcTimestamp(entry.createdAt) ||
        !(entry.usedAt === null || isUtcTimestamp(entry.usedAt)) ||
        !(entry.usedCommand === null || typeof entry.usedCommand === 'string') ||
        (entry.usedAt === null) !== (entry.usedCommand === null)) return false;
    seen.add(entry.pr);
  }
  return true;
}

export function recordApproval(record, approval, now) {
  const entries = isApprovalRecord(record) ? record.approvals : [];
  const fresh = { ...approval, createdAt: new Date(now).toISOString(), usedAt: null, usedCommand: null };
  return { version: 1, approvals: [...entries.filter(entry => entry.pr !== approval.pr), fresh] };
}

// §4 conditions 5–11. The caller must persist the returned record before printing allow.
export function evaluateMergeAttempt(attempt, { marker, subagent, record, now }) {
  if (!marker) return blocked('not-main-checkout');
  if (subagent) return blocked('subagent');
  const entry = isApprovalRecord(record) ? record.approvals.find(item => item.pr === attempt.pr) : null;
  if (!entry) return blocked('no-approval');
  if (entry.head !== attempt.head) return blocked('head-mismatch');
  const age = now - Date.parse(entry.createdAt);
  if (!Number.isFinite(age) || age < 0 || age >= approvalLifetime) return blocked('expired');
  if (entry.usedAt !== null) return blocked('already-used');
  const used = { ...entry, usedAt: new Date(now).toISOString(), usedCommand: attempt.command };
  return {
    kind: 'use',
    record: { version: 1, approvals: record.approvals.map(item => item.pr === attempt.pr ? used : item) },
    output: {
      hookSpecificOutput: {
        hookEventName: 'PreToolUse',
        permissionDecision: 'allow',
        permissionDecisionReason: `merge-gate:approved PR${attempt.pr}`,
      },
    },
  };
}

export function canRequestMergePermission(input) {
  return validToolInput(input) && input.tool_name === 'Bash' && !hasAgent(input) &&
    /\bgh\s+pr\s+merge\b/.test(input.tool_input.command) && inspectMergeCommand(input.tool_input.command).kind === 'merge';
}

export function evaluatePermissionRequest(input, record, now) {
  if (!canRequestMergePermission(input) || !isApprovalRecord(record)) return null;
  const passed = record.approvals.some(entry => {
    const age = entry.usedAt === null ? NaN : now - Date.parse(entry.usedAt);
    return entry.usedCommand === input.tool_input.command && age >= 0 && age <= permissionWindow;
  });
  // Exact command equality keeps this exception confined to the previous PreToolUse pass.
  return passed ? { hookSpecificOutput: { hookEventName: 'PermissionRequest', decision: { behavior: 'allow' } } } : null;
}

// Shared fixture for the independent session guard regressions. It builds a throwaway project
// folder that is handed to the hook as CLAUDE_PROJECT_DIR, gives the hook process its own TEMP,
// TMP, TMPDIR and HOME, runs 99_Tools/SessionGuard/claude-hook.mjs as a black box and reads its
// decision and session state. It holds no judgement of the guard: every expected decision and
// code is written in the test files from goal 「설계 / 세션 쓰기 가드」
// (01_Phases/goals/2026-10-10-operating-tool-guards/goal.md at 852f88ca) and the Rules lead's
// test contract v1 with its supplements 1 and 2 (E/tdd/work/ask1-answer.txt, ask2-answer.txt).
//
// Input shapes copy the fields of the PreToolUse inputs observed in the merge gate design probe
// (E of 2026-10-06-merge-gate-canon-refresh, design-probe/raw/hooklog/r5.jsonl, Claude Code
// 2.1.291): session_id, transcript_path, cwd, permission_mode, hook_event_name, tool_name,
// tool_input, tool_use_id, and agent_id with agent_type for a subagent call.
import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { existsSync } from 'node:fs';
import { mkdir, mkdtemp, readFile, readdir, rm, symlink, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { dirname, join, relative } from 'node:path';
import { fileURLToPath } from 'node:url';

export const repositoryRoot = fileURLToPath(new URL('../../', import.meta.url));
// A folder outside every throwaway project and outside every temp root of the hook: the parent of
// this checkout. On a Linux runner the throwaway folders live under /tmp, which is itself a temp
// root (goal line 104), so `outside` cannot stand for "outside the checkout but not temp". Paths
// here are only handed to the hook as Write inputs; nothing is created there.
export const notTempFolder = join(dirname(repositoryRoot.replace(/[\\/]+$/, '')), 'session-guard-never-written');
export const hookPath = join(repositoryRoot, '99_Tools', 'SessionGuard', 'claude-hook.mjs');

export const SESSION = '5e551011-0000-4000-8000-0000000000a1';
export const OTHER_SESSION = '5e551011-0000-4000-8000-0000000000a2';
// The subagent identity observed in r5.
export const AGENT = { agent_id: 'a3a3f93dba75a3113', agent_type: 'general-purpose' };

// The evidence folder of a goal inside the throwaway project, as a command would spell it.
export const EVIDENCE = '.backups/verification/2026-10-10-operating-tool-guards';
export const MEMO = `${EVIDENCE}/tdd/context.md`;
// A variable name no test environment sets; runHook removes it from the hook environment.
export const UNSET_VARIABLE = 'SESSION_GUARD_TEST_UNSET';

export const isWindows = process.platform === 'win32';
export const toSlash = path => path.split('\\').join('/');

// Git Bash drive form of a Windows path: C:\a\b -> /c/a/b.
export function driveForm(path) {
  const slashed = toSlash(path);
  const match = /^([A-Za-z]):\/(.*)$/.exec(slashed);
  assert.ok(match, `driveForm needs a drive path: ${path}`);
  return `/${match[1].toLowerCase()}/${match[2]}`;
}

// One owned folder per test, removed when the test ends.
// - `project` is CLAUDE_PROJECT_DIR with an evidence folder under `.backups/verification/`.
// - `temp` is TEMP/TMP/TMPDIR of the hook process: outside the project by default, or under the
//   project's `.backups/tmp/` like the TEMP that R-5 gives a Claude worker (tempInsideProject).
//   separateTempRoots gives TEMP, TMP and TMPDIR three different folders.
// - `home` is HOME of the hook process, outside the project unless homeInsideProject.
// - marker writes the merge gate main checkout marker that turns rules 2 and 3 off.
// - junction makes `.backups` a junction (Windows) or directory symlink (elsewhere) to
//   `<root>/shared-backups`, the core-active layout.
export async function createWorkspace(t, options = {}) {
  const {
    marker = false, tempInsideProject = false, separateTempRoots = false, homeInsideProject = false, junction = false,
  } = options;
  const root = await mkdtemp(join(tmpdir(), 'session-guard-'));
  t.after(() => rm(root, { recursive: true, force: true }));
  const project = join(root, 'project');
  await mkdir(project);

  let linkTarget = null;
  if (junction) {
    linkTarget = join(root, 'shared-backups');
    await mkdir(join(linkTarget, 'verification'), { recursive: true });
    await symlink(linkTarget, join(project, '.backups'), isWindows ? 'junction' : 'dir');
  }
  const evidence = join(project, ...EVIDENCE.split('/'));
  await mkdir(evidence, { recursive: true });

  const temp = tempInsideProject ? join(project, '.backups', 'tmp', 'sgt') : join(root, 'temp');
  await mkdir(temp, { recursive: true });
  const temps = { TEMP: temp, TMP: temp, TMPDIR: temp };
  if (separateTempRoots) {
    for (const name of ['TMP', 'TMPDIR']) {
      temps[name] = join(root, `temp-${name.toLowerCase()}`);
      await mkdir(temps[name]);
    }
  }
  const home = homeInsideProject ? join(evidence, 'home') : join(root, 'home');
  await mkdir(home, { recursive: true });
  const outside = join(root, 'outside');
  await mkdir(outside);

  if (marker) {
    await mkdir(join(project, '.claude', 'state', 'merge-gate'), { recursive: true });
    await writeFile(join(project, '.claude', 'state', 'merge-gate', 'main-checkout'), '');
  }
  return { root, project, evidence, temp, temps, home, outside, linkTarget };
}

export const stateDirectory = project => join(project, '.claude', 'state', 'session-guard');
export const statePath = (project, session = SESSION) => join(stateDirectory(project), `${session}.json`);

export async function readState(project, session = SESSION) {
  const path = statePath(project, session);
  if (!existsSync(path)) return null;
  return JSON.parse(await readFile(path, 'utf8'));
}

// Every file under `directory`, as sorted `/`-separated relative paths.
export async function listFiles(directory) {
  if (!existsSync(directory)) return [];
  const found = [];
  const walk = async folder => {
    for (const entry of await readdir(folder, { withFileTypes: true })) {
      const path = join(folder, entry.name);
      if (entry.isDirectory()) await walk(path);
      else found.push(toSlash(relative(directory, path)));
    }
  };
  await walk(directory);
  return found.sort();
}

// Hook inputs. `cwd` defaults to the project folder, as in every observed session; `cwd: null`
// leaves the field out.
function common(ws, { session = SESSION, cwd = ws.project, agent = false } = {}) {
  const input = {
    session_id: session,
    transcript_path: join(ws.home, '.claude', 'projects', 'session-guard-test', `${session}.jsonl`),
    cwd,
    permission_mode: 'auto',
  };
  if (cwd === null) delete input.cwd;
  return agent ? { ...input, ...AGENT } : input;
}

export function bashInput(ws, command, { runInBackground = false, ...options } = {}) {
  const toolInput = { command, description: 'session guard regression input' };
  if (runInBackground) toolInput.run_in_background = true;
  return {
    ...common(ws, options),
    hook_event_name: 'PreToolUse',
    tool_name: 'Bash',
    tool_input: toolInput,
    tool_use_id: 'toolu_session_guard_regression',
  };
}

const fileToolInputs = {
  Write: path => ({ file_path: path, content: 'x' }),
  Edit: path => ({ file_path: path, old_string: 'a', new_string: 'b' }),
  MultiEdit: path => ({ file_path: path, edits: [{ old_string: 'a', new_string: 'b' }] }),
  NotebookEdit: path => ({ notebook_path: path, new_source: 'x' }),
};

export const fileToolInput = (ws, toolName, path, options) => toolInput(ws, toolName, fileToolInputs[toolName](path), options);

export const toolInput = (ws, toolName, input, options) => ({
  ...common(ws, options),
  hook_event_name: 'PreToolUse',
  tool_name: toolName,
  tool_input: input,
  tool_use_id: 'toolu_session_guard_regression',
});

// Runs the real entry like a registered command: `node <entry>` with no arguments, the hook JSON
// on stdin and the environment of the workspace. Inherited copies of the variables the workspace
// owns are dropped first, case-insensitively, because Windows environment names ignore case.
export function runHook(ws, input, { raw, env = {} } = {}) {
  const owned = { CLAUDE_PROJECT_DIR: ws.project, ...ws.temps, HOME: ws.home, ...env };
  const ownedNames = new Set([...Object.keys(owned), UNSET_VARIABLE, 'NODE_TEST_CONTEXT'].map(name => name.toUpperCase()));
  const environment = {};
  for (const [name, value] of Object.entries(process.env)) {
    if (!ownedNames.has(name.toUpperCase())) environment[name] = value;
  }
  Object.assign(environment, owned);
  const result = spawnSync(process.execPath, [hookPath], {
    cwd: ws.project,
    input: raw ?? JSON.stringify(input),
    encoding: 'utf8',
    env: environment,
    timeout: 30000,
  });
  return { exit: result.status, stdout: result.stdout ?? '', stderr: result.stderr ?? '', error: result.error };
}

// Failure text that says whether the entry exists, so a missing implementation reads as such.
export function runReport(run, label) {
  const entry = existsSync(hookPath) ? '' : ' [entry missing: 99_Tools/SessionGuard/claude-hook.mjs]';
  return `${label}${entry}\nexit: ${run.exit}\nstdout: ${run.stdout}\nstderr: ${run.stderr}${run.error ? `\nerror: ${run.error}` : ''}`;
}

// goal 「설계」: decision none = empty stdout and exit 0.
export function assertNoDecision(run, label) {
  assert.equal(run.exit, 0, runReport(run, `${label}: exit 0 (goal 설계 출력)`));
  assert.equal(run.stdout, '', runReport(run, `${label}: empty stdout, no decision (goal 설계 출력)`));
}

// The repair each code must show, as literal names from goal 「설계」: rule 1 repair line 97
// (evidence folder file, Bash run_in_background), rule 2 line 106 (evidence folder), rule 3
// line 114 (this session's context memo under the evidence folder).
const repairHints = {
  'mailbox-output-loss': ['.backups/verification', 'run_in_background'],
  'write-outside-checkout': ['.backups/verification'],
  'temp-write': ['.backups/verification'],
  'memo-first': ['.backups/verification', 'context'],
};

// goal 「설계」 출력: one line `{"hookSpecificOutput":{"hookEventName":"PreToolUse",
// "permissionDecision":"deny","permissionDecisionReason":"session-guard:<code> <이유와 고치는 법>"}}`,
// exit 0, and never allow or ask.
export function assertDeny(run, code, label) {
  assert.ok(repairHints[code], `unknown result code in the test: ${code}`);
  assert.equal(run.exit, 0, runReport(run, `${label}: exit 0 (goal 설계 출력)`));
  const text = run.stdout.trim();
  assert.notEqual(text, '', runReport(run, `${label}: a deny decision is printed on stdout`));
  assert.ok(!text.includes('\n'), runReport(run, `${label}: the decision is one line`));
  let output;
  try {
    output = JSON.parse(text);
  } catch {
    assert.fail(runReport(run, `${label}: stdout is one JSON value`));
  }
  assert.deepEqual(Object.keys(output), ['hookSpecificOutput'], runReport(run, `${label}: only hookSpecificOutput`));
  const specific = output.hookSpecificOutput ?? {};
  assert.deepEqual(Object.keys(specific).sort(), ['hookEventName', 'permissionDecision', 'permissionDecisionReason'],
    runReport(run, `${label}: hookSpecificOutput fields`));
  assert.equal(specific.hookEventName, 'PreToolUse', runReport(run, `${label}: hookEventName`));
  assert.equal(specific.permissionDecision, 'deny', runReport(run, `${label}: permissionDecision is deny, never allow or ask`));
  const reason = String(specific.permissionDecisionReason ?? '');
  assert.equal(reason.split(' ')[0], `session-guard:${code}`, runReport(run, `${label}: result code ${code}`));
  assert.match(reason, /^session-guard:\S+ +\S/, runReport(run, `${label}: reason carries cause and repair after the code`));
  for (const hint of repairHints[code]) {
    assert.ok(reason.includes(hint), runReport(run, `${label}: the repair names ${hint}`));
  }
}

// Writes this session's context memo with the Write tool and checks that it passes (goal 「설계」
// 규칙 3: the first write is the memo, so the memo is recorded and the call passes).
export function writeMemo(ws, options = {}) {
  const path = options.path ?? join(ws.project, ...MEMO.split('/'));
  const run = runHook(ws, fileToolInput(ws, 'Write', path, options));
  assertNoDecision(run, `memo write ${toSlash(path)}`);
  return path;
}

export const UTC = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d{1,9})?Z$/;

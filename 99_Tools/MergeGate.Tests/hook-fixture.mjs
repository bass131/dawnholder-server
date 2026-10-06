// Shared fixture for the independent merge gate regressions. It builds a throwaway project folder
// that is handed to the hook as CLAUDE_PROJECT_DIR, writes approval records in the documented
// format (behavior contract v2 §1), runs 99_Tools/MergeGate/claude-hook.mjs as a black box and
// reads its decision. It holds no judgement of the gate: every expected decision and code is
// written in the test files from the contract text.
//
// Input shapes copy the fields of the hook inputs observed in the design probe
// (E/design-probe/raw/hooklog/r5.jsonl and r6.jsonl, Claude Code 2.1.291): PermissionRequest has
// no tool_use_id, and a subagent call carries agent_id and agent_type.
import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { existsSync } from 'node:fs';
import { chmod, lstat, mkdir, mkdtemp, readFile, readdir, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join, relative } from 'node:path';
import { fileURLToPath } from 'node:url';

export const repositoryRoot = fileURLToPath(new URL('../../', import.meta.url));
export const hookPath = join(repositoryRoot, '99_Tools', 'MergeGate', 'claude-hook.mjs');

export const SESSION = '5e551011-0000-4000-8000-000000000001';
export const OTHER_SESSION = '5e551011-0000-4000-8000-000000000002';
export const PR = 12;
export const OTHER_PR = 13;
export const HEAD_A = '0123456789abcdef0123456789abcdef01234567';
export const HEAD_B = 'fedcba9876543210fedcba9876543210fedcba98';
// The subagent identity observed in r5.
export const AGENT = { agent_id: 'a3a3f93dba75a3113', agent_type: 'general-purpose' };

export const MINUTE = 60 * 1000;

// One owned folder per test, removed when the test ends. `project` is CLAUDE_PROJECT_DIR; the
// other children of `root` are free for git repositories and non-repository folders.
export async function createProject(t, { marker = true } = {}) {
  const root = await mkdtemp(join(tmpdir(), 'merge-gate-'));
  t.after(() => removeTree(root));
  const project = join(root, 'project');
  await mkdir(project);
  if (marker) {
    await mkdir(stateDirectory(project), { recursive: true });
    await writeFile(join(stateDirectory(project), 'main-checkout'), '');
  }
  return { root, project };
}

// Permission tests make folders read-only; give write access back before removing the tree.
export async function removeTree(path) {
  const restore = async target => {
    const stats = await lstat(target).catch(() => null);
    if (!stats || stats.isSymbolicLink()) return;
    await chmod(target, stats.isDirectory() ? 0o755 : 0o644).catch(() => {});
    if (stats.isDirectory()) {
      for (const name of await readdir(target)) await restore(join(target, name));
    }
  };
  await restore(path);
  await rm(path, { recursive: true, force: true });
}

export const stateDirectory = project => join(project, '.claude', 'state', 'merge-gate');
export const approvalsDirectory = project => join(stateDirectory(project), 'approvals');
export const approvalsFile = (project, session = SESSION) => join(approvalsDirectory(project), `${session}.json`);

const isoAgo = ago => (ago === null ? null : new Date(Date.now() - ago).toISOString());

// One approval entry in the §1 format. Times are given as "milliseconds ago" so a test states the
// age it needs (for example 31 minutes for an expired record) without a clock in the product.
export function approval({ pr = PR, head = HEAD_A, createdAgo = MINUTE, usedAgo = null, usedCommand = null } = {}) {
  return { pr, head, createdAt: isoAgo(createdAgo), usedAt: isoAgo(usedAgo), usedCommand };
}

export async function writeApprovals(project, approvals, session = SESSION) {
  await mkdir(approvalsDirectory(project), { recursive: true });
  const text = `${JSON.stringify({ version: 1, approvals })}\n`;
  await writeFile(approvalsFile(project, session), text);
  return JSON.parse(text);
}

export async function readApprovals(project, session = SESSION) {
  const path = approvalsFile(project, session);
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
      else found.push(relative(directory, path).split('\\').join('/'));
    }
  };
  await walk(directory);
  return found.sort();
}

// Hook inputs. `cwd` defaults to the project folder, as in every observed session.
function common(project, { session = SESSION, cwd = project, agent = false } = {}) {
  const input = {
    session_id: session,
    transcript_path: join(project, 'transcript.jsonl'),
    cwd,
    permission_mode: 'auto',
  };
  return agent ? { ...input, ...AGENT } : input;
}

export const promptInput = (project, prompt, options) => ({
  ...common(project, options),
  prompt_id: '00000000-0000-4000-8000-0000000000aa',
  hook_event_name: 'UserPromptSubmit',
  prompt,
});

export const bashInput = (project, command, options) => ({
  ...common(project, options),
  hook_event_name: 'PreToolUse',
  tool_name: 'Bash',
  tool_input: { command, description: 'merge gate regression input' },
  tool_use_id: 'toolu_merge_gate_regression',
});

export const permissionInput = (project, command, options) => ({
  ...common(project, options),
  hook_event_name: 'PermissionRequest',
  tool_name: 'Bash',
  tool_input: { command, description: 'merge gate regression input' },
});

const fileToolInputs = {
  Write: path => ({ file_path: path, content: 'x' }),
  Edit: path => ({ file_path: path, old_string: 'a', new_string: 'b' }),
  MultiEdit: path => ({ file_path: path, edits: [{ old_string: 'a', new_string: 'b' }] }),
  NotebookEdit: path => ({ notebook_path: path, new_source: 'x' }),
};

export const fileToolInput = (project, toolName, path, options) => ({
  ...common(project, options),
  hook_event_name: 'PreToolUse',
  tool_name: toolName,
  tool_input: fileToolInputs[toolName](path),
  tool_use_id: 'toolu_merge_gate_regression',
});

// Runs the real entry like the registered command: `node <entry>` with no arguments, the hook
// JSON on stdin and CLAUDE_PROJECT_DIR set. The process cwd is the project folder, which is not a
// git repository, so a branch lookup must use the input `cwd`. The git ceiling keeps lookups from
// climbing out of the test folder into whatever contains the system temp folder.
export function runHook({ root, project }, input, { raw } = {}) {
  const environment = { ...process.env, CLAUDE_PROJECT_DIR: project, GIT_CEILING_DIRECTORIES: root };
  delete environment.NODE_TEST_CONTEXT;
  const result = spawnSync(process.execPath, [hookPath], {
    cwd: project,
    input: raw ?? JSON.stringify(input),
    encoding: 'utf8',
    env: environment,
    timeout: 30000,
  });
  return { exit: result.status, stdout: result.stdout ?? '', stderr: result.stderr ?? '', error: result.error };
}

// Failure text that says whether the entry exists, so a missing implementation reads as such.
export function runReport(run, label) {
  const entry = existsSync(hookPath) ? '' : ' [entry missing: 99_Tools/MergeGate/claude-hook.mjs]';
  return `${label}${entry}\nexit: ${run.exit}\nstdout: ${run.stdout}\nstderr: ${run.stderr}${run.error ? `\nerror: ${run.error}` : ''}`;
}

function parseDecision(run, label) {
  assert.equal(run.exit, 0, runReport(run, `${label}: the hook ends with exit 0 (§2)`));
  assert.notEqual(run.stdout.trim(), '', runReport(run, `${label}: a decision is printed on stdout (§2)`));
  try {
    return JSON.parse(run.stdout);
  } catch {
    assert.fail(runReport(run, `${label}: stdout is one JSON value (§2)`));
  }
}

// §2: decision none = empty stdout and exit 0.
export function assertNoDecision(run, label) {
  assert.equal(run.exit, 0, runReport(run, `${label}: exit 0 (§2 decision none)`));
  assert.equal(run.stdout, '', runReport(run, `${label}: empty stdout (§2 decision none)`));
}

// §2: deny = PreToolUse output with permissionDecision deny and reason
// `merge-gate:<code> <cause and repair>`.
export function assertDeny(run, code, label) {
  const output = parseDecision(run, label);
  const specific = output.hookSpecificOutput ?? {};
  assert.equal(specific.hookEventName, 'PreToolUse', runReport(run, `${label}: hookEventName`));
  assert.equal(specific.permissionDecision, 'deny', runReport(run, `${label}: permissionDecision deny`));
  const reason = String(specific.permissionDecisionReason ?? '');
  assert.equal(reason.split(' ')[0], `merge-gate:${code}`, runReport(run, `${label}: block code ${code}`));
  assert.match(reason, /^merge-gate:\S+ +\S/, runReport(run, `${label}: reason carries cause and repair after the code`));
}

// §2: pass = PreToolUse allow with reason `merge-gate:approved PR<number>`.
export function assertAllow(run, pr, label) {
  const output = parseDecision(run, label);
  const specific = output.hookSpecificOutput ?? {};
  assert.equal(specific.hookEventName, 'PreToolUse', runReport(run, `${label}: hookEventName`));
  assert.equal(specific.permissionDecision, 'allow', runReport(run, `${label}: permissionDecision allow`));
  assert.equal(specific.permissionDecisionReason, `merge-gate:approved PR${pr}`, runReport(run, `${label}: reason`));
}

// §2: PermissionRequest allow output, exactly as documented.
export function assertPermissionAllow(run, label) {
  const output = parseDecision(run, label);
  assert.deepEqual(output, { hookSpecificOutput: { hookEventName: 'PermissionRequest', decision: { behavior: 'allow' } } },
    runReport(run, `${label}: PermissionRequest allow`));
}

// §2/§3: UserPromptSubmit never blocks the prompt. Optional stdout is JSON without a block.
export function assertPromptNotBlocked(run, label) {
  assert.equal(run.exit, 0, runReport(run, `${label}: UserPromptSubmit exit 0 (§2)`));
  if (run.stdout.trim() === '') return;
  let output;
  try {
    output = JSON.parse(run.stdout);
  } catch {
    assert.fail(runReport(run, `${label}: optional UserPromptSubmit stdout is JSON`));
  }
  assert.notEqual(output.decision, 'block', runReport(run, `${label}: no block decision`));
  assert.notEqual(output.continue, false, runReport(run, `${label}: the prompt continues`));
}

// Throwaway git repositories for the branch lookup of §4 Bash 3. No remote is configured and only
// local commands run. Global and system git settings are kept out of the setup commands.
export async function createRepository(root, name, branch) {
  const directory = join(root, name);
  await mkdir(directory);
  const globalConfig = join(root, 'empty.gitconfig');
  if (!existsSync(globalConfig)) await writeFile(globalConfig, '');
  const git = args => {
    const result = spawnSync('git', args, {
      cwd: directory,
      encoding: 'utf8',
      env: { ...process.env, GIT_CONFIG_NOSYSTEM: '1', GIT_CONFIG_GLOBAL: globalConfig, GIT_CEILING_DIRECTORIES: root },
    });
    assert.equal(result.status, 0, `git ${args.join(' ')}: ${result.stderr}${result.error ?? ''}`);
  };
  git(['init', '-b', 'main']);
  git(['-c', 'user.name=merge-gate-test', '-c', 'user.email=merge-gate-test@example.invalid',
    '-c', 'commit.gpgsign=false', 'commit', '--allow-empty', '--no-verify', '-m', 'fixture']);
  if (branch !== 'main') git(['switch', '-c', branch]);
  return directory;
}

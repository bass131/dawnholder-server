// Shared fixture for the Codex guard requirement tests. It runs the public entry
// 99_Tools/CodexGuard/codex-hook.mjs as a black box, the way the project hook registration
// launches it: `node <entry>` with no arguments and one PreToolUse JSON object on stdin. It holds
// no judgement of the guard: which commands must be blocked or kept is written in each test file
// from the requirement text, never computed here or taken from the product.
//
// Input fields follow the stored Codex hooks documentation for Codex CLI 0.162.1
// (E/research/hooks.md, "Common input fields" and "PreToolUse"): canonical tool_name "Bash" with
// the command string in tool_input.command, plus the session cwd. Codex runs command hooks with
// the session cwd as working directory (hooks.md "Config shape" notes), so the hook process runs
// in the input cwd.
//
// Nothing here reaches a network or a real remote. Each test owns one folder under the process
// temp folder holding working repositories whose remotes are empty bare repositories in the same
// folder, and a fake `gh` that only records that it ran. A guard that executed the command it
// judges would leave a ref in a bare repository or a line in the gh call log.
import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { createHash } from 'node:crypto';
import { existsSync, readFileSync } from 'node:fs';
import { chmod, mkdir, mkdtemp, readdir, readFile, realpath, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { basename, delimiter, dirname, isAbsolute, join, relative } from 'node:path';
import { fileURLToPath } from 'node:url';

export const repositoryRoot = fileURLToPath(new URL('../../', import.meta.url));
export const hookPath = join(repositoryRoot, '99_Tools', 'CodexGuard', 'codex-hook.mjs');
export const hooksConfigPath = join(repositoryRoot, '.codex', 'hooks.json');

const isWindows = process.platform === 'win32';
const fixturePrefix = 'codex-guard-';

export const SESSION = '0199c0de-0000-7000-8000-00000000c0de';
export const PR = 12;
export const HEAD_SHA = '0123456789abcdef0123456789abcdef01234567';

// ---------------------------------------------------------------------------------------------
// Fixture folders

// One owned folder per test, removed when the test ends.
export async function createFixture(t) {
  const parent = await realpath(tmpdir());
  const root = await mkdtemp(join(parent, fixturePrefix));
  t.after(() => removeFixture(root, parent));

  const bin = join(root, 'bin');
  const ghConfig = join(root, 'gh-config');
  const globalConfig = join(root, 'empty.gitconfig');
  const ghCalls = join(root, 'gh-calls.log');
  await mkdir(bin);
  await mkdir(ghConfig);
  await writeFile(globalConfig, '');
  await writeFakeGh(bin, ghCalls);
  return { root, bin, ghConfig, globalConfig, ghCalls };
}

// Deletes only a folder this fixture created: an absolute path directly under the temp folder
// whose name carries the fixture prefix.
async function removeFixture(root, parent) {
  const owned = isAbsolute(root) && dirname(root) === parent && basename(root).startsWith(fixturePrefix);
  if (!owned) throw new Error(`[fixture] refusing to remove ${root}: not a folder this fixture created`);
  await rm(root, { recursive: true, force: true, maxRetries: 5, retryDelay: 100 });
}

// The fake only appends to the call log. The POSIX script keeps the arguments (quoted, so they
// are never run). The Windows batch file records a fixed line because cmd would interpret `&`
// or `|` inside echoed arguments.
async function writeFakeGh(bin, ghCalls) {
  const posix = join(bin, 'gh');
  await writeFile(posix, `#!/bin/sh\nprintf 'gh %s\\n' "$*" >> '${ghCalls.split('\\').join('/')}'\nexit 0\n`);
  await chmod(posix, 0o755);
  await writeFile(join(bin, 'gh.cmd'), `@echo off\r\n>>"${ghCalls}" echo gh called\r\nexit /b 0\r\n`);
}

export function ghCallLines(fixture) {
  if (!existsSync(fixture.ghCalls)) return [];
  return readFileSync(fixture.ghCalls, 'utf8').split(/\r?\n/).filter(line => line !== '');
}

// ---------------------------------------------------------------------------------------------
// Process environment

function findOnPath(name) {
  const pathKey = Object.keys(process.env).find(key => key.toUpperCase() === 'PATH');
  const directories = (process.env[pathKey] ?? '').split(delimiter).filter(Boolean);
  const fileName = isWindows ? `${name}.exe` : name;
  const found = directories.map(directory => join(directory, fileName)).find(path => existsSync(path));
  if (!found) throw new Error(`[fixture] ${fileName} is not on PATH`);
  return found;
}

let gitExecutable;
export function gitPath() {
  gitExecutable ??= findOnPath('git');
  return gitExecutable;
}

function setVariable(environment, name, value) {
  for (const key of Object.keys(environment)) {
    if (key.toUpperCase() === name.toUpperCase()) delete environment[key];
  }
  if (value !== undefined) environment[name] = value;
}

// Git sees only the fixture: no system or user configuration, no climbing above the fixture
// folder, no credential prompt.
function gitIsolation(fixture) {
  return {
    GIT_CONFIG_NOSYSTEM: '1',
    GIT_CONFIG_GLOBAL: fixture.globalConfig,
    GIT_CEILING_DIRECTORIES: fixture.root,
    GIT_TERMINAL_PROMPT: '0',
  };
}

// The hook gets a narrow PATH: the fake gh folder, git, node and the system folders. On Windows a
// call without a shell does not find gh.cmd, so the real gh folder is left out of PATH as well; gh
// configuration and tokens are emptied as a second layer.
export function hookEnvironment(fixture) {
  const environment = { ...process.env };
  delete environment.NODE_TEST_CONTEXT;
  for (const name of ['GH_TOKEN', 'GITHUB_TOKEN', 'GH_ENTERPRISE_TOKEN', 'GITHUB_ENTERPRISE_TOKEN', 'GH_REPO']) {
    setVariable(environment, name, undefined);
  }
  const systemFolders = isWindows
    ? [join(process.env.SystemRoot ?? 'C:\\Windows', 'System32'), process.env.SystemRoot ?? 'C:\\Windows']
    : ['/usr/bin', '/bin'];
  const searchPath = [...new Set([fixture.bin, dirname(gitPath()), dirname(process.execPath), ...systemFolders])];
  setVariable(environment, 'PATH', searchPath.join(delimiter));
  setVariable(environment, 'GH_CONFIG_DIR', fixture.ghConfig);
  setVariable(environment, 'GH_HOST', 'codex-guard-test.invalid');
  setVariable(environment, 'GH_PROMPT_DISABLED', '1');
  for (const [name, value] of Object.entries(gitIsolation(fixture))) setVariable(environment, name, value);
  return environment;
}

// ---------------------------------------------------------------------------------------------
// Local repositories

const author = [
  '-c', 'user.name=codex-guard-test',
  '-c', 'user.email=codex-guard-test@example.invalid',
  '-c', 'commit.gpgsign=false',
];

export function git(fixture, cwd, args) {
  const result = spawnSync(gitPath(), args, {
    cwd,
    encoding: 'utf8',
    env: { ...process.env, ...gitIsolation(fixture) },
    windowsHide: true,
  });
  if (result.status !== 0) {
    throw new Error(`[fixture] git ${args.join(' ')} in ${cwd}: exit ${result.status} ${result.stderr}${result.error ?? ''}`);
  }
  return result.stdout;
}

// A working repository with one empty commit. Every remote is an empty bare repository in the
// fixture folder. Remote-tracking refs and origin/HEAD are written with update-ref and
// symbolic-ref, so building it runs no fetch, clone or push and the bare repositories stay empty.
//
// - current: the branch checked out.
// - branches: local branches; each tracks <first remote>/<same name> when `tracking` is true.
// - remoteDefault: target of refs/remotes/<first remote>/HEAD, or null to leave it unknown.
// - remotes: remote names; the first is the tracking remote. Every remote gets
//   refs/remotes/<remote>/<branch> for the local branches.
// - remoteDefaults: default branch of other remotes by name (null for unknown); a remote not
//   named here has main.
// - remoteRefs: false writes no refs/remotes at all, like a remote that was added and never
//   fetched; the default branches above are then unknown too.
// - config: extra [key, value] entries written to the repository configuration.
export async function createRepository(fixture, {
  name = 'work',
  current = 'feat/guard',
  branches = ['main', 'feat/guard'],
  remoteDefault = 'main',
  remotes = ['origin'],
  remoteDefaults = {},
  remoteRefs = true,
  tracking = true,
  config = [],
} = {}) {
  const directory = join(fixture.root, name);
  git(fixture, fixture.root, ['init', '-q', '-b', 'main', directory]);
  git(fixture, directory, [...author, 'commit', '-q', '--allow-empty', '--no-verify', '-m', 'fixture']);
  for (const branch of branches) {
    if (branch !== 'main') git(fixture, directory, ['branch', branch]);
  }

  const bares = {};
  for (const [index, remote] of remotes.entries()) {
    const defaultBranch = index === 0 ? remoteDefault : (remoteDefaults[remote] === undefined ? 'main' : remoteDefaults[remote]);
    const bare = join(fixture.root, `${name}-${remote}.git`);
    git(fixture, fixture.root, ['init', '-q', '--bare', '-b', defaultBranch ?? 'main', bare]);
    git(fixture, directory, ['remote', 'add', remote, bare]);
    bares[remote] = bare;
    if (!remoteRefs) continue;
    for (const branch of branches) {
      git(fixture, directory, ['update-ref', `refs/remotes/${remote}/${branch}`, 'HEAD']);
    }
    if (defaultBranch !== null) {
      git(fixture, directory, ['update-ref', `refs/remotes/${remote}/${defaultBranch}`, 'HEAD']);
      git(fixture, directory, ['symbolic-ref', `refs/remotes/${remote}/HEAD`, `refs/remotes/${remote}/${defaultBranch}`]);
    }
  }
  if (tracking) {
    for (const branch of branches) {
      git(fixture, directory, ['config', `branch.${branch}.remote`, remotes[0]]);
      git(fixture, directory, ['config', `branch.${branch}.merge`, `refs/heads/${branch}`]);
    }
  }
  for (const [key, value] of config) git(fixture, directory, ['config', key, value]);
  if (current !== 'main') git(fixture, directory, ['checkout', '-q', current]);
  return { directory, bares };
}

export async function createSubdirectory(directory, ...parts) {
  const path = join(directory, ...parts);
  await mkdir(path, { recursive: true });
  return path;
}

// Refs present in a bare remote. An empty list means nothing was pushed into it.
export function bareRefs(fixture, bare) {
  return git(fixture, fixture.root, ['--git-dir', bare, 'for-each-ref', '--format=%(refname)'])
    .split(/\r?\n/)
    .filter(line => line !== '');
}

export function assertRemotesUntouched(fixture, repository, label) {
  for (const [remote, bare] of Object.entries(repository.bares)) {
    assert.deepEqual(bareRefs(fixture, bare), [], `${label}: remote ${remote} received refs, so a push ran`);
  }
}

// Every file under `directory` outside .git, as sorted `/`-separated paths with a content hash.
export async function snapshotFiles(directory) {
  const found = [];
  const walk = async folder => {
    for (const entry of await readdir(folder, { withFileTypes: true })) {
      if (entry.name === '.git') continue;
      const path = join(folder, entry.name);
      if (entry.isDirectory()) {
        await walk(path);
      } else {
        const hash = createHash('sha256').update(await readFile(path)).digest('hex');
        found.push(`${relative(directory, path).split('\\').join('/')} ${hash}`);
      }
    }
  };
  await walk(directory);
  return found.sort();
}

// ---------------------------------------------------------------------------------------------
// Hook inputs and runs

// One PreToolUse input with every field the stored documentation lists for the event.
export function bashInput(cwd, command, overrides = {}) {
  return {
    session_id: SESSION,
    transcript_path: null,
    cwd,
    hook_event_name: 'PreToolUse',
    model: 'gpt-6-astra',
    permission_mode: 'default',
    turn_id: 'turn-codex-guard-test',
    tool_name: 'Bash',
    tool_use_id: 'call-codex-guard-test',
    tool_input: { command },
    ...overrides,
  };
}

// Runs the real entry. `raw` replaces the JSON text on stdin; `cwd` defaults to the input cwd.
export function runHook(fixture, input, { raw, cwd = input?.cwd } = {}) {
  const result = spawnSync(process.execPath, [hookPath], {
    cwd,
    input: raw ?? JSON.stringify(input),
    encoding: 'utf8',
    env: hookEnvironment(fixture),
    timeout: 30000,
    windowsHide: true,
  });
  return {
    exit: result.status,
    signal: result.signal,
    stdout: result.stdout ?? '',
    stderr: result.stderr ?? '',
    error: result.error,
  };
}

const clip = (text, limit = 800) => (text.length > limit ? `${text.slice(0, limit)}…(${text.length} chars)` : text);

// Failure text that says whether the entry exists, so a missing implementation reads as such.
export function runReport(run, label, problems = []) {
  const entry = existsSync(hookPath) ? '' : ' [entry missing: 99_Tools/CodexGuard/codex-hook.mjs]';
  const lines = [
    `${label}${entry}`,
    ...problems.map(problem => `- ${problem}`),
    `exit: ${run.exit}${run.signal ? ` signal: ${run.signal}` : ''}`,
    `stdout: ${clip(run.stdout)}`,
    `stderr: ${clip(run.stderr)}`,
  ];
  if (run.error) lines.push(`error: ${run.error}`);
  return lines.join('\n');
}

// ---------------------------------------------------------------------------------------------
// Outcomes. The accepted shapes come from contract requirement 6, the lead's design supplement 1
// (E/guard-pretest-design-supplement-01.md, 「출력 동등성 보충」) and hooks.md "PreToolUse".

// Fields that make Codex mark a PreToolUse hook run as failed and continue the tool call
// (hooks.md "Common output fields" and "PreToolUse").
const unsupportedFields = ['continue', 'stopReason', 'suppressOutput'];

const isText = value => typeof value === 'string' && value.trim() !== '';

function readStdoutObject(stdout) {
  if (stdout.trim() === '') return { empty: true };
  let value;
  try {
    value = JSON.parse(stdout);
  } catch {
    return { invalid: 'stdout is not exactly one JSON value (log text mixed in?)' };
  }
  if (value === null || typeof value !== 'object' || Array.isArray(value)) {
    return { invalid: 'stdout JSON is not an object' };
  }
  return { value };
}

// A block is one of these explicit forms:
// - exit 0 and one JSON object on stdout whose hookSpecificOutput has hookEventName "PreToolUse",
//   permissionDecision "deny" and a non-empty permissionDecisionReason (contract requirement 6);
// - exit 0 and one JSON object with the older shape {"decision": "block", "reason": "…"}, which
//   the stored hooks.md says Codex also accepts (design supplement 1);
// - exit 2 with the reason on stderr (contract requirement 6).
// An object that also carries an unsupported field, updatedInput, an allow or ask decision or a
// hookSpecificOutput for another event does not block: Codex treats such a hook run as failed and
// runs the command.
export function blockProblems(run) {
  if (run.exit === 2) {
    const problems = [];
    if (run.stderr.trim() === '') problems.push('exit 2 carries no reason on stderr');
    const stdout = readStdoutObject(run.stdout);
    if (stdout.invalid) problems.push(stdout.invalid);
    const specific = stdout.value?.hookSpecificOutput ?? {};
    if (specific.permissionDecision === 'allow' || 'updatedInput' in specific) {
      problems.push('stdout allows or rewrites the call next to exit 2');
    }
    return problems;
  }
  if (run.exit !== 0) return [`exit ${run.exit} is neither a JSON deny with exit 0 nor exit 2`];

  const stdout = readStdoutObject(run.stdout);
  if (stdout.empty) return ['exit 0 with empty stdout lets the call run'];
  if (stdout.invalid) return [stdout.invalid];

  const output = stdout.value;
  const hasSpecific = 'hookSpecificOutput' in output;
  const specific = output.hookSpecificOutput ?? {};
  const specificDeny = specific.hookEventName === 'PreToolUse'
    && specific.permissionDecision === 'deny'
    && isText(specific.permissionDecisionReason);
  const legacyBlock = output.decision === 'block' && isText(output.reason);

  const problems = [];
  if (!specificDeny && !legacyBlock) {
    problems.push('neither hookSpecificOutput {hookEventName "PreToolUse", permissionDecision "deny", reason} '
      + 'nor {"decision": "block", "reason"} with a non-empty reason');
  }
  if (hasSpecific && specific.hookEventName !== 'PreToolUse') problems.push('hookSpecificOutput is for another event');
  if (hasSpecific && 'permissionDecision' in specific && specific.permissionDecision !== 'deny') {
    problems.push(`permissionDecision "${specific.permissionDecision}" next to the block`);
  }
  if ('decision' in output && output.decision !== 'block') problems.push(`decision "${output.decision}" next to the block`);
  if ('updatedInput' in specific) problems.push('the block also carries updatedInput');
  for (const field of unsupportedFields) {
    if (field in output) problems.push(`unsupported field "${field}" makes the hook run fail open`);
  }
  return problems;
}

// The text a block gives as its reason, whichever form it used.
export function blockReason(run) {
  if (run.exit === 2) return run.stderr.trim();
  const stdout = readStdoutObject(run.stdout);
  const output = stdout.value ?? {};
  return String(output.hookSpecificOutput?.permissionDecisionReason ?? output.reason ?? '').trim();
}

// A kept call ends with exit 0 and either empty stdout or one JSON object that makes no
// permission decision, does not rewrite the input and carries no unsupported field. An explicit
// "allow" is not a kept call either: the approved scope draft says ordinary calls are left to
// Codex's own permission handling.
export function passProblems(run) {
  if (run.exit !== 0) return [`exit ${run.exit}; a kept call ends with exit 0`];
  const stdout = readStdoutObject(run.stdout);
  if (stdout.empty) return [];
  if (stdout.invalid) return [stdout.invalid];

  const output = stdout.value;
  const specific = output.hookSpecificOutput ?? {};
  const problems = [];
  if ('permissionDecision' in specific) {
    problems.push(`permissionDecision "${specific.permissionDecision}"; a kept call leaves the decision to Codex`);
  }
  if ('updatedInput' in specific) problems.push('updatedInput rewrites the call');
  if ('decision' in output) problems.push(`decision "${output.decision}"`);
  for (const field of unsupportedFields) {
    if (field in output) problems.push(`unsupported field "${field}"`);
  }
  return problems;
}

function assertNoGhCall(fixture, run, label) {
  const calls = ghCallLines(fixture);
  if (calls.length > 0) assert.fail(runReport(run, `${label}: the hook ran gh (${calls.join(' | ')})`));
}

export function assertBlocked(fixture, run, label) {
  assertNoGhCall(fixture, run, label);
  const problems = blockProblems(run);
  if (problems.length > 0) assert.fail(runReport(run, `${label}: expected a block`, problems));
}

export function assertKept(fixture, run, label) {
  assertNoGhCall(fixture, run, label);
  const problems = passProblems(run);
  if (problems.length > 0) assert.fail(runReport(run, `${label}: expected the call to be kept`, problems));
}

// Subtest names show newlines and tabs so multi-line commands stay on one report line.
export const caseName = command => command.replace(/\n/g, '\\n').replace(/\t/g, '\\t');

// One subtest per command, all judged in the same cwd.
export async function expectBlocked(t, fixture, cwd, commands, overrides) {
  for (const command of commands) {
    await t.test(caseName(command), () => {
      assertBlocked(fixture, runHook(fixture, bashInput(cwd, command, overrides)), caseName(command));
    });
  }
}

export async function expectKept(t, fixture, cwd, commands, overrides) {
  for (const command of commands) {
    await t.test(caseName(command), () => {
      assertKept(fixture, runHook(fixture, bashInput(cwd, command, overrides)), caseName(command));
    });
  }
}

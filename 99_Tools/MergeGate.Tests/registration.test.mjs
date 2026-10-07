// Independent regressions for how the merge gate is wired into the repository. Expected outcomes
// come from the approved rules, not from 99_Tools/MergeGate:
// - behavior contract v2 (E/merge-gate-behavior-spec.md, SHA256 d16918f6…7c9a) §1: the project
//   `.claude/settings.json` registers UserPromptSubmit, PreToolUse (matcher covering Bash, Write,
//   Edit, MultiEdit, NotebookEdit) and PermissionRequest (matcher Bash) with the command
//   `node "$CLAUDE_PROJECT_DIR/99_Tools/MergeGate/claude-hook.mjs"`, and the state folder is
//   excluded from Git;
// - goal 「범위」 PR1: the tests are connected to the independent regression steps of code-rules CI;
// - 리드 답 msg_93f732ba921f item 1 (reply msg_eaa7a91f2cee, a lead decision outside contract v2):
//   the step name contains MergeGate, it has `if: always()` and `shell: bash` and comes before the
//   results upload; a missing, unloadable or failing suite ends it nonzero and a passing one with
//   0; it keeps command, stdout, stderr and exit in $RULES_OUTPUT/merge-gate-independent-tests/.
//   The full step name and the exact command are the implementation's choice, so the step is
//   found by that name part and judged by running its body.
// Step bodies run as GitHub hands them to `bash --noprofile --norc -eo pipefail {0}` (workflow
// syntax reference), the same harness as 99_Tools/Orca.Tests/message-policy.test.mjs.
//
// Environment: MERGE_GATE_TESTS_BASH (a POSIX bash for the workflow step; Windows default is Git
// Bash). Git runs read-only against this checkout.
import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { existsSync } from 'node:fs';
import { mkdir, mkdtemp, readFile, readdir, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { delimiter, dirname, join } from 'node:path';
import { test } from 'node:test';

import { removeTree, repositoryRoot } from './hook-fixture.mjs';

const settingsPath = join(repositoryRoot, '.claude', 'settings.json');
const workflowPath = join(repositoryRoot, '.github', 'workflows', 'code-rules.yml');
const testDirectory = join(repositoryRoot, '99_Tools', 'MergeGate.Tests');
const hookCommand = 'node "$CLAUDE_PROJECT_DIR/99_Tools/MergeGate/claude-hook.mjs"';

async function readSettings() {
  assert.ok(existsSync(settingsPath), '.claude/settings.json is missing: §1 registers the hook in the project settings');
  return JSON.parse(await readFile(settingsPath, 'utf8'));
}

// Matcher groups of `event` whose hooks run the merge gate command.
function registrations(settings, event) {
  const groups = settings.hooks?.[event];
  assert.ok(Array.isArray(groups), `.claude/settings.json hooks.${event} is not a list`);
  return groups.filter(group => (group.hooks ?? []).some(hook => hook.type === 'command' && hook.command === hookCommand));
}

// Claude Code matcher semantics: omitted, empty or `*` match every tool; otherwise the matcher is
// a pattern such as `Bash` or `Edit|Write` matched against the whole tool name.
const covers = (matcher, toolName) => matcher === undefined || matcher === '' || matcher === '*' ||
  new RegExp(`^(?:${matcher})$`).test(toolName);

test('§1 UserPromptSubmit runs the merge gate command', async () => {
  const settings = await readSettings();
  assert.ok(registrations(settings, 'UserPromptSubmit').length > 0, `no UserPromptSubmit hook runs ${hookCommand}`);
});

test('§1 PreToolUse runs the merge gate for Bash, Write, Edit, MultiEdit and NotebookEdit', async () => {
  const groups = registrations(await readSettings(), 'PreToolUse');
  assert.ok(groups.length > 0, `no PreToolUse hook runs ${hookCommand}`);
  for (const toolName of ['Bash', 'Write', 'Edit', 'MultiEdit', 'NotebookEdit']) {
    assert.ok(groups.some(group => covers(group.matcher, toolName)), `PreToolUse matchers do not cover ${toolName}`);
  }
});

test('§1 PermissionRequest runs the merge gate with matcher Bash', async () => {
  const groups = registrations(await readSettings(), 'PermissionRequest');
  assert.ok(groups.length > 0, `no PermissionRequest hook runs ${hookCommand}`);
  for (const group of groups) assert.equal(group.matcher, 'Bash', 'PermissionRequest matcher');
});

test('§1 the state folder is excluded from Git and the project settings are not', async t => {
  const work = await mkdtemp(join(tmpdir(), 'merge-gate-ignore-'));
  t.after(() => removeTree(work));
  // Only this checkout's ignore rules count, not the machine's global or system git settings.
  const globalConfig = join(work, 'empty.gitconfig');
  await writeFile(globalConfig, '');
  const checkIgnore = path => spawnSync('git', ['check-ignore', '--no-index', '--quiet', path], {
    cwd: repositoryRoot,
    encoding: 'utf8',
    env: { ...process.env, GIT_CONFIG_NOSYSTEM: '1', GIT_CONFIG_GLOBAL: globalConfig },
  });
  for (const path of ['.claude/state/merge-gate/main-checkout', '.claude/state/merge-gate/approvals/x.json']) {
    const result = checkIgnore(path);
    assert.equal(result.status, 0, `${path} must be ignored by Git: ${result.stderr}`);
  }
  const settings = checkIgnore('.claude/settings.json');
  assert.equal(settings.status, 1, `.claude/settings.json must stay trackable: ${settings.stderr}`);
});

const readWorkflowLines = async () => (await readFile(workflowPath, 'utf8')).replace(/\r\n/g, '\n').split('\n');
const indentOf = line => line.length - line.trimStart().length;
const isStepStart = line => line.trimStart().startsWith('- name: ');

function stepAt(lines, step) {
  const keyIndent = indentOf(lines[step]) + 2;
  const keys = [];
  for (let index = step + 1; index < lines.length; index += 1) {
    const line = lines[index];
    if (line.trim() === '' || line.trimStart().startsWith('#')) continue;
    if (indentOf(line) < keyIndent) break;
    if (indentOf(line) === keyIndent) keys.push(index);
  }
  return { step, name: lines[step].trim().slice('- name: '.length), keyIndent, keys };
}

function namedStep(lines, part) {
  const matches = lines.map((line, index) => [line, index])
    .filter(([line]) => isStepStart(line) && line.includes(part)).map(([, index]) => index);
  assert.equal(matches.length, 1, `expected one code-rules step whose name contains ${part}, found ${matches.length}`);
  return stepAt(lines, matches[0]);
}

function exactStep(lines, name) {
  const index = lines.findIndex(line => line.trim() === `- name: ${name}`);
  assert.ok(index >= 0, `workflow step not found: ${name}`);
  return stepAt(lines, index);
}

// The `run: |` body and the literal `env:` of a step. `${{ runner.temp }}` is the only expression
// replaced (as the existing checker step uses it); other keys or expressions are reported, since
// they cannot be reproduced outside GitHub.
function bashStep(lines, step, runnerTemp) {
  const keyText = step.keys.map(index => lines[index].trim());
  const supported = ['if: always()', 'shell: bash', 'run: |', 'env:'];
  const unsupported = keyText.filter(text => !supported.includes(text));
  assert.deepEqual(unsupported, [], `step "${step.name}" at line ${step.step + 1} has keys this harness cannot run`);
  const blockAfter = key => {
    const start = step.keys[keyText.indexOf(key)];
    const block = [];
    for (let index = start + 1; index < lines.length; index += 1) {
      if (lines[index].trim() !== '' && indentOf(lines[index]) <= step.keyIndent) break;
      block.push(lines[index]);
    }
    while (block.length > 0 && block.at(-1).trim() === '') block.pop();
    return block;
  };
  const body = blockAfter('run: |');
  const bodyIndent = indentOf(body.find(line => line.trim() !== '') ?? '');
  const environment = {};
  if (keyText.includes('env:')) {
    for (const line of blockAfter('env:').filter(text => text.trim() !== '')) {
      const [, key, value] = line.trim().match(/^([A-Za-z_][A-Za-z0-9_]*): ?(.*)$/) ?? [];
      assert.ok(key, `step "${step.name}": env line not understood: ${line.trim()}`);
      const resolved = value.replaceAll('${{ runner.temp }}', runnerTemp);
      assert.ok(!resolved.includes('${{'), `step "${step.name}": env ${key} uses an expression this harness cannot evaluate`);
      environment[key] = resolved;
    }
  }
  return { script: `${body.map(line => line.slice(bodyIndent)).join('\n')}\n`, environment };
}

function bashExecutable() {
  if (process.env.MERGE_GATE_TESTS_BASH) return process.env.MERGE_GATE_TESTS_BASH;
  if (process.platform !== 'win32') return 'bash';
  // On Windows, `bash` on PATH may be the WSL launcher, which does not see these Windows paths.
  const gitBash = 'C:\\Program Files\\Git\\bin\\bash.exe';
  if (existsSync(gitBash)) return gitBash;
  throw new Error('Set MERGE_GATE_TESTS_BASH to a POSIX bash; the workflow step cannot be run otherwise');
}

const slashPath = path => path.split('\\').join('/');
const mergeGateStepPart = 'MergeGate';
const evidenceNames = ['command.txt', 'stdout.txt', 'stderr.txt', 'exit.txt'];

// Runs the initializer and the MergeGate step in a scratch workspace whose test folder holds
// `suite(name)` for every real test file name, or no folder at all when `suite` is null.
async function runMergeGateStep(t, label, suite) {
  const workspace = await mkdtemp(join(tmpdir(), `merge-gate-workflow-${label}-`));
  t.after(() => removeTree(workspace));
  const runnerTemp = join(workspace, 'runner-temp');
  await mkdir(runnerTemp);
  if (suite !== null) {
    const suiteFolder = join(workspace, '99_Tools', 'MergeGate.Tests');
    await mkdir(suiteFolder, { recursive: true });
    const names = (await readdir(testDirectory)).filter(name => name.endsWith('.test.mjs')).sort();
    for (const [index, name] of names.entries()) await writeFile(join(suiteFolder, name), suite(index));
  }
  const lines = await readWorkflowLines();
  const githubEnv = join(runnerTemp, 'set_env');
  await writeFile(githubEnv, '');
  const initializer = bashStep(lines, exactStep(lines, 'Initialize job-local paths'), slashPath(runnerTemp));
  const mergeGate = bashStep(lines, namedStep(lines, mergeGateStepPart), slashPath(runnerTemp));
  await writeFile(join(workspace, 'initializer.sh'), initializer.script);
  await writeFile(join(workspace, 'merge-gate-step.sh'), mergeGate.script);

  const bash = bashExecutable();
  const path = `${dirname(process.execPath)}${delimiter}${process.env.PATH ?? ''}`;
  const runScript = (script, extra) => {
    const environment = { ...process.env, PATH: path, ...extra };
    delete environment.NODE_TEST_CONTEXT;
    return spawnSync(bash, ['--noprofile', '--norc', '-eo', 'pipefail', script], { cwd: workspace, encoding: 'utf8', env: environment });
  };
  const init = runScript('initializer.sh', { RUNNER_TEMP: slashPath(runnerTemp), GITHUB_ENV: slashPath(githubEnv) });
  assert.equal(init.status, 0, `initializer: ${init.stderr}${init.error ?? ''}`);
  const handedOver = Object.fromEntries((await readFile(githubEnv, 'utf8')).split('\n').filter(Boolean)
    .map(line => [line.slice(0, line.indexOf('=')), line.slice(line.indexOf('=') + 1)]));
  // A nested CI run must not write into the real job's environment or summary files.
  const jobFiles = { GITHUB_ENV: slashPath(githubEnv), GITHUB_STEP_SUMMARY: slashPath(join(runnerTemp, 'step_summary')) };
  const step = runScript('merge-gate-step.sh', { ...handedOver, ...jobFiles, ...mergeGate.environment });
  const results = join(runnerTemp, 'code-rules-results', 'merge-gate-independent-tests');
  const evidence = {};
  for (const name of evidenceNames) evidence[name] = existsSync(join(results, name)) ? await readFile(join(results, name), 'utf8') : null;
  return { exit: step.status, output: `${step.stdout}${step.stderr}${step.error ?? ''}`, evidence };
}

function assertEvidence(run, label) {
  for (const name of evidenceNames) {
    assert.notEqual(run.evidence[name], null, `${label}: merge-gate-independent-tests/${name} is kept\n${run.output}`);
  }
  assert.equal(run.evidence['exit.txt'].trim(), String(run.exit), `${label}: kept exit equals the step exit`);
  assert.match(run.evidence['command.txt'], /node --test/, `${label}: command.txt names the node test run`);
  assert.match(run.evidence['command.txt'], /MergeGate\.Tests/, `${label}: command.txt names the MergeGate tests`);
}

const passingSuite = () => "import { test } from 'node:test';\ntest('passes', () => {});\n";
const failingFirst = index => (index === 0
  ? "import assert from 'node:assert/strict';\nimport { test } from 'node:test';\ntest('fails', () => assert.equal(1, 2));\n"
  : passingSuite());
const unloadableFirst = index => (index === 0 ? "import { test } from 'node:test';\ntest('broken', () => {\n" : passingSuite());

test('msg_93f732ba921f: the MergeGate step always runs, in bash, before the results upload', async () => {
  const lines = await readWorkflowLines();
  const step = namedStep(lines, mergeGateStepPart);
  const keyText = step.keys.map(index => lines[index].trim());
  assert.ok(keyText.includes('if: always()'), `step "${step.name}": an earlier failure must not skip it`);
  assert.ok(keyText.includes('shell: bash'), `step "${step.name}": shell bash`);
  const upload = lines.findIndex(line => line.trim().startsWith('uses: actions/upload-artifact'));
  assert.ok(upload >= 0, 'the results upload step exists');
  assert.ok(step.step < upload, `step "${step.name}" must come before the results upload`);
});

test('msg_93f732ba921f: the MergeGate step fails for a missing, unloadable or failing suite and keeps its evidence', async t => {
  for (const [label, suite] of [['missing', null], ['unloadable', unloadableFirst], ['failing', failingFirst]]) {
    const run = await runMergeGateStep(t, label, suite);
    assert.notEqual(run.exit, 0, `${label}: the step must fail\n${run.output}`);
    assertEvidence(run, label);
  }
});

test('msg_93f732ba921f: the MergeGate step passes a passing suite and keeps its evidence', async t => {
  const run = await runMergeGateStep(t, 'passing', passingSuite);
  assert.equal(run.exit, 0, `passing: ${run.output}`);
  assertEvidence(run, 'passing');
  // The reporter is spec or TAP depending on the terminal; both print the counts.
  assert.match(run.evidence['stdout.txt'], /\bpass [1-9]\d*\b/, 'passing: the tests ran');
  assert.match(run.evidence['stdout.txt'], /\bfail 0\b/, 'passing: nothing failed');
});

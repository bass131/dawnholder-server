// Shared helpers for the independent Backlog candidate check regressions. They load
// 99_Tools/Backlog/candidate-policy.mjs inside each test, so a missing or broken module fails every
// test on its own instead of hiding the suite, run check-candidates.mjs as a black box, own
// throwaway folders and run one workflow step body. They hold no judgement of the check: every
// expected status, count and diagnostic is written in the test files from the lead's contract v1
// and its answer msg_f79fa2b5abb2.
//
// Environment: BACKLOG_TESTS_BASH (a POSIX bash for the workflow step; Windows default is Git Bash).
import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { existsSync } from 'node:fs';
import { mkdir, mkdtemp, readFile, readdir, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { delimiter, dirname, join } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';

export const repositoryRoot = fileURLToPath(new URL('../../', import.meta.url));
export const policyPath = join(repositoryRoot, '99_Tools', 'Backlog', 'candidate-policy.mjs');
export const cliPath = join(repositoryRoot, '99_Tools', 'Backlog', 'check-candidates.mjs');
export const workflowPath = join(repositoryRoot, '.github', 'workflows', 'code-rules.yml');
export const testsDirectory = fileURLToPath(new URL('./', import.meta.url));
export const fixturesDirectory = join(testsDirectory, 'fixtures');

// The status / exit table of the contract.
export const documentedExit = { allowed: 0, 'policy-violation': 1, 'input-error': 2 };

export const loadPolicy = () => import(pathToFileURL(policyPath).href);

// The CLI reports every path with `\` turned into `/` (answer msg_f79fa2b5abb2, item 2).
export const slashPath = path => path.replace(/\\/g, '/');

// Text from numbered lines, so a test can name the line it expects: lines('a', 'b') is "a\nb\n".
export const lines = (...rows) => `${rows.join('\n')}\n`;
export const withCrlf = text => text.replace(/\n/g, '\r\n');

const resultFields = ['status', 'exitCode', 'backlog', 'goal', 'counts', 'diagnostics'];
const countFields = ['backlogIds', 'duplicateIds', 'candidates', 'candidatesWithoutReference', 'unknownIds'];

// The documented result shape; it checks form only and never decides the status.
export function assertResult(result, status, label) {
  assert.equal(result?.status, status, `${label}: ${JSON.stringify(result)}`);
  assert.equal(result.exitCode, documentedExit[status], `${label}: exit code`);
  for (const field of resultFields) assert.ok(field in result, `${label}: result field ${field}`);
  if (status === 'input-error') {
    assert.equal(result.counts, null, `${label}: an input error has no counts`);
  } else {
    for (const field of countFields) assert.ok(field in result.counts, `${label}: counts.${field}`);
  }
  assert.ok(Array.isArray(result.diagnostics), `${label}: diagnostics`);
  assert.equal(result.diagnostics.length === 0, status === 'allowed', `${label}: diagnostics only when not allowed`);
  for (const diagnostic of result.diagnostics) {
    assert.equal(typeof diagnostic.code, 'string', `${label}: diagnostic code`);
    assert.ok(diagnostic.path === null || typeof diagnostic.path === 'string', `${label}: diagnostic path`);
    assert.ok(diagnostic.line === null || (Number.isInteger(diagnostic.line) && diagnostic.line >= 1),
      `${label}: diagnostic line ${diagnostic.line}`);
    for (const field of ['message', 'repair']) {
      assert.equal(typeof diagnostic[field], 'string', `${label}: diagnostic ${field}`);
      assert.ok(diagnostic[field].trim().length > 0, `${label}: empty diagnostic ${field}`);
    }
  }
}

const byPlace = (left, right) => String(left.path).localeCompare(String(right.path))
  || (left.line ?? 0) - (right.line ?? 0)
  || left.code.localeCompare(right.code);

// Exact comparison of code, path and line; the order of diagnostics is not part of the contract.
export function assertDiagnostics(result, expected, label) {
  const actual = result.diagnostics.map(({ code, path, line }) => ({ code, path, line })).sort(byPlace);
  const wanted = expected.map(([code, path, line]) => ({ code, path, line })).sort(byPlace);
  assert.deepEqual(actual, wanted, `${label}: ${JSON.stringify(result.diagnostics)}`);
}

// Each expected [line, text] must be named by its own diagnostic of that code on that line.
export function assertMessagesName(result, code, expected, label) {
  const remaining = result.diagnostics.filter(diagnostic => diagnostic.code === code);
  for (const [line, text] of expected) {
    const index = remaining.findIndex(diagnostic => diagnostic.line === line && diagnostic.message.includes(text));
    assert.ok(index >= 0, `${label}: no ${code} on line ${line} names ${text}: ${JSON.stringify(result.diagnostics)}`);
    remaining.splice(index, 1);
  }
}

// One owned folder per test under os.tmpdir(), removed when the test ends.
export async function createWork(t, prefix) {
  const root = await mkdtemp(join(tmpdir(), `backlog-${prefix}-`));
  t.after(() => rm(root, { recursive: true, force: true }));
  return root;
}

export async function writeText(root, relativePath, text) {
  const path = join(root, relativePath);
  await mkdir(dirname(path), { recursive: true });
  await writeFile(path, text);
  return path;
}

// A child environment without the parent test runner's context, so nested `node --test` runs alone.
export function childEnvironment(extra = {}) {
  const environment = { ...process.env, ...extra };
  delete environment.NODE_TEST_CONTEXT;
  return environment;
}

export function runCli(args, { cwd, nodeArgs = [] } = {}) {
  const run = spawnSync(process.execPath, [...nodeArgs, cliPath, ...args], {
    cwd,
    encoding: 'utf8',
    env: childEnvironment(),
  });
  return { exit: run.status, stdout: run.stdout, stderr: run.stderr, error: run.error };
}

// The CLI writes one JSON value to stdout and ends with that value's exitCode.
export function parseCliResult(run, label) {
  let result;
  assert.doesNotThrow(() => {
    result = JSON.parse(run.stdout);
  }, `${label}: stdout must be one JSON value: ${run.stdout}${run.stderr}${run.error ?? ''}`);
  assert.equal(run.exit, result.exitCode, `${label}: process exit must equal exitCode`);
  return result;
}

// Workflow step bodies as GitHub hands them to `bash --noprofile --norc -eo pipefail {0}`.
// The same reading exists in Orca.Tests/message-policy.test.mjs, but that file exports nothing and
// importing it would register its tests, so this suite keeps its own copy.
export const readWorkflowLines = async () => (await readFile(workflowPath, 'utf8')).replace(/\r\n/g, '\n').split('\n');
const indentOf = line => line.length - line.trimStart().length;

export function stepNames(lines) {
  return lines.filter(line => /^\s*- name: /.test(line)).map(line => line.trim().slice('- name: '.length));
}

export function stepKeys(lines, name) {
  const step = lines.findIndex(line => line.trim() === `- name: ${name}`);
  if (step < 0) throw new Error(`Workflow step not found: ${name}`);
  const keyIndent = indentOf(lines[step]) + 2;
  const keys = [];
  for (let index = step + 1; index < lines.length; index += 1) {
    const line = lines[index];
    if (line.trim() === '' || line.trimStart().startsWith('#')) continue;
    if (indentOf(line) < keyIndent) break;
    if (indentOf(line) === keyIndent) keys.push(index);
  }
  return { step, keyIndent, keys };
}

export function bashStepBody(lines, name, allowedKeys) {
  const { step, keyIndent, keys } = stepKeys(lines, name);
  const keyText = keys.map(index => lines[index].trim());
  const unexpected = keyText.filter(text => !allowedKeys.includes(text));
  if (unexpected.length > 0 || !keyText.includes('run: |')) {
    throw new Error(`Unsupported form of step "${name}" at line ${step + 1}: ${keyText.join(' / ')}`);
  }
  const body = [];
  for (let index = keys[keyText.indexOf('run: |')] + 1; index < lines.length; index += 1) {
    if (lines[index].trim() !== '' && indentOf(lines[index]) <= keyIndent) break;
    body.push(lines[index]);
  }
  while (body.length > 0 && body.at(-1).trim() === '') body.pop();
  const bodyIndent = indentOf(body.find(line => line.trim() !== '') ?? '');
  return `${body.map(line => line.slice(bodyIndent)).join('\n')}\n`;
}

function bashExecutable() {
  if (process.env.BACKLOG_TESTS_BASH) return process.env.BACKLOG_TESTS_BASH;
  if (process.platform !== 'win32') return 'bash';
  // On Windows, `bash` on PATH may be the WSL launcher, which does not see these Windows paths.
  const gitBash = 'C:\\Program Files\\Git\\bin\\bash.exe';
  if (existsSync(gitBash)) return gitBash;
  throw new Error('Set BACKLOG_TESTS_BASH to a POSIX bash; the workflow step cannot be run otherwise');
}

// Runs the job initializer and then one step in a throwaway checkout holding only `files`
// ({ relative path: text }). It returns the step outcome and every results folder the step made
// under the job-local RULES_OUTPUT, with the four evidence files of each.
export async function runWorkflowStep(work, label, stepName, files) {
  const workspace = join(work, `workflow-${label}`);
  const runnerTemp = join(workspace, 'runner-temp');
  await mkdir(runnerTemp, { recursive: true });
  for (const [relativePath, text] of Object.entries(files)) await writeText(workspace, relativePath, text);
  const workflow = await readWorkflowLines();
  const githubEnv = join(runnerTemp, 'set_env');
  await writeFile(githubEnv, '');
  await writeFile(join(workspace, 'initializer.sh'),
    bashStepBody(workflow, 'Initialize job-local paths', ['shell: bash', 'run: |']));
  await writeFile(join(workspace, 'backlog-step.sh'),
    bashStepBody(workflow, stepName, ['if: always()', 'shell: bash', 'run: |']));

  const bash = bashExecutable();
  const path = `${dirname(process.execPath)}${delimiter}${process.env.PATH ?? ''}`;
  const runStep = (script, extra) => spawnSync(bash, ['--noprofile', '--norc', '-eo', 'pipefail', script], {
    cwd: workspace,
    encoding: 'utf8',
    env: childEnvironment({ PATH: path, ...extra }),
  });
  const initializer = runStep('initializer.sh', { RUNNER_TEMP: slashPath(runnerTemp), GITHUB_ENV: slashPath(githubEnv) });
  assert.equal(initializer.status, 0, `initializer: ${initializer.stderr}${initializer.error ?? ''}`);
  const handedOver = Object.fromEntries((await readFile(githubEnv, 'utf8')).split('\n').filter(Boolean)
    .map(line => [line.slice(0, line.indexOf('=')), line.slice(line.indexOf('=') + 1)]));
  const step = runStep('backlog-step.sh', handedOver);

  const resultsRoot = join(runnerTemp, 'code-rules-results');
  const folders = existsSync(resultsRoot) ? await readdir(resultsRoot) : [];
  const evidence = {};
  for (const folder of folders) {
    evidence[folder] = {};
    for (const name of ['command.txt', 'stdout.txt', 'stderr.txt', 'exit.txt']) {
      const file = join(resultsRoot, folder, name);
      evidence[folder][name] = existsSync(file) ? await readFile(file, 'utf8') : null;
    }
  }
  return { exit: step.status, stdout: step.stdout, stderr: step.stderr, error: step.error, evidence };
}

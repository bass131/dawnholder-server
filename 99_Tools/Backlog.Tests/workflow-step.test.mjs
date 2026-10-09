// Independent regressions for the Backlog step of .github/workflows/code-rules.yml. Expected
// behaviour comes from the Rules lead's test contract v1 (task_4e23d12d8aee, R9) and its answer
// msg_f79fa2b5abb2, item 4, not from the workflow:
// - one step, shaped like the Orca and MergeGate regression steps: `if: always()`, `shell: bash`,
//   `run: |`, command / stdout / stderr / exit kept in its own folder under the job-local
//   RULES_OUTPUT, and exit 1 when the suite is missing or fails;
// - the step is found by `Backlog` in its name; the folder name is the implementer's choice;
// - the step runs every `*.test.mjs` of 99_Tools/Backlog.Tests.
// The step body is run for real with bash in a throwaway checkout (Windows: Git Bash), because
// comparing the YAML text alone would not show the exit and evidence it produces. Stand-in suites
// carry the real test file names.
import assert from 'node:assert/strict';
import { readdir } from 'node:fs/promises';
import { test } from 'node:test';

import {
  bashStepBody, createWork, readWorkflowLines, runWorkflowStep, stepKeys, stepNames, testsDirectory,
} from './candidate-fixture.mjs';

const realTestFiles = (await readdir(testsDirectory)).filter(name => name.endsWith('.test.mjs')).sort();

const passingSuite = "import { test } from 'node:test';\ntest('passes', () => {});\n";
const failingSuite = "import assert from 'node:assert/strict';\nimport { test } from 'node:test';\n" +
  "test('fails', () => assert.equal(1, 2));\n";
const unloadableSuite = "import { test } from 'node:test';\ntest('broken', () => {\n";

async function backlogStepName() {
  const names = stepNames(await readWorkflowLines()).filter(name => name.includes('Backlog'));
  assert.equal(names.length, 1, `exactly one workflow step must name Backlog: ${JSON.stringify(names)}`);
  return names[0];
}

// Stand-ins for the real test files; `replace` picks a different text for one of them.
function standInSuites(replace = {}) {
  return Object.fromEntries(realTestFiles.map(name => [`99_Tools/Backlog.Tests/${name}`,
    replace[name] ?? passingSuite]));
}

function onlyEvidence(run, label) {
  const folders = Object.keys(run.evidence);
  assert.equal(folders.length, 1, `${label}: one results folder under RULES_OUTPUT: ${JSON.stringify(folders)}`);
  return run.evidence[folders[0]];
}

test('one Backlog step has the form of the Orca and MergeGate steps and runs before the artifact upload', async () => {
  const name = await backlogStepName();
  const lines = await readWorkflowLines();
  assert.doesNotThrow(() => bashStepBody(lines, name, ['if: always()', 'shell: bash', 'run: |']));
  const step = stepKeys(lines, name);
  const upload = stepKeys(lines, 'Preserve results even on failure');
  assert.ok(step.keys.some(index => lines[index].trim() === 'if: always()'), 'an earlier failure must not skip it');
  assert.ok(step.step < upload.step, 'evidence must exist before the upload step');
  assert.ok(upload.keys.some(index => lines[index].trim() === 'if: always()'));
  const uploadLines = lines.slice(upload.step, upload.step + 10).map(line => line.trim());
  assert.ok(uploadLines.includes('path: ${{ runner.temp }}/code-rules-results/'),
    'the uploaded folder is the job-local results folder');
});

test('the Backlog step exits 1 and keeps its evidence for a missing, unloadable or failing suite', async t => {
  assert.ok(realTestFiles.length > 0, 'the real suite has test files');
  const name = await backlogStepName();
  const work = await createWork(t, 'workflow-failures');
  const cases = [
    ['missing-folder', {}],
    ['no-test-files', { '99_Tools/Backlog.Tests/fixtures/observed-sources.json': '{}\n' }],
    ['unloadable', standInSuites({ [realTestFiles[0]]: unloadableSuite })],
    ['failing', standInSuites({ [realTestFiles.at(-1)]: failingSuite })],
  ];
  for (const [label, files] of cases) {
    const run = await runWorkflowStep(work, label, name, files);
    assert.equal(run.exit, 1, `${label}: ${run.stdout}${run.stderr}${run.error ?? ''}`);
    const evidence = onlyEvidence(run, label);
    assert.equal(evidence['exit.txt'], '1\n', `${label}: kept exit`);
    assert.ok(evidence['stdout.txt'] !== null && evidence['stderr.txt'] !== null, `${label}: raw output kept`);
    assert.match(evidence['command.txt'] ?? '', /99_Tools\/Backlog\.Tests\//, `${label}: command kept`);
  }
});

test('the Backlog step passes and runs every test file of Backlog.Tests', async t => {
  const name = await backlogStepName();
  const work = await createWork(t, 'workflow-passing');
  const run = await runWorkflowStep(work, 'passing', name, standInSuites());
  assert.equal(run.exit, 0, `${run.stdout}${run.stderr}${run.error ?? ''}`);
  const evidence = onlyEvidence(run, 'passing');
  assert.equal(evidence['exit.txt'], '0\n');
  // One passing test per stand-in; the reporter is spec or TAP depending on the terminal.
  assert.match(evidence['stdout.txt'] ?? '', new RegExp(`\\bpass ${realTestFiles.length}\\b`),
    `every one of ${realTestFiles.join(', ')} must run`);
  assert.match(evidence['command.txt'] ?? '', /99_Tools\/Backlog\.Tests\//);
});

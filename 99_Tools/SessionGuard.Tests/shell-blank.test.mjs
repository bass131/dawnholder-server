// Regressions for item 3 of 01_Phases/goals/2026-11-10-code-audit-gate-tools/goal.md: a character
// Bash reads as part of a word stays part of the word, and reading a command always ends. Expected
// words and decisions come from that goal and its test contract, not from 99_Tools/SessionGuard:
// the Bash word boundary is space, tab, CR and LF, and a redirect destination outside the project
// is write-outside-checkout.
//
// Environment: node only. The failure being guarded against is an endless loop that grows memory,
// so every child runs under a 64 MB heap and a 5 s kill, and shell-command.mjs is never imported
// into this process. Those two numbers bound the test runs; they are not limits of the product.
// The characters are built from code points so that no shell input or source line carries them.
import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { join } from 'node:path';
import { test } from 'node:test';
import { pathToFileURL } from 'node:url';

import {
  UNSET_VARIABLE, assertDeny, assertNoDecision, bashInput, createWorkspace, hookPath, repositoryRoot, toSlash,
} from './guard-fixture.mjs';

const heapLimit = '--max-old-space-size=64';
const childLimits = { timeout: 5000, killSignal: 'SIGKILL', encoding: 'utf8' };

const blanks = [0x00A0, 0x000C, 0x000B, 0x3000, 0xFEFF, 0x2028].map(code => ({
  name: `U+${code.toString(16).toUpperCase().padStart(4, '0')}`,
  character: String.fromCodePoint(code),
}));
const noBreakSpace = blanks[0];

// The hook environment of the shared fixture's runHook, repeated because that helper has no heap
// or kill limit and must not run these inputs.
function hookEnvironment(ws) {
  const owned = { CLAUDE_PROJECT_DIR: ws.project, ...ws.temps, HOME: ws.home };
  const ownedNames = new Set([...Object.keys(owned), UNSET_VARIABLE, 'NODE_TEST_CONTEXT'].map(name => name.toUpperCase()));
  const environment = {};
  for (const [name, value] of Object.entries(process.env)) {
    if (!ownedNames.has(name.toUpperCase())) environment[name] = value;
  }
  return Object.assign(environment, owned);
}

function outcome(result) {
  return { exit: result.status, signal: result.signal, stdout: result.stdout ?? '', stderr: result.stderr ?? '', error: result.error };
}

function runBoundedHook(ws, input) {
  return outcome(spawnSync(process.execPath, [heapLimit, hookPath], {
    ...childLimits,
    cwd: ws.project,
    input: JSON.stringify(input),
    env: hookEnvironment(ws),
  }));
}

const shellCommandUrl = pathToFileURL(join(repositoryRoot, '99_Tools', 'SessionGuard', 'shell-command.mjs')).href;
const wordReader = `
import { readShellCommand } from ${JSON.stringify(shellCommandUrl)};
const chunks = [];
for await (const chunk of process.stdin) chunks.push(chunk);
const { commands } = readShellCommand(JSON.parse(Buffer.concat(chunks).toString('utf8')), {});
process.stdout.write(JSON.stringify(commands.map(command => command.argv.map(word => word.value))));
`;

// The words of each simple command, read in a child under the same limits.
function readWords(source) {
  const environment = { ...process.env };
  delete environment.NODE_TEST_CONTEXT;
  return outcome(spawnSync(process.execPath, [heapLimit, '--input-type=module', '-e', wordReader], {
    ...childLimits,
    input: JSON.stringify(source),
    env: environment,
  }));
}

// Failure messages name the character by its code point so logs carry no shell-unsafe bytes.
const shown = (text, { name, character }) => text.split(character).join(`<${name}>`);

function childReport(run, label) {
  const stderr = run.stderr.length > 400 ? `${run.stderr.slice(0, 400)}…` : run.stderr;
  return `${label}\nexit: ${run.exit}\nsignal: ${run.signal}\nerror: ${run.error?.code ?? ''}\nstderr: ${stderr}`;
}

function assertEndedInsideLimits(run, label) {
  const timedOut = run.error?.code === 'ETIMEDOUT';
  assert.equal(timedOut, false, childReport(run, `${label}: ends before the 5 s limit`));
  assert.equal(run.signal, null, childReport(run, `${label}: not ended by a signal`));
  assert.equal(run.exit, 0, childReport(run, `${label}: exit 0`));
}

function assertWords(source, expected, label, blank = noBreakSpace) {
  const run = readWords(source);
  assertEndedInsideLimits(run, label);
  const words = JSON.parse(run.stdout);
  assert.deepEqual(words, expected, `${label}: words were ${shown(JSON.stringify(words), blank)}`);
}

const outsideFile = ws => `${toSlash(ws.outside)}/y.txt`;

for (const blank of blanks) {
  const word = `a${blank.character}b`;

  test(`${blank.name} outside quotes: the hook ends inside the limits`, async t => {
    const ws = await createWorkspace(t);
    const command = `printf ${word}`;
    assertEndedInsideLimits(runBoundedHook(ws, bashInput(ws, command)), shown(command, blank));
  });

  test(`${blank.name} outside quotes: a redirect after the word is judged by its destination`, async t => {
    const ws = await createWorkspace(t);
    const command = `printf ${word} > "${outsideFile(ws)}"`;
    const run = runBoundedHook(ws, bashInput(ws, command));
    assertEndedInsideLimits(run, shown(command, blank));
    assertDeny(run, 'write-outside-checkout', shown(command, blank));
  });

  test(`${blank.name} outside quotes: the character stays inside its word`, () => {
    const command = `printf ${word}`;
    assertWords(command, [['printf', word]], shown(command, blank), blank);
  });
}

test('U+00A0 inside double quotes stays inside its word', () => {
  const word = `a${noBreakSpace.character}b`;
  const command = `printf "${word}"`;
  assertWords(command, [['printf', word]], shown(command, noBreakSpace));
});

const separatedCommands = [
  { name: 'spaces', plain: 'printf a b', words: [['printf', 'a', 'b']] },
  { name: 'tabs', plain: 'printf\ta\tb', words: [['printf', 'a', 'b']] },
  { name: 'CRLF', plain: 'printf a\r\nprintf b', words: [['printf', 'a'], ['printf', 'b']] },
];

for (const { name, plain, words } of separatedCommands) {
  test(`${name} still divide the words of a command`, () => {
    assertWords(plain, words, JSON.stringify(plain));
  });

  test(`${name}: a command without a destination still gets no decision`, async t => {
    const ws = await createWorkspace(t);
    const run = runBoundedHook(ws, bashInput(ws, plain));
    assertEndedInsideLimits(run, JSON.stringify(plain));
    assertNoDecision(run, JSON.stringify(plain));
  });

  test(`${name}: a redirect outside the project is still write-outside-checkout`, async t => {
    const ws = await createWorkspace(t);
    const command = `${plain} > "${outsideFile(ws)}"`;
    const run = runBoundedHook(ws, bashInput(ws, command));
    assertEndedInsideLimits(run, JSON.stringify(command));
    assertDeny(run, 'write-outside-checkout', JSON.stringify(command));
  });
}

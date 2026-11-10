// Independent verifier's regressions for item 3 of 01_Phases/goals/2026-11-10-code-audit-gate-tools/goal.md
// (「만들 것」 3, 「설계 선택」 3, completion conditions 5 and 6). Expected words and decisions are
// written here from that goal, not computed from 99_Tools/SessionGuard:
// - outside quotes a word ends at space, tab, CR or LF and at the Bash operators ; & | ( ) < >;
//   every other character JavaScript's \s counts as white space stays inside its word;
// - reading a command always ends, by returning or by an explicit error;
// - a redirect or tee destination next to such a word keeps its decision.
// shell-blank.test.mjs covers six of those characters in two positions. This file covers all of
// them next to every boundary, the decisions only a finished reading can give, inputs nobody
// listed, and the behaviour item 3 must keep.
//
// Environment: node only. Like shell-blank.test.mjs, every child runs under a 64 MB heap and a 5 s
// kill, and shell-command.mjs is never imported into this process; the two numbers bound the test
// runs, they are not limits of the product. The bounded runners below repeat that file's, because
// importing a test file runs its tests and guard-fixture.mjs's runHook has no heap or kill limit.
// Characters are built from code points so that no shell input or source line carries them.
import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { join } from 'node:path';
import { test } from 'node:test';
import { pathToFileURL } from 'node:url';

import {
  MEMO, UNSET_VARIABLE, UTC, assertDeny, assertNoDecision, bashInput, createWorkspace, hookPath, readState,
  repositoryRoot, toSlash,
} from './guard-fixture.mjs';

const codeName = code => `U+${code.toString(16).toUpperCase().padStart(4, '0')}`;
// Failure messages name every character outside printable ASCII by its code point.
const shown = text => String(text).replace(/[^\x20-\x7e]/g, char => `<${codeName(char.codePointAt(0))}>`);

// JavaScript's \s without space, tab, CR and LF: the characters whose word boundary item 3 changes.
const nonBashBlanks = [
  0x000B, 0x000C, 0x00A0, 0x1680, 0x2000, 0x2001, 0x2002, 0x2003, 0x2004, 0x2005, 0x2006, 0x2007,
  0x2008, 0x2009, 0x200A, 0x2028, 0x2029, 0x202F, 0x205F, 0x3000, 0xFEFF,
].map(code => ({ name: codeName(code), character: String.fromCodePoint(code) }));
const character = name => nonBashBlanks.find(blank => blank.name === name).character;
const noBreakSpace = character('U+00A0');
const byteOrderMark = character('U+FEFF');

// --- bounded children -------------------------------------------------------------------------

const heapLimit = '--max-old-space-size=64';
const childLimits = { timeout: 5000, killSignal: 'SIGKILL', encoding: 'utf8', maxBuffer: 16 * 1024 * 1024 };

function outcome(result) {
  return { exit: result.status, signal: result.signal, stdout: result.stdout ?? '', stderr: result.stderr ?? '', error: result.error };
}

function childReport(run, label) {
  const stderr = run.stderr.length > 400 ? `${run.stderr.slice(0, 400)}...` : run.stderr;
  return `${label}\nexit: ${run.exit}\nsignal: ${run.signal}\nerror: ${run.error?.code ?? ''}\nstderr: ${shown(stderr)}`;
}

function assertEndedInsideLimits(run, label) {
  assert.notEqual(run.error?.code, 'ETIMEDOUT', childReport(run, `${label}: ends before the 5 s limit`));
  assert.equal(run.signal, null, childReport(run, `${label}: not ended by a signal`));
  assert.equal(run.exit, 0, childReport(run, `${label}: exit 0`));
}

// The environment runHook gives the hook, with inherited copies of the owned names dropped.
function hookEnvironment(ws) {
  const owned = { CLAUDE_PROJECT_DIR: ws.project, ...ws.temps, HOME: ws.home };
  const ownedNames = new Set([...Object.keys(owned), UNSET_VARIABLE, 'NODE_TEST_CONTEXT'].map(name => name.toUpperCase()));
  const environment = {};
  for (const [name, value] of Object.entries(process.env)) {
    if (!ownedNames.has(name.toUpperCase())) environment[name] = value;
  }
  return Object.assign(environment, owned);
}

function runBoundedHook(ws, command) {
  return outcome(spawnSync(process.execPath, [heapLimit, hookPath], {
    ...childLimits,
    cwd: ws.project,
    input: JSON.stringify(bashInput(ws, command)),
    env: hookEnvironment(ws),
  }));
}

const shellCommandUrl = pathToFileURL(join(repositoryRoot, '99_Tools', 'SessionGuard', 'shell-command.mjs')).href;
const readStdin = `
const chunks = [];
for await (const chunk of process.stdin) chunks.push(chunk);
const input = JSON.parse(Buffer.concat(chunks).toString('utf8'));
`;

// Per source of a JSON array: what the reading returned (the words of each simple command that has
// any, every redirect as [operator, destination], every write destination) or the message it threw.
const readerScript = `
import { readShellCommand } from ${JSON.stringify(shellCommandUrl)};
${readStdin}
process.stdout.write(JSON.stringify(input.map(source => {
  try {
    const { commands, writes } = readShellCommand(source, {});
    return {
      words: commands.map(command => command.argv.map(word => word.value)).filter(argv => argv.length),
      redirects: commands.flatMap(command => command.redirects.map(({ operator, destination }) => [operator, destination.value])),
      writes: writes.map(write => write.value),
    };
  } catch (error) {
    return { error: String(error?.message ?? error) };
  }
})));
`;

// Only counts, for many inputs: how many readings returned and which messages the others threw.
const endingScript = `
import { readShellCommand } from ${JSON.stringify(shellCommandUrl)};
${readStdin}
let returned = 0;
const thrown = {};
for (const source of input) {
  try {
    readShellCommand(source, {});
    returned++;
  } catch (error) {
    const message = String(error?.message ?? error);
    thrown[message] = (thrown[message] ?? 0) + 1;
  }
}
process.stdout.write(JSON.stringify({ count: input.length, returned, thrown }));
`;

function runReader(script, input) {
  const environment = { ...process.env };
  delete environment.NODE_TEST_CONTEXT;
  return outcome(spawnSync(process.execPath, [heapLimit, '--input-type=module', '-e', script], {
    ...childLimits,
    input: JSON.stringify(input),
    env: environment,
  }));
}

function readAll(sources, label) {
  const run = runReader(readerScript, sources);
  assertEndedInsideLimits(run, label);
  return JSON.parse(run.stdout);
}

// The order of commands around a substitution is not part of item 3, so words compare as a set.
const commandSet = words => words.map(argv => JSON.stringify(argv)).sort();

function assertReading(result, { words, redirects = [], writes = [] }, label) {
  assert.equal(result.error, undefined, `${label}: threw ${result.error}`);
  assert.deepEqual(commandSet(result.words), commandSet(words), `${label}: words were ${shown(JSON.stringify(result.words))}`);
  assert.deepEqual(result.redirects, redirects, `${label}: redirects were ${shown(JSON.stringify(result.redirects))}`);
  assert.deepEqual(result.writes, writes, `${label}: writes were ${shown(JSON.stringify(result.writes))}`);
}

function assertReadings(cases, label) {
  const results = readAll(cases.map(({ source }) => source), label);
  cases.forEach((expected, index) => assertReading(results[index], expected, `${label} ${expected.label}: ${shown(expected.source)}`));
}

function assertEveryReadingEnds(t, sources, label) {
  const run = runReader(endingScript, sources);
  assertEndedInsideLimits(run, label);
  const { count, returned, thrown } = JSON.parse(run.stdout);
  const threw = Object.values(thrown).reduce((sum, times) => sum + times, 0);
  assert.equal(count, sources.length, `${label}: every input reached the reader`);
  assert.equal(returned + threw, count, `${label}: every reading returned or threw`);
  for (const message of Object.keys(thrown)) assert.notEqual(message.trim(), '', `${label}: a thrown error has a message`);
  t.diagnostic(`${label}: ${count} inputs, ${returned} returned, thrown ${JSON.stringify(thrown)}`);
}

// --- requirement 1: a character Bash reads as part of a word stays part of the word ----------

test("the characters under test are JavaScript's \\s except space, tab, CR and LF", () => {
  const engine = [];
  for (let code = 0; code <= 0xFFFF; code++) {
    if (/\s/.test(String.fromCharCode(code)) && ![0x09, 0x0A, 0x0D, 0x20].includes(code)) engine.push(codeName(code));
  }
  assert.deepEqual(nonBashBlanks.map(({ name }) => name), engine);
});

// The word boundaries of goal 「설계 선택」 3 and the Bash operators, each with the character on
// one or both sides.
function positions(x) {
  return [
    { label: 'alone', source: x, words: [[x]] },
    { label: 'first character', source: `${x}printf a`, words: [[`${x}printf`, 'a']] },
    { label: 'last character', source: `printf a${x}`, words: [['printf', `a${x}`]] },
    { label: 'repeated', source: `printf a${x}${x}b`, words: [['printf', `a${x}${x}b`]] },
    { label: 'next to spaces', source: `printf a${x} ${x}b`, words: [['printf', `a${x}`, `${x}b`]] },
    { label: 'next to a tab and a CR', source: `printf a${x}\t${x}b${x}\r${x}c`, words: [['printf', `a${x}`, `${x}b${x}`, `${x}c`]] },
    { label: 'around ;', source: `a${x};${x}b`, words: [[`a${x}`], [`${x}b`]] },
    { label: 'around &&', source: `a${x}&&${x}b`, words: [[`a${x}`], [`${x}b`]] },
    { label: 'around ||', source: `a${x}||${x}b`, words: [[`a${x}`], [`${x}b`]] },
    { label: 'around |', source: `a${x}|${x}b`, words: [[`a${x}`], [`${x}b`]] },
    { label: 'around &', source: `a${x}&${x}b`, words: [[`a${x}`], [`${x}b`]] },
    { label: 'around LF', source: `a${x}\n${x}b`, words: [[`a${x}`], [`${x}b`]] },
    { label: 'inside parentheses', source: `(${x}a${x})`, words: [[`${x}a${x}`]] },
    { label: 'alone between operators', source: `a;${x};b`, words: [['a'], [x], ['b']] },
    { label: 'before #', source: `printf ${x}#a`, words: [['printf', `${x}#a`]] },
    { label: 'right before >', source: `printf a${x}>b`, words: [['printf', `a${x}`]], redirects: [['>', 'b']], writes: ['b'] },
    { label: 'right after >', source: `printf a>${x}b`, words: [['printf', 'a']], redirects: [['>', `${x}b`]], writes: [`${x}b`] },
    { label: 'after > and a space', source: `printf a > ${x}b`, words: [['printf', 'a']], redirects: [['>', `${x}b`]], writes: [`${x}b`] },
    { label: 'inside a > destination', source: `printf a >b${x}c`, words: [['printf', 'a']], redirects: [['>', `b${x}c`]], writes: [`b${x}c`] },
    { label: 'right after >>', source: `printf a>>${x}b`, words: [['printf', 'a']], redirects: [['>>', `${x}b`]], writes: [`${x}b`] },
    { label: 'right after &>', source: `printf a &>${x}b`, words: [['printf', 'a']], redirects: [['&>', `${x}b`]], writes: [`${x}b`] },
    { label: 'right after 2>', source: `printf a 2>${x}b`, words: [['printf', 'a']], redirects: [['2>', `${x}b`]], writes: [`${x}b`] },
    // Digits glued to a word are not a file descriptor in Bash, so > alone is the operator.
    { label: 'before digits and >', source: `printf a${x}2>b`, words: [['printf', `a${x}2`]], redirects: [['>', 'b']], writes: ['b'] },
    { label: 'right after <', source: `cat <${x}b`, words: [['cat']], redirects: [['<', `${x}b`]] },
    { label: 'right after <<<', source: `cat <<<${x}b`, words: [['cat']], redirects: [['<<<', `${x}b`]] },
    { label: 'right after $(', source: `echo $(${x}a)`, words: [[`${x}a`], ['echo', '$()']] },
    { label: 'right before )', source: `echo $(a${x})`, words: [[`a${x}`], ['echo', '$()']] },
    { label: 'after a substitution', source: `echo $(a)${x}b`, words: [['a'], ['echo', `$()${x}b`]] },
    { label: 'in a substitution inside double quotes', source: `echo "$(a${x}b)"`, words: [[`a${x}b`], ['echo', '$()']] },
    { label: 'in a heredoc delimiter', source: `cat <<a${x}b\nbody\na${x}b\necho done`, words: [['cat'], ['echo', 'done']], redirects: [['<<', `a${x}b`]] },
    { label: 'in a heredoc body', source: `cat <<E\na${x}b\nE\necho done`, words: [['cat'], ['echo', 'done']], redirects: [['<<', 'E']] },
    { label: 'in an assignment value', source: `V=a${x}b; printf c > "$V"`, words: [['printf', 'c']], redirects: [['>', `a${x}b`]], writes: [`a${x}b`] },
    { label: 'next to a line join', source: `printf a${x}\\\n${x}b`, words: [['printf', `a${x}${x}b`]] },
    { label: 'after a backslash', source: `printf a\\${x}b`, words: [['printf', `a${x}b`]] },
    { label: 'in a comment', source: `# a${x}b\nprintf c`, words: [['printf', 'c']] },
    { label: 'inside single quotes', source: `printf 'a${x} b'`, words: [['printf', `a${x} b`]] },
    { label: 'inside double quotes', source: `printf "a${x} b"`, words: [['printf', `a${x} b`]] },
    { label: 'between quoted parts', source: `printf 'a'${x}"b"`, words: [['printf', `a${x}b`]] },
    { label: 'in a tee destination', source: `printf a|tee b${x}c`, words: [['printf', 'a'], ['tee', `b${x}c`]], writes: [`b${x}c`] },
  ];
}

for (const blank of nonBashBlanks) {
  test(`${blank.name} stays inside its word next to every boundary`, () => {
    assertReadings(positions(blank.character), blank.name);
  });
}

test("characters outside JavaScript's \\s stay inside their word", () => {
  assertReadings([0x0085, 0x180E, 0x200B, 0x2060].map(code => {
    const x = String.fromCodePoint(code);
    return { label: codeName(code), source: `printf a${x}b`, words: [['printf', `a${x}b`]] };
  }), 'not \\s');
});

// --- requirement 2: reading ends on any input -------------------------------------------------

test('an unclosed input with U+00A0 outside quotes ends with an explicit error', () => {
  const x = noBreakSpace;
  const sources = [`printf a${x}b "open`, `printf a${x}b 'open`, `printf $(a${x}b`, `cat <<a${x}b\nbody`, `cat <<a${x}b`, `printf a${x}b >`];
  readAll(sources, 'unclosed').forEach((result, index) => {
    const label = `unclosed ${shown(sources[index])}`;
    assert.equal(typeof result.error, 'string', `${label}: an error, but it returned ${shown(JSON.stringify(result))}`);
    assert.notEqual(result.error.trim(), '', `${label}: the error has a message`);
  });
});

const alphabet = [
  ' ', '\t', '\r', '\n', noBreakSpace, character('U+000C'), ';', '&', '|', '(', ')', '<', '>', '$', "'", '"', '\\',
  '#', 'a', '2', '-', '~', '=', '`',
];

test('every input of up to three characters from a 24-character alphabet ends', t => {
  let level = [''];
  const inputs = [''];
  for (let length = 1; length <= 3; length++) {
    level = level.flatMap(prefix => alphabet.map(char => prefix + char));
    inputs.push(...level);
  }
  assertEveryReadingEnds(t, inputs, 'all short inputs');
});

// mulberry32; the fixed seed keeps a failing input reproducible.
function seededRandom(seed) {
  let state = seed;
  return () => {
    state = (state + 0x6D2B79F5) | 0;
    let mixed = Math.imul(state ^ (state >>> 15), 1 | state);
    mixed = (mixed + Math.imul(mixed ^ (mixed >>> 7), 61 | mixed)) ^ mixed;
    return ((mixed ^ (mixed >>> 14)) >>> 0) / 4294967296;
  };
}

test('20000 seeded random inputs of characters and shell pieces end', t => {
  const pieces = [
    ...alphabet, ...nonBashBlanks.map(blank => blank.character),
    '$(', '<<', '<<-', '<<<', '\\\n', '\r\n', '&&', '||', '2>', '&>', '>>', 'E', 'printf', 'tee', 'V=', '"$V"', 'orca orchestration check',
  ];
  const next = seededRandom(20261111);
  const inputs = [];
  for (let index = 0; index < 20000; index++) {
    const length = 1 + Math.floor(next() * 40);
    let source = '';
    for (let piece = 0; piece < length; piece++) source += pieces[Math.floor(next() * pieces.length)];
    inputs.push(source);
  }
  assertEveryReadingEnds(t, inputs, 'seeded random inputs');
});

test('long and deeply nested inputs end', t => {
  const x = noBreakSpace;
  assertEveryReadingEnds(t, [
    x.repeat(200000),
    `printf a${x}b `.repeat(10000),
    `'${x}`.repeat(50000),
    '$('.repeat(100000),
    `${'$('.repeat(1000)}a${x}b${')'.repeat(1000)}`,
  ], 'long inputs');
});

// --- decisions that only a finished reading gives (completion condition 5) ---------------------

const outsideFile = ws => `${toSlash(ws.outside)}/y.txt`;

for (const blank of nonBashBlanks) {
  test(`${blank.name} next to > and inside an outside destination: still write-outside-checkout`, async t => {
    const ws = await createWorkspace(t);
    const x = blank.character;
    const command = `printf a${x}b${x}>"${toSlash(ws.outside)}/y${x}.txt"`;
    const run = runBoundedHook(ws, command);
    assertEndedInsideLimits(run, shown(command));
    assertDeny(run, 'write-outside-checkout', shown(command));
  });
}

test('U+00A0 outside quotes: a first write inside the project before the memo is still memo-first', async t => {
  const ws = await createWorkspace(t);
  const command = `printf a${noBreakSpace}b > notes${noBreakSpace}.txt`;
  const run = runBoundedHook(ws, command);
  assertEndedInsideLimits(run, shown(command));
  assertDeny(run, 'memo-first', shown(command));
});

test('U+00A0 outside quotes: a first write to the context memo is recorded and later writes pass', async t => {
  const ws = await createWorkspace(t);
  const memo = `printf a${noBreakSpace}b > ${MEMO}`;
  const run = runBoundedHook(ws, memo);
  assertEndedInsideLimits(run, shown(memo));
  assertNoDecision(run, shown(memo));
  const state = await readState(ws.project);
  assert.match(state?.memoWrittenAt ?? '', UTC, 'the memo write is recorded, which only a finished reading can do');

  const later = `printf c${noBreakSpace}d > notes.txt`;
  const laterRun = runBoundedHook(ws, later);
  assertEndedInsideLimits(laterRun, shown(later));
  assertNoDecision(laterRun, shown(later));
});

test('U+FEFF outside quotes: a piped mailbox command is still mailbox-output-loss', async t => {
  const ws = await createWorkspace(t);
  const command = `printf a${byteOrderMark}b; orca orchestration check --wait ${byteOrderMark}x | head -5`;
  const run = runBoundedHook(ws, command);
  assertEndedInsideLimits(run, shown(command));
  assertDeny(run, 'mailbox-output-loss', shown(command));
});

const nestedOutsideWrites = [
  { name: 'in a substitution', command: ws => `echo "$(printf a${noBreakSpace}b > '${outsideFile(ws)}')"` },
  { name: 'after a heredoc delimiter', command: ws => `cat <<a${noBreakSpace}b > "${outsideFile(ws)}"\nbody\na${noBreakSpace}b\n` },
  { name: 'after a line join', command: ws => `printf a${noBreakSpace}b \\\n > "${outsideFile(ws)}"` },
  { name: 'inside double quotes', command: ws => `printf "a${noBreakSpace}b" > "${outsideFile(ws)}"` },
];

for (const { name, command: build } of nestedOutsideWrites) {
  test(`U+00A0 ${name}: a redirect outside the project is still write-outside-checkout`, async t => {
    const ws = await createWorkspace(t);
    const command = build(ws);
    const run = runBoundedHook(ws, command);
    assertEndedInsideLimits(run, shown(command));
    assertDeny(run, 'write-outside-checkout', shown(command));
  });
}

test('U+00A0 outside quotes in an unclosed command: the hook ends with no decision', async t => {
  const ws = await createWorkspace(t);
  const command = `printf a${noBreakSpace}b "open > ${outsideFile(ws)}`;
  const run = runBoundedHook(ws, command);
  assertEndedInsideLimits(run, shown(command));
  assertNoDecision(run, shown(command));
});

// --- requirement 3: what item 3 keeps --------------------------------------------------------

test('spaces, tabs, CRLF, LF, CR, line joins, heredocs, substitutions, assignments and tee read as before', () => {
  assertReadings([
    { label: 'spaces', source: 'printf a  b', words: [['printf', 'a', 'b']] },
    { label: 'tabs', source: 'printf\ta\t\tb', words: [['printf', 'a', 'b']] },
    { label: 'CRLF', source: 'printf a\r\nprintf b', words: [['printf', 'a'], ['printf', 'b']] },
    { label: 'LF alone', source: 'printf a\nprintf b', words: [['printf', 'a'], ['printf', 'b']] },
    // goal 「설계 선택」 3: CR keeps its old handling as a blank.
    { label: 'CR alone', source: 'printf a\rb', words: [['printf', 'a', 'b']] },
    { label: 'line join', source: 'printf a\\\nb c', words: [['printf', 'ab', 'c']] },
    { label: 'line join with CRLF', source: 'printf a \\\r\n b', words: [['printf', 'a', 'b']] },
    {
      label: 'heredoc', source: 'cat <<E > out.txt\nx y\nE\necho z',
      words: [['cat'], ['echo', 'z']], redirects: [['<<', 'E'], ['>', 'out.txt']], writes: ['out.txt'],
    },
    { label: 'heredoc with tabs stripped', source: 'cat <<-E\n\tx\n\tE\necho z', words: [['cat'], ['echo', 'z']], redirects: [['<<-', 'E']] },
    { label: 'quoted heredoc delimiter', source: "cat <<'E E'\nx\nE E\necho z", words: [['cat'], ['echo', 'z']], redirects: [['<<', 'E E']] },
    {
      label: 'substitution', source: 'echo "$(printf a > in.txt)" b',
      words: [['printf', 'a'], ['echo', '$()', 'b']], redirects: [['>', 'in.txt']], writes: ['in.txt'],
    },
    { label: 'single quotes keep $( literal', source: "echo '$(a b)'", words: [['echo', '$(a b)']] },
    { label: 'comment', source: 'printf a # b > c', words: [['printf', 'a']] },
    {
      label: 'export and assignment', source: 'export V=x.txt; printf a > "$V"',
      words: [['export'], ['printf', 'a']], redirects: [['>', 'x.txt']], writes: ['x.txt'],
    },
    { label: 'tee after its option', source: 'printf a | tee -a t.txt', words: [['printf', 'a'], ['tee', '-a', 't.txt']], writes: ['t.txt'] },
    { label: 'non-file destinations', source: 'printf a > /dev/null 2>&1', words: [['printf', 'a']], redirects: [['>', '/dev/null'], ['2>&', '1']] },
  ], 'kept');
});

test('a redirect outside the project after an LF-separated command is still write-outside-checkout', async t => {
  const ws = await createWorkspace(t);
  const command = `printf a\nprintf b > "${outsideFile(ws)}"`;
  const run = runBoundedHook(ws, command);
  assertEndedInsideLimits(run, shown(command));
  assertDeny(run, 'write-outside-checkout', shown(command));
});

// The exported names and the returned shape of shell-command.mjs at base c9dc44e4.
const shapeScript = `
import * as shellCommand from ${JSON.stringify(shellCommandUrl)};
const reading = shellCommand.readShellCommand('A=1 printf a > out.txt | tee t.txt', {});
const [first, second] = reading.commands;
const keys = value => Object.keys(value).sort();
process.stdout.write(JSON.stringify({
  exports: keys(shellCommand),
  reading: keys(reading),
  command: keys(first),
  argument: keys(first.argv[0]),
  redirect: keys(first.redirects[0]),
  destination: keys(first.redirects[0].destination),
  write: keys(reading.writes[0]),
  pipe: [first.end, first.next === second],
  commandName: shellCommand.commandName(['C:', 'Tools', 'Git.EXE'].join(String.fromCharCode(92))),
}));
`;

test('the exported names and the returned shape are kept', () => {
  const run = runReader(shapeScript, null);
  assertEndedInsideLimits(run, 'shape');
  assert.deepEqual(JSON.parse(run.stdout), {
    exports: ['commandName', 'readShellCommand'],
    reading: ['commands', 'writes'],
    command: ['argv', 'end', 'next', 'redirects'],
    argument: ['position', 'unresolved', 'value'],
    redirect: ['destination', 'operator'],
    destination: ['position', 'unresolved', 'value'],
    write: ['position', 'unresolved', 'value'],
    pipe: ['|', true],
    commandName: 'git',
  });
});

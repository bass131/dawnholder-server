// Independent regressions for rule 1, mailbox output loss. Every command here is a synthetic
// boundary case. Expected decisions come from goal 「설계 / 세션 쓰기 가드」 규칙 1
// (01_Phases/goals/2026-10-10-operating-tool-guards/goal.md lines 95-97 at 852f88ca), not from the
// hook:
// - target: `orca` (with a path or `.exe`) running `orchestration <sub>`; not targets are the
//   read-only subcommands inbox, run-current, run-list, run-show, task-list, worker-show,
//   worker-read, worker-list, dispatch-show, gate-list, request-show, a `check` with `--peek` or
//   `--all`, and any command with `--help`;
// - blocked shapes: (가) stdout to /dev/null or NUL by `>`, `1>`, `>>`, `&>`, `>|`; (나) a lone `&`
//   after the command; (다) stdout piped by `|` or `|&` to a next command, unless that next
//   command is the last `tee` of the pipe;
// - rule 1 also applies with the main checkout marker (line 90).
// None of these commands writes a file, so rules 2 and 3 have nothing to judge (lines 101, 112).
import { test } from 'node:test';

import { EVIDENCE, assertDeny, assertNoDecision, bashInput, createWorkspace, runHook, writeMemo } from './guard-fixture.mjs';

const WAIT = 'orca orchestration check --wait --types "worker_done,escalation" --timeout-ms 900000 --json';

const blocked = [
  [`${WAIT} > /dev/null`, '가: > /dev/null'],
  [`${WAIT} 1> /dev/null`, '가: 1> /dev/null'],
  [`${WAIT} >> /dev/null`, '가: >> /dev/null'],
  [`${WAIT} &> /dev/null`, '가: &> /dev/null'],
  [`${WAIT} >| /dev/null`, '가: >| /dev/null'],
  [`${WAIT} >/dev/null`, '가: no space before /dev/null'],
  [`${WAIT} > NUL`, '가: > NUL'],
  [`${WAIT} > /dev/null 2>&1`, '가: > /dev/null 2>&1'],
  [`${WAIT} &`, '나: lone & after the command'],
  [`${WAIT} | head -c 1024`, '다: pipe to head'],
  [`${WAIT} | grep -m1 worker_done`, '다: pipe to grep -m1'],
  [`${WAIT} |& cat`, '다: |& pipe'],
  [`${WAIT} | tee ${EVIDENCE}/session/wait.json | head -c 1024`, '다: tee that is not the last command of the pipe'],
  ['orca orchestration check --ack delivery_000000000001 --json > /dev/null', 'check --ack without --peek/--all'],
  ['orca orchestration send --to run:run_000000000001 --subject s --body b --json > /dev/null', 'send'],
  ['orca orchestration reply --id msg_000000000001 --body b --json | head -c 100', 'reply piped'],
  ['orca orchestration ask --question q --timeout-ms 600000 > /dev/null', 'ask'],
  ['orca.exe orchestration check --wait --json > /dev/null', 'orca.exe'],
  ['./tools/orca orchestration check --wait --json > /dev/null', 'orca with a path'],
  ['cd .backups && orca orchestration check --wait --json > /dev/null', 'after &&'],
  [`orca orchestration check --wait \\\n  --types worker_done --json > /dev/null`, 'line continuation inside the command'],
  [`git status --short\n${WAIT} &`, 'second line of a multi-line command'],
];

for (const [command, label] of blocked) {
  test(`rule 1 blocks ${label}`, async t => {
    const ws = await createWorkspace(t);
    assertDeny(runHook(ws, bashInput(ws, command)), 'mailbox-output-loss', label);
  });
}

test('rule 1 blocks lost output even when the Bash call itself runs in the background', async t => {
  const ws = await createWorkspace(t);
  assertDeny(runHook(ws, bashInput(ws, `${WAIT} > /dev/null`, { runInBackground: true })), 'mailbox-output-loss',
    'run_in_background with > /dev/null');
});

test('rule 1 still blocks with the main checkout marker (line 90)', async t => {
  const ws = await createWorkspace(t, { marker: true });
  assertDeny(runHook(ws, bashInput(ws, `${WAIT} > /dev/null 2>&1 &`)), 'mailbox-output-loss', 'main checkout');
});

const readOnly = [
  'inbox', 'run-current', 'run-list', 'run-show', 'task-list', 'worker-show', 'worker-read', 'worker-list',
  'dispatch-show', 'gate-list', 'request-show',
];

for (const sub of readOnly) {
  test(`rule 1 does not target the read-only subcommand ${sub}`, async t => {
    const ws = await createWorkspace(t);
    assertNoDecision(runHook(ws, bashInput(ws, `orca orchestration ${sub} --json > /dev/null`)), `${sub} > /dev/null`);
    assertNoDecision(runHook(ws, bashInput(ws, `orca orchestration ${sub} --json | head -5`)), `${sub} | head`);
  });
}

const notTargets = [
  ['orca orchestration check --peek --json | head -5', 'check --peek piped'],
  ['orca orchestration check --terminal term_x --peek --json > /dev/null', 'check --peek to /dev/null'],
  ['orca orchestration check --all --json > /dev/null', 'check --all'],
  ['orca orchestration check --wait --help > /dev/null', 'check --help'],
  ['orca orchestration send --help | head -20', 'send --help piped'],
  ['orca terminal read --terminal term_x --json > /dev/null', 'orca terminal is not orchestration'],
  [`${WAIT} 2>/dev/null`, 'only stderr to /dev/null'],
  ['orca orchestration check --ack delivery_000000000001 --json && echo done', '&& is not a lone &'],
  ['orca orchestration check --ack delivery_000000000001 --json || echo failed', '|| is not a pipe'],
  [`true & ${WAIT}`, 'the lone & belongs to the command before orca'],
  ['echo "orca orchestration check --wait > /dev/null &"', 'the shape is quoted text, not a command'],
];

for (const [command, label] of notTargets) {
  test(`rule 1 passes ${label}`, async t => {
    const ws = await createWorkspace(t);
    assertNoDecision(runHook(ws, bashInput(ws, command)), label);
  });
}

test('rule 1 passes a pipe whose next command is the last tee (the tee file is left to rules 2 and 3)', async t => {
  const ws = await createWorkspace(t);
  writeMemo(ws);
  assertNoDecision(runHook(ws, bashInput(ws, `${WAIT} | tee ${EVIDENCE}/session/wait.json`)), 'last tee after memo');
});

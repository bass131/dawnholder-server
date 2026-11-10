// Independent re-verification of fix round 1 for items 1 and 2 of
// 01_Phases/goals/2026-11-10-code-audit-gate-tools/goal.md, written by the verifier ([Rules 검증자])
// apart from the implementation, its pre-tests and the first verifier test. Nothing here imports
// 99_Tools/MergeGate; every expected decision is read with Bash semantics for where a push runs:
// - fix #1 (invariant): a cd or pushd gives its location only to commands that run only after it ran.
//   When the push can run although the cd did not, the location is unknown and the lookup push is
//   blocked (branch-lookup-failed, or push-main when the reader can tell it is main). A cd that always
//   runs keeps its location, for the rest of its && chain and past ; or a newline.
// - fix #2: text that Bash does not run as code (quotes, comments, here-document bodies) neither opens
//   nor closes a subshell or substitution scope, so a cd inside a subshell never moves a push outside
//   it. A here-document body is plain text to Bash just like a quoted string.
// - goal completion 2 and 6: the reverse layouts and the usual pushes keep their decisions.
//
// Environment: node and local git only. Each test owns a temporary project and throwaway repositories
// without a remote; commands are only handed to the hook as input. Special characters are built from
// code points. The Git Bash drive form exists only on win32 and is skipped elsewhere.
import assert from 'node:assert/strict';
import { join } from 'node:path';
import { test } from 'node:test';

import { assertDeny, assertNoDecision, bashInput, createProject, createRepository, runHook, runReport } from './hook-fixture.mjs';

const slashed = path => path.split('\\').join('/');
const driveForm = path => slashed(path).replace(/^([A-Za-z]):/, (_, drive) => `/${drive.toLowerCase()}`);
const BQ = String.fromCodePoint(0x60);
const WORK_BRANCH = 'verify/location';

async function decide(t, { cwd, command }) {
  const fixture = await createProject(t);
  const main = await createRepository(fixture.root, 'repo-main', 'main');
  const feature = await createRepository(fixture.root, 'repo-feature', WORK_BRANCH);
  const filled = command
    .split('<main-drive>').join(driveForm(main))
    .split('<feature-drive>').join(driveForm(feature))
    .split('<main>').join(slashed(main))
    .split('<feature>').join(slashed(feature))
    .split('<missing>').join(slashed(join(fixture.root, 'missing-repository')));
  return runHook(fixture, bashInput(fixture.project, filled, { cwd: { main, feature }[cwd] }));
}

// Fix #1 asks only that the push is blocked; the code depends on whether the reader can tell main.
function assertBlockedPush(run, label) {
  assert.equal(run.exit, 0, runReport(run, `${label}: exit 0`));
  let reason = '';
  try {
    reason = String(JSON.parse(run.stdout).hookSpecificOutput?.permissionDecisionReason ?? '');
  } catch {
    assert.fail(runReport(run, `${label}: a deny decision on stdout`));
  }
  assert.match(reason, /^merge-gate:(?:push-main|branch-lookup-failed) +\S/, runReport(run, `${label}: push-main or branch-lookup-failed`));
}

const cases = [
  // Fix #1 class beyond the contract rows: the cd may not run, the push runs anyway. From main.
  { name: '#1 a cd after && that follows another list, then ;', cwd: 'main', command: 'true; false && cd <feature>; git push origin HEAD', expect: 'blocked' },
  { name: '#1 a conditional cd, then a blank line', cwd: 'main', command: 'false && cd <feature>\n\ngit push origin HEAD', expect: 'blocked' },
  { name: '#1 a conditional pushd, then ;', cwd: 'main', command: 'false && pushd <feature>; git push origin HEAD', expect: 'blocked' },
  { name: '#1 a cd behind a test, then ;', cwd: 'main', command: '[ -d <missing> ] && cd <feature>; git push origin HEAD', expect: 'blocked' },
  { name: '#1 a conditional chain of cds, then ;', cwd: 'main', command: 'false && cd <feature> && cd .; git push origin HEAD', expect: 'blocked' },
  { name: '#1 a cd on its own line inside an if body', cwd: 'main', command: 'if false\nthen\n  cd <feature>\nfi\ngit push origin HEAD', expect: 'blocked' },
  { name: '#1 a conditional cd inside a brace group', cwd: 'main', command: '{ false && cd <feature>; }; git push origin HEAD', expect: 'blocked' },
  { name: '#1 a cd after a failing brace group', cwd: 'main', command: '{ false; } && cd <feature>; git push origin HEAD', expect: 'blocked' },
  { name: '#1 a cd inside an empty for loop', cwd: 'main', command: 'for d in; do cd <feature>; done; git push origin HEAD', expect: 'blocked' },

  // Fix #1 kept: a cd that always runs moves the push.
  { name: '#1 kept: cd, newline, usual push', cwd: 'main', command: 'cd <feature>\ngit push -u origin HEAD', expect: 'none' },
  { name: '#1 kept: chained cds then usual push', cwd: 'main', command: 'cd <feature> && cd . && git push -u origin HEAD', expect: 'none' },
  { name: '#1 kept: pushd then usual push', cwd: 'main', command: 'pushd <feature> && git push -u origin HEAD', expect: 'none' },
  { name: '#1 kept: cd main then ; push', cwd: 'feature', command: 'cd <main>; git push origin HEAD', expect: 'push-main' },
  { name: '#1 kept: cd main && true then ; push', cwd: 'feature', command: 'cd <main> && true; git push origin HEAD', expect: 'push-main' },
  { name: '#1 kept: cd main, blank line, push', cwd: 'feature', command: 'cd <main>\n\ngit push origin HEAD', expect: 'push-main' },

  // Fix #2: quoted and commented scope characters.
  { name: '#2 a quoted ) inside the subshell that reaches main', cwd: 'feature', command: '(cd <main> && echo "a)b" && git push origin HEAD)', expect: 'push-main' },
  { name: '#2 a single-quoted backquote inside the subshell that reaches main', cwd: 'feature', command: `(cd <main> && echo 'x${BQ}y' && git push origin HEAD)`, expect: 'push-main' },
  { name: '#2 a double-quoted ( inside a subshell, push outside', cwd: 'main', command: '(cd <feature> && echo "(") ; git push origin HEAD', expect: 'blocked' },
  { name: '#2 a single-quoted ( inside a subshell, push outside', cwd: 'main', command: "(cd <feature> && echo '(') ; git push origin HEAD", expect: 'blocked' },
  { name: '#2 a commented ( inside a subshell, push outside', cwd: 'main', command: '(cd <feature> && true # (\n) && git push origin HEAD', expect: 'blocked' },
  // Fix #2: here-document bodies are text too.
  { name: '#2 a here-document ( inside a subshell, push outside', cwd: 'main', command: "(cd <feature> && cat <<'EOF'\nfix (wip\nEOF\n); git push origin HEAD", expect: 'blocked' },
  { name: '#2 a here-document backquote inside a subshell, push outside', cwd: 'main', command: `(cd <feature> && cat <<'EOF'\nuse ${BQ}x\nEOF\n); git push origin HEAD`, expect: 'blocked' },
  { name: '#2 a commit message here-document ( inside a subshell, push outside', cwd: 'main', command: "(cd <feature> && git commit --allow-empty -F - <<'EOF'\nfix: gate (wip\nEOF\n) && git push origin HEAD", expect: 'blocked' },
  { name: '#2 a here-document code fence inside a subshell, push outside', cwd: 'main', command: `(cd <feature> && cat <<'EOF' > notes.md\n${BQ.repeat(3)}\ncode\nEOF\n) && git push origin HEAD`, expect: 'blocked' },

  // Every push of one command is judged.
  { name: 'two pushes: the second runs in main', cwd: 'feature', command: 'git push -u origin HEAD; cd <main>; git push origin HEAD', expect: 'push-main' },
  { name: 'two pushes: the first runs in main inside a subshell', cwd: 'feature', command: '(cd <main> && git push origin HEAD); git push -u origin HEAD', expect: 'push-main' },
  { name: 'kept: usual push from the work branch', cwd: 'feature', command: 'git push -u origin HEAD', expect: 'none' },
];

for (const { name, cwd, command, expect } of cases) {
  test(`from ${cwd}: ${name}`, async t => {
    const run = await decide(t, { cwd, command });
    if (expect === 'none') assertNoDecision(run, name);
    else if (expect === 'blocked') assertBlockedPush(run, name);
    else assertDeny(run, expect, name);
  });
}

const notWindows = process.platform !== 'win32' && 'the Git Bash drive form exists only on win32';

test('from main, win32: cd to the drive form of the work repository, then usual push', { skip: notWindows }, async t => {
  const run = await decide(t, { cwd: 'main', command: 'cd <feature-drive> && git push -u origin HEAD' });
  assertNoDecision(run, 'drive form of the work repository');
});

test('from feature, win32: cd to the drive form of main, then ; push', { skip: notWindows }, async t => {
  const run = await decide(t, { cwd: 'feature', command: 'cd <main-drive>; git push origin HEAD' });
  assertDeny(run, 'push-main', 'drive form of main after ;');
});

// Independent re-verification of fix round 2 (the narrow location grammar) for items 1 and 2 of
// 01_Phases/goals/2026-11-10-code-audit-gate-tools/goal.md, written by the verifier ([Rules 검증자])
// apart from the implementation, its tests and the earlier verifier tests. Nothing here imports
// 99_Tools/MergeGate. Every expected decision comes from running the same command template in Git
// for Windows bash with the push replaced by `git symbolic-ref --short HEAD`, recorded per case id in
// .backups/verification/2026-11-10-code-audit-gate-tools/pr-gate-fix2/verify/raw/oracle-attempt-2.log
// (the `id` after each case below). The rule is one-way, as in fix contract v3: when bash printed
// main, the hook must block the lookup push (push-main or branch-lookup-failed). Cases where bash
// printed a work branch and the hook must keep passing are the preserved behaviors.
//
// Cases marked "defect" reproduce verifier findings #1 to #5 of the round-2 verdict and fail until the
// product is fixed. They are kept as failing tests on purpose so that a fix shows up here.
//
// Environment: node and local git only. Each test owns a temporary project and throwaway repositories
// without a remote; commands are only handed to the hook as input, never run. Special characters are
// built from code points. Drive-less Windows paths (`/Users/...`) exist only on win32 and are skipped
// elsewhere with the reason in the test output.
import assert from 'node:assert/strict';
import { mkdir } from 'node:fs/promises';
import { join } from 'node:path';
import { test } from 'node:test';

import { assertDeny, assertNoDecision, bashInput, createProject, createRepository, runHook, runReport } from './hook-fixture.mjs';

const LF = String.fromCodePoint(10);
const FF = String.fromCodePoint(12);
const TAB = String.fromCodePoint(9);
const BS = String.fromCodePoint(92);
const slashed = path => path.split('\\').join('/');
const onlyWindows = process.platform === 'win32' ? false : 'drive-less paths exist only on win32';

// Repositories by role: main and a work branch side by side, a work-branch repository inside main
// (fsub), and a work-branch repository under a folder whose name holds a space and parentheses.
async function decide(t, { cwd, command }) {
  const fixture = await createProject(t);
  const main = slashed(await createRepository(fixture.root, 'repo-main', 'main'));
  const feature = slashed(await createRepository(fixture.root, 'repo-feature', 'verify/feature'));
  if (command.includes('fsub')) await createRepository(fixture.root, 'repo-main/fsub', 'verify/nested');
  if (command.includes('paren (1)')) {
    await mkdir(join(fixture.root, 'paren (1)'));
    await createRepository(fixture.root, 'paren (1)/repo', 'verify/paren');
  }
  const filled = command
    .split('<feature-nodrive>').join(feature.slice(2))
    .split('<root>').join(slashed(fixture.root))
    .split('<main>').join(main)
    .split('<feature>').join(feature);
  return runHook(fixture, bashInput(fixture.project, filled, { cwd: { main, feature }[cwd] }));
}

function assertBlockedPush(run, label) {
  assert.equal(run.exit, 0, runReport(run, `${label}: exit 0`));
  let reason = '';
  try {
    reason = String(JSON.parse(run.stdout).hookSpecificOutput?.permissionDecisionReason ?? '');
  } catch {
    assert.fail(runReport(run, `${label}: bash ran this push on main, so a deny decision is expected`));
  }
  assert.match(reason, /^merge-gate:(?:push-main|branch-lookup-failed) +\S/, runReport(run, `${label}: push-main or branch-lookup-failed`));
}

const cases = [
  // Defect #1: a cd that fails in bash leaves the push in main, but Windows git still resolves the
  // same path for the lookup (it drops a trailing dot or space, and reads a drive-less /Users/... as
  // the current drive while bash reads it under the Git install). The README assumes a failed cd
  // makes its lookup fail too. From main; bash printed main for each.
  { name: 'defect #1: cd to a path with a trailing dot, then ;', cwd: 'main', command: 'cd <feature>.; git push origin HEAD', expect: 'blocked', source: 'f-dot-semi' },
  { name: 'defect #1: cd to a path with a trailing dot, then a newline', cwd: 'main', command: `cd <feature>.${LF}git push origin HEAD`, expect: 'blocked', source: 'f-dot-nl' },
  { name: 'defect #1: cd to a path with a trailing dot, then || true;', cwd: 'main', command: 'cd <feature>. || true; git push origin HEAD', expect: 'blocked', source: 'f-dot-or' },
  { name: 'defect #1: cd to a quoted path with a trailing space, then ;', cwd: 'main', command: 'cd "<feature> "; git push origin HEAD', expect: 'blocked', source: 'f-space-semi' },
  { name: 'defect #1: cd through a folder name with a trailing dot', cwd: 'main', command: 'cd <root>./repo-feature; git push origin HEAD', expect: 'blocked', source: 'f-mid-dot-semi' },
  { name: 'defect #1: pushd to a path with a trailing dot, then ;', cwd: 'main', command: 'pushd <feature>.; git push origin HEAD', expect: 'blocked', source: 'f-pushd-dot' },
  { name: 'defect #1: cd to a drive-less absolute path, then ;', cwd: 'main', command: 'cd <feature-nodrive>; git push origin HEAD', expect: 'blocked', source: 'f-nodrive-semi', skip: onlyWindows },
  { name: 'defect #1: a good cd then a failing drive-less cd, from the work repository', cwd: 'feature', command: 'cd <main>/ && cd <feature-nodrive>; git push -u origin HEAD', expect: 'blocked', source: 'g7203-224', skip: onlyWindows },

  // Defect #2: the push-count cross-check compares only counts. A push the narrow reading sees but
  // the word reading misses (a partly quoted subcommand, a -C path with parentheses) balances a push
  // the word reading sees but the narrow reading cannot place (inside bash -c), so no unknown
  // location is added. bash printed main (from the bash -c push) for each.
  { name: 'defect #2: bash -c push, then cd and a partly quoted push', cwd: 'main', command: "bash -c \"git push origin HEAD\"; cd <feature> && git pu'sh' origin HEAD", expect: 'blocked', source: 'x-count-q-main' },
  { name: 'defect #2: a subshell cd with a partly quoted push, then a bash -c push', cwd: 'main', command: "(cd <feature> && git pu'sh' origin HEAD); bash -c \"git push origin HEAD\"", expect: 'blocked', source: 'x-count-q-sub' },
  { name: 'defect #2: bash -c cd to main, then cd and a partly quoted push', cwd: 'feature', command: "bash -c 'cd <main>; git push origin HEAD'; cd <feature> && git pu'sh' origin HEAD", expect: 'blocked', source: 'x-count-q-feature' },
  { name: 'defect #2: bash -c cd to main, then a -C push into a folder with parentheses', cwd: 'feature', command: "bash -c 'cd <main>; git push origin HEAD'; git -C \"<root>/paren (1)/repo\" push origin HEAD", expect: 'blocked', source: 'x-count-paren' },

  // Defect #3: env -C and env --chdir move the push, but the location reading keeps the cd path or
  // the input cwd. bash printed main for each.
  { name: 'defect #3: cd to the work repository, then env -C main', cwd: 'main', command: 'cd <feature> && env -C <main> git push origin HEAD', expect: 'blocked', source: 'w-env-c-main' },
  { name: 'defect #3: cd to the work repository, then env --chdir=main', cwd: 'main', command: 'cd <feature> && env --chdir=<main> git push origin HEAD', expect: 'blocked', source: 'w-env-chdir' },
  { name: 'defect #3: env -C main from the work repository after a cd', cwd: 'feature', command: 'cd <feature> && env -C <main> git push origin HEAD', expect: 'blocked', source: 'w-env-c-feature' },
  { name: 'defect #3: env -C main from the work repository without a cd', cwd: 'feature', command: 'env -C <main> git push origin HEAD', expect: 'blocked', source: 'w-env-c-nocd' },

  // Defect #4: a line continuation inside the cd word. bash joins it into cd; the cd-word check strips
  // quotes and backslashes but keeps the newline, so the command reads as having no cd.
  { name: 'defect #4: c, backslash-newline, d to main, then &&', cwd: 'feature', command: `c${BS}${LF}d <main> && git push origin HEAD`, expect: 'blocked', source: 'w-split-cd-feature' },
  { name: 'defect #4: c, backslash-newline, d to main, then ;', cwd: 'feature', command: `c${BS}${LF}d <main>; git push origin HEAD`, expect: 'blocked', source: 'w-split-cd-semi' },

  // Defect #5: a partly quoted push subcommand is not found by the word reading, so nothing is looked
  // up. With a cd word only in text and a form feed, the narrow reading cannot read the command either.
  { name: "defect #5: git pu'sh' origin HEAD from main", cwd: 'main', command: "git pu'sh' origin HEAD", expect: 'blocked', source: 'x-q-nocd-main' },
  { name: "defect #5: git pu'sh' after quoted cd text and a form feed", cwd: 'main', command: `true;${LF}echo "(cd y" || echo "(1)"${FF}&& git pu'sh' origin HEAD`, expect: 'blocked', source: 'g9157-354' },

  // Kept: a relative cd that an inherited CDPATH can send elsewhere stays unknown. With CDPATH set,
  // bash printed main (c-cdpath-to-main); without it, bash printed the nested work branch
  // (c-no-cdpath-rel). The hook cannot see the shell CDPATH, so both must be blocked.
  { name: 'kept: a bare relative cd is blocked because CDPATH can move it', cwd: 'main', command: 'cd fsub && git push origin HEAD', expect: 'blocked', source: 'c-cdpath-to-main' },
  // Kept: the retyping the block reason gives. bash printed the work branch for each.
  { name: 'kept: cd ./relative then the usual push passes', cwd: 'main', command: 'cd ./fsub && git push origin HEAD', expect: 'none', source: 'q-dot-rel-main' },
  { name: 'kept: cd to the work repository then the usual push passes', cwd: 'main', command: 'cd <feature> && git push -u origin HEAD', expect: 'none', source: 'p1' },
  { name: 'kept: a tab after cd still gives the location', cwd: 'main', command: `cd${TAB}<feature>; git push origin HEAD`, expect: 'none', source: 'lp-tab-after-cd' },
  { name: 'kept: a subshell cd to main with a quoted ) in a commit message', cwd: 'feature', command: '(cd <main> && git commit -m "1) fix" && git push origin HEAD)', expect: 'push-main', source: 'b1' },
  { name: 'kept: a here-document ( inside a subshell, push outside', cwd: 'main', command: `(cd <feature> && cat <<'EOF'${LF}fix (wip${LF}EOF${LF}); git push origin HEAD`, expect: 'blocked', source: 'n1' },
];

for (const { name, cwd, command, expect, source, skip = false } of cases) {
  test(`${name} [oracle ${source}]`, { skip }, async t => {
    const run = await decide(t, { cwd, command });
    const label = `${name} (from ${cwd}, oracle ${source})`;
    if (expect === 'blocked') assertBlockedPush(run, label);
    else if (expect === 'push-main') assertDeny(run, 'push-main', label);
    else assertNoDecision(run, label);
  });
}

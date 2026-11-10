// Independent verification for items 1 and 2 of 01_Phases/goals/2026-11-10-code-audit-gate-tools/goal.md,
// written by the verifier ([Rules 검증자]) apart from the implementation and its pre-tests.
// Every expected decision comes from the goal's observable completion conditions, read with Bash
// semantics for where a push actually runs. Nothing here imports 99_Tools/MergeGate:
// - 2: a same-command cd, -C or --git-dir that reaches main is push-main; the reverse layout gets
//   no decision.
// - 3: a lookup push whose repository the text cannot settle is blocked (branch-lookup-failed, or
//   push-main when the reader can tell it is main); an explicit destination needs no lookup.
// - 4: in-scope gh api writes are api-merge and reads get no decision.
// - 6: the lead's usual push, an explicit work-branch push and special whitespace inside quotes
//   keep their decisions.
//
// Environment: node and local git only. Each test owns a temporary project and throwaway
// repositories without a remote; commands are only handed to the hook as input. Special
// characters are built from code points.
import assert from 'node:assert/strict';
import { join } from 'node:path';
import { test } from 'node:test';

import { assertDeny, assertNoDecision, bashInput, createProject, createRepository, runHook, runReport } from './hook-fixture.mjs';

const slashed = path => path.split('\\').join('/');
const driveForm = path => slashed(path).replace(/^([A-Za-z]):/, (_, drive) => `/${drive.toLowerCase()}`);
const NBSP = String.fromCodePoint(0xa0);
const TAB = String.fromCodePoint(0x09);
const CRLF = String.fromCodePoint(0x0d, 0x0a);
const WORK_BRANCH = 'verify/work';

const monitorInput = (project, command, options) => ({
  ...bashInput(project, '', options),
  tool_name: 'Monitor',
  tool_input: { command, description: 'merge gate verification input', timeout_ms: 60000 },
});

async function workspace(t) {
  const fixture = await createProject(t);
  const main = await createRepository(fixture.root, 'repo-main', 'main');
  const feature = await createRepository(fixture.root, 'repo-feature', WORK_BRANCH);
  const fill = command => command
    .split('<main-drive>').join(driveForm(main))
    .split('<main>').join(slashed(main))
    .split('<feature>').join(slashed(feature))
    .split('<missing>').join(slashed(join(fixture.root, 'missing-repository')));
  return { fixture, cwd: { main, feature }, fill };
}

async function decide(t, { cwd, command, tool = 'Bash' }) {
  const { fixture, cwd: places, fill } = await workspace(t);
  const make = tool === 'Monitor' ? monitorInput : bashInput;
  return runHook(fixture, make(fixture.project, fill(command), { cwd: places[cwd] }));
}

// Completion 3 asks only that the push is blocked; which of the two codes depends on whether the
// reader can tell the repository is main.
function assertBlockedPush(run, label) {
  const reason = (() => {
    try {
      return String(JSON.parse(run.stdout).hookSpecificOutput?.permissionDecisionReason ?? '');
    } catch {
      return '';
    }
  })();
  assert.ok(/^merge-gate:(?:push-main|branch-lookup-failed) /.test(reason), runReport(run, `${label}: blocked as push-main or branch-lookup-failed`));
}

const cases = [
  // Completion 2: the repository the push reaches decides, the reverse layout passes.
  { label: 'cd on its own line, then a lookup push', cwd: 'feature', command: `cd <main>\ngit push origin HEAD`, expect: 'push-main' },
  { label: '--git-dir decides even with a work tree elsewhere', cwd: 'feature', command: 'git --work-tree=<feature> --git-dir=<main>/.git push -u origin HEAD', expect: 'push-main' },
  { label: 'relative cd steps add up', cwd: 'feature', command: 'cd .. && cd repo-main && git push origin HEAD', expect: 'push-main' },
  { label: 'cd into the work repository', cwd: 'main', command: 'cd <feature> && git push -u origin HEAD', expect: 'none' },
  { label: 'a cd inside a closed subshell does not move the later push', cwd: 'main', command: '(cd <feature>) && git push origin HEAD', expect: 'push-main' },
  { label: 'a numbered commit message inside a subshell that changed into main', cwd: 'feature',
    command: '(cd <main> && git commit -m "1) fix" && git push origin HEAD)', expect: 'push-main' },
  // Completion 3: a switch the text cannot settle blocks a lookup push; explicit destinations as before.
  { label: 'a cd that runs only if the command before it succeeds', cwd: 'main',
    command: 'false && cd <feature>; git push origin HEAD', expect: 'blocked' },
  { label: 'a cd to a variable path', cwd: 'feature', command: 'cd "$TARGET" && git push origin HEAD', expect: 'branch-lookup-failed' },
  { label: 'a missing -C path', cwd: 'feature', command: 'git -C <missing> push origin HEAD', expect: 'branch-lookup-failed' },
  { label: 'an explicit work-branch destination after an unknown switch', cwd: 'feature',
    command: `cd "$TARGET" && git push origin ${WORK_BRANCH}`, expect: 'none' },
  { label: 'an explicit main destination after an unknown switch', cwd: 'feature', command: 'cd "$TARGET" && git push origin HEAD:main', expect: 'push-main' },
  // Completion 4: in-scope gh api writes and reads.
  { label: 'delete the main ref with placeholders', cwd: 'feature', command: 'gh api -X DELETE repos/{owner}/{repo}/git/refs/heads/main', expect: 'api-merge' },
  { label: 'move the main ref with --method and a typed field', cwd: 'feature', command: 'gh api --method PATCH repos/o/r/git/refs/heads/main -F sha=abc', expect: 'api-merge' },
  { label: 'merge a branch with --input', cwd: 'feature', command: 'gh api repos/o/r/merges --input body.json', expect: 'api-merge' },
  { label: 'GraphQL createCommitOnBranch', cwd: 'feature',
    command: "gh api graphql -f query='mutation { createCommitOnBranch(input: {}) { commit { oid } } }'", expect: 'api-merge' },
  { label: 'Monitor runs a branch merge write', cwd: 'feature', tool: 'Monitor', command: 'gh api -X POST repos/o/r/merges -f base=main -f head=x', expect: 'api-merge' },
  { label: 'read the main ref with --jq', cwd: 'feature', command: 'gh api repos/o/r/git/refs/heads/main --jq .object.sha', expect: 'none' },
  { label: 'read the main ref with an explicit GET', cwd: 'feature', command: 'gh api -X GET repos/o/r/git/refs/heads/main', expect: 'none' },
  { label: 'read the main branch', cwd: 'feature', command: 'gh api repos/o/r/branches/main', expect: 'none' },
  // Completion 6: preserved decisions.
  { label: 'the usual work-branch push', cwd: 'feature', command: 'git push -u origin HEAD', expect: 'none' },
  { label: 'an explicit work-branch push from main', cwd: 'main', command: `git push -u origin ${WORK_BRANCH}`, expect: 'none' },
  { label: 'NBSP inside a quoted commit message, then the usual push', cwd: 'feature',
    command: `git commit -m "a${NBSP}b" && git push -u origin HEAD`, expect: 'none' },
  { label: 'NBSP inside quotes still lets a main lookup block', cwd: 'main',
    command: `git commit -m "a${NBSP}b" && git push -u origin HEAD`, expect: 'push-main' },
  { label: 'tabs and CRLF around the usual push', cwd: 'feature', command: `git${TAB}push${TAB}-u origin HEAD${CRLF}`, expect: 'none' },
];

for (const { label, cwd, command, tool, expect } of cases) {
  test(`from ${cwd}, ${label}`, async t => {
    const run = await decide(t, { cwd, command, tool });
    if (expect === 'none') assertNoDecision(run, label);
    else if (expect === 'blocked') assertBlockedPush(run, label);
    else assertDeny(run, expect, label);
  });
}

test('from feature, win32: Git Bash drive form of main', async t => {
  if (process.platform !== 'win32') {
    t.skip('the Git Bash drive form exists only on win32');
    return;
  }
  const run = await decide(t, { cwd: 'feature', command: 'cd <main-drive> && git push origin HEAD' });
  assertDeny(run, 'push-main', 'drive form of main');
});

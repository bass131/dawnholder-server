// Regressions for item 2 of 01_Phases/goals/2026-11-10-code-audit-gate-tools/goal.md: besides the
// pull request merge call, gh api must not reach a branch merge, a write to the main ref or the
// GraphQL mutations that move a branch. Expected decisions come from that goal and its test
// contract, not from 99_Tools/MergeGate:
// - a write of these shapes read as a gh api call is api-merge;
// - a shape that only the last net can see (a --hostname value before api) is suspect-words;
// - reads of the same paths and the same words outside gh api get no decision.
//
// Environment: node only. Each test owns a temporary project passed as CLAUDE_PROJECT_DIR. Commands
// are only handed to the hook as input; none of them is executed.
import { test } from 'node:test';

import { assertDeny, assertNoDecision, bashInput, createProject, HEAD_A, runHook } from './hook-fixture.mjs';

const branchMerge = 'repos/o/r/merges -f base=main -f head=feat/x';
const mainRefWrite = `-X PATCH repos/o/r/git/refs/heads/main -f sha=${HEAD_A}`;
const mergeBranchMutation = 'graphql -f query=\'mutation { mergeBranch(input: {repositoryId: "x", base: "main", head: "feat/x"}) { clientMutationId } }\'';
const representatives = [branchMerge, mainRefWrite, mergeBranchMutation];

const writesReadAsApiCalls = [
  `gh api ${branchMerge}`,
  'gh api -X POST repos/o/r/merges -f base=main -f head=feat/x',
  `gh api ${mainRefWrite}`,
  `gh api --method PATCH repos/o/r/git/refs/heads/main -f sha=${HEAD_A}`,
  'gh api --method=DELETE repos/o/r/git/refs/heads/main',
  `gh api -XPATCH repos/o/r/git/refs/heads/main -f sha=${HEAD_A}`,
  'gh api repos/o/r/git/refs/heads/main -F force=true',
  'gh api repos/o/r/git/refs/heads/main --input body.json',
  `gh api ${mergeBranchMutation}`,
  `gh api graphql -f query='mutation { updateRef(input: {refId: "x", oid: "${HEAD_A}"}) { clientMutationId } }'`,
  `gh api graphql -f query='mutation { createCommitOnBranch(input: {branch: {branchName: "main"}, expectedHeadOid: "${HEAD_A}", message: {headline: "x"}, fileChanges: {}}) { clientMutationId } }'`,
];

// Spellings of the command word, each given once with a representative of the three write families.
const commandSpellings = [
  rest => `gh.exe api ${rest}`,
  rest => `GH api ${rest}`,
  rest => `C:/bin/gh.exe api ${rest}`,
  rest => `gh -R o/r api ${rest}`,
  rest => `(gh api ${rest})`,
];

for (const command of [...writesReadAsApiCalls, ...commandSpellings.flatMap(spell => representatives.map(spell))]) {
  test(`api-merge: ${command}`, async t => {
    const fixture = await createProject(t);
    assertDeny(runHook(fixture, bashInput(fixture.project, command)), 'api-merge', command);
  });
}

for (const command of representatives.map(rest => `gh --hostname github.com api ${rest}`)) {
  test(`suspect-words: ${command}`, async t => {
    const fixture = await createProject(t);
    assertDeny(runHook(fixture, bashInput(fixture.project, command)), 'suspect-words', command);
  });
}

const readsAndOtherWords = [
  'gh api repos/o/r/git/refs/heads/main',
  'gh api repos/o/r/git/ref/heads/main',
  'gh api -X GET repos/o/r/git/refs/heads/main',
  'gh api --method GET repos/o/r/git/refs/heads/main',
  'gh api repos/o/r/pulls/12',
  'gh api graphql -f query=\'query { repository(owner: "o", name: "r") { pullRequest(number: 12) { mergeStateStatus headRefOid } } }\'',
  'gh api graphql -f query=\'query { repository(owner: "o", name: "r") { ref(qualifiedName: "refs/heads/main") { target { oid } } } }\'',
  'echo updateRef',
  'git log --grep=mergeBranch',
];

for (const command of readsAndOtherWords) {
  test(`no decision: ${command}`, async t => {
    const fixture = await createProject(t);
    assertNoDecision(runHook(fixture, bashInput(fixture.project, command)), command);
  });
}

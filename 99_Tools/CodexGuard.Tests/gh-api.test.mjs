// Requirement tests for pull request merge and auto-merge changes sent through `gh api`.
// Expected outcomes come from the approved texts, not from 99_Tools/CodexGuard:
// - contract E/guard-pretest-contract-v1.md (SHA256 7fea9bfb…14d2) requirement 2: the REST pull
//   request merge change and the GraphQL merge / auto-merge changes are judged from the explicit
//   method, endpoint and inline data in any argument order; read calls and words inside arguments
//   are not mistaken for a change;
// - goal 「만들 것」 6: gh api PR merge/auto-merge change calls are blocked, ordinary reads kept;
// - the lead's design supplement 1 (E/guard-pretest-design-supplement-01.md, Q4 (ii)(iii), reply
//   to msg_cff106ac428f): on the PR merge endpoint an explicit GET is kept and any changing
//   method, or the default POST that fields cause, is blocked; enqueuePullRequest (merge queue)
//   is blocked and disablePullRequestAutoMerge (cancelling the schedule) is kept.
// Facts about the API used to derive the samples:
// - REST: PUT /repos/{owner}/{repo}/pulls/{pull_number}/merge merges; GET on the same path only
//   reports whether the pull request is merged. `gh api` sends GET unless a method is given or
//   fields are added.
// - GraphQL: mergePullRequest merges and enablePullRequestAutoMerge schedules a merge; a `query`
//   operation cannot change anything.
// Bodies read from files or stdin (`--input`, `-F field=@file`) are outside the explicit text and
// listed as a limit in the requirement table, not tested here.
import { test } from 'node:test';

import { createFixture, createRepository, expectBlocked, expectKept, HEAD_SHA, PR } from './hook-fixture.mjs';

const pullMerge = `repos/owner/repo/pulls/${PR}/merge`;
const pullId = 'PR_kwDOcodexguard';
const mergeMutation = `mutation { mergePullRequest(input: {pullRequestId: "${pullId}"}) { clientMutationId } }`;
const autoMergeMutation = `mutation { enablePullRequestAutoMerge(input: {pullRequestId: "${pullId}", mergeMethod: SQUASH}) { clientMutationId } }`;

test('requirement 2: REST PUT on the pull request merge endpoint is blocked in every method and endpoint spelling', async t => {
  const fixture = await createFixture(t);
  const repository = await createRepository(fixture);
  await expectBlocked(t, fixture, repository.directory, [
    `gh api -X PUT ${pullMerge}`,
    `gh api --method PUT ${pullMerge} -f merge_method=squash`,
    `gh api --method=PUT /${pullMerge}`,
    `gh api ${pullMerge} -X PUT -f sha=${HEAD_SHA}`,
    `gh api -XPUT 'repos/{owner}/{repo}/pulls/${PR}/merge'`,
    `gh api "https://api.github.com/${pullMerge}" --method PUT`,
    `gh api -H "Accept: application/vnd.github+json" -H "X-GitHub-Api-Version: 2022-11-28" --method PUT ${pullMerge}`,
    `gh api --hostname github.com -X PUT ${pullMerge}`,
    `gh api -X PUT ${pullMerge} -F merge_method=rebase --silent`,
    `/usr/bin/gh api -X PUT ${pullMerge}`,
  ]);
});

test('supplement 1 Q4 (ii): other changing methods and the default POST from fields on the merge endpoint are blocked', async t => {
  const fixture = await createFixture(t);
  const repository = await createRepository(fixture);
  await expectBlocked(t, fixture, repository.directory, [
    `gh api ${pullMerge} -f merge_method=squash`,
    `gh api ${pullMerge} -F sha=${HEAD_SHA}`,
    `gh api -X POST ${pullMerge}`,
    `gh api --method POST ${pullMerge} -f commit_title="merge"`,
    `gh api -X PATCH ${pullMerge}`,
    `gh api --method=DELETE ${pullMerge}`,
  ]);
});

test('supplement 1 Q4 (iii): GraphQL enqueuePullRequest is blocked and disablePullRequestAutoMerge is kept', async t => {
  const fixture = await createFixture(t);
  const repository = await createRepository(fixture);
  await t.test('merge queue', async t => {
    await expectBlocked(t, fixture, repository.directory, [
      `gh api graphql -f query='mutation { enqueuePullRequest(input: {pullRequestId: "${pullId}"}) { mergeQueueEntry { id } } }'`,
      `gh api graphql -f query='mutation($id: ID!) { enqueuePullRequest(input: {pullRequestId: $id, jump: true}) { mergeQueueEntry { position } } }' -f id=${pullId}`,
    ]);
  });
  await t.test('cancelling auto-merge', async t => {
    await expectKept(t, fixture, repository.directory, [
      `gh api graphql -f query='mutation { disablePullRequestAutoMerge(input: {pullRequestId: "${pullId}"}) { clientMutationId } }'`,
    ]);
  });
});

test('requirement 2: GraphQL mergePullRequest and enablePullRequestAutoMerge are blocked in every field and order spelling', async t => {
  const fixture = await createFixture(t);
  const repository = await createRepository(fixture);
  await expectBlocked(t, fixture, repository.directory, [
    `gh api graphql -f query='${mergeMutation}'`,
    `gh api graphql -f query='${autoMergeMutation}'`,
    `gh api graphql -F query='${mergeMutation}'`,
    `gh api graphql --raw-field query='${mergeMutation}'`,
    `gh api graphql --field 'query=${autoMergeMutation}'`,
    `gh api -f query='${mergeMutation}' graphql`,
    `gh api graphql -X POST -f query='${autoMergeMutation}'`,
    `gh api https://api.github.com/graphql -f query='${mergeMutation}'`,
    `gh api graphql -f query='mutation($id: ID!) { mergePullRequest(input: {pullRequestId: $id, mergeMethod: SQUASH}) { pullRequest { merged } } }' -f id=${pullId}`,
    `gh api graphql -f query='mutation MergeIt { mergePullRequest(input: {pullRequestId: "${pullId}"}) { clientMutationId } }'`,
    `gh api graphql -f query='mutation { done: mergePullRequest(input: {pullRequestId: "${pullId}"}) { clientMutationId } }'`,
    `gh api graphql -f query='mutation{enablePullRequestAutoMerge(input:{pullRequestId:"${pullId}"}){clientMutationId}}'`,
    `gh api graphql -f query="mutation { mergePullRequest(input: {pullRequestId: \\"${pullId}\\"}) { clientMutationId } }"`,
    `gh api graphql -f query='\nmutation {\n  mergePullRequest(input: {pullRequestId: "${pullId}"}) {\n    clientMutationId\n  }\n}\n'`,
  ]);
});

test('requirement 2: REST and GraphQL reads, and changes that are not a merge, are kept', async t => {
  const fixture = await createFixture(t);
  const repository = await createRepository(fixture);
  await expectKept(t, fixture, repository.directory, [
    `gh api repos/owner/repo/pulls/${PR}`,
    `gh api ${pullMerge}`,
    `gh api -X GET ${pullMerge}`,
    'gh api repos/owner/repo/pulls --paginate --jq ".[].title"',
    `gh api repos/owner/repo/pulls/${PR}/reviews`,
    'gh api repos/owner/repo/branches/main/protection',
    `gh api -X PATCH repos/owner/repo/pulls/${PR} -f title="merge guard follow-up"`,
    `gh api repos/owner/repo/pulls/${PR}/commits --jq '.[].commit.message | select(test("merge"))'`,
    `gh api -X POST repos/owner/repo/issues/${PR}/comments -f body="please gh pr merge after approval"`,
    `gh api graphql -f query='query { repository(owner: "owner", name: "repo") { pullRequest(number: ${PR}) { mergeable merged mergeStateStatus autoMergeRequest { enabledAt } } } }'`,
    `gh api graphql -f query='{ viewer { login } }'`,
    `gh api graphql -f query='query { search(query: "mergePullRequest enablePullRequestAutoMerge", type: ISSUE, first: 1) { issueCount } }'`,
    `gh api graphql -f query='mutation { addComment(input: {subjectId: "${pullId}", body: "ready to merge; mergePullRequest after approval"}) { clientMutationId } }'`,
  ]);
});

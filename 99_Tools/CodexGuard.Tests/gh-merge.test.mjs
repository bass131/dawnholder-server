// Requirement tests for the gh pull request merge command. Expected outcomes come from the
// approved texts, not from 99_Tools/CodexGuard:
// - contract E/guard-pretest-contract-v1.md (SHA256 7fea9bfb…14d2) requirement 1 (merge and
//   auto-merge scheduling are blocked for every session role) and 2 (the gh merge command and its
//   auto option, in every interpretable spelling: option order, executable path, quoting and
//   whitespace; reads and words inside arguments are not mistaken for a change);
// - contract requirement 3 and goal 「만들 것」 6: gh pr create/view/checks and ordinary reads
//   are kept;
// - scope draft v1 61행: words in PR titles and bodies are not a reason to block;
// - the lead's design supplement 1 (E/guard-pretest-design-supplement-01.md, Q4 (i), reply to
//   msg_cff106ac428f): the whole gh pr merge surface is forbidden, --disable-auto included.
// Shell compounds and wrappers are in shell-forms.test.mjs.
import { test } from 'node:test';

import { createFixture, createRepository, expectBlocked, expectKept, HEAD_SHA, PR } from './hook-fixture.mjs';

test('requirements 1 and 2: gh pr merge with any target, method, auto or admin option is blocked', async t => {
  const fixture = await createFixture(t);
  const repository = await createRepository(fixture);
  await expectBlocked(t, fixture, repository.directory, [
    'gh pr merge',
    `gh pr merge ${PR}`,
    `gh pr merge ${PR} --merge`,
    `gh pr merge ${PR} --squash --delete-branch`,
    `gh pr merge ${PR} --rebase --admin`,
    `gh pr merge ${PR} -s -d`,
    `gh pr merge --auto --squash ${PR}`,
    `gh pr merge ${PR} --auto`,
    `gh pr merge ${PR} --auto --merge --match-head-commit ${HEAD_SHA}`,
    `gh pr merge ${PR} --merge --match-head-commit ${HEAD_SHA}`,
    `gh pr merge https://github.com/owner/repo/pull/${PR} --squash`,
    'gh pr merge feat/guard --merge',
    `gh pr merge -R owner/repo ${PR} --merge`,
    `gh pr merge ${PR} --repo=owner/repo --squash`,
    `gh pr -R owner/repo merge ${PR}`,
    `gh pr merge ${PR} --subject "Codex guard" --body "merged after checks"`,
    `gh pr merge ${PR} --disable-auto`,
  ]);
});

test('requirement 2: spellings of the gh executable, quoting and whitespace are blocked', async t => {
  const fixture = await createFixture(t);
  const repository = await createRepository(fixture);
  await expectBlocked(t, fixture, repository.directory, [
    `gh  pr   merge ${PR}`,
    `gh\tpr\tmerge ${PR}`,
    `   gh pr merge ${PR}   `,
    `gh "pr" "merge" ${PR}`,
    `gh 'pr' 'merge' ${PR}`,
    `"gh" pr merge ${PR}`,
    `/usr/bin/gh pr merge ${PR}`,
    `gh.exe pr merge ${PR}`,
    `"C:\\Program Files\\GitHub CLI\\gh.exe" pr merge ${PR}`,
    `"C:/Program Files/GitHub CLI/gh.exe" pr merge ${PR} --auto`,
  ]);
});

test('requirements 2 and 3: gh pr create, view, checks and other non-merge gh commands are kept', async t => {
  const fixture = await createFixture(t);
  const repository = await createRepository(fixture);
  await expectKept(t, fixture, repository.directory, [
    `gh pr view ${PR}`,
    `gh pr view ${PR} --json state,mergeable,mergeStateStatus,mergedAt,autoMergeRequest`,
    `gh pr checks ${PR}`,
    `gh pr checks ${PR} --watch --fail-fast`,
    'gh pr status',
    'gh pr list --state merged --limit 5',
    `gh pr diff ${PR}`,
    'gh pr create --base main --head feat/guard --title "Codex guard: block gh pr merge" --body "Blocks gh pr merge --auto and git push origin main."',
    'gh pr create --fill --base main',
    `gh pr comment ${PR} --body "ready for gh pr merge after approval"`,
    `gh pr edit ${PR} --title "merge guard follow-up"`,
    'gh repo view --json defaultBranchRef',
    'gh run list --branch main',
    'gh issue create --title "auto merge question" --body "gh pr merge --auto stays blocked"',
  ]);
});

// Regressions for item 1 of 01_Phases/goals/2026-11-10-code-audit-gate-tools/goal.md: a push that
// needs a branch lookup is judged by the repository it actually reaches, not by the cwd of the
// call. Expected decisions come from that goal and its test contract, not from 99_Tools/MergeGate:
// - a -C, --git-dir, --work-tree, cd or pushd that reaches main is push-main, and one that reaches
//   a feature branch from a main checkout gets no decision;
// - a closed subshell returns to the cwd of the call;
// - a repository switch that cannot be resolved is branch-lookup-failed for a push that needs the
//   lookup, and an explicit destination needs no lookup.
//
// Environment: node and local git only. Each test owns a temporary project and throwaway
// repositories without a remote, and the commands are only handed to the hook as input; nothing is
// pushed. The hook runs in the fixture's environment, whose git ceiling keeps lookups inside it.
import { join } from 'node:path';
import { test } from 'node:test';

import { assertDeny, assertNoDecision, bashInput, createProject, createRepository, runHook } from './hook-fixture.mjs';

const slashed = path => path.split('\\').join('/');

// Git Bash drive form of a Windows path: C:/a/b -> /c/a/b.
const driveForm = path => slashed(path).replace(/^([A-Za-z]):/, (_, drive) => `/${drive.toLowerCase()}`);

const monitorInput = (project, command, options) => ({
  ...bashInput(project, '', options),
  tool_name: 'Monitor',
  tool_input: { command, description: 'merge gate regression input', timeout_ms: 60000 },
});

async function workspace(t, { spacedRepository = false } = {}) {
  const fixture = await createProject(t);
  const main = await createRepository(fixture.root, 'repo-main', 'main');
  const feature = await createRepository(fixture.root, 'repo-feature', 'feat/x');
  const spaced = spacedRepository ? await createRepository(fixture.root, 'repo with space', 'feat/x') : null;
  return {
    fixture,
    cwd: { main, feature },
    placeholders: {
      '<main>': slashed(main),
      '<feature>': slashed(feature),
      '<feature-drive>': driveForm(feature),
      '<parent>': slashed(fixture.root),
      '<missing>': `${slashed(fixture.root)}/no-such-folder`,
      '<spaced>': spaced ? slashed(spaced) : '',
    },
  };
}

const fill = (template, placeholders) => template.replace(/<[a-z-]+>/g, name => placeholders[name] ?? name);
const shownInName = template => template.replaceAll('\n', '<newline>');

// `expected` is a block code, or null for decision none.
async function judge(t, template, location, expected, buildInput = bashInput, options = {}) {
  const { fixture, cwd, placeholders } = await workspace(t, options);
  const command = fill(template, placeholders);
  const run = runHook(fixture, buildInput(fixture.project, command, { cwd: cwd[location] }));
  const label = `${JSON.stringify(command)} from repo-${location}`;
  if (expected) assertDeny(run, expected, label);
  else assertNoDecision(run, label);
}

const groups = [
  {
    title: 'on a feature branch, push-main', location: 'feature', expected: 'push-main',
    templates: [
      'git -C <main> push',
      'git -C <main> push origin HEAD',
      'git -C <main> push origin @',
      'git --git-dir=<main>/.git push origin HEAD',
      'git --git-dir <main>/.git push origin HEAD',
      'git --git-dir <main>/.git --work-tree <main> push origin HEAD',
      'git -C <parent> -C repo-main push origin HEAD',
      'cd <main> && git push origin HEAD',
      'cd <main>; git push',
      'cd <main>\ngit push origin HEAD',
      'pushd <main> && git push origin HEAD',
      '(cd <main> && git push origin HEAD)',
      'cd <parent> && cd repo-main && git push origin HEAD',
      'git push origin feat/x && git -C <main> push origin HEAD',
    ],
  },
  {
    title: 'on main, no decision', location: 'main', expected: null,
    templates: [
      'git -C <feature> push origin HEAD',
      'cd <feature> && git push -u origin HEAD',
      '(cd <feature> && git push origin HEAD)',
    ],
  },
  {
    title: 'on main, push-main after a closed subshell', location: 'main', expected: 'push-main',
    templates: ['(cd <feature>); git push origin HEAD'],
  },
  {
    title: 'on a feature branch, branch-lookup-failed', location: 'feature', expected: 'branch-lookup-failed',
    templates: [
      'git -C "$TARGET" push origin HEAD',
      'git -C $(pwd) push',
      'git -C <missing> push origin HEAD',
      'cd "$TARGET" && git push origin HEAD',
      'cd && git push',
      'cd - && git push origin HEAD',
      'popd && git push origin HEAD',
      'cd <main> | git push origin HEAD',
      'cd <main> || git push origin HEAD',
      'cd <main> & git push origin HEAD',
      'GIT_DIR=<main>/.git git push origin HEAD',
      'GIT_WORK_TREE=<main> git push',
      'GIT_COMMON_DIR=<main>/.git git push origin @',
    ],
  },
  {
    title: 'on a feature branch, no decision', location: 'feature', expected: null,
    templates: [
      'git push -u origin HEAD',
      'git push',
      'git push origin HEAD',
      'git -C <feature> push origin HEAD',
      'git -C "$TARGET" push origin feat/x',
      'cd "$TARGET" && git push origin feat/x',
      'GIT_DIR=<main>/.git git push origin feat/x',
    ],
  },
  {
    title: 'on main, push-main', location: 'main', expected: 'push-main',
    templates: ['git push', 'git push origin HEAD', 'git -C <main> push origin HEAD'],
  },
];

for (const { title, location, expected, templates } of groups) {
  for (const template of templates) {
    test(`${title}: ${shownInName(template)}`, t => judge(t, template, location, expected));
  }
}

test('on a feature branch, push-main (Monitor): git -C <main> push origin HEAD', t =>
  judge(t, 'git -C <main> push origin HEAD', 'feature', 'push-main', monitorInput));

test('on a feature branch, no decision: git -C "<spaced>" push origin HEAD', t =>
  judge(t, 'git -C "<spaced>" push origin HEAD', 'feature', null, bashInput, { spacedRepository: true }));

test('on a feature branch, no decision (win32): git -C <feature-drive> push origin HEAD', async t => {
  if (process.platform !== 'win32') {
    t.skip('the Git Bash drive form only exists on Windows');
    return;
  }
  await judge(t, 'git -C <feature-drive> push origin HEAD', 'feature', null);
});

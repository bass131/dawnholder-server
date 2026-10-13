// Requirement tests for the entry, its registration and the output contract of the Codex guard.
// Expected outcomes come from the approved texts, not from 99_Tools/CodexGuard:
// - contract E/guard-pretest-contract-v1.md (SHA256 7fea9bfb…14d2) requirements 1, 4, 5 and 6;
// - goal 2026-10-13-astra-lead-mailbox-hotfix 「만들 것」 6 (project .codex/hooks.json PreToolUse
//   registration, Claude hook/settings/approval storage untouched);
// - scope draft v1 (E/scope-draft-v1.md) 57행: the guard runs no command and creates no approval
//   state, and ordinary calls are left to Codex's own permission handling; 61행: words in PR
//   titles and document writes are not blocked;
// - stored Codex hooks documentation (E/research/hooks.md) for the registration shape: matcher
//   groups under hooks.PreToolUse, command handlers, and background (`async`) hooks that cannot
//   block;
// - the lead's design supplement 1 (E/guard-pretest-design-supplement-01.md, reply to
//   msg_cff106ac428f): the legacy {"decision": "block", "reason"} shape counts as an explicit
//   block too, and normal events and tools are not input errors.
// Tests named "fixture self-check" check hook-fixture.mjs only: its detection mechanisms and its
// outcome checks.
import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { existsSync, readFileSync } from 'node:fs';
import { mkdir, writeFile } from 'node:fs/promises';
import { join } from 'node:path';
import { test } from 'node:test';

import {
  assertBlocked, assertKept, assertRemotesUntouched, bareRefs, bashInput, blockProblems, createFixture, createRepository,
  ghCallLines, git, gitPath, HEAD_SHA, hookEnvironment, hookPath, hooksConfigPath, passProblems, PR, runHook, runReport,
  SESSION, snapshotFiles,
} from './hook-fixture.mjs';

const entryRelative = '99_Tools/CodexGuard/codex-hook.mjs';

test('requirement 5: the public entry 99_Tools/CodexGuard/codex-hook.mjs exists', () => {
  assert.ok(existsSync(hookPath), `[entry missing: ${entryRelative}] the hook entry file does not exist`);
});

test('goal 6: .codex/hooks.json runs the entry for Bash in PreToolUse and can block', () => {
  assert.ok(existsSync(hooksConfigPath), '[registration missing: .codex/hooks.json] the project hook file does not exist');
  let config;
  try {
    config = JSON.parse(readFileSync(hooksConfigPath, 'utf8'));
  } catch (error) {
    assert.fail(`.codex/hooks.json is not JSON: ${error.message}`);
  }

  const groups = config?.hooks?.PreToolUse;
  assert.ok(Array.isArray(groups), '.codex/hooks.json has hooks.PreToolUse as an array of matcher groups');
  // hooks.md "Matcher patterns": the matcher is a regex on the tool name; "*", "" or no matcher
  // match every call.
  const matchesBash = group => {
    const matcher = group?.matcher;
    if (matcher === undefined || matcher === '' || matcher === '*') return true;
    return new RegExp(matcher).test('Bash');
  };
  const namesEntry = command => typeof command === 'string' && command.split('\\').join('/').includes(entryRelative);
  const handlers = groups.filter(matchesBash).flatMap(group => (Array.isArray(group.hooks) ? group.hooks : []));
  const guardHandlers = handlers.filter(handler => handler?.type === 'command' && namesEntry(handler.command));
  assert.ok(guardHandlers.length > 0,
    `a PreToolUse group matching "Bash" has a command handler that runs ${entryRelative}`);

  for (const handler of guardHandlers) {
    if (handler.commandWindows !== undefined) {
      assert.ok(namesEntry(handler.commandWindows), `commandWindows also runs ${entryRelative}`);
    }
    // hooks.md "Run hooks in the background": background hooks can't block.
    assert.notEqual(handler.async, true, 'the guard handler is synchronous so its deny takes effect');
    if (handler.timeout !== undefined) {
      assert.ok(Number.isFinite(handler.timeout) && handler.timeout > 0, 'timeout is a positive number of seconds');
    }
  }
});

// The outcome checks must accept every equivalent explicit block (contract requirement 6 and
// design supplement 1) and must not count shapes that Codex treats as a failed hook run.
test('fixture self-check: outcome checks accept each explicit block form and reject fail-open shapes', () => {
  const run = (exit, stdout = '', stderr = '') => ({ exit, stdout, stderr });
  const json = value => JSON.stringify(value);
  const deny = { hookEventName: 'PreToolUse', permissionDecision: 'deny', permissionDecisionReason: 'blocked' };
  const allow = { hookEventName: 'PreToolUse', permissionDecision: 'allow' };

  const blocks = [
    ['hookSpecificOutput deny', run(0, json({ hookSpecificOutput: deny }))],
    ['hookSpecificOutput deny with systemMessage', run(0, json({ hookSpecificOutput: deny, systemMessage: 'note' }))],
    ['legacy decision block', run(0, json({ decision: 'block', reason: 'blocked' }))],
    ['exit 2 with stderr', run(2, '', 'blocked')],
  ];
  for (const [label, outcome] of blocks) assert.deepEqual(blockProblems(outcome), [], `${label} is a block`);

  const notBlocks = [
    ['exit 0 without output', run(0)],
    ['deny with continue false', run(0, json({ hookSpecificOutput: deny, continue: false }))],
    ['deny with updatedInput', run(0, json({ hookSpecificOutput: { ...deny, updatedInput: { command: 'true' } } }))],
    ['deny with an empty reason', run(0, json({ hookSpecificOutput: { ...deny, permissionDecisionReason: ' ' } }))],
    ['legacy block next to an allow', run(0, json({ decision: 'block', reason: 'x', hookSpecificOutput: allow }))],
    ['legacy approve', run(0, json({ decision: 'approve', reason: 'x' }))],
    ['ask', run(0, json({ hookSpecificOutput: { ...deny, permissionDecision: 'ask' } }))],
    ['log line before the JSON', run(0, `checking\n${json({ hookSpecificOutput: deny })}`)],
    ['exit 1 with stderr', run(1, '', 'blocked')],
    ['exit 2 without stderr', run(2)],
  ];
  for (const [label, outcome] of notBlocks) assert.notDeepEqual(blockProblems(outcome), [], `${label} is not a block`);

  const kept = [
    ['exit 0 without output', run(0)],
    ['systemMessage only', run(0, json({ systemMessage: 'note' }))],
    ['additionalContext only', run(0, json({ hookSpecificOutput: { hookEventName: 'PreToolUse', additionalContext: 'note' } }))],
  ];
  for (const [label, outcome] of kept) assert.deepEqual(passProblems(outcome), [], `${label} keeps the call`);

  const notKept = [
    ['explicit allow', run(0, json({ hookSpecificOutput: allow }))],
    ['rewrite', run(0, json({ hookSpecificOutput: { ...allow, updatedInput: { command: 'true' } } }))],
    ['legacy block', run(0, json({ decision: 'block', reason: 'x' }))],
    ['plain text on stdout', run(0, 'ok')],
    ['exit 1', run(1)],
    ['exit 2', run(2, '', 'blocked')],
  ];
  for (const [label, outcome] of notKept) assert.notDeepEqual(passProblems(outcome), [], `${label} does not keep the call`);
});

test('fixture self-check: gh reachable from the hook environment is the recording fake or nothing', async t => {
  const fixture = await createFixture(t);
  const environment = hookEnvironment(fixture);

  const direct = spawnSync('gh', ['--version'], { cwd: fixture.root, env: environment, encoding: 'utf8', windowsHide: true });
  if (process.platform === 'win32') {
    assert.equal(direct.error?.code, 'ENOENT', 'without a shell Windows finds no gh: the fake is a .cmd and the real one is off PATH');
    assert.deepEqual(ghCallLines(fixture), []);
  } else {
    assert.equal(direct.status, 0, `the fake gh runs: ${direct.stderr}`);
    assert.deepEqual(ghCallLines(fixture), ['gh --version']);
  }

  const throughShell = spawnSync('gh --version', { cwd: fixture.root, env: environment, encoding: 'utf8', shell: true, windowsHide: true });
  assert.equal(throughShell.status, 0, `the fake gh runs through the shell: ${throughShell.stderr}`);
  assert.ok(ghCallLines(fixture).length >= 1, 'a call through the shell is recorded');
});

test('fixture self-check: a ref that reaches a bare remote is visible, and repositories ignore user git config', async t => {
  const fixture = await createFixture(t);
  const repository = await createRepository(fixture, { current: 'main' });
  const bare = repository.bares.origin;
  assert.deepEqual(bareRefs(fixture, bare), [], 'a new bare remote starts empty');

  // Local fetch into the bare repository; nothing leaves the fixture folder.
  git(fixture, fixture.root, ['--git-dir', bare, 'fetch', '-q', repository.directory, 'main:refs/heads/main']);
  assert.deepEqual(bareRefs(fixture, bare), ['refs/heads/main'], 'the check sees a ref written into the bare remote');

  const listed = spawnSync(gitPath(), ['config', '--list', '--show-scope'], {
    cwd: repository.directory, env: hookEnvironment(fixture), encoding: 'utf8', windowsHide: true,
  });
  assert.equal(listed.status, 0, listed.stderr);
  const scopes = new Set(listed.stdout.split(/\r?\n/).filter(Boolean).map(line => line.split('\t')[0]));
  assert.deepEqual([...scopes].filter(scope => scope !== 'local'), [], 'only repository-local git settings are visible to the hook');
});

test('requirement 1: no session role and no Claude merge-gate record opens an allow path; the hook writes nothing', async t => {
  const fixture = await createFixture(t);
  const repository = await createRepository(fixture, { current: 'main' });
  // Files in the shape the Claude merge gate reads (99_Tools/MergeGate.Tests/hook-fixture.mjs):
  // the main-checkout marker and an unused approval for PR 12 at HEAD_SHA, as if the main
  // session had approved this exact merge. The Codex guard has no allow path through them.
  const state = join(repository.directory, '.claude', 'state', 'merge-gate');
  await mkdir(join(state, 'approvals'), { recursive: true });
  await writeFile(join(state, 'main-checkout'), '');
  const approval = { pr: PR, head: HEAD_SHA, createdAt: new Date().toISOString(), usedAt: null, usedCommand: null };
  await writeFile(join(state, 'approvals', `${SESSION}.json`), `${JSON.stringify({ version: 1, approvals: [approval] })}\n`);
  const before = await snapshotFiles(repository.directory);

  const commands = [`gh pr merge ${PR} --merge --match-head-commit ${HEAD_SHA}`, 'git push origin HEAD:main', 'git push'];
  const roles = [
    ['Claude-shaped main session, bypassPermissions', { model: 'claude-opus-5-5', permission_mode: 'bypassPermissions' }],
    ['lead Astra, default', { model: 'gpt-6-astra', permission_mode: 'default' }],
    ['implementer Sol, acceptEdits', { model: 'gpt-6.1-sol', permission_mode: 'acceptEdits' }],
    ['dontAsk', { permission_mode: 'dontAsk' }],
    ['plan', { permission_mode: 'plan' }],
  ];
  for (const [role, overrides] of roles) {
    await t.test(role, async () => {
      for (const command of commands) {
        const run = runHook(fixture, bashInput(repository.directory, command, overrides));
        assertBlocked(fixture, run, `${command} (${role})`);
        assert.deepEqual(await snapshotFiles(repository.directory), before,
          runReport(run, `${command} (${role}): the hook leaves the Claude records and every other file unchanged`));
      }
    });
  }
  assertRemotesUntouched(fixture, repository, 'after every role');
});

test('requirements 4 and 6: other tools carrying the same words are kept', async t => {
  const fixture = await createFixture(t);
  const repository = await createRepository(fixture);
  const cwd = repository.directory;
  const patch = [
    '*** Begin Patch',
    '*** Add File: notes/guard.md',
    '+Blocked: gh pr merge 12 --auto and git push origin main.',
    '+Kept: gh pr view 12 and git push origin feat/guard.',
    '*** End Patch',
    '',
  ].join('\n');
  const inputs = [
    ['apply_patch writing a note about the commands', { tool_name: 'apply_patch', tool_input: { command: patch } }],
    ['MCP tool writing the commands as content', {
      tool_name: 'mcp__filesystem__write_file',
      tool_input: { path: 'notes/guard.md', content: 'gh pr merge 12\ngit push origin main\n' },
    }],
    ['update_plan naming the commands', {
      tool_name: 'update_plan',
      tool_input: { plan: [{ step: 'ask main to run gh pr merge 12', status: 'pending' }] },
    }],
  ];
  for (const [label, overrides] of inputs) {
    await t.test(label, () => {
      assertKept(fixture, runHook(fixture, bashInput(cwd, '', overrides)), label);
    });
  }
});

// Requirement 6: events other than PreToolUse are outside the guard. The hook makes no decision
// there and claims no protection: exit 0 and either empty stdout or JSON holding only a
// systemMessage.
test('requirement 6: events other than PreToolUse get no decision', async t => {
  const fixture = await createFixture(t);
  const repository = await createRepository(fixture);
  const cwd = repository.directory;
  const command = `gh pr merge ${PR} --squash`;
  const base = bashInput(cwd, command);
  // Each event carries the fields hooks.md lists for it: PermissionRequest has no tool_use_id and
  // UserPromptSubmit has a prompt instead of a tool call.
  const withoutFields = (input, ...names) => Object.fromEntries(
    Object.entries(input).filter(([key]) => !names.includes(key)),
  );
  const inputs = [
    ['PostToolUse', {
      ...base,
      hook_event_name: 'PostToolUse',
      tool_response: { exit_code: 1, output: 'blocked elsewhere' },
    }],
    ['PermissionRequest', {
      ...withoutFields(base, 'tool_use_id'),
      hook_event_name: 'PermissionRequest',
      tool_input: { command, description: null },
    }],
    ['UserPromptSubmit', {
      ...withoutFields(base, 'tool_name', 'tool_input', 'tool_use_id'),
      hook_event_name: 'UserPromptSubmit',
      prompt: command,
    }],
  ];
  for (const [label, input] of inputs) {
    await t.test(label, () => {
      const run = runHook(fixture, input);
      assert.equal(run.exit, 0, runReport(run, `${label}: exit 0`));
      assert.deepEqual(ghCallLines(fixture), [], runReport(run, `${label}: the hook ran no gh`));
      if (run.stdout.trim() === '') return;
      let output;
      try {
        output = JSON.parse(run.stdout);
      } catch {
        assert.fail(runReport(run, `${label}: stdout is empty or one JSON object`));
      }
      assert.deepEqual(Object.keys(output).filter(key => key !== 'systemMessage'), [],
        runReport(run, `${label}: no decision, rewrite or stop field`));
    });
  }
});

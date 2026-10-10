// Independent verifier's regression for the guard registration in the repository settings.
// Expected values come from goal 「만들 것」 4 (one PreToolUse group in `.claude/settings.json`) and
// 「설계 / 세션 쓰기 가드」 lines 88 (the entry command) and 91 (Bash, Write, Edit, MultiEdit and
// NotebookEdit are the target tools) of 01_Phases/goals/2026-10-10-operating-tool-guards/goal.md at
// 484d74c7. The merge gate registrations keep their own regression in MergeGate.Tests.
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { join } from 'node:path';
import { test } from 'node:test';

import { repositoryRoot } from './guard-fixture.mjs';

const entryCommand = 'node "$CLAUDE_PROJECT_DIR/99_Tools/SessionGuard/claude-hook.mjs"';
const targetTools = ['Bash', 'Edit', 'MultiEdit', 'NotebookEdit', 'Write'];

test('settings register the guard once, as one PreToolUse group for the target tools (goal 만들 것 4, lines 88, 91)', async () => {
  const settings = JSON.parse(await readFile(join(repositoryRoot, '.claude', 'settings.json'), 'utf8'));
  const registrations = [];
  for (const [event, groups] of Object.entries(settings.hooks ?? {})) {
    for (const group of groups) {
      for (const hook of group.hooks ?? []) {
        if (String(hook.command ?? '').includes('SessionGuard')) registrations.push({ event, group, hook });
      }
    }
  }

  assert.equal(registrations.length, 1, 'the guard is registered exactly once');
  const [{ event, group, hook }] = registrations;
  assert.equal(event, 'PreToolUse', 'the guard runs before a tool call');
  assert.equal(group.hooks.length, 1, 'the guard group holds only the guard');
  assert.deepEqual(group.matcher.split('|').sort(), targetTools, 'the matcher names exactly the target tools');
  assert.equal(hook.type, 'command');
  assert.equal(hook.command, entryCommand, 'the entry command of goal line 88');
});

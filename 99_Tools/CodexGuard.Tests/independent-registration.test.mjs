// Independent review tests that run the registered hook commands of .codex/hooks.json the way the
// pinned Codex runner starts them. Expected outcomes come from the approved texts, not from
// 99_Tools/CodexGuard:
// - review contract E/guard-review-contract-v1.md requirement 6: the real POSIX/Windows command
//   keeps the project root from a sub folder and a root with spaces, passes stdin, exit and output
//   through, and a root lookup failure or a missing entry is not a silent success;
// - the lead's design supplement (E/guard-impl-design-supplement-01.md, items 2 and 3).
// Runner shape, from the pinned source E/guard-impl/command_runner.rs (Codex 0.162.1,
// 092d3acd6bec3e3a14bdc7e7a2810ab628ab759d, build_command): with no configured shell, Windows runs
// `%COMSPEC% /C "<commandWindows>"` (the line wrapped in one pair of quotes, no /D or /S) and other
// systems run `$SHELL -lc <command>` (fallback /bin/sh), in the session cwd, stdin piped. This file
// reproduces those launches. On Windows the POSIX command is run by Git Bash `bash -c` as the
// closest local stand-in; Linux CI runs it through `$SHELL -lc`.
// Each launch uses a copy of the guard under a fixture project whose path contains spaces, with
// Git limited to the fixture folder (GIT_CEILING_DIRECTORIES), so the real repository is never the
// root that is found.
import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { createHash } from 'node:crypto';
import { existsSync, readFileSync } from 'node:fs';
import { copyFile, mkdir, readdir, writeFile } from 'node:fs/promises';
import { dirname, join } from 'node:path';
import { test } from 'node:test';

import {
  bashInput, blockProblems, createFixture, git, gitPath, hookEnvironment, hooksConfigPath, passProblems, PR,
  repositoryRoot, runReport,
} from './hook-fixture.mjs';

const isWindows = process.platform === 'win32';
const entryRelative = ['99_Tools', 'CodexGuard', 'codex-hook.mjs'];

function registeredHandler() {
  const config = JSON.parse(readFileSync(hooksConfigPath, 'utf8'));
  const handlers = (config.hooks?.PreToolUse ?? []).flatMap(group => group.hooks ?? []);
  const handler = handlers.find(item => item?.type === 'command' && String(item.command).includes('codex-hook.mjs'));
  assert.ok(handler, 'a PreToolUse command handler runs codex-hook.mjs');
  return handler;
}

function gitBash() {
  const gitRoots = [dirname(dirname(gitPath())), dirname(dirname(dirname(gitPath())))];
  const found = gitRoots.map(root => join(root, 'bin', 'bash.exe')).find(path => existsSync(path));
  if (!found) throw new Error('[fixture] Git Bash bin/bash.exe not found next to git');
  return found;
}

// Launches one registered command as the runner would, with `text` on stdin.
function launch(kind, handler, cwd, text, environment) {
  const options = { cwd, env: environment, input: text, encoding: 'utf8', timeout: handler.timeout * 1000, windowsHide: true };
  let result;
  if (kind === 'windows') {
    const comspec = environment.ComSpec ?? environment.COMSPEC ?? 'cmd.exe';
    result = spawnSync(comspec, ['/C', `"${handler.commandWindows}"`], { ...options, windowsVerbatimArguments: true });
  } else if (isWindows) {
    result = spawnSync(gitBash(), ['-c', handler.command], options);
  } else {
    result = spawnSync(process.env.SHELL || '/bin/sh', ['-lc', handler.command], options);
  }
  return { exit: result.status, signal: result.signal, stdout: result.stdout ?? '', stderr: result.stderr ?? '', error: result.error };
}

// A git project with spaces in its path holding a copy of the guard, a sub folder, a project with
// no guard and a folder outside any repository.
async function projects(t) {
  const fixture = await createFixture(t);
  const project = join(fixture.root, 'project with space');
  git(fixture, fixture.root, ['init', '-q', '-b', 'main', project]);
  const guardSource = join(repositoryRoot, '99_Tools', 'CodexGuard');
  const guardCopy = join(project, '99_Tools', 'CodexGuard');
  await mkdir(guardCopy, { recursive: true });
  for (const name of await readdir(guardSource)) {
    if (name.endsWith('.mjs')) await copyFile(join(guardSource, name), join(guardCopy, name));
  }
  const inner = join(project, 'sub dir', 'inner');
  await mkdir(inner, { recursive: true });

  const bare = join(fixture.root, 'no guard project');
  git(fixture, fixture.root, ['init', '-q', '-b', 'main', bare]);

  const outside = join(fixture.root, 'outside');
  await mkdir(outside);

  // A stand-in entry that reports what it received: the SHA-256 of stdin on stderr, a fixed JSON
  // line on stdout and the exit code named in the input.
  const echoProject = join(fixture.root, 'echo project');
  git(fixture, fixture.root, ['init', '-q', '-b', 'main', echoProject]);
  await mkdir(join(echoProject, ...entryRelative.slice(0, -1)), { recursive: true });
  await writeFile(join(echoProject, ...entryRelative), [
    "import { createHash } from 'node:crypto';",
    'const chunks = [];',
    'for await (const chunk of process.stdin) chunks.push(chunk);',
    'const bytes = Buffer.concat(chunks);',
    "process.stderr.write(`sha256:${createHash('sha256').update(bytes).digest('hex')}`);",
    "process.stdout.write('{\"systemMessage\":\"echo entry\"}\\n');",
    'process.exit(JSON.parse(bytes.toString()).exitWanted);',
    '',
  ].join('\n'));

  return { fixture, project, inner, bare, outside, echoProject, environment: hookEnvironment(fixture) };
}

const kinds = ['posix', 'windows'];

for (const kind of kinds) {
  const skip = kind === 'windows' && !isWindows ? 'cmd.exe exists only on Windows; the POSIX command runs on every system' : false;

  test(`requirement 6 (${kind}): the registered command finds the entry from the root and a sub folder with spaces`, { skip }, async t => {
    const handler = registeredHandler();
    const { project, inner, environment } = await projects(t);
    for (const [label, cwd] of [['project root', project], ['sub folder', inner]]) {
      await t.test(`blocked call from the ${label}`, () => {
        const run = launch(kind, handler, cwd, JSON.stringify(bashInput(cwd, `gh pr merge ${PR}`)), environment);
        const problems = blockProblems(run);
        if (problems.length) assert.fail(runReport(run, `${kind} ${label}: expected a block`, problems));
      });
      await t.test(`kept call from the ${label}`, () => {
        const run = launch(kind, handler, cwd, JSON.stringify(bashInput(cwd, `gh pr view ${PR}`)), environment);
        const problems = passProblems(run);
        if (problems.length) assert.fail(runReport(run, `${kind} ${label}: expected the call to be kept`, problems));
      });
    }
    await t.test('a large kept input reaches the entry whole', () => {
      const command = `echo '${'x'.repeat(200 * 1024)}'`;
      const run = launch(kind, handler, project, JSON.stringify(bashInput(project, command)), environment);
      const problems = passProblems(run);
      if (problems.length) assert.fail(runReport(run, `${kind}: a 200 KiB kept input`, problems));
    });
  });

  test(`requirement 6 (${kind}): a failed root lookup or a missing entry is a block, not a silent success`, { skip }, async t => {
    const handler = registeredHandler();
    const { bare, outside, environment } = await projects(t);
    for (const [label, cwd] of [['folder outside any repository', outside], ['repository without the guard', bare]]) {
      await t.test(label, () => {
        const run = launch(kind, handler, cwd, JSON.stringify(bashInput(cwd, `gh pr view ${PR}`)), environment);
        const problems = blockProblems(run);
        if (problems.length) assert.fail(runReport(run, `${kind} ${label}: expected a block`, problems));
      });
    }
  });

  test(`requirement 6 (${kind}): stdin bytes, stdout and the entry exit code pass through unchanged`, { skip }, async t => {
    const handler = registeredHandler();
    const { echoProject, environment } = await projects(t);
    for (const exitWanted of [0, 2, 7]) {
      await t.test(`entry exit ${exitWanted}`, () => {
        const text = JSON.stringify({ ...bashInput(echoProject, 'git status'), exitWanted, note: 'quote " and & | < > % ^ !' });
        const run = launch(kind, handler, echoProject, text, environment);
        const sha = createHash('sha256').update(Buffer.from(text)).digest('hex');
        assert.equal(run.exit, exitWanted, runReport(run, `${kind}: the wrapper returns the entry exit code`));
        assert.equal(run.stderr.trim(), `sha256:${sha}`, runReport(run, `${kind}: the entry received the exact stdin bytes`));
        assert.equal(run.stdout, '{"systemMessage":"echo entry"}\n', runReport(run, `${kind}: stdout is the entry output only`));
      });
    }
  });
}

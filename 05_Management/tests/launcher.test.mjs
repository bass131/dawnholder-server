import assert from 'node:assert/strict';
import { test, after } from 'node:test';
import { spawnSync } from 'node:child_process';
import { mkdirSync, mkdtempSync, readFileSync, writeFileSync, existsSync, rmSync } from 'node:fs';
import { dirname, join, resolve, sep } from 'node:path';
import { fileURLToPath } from 'node:url';

const management = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const evidence = resolve(management, '.verification');
const fixtures = mkdtempSync(join(evidence, 'launcher fixtures '));
const source = readFileSync(join(management, 'Start-Management.bat'), 'utf8');
const startLine = 'start "" "%CD%\\node_modules\\electron\\dist\\electron.exe" .';
assert.equal(source.split(startLine).length, 2, 'Exactly one quoted detached launch site must be intercepted');
// Preserve real cmd flow and pause; replace only START so no executable/app is launched.
const instrumented = source.replace(startLine, 'call "%TEST_START_STUB%" "%CD%\\node_modules\\electron\\dist\\electron.exe" .')
  .replace(/^pause\r?$/gm, 'echo TEST_PAUSE_REACHED\r\npause');
let fixtureNumber = 0;

after(() => {
  // Only this run's verified, direct child fixture directory is removed.
  assert.equal(dirname(resolve(fixtures)), evidence);
  assert.ok(resolve(fixtures).startsWith(evidence + sep));
  rmSync(fixtures, { recursive: true });
});

function runCase({ missing, buildExit = 0, launchExit = 0 } = {}) {
  const root = join(fixtures, `case ${++fixtureNumber}`);
  const app = join(root, 'management path with spaces');
  const frontend = join(app, 'frontend');
  const bin = join(root, 'stub tools');
  const caller = join(root, 'unrelated caller');
  for (const directory of [app, bin, caller]) mkdirSync(directory, { recursive: true });
  const files = [
    'node_modules/.bin/tsc.cmd', 'node_modules/.bin/vite.cmd',
    'node_modules/react/package.json', 'node_modules/react-dom/package.json',
    'node_modules/electron/dist/electron.exe',
  ];
  if (missing !== 'frontend') {
    mkdirSync(frontend, { recursive: true });
    for (const path of files) {
      if (path === missing) continue;
      const target = join(frontend, path);
      mkdirSync(dirname(target), { recursive: true });
      writeFileSync(target, ''); // Presence stubs only; none of these files are executed.
    }
  }
  if (missing !== 'node') writeFileSync(join(bin, 'node.exe'), '');
  if (missing !== 'npm') writeFileSync(join(bin, 'npm.cmd'), [
    '@echo off', '> "%TEST_NPM_LOG%" echo args=%*', '>> "%TEST_NPM_LOG%" echo cwd=%CD%', 'exit /b %TEST_BUILD_EXIT%', '',
  ].join('\r\n'));
  const startStub = join(bin, 'record-start.cmd');
  writeFileSync(startStub, [
    '@echo off', '> "%TEST_START_LOG%" echo exe=%~1', '>> "%TEST_START_LOG%" echo arg=%~2',
    '>> "%TEST_START_LOG%" echo cwd=%CD%', 'exit /b %TEST_LAUNCH_EXIT%', '',
  ].join('\r\n'));
  const launcher = join(app, 'Start-Management.bat');
  writeFileSync(launcher, instrumented, 'ascii');
  const npmLog = join(root, 'npm.log');
  const startLog = join(root, 'start.log');
  const cwdLog = join(root, 'returned-cwd.log');
  const wrapper = join(caller, 'invoke.cmd');
  writeFileSync(wrapper, [
    '@echo off', `call "${launcher}"`, 'set "testResult=%errorlevel%"',
    `cd > "${cwdLog}"`, 'exit /b %testResult%', '',
  ].join('\r\n'), 'ascii');
  const system32 = join(process.env.SystemRoot ?? 'C:\\Windows', 'System32');
  const result = spawnSync(join(system32, 'cmd.exe'), ['/d', '/c', wrapper], {
    cwd: caller, encoding: 'utf8', input: '\r\n', timeout: 5000, windowsHide: true,
    env: { ...process.env, PATH: `${bin};${system32}`, PATHEXT: '.COM;.EXE;.BAT;.CMD',
      TEST_START_STUB: startStub, TEST_NPM_LOG: npmLog, TEST_START_LOG: startLog,
      TEST_BUILD_EXIT: String(buildExit), TEST_LAUNCH_EXIT: String(launchExit) },
  });
  assert.ifError(result.error);
  assert.equal(result.signal, null);
  assert.equal(readFileSync(cwdLog, 'utf8').trim().toLowerCase(), caller.toLowerCase(), 'pushd is balanced for every return path');
  return { ...result, output: result.stdout + result.stderr, frontend,
    npm: existsSync(npmLog) ? readFileSync(npmLog, 'utf8') : null,
    launch: existsSync(startLog) ? readFileSync(startLog, 'utf8') : null };
}

function expectPreflightFailure(result, message) {
  assert.notEqual(result.status, 0);
  assert.match(result.output, message);
  assert.match(result.output, /TEST_PAUSE_REACHED/);
  assert.equal(result.npm, null, 'No build/install command on missing prerequisites');
  assert.equal(result.launch, null, 'No app launch on missing prerequisites');
}

test('launcher remains ASCII/CRLF with directory-relative quoted startup', () => {
  assert.ok([...readFileSync(join(management, 'Start-Management.bat'))].every((byte) => byte < 128));
  assert.ok(!/(?<!\r)\n/.test(source));
  assert.ok(source.includes('pushd "%~dp0frontend"'));
  assert.ok(source.includes(startLine));
});

test('space path and unrelated cwd build the correct frontend and then request detached launch', () => {
  const result = runCase();
  assert.equal(result.status, 0);
  assert.equal(result.npm, `args=run desktop:build\r\ncwd=${result.frontend}\r\n`);
  assert.equal(result.launch, `exe=${join(result.frontend, 'node_modules/electron/dist/electron.exe')}\r\narg=.\r\ncwd=${result.frontend}\r\n`);
  assert.doesNotMatch(result.output, /TEST_PAUSE_REACHED/);
});

for (const [missing, message] of [['node', /Node\.js is not available/], ['npm', /npm\.cmd is not available/], ['frontend', /frontend folder is unavailable/]]) {
  test(`missing ${missing} reports error, pauses, and starts no process`, () => expectPreflightFailure(runCase({ missing }), message));
}

for (const missing of ['node_modules/.bin/tsc.cmd', 'node_modules/.bin/vite.cmd', 'node_modules/react/package.json', 'node_modules/react-dom/package.json']) {
  test(`missing ${missing} never invokes npm or Electron`, () => expectPreflightFailure(runCase({ missing }), /Local frontend dependencies are missing/));
}

test('missing Electron binary never initiates a lazy download', () => {
  expectPreflightFailure(runCase({ missing: 'node_modules/electron/dist/electron.exe' }), /local Electron binary is missing/);
});

test('build failure preserves its exit code and cannot launch a stale build', () => {
  const result = runCase({ buildExit: 7 });
  assert.equal(result.status, 7);
  assert.match(result.npm, /^args=run desktop:build/);
  assert.equal(result.launch, null);
  assert.match(result.output, /Build failed\. No previous build will be launched/);
  assert.match(result.output, /TEST_PAUSE_REACHED/);
});

test('launch request failure pauses and preserves its nonzero exit code', () => {
  const result = runCase({ launchExit: 9 });
  assert.equal(result.status, 9);
  assert.notEqual(result.launch, null);
  assert.match(result.output, /Electron could not be started/);
  assert.match(result.output, /TEST_PAUSE_REACHED/);
});

// Harmless command for the execution lock regressions. The runner under test starts it as
// `node fixture-child.mjs <spec.json> [extra args...]`; it touches only the record folder named in
// the spec. It stands in for a Unity root, a Unity import child or a WSL client so the tests can
// observe start, argv, cwd, output, exit and lifetime without starting any real heavy process.
//
// Spec fields: label, name (the process name the injected observation reports), recordDir,
// release (path whose creation ends the hold, or null to exit at once), exitCode, stdout, stderr,
// grandchild (another spec path started as a detached-lifetime child, or null) and holdMaxMs
// (self-imposed upper bound so a lost test never leaves this process behind).
import { spawn } from 'node:child_process';
import { existsSync, readFileSync, renameSync, writeFileSync, writeSync } from 'node:fs';
import { join } from 'node:path';

const startedAt = new Date().toISOString();
const spec = JSON.parse(readFileSync(process.argv[2], 'utf8'));

// Records are written to a temporary name and renamed so a reader never sees half a record.
function writeRecord(suffix, value) {
  const path = join(spec.recordDir, `${spec.label}.${suffix}.json`);
  writeFileSync(`${path}.partial`, JSON.stringify(value));
  renameSync(`${path}.partial`, path);
}

// The first statement after reading the spec: the injected observation can only report this
// process once this record exists (see the fidelity note in lock-fixture.mjs).
writeRecord('started', {
  label: spec.label,
  name: spec.name,
  pid: process.pid,
  parentPid: process.ppid,
  startedAt,
  cwd: process.cwd(),
  argv: process.argv.slice(3),
});

// Synchronous writes, so an immediate exit below cannot drop the output the tests look for.
if (spec.stdout) writeSync(1, spec.stdout);
if (spec.stderr) writeSync(2, spec.stderr);

if (spec.grandchild) {
  // Detached, because on Windows Node ends its non-detached children when it exits (libuv job
  // object); a Unity root does not do that to its import workers.
  const grandchild = spawn(process.execPath, [process.argv[1], spec.grandchild], {
    detached: true,
    stdio: 'ignore',
    windowsHide: true,
  });
  grandchild.unref();
  // Hold until the grandchild has recorded itself, so "root exited while its child lives" is a
  // fact of the scenario and not a race of the fixture.
  const grandchildSpec = JSON.parse(readFileSync(spec.grandchild, 'utf8'));
  const grandchildRecord = join(grandchildSpec.recordDir, `${grandchildSpec.label}.started.json`);
  while (!existsSync(grandchildRecord)) await new Promise(resolve => setTimeout(resolve, 10));
}

function finish(exitCode, reason) {
  writeRecord('ended', { label: spec.label, pid: process.pid, exitCode, reason, endedAt: new Date().toISOString() });
  process.exit(exitCode);
}

const stopAll = join(spec.recordDir, 'stop-all');
const holdLimit = Date.now() + (spec.holdMaxMs ?? 90_000);
if (!spec.release) finish(spec.exitCode ?? 0, 'no-hold');
const poll = setInterval(() => {
  if (existsSync(spec.release)) {
    clearInterval(poll);
    finish(spec.exitCode ?? 0, 'released');
  } else if (existsSync(stopAll)) {
    clearInterval(poll);
    finish(98, 'stop-all');
  } else if (Date.now() > holdLimit) {
    clearInterval(poll);
    finish(99, 'hold-limit');
  }
}, 20);

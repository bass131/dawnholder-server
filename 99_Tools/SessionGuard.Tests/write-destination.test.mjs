// Independent regressions for rule 2, write destinations. Every command and path here is a
// synthetic boundary case. Expected decisions come from goal 「설계 / 세션 쓰기 가드」 규칙 2
// (01_Phases/goals/2026-10-10-operating-tool-guards/goal.md lines 101-106 at 852f88ca) and the
// lead's supplement 1 item 7 (E/tdd/work/ask1-answer.txt: a heredoc body is not a destination, the
// redirect of the command that opens it is), not from the hook:
// - Bash destinations are redirect targets (`>`, `>>`, `>|`, `&>`, `&>>`, `N>`, `N>>`) and tee file
//   arguments; `>&N` duplication and /dev/null, /dev/stdout, /dev/stderr, /dev/fd/*, NUL are not
//   files;
// - paths: quotes are stripped; `$NAME`/`${NAME}` resolve from `NAME=` (also `export`) earlier in
//   the same command, then from the hook environment; `~` is HOME; `/c/…` reads as `C:/…`;
//   relative paths start at the input cwd, or CLAUDE_PROJECT_DIR without one; `.`/`..` fold as
//   text; drive paths ignore case; an unresolved variable leaves the destination unresolved and
//   rule 2 does not judge it;
// - allowed roots are CLAUDE_PROJECT_DIR and the real target of `<checkout>/.backups` when that
//   is a junction or symlink; a Bash destination outside them is write-outside-checkout;
// - a Write/Edit/MultiEdit file_path or NotebookEdit notebook_path under a temp root (os.tmpdir(),
//   TEMP, TMP, TMPDIR, /tmp) is temp-write; other paths outside the checkout are not rule 2's;
// - a path inside an allowed root passes rule 2 even under a temp root.
// Destinations inside the project are checked after the memo so that rule 3 has nothing to say
// (line 112); the memo-first cases of rule 3 are in memo-order.test.mjs.
import { mkdir } from 'node:fs/promises';
import { join } from 'node:path';
import { test } from 'node:test';

import {
  EVIDENCE, UNSET_VARIABLE, assertDeny, assertNoDecision, bashInput, createWorkspace, driveForm, fileToolInput, isWindows,
  notTempFolder, runHook, toSlash, writeMemo,
} from './guard-fixture.mjs';

const outsideFile = (ws, name = 'y.txt') => `${toSlash(ws.outside)}/${name}`;

// `command` and `options.input` may be values or functions of the workspace, because most paths
// only exist once the workspace is made.
const forWorkspace = (value, ws) => (typeof value === 'function' ? value(ws) : value);

async function denyBash(t, command, code, label, options = {}) {
  const ws = await createWorkspace(t, options.workspace);
  if (options.memo) writeMemo(ws);
  assertDeny(runHook(ws, bashInput(ws, forWorkspace(command, ws), forWorkspace(options.input, ws))), code, label);
}

async function passBashAfterMemo(t, command, label, options = {}) {
  const ws = await createWorkspace(t, options.workspace);
  if (options.prepare) await options.prepare(ws);
  writeMemo(ws);
  assertNoDecision(runHook(ws, bashInput(ws, forWorkspace(command, ws), forWorkspace(options.input, ws))), label);
}

const redirectForms = ['>', '>>', '>|', '&>', '&>>', '1>', '2>', '2>>'];

for (const operator of redirectForms) {
  test(`rule 2 blocks a ${operator} redirect outside the project`, async t => {
    await denyBash(t, ws => `echo x ${operator} "${outsideFile(ws)}"`, 'write-outside-checkout', `${operator} outside`);
  });
}

test('rule 2 blocks a redirect written without a space before the path', async t => {
  await denyBash(t, ws => `echo x >"${outsideFile(ws)}"`, 'write-outside-checkout', '>path');
});

test('rule 2 blocks /tmp as a Bash destination', async t => {
  await denyBash(t, 'echo x > /tmp/session-guard-y.txt', 'write-outside-checkout', '/tmp');
});

test('rule 2 blocks tee file arguments outside the project, options skipped', async t => {
  await denyBash(t, ws => `echo x | tee "${outsideFile(ws)}"`, 'write-outside-checkout', 'tee outside');
  await denyBash(t, ws => `echo x | tee -a "${outsideFile(ws)}"`, 'write-outside-checkout', 'tee -a outside');
  await denyBash(t, ws => `echo x | tee ${EVIDENCE}/a.txt "${outsideFile(ws)}"`, 'write-outside-checkout',
    'tee with a second file outside', { memo: true });
});

test('rule 2 passes tee files inside the evidence folder after the memo', async t => {
  await passBashAfterMemo(t, `echo x | tee ${EVIDENCE}/a.txt ${EVIDENCE}/b.txt`, 'tee inside');
});

const notFiles = [
  'echo x >&2', 'echo x 2>&1', 'echo x 1>&2', 'echo x > /dev/stdout', 'echo x > /dev/stderr', 'echo x > /dev/fd/3',
  'echo x > /dev/null', 'echo x > NUL', 'echo x 2>/dev/null', 'echo x &> /dev/null',
];

for (const command of notFiles) {
  test(`rule 2 treats the destination of \`${command}\` as no file (passes before the memo)`, async t => {
    const ws = await createWorkspace(t);
    assertNoDecision(runHook(ws, bashInput(ws, command)), command);
  });
}

test('rule 2 strips quotes from destinations and ignores redirect shapes inside quoted text', async t => {
  await denyBash(t, ws => `echo x > '${outsideFile(ws)}'`, 'write-outside-checkout', 'single-quoted destination');
  await denyBash(t, ws => `echo x > "${toSlash(ws.outside)}/with space/y.txt"`, 'write-outside-checkout', 'quoted path with a space');
  const ws = await createWorkspace(t);
  assertNoDecision(runHook(ws, bashInput(ws, `echo "x > ${outsideFile(ws)}"`)), 'redirect shape in double quotes');
  assertNoDecision(runHook(ws, bashInput(ws, `echo 'x > ${outsideFile(ws)}'`)), 'redirect shape in single quotes');
});

test('rule 2 resolves variables from NAME= and export earlier in the same command', async t => {
  await denyBash(t, ws => `D="${toSlash(ws.outside)}"; echo x > "$D/y.txt"`, 'write-outside-checkout', 'D= then $D');
  await denyBash(t, ws => `export D="${toSlash(ws.outside)}"; echo x > "\${D}/y.txt"`, 'write-outside-checkout', 'export D= then ${D}');
  await passBashAfterMemo(t, `D=${EVIDENCE}; echo x > "$D/y.txt"`, 'D= inside the evidence folder');
});

test('rule 2 prefers NAME= of the command over the hook environment', async t => {
  await passBashAfterMemo(t, `TEMP=${EVIDENCE}; echo x > "$TEMP/y.txt"`, 'TEMP= of the command over the environment TEMP');
});

test('rule 2 resolves TEMP, TMP and TMPDIR from the hook environment', async t => {
  await denyBash(t, 'echo x > "$TEMP/y.txt"', 'write-outside-checkout', '$TEMP');
  await denyBash(t, 'echo x > "${TMP}/y.txt"', 'write-outside-checkout', '${TMP}');
  await denyBash(t, 'echo x > "$TMPDIR/y.txt"', 'write-outside-checkout', '$TMPDIR');
});

test('rule 2 reads ~ as HOME of the hook environment', async t => {
  await denyBash(t, 'echo x > ~/y.txt', 'write-outside-checkout', '~ outside the project');
  await passBashAfterMemo(t, 'echo x > ~/y.txt', '~ with HOME inside the evidence folder', { workspace: { homeInsideProject: true } });
});

test('rule 2 does not judge a destination with an unresolved variable (limit, line 103)', async t => {
  await passBashAfterMemo(t, `echo x > "$${UNSET_VARIABLE}/y.txt"`, 'unresolved destination after the memo');
});

test('rule 2 reads redirects next to and inside command substitution', async t => {
  await denyBash(t, ws => `echo "$(date -u)" > "${toSlash(ws.outside)}/clock.txt"`, 'write-outside-checkout',
    'redirect after an argument with $(…)');
  await denyBash(t, ws => `X=$(git rev-parse HEAD > "${toSlash(ws.outside)}/head.txt")`, 'write-outside-checkout',
    'redirect inside $(…)');
  await passBashAfterMemo(t, `echo "$(git rev-parse HEAD)" > ${EVIDENCE}/head.txt`, '$(…) argument with an evidence destination');
});

test('rule 2 follows line continuations and later lines', async t => {
  await denyBash(t, ws => `echo x \\\n  > "${outsideFile(ws)}"`, 'write-outside-checkout', 'line continuation before the redirect');
  await denyBash(t, ws => `git status --short\necho x > "${outsideFile(ws)}"`, 'write-outside-checkout', 'second line');
});

test('rule 2 reads the heredoc opener redirect and not the heredoc body (supplement 1 item 7)', async t => {
  await denyBash(t, ws => `cat > "${outsideFile(ws, 'x.md')}" <<'EOF'\nbody\nEOF`, 'write-outside-checkout', 'heredoc opener outside');
  await passBashAfterMemo(t, ws => `cat > ${EVIDENCE}/notes.md <<'EOF'\n> quoted markdown line\na > ${outsideFile(ws)}\nEOF`,
    'quoted heredoc body with redirect shapes');
  await passBashAfterMemo(t, ws => `cat > ${EVIDENCE}/notes.md <<EOF\nb >> ${outsideFile(ws)}\nEOF`, 'unquoted heredoc body');
});

test('rule 2 takes relative paths from the input cwd', async t => {
  await passBashAfterMemo(t, `echo x > ../${EVIDENCE}/r.txt`, 'cwd in a project subfolder', {
    prepare: ws => mkdir(join(ws.project, 'sub')),
    input: ws => ({ cwd: join(ws.project, 'sub') }),
  });
  await denyBash(t, 'echo x > ../escape.txt', 'write-outside-checkout', 'cwd at the project, ../ leaves it');
  const ws = await createWorkspace(t);
  assertDeny(runHook(ws, bashInput(ws, 'echo x > y.txt', { cwd: ws.outside })), 'write-outside-checkout', 'cwd outside the project');
});

test('rule 2 takes relative paths from CLAUDE_PROJECT_DIR when the input has no cwd', async t => {
  await denyBash(t, 'echo x > ../escape.txt', 'write-outside-checkout', 'no cwd, ../ leaves the project', { input: { cwd: null } });
  await passBashAfterMemo(t, `echo x > ${EVIDENCE}/r.txt`, 'no cwd, evidence destination', { input: { cwd: null } });
});

test('rule 2 folds . and .. as text', async t => {
  await denyBash(t, `echo x > ${EVIDENCE}/../../../../escape.txt`, 'write-outside-checkout', '.. above the project');
  await denyBash(t, ws => `echo x > "${toSlash(ws.project)}/../outside/y.txt"`, 'write-outside-checkout', 'absolute path with ..');
  await passBashAfterMemo(t, `echo x > missing/../${EVIDENCE}/./f.txt`, '.. and . inside the project');
});

test('rule 2 compares whole path segments, not string prefixes', async t => {
  await denyBash(t, ws => `echo x > "${toSlash(ws.project)}-sibling/y.txt"`, 'write-outside-checkout', 'project-sibling');
});

const windowsOnly = { skip: !isWindows && 'Git Bash drive paths and drive case only exist on Windows' };

test('rule 2 reads the Git Bash drive form /c/… as C:/… (Windows)', windowsOnly, async t => {
  await passBashAfterMemo(t, ws => `echo x > "${driveForm(ws.project)}/${EVIDENCE}/d.txt"`, 'drive form inside the project');
  await denyBash(t, ws => `echo x > "${driveForm(ws.outside)}/y.txt"`, 'write-outside-checkout', 'drive form outside');
});

test('rule 2 ignores case in drive paths (Windows)', windowsOnly, async t => {
  await passBashAfterMemo(t, ws => `echo x > "${toSlash(ws.project).toUpperCase()}/${EVIDENCE}/u.txt"`, 'upper-cased project path');
  await passBashAfterMemo(t, ws => `echo x > "${toSlash(ws.project).replace(/^[A-Z]:/, drive => drive.toLowerCase())}/${EVIDENCE}/l.txt"`,
    'lower-case drive letter');
});

test('rule 2 blocks write tools under each temp root as temp-write', async t => {
  const ws = await createWorkspace(t, { separateTempRoots: true });
  writeMemo(ws);
  const scratchpad = join(ws.temps.TEMP, 'claude', 'C--project', '5e551011-0000-4000-8000-0000000000b1', 'scratchpad');
  assertDeny(runHook(ws, fileToolInput(ws, 'Write', join(scratchpad, 'x.md'))), 'temp-write', 'Write under TEMP (scratchpad)');
  assertDeny(runHook(ws, fileToolInput(ws, 'Edit', join(ws.temps.TMP, 'x.md'))), 'temp-write', 'Edit under TMP');
  assertDeny(runHook(ws, fileToolInput(ws, 'MultiEdit', join(ws.temps.TMPDIR, 'x.md'))), 'temp-write', 'MultiEdit under TMPDIR');
  assertDeny(runHook(ws, fileToolInput(ws, 'NotebookEdit', join(ws.temps.TEMP, 'n.ipynb'))), 'temp-write', 'NotebookEdit under TEMP');
  assertDeny(runHook(ws, fileToolInput(ws, 'Write', '/tmp/session-guard-x.md')), 'temp-write', 'Write under /tmp');
});

test('rule 2 leaves write tool paths outside the checkout that are not temp roots to rule 3 (line 104)', async t => {
  const ws = await createWorkspace(t);
  writeMemo(ws);
  assertNoDecision(runHook(ws, fileToolInput(ws, 'Write', join(notTempFolder, 'notes.md'))), 'Write outside, not temp, after the memo');
  assertNoDecision(runHook(ws, fileToolInput(ws, 'Write', join(ws.evidence, 'notes.md'))), 'Write inside the evidence folder');
});

test('rule 2 lets an allowed root win over a temp root inside it (line 105)', async t => {
  const ws = await createWorkspace(t, { tempInsideProject: true });
  writeMemo(ws);
  const scratchpad = join(ws.temp, 'claude', 'C--project', '5e551011-0000-4000-8000-0000000000b1', 'scratchpad');
  assertNoDecision(runHook(ws, fileToolInput(ws, 'Write', join(scratchpad, 'x.md'))), 'Write under a TEMP inside the project');
  assertNoDecision(runHook(ws, bashInput(ws, 'echo x > "$TEMP/y.txt"')), 'redirect into a TEMP inside the project');
});

test('rule 2 accepts the real target of a .backups junction as an allowed root (core-active)', async t => {
  const ws = await createWorkspace(t, { junction: true });
  writeMemo(ws);
  const target = `${toSlash(ws.linkTarget)}/verification/2026-10-10-operating-tool-guards`;
  assertNoDecision(runHook(ws, bashInput(ws, `echo x > "${target}/j.json"`)), 'redirect to the link target');
  assertNoDecision(runHook(ws, fileToolInput(ws, 'Write', `${target}/j.md`)), 'Write to the link target');
  assertNoDecision(runHook(ws, bashInput(ws, `echo x > ${EVIDENCE}/k.json`)), 'redirect through the link');
  assertDeny(runHook(ws, bashInput(ws, `echo x > "${toSlash(ws.linkTarget)}-other/y.txt"`)), 'write-outside-checkout',
    'sibling of the link target');
  assertDeny(runHook(ws, bashInput(ws, `echo x > "${outsideFile(ws)}"`)), 'write-outside-checkout', 'outside with a junction');
});

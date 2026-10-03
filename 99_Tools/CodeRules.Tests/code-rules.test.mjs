// Independent regressions for 99_Tools/CodeRules. Expectations come from the approved goal,
// the Changed/All decision and the SQL deferral decision, not from the checker's code: every
// fixture line below is chosen so its file, rule and line are known before the checker runs.
// Workflow tests take platform rules from the GitHub Actions reference and the job-local layout
// from the approved paths, not from the workflow's own text.
//
// Environment: CODE_RULES_PSSA_MANIFEST (approved PSScriptAnalyzer 1.25.0 manifest),
// CODE_RULES_PYTHON (default python3), CODE_RULES_WSL_DISTRIBUTION (required on Windows),
// CODE_RULES_RESULTS (raw evidence parent), CODE_RULES_TEST_WORK (temporary repositories).
import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import {
  appendFile, chmod, copyFile, cp, lstat, mkdir, readFile, readdir, rm, stat, symlink, writeFile,
} from 'node:fs/promises';
import { dirname, join } from 'node:path';
import { after, before, test } from 'node:test';
import {
  TemporaryRepository, createWorkspace, repositoryRoot, runChecker, sha256, toolEnvironment,
} from './repository-fixture.mjs';

const goodPowerShell = [
  'function Get-Value {',
  '    param(',
  '        [int]$Number',
  '    )',
  '    return $Number + 1',
  '}',
  '',
].join('\n');
// Line 2 is indented by two spaces: PSUseConsistentIndentation (4 spaces) applies.
const twoSpaceIndent = ['function Get-Indented {', '  return 1', '}', ''].join('\n');
// Line 1 has no spaces around '=': PSUseConsistentWhitespace (CheckOperator) applies.
const operatorWithoutSpaces = ['$value=2', ''].join('\n');
// Line 2 has an unexpected ')': a PowerShell parse error.
const powerShellParseError = ["Write-Output 'ok'", 'Write-Output )', ''].join('\n');
const goodPython = ['VALUE = 1', '', '', 'def read_value():', '    return VALUE', ''].join('\n');
// Line 3 is not valid Python syntax.
const pythonSyntaxError = ['VALUE = 1', '', 'def broken(:', '    return VALUE', ''].join('\n');
// Line 2 assigns a string to a number: TS2322.
const typeScriptError = [
  '// Intentional type error for the checker fixture.',
  "export const label: number = 'text';",
  '',
].join('\n');

const targetPaths = report => report.inputs.targets.map(target => target.path).sort();
const checkOf = (report, language) => report.checks.find(check => check.language === language);
const allDiagnostics = report => report.checks.flatMap(check => check.diagnostics ?? []);
const diagnosticsAt = (report, path) => allDiagnostics(report).filter(issue => issue.path === path);
const excludedReason = (report, path) => report.inputs.excluded.find(item => item.path === path)?.reason;
const commandLines = report => report.commands.map(command => [command.executable, ...command.args].join(' '));
const describeRun = run => `exit=${run.exit} results=${run.runDirectory}\n${run.text ?? run.stderr}`;

async function exists(path) {
  try {
    await stat(path);
    return true;
  } catch (error) {
    if (error.code === 'ENOENT') return false;
    throw error;
  }
}

function assertResultsKept(run) {
  assert.ok(run.runDirectory, `a new run directory must exist: ${run.stderr}`);
  assert.ok(run.report, 'results.json must be written');
  assert.match(run.text, /Code rules:/, 'results.txt must be written');
}

let workspace;

before(async () => {
  const { missing } = toolEnvironment();
  if (missing.length) throw new Error(`Test environment is incomplete: ${missing.join('; ')}`);
  workspace = await createWorkspace();
});

after(async () => {
  await workspace?.dispose();
});

test('Changed selects the change set while All adds unchanged files with the same processing', async t => {
  const repository = await TemporaryRepository.create(workspace, 'selection');
  await repository.write('.gitignore', 'ignored/\n');
  await repository.write('tools/old-bad.ps1', twoSpaceIndent);
  await repository.write('tools/keep.ps1', goodPowerShell);
  await repository.write('tools/modified.ps1', goodPowerShell);
  await repository.write('tools/rename-source.ps1', "Write-Output 'renamed without edits'\n");
  await repository.write('tools/delete-me.ps1', "Write-Output 'deleted in head'\n");
  await repository.write('tools/unstaged.ps1', goodPowerShell);
  await repository.write('03_Client/Assets/excluded.ps1', goodPowerShell);
  await repository.write('docs/readme.md', 'base\n');
  await repository.write('99_Tools/Architecture/Pipeline/module.py', goodPython);
  await repository.write('db/existing.sql', 'SELECT 1;\n');
  const base = repository.commit('base');

  await repository.write('tools/added with space.ps1', goodPowerShell);
  await repository.write('tools/modified.ps1', operatorWithoutSpaces);
  await mkdir(repository.path('tools/renamed dir'), { recursive: true });
  repository.git('mv', 'tools/rename-source.ps1', 'tools/renamed dir/이름 변경.ps1');
  repository.git('rm', '-q', 'tools/delete-me.ps1');
  await repository.write('03_Client/Assets/excluded.ps1', twoSpaceIndent);
  await repository.write('99_Tools/CodeRules.Tests/fixtures/intended-bad.ps1', twoSpaceIndent);
  await repository.write('99_Tools/CodeRules.Tests/fixtures-extra/not-a-fixture.ps1', twoSpaceIndent);
  await repository.write('99_Tools/CodeRules.Tests/test_helper.py', goodPython);
  await repository.write('docs/readme.md', 'head\n');
  repository.commit('head');

  await repository.write('tools/staged.ps1', goodPowerShell);
  repository.git('add', 'tools/staged.ps1');
  await repository.write('tools/unstaged.ps1', `${goodPowerShell}# unstaged edit\n`);
  await repository.write('tools/untracked é.ps1', goodPowerShell);
  await repository.write('db/new query.sql', 'THIS IS NOT ( VALID SQL\n');
  await repository.write('99_Tools/Architecture/Pipeline/new_module.py', goodPython);
  await repository.write('ignored/hidden.ps1', twoSpaceIndent);
  const bytesBefore = await repository.snapshot();
  const statusBefore = repository.git('status', '--porcelain=v1', '-z', '--untracked-files=all');

  const changed = await runChecker(workspace, repository, workspace.nextCase('selection-changed'), {
    scope: 'Changed', base,
  });
  const all = await runChecker(workspace, repository, workspace.nextCase('selection-all'), { scope: 'All', base });
  t.diagnostic(`Changed: ${changed.runDirectory}; All: ${all.runDirectory}`);
  assertResultsKept(changed);
  assertResultsKept(all);

  const expectedChanged = [
    '99_Tools/Architecture/Pipeline/new_module.py',
    '99_Tools/CodeRules.Tests/fixtures-extra/not-a-fixture.ps1',
    '99_Tools/CodeRules.Tests/test_helper.py',
    'db/new query.sql',
    'tools/added with space.ps1',
    'tools/modified.ps1',
    'tools/renamed dir/이름 변경.ps1',
    'tools/staged.ps1',
    'tools/unstaged.ps1',
    'tools/untracked é.ps1',
  ].sort();
  assert.deepEqual(targetPaths(changed.report), expectedChanged, describeRun(changed));
  assert.ok(changed.report.inputs.deleted.includes('tools/delete-me.ps1'), 'deletion must be recorded');
  assert.ok(changed.report.inputs.changes.some(change =>
    change.oldPath === 'tools/rename-source.ps1' && change.path === 'tools/renamed dir/이름 변경.ps1'),
  'rename must be recorded with its new path');
  for (const path of ['03_Client/Assets/excluded.ps1', 'docs/readme.md']) {
    assert.ok(excludedReason(changed.report, path), `${path} must be listed as excluded with a reason`);
  }
  assert.match(excludedReason(changed.report, '99_Tools/CodeRules.Tests/fixtures/intended-bad.ps1'), /fixtures/);

  assert.deepEqual(diagnosticsAt(changed.report, 'tools/modified.ps1').map(issue => [issue.rule, issue.line]),
    [['PSUseConsistentWhitespace', 1]]);
  assert.deepEqual(
    diagnosticsAt(changed.report, '99_Tools/CodeRules.Tests/fixtures-extra/not-a-fixture.ps1')
      .map(issue => [issue.rule, issue.line]),
    [['PSUseConsistentIndentation', 2]],
    'only the fixtures/ root is excluded, not similarly named test folders',
  );
  for (const path of ['tools/old-bad.ps1', '03_Client/Assets/excluded.ps1', 'ignored/hidden.ps1',
    '99_Tools/CodeRules.Tests/fixtures/intended-bad.ps1']) {
    assert.deepEqual(diagnosticsAt(changed.report, path), [], `${path} is outside the Changed targets`);
  }
  assert.notEqual(changed.exit, 0, 'violations must fail the run');
  assert.equal(changed.report.summary.violations, 2);

  const allTargets = new Set(targetPaths(all.report));
  for (const path of [...expectedChanged, 'tools/old-bad.ps1', 'tools/keep.ps1', 'db/existing.sql',
    '99_Tools/Architecture/Pipeline/module.py', '99_Tools/CodeRules/check-powershell.ps1',
    '99_Tools/CodeRules/check-python.py']) {
    assert.ok(allTargets.has(path), `All must include ${path}`);
  }
  for (const path of ['tools/delete-me.ps1', 'tools/rename-source.ps1', 'ignored/hidden.ps1',
    '03_Client/Assets/excluded.ps1', '99_Tools/CodeRules.Tests/fixtures/intended-bad.ps1', 'docs/readme.md']) {
    assert.ok(!allTargets.has(path), `All must not inspect ${path}`);
  }
  assert.deepEqual(diagnosticsAt(all.report, 'tools/old-bad.ps1').map(issue => [issue.rule, issue.line]),
    [['PSUseConsistentIndentation', 2]]);
  assert.deepEqual(diagnosticsAt(all.report, 'tools/modified.ps1'), diagnosticsAt(changed.report, 'tools/modified.ps1'),
    'the same file must get the same diagnostics in both scopes');
  for (const path of ['03_Client/Assets/excluded.ps1', '99_Tools/CodeRules.Tests/fixtures/intended-bad.ps1']) {
    assert.equal(excludedReason(all.report, path), excludedReason(changed.report, path), `${path} exclusion differs`);
  }
  assert.deepEqual(all.report.settings, changed.report.settings, 'settings and their hashes must be identical');
  assert.equal(checkOf(all.report, 'powershell').version, '1.25.0');
  assert.equal(checkOf(changed.report, 'powershell').version, '1.25.0');
  assert.equal(checkOf(all.report, 'python').version, checkOf(changed.report, 'python').version);
  assert.notEqual(all.exit, 0);
  assert.match(all.text, /tools\/old-bad\.ps1:2:\d+ PSUseConsistentIndentation/);
  assert.match(changed.text, /tools\/modified\.ps1:1:\d+ PSUseConsistentWhitespace/);

  assert.deepEqual(checkOf(changed.report, 'sql').targets.map(target => target.path), ['db/new query.sql']);
  assert.deepEqual(checkOf(all.report, 'sql').targets.map(target => target.path).sort(),
    ['db/existing.sql', 'db/new query.sql']);

  assert.deepEqual(await repository.snapshot(), bytesBefore, 'the checker must not modify or add worktree files');
  assert.equal(repository.git('status', '--porcelain=v1', '-z', '--untracked-files=all'), statusBefore);
});

test('changed files that follow the rules pass without executing or importing them', async () => {
  const repository = await TemporaryRepository.create(workspace, 'normal');
  const base = repository.commit('base');
  const powerShellMarker = repository.path('tools/POWERSHELL-EXECUTED');
  await repository.write('tools/Format-Value.ps1', goodPowerShell);
  await repository.write('tools/Write-Marker.ps1', [
    `$markerPath = '${powerShellMarker.replaceAll("'", "''")}'`,
    'New-Item -ItemType File -Path $markerPath -Force | Out-Null',
    '',
  ].join('\n'));
  await repository.write('99_Tools/Architecture/Pipeline/reader.py', goodPython);
  await repository.write('99_Tools/Architecture/Pipeline/side_effect.py', [
    'import pathlib',
    '',
    'pathlib.Path(__file__).with_name("PYTHON-IMPORTED").write_text("imported", encoding="utf-8")',
    '',
  ].join('\n'));
  const bytesBefore = await repository.snapshot();

  const run = await runChecker(workspace, repository, workspace.nextCase('normal-pass'), { scope: 'Changed', base });
  assertResultsKept(run);
  assert.equal(run.exit, 0, describeRun(run));
  assert.equal(run.report.summary.passed, true);
  assert.equal(run.report.summary.violations, 0);
  assert.equal(run.report.summary.failures, 0);
  assert.equal(checkOf(run.report, 'powershell').status, 'passed');
  assert.equal(checkOf(run.report, 'powershell').targetCount, 2);
  assert.equal(checkOf(run.report, 'python').status, 'passed');
  assert.equal(checkOf(run.report, 'python').targetCount, 2);
  assert.equal(await exists(powerShellMarker), false, 'inspected PowerShell must not run');
  assert.equal(await exists(repository.path('99_Tools/Architecture/Pipeline/PYTHON-IMPORTED')), false,
    'inspected Python must not be imported');
  assert.equal(await exists(repository.path('99_Tools/Architecture/Pipeline/__pycache__')), false,
    'no bytecode may be written');
  assert.deepEqual(await repository.snapshot(), bytesBefore);
});

test('parse and compile failures fail with file, rule and line', async () => {
  const repository = await TemporaryRepository.create(workspace, 'parse');
  const base = repository.commit('base');
  await repository.write('tools/broken.ps1', powerShellParseError);
  await repository.write('99_Tools/Architecture/Pipeline/broken.py', pythonSyntaxError);
  await repository.write('scripts/outside_scope.py', pythonSyntaxError);

  const run = await runChecker(workspace, repository, workspace.nextCase('parse-failures'), { scope: 'Changed', base });
  assertResultsKept(run);
  assert.notEqual(run.exit, 0, describeRun(run));
  assert.deepEqual(diagnosticsAt(run.report, 'tools/broken.ps1').map(issue => [issue.rule, issue.line]),
    [['PowerShellParse', 2]]);
  assert.deepEqual(
    diagnosticsAt(run.report, '99_Tools/Architecture/Pipeline/broken.py').map(issue => [issue.rule, issue.line]),
    [['PythonCompile', 3]],
  );
  assert.match(run.text, /tools\/broken\.ps1:2:\d+ PowerShellParse/);
  assert.match(run.text, /99_Tools\/Architecture\/Pipeline\/broken\.py:3:\d+ PythonCompile/);
  // Python outside the approved Architecture/CodeRules roots is out of scope, but listed.
  assert.ok(excludedReason(run.report, 'scripts/outside_scope.py'));
  assert.ok(!targetPaths(run.report).includes('scripts/outside_scope.py'));
});

test('a source suppression attribute does not hide a selected PowerShell rule', async () => {
  const repository = await TemporaryRepository.create(workspace, 'suppression');
  const base = repository.commit('base');
  // Line 4 is indented by two spaces; the attribute would hide it from a default analyzer run.
  await repository.write('tools/suppressed.ps1', [
    'function Get-Suppressed {',
    "    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseConsistentIndentation', '')]",
    '    param()',
    '  return 1',
    '}',
    '',
  ].join('\n'));

  const run = await runChecker(workspace, repository, workspace.nextCase('source-suppression'), {
    scope: 'Changed', base,
  });
  assertResultsKept(run);
  assert.notEqual(run.exit, 0, describeRun(run));
  assert.deepEqual(diagnosticsAt(run.report, 'tools/suppressed.ps1').map(issue => [issue.rule, issue.line]),
    [['PSUseConsistentIndentation', 4]]);
});

test('missing, mismatched or failing tools fail; zero targets are not applicable', async t => {
  const repository = await TemporaryRepository.create(workspace, 'tools');
  const base = repository.commit('base');
  await repository.write('tools/sample.ps1', goodPowerShell);
  await repository.write('99_Tools/Architecture/Pipeline/sample.py', goodPython);
  const { tools } = toolEnvironment();

  const expectFailure = async (name, overrides, failedLanguage, otherLanguage) => {
    const run = await runChecker(workspace, repository, workspace.nextCase(name), {
      scope: 'Changed', base, tools: overrides,
    });
    assertResultsKept(run);
    assert.notEqual(run.exit, 0, `${name}: ${describeRun(run)}`);
    const failed = checkOf(run.report, failedLanguage);
    assert.equal(failed.status, 'failed', `${name}: ${failed.message}`);
    assert.ok(failed.message, `${name}: the failure reason must be recorded`);
    assert.equal(checkOf(run.report, otherLanguage).status, 'passed', `${name}: other checks still run`);
    assert.equal(run.report.summary.passed, false);
    t.diagnostic(`${name}: ${failed.message}`);
    return failed;
  };

  await expectFailure('pssa-manifest-not-given', { pssaManifest: null }, 'powershell', 'python');
  await expectFailure('pssa-manifest-absent',
    { pssaManifest: join(workspace.work, 'absent', 'PSScriptAnalyzer.psd1') }, 'powershell', 'python');
  await expectFailure('pwsh-absent', { pwsh: 'code-rules-missing-pwsh' }, 'powershell', 'python');
  await expectFailure('python-absent', { python: '/nonexistent/code-rules/python3' }, 'python', 'powershell');
  await expectFailure('python-not-following-contract', { python: 'false' }, 'python', 'powershell');

  // A changed analyzer file must not be accepted as the approved distribution.
  const tamperedModule = join(workspace.work, 'pssa-tampered', 'PSScriptAnalyzer', '1.25.0');
  await cp(dirname(tools.pssaManifest), tamperedModule, { recursive: true });
  await writeFile(join(tamperedModule, 'README.md'), '\nchanged', { flag: 'a' });
  const tampered = await expectFailure('pssa-distribution-tampered',
    { pssaManifest: join(tamperedModule, 'PSScriptAnalyzer.psd1') }, 'powershell', 'python');
  assert.match(tampered.message, /hash|mismatch/i);

  await repository.remove('tools/sample.ps1');
  await repository.remove('99_Tools/Architecture/Pipeline/sample.py');
  await repository.write('docs/notes.md', 'outside the checked languages\n');
  const empty = await runChecker(workspace, repository, workspace.nextCase('zero-targets-without-tools'), {
    scope: 'Changed', base, tools: { pssaManifest: null, python: '/nonexistent/code-rules/python3' },
  });
  assertResultsKept(empty);
  assert.equal(empty.exit, 0, describeRun(empty));
  for (const language of ['powershell', 'sql', 'typescript', 'python']) {
    assert.equal(checkOf(empty.report, language).status, 'not-applicable', language);
  }
  assert.ok(commandLines(empty.report).every(line => /^git /.test(line)),
    `no tool may be started without targets: ${commandLines(empty.report).join(' | ')}`);
});

// CI saves the approved analyzer anew in every job. Save-Module also writes PSGetModuleInfo.xml
// (install location, time, download count), which differs per machine; the package files do not.
test('the approved analyzer passes from any saved location; changed, missing or relabelled files fail', async t => {
  const repository = await TemporaryRepository.create(workspace, 'analyzer-location');
  const base = repository.commit('base');
  await repository.write('tools/sample.ps1', goodPowerShell);
  // Line 2 is indented by two spaces, so an accepted analyzer must report exactly this issue.
  await repository.write('tools/indented.ps1', twoSpaceIndent);
  const { tools } = toolEnvironment();
  const approved = dirname(tools.pssaManifest);
  const relocated = join(workspace.work, 'analyzer saved elsewhere', 'PSScriptAnalyzer', '1.25.0');
  await cp(approved, relocated, { recursive: true });
  const analyze = async name => {
    const run = await runChecker(workspace, repository, workspace.nextCase(name), {
      scope: 'Changed', base, tools: { pssaManifest: join(relocated, 'PSScriptAnalyzer.psd1') },
    });
    assertResultsKept(run);
    t.diagnostic(`${name}: exit=${run.exit}; ${checkOf(run.report, 'powershell').message || 'no message'}`);
    return run;
  };
  const expectAnalyzed = (name, run) => {
    const powershell = checkOf(run.report, 'powershell');
    assert.equal(powershell.completed, true, `${name}: ${describeRun(run)}`);
    assert.equal(powershell.version, '1.25.0');
    assert.deepEqual(diagnosticsAt(run.report, 'tools/indented.ps1').map(issue => [issue.rule, issue.line]),
      [['PSUseConsistentIndentation', 2]], `${name}: the relocated analyzer must really run`);
    assert.deepEqual(diagnosticsAt(run.report, 'tools/sample.ps1'), []);
  };

  const metadata = join(relocated, 'PSGetModuleInfo.xml');
  // Windows marks the saved metadata hidden, which refuses an in-place overwrite.
  await rm(metadata, { force: true });
  await writeFile(metadata, [
    '<Objs Version="1.1.0.1" xmlns="http://schemas.microsoft.com/powershell/2004/04">',
    '  <S N="InstalledLocation">another machine/another job</S>',
    '  <S N="InstalledDate">2030-01-01T00:00:00</S>',
    '</Objs>',
    '',
  ].join('\n'));
  expectAnalyzed('metadata-rewritten', await analyze('analyzer-install-metadata-rewritten'));
  await rm(metadata, { force: true });
  expectAnalyzed('metadata-absent', await analyze('analyzer-install-metadata-absent'));
  await repository.remove('tools/indented.ps1');
  const clean = await analyze('analyzer-relocated-clean-input');
  assert.equal(clean.exit, 0, describeRun(clean));
  assert.equal(checkOf(clean.report, 'powershell').status, 'passed');
  await repository.write('tools/indented.ps1', twoSpaceIndent);

  // Each change is undone from the approved copy before the next one.
  const expectRefused = async (name, relativePath, change) => {
    const file = join(relocated, ...relativePath.split('/'));
    await change(file);
    try {
      const run = await analyze(name);
      const powershell = checkOf(run.report, 'powershell');
      assert.notEqual(run.exit, 0, `${name}: ${describeRun(run)}`);
      assert.equal(powershell.status, 'failed', name);
      assert.equal(powershell.completed, false, `${name}: an unaccepted analyzer must not report an analysis`);
      assert.deepEqual(diagnosticsAt(run.report, 'tools/indented.ps1'), [], `${name}: the analyzer must not run`);
      assert.ok(powershell.message, `${name}: the refusal reason must be recorded`);
    } finally {
      await copyFile(join(approved, ...relativePath.split('/')), file);
    }
  };
  // The binary module that pwsh 7 imports; trailing bytes keep it loadable.
  await expectRefused('analyzer-loaded-binary-changed', 'PSv7/Microsoft.Windows.PowerShell.ScriptAnalyzer.dll',
    file => appendFile(file, Buffer.from([0])));
  await expectRefused('analyzer-distribution-file-missing', 'PSv7/Pluralize.NET.dll', file => rm(file));
  await expectRefused('analyzer-manifest-other-version', 'PSScriptAnalyzer.psd1', async file => {
    const text = await readFile(file, 'utf8');
    const relabelled = text.replace("ModuleVersion = '1.25.0'", "ModuleVersion = '1.25.1'");
    assert.notEqual(relabelled, text, 'the fixture must change the manifest version');
    await writeFile(file, relabelled);
  });
});

test('configuration faults fail and keep results', async () => {
  const repository = await TemporaryRepository.create(workspace, 'configuration');
  const base = repository.commit('base');
  await repository.write('tools/sample.ps1', goodPowerShell);
  const original = await repository.read('99_Tools/CodeRules/config.json');

  const mismatched = JSON.parse(original);
  mismatched.powerShellVersion = '1.24.0';
  await repository.write('99_Tools/CodeRules/config.json', `${JSON.stringify(mismatched, null, 2)}\n`);
  const version = await runChecker(workspace, repository, workspace.nextCase('config-version-mismatch'), {
    scope: 'Changed', base,
  });
  assertResultsKept(version);
  assert.notEqual(version.exit, 0, describeRun(version));
  assert.equal(checkOf(version.report, 'powershell').status, 'failed');

  await repository.write('99_Tools/CodeRules/config.json', '{ not json');
  const broken = await runChecker(workspace, repository, workspace.nextCase('config-invalid-json'), {
    scope: 'Changed', base,
  });
  assertResultsKept(broken);
  assert.notEqual(broken.exit, 0, describeRun(broken));
  assert.ok(broken.report.errors.length > 0);
  assert.deepEqual(broken.report.checks, []);
  await repository.write('99_Tools/CodeRules/config.json', original);
});

test('Git, base, scope and root failures fail closed and keep results', async t => {
  const repository = await TemporaryRepository.create(workspace, 'git-failures');
  repository.commit('base');
  await repository.write('tools/sample.ps1', goodPowerShell);

  const cases = [
    ['base-unknown', { scope: 'Changed', base: 'no-such-revision' }],
    ['base-option-like', { scope: 'Changed', base: '-c' }],
    ['base-empty', { scope: 'Changed', base: '' }],
    ['scope-unknown', { scope: 'Everything', base: 'HEAD' }],
    ['git-unavailable', { scope: 'Changed', base: 'HEAD', env: { GIT_DIR: join(workspace.work, 'no-git-dir') } }],
  ];
  for (const [name, options] of cases) {
    const run = await runChecker(workspace, repository, workspace.nextCase(name), options);
    assertResultsKept(run);
    assert.notEqual(run.exit, 0, `${name}: ${describeRun(run)}`);
    assert.ok(run.report.errors.length > 0, `${name}: the error must be recorded`);
    assert.equal(run.report.inputs, null, `${name}: nothing may be inspected`);
    assert.equal(run.report.summary.passed, false);
    assert.match(run.text, /ERROR:/);
    t.diagnostic(`${name}: ${run.report.errors.join(' | ')}`);
  }

  // A nested folder with its own checker copy is not the Git repository root.
  await cp(repository.path('99_Tools/CodeRules'), repository.path('nested/99_Tools/CodeRules'), { recursive: true });
  const nested = await runChecker(workspace, repository, workspace.nextCase('root-not-repository-root'), {
    scope: 'Changed', base: 'HEAD', root: repository.path('nested'),
  });
  assertResultsKept(nested);
  assert.notEqual(nested.exit, 0, describeRun(nested));
  assert.match(nested.report.errors.join(' '), /root/i);
  assert.equal(nested.report.inputs, null);
});

test('SQL is listed as deferred, never inspected, and kept apart from pass and failure counts', async () => {
  const repository = await TemporaryRepository.create(workspace, 'sql');
  await repository.write('db/committed.sql', 'SELECT 1;\n');
  const base = repository.commit('base');
  const unparsable = 'DECLARE @checks TABLE (\n  -- intentionally broken and unformatted\nSELECT FROM WHERE (\n';
  await repository.write('db/changed query.sql', unparsable);

  const deferred = await runChecker(workspace, repository, workspace.nextCase('sql-deferred-only'), {
    scope: 'Changed', base,
  });
  assertResultsKept(deferred);
  const sql = checkOf(deferred.report, 'sql');
  assert.equal(sql.status, 'deferred');
  assert.equal(sql.targetCount, 1);
  assert.deepEqual(sql.targets.map(target => [target.path, target.sha256]),
    [['db/changed query.sql', sha256(Buffer.from(unparsable))]]);
  assert.match(sql.evidence, /goal\.md#sql-deferred$/);
  assert.ok(sql.message, 'the user-decision reason must be recorded');
  assert.equal(deferred.report.summary.deferredTargets, 1);
  assert.equal(deferred.report.summary.failures, 0, 'deferral is not a failure');
  assert.equal(deferred.report.summary.passed, null, 'deferral is not a pass');
  assert.notEqual(deferred.report.summary.status, 'PASS');
  assert.equal(deferred.exit, 0, describeRun(deferred));
  assert.ok(!deferred.report.inputs.excluded.some(item => item.path.endsWith('.sql')), 'SQL must not be excluded');
  assert.ok(!commandLines(deferred.report).some(line => /sqlfluff/i.test(line)), 'SQL tools must not run');
  assert.match(deferred.text, /db\/changed query\.sql/);
  assert.match(deferred.text, /goal\.md#sql-deferred/);
  assert.match(deferred.text, /deferred/i);

  await repository.write('tools/bad.ps1', twoSpaceIndent);
  const mixed = await runChecker(workspace, repository, workspace.nextCase('sql-deferred-with-failure'), {
    scope: 'Changed', base,
  });
  assertResultsKept(mixed);
  assert.notEqual(mixed.exit, 0, describeRun(mixed));
  assert.equal(checkOf(mixed.report, 'sql').status, 'deferred');
  assert.equal(checkOf(mixed.report, 'powershell').status, 'failed');
  assert.deepEqual(diagnosticsAt(mixed.report, 'tools/bad.ps1').map(issue => [issue.rule, issue.line]),
    [['PSUseConsistentIndentation', 2]]);
  assert.equal(mixed.report.summary.failures, 1, 'only the PowerShell check fails');
  assert.equal(mixed.report.summary.deferredTargets, 1);

  const all = await runChecker(workspace, repository, workspace.nextCase('sql-deferred-all'), { scope: 'All', base });
  assertResultsKept(all);
  assert.deepEqual(checkOf(all.report, 'sql').targets.map(target => target.path).sort(),
    ['db/changed query.sql', 'db/committed.sql']);
  assert.equal(all.report.summary.deferredTargets, 2);
});

test('link inputs and output paths through links are refused', async t => {
  const repository = await TemporaryRepository.create(workspace, 'links');
  await repository.write('tools/real.ps1', twoSpaceIndent);
  const base = repository.commit('base');
  const outside = join(workspace.work, 'outside-links');
  await mkdir(join(outside, 'directory'), { recursive: true });
  await writeFile(join(outside, 'outside.ps1'), twoSpaceIndent);
  await writeFile(join(outside, 'directory', 'escaped.ps1'), twoSpaceIndent);

  // DEVELOPMENT promises that link inputs are refused, whether they point outside or inside.
  const refusesFileLink = async (st, name, target) => {
    try {
      await symlink(target, repository.path(name), 'file');
    } catch (error) {
      if (['EPERM', 'EACCES'].includes(error.code)) {
        st.skip(`file links cannot be created here: ${error.code}`);
        return;
      }
      throw error;
    }
    const run = await runChecker(workspace, repository, workspace.nextCase(`file-${name.split('/').pop()}`), {
      scope: 'Changed', base,
    });
    assertResultsKept(run);
    assert.notEqual(run.exit, 0, describeRun(run));
    assert.ok(run.report.errors.length > 0, 'the refusal must be recorded');
    assert.deepEqual(diagnosticsAt(run.report, name), [], 'the link target must not be analyzed');
    st.diagnostic(run.report.errors.join(' | '));
    await repository.remove(name);
  };

  await t.test('file pointer to outside', st => refusesFileLink(st, 'tools/pointer.ps1', join(outside, 'outside.ps1')));
  await t.test('file pointer inside the repository', st =>
    refusesFileLink(st, 'tools/inside-pointer.ps1', repository.path('tools/real.ps1')));

  await t.test('directory pointer', async st => {
    const type = process.platform === 'win32' ? 'junction' : 'dir';
    await symlink(join(outside, 'directory'), repository.path('tools/pointed-dir'), type);
    const listed = repository.git('ls-files', '--others', '--exclude-standard', '-z').split('\0');
    const run = await runChecker(workspace, repository, workspace.nextCase('directory-pointer'), {
      scope: 'Changed', base,
    });
    assertResultsKept(run);
    const behindPointer = path => path.startsWith('tools/pointed-dir/');
    const analyzedBehindPointer = allDiagnostics(run.report).some(issue => behindPointer(issue.path));
    assert.ok(!analyzedBehindPointer, 'outside files must not be analyzed');
    assert.ok(!(run.report.inputs?.targets ?? []).some(target => behindPointer(target.path)));
    if (listed.some(behindPointer)) {
      // Git lists files behind the pointer on this platform, so the checker has to refuse the run.
      assert.notEqual(run.exit, 0, describeRun(run));
      assert.ok(run.report.errors.length > 0);
    }
    st.diagnostic(`${type}: Git listed ${listed.filter(behindPointer).join(', ') || 'nothing'} behind the pointer; `
      + `errors: ${run.report.errors.join(' | ') || 'none'}`);
    await repository.remove('tools/pointed-dir');
  });

  await t.test('output through a pointer', async () => {
    const outsideOutput = join(outside, 'output-target');
    await mkdir(outsideOutput, { recursive: true });
    const pointedOutput = join(workspace.work, 'pointed-output');
    await symlink(outsideOutput, pointedOutput, process.platform === 'win32' ? 'junction' : 'dir');
    const run = await runChecker(workspace, repository, workspace.nextCase('output-pointer'), {
      scope: 'Changed', base, output: join(pointedOutput, 'nested'),
    });
    assert.notEqual(run.exit, 0, `output through a pointer must be refused: ${run.stdout}${run.stderr}`);
    assert.deepEqual(await readdir(outsideOutput), [], 'nothing may be written through the pointer');
  });
});

test('TypeScript runs the three existing typecheck scripts without emitting or running package scripts', async () => {
  const repository = await TemporaryRepository.create(workspace, 'typescript');
  await repository.addTypeScriptFrontend();
  await repository.write('05_Management/frontend/src/good.ts', [
    'export function double(value: number): number {',
    '  return value * 2;',
    '}',
    '',
  ].join('\n'));
  repository.commit('base');
  const frontend = '05_Management/frontend';

  await repository.write(`${frontend}/src/bad.ts`, typeScriptError);
  const bytesBefore = await repository.snapshot();
  const failing = await runChecker(workspace, repository, workspace.nextCase('typescript-type-error'), {
    scope: 'Changed', base: 'HEAD',
  });
  assertResultsKept(failing);
  assert.notEqual(failing.exit, 0, describeRun(failing));
  const failed = checkOf(failing.report, 'typescript');
  assert.equal(failed.status, 'failed', failed.message);
  assert.deepEqual(failed.scripts.map(script => script.name), ['typecheck', 'desktop:typecheck', 'mcp:typecheck']);
  assert.ok(diagnosticsAt(failing.report, `${frontend}/src/bad.ts`)
    .some(issue => issue.rule === 'TS2322' && issue.line === 2), describeRun(failing));
  assert.match(failing.text, /05_Management\/frontend\/src\/bad\.ts:2:\d+ TS2322/);
  assert.deepEqual(await repository.snapshot(), bytesBefore, 'typecheck must not emit or change files');
  await repository.remove(`${frontend}/src/bad.ts`);

  const packageText = await repository.read(`${frontend}/package.json`);
  const injected = JSON.parse(packageText);
  injected.scripts.typecheck = 'tsc --noEmit && node -e "require(\'fs\').writeFileSync(\'SCRIPT-EXECUTED\', \'x\')"';
  await repository.write(`${frontend}/package.json`, `${JSON.stringify(injected, null, 2)}\n`);
  const shell = await runChecker(workspace, repository, workspace.nextCase('typescript-script-not-executed'), {
    scope: 'Changed', base: 'HEAD',
  });
  assertResultsKept(shell);
  assert.notEqual(shell.exit, 0, describeRun(shell));
  assert.equal(checkOf(shell.report, 'typescript').status, 'failed');
  assert.equal(await exists(repository.path(`${frontend}/SCRIPT-EXECUTED`)), false, 'package scripts must not run');
  assert.equal(await exists(repository.path('SCRIPT-EXECUTED')), false, 'package scripts must not run');
  await repository.write(`${frontend}/package.json`, packageText);

  const all = await runChecker(workspace, repository, workspace.nextCase('typescript-all'), {
    scope: 'All', base: 'HEAD',
  });
  assertResultsKept(all);
  assert.equal(checkOf(all.report, 'typescript').status, 'passed', describeRun(all));
  assert.equal(all.exit, 0, describeRun(all));
});

// The compiler may only read repository files. Configuration and import forms that name a file
// beside the repository must be refused, and the recognised forms before the compiler starts.
test('TypeScript keeps real JSONC config and refuses config or sources outside the repository', async t => {
  const repository = await TemporaryRepository.create(workspace, 'typescript-boundaries');
  await repository.addTypeScriptFrontend();
  const frontend = '05_Management/frontend';
  await repository.write(`${frontend}/src/good.ts`, 'export const value = 1;\n');
  repository.commit('base');
  const originalConfig = await repository.read(`${frontend}/tsconfig.json`);
  const project = JSON.parse(originalConfig);
  // The repository is workspace.work/typescript-boundaries, so '../../../typescript-outside' from
  // the frontend and '../../../../typescript-outside' from src/ name this folder beside it.
  const outside = join(workspace.work, 'typescript-outside');
  await mkdir(outside, { recursive: true });
  await writeFile(join(outside, 'mod.ts'), 'export const outside = 1;\n');
  await writeFile(join(outside, 'base.json'), '{ "compilerOptions": { "strict": true } }\n');
  await writeFile(join(outside, 'tsconfig.json'),
    '{ "compilerOptions": { "composite": true }, "files": ["mod.ts"] }\n');

  const compilerStarted = run => commandLines(run.report).some(line => /node_modules[\\/]typescript[\\/]/.test(line));
  const check = async name => {
    const run = await runChecker(workspace, repository, workspace.nextCase(name), { scope: 'Changed', base: 'HEAD' });
    assertResultsKept(run);
    t.diagnostic(`${name}: exit=${run.exit}; ${checkOf(run.report, 'typescript').message}`);
    return run;
  };
  const expectRefused = (name, run, { beforeCompiler }) => {
    const typescript = checkOf(run.report, 'typescript');
    assert.notEqual(run.exit, 0, `${name}: ${describeRun(run)}`);
    assert.equal(typescript.status, 'failed', `${name}: ${typescript.message}`);
    assert.equal(typescript.completed, false, `${name}: a refused input is not a completed check`);
    assert.ok(typescript.message, `${name}: the refusal reason must be recorded`);
    if (beforeCompiler) {
      assert.ok(!compilerStarted(run),
        `${name}: the compiler must not start: ${commandLines(run.report).join(' | ')}`);
    }
  };

  // JSONC as tsc accepts it: comments, trailing commas and strings holding '//', '/**/' and ',]'.
  await repository.write(`${frontend}/tsconfig.json`, [
    '{',
    '  "$schema": "https://json.schemastore.org/tsconfig", // editor schema',
    `  "compilerOptions": ${JSON.stringify(project.compilerOptions)}, /* fixture options */`,
    '  "include": ["src/**/*.ts",],',
    '  "exclude": ["src/*/generated/**", "src/odd,]name.ts",],',
    '}',
    '',
  ].join('\n'));
  const jsonc = await check('typescript-jsonc-config');
  assert.equal(checkOf(jsonc.report, 'typescript').status, 'passed', describeRun(jsonc));
  assert.equal(jsonc.exit, 0, describeRun(jsonc));

  const configCases = [
    ['typescript-config-invalid', '{ "include": ["src"]\n'],
    ['typescript-extends-outside', { ...project, extends: '../../../typescript-outside/base.json' }],
    ['typescript-reference-outside', { ...project, references: [{ path: '../../../typescript-outside' }] }],
    ['typescript-paths-outside', {
      ...project,
      compilerOptions: { ...project.compilerOptions, paths: { '@outside/*': ['../../../typescript-outside/*'] } },
    }],
  ];
  for (const [name, config] of configCases) {
    const text = typeof config === 'string' ? config : `${JSON.stringify(config, null, 2)}\n`;
    await repository.write(`${frontend}/tsconfig.json`, text);
    expectRefused(name, await check(name), { beforeCompiler: true });
  }
  await repository.write(`${frontend}/tsconfig.json`, originalConfig);

  // Every form named next to the preflight scanner; the escaped one only leaves the
  // repository after the string escapes are decoded as the compiler decodes them.
  const outsideModule = '../../../../typescript-outside/mod';
  const sourceCases = [
    ['typescript-import-from', `import { outside } from '${outsideModule}';\nexport const copy = outside;\n`],
    ['typescript-export-from', `export { outside } from "${outsideModule}";\n`],
    ['typescript-side-effect-import', `import '${outsideModule}';\n`],
    ['typescript-dynamic-import', `export const load = () => import('${outsideModule}');\n`],
    ['typescript-require', [
      'declare const require: (name: string) => unknown;',
      `export const loaded = require('${outsideModule}');`,
      '',
    ].join('\n')],
    ['typescript-reference-path', `/// <reference path="${outsideModule}.ts" />\nexport {};\n`],
    ['typescript-escaped-specifier', "import '.\\u002e/.\\x2e/.\\u{2e}/.\\u002e/typescript-outside/mod';\n"],
  ];
  for (const [name, source] of sourceCases) {
    await repository.write(`${frontend}/src/probe.ts`, source);
    expectRefused(name, await check(name), { beforeCompiler: true });
  }

  // The scanner is a documented approximation; a form it does not see must still be refused.
  await repository.write(`${frontend}/src/probe.ts`, `import /* kept */ '${outsideModule}';\n`);
  expectRefused('typescript-unrecognised-import-form', await check('typescript-unrecognised-import-form'),
    { beforeCompiler: false });
  await repository.remove(`${frontend}/src/probe.ts`);
});

// The real Management frontend imports '../../records/catalog.json' from a test; its tsconfig
// sets resolveJsonModule. The fixture configs get the same option so tsc accepts that import.
async function enableJsonModules(repository, extraOptions = {}) {
  for (const name of ['tsconfig.json', 'tsconfig.electron.json', 'tsconfig.mcp.json']) {
    const path = `05_Management/frontend/${name}`;
    const project = JSON.parse(await repository.read(path));
    Object.assign(project.compilerOptions, { resolveJsonModule: true, ...extraOptions });
    await repository.write(path, `${JSON.stringify(project, null, 2)}\n`);
  }
}

// Defect #5: Linux All run 37109061247 stopped the whole TypeScript check with
// "ENOTDIR … lstat …/05_Management/records/catalog.json/index.ts". On POSIX a path below a file
// is ENOTDIR, not ENOENT. A missing module candidate is not an input failure, and modules the
// compiler reads outside the frontend must still be checked and recorded before it starts.
test('TypeScript follows a JSON import and module imports beyond the frontend as checked inputs', async t => {
  const repository = await TemporaryRepository.create(workspace, 'typescript-json-import');
  await repository.addTypeScriptFrontend();
  // With outDir, TypeScript 7 defaults rootDir to the config folder; the shared modules sit beside it.
  await enableJsonModules(repository, { rootDir: '..' });
  const frontend = '05_Management/frontend';
  await repository.write(`${frontend}/src/good.ts`, 'export const value = 1;\n');
  repository.commit('base');

  // Each module below is only reachable through one import form, never through the frontend tree.
  const inputs = {
    '05_Management/records/catalog.json': '{ "records": [] }\n',
    '05_Management/shared/plain.ts': 'export const plain = 1;\n',
    '05_Management/shared/mapped.ts': 'export const mapped = 2;\n',
    '05_Management/shared/folder/index.ts': 'export const folder = 3;\n',
    '05_Management/shared/widgets.v2/index.ts': 'export const widget = 4;\n',
    '05_Management/shared/explicit.ts': 'declare const explicitReference: number;\n',
  };
  for (const [path, content] of Object.entries(inputs)) await repository.write(path, content);
  await repository.write(`${frontend}/src/consumer.ts`, [
    '/// <reference path="../../shared/explicit.ts" />',
    "import fixture from '../../records/catalog.json';",
    "import { plain } from '../../shared/plain';",
    "import { mapped } from '../../shared/mapped.js';",
    "import { folder } from '../../shared/folder';",
    "import { widget } from '../../shared/widgets.v2';",
    'export const total: number = fixture.records.length + plain + mapped + folder + widget;',
    '',
  ].join('\n'));

  const belowFile = join(repository.path('05_Management/records/catalog.json'), 'index.ts');
  const belowFileResult = await lstat(belowFile).then(() => 'exists', error => error.code);
  t.diagnostic(`${process.platform}: lstat of catalog.json/index.ts reports ${belowFileResult}`);
  if (process.platform !== 'win32') {
    assert.equal(belowFileResult, 'ENOTDIR', 'this run must exercise the POSIX meaning of the original failure');
  }

  const run = await runChecker(workspace, repository, workspace.nextCase('typescript-json-import'), {
    scope: 'Changed', base: 'HEAD',
  });
  assertResultsKept(run);
  const typescript = checkOf(run.report, 'typescript');
  assert.equal(typescript.completed, true, describeRun(run));
  assert.equal(typescript.status, 'passed', describeRun(run));
  assert.equal(run.exit, 0, describeRun(run));
  assert.deepEqual(typescript.scripts.map(script => [script.name, script.exit]),
    [['typecheck', 0], ['desktop:typecheck', 0], ['mcp:typecheck', 0]]);
  // The preflight record is the evidence that the compiler's extra inputs were checked first.
  for (const [path, content] of Object.entries(inputs)) {
    const recorded = typescript.preflight.find(input => input.path === path);
    assert.deepEqual(recorded, { path, sha256: sha256(Buffer.from(content)), bytes: Buffer.byteLength(content) },
      `${path} must be recorded with the hash of its bytes`);
  }
});

test('TypeScript leaves absent import candidates to the compiler but stops on other candidate failures', async t => {
  const repository = await TemporaryRepository.create(workspace, 'typescript-import-candidates');
  await repository.addTypeScriptFrontend();
  await enableJsonModules(repository);
  const frontend = '05_Management/frontend';
  const probe = `${frontend}/src/probe.ts`;
  await repository.write(`${frontend}/src/good.ts`, 'export const value = 1;\n');
  await repository.write('05_Management/records/catalog.json', '{ "records": [] }\n');
  await repository.write('.gitignore', 'node_modules/\nlocked/\n');
  repository.commit('base');
  const outside = join(workspace.work, 'typescript-candidates-outside');
  await mkdir(outside, { recursive: true });
  await writeFile(join(outside, 'data.json'), '{ "outside": true }\n');
  await writeFile(join(outside, 'mod.ts'), 'export const outside = 1;\n');

  const compilerStarted = run => commandLines(run.report).some(line => /node_modules[\\/]typescript[\\/]/.test(line));
  const check = async (name, source) => {
    await repository.write(probe, source);
    const run = await runChecker(workspace, repository, workspace.nextCase(name), { scope: 'Changed', base: 'HEAD' });
    assertResultsKept(run);
    t.diagnostic(`${name}: exit=${run.exit}; ${checkOf(run.report, 'typescript').message ?? 'no message'}`);
    return run;
  };

  // Absent modules, including a path below a file (ENOTDIR on POSIX), are the compiler's
  // located diagnostics in a completed check, not an input failure.
  const absent = await check('typescript-absent-candidates', [
    "import missing from '../../records/missing.json';",
    "import belowFile from '../../records/catalog.json/extra';",
    'export const values = [missing, belowFile];',
    '',
  ].join('\n'));
  const located = checkOf(absent.report, 'typescript');
  assert.equal(located.completed, true, describeRun(absent));
  assert.equal(located.status, 'failed', describeRun(absent));
  assert.notEqual(absent.exit, 0, describeRun(absent));
  // TS2307 is the compiler's "Cannot find module" diagnostic.
  assert.deepEqual(diagnosticsAt(absent.report, probe).map(issue => `${issue.line}:${issue.rule}`).sort(),
    ['1:TS2307', '1:TS2307', '1:TS2307', '2:TS2307', '2:TS2307', '2:TS2307'],
    'each typecheck script locates both unresolved imports');

  // Any other failure on a candidate stops the check before the compiler and names the source,
  // the import, the candidate and the cause with a way to fix it.
  const expectInputFailure = (name, run, parts) => {
    const typescript = checkOf(run.report, 'typescript');
    assert.notEqual(run.exit, 0, `${name}: ${describeRun(run)}`);
    assert.equal(typescript.status, 'failed', `${name}: ${typescript.message}`);
    assert.equal(typescript.completed, false, `${name}: an input failure is not a completed check`);
    assert.deepEqual(typescript.diagnostics, [], `${name}: an input failure is not a rule violation`);
    assert.ok(!compilerStarted(run), `${name}: the compiler must not start: ${commandLines(run.report).join(' | ')}`);
    for (const part of [probe, ...parts]) {
      assert.ok(typescript.message.includes(part), `${name}: the message must name ${part}: ${typescript.message}`);
    }
    assert.match(typescript.message, /\b(?:correct|fix|restore|remove|rename|replace)\b/i,
      `${name}: the message must say how to fix the input`);
  };
  const linkOrSkip = async (st, target, name) => {
    try {
      await symlink(target, repository.path(name), 'file');
      return true;
    } catch (error) {
      if (!['EPERM', 'EACCES'].includes(error.code)) throw error;
      st.skip(`file links cannot be created here: ${error.code}`);
      return false;
    }
  };

  await t.test('a candidate that is a directory', async () => {
    await repository.write('05_Management/shared/broken.ts/keep.txt', 'not a module\n');
    const run = await check('typescript-candidate-directory', "import '../../shared/broken';\n");
    expectInputFailure('typescript-candidate-directory', run, ['../../shared/broken', 'broken.ts', 'EISDIR']);
    await repository.remove('05_Management/shared/broken.ts');
  });

  await t.test('an unreadable candidate parent on POSIX', async st => {
    if (process.platform === 'win32' || process.getuid?.() === 0) {
      st.skip('directory permissions do not deny lstat for this user on this platform');
      return;
    }
    const locked = repository.path('05_Management/shared/locked');
    await repository.write('05_Management/shared/locked/mod.ts', 'export const locked = 1;\n');
    await chmod(locked, 0o000);
    try {
      const run = await check('typescript-candidate-permission', "import '../../shared/locked/mod';\n");
      expectInputFailure('typescript-candidate-permission', run, ['../../shared/locked/mod', 'locked', 'EACCES']);
    } finally {
      await chmod(locked, 0o755);
      await repository.remove('05_Management/shared/locked');
    }
  });

  await t.test('a directory index candidate that is a link', async st => {
    await mkdir(repository.path('05_Management/shared/linked'), { recursive: true });
    if (!await linkOrSkip(st, join(outside, 'mod.ts'), '05_Management/shared/linked/index.ts')) return;
    const run = await check('typescript-candidate-link', "import '../../shared/linked';\n");
    expectInputFailure('typescript-candidate-link', run, ['../../shared/linked', 'index.ts']);
    await repository.remove('05_Management/shared/linked');
  });

  await t.test('a JSON import that is a link', async st => {
    if (!await linkOrSkip(st, join(outside, 'data.json'), '05_Management/records/linked.json')) return;
    const run = await check('typescript-json-link', "import '../../records/linked.json';\n");
    expectInputFailure('typescript-json-link', run, ['../../records/linked.json']);
    await repository.remove('05_Management/records/linked.json');
  });

  await t.test('a JSON import outside the repository', async () => {
    const specifier = '../../../../typescript-candidates-outside/data.json';
    const run = await check('typescript-json-outside', `import '${specifier}';\n`);
    expectInputFailure('typescript-json-outside', run, [specifier]);
  });
  await repository.remove(probe);
});

test('every invocation keeps its own results and the default scope is Changed', async () => {
  const repository = await TemporaryRepository.create(workspace, 'evidence');
  repository.commit('base');
  const output = join(workspace.results, workspace.nextCase('shared-output'));
  const first = await runChecker(workspace, repository, workspace.nextCase('evidence-first'), { output });
  const firstBytes = sha256(await readFile(join(first.runDirectory, 'results.json')));
  const second = await runChecker(workspace, repository, workspace.nextCase('evidence-second'), { output });
  assertResultsKept(first);
  assertResultsKept(second);
  assert.notEqual(first.runDirectory, second.runDirectory);
  assert.equal(sha256(await readFile(join(first.runDirectory, 'results.json'))), firstBytes);
  assert.equal(first.report.scope, 'Changed');
  assert.equal(first.report.inputs.base, first.report.inputs.head, 'the default base is HEAD');
});

test('the workflow keeps PR Changed, manual All or Changed, no schedule and narrow artifacts', async () => {
  const workflow = await readFile(join(repositoryRoot, '.github/workflows/code-rules.yml'), 'utf8');
  const steps = workflow.split(/\n(?=\s+- (?:name|uses):)/);
  const stepWith = pattern => steps.find(step => pattern.test(step)) ?? '';
  assert.match(workflow, /^on:\s*\n\s+pull_request:/m);
  assert.match(workflow, /workflow_dispatch:[\s\S]*?scope:[\s\S]*?options:\s*\[\s*Changed,\s*All\s*\]/);
  assert.doesNotMatch(workflow, /^\s*schedule:/m, 'All must not be scheduled');
  // Pull requests always use Changed; only a manual dispatch may choose All.
  assert.match(workflow, /RULES_SCOPE:\s*\$\{\{\s*github\.event_name == 'pull_request' && 'Changed' \|\| inputs\.scope/);
  const checker = stepWith(/check-code-rules\.mjs/);
  assert.match(checker, /if: always\(\)/);
  assert.match(checker, /--scope "\$RULES_SCOPE"/);
  const regressions = stepWith(/node --test 99_Tools\/CodeRules\.Tests\/code-rules\.test\.mjs/);
  assert.match(regressions, /if: always\(\)/);
  assert.match(regressions, /CODE_RULES_PSSA_MANIFEST:/);
  assert.match(regressions, /CODE_RULES_RESULTS:/);
  const upload = stepWith(/actions\/upload-artifact/);
  assert.match(upload, /if: always\(\)/);
  assert.match(upload, /path: \$\{\{ runner\.temp \}\}\/code-rules-results\/\s*$/m);
  const executable = workflow.split('\n').filter(line => !/^\s*#/.test(line)).join('\n');
  assert.doesNotMatch(executable, /sqlfluff/i, 'SQL inspection must stay disconnected while deferred');
  assert.doesNotMatch(executable, /--fix|baseline/i);
});

const workflowPath = join(repositoryRoot, '.github/workflows/code-rules.yml');
// Lines as GitHub checks the workflow out (LF); a Windows checkout may have added CR.
const readWorkflowLines = async () => (await readFile(workflowPath, 'utf8')).replace(/\r\n/g, '\n').split('\n');
const indentOf = line => line.length - line.trimStart().length;
const isYamlContent = line => line.trim() !== '' && !line.trimStart().startsWith('#');
const unsupportedForm = (lines, index, reason) =>
  new Error(`Unsupported workflow form at line ${index + 1} (${reason}): ${lines[index]}`);

// Indices of the YAML content lines nested under lines[parent], up to its next sibling.
function nestedLines(lines, parent) {
  const nested = [];
  for (let index = parent + 1; index < lines.length; index += 1) {
    if (!isYamlContent(lines[index])) continue;
    if (indentOf(lines[index]) <= indentOf(lines[parent])) break;
    nested.push(index);
  }
  return nested;
}

function directChildren(lines, parent) {
  const nested = nestedLines(lines, parent);
  if (nested.length === 0) return [];
  return nested.filter(index => indentOf(lines[index]) === indentOf(lines[nested[0]]));
}

// Contexts GitHub allows in jobs.<job_id>.env ("Context availability" in the contexts reference).
// runner is not one of them: it only exists once the job's steps run.
const jobEnvironmentContexts = ['github', 'needs', 'strategy', 'matrix', 'vars', 'secrets', 'inputs'];

// The `NAME: value` lines of every jobs.<job_id>.env block. Only the one-line plain form this
// workflow uses is read; any other form fails instead of being skipped.
function jobEnvironmentEntries(lines) {
  const jobsLine = lines.indexOf('jobs:');
  if (jobsLine < 0) throw new Error('Unsupported workflow form: no top-level "jobs:" line');
  const jobs = directChildren(lines, jobsLine);
  if (jobs.length === 0) throw unsupportedForm(lines, jobsLine, 'no job');
  const entries = [];
  for (const job of jobs) {
    const jobId = /^\s+([A-Za-z_][\w-]*):$/.exec(lines[job])?.[1];
    if (!jobId) throw unsupportedForm(lines, job, 'expected "<job_id>:"');
    const keys = directChildren(lines, job);
    const notKey = keys.find(index => !/^\s+[\w-]+:(?: |$)/.test(lines[index]));
    if (notKey !== undefined) throw unsupportedForm(lines, notKey, 'expected "key:" under a job');
    const env = keys.find(index => /^\s+env:/.test(lines[index]));
    if (env === undefined) continue;
    if (lines[env].trim() !== 'env:') throw unsupportedForm(lines, env, 'job env must be a block mapping');
    const values = nestedLines(lines, env);
    for (const index of values) {
      const match = /^\s+([A-Za-z_]\w*): (\S.*)$/.exec(lines[index]);
      const sameLevel = indentOf(lines[index]) === indentOf(lines[values[0]]);
      if (!match || !sameLevel || /^['"|>{[&*!]/.test(match[2])) {
        throw unsupportedForm(lines, index, 'expected a one-line plain "NAME: value"');
      }
      entries.push({ line: index + 1, name: `jobs.${jobId}.env.${match[1]}`, value: match[2] });
    }
  }
  return entries;
}

// First names of the property chains in a value's ${{ }} expressions, such as github in
// github.event_name. Quoted strings, function calls and true/false/null are skipped; nothing is evaluated.
function contextsIn(value) {
  if (value.replace(/\$\{\{[\s\S]*?\}\}/g, '').includes('${{')) throw new Error(`Unclosed expression: ${value}`);
  const contexts = new Set();
  for (const [, expression] of value.matchAll(/\$\{\{([\s\S]*?)\}\}/g)) {
    const withoutStrings = expression.replace(/'(?:[^']|'')*'/g, "''");
    for (const [, name, call] of withoutStrings.matchAll(/(?<![\w.-])([A-Za-z_][\w-]*)(\s*\()?/g)) {
      if (call === undefined && !['true', 'false', 'null'].includes(name)) contexts.add(name);
    }
  }
  return [...contexts];
}

test('the workflow job env uses only the contexts GitHub allows in jobs.<job_id>.env', async () => {
  // The first remote run was rejected before the job started: runner.temp in jobs.check.env.
  const refused = jobEnvironmentEntries(await readWorkflowLines()).flatMap(entry => contextsIn(entry.value)
    .filter(context => !jobEnvironmentContexts.includes(context))
    .map(context => `line ${entry.line} ${entry.name}: ${context}`));
  assert.deepEqual(refused, []);
});

// The `run: |` body of the bash step `name`, as YAML's default clipping hands it to the shell.
// Only that body runs here, so a step with any other key (env, if, working-directory) fails.
function bashStepBody(lines, name) {
  const step = lines.findIndex(line => line.trim() === `- name: ${name}`);
  if (step < 0) throw new Error(`Workflow step not found: ${name}`);
  const keyIndent = indentOf(lines[step]) + 2;
  const keys = nestedLines(lines, step).filter(index => indentOf(lines[index]) === keyIndent);
  const keyText = keys.map(index => lines[index].trim());
  if (keyText.length !== 2 || !keyText.includes('shell: bash') || !keyText.includes('run: |')) {
    throw unsupportedForm(lines, step, `expected only "shell: bash" and "run: |", found ${keyText.join(' / ')}`);
  }
  const run = keys[keyText.indexOf('run: |')];
  const body = [];
  for (let index = run + 1; index < lines.length; index += 1) {
    if (lines[index].trim() !== '' && indentOf(lines[index]) <= keyIndent) break;
    body.push(lines[index]);
  }
  while (body.length > 0 && body.at(-1).trim() === '') body.pop();
  const bodyIndent = indentOf(body.find(line => line.trim() !== '') ?? '');
  if (body.length === 0 || body.some(line => line.trim() !== '' && indentOf(line) < bodyIndent)) {
    throw unsupportedForm(lines, run, 'expected an indented literal block');
  }
  return `${body.map(line => line.slice(bodyIndent)).join('\n')}\n`;
}

// GitHub runs `shell: bash` as `bash --noprofile --norc -eo pipefail {0}` (workflow syntax reference).
// On Windows the same bash comes from the WSL distribution these tests already require. `env -i`
// limits the process environment to what the test hands it.
function runBash(scriptPath, environment) {
  const argv = [
    'env', '-i', 'PATH=/usr/local/bin:/usr/bin:/bin',
    ...Object.entries(environment).map(([name, value]) => `${name}=${value}`),
    'bash', '--noprofile', '--norc', '-eo', 'pipefail', scriptPath,
  ];
  const [command, ...args] = process.platform === 'win32'
    ? ['wsl', '-d', toolEnvironment().tools.wslDistribution, '--exec', ...argv]
    : argv;
  const result = spawnSync(command, args, { encoding: 'utf8' });
  return {
    argv: [command, ...args],
    exit: result.status,
    error: result.error?.message ?? null,
    stdout: result.stdout,
    stderr: result.stderr,
  };
}

// The bash spelling of a local directory: unchanged on Linux, its WSL mount path on Windows.
function bashPath(path) {
  if (process.platform !== 'win32') return path;
  const distribution = toolEnvironment().tools.wslDistribution;
  const result = spawnSync('wsl', ['-d', distribution, '--exec', 'wslpath', '-a', '-u', path], { encoding: 'utf8' });
  if (result.status !== 0) throw new Error(`wslpath failed (${result.status}): ${result.stderr}${result.error ?? ''}`);
  return result.stdout.trim();
}

// Approved job-local layout under RUNNER_TEMP: results (the uploaded artifact), analyzer modules
// and npm cache. The job env held these paths before they moved to GITHUB_ENV.
const jobLocalPaths = {
  RULES_OUTPUT: 'code-rules-results',
  RULES_MODULES: 'code-rules-tools/modules',
  NPM_CONFIG_CACHE: 'code-rules-tools/npm',
};

// GITHUB_ENV in the documented one-line form NAME=value. The multiline NAME<<DELIMITER form is not
// used by this workflow and is refused, as is a repeated name.
function readGitHubEnvironment(text) {
  const values = {};
  for (const line of text.split('\n').filter(line => line !== '')) {
    const match = /^([A-Za-z_]\w*)=(.*)$/.exec(line);
    if (!match) throw new Error(`Unsupported GITHUB_ENV line: ${line}`);
    if (Object.hasOwn(values, match[1])) throw new Error(`Repeated GITHUB_ENV name: ${match[1]}`);
    values[match[1]] = match[2];
  }
  return values;
}

test('the workflow initializer hands three job-local paths to a later bash process through GITHUB_ENV', async () => {
  const directory = join(workspace.work, 'workflow-initializer');
  // GitHub-hosted runner paths have no space; one here shows that every use is quoted.
  const runnerTemp = join(directory, 'runner temp');
  await mkdir(runnerTemp, { recursive: true });
  await writeFile(join(runnerTemp, 'set_env'), '');
  const initializerBody = bashStepBody(await readWorkflowLines(), 'Initialize job-local paths');
  await writeFile(join(directory, 'initializer.sh'), initializerBody);
  // Local stand-in for the runner hand-off (workflow commands reference: GITHUB_ENV is read after
  // the step and its values reach every later step). A separate bash gets only what the real step
  // wrote and leaves a marker where each value points; the later workflow steps do not run here.
  const laterStep = [
    'set -u',
    `for name in ${Object.keys(jobLocalPaths).join(' ')}; do`,
    '  mkdir -p "${!name}"',
    '  printf \'%s\\n\' "$name" > "${!name}/marker.txt"',
    'done',
    '',
  ].join('\n');
  await writeFile(join(directory, 'later-step.sh'), laterStep);

  const bashDirectory = bashPath(directory);
  const bashRunnerTemp = `${bashDirectory}/runner temp`;
  const evidencePath = join(workspace.results, `${workspace.nextCase('workflow-initializer')}.json`);
  const keep = evidence => writeFile(evidencePath, `${JSON.stringify(evidence, null, 2)}\n`);
  const initializer = runBash(`${bashDirectory}/initializer.sh`, {
    RUNNER_TEMP: bashRunnerTemp,
    GITHUB_ENV: `${bashRunnerTemp}/set_env`,
  });
  const githubEnvironment = await readFile(join(runnerTemp, 'set_env'), 'utf8');
  await keep({ initializerBody, initializer, githubEnvironment });
  assert.equal(initializer.exit, 0, `${initializer.stderr}${initializer.error ?? ''}`);

  const handedOver = readGitHubEnvironment(githubEnvironment);
  const expected = Object.fromEntries(Object.entries(jobLocalPaths)
    .map(([name, relative]) => [name, `${bashRunnerTemp}/${relative}`]));
  assert.deepEqual(handedOver, expected);

  const later = runBash(`${bashDirectory}/later-step.sh`, handedOver);
  await keep({ initializerBody, initializer, githubEnvironment, laterStep, later });
  assert.equal(later.exit, 0, `${later.stderr}${later.error ?? ''}`);
  for (const [name, relative] of Object.entries(jobLocalPaths)) {
    const marker = join(runnerTemp, ...relative.split('/'), 'marker.txt');
    assert.ok(await exists(marker), `${name} must lead the later process to runner temp/${relative}`);
    assert.equal(await readFile(marker, 'utf8'), `${name}\n`);
  }
});

test('the SQL deferral evidence link resolves to the recorded decision', async () => {
  const config = JSON.parse(await readFile(join(repositoryRoot, '99_Tools/CodeRules/config.json'), 'utf8'));
  assert.equal(config.sqlGate.enabled, false);
  const [file, anchor] = config.sqlGate.evidence.split('#');
  const goal = await readFile(join(repositoryRoot, file), 'utf8');
  assert.ok(goal.includes(`<a id="${anchor}"></a>`), `${config.sqlGate.evidence} must exist`);
  assert.ok(!config.excludedRoots.some(root => /sql/i.test(root)), 'SQL must not be hidden by exclusion');
});

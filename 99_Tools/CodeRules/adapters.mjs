import { readFile, writeFile } from 'node:fs/promises';
import { dirname, isAbsolute, join, relative, sep } from 'node:path';
import { describe, hash, safeExternalPath, safePath } from './inputs.mjs';
import { preflightTypeScript } from './typescript-inputs.mjs';

function structured(result) {
  if (result.record.error || result.record.timedOut || result.record.exit === null) {
    throw new Error(`Process failed: ${result.record.error ?? 'timeout or unavailable exit'}`);
  }
  return JSON.parse(result.stdout.toString('utf8'));
}

function checkedFiles(result, targets, executionResult) {
  if (!Array.isArray(result.files) || !Array.isArray(result.errors)) throw new Error('Invalid adapter result schema.');
  if (result.errors.length) throw new Error(result.errors.join('; '));
  if (result.files.length !== targets.length) throw new Error('Adapter did not report every input.');
  const errors = [];
  for (const target of targets) {
    const matches = result.files.filter(file => file.path === target.path && file.sha256 === target.sha256);
    if (matches.length !== 1) throw new Error(`Adapter input identity mismatch: ${target.path}`);
    if (matches[0].error) errors.push(`${target.path}: ${matches[0].error}`);
    if (!Array.isArray(matches[0].diagnostics)) throw new Error('Adapter diagnostics absent.');
  }
  const diagnostics = result.files.flatMap(file => file.diagnostics);
  if (executionResult.record.exit !== 0 && !diagnostics.length && !errors.length) {
    errors.push('Adapter exited unsuccessfully without diagnostics.');
  }
  return {
    status: diagnostics.length || errors.length ? 'failed' : 'passed', diagnostics,
    message: errors.join('; '),
    completed: errors.length === 0,
    version: result.version, runtime: result.runtime, files: result.files,
  };
}

function sqlTreeText(value) {
  if (typeof value === 'string') return value;
  if (Array.isArray(value)) return value.map(sqlTreeText).join('');
  if (value && typeof value === 'object') return Object.values(value).map(sqlTreeText).join('');
  return '';
}

function sqlHasUnparsable(value) {
  if (Array.isArray(value)) return value.some(sqlHasUnparsable);
  return value && typeof value === 'object' &&
    (Object.hasOwn(value, 'unparsable') || Object.values(value).some(sqlHasUnparsable));
}

async function powershell(context, targets) {
  const { root, options, config, execution, directory } = context;
  if (!options.pssaManifest) throw new Error('PSScriptAnalyzer manifest is required for applicable targets.');
  const moduleManifest = await safeExternalPath(options.pssaManifest);
  const distribution = JSON.parse(await readFile(await safePath(root, '99_Tools/CodeRules/pssa-files.json'), 'utf8'));
  if (distribution.version !== config.powerShellVersion) {
    throw new Error('Analyzer distribution/version configuration mismatch.');
  }
  const toolFiles = [];
  // Pin package distribution bytes before loading the analyzer. PowerShellGet generates
  // PSGetModuleInfo.xml with installation location, timestamps and download counts;
  // the analyzer does not load it, so that metadata is absent from the pinned file list.
  for (const item of distribution.files) {
    const file = await safePath(dirname(moduleManifest), item.path);
    const bytes = await readFile(file);
    if (hash(bytes) !== item.sha256) throw new Error(`Analyzer distribution hash mismatch: ${item.path}`);
    toolFiles.push({ ...item, absolutePath: file });
  }
  const manifest = targets.map(target => ({ ...target, absolutePath: join(root, target.path) }));
  const inputManifest = join(directory, 'powershell-inputs.json');
  await writeFile(inputManifest, JSON.stringify(manifest), { flag: 'wx' });
  const result = await execution.run(options.pwsh, [
    '-NoLogo', '-NoProfile', '-NonInteractive', '-File',
    join(root, '99_Tools/CodeRules/check-powershell.ps1'),
    '-InputManifest', inputManifest, '-ModuleManifest', moduleManifest,
    '-SettingsPath', join(root, '99_Tools/CodeRules/pssa-settings.psd1'),
    '-RequiredVersion', config.powerShellVersion,
  ]);
  return { ...checkedFiles(structured(result), targets, result), toolFiles };
}

async function python(context, targets) {
  const { root, options, execution } = context;
  const payload = [];
  for (const target of targets) {
    const bytes = await readFile(await safePath(root, target.path));
    if (hash(bytes) !== target.sha256) throw new Error(`Input changed: ${target.path}`);
    payload.push({ ...target, base64: bytes.toString('base64') });
  }
  const helper = await execution.linuxPath(join(root, '99_Tools/CodeRules/check-python.py'), options.wslDistribution);
  const result = await execution.python(options.python, ['-I', '-B', helper], options.wslDistribution, {
    input: JSON.stringify(payload),
  });
  return checkedFiles(structured(result), targets, result);
}

async function sql(context, targets) {
  const { config, root, options, execution } = context;
  if (!config.sqlGate.enabled) return {
    status: 'deferred', message: config.sqlGate.reason, evidence: config.sqlGate.evidence,
    diagnostics: [], targets, inspected: false, completed: false,
  };
  if (!options.sqlPython) throw new Error('Exact SQLFluff venv Python path is required.');
  const dependencies = await readFile(await safePath(root, '99_Tools/CodeRules/sql-dependencies.json'));
  const helper = await execution.linuxPath(
    join(root, '99_Tools/CodeRules/check-sql-tools.py'), options.wslDistribution,
  );
  const dependencyCheck = await execution.python(options.sqlPython, ['-I', '-B', helper], options.wslDistribution, {
    input: dependencies,
  });
  const dependencyResult = structured(dependencyCheck);
  if (!dependencyCheck.ok || dependencyResult.errors?.length ||
      dependencyResult.dependencies?.length !== JSON.parse(dependencies).dependencies.length) {
    throw new Error(`SQLFluff dependency mismatch: ${JSON.stringify(dependencyResult.errors)}`);
  }
  const version = await execution.python(
    options.sqlPython, ['-I', '-m', 'sqlfluff', '--version'], options.wslDistribution,
  );
  const expectedVersion = new RegExp(`\\b${config.sqlFluffVersion.replaceAll('.', '\\.')}\\s*$`);
  if (!version.ok || !expectedVersion.test(version.stdout.toString('utf8').trim())) {
    throw new Error('SQLFluff is missing or has a version mismatch.');
  }
  const settings = await execution.linuxPath(join(root, '99_Tools/CodeRules/sqlfluff.cfg'), options.wslDistribution);
  const diagnostics = [];
  for (const target of targets) {
    const file = await safePath(root, target.path);
    const bytes = await readFile(file);
    // Inline configuration can change even explicit settings; refuse it rather than remove comments.
    if (/^\s*--\s*sqlfluff\s*:/im.test(bytes.toString('utf8'))) {
      throw new Error(`SQL inline configuration is not accepted: ${target.path}`);
    }
    const converted = await execution.linuxPath(file, options.wslDistribution);
    const parsedResult = await execution.python(options.sqlPython, [
      '-I', '-m', 'sqlfluff', 'parse', '--ignore-local-config', '--config', settings,
      '--dialect', 'tsql', '--templater', 'raw', '--disable-noqa', '--format', 'json', converted,
    ], options.wslDistribution);
    const tree = structured(parsedResult);
    if (!Array.isArray(tree) || tree.length !== 1 || tree[0].filepath !== converted || !tree[0].segments?.file) {
      throw new Error(`SQLFluff skipped or did not parse exactly one input: ${target.path}`);
    }
    if (sqlTreeText(tree[0].segments) !== bytes.toString('utf8').replaceAll('\r\n', '\n').replace(/^\uFEFF/, '')) {
      throw new Error(`SQL parse tree did not cover the complete input: ${target.path}`);
    }
    const result = await execution.python(options.sqlPython, [
      '-I', '-m', 'sqlfluff', 'lint', '--ignore-local-config', '--config', settings,
      '--dialect', 'tsql', '--templater', 'raw', '--rules', 'LT01,LT02',
      '--disable-noqa', '--disregard-sqlfluffignores', '--format', 'json', converted,
    ], options.wslDistribution);
    const parsed = structured(result);
    if (!Array.isArray(parsed) || parsed.length !== 1 || parsed[0].filepath !== converted ||
        !Array.isArray(parsed[0].violations)) {
      throw new Error(`SQLFluff did not report exactly one input: ${target.path}`);
    }
    const issues = parsed[0].violations;
    if (result.record.exit !== 0 && issues.length === 0) throw new Error(`SQL process failure: ${target.path}`);
    diagnostics.push(...issues.map(issue => ({
      path: target.path, line: issue.start_line_no, column: issue.start_line_pos,
      rule: issue.code, message: issue.description,
    })));
    if ((!parsedResult.ok || sqlHasUnparsable(tree[0].segments)) &&
        !issues.some(issue => ['PRS', 'LXR', 'TMP'].includes(issue.code))) {
      throw new Error(`SQL parse command failed without parse diagnostics: ${target.path}`);
    }
  }
  return {
    status: diagnostics.length ? 'failed' : 'passed', version: config.sqlFluffVersion, diagnostics,
    completed: true,
    runtime: dependencyResult.runtime, dependencies: dependencyResult.dependencies,
  };
}

async function typescript(context) {
  const { root, config, execution } = context;
  const frontend = await safePath(root, config.managementRoot);
  const packagePath = `${config.managementRoot}/package.json`;
  const lockPath = `${config.managementRoot}/package-lock.json`;
  const packageInfo = JSON.parse(await readFile(await safePath(root, packagePath), 'utf8'));
  const lock = JSON.parse(await readFile(await safePath(root, lockPath), 'utf8'));
  const installedPath = `${config.managementRoot}/node_modules/typescript/package.json`;
  const installed = JSON.parse(await readFile(await safePath(root, installedPath), 'utf8'));
  const expected = lock.packages?.['node_modules/typescript']?.version;
  if (!expected || installed.version !== expected || expected !== packageInfo.devDependencies.typescript) {
    throw new Error('Installed TypeScript must match both exact package and lockfile version.');
  }
  const compilerName = typeof installed.bin === 'string' ? installed.bin : installed.bin?.tsc;
  if (!compilerName) throw new Error('Installed TypeScript has no tsc entry point.');
  const compiler = await safePath(
    root, `${config.managementRoot}/node_modules/typescript/${compilerName.replace(/^\.\//, '')}`,
  );
  const dependencies = await describe(root, [packagePath, lockPath, installedPath]);
  const preflight = await preflightTypeScript(root, config.managementRoot, [
    'tsconfig.json', 'tsconfig.electron.json', 'tsconfig.mcp.json',
  ]);
  const diagnostics = [];
  const scripts = [];
  const sources = new Set();
  for (const name of config.typecheckScripts) {
    const command = packageInfo.scripts?.[name];
    // The existing scripts are pure tsc argv; never execute a changed package script as a shell.
    if (typeof command !== 'string' || !/^tsc(?: --noEmit| --project tsconfig(?:\.[a-z]+)?\.json)+$/.test(command)) {
      throw new Error(`Unsupported typecheck command contract: ${name}`);
    }
    const args = command.split(' ').slice(1);
    if (!args.includes('--noEmit')) throw new Error(`Typecheck may emit output: ${name}`);
    const projectIndex = args.indexOf('--project');
    const configName = projectIndex < 0 ? 'tsconfig.json' : args[projectIndex + 1];
    dependencies.push(...await describe(root, [`${config.managementRoot}/${configName}`]));
    const listed = await execution.run(process.execPath, [compiler, ...args, '--listFilesOnly'], { cwd: frontend });
    if (!listed.ok) throw new Error(`TypeScript source enumeration failed: ${name}`);
    for (const file of listed.stdout.toString('utf8').split(/\r?\n/).filter(Boolean)) {
      const local = relative(root, file);
      if (isAbsolute(local) || local === '..' || local.startsWith(`..${sep}`)) {
        throw new Error('TypeScript source escaped repository.');
      }
      const normalized = local.split(sep).join('/');
      if (!normalized.split('/').includes('node_modules')) sources.add(normalized);
    }
    const beforeSources = await describe(root, [...sources].sort());
    const checked = await execution.run(process.execPath, [compiler, ...args, '--pretty', 'false'], {
      cwd: frontend, timeoutMs: 300000,
    });
    for (const source of beforeSources) {
      if (hash(await readFile(await safePath(root, source.path))) !== source.sha256) {
        throw new Error(`TypeScript source changed during typecheck: ${source.path}`);
      }
    }
    scripts.push({ name, packageScript: command, commandId: checked.record.id, exit: checked.record.exit });
    const scriptDiagnostics = [];
    for (const line of `${checked.stdout.toString('utf8')}\n${checked.stderr.toString('utf8')}`.split(/\r?\n/)) {
      const issue = /^(.*?)\((\d+),(\d+)\): (?:error|warning) (TS\d+): (.*)$/.exec(line);
      if (issue) scriptDiagnostics.push({
        path: `${config.managementRoot}/${issue[1].replaceAll('\\', '/')}`,
        line: Number(issue[2]), column: Number(issue[3]), rule: issue[4], message: issue[5],
      });
    }
    if (!checked.ok && !scriptDiagnostics.length) {
      throw new Error(`Typecheck process failed without located diagnostics: ${name}`);
    }
    diagnostics.push(...scriptDiagnostics);
  }
  const files = await describe(root, [...sources].sort());
  return {
    status: diagnostics.length || scripts.some(script => script.exit !== 0) ? 'failed' : 'passed',
    completed: true,
    version: installed.version, diagnostics, scripts, dependencies, files, preflight,
    message: 'Existing three typecheck scripts executed as their pure tsc argv; no product build or package scripts.',
  };
}

export async function checkLanguages(context) {
  const checks = [];
  for (const [language, adapter] of Object.entries({ powershell, sql, typescript, python })) {
    const targets = context.inputs.targets.filter(target => target.language === language);
    if (targets.length === 0) {
      checks.push({
        language, status: 'not-applicable', completed: true, diagnostics: [],
        message: 'No selected targets; tool not invoked.',
      });
      continue;
    }
    try {
      checks.push({ language, targetCount: targets.length, ...await adapter(context, targets) });
    } catch (error) {
      checks.push({
        language, targetCount: targets.length, status: 'failed', completed: false,
        diagnostics: [], message: error.message,
      });
    }
  }
  return checks;
}

import { mkdir, readFile, realpath } from 'node:fs/promises';
import { resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { parseArgs } from 'node:util';
import { checkLanguages } from './adapters.mjs';
import { Execution } from './execution.mjs';
import { collect, describe, hash, safeExternalPath, safePath } from './inputs.mjs';
import { saveResults } from './results.mjs';

const defaultRoot = fileURLToPath(new URL('../../', import.meta.url));
const { values } = parseArgs({ options: {
  scope: { type: 'string', default: 'Changed' },
  base: { type: 'string', default: 'HEAD' },
  root: { type: 'string', default: defaultRoot },
  output: { type: 'string' },
  'pssa-manifest': { type: 'string' },
  pwsh: { type: 'string', default: 'pwsh' },
  python: { type: 'string', default: 'python3' },
  'sql-python': { type: 'string' },
  'wsl-distribution': { type: 'string' },
} });
const root = await realpath(resolve(values.root));
const output = await safeExternalPath(values.output ?? resolve(root, '.backups/code-rules'), true);
// Every invocation owns a new directory, including failures. No prior evidence is overwritten.
await mkdir(output, { recursive: true });
const directory = resolve(output, `run-${new Date().toISOString().replaceAll(':', '-')}-${process.pid}`);
await mkdir(directory);
const execution = new Execution(directory, root);
const report = {
  schemaVersion: 1, startedAt: new Date().toISOString(), scope: values.scope,
  environment: { platform: process.platform, node: process.version },
  inputs: null, settings: [], checks: [], errors: [],
};
try {
  if (!['Changed', 'All'].includes(values.scope)) throw new Error('scope must be Changed or All.');
  const configPath = await safePath(root, '99_Tools/CodeRules/config.json');
  const config = JSON.parse(await readFile(configPath, 'utf8'));
  if (config.schemaVersion !== 1) throw new Error('Unsupported checker configuration schema.');
  report.settings = await describe(root, [
    '99_Tools/CodeRules/config.json', '99_Tools/CodeRules/pssa-settings.psd1',
    '99_Tools/CodeRules/sqlfluff.cfg', '99_Tools/CodeRules/check-code-rules.mjs',
    '99_Tools/CodeRules/inputs.mjs', '99_Tools/CodeRules/execution.mjs',
    '99_Tools/CodeRules/adapters.mjs', '99_Tools/CodeRules/results.mjs',
    '99_Tools/CodeRules/check-powershell.ps1', '99_Tools/CodeRules/check-python.py',
    '99_Tools/CodeRules/pssa-files.json',
    '99_Tools/CodeRules/check-sql-tools.py', '99_Tools/CodeRules/sql-dependencies.json',
    '99_Tools/CodeRules/sql-requirements.txt',
    '99_Tools/CodeRules/typescript-inputs.mjs',
  ]);
  report.inputs = await collect(root, values.scope, values.base, config, execution);
  report.checks = await checkLanguages({ root, config, execution, directory, inputs: report.inputs, options: {
    pssaManifest: values['pssa-manifest'] ? resolve(values['pssa-manifest']) : null,
    pwsh: values.pwsh, python: values.python, sqlPython: values['sql-python'],
    wslDistribution: values['wsl-distribution'],
  } });
  const actual = [...report.inputs.targets, ...report.settings,
    ...report.checks.flatMap(check => [
      ...(check.files ?? []), ...(check.preflight ?? []),
      ...(check.dependencies ?? []).filter(item => item.path),
    ])];
  for (const item of actual) {
    if (hash(await readFile(await safePath(root, item.path))) !== item.sha256) {
      throw new Error(`Input changed during checking: ${item.path}`);
    }
  }
} catch (error) {
  report.errors.push(error.message);
}
report.commands = execution.commands;
report.finishedAt = new Date().toISOString();
const exit = await saveResults(directory, report);
console.log(`Code rules ${report.summary.status}; results: ${directory}`);
process.exitCode = exit;

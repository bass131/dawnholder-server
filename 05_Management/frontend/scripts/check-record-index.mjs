import { register } from 'node:module';
import { fileURLToPath } from 'node:url';

register('./ts-source-loader.mjs', import.meta.url);
const { checkRecordIndex } = await import('../electron/record-index-check.ts');
const repositoryRoot = fileURLToPath(new URL('../../../', import.meta.url));
const result = await checkRecordIndex({ repositoryRoot });
const oneLine = text => text.replace(/[\u0000-\u001f\u007f]+/g, ' ');
for (const item of result.diagnostics) {
  process.stdout.write(`${item.severity} ${item.code} ${oneLine(item.location)} — ${oneLine(item.cause)} — 고치는 방법: ${oneLine(item.fix)}\n`);
}
const groups = result.groups.map(item => `${item.group}=${item.ran ? 'ran' : 'failed'}`).join(' ');
const errors = result.diagnostics.filter(item => item.severity === 'error').length;
const warnings = result.diagnostics.filter(item => item.severity === 'warning').length;
process.stdout.write(`records:check ${groups} errors=${errors} warnings=${warnings}\n`);
process.exitCode = result.exitCode;

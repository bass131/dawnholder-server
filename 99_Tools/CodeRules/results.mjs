import { writeFile } from 'node:fs/promises';
import { join } from 'node:path';

export async function saveResults(directory, report) {
  const diagnostics = report.checks.flatMap(check => check.diagnostics ?? []);
  const failures = report.errors.length + report.checks.filter(check => check.status === 'failed').length;
  const deferred = report.checks.filter(check => check.status === 'deferred');
  const deferredTargets = deferred.reduce((count, check) => count + check.targetCount, 0);
  const activeChecksPassed = failures === 0 && diagnostics.length === 0;
  report.summary = {
    targets: report.inputs?.targets.length ?? 0,
    violations: diagnostics.length,
    failures,
    deferredTargets,
    activeChecksPassed,
    allTargetsChecked: report.inputs !== null && report.errors.length === 0 &&
      report.checks.every(check => check.completed === true),
    passed: deferredTargets > 0 ? null : activeChecksPassed,
    status: !activeChecksPassed ? 'FAIL' : deferredTargets > 0 ? 'ACTIVE CHECKS PASS WITH SQL DEFERRED' : 'PASS',
  };
  const lines = [
    `Code rules: ${report.summary.status}`,
    `Scope: ${report.scope}; base: ${report.inputs?.base ?? 'unavailable'}`,
    `HEAD: ${report.inputs?.head ?? 'unavailable'}`,
    `Targets: ${report.summary.targets}; violations: ${diagnostics.length}; failed checks: ${failures}`,
    `SQL deferred targets: ${deferredTargets}`,
    `Dirty: ${report.inputs?.dirty ?? 'unknown'}; input: current worktree bytes`,
    '',
  ];
  for (const error of report.errors) lines.push(`ERROR: ${error}`);
  for (const check of report.checks) {
    lines.push(`${check.language}: ${check.status}; ${check.message ?? ''}`);
    if (check.status === 'deferred') {
      lines.push(`  Evidence: ${check.evidence}; inspected: false; count: ${check.targetCount}`);
      for (const target of check.targets) lines.push(`  DEFERRED: ${target.path} ${target.sha256}`);
    }
    for (const issue of check.diagnostics ?? []) {
      lines.push(`  ${issue.path}:${issue.line ?? '?'}:${issue.column ?? '?'} ${issue.rule}: ${issue.message}`);
    }
  }
  lines.push('', 'Targets and SHA256:');
  for (const target of report.inputs?.targets ?? []) lines.push(`  ${target.path} ${target.sha256}`);
  lines.push('', 'Deleted:');
  for (const path of report.inputs?.deleted ?? []) lines.push(`  ${path}`);
  lines.push('', 'Excluded:');
  for (const item of report.inputs?.excluded ?? []) lines.push(`  ${item.path}: ${item.reason}`);
  lines.push('', 'This is syntax/style/type checking, not design, gameplay, DB or independent verification.');
  await writeFile(join(directory, 'results.json'), `${JSON.stringify(report, null, 2)}\n`, { flag: 'wx' });
  await writeFile(join(directory, 'results.txt'), `${lines.join('\n')}\n`, { flag: 'wx' });
  return activeChecksPassed ? 0 : 1;
}

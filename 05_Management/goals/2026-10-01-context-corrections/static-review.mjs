// Read-only review of the documentation/catalog correction. No app or test suite runs.
import fs from 'node:fs';
import path from 'node:path';
import { execFileSync } from 'node:child_process';
import { createHash } from 'node:crypto';
import assert from 'node:assert/strict';
import { readCatalog, catalogReferenceErrors, MAX_CATALOG_BYTES } from '../../frontend/electron/catalog-contract.ts';

const git = (...args) => execFileSync('git', args, { maxBuffer: 8 * 1024 * 1024 });
const sha = bytes => createHash('sha256').update(bytes).digest('hex').toUpperCase();
const base = 'ef5f1023fe3353ee9eec5da04422232a33855233';
const checkpoint = '8c6fbd57f2f54825a937a82a6ad949755b43411c';
const transplant = 'dd4e7eabf955bec7f47778cb22a3aefd9cf7ef5b';
const catalogPath = '05_Management/records/catalog.json';
const oldBytes = git('show', `${base}:${catalogPath}`);
const newBytes = fs.readFileSync(catalogPath);
const old = JSON.parse(oldBytes);
const current = JSON.parse(newBytes);
assert(readCatalog(current));
assert(newBytes.length <= MAX_CATALOG_BYTES);
assert.deepEqual(catalogReferenceErrors(current), []);
for (const key of ['schemaVersion', 'asOf', 'sourceCommit', 'systems', 'records']) assert.deepEqual(current[key], old[key], key);
assert.deepEqual(current.sources.map(s => s.id), old.sources.map(s => s.id));
assert.equal(current.sources.length, 35);
assert.equal(current.systems.length, 18);
assert.equal(current.records.length, 18);
assert.equal(current.revision, '2026-10-01-r2');
assert(current.scopeNote.startsWith(old.scopeNote));
const changed = [];
const gitSources = [];
for (let i = 0; i < current.sources.length; i++) {
  const source = current.sources[i];
  const prior = old.sources[i];
  if (JSON.stringify(source) !== JSON.stringify(prior)) {
    for (const key of ['id', 'title', 'section']) assert.equal(source[key], prior[key]);
    assert.equal(prior.kind, 'local');
    assert.equal(prior.availability, 'local-only');
    assert.equal(source.kind, 'git');
    assert.equal(source.availability, 'versioned');
    assert(source.locator.startsWith('05_Management/'));
    assert.equal(source.revision, '715bff5bfc62ab5c1f1cdf0aabdf7358492bb1d3');
    const actual = sha(git('show', `${source.revision}:${source.locator}`));
    assert.equal(`SHA256:${actual}`, prior.revision);
    assert(source.note.includes(prior.revision));
    changed.push({ id: source.id, locator: source.locator, sha256: actual });
  }
  if (source.kind === 'git') {
    git('cat-file', '-e', `${source.revision}:${source.locator}`);
    gitSources.push(source.id);
  }
}
assert.deepEqual(changed.map(s => s.id), ['management-decisions', 'management-requirements', 'management-foundation', 'management-desktop', 'management-console', 'management-launcher']);
const reverseErrors = [];
for (const system of current.systems) for (const id of system.recordIds) if (!current.records.find(r => r.id === id).systemIds.includes(system.id)) reverseErrors.push(`${system.id}/${id}`);
for (const record of current.records) for (const id of record.systemIds) if (!current.systems.find(s => s.id === id).recordIds.includes(record.id)) reverseErrors.push(`${record.id}/${id}`);
assert.deepEqual(reverseErrors, []);
assert.equal(git('rev-parse', 'feat/management-system-records').toString().trim(), checkpoint);
assert.equal(git('rev-parse', `${transplant}^`).toString().trim(), base);
const originalPatch = git('show', '--format=', checkpoint);
const transplantedPatch = git('show', '--format=', transplant);
assert.deepEqual(transplantedPatch, originalPatch);

const changedPaths = git('diff', '--name-only', base).toString().trim().split(/\r?\n/).filter(Boolean);
assert(changedPaths.every(p => p.startsWith('05_Management/') && (p.endsWith('.md') || p === catalogPath)));
const reviewDocs = [...changedPaths.filter(p => p.endsWith('.md')), '05_Management/goals/2026-10-01-context-corrections/goal.md'];
const links = text => [...text.matchAll(/\[[^\]\n]+\]\(([^)\s]+)\)/g)].map(m => m[1]);
const headings = text => {
  const ids = new Set([...text.matchAll(/<a\s+[^>]*id=["']([^"']+)["']/g)].map(m => m[1]));
  const seen = new Map();
  for (const match of text.matchAll(/^#{1,6}\s+(.+)$/gm)) {
    const slug = match[1].trim().toLowerCase().replace(/[^\p{L}\p{N}\p{M}\s_-]/gu, '').replace(/ /g, '-');
    const n = seen.get(slug) || 0; seen.set(slug, n + 1); ids.add(n ? `${slug}-${n}` : slug);
  }
  return ids;
};
const broken = [];
const basePaths = new Set(git('ls-tree', '-r', '--name-only', base).toString().split(/\r?\n/));
let checkedLinks = 0;
for (const file of reviewDocs) {
  const text = fs.readFileSync(file, 'utf8');
  const oldText = basePaths.has(file) ? git('show', `${base}:${file}`).toString() : '';
  const priorLinks = new Set(links(oldText));
  for (const link of links(text)) {
    if (/^[a-z][a-z0-9+.-]*:/i.test(link)) continue;
    checkedLinks++;
    const [location, fragment] = link.split('#');
    const target = location ? path.resolve(path.dirname(file), decodeURIComponent(location)) : path.resolve(file);
    const exists = fs.existsSync(target);
    const anchorOk = !fragment || (exists && headings(fs.readFileSync(target, 'utf8')).has(decodeURIComponent(fragment)));
    if (!exists || !anchorOk) broken.push({ file, link, issue: !exists ? 'missing file' : 'missing anchor', newLink: !priorLinks.has(link) });
  }
}
console.log(JSON.stringify({ snapshotUtc: new Date().toISOString(), base, head: git('rev-parse', 'HEAD').toString().trim(), checkpointPreserved: true, checkpointPatchIdentical: true, contractAccepted: true, byteLength: newBytes.length, catalogSha256: sha(newBytes), originalCatalogSha256: sha(oldBytes), changedSources: changed, gitSourcePathsPresent: gitSources.length, kindCounts: current.sources.reduce((a, s) => (a[s.kind] = (a[s.kind] || 0) + 1, a), {}), unchangedHistoryAndOtherSources: true, referenceAndReverseErrors: 0, changedPaths, checkedLinks, broken, reviewedDocuments: reviewDocs.map(file => ({ file, sha256: sha(fs.readFileSync(file)) })) }, null, 2));
if (broken.some(link => link.newLink)) process.exitCode = 1;

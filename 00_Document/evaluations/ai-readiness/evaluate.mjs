// Node 24 + Windows tar + WSL Ubuntu Python 3.10+. Writes only to the supplied temp directory.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { execFileSync } from 'node:child_process';

const [repoArg, revision, outputArg] = process.argv.slice(2);
if (!repoArg || !revision || !outputArg) throw new Error('Usage: node evaluate.mjs <repo> <commit|working> <new-temp-directory>');
const repo = path.resolve(repoArg), output = path.resolve(outputArg);
if (output === repo || output.startsWith(repo + path.sep) || fs.existsSync(output)) throw new Error('Output must be a new directory outside the repository.');
const git = (...args) => execFileSync('git', ['-C', repo, ...args], { maxBuffer: 32 * 1024 * 1024 });
const hash = data => crypto.createHash('sha256').update(data).digest('hex');
const excluded = '00_Document/evaluations/ai-readiness/';
const sha = '900d3956a4ad85117d66d34f902faa5c84392b02';
const sourceUrl = `https://raw.githubusercontent.com/jha0313/skills_repo/${sha}/ai-readiness-cartography/scripts/score.py`;
const expected = '14e9736909bf01266a0d409114dfd9e63768016a94296f07177104bf74cec0b8';
const response = await fetch(sourceUrl);
if (!response.ok) throw new Error(`Source download failed: ${response.status}`);
const source = Buffer.from(await response.arrayBuffer());
if (hash(source) !== expected) throw new Error('Pinned scorer hash mismatch.');
fs.mkdirSync(output, { recursive: true });
const snapshot = path.join(output, 'snapshot');
fs.mkdirSync(snapshot);
fs.writeFileSync(path.join(output, 'score.py'), source);
let files;
if (revision === 'working') {
  files = git('ls-files', '-z', '--cached', '--others', '--exclude-standard').toString().split('\0').filter(Boolean);
  files = [...new Set(files)].filter(p => !p.startsWith(excluded) && fs.existsSync(path.join(repo, p)));
  const untracked = new Set(git('ls-files', '-z', '--others', '--exclude-standard').toString().split('\0'));
  if (files.some(p => untracked.has(p) && !/^(00_Document\/|01_Phases\/goals\/|\.agents\/|99_Tools\/|\.github\/|(?:02_Server|03_Client|04_ClientNet|98_Shared)\/README\.md$|AGENTS\.md$)/.test(p))) throw new Error('Review untracked files before snapshotting; only task documentation and validation paths are allowed.');
  for (const p of files) {
    const src = path.join(repo, p), dest = path.join(snapshot, p);
    if (!fs.lstatSync(src).isFile()) throw new Error(`Unsupported file type: ${p}`);
    fs.mkdirSync(path.dirname(dest), { recursive: true });
    fs.copyFileSync(src, dest);
  }
} else {
  const commit = git('rev-parse', '--verify', `${revision}^{commit}`).toString().trim();
  const archive = path.join(output, 'snapshot.tar');
  git('archive', '--format=tar', '--output', archive, commit, '.', `:(exclude)${excluded}**`);
  execFileSync('tar', ['-xf', archive, '-C', snapshot]);
  files = git('ls-tree', '-rz', '--name-only', commit).toString().split('\0').filter(p => p && !p.startsWith(excluded));
}
const manifest = files.sort().map(p => ({ path: p, sha256: hash(fs.readFileSync(path.join(snapshot, p))) }));
fs.writeFileSync(path.join(output, 'manifest.json'), JSON.stringify({ revision, files: manifest }, null, 2));
const toWsl = p => '/mnt/' + p[0].toLowerCase() + p.slice(2).replaceAll('\\', '/');
const result = path.join(output, 'raw.json');
execFileSync('wsl', ['-d', 'Ubuntu', '--', 'python3', toWsl(path.join(output, 'score.py')), toWsl(snapshot), '--json', toWsl(result), '--quiet'], { stdio: 'inherit' });
const raw = JSON.parse(fs.readFileSync(result, 'utf8'));
fs.writeFileSync(path.join(output, 'provenance.json'), JSON.stringify({ sourceUrl, sourceCommit: sha, sourceSha256: expected, revision, files: manifest.length, rawTotal: raw.total, snapshotMtimeIsNotFreshnessEvidence: true }, null, 2));
console.log(JSON.stringify({ output, files: manifest.length, rawTotal: raw.total }));

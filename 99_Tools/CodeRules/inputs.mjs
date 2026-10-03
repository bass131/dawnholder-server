import { createHash } from 'node:crypto';
import { lstat, readFile, realpath } from 'node:fs/promises';
import { isAbsolute, join, parse, relative, resolve, sep } from 'node:path';

export const hash = bytes => createHash('sha256').update(bytes).digest('hex');

export async function safeExternalPath(path, allowMissing = false) {
  const absolute = resolve(path);
  const anchor = parse(absolute).root;
  let current = anchor;
  for (const part of relative(anchor, absolute).split(sep)) {
    current = join(current, part);
    try {
      const stat = await lstat(current);
      if (stat.isSymbolicLink()) throw new Error(`Links are not accepted at execution/output boundaries: ${current}`);
    } catch (error) {
      if (!(allowMissing && error.code === 'ENOENT')) throw error;
    }
  }
  return absolute;
}

export async function safePath(root, name) {
  if (typeof name !== 'string' || !name || name.includes('\0') || name.includes('\\') || isAbsolute(name)) {
    throw new Error(`Invalid repository path: ${name}`);
  }
  const parts = name.split('/');
  if (parts.some(part => !part || part === '.' || part === '..')) throw new Error(`Invalid repository path: ${name}`);
  let current = root;
  for (const part of parts) {
    current = join(current, part);
    const stat = await lstat(current);
    if (stat.isSymbolicLink()) throw new Error(`Links are not accepted as checker inputs: ${name}`);
  }
  const actual = await realpath(current);
  const local = relative(root, actual);
  if (isAbsolute(local) || local === '..' || local.startsWith(`..${sep}`)) {
    throw new Error(`Path escapes repository: ${name}`);
  }
  return actual;
}

function nulNames(bytes) {
  const decoded = new TextDecoder('utf-8', { fatal: true }).decode(bytes);
  return decoded.split('\0').filter(Boolean);
}

function changes(bytes, source) {
  const fields = nulNames(bytes);
  const result = [];
  for (let index = 0; index < fields.length;) {
    const status = fields[index++];
    if (!/^[ACDMRTUXB][0-9]*$/.test(status)) throw new Error(`Unexpected Git status: ${status}`);
    const oldPath = fields[index++];
    const path = /^[RC]/.test(status) ? fields[index++] : oldPath;
    if (!path) throw new Error('Incomplete Git name-status output.');
    result.push({ status, path, oldPath: /^[RC]/.test(status) ? oldPath : null, source });
  }
  return result;
}

export function classify(path, config) {
  const excludedRoot = [...config.excludedRoots, ...config.fixtureRoots].find(root => path.startsWith(root));
  const excludedSegment = path.split('/').find(part => config.excludedSegments.includes(part));
  if (excludedRoot || excludedSegment) return { excluded: excludedRoot ?? excludedSegment };
  if (/\.(ps1|psm1|psd1)$/i.test(path)) return { language: 'powershell' };
  if (/\.sql$/i.test(path)) return { language: 'sql' };
  if (path.startsWith(`${config.managementRoot}/`) &&
      (/\.(ts|tsx|cts)$/.test(path) || /\/(package(?:-lock)?\.json|tsconfig[^/]*\.json)$/.test(path))) {
    return { language: 'typescript' };
  }
  if (path.endsWith('.py') && config.pythonRoots.some(root => path.startsWith(root))) return { language: 'python' };
  return { excluded: 'Outside selected language/path scope' };
}

export async function collect(root, scope, base, config, execution) {
  const git = async args => {
    const result = await execution.run('git', args, { cwd: root });
    if (!result.ok) throw new Error(`Git failed (${args.join(' ')}): ${result.stderr}`);
    return result.stdout;
  };
  const top = (await git(['rev-parse', '--show-toplevel'])).toString('utf8').trim();
  if (await realpath(top) !== root) throw new Error('Checker must address the Git repository root.');
  const head = (await git(['rev-parse', '--verify', 'HEAD^{commit}'])).toString('utf8').trim();
  if (!base || base.startsWith('-')) throw new Error('Git base must be a nonempty revision, not an option.');
  const resolvedBase = await git(['rev-parse', '--verify', '--end-of-options', `${base}^{commit}`]);
  const actualBase = resolvedBase.toString('utf8').trim();
  const dirty = await git(['status', '--porcelain=v1', '-z', '--untracked-files=all']);
  const committed = changes(
    await git(['diff', '--name-status', '-z', '--find-renames', actualBase, head, '--']), 'base-to-head',
  );
  const staged = changes(
    await git(['diff', '--cached', '--name-status', '-z', '--find-renames', 'HEAD', '--']), 'staged',
  );
  const unstaged = changes(await git(['diff', '--name-status', '-z', '--find-renames', '--']), 'unstaged');
  const untracked = nulNames(await git(['ls-files', '--others', '--exclude-standard', '-z']));
  const changeList = [
    ...committed, ...staged, ...unstaged,
    ...untracked.map(path => ({ path, status: 'A', source: 'untracked' })),
  ];
  const candidates = scope === 'All'
    ? nulNames(await git(['ls-files', '--cached', '--others', '--exclude-standard', '-z']))
    : changeList.map(item => item.path);
  const targets = [];
  const excluded = [];
  const deleted = [...new Set(changeList.filter(item => item.status === 'D').map(item => item.path))].sort();
  for (const path of [...new Set(candidates)].sort()) {
    const category = classify(path, config);
    if (category.excluded) {
      excluded.push({ path, reason: category.excluded });
      continue;
    }
    try {
      const file = await safePath(root, path);
      const bytes = await readFile(file);
      targets.push({ path, language: category.language, sha256: hash(bytes), bytes: bytes.length });
    } catch (error) {
      if (error.code === 'ENOENT' && deleted.includes(path)) continue;
      throw error;
    }
  }
  return {
    root, scope, base: actualBase, head, dirty: dirty.length > 0,
    dirtyPorcelainNul: dirty.toString('utf8'),
    inputState: 'Current worktree bytes, including staged/unstaged/untracked; not a clean commit snapshot',
    changes: changeList, targets, excluded, deleted,
  };
}

export async function describe(root, names) {
  const result = [];
  for (const path of names) {
    const bytes = await readFile(await safePath(root, path));
    result.push({ path, sha256: hash(bytes), bytes: bytes.length });
  }
  return result;
}

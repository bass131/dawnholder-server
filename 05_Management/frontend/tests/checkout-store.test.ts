// @vitest-environment node
// Requirement: goal 「만들 것」 2 (show the read checkout's branch and HEAD from Git metadata files,
// unknown when they cannot be read; the app runs no Git command) and index-v2-design.md
// 「checkout 정보」 steps 1-4 and reasons. Fixtures are owned TEMP repositories. The real
// checkout's expected branch and HEAD come from the git CLI, never from the product.
import { spawnSync } from 'node:child_process';
import { symlinkSync } from 'node:fs';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { createCheckoutStore } from '../electron/checkout-store';
import { createOwnedTempRepository, type OwnedTempRepository } from './record-sources/owned-temp-repository';

const HEAD_A = '1111111111111111111111111111111111111111';
const HEAD_B = '2222222222222222222222222222222222222222';
const HEAD_SHA256 = 'a'.repeat(64);

let repository: OwnedTempRepository;
beforeEach(() => { repository = createOwnedTempRepository(); });
afterEach(() => repository.remove());

const read = () => createCheckoutStore({ repositoryRoot: repository.root }).read();
const slashes = (path: string) => path.replaceAll('\\', '/');

// A plain clone: <root>/.git is the gitdir and the commondir.
function directoryCheckout(head: string, refs: Record<string, string> = {}) {
  repository.write('.git/HEAD', head);
  for (const [name, value] of Object.entries(refs)) repository.write(`.git/${name}`, value);
}

// A linked worktree like this checkout: <root>/.git is a pointer file, the private gitdir lives
// in the main repository's .git/worktrees/<name> and links back to <root>/.git.
function worktreeCheckout(options: { head: string; backlink?: string; pointer?: string; commondir?: string | null }) {
  const commonDir = join(repository.base, 'main-repo', '.git');
  const gitDir = join(commonDir, 'worktrees', 'wt');
  repository.write('.git', options.pointer ?? `gitdir: ${slashes(gitDir)}\n`);
  const inGitDir = (name: string, value: string) => {
    const relative = slashes(join('..', 'main-repo', '.git', 'worktrees', 'wt', name));
    return repository.write(relative, value);
  };
  inGitDir('HEAD', options.head);
  inGitDir('gitdir', options.backlink ?? `${slashes(join(repository.root, '.git'))}\n`);
  if (options.commondir !== null) inGitDir('commondir', options.commondir ?? '../..\n');
  return {
    commonDir,
    gitDir,
    writeCommon: (name: string, value: string) => repository.write(slashes(join('..', 'main-repo', '.git', name)), value),
    writeGitDir: inGitDir,
  };
}

describe('checkout from a .git directory', () => {
  it('reads the branch and its loose ref', async () => {
    directoryCheckout('ref: refs/heads/main\n', { 'refs/heads/main': `${HEAD_A}\n` });
    expect(await read()).toEqual({ state: 'known', branch: 'main', head: HEAD_A });
  });

  it('keeps a branch name with slashes', async () => {
    directoryCheckout('ref: refs/heads/feat/record-index\n', { 'refs/heads/feat/record-index': `${HEAD_A}\n` });
    expect(await read()).toEqual({ state: 'known', branch: 'feat/record-index', head: HEAD_A });
  });

  it('reads a detached HEAD of 40 or 64 hex digits as no branch', async () => {
    directoryCheckout(`${HEAD_A}\n`);
    expect(await read()).toEqual({ state: 'known', branch: null, head: HEAD_A });
    directoryCheckout(`${HEAD_SHA256}\n`);
    expect(await read()).toEqual({ state: 'known', branch: null, head: HEAD_SHA256 });
  });

  it('falls back to packed-refs, ignoring # comment and ^ peeled lines', async () => {
    directoryCheckout('ref: refs/heads/main\n', {
      'packed-refs': [
        '# pack-refs with: peeled fully-peeled sorted ',
        `# ${HEAD_B} refs/heads/main`,
        `${HEAD_A} refs/heads/main`,
        `^${HEAD_B}`,
        `${HEAD_B} refs/heads/other`,
        '',
      ].join('\n'),
    });
    expect(await read()).toEqual({ state: 'known', branch: 'main', head: HEAD_A });
  });

  it('prefers the loose ref over packed-refs', async () => {
    directoryCheckout('ref: refs/heads/main\n', { 'refs/heads/main': `${HEAD_A}\n`, 'packed-refs': `${HEAD_B} refs/heads/main\n` });
    expect(await read()).toEqual({ state: 'known', branch: 'main', head: HEAD_A });
  });

  it('reports ref-missing when neither a loose ref nor packed-refs has the branch', async () => {
    directoryCheckout('ref: refs/heads/main\n', { 'packed-refs': `${HEAD_B} refs/heads/other\n` });
    expect(await read()).toEqual({ state: 'unknown', reason: 'ref-missing' });
  });

  it('reports head-invalid for a HEAD that is neither a branch ref nor a full hash', async () => {
    for (const head of ['garbage\n', '\n', 'refs/heads/main\n', `${HEAD_A.slice(1)}\n`, `${HEAD_A}0\n`]) {
      directoryCheckout(head, { 'refs/heads/main': `${HEAD_A}\n` });
      expect(await read(), JSON.stringify(head)).toEqual({ state: 'unknown', reason: 'head-invalid' });
    }
  });

  it('rejects ref names outside refs/heads or with parent, backslash, control, dot-leading or .lock parts', async () => {
    const names = [
      'refs/tags/v1',
      'refs/heads/../../config',
      'refs/heads/a\\b',
      'refs/heads/a\u0001b',
      'refs/heads/.hidden',
      'refs/heads/feat/.hidden',
      'refs/heads/main.lock',
    ];
    for (const name of names) {
      directoryCheckout(`ref: ${name}\n`, { 'refs/heads/main': `${HEAD_A}\n` });
      expect(await read(), JSON.stringify(name)).toEqual({ state: 'unknown', reason: 'ref-invalid' });
    }
  });

  it('reports no-git without a .git entry', async () => {
    expect(await read()).toEqual({ state: 'unknown', reason: 'no-git' });
  });

  it('refuses files over 4 KiB and packed-refs over 1 MiB as too-large', async () => {
    directoryCheckout(`ref: refs/heads/main\n${' '.repeat(4096)}`, { 'refs/heads/main': `${HEAD_A}\n` });
    expect(await read(), 'HEAD').toEqual({ state: 'unknown', reason: 'too-large' });
    directoryCheckout('ref: refs/heads/main\n', { 'refs/heads/main': `${HEAD_A}\n${' '.repeat(4096)}` });
    expect(await read(), 'loose ref').toEqual({ state: 'unknown', reason: 'too-large' });
    repository.remove();
    repository = createOwnedTempRepository();
    directoryCheckout('ref: refs/heads/main\n', { 'packed-refs': `${HEAD_A} refs/heads/main\n${'#'.repeat(1024 * 1024)}\n` });
    expect(await read(), 'packed-refs').toEqual({ state: 'unknown', reason: 'too-large' });
  });
});

describe('checkout from a worktree pointer', () => {
  it('follows gitdir, checks the backlink and reads the branch ref from commondir', async () => {
    const worktree = worktreeCheckout({ head: 'ref: refs/heads/feat/x\n' });
    worktree.writeCommon('refs/heads/feat/x', `${HEAD_A}\n`);
    expect(await read()).toEqual({ state: 'known', branch: 'feat/x', head: HEAD_A });
  });

  it('looks in gitdir before commondir and commondir packed-refs last', async () => {
    const worktree = worktreeCheckout({ head: 'ref: refs/heads/feat/x\n' });
    worktree.writeGitDir('refs/heads/feat/x', `${HEAD_A}\n`);
    worktree.writeCommon('refs/heads/feat/x', `${HEAD_B}\n`);
    expect(await read(), 'gitdir first').toEqual({ state: 'known', branch: 'feat/x', head: HEAD_A });

    repository.remove();
    repository = createOwnedTempRepository();
    const packed = worktreeCheckout({ head: 'ref: refs/heads/feat/x\n' });
    packed.writeCommon('packed-refs', `# pack-refs with: peeled\n${HEAD_B} refs/heads/feat/x\n`);
    expect(await read(), 'commondir packed-refs').toEqual({ state: 'known', branch: 'feat/x', head: HEAD_B });
  });

  it('uses gitdir as commondir when the worktree has no commondir file', async () => {
    const worktree = worktreeCheckout({ head: 'ref: refs/heads/feat/x\n', commondir: null });
    worktree.writeGitDir('refs/heads/feat/x', `${HEAD_A}\n`);
    worktree.writeCommon('refs/heads/feat/x', `${HEAD_B}\n`);
    expect(await read()).toEqual({ state: 'known', branch: 'feat/x', head: HEAD_A });
  });

  it('reads a detached worktree HEAD', async () => {
    worktreeCheckout({ head: `${HEAD_B}\n` });
    expect(await read()).toEqual({ state: 'known', branch: null, head: HEAD_B });
  });

  it('reports backlink-mismatch when the gitdir does not point back to this root', async () => {
    worktreeCheckout({ head: `${HEAD_A}\n`, backlink: `${slashes(join(repository.base, 'other', '.git'))}\n` });
    expect(await read()).toEqual({ state: 'unknown', reason: 'backlink-mismatch' });
  });

  it('reports pointer-invalid for a pointer that is not one absolute gitdir line', async () => {
    const gitDir = slashes(join(repository.base, 'main-repo', '.git', 'worktrees', 'wt'));
    for (const pointer of ['garbage\n', 'gitdir:\n', 'gitdir: relative/worktrees/wt\n', `gitdir: ${gitDir}\ngitdir: ${gitDir}\n`]) {
      worktreeCheckout({ head: `${HEAD_A}\n`, pointer });
      expect(await read(), JSON.stringify(pointer)).toEqual({ state: 'unknown', reason: 'pointer-invalid' });
    }
  });

  it('refuses a pointer file over 4 KiB as too-large', async () => {
    const gitDir = slashes(join(repository.base, 'main-repo', '.git', 'worktrees', 'wt'));
    worktreeCheckout({ head: `${HEAD_A}\n`, pointer: `gitdir: ${gitDir}\n${' '.repeat(4096)}` });
    expect(await read()).toEqual({ state: 'unknown', reason: 'too-large' });
  });
});

describe('checkout through a link', () => {
  it('reports link when .git is a junction to a real git directory', async () => {
    const target = join(repository.outside, 'real.git');
    repository.write('../outside/real.git/HEAD', `${HEAD_A}\n`);
    symlinkSync(target, repository.path('.git'), 'junction');
    expect(await read()).toEqual({ state: 'unknown', reason: 'link' });
  });
});

describe('the checkout this test runs in', () => {
  it('matches the branch and HEAD that the git CLI reports for the same root', async () => {
    const root = fileURLToPath(new URL('../../../', import.meta.url));
    const git = (args: string[]) => spawnSync('git', args, { cwd: root, encoding: 'utf8' });
    const head = git(['rev-parse', 'HEAD']);
    const symbolic = git(['symbolic-ref', '--quiet', '--short', 'HEAD']);
    expect(head.status).toBe(0);
    const expected = {
      state: 'known',
      branch: symbolic.status === 0 ? symbolic.stdout.trim() : null,
      head: head.stdout.trim(),
    };
    expect(await createCheckoutStore({ repositoryRoot: root }).read()).toEqual(expected);
  });
});

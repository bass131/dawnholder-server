import { lstat, open } from 'node:fs/promises';
import { join, resolve } from 'node:path';
import { isCommitHash, MAX_GIT_METADATA_BYTES, MAX_PACKED_REFS_BYTES, metadataLine, packedRef, parseCheckoutHead, parseGitPointer } from './checkout-contract.js';
import type { CheckoutInfo, CheckoutUnknownReason } from './checkout-contract.js';

type MetadataRead = { kind: 'text'; text: string } | { kind: 'missing' } | { kind: 'error'; reason: CheckoutUnknownReason };

function isMissing(error: unknown): boolean {
  return typeof error === 'object' && error !== null && 'code' in error && error.code === 'ENOENT';
}

async function readMetadata(path: string, limit = MAX_GIT_METADATA_BYTES): Promise<MetadataRead> {
  try {
    const entry = await lstat(path);
    if (entry.isSymbolicLink()) return { kind: 'error', reason: 'link' };
    if (!entry.isFile()) return { kind: 'error', reason: 'load' };
    const handle = await open(path, 'r');
    try {
      const before = await handle.stat();
      if (!before.isFile() || entry.dev !== before.dev || entry.ino !== before.ino) return { kind: 'error', reason: 'load' };
      if (before.size > limit) return { kind: 'error', reason: 'too-large' };
      const buffer = Buffer.alloc(limit + 1);
      let length = 0;
      while (length < buffer.length) {
        const result = await handle.read(buffer, length, buffer.length - length, length);
        if (result.bytesRead === 0) break;
        length += result.bytesRead;
      }
      if (length > limit) return { kind: 'error', reason: 'too-large' };
      const after = await handle.stat();
      if (before.size !== after.size || before.mtimeMs !== after.mtimeMs || before.ctimeMs !== after.ctimeMs || length !== before.size) {
        return { kind: 'error', reason: 'load' };
      }
      return { kind: 'text', text: new TextDecoder('utf-8', { fatal: true }).decode(buffer.subarray(0, length)) };
    } finally {
      await handle.close();
    }
  } catch (error) {
    return isMissing(error) ? { kind: 'missing' } : { kind: 'error', reason: 'load' };
  }
}

const unknown = (reason: CheckoutUnknownReason): CheckoutInfo => ({ state: 'unknown', reason });

export function createCheckoutStore({ repositoryRoot }: { repositoryRoot: string }) {
  async function read(): Promise<CheckoutInfo> {
    const pointerPath = resolve(repositoryRoot, '.git');
    let foundGit = false;
    try {
      const entry = await lstat(pointerPath);
      foundGit = true;
      if (entry.isSymbolicLink()) return unknown('link');
      let gitDir = pointerPath;
      let commonDir = gitDir;
      if (!entry.isDirectory()) {
        if (!entry.isFile()) return unknown('pointer-invalid');
        const pointer = await readMetadata(pointerPath);
        if (pointer.kind !== 'text') return unknown(pointer.kind === 'error' ? pointer.reason : 'no-git');
        const parsed = parseGitPointer(pointer.text);
        if (!parsed) return unknown('pointer-invalid');
        gitDir = resolve(parsed);
        const directory = await lstat(gitDir);
        if (directory.isSymbolicLink()) return unknown('link');
        if (!directory.isDirectory()) return unknown('pointer-invalid');
        const backlink = await readMetadata(join(gitDir, 'gitdir'));
        if (backlink.kind === 'error') return unknown(backlink.reason);
        const target = backlink.kind === 'text' ? metadataLine(backlink.text) : null;
        // The pointer is allowed to leave the checkout only when Git's inverse
        // pointer identifies this exact checkout, not another worktree.
        if (!target || resolve(target) !== pointerPath) return unknown('backlink-mismatch');
        const common = await readMetadata(join(gitDir, 'commondir'));
        if (common.kind === 'error') return unknown(common.reason);
        if (common.kind === 'text') {
          const path = metadataLine(common.text);
          if (!path) return unknown('pointer-invalid');
          commonDir = resolve(gitDir, path);
        } else {
          commonDir = gitDir;
        }
      }
      const headFile = await readMetadata(join(gitDir, 'HEAD'));
      if (headFile.kind !== 'text') return unknown(headFile.kind === 'error' ? headFile.reason : 'head-invalid');
      const head = parseCheckoutHead(headFile.text);
      if (head.kind === 'invalid') return unknown(head.reason);
      if (head.kind === 'detached') return { state: 'known', branch: null, head: head.head };
      for (const directory of new Set([gitDir, commonDir])) {
        const ref = await readMetadata(join(directory, head.ref));
        if (ref.kind === 'error') return unknown(ref.reason);
        if (ref.kind === 'text') {
          const hash = metadataLine(ref.text);
          if (!hash || !isCommitHash(hash)) return unknown('ref-invalid');
          return { state: 'known', branch: head.branch, head: hash };
        }
      }
      const packed = await readMetadata(join(commonDir, 'packed-refs'), MAX_PACKED_REFS_BYTES);
      if (packed.kind === 'error') return unknown(packed.reason);
      const hash = packed.kind === 'text' ? packedRef(packed.text, head.ref) : null;
      return hash ? { state: 'known', branch: head.branch, head: hash } : unknown('ref-missing');
    } catch (error) {
      return unknown(!foundGit && isMissing(error) ? 'no-git' : 'load');
    }
  }
  return { read };
}

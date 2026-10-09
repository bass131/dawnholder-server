import { isAbsolute } from 'node:path';

export type CheckoutUnknownReason =
  | 'no-git' | 'link' | 'pointer-invalid' | 'backlink-mismatch' | 'head-invalid'
  | 'ref-invalid' | 'ref-missing' | 'too-large' | 'load';
export type CheckoutInfo =
  | { state: 'known'; branch: string | null; head: string }
  | { state: 'unknown'; reason: CheckoutUnknownReason };
export type CheckoutResult =
  | { ok: true; checkout: CheckoutInfo }
  | { ok: false; code: 'denied'; message: string };

export const MAX_GIT_METADATA_BYTES = 4096;
export const MAX_PACKED_REFS_BYTES = 1024 * 1024;

export function metadataLine(text: string): string | null {
  const line = text.replace(/\r?\n$/, '');
  return line.length > 0 && !/[\u0000-\u001f\u007f]/.test(line) ? line : null;
}

export function parseGitPointer(text: string): string | null {
  const line = metadataLine(text);
  if (!line?.startsWith('gitdir: ')) return null;
  const path = line.slice('gitdir: '.length);
  return isAbsolute(path) ? path : null;
}

export function isCommitHash(value: string): boolean {
  return /^(?:[a-fA-F0-9]{40}|[a-fA-F0-9]{64})$/.test(value);
}

export function isBranchRef(ref: string): boolean {
  return ref.startsWith('refs/heads/') && ref.length > 'refs/heads/'.length
    && !ref.includes('..') && !/[\\\u0000-\u0020\u007f~^:?*\[\]]/.test(ref)
    && !ref.includes('@{') && !ref.endsWith('.')
    && ref.split('/').every(part => part.length > 0 && !part.startsWith('.') && !part.endsWith('.lock'));
}

export function parseCheckoutHead(text: string):
  | { kind: 'detached'; head: string }
  | { kind: 'branch'; ref: string; branch: string }
  | { kind: 'invalid'; reason: 'head-invalid' | 'ref-invalid' } {
  const line = metadataLine(text);
  if (line !== null && isCommitHash(line)) return { kind: 'detached', head: line };
  // A control character in a symbolic reference is a ref error rather than a
  // generic HEAD error; it must never become a filesystem path.
  const rawLine = text.replace(/\r?\n$/, '');
  if (!rawLine.startsWith('ref: ')) return { kind: 'invalid', reason: 'head-invalid' };
  const ref = rawLine.slice('ref: '.length);
  if (!isBranchRef(ref)) return { kind: 'invalid', reason: 'ref-invalid' };
  return { kind: 'branch', ref, branch: ref.slice('refs/heads/'.length) };
}

export function packedRef(text: string, ref: string): string | null {
  for (const line of text.split(/\r?\n/)) {
    if (line.startsWith('#') || line.startsWith('^')) continue;
    const [hash, name] = line.split(' ');
    if (name === ref && hash && isCommitHash(hash)) return hash;
  }
  return null;
}

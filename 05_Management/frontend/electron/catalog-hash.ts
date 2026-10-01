import { createHash } from 'node:crypto';

// Hash the UTF-8 decoded text, preserving the existing UI save/conflict version.
export function catalogHash(text: string): string {
  return createHash('sha256').update(text, 'utf8').digest('hex');
}

// An owned OS TEMP directory holding a test repository root and a sibling "outside" folder.
// Link targets used by the tests stay inside this owned directory, so removal never reaches
// anything the test did not create. Removal checks the exact prefix before deleting.
import { mkdirSync, mkdtempSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, join, resolve } from 'node:path';

export const OWNED_TEMP_PREFIX = 'dawnholder-record-sources-';

export interface OwnedTempRepository {
  base: string;
  root: string;
  outside: string;
  path(relative: string): string;
  write(relative: string, content: string | Uint8Array): string;
  mkdir(relative: string): string;
  remove(): void;
}

export function createOwnedTempRepository(): OwnedTempRepository {
  const base = mkdtempSync(join(tmpdir(), OWNED_TEMP_PREFIX));
  const root = join(base, 'repo');
  const outside = join(base, 'outside');
  mkdirSync(root);
  mkdirSync(outside);
  const path = (relative: string) => join(root, ...relative.split('/'));
  return {
    base,
    root,
    outside,
    path,
    write(relative, content) {
      const target = path(relative);
      mkdirSync(dirname(target), { recursive: true });
      writeFileSync(target, content);
      return target;
    },
    mkdir(relative) {
      const target = path(relative);
      mkdirSync(target, { recursive: true });
      return target;
    },
    remove() {
      const owned = resolve(base).startsWith(join(resolve(tmpdir()), OWNED_TEMP_PREFIX));
      if (!owned) throw new Error(`refusing to remove a directory this test does not own: ${base}`);
      rmSync(base, { recursive: true, force: true });
    },
  };
}

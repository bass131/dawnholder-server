// @vitest-environment node
// Requirement: index-v2-design.md 「원문 구간 읽기 경계」 step 8: when the opened handle's fstat
// differs from the step-7 lstat (dev·ino), the result is changed (only a file replaced between
// steps 7 and 8 reaches this, Astra msg_28ce06a18c04 Q3).
// Premise: step 8 follows system-guide-store.ts's bounded read, which opens with node:fs/promises
// open. This file wraps that open so the target is replaced by another file right before the real
// open runs. The product needs no test hook; if it opens through another API, this premise breaks
// and the test fails with ok:true (report: 「Sol이 알아야 할 시험 전제」).
import { renameSync } from 'node:fs';
import { resolve } from 'node:path';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { createSourceSectionStore } from '../electron/source-section-store';
import { gitSource } from './record-sources/catalog-v2-fixture';
import { createOwnedTempRepository, type OwnedTempRepository } from './record-sources/owned-temp-repository';

const replaceOnOpen = vi.hoisted(() => ({ target: '', replacement: '', replaced: 0 }));

vi.mock('node:fs/promises', async original => {
  const actual = await original<typeof import('node:fs/promises')>();
  const open = (async (path: Parameters<typeof actual.open>[0], ...rest: unknown[]) => {
    const requested = resolve(String(path)).toLowerCase();
    if (replaceOnOpen.target && requested === resolve(replaceOnOpen.target).toLowerCase()) {
      renameSync(replaceOnOpen.replacement, replaceOnOpen.target);
      replaceOnOpen.target = '';
      replaceOnOpen.replaced += 1;
    }
    return (actual.open as (...args: unknown[]) => ReturnType<typeof actual.open>)(path, ...rest);
  }) as typeof actual.open;
  return { ...actual, open, default: { ...actual, open } };
});

let repository: OwnedTempRepository;
beforeEach(() => {
  repository = createOwnedTempRepository();
  replaceOnOpen.target = '';
  replaceOnOpen.replaced = 0;
});
afterEach(() => repository.remove());

describe('a file replaced between the path checks and the read', () => {
  it('is reported as changed with reason null, not as the replacement text', async () => {
    const target = repository.write('docs/page.md', '# 처음\n처음 본문\n');
    replaceOnOpen.replacement = repository.write('docs/replacement.tmp', '# 처음\n바뀐 본문이 더 깁니다\n');
    replaceOnOpen.target = target;
    const result = await createSourceSectionStore({ repositoryRoot: repository.root }).read(gitSource('page', 'docs/page.md', '처음'));
    const replacedBeforeRead = replaceOnOpen.replaced;
    expect(replacedBeforeRead, 'the product opened the file through node:fs/promises open').toBe(1);
    expect(result.ok ? { ok: true } : { ok: false, code: result.code, reason: result.reason }).toEqual({ ok: false, code: 'changed', reason: null });
  });
});

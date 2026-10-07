// @vitest-environment node
// Requirement: backlog-menu-design.md 「색인 검사」: an unexpected exception from the backlog store stays the backlog
// group's inability to run (exit 2), never a diagnostic, and the other groups keep their own state.
// The real store catches its file system errors, so no BACKLOG.md makes it throw; the store module is replaced by a
// double whose read() rejects (the tests/backlog-ipc-preload.test.ts approach for the same module). The index and
// goal groups run for real on an owned TEMP repository.
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { checkRecordIndex } from '../electron/record-index-check';
import { catalogText, gitSource, recordFixture } from './record-sources/catalog-v2-fixture';
import { createOwnedTempRepository, type OwnedTempRepository } from './record-sources/owned-temp-repository';

const store = vi.hoisted(() => ({ read: vi.fn(), createBacklogStore: vi.fn() }));
vi.mock('../electron/backlog-store.js', () => ({ createBacklogStore: store.createBacklogStore }));

const INDEXED_GOAL = '01_Phases/goals/2026-10-01-indexed/goal.md';

let repository: OwnedTempRepository;
beforeEach(() => {
  repository = createOwnedTempRepository();
  repository.write(INDEXED_GOAL, '# 목표\n\n## 결과와 열린 사항\n결과\n');
  repository.mkdir('05_Management/goals');
  repository.write('05_Management/records/catalog.json', catalogText({
    schemaVersion: 2,
    sources: [gitSource('indexed-goal', INDEXED_GOAL, '결과와 열린 사항')],
    systems: [],
    records: [recordFixture({ id: 'indexed', sourceIds: ['indexed-goal'] })],
  }));
  store.read.mockReset().mockRejectedValue(new Error('unexpected store failure (test double)'));
  store.createBacklogStore.mockReset().mockReturnValue({ read: store.read });
});
afterEach(() => repository.remove());

describe('an unexpected exception from the backlog store', () => {
  it('marks only the backlog group not run with exit code 2 and adds no diagnostic', async () => {
    const result = await checkRecordIndex({ repositoryRoot: repository.root });
    const storeWasAsked = store.read.mock.calls.length;
    expect(storeWasAsked, 'the check asked the store double').toBe(1);
    expect(store.createBacklogStore.mock.calls).toEqual([[{ repositoryRoot: repository.root }]]);
    expect(result.groups).toEqual([{ group: 'index', ran: true }, { group: 'goals', ran: true }, { group: 'backlog', ran: false }]);
    expect(result.diagnostics).toEqual([]);
    expect(result.exitCode).toBe(2);
  });
});

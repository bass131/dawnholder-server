// @vitest-environment node
// Requirement: screen-design.md 「모듈 배치」 (electron/server-operations-contract.ts is the pure
// module for IPC input checks, backend answer checks, the error message table and the WSL path),
// 「시작과 붙기」 3 (C:\… becomes /mnt/<lower-case drive>/…), 「IPC 계약」 (logs and commit inputs),
// 「HTTP 대화」 (answers checked from unknown, the 19 backend error codes and a general message for
// unknown codes) and backend-design.md 「상태 응답」·「실행본 목록 응답」·「로그 조회」 shapes.
// Expected values are literal pairs; no product calculation is repeated here.
//
// The module does not exist before the implementation step, so every test imports it on its own and
// fails alone with the missing module. Outside contract for the implementer (pure functions):
// toWslPath(path) → string | null, parseLogQuery(input) → { minutes, contains, limit } | null,
// parseCommitInput(input) → string | null, parseStatusResponse(body) · parseReleasesResponse(body) ·
// parseLogsResponse(body) → the checked answer | null, serverOperationMessage(code) → Korean text.
import { describe, expect, it } from 'vitest';
import { LOGS, RELEASES, STATUS_RUNNING, STATUS_STOPPED } from './server-operations-fixtures';

const load = () => import('../electron/server-operations-contract.js');

type Sample = Record<string, unknown>;

// A deep copy of a sample with one change, so each rejected answer differs from a valid one by one field.
function changed<T extends Sample>(sample: T, change: (copy: T) => void): T {
  const copy = structuredClone(sample);
  change(copy);
  return copy;
}

const asRecord = (value: unknown) => value as Record<string, unknown>;
const HANGUL = /[가-힣]/;

describe('C1 Windows checkout path to WSL path', () => {
  it('C1 maps a drive path to /mnt/<lower-case drive>/… with forward slashes and no trailing separator', async () => {
    const { toWslPath } = await load();
    const pairs: Array<[string, string]> = [
      ['C:\\Users\\bass1\\orca\\workspaces\\DawnHolder_Project\\management-active', '/mnt/c/Users/bass1/orca/workspaces/DawnHolder_Project/management-active'],
      ['C:\\Users\\bass1\\orca\\workspaces\\DawnHolder_Project\\management-active\\', '/mnt/c/Users/bass1/orca/workspaces/DawnHolder_Project/management-active'],
      ['d:\\Data\\Dawn Holder\\checkout', '/mnt/d/Data/Dawn Holder/checkout'],
      ['E:\\a', '/mnt/e/a'],
    ];
    expect(pairs.map(([windowsPath]) => toWslPath(windowsPath))).toEqual(pairs.map(([, wslPath]) => wslPath));
  });

  it('C1 rejects UNC, relative and drive-less paths', async () => {
    const { toWslPath } = await load();
    const rejected = [
      '\\\\server\\share\\checkout',
      '\\\\?\\C:\\checkout',
      '\\\\wsl.localhost\\Ubuntu\\home\\tester',
      'Users\\bass1\\checkout',
      '.\\checkout',
      '..\\checkout',
      '\\Users\\bass1\\checkout',
      'C:Users\\bass1',
      '/mnt/c/Users/bass1',
      '',
    ];
    expect(rejected.map(path => [path, toWslPath(path)])).toEqual(rejected.map(path => [path, null]));
  });
});

describe('C2 logs input', () => {
  const base = { minutes: 10, contains: ['error'], limit: 1000 };

  it('C2 accepts minutes 1~1440, 0~5 words of 1~64 characters and limit 1~5000', async () => {
    const { parseLogQuery } = await load();
    const accepted = [
      { minutes: 10, contains: [], limit: 1000 },
      { minutes: 1, contains: ['a'], limit: 1 },
      { minutes: 1440, contains: ['a'.repeat(64), 'warn', 'error', 'timeout', '오류'], limit: 5000 },
      { minutes: 30, contains: ['error', 'warn'], limit: 500 },
    ];
    expect(accepted.map(input => parseLogQuery(input))).toEqual(accepted);
  });

  it('C2 rejects values outside the ranges and inputs of another shape', async () => {
    const { parseLogQuery } = await load();
    const rejected: Array<[string, unknown]> = [
      ['minutes 0', { ...base, minutes: 0 }],
      ['minutes 1441', { ...base, minutes: 1441 }],
      ['minutes 10.5', { ...base, minutes: 10.5 }],
      ['minutes -10', { ...base, minutes: -10 }],
      ['minutes NaN', { ...base, minutes: Number.NaN }],
      ['minutes Infinity', { ...base, minutes: Number.POSITIVE_INFINITY }],
      ['minutes as text', { ...base, minutes: '10' }],
      ['six words', { ...base, contains: ['a', 'b', 'c', 'd', 'e', 'f'] }],
      ['empty word', { ...base, contains: [''] }],
      ['65-character word', { ...base, contains: ['a'.repeat(65)] }],
      ['word that is a number', { ...base, contains: [1] }],
      ['words as text', { ...base, contains: 'error' }],
      ['words null', { ...base, contains: null }],
      ['limit 0', { ...base, limit: 0 }],
      ['limit 5001', { ...base, limit: 5001 }],
      ['limit 2.5', { ...base, limit: 2.5 }],
      ['limit as text', { ...base, limit: '1000' }],
      ['null', null],
      ['undefined', undefined],
      ['array', [10, ['error'], 1000]],
      ['query text', 'minutes=10&limit=1000'],
      ['number', 42],
    ];
    expect(rejected.map(([name, input]) => [name, parseLogQuery(input)])).toEqual(rejected.map(([name]) => [name, null]));
  });
});

describe('C3 commit input', () => {
  const commit = '0123456789abcdef0123456789abcdef01234567';

  it('C3 accepts { commit } with 40 lower-case hexadecimal characters', async () => {
    const { parseCommitInput } = await load();
    expect(parseCommitInput({ commit })).toBe(commit);
  });

  it('C3 rejects other lengths, upper case, other characters and other shapes', async () => {
    const { parseCommitInput } = await load();
    const rejected: Array<[string, unknown]> = [
      ['upper case', { commit: commit.toUpperCase() }],
      ['39 characters', { commit: commit.slice(1) }],
      ['41 characters', { commit: `${commit}0` }],
      ['64 characters', { commit: `${commit}${commit.slice(0, 24)}` }],
      ['non-hex character', { commit: `g${commit.slice(1)}` }],
      ['surrounding space', { commit: ` ${commit}` }],
      ['number', { commit: 123 }],
      ['null commit', { commit: null }],
      ['missing commit', {}],
      ['bare string', commit],
      ['null', null],
      ['array', [commit]],
    ];
    expect(rejected.map(([name, input]) => [name, parseCommitInput(input)])).toEqual(rejected.map(([name]) => [name, null]));
  });
});

describe('C4 backend answers', () => {
  const stoppingTimedOut = changed(STATUS_RUNNING, copy => {
    Object.assign(copy.server, { state: 'stopping', stopRequestedAt: '2026-10-10T03:10:00Z', stopTimedOut: true });
  });
  const otherHolder = changed(STATUS_STOPPED, copy => {
    Object.assign(copy.server, { portListening: true, portOwner: 'other', portOwnerDetail: 'pid 4242 dotnet', portLockHeldByOther: true });
  });
  const forcedExit = changed(STATUS_STOPPED, copy => {
    Object.assign(copy.lastExit, { kind: 'forced', exitCode: null, signal: 9 });
  });
  const firstStart = changed(STATUS_STOPPED, copy => {
    Object.assign(copy, { currentRelease: null, lastExit: null });
  });
  const startingUnknownOwner = changed(STATUS_RUNNING, copy => {
    Object.assign(copy.server, { state: 'starting', portListening: false, portOwner: 'unknown' });
  });

  it('C4 accepts status answers in the backend-design.md 「상태 응답」 shape', async () => {
    const { parseStatusResponse } = await load();
    const accepted = [STATUS_STOPPED, STATUS_RUNNING, stoppingTimedOut, otherHolder, forcedExit, firstStart, startingUnknownOwner];
    expect(accepted.map(body => parseStatusResponse(body))).toEqual(accepted);
  });

  it('C4 accepts the extra logError and recordError fields the PR1 backend adds to a status answer', async () => {
    // Committed PR1 backend ServerSupervisor.StatusLocked() returns these besides the design fields.
    const { parseStatusResponse } = await load();
    expect(parseStatusResponse({ ...STATUS_RUNNING, logError: null, recordError: null })).toMatchObject(STATUS_RUNNING);
  });

  it('C4 rejects status answers with a missing field, another type or an unknown state, portOwner or kind', async () => {
    const { parseStatusResponse } = await load();
    const rejected: Array<[string, unknown]> = [
      ['no server', changed(STATUS_RUNNING, copy => { Reflect.deleteProperty(copy, 'server'); })],
      ['unknown state', changed(STATUS_RUNNING, copy => { asRecord(copy.server).state = 'paused'; })],
      ['unknown portOwner', changed(STATUS_RUNNING, copy => { asRecord(copy.server).portOwner = 'nobody'; })],
      ['unknown lastExit kind', changed(STATUS_STOPPED, copy => { asRecord(copy.lastExit).kind = 'crashed'; })],
      ['server pid as text', changed(STATUS_RUNNING, copy => { asRecord(copy.server).pid = '4321'; })],
      ['backend pid as text', changed(STATUS_RUNNING, copy => { asRecord(copy.backend).pid = '501'; })],
      ['stopTimedOut as text', changed(STATUS_RUNNING, copy => { asRecord(copy.server).stopTimedOut = 'false'; })],
      ['no portLockHeldByOther', changed(STATUS_RUNNING, copy => { Reflect.deleteProperty(copy.server, 'portLockHeldByOther'); })],
      ['currentRelease commit as number', changed(STATUS_RUNNING, copy => { copy.currentRelease = { commit: 1, builtAt: '2026-10-10T01:00:00Z' } as never; })],
      ['running release without builtAt', changed(STATUS_RUNNING, copy => { asRecord(copy.server).release = { commit: STATUS_RUNNING.server.release?.commit }; })],
      ['null', null],
      ['array', [STATUS_RUNNING]],
      ['text', JSON.stringify(STATUS_RUNNING)],
    ];
    expect(rejected.map(([name, body]) => [name, parseStatusResponse(body)])).toEqual(rejected.map(([name]) => [name, null]));
  });

  it('C4 accepts release list answers in the 「실행본 목록 응답」 shape and rejects broken ones', async () => {
    const { parseReleasesResponse } = await load();
    const accepted = [RELEASES, { releases: [], currentRelease: null }];
    expect(accepted.map(body => parseReleasesResponse(body))).toEqual(accepted);
    const rejected: Array<[string, unknown]> = [
      ['releases as text', { ...RELEASES, releases: 'none' }],
      ['entry without sdkVersion', changed(RELEASES, copy => { Reflect.deleteProperty(copy.releases[0] as object, 'sdkVersion'); })],
      ['currentRelease builtAt as number', { ...RELEASES, currentRelease: { commit: RELEASES.currentRelease.commit, builtAt: 5 } }],
      ['no currentRelease', changed(RELEASES, copy => { Reflect.deleteProperty(copy, 'currentRelease'); })],
      ['null', null],
    ];
    expect(rejected.map(([name, body]) => [name, parseReleasesResponse(body)])).toEqual(rejected.map(([name]) => [name, null]));
  });

  it('C4 accepts log answers in the 「로그 조회」 shape and rejects broken ones', async () => {
    const { parseLogsResponse } = await load();
    const emptyLog = changed(LOGS, copy => {
      copy.lines = [];
      Object.assign(copy.retention, { oldestCollectedAt: null, totalBytes: 0 });
    });
    const cutLog = changed(LOGS, copy => {
      copy.truncated = true;
      Object.assign(copy.lines[1] as object, { textTruncated: true });
      copy.retention.recentDeletions = [{ from: '2026-10-01T00:00:00Z', to: '2026-10-02T00:00:00Z', bytes: 4096, reason: 'age' }] as never;
    });
    const accepted = [LOGS, emptyLog, cutLog];
    expect(accepted.map(body => parseLogsResponse(body))).toEqual(accepted);
    const rejected: Array<[string, unknown]> = [
      ['stream stdin', changed(LOGS, copy => { Object.assign(copy.lines[0] as object, { stream: 'stdin' }); })],
      ['seq as text', changed(LOGS, copy => { Object.assign(copy.lines[0] as object, { seq: '1' }); })],
      ['collectedAt as number', changed(LOGS, copy => { Object.assign(copy.lines[0] as object, { collectedAt: 1760065445 }); })],
      ['no textTruncated', changed(LOGS, copy => { Reflect.deleteProperty(copy.lines[0] as object, 'textTruncated'); })],
      ['no truncated', changed(LOGS, copy => { Reflect.deleteProperty(copy, 'truncated'); })],
      ['lines as text', { ...LOGS, lines: 'Server started' }],
      ['no retention', changed(LOGS, copy => { Reflect.deleteProperty(copy, 'retention'); })],
      ['null', null],
    ];
    expect(rejected.map(([name, body]) => [name, parseLogsResponse(body)])).toEqual(rejected.map(([name]) => [name, null]));
  });
});

describe('C5 error code messages', () => {
  // screen-design.md 「HTTP 대화」: every code 05_Management/backend/ManagementBackend/ answers with.
  const BACKEND_CODES = [
    'busy', 'alreadyRunning', 'noCurrentRelease', 'portBusy', 'startFailed', 'notRunning', 'invalidCommit',
    'commitNotFound', 'releaseNotFound', 'releaseSourceNotConfigured', 'buildFailed', 'invalidQuery',
    'logReadFailed', 'operationFailed', 'invalidRequest', 'bodyTooLarge', 'notFound', 'forbidden', 'unauthorized',
  ];

  it('C5 gives each of the 19 backend codes its own nonempty Korean message, unlike the general failure message', async () => {
    const { serverOperationMessage } = await load();
    const general = serverOperationMessage('codeNoBackendSends');
    const rows = BACKEND_CODES.map(code => {
      const message = serverOperationMessage(code);
      return { code, korean: HANGUL.test(message), differsFromGeneral: message !== general };
    });
    expect(rows).toEqual(BACKEND_CODES.map(code => ({ code, korean: true, differsFromGeneral: true })));
  });

  it('C5 shows the general Korean failure message for unknown codes, including object member names', async () => {
    const { serverOperationMessage } = await load();
    const general = serverOperationMessage('codeNoBackendSends');
    expect(HANGUL.test(general)).toBe(true);
    const unknownCodes = ['', 'BUSY', 'portbusy', 'toString', 'constructor', '__proto__'];
    expect(unknownCodes.map(code => [code, serverOperationMessage(code)])).toEqual(unknownCodes.map(code => [code, general]));
  });
});

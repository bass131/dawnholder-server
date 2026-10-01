// V3-R1 (verifier-owned): requirement invariants of the Windows save rename retry (goal 범위·D3,
// main msg_f1bd05219af4), independent of the backoff schedule the product picks.
// - Only EPERM/EACCES/EBUSY are retried; any other error, or one without a string code, fails at once.
// - Every new attempt starts before 1000 ms from the first one, after a non-zero backoff.
// - Retrying goes on until no further attempt could start in time; then the LAST native error is
//   rethrown, so the store reports its existing write failure.
// - The renamer never waits past the deadline; an in-flight OS call may still finish later.
// The concrete schedule is only measured and logged. Time is virtual: it moves through wait() and an
// optional per-attempt cost, and attempt starts are recorded by the injected rename, not the observer.
import { expect } from 'vitest';
import type { CatalogRenameObservation, CatalogRenameOptions } from '../../electron/catalog-rename';
import { osError } from '../mcp-fixtures';

export const RETRY_START_DEADLINE_MS = 1000;
export const TRANSIENT_CODES = ['EPERM', 'EACCES', 'EBUSY'] as const;
export const NON_TRANSIENT_CODES = ['EIO', 'ENOENT', 'EEXIST', 'EXDEV', 'ENOSPC'] as const;
// Stops a renamer that never honours the deadline (e.g. a zero backoff on a virtual clock).
const RUNAWAY_ATTEMPTS = 5_000;

export type CreateRenamer = (options: CatalogRenameOptions) => (source: string, target: string) => Promise<void>;
export interface RenameRun {
  starts: number[];
  waits: number[];
  observations: CatalogRenameObservation[];
  errors: unknown[];
  settledAt: number;
  runaway: boolean;
  result: { ok: true } | { ok: false; error: unknown };
}

// script[i] is the outcome of attempt i: an error code to throw, or null for success. The last entry
// repeats. attemptCostMs[i] advances the clock while attempt i is "in the OS".
export async function runRenamer(create: CreateRenamer, script: Array<string | null>, options: { attemptCostMs?: number[] } = {}): Promise<RenameRun> {
  let time = 0;
  const run: Omit<RenameRun, 'result'> = { starts: [], waits: [], observations: [], errors: [], settledAt: 0, runaway: false };
  const renamer = create({
    now: () => time,
    wait: async milliseconds => { run.waits.push(milliseconds); time += milliseconds; },
    onAttempt: observation => { run.observations.push(observation); },
    rename: async () => {
      const index = run.starts.length;
      run.starts.push(time);
      time += options.attemptCostMs?.[index] ?? 0;
      const step = index >= RUNAWAY_ATTEMPTS ? 'EIO' : index < script.length ? script[index] : script.at(-1);
      if (index >= RUNAWAY_ATTEMPTS) run.runaway = true;
      if (step === null || step === undefined) return;
      const error = osError(step);
      run.errors.push(error);
      throw error;
    },
  });
  let result: RenameRun['result'];
  try {
    await renamer('tmp', 'catalog');
    result = { ok: true };
  } catch (error) {
    result = { ok: false, error };
  }
  return { ...run, settledAt: time, result };
}

const gapsOf = (starts: number[]) => starts.slice(1).map((start, index) => start - (starts[index] as number));

// Attempt starts move forward after a backoff and none starts at or after the deadline.
function expectStartsWithinDeadline(run: RenameRun) {
  expect(run.runaway, 'attempts kept starting without honouring the deadline').toBe(false);
  for (const gap of gapsOf(run.starts)) expect(gap, 'a new attempt starts only after a non-zero backoff').toBeGreaterThan(0);
  expect(Math.max(...run.starts), 'no attempt starts at or after the deadline').toBeLessThan(RETRY_START_DEADLINE_MS);
}

// Observer hook (test measurement seam): one observation per attempt, in order, with the code.
function expectObservations(run: RenameRun, outcomes: Array<string | null>) {
  expect(run.observations.map(item => item.attempt)).toEqual(outcomes.map((_, index) => index + 1));
  expect(run.observations.map(item => (item.outcome === 'succeeded' ? null : item.code ?? '(none)'))).toEqual(outcomes);
  run.observations.forEach((item, index) => expect(item.elapsedMs).toBeGreaterThanOrEqual(run.starts[index] as number));
}

// Exhaustion: no start at/after the deadline, no wait past it (a slow OS call may end later than
// the deadline; the renamer adds nothing after it), and the last native error is rethrown.
export function expectDeadlineRespected(run: RenameRun, attemptCostMs: number[] = []) {
  expectStartsWithinDeadline(run);
  const lastAttemptEnd = (run.starts.at(-1) as number) + (attemptCostMs[run.starts.length - 1] ?? 0);
  expect(run.settledAt, 'the renamer does not wait past the deadline').toBeLessThanOrEqual(Math.max(RETRY_START_DEADLINE_MS, lastAttemptEnd));
  expect(run.result.ok).toBe(false);
  expect(!run.result.ok && run.result.error, 'the last native error is rethrown').toBe(run.errors.at(-1));
}

// A transient code that never clears: retried within the window, then the last error.
export function expectExhaustedRetry(run: RenameRun, code: string) {
  expectDeadlineRespected(run);
  expect(run.starts.length, `${code} must be retried`).toBeGreaterThanOrEqual(2);
  // Gave up only when one more backoff of the size it already used would reach the deadline.
  expect((run.starts.at(-1) as number) + Math.max(...gapsOf(run.starts)), 'retrying stopped before the window was used').toBeGreaterThanOrEqual(RETRY_START_DEADLINE_MS);
  expectObservations(run, run.starts.map(() => code));
}

// A non-target code (or the first non-target after transient ones) ends the rename at once.
export function expectImmediateFailure(run: RenameRun, outcomes: Array<string | null>) {
  expectStartsWithinDeadline(run);
  expect(run.starts).toHaveLength(outcomes.length);
  expect(run.waits.length, 'no backoff after a non-target error').toBe(outcomes.length - 1);
  expect(run.result.ok).toBe(false);
  expect(!run.result.ok && run.result.error).toBe(run.errors.at(-1));
  expectObservations(run, outcomes);
}

// Transient failures that clear before the deadline: the rename succeeds on the next attempt.
export function expectRecovered(run: RenameRun, outcomes: Array<string | null>) {
  expectStartsWithinDeadline(run);
  expect(run.result.ok).toBe(true);
  expect(run.starts).toHaveLength(outcomes.length);
  expectObservations(run, outcomes);
}

export function scheduleOf(run: RenameRun) {
  return { attempts: run.starts.length, starts: run.starts, waits: run.waits, settledAt: run.settledAt };
}

// The requirement scenarios, shared by the V1 rename test (product) and the V3-R1 mutant check.
export interface RenameScenario { title: string; check(create: CreateRenamer): Promise<RenameRun | undefined> }
export const RENAME_SCENARIOS: RenameScenario[] = [
  ...TRANSIENT_CODES.map(code => ({
    title: `${code}: retried after a backoff, no attempt starts at or after 1000 ms, gives up only when the window is used, last error rethrown`,
    async check(create: CreateRenamer) { const run = await runRenamer(create, [code]); expectExhaustedRetry(run, code); return run; },
  })),
  ...NON_TRANSIENT_CODES.map(code => ({
    title: `${code} is not transient: one attempt, no wait`,
    async check(create: CreateRenamer) { const run = await runRenamer(create, [code]); expectImmediateFailure(run, [code]); return run; },
  })),
  {
    title: 'an error without a string code is not retried',
    async check(create) {
      for (const failure of [new Error('no code'), Object.assign(new Error('numeric code'), { code: -4048 })]) {
        const observations: CatalogRenameObservation[] = [];
        const renamer = create({ now: () => 0, wait: async () => { throw new Error('must not wait'); }, rename: async () => { throw failure; }, onAttempt: item => { observations.push(item); } });
        await expect(renamer('tmp', 'catalog')).rejects.toBe(failure);
        expect(observations).toEqual([{ attempt: 1, elapsedMs: 0, outcome: 'failed' }]);
      }
      return undefined;
    },
  },
  {
    title: 'recovers on a later attempt and reports the attempt distribution',
    async check(create) { const outcomes = ['EPERM', 'EBUSY', 'EACCES', null]; const run = await runRenamer(create, outcomes); expectRecovered(run, outcomes); return run; },
  },
  {
    title: 'a transient failure followed by a non-transient one stops immediately with the latter',
    async check(create) {
      const run = await runRenamer(create, ['EPERM', 'EIO']);
      expectImmediateFailure(run, ['EPERM', 'EIO']);
      expect(!run.result.ok && (run.result.error as { code?: string }).code).toBe('EIO');
      return run;
    },
  },
  {
    title: 'a slow attempt that ends past the deadline is not followed by another attempt (OS latency is not cut short)',
    async check(create) {
      const run = await runRenamer(create, ['EPERM', 'EPERM'], { attemptCostMs: [1_500] });
      expectImmediateFailure(run, ['EPERM']);
      expect(run.observations[0]?.elapsedMs).toBe(1_500);
      return run;
    },
  },
  {
    title: 'when the deadline falls inside an attempt or its backoff, no attempt follows and nothing waits past it',
    async check(create) { const attemptCostMs = [995]; const run = await runRenamer(create, ['EBUSY'], { attemptCostMs }); expectDeadlineRespected(run, attemptCostMs); return run; },
  },
];

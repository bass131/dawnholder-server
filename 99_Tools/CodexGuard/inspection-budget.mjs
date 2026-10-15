// The mandatory candidate index has a fixed linear reservation. It finishes before
// precision can stop, so exhaustion retains both candidate and candidate-free outcomes.
export const supportedCommandLimit = 2048;
// A conservative 1024n reservation covers both views, including 32 nested body levels.
// Literal location
// resolution can read the accumulated path once per supported command: <= 2048n.
// 4096 * 1 MiB leaves headroom for both under the unchanged serialized input limit.
// These are logical work units, not an allocation or a millisecond timeout.
export const inspectionStepLimit = 4096 * 1024 * 1024;
export class InspectionLimit extends Error {}

export function inspectionSlice(source, start, end, work, phase) {
  const value = source.slice(start, end);
  work?.record('substringCharacters', value.length);
  work?.record(phase, value.length);
  return value;
}

export function createInspectionWork(metrics = {}, limit = inspectionStepLimit) {
  metrics.steps = 0;
  metrics.calls = {};
  metrics.phases = {};
  metrics.limit = limit;
  metrics.exhausted = false;
  return {
    metrics,
    call(name) { metrics.calls[name] = (metrics.calls[name] ?? 0) + 1; },
    record(name, units = 1) {
      metrics.steps += units;
      metrics.phases[name] = (metrics.phases[name] ?? 0) + units;
    },
    check() {
      if (metrics.steps > limit) {
        metrics.exhausted = true;
        throw new InspectionLimit('command inspection work budget exceeded');
      }
    },
    step(name, units = 1) {
      this.record(name, units);
      this.check();
    },
  };
}

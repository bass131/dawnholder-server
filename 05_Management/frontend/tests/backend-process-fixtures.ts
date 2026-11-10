// /proc/<pid>/stat text for the fake WSL of the backend-connection tests. The layout is written out here from proc(5),
// not derived from the product: pid, (comm), state, fields 4-21, starttime as field 22, then fields 23-52.
const FIELDS_BEFORE_START_TIME = 18;
const FIELDS_AFTER_START_TIME = 30;

export interface ProcStat {
  pid: number;
  name: string;
  startTime: number;
  // Changes every field except pid, name and starttime, as a running process does between two reads.
  tick?: number;
}

export function procStatLine({ pid, name, startTime, tick = 0 }: ProcStat): string {
  const counters = (first: number, count: number) => Array.from({ length: count }, (_unused, index) => String(first + index * 7 + tick * 3));
  return [
    String(pid),
    `(${name})`,
    'S',
    ...counters(1000, FIELDS_BEFORE_START_TIME),
    String(startTime),
    ...counters(5000, FIELDS_AFTER_START_TIME),
  ].join(' ');
}

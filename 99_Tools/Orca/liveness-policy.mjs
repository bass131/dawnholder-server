function isRecord(value) {
  return value !== null && typeof value === 'object' && !Array.isArray(value);
}

function isText(value) {
  return typeof value === 'string' && value.trim().length > 0;
}

export function inputFailure(thresholdSeconds, code, path, message, repair) {
  return {
    status: 'input-error',
    thresholdSeconds,
    dispatches: [],
    diagnostics: [{ code, path, message, repair }],
  };
}

function parseMessages(text) {
  const lines = text.split(/\r?\n/);
  let start = 0;
  while (start < lines.length) {
    if (lines[start].trim() === '') {
      start++;
      continue;
    }
    try {
      const line = JSON.parse(lines[start]);
      if (!isRecord(line) || line._keepalive !== true) {
        break;
      }
    } catch {
      break;
    }
    start++;
  }
  const input = JSON.parse(lines.slice(start).join('\n'));
  return Array.isArray(input) ? input : input?.result?.messages;
}

function normalizeMessage(message, order) {
  if (!isRecord(message) || !isText(message.id) || !isText(message.type) ||
    !isText(message.from_handle) || !isText(message.created_at)) {
    throw new Error('Each message needs id, type, from_handle and created_at text.');
  }
  const time = Date.parse(message.created_at);
  if (!Number.isFinite(time)) {
    throw new Error('created_at must be a valid sending timestamp.');
  }
  if (message.sequence != null && !Number.isSafeInteger(message.sequence)) {
    throw new Error('sequence must be an integer when present.');
  }
  if (message.thread_id != null && !isText(message.thread_id)) {
    throw new Error('thread_id must be text when present.');
  }
  const payload = typeof message.payload === 'string' ? JSON.parse(message.payload) : message.payload;
  if (payload != null && !isRecord(payload)) {
    throw new Error('payload must be an object, a JSON object string, or null.');
  }
  let dispatchId = payload?.dispatchId;
  if (dispatchId != null && !isText(dispatchId)) {
    throw new Error('payload.dispatchId must be nonempty text when present.');
  }
  if (dispatchId == null && message.from_handle.startsWith('dispatch:')) {
    dispatchId = message.from_handle.slice('dispatch:'.length);
    if (!isText(dispatchId)) {
      throw new Error('A dispatch: sender must include its Dispatch id.');
    }
  }
  return { ...message, dispatchId, time, order };
}

function orderMessages(messages) {
  const byTime = new Map();
  for (const message of messages) {
    if (!byTime.has(message.time)) {
      byTime.set(message.time, []);
    }
    byTime.get(message.time).push(message);
  }
  // Sender timestamps measure worker silence; mailbox delivery/collection time does not.
  return [...byTime.entries()].sort(([a], [b]) => a - b).flatMap(([, tied]) => {
    const hasSequences = tied.every(message => message.sequence != null);
    return tied.sort((a, b) => hasSequences
      ? a.sequence - b.sequence || a.order - b.order
      : a.order - b.order);
  });
}

function point(message) {
  return { id: message.id, at: message.created_at, type: message.type };
}

function gap(from, to) {
  return { from: point(from), to: point(to), seconds: (to.time - from.time) / 1000 };
}

function measureDispatch(dispatchId, messages, replies, thresholdSeconds) {
  const ordered = orderMessages(messages);
  const done = ordered.findIndex(message => message.type === 'worker_done');
  const signals = done < 0 ? ordered : ordered.slice(0, done + 1);
  const overGaps = [];
  const questionWaits = [];
  let maxGapSeconds = 0;
  for (let index = 1; index < signals.length; index++) {
    let from = signals[index - 1];
    const to = signals[index];
    if (from.type === 'question') {
      const reply = (replies.get(from.id) ?? []).find(candidate =>
        candidate.from_handle !== from.from_handle &&
        candidate.time >= from.time && candidate.time <= to.time);
      if (reply) {
        // A blocking ask prevents signals until the reply; silence after it counts again.
        questionWaits.push({ ...gap(from, reply), replyId: reply.id });
        from = reply;
      }
    }
    const interval = gap(from, to);
    maxGapSeconds = Math.max(maxGapSeconds, interval.seconds);
    if (interval.seconds > thresholdSeconds) {
      overGaps.push(interval);
    }
  }
  return {
    dispatchId,
    signalCount: signals.length,
    firstAt: signals[0].created_at,
    lastAt: signals.at(-1).created_at,
    maxGapSeconds,
    overGaps,
    questionWaits,
  };
}

/** Analyze saved { path, text } inputs; no live mailbox access or lifecycle changes. */
export function evaluateLiveness(inputs, thresholdSeconds = 300) {
  if (!Number.isFinite(thresholdSeconds) || thresholdSeconds < 0) {
    return inputFailure(null, 'threshold', '--threshold-seconds', 'Expected finite, nonnegative seconds.',
      'Supply a numeric threshold such as --threshold-seconds 300.');
  }
  if (!Array.isArray(inputs) || inputs.length === 0) {
    return inputFailure(thresholdSeconds, 'inputs', '$', 'No saved message inputs were supplied.',
      'Supply one or more saved Orca JSON output files.');
  }
  const unique = new Map();
  let order = 0;
  for (const input of inputs) {
    if (!isRecord(input) || !isText(input.path) || typeof input.text !== 'string') {
      return inputFailure(thresholdSeconds, 'input-shape', '$', 'Expected inputs with path and text.',
        'Pass the source filename and its decoded UTF-8 contents.');
    }
    let messages;
    try {
      messages = parseMessages(input.text);
    } catch {
      return inputFailure(thresholdSeconds, 'input-json', input.path, 'Input is not valid JSON after leading keepalive lines.',
        'Preserve the final Orca JSON output after any leading _keepalive JSON lines.');
    }
    if (!Array.isArray(messages)) {
      return inputFailure(thresholdSeconds, 'input-shape', input.path, 'Expected a message array or result.messages array.',
        'Save inbox/check --json output or an array of the original message objects.');
    }
    for (let index = 0; index < messages.length; index++) {
      let message;
      try {
        message = normalizeMessage(messages[index], order++);
      } catch (error) {
        return inputFailure(thresholdSeconds, 'message-shape', `${input.path}:messages[${index}]`, error.message,
          'Re-export the original message with its identity, sender timestamp, payload and optional sequence intact.');
      }
      if (!unique.has(message.id)) {
        unique.set(message.id, message);
      }
    }
  }
  const groups = new Map();
  const replies = new Map();
  for (const message of orderMessages([...unique.values()])) {
    if (message.dispatchId != null) {
      if (!groups.has(message.dispatchId)) {
        groups.set(message.dispatchId, []);
      }
      groups.get(message.dispatchId).push(message);
    }
    if (message.thread_id != null) {
      if (!replies.has(message.thread_id)) {
        replies.set(message.thread_id, []);
      }
      replies.get(message.thread_id).push(message);
    }
  }
  if (groups.size === 0) {
    return inputFailure(thresholdSeconds, 'no-dispatch', inputs.map(input => input.path).join(', '),
      'The saved inputs contain no Dispatch signals.',
      'Include worker messages with payload.dispatchId or a dispatch:<id> sender.');
  }
  const dispatches = [...groups].map(([id, messages]) => measureDispatch(id, messages, replies, thresholdSeconds));
  return {
    status: dispatches.some(dispatch => dispatch.overGaps.length > 0) ? 'over' : 'within',
    thresholdSeconds,
    dispatches,
    diagnostics: [],
  };
}

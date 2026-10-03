const supportedTypes = new Set(['status', 'question', 'worker_done', 'escalation', 'heartbeat']);

function isRecord(value) {
  return value !== null && typeof value === 'object' && !Array.isArray(value);
}

function isIdentity(value) {
  return typeof value === 'string' && value.trim().length > 0 && value === value.trim();
}

function diagnostic(code, path, message, repair) {
  return { code, path, message, repair };
}

function result(status, diagnostics, exception = null) {
  const exitCode = status === 'allowed' ? 0 : status === 'policy-violation' ? 1 : 2;
  return { status, exitCode, exception, diagnostics };
}

function readPayload(value) {
  if (typeof value === 'string') {
    try {
      value = JSON.parse(value);
    } catch {
      return null;
    }
  }
  return isRecord(value) ? value : null;
}

function isOptionalText(value) {
  return value === undefined || value === null || typeof value === 'string';
}

function isEmptyText(value) {
  return value === undefined || value === null || value.trim().length === 0;
}

function hasTag(value, tag) {
  return typeof value === 'string' && value.startsWith(tag);
}

function isHeartbeatMetadata(payload) {
  return Object.keys(payload).every(key => ['taskId', 'dispatchId', 'phase'].includes(key));
}

function validateInput(input) {
  const issues = [];
  if (!isRecord(input) || !isRecord(input.expected) || !isRecord(input.message)) {
    return [diagnostic('input-shape', '$', 'Expected an object with message and expected objects.',
      'Export one Orca message and independently confirmed expected identities into a JSON file.')];
  }

  const { expected, message } = input;
  for (const key of ['fromHandle', 'taskId', 'dispatchId']) {
    if (!isIdentity(expected[key])) {
      issues.push(diagnostic('expected-identity', `expected.${key}`, 'Expected identity is missing or malformed.',
        'Confirm the active receipt/worker-show identity; do not derive it from this message.'));
    }
  }
  if (typeof expected.tag !== 'string' || !/^\[[^\[\]\r\n]+\]$/.test(expected.tag)) {
    issues.push(diagnostic('expected-tag', 'expected.tag', 'Expected a bracketed sender tag.',
      'Supply the assigned sender tag, for example [Rules Sol], from the current contract.'));
  }
  if (!isIdentity(message.from_handle)) {
    issues.push(diagnostic('actual-identity', 'message.from_handle', 'Message sender identity is missing or malformed.',
      'Read the original Orca message object; an absent identity cannot match a valid expected identity.'));
  }
  if (typeof message.type !== 'string' || !supportedTypes.has(message.type)) {
    issues.push(diagnostic('unsupported-context', 'message.type', 'This is not a supported active-Dispatch worker message.',
      'Use the current Orca CLI and a human source comparison for replies, Main terminal-only messages, or other contexts.'));
  }
  for (const key of ['subject', 'body']) {
    if (!isOptionalText(message[key])) {
      issues.push(diagnostic('text-shape', `message.${key}`, 'Text must be a string, null, or absent.',
        'Preserve the raw Orca text field; do not coerce numbers, arrays, or objects to empty text.'));
    }
  }
  if (expected.officialAsk !== undefined) {
    const proof = expected.officialAsk;
    if (!isRecord(proof) || !isIdentity(proof.messageId) || proof.cliVersion !== '1.4.218') {
      issues.push(diagnostic('official-ask-proof', 'expected.officialAsk', 'The confirmed blocking-ask evidence is incomplete or unsupported.',
        'Use the coordinator-confirmed question receipt ID and CLI version 1.4.218; otherwise compare the source manually.'));
    }
  }

  return issues;
}

function officialAskMatches(message, payload, expected) {
  const proof = expected.officialAsk;
  // send can imitate the visible question shape. Only independently supplied receipt evidence opens this exception.
  return proof !== undefined && message.type === 'question' && message.subject === 'Question' &&
    message.id === proof.messageId && message.thread_id === proof.messageId &&
    message.from_handle === `dispatch:${expected.dispatchId}` &&
    typeof payload.question === 'string' && payload.question === message.body;
}

/** Read-only policy aid for one active Dispatch. expected comes from the coordinator, not the message. */
export function evaluateMessage(input) {
  const inputIssues = validateInput(input);
  if (inputIssues.length > 0) {
    return result('input-error', inputIssues);
  }

  const { message, expected } = input;
  const payload = readPayload(message.payload);
  if (payload === null) {
    return result('input-error', [diagnostic('payload-shape', 'message.payload',
      'Payload must be a JSON object or a JSON string containing an object.',
      'Export the raw message with taskId and dispatchId; do not replace a missing or invalid payload with an empty object.')]);
  }
  const missingIdentities = ['taskId', 'dispatchId'].filter(key => !isIdentity(payload[key]));
  if (missingIdentities.length > 0) {
    return result('input-error', missingIdentities.map(key => diagnostic('actual-identity', `message.payload.${key}`,
      'Payload identity is missing or malformed.',
      'Read the original current-Dispatch message and retain its explicit taskId and dispatchId.')));
  }
  if (message.type === 'heartbeat' && !isOptionalText(payload.phase)) {
    return result('input-error', [diagnostic('heartbeat-phase', 'message.payload.phase',
      'Heartbeat phase must be a string, null, or absent.',
      'Retain the CLI phase text; do not coerce malformed payload data to an empty heartbeat.')]);
  }

  const issues = [];
  const identities = [
    ['message.from_handle', message.from_handle, expected.fromHandle],
    ['message.payload.taskId', payload.taskId, expected.taskId],
    ['message.payload.dispatchId', payload.dispatchId, expected.dispatchId],
  ];
  for (const [path, actual, wanted] of identities) {
    if (actual !== wanted) {
      issues.push(diagnostic('identity-mismatch', path, 'Message identity does not match the independently confirmed active Dispatch.',
        'Do not process this message. Recheck the current receipt/worker-show and report the mismatch to Main.'));
    }
  }
  if (issues.length > 0) {
    return result('policy-violation', issues);
  }

  const emptyHeartbeat = message.type === 'heartbeat' && isHeartbeatMetadata(payload) && isEmptyText(message.body) &&
    (isEmptyText(message.subject) || message.subject === 'alive');
  if (emptyHeartbeat) {
    return result('allowed', [], 'empty-heartbeat');
  }

  const officialAsk = officialAskMatches(message, payload, expected);
  if (expected.officialAsk !== undefined && !officialAsk) {
    issues.push(diagnostic('official-ask-mismatch', 'expected.officialAsk',
      'The message does not match the confirmed version-specific blocking-ask receipt and shape.',
      'Do not open the Question exception based on its subject or self-declaration. Recheck the question receipt and report the mismatch to Main.'));
  }
  if (!officialAsk && !hasTag(message.subject, expected.tag)) {
    issues.push(diagnostic('subject-tag', 'message.subject', 'Subject must start with the assigned sender tag.',
      'Use a tagged subject for ordinary send. For official blocking ask, independently confirm its receipt before supplying officialAsk evidence.'));
  }
  if (!hasTag(message.body, expected.tag)) {
    issues.push(diagnostic('body-tag', 'message.body', 'Body must start with the assigned sender tag.',
      'Prefix the body with the assigned tag. Do not process an untagged report/question; report a source mismatch to Main.'));
  }

  return issues.length > 0
    ? result('policy-violation', issues)
    : result('allowed', [], officialAsk ? 'official-blocking-ask-1.4.218' : null);
}

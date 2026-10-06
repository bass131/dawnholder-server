// V2 verifier-owned stdio fixture entry (not a product entry). It assembles the BUILT product
// factories (mcp-dist: createCatalogServer, createCatalogReader, nodeCatalogFileOperations)
// exactly like mcp/main.ts does, but with a test-chosen catalog path, an optional injected
// clock and counters reported over a Node IPC side channel. stdout stays the MCP protocol;
// this entry never writes to stdout or stderr itself.
//
// Record index v2 (index-v2-design.md 「MCP 서버 주입 지점」): it also wires the built stores
// (mcp-dist/electron: system-guide, source-section, checkout) like main.ts. Until step 5 rebuilds
// mcp-dist those modules are absent and this entry fails to start.
//
// argv: --mode real|controlled  --catalog <path>  --clock real|frozen|advancing  --version <s>
//       --guide <path>            default: system-guide.json next to the catalog (main.ts layout)
//       --repository-root <path>  default: two folders above the catalog folder (main.ts layout)
//   real        real Node file I/O on <path>, wrapped only to count open/close per request
//   controlled  in-memory bytes loaded once from <path>; open/stat/read/close can be paused,
//               failed or replaced by IPC commands (deterministic interference/cancellation)
// IPC commands: {c:'counters'} {c:'pause',op,abortAware} {c:'release',op} {c:'setBytes',b64}
//               {c:'failNext',op,code}
// IPC events:   {t:'ready'} {t:'reached',op} {t:'counters',...}
import { readFileSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { serveStdio } from '@modelcontextprotocol/server/stdio';
import { createCatalogReader, nodeCatalogFileOperations } from '../../mcp-dist/mcp/catalog-reader.js';
import { createCatalogServer } from '../../mcp-dist/mcp/catalog-server.js';
import { createCheckoutStore } from '../../mcp-dist/electron/checkout-store.js';
import { createSourceSectionStore } from '../../mcp-dist/electron/source-section-store.js';
import { createSystemGuideStore } from '../../mcp-dist/electron/system-guide-store.js';

const args = new Map();
for (let index = 2; index < process.argv.length; index += 2) args.set(process.argv[index], process.argv[index + 1]);
const mode = args.get('--mode') ?? 'real';
const catalogPath = args.get('--catalog');
const clock = args.get('--clock') ?? 'real';
const version = args.get('--version') ?? 'v2-fixture';
if (!catalogPath) throw new Error('fixture entry needs --catalog');
const guidePath = args.get('--guide') ?? join(dirname(catalogPath), 'system-guide.json');
const repositoryRoot = args.get('--repository-root') ?? resolve(dirname(catalogPath), '..', '..');

const counters = {
  factoryCalls: 0, factoryEras: [], entered: 0, enteredByTool: {}, readSnapshotCalls: 0, readSnapshotOk: 0, readSnapshotErrors: {},
  openAttempts: 0, openFailures: {}, opens: 0, closes: 0, openedPaths: [], openAtSettle: 0, maxConcurrentReads: 0,
  readGuideCalls: 0, readSourceSectionCalls: 0, readCheckoutCalls: 0,
};
const send = message => { if (process.connected) process.send(message); };

// ------------------------------------------------------------- controlled in-memory file
const controlled = { bytes: new Uint8Array(0), mtimeMs: 1000, ctimeMs: 1000, nextHandle: 1, open: new Set(), pauses: new Map(), failures: new Map() };
if (mode === 'controlled') controlled.bytes = new Uint8Array(readFileSync(catalogPath));

async function gate(op, signal) {
  const queue = controlled.pauses.get(op) ?? [];
  const pause = queue.shift();
  if (pause) {
    pause.reached = true;
    send({ t: 'reached', op });
    if (pause.abortAware && signal) {
      const aborted = new Promise(resolve => {
        if (signal.aborted) resolve('aborted'); else signal.addEventListener('abort', () => resolve('aborted'), { once: true });
      });
      const outcome = await Promise.race([pause.gate.then(() => 'released'), aborted]);
      if (outcome === 'aborted') {
        // An aborted pause must not absorb a later release meant for another request.
        pending.set(op, (pending.get(op) ?? []).filter(item => item !== pause));
        throw Object.assign(new Error('fixture abort'), { name: 'AbortError', code: 'ABORT_ERR' });
      }
    } else {
      await pause.gate;
    }
  }
  const failure = controlled.failures.get(op)?.shift();
  if (failure) throw Object.assign(new Error(`fixture ${failure} SENTINEL_FIXTURE_OS_TEXT`), { code: failure });
}
const pending = new Map();
function addPause(op, abortAware) {
  let release;
  const gatePromise = new Promise(resolve => { release = resolve; });
  const pause = { abortAware, gate: gatePromise, release, reached: false };
  controlled.pauses.set(op, [...controlled.pauses.get(op) ?? [], pause]);
  pending.set(op, [...pending.get(op) ?? [], pause]);
}
const controlledOperations = {
  async open(_path, signal) { await gate('open', signal); const id = controlled.nextHandle++; controlled.open.add(id); return { id }; },
  async stat(handle, signal) {
    if (!controlled.open.has(handle.id)) throw new Error('stat on closed handle');
    await gate('stat', signal);
    return { size: controlled.bytes.byteLength, mtimeMs: controlled.mtimeMs, ctimeMs: controlled.ctimeMs, dev: 7, ino: 42 };
  },
  async read(handle, maxBytes, signal) {
    if (!controlled.open.has(handle.id)) throw new Error('read on closed handle');
    await gate('read', signal);
    return controlled.bytes.slice(0, Math.min(maxBytes, controlled.bytes.byteLength));
  },
  async close(handle) { await gate('close'); controlled.open.delete(handle.id); },
};

// ------------------------------------------------------------- counting wrapper (no delay)
const handlesBySignal = new WeakMap();
const openHandles = new Set();
const base = mode === 'controlled' ? controlledOperations : nodeCatalogFileOperations;
const fileOperations = {
  async open(path, signal) {
    counters.openedPaths.push(path);
    counters.openAttempts += 1;
    let handle;
    try { handle = await base.open(path, signal); } catch (error) {
      const code = error?.code ?? 'UNKNOWN';
      counters.openFailures[code] = (counters.openFailures[code] ?? 0) + 1;
      throw error;
    }
    counters.opens += 1;
    openHandles.add(handle);
    handlesBySignal.set(signal, [...handlesBySignal.get(signal) ?? [], handle]);
    return handle;
  },
  stat: (handle, signal) => base.stat(handle, signal),
  read: (handle, maxBytes, signal) => base.read(handle, maxBytes, signal),
  async close(handle) { await base.close(handle); counters.closes += 1; openHandles.delete(handle); },
};
const reader = createCatalogReader({ catalogPath, fileOperations });
let activeReads = 0;
async function readSnapshot(signal) {
  counters.readSnapshotCalls += 1;
  activeReads += 1;
  counters.maxConcurrentReads = Math.max(counters.maxConcurrentReads, activeReads);
  try {
    const snapshot = await reader.readSnapshot(signal);
    counters.readSnapshotOk += 1;
    return snapshot;
  } catch (error) {
    const code = error?.code ?? 'UNKNOWN';
    counters.readSnapshotErrors[code] = (counters.readSnapshotErrors[code] ?? 0) + 1;
    throw error;
  } finally {
    activeReads -= 1;
    // Search/serialization run after readSnapshot settles: this request's handle must be closed.
    for (const handle of handlesBySignal.get(signal) ?? []) if (openHandles.has(handle)) counters.openAtSettle += 1;
  }
}

// Same wiring as main.ts: a new guide store per request; one section and one checkout store.
const sectionStore = createSourceSectionStore({ repositoryRoot });
const checkoutStore = createCheckoutStore({ repositoryRoot });
const readGuide = () => { counters.readGuideCalls += 1; return createSystemGuideStore(guidePath).read(); };
const readSourceSection = source => { counters.readSourceSectionCalls += 1; return sectionStore.read(source); };
const readCheckout = () => { counters.readCheckoutCalls += 1; return checkoutStore.read(); };

let tick = 0;
const now = clock === 'frozen' ? () => 0 : clock === 'advancing' ? () => (tick += 1000) : undefined;

process.channel?.unref();
process.on('message', message => {
  if (message.c === 'counters') send({ t: 'counters', ...counters, openHandlesNow: openHandles.size, inMemoryOpen: controlled.open.size });
  else if (message.c === 'pause') addPause(message.op, message.abortAware === true);
  else if (message.c === 'release') {
    const queue = pending.get(message.op) ?? [];
    const index = queue.findIndex(item => item.reached);
    const pause = index >= 0 ? queue.splice(index, 1)[0] : undefined;
    pause?.release();
    send({ t: 'released', op: message.op, found: pause !== undefined });
  } else if (message.c === 'setBytes') {
    controlled.bytes = new Uint8Array(Buffer.from(message.b64, 'base64'));
    controlled.mtimeMs += 1; controlled.ctimeMs += 1;
    send({ t: 'bytesSet' });
  } else if (message.c === 'failNext') {
    controlled.failures.set(message.op, [...controlled.failures.get(message.op) ?? [], message.code]);
    send({ t: 'failSet' });
  }
});

serveStdio(({ era }) => {
  counters.factoryCalls += 1;
  counters.factoryEras.push(era);
  return createCatalogServer({
    readSnapshot, readGuide, readSourceSection, readCheckout, version, ...(now ? { now } : {}),
    onToolHandlerEntered: name => { counters.entered += 1; counters.enteredByTool[name] = (counters.enteredByTool[name] ?? 0) + 1; },
  });
});
send({ t: 'ready', pid: process.pid });

import { fileURLToPath } from 'node:url';
import { serveStdio } from '@modelcontextprotocol/server/stdio';
import type { RecordSource } from '../electron/catalog-contract.js';
import type { CheckoutInfo } from '../electron/checkout-contract.js';
import { createCheckoutStore } from '../electron/checkout-store.js';
import type { SourceSectionResult } from '../electron/source-section-contract.js';
import { createSourceSectionStore } from '../electron/source-section-store.js';
import type { GuideResult } from '../electron/system-guide-contract.js';
import { createSystemGuideStore } from '../electron/system-guide-store.js';
import { BUILD_VERSION } from './build-info.js';
import { createCatalogReader, nodeCatalogFileOperations } from './catalog-reader.js';
import { createCatalogServer } from './catalog-server.js';
import { checkCancelled } from './catalog-errors.js';

// All three paths are fixed relative to mcp-dist/mcp/main.js.
// No process arguments, environment, caller cwd or client roots select a path.
const guidePath = fileURLToPath(new URL('../../../records/system-guide.json', import.meta.url));
const repositoryRoot = fileURLToPath(new URL('../../../../', import.meta.url));
const reader = createCatalogReader({
  catalogPath: fileURLToPath(new URL('../../../records/catalog.json', import.meta.url)),
  fileOperations: nodeCatalogFileOperations,
});
const sourceStore = createSourceSectionStore({ repositoryRoot });
const checkoutStore = createCheckoutStore({ repositoryRoot });

async function readGuide(signal: AbortSignal): Promise<GuideResult> {
  checkCancelled(signal);
  // The app store's busy flag belongs to one read's lifetime. A new store
  // per MCP request preserves app validation without rejecting concurrency.
  const result = await createSystemGuideStore(guidePath).read();
  checkCancelled(signal);
  return result;
}

async function readSourceSection(source: RecordSource, signal: AbortSignal): Promise<SourceSectionResult> {
  checkCancelled(signal);
  const result = await sourceStore.read(source);
  checkCancelled(signal);
  return result;
}

async function readCheckout(signal: AbortSignal): Promise<CheckoutInfo> {
  checkCancelled(signal);
  const result = await checkoutStore.read();
  checkCancelled(signal);
  return result;
}

if (BUILD_VERSION === 'unbuilt') {
  process.stderr.write('Catalog MCP build is required.\n');
  process.exitCode = 1;
} else {
  const connection = serveStdio(() => createCatalogServer({
    readSnapshot: reader.readSnapshot,
    readGuide,
    readSourceSection,
    readCheckout,
    version: BUILD_VERSION,
  }), {
    onerror: () => { process.stderr.write('Catalog MCP transport error.\n'); },
  });
  const close = () => {
    void connection.close().catch(() => {
      process.stderr.write('Catalog MCP shutdown error.\n');
      process.exitCode = 1;
    });
  };
  process.once('SIGINT', close);
  process.once('SIGTERM', close);
}

import { fileURLToPath } from 'node:url';
import { serveStdio } from '@modelcontextprotocol/server/stdio';
import { BUILD_VERSION } from './build-info.js';
import { createCatalogReader, nodeCatalogFileOperations } from './catalog-reader.js';
import { createCatalogServer } from './catalog-server.js';

// mcp-dist/mcp/main.js and desktop-dist/main.js resolve the same 05 catalog.
// No process arguments, environment, caller cwd or client roots select a path.
const reader = createCatalogReader({
  catalogPath: fileURLToPath(new URL('../../../records/catalog.json', import.meta.url)),
  fileOperations: nodeCatalogFileOperations,
});
if (BUILD_VERSION === 'unbuilt') {
  process.stderr.write('Catalog MCP build is required.\n');
  process.exitCode = 1;
} else {
  const connection = serveStdio(() => createCatalogServer({ readSnapshot: reader.readSnapshot, version: BUILD_VERSION }), {
    onerror: () => { process.stderr.write('Catalog MCP transport error.\n'); },
  });
  const close = () => { void connection.close().catch(() => { process.stderr.write('Catalog MCP shutdown error.\n'); process.exitCode = 1; }); };
  process.once('SIGINT', close);
  process.once('SIGTERM', close);
}

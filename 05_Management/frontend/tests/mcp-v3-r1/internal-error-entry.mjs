// V3-R1 verifier-owned stdio entry for R06 (not a product entry). It assembles the BUILT product
// server factory (mcp-dist) like mcp/main.ts does, with a readSnapshot chosen by argv[2]:
//   ok           a small valid snapshot (normal path: no diagnostic expected)
//   domain       the product's CatalogReadError('CATALOG_MISSING') (mapped code: no diagnostic expected)
//   read-throw   an unexpected (non-CatalogReadError) error from the read step
//   query-throw  a snapshot whose catalog throws such an error while it is queried
// The unexpected errors carry sentinels in name/message/stack/code/path. This entry never writes
// to stdout or stderr itself; everything on stderr comes from the product.
import { serveStdio } from '@modelcontextprotocol/server/stdio';
import { CatalogReadError } from '../../mcp-dist/mcp/catalog-errors.js';
import { createCatalogServer } from '../../mcp-dist/mcp/catalog-server.js';

const mode = process.argv[2] ?? 'ok';
const SENTINEL = 'SENTINEL_R06';

function unexpectedError() {
  const error = new TypeError(`${SENTINEL}_MESSAGE reading C:\\${SENTINEL}_PATH\\catalog.json`);
  error.name = `${SENTINEL}_NAME`;
  error.stack = `${SENTINEL}_NAME: ${SENTINEL}_STACK\n    at read (C:\\${SENTINEL}_PATH\\module.js:1:1)`;
  return Object.assign(error, { code: `${SENTINEL}_CODE`, path: `C:\\${SENTINEL}_PATH\\catalog.json`, syscall: 'open' });
}

const metadata = { hash: 'a'.repeat(64), revision: 'r06-revision', asOf: '2026-10-02T00:00:00Z', sourceCommit: 'r06-source-commit' };
const catalog = { schemaVersion: 1, revision: metadata.revision, asOf: metadata.asOf, sourceCommit: metadata.sourceCommit, scopeNote: 'r06', sources: [], systems: [], records: [] };
const throwingCatalog = { ...catalog, get systems() { throw unexpectedError(); }, get records() { throw unexpectedError(); }, get sources() { throw unexpectedError(); } };

async function readSnapshot() {
  if (mode === 'domain') throw new CatalogReadError('CATALOG_MISSING');
  if (mode === 'read-throw') throw unexpectedError();
  if (mode === 'query-throw') return { metadata, catalog: throwingCatalog };
  return { metadata, catalog };
}

const connection = serveStdio(() => createCatalogServer({ readSnapshot, version: `v3-r1-${mode}` }));
process.once('SIGTERM', () => { void connection.close(); });

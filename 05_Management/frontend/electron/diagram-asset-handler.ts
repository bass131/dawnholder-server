import { readFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import { checkDiagramAssetRequest, diagramAssets } from './diagram-asset-contract.js';

const responseHeaders = { 'X-Content-Type-Options': 'nosniff', 'Cache-Control': 'no-store' };

function assetError(status: number, code: string): Response {
  return new Response(code, {
    status,
    headers: { ...responseHeaders, 'Content-Type': 'text/plain; charset=utf-8', 'X-Diagram-Error': code, ...(status === 405 ? { Allow: 'GET' } : {}) },
  });
}

// The file: directory URL must end in '/': URL resolution otherwise replaces its
// last segment. main supplies '../dist/'; only fixed asset names are resolved here.
export function createDiagramAssetHandler(directoryUrl: URL): (request: { url: string; method: string }) => Promise<Response> {
  const paths = new Map(Object.values(diagramAssets).map(asset => [asset.url, fileURLToPath(new URL(asset.fileName, directoryUrl))]));
  return async request => {
    const result = checkDiagramAssetRequest(request.url, request.method);
    if (!result.ok) return assetError(result.status, result.code);
    try {
      const bytes = await readFile(paths.get(result.asset.url)!);
      return new Response(new Uint8Array(bytes), { headers: { ...responseHeaders, 'Content-Type': result.asset.mime } });
    } catch (error: unknown) {
      const missing = typeof error === 'object' && error !== null && 'code' in error && error.code === 'ENOENT';
      return assetError(missing ? 404 : 500, missing ? 'asset-missing' : 'asset-read-failed');
    }
  };
}

export const diagramScheme = 'dh-diagram';
export const diagramOrigin = `${diagramScheme}://renderer`;
export const diagramAssets = {
  document: { url: `${diagramOrigin}/diagram-renderer.html`, fileName: 'diagram-renderer.html', mime: 'text/html; charset=utf-8' },
  script: { url: `${diagramOrigin}/diagram-renderer.js`, fileName: 'diagram-renderer.js', mime: 'text/javascript; charset=utf-8' },
  style: { url: `${diagramOrigin}/diagram-renderer.css`, fileName: 'diagram-renderer.css', mime: 'text/css; charset=utf-8' },
} as const;

export type DiagramAsset = typeof diagramAssets[keyof typeof diagramAssets];
export type DiagramAssetRequest = { ok: true; asset: DiagramAsset } | { ok: false; status: 404 | 405; code: 'asset-url-denied' | 'method-denied' };

export function checkDiagramAssetRequest(url: string, method: string): DiagramAssetRequest {
  if (method !== 'GET') return { ok: false, status: 405, code: 'method-denied' };
  // Match the received spelling: URL parsing must not turn a rejected input into an allowed route.
  const asset = Object.values(diagramAssets).find(item => item.url === url);
  return asset ? { ok: true, asset } : { ok: false, status: 404, code: 'asset-url-denied' };
}

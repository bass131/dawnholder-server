import { fileURLToPath } from 'node:url';
import react from '@vitejs/plugin-react';
import { defineConfig } from 'vitest/config';

// Native Node loads the actual TS source; the URL import keeps existing compiler
// options while the type-only .js specifier follows TypeScript's module mapping.
const { diagramOrigin } = await import(new URL('./electron/diagram-asset-contract.ts', import.meta.url).href) as typeof import('./electron/diagram-asset-contract.js');

export default defineConfig({
  base: './',
  plugins: [
    react(),
    {
      name: 'ui-build-module-evidence',
      apply: 'build',
      generateBundle(_options, bundle) {
        const graph = Array.from(this.getModuleIds(), id => {
          const info = this.getModuleInfo(id);
          return { id, importedIds: info?.importedIds ?? [], dynamicallyImportedIds: info?.dynamicallyImportedIds ?? [] };
        });
        const output = Object.values(bundle).filter(item => item.type === 'chunk').map(item => ({ fileName: item.fileName, moduleIds: item.moduleIds, modules: item.modules }));
        this.emitFile({ type: 'asset', fileName: 'ui-build-graph.json', source: JSON.stringify({ graph, output }, null, 2) });
      },
    },
    {
      name: 'production-content-security-policy',
      apply: 'build',
      transformIndexHtml() {
        return [{
          tag: 'meta',
          attrs: {
            'http-equiv': 'Content-Security-Policy',
            content: `default-src 'none'; script-src 'self'; style-src 'self'; img-src 'self'; font-src 'self'; connect-src 'none'; base-uri 'none'; form-action 'none'; frame-src 'self' ${diagramOrigin}; object-src 'none'`,
          },
          injectTo: 'head-prepend',
        }];
      },
    },
  ],
  build: { assetsInlineLimit: 0 },
  server: {
    host: '127.0.0.1',
    port: 5173,
    strictPort: true,
    cors: false,
    fs: {
      strict: true,
      allow: [fileURLToPath(new URL('.', import.meta.url))],
    },
  },
  preview: {
    host: '127.0.0.1',
    port: 4173,
    strictPort: true,
    cors: false,
  },
  test: {
    environment: 'jsdom',
    clearMocks: true,
    restoreMocks: true,
  },
});

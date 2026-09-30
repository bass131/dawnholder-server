import { fileURLToPath } from 'node:url';
import react from '@vitejs/plugin-react';
import { defineConfig } from 'vitest/config';

export default defineConfig({
  base: './',
  plugins: [
    react(),
    {
      name: 'production-content-security-policy',
      apply: 'build',
      transformIndexHtml() {
        return [{
          tag: 'meta',
          attrs: {
            'http-equiv': 'Content-Security-Policy',
            content: "default-src 'none'; script-src 'self'; style-src 'self'; img-src 'self'; connect-src 'none'; base-uri 'none'; form-action 'none'; frame-src 'none'; object-src 'none'",
          },
          injectTo: 'head-prepend',
        }];
      },
    },
  ],
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

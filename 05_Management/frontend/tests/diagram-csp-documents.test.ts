// @vitest-environment node
// Static policy tests for the two documents involved in diagram display (R-12/R-14, design-spec 8.3 isolation/CSP).
// Runtime CSP enforcement is observed separately in real Electron; these fix what each document declares.
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { describe, expect, it } from 'vitest';
import { diagramAssets, diagramOrigin } from '../electron/diagram-asset-contract';
import viteConfig from '../vite.config';

type Policy = Map<string, string[]>;
function parsePolicy(content: string): Policy {
  const policy: Policy = new Map();
  for (const part of content.split(';').map(item => item.trim()).filter(Boolean)) {
    const [name, ...sources] = part.split(/\s+/);
    expect(policy.has(name!), `duplicate directive ${name}`).toBe(false);
    policy.set(name!, sources);
  }
  return policy;
}
const rendererHtml = readFileSync(fileURLToPath(new URL('../diagram-renderer.html', import.meta.url)), 'utf8');
function attribute(tag: string, name: string): string | undefined {
  return new RegExp(`\\s${name}="([^"]*)"`, 'i').exec(tag)?.[1]?.replaceAll('&#39;', "'").replaceAll('&quot;', '"').replaceAll('&amp;', '&');
}

describe('diagram renderer document', () => {
  const metas = rendererHtml.match(/<meta\b[^>]*>/gi) ?? [];
  const cspTags = metas.filter(tag => /http-equiv="Content-Security-Policy"/i.test(tag));
  const policy = parsePolicy(attribute(cspTags[0] ?? '', 'content') ?? '');

  it('declares one CSP before any loaded resource', () => {
    expect(cspTags).toHaveLength(1);
    const cspAt = rendererHtml.indexOf(cspTags[0]!);
    for (const marker of ['<link', '<script', '<style']) {
      const at = rendererHtml.indexOf(marker);
      if (at >= 0) expect(cspAt, marker).toBeLessThan(at);
    }
  });

  it('closes every fetch directive except the fixed script, the fixed stylesheet and inline diagram style', () => {
    for (const name of ['default-src', 'connect-src', 'img-src', 'frame-src', 'object-src', 'base-uri', 'form-action']) expect(policy.get(name), name).toEqual(["'none'"]);
    expect(policy.get('script-src')).toEqual([diagramAssets.script.url]);
    expect(policy.get('style-src')?.slice().sort()).toEqual([diagramAssets.style.url, "'unsafe-inline'"].sort());
    expect([...policy.keys()].sort()).toEqual(['base-uri', 'connect-src', 'default-src', 'form-action', 'frame-src', 'img-src', 'object-src', 'script-src', 'style-src']);
    const all = [...policy.values()].flat();
    for (const loose of ["'unsafe-eval'", "'wasm-unsafe-eval'", "'self'", '*', 'data:', 'blob:', 'file:', 'dh-diagram:', diagramOrigin, `${diagramOrigin}/`]) expect(all, loose).not.toContain(loose);
  });

  it('loads only the fixed stylesheet and script, with no inline script or other external reference', () => {
    const links = rendererHtml.match(/<link\b[^>]*>/gi) ?? [];
    const scripts = rendererHtml.match(/<script\b[^>]*>([\s\S]*?)<\/script>/gi) ?? [];
    expect(links.map(tag => [attribute(tag, 'rel'), attribute(tag, 'href')])).toEqual([['stylesheet', diagramAssets.style.url]]);
    expect(scripts).toHaveLength(1);
    expect(attribute(scripts[0]!, 'src')).toBe(diagramAssets.script.url);
    expect(scripts[0]!.replace(/^<script\b[^>]*>/i, '').replace(/<\/script>$/i, '').trim()).toBe('');
    const references = [...rendererHtml.matchAll(/\s(?:src|href|action|data|poster|srcset)="([^"]*)"/gi)].map(match => match[1]);
    expect(references.sort()).toEqual([diagramAssets.script.url, diagramAssets.style.url].sort());
    expect(rendererHtml).not.toMatch(/\son[a-z]+=/i);
    expect(rendererHtml).not.toMatch(/<(?:iframe|object|embed|base|form)\b/i);
  });
});

describe('parent index document policy built by Vite', () => {
  type IndexTag = { tag: string; attrs: Record<string, string> };
  type HtmlPlugin = { name: string; transformIndexHtml?: () => IndexTag[] };
  const plugins = (viteConfig as { plugins?: unknown[] }).plugins?.flat() as HtmlPlugin[];
  const plugin = plugins.find(item => item?.name === 'production-content-security-policy');
  const tags = plugin?.transformIndexHtml?.() ?? [];
  const policy = parsePolicy(tags.find(tag => tag.attrs['http-equiv'] === 'Content-Security-Policy')?.attrs.content ?? '');

  it('adds only the fixed diagram origin to frame-src and keeps the rest of the parent policy closed', () => {
    expect(policy.get('frame-src')?.slice().sort()).toEqual(["'self'", diagramOrigin].sort());
    expect(policy.get('script-src')).toEqual(["'self'"]);
    expect(policy.get('style-src')).toEqual(["'self'"]);
    expect(policy.get('connect-src')).toEqual(["'none'"]);
    expect(policy.get('default-src')).toEqual(["'none'"]);
    const all = [...policy.values()].flat();
    for (const loose of ["'unsafe-inline'", "'unsafe-eval'", "'wasm-unsafe-eval'", '*', 'data:', 'blob:', 'dh-diagram:']) expect(all, loose).not.toContain(loose);
    for (const [name, sources] of policy) if (name !== 'frame-src') expect(sources, name).not.toContain(diagramOrigin);
  });

  it('uses the same shared origin as the renderer asset URLs', () => {
    for (const asset of Object.values(diagramAssets)) expect(asset.url.startsWith(`${diagramOrigin}/`), asset.url).toBe(true);
  });
});

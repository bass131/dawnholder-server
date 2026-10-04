export const MAX_GUIDE_BYTES = 2 * 1024 * 1024;
export const MAX_DIAGRAM_SOURCE_BYTES = 4 * 1024;
export const MAX_DIAGRAM_DESCRIPTION_BYTES = 2 * 1024;

export interface CodeMapping { path: string; kind: 'file' | 'directory'; namespace?: string; role: string; }
export interface SystemCard {
  id: string; parentId: string | null; title: string; summary: string; relatedSystemIds: string[];
  status?: 'code-present'; documentId?: string;
  codeReference?: { commitSha: string; mappings: CodeMapping[] };
}
export interface DiagramBlock { type: 'diagram'; id: string; format: 'mermaid'; source: string; title: string; description: string; }
export type GuideBlock = { type: 'paragraph'; text: string } | DiagramBlock;
export interface GuideSection { id: string; title: string; blocks: GuideBlock[]; }
export interface ImplementationDocument {
  id: string; cardId: string; title: string; sourceCommit: string;
  sourceRefs: { path: string; symbol: string }[]; sections: GuideSection[];
}
export interface SystemGuide {
  schemaVersion: 1; revision: string; asOf: string; coverage: 'representative-diagrams'; coverageNote: string;
  cards: SystemCard[]; documents: ImplementationDocument[];
}
export type GuideResult = { ok: true; guide: SystemGuide; version: string; bytes: number } |
  { ok: false; code: 'missing' | 'load' | 'changed' | 'invalid' | 'too-large' | 'denied' | 'busy'; message: string };
export interface GuideBridge { readGuide(): Promise<GuideResult>; }

const object = (v: unknown): v is Record<string, unknown> => typeof v === 'object' && v !== null && !Array.isArray(v);
const text = (v: unknown): v is string => typeof v === 'string' && v.trim().length > 0;
const id = (v: unknown): v is string => typeof v === 'string' && /^[a-z0-9]+(?:[.-][a-z0-9]+)*$/.test(v);
const sha = (v: unknown): v is string => typeof v === 'string' && /^[a-f0-9]{40}$/.test(v);
const bytes = (v: string) => new TextEncoder().encode(v).byteLength;
const unique = (items: unknown[]): boolean => items.every(v => object(v) && id(v.id)) && new Set(items.map(v => (v as { id: string }).id)).size === items.length;

export function isRepositoryPath(value: unknown): value is string {
  return typeof value === 'string' && value.length > 0 && !/[\\:*?\[\]{}#%\u0000-\u001f\u007f]/.test(value) &&
    value.split('/').every(part => part !== '' && part !== '.' && part !== '..') && !/^[a-z]+:/i.test(value);
}

// Structural and byte failures reject the whole snapshot. DSL failures belong to the diagram block.
export function readSystemGuide(value: unknown): SystemGuide | null {
  if (!object(value) || value.schemaVersion !== 1 || !text(value.revision) || !text(value.asOf) || value.coverage !== 'representative-diagrams' || !text(value.coverageNote)) return null;
  if (!Array.isArray(value.cards) || !Array.isArray(value.documents) || !unique(value.cards) || !unique(value.documents)) return null;
  for (const card of value.cards) {
    if (!object(card) || !id(card.id) || !(card.parentId === null || id(card.parentId)) || !text(card.title) || !text(card.summary) || !Array.isArray(card.relatedSystemIds) || !card.relatedSystemIds.every(id) || new Set(card.relatedSystemIds).size !== card.relatedSystemIds.length) return null;
    if (card.parentId === null) {
      if (card.documentId !== undefined || card.codeReference !== undefined || card.status !== undefined) return null;
    } else {
      if (card.status !== 'code-present' || !id(card.documentId) || !object(card.codeReference) || !sha(card.codeReference.commitSha) || !Array.isArray(card.codeReference.mappings)) return null;
      for (const mapping of card.codeReference.mappings) {
        if (!object(mapping) || !isRepositoryPath(mapping.path) || !['file', 'directory'].includes(String(mapping.kind)) || !text(mapping.role) || (mapping.namespace !== undefined && !text(mapping.namespace))) return null;
      }
      const paths = card.codeReference.mappings.map((m: { path: string }) => m.path);
      if (new Set(paths).size !== paths.length) return null;
    }
  }
  const diagramIds = new Set<string>();
  for (const doc of value.documents) {
    if (!object(doc) || !id(doc.id) || !id(doc.cardId) || !text(doc.title) || !sha(doc.sourceCommit) || !Array.isArray(doc.sourceRefs) || !doc.sourceRefs.length || !Array.isArray(doc.sections) || !doc.sections.length || !unique(doc.sections)) return null;
    if (!doc.sourceRefs.every(ref => object(ref) && isRepositoryPath(ref.path) && text(ref.symbol))) return null;
    for (const section of doc.sections) {
      if (!object(section) || !text(section.title) || !Array.isArray(section.blocks) || !section.blocks.length) return null;
      let diagrams = 0;
      for (const block of section.blocks) {
        if (!object(block)) return null;
        if (block.type === 'paragraph') { if (!text(block.text)) return null; }
        else if (block.type === 'diagram') {
          if (++diagrams > 1 || !id(block.id) || diagramIds.has(block.id) || block.format !== 'mermaid' || !text(block.title) || !text(block.source) || !text(block.description) || bytes(block.source) > MAX_DIAGRAM_SOURCE_BYTES || bytes(block.description) > MAX_DIAGRAM_DESCRIPTION_BYTES) return null;
          diagramIds.add(block.id);
        } else return null;
      }
    }
  }
  const guide = value as unknown as SystemGuide;
  for (const card of guide.cards) {
    if (card.parentId === null) continue;
    const parent = guide.cards.find(c => c.id === card.parentId);
    const doc = guide.documents.find(d => d.id === card.documentId);
    if (!parent || parent.parentId !== null || !doc || doc.cardId !== card.id || doc.sourceCommit !== card.codeReference?.commitSha) return null;
    if (!doc.sourceRefs.every(ref => card.codeReference!.mappings.some(m => m.kind === 'file' ? m.path === ref.path : ref.path === m.path || ref.path.startsWith(`${m.path}/`)))) return null;
  }
  if (!guide.documents.every(doc => guide.cards.some(card => card.parentId !== null && card.id === doc.cardId && card.documentId === doc.id))) return null;
  return guide;
}

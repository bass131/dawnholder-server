import type { SystemGuide } from '../../electron/system-guide-contract';
import { matchesQuery } from '../../electron/catalog-query';

export function searchGuide(guide: SystemGuide, query: string) {
  return guide.cards.filter(card => {
    const document = guide.documents.find(doc => doc.id === card.documentId);
    return matchesQuery(query, [card.id, card.title, card.summary, ...card.relatedSystemIds,
      ...(card.codeReference?.mappings.flatMap(mapping => [mapping.path, mapping.role, mapping.namespace ?? '']) ?? []),
      document?.title ?? '', ...(document?.sections.flatMap(section => [section.title, ...section.blocks.map(block => block.type === 'paragraph' ? block.text : `${block.title} ${block.description} ${block.source}`)]) ?? []),
    ]);
  });
}

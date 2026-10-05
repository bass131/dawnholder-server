export interface GuideLocation { parentId: string | null; cardId: string | null; query: string; }
export const rootLocation: GuideLocation = { parentId: null, cardId: null, query: '' };
export function parentLocation(location: GuideLocation): GuideLocation {
  return location.cardId ? { ...location, cardId: null } : { ...location, parentId: null };
}

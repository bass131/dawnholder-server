export type CatalogView = 'systems' | 'records' | 'sources';

export interface RecordSelection {
  kind: 'system' | 'record';
  id: string;
}

export interface ExplorerLocation {
  view: CatalogView;
  query: string;
  area: string;
  recordType: string;
  selection: RecordSelection | null;
}

export interface ReturnPoint {
  scrollLeft: number;
  scrollTop: number;
  focusKey: string | null;
  evidenceOpen: boolean;
}

export interface NavigationFrame {
  location: ExplorerLocation;
  returnPoint: ReturnPoint;
}

export interface ExplorerNavigation {
  current: NavigationFrame;
  history: NavigationFrame[];
}

function frame(location: ExplorerLocation): NavigationFrame {
  return {
    location,
    returnPoint: { scrollLeft: 0, scrollTop: 0, focusKey: null, evidenceOpen: false },
  };
}

export const initialNavigation: ExplorerNavigation = {
  current: frame({ view: 'systems', query: '', area: '', recordType: '', selection: null }),
  history: [],
};

export function navigateTo(
  navigation: ExplorerNavigation,
  location: ExplorerLocation,
  returnPoint: ReturnPoint,
): ExplorerNavigation {
  return {
    current: frame(location),
    history: [...navigation.history, { ...navigation.current, returnPoint }],
  };
}

export function goBack(navigation: ExplorerNavigation): ExplorerNavigation {
  const previous = navigation.history.at(-1);
  return previous
    ? { current: previous, history: navigation.history.slice(0, -1) }
    : navigation;
}

export function returnToList(navigation: ExplorerNavigation): ExplorerNavigation {
  for (let index = navigation.history.length - 1; index >= 0; index--) {
    const previous = navigation.history[index];
    if (previous && previous.location.selection === null) {
      return { current: previous, history: navigation.history.slice(0, index) };
    }
  }
  return {
    current: frame({ ...navigation.current.location, selection: null }),
    history: [],
  };
}

export function updateFilters(
  navigation: ExplorerNavigation,
  filters: Partial<Pick<ExplorerLocation, 'query' | 'area' | 'recordType'>>,
): ExplorerNavigation {
  return {
    current: frame({ ...navigation.current.location, ...filters, selection: null }),
    history: [],
  };
}

export function selectionFocusKey(selection: RecordSelection): string {
  return `${selection.kind}:${selection.id}`;
}

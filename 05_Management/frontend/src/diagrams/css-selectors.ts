const identifierCharacter = /[A-Za-z0-9_-]/;
const typeStart = /[A-Za-z]/;
const whitespace = /[\t\n\f\r ]/;

export function cssRootIdValid(id: string): boolean {
  return /^(?:[A-Za-z_]|-[A-Za-z_-])[A-Za-z0-9_-]*$/.test(id);
}

function identifierEnd(selector: string, start: number): number {
  let cursor = start;
  while (cursor < selector.length && identifierCharacter.test(selector[cursor]!)) {
    cursor += 1;
  }
  return cursor;
}

function attributeEnd(selector: string, start: number): number {
  const look = '[data-look="neo"]';
  if (selector.startsWith(look, start)) return start + look.length;

  const suffixStart = '[id$="-';
  if (!selector.startsWith(suffixStart, start)) return -1;
  const valueStart = start + suffixStart.length;
  const end = identifierEnd(selector, valueStart);
  if (end === valueStart || !selector.startsWith('"]', end)) return -1;
  return end + 2;
}

function compoundEnd(selector: string, start: number, allowType: boolean): number {
  let cursor = start;
  if (allowType && typeStart.test(selector[cursor] ?? '')) {
    cursor = identifierEnd(selector, cursor);
  }
  while (cursor < selector.length) {
    const token = selector[cursor];
    if (token === '.' || token === '#') {
      const end = identifierEnd(selector, cursor + 1);
      if (end === cursor + 1) return -1;
      cursor = end;
    } else if (token === '[') {
      cursor = attributeEnd(selector, cursor);
      if (cursor < 0) return -1;
    } else {
      break;
    }
  }
  return cursor === start ? -1 : cursor;
}

function selectorValid(selector: string, rootId: string, raw: boolean): boolean {
  let cursor = 0;
  if (selector.startsWith('svg#')) cursor = 3;
  else if (!raw) return false;

  if (selector[cursor] !== '#') return false;
  const idStart = cursor + 1;
  cursor = identifierEnd(selector, idStart);
  if (selector.slice(idStart, cursor) !== rootId) return false;

  // The first compound identifies an SVG root, even if an HTML host has the
  // same ID. An ID prefix alone would also match that HTML element. Raw #id
  // forms are inspected here, then the adapter adds the exact svg type.
  if (cursor < selector.length && '.#['.includes(selector[cursor]!)) {
    cursor = compoundEnd(selector, cursor, false);
    if (cursor < 0) return false;
  }

  let insideRoot = false;
  while (cursor < selector.length) {
    const beforeWhitespace = cursor;
    while (cursor < selector.length && whitespace.test(selector[cursor]!)) cursor += 1;
    if (cursor === selector.length) return true;

    const combinator = selector[cursor];
    if (combinator === '>' || combinator === '+' || combinator === '~') {
      // Siblings of the root escape its subtree. Siblings reached after a
      // descendant/child step still have a parent inside this same SVG.
      if (!insideRoot && combinator !== '>') return false;
      cursor += 1;
      while (cursor < selector.length && whitespace.test(selector[cursor]!)) cursor += 1;
    } else if (cursor === beforeWhitespace) {
      return false;
    }
    insideRoot = true;
    cursor = compoundEnd(selector, cursor, true);
    if (cursor < 0) return false;
  }
  return true;
}

// Each loop advances its cursor; each character belongs to one identifier or
// separator. This gives linear parsing work within the existing SVG byte cap,
// rather than a timer attempting to interrupt a synchronous/backtracking check.
// It is an algorithmic bound, not a wall-clock deadline. No escapes, namespaces,
// pseudo-classes, universal selectors, nesting or additional attributes enter.
export function scopedSelectorsValid(selectors: string, rootId: string, raw = false): boolean {
  return scopedSelectorCheck(rootId, raw)(selectors);
}

// A document prepares this once. Revalidating a long root ID for every tiny
// stylesheet would charge the same shared input repeatedly.
export function scopedSelectorCheck(rootId: string, raw: boolean): (selectors: string) => boolean {
  const validRootId = cssRootIdValid(rootId);
  return selectors => validRootId && selectorListValid(selectors, rootId, raw);
}

function selectorListValid(selectors: string, rootId: string, raw: boolean): boolean {
  let cursor = 0;
  while (cursor < selectors.length) {
    const comma = selectors.indexOf(',', cursor);
    const end = comma < 0 ? selectors.length : comma;
    const selector = selectors.slice(cursor, end).trim();
    if (!selectorValid(selector, rootId, raw)) return false;
    if (comma < 0) return true;
    cursor = comma + 1;
  }
  return false;
}

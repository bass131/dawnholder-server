const nameCharacter = /[A-Za-z0-9_:.-]/;
const spaceCharacter = /\s/;

// This is a lexical inspection copy, not an XML parser or a sanitizer. Every
// cursor advances, including malformed/unterminated tags. DOMParser still owns
// XML syntax and namespace validation; only original style names are neutralized.
export function inspectionSvg(svg: string, styleName: string): string | null {
  const pieces: string[] = [];
  let copied = 0;
  let cursor = 0;
  const rename = (start: number, end: number, replacement: string): void => {
    pieces.push(svg.slice(copied, start), replacement);
    copied = end;
  };
  while (cursor < svg.length) {
    if (svg[cursor] !== '<') {
      cursor += 1;
      continue;
    }
    const comment = svg.startsWith('<!--', cursor);
    const cdata = svg.startsWith('<![CDATA[', cursor);
    if (comment || cdata) {
      const terminator = comment ? '-->' : ']]>';
      const end = svg.indexOf(terminator, cursor + (comment ? 4 : 9));
      if (end < 0) return null;
      cursor = end + terminator.length;
      continue;
    }
    cursor += 1;
    if (svg[cursor] === '/') cursor += 1;
    const nameStart = cursor;
    while (cursor < svg.length && nameCharacter.test(svg[cursor]!)) cursor += 1;
    if (cursor === nameStart) return null;
    const name = svg.slice(nameStart, cursor);
    if (name === 'style' || name.endsWith(':style')) {
      rename(nameStart, cursor, name.slice(0, name.length - 5) + styleName);
    }
    while (cursor < svg.length && svg[cursor] !== '>') {
      const beforeSpace = cursor;
      while (cursor < svg.length && spaceCharacter.test(svg[cursor]!)) cursor += 1;
      if (svg[cursor] === '>') break;
      if (svg[cursor] === '/' && svg[cursor + 1] === '>') {
        cursor += 1;
        break;
      }
      if (cursor === beforeSpace) return null;
      const attrStart = cursor;
      while (cursor < svg.length && nameCharacter.test(svg[cursor]!)) cursor += 1;
      if (cursor === attrStart) return null;
      const attrEnd = cursor;
      while (cursor < svg.length && spaceCharacter.test(svg[cursor]!)) cursor += 1;
      if (svg[cursor] !== '=') return null;
      cursor += 1;
      while (cursor < svg.length && spaceCharacter.test(svg[cursor]!)) cursor += 1;
      const quote = svg[cursor];
      if (quote !== '"' && quote !== "'") return null;
      const end = svg.indexOf(quote, cursor + 1);
      if (end < 0) return null;
      if (svg.slice(attrStart, attrEnd) === 'style') rename(attrStart, attrEnd, styleName);
      cursor = end + 1;
    }
    if (svg[cursor] !== '>') return null;
    cursor += 1;
  }
  pieces.push(svg.slice(copied));
  return pieces.join('');
}

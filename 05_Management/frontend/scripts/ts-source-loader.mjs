// The read-only CLI uses the Electron modules directly. NodeNext's emitted
// .js specifiers resolve to their .ts sources only within a TypeScript parent.
export function resolve(specifier, context, nextResolve) {
  if (context.parentURL?.endsWith('.ts')
    && (specifier.startsWith('./') || specifier.startsWith('../'))
    && specifier.endsWith('.js')) {
    return nextResolve(`${specifier.slice(0, -3)}.ts`, context);
  }
  return nextResolve(specifier, context);
}

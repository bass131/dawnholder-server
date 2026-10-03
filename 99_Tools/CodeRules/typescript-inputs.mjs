import { lstat, readFile, readdir } from 'node:fs/promises';
import { dirname, extname, isAbsolute, relative, resolve, sep } from 'node:path';
import { hash, safePath } from './inputs.mjs';

function configJson(text) {
  // Keep string tokens intact in both passes so comment/comma syntax inside them survives.
  // JSONC cleanup only changes the in-memory input to JSON.parse.
  const stringLiteralPattern = /"(?:\\.|[^"\\])*"/.source;
  const outsideStringPatterns = [/[^"/]+/.source, /\//.source];
  const jsoncTokenPattern = new RegExp([
    stringLiteralPattern,
    /\/\*[\s\S]*?\*\//.source,
    /\/\/[^\r\n]*/.source,
    ...outsideStringPatterns,
  ].join('|'), 'g');
  const withoutBom = text.replace(/^\uFEFF/, '');
  const tokens = withoutBom.match(jsoncTokenPattern) ?? [];
  const withoutComments = tokens.map(token => {
    if (/^\/[/\*]/.test(token)) {
      return ' ';
    }
    return token;
  });
  const cleaned = withoutComments.join('');

  const jsonTokenPattern = new RegExp([
    stringLiteralPattern,
    ...outsideStringPatterns,
  ].join('|'), 'g');
  const parts = cleaned.match(jsonTokenPattern) ?? [];
  const withoutTrailingCommas = parts.map(token => {
    if (token.startsWith('"')) {
      return token;
    }
    return token.replace(/,\s*(?=[}\]])/g, '');
  });
  return JSON.parse(withoutTrailingCommas.join(''));
}

function decodeLiteral(text) {
  const escapePattern = /\\(?:u\{([\da-f]+)\}|u([\da-f]{4})|x([\da-f]{2})|\r?\n|(.))/gi;
  const escapedCharacters = {
    n: '\n',
    r: '\r',
    t: '\t',
    b: '\b',
    f: '\f',
    v: '\v',
    0: '\0',
  };
  return text.replace(
    escapePattern,
    (match, point, unicode, hex, escaped) => {
      if (point || unicode || hex) {
        const codePoint = Number.parseInt(point ?? unicode ?? hex, 16);
        return String.fromCodePoint(codePoint);
      }
      if (!escaped) {
        return '';
      }
      return escapedCharacters[escaped] ?? escaped;
    },
  );
}

export async function preflightTypeScript(root, frontendRoot, configNames) {
  const files = new Map();
  const visitedConfigs = new Set();
  const frontend = resolve(root, frontendRoot);
  const relativePath = absolute => {
    const local = relative(root, absolute);
    if (isAbsolute(local) || local === '..' || local.startsWith(`..${sep}`)) {
      throw new Error(`TypeScript input escapes repository: ${absolute}`);
    }
    return local.split(sep).join('/');
  };
  const inspect = async absolute => {
    const path = relativePath(absolute);
    const bytes = await readFile(await safePath(root, path));
    files.set(path, { path, sha256: hash(bytes), bytes: bytes.length });
    return bytes.toString('utf8');
  };
  const checkPath = async (directory, name, optionalCandidate = false) => {
    if (typeof name !== 'string' || name.includes('\0')) throw new Error('Invalid TypeScript path input.');
    if (/^[a-z]:[\\/]/i.test(name) && process.platform !== 'win32') {
      throw new Error('Foreign absolute TypeScript path.');
    }
    const absolute = resolve(directory, name.replaceAll('\\', '/'));
    const path = relativePath(absolute);
    // Validate existing parents as well as the final file; no compiler read precedes link checks.
    const parts = path.split('/');
    for (let length = 1; length <= parts.length; length++) {
      try {
        await safePath(root, parts.slice(0, length).join('/'));
      } catch (error) {
        if (error.code !== 'ENOENT' && !(optionalCandidate && error.code === 'ENOTDIR')) {
          throw error;
        }
        if (optionalCandidate) {
          // Candidate absence is platform-dependent; required inspect reads still propagate errors.
          return null;
        }
        break;
      }
    }
    return absolute;
  };
  const scanConfig = async absolute => {
    if (visitedConfigs.has(absolute)) return;
    visitedConfigs.add(absolute);
    const config = configJson(await inspect(absolute));
    const directory = dirname(absolute);
    const options = config.compilerOptions ?? {};
    const paths = [
      ...(config.files ?? []), ...(config.include ?? []), ...(config.exclude ?? []),
      ...(options.rootDirs ?? []), ...(options.typeRoots ?? []),
      ...Object.values(options.paths ?? {}).flat(),
      ...[options.baseUrl, options.rootDir].filter(value => value !== undefined),
      ...(config.references ?? []).map(reference => reference.path),
    ];
    for (const path of paths) await checkPath(directory, path);
    for (const parent of [config.extends].flat().filter(Boolean)) {
      const resolved = parent.startsWith('.') || isAbsolute(parent)
        ? await checkPath(directory, parent)
        : await checkPath(frontend, `node_modules/${parent}`);
      try {
        await scanConfig(resolved.endsWith('.json') ? resolved : `${resolved}.json`);
      } catch (error) {
        if (error.code !== 'ENOENT') throw error;
        await scanConfig(resolve(resolved, 'tsconfig.json'));
      }
    }
  };
  const scanned = new Set();
  const scanSource = async absolute => {
    if (scanned.has(absolute)) {
      return;
    }
    scanned.add(absolute);
    const text = await inspect(absolute);
    const imports = [];
    const blockCommentPattern = /\/\*[\s\S]*?\*\//.source;
    const lineCommentPattern = /\/\/[^\r\n]*/.source;
    const singleQuotedPattern = /'((?:\\[\s\S]|[^'\\])*)'/.source;
    const doubleQuotedPattern = /"((?:\\[\s\S]|[^"\\])*)"/.source;
    const templatePattern = /`(?:\\[\s\S]|[^`\\])*`/.source;
    const tokenPattern = new RegExp([
      blockCommentPattern,
      lineCommentPattern,
      singleQuotedPattern,
      doubleQuotedPattern,
      templatePattern,
    ].join('|'), 'g');
    // Recognize quoted import/export ... from, side-effect import, import() and require(),
    // plus reference path directives below. The prefix lookback is limited to 100 characters.
    // This regex approximation can miss computed/template imports, intervening comments
    // and long gaps; it does not reproduce the compiler's syntax or module resolution.
    const importPrefixPattern = /(?:\bfrom|\bimport|\brequire)\s*(?:\(\s*)?$/;
    for (const token of text.matchAll(tokenPattern)) {
      if (token[1] === undefined && token[2] === undefined) {
        continue;
      }
      const prefix = text.slice(Math.max(0, token.index - 100), token.index);
      if (importPrefixPattern.test(prefix)) {
        imports.push(decodeLiteral(token[1] ?? token[2]));
      }
    }
    for (const reference of text.matchAll(/\/\/\/\s*<reference\s+path\s*=\s*['"]([^'"]+)['"]/g)) {
      imports.push(reference[1]);
    }
    for (const name of imports) {
      let candidate = name;
      try {
        const isPackageImport = !name.startsWith('.') &&
          !name.startsWith('\\') &&
          !isAbsolute(name) &&
          !/^[a-z]:[\\/]/i.test(name);
        if (isPackageImport) {
          if (name.split(/[\\/]/).includes('..')) {
            candidate = resolve(frontend, `node_modules/${name}`);
            await checkPath(frontend, `node_modules/${name}`);
          }
          continue;
        }
        candidate = resolve(dirname(absolute), name.replaceAll('\\', '/'));
        const referenced = candidate;
        const existingReference = await checkPath(dirname(absolute), name, true);

        const nonSourceImport = extname(referenced) !== '' &&
          !/\.(ts|tsx|cts|mts|js|jsx|cjs|mjs)$/.test(referenced);
        if (nonSourceImport) {
          if (existingReference === null) {
            continue;
          }
          // Assets remain checked/hashed inputs; dotted directory names still get index candidates.
          if (!(await lstat(existingReference)).isDirectory()) {
            await inspect(referenced);
            continue;
          }
        }

        const stem = referenced.replace(/\.(js|jsx|cjs|mjs)$/, '');
        const candidates = [
          referenced,
          ...['.ts', '.tsx', '.cts', '.mts', '.d.ts', '/index.ts'].map(ext => `${stem}${ext}`),
        ];
        for (candidate of candidates) {
          if (!/\.(ts|tsx|cts|mts)$/.test(candidate)) {
            continue;
          }
          if (await checkPath(dirname(absolute), candidate, true) === null) {
            continue;
          }
          await scanSource(candidate);
        }
      } catch (error) {
        throw new Error(
          `TypeScript input preflight failed in source ${JSON.stringify(relativePath(absolute))} ` +
          `for import/reference ${JSON.stringify(name)} at candidate ${JSON.stringify(candidate)}: ` +
          `${error.message}. Correct the import path or restore a readable repository input without symlinks.`,
          { cause: error },
        );
      }
    }
  };
  const visit = async directory => {
    for (const entry of await readdir(directory, { withFileTypes: true })) {
      if (['node_modules', 'dist', 'desktop-dist', 'mcp-dist', '.git'].includes(entry.name)) continue;
      const file = resolve(directory, entry.name);
      await safePath(root, relativePath(file));
      if (entry.isDirectory()) await visit(file);
      else if (/\.(ts|tsx|cts|mts)$/.test(entry.name)) await scanSource(file);
    }
  };
  for (const name of configNames) await scanConfig(resolve(frontend, name));
  await visit(frontend);
  return [...files.values()];
}

// Parse with CodeGraph's own bundled C# grammar. This supplies lexical context
// only; it neither binds symbols nor creates relationships.
const fs = require('node:fs');
const path = require('node:path');

async function main() {
    const [bundle, root, manifestPath, output] = process.argv.slice(2);
    if (!output) throw new Error('Expected bundle root manifest output');
    const grammars = require(path.join(bundle, 'lib/dist/extraction/grammars.js'));
    const { blankCsharpPreprocessorDirectives } = require(path.join(bundle, 'lib/dist/extraction/languages/csharp.js'));
    await grammars.initGrammars();
    await grammars.loadGrammarsForLanguages(['csharp']);
    const parser = grammars.getParser('csharp');
    if (!parser) throw new Error('Bundled C# parser unavailable');
    const manifest = JSON.parse(fs.readFileSync(manifestPath, 'utf8'));
    const occurrences = [];
    const diagnostics = [];
    for (const file of manifest.files.filter(file => file.path.endsWith('.cs'))) {
        const source = blankCsharpPreprocessorDirectives(fs.readFileSync(path.join(root, file.path), 'utf8'));
        const tree = parser.parse(source);
        if (!tree) throw new Error(`Parse failed: ${file.path}`);
        if (tree.rootNode.hasError) diagnostics.push({ path: file.path, kind: 'syntaxError' });
        function visit(node, owner, deferred) {
            if (['method_declaration', 'constructor_declaration', 'local_function_statement'].includes(node.type)) {
                owner = { line: node.startPosition.row + 1, name: node.childForFieldName('name')?.text ?? null };
                deferred = false;
            }
            if (['lambda_expression', 'anonymous_method_expression'].includes(node.type)) deferred = true;
            if (owner && ['invocation_expression', 'object_creation_expression'].includes(node.type)) {
                occurrences.push({ path: file.path, line: node.startPosition.row + 1, column: node.startPosition.column + 1, owner, syntaxKind: node.type, context: deferred ? 'deferredLambda' : 'direct' });
            }
            for (const child of node.namedChildren) visit(child, owner, deferred);
        }
        visit(tree.rootNode, null, false);
        tree.delete();
    }
    fs.writeFileSync(output, JSON.stringify({ rawVersion: 1, parser: 'CodeGraph bundled C# tree-sitter; no symbol binding', occurrences, diagnostics }, null, 2) + '\n');
}

main().catch(error => {
    console.error(error);
    process.exitCode = 1;
});

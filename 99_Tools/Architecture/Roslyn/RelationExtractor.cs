using System.Reflection;
using System.Security.Cryptography;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Dawnholder.Tools.Architecture;

internal sealed record SourcePosition(string Path, int Line, int Column);
internal sealed record RawSymbol(string Key, string Kind, string Name, string Namespace, string Signature, string? DocumentationId, SourcePosition? Source, string Resolution);
internal sealed record RawRelation(string SourceKey, string? TargetKey, string Kind, SourcePosition? Evidence, string Context, string Resolution, string? TargetText, string[] Candidates);

internal sealed class RelationExtractor(string root, HashSet<string> files)
{
    private readonly Dictionary<string, HashSet<string>> _documentationKeys = new(StringComparer.Ordinal);

    public Dictionary<string, RawSymbol> Symbols { get; } = new(StringComparer.Ordinal);

    public List<RawRelation> Relations { get; } = [];

    public HashSet<string> CoveredFiles { get; } = new(StringComparer.Ordinal);

    public SourcePosition? Location(Location location)
    {
        if (!location.IsInSource) return null;
        var span = location.GetLineSpan();
        var path = Path.GetRelativePath(root, span.Path).Replace('\\', '/');
        if (!files.Contains(path)) return null;
        return new SourcePosition(path, span.StartLinePosition.Line + 1, span.StartLinePosition.Character + 1);
    }

    public void Index(Compilation compilation)
    {
        foreach (var tree in InputTrees(compilation))
        {
            CoveredFiles.Add(Path.GetRelativePath(root, tree.FilePath).Replace('\\', '/'));
            var model = compilation.GetSemanticModel(tree);
            foreach (var node in tree.GetRoot().DescendantNodes().Where(IsDeclaration))
            {
                var symbol = model.GetDeclaredSymbol(node);
                if (symbol is INamedTypeSymbol or IMethodSymbol) AddSymbol(symbol);
            }
        }
    }

    public void Extract(Compilation compilation)
    {
        foreach (var tree in InputTrees(compilation))
        {
            var model = compilation.GetSemanticModel(tree);
            var syntax = tree.GetRoot();
            foreach (var declaration in syntax.DescendantNodes().Where(IsDeclaration))
            {
                var symbol = model.GetDeclaredSymbol(declaration);
                if (symbol is not (INamedTypeSymbol or IMethodSymbol)) continue;
                var target = AddSymbol(symbol);
                if (symbol.ContainingType is { } containing)
                {
                    var parent = AddSymbol(containing);
                    Relations.Add(new RawRelation(parent, target, "contains", Location(declaration.GetLocation()), "declaration", "resolved", null, []));
                }
                if (declaration is BaseTypeDeclarationSyntax { BaseList: { } baseList })
                {
                    foreach (var baseType in baseList.Types)
                    {
                        var info = model.GetSymbolInfo(baseType.Type);
                        if (info.Symbol is INamedTypeSymbol { TypeKind: TypeKind.Interface } iface)
                        {
                            AddRelation(target, iface, "implements", baseType, "declaration");
                        }
                        else if (info.Symbol is null && info.CandidateSymbols.Length > 0)
                        {
                            Unresolved(target, "implements", baseType, "declaration", info);
                        }
                    }
                }
            }
            foreach (var invocation in syntax.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                var owner = Owner(model, invocation);
                if (owner is null) continue;
                var info = model.GetSymbolInfo(invocation);
                var context = Context(invocation);
                if (info.Symbol is IMethodSymbol method)
                {
                    AddRelation(AddSymbol(owner), method.ReducedFrom ?? method.OriginalDefinition, "calls", invocation, context);
                }
                else Unresolved(AddSymbol(owner), "calls", invocation, context, info);
            }
            foreach (var typeSyntax in syntax.DescendantNodes().OfType<TypeSyntax>())
            {
                var methodSyntax = typeSyntax.Ancestors().FirstOrDefault(n => n is BaseMethodDeclarationSyntax or LocalFunctionStatementSyntax);
                if (methodSyntax is null || !InBody(methodSyntax, typeSyntax)) continue;
                var owner = Owner(model, typeSyntax);
                if (owner is null) continue;
                var info = model.GetSymbolInfo(typeSyntax);
                var type = info.Symbol is IAliasSymbol alias ? alias.Target as INamedTypeSymbol : info.Symbol as INamedTypeSymbol;
                if (type is { TypeKind: not TypeKind.Error })
                {
                    AddRelation(AddSymbol(owner), type.OriginalDefinition, "usesType", typeSyntax, Context(typeSyntax));
                }
                else if (type?.TypeKind == TypeKind.Error || info.CandidateSymbols.Length > 0)
                {
                    Unresolved(AddSymbol(owner), "usesType", typeSyntax, Context(typeSyntax), info);
                }
            }
        }
    }

    public object CompilerEvidence(string name, Compilation compilation)
    {
        return new
        {
            name,
            compilation.AssemblyName,
            options = new { outputKind = compilation.Options.OutputKind.ToString(), optimization = compilation.Options.OptimizationLevel.ToString(), compilation.Options.Platform, nullable = ((CSharpCompilationOptions)compilation.Options).NullableContextOptions.ToString(), allowUnsafe = ((CSharpCompilationOptions)compilation.Options).AllowUnsafe },
            parseOptions = compilation.SyntaxTrees.Select(t => new { path = Path.GetRelativePath(root, t.FilePath).Replace('\\', '/'), languageVersion = ((CSharpParseOptions)t.Options).LanguageVersion.ToString(), symbols = ((CSharpParseOptions)t.Options).PreprocessorSymbolNames.Order(StringComparer.Ordinal).ToArray(), documentationMode = t.Options.DocumentationMode.ToString() }),
            references = compilation.References.Select(reference => reference is PortableExecutableReference pe && pe.FilePath is { } path
                ? (object)new { kind = "file", path, version = AssemblyName.GetAssemblyName(path).Version?.ToString(), sha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path))) }
                : new { kind = "compilation", display = reference.Display, assembly = (reference as CompilationReference)?.Compilation.AssemblyName }),
            compiler = new { path = typeof(CSharpCompilation).Assembly.Location, version = typeof(CSharpCompilation).Assembly.GetName().Version?.ToString(), sha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(typeof(CSharpCompilation).Assembly.Location))) },
        };
    }

    private static bool IsDeclaration(SyntaxNode node) => node is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax or BaseMethodDeclarationSyntax or LocalFunctionStatementSyntax;

    private static bool InBody(SyntaxNode method, SyntaxNode node)
    {
        return method switch
        {
            BaseMethodDeclarationSyntax m => (m.Body?.Span.Contains(node.Span) ?? false) || (m.ExpressionBody?.Span.Contains(node.Span) ?? false),
            LocalFunctionStatementSyntax m => (m.Body?.Span.Contains(node.Span) ?? false) || (m.ExpressionBody?.Span.Contains(node.Span) ?? false),
            _ => false,
        };
    }

    private static IMethodSymbol? Owner(SemanticModel model, SyntaxNode node)
    {
        var symbol = model.GetEnclosingSymbol(node.SpanStart);
        while (symbol is IMethodSymbol { MethodKind: MethodKind.AnonymousFunction }) symbol = symbol.ContainingSymbol;
        return symbol as IMethodSymbol;
    }

    private static string Context(SyntaxNode node) => node.Ancestors().Any(n => n is AnonymousFunctionExpressionSyntax) ? "deferredLambda" : "direct";

    private IEnumerable<SyntaxTree> InputTrees(Compilation compilation) => compilation.SyntaxTrees.Where(t => files.Contains(Path.GetRelativePath(root, t.FilePath).Replace('\\', '/')));

    private string AddSymbol(ISymbol symbol)
    {
        symbol = symbol.OriginalDefinition;
        var docId = symbol.GetDocumentationCommentId();
        var source = symbol.Locations.Select(Location).Where(l => l is not null).OrderBy(l => l!.Path, StringComparer.Ordinal).ThenBy(l => l!.Line).FirstOrDefault();
        if (source is null && docId is not null && _documentationKeys.TryGetValue(docId, out var keys) && keys.Count == 1) return keys.Single();
        var signature = symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
        var key = $"{docId ?? signature}|{source?.Path ?? "@external"}";
        if (!Symbols.ContainsKey(key))
        {
            Symbols.Add(key, new RawSymbol(key, symbol is IMethodSymbol ? "method" : "type", symbol.Name, symbol.ContainingNamespace?.ToDisplayString() ?? "", signature, docId, source, source is null ? "external" : "resolved"));
            if (source is not null && docId is not null)
            {
                if (!_documentationKeys.TryGetValue(docId, out keys)) _documentationKeys[docId] = keys = [];
                keys.Add(key);
            }
        }
        return key;
    }

    private void AddRelation(string source, ISymbol target, string kind, SyntaxNode evidence, string context)
    {
        var targetKey = AddSymbol(target);
        // Error symbols and duplicate metadata identities cannot masquerade as
        // resolved targets. Keep evidence instead of guessing among names.
        var docId = target.OriginalDefinition.GetDocumentationCommentId();
        if (target is ITypeSymbol { TypeKind: TypeKind.Error })
        {
            Relations.Add(new RawRelation(source, null, kind, Location(evidence.GetLocation()), context, "unresolved", target.ToDisplayString(), []));
        }
        else if (Symbols[targetKey].Source is null && docId is not null && _documentationKeys.TryGetValue(docId, out var keys) && keys.Count > 1)
        {
            Relations.Add(new RawRelation(source, null, kind, Location(evidence.GetLocation()), context, "ambiguous", target.ToDisplayString(), keys.Order(StringComparer.Ordinal).ToArray()));
        }
        else Relations.Add(new RawRelation(source, targetKey, kind, Location(evidence.GetLocation()), context, "resolved", null, []));
    }

    private void Unresolved(string source, string kind, SyntaxNode evidence, string context, SymbolInfo info)
    {
        Relations.Add(new RawRelation(source, null, kind, Location(evidence.GetLocation()), context, info.CandidateSymbols.Length > 1 ? "ambiguous" : "unresolved", evidence.ToString(), info.CandidateSymbols.Select(AddSymbol).Order(StringComparer.Ordinal).ToArray()));
    }
}

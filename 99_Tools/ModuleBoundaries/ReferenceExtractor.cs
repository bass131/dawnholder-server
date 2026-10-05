using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Dawnholder.Tools.Architecture.Boundaries;

// Resolve symbols first, then classify their original source declarations.
// No using-text, namespace substring, comment or string scanning is performed.
public sealed class ReferenceExtractor
{
    private readonly Policy policy;
    private readonly Dictionary<string, TargetSpot> declarations = new(StringComparer.Ordinal);
    private readonly HashSet<string> occurrences = new(StringComparer.Ordinal);

    public ReferenceExtractor(Policy policy)
    {
        this.policy = policy;
    }

    private static ISymbol Original(ISymbol symbol)
    {
        if (symbol is IAliasSymbol alias)
        {
            symbol = alias.Target;
        }
        if (symbol is IMethodSymbol method && method.ReducedFrom != null)
        {
            symbol = method.ReducedFrom;
        }
        return symbol.OriginalDefinition;
    }

    private static string Identity(ISymbol symbol)
    {
        symbol = Original(symbol);
        return symbol.ContainingAssembly?.Identity.Name + ":" +
            (symbol.GetDocumentationCommentId() ?? symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
    }

    private void IndexType(INamedTypeSymbol type)
    {
        var location = type.Locations.FirstOrDefault(candidate => candidate.IsInSource);
        if (location == null)
        {
            return;
        }
        var spot = policy.Spot(location);
        var area = policy.Area(spot.Path);
        if (area == null)
        {
            return;
        }
        var typeName = type.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
        declarations[Identity(type)] = new TargetSpot(typeName, typeName, area, spot);
        foreach (var member in type.GetMembers())
        {
            var memberLocation = member.Locations.FirstOrDefault(candidate => candidate.IsInSource) ?? location;
            declarations[Identity(member)] = new TargetSpot(typeName,
                member.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat), area, policy.Spot(memberLocation));
        }
    }

    public void Extract(List<Compilation> compilations, SourceManifest manifest,
        AnalysisResult result, CancellationToken cancellation)
    {
        var phase = Stopwatch.StartNew();
        foreach (var compilation in compilations)
        {
            foreach (var tree in compilation.SyntaxTrees)
            {
                var model = compilation.GetSemanticModel(tree);
                foreach (var declaration in tree.GetRoot(cancellation).DescendantNodes()
                    .Where(node => node is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax))
                {
                    if (model.GetDeclaredSymbol(declaration, cancellation) is INamedTypeSymbol type)
                    {
                        IndexType(type);
                    }
                }
            }
        }
        var expected = manifest.ExpectedBoundaryFiles.ToHashSet(StringComparer.Ordinal);
        var analyzed = new HashSet<string>(StringComparer.Ordinal);
        foreach (var compilation in compilations)
        {
            foreach (var tree in compilation.SyntaxTrees)
            {
                var relative = policy.Relative(tree.FilePath);
                if (!expected.Contains(relative))
                {
                    continue;
                }
                var model = compilation.GetSemanticModel(tree);
                var area = policy.Area(relative)!;
                foreach (var node in tree.GetRoot(cancellation).DescendantNodes())
                {
                    cancellation.ThrowIfCancellationRequested();
                    // Names cover aliases, signatures, fields, generic arguments and
                    // named calls. Non-name operations expose implicit/user operators.
                    if (node is not (SimpleNameSyntax or ImplicitObjectCreationExpressionSyntax
                        or BinaryExpressionSyntax or PrefixUnaryExpressionSyntax
                        or PostfixUnaryExpressionSyntax or ElementAccessExpressionSyntax
                        or InvocationExpressionSyntax))
                    {
                        continue;
                    }
                    var symbol = model.GetSymbolInfo(node, cancellation).Symbol;
                    if (node is IdentifierNameSyntax identifier && model.GetAliasInfo(identifier, cancellation) is { } alias)
                    {
                        symbol = alias.Target;
                    }
                    if (symbol is INamedTypeSymbol or IMethodSymbol or IPropertySymbol or IFieldSymbol or IEventSymbol)
                    {
                        // Named invocations have their member occurrence at the name.
                        if (node is not InvocationExpressionSyntax)
                        {
                            Observe(symbol, node, area, "symbol", result);
                        }
                    }
                    // Inferred values (including var/session-returned map values)
                    // are still static type dependencies; namespaces have no type.
                    var type = model.GetTypeInfo(node, cancellation).Type;
                    if (type != null)
                    {
                        ObserveType(type, node, area, result);
                    }
                }
                analyzed.Add(relative);
            }
        }
        result.Coverage.AnalyzedFiles.AddRange(analyzed.Order(StringComparer.Ordinal));
        result.Coverage.AnalyzedBoundaryFiles = analyzed.Count;
        if (!analyzed.SetEquals(expected) || analyzed.Count == 0)
        {
            throw new InvalidOperationException("Boundary analysis coverage is incomplete or empty");
        }
        result.References.Sort((left, right) => Compare(left.Source, right.Source));
        result.Violations.Sort((left, right) => Compare(left.Source, right.Source));
        result.PhaseSeconds["referencesAndPolicy"] = phase.Elapsed.TotalSeconds;
    }

    private static int Compare(SourceSpot left, SourceSpot right)
    {
        var path = string.Compare(left.Path, right.Path, StringComparison.Ordinal);
        return path != 0 ? path : left.Offset.CompareTo(right.Offset);
    }

    private void ObserveType(ITypeSymbol type, SyntaxNode node, string sourceArea, AnalysisResult result)
    {
        // A returned array/container can expose Maps without naming Maps in the
        // caller. Traverse its statically resolved element/type arguments too.
        switch (type)
        {
            case IArrayTypeSymbol array:
                ObserveType(array.ElementType, node, sourceArea, result);
                break;
            case IPointerTypeSymbol pointer:
                ObserveType(pointer.PointedAtType, node, sourceArea, result);
                break;
            case INamedTypeSymbol named:
                Observe(named, node, sourceArea, "valueType", result);
                foreach (var argument in named.TypeArguments)
                {
                    ObserveType(argument, node, sourceArea, result);
                }
                if (named.ContainingType != null)
                {
                    ObserveType(named.ContainingType, node, sourceArea, result);
                }
                break;
        }
    }

    private void Observe(ISymbol symbol, SyntaxNode node, string sourceArea, string kind, AnalysisResult result)
    {
        if (!declarations.TryGetValue(Identity(symbol), out var target))
        {
            return;
        }
        var source = policy.Spot(node.GetLocation());
        var key = $"{source.Path}:{source.Offset}:{source.Length}:{Identity(symbol)}";
        if (!occurrences.Add(key))
        {
            return;
        }
        var reference = new StaticReference(sourceArea, source, target, kind);
        result.References.Add(reference);
        if (policy.Evaluate(reference) is { } violation)
        {
            result.Violations.Add(violation);
        }
    }
}

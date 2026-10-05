using Microsoft.CodeAnalysis;

namespace Dawnholder.Tools.Architecture.Boundaries;

// Physical source/declaration folders own classification; namespaces alone do not.
public sealed class Policy
{
    private readonly string root;
    private readonly BoundaryPolicy document;

    public Policy(string root, BoundaryPolicy document)
    {
        this.root = Path.GetFullPath(root);
        this.document = document;
    }

    public string Relative(string path)
    {
        var relative = Path.GetRelativePath(root, Path.GetFullPath(path)).Replace('\\', '/');
        if (relative == ".." || relative.StartsWith("../", StringComparison.Ordinal) || Path.IsPathRooted(relative))
        {
            throw new InvalidOperationException($"Workspace input leaves the fixed snapshot: {path}");
        }
        return relative;
    }

    public string? Area(string path)
    {
        foreach (var area in document.Areas)
        {
            if (path.StartsWith(area.Value + "/", StringComparison.Ordinal))
            {
                return area.Key;
            }
        }
        return null;
    }

    public SourceSpot Spot(Location location)
    {
        var line = location.GetLineSpan();
        return new SourceSpot(Relative(line.Path), line.StartLinePosition.Line + 1,
            line.StartLinePosition.Character + 1, location.SourceSpan.Start, location.SourceSpan.Length);
    }

    public BoundaryViolation? Evaluate(StaticReference reference)
    {
        var rule = document.Rules.SingleOrDefault(candidate =>
            candidate.From == reference.SourceArea && candidate.To == reference.Target.Area);
        if (rule == null || rule.AllowedTypes.Contains(reference.Target.Type, StringComparer.Ordinal))
        {
            return null;
        }
        var message = $"{rule.From} -> {rule.To}: {reference.Target.Symbol}. {rule.Reason}";
        return new BoundaryViolation(rule.Id, document.Version, rule.Severity, rule.From,
            rule.To, reference.Source, reference.Target, message, rule.Repair);
    }
}

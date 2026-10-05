using System.Text.Json;

namespace Dawnholder.Tools.Architecture.Boundaries;

public sealed record SourceSpot(string Path, int Line, int Column, int Offset, int Length);

public sealed record TargetSpot(string Type, string Symbol, string Area, SourceSpot Declaration);

public sealed record StaticReference(string SourceArea, SourceSpot Source, TargetSpot Target, string ReferenceKind);

public sealed record BoundaryViolation(string RuleId, string Version, string Severity,
    string From, string To, SourceSpot Source, TargetSpot Target, string Message, string Repair);

public sealed record AnalysisDiagnostic(string Kind, string Id, string Severity, string Message, SourceSpot? Source);

public sealed record BoundaryRule(string Id, string From, string To, string Severity,
    string[] AllowedTypes, string Reason, string Repair, string[] Sources);

public sealed record BoundaryPolicy(int SchemaVersion, string Version, string Project,
    Dictionary<string, string> Areas, BoundaryRule[] Rules, string OccurrenceUnit, string[] Exclusions);

public sealed record SourceManifest(string[] Projects, string[] SourceFiles, string[] ExpectedBoundaryFiles);

public sealed class AnalysisCoverage
{
    public int InputSourceFiles { get; set; }

    public int CompiledInputFiles { get; set; }

    public int ExpectedBoundaryFiles { get; set; }

    public int AnalyzedBoundaryFiles { get; set; }

    public string Configuration { get; set; } = "Debug";

    public List<string> CompiledFiles { get; } = [];

    public List<string> AnalyzedFiles { get; } = [];

    public List<string> NonBoundaryInputsNotCompiled { get; } = [];

    public List<string> GeneratedCompilationFiles { get; } = [];
}

public sealed class AnalysisResult
{
    public string Status { get; set; } = "failed";

    public string ReasonCode { get; set; } = "analysis_failed";

    public string Message { get; set; } = "Analysis did not complete";

    public string RoslynVersion { get; set; } = "unknown";

    public AnalysisCoverage Coverage { get; } = new();

    public List<AnalysisDiagnostic> Diagnostics { get; } = [];

    public List<StaticReference> References { get; } = [];

    public List<BoundaryViolation> Violations { get; } = [];

    public Dictionary<string, double> PhaseSeconds { get; } = [];

    public double ElapsedSeconds { get; set; }
}

public static class ResultJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };
}

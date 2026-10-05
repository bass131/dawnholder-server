using System.Diagnostics;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace Dawnholder.Tools.Architecture.Boundaries;

public static class WorkspaceInput
{
    public static async Task<List<Compilation>> Load(string root, SourceManifest manifest,
        Policy policy, AnalysisResult result, CancellationToken cancellation)
    {
        var phase = Stopwatch.StartNew();
        var sdk = Path.Combine(Environment.GetEnvironmentVariable("DOTNET_ROOT")
            ?? throw new InvalidOperationException("DOTNET_ROOT missing"), "sdk", "10.0.301");
        MSBuildLocator.RegisterMSBuildPath(sdk);
        using var workspace = MSBuildWorkspace.Create(new Dictionary<string, string>
        {
            ["Configuration"] = "Debug",
            ["MSBuildSDKsPath"] = Path.Combine(sdk, "Sdks"),
            ["DesignTimeBuild"] = "true",
            ["BuildProjectReferences"] = "false",
            ["SkipCompilerExecution"] = "true",
            ["UseSharedCompilation"] = "false",
            ["RunAnalyzersDuringBuild"] = "false",
            ["RunAnalyzers"] = "false",
            ["NuGetAudit"] = "false",
        });
        workspace.LoadMetadataForReferencedProjects = false;
        var workspaceDiagnostics = new System.Collections.Concurrent.ConcurrentQueue<AnalysisDiagnostic>();
        workspace.WorkspaceFailed += (_, eventArgs) => workspaceDiagnostics.Enqueue(
            new AnalysisDiagnostic("workspace", "Workspace", eventArgs.Diagnostic.Kind.ToString(), eventArgs.Diagnostic.Message, null));
        // Dependency-first follows the existing SDK Workspace loading convention.
        foreach (var relative in manifest.Projects.Reverse())
        {
            var path = Path.GetFullPath(Path.Combine(root, relative));
            if (!workspace.CurrentSolution.Projects.Any(project => project.FilePath == path))
            {
                await workspace.OpenProjectAsync(path, cancellationToken: cancellation);
            }
        }
        var expectedProjects = manifest.Projects.ToHashSet(StringComparer.Ordinal);
        var actualProjects = workspace.CurrentSolution.Projects
            .Select(project => policy.Relative(project.FilePath ?? throw new InvalidOperationException("Project path missing")))
            .ToHashSet(StringComparer.Ordinal);
        if (!actualProjects.SetEquals(expectedProjects))
        {
            throw new InvalidOperationException("Workspace project graph differs from the fixed input manifest");
        }
        result.PhaseSeconds["workspaceLoad"] = phase.Elapsed.TotalSeconds;
        phase.Restart();
        var compilations = new List<Compilation>();
        var inputFiles = manifest.SourceFiles.ToHashSet(StringComparer.Ordinal);
        var compiledFiles = new HashSet<string>(StringComparer.Ordinal);
        foreach (var project in workspace.CurrentSolution.Projects.OrderBy(project => project.FilePath, StringComparer.Ordinal))
        {
            var compilation = await project.GetCompilationAsync(cancellation)
                ?? throw new InvalidOperationException($"Compilation missing: {project.FilePath}");
            compilations.Add(compilation);
            foreach (var tree in compilation.SyntaxTrees)
            {
                var relative = policy.Relative(tree.FilePath);
                if (inputFiles.Contains(relative))
                {
                    compiledFiles.Add(relative);
                }
                else if (relative.Split('/').Contains("obj", StringComparer.Ordinal))
                {
                    result.Coverage.GeneratedCompilationFiles.Add(relative);
                }
                else
                {
                    throw new InvalidOperationException($"Compile source absent from fixed inputs: {relative}");
                }
            }
            foreach (var diagnostic in compilation.GetDiagnostics(cancellation))
            {
                result.Diagnostics.Add(new AnalysisDiagnostic("compiler", diagnostic.Id,
                    diagnostic.Severity.ToString(), diagnostic.GetMessage(),
                    diagnostic.Location.IsInSource ? policy.Spot(diagnostic.Location) : null));
            }
        }
        result.Diagnostics.AddRange(workspaceDiagnostics);
        result.Coverage.CompiledInputFiles = compiledFiles.Count;
        result.Coverage.CompiledFiles.AddRange(compiledFiles.Order(StringComparer.Ordinal));
        result.Coverage.NonBoundaryInputsNotCompiled.AddRange(inputFiles.Except(compiledFiles).Order(StringComparer.Ordinal));
        var missing = manifest.ExpectedBoundaryFiles.Except(compiledFiles).ToArray();
        if (missing.Length != 0)
        {
            throw new InvalidOperationException($"Boundary sources missing from Compile: {string.Join(", ", missing)}");
        }
        result.PhaseSeconds["compilation"] = phase.Elapsed.TotalSeconds;
        return compilations;
    }
}

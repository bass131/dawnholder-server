using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Dawnholder.Tools.Architecture.Boundaries;
using Microsoft.CodeAnalysis;

var result = new AnalysisResult { RoslynVersion = typeof(Compilation).Assembly.GetName().Version!.ToString() };
var timer = Stopwatch.StartNew();
var exit = 2;
try
{
    if (args.Length != 5)
    {
        throw new ArgumentException("Expected snapshot-root manifest-json rules-json new-analysis-json timeout-seconds");
    }
    var root = Path.GetFullPath(args[0]);
    var manifest = JsonSerializer.Deserialize<SourceManifest>(File.ReadAllText(args[1]), ResultJson.Options)
        ?? throw new InvalidOperationException("Input manifest missing");
    var document = JsonSerializer.Deserialize<BoundaryPolicy>(File.ReadAllText(args[2]), ResultJson.Options)
        ?? throw new InvalidOperationException("Policy missing");
    var timeout = double.Parse(args[4], CultureInfo.InvariantCulture);
    if (timeout <= 0 || timeout > 600 || manifest.SourceFiles.Length > 5000 || manifest.ExpectedBoundaryFiles.Length == 0)
    {
        throw new InvalidOperationException("Invalid bounded input contract");
    }
    using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(timeout));
    var policy = new Policy(root, document);
    result.Coverage.InputSourceFiles = manifest.SourceFiles.Length;
    result.Coverage.ExpectedBoundaryFiles = manifest.ExpectedBoundaryFiles.Length;
    var compilations = await WorkspaceInput.Load(root, manifest, policy, result, cancellation.Token);
    if (result.Diagnostics.Any(diagnostic => diagnostic.Severity is "Error" or "Failure"))
    {
        throw new InvalidOperationException("Source compilation/Workspace has errors; policy analysis was not completed");
    }
    new ReferenceExtractor(policy).Extract(compilations, manifest, result, cancellation.Token);
    result.Status = "completed";
    result.ReasonCode = "completed";
    result.Message = "Static references resolved for every boundary Compile input";
    exit = 0;
}
catch (OperationCanceledException)
{
    result.ReasonCode = "timeout";
    result.Message = "Design-time analysis exceeded its bounded cancellation deadline";
    exit = 124;
}
catch (Exception exception)
{
    result.Message = exception.Message;
    result.Diagnostics.Add(new AnalysisDiagnostic("tool", exception.GetType().Name, "Error", exception.ToString(), null));
    Console.Error.WriteLine(exception);
}
finally
{
    result.ElapsedSeconds = timer.Elapsed.TotalSeconds;
    if (args.Length >= 4)
    {
        // CreateNew prevents accidental overwriting even when invoked directly.
        using var stream = new FileStream(args[3], FileMode.CreateNew, FileAccess.Write);
        await JsonSerializer.SerializeAsync(stream, result, ResultJson.Options);
        stream.WriteByte((byte)'\n');
    }
}
Console.WriteLine($"Roslyn: {result.Status}, boundary coverage {result.Coverage.AnalyzedBoundaryFiles}/{result.Coverage.ExpectedBoundaryFiles}; {timer.Elapsed.TotalSeconds:F3}s");
return exit;

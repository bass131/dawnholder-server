using System.Text.Json;
using Dawnholder.Tools.Architecture;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;

try
{
    if (args.Length != 3) throw new ArgumentException("Expected input-root manifest output-json.");
    var root = Path.GetFullPath(args[0]);
    using var manifestDocument = JsonDocument.Parse(File.ReadAllText(args[1]));
    var manifest = manifestDocument.RootElement;
    var files = manifest.GetProperty("files").EnumerateArray()
        .Where(file => file.GetProperty("path").GetString()!.EndsWith(".cs", StringComparison.Ordinal))
        .Select(file => file.GetProperty("path").GetString()!).ToHashSet(StringComparer.Ordinal);
    var sdkDirectory = Path.Combine(Environment.GetEnvironmentVariable("DOTNET_ROOT") ?? throw new InvalidOperationException("DOTNET_ROOT missing."), "sdk", "10.0.301");
    MSBuildLocator.RegisterMSBuildPath(sdkDirectory);
    using var workspace = MSBuildWorkspace.Create(new Dictionary<string, string> { ["Configuration"] = "Debug", ["MSBuildSDKsPath"] = Path.Combine(sdkDirectory, "Sdks") });
    workspace.LoadMetadataForReferencedProjects = false;
    var diagnostics = new List<object>();
    workspace.WorkspaceFailed += (_, e) => diagnostics.Add(new { kind = "workspace", severity = e.Diagnostic.Kind.ToString(), message = e.Diagnostic.Message });
    var projectPaths = manifest.GetProperty("files").EnumerateArray()
        .Where(file => file.GetProperty("path").GetString()!.EndsWith(".csproj", StringComparison.Ordinal))
        .Select(file => file.GetProperty("path").GetString()!).ToArray();
    // Dependency-first avoids the Workspace's missing reference-assembly path
    // problem for discovered netstandard projects (same as Formatting).
    foreach (var project in projectPaths.OrderBy(path => path.StartsWith("98_Shared/", StringComparison.Ordinal) ? 0 : path.StartsWith("02_Server/Network/", StringComparison.Ordinal) ? 1 : 2))
    {
        var path = Path.Combine(root, project);
        if (!workspace.CurrentSolution.Projects.Any(p => p.FilePath == path)) await workspace.OpenProjectAsync(path);
    }

    var compilations = new List<(string Name, Compilation Compilation)>();
    foreach (var project in workspace.CurrentSolution.Projects.OrderBy(p => p.FilePath, StringComparer.Ordinal))
    {
        var compilation = await project.GetCompilationAsync() ?? throw new InvalidOperationException($"Compilation missing: {project.FilePath}");
        compilations.Add((project.Name, compilation));
    }
    var shared = compilations.Single(c => c.Compilation.AssemblyName == "Shared").Compilation;
    var clientNet = compilations.Single(c => c.Compilation.AssemblyName == "Dawnholder.Client.Net").Compilation;
    // This is an explicit asmdef approximation, never an Editor/Library build.
    var unitySymbols = new[] { "UNITY_6000_4", "UNITY_6000_4_OR_NEWER", "UNITY_EDITOR", "UNITY_EDITOR_WIN", "UNITY_STANDALONE_WIN", "ENABLE_INPUT_SYSTEM" };
    var unityOptions = new CSharpParseOptions(LanguageVersion.CSharp9, DocumentationMode.Parse, SourceCodeKind.Regular, unitySymbols);
    var unityTrees = files.Where(p => p.StartsWith("03_Client/Assets/Scripts/", StringComparison.Ordinal)).Order(StringComparer.Ordinal)
        .Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(root, path)), unityOptions, Path.Combine(root, path))).ToArray();
    var unityReferences = new List<MetadataReference>(shared.References.OfType<PortableExecutableReference>());
    unityReferences.Add(shared.ToMetadataReference());
    unityReferences.Add(clientNet.ToMetadataReference());
    foreach (var reference in manifest.GetProperty("externalReferences").EnumerateArray())
    {
        var path = Path.Combine(root, ".architecture-references", reference.GetProperty("rootId").GetString()!, reference.GetProperty("path").GetString()!);
        unityReferences.Add(MetadataReference.CreateFromFile(path));
    }
    compilations.Add(("Unity asmdef approximation", CSharpCompilation.Create("Dawnholder.Client", unityTrees, unityReferences,
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: false, nullableContextOptions: NullableContextOptions.Disable))));
    diagnostics.Add(new { kind = "limitation", severity = "Warning", message = "Unity approximation uses supplied Managed DLLs and explicit editor/windows symbols; InputSystem/TMP package assemblies are absent. This is partial semantics, not a Unity compile/play result." });
    var extractor = new RelationExtractor(root, files);
    // Index all source declarations before relationships so metadata/project
    // reference symbols can join only to unique declarations in this input.
    foreach (var (_, compilation) in compilations) extractor.Index(compilation);
    var compilerEvidence = new List<object>();
    foreach (var (name, compilation) in compilations)
    {
        compilerEvidence.Add(extractor.CompilerEvidence(name, compilation));
        foreach (var diagnostic in compilation.GetDiagnostics())
        {
            diagnostics.Add(new { kind = "compiler", project = name, id = diagnostic.Id, severity = diagnostic.Severity.ToString(), message = diagnostic.GetMessage(), source = extractor.Location(diagnostic.Location) });
        }
        extractor.Extract(compilation);
    }
    var result = new
    {
        rawVersion = 1,
        extractor = "Roslyn",
        version = typeof(Compilation).Assembly.GetName().Version!.ToString(),
        status = diagnostics.Count == 0 ? "complete" : "partial",
        sourceCommit = manifest.GetProperty("sourceCommit").GetString(),
        symbols = extractor.Symbols.Values.OrderBy(s => s.Key, StringComparer.Ordinal),
        relations = extractor.Relations,
        diagnostics,
        compilations = compilerEvidence,
        inputSourceCount = files.Count,
        coverage = extractor.CoveredFiles.Order(StringComparer.Ordinal),
        supports = new { calls = true, implements = true, usesType = true, contains = true },
    };
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[2]))!);
    await File.WriteAllTextAsync(args[2], JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) + "\n");
    Console.WriteLine($"Roslyn: {files.Count} input sources, {extractor.Symbols.Count} symbols, {extractor.Relations.Count} occurrences; partial Unity semantics.");
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception);
    return 1;
}

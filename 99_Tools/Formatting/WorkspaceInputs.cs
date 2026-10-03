using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;

namespace Dawnholder.Tools.Formatting;

internal sealed record PackageCompileInput(string Package, string Version, string Path, string Sha256);
internal sealed record ProjectInputs(string Path, string Configuration, string LanguageVersion, string DocumentationMode, string SourceKind, string[] Symbols, KeyValuePair<string, string>[] Features, string[] Sources, string[] GeneratedSources, PackageCompileInput[] PackageSources);
internal sealed record LoadedSource(string Project, string Configuration, string Path, CSharpParseOptions Options);
internal sealed record WorkspaceInputs(List<ProjectInputs> Projects, List<LoadedSource> Sources)
{
    public static async Task<WorkspaceInputs> LoadAsync(string root, SdkSelection sdk, IEnumerable<string> expectedFiles)
    {
        Environment.SetEnvironmentVariable("DOTNET_HOST_PATH", sdk.Executable);
        Environment.SetEnvironmentVariable("DOTNET_ROOT", Path.GetDirectoryName(sdk.Executable));
        if (!MSBuildLocator.IsRegistered) MSBuildLocator.RegisterMSBuildPath(sdk.SdkDirectory);
        var bundledWorkspace = Path.Combine(sdk.SdkDirectory, "DotnetTools/dotnet-format/Microsoft.CodeAnalysis.Workspaces.dll");
        if (InputPaths.Hash(typeof(Workspace).Assembly.Location) != InputPaths.Hash(bundledWorkspace)) throw new InvalidOperationException("Workspace differs from pinned SDK formatter.");
        var files = expectedFiles.ToHashSet(StringComparer.Ordinal);
        var registration = ProjectRegistration.Load(root, files);
        var expectedProjects = registration.AllProjects.ToHashSet(StringComparer.Ordinal);
        var results = new WorkspaceInputs([], []);
        foreach (var configuration in new[] { "Debug", "Release" })
        {
            using var workspace = MSBuildWorkspace.Create(new Dictionary<string, string>
            {
                ["Configuration"] = configuration,
                ["MSBuildSDKsPath"] = Path.Combine(sdk.SdkDirectory, "Sdks"),
            });
            workspace.LoadMetadataForReferencedProjects = false;
            workspace.SkipUnrecognizedProjects = false;
            // Open dependencies first: Roslyn's discovered-project resolver requires a non-null
            // reference-assembly path even for netstandard projects that do not produce one.
            foreach (var projectPath in DependencyOrder(root, expectedProjects))
                await workspace.OpenProjectAsync(InputPaths.Resolve(root, projectPath));
            if (workspace.Diagnostics.Count != 0) throw new InvalidDataException("Workspace load diagnostics: " + string.Join("\n", workspace.Diagnostics));
            var actualProjects = new HashSet<string>(StringComparer.Ordinal);
            foreach (var project in workspace.CurrentSolution.Projects)
            {
                var projectPath = InputPaths.Relative(root, project.FilePath ?? throw new InvalidDataException("Project has no path."));
                if (!expectedProjects.Contains(projectPath) || !actualProjects.Add(projectPath)) throw new InvalidDataException($"Unknown/duplicate Workspace project: {projectPath}");
                if (project.ParseOptions is not CSharpParseOptions options) throw new InvalidDataException($"Missing C# parse options: {projectPath}");
                var sources = new List<string>();
                var generated = new List<string>();
                var packageSources = new List<PackageCompileInput>();
                var documentPaths = new HashSet<string>(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
                foreach (var document in project.Documents)
                {
                    var absoluteSource = Path.GetFullPath(document.FilePath ?? throw new InvalidDataException("Document has no path."));
                    if (!documentPaths.Add(absoluteSource)) throw new InvalidDataException($"Duplicate Compile input: {projectPath}: {absoluteSource}");
                    var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
                    var packageRoot = Path.GetFullPath(Environment.GetEnvironmentVariable("NUGET_PACKAGES") ?? throw new InvalidDataException("NUGET_PACKAGES must be task scoped."));
                    if (absoluteSource.StartsWith(packageRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, comparison) || !absoluteSource.StartsWith(Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, comparison))
                    {
                        packageSources.Add(PackageInput(project.FilePath!, absoluteSource));
                        var packageTree = await document.GetSyntaxTreeAsync() ?? throw new InvalidDataException("Missing package Compile syntax tree.");
                        if (packageTree.GetDiagnostics().Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)) throw new InvalidDataException($"Package Compile parse error: {absoluteSource}");
                        continue;
                    }
                    var path = InputPaths.Relative(root, absoluteSource);
                    var projectDirectory = Path.GetDirectoryName(projectPath)!.Replace('\\', '/');
                    if (path.StartsWith(projectDirectory + "/obj/", StringComparison.Ordinal))
                    {
                        // Design-time Compile also lists SDK outputs before a normal build creates them.
                        generated.Add(path);
                        continue;
                    }
                    if (!files.Contains(path) || !path.EndsWith(".cs", StringComparison.Ordinal)) throw new InvalidDataException($"Compile input absent from manifest: {path}");
                    var tree = await document.GetSyntaxTreeAsync() ?? throw new InvalidDataException($"Missing syntax tree: {path}");
                    if (tree.GetDiagnostics().Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)) throw new InvalidDataException($"Parse errors: {path}: {string.Join("; ", tree.GetDiagnostics())}");
                    sources.Add(path);
                    results.Sources.Add(new LoadedSource(projectPath, configuration, path, options));
                }
                results.Projects.Add(new ProjectInputs(projectPath, configuration, options.LanguageVersion.ToString(), options.DocumentationMode.ToString(), options.Kind.ToString(), options.PreprocessorSymbolNames.Order(StringComparer.Ordinal).ToArray(), options.Features.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToArray(), sources.Order(StringComparer.Ordinal).ToArray(), generated.Order(StringComparer.Ordinal).ToArray(), packageSources.OrderBy(item => item.Path, StringComparer.Ordinal).ToArray()));
            }
            if (!expectedProjects.SetEquals(actualProjects)) throw new InvalidDataException("Missing Workspace project.");
            var compiled = results.Sources.Where(source => source.Configuration == configuration).Select(source => source.Path).ToHashSet(StringComparer.Ordinal);
            foreach (var path in files.Where(path => path.EndsWith(".cs", StringComparison.Ordinal)))
            {
                if (!compiled.Contains(path) && !path.StartsWith("99_Tools/Formatting.Tests/Fixtures/", StringComparison.Ordinal)) throw new InvalidDataException($"Source not in Compile set: {path}");
            }
        }
        results.Projects.Sort((left, right) => StringComparer.Ordinal.Compare(left.Path + "/" + left.Configuration, right.Path + "/" + right.Configuration));
        return results;
    }

    private static PackageCompileInput PackageInput(string projectPath, string source)
    {
        var packages = Environment.GetEnvironmentVariable("NUGET_PACKAGES") ?? throw new InvalidDataException("NUGET_PACKAGES must be task scoped.");
        var relative = InputPaths.Relative(packages, source);
        var parts = relative.Split('/');
        if (parts.Length < 3) throw new InvalidDataException($"Unknown external Compile input: {source}");
        using var assets = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(Path.GetDirectoryName(projectPath)!, "obj/project.assets.json")));
        var library = assets.RootElement.GetProperty("libraries").EnumerateObject().SingleOrDefault(item => string.Equals(item.Name, parts[0] + "/" + parts[1], StringComparison.OrdinalIgnoreCase));
        if (library.Value.ValueKind != System.Text.Json.JsonValueKind.Object || library.Value.GetProperty("type").GetString() != "package" || !library.Value.GetProperty("files").EnumerateArray().Any(item => item.GetString() == string.Join('/', parts[2..]))) throw new InvalidDataException($"External Compile input is absent from restored package manifest: {source}");
        InputPaths.Resolve(packages, relative);
        return new PackageCompileInput(parts[0], parts[1], string.Join('/', parts[2..]), InputPaths.Hash(source));
    }

    private static IEnumerable<string> DependencyOrder(string root, HashSet<string> projects)
    {
        var visiting = new HashSet<string>(StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var ordered = new List<string>();
        void Visit(string project)
        {
            if (visited.Contains(project)) return;
            if (!projects.Contains(project) || !visiting.Add(project)) throw new InvalidDataException($"Unknown/cyclic project reference: {project}");
            var absolute = InputPaths.Resolve(root, project);
            foreach (var reference in System.Xml.Linq.XDocument.Load(absolute).Descendants("ProjectReference"))
            {
                var include = (string?)reference.Attribute("Include") ?? throw new InvalidDataException("ProjectReference has no Include.");
                if (include.IndexOfAny(['$', '*', '?', ';']) >= 0) throw new InvalidDataException($"Unresolved project reference: {include}");
                Visit(InputPaths.Relative(root, Path.Combine(Path.GetDirectoryName(absolute)!, include.Replace('\\', '/'))));
            }
            visiting.Remove(project);
            visited.Add(project);
            ordered.Add(project);
        }
        foreach (var project in projects.Order(StringComparer.Ordinal)) Visit(project);
        return ordered;
    }
}

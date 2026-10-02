using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;

namespace Dawnholder.Tools.Formatting.Tests.Support;

/// <summary>
/// Actual Debug/Release parse options of every product project the repository's Dawnholder.slnx
/// declares, read through the pinned SDK Workspace independently of the tool's own manifest and
/// registration code. Requires a restored checkout.
/// </summary>
public sealed class ProductParseOptions : IAsyncLifetime
{
    private readonly Dictionary<(string Project, string Configuration), CSharpParseOptions> _options = new();

    /// <summary>The slnx product set, dependencies first so every opened project's references are already loaded.</summary>
    public static IReadOnlyList<string> Projects { get; } = SolutionProjectsInDependencyOrder();

    public IReadOnlyDictionary<(string Project, string Configuration), CSharpParseOptions> Options => _options;

    public async Task InitializeAsync()
    {
        var root = TestEnvironment.RepositoryRoot;
        var sdks = TestEnvironment.Run(TestEnvironment.Dotnet, root, ["--list-sdks"]);
        var row = sdks.StandardOutput.Split('\n').Single(line => line.StartsWith(TestEnvironment.RequiredSdk + " [", StringComparison.Ordinal)).Trim();
        var sdkDirectory = Path.Combine(row[(row.IndexOf('[') + 1)..^1], TestEnvironment.RequiredSdk);
        Register(sdkDirectory);
        await LoadAsync(root, sdkDirectory);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public CSharpParseOptions Get(string project, string configuration) => _options[(project, configuration)];

    // Read with plain XML so the expectation never comes from the formatting tool's registration code.
    private static string[] SolutionProjectsInDependencyOrder()
    {
        var root = TestEnvironment.RepositoryRoot;
        var declared = XDocument.Load(Path.Combine(root, "Dawnholder.slnx")).Descendants("Project")
            .Select(element => (string?)element.Attribute("Path") ?? throw new InvalidOperationException("slnx project without Path."))
            .ToArray();
        var ordered = new List<string>();
        var visiting = new HashSet<string>(StringComparer.Ordinal);
        void Visit(string project)
        {
            if (ordered.Contains(project, StringComparer.Ordinal)) return;
            if (!visiting.Add(project)) throw new InvalidOperationException($"Cyclic product reference: {project}");
            var directory = Path.GetDirectoryName(Path.Combine(root, project))!;
            foreach (var reference in XDocument.Load(Path.Combine(root, project)).Descendants("ProjectReference"))
            {
                var include = ((string?)reference.Attribute("Include") ?? string.Empty).Replace('\\', '/');
                var target = Path.GetRelativePath(root, Path.GetFullPath(Path.Combine(directory, include))).Replace('\\', '/');
                if (declared.Contains(target, StringComparer.Ordinal)) Visit(target);
            }

            ordered.Add(project);
        }

        foreach (var project in declared) Visit(project);
        return ordered.ToArray();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Register(string sdkDirectory)
    {
        if (!MSBuildLocator.IsRegistered) MSBuildLocator.RegisterMSBuildPath(sdkDirectory);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private async Task LoadAsync(string root, string sdkDirectory)
    {
        foreach (var configuration in new[] { "Debug", "Release" })
        {
            using var workspace = MSBuildWorkspace.Create(new Dictionary<string, string>
            {
                ["Configuration"] = configuration,
                ["MSBuildSDKsPath"] = Path.Combine(sdkDirectory, "Sdks"),
            });
            workspace.LoadMetadataForReferencedProjects = false;
            foreach (var project in Projects)
            {
                var path = Path.GetFullPath(Path.Combine(root, project));
                if (workspace.CurrentSolution.Projects.Any(item => string.Equals(item.FilePath, path, StringComparison.Ordinal))) continue;
                await workspace.OpenProjectAsync(path);
            }

            foreach (var project in Projects)
            {
                var path = Path.GetFullPath(Path.Combine(root, project));
                var loaded = workspace.CurrentSolution.Projects.Single(item => string.Equals(item.FilePath, path, StringComparison.Ordinal));
                _options[(project, configuration)] = (CSharpParseOptions)(loaded.ParseOptions ?? throw new InvalidOperationException($"No parse options: {project}/{configuration}"));
            }
        }
    }
}

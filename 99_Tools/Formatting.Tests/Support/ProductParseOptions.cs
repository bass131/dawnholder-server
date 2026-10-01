using System.Runtime.CompilerServices;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;

namespace Dawnholder.Tools.Formatting.Tests.Support;

/// <summary>
/// Actual Debug/Release parse options of the eight product projects, read through the pinned SDK
/// Workspace independently of the tool's own manifest code. Requires a restored checkout.
/// </summary>
public sealed class ProductParseOptions : IAsyncLifetime
{
    // Dependencies first so every opened project's references are already in the solution.
    private static readonly string[] Projects =
    [
        "98_Shared/Shared.csproj",
        "04_ClientNet/Dawnholder.Client.Net.csproj",
        "02_Server/Network/Dawnholder.Server.Network.csproj",
        "02_Server/GameServer/GameServer.csproj",
        "99_Tools/PacketGenerator/PacketGenerator.csproj",
        "99_Tools/headless-bot/HeadlessBot.csproj",
        "99_Tools/BgmComposer/BgmComposer.csproj",
        "02_Server/GameServer.Tests/GameServer.Tests.csproj",
    ];

    private readonly Dictionary<(string Project, string Configuration), CSharpParseOptions> _options = new();

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

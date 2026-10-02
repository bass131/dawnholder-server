using System.Text.Json.Nodes;

namespace Dawnholder.Tools.Formatting.Tests.Support;

/// <summary>One restored mini repository and the manifest the CLI captured for it.</summary>
public sealed class BaselineRepository : IDisposable
{
    public BaselineRepository()
    {
        Repository = MiniRepository.Create("baseline");
        ManifestPath = Repository.CreateManifest();
    }

    internal MiniRepository Repository { get; }

    public string ManifestPath { get; }

    public JsonObject Manifest() => (JsonObject)JsonNode.Parse(File.ReadAllText(ManifestPath))!;

    public string WriteManifest(JsonNode manifest)
    {
        var path = Path.Combine(TestEnvironment.NewTemporaryDirectory("edited-manifest"), "input-manifest.json");
        File.WriteAllText(path, manifest.ToJsonString());
        return path;
    }

    public void Dispose() => Repository.Dispose();
}

/// <summary>Marks POSIX-shell contract tests; they are reported as skipped, never as passed, elsewhere.</summary>
public sealed class LinuxFactAttribute : FactAttribute
{
    public LinuxFactAttribute()
    {
        if (!OperatingSystem.IsLinux()) Skip = "Requires the Linux/WSL shell entry points.";
    }
}

/// <summary>Theory counterpart of <see cref="LinuxFactAttribute"/>.</summary>
public sealed class LinuxTheoryAttribute : TheoryAttribute
{
    public LinuxTheoryAttribute()
    {
        if (!OperatingSystem.IsLinux()) Skip = "Requires the Linux/WSL shell entry points.";
    }
}

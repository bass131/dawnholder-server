using Dawnholder.Tools.Formatting.Tests.Support;

namespace Dawnholder.Tools.Formatting.Tests;

/// <summary>
/// The manifest is built from the actual Workspace. Load diagnostics, parse errors, Compile sets
/// that disagree with the input list and changed parse options must stop the check.
/// </summary>
public sealed class WorkspaceFailClosedTests
{
    [Fact]
    public void SourceExcludedFromCompile_FailsManifest()
    {
        using var repository = MiniRepository.Create("compile-remove", restore: false);
        repository.Write("02_Server/Alpha/Alpha.csproj", ProjectWith("<ItemGroup>\n    <Compile Remove=\"Hidden.cs\" />\n  </ItemGroup>"));
        repository.Write("02_Server/Alpha/Hidden.cs", "namespace Mini.Hidden;\n");
        repository.Commit("hidden source");
        repository.Restore();
        AssertManifestFails(repository, "Source not in Compile set");
    }

    [Fact]
    public void CompileInputOutsideInputTrees_FailsManifest()
    {
        using var repository = MiniRepository.Create("outside-compile", restore: false);
        repository.Write("Outside/Extra.cs", "namespace Mini.Outside;\n");
        repository.Write("02_Server/Alpha/Alpha.csproj", ProjectWith("<ItemGroup>\n    <Compile Include=\"../../Outside/Extra.cs\" />\n  </ItemGroup>"));
        repository.Commit("outside source");
        repository.Restore();
        AssertManifestFails(repository, "absent from manifest");
    }

    [Fact]
    public void ParseError_FailsManifest()
    {
        using var repository = MiniRepository.Create("parse-error", restore: false);
        repository.Write("02_Server/Beta/BetaSource.cs", "namespace Mini.Beta;\n\npublic static class BetaSource\n{\n    public static int Value() => ;\n}\n");
        repository.Commit("parse error");
        repository.Restore();
        AssertManifestFails(repository, "Parse errors");
    }

    [Fact]
    public void BrokenProjectReference_FailsManifest()
    {
        using var repository = MiniRepository.Create("broken-reference", restore: false);
        repository.Write("02_Server/Alpha/Alpha.csproj", ProjectWith("<ItemGroup>\n    <ProjectReference Include=\"../Missing/Missing.csproj\" />\n  </ItemGroup>"));
        repository.Commit("broken reference");
        AssertManifestFails(repository, string.Empty);
    }

    [Fact]
    public void SolutionWithoutEightProducts_FailsManifest()
    {
        using var repository = MiniRepository.Create("seven-projects", restore: false);
        var solution = repository.Read("Dawnholder.slnx").Replace("  <Project Path=\"99_Tools/Zeta/Zeta.csproj\" />\n", string.Empty, StringComparison.Ordinal);
        repository.Write("Dawnholder.slnx", solution);
        repository.Commit("seven projects");
        repository.Restore();
        AssertManifestFails(repository, "eight product projects");
    }

    [Fact]
    public void MissingGeneratedSource_FailsManifest()
    {
        using var repository = MiniRepository.Create("no-generated", restore: false);
        File.Delete(repository.PathOf(MiniRepository.GeneratedPath));
        repository.Commit("no generated");
        repository.Restore();
        AssertManifestFails(repository, MiniRepository.GeneratedPath);
    }

    [Fact]
    public void ParseOptionsChangedByEnvironment_FailValidation()
    {
        // MSBuild reads environment variables as initial properties; the hashes stay identical.
        using var repository = MiniRepository.Create("env-options");
        var manifest = repository.CreateManifest();
        var baseline = repository.Cli("validate", "--root", repository.Root, "--dotnet", TestEnvironment.Dotnet, "--manifest", manifest);
        Assert.True(baseline.ExitCode == 0, baseline.Combined);
        var environment = new Dictionary<string, string?>(TestEnvironment.IsolatedGit()) { ["DefineConstants"] = "DAWNHOLDER_ENVIRONMENT_SYMBOL" };
        var changed = TestEnvironment.Run(TestEnvironment.Dotnet, repository.Root, [TestEnvironment.FormattingCli, "validate", "--root", repository.Root, "--dotnet", TestEnvironment.Dotnet, "--manifest", manifest], environment);
        Assert.True(changed.ExitCode != 0, changed.Combined);
        Assert.Contains("parse options", changed.StandardError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ManifestCapturedFromAnotherTree_FailsFullValidation()
    {
        using var repository = MiniRepository.Create("other-tree");
        var manifest = repository.CreateManifest();
        using var other = MiniRepository.Create("other-tree-b", restore: false);
        other.Write("02_Server/Gamma/GammaSource.cs", "namespace Mini.Gamma;\n\npublic static class GammaSource\n{\n    public static int Value() => 2;\n}\n");
        other.Commit("different");
        other.Restore();
        var result = other.Cli("validate", "--root", other.Root, "--dotnet", TestEnvironment.Dotnet, "--manifest", manifest, "--git");
        Assert.True(result.ExitCode != 0, result.Combined);
    }

    private static string ProjectWith(string items) =>
        "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup>\n    <TargetFramework>net10.0</TargetFramework>\n    <Nullable>enable</Nullable>\n    <LangVersion>latest</LangVersion>\n  </PropertyGroup>\n  " + items + "\n</Project>\n";

    private static void AssertManifestFails(MiniRepository repository, string reason)
    {
        var output = Path.Combine(TestEnvironment.NewTemporaryDirectory("failed-manifest"), "input-manifest.json");
        var result = repository.Cli("manifest", "--root", repository.Root, "--git-root", repository.Root, "--dotnet", TestEnvironment.Dotnet, "--out", output);
        Assert.True(result.ExitCode != 0, $"Expected manifest failure ({reason}).\n{result.Combined}");
        Assert.False(File.Exists(output), "A failed capture must not leave a manifest.");
        if (reason.Length > 0) Assert.Contains(reason, result.StandardError, StringComparison.Ordinal);
    }
}

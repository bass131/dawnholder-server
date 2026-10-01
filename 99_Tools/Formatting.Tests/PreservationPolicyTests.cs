using System.Text;
using System.Text.Json.Nodes;
using Dawnholder.Tools.Formatting.Tests.Support;

namespace Dawnholder.Tools.Formatting.Tests;

/// <summary>
/// End-to-end compare through the CLI with the tool's own Workspace options: whitespace-only output
/// passes, while encoding/EOL/EOF policy, protected inputs and semantic differences fail.
/// </summary>
public sealed class PreservationPolicyTests : IClassFixture<BaselineRepository>
{
    private const string Source = "02_Server/Alpha/AlphaSource.cs";
    private static readonly UTF8Encoding Utf8 = new(false);
    private readonly BaselineRepository _baseline;

    public PreservationPolicyTests(BaselineRepository baseline)
    {
        _baseline = baseline;
    }

    [Fact]
    public void UnchangedSnapshot_PassesWithEveryProofRecorded()
    {
        var after = RestoredCopy("unchanged-after");
        var (result, proof) = Compare(after);
        Assert.True(result.ExitCode == 0, result.Combined);
        var files = proof!["Files"]!.AsArray();
        var sources = _baseline.Manifest()["Files"]!.AsArray().Where(file => (string?)file!["Category"] is "handwritten-source" or "generated-source").Select(file => (string)file!["Path"]!).Order(StringComparer.Ordinal);
        Assert.Equal(sources, files.Select(file => (string)file!["Path"]!).Order(StringComparer.Ordinal));
        Assert.All(files, file => Assert.False((bool)file!["Changed"]!));
        Assert.All(files, file => Assert.Equal(2, (int)file!["Conditions"]!));
    }

    [Fact]
    public void WhitespaceOnlyOutput_PassesAndRecordsChange()
    {
        var after = RestoredCopy("whitespace-after");
        Rewrite(after, Source, text => text.Replace("    public static int Value() => 1;", "    public static int Value()  =>  1;", StringComparison.Ordinal).Replace("{\n", "{\n\n", StringComparison.Ordinal));
        var (result, proof) = Compare(after);
        Assert.True(result.ExitCode == 0, result.Combined);
        Assert.True((bool)FileProof(proof!, Source)["Changed"]!);
    }

    [Fact]
    public void DebugBranchWhitespace_IsProvedThroughActualReleaseInactiveRegion()
    {
        var after = RestoredCopy("debug-branch-after");
        File.WriteAllText(Path.Combine(after, MiniRepository.RegionsPath), MiniRepository.RegionsSource("        Check(registered,  \"debug\");", "        var  hidden = 1;"), Utf8);
        var (result, proof) = Compare(after);
        Assert.True(result.ExitCode == 0, result.Combined);
        Assert.True((int)FileProof(proof!, MiniRepository.RegionsPath)["InactiveRegionsProved"]! >= 1);
    }

    public static TheoryData<string> Violations => new()
    {
        "bom added",
        "crlf line endings",
        "missing final newline",
        "generated source whitespace",
        "project file changed",
        "root policy changed",
        "string literal changed",
        "comment changed",
        "never-active region whitespace",
        "extra compiled source",
        "deleted source",
    };

    [Theory]
    [MemberData(nameof(Violations))]
    public void PolicyOrSemanticViolation_Fails(string violation)
    {
        var after = RestoredCopy("violation-after");
        switch (violation)
        {
            case "bom added":
                File.WriteAllBytes(Path.Combine(after, Source), new byte[] { 0xef, 0xbb, 0xbf }.Concat(File.ReadAllBytes(Path.Combine(after, Source))).ToArray());
                break;
            case "crlf line endings":
                Rewrite(after, Source, text => text.Replace("\n", "\r\n", StringComparison.Ordinal));
                break;
            case "missing final newline":
                Rewrite(after, Source, text => text.TrimEnd('\n'));
                break;
            case "generated source whitespace":
                Rewrite(after, MiniRepository.GeneratedPath, text => text.Replace("    public", "  public", StringComparison.Ordinal));
                break;
            case "project file changed":
                Rewrite(after, "02_Server/Alpha/Alpha.csproj", text => text.Replace("</Project>", "  <!-- changed -->\n</Project>", StringComparison.Ordinal));
                break;
            case "root policy changed":
                Rewrite(after, ".editorconfig", text => text + "\n");
                break;
            case "string literal changed":
                Rewrite(after, MiniRepository.RegionsPath, text => text.Replace("\"release\"", "\"release \"", StringComparison.Ordinal));
                break;
            case "comment changed":
                Rewrite(after, Source, text => text.Replace("public static class", "// added comment\npublic static class", StringComparison.Ordinal));
                break;
            case "never-active region whitespace":
                File.WriteAllText(Path.Combine(after, MiniRepository.RegionsPath), MiniRepository.RegionsSource("        Check(registered, \"debug\");", "        var hidden = 1;"), Utf8);
                break;
            case "extra compiled source":
                File.WriteAllText(Path.Combine(after, "02_Server/Alpha/Extra.cs"), "namespace Mini.Extra;\n", Utf8);
                break;
            case "deleted source":
                File.Delete(Path.Combine(after, Source));
                break;
        }

        var (result, _) = Compare(after);
        Assert.True(result.ExitCode != 0, $"{violation}: expected failure.\n{result.Combined}");
        Assert.Contains("ERROR:", result.StandardError, StringComparison.Ordinal);
    }

    [Fact]
    public void BomOnlyInOriginal_IsRemovedWithoutSemanticChange()
    {
        using var repository = MiniRepository.Create("bom-before", restore: false);
        var path = repository.PathOf(Source);
        File.WriteAllBytes(path, new byte[] { 0xef, 0xbb, 0xbf }.Concat(File.ReadAllBytes(path)).ToArray());
        repository.Commit("bom");
        repository.Restore();
        var manifest = repository.CreateManifest();
        var after = repository.CopyInputsTo("bom-after");
        File.WriteAllBytes(Path.Combine(after, Source), File.ReadAllBytes(path)[3..]);
        Restore(after);
        var output = Path.Combine(TestEnvironment.NewTemporaryDirectory("bom-proof"), "preservation.json");
        var result = repository.Cli("compare", "--root", repository.Root, "--after", after, "--dotnet", TestEnvironment.Dotnet, "--manifest", manifest, "--out", output);
        Assert.True(result.ExitCode == 0, result.Combined);
        var proof = FileProof((JsonObject)JsonNode.Parse(File.ReadAllText(output))!, Source);
        Assert.True((bool)proof["BeforeBom"]!);
        Assert.False((bool)proof["AfterBom"]!);
    }

    private static JsonObject FileProof(JsonObject proof, string path) =>
        proof["Files"]!.AsArray().Single(file => (string?)file!["Path"] == path)!.AsObject();

    private static void Rewrite(string root, string relative, Func<string, string> change)
    {
        var path = Path.Combine(root, relative);
        File.WriteAllText(path, change(File.ReadAllText(path, Utf8)), Utf8);
    }

    private static void Restore(string root)
    {
        var result = TestEnvironment.Run(TestEnvironment.Dotnet, root, ["restore", "Dawnholder.slnx", "--nologo"]);
        Assert.True(result.ExitCode == 0, result.Combined);
    }

    private string RestoredCopy(string purpose)
    {
        var copy = _baseline.Repository.CopyInputsTo(purpose);
        Restore(copy);
        return copy;
    }

    private (ProcessResult Result, JsonObject? Proof) Compare(string after)
    {
        var output = Path.Combine(TestEnvironment.NewTemporaryDirectory("proof"), "preservation.json");
        var root = _baseline.Repository.Root;
        var result = TestEnvironment.RunCli(root, "compare", "--root", root, "--after", after, "--dotnet", TestEnvironment.Dotnet, "--manifest", _baseline.ManifestPath, "--out", output);
        return (result, File.Exists(output) ? (JsonObject)JsonNode.Parse(File.ReadAllText(output))! : null);
    }
}

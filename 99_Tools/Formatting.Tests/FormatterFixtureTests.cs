using System.Text;
using System.Text.Json.Nodes;
using Dawnholder.Tools.Formatting.Tests.Support;

namespace Dawnholder.Tools.Formatting.Tests;

/// <summary>
/// The pinned formatter with the approved policy must detect the fixture's layout violations,
/// fix them, converge to zero changes and keep every literal byte; the tool must prove that.
/// </summary>
public sealed class FormatterFixtureTests
{
    private const string Sample = "02_Server/Alpha/Sample.cs";
    private const string Literals = "02_Server/Gamma/Literals.cs";
    private const string Encoded = "02_Server/Beta/BetaSource.cs";
    private const string VerbatimLines = "    public const string Verbatim = @\"line one   \n  line two\";\n";
    private const string RawLines = "    public static readonly string Raw = \"\"\"\n          indented   \n        \"\"\";\n";
    private static readonly UTF8Encoding Utf8 = new(false);

    [Fact]
    public void PinnedFormatter_FixesLayoutKeepsLiteralsAndIsProved()
    {
        using var repository = MiniRepository.Create("formatter-fixture", restore: false);
        repository.Write(Sample, File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "UnformattedSample.cs"), Utf8));
        repository.Write(Literals, "namespace Fixture.Literals;\n\npublic static class Literals\n{\n" + VerbatimLines + RawLines + "}\n");
        var encoded = repository.Read(Encoded).Replace("\n", "\r\n", StringComparison.Ordinal).TrimEnd('\r', '\n');
        repository.WriteBytes(Encoded, new byte[] { 0xef, 0xbb, 0xbf }.Concat(Utf8.GetBytes(encoded)).ToArray());
        repository.Commit("fixture inputs");
        repository.Restore();
        var manifest = repository.CreateManifest();
        var generatedBefore = File.ReadAllBytes(repository.PathOf(MiniRepository.GeneratedPath));

        var after = repository.CopyInputsTo("formatter-after");
        Dotnet(after, 0, "restore", "Dawnholder.slnx", "--nologo");
        Dotnet(after, 2, "format", "whitespace", "Dawnholder.slnx", "--no-restore", "--verify-no-changes", "--exclude", MiniRepository.GeneratedPath);
        Dotnet(after, 0, "format", "whitespace", "Dawnholder.slnx", "--no-restore", "--exclude", MiniRepository.GeneratedPath);
        Dotnet(after, 0, "format", "whitespace", "Dawnholder.slnx", "--no-restore", "--verify-no-changes", "--exclude", MiniRepository.GeneratedPath);

        var literals = File.ReadAllText(Path.Combine(after, Literals), Utf8);
        Assert.Contains(VerbatimLines, literals, StringComparison.Ordinal);
        Assert.Contains(RawLines, literals, StringComparison.Ordinal);
        Assert.Equal(generatedBefore, File.ReadAllBytes(Path.Combine(after, MiniRepository.GeneratedPath)));
        var encodedAfter = File.ReadAllBytes(Path.Combine(after, Encoded));
        Assert.False(encodedAfter.Length >= 3 && encodedAfter[0] == 0xef, "BOM must be removed by charset = utf-8.");
        Assert.DoesNotContain((byte)'\r', encodedAfter);
        Assert.Equal((byte)'\n', encodedAfter[^1]);

        var output = Path.Combine(TestEnvironment.NewTemporaryDirectory("formatter-proof"), "preservation.json");
        var result = repository.Cli("compare", "--root", repository.Root, "--after", after, "--dotnet", TestEnvironment.Dotnet, "--manifest", manifest, "--out", output);
        Assert.True(result.ExitCode == 0, result.Combined);
        var files = ((JsonObject)JsonNode.Parse(File.ReadAllText(output))!)["Files"]!.AsArray();
        Assert.True((bool)files.Single(file => (string?)file!["Path"] == Sample)!["Changed"]!);
        Assert.True((bool)files.Single(file => (string?)file!["Path"] == Encoded)!["BeforeBom"]!);
    }

    [Fact]
    public void Fixture_IsOutsideEveryCompileSet()
    {
        var project = File.ReadAllText(Path.Combine(TestEnvironment.RepositoryRoot, "99_Tools", "Formatting.Tests", "Formatting.Tests.csproj"));
        Assert.Contains("<Compile Remove=\"Fixtures/**\" />", project, StringComparison.Ordinal);
        Assert.DoesNotContain(typeof(FormatterFixtureTests).Assembly.GetTypes(), type => type.Namespace == "Fixture.Unformatted");
    }

    private static void Dotnet(string root, int expectedExit, params string[] arguments)
    {
        var result = TestEnvironment.Run(TestEnvironment.Dotnet, root, arguments);
        Assert.True(result.ExitCode == expectedExit, $"dotnet {string.Join(' ', arguments)} exited {result.ExitCode}, expected {expectedExit}.\n{result.Combined}");
    }
}

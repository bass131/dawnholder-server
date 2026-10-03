using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Dawnholder.Tools.Formatting.Tests.Support;

namespace Dawnholder.Tools.Formatting.Tests;

/// <summary>
/// Explicit project registration: the product set is exactly what Dawnholder.slnx declares and the
/// independent set is exactly the checked-in list, with no fixed count. Both sets must reach the
/// actual Debug/Release Compile capture and the preservation proof; anything unregistered,
/// duplicated, missing, unsafe or tampered with must stop the check before targets are exported.
/// </summary>
public sealed class ProjectRegistrationTests : IClassFixture<RegisteredToolBaseline>
{
    private const string ExtraProduct = "02_Server/Eta/Eta.csproj";
    private const string ExtraProductSource = "02_Server/Eta/EtaSource.cs";
    private const string ListedTool = RegisteredToolBaseline.Tool;
    private readonly RegisteredToolBaseline _baseline;

    public ProjectRegistrationTests(RegisteredToolBaseline baseline)
    {
        _baseline = baseline;
    }

    [Fact]
    public void ExportedTargets_KeepSlnxProductsAndListedToolsApart()
    {
        using var repository = MiniRepository.Create("export-sets", restore: false);
        var (products, independent) = Export(repository);
        Assert.Equal(MiniRepository.ProjectPaths.Order(StringComparer.Ordinal), products);
        Assert.Empty(independent);

        repository.WriteProject(ExtraProduct);
        repository.Write("Dawnholder.slnx", WithSolutionProject(repository, ExtraProduct));
        repository.WriteProject(RegisteredToolBaseline.Tool);
        repository.Write(MiniRepository.IndependentListPath, MiniRepository.IndependentList(RegisteredToolBaseline.Tool));
        repository.Commit("ninth product and one tool");
        (products, independent) = Export(repository);
        Assert.Equal(MiniRepository.ProjectPaths.Append(ExtraProduct).Order(StringComparer.Ordinal), products);
        Assert.Equal([RegisteredToolBaseline.Tool], independent);
    }

    [Fact]
    public void NinthSolutionProject_IsCapturedInDebugAndRelease()
    {
        using var repository = MiniRepository.Create("ninth-product", restore: false);
        repository.WriteProject(ExtraProduct);
        repository.Write("Dawnholder.slnx", WithSolutionProject(repository, ExtraProduct));
        repository.Commit("ninth product");
        repository.Restore();
        var manifest = (JsonObject)JsonNode.Parse(File.ReadAllText(repository.CreateManifest()))!;
        Assert.Equal((MiniRepository.ProjectPaths.Length + 1) * 2, manifest["Projects"]!.AsArray().Count);
        foreach (var configuration in new[] { "Debug", "Release" })
            Assert.Equal([ExtraProductSource], Strings(ProjectEntry(manifest, ExtraProduct, configuration)["Sources"]));
    }

    [Fact]
    public void ListedTool_IsCapturedInDebugAndReleaseAndItsListIsHashed()
    {
        var manifest = _baseline.Manifest();
        Assert.Equal((MiniRepository.ProjectPaths.Length + 1) * 2, manifest["Projects"]!.AsArray().Count);
        foreach (var configuration in new[] { "Debug", "Release" })
            Assert.Equal([RegisteredToolBaseline.ToolSource], Strings(ProjectEntry(manifest, RegisteredToolBaseline.Tool, configuration)["Sources"]));
        var list = manifest["Files"]!.AsArray().Single(file => (string?)file!["Path"] == MiniRepository.IndependentListPath)!;
        Assert.Equal("build-or-policy-input", (string?)list["Category"]);
        var bytes = File.ReadAllBytes(_baseline.Repository.PathOf(MiniRepository.IndependentListPath));
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(bytes)), (string?)list["Sha256"]);
    }

    [Fact]
    public void ListedToolSource_IsInThePreservationProof()
    {
        var spacing = _baseline.RestoredCopy("tool-spacing");
        Rewrite(spacing, RegisteredToolBaseline.ToolSource, "public static int Value() => 1;", "public static int Value()  =>  1;");
        var (accepted, proof) = _baseline.Compare(spacing);
        Assert.True(accepted.ExitCode == 0, accepted.Combined);
        var file = proof!["Files"]!.AsArray().Single(item => (string?)item!["Path"] == RegisteredToolBaseline.ToolSource)!;
        Assert.True((bool)file["Changed"]!);
        Assert.Equal(2, (int)file["Conditions"]!);

        var literal = _baseline.RestoredCopy("tool-literal");
        Rewrite(literal, RegisteredToolBaseline.ToolSource, "public static int Value() => 1;", "public static int Value() => 2;");
        var (rejected, _) = _baseline.Compare(literal);
        Assert.True(rejected.ExitCode != 0, rejected.Combined);
        Assert.Contains("Token/literal/comment/directive difference: " + RegisteredToolBaseline.ToolSource, rejected.StandardError, StringComparison.Ordinal);
    }

    [Fact]
    public void ProjectInNeitherSet_FailsUntilListed()
    {
        using var repository = MiniRepository.Create("unregistered-project", restore: false);
        repository.WriteProject(RegisteredToolBaseline.Tool);
        repository.Commit("unregistered tool");
        AssertRegistrationFails(repository, "outside explicit registration");

        // Control: the same tree passes once the tool is listed, so only the registration was missing.
        repository.Write(MiniRepository.IndependentListPath, MiniRepository.IndependentList(RegisteredToolBaseline.Tool));
        repository.Commit("listed tool");
        Export(repository);
    }

    [Fact]
    public void CSharpOutsideEveryProject_FailsManifest()
    {
        using var repository = MiniRepository.Create("loose-source", restore: false);
        repository.Write("99_Tools/Loose/Loose.cs", "namespace Mini.Loose;\n\npublic static class Loose\n{\n}\n");
        repository.Commit("loose source");
        repository.Restore();
        Export(repository);
        var output = Path.Combine(TestEnvironment.NewTemporaryDirectory("loose-manifest"), "input-manifest.json");
        var result = repository.Cli("manifest", "--root", repository.Root, "--git-root", repository.Root, "--dotnet", TestEnvironment.Dotnet, "--out", output);
        Assert.True(result.ExitCode != 0, result.Combined);
        Assert.False(File.Exists(output), "A failed capture must not leave a manifest.");
        Assert.Contains("Source not in Compile set: 99_Tools/Loose/Loose.cs", result.StandardError, StringComparison.Ordinal);
    }

    // Each mutation is a solution a person could plausibly commit; MSBuild alone would not stop most of them.
    public static TheoryData<string, string> SolutionMutations => new()
    {
        { "duplicate declaration", "Duplicate/overlapping project registration: 02_Server/Alpha/Alpha.csproj" },
        { "case alias declaration", "Duplicate/overlapping project registration: 02_Server/Alpha/ALPHA.csproj" },
        { "duplicate inside folder", "Duplicate/overlapping project registration: 02_Server/Beta/Beta.csproj" },
        { "declared project absent", "Registered project absent from inputs: 02_Server/Ghost/Ghost.csproj" },
        { "declared outside inspected trees", "Invalid registered project path: 03_Client/Client/Client.csproj" },
        { "declared source file", "Invalid registered project path: 02_Server/Alpha/AlphaSource.cs" },
        { "parent-relative declaration", "Unsafe relative path: 02_Server/../02_Server/Gamma/Gamma.csproj" },
        { "declaration without path", "Missing project path." },
        { "no declarations", "Product project registration is empty." },
    };

    [Theory]
    [MemberData(nameof(SolutionMutations))]
    public void SolutionRegistrationDefects_Fail(string mutation, string reason)
    {
        using var repository = MiniRepository.Create("solution-registration", restore: false);
        var solution = repository.Read("Dawnholder.slnx");
        switch (mutation)
        {
            case "duplicate declaration":
                solution = WithSolutionProject(repository, "02_Server/Alpha/Alpha.csproj");
                break;
            case "case alias declaration":
                solution = WithSolutionProject(repository, "02_Server/Alpha/ALPHA.csproj");
                break;
            case "duplicate inside folder":
                solution = solution.Replace("</Solution>", "  <Folder Name=\"/again/\">\n    <Project Path=\"02_Server/Beta/Beta.csproj\" />\n  </Folder>\n</Solution>", StringComparison.Ordinal);
                break;
            case "declared project absent":
                solution = WithSolutionProject(repository, "02_Server/Ghost/Ghost.csproj");
                break;
            case "declared outside inspected trees":
                repository.WriteProject("03_Client/Client/Client.csproj");
                solution = WithSolutionProject(repository, "03_Client/Client/Client.csproj");
                break;
            case "declared source file":
                solution = WithSolutionProject(repository, "02_Server/Alpha/AlphaSource.cs");
                break;
            case "parent-relative declaration":
                solution = solution.Replace("02_Server/Gamma/Gamma.csproj", "02_Server/../02_Server/Gamma/Gamma.csproj", StringComparison.Ordinal);
                break;
            case "declaration without path":
                solution = solution.Replace("</Solution>", "  <Project />\n</Solution>", StringComparison.Ordinal);
                break;
            case "no declarations":
                solution = "<Solution>\n</Solution>\n";
                break;
        }

        repository.Write("Dawnholder.slnx", solution);
        repository.Commit(mutation);
        AssertRegistrationFails(repository, reason);
    }

    // Every case starts from a committed tool whose correct list passes; only the list differs.
    public static TheoryData<string, string> ListMutations => new()
    {
        { "duplicate entry", "Duplicate/overlapping project registration: " + ListedTool },
        { "case alias entry", "Duplicate/overlapping project registration: 99_Tools/Omega Tool/OMEGA TOOL.csproj" },
        { "tool omitted", "outside explicit registration" },
        { "product listed again", "Duplicate/overlapping project registration: 02_Server/Alpha/Alpha.csproj" },
        { "absent path", "Registered project absent from inputs: 99_Tools/Ghost/Ghost.csproj" },
        { "parent escape", "Unsafe relative path: ../outside/Outside.csproj" },
        { "dot segment", "Unsafe relative path: 99_Tools/./Omega Tool/Omega Tool.csproj" },
        { "empty segment", "Unsafe relative path: 99_Tools//Omega Tool/Omega Tool.csproj" },
        { "absolute path", "Unsafe relative path: /tmp/Omega Tool.csproj" },
        { "backslash separator", "Unsafe relative path: 99_Tools\\Omega Tool\\Omega Tool.csproj" },
        { "wildcard", "Invalid registered project path: 99_Tools/*/Omega Tool.csproj" },
        { "source file", "Invalid registered project path: " + RegisteredToolBaseline.ToolSource },
        { "outside inspected trees", "Invalid registered project path: 03_Client/Client/Client.csproj" },
        { "unknown field", "Unknown, missing or duplicate independent registration field." },
        { "missing projects field", "Unknown, missing or duplicate independent registration field." },
        { "duplicate projects field", "Unknown, missing or duplicate independent registration field." },
        { "wrong field case", "Unknown, missing or duplicate independent registration field." },
        { "newer schema version", "Unknown independent registration schema." },
        { "string schema version", "Unknown independent registration schema." },
        { "projects object", "Unknown independent registration schema." },
        { "non-string entry", "Independent project paths must be strings." },
        { "null entry", "Independent project paths must be strings." },
        { "array document", "Independent registration must be an object." },
        { "truncated document", "JsonReaderException" },
        { "list file deleted", MiniRepository.IndependentListPath },
    };

    [Theory]
    [MemberData(nameof(ListMutations))]
    public void IndependentListDefects_Fail(string mutation, string reason)
    {
        using var repository = MiniRepository.Create("list-registration", restore: false);
        repository.WriteProject(ListedTool);
        repository.Write(MiniRepository.IndependentListPath, MiniRepository.IndependentList(ListedTool));
        repository.Commit("listed tool");
        Export(repository);

        var tool = "\"" + ListedTool + "\"";
        string? list = mutation switch
        {
            "duplicate entry" => MiniRepository.IndependentList(ListedTool, ListedTool),
            "case alias entry" => MiniRepository.IndependentList(ListedTool, "99_Tools/Omega Tool/OMEGA TOOL.csproj"),
            "tool omitted" => MiniRepository.IndependentList(),
            "product listed again" => MiniRepository.IndependentList(ListedTool, "02_Server/Alpha/Alpha.csproj"),
            "absent path" => MiniRepository.IndependentList(ListedTool, "99_Tools/Ghost/Ghost.csproj"),
            "parent escape" => MiniRepository.IndependentList(ListedTool, "../outside/Outside.csproj"),
            "dot segment" => MiniRepository.IndependentList("99_Tools/./Omega Tool/Omega Tool.csproj"),
            "empty segment" => MiniRepository.IndependentList("99_Tools//Omega Tool/Omega Tool.csproj"),
            "absolute path" => MiniRepository.IndependentList(ListedTool, "/tmp/Omega Tool.csproj"),
            "backslash separator" => MiniRepository.IndependentList("99_Tools\\Omega Tool\\Omega Tool.csproj"),
            "wildcard" => MiniRepository.IndependentList("99_Tools/*/Omega Tool.csproj"),
            "source file" => MiniRepository.IndependentList(ListedTool, RegisteredToolBaseline.ToolSource),
            "outside inspected trees" => MiniRepository.IndependentList(ListedTool, "03_Client/Client/Client.csproj"),
            "unknown field" => "{\"SchemaVersion\":1,\"Projects\":[" + tool + "],\"Owner\":\"GameDev\"}\n",
            "missing projects field" => "{\"SchemaVersion\":1}\n",
            "duplicate projects field" => "{\"SchemaVersion\":1,\"Projects\":[" + tool + "],\"Projects\":[]}\n",
            "wrong field case" => "{\"SchemaVersion\":1,\"projects\":[" + tool + "]}\n",
            "newer schema version" => "{\"SchemaVersion\":2,\"Projects\":[" + tool + "]}\n",
            "string schema version" => "{\"SchemaVersion\":\"1\",\"Projects\":[" + tool + "]}\n",
            "projects object" => "{\"SchemaVersion\":1,\"Projects\":{\"Tool\":" + tool + "}}\n",
            "non-string entry" => "{\"SchemaVersion\":1,\"Projects\":[" + tool + ",1]}\n",
            "null entry" => "{\"SchemaVersion\":1,\"Projects\":[" + tool + ",null]}\n",
            "array document" => "[" + tool + "]\n",
            "truncated document" => "{\"SchemaVersion\":1,\"Projects\":[" + tool + "\n",
            _ => null,
        };
        if (mutation == "outside inspected trees") repository.WriteProject("03_Client/Client/Client.csproj");
        if (list == null) File.Delete(repository.PathOf(MiniRepository.IndependentListPath));
        else repository.Write(MiniRepository.IndependentListPath, list);
        repository.Commit(mutation);
        AssertRegistrationFails(repository, reason);
    }

    public static TheoryData<string> ListTampering => new()
    {
        "tool dropped after capture",
        "same registration reindented after capture",
        "manifest without the list",
        "snapshot from a tampered list",
    };

    [Theory]
    [MemberData(nameof(ListTampering))]
    public void ListChangedOrMissingAfterCapture_Fails(string mutation)
    {
        var copy = _baseline.Repository.CopyInputsTo("list-tampering");
        var listPath = Path.Combine(copy, MiniRepository.IndependentListPath);
        var manifestPath = _baseline.ManifestPath;
        ProcessResult result;
        switch (mutation)
        {
            case "tool dropped after capture":
                File.WriteAllText(listPath, MiniRepository.IndependentList());
                result = ValidateFiles(copy, manifestPath);
                Assert.Contains("Input changed before inspection: " + MiniRepository.IndependentListPath, result.StandardError, StringComparison.Ordinal);
                break;
            case "same registration reindented after capture":
                File.WriteAllText(listPath, "{\"SchemaVersion\":1,\"Projects\":[\"" + RegisteredToolBaseline.Tool + "\"]}\n");
                result = ValidateFiles(copy, manifestPath);
                Assert.Contains("Input changed before inspection: " + MiniRepository.IndependentListPath, result.StandardError, StringComparison.Ordinal);
                break;
            case "manifest without the list":
                var legacy = _baseline.Manifest();
                var files = legacy["Files"]!.AsArray();
                files.Remove(files.Single(file => (string?)file!["Path"] == MiniRepository.IndependentListPath));
                result = ValidateFiles(copy, _baseline.WriteManifest(legacy));
                Assert.Contains("Missing required input: " + MiniRepository.IndependentListPath, result.StandardError, StringComparison.Ordinal);
                break;
            default:
                File.WriteAllText(listPath, MiniRepository.IndependentList());
                var destination = TestEnvironment.NewTemporaryDirectory("tampered-snapshot");
                var report = Path.Combine(TestEnvironment.NewTemporaryDirectory("tampered-snapshot-report"), "copy.json");
                result = TestEnvironment.RunCli(copy, "snapshot", "--root", copy, "--after", destination, "--dotnet", TestEnvironment.Dotnet, "--manifest", manifestPath, "--out", report);
                Assert.Empty(Directory.EnumerateFileSystemEntries(destination));
                Assert.False(File.Exists(report));
                break;
        }

        Assert.True(result.ExitCode != 0, $"{mutation}: expected failure.\n{result.Combined}");
        Assert.Contains("ERROR:", result.StandardError, StringComparison.Ordinal);
        Assert.DoesNotContain("Validated", result.StandardOutput, StringComparison.Ordinal);
    }

    private static string WithSolutionProject(MiniRepository repository, string project) =>
        repository.Read("Dawnholder.slnx").Replace("</Solution>", "  <Project Path=\"" + project + "\" />\n</Solution>", StringComparison.Ordinal);

    private static string[] Strings(JsonNode? array) => array!.AsArray().Select(item => (string)item!).ToArray();

    private static JsonObject ProjectEntry(JsonObject manifest, string project, string configuration) =>
        manifest["Projects"]!.AsArray().Single(entry => (string?)entry!["Path"] == project && (string?)entry["Configuration"] == configuration)!.AsObject();

    private static void Rewrite(string root, string relative, string from, string to)
    {
        var path = Path.Combine(root, relative);
        var text = File.ReadAllText(path);
        Assert.Contains(from, text, StringComparison.Ordinal);
        File.WriteAllText(path, text.Replace(from, to, StringComparison.Ordinal));
    }

    private static ProcessResult ValidateFiles(string root, string manifest) =>
        TestEnvironment.RunCli(root, "validate", "--root", root, "--dotnet", TestEnvironment.Dotnet, "--manifest", manifest, "--files-only");

    private static (string[] Products, string[] Independent) Export(MiniRepository repository)
    {
        var output = Path.Combine(TestEnvironment.NewTemporaryDirectory("projects"), "projects.json");
        var result = repository.Cli("projects", "--root", repository.Root, "--git-root", repository.Root, "--dotnet", TestEnvironment.Dotnet, "--out", output);
        Assert.True(result.ExitCode == 0, result.Combined);
        var exported = (JsonObject)JsonNode.Parse(File.ReadAllText(output))!;
        Assert.Equal(["ProductProjects", "IndependentProjects"], exported.Select(property => property.Key));
        return (Strings(exported["ProductProjects"]), Strings(exported["IndependentProjects"]));
    }

    // Both the target export and the manifest capture must refuse, and neither may leave an output.
    private static void AssertRegistrationFails(MiniRepository repository, string reason)
    {
        var exported = Path.Combine(TestEnvironment.NewTemporaryDirectory("rejected-projects"), "projects.json");
        var projects = repository.Cli("projects", "--root", repository.Root, "--git-root", repository.Root, "--dotnet", TestEnvironment.Dotnet, "--out", exported);
        Assert.True(projects.ExitCode != 0, $"projects: expected failure ({reason}).\n{projects.Combined}");
        Assert.False(File.Exists(exported), "A rejected registration must not export targets.");
        Assert.Contains(reason, projects.StandardError, StringComparison.Ordinal);

        var manifest = Path.Combine(TestEnvironment.NewTemporaryDirectory("rejected-manifest"), "input-manifest.json");
        var capture = repository.Cli("manifest", "--root", repository.Root, "--git-root", repository.Root, "--dotnet", TestEnvironment.Dotnet, "--out", manifest);
        Assert.True(capture.ExitCode != 0, $"manifest: expected failure ({reason}).\n{capture.Combined}");
        Assert.False(File.Exists(manifest), "A rejected registration must not leave a manifest.");
        Assert.Contains(reason, capture.StandardError, StringComparison.Ordinal);
    }
}

/// <summary>
/// One restored mini repository whose list registers a tool with a space in its path and a
/// reference into the product set, plus the manifest the CLI captured for it.
/// </summary>
public sealed class RegisteredToolBaseline : IDisposable
{
    public const string Tool = "99_Tools/Omega Tool/Omega Tool.csproj";
    public const string ToolSource = "99_Tools/Omega Tool/OmegaToolSource.cs";

    public RegisteredToolBaseline()
    {
        Repository = MiniRepository.Create("registered-tool", restore: false);
        Repository.WriteProject(Tool, "<ItemGroup>\n    <ProjectReference Include=\"../../98_Shared/Shared.csproj\" />\n  </ItemGroup>");
        Repository.Write(MiniRepository.IndependentListPath, MiniRepository.IndependentList(Tool));
        Repository.Commit("registered tool");
        Repository.Restore();
        Repository.Restore(Tool);
        ManifestPath = Repository.CreateManifest();
    }

    public string ManifestPath { get; }

    internal MiniRepository Repository { get; }

    public JsonObject Manifest() => (JsonObject)JsonNode.Parse(File.ReadAllText(ManifestPath))!;

    public string WriteManifest(JsonNode manifest)
    {
        var path = Path.Combine(TestEnvironment.NewTemporaryDirectory("edited-manifest"), "input-manifest.json");
        File.WriteAllText(path, manifest.ToJsonString());
        return path;
    }

    internal string RestoredCopy(string purpose)
    {
        var copy = Repository.CopyInputsTo(purpose);
        foreach (var target in new[] { "Dawnholder.slnx", Tool })
        {
            var result = TestEnvironment.Run(TestEnvironment.Dotnet, copy, ["restore", target, "--nologo"]);
            Assert.True(result.ExitCode == 0, result.Combined);
        }

        return copy;
    }

    internal (ProcessResult Result, JsonObject? Proof) Compare(string after)
    {
        var output = Path.Combine(TestEnvironment.NewTemporaryDirectory("tool-proof"), "preservation.json");
        var result = TestEnvironment.RunCli(Repository.Root, "compare", "--root", Repository.Root, "--after", after, "--dotnet", TestEnvironment.Dotnet, "--manifest", ManifestPath, "--out", output);
        return (result, File.Exists(output) ? (JsonObject)JsonNode.Parse(File.ReadAllText(output))! : null);
    }

    public void Dispose() => Repository.Dispose();
}

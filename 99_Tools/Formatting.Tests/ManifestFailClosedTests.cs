using System.Text.Json.Nodes;
using Dawnholder.Tools.Formatting.Tests.Support;

namespace Dawnholder.Tools.Formatting.Tests;

/// <summary>
/// Black-box contract of the input manifest: an unchanged copy validates, and every unknown,
/// missing, stale, out-of-scope, escaping or SDK-mismatched input fails instead of passing.
/// </summary>
public sealed class ManifestFailClosedTests : IClassFixture<BaselineRepository>
{
    private const string Source = "02_Server/Alpha/AlphaSource.cs";
    private readonly BaselineRepository _baseline;

    public ManifestFailClosedTests(BaselineRepository baseline)
    {
        _baseline = baseline;
    }

    [Fact]
    public void UnchangedCopy_Validates()
    {
        var copy = _baseline.Repository.CopyInputsTo("unchanged");
        var result = ValidateFiles(copy, _baseline.ManifestPath);
        Assert.True(result.ExitCode == 0, result.Combined);
        Assert.Contains("Validated", result.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public void ManifestRecordsCheckoutStatusSdkAndProjects()
    {
        var manifest = _baseline.Manifest();
        Assert.Equal(_baseline.Repository.Git("rev-parse", "HEAD").Trim(), (string?)manifest["CheckoutSha"]);
        Assert.Equal(TestEnvironment.RequiredSdk, (string?)manifest["Sdk"]!["Version"]);
        var files = manifest["Files"]!.AsArray().Select(file => (string)file!["Path"]!).ToArray();
        Assert.Equal(files.Order(StringComparer.Ordinal), files);
        Assert.Contains(MiniRepository.GeneratedPath, files);
        Assert.Equal("generated-source", (string?)manifest["Files"]!.AsArray().Single(file => (string?)file!["Path"] == MiniRepository.GeneratedPath)!["Category"]);
        var projects = manifest["Projects"]!.AsArray();
        Assert.Equal(16, projects.Count);
        Assert.All(projects, project => Assert.NotEmpty(project!["Symbols"]!.AsArray()));
    }

    public static TheoryData<string> TreeMutations => new()
    {
        "changed content",
        "same size different content",
        "deleted input",
        "unknown source",
        "unknown project file",
        "uncaptured NuGet.config",
        "changed root policy",
        "changed generated source",
    };

    [Theory]
    [MemberData(nameof(TreeMutations))]
    public void InspectedTreeDifferences_Fail(string mutation)
    {
        var copy = _baseline.Repository.CopyInputsTo("tree-mutation");
        var path = Path.Combine(copy, Source);
        switch (mutation)
        {
            case "changed content":
                File.AppendAllText(path, "// extra\n");
                break;
            case "same size different content":
                var bytes = File.ReadAllBytes(path);
                bytes[^2] = bytes[^2] == (byte)'}' ? (byte)']' : (byte)'}';
                File.WriteAllBytes(path, bytes);
                break;
            case "deleted input":
                File.Delete(path);
                break;
            case "unknown source":
                File.WriteAllText(Path.Combine(copy, "02_Server/Alpha/Unlisted.cs"), "namespace X;\n");
                break;
            case "unknown project file":
                File.WriteAllText(Path.Combine(copy, "99_Tools/Delta/Extra.props"), "<Project />\n");
                break;
            case "uncaptured NuGet.config":
                File.WriteAllText(Path.Combine(copy, "NuGet.config"), "<configuration />\n");
                break;
            case "changed root policy":
                File.AppendAllText(Path.Combine(copy, ".editorconfig"), "\n[*.txt]\nindent_size = 2\n");
                break;
            case "changed generated source":
                File.AppendAllText(Path.Combine(copy, MiniRepository.GeneratedPath), "\n");
                break;
        }

        AssertFails(ValidateFiles(copy, _baseline.ManifestPath), mutation);
    }

    public static TheoryData<string> ManifestMutations => new()
    {
        "parent escape",
        "absolute posix path",
        "absolute windows path",
        "backslash separator",
        "empty segment",
        "dot segment",
        "out-of-scope tree",
        "case duplicate",
        "exact duplicate",
        "spoofed category",
        "malformed hash",
        "uppercase-only wrong hash",
        "wrong byte count",
        "removed entry",
        "removed generated entry",
        "schema version",
        "short checkout sha",
        "null status",
        "empty files",
        "empty projects",
        "sdk version",
        "formatter version",
        "sdk directory version",
        "relative sdk executable",
        "dotted sdk directory",
        "unknown member",
        "missing sdk",
    };

    [Theory]
    [MemberData(nameof(ManifestMutations))]
    public void ManifestDifferences_Fail(string mutation)
    {
        var manifest = _baseline.Manifest();
        var files = manifest["Files"]!.AsArray();
        var entry = files.Single(file => (string?)file!["Path"] == Source)!.AsObject();
        switch (mutation)
        {
            case "parent escape": entry["Path"] = "../outside/AlphaSource.cs"; break;
            case "absolute posix path": entry["Path"] = "/etc/hostname"; break;
            case "absolute windows path": entry["Path"] = "C:/Dev/AlphaSource.cs"; break;
            case "backslash separator": entry["Path"] = Source.Replace('/', '\\'); break;
            case "empty segment": entry["Path"] = "02_Server//Alpha/AlphaSource.cs"; break;
            case "dot segment": entry["Path"] = "02_Server/./Alpha/AlphaSource.cs"; break;
            case "out-of-scope tree": entry["Path"] = "03_Client/Assets/AlphaSource.cs"; break;
            case "case duplicate": files.Add(Clone(entry, "Path", Source.ToUpperInvariant())); break;
            case "exact duplicate": files.Add(entry.DeepClone()); break;
            case "spoofed category": entry["Category"] = "fixture"; break;
            case "malformed hash": entry["Sha256"] = new string('z', 64); break;
            case "uppercase-only wrong hash": entry["Sha256"] = new string('A', 64); break;
            case "wrong byte count": entry["Bytes"] = (long)entry["Bytes"]! + 1; break;
            case "removed entry": files.Remove(entry); break;
            case "removed generated entry": files.Remove(files.Single(file => (string?)file!["Path"] == MiniRepository.GeneratedPath)); break;
            case "schema version": manifest["SchemaVersion"] = 2; break;
            case "short checkout sha": manifest["CheckoutSha"] = "abc123"; break;
            case "null status": manifest["WorkTreeStatus"] = null; break;
            case "empty files": files.Clear(); break;
            case "empty projects": manifest["Projects"]!.AsArray().Clear(); break;
            case "sdk version": manifest["Sdk"]!["Version"] = "10.0.300"; break;
            case "formatter version": manifest["Sdk"]!["FormatterVersion"] = "10.0.300-servicing"; break;
            case "sdk directory version": manifest["Sdk"]!["SdkDirectory"] = "/usr/share/dotnet/sdk/10.0.300"; break;
            case "relative sdk executable": manifest["Sdk"]!["Executable"] = "dotnet"; break;
            case "dotted sdk directory": manifest["Sdk"]!["SdkDirectory"] = "/usr/share/dotnet/../dotnet/sdk/10.0.301"; break;
            case "unknown member": manifest["Unexpected"] = true; break;
            case "missing sdk": manifest.Remove("Sdk"); break;
        }

        var copy = _baseline.Repository.CopyInputsTo("manifest-mutation");
        AssertFails(ValidateFiles(copy, _baseline.WriteManifest(manifest)), mutation);
    }

    [Fact]
    public void TruncatedManifest_Fails()
    {
        var text = File.ReadAllText(_baseline.ManifestPath);
        var path = Path.Combine(TestEnvironment.NewTemporaryDirectory("truncated"), "input-manifest.json");
        File.WriteAllText(path, text[..(text.Length / 2)]);
        AssertFails(ValidateFiles(_baseline.Repository.CopyInputsTo("truncated"), path), "truncated");
    }

    [Fact]
    public void WindowsSdkProvenance_ValidatesInAnotherOs()
    {
        // A manifest captured by the Windows entry point carries Windows SDK paths; inputs stay '/' relative.
        var manifest = _baseline.Manifest();
        manifest["Sdk"]!["Executable"] = @"C:\Program Files\dotnet\dotnet.exe";
        manifest["Sdk"]!["SdkDirectory"] = @"C:\Program Files\dotnet\sdk\10.0.301";
        var result = ValidateFiles(_baseline.Repository.CopyInputsTo("windows-provenance"), _baseline.WriteManifest(manifest));
        Assert.True(result.ExitCode == 0, result.Combined);
    }

    [Fact]
    public void WrongPinnedGlobalJson_Fails()
    {
        var copy = _baseline.Repository.CopyInputsTo("global-json");
        File.WriteAllText(Path.Combine(copy, "global.json"), "{\n  \"sdk\": {\n    \"version\": \"10.0.301\",\n    \"rollForward\": \"latestFeature\"\n  }\n}\n");
        AssertFails(ValidateFiles(copy, _baseline.ManifestPath), "rollForward");
    }

    [Fact]
    public void MissingOrRelativeDotnet_Fails()
    {
        var copy = _baseline.Repository.CopyInputsTo("dotnet-path");
        AssertFails(TestEnvironment.RunCli(copy, "validate", "--root", copy, "--dotnet", "dotnet", "--manifest", _baseline.ManifestPath, "--files-only"), "relative dotnet");
        AssertFails(TestEnvironment.RunCli(copy, "validate", "--root", copy, "--dotnet", Path.Combine(copy, "missing-dotnet"), "--manifest", _baseline.ManifestPath, "--files-only"), "missing dotnet");
    }

    [Fact]
    public void UnknownOptionsAndCommands_Fail()
    {
        var copy = _baseline.Repository.CopyInputsTo("arguments");
        AssertFails(TestEnvironment.RunCli(copy, "validate", "--root", copy, "--dotnet", TestEnvironment.Dotnet, "--manifest", _baseline.ManifestPath, "--files-only", "--skip-hash"), "unknown option");
        AssertFails(TestEnvironment.RunCli(copy, "apply", "--root", copy, "--dotnet", TestEnvironment.Dotnet), "unknown command");
        AssertFails(TestEnvironment.RunCli(copy, "validate", "--root", copy, "--dotnet", TestEnvironment.Dotnet, "--files-only"), "missing manifest");
    }

    [LinuxFact]
    public void LinkedInputFileOrDirectory_Fails()
    {
        var fileCopy = _baseline.Repository.CopyInputsTo("linked-file");
        var file = Path.Combine(fileCopy, Source);
        var target = Path.Combine(TestEnvironment.NewTemporaryDirectory("link-target"), "AlphaSource.cs");
        File.Move(file, target);
        File.CreateSymbolicLink(file, target);
        AssertFails(ValidateFiles(fileCopy, _baseline.ManifestPath), "linked file");

        var directoryCopy = _baseline.Repository.CopyInputsTo("linked-directory");
        var directory = Path.Combine(directoryCopy, "02_Server/Alpha");
        var moved = Path.Combine(TestEnvironment.NewTemporaryDirectory("link-target"), "Alpha");
        Directory.Move(directory, moved);
        Directory.CreateSymbolicLink(directory, moved);
        AssertFails(ValidateFiles(directoryCopy, _baseline.ManifestPath), "linked directory");
    }

    [Fact]
    public void Snapshot_CopiesExactInputsOnlyIntoEmptyDestination()
    {
        var copy = _baseline.Repository.CopyInputsTo("snapshot-source");
        var destination = TestEnvironment.NewTemporaryDirectory("snapshot");
        var report = Path.Combine(TestEnvironment.NewTemporaryDirectory("snapshot-report"), "copy.json");
        var result = TestEnvironment.RunCli(copy, "snapshot", "--root", copy, "--after", destination, "--dotnet", TestEnvironment.Dotnet, "--manifest", _baseline.ManifestPath, "--out", report);
        Assert.True(result.ExitCode == 0, result.Combined);
        var expected = _baseline.Manifest()["Files"]!.AsArray().Select(file => (string)file!["Path"]!).Order(StringComparer.Ordinal);
        var actual = Directory.EnumerateFiles(destination, "*", SearchOption.AllDirectories).Select(path => Path.GetRelativePath(destination, path).Replace('\\', '/')).Order(StringComparer.Ordinal);
        Assert.Equal(expected, actual);

        AssertFails(TestEnvironment.RunCli(copy, "snapshot", "--root", copy, "--after", destination, "--dotnet", TestEnvironment.Dotnet, "--manifest", _baseline.ManifestPath, "--out", report), "nonempty destination");
    }

    [Fact]
    public void Snapshot_OfStaleSource_FailsBeforeCopying()
    {
        var copy = _baseline.Repository.CopyInputsTo("stale-source");
        File.AppendAllText(Path.Combine(copy, Source), "\n");
        var destination = TestEnvironment.NewTemporaryDirectory("stale-snapshot");
        var report = Path.Combine(TestEnvironment.NewTemporaryDirectory("stale-report"), "copy.json");
        AssertFails(TestEnvironment.RunCli(copy, "snapshot", "--root", copy, "--after", destination, "--dotnet", TestEnvironment.Dotnet, "--manifest", _baseline.ManifestPath, "--out", report), "stale snapshot");
        Assert.Empty(Directory.EnumerateFileSystemEntries(destination));
        Assert.False(File.Exists(report));
    }

    public static TheoryData<string> GitMutations => new()
    {
        "untracked non-input file",
        "modified ignore rules",
        "new commit",
        "staged change",
    };

    [Theory]
    [MemberData(nameof(GitMutations))]
    public void GitCheckoutOrStatusChange_FailsGitValidation(string mutation)
    {
        var clone = TestEnvironment.NewTemporaryDirectory("git-clone");
        var cloned = TestEnvironment.Run("git", clone, ["clone", "-q", "--no-hardlinks", _baseline.Repository.Root, "repo"], TestEnvironment.IsolatedGit());
        Assert.True(cloned.ExitCode == 0, cloned.Combined);
        var root = Path.Combine(clone, "repo");
        var baseline = TestEnvironment.RunCli(root, "validate", "--root", root, "--dotnet", TestEnvironment.Dotnet, "--manifest", _baseline.ManifestPath, "--files-only", "--git");
        Assert.True(baseline.ExitCode == 0, baseline.Combined);

        switch (mutation)
        {
            case "untracked non-input file":
                File.WriteAllText(Path.Combine(root, "notes.md"), "local\n");
                break;
            case "modified ignore rules":
                File.AppendAllText(Path.Combine(root, ".gitignore"), "*.tmp\n");
                break;
            case "new commit":
                File.WriteAllText(Path.Combine(root, "notes.md"), "committed\n");
                Git(root, "add", "notes.md");
                Git(root, "commit", "-q", "-m", "change");
                break;
            case "staged change":
                File.WriteAllText(Path.Combine(root, "notes.md"), "staged\n");
                Git(root, "add", "notes.md");
                break;
        }

        AssertFails(TestEnvironment.RunCli(root, "validate", "--root", root, "--dotnet", TestEnvironment.Dotnet, "--manifest", _baseline.ManifestPath, "--files-only", "--git"), mutation);
    }

    public static TheoryData<string, bool> Reports => new()
    {
        { "[]", true },
        { "[{\"FilePath\":\"02_Server/Alpha/AlphaSource.cs\"}]", false },
        { "[{\"FilePath\":\"ROOT/02_Server/Alpha/AlphaSource.cs\"}]", false },
    };

    [Theory]
    [MemberData(nameof(Reports))]
    public void FormatterReport_AllowedPathsWithoutZeroRequirement_Pass(string report, bool empty)
    {
        var root = _baseline.Repository.Root;
        var result = CheckReport(report.Replace("ROOT/", JsonEscape(root + Path.DirectorySeparatorChar), StringComparison.Ordinal), zero: false);
        Assert.True(result.ExitCode == 0, result.Combined);
        if (empty) Assert.True(CheckReport(report, zero: true).ExitCode == 0);
    }

    // Only the first case relies on the zero requirement; every other case must fail on its path or shape alone.
    public static TheoryData<string, string, bool> BadReports => new()
    {
        { "nonzero with zero requirement", "[{\"FilePath\":\"02_Server/Alpha/AlphaSource.cs\"}]", true },
        { "generated source", "[{\"FilePath\":\"98_Shared/Protocol/Generated/GenPackets.cs\"}]", false },
        { "build input", "[{\"FilePath\":\"Dawnholder.slnx\"}]", false },
        { "project file", "[{\"FilePath\":\"02_Server/Alpha/Alpha.csproj\"}]", false },
        { "outside manifest", "[{\"FilePath\":\"02_Server/Alpha/Missing.cs\"}]", false },
        { "escaping path", "[{\"FilePath\":\"../elsewhere/AlphaSource.cs\"}]", false },
        { "absolute outside root", "[{\"FilePath\":\"/tmp/AlphaSource.cs\"}]", false },
        { "missing path", "[{\"Other\":1}]", false },
        { "not an array", "{\"FilePath\":\"02_Server/Alpha/AlphaSource.cs\"}", false },
        { "not json", "formatter crashed", false },
    };

    [Theory]
    [MemberData(nameof(BadReports))]
    public void FormatterReport_UnexpectedContent_Fails(string name, string report, bool zero)
    {
        AssertFails(CheckReport(report, zero), name);
    }

    private static JsonObject Clone(JsonObject entry, string property, string value)
    {
        var copy = entry.DeepClone().AsObject();
        copy[property] = value;
        return copy;
    }

    private static string JsonEscape(string value) => System.Text.Json.JsonSerializer.Serialize(value)[1..^1];

    private static void Git(string root, params string[] arguments)
    {
        var result = TestEnvironment.Run("git", root, arguments, TestEnvironment.IsolatedGit());
        Assert.True(result.ExitCode == 0, result.Combined);
    }

    private static ProcessResult ValidateFiles(string root, string manifest) =>
        TestEnvironment.RunCli(root, "validate", "--root", root, "--dotnet", TestEnvironment.Dotnet, "--manifest", manifest, "--files-only");

    private static void AssertFails(ProcessResult result, string scenario)
    {
        Assert.True(result.ExitCode != 0, $"{scenario}: expected failure but exit 0.\n{result.Combined}");
        Assert.Contains("ERROR:", result.StandardError, StringComparison.Ordinal);
        Assert.DoesNotContain("Validated", result.StandardOutput, StringComparison.Ordinal);
    }

    private ProcessResult CheckReport(string report, bool zero)
    {
        var path = Path.Combine(TestEnvironment.NewTemporaryDirectory("report"), "report.json");
        File.WriteAllText(path, report);
        var root = _baseline.Repository.Root;
        var arguments = new List<string> { "check-report", "--root", root, "--dotnet", TestEnvironment.Dotnet, "--manifest", _baseline.ManifestPath, "--report", path };
        if (zero) arguments.Add("--zero");
        return TestEnvironment.RunCli(root, arguments.ToArray());
    }
}

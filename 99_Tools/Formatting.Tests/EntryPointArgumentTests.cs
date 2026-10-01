using Dawnholder.Tools.Formatting.Tests.Support;

namespace Dawnholder.Tools.Formatting.Tests;

/// <summary>
/// Boundary checks of the Linux/CI entry point that must stop before any build or inspection:
/// bad arguments, unusable state/evidence locations and missing or linked manifests.
/// </summary>
public sealed class EntryPointArgumentTests
{
    private static string Script => Path.Combine(TestEnvironment.RepositoryRoot, "99_Tools", "format-check.sh");

    [LinuxFact]
    public void UnknownArgument_FailsBeforeCreatingState()
    {
        var state = TestEnvironment.NewTemporaryDirectory("state");
        var result = Run(state, "--verify-only");
        Assert.True(result.ExitCode != 0, result.Combined);
        Assert.Contains("Unknown argument", result.StandardError, StringComparison.Ordinal);
        Assert.Empty(Directory.EnumerateFileSystemEntries(state));
    }

    [LinuxFact]
    public void OptionWithoutValue_Fails()
    {
        var state = TestEnvironment.NewTemporaryDirectory("state");
        var result = Run(state, "--manifest");
        Assert.True(result.ExitCode != 0, result.Combined);
        Assert.Empty(Directory.EnumerateFileSystemEntries(state));
    }

    [LinuxFact]
    public void RelativeStateBase_Fails()
    {
        var result = TestEnvironment.Run("/bin/bash", TestEnvironment.RepositoryRoot, [Script], new Dictionary<string, string?> { ["DAWNHOLDER_FORMAT_STATE_BASE"] = "relative/state" });
        Assert.True(result.ExitCode != 0, result.Combined);
        Assert.Contains("absolute", result.StandardError, StringComparison.Ordinal);
    }

    [LinuxFact]
    public void NonEmptyEvidenceDirectory_Fails()
    {
        var evidence = TestEnvironment.NewTemporaryDirectory("evidence");
        File.WriteAllText(Path.Combine(evidence, "previous.log"), "old run\n");
        var result = Run(TestEnvironment.NewTemporaryDirectory("state"), "--evidence", evidence);
        Assert.True(result.ExitCode != 0, result.Combined);
        Assert.Contains("not empty", result.StandardError, StringComparison.Ordinal);
        Assert.Equal(["previous.log"], Directory.EnumerateFileSystemEntries(evidence).Select(Path.GetFileName));
    }

    [LinuxFact]
    public void MissingManifest_FailsBeforeToolBuild()
    {
        var evidence = Path.Combine(TestEnvironment.NewTemporaryDirectory("evidence-parent"), "evidence");
        var result = Run(TestEnvironment.NewTemporaryDirectory("state"), "--manifest", Path.Combine(evidence, "absent.json"), "--evidence", evidence);
        Assert.True(result.ExitCode != 0, result.Combined);
        Assert.Contains("Missing or linked manifest", result.StandardError, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(evidence, "tool-build.command.txt")));
    }

    [LinuxFact]
    public void LinkedManifest_Fails()
    {
        var directory = TestEnvironment.NewTemporaryDirectory("linked-manifest");
        var target = Path.Combine(directory, "real.json");
        File.WriteAllText(target, "{}");
        var link = Path.Combine(directory, "link.json");
        File.CreateSymbolicLink(link, target);
        var result = Run(TestEnvironment.NewTemporaryDirectory("state"), "--manifest", link);
        Assert.True(result.ExitCode != 0, result.Combined);
        Assert.Contains("Missing or linked manifest", result.StandardError, StringComparison.Ordinal);
    }

    private static ProcessResult Run(string stateBase, params string[] arguments) =>
        TestEnvironment.Run("/bin/bash", TestEnvironment.RepositoryRoot, new[] { Script }.Concat(arguments), new Dictionary<string, string?> { ["DAWNHOLDER_FORMAT_STATE_BASE"] = stateBase });
}

using Dawnholder.Tools.Formatting.Tests.Support;

namespace Dawnholder.Tools.Formatting.Tests;

/// <summary>
/// D-1 contract of the Linux/CI formatting entry point: the script itself sets
/// DOTNET_ADD_GLOBAL_TOOLS_TO_PATH=0 before its first SDK probe, whatever the caller passed, and
/// every later dotnet child inherits it. A recording fake host, selected through the product's own
/// override, captures the environment each invocation really receives; no real SDK runs here.
/// </summary>
public sealed class ProcessEnvironmentGuardTests
{
    private const string Guard = "DOTNET_ADD_GLOBAL_TOOLS_TO_PATH";
    private const string Certificate = "DOTNET_GENERATE_ASPNET_CERTIFICATE";

    // Records one tab-separated line per invocation, then answers only the calls before the first restore.
    private const string RecordingHost = """
        #!/bin/sh
        if [ "${DOTNET_ADD_GLOBAL_TOOLS_TO_PATH+x}" = x ]; then guard="set:$DOTNET_ADD_GLOBAL_TOOLS_TO_PATH"; else guard=unset; fi
        if [ "${DOTNET_GENERATE_ASPNET_CERTIFICATE+x}" = x ]; then certificate="set:$DOTNET_GENERATE_ASPNET_CERTIFICATE"; else certificate=unset; fi
        printf '%s\t%s\t%s\t%s\n' "$guard" "$certificate" "${DOTNET_CLI_HOME-}" "$*" >> "$FAKE_DOTNET_RECORD"
        case "$1" in
          --version) echo 10.0.301; exit 0 ;;
          format) exit 0 ;;
        esac
        exit 9
        """;

    // Unset and every spelling the SDK reads as true or as "use the default" must still end up disabled.
    public static TheoryData<string?> CallerValues => new() { null, "true", "1", "yes", "" };

    private static string Script => Path.Combine(TestEnvironment.RepositoryRoot, "99_Tools", "format-check.sh");

    [LinuxTheory]
    [MemberData(nameof(CallerValues))]
    public void FormatEntryPoint_DisablesPathAdditionFromFirstSdkProbe(string? callerValue)
    {
        var work = TestEnvironment.NewTemporaryDirectory("path-guard");
        var record = Path.Combine(work, "invocations.tsv");
        var stateBase = Path.Combine(work, "states");
        var manifest = Path.Combine(work, "manifest.json");
        File.WriteAllText(manifest, "{}");
        var result = TestEnvironment.Run("/bin/bash", TestEnvironment.RepositoryRoot, [Script, "--manifest", manifest], new Dictionary<string, string?>
        {
            [Guard] = callerValue,
            [Certificate] = null,
            ["DOTNET_CLI_HOME"] = null,
            ["DAWNHOLDER_DOTNET"] = WriteHost(work),
            ["DAWNHOLDER_FORMAT_STATE_BASE"] = stateBase,
            ["FAKE_DOTNET_RECORD"] = record,
        });

        // The fake host stops the run at the tool restore, after the SDK probe and the formatter probe.
        Assert.True(result.ExitCode != 0, result.Combined);
        Assert.Contains("tool-restore exited 9", result.StandardError, StringComparison.Ordinal);
        var calls = File.ReadAllLines(record).Select(line => line.Split('\t')).ToList();
        Assert.Equal(["--version", "format --version", "restore 99_Tools/Formatting/Formatting.csproj"], calls.Select(call => call[3]));
        Assert.All(calls, call => Assert.Equal("set:0", call[0]));
        Assert.All(calls, call => Assert.Equal("set:false", call[1]));
        Assert.All(calls, call => Assert.StartsWith(stateBase + "/", call[2], StringComparison.Ordinal));
    }

    private static string WriteHost(string work)
    {
        var path = Path.Combine(work, "fake-sdk", "dotnet");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, RecordingHost.Replace("\r\n", "\n", StringComparison.Ordinal) + "\n");
        File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        return path;
    }
}

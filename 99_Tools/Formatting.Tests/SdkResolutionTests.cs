using Dawnholder.Tools.Formatting.Tests.Support;

namespace Dawnholder.Tools.Formatting.Tests;

/// <summary>
/// C-1 contract of the shared WSL SDK selection (sdk.sh), exercised as a black box with fake hosts:
/// explicit override → approved dedicated SDK → PATH → ~/.dotnet, and no silent fallback after a
/// wrong override or version. The approved path is hidden in a private mount namespace when present.
/// </summary>
public sealed class SdkResolutionTests
{
    private const string Approved = "/home/bass1/.local/share/dawnholder/dotnet-10.0.301/dotnet";
    private const string ApprovedParent = "/home/bass1/.local/share/dawnholder";

    // Prints the selection and what a child process inherits, using the product function itself.
    private const string Harness = """
        set -euo pipefail
        fail() { echo "ERROR: $*" >&2; exit 1; }
        source "$1"
        dawnholder_sdk "$2"
        printf 'DOTNET=%s\nHOST=%s\nROOT=%s\n' "$DOTNET" "$DOTNET_HOST_PATH" "$DOTNET_ROOT"
        bash -c 'printf "CHILD_HOST=%s\n" "$DOTNET_HOST_PATH"'
        """;

    private readonly string _work = TestEnvironment.NewTemporaryDirectory("sdk-resolution");

    [LinuxFact]
    public void ExplicitOverride_WinsOverApprovedAndPath()
    {
        var root = PinnedRoot();
        var overrideHost = FakeHost("override", "10.0.301", root);
        var pathHost = FakeHost("path", "10.0.301", root);
        var result = RunHarness(root, overrideHost, Path.GetDirectoryName(pathHost)! + ":/usr/bin:/bin", hideApproved: false);
        Assert.True(result.ExitCode == 0, result.Combined);
        Assert.Contains($"DOTNET={overrideHost}\n", result.StandardOutput, StringComparison.Ordinal);
        Assert.Contains($"HOST={overrideHost}\n", result.StandardOutput, StringComparison.Ordinal);
        Assert.Contains($"CHILD_HOST={overrideHost}\n", result.StandardOutput, StringComparison.Ordinal);
        Assert.Contains($"ROOT={Path.GetDirectoryName(overrideHost)}\n", result.StandardOutput, StringComparison.Ordinal);
    }

    public static TheoryData<string> BadOverrides => new()
    {
        "missing",
        "empty",
        "relative",
        "wrong version",
        "failing host",
        "not executable",
    };

    [LinuxTheory]
    [MemberData(nameof(BadOverrides))]
    public void BadExplicitOverride_FailsWithoutFallback(string kind)
    {
        var root = PinnedRoot();
        var good = FakeHost("path-good", "10.0.301", root);
        var value = kind switch
        {
            "missing" => Path.Combine(_work, "absent", "dotnet"),
            "empty" => string.Empty,
            "relative" => "dotnet",
            "wrong version" => FakeHost("old", "10.0.300", root),
            "failing host" => FakeHost("failing", "10.0.301", root, exitCode: 3),
            _ => NonExecutable(),
        };
        var result = RunHarness(root, value, Path.GetDirectoryName(good)! + ":/usr/bin:/bin", hideApproved: false);
        Assert.True(result.ExitCode != 0, $"{kind}: expected failure.\n{result.Combined}");
        Assert.True(result.StandardError.Contains("ERROR: Required SDK 10.0.301", StringComparison.Ordinal), result.Combined);
        Assert.DoesNotContain("DOTNET=", result.StandardOutput, StringComparison.Ordinal);
    }

    [LinuxFact]
    public void VersionIsObservedInTheInspectedRoot()
    {
        // The fake host reports the pinned version only from the root whose global.json applies.
        var root = PinnedRoot();
        var host = FakeHost("cwd", "10.0.301", root);
        var result = RunHarness(root, host, "/usr/bin:/bin", hideApproved: false, workingDirectory: "/");
        Assert.True(result.ExitCode == 0, result.Combined);
    }

    [LinuxFact]
    public void WithoutOverride_ApprovedDedicatedSdkWinsOverPath()
    {
        // Where the approved install is absent (CI), the same rule selects the PATH host.
        var root = PinnedRoot();
        var pathHost = FakeHost("path", "10.0.301", root);
        var result = RunHarness(root, null, Path.GetDirectoryName(pathHost)! + ":/usr/bin:/bin", hideApproved: false);
        Assert.True(result.ExitCode == 0, result.Combined);
        var expected = File.Exists(Approved) ? RealPath(Approved) : pathHost;
        Assert.Contains($"DOTNET={expected}\n", result.StandardOutput, StringComparison.Ordinal);
        Assert.Contains($"CHILD_HOST={expected}\n", result.StandardOutput, StringComparison.Ordinal);
    }

    [LinuxFact]
    public void WithoutApproved_PathDotnetIsSelected()
    {
        var root = PinnedRoot();
        var pathHost = FakeHost("path", "10.0.301", root);
        var home = FakeHome(root, "10.0.301");
        var result = RunHarness(root, null, Path.GetDirectoryName(pathHost)! + ":/usr/bin:/bin", hideApproved: true, home: home);
        Assert.True(result.ExitCode == 0, result.Combined);
        Assert.Contains($"DOTNET={pathHost}\n", result.StandardOutput, StringComparison.Ordinal);
    }

    [LinuxFact]
    public void WrongPathVersion_DoesNotFallBackToHomeDotnet()
    {
        var root = PinnedRoot();
        var pathHost = FakeHost("path-old", "10.0.300", root);
        var home = FakeHome(root, "10.0.301");
        var result = RunHarness(root, null, Path.GetDirectoryName(pathHost)! + ":/usr/bin:/bin", hideApproved: true, home: home);
        Assert.True(result.ExitCode != 0, result.Combined);
        Assert.Contains("observed '10.0.300'", result.StandardError, StringComparison.Ordinal);
    }

    [LinuxFact]
    public void WithoutApprovedOrPath_HomeDotnetIsLastFallback()
    {
        var root = PinnedRoot();
        var home = FakeHome(root, "10.0.301");
        var result = RunHarness(root, null, EmptyPath(), hideApproved: true, home: home);
        Assert.True(result.ExitCode == 0, result.Combined);
        Assert.Contains($"DOTNET={Path.Combine(home, ".dotnet", "dotnet")}\n", result.StandardOutput, StringComparison.Ordinal);
    }

    [LinuxFact]
    public void NoSdkAnywhere_Fails()
    {
        var root = PinnedRoot();
        var home = TestEnvironment.NewTemporaryDirectory("empty-home");
        var result = RunHarness(root, null, EmptyPath(), hideApproved: true, home: home);
        Assert.True(result.ExitCode != 0, result.Combined);
        Assert.Contains("executable missing", result.StandardError, StringComparison.Ordinal);
    }

    [LinuxFact]
    public void GlobalJsonWithoutExactPin_Fails()
    {
        foreach (var pin in new[] { "{\"sdk\":{\"version\":\"10.0.300\",\"rollForward\":\"disable\"}}", "{\"sdk\":{\"version\":\"10.0.301\",\"rollForward\":\"latestFeature\"}}", "{}" })
        {
            var root = RealPath(TestEnvironment.NewTemporaryDirectory("pin"));
            File.WriteAllText(Path.Combine(root, "global.json"), pin);
            var host = FakeHost("pin", "10.0.301", root);
            var result = RunHarness(root, host, "/usr/bin:/bin", hideApproved: false);
            Assert.True(result.ExitCode != 0, $"{pin}\n{result.Combined}");
        }
    }

    private static ProcessResult RunHarness(string root, string? overrideValue, string path, bool hideApproved, string? home = null, string? workingDirectory = null)
    {
        var sdkScript = Path.Combine(TestEnvironment.RepositoryRoot, "99_Tools", "Formatting", "sdk.sh");
        var environment = new Dictionary<string, string?>
        {
            ["PATH"] = path,
            ["DAWNHOLDER_DOTNET"] = overrideValue,
            ["DOTNET_HOST_PATH"] = null,
            ["DOTNET_ROOT"] = null,
        };
        if (home != null) environment["HOME"] = home;
        var arguments = new List<string>();
        if (hideApproved && Directory.Exists(ApprovedParent))
        {
            // Hide only inside a private user/mount namespace; the real install is never touched.
            var empty = TestEnvironment.NewTemporaryDirectory("hidden-sdk");
            arguments.AddRange(["--user", "--map-root-user", "--mount", "/bin/bash", "-c", "/usr/bin/mount --bind \"$0\" \"$1\" && shift && exec /bin/bash -c \"$@\"", empty, ApprovedParent, Harness, "harness", sdkScript, root]);
            return TestEnvironment.Run("/usr/bin/unshare", workingDirectory ?? root, arguments, environment);
        }

        arguments.AddRange(["-c", Harness, "harness", sdkScript, root]);
        return TestEnvironment.Run("/bin/bash", workingDirectory ?? root, arguments, environment);
    }

    private static string RealPath(string path)
    {
        var result = TestEnvironment.Run("/usr/bin/realpath", "/", ["--", path]);
        Assert.True(result.ExitCode == 0, result.Combined);
        return result.StandardOutput.Trim();
    }

    private static string PinnedRoot()
    {
        var root = RealPath(TestEnvironment.NewTemporaryDirectory("pinned-root"));
        File.Copy(Path.Combine(TestEnvironment.RepositoryRoot, "global.json"), Path.Combine(root, "global.json"));
        return root;
    }

    private string FakeHost(string name, string version, string root, int exitCode = 0)
    {
        var directory = Path.Combine(RealPath(_work), name + "-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "dotnet");
        File.WriteAllText(path, $"#!/bin/sh\nif [ \"$(pwd -P)\" != \"{root}\" ]; then echo wrong-cwd; exit 0; fi\necho {version}\nexit {exitCode}\n");
        File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        return path;
    }

    private string FakeHome(string root, string version)
    {
        var home = TestEnvironment.NewTemporaryDirectory("home");
        var host = FakeHost("home", version, root);
        Directory.CreateDirectory(Path.Combine(home, ".dotnet"));
        File.Copy(host, Path.Combine(home, ".dotnet", "dotnet"));
        File.SetUnixFileMode(Path.Combine(home, ".dotnet", "dotnet"), UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        return home;
    }

    private string NonExecutable()
    {
        var path = Path.Combine(_work, "plain-" + Guid.NewGuid().ToString("N"));
        File.WriteAllText(path, "#!/bin/sh\necho 10.0.301\n");
        return path;
    }

    // Only coreutils/python3 are needed by sdk.sh; a directory without dotnet keeps PATH lookup empty.
    private string EmptyPath()
    {
        var bin = Path.Combine(_work, "bin-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(bin);
        foreach (var tool in new[] { "python3", "realpath", "dirname", "cat", "bash", "sh" })
        {
            var source = new[] { "/usr/bin", "/bin" }.Select(directory => Path.Combine(directory, tool)).First(File.Exists);
            File.CreateSymbolicLink(Path.Combine(bin, tool), source);
        }

        return bin;
    }
}

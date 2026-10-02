using System.Diagnostics;
using System.Text;

namespace Dawnholder.Tools.Formatting.Tests.Support;

/// <summary>Locates the repository, the pinned SDK host and the built formatting CLI for subprocess tests.</summary>
internal static class TestEnvironment
{
    public const string RequiredSdk = "10.0.301";

    public static string RepositoryRoot { get; } = FindRepositoryRoot();

    /// <summary>The host selected by the entry point. Tests never guess another SDK.</summary>
    public static string Dotnet
    {
        get
        {
            var path = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
            if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path) || !File.Exists(path))
                throw new InvalidOperationException($"DOTNET_HOST_PATH must name the pinned SDK host; got '{path}'.");
            return path;
        }
    }

    public static string FormattingCli
    {
        get
        {
            var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent?.Name ?? "Debug";
            var path = Path.Combine(RepositoryRoot, "99_Tools", "Formatting", "bin", configuration, "net10.0", "Formatting.dll");
            if (!File.Exists(path)) throw new InvalidOperationException($"Formatting CLI is not built: {path}");
            return path;
        }
    }

    public static ProcessResult Run(string executable, string workingDirectory, IEnumerable<string> arguments, IReadOnlyDictionary<string, string?>? environment = null, int timeoutSeconds = 600)
    {
        var info = new ProcessStartInfo(executable)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (var argument in arguments) info.ArgumentList.Add(argument);
        if (environment != null)
        {
            foreach (var (name, value) in environment)
            {
                if (value == null) info.Environment.Remove(name);
                else info.Environment[name] = value;
            }
        }

        using var process = Process.Start(info) ?? throw new InvalidOperationException($"Cannot start {executable}.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(TimeSpan.FromSeconds(timeoutSeconds)))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"{executable} {string.Join(' ', arguments)} did not exit in {timeoutSeconds}s.");
        }

        process.WaitForExit();
        return new ProcessResult(process.ExitCode, stdout.Result, stderr.Result);
    }

    /// <summary>Runs the formatting CLI with the pinned host and an isolated Git configuration.</summary>
    public static ProcessResult RunCli(string workingDirectory, params string[] arguments) =>
        Run(Dotnet, workingDirectory, new[] { FormattingCli }.Concat(arguments), IsolatedGit());

    public static IReadOnlyDictionary<string, string?> IsolatedGit()
    {
        var empty = Path.Combine(Path.GetTempPath(), "dawnholder-formatting-tests-empty.gitconfig");
        if (!File.Exists(empty)) File.WriteAllText(empty, string.Empty);
        return new Dictionary<string, string?>
        {
            ["GIT_CONFIG_NOSYSTEM"] = "1",
            ["GIT_CONFIG_GLOBAL"] = empty,
            ["GIT_AUTHOR_NAME"] = "Formatting Tests",
            ["GIT_AUTHOR_EMAIL"] = "formatting-tests@localhost",
            ["GIT_COMMITTER_NAME"] = "Formatting Tests",
            ["GIT_COMMITTER_EMAIL"] = "formatting-tests@localhost",
        };
    }

    public static string NewTemporaryDirectory(string purpose)
    {
        var path = Path.Combine(Path.GetTempPath(), "dawnholder-formatting-tests", $"{purpose}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Dawnholder.slnx")) && File.Exists(Path.Combine(directory.FullName, "global.json")))
                return directory.FullName;
        }

        throw new InvalidOperationException("Repository root with Dawnholder.slnx was not found above the test output.");
    }
}

internal sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError)
{
    public string Combined => StandardOutput + StandardError;
}

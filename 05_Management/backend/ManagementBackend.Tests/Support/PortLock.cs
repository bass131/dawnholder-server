using System.Diagnostics;

namespace Dawnholder.Management.Backend.Tests.Support;

/// <summary>
/// Observes and holds the server port lock the same way 99_Tools/sync-wsl.sh does: util-linux flock on the file.
/// .NET file sharing is not used because it is not known to conflict with flock (backend-design.md).
/// </summary>
internal static class PortLock
{
    /// <summary>True when another process could take the lock right now (flock -n succeeds and releases at once).</summary>
    public static async Task<bool> IsFreeAsync(string lockFile)
    {
        ProcessResult result = await ProcessRunner.RunAsync(
            "flock", ["-n", lockFile, "true"], Path.GetDirectoryName(lockFile)!, TimeSpan.FromSeconds(10));
        Assert.True(result.ExitCode is 0 or 1, $"flock probe failed: {result.Describe()}");
        return result.ExitCode == 0;
    }

    /// <summary>
    /// Starts a separate holder process. bash opens the file without truncating it, takes the lock, then execs sleep,
    /// so a single process keeps the descriptor until it is killed.
    /// </summary>
    public static async Task<Process> HoldAsync(string lockFile)
    {
        ProcessStartInfo start = new("bash")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (string argument in new[] { "-c", "exec 9<>\"$1\" && flock -n 9 && echo locked && exec sleep 300", "hold-lock", lockFile })
        {
            start.ArgumentList.Add(argument);
        }

        Process holder = Process.Start(start) ?? throw new InvalidOperationException("Cannot start the lock holder.");
        string? line = await holder.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(10));
        if (line != "locked")
        {
            holder.Kill(entireProcessTree: true);
            Assert.Fail($"Lock holder did not take {lockFile}: {line} {await holder.StandardError.ReadToEndAsync()}");
        }

        return holder;
    }
}

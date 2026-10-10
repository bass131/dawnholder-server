using System.Text.Json.Nodes;
using Dawnholder.Management.Backend.Tests.Support;

namespace Dawnholder.Management.Backend.Tests;

/// <summary>
/// Processes a release build leaves behind (backend-design.md 「운영 실행본」 3): a build over buildTimeoutSeconds ends
/// its own process tree and is buildFailed without partial output, and a finished build leaves no MSBuild or compiler
/// server even when the backend's environment does not switch them off.
/// </summary>
public sealed class ReleaseBuildLifetimeTests
{
    static readonly TimeSpan ProcessExitWait = TimeSpan.FromSeconds(15);

    [LinuxFact]
    public async Task BuildOverTimeLimitIsBuildFailedAndLeavesNoOutputOrBuildProcess()
    {
        await using BackendScenario scenario = BackendScenario.Create("build-timeout");
        scenario.Settings.BuildTimeoutSeconds = 1;
        ReleaseSourceRepository source = await scenario.CreateReleaseSourceAsync();
        BackendProcess backend = await scenario.StartBackendAsync();

        ApiResult build = await scenario.PostReleaseAsync(backend, source.FirstCommit);
        IReadOnlyList<string> remaining = await WaitForNoDescendantsAsync(scenario.Home, backend.Pid);
        IReadOnlyList<string> leftovers = DataLayout.DirectoriesNamedFor(scenario.DataDirectory, source.FirstCommit);
        string buildWork = DataLayout.BuildWork(scenario.DataDirectory);
        JsonNode status = await backend.StatusAsync();

        ApiAssert.Error(build, 500, "buildFailed");
        Assert.True(remaining.Count == 0, $"build processes outlived the timeout: {string.Join(" | ", remaining)}");
        Assert.True(leftovers.Count == 0, $"no release folder may remain for a timed-out build: {string.Join(", ", leftovers)}");
        Assert.True(
            !Directory.Exists(buildWork) || !Directory.EnumerateFileSystemEntries(buildWork).Any(),
            $"build-work must not keep the timed-out build folder: {buildWork}");
        Assert.True(JsonFields.IsNull(status, "currentRelease"), $"a failed build must not set the current release: {JsonFields.Show(status)}");
    }

    [LinuxFact]
    public async Task FinishedBuildLeavesNoBuildServerEvenWithoutEnvironmentSwitches()
    {
        await using BackendScenario scenario = BackendScenario.Create("build-servers");
        ReleaseSourceRepository source = await scenario.CreateReleaseSourceAsync();
        Dictionary<string, string?> environment = scenario.BackendEnvironment();
        // Only the backend's own publish arguments may keep build servers away here (null removes the variable).
        environment["MSBUILDDISABLENODEREUSE"] = null;
        environment["DOTNET_CLI_USE_MSBUILD_SERVER"] = null;
        environment["UseSharedCompilation"] = null;
        File.WriteAllText(scenario.ConfigPath, scenario.Settings.ToJson().ToJsonString());
        await using BackendProcess backend = await BackendProcess.StartAsync(
            scenario.ConfigPath, scenario.DataDirectory, scenario.Root, environment);

        await scenario.BuildReleaseAsync(backend, source.FirstCommit);
        IReadOnlyList<string> remaining = await WaitForNoDescendantsAsync(scenario.Home, backend.Pid);

        Assert.True(remaining.Count == 0, $"processes outlived the finished build: {string.Join(" | ", remaining)}");
    }

    /// <summary>
    /// Waits until no process other than the backend carries the scenario's HOME. Every process the backend starts
    /// inherits that HOME, and no other process on the machine has it, so this sees exactly the backend's descendants.
    /// </summary>
    static async Task<IReadOnlyList<string>> WaitForNoDescendantsAsync(string home, int backendPid)
    {
        DateTime deadline = DateTime.UtcNow + ProcessExitWait;
        while (true)
        {
            IReadOnlyList<string> found = ProcessesWithHome(home, backendPid);
            if (found.Count == 0 || DateTime.UtcNow >= deadline)
            {
                return found;
            }

            await Task.Delay(200);
        }
    }

    static IReadOnlyList<string> ProcessesWithHome(string home, int backendPid)
    {
        string entry = "HOME=" + home;
        List<string> found = [];
        foreach (string directory in Directory.EnumerateDirectories("/proc"))
        {
            if (!int.TryParse(Path.GetFileName(directory), out int pid) || pid == backendPid || pid == Environment.ProcessId)
            {
                continue;
            }

            string environ;
            try
            {
                environ = File.ReadAllText(Path.Combine(directory, "environ"));
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            if (environ.Split('\0').Contains(entry) && LinuxProcess.IsAlive(pid))
            {
                found.Add($"{pid} {LinuxProcess.CommandLine(pid)}");
            }
        }

        return found;
    }
}

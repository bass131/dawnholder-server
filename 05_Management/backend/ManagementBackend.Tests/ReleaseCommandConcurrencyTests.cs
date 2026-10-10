using System.Text.Json.Nodes;
using Dawnholder.Management.Backend.Tests.Support;

namespace Dawnholder.Management.Backend.Tests;

/// <summary>
/// Release commands run one at a time on their own (backend-design.md 「상태 소유와 수명」: build and current-release
/// selection are separately one at a time, and both work while the server runs).
/// </summary>
public sealed class ReleaseCommandConcurrencyTests
{
    [LinuxFact]
    public async Task ReleaseCommandsDuringABuildAreBusyWhileStatusAndServerCommandsStillAnswer()
    {
        await using BackendScenario scenario = BackendScenario.Create("release-busy");
        BackendProcess backend = await scenario.StartWithReleaseAsync();
        ReleaseSourceRepository source = scenario.Source!;
        await BackendScenario.StartServerAsync(backend);
        string commitB = await source.CommitMarkerAsync("release-b");

        Task<ApiResult> build = scenario.PostReleaseAsync(backend, commitB);
        // build-work/<folder> is the design's per-build work folder; seeing one means the build has begun.
        await WaitUntilAsync(() => BuildWorkFolders(scenario.DataDirectory) > 0 || build.IsCompleted, TimeSpan.FromSeconds(60));
        ApiResult secondBuild = await scenario.PostReleaseAsync(backend, commitB);
        ApiResult selectDuringBuild = await backend.PutAsync("/api/releases/current", new { commit = source.FirstCommit });
        JsonNode statusDuringBuild = await backend.StatusAsync();
        ApiResult stopDuringBuild = await backend.PostAsync("/api/server/stop");
        bool buildStillRunning = !build.IsCompleted;
        ApiResult built = await build;
        ApiResult selectAfterBuild = await backend.PutAsync("/api/releases/current", new { commit = commitB });
        JsonNode statusAfterBuild = await backend.StatusAsync();

        Assert.True(buildStillRunning, "the concurrent requests must have reached the backend while the build was running");
        ApiAssert.Error(secondBuild, 409, "busy");
        ApiAssert.Error(selectDuringBuild, 409, "busy");
        Assert.Equal("running", JsonFields.Text(statusDuringBuild, "server.state"));
        Assert.Equal(source.FirstCommit, JsonFields.Text(statusDuringBuild, "currentRelease.commit"));
        ApiAssert.Success(stopDuringBuild, "POST /api/server/stop during a release build");
        ApiAssert.Success(built, $"POST /api/releases {commitB}");
        ApiAssert.Success(selectAfterBuild, $"PUT /api/releases/current {commitB}");
        Assert.Equal(commitB, JsonFields.Text(statusAfterBuild, "currentRelease.commit"));
    }

    static int BuildWorkFolders(string dataDirectory)
    {
        string buildWork = DataLayout.BuildWork(dataDirectory);
        return Directory.Exists(buildWork) ? Directory.EnumerateDirectories(buildWork).Count() : 0;
    }

    static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        DateTime deadline = DateTime.UtcNow + timeout;
        while (!condition())
        {
            Assert.True(DateTime.UtcNow < deadline, $"condition not reached within {timeout}");
            await Task.Delay(20);
        }
    }
}

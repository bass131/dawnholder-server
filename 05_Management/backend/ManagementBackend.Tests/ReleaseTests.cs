using System.Text.Json.Nodes;
using Dawnholder.Management.Backend.Tests.Support;

namespace Dawnholder.Management.Backend.Tests;

/// <summary>F: release builds and the current release (backend-design.md 「운영 실행본」, 「실행본 목록 응답」).</summary>
public sealed class ReleaseTests
{
    // Valid lowercase hex that no test repository contains.
    const string AbsentCommit = "0123456789abcdef0123456789abcdef01234567";
    const string PinnedSdk = "10.0.301";

    [LinuxFact]
    public async Task F1_CommitThatIsNotFortyLowercaseHexIsInvalidCommit()
    {
        await using BackendScenario scenario = BackendScenario.Create("f1");
        ReleaseSourceRepository source = await scenario.CreateReleaseSourceAsync();
        BackendProcess backend = await scenario.StartBackendAsync();
        string real = source.FirstCommit;
        (string Name, object Body)[] invalid =
        [
            ("uppercase real commit", new { commit = real.ToUpperInvariant() }),
            ("39 characters", new { commit = real[..39] }),
            ("41 characters", new { commit = real + "0" }),
            ("non-hex", new { commit = new string('g', 40) }),
            ("empty", new { commit = string.Empty }),
            ("number", new { commit = 1234 }),
            ("missing commit", new { other = real }),
        ];

        foreach ((string name, object body) in invalid)
        {
            ApiResult build = await backend.PostAsync("/api/releases", body);
            ApiResult select = await backend.PutAsync("/api/releases/current", body);
            Assert.True(build.Code == 400, $"POST /api/releases with {name}: {build.Describe()}");
            ApiAssert.Error(build, 400, "invalidCommit");
            Assert.True(select.Code == 400, $"PUT /api/releases/current with {name}: {select.Describe()}");
            ApiAssert.Error(select, 400, "invalidCommit");
        }
    }

    [LinuxFact]
    public async Task F2_CommitMissingFromSourceIsCommitNotFound()
    {
        await using BackendScenario scenario = BackendScenario.Create("f2");
        await scenario.CreateReleaseSourceAsync();
        BackendProcess backend = await scenario.StartBackendAsync();

        ApiResult build = await scenario.PostReleaseAsync(backend, AbsentCommit);

        ApiAssert.Error(build, 404, "commitNotFound");
    }

    [LinuxFact]
    public async Task F3_BuildWithoutSourceRepositoryIsReleaseSourceNotConfigured()
    {
        await using BackendScenario scenario = BackendScenario.Create("f3");
        BackendProcess backend = await scenario.StartBackendAsync();

        ApiResult build = await scenario.PostReleaseAsync(backend, AbsentCommit);

        ApiAssert.Error(build, 409, "releaseSourceNotConfigured");
    }

    [LinuxFact]
    public async Task F4_BuildListsReleaseWritesManifestAndLeavesSourceUntouched()
    {
        await using BackendScenario scenario = BackendScenario.Create("f4");
        ReleaseSourceRepository source = await scenario.CreateReleaseSourceAsync();
        string before = await source.WorkingTreeSnapshotAsync();
        BackendProcess backend = await scenario.StartBackendAsync();

        await scenario.BuildReleaseAsync(backend, source.FirstCommit);
        ApiResult list = await backend.GetAsync("/api/releases");
        JsonNode manifest = JsonFields.ReadFile(DataLayout.ReleaseManifest(scenario.DataDirectory, source.FirstCommit));
        string after = await source.WorkingTreeSnapshotAsync();
        string buildWork = DataLayout.BuildWork(scenario.DataDirectory);

        Assert.True(list.Code == 200, $"GET /api/releases: {list.Describe()}");
        JsonNode release = Assert.Single(
            JsonFields.Array(list.Json, "releases"), item => JsonFields.Text(item, "commit") == source.FirstCommit)!;
        Assert.True(JsonFields.IsNull(list.Json, "currentRelease"), "building must not change the current release");
        Assert.Equal(PinnedSdk, JsonFields.Text(release, "sdkVersion"));
        Assert.Equal(source.Root, JsonFields.Text(release, "sourceRepository"));
        JsonFields.Time(release, "builtAt");
        Assert.Equal(source.FirstCommit, JsonFields.Text(manifest, "commit"));
        Assert.Equal(JsonFields.Text(release, "builtAt"), JsonFields.Text(manifest, "builtAt"));
        Assert.Equal(PinnedSdk, JsonFields.Text(manifest, "sdkVersion"));
        Assert.Equal(source.Root, JsonFields.Text(manifest, "sourceRepository"));
        Assert.Equal(before, after);
        Assert.True(
            !Directory.Exists(buildWork) || !Directory.EnumerateFileSystemEntries(buildWork).Any(),
            $"build-work must not keep the build folder: {buildWork}");
    }

    [LinuxFact]
    public async Task F5_SameCommitTwiceIsNotRebuilt()
    {
        await using BackendScenario scenario = BackendScenario.Create("f5");
        ReleaseSourceRepository source = await scenario.CreateReleaseSourceAsync();
        BackendProcess backend = await scenario.StartBackendAsync();

        await scenario.BuildReleaseAsync(backend, source.FirstCommit);
        string firstBuiltAt = await BuiltAtAsync(backend, source.FirstCommit);
        await scenario.BuildReleaseAsync(backend, source.FirstCommit);
        string secondBuiltAt = await BuiltAtAsync(backend, source.FirstCommit);

        Assert.Equal(firstBuiltAt, secondBuiltAt);
    }

    [LinuxFact]
    public async Task F6_CurrentReleaseChangeAppliesFromNextStart()
    {
        await using BackendScenario scenario = BackendScenario.Create("f6");
        BackendProcess backend = await scenario.StartWithReleaseAsync();
        ReleaseSourceRepository source = scenario.Source!;
        string runA = JsonFields.Text(await BackendScenario.StartServerAsync(backend), "server.runId");
        await LogQuery.WaitForTextAsync(backend, $"runId={runA}", "release-marker: release-a", TimeSpan.FromSeconds(20));

        string commitB = await source.CommitMarkerAsync("release-b");
        await scenario.InstallReleaseAsync(backend, commitB);
        JsonNode whileRunning = await backend.StatusAsync();
        await BackendScenario.StopServerAsync(backend);
        JsonNode restarted = await BackendScenario.StartServerAsync(backend);
        string runB = JsonFields.Text(restarted, "server.runId");
        JsonNode logsB = await LogQuery.WaitForTextAsync(backend, $"runId={runB}", "release-marker: release-b", TimeSpan.FromSeconds(20));

        Assert.Equal(source.FirstCommit, JsonFields.Text(whileRunning, "server.release.commit"));
        Assert.Equal(commitB, JsonFields.Text(whileRunning, "currentRelease.commit"));
        Assert.Equal(runA, JsonFields.Text(whileRunning, "server.runId"));
        Assert.Equal(commitB, JsonFields.Text(restarted, "server.release.commit"));
        Assert.DoesNotContain("release-marker: release-a", LogQuery.Texts(logsB));
    }

    [LinuxFact]
    public async Task F7_SelectingUnbuiltCommitIsReleaseNotFound()
    {
        await using BackendScenario scenario = BackendScenario.Create("f7");
        ReleaseSourceRepository source = await scenario.CreateReleaseSourceAsync();
        BackendProcess backend = await scenario.StartBackendAsync();

        ApiResult absent = await backend.PutAsync("/api/releases/current", new { commit = AbsentCommit });
        ApiResult unbuilt = await backend.PutAsync("/api/releases/current", new { commit = source.FirstCommit });
        JsonNode status = await backend.StatusAsync();

        ApiAssert.Error(absent, 404, "releaseNotFound");
        ApiAssert.Error(unbuilt, 404, "releaseNotFound");
        Assert.True(JsonFields.IsNull(status, "currentRelease"), "a refused selection must not set the current release");
    }

    [LinuxFact]
    public async Task F8_FailingBuildIsBuildFailedWithoutPartialOutput()
    {
        await using BackendScenario scenario = BackendScenario.Create("f8");
        BackendProcess backend = await scenario.StartWithReleaseAsync();
        ReleaseSourceRepository source = scenario.Source!;
        string broken = await source.CommitBrokenBuildAsync();

        ApiResult build = await scenario.PostReleaseAsync(backend, broken);
        ApiResult list = await backend.GetAsync("/api/releases");
        IReadOnlyList<string> leftovers = DataLayout.DirectoriesNamedFor(scenario.DataDirectory, broken);
        string buildWork = DataLayout.BuildWork(scenario.DataDirectory);

        ApiAssert.Error(build, 500, "buildFailed");
        Assert.True(leftovers.Count == 0, $"no release folder may remain for a failed build: {string.Join(", ", leftovers)}");
        Assert.True(
            !Directory.Exists(buildWork) || !Directory.EnumerateFileSystemEntries(buildWork).Any(),
            $"build-work must not keep the failed build folder: {buildWork}");
        Assert.DoesNotContain(JsonFields.Array(list.Json, "releases"), item => JsonFields.Text(item, "commit") == broken);
        Assert.Equal(source.FirstCommit, JsonFields.Text(list.Json, "currentRelease.commit"));
    }

    static async Task<string> BuiltAtAsync(BackendProcess backend, string commit)
    {
        ApiResult list = await backend.GetAsync("/api/releases");
        Assert.True(list.Code == 200, $"GET /api/releases: {list.Describe()}");
        JsonNode release = Assert.Single(JsonFields.Array(list.Json, "releases"), item => JsonFields.Text(item, "commit") == commit)!;
        return JsonFields.Text(release, "builtAt");
    }
}

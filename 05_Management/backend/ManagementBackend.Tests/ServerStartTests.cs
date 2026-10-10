using System.Diagnostics;
using System.Globalization;
using System.Text.Json.Nodes;
using Dawnholder.Management.Backend.Tests.Support;

namespace Dawnholder.Management.Backend.Tests;

/// <summary>C: POST /api/server/start (backend-design.md 「게임 서버 실행 수명」 1~3, 「상태 소유와 수명」).</summary>
public sealed class ServerStartTests
{
    [LinuxFact]
    public async Task C1_StartWithoutCurrentReleaseIsNoCurrentRelease()
    {
        await using BackendScenario scenario = BackendScenario.Create("c1");
        BackendProcess backend = await scenario.StartBackendAsync();

        ApiResult start = await backend.PostAsync("/api/server/start");
        JsonNode status = await backend.StatusAsync();

        ApiAssert.Error(start, 409, "noCurrentRelease");
        Assert.Equal("stopped", JsonFields.Text(status, "server.state"));
        Assert.Empty(scenario.Stub.StartedPids());
    }

    [LinuxFact]
    public async Task C2_StartRunsCurrentReleaseAndReportsRunning()
    {
        await using BackendScenario scenario = BackendScenario.Create("c2");
        BackendProcess backend = await scenario.StartWithReleaseAsync();

        ApiResult start = await backend.PostAsync("/api/server/start");
        JsonNode status = await backend.StatusAsync();
        int stubPid = scenario.Stub.SinglePid();
        bool lockFree = await PortLock.IsFreeAsync(scenario.Settings.PortLockFile);
        IReadOnlyList<ListeningSocket> stubSockets = LinuxProcess.ListeningSocketsOf(stubPid);

        ApiAssert.Success(start, "POST /api/server/start");
        Assert.Equal("running", JsonFields.Text(status, "server.state"));
        Assert.Equal(stubPid, JsonFields.Number(status, "server.pid"));
        Assert.False(string.IsNullOrWhiteSpace(JsonFields.Text(status, "server.runId")), "runId is empty");
        JsonFields.Time(status, "server.startedAt");
        Assert.Equal(scenario.Source!.FirstCommit, JsonFields.Text(status, "server.release.commit"));
        JsonFields.Time(status, "server.release.builtAt");
        Assert.True(JsonFields.Flag(status, "server.portListening"), "portListening must be true while the stub listens");
        Assert.Equal("self", JsonFields.Text(status, "server.portOwner"));
        Assert.False(JsonFields.Flag(status, "server.portLockHeldByOther"));
        Assert.False(lockFree, "the backend must hold the port lock while the server runs");
        Assert.Contains(stubSockets, socket => socket.Port == scenario.Stub.Port);
    }

    [LinuxFact]
    public async Task C3_LockHeldByAnotherProcessIsPortBusy()
    {
        await using BackendScenario scenario = BackendScenario.Create("c3");
        BackendProcess backend = await scenario.StartWithReleaseAsync();
        await scenario.HoldPortLockAsync();

        ApiResult start = await backend.PostAsync("/api/server/start");
        JsonNode status = await backend.StatusAsync();

        ApiAssert.Error(start, 409, "portBusy");
        Assert.True(JsonFields.Flag(status, "server.portLockHeldByOther"), $"portLockHeldByOther must be true: {JsonFields.Show(status)}");
        Assert.Equal("stopped", JsonFields.Text(status, "server.state"));
        Assert.Empty(scenario.Stub.StartedPids());
    }

    [LinuxFact]
    public async Task C4_PortListenedByAnotherProcessIsPortBusyAndLeftAlone()
    {
        await using BackendScenario scenario = BackendScenario.Create("c4");
        BackendProcess backend = await scenario.StartWithReleaseAsync();
        StubProcess other = await scenario.StartOtherListenerAsync();

        ApiResult start = await backend.PostAsync("/api/server/start");
        JsonNode status = await backend.StatusAsync();
        bool lockFree = await PortLock.IsFreeAsync(scenario.Settings.PortLockFile);

        ApiAssert.Error(start, 409, "portBusy");
        Assert.Equal("other", JsonFields.Text(status, "server.portOwner"));
        string detail = JsonFields.Show(JsonFields.Field(status, "server.portOwnerDetail"));
        Assert.True(
            detail.Contains(other.Pid.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal),
            $"portOwnerDetail must name the listening pid {other.Pid}: {detail}");
        Assert.Equal("stopped", JsonFields.Text(status, "server.state"));
        Assert.True(lockFree, "the lock must be released after a portBusy refusal");
        Assert.False(other.HasExited, "the backend must not stop another process");
        Assert.Empty(scenario.Stub.StartedPids());
    }

    [LinuxFact]
    public async Task C5_ServerExitingBeforeListeningIsStartFailedAndReleasesLock()
    {
        await using BackendScenario scenario = BackendScenario.Create("c5");
        scenario.Stub.Mode = StubBehavior.ExitBeforeListen;
        scenario.Stub.ExitCode = 4;
        BackendProcess backend = await scenario.StartWithReleaseAsync();

        ApiResult start = await backend.PostAsync("/api/server/start");
        JsonNode status = await backend.StatusAsync();
        bool lockFree = await PortLock.IsFreeAsync(scenario.Settings.PortLockFile);
        int stubPid = scenario.Stub.SinglePid();

        ApiAssert.Error(start, 500, "startFailed");
        Assert.True(lockFree, "the lock must be released after startFailed");
        Assert.Equal("stopped", JsonFields.Text(status, "server.state"));
        Assert.Equal("startFailed", JsonFields.Text(status, "lastExit.kind"));
        Assert.False(LinuxProcess.IsAlive(stubPid), "the stub exited by itself and must stay gone");
    }

    [LinuxFact]
    public async Task C6_ServerNeverListeningTimesOutAndIsCleanedUp()
    {
        await using BackendScenario scenario = BackendScenario.Create("c6");
        scenario.Stub.Mode = StubBehavior.NoListen;
        scenario.Settings.StartTimeoutSeconds = 3;
        BackendProcess backend = await scenario.StartWithReleaseAsync();

        Stopwatch elapsed = Stopwatch.StartNew();
        ApiResult start = await backend.PostAsync("/api/server/start");
        elapsed.Stop();
        int stubPid = scenario.Stub.SinglePid();
        bool stubGone = await LinuxProcess.WaitForExitAsync(stubPid, TimeSpan.FromSeconds(10));
        bool lockFree = await PortLock.IsFreeAsync(scenario.Settings.PortLockFile);
        JsonNode status = await backend.StatusAsync();

        ApiAssert.Error(start, 500, "startFailed");
        Assert.True(elapsed.Elapsed >= TimeSpan.FromSeconds(2.5), $"startFailed came after {elapsed.Elapsed}, before the 3 s start timeout");
        Assert.True(stubGone, $"the stub {stubPid} that never listened must be cleaned up");
        Assert.True(lockFree, "the lock must be released after startFailed");
        Assert.Equal("stopped", JsonFields.Text(status, "server.state"));
        Assert.Equal("startFailed", JsonFields.Text(status, "lastExit.kind"));
    }

    [LinuxFact]
    public async Task C7_StartWhileRunningIsAlreadyRunning()
    {
        await using BackendScenario scenario = BackendScenario.Create("c7");
        BackendProcess backend = await scenario.StartWithReleaseAsync();
        JsonNode running = await BackendScenario.StartServerAsync(backend);

        ApiResult again = await backend.PostAsync("/api/server/start");
        JsonNode status = await backend.StatusAsync();

        ApiAssert.Error(again, 409, "alreadyRunning");
        Assert.Equal("running", JsonFields.Text(status, "server.state"));
        Assert.Equal(JsonFields.Text(running, "server.runId"), JsonFields.Text(status, "server.runId"));
        Assert.Single(scenario.Stub.StartedPids());
    }

    [LinuxFact]
    public async Task C8_ServerCommandDuringAnotherIsBusy()
    {
        await using BackendScenario scenario = BackendScenario.Create("c8");
        scenario.Stub.ListenDelayMilliseconds = 3000;
        scenario.Settings.StartTimeoutSeconds = 15;
        BackendProcess backend = await scenario.StartWithReleaseAsync();

        Task<ApiResult> first = backend.PostAsync("/api/server/start");
        await backend.WaitForStatusAsync(
            status => JsonFields.Text(status, "server.state") == "starting", TimeSpan.FromSeconds(10), "server.state starting");
        ApiResult secondStart = await backend.PostAsync("/api/server/start");
        ApiResult stopDuringStart = await backend.PostAsync("/api/server/stop");
        ApiResult firstResult = await first;
        JsonNode status = await backend.StatusAsync();

        ApiAssert.Error(secondStart, 409, "busy");
        ApiAssert.Error(stopDuringStart, 409, "busy");
        ApiAssert.Success(firstResult, "the first POST /api/server/start");
        Assert.Equal("running", JsonFields.Text(status, "server.state"));
        Assert.Single(scenario.Stub.StartedPids());
    }
}

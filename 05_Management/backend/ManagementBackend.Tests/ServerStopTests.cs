using System.Text.Json.Nodes;
using Dawnholder.Management.Backend.Tests.Support;

namespace Dawnholder.Management.Backend.Tests;

/// <summary>D: normal stop, forced stop and unrequested exits (backend-design.md 「게임 서버 실행 수명」 4~7).</summary>
public sealed class ServerStopTests
{
    static readonly TimeSpan StopWait = TimeSpan.FromSeconds(20);

    [LinuxFact]
    public async Task D1_StopAnswersStoppingAtOnceThenRecordsGracefulExit()
    {
        await using BackendScenario scenario = BackendScenario.Create("d1");
        scenario.Stub.ExitDelayMilliseconds = 2000;
        BackendProcess backend = await scenario.StartWithReleaseAsync();
        File.WriteAllText(scenario.Settings.PortLockFile, "lock file content\n");
        JsonNode running = await BackendScenario.StartServerAsync(backend);
        int stubPid = scenario.Stub.SinglePid();

        ApiResult stop = await backend.PostAsync("/api/server/stop");
        bool aliveWhenAnswered = LinuxProcess.IsAlive(stubPid);
        JsonNode stopped = await backend.WaitForStatusAsync(
            status => JsonFields.Text(status, "server.state") == "stopped", StopWait, "server.state stopped");
        bool lockFree = await PortLock.IsFreeAsync(scenario.Settings.PortLockFile);

        ApiAssert.Success(stop, "POST /api/server/stop");
        Assert.Equal("stopping", JsonFields.Text(stop.Json, "server.state"));
        JsonFields.Time(stop.Json, "server.stopRequestedAt");
        Assert.True(aliveWhenAnswered, "stop must answer before the server exits (the stub waits 2 s after Enter)");
        Assert.Equal("graceful", JsonFields.Text(stopped, "lastExit.kind"));
        Assert.Equal(0, JsonFields.Number(stopped, "lastExit.exitCode"));
        Assert.Equal(JsonFields.Text(running, "server.runId"), JsonFields.Text(stopped, "lastExit.runId"));
        JsonFields.Time(stopped, "lastExit.endedAt");
        Assert.False(LinuxProcess.IsAlive(stubPid), "the stub must have exited");
        Assert.True(lockFree, "the lock must be released after the server exits");
        Assert.Equal("lock file content\n", File.ReadAllText(scenario.Settings.PortLockFile));
    }

    [LinuxFact]
    public async Task D2_ServerIgnoringEnterTimesOutThenForceStopIsForced()
    {
        await using BackendScenario scenario = BackendScenario.Create("d2");
        scenario.Stub.Mode = StubBehavior.IgnoreEnter;
        scenario.Settings.StopTimeoutSeconds = 2;
        BackendProcess backend = await scenario.StartWithReleaseAsync();
        await BackendScenario.StartServerAsync(backend);
        int stubPid = scenario.Stub.SinglePid();

        ApiResult stop = await backend.PostAsync("/api/server/stop");
        JsonNode timedOut = await backend.WaitForStatusAsync(
            status => JsonFields.Flag(status, "server.stopTimedOut"), StopWait, "server.stopTimedOut true");
        bool aliveAfterTimeout = LinuxProcess.IsAlive(stubPid);
        ApiResult force = await backend.PostAsync("/api/server/force-stop");
        JsonNode stopped = await backend.WaitForStatusAsync(
            status => JsonFields.Text(status, "server.state") == "stopped", StopWait, "server.state stopped");
        bool stubGone = await LinuxProcess.WaitForExitAsync(stubPid, TimeSpan.FromSeconds(10));
        bool lockFree = await PortLock.IsFreeAsync(scenario.Settings.PortLockFile);

        ApiAssert.Success(stop, "POST /api/server/stop");
        Assert.Equal("stopping", JsonFields.Text(timedOut, "server.state"));
        Assert.True(aliveAfterTimeout, "the stop timeout alone must not kill the server; force-stop is asked separately");
        ApiAssert.Success(force, "POST /api/server/force-stop");
        Assert.Equal("forced", JsonFields.Text(stopped, "lastExit.kind"));
        Assert.True(stubGone, "force-stop must end the stub");
        Assert.True(lockFree, "the lock must be released after the forced exit");
    }

    [LinuxFact]
    public async Task D3_UnrequestedExitIsAbnormalWithExitCode()
    {
        await using BackendScenario scenario = BackendScenario.Create("d3");
        scenario.Stub.Mode = StubBehavior.Crash;
        scenario.Stub.ExitCode = 3;
        scenario.Stub.CrashAfterMilliseconds = 3000;
        BackendProcess backend = await scenario.StartWithReleaseAsync();
        await BackendScenario.StartServerAsync(backend);

        JsonNode stopped = await backend.WaitForStatusAsync(
            status => JsonFields.Text(status, "server.state") == "stopped", StopWait, "server.state stopped after the crash");
        bool lockFree = await PortLock.IsFreeAsync(scenario.Settings.PortLockFile);

        Assert.Equal("abnormal", JsonFields.Text(stopped, "lastExit.kind"));
        Assert.Equal(3, JsonFields.Number(stopped, "lastExit.exitCode"));
        Assert.True(lockFree, "the lock must be released after an abnormal exit");
        Assert.Single(scenario.Stub.StartedPids());
    }

    [LinuxFact]
    public async Task D4_StopOrForceStopWhileNotRunningIsNotRunning()
    {
        await using BackendScenario scenario = BackendScenario.Create("d4");
        BackendProcess backend = await scenario.StartBackendAsync();

        ApiResult stop = await backend.PostAsync("/api/server/stop");
        ApiResult force = await backend.PostAsync("/api/server/force-stop");

        ApiAssert.Error(stop, 409, "notRunning");
        ApiAssert.Error(force, 409, "notRunning");
    }

    [LinuxFact]
    public async Task D5_BackendSigtermStopsServerAndRecordsReason()
    {
        await using BackendScenario scenario = BackendScenario.Create("d5");
        BackendProcess backend = await scenario.StartWithReleaseAsync();
        JsonNode running = await BackendScenario.StartServerAsync(backend);
        string serverId = JsonFields.Text(running, "server.serverId");
        string runId = JsonFields.Text(running, "server.runId");
        int stubPid = scenario.Stub.SinglePid();

        await backend.TerminateAsync(TimeSpan.FromSeconds(scenario.Settings.StopTimeoutSeconds + 25));
        bool stubGone = await LinuxProcess.WaitForExitAsync(stubPid, TimeSpan.FromSeconds(10));
        JsonNode record = JsonFields.ReadFile(DataLayout.RunRecord(scenario.DataDirectory, serverId, runId));

        Assert.True(stubGone, "the server must end with the backend");
        Assert.Equal("graceful", JsonFields.Text(record, "exitKind"));
        JsonFields.Time(record, "endedAt");
        Assert.False(string.IsNullOrWhiteSpace(JsonFields.Text(record, "reason")), $"run.json reason is empty: {JsonFields.Show(record)}");
    }
}

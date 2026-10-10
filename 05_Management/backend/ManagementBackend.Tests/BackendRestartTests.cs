using System.Text.Json.Nodes;
using Dawnholder.Management.Backend.Tests.Support;

namespace Dawnholder.Management.Backend.Tests;

/// <summary>
/// Port lock, connection file and live-state judgement across a backend that dies or shares its data directory
/// (backend-design.md 「상태 소유와 수명」, 「관리 접근 경계」 connection.json, 「상태 응답」 portOwner).
/// </summary>
public sealed class BackendRestartTests
{
    [LinuxFact]
    public async Task KilledBackendLeavesNoPortLockOnceItsServerStopsAndANewBackendRunsTheServerAgain()
    {
        await using BackendScenario scenario = BackendScenario.Create("restart-lock");
        BackendProcess crashed = await scenario.StartWithReleaseAsync();
        await BackendScenario.StartServerAsync(crashed);
        int stubPid = scenario.Stub.SinglePid();
        string crashedToken = crashed.Token;

        await crashed.KillAsync();
        bool stubGone = await LinuxProcess.WaitForExitAsync(stubPid, TimeSpan.FromSeconds(10));
        bool lockFree = await PortLock.IsFreeAsync(scenario.Settings.PortLockFile);
        BackendProcess restarted = await scenario.StartBackendAsync();
        JsonNode before = await restarted.StatusAsync();
        JsonNode running = await BackendScenario.StartServerAsync(restarted);
        bool lockHeldWhileRunning = !await PortLock.IsFreeAsync(scenario.Settings.PortLockFile);
        JsonNode stopped = await BackendScenario.StopServerAsync(restarted);

        Assert.True(stubGone, "the server must stop once the dead backend's end of its standard input closes");
        Assert.True(lockFree, "no port lock may stay behind after the killed backend's server stopped");
        Assert.NotEqual(crashedToken, restarted.Token);
        Assert.Equal("stopped", JsonFields.Text(before, "server.state"));
        Assert.False(JsonFields.Flag(before, "server.portListening"), $"nothing listens after the crash: {JsonFields.Show(before)}");
        Assert.False(JsonFields.Flag(before, "server.portLockHeldByOther"), $"no lock holder after the crash: {JsonFields.Show(before)}");
        Assert.Equal("self", JsonFields.Text(running, "server.portOwner"));
        Assert.True(lockHeldWhileRunning, "the new backend must hold the port lock while its server runs");
        Assert.Equal("graceful", JsonFields.Text(stopped, "lastExit.kind"));
    }

    [LinuxFact]
    public async Task BackendShutdownKeepsAConnectionFileItDidNotWrite()
    {
        await using BackendScenario scenario = BackendScenario.Create("restart-connection");
        BackendProcess first = await scenario.StartBackendAsync();
        BackendProcess second = await scenario.StartBackendAsync();

        int firstExit = await first.TerminateAsync(TimeSpan.FromSeconds(30));
        JsonNode? afterFirst = File.Exists(second.ConnectionFile) ? JsonFields.ReadFile(second.ConnectionFile) : null;
        ApiResult secondStillReachable = await second.GetAsync("/api/status");
        int secondExit = await second.TerminateAsync(TimeSpan.FromSeconds(30));
        bool removedBySecond = !File.Exists(second.ConnectionFile);

        Assert.Equal(0, firstExit);
        Assert.True(afterFirst != null, "the first backend deleted connection.json written by the second backend");
        Assert.Equal(second.Pid, JsonFields.Number(afterFirst, "pid"));
        Assert.Equal(second.Token, JsonFields.Text(afterFirst, "token"));
        Assert.True(secondStillReachable.Code == 200, $"the second backend must still answer: {secondStillReachable.Describe()}");
        Assert.Equal(0, secondExit);
        Assert.True(removedBySecond, "the second backend must remove its own connection.json on SIGTERM");
    }

    [LinuxFact]
    public async Task ServerLeftByKilledBackendIsReportedAsOtherAndNotStoppedByTheNewBackend()
    {
        await using BackendScenario scenario = BackendScenario.Create("restart-orphan");
        // A server that ignores Enter and closed input outlives the backend that started it.
        scenario.Stub.Mode = StubBehavior.IgnoreEnter;
        BackendProcess crashed = await scenario.StartWithReleaseAsync();
        await BackendScenario.StartServerAsync(crashed);
        int stubPid = scenario.Stub.SinglePid();

        await crashed.KillAsync();
        BackendProcess restarted = await scenario.StartBackendAsync();
        JsonNode status = await restarted.StatusAsync();
        ApiResult start = await restarted.PostAsync("/api/server/start");
        ApiResult stop = await restarted.PostAsync("/api/server/stop");
        await Task.Delay(TimeSpan.FromSeconds(1));
        bool orphanAlive = LinuxProcess.IsAlive(stubPid);

        Assert.Equal("stopped", JsonFields.Text(status, "server.state"));
        Assert.True(JsonFields.Flag(status, "server.portListening"), $"the orphan still listens: {JsonFields.Show(status)}");
        Assert.Equal("other", JsonFields.Text(status, "server.portOwner"));
        Assert.Contains(stubPid.ToString(System.Globalization.CultureInfo.InvariantCulture), JsonFields.Text(status, "server.portOwnerDetail"));
        ApiAssert.Error(start, 409, "portBusy");
        ApiAssert.Error(stop, 409, "notRunning");
        Assert.True(orphanAlive, "the new backend must not stop a process it did not start");
    }
}

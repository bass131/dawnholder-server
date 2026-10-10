using System.Diagnostics;
using System.Globalization;
using System.Text.Json.Nodes;
using Dawnholder.Management.Backend.Tests.Support;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Dawnholder.Management.Backend.Tests;

/// <summary>
/// B: GET /api/status and the fixed server identity (backend-design.md 「상태 응답」, 「서버·실행 기록」), and the
/// status query's reading of the shared port lock while the server is stopped (screen-design.md 「백엔드 조정」 B-3).
/// </summary>
public sealed class ServerStatusTests
{
    // Other sessions' tests pick free loopback ports too, and one of them once listened on B1's server port during a
    // full run (PR1 verdict, B1 failure). An attempt that sees another listener is repeated on a new port.
    const int B1Attempts = 3;

    // The old status query owned the lock for microseconds per call; this many calls make that visible to the probe.
    const int B3StatusQueries = 200;

    readonly ITestOutputHelper _output;

    public ServerStatusTests(ITestOutputHelper output) => _output = output;

    /// <summary>
    /// The server port is held bound (not listening) by the test and observed right before and after the query, so a
    /// listener of another process is told apart from a wrong backend answer. Failures carry the port and the status.
    /// </summary>
    [LinuxFact]
    public async Task B1_InitialStatusHasEveryFieldAndIsStopped()
    {
        List<string> contended = [];
        for (int attempt = 1; attempt <= B1Attempts; attempt++)
        {
            await using BackendScenario scenario = BackendScenario.CreateWithReservedServerPort("b1");
            int port = scenario.Stub.Port;
            BackendProcess backend = await scenario.StartBackendAsync();

            IReadOnlyList<ListeningSocket> before = LinuxProcess.ListeningSocketsOnPort(port);
            JsonNode status = await backend.StatusAsync();
            IReadOnlyList<ListeningSocket> after = LinuxProcess.ListeningSocketsOnPort(port);

            if (before.Count > 0 || after.Count > 0)
            {
                contended.Add($"attempt {attempt}: another process listened on server port {port} "
                    + $"(before [{string.Join(", ", before)}], after [{string.Join(", ", after)}]); status {JsonFields.Show(status)}");
                continue;
            }

            try
            {
                AssertInitialStatus(scenario, backend, status);
            }
            catch (XunitException failure)
            {
                throw new XunitException(
                    $"{failure.Message}\nserver port {port}, no other listener before or after the query; status {JsonFields.Show(status)}",
                    failure);
            }

            return;
        }

        Assert.Fail($"another process listened on the server port in all {B1Attempts} attempts; no backend verdict:\n{string.Join('\n', contended)}");
    }

    [LinuxFact]
    public async Task B2_ServerIdSurvivesBackendRestartWithSameDataDirectory()
    {
        await using BackendScenario scenario = BackendScenario.Create("b2");
        BackendProcess first = await scenario.StartBackendAsync();
        string firstId = JsonFields.Text(await first.StatusAsync(), "server.serverId");
        await first.TerminateAsync(TimeSpan.FromSeconds(30));

        BackendProcess second = await scenario.StartBackendAsync();
        string secondId = JsonFields.Text(await second.StatusAsync(), "server.serverId");
        JsonNode record = JsonFields.ReadFile(DataLayout.ServerRecord(scenario.DataDirectory, secondId));

        Assert.Equal(firstId, secondId);
        Assert.Equal(secondId, JsonFields.Text(record, "serverId"));
        Assert.Equal(scenario.Settings.DisplayName, JsonFields.Text(record, "displayName"));
        Assert.Equal(scenario.Stub.Port, JsonFields.Number(record, "port"));
        JsonFields.Time(record, "createdAt");
    }

    /// <summary>
    /// A probe meets the lock file at arbitrary moments while the status queries run. With the server stopped the
    /// backend has to read the lock owner without taking the lock, so the probe never finds it busy. A control first
    /// shows the probe does notice another holder.
    /// </summary>
    [LinuxFact(Timeout = 90_000)]
    public async Task B3_StatusWhileStoppedNeverTakesThePortLock()
    {
        await using BackendScenario scenario = BackendScenario.Create("b3");
        string lockFile = scenario.Settings.PortLockFile;
        BackendProcess backend = await scenario.StartBackendAsync();
        Assert.Equal("stopped", JsonFields.Text(await backend.StatusAsync(), "server.state"));

        // The holder locks first: a running probe could make its single flock -n fail.
        FlockProbeCount control;
        Process holder = await scenario.HoldPortLockAsync();
        using (FlockProbe probe = FlockProbe.Start(lockFile))
        {
            await Task.Delay(100);
            control = probe.Stop();
        }

        holder.Kill(entireProcessTree: true);
        await holder.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));

        FlockProbeCount during;
        int heldByOtherAnswers = 0;
        Stopwatch watch = Stopwatch.StartNew();
        using (FlockProbe probe = FlockProbe.Start(lockFile))
        {
            for (int query = 0; query < B3StatusQueries; query++)
            {
                JsonNode status = await backend.StatusAsync();
                heldByOtherAnswers += JsonFields.Flag(status, "server.portLockHeldByOther") ? 1 : 0;
            }

            during = probe.Stop();
        }

        watch.Stop();
        _output.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"B3 control: attempts={control.Attempts} busy={control.Busy}; status_queries={B3StatusQueries} seconds={watch.Elapsed.TotalSeconds:F3} "
            + $"probe_attempts={during.Attempts} probe_busy={during.Busy} answers_portLockHeldByOther_true={heldByOtherAnswers}"));

        Assert.True(control.Busy > 0, $"control: the probe must notice a lock held by another process ({control})");
        Assert.True(during.Attempts > 0, "the probe made no attempt while the status queries ran");
        Assert.True(
            during.Busy == 0,
            $"{during.Busy} of {during.Attempts} flock -n attempts found the lock taken during {B3StatusQueries} status queries with the server stopped; "
            + "the status query must not lock the shared port lock file");
    }

    [LinuxFact(Timeout = 90_000)]
    public async Task B4_LockHeldByAnotherProcessWhileStoppedIsReportedAndClearsAfterRelease()
    {
        await using BackendScenario scenario = BackendScenario.Create("b4");
        BackendProcess backend = await scenario.StartBackendAsync();
        Process holder = await scenario.HoldPortLockAsync();

        JsonNode whileHeld = await backend.StatusAsync();
        holder.Kill(entireProcessTree: true);
        await holder.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
        bool freeAfterRelease = await PortLock.IsFreeAsync(scenario.Settings.PortLockFile);
        JsonNode afterRelease = await backend.StatusAsync();

        Assert.Equal("stopped", JsonFields.Text(whileHeld, "server.state"));
        Assert.True(JsonFields.Flag(whileHeld, "server.portLockHeldByOther"), $"portLockHeldByOther must be true while another process holds the lock: {JsonFields.Show(whileHeld)}");
        Assert.True(freeAfterRelease, "the holder's lock must be gone once it exited");
        Assert.False(JsonFields.Flag(afterRelease, "server.portLockHeldByOther"), $"portLockHeldByOther must be false after the holder exited: {JsonFields.Show(afterRelease)}");
    }

    [LinuxFact(Timeout = 90_000)]
    public async Task B5_StatusWithoutLockFileLeavesItAbsentAndReportsNotHeld()
    {
        await using BackendScenario scenario = BackendScenario.Create("b5");
        string lockFile = scenario.Settings.PortLockFile;
        bool absentBeforeStart = !File.Exists(lockFile);
        BackendProcess backend = await scenario.StartBackendAsync();

        JsonNode status = await backend.StatusAsync();
        bool existsAfterQuery = File.Exists(lockFile);

        Assert.True(absentBeforeStart, $"the scenario must start without {lockFile}");
        Assert.Equal("stopped", JsonFields.Text(status, "server.state"));
        Assert.False(existsAfterQuery, $"the status query must not create the missing lock file {lockFile}");
        Assert.False(JsonFields.Flag(status, "server.portLockHeldByOther"), $"nobody holds a lock file that does not exist: {JsonFields.Show(status)}");
    }

    static void AssertInitialStatus(BackendScenario scenario, BackendProcess backend, JsonNode status)
    {
        JsonFields.Time(status, "backend.startedAt");
        Assert.Equal(backend.Pid, JsonFields.Number(status, "backend.pid"));
        Assert.False(string.IsNullOrWhiteSpace(JsonFields.Text(status, "server.serverId")), "serverId is empty");
        Assert.Equal(scenario.Settings.DisplayName, JsonFields.Text(status, "server.displayName"));
        Assert.Equal("stopped", JsonFields.Text(status, "server.state"));
        Assert.True(JsonFields.IsNull(status, "server.pid"), "server.pid must be null while stopped");
        Assert.True(JsonFields.IsNull(status, "server.runId"), "server.runId must be null while stopped");
        Assert.True(JsonFields.IsNull(status, "server.startedAt"), "server.startedAt must be null while stopped");
        Assert.True(JsonFields.IsNull(status, "server.release"), "server.release must be null while stopped");
        Assert.True(JsonFields.IsNull(status, "server.stopRequestedAt"), "server.stopRequestedAt must be null while stopped");
        Assert.False(JsonFields.Flag(status, "server.stopTimedOut"));
        Assert.Equal(scenario.Stub.Port, JsonFields.Number(status, "server.port"));
        Assert.False(JsonFields.Flag(status, "server.portListening"));
        Assert.Equal("none", JsonFields.Text(status, "server.portOwner"));
        Assert.True(JsonFields.IsNull(status, "server.portOwnerDetail"), "portOwnerDetail must be null when nobody listens");
        Assert.False(JsonFields.Flag(status, "server.portLockHeldByOther"));
        Assert.True(JsonFields.IsNull(status, "currentRelease"), "currentRelease must be null without releases");
        Assert.True(JsonFields.IsNull(status, "lastExit"), "lastExit must be null without exits");
    }
}

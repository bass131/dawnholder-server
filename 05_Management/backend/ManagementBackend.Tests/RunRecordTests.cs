using System.Text.Json;
using System.Text.Json.Nodes;
using Dawnholder.Management.Backend.Tests.Support;

namespace Dawnholder.Management.Backend.Tests;

/// <summary>G: &lt;serverId&gt;/runs/&lt;runId&gt;/run.json (backend-design.md 「서버·실행 기록」).</summary>
public sealed class RunRecordTests
{
    [LinuxFact]
    public async Task G1_FinishedRunRecordHasEveryField()
    {
        await using BackendScenario scenario = BackendScenario.Create("g1");
        BackendProcess backend = await scenario.StartWithReleaseAsync();
        JsonNode running = await BackendScenario.StartServerAsync(backend);
        string serverId = JsonFields.Text(running, "server.serverId");
        string runId = JsonFields.Text(running, "server.runId");
        int stubPid = scenario.Stub.SinglePid();
        await BackendScenario.StopServerAsync(backend);

        JsonNode record = JsonFields.ReadFile(DataLayout.RunRecord(scenario.DataDirectory, serverId, runId));

        Assert.Equal(runId, JsonFields.Text(record, "runId"));
        DateTimeOffset started = JsonFields.Time(record, "startedAt");
        DateTimeOffset ended = JsonFields.Time(record, "endedAt");
        Assert.True(ended >= started, $"endedAt {ended:O} is before startedAt {started:O}");
        Assert.Equal(stubPid, JsonFields.Number(record, "pid"));
        Assert.Equal(scenario.Source!.FirstCommit, JsonFields.Text(record, "releaseCommit"));
        Assert.Equal("graceful", JsonFields.Text(record, "exitKind"));
        Assert.Equal(0, JsonFields.Number(record, "exitCode"));
        Assert.True(JsonFields.IsNull(record, "signal"), $"a graceful exit has no signal: {JsonFields.Show(record)}");
        JsonFields.Field(record, "reason");
    }

    [LinuxFact]
    public async Task G2_RunLeftOpenByKilledBackendIsClosedAsUnknownOnRestart()
    {
        await using BackendScenario scenario = BackendScenario.Create("g2");
        BackendProcess crashed = await scenario.StartWithReleaseAsync();
        JsonNode running = await BackendScenario.StartServerAsync(crashed);
        string serverId = JsonFields.Text(running, "server.serverId");
        string runId = JsonFields.Text(running, "server.runId");
        int stubPid = scenario.Stub.SinglePid();
        string recordPath = DataLayout.RunRecord(scenario.DataDirectory, serverId, runId);

        await crashed.KillAsync();

        // Like the real server, the stub stops when the dead backend's end of its standard input closes.
        bool stubGone = await LinuxProcess.WaitForExitAsync(stubPid, TimeSpan.FromSeconds(10));
        BackendProcess restarted = await scenario.StartBackendAsync();
        JsonNode closed = await WaitForRecordAsync(recordPath, record => JsonFields.Text(record, "exitKind") == "unknown");
        JsonNode status = await restarted.StatusAsync();

        Assert.True(stubGone, "the stub should stop once its standard input closes");
        Assert.Equal("unknown", JsonFields.Text(closed, "exitKind"));
        Assert.Equal("stopped", JsonFields.Text(status, "server.state"));
    }

    static async Task<JsonNode> WaitForRecordAsync(string path, Func<JsonNode, bool> condition)
    {
        DateTime deadline = DateTime.UtcNow + TimeSpan.FromSeconds(20);
        while (true)
        {
            JsonNode? record = TryRead(path);
            if (record is JsonObject && record["exitKind"] is JsonValue && condition(record))
            {
                return record;
            }

            if (DateTime.UtcNow >= deadline)
            {
                Assert.Fail($"{path} was not closed within 20 s: {JsonFields.Show(record)}");
            }

            await Task.Delay(100);
        }
    }

    /// <summary>The record may be rewritten while the test polls; an unreadable moment is retried.</summary>
    static JsonNode? TryRead(string path)
    {
        try
        {
            return File.Exists(path) ? JsonNode.Parse(File.ReadAllText(path)) : null;
        }
        catch (Exception error) when (error is IOException or JsonException)
        {
            return null;
        }
    }
}

using System.Text.Json.Nodes;
using Dawnholder.Management.Backend.Tests.Support;

namespace Dawnholder.Management.Backend.Tests;

/// <summary>B: GET /api/status and the fixed server identity (backend-design.md 「상태 응답」, 「서버·실행 기록」).</summary>
public sealed class ServerStatusTests
{
    [LinuxFact]
    public async Task B1_InitialStatusHasEveryFieldAndIsStopped()
    {
        await using BackendScenario scenario = BackendScenario.Create("b1");
        BackendProcess backend = await scenario.StartBackendAsync();

        JsonNode status = await backend.StatusAsync();

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
}

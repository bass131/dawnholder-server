using System.Text.Json.Nodes;
using Dawnholder.Management.Backend.Tests.Support;

namespace Dawnholder.Management.Backend.Tests;

/// <summary>
/// One backend owns its data directory for its whole life (05_Management/goals/2026-11-10-code-audit-management-fixes/goal.md
/// 「만들 것」 1 and 완료조건 2). A second backend started on the same directory while the first runs the game server stub
/// exits by itself with a nonzero code and a reason naming the directory, and leaves the first's open run record, active
/// log, connection.json and answers alone. A killed owner gives the directory up at once, also while its server lives on.
/// </summary>
public sealed class DataDirectoryOwnershipTests
{
    const string OwnerLogLine = "owner log line";

    // Long enough for a second backend that ignores the owner to finish its startup (closing open run records, log
    // retention) and publish its own connection.json, so each damage below has happened before it is killed.
    static readonly TimeSpan SecondBackendWait = TimeSpan.FromSeconds(30);

    static readonly TimeSpan LogWait = TimeSpan.FromSeconds(20);

    [LinuxFact]
    public async Task Goal1_SecondBackendOnTheSameDataDirectoryExitsByItselfNamingTheDirectory()
    {
        await using BackendScenario scenario = BackendScenario.Create("owner-refusal");
        await StartOwnerWithRunningServerAsync(scenario);

        RefusalRun second = await RunSecondBackendAsync(scenario, scenario.Settings.ToJson());
        bool namesDirectory = second.Output.Contains(scenario.DataDirectory, StringComparison.Ordinal);

        Assert.True(second.ExitedByItself, $"the second backend must stop by itself: {second.Describe()}");
        Assert.True(second.ExitCode != 0, $"the second backend must exit with a nonzero code: {second.Describe()}");
        Assert.True(namesDirectory, $"the reason must name the data directory {scenario.DataDirectory}: {second.Describe()}");
    }

    [LinuxFact]
    public async Task Goal1_SecondBackendLeavesTheOwnersOpenRunRecordOpen()
    {
        await using BackendScenario scenario = BackendScenario.Create("owner-run-record");
        (_, string serverId, string runId) = await StartOwnerWithRunningServerAsync(scenario);
        string recordPath = DataLayout.RunRecord(scenario.DataDirectory, serverId, runId);
        bool openBefore = IsOpen(JsonFields.ReadFile(recordPath));

        RefusalRun second = await RunSecondBackendAsync(scenario, scenario.Settings.ToJson());
        JsonNode after = JsonFields.ReadFile(recordPath);

        Assert.True(openBefore, $"precondition: the running server's run.json is open: {File.ReadAllText(recordPath)}");
        Assert.True(IsOpen(after), $"the second backend closed the owner's open run.json: {JsonFields.Show(after)}\n{second.Describe()}");
    }

    [LinuxFact]
    public async Task Goal1_SecondBackendLeavesTheOwnersActiveLogFile()
    {
        await using BackendScenario scenario = BackendScenario.Create("owner-active-log");
        scenario.Stub.WriteLine("stdout", OwnerLogLine);
        (BackendProcess first, string serverId, string runId) = await StartOwnerWithRunningServerAsync(scenario);
        await LogQuery.WaitForTextAsync(first, $"runId={runId}", OwnerLogLine, LogWait);
        IReadOnlyList<string> files = DataLayout.LogFiles(scenario.DataDirectory, serverId, runId);
        long bytesBefore = LogQuery.Size(files);

        // The stub has written its only line, so the active file keeps its length. A second backend whose start-up
        // retention ran over this directory with a one-byte limit would delete it (the owner's file is not its own).
        JsonObject config = scenario.Settings.ToJson();
        config["logs"]!["retentionBytes"] = 1;
        RefusalRun second = await RunSecondBackendAsync(scenario, config);
        string[] missing = [.. files.Where(file => !File.Exists(file))];

        Assert.True(files.Count > 0 && bytesBefore > 0, $"precondition: the owner wrote an active log file: [{string.Join(", ", files)}]");
        Assert.True(missing.Length == 0, $"the second backend deleted the owner's active log: [{string.Join(", ", missing)}]\n{second.Describe()}");
        Assert.True(LogQuery.Size(files) >= bytesBefore, $"the owner's active log shrank below {bytesBefore} bytes\n{second.Describe()}");
    }

    [LinuxFact]
    public async Task Goal1_SecondBackendLeavesTheOwnersConnectionFile()
    {
        await using BackendScenario scenario = BackendScenario.Create("owner-connection");
        (BackendProcess first, _, _) = await StartOwnerWithRunningServerAsync(scenario);
        string before = File.ReadAllText(first.ConnectionFile);

        RefusalRun second = await RunSecondBackendAsync(scenario, scenario.Settings.ToJson());
        string? after = File.Exists(first.ConnectionFile) ? File.ReadAllText(first.ConnectionFile) : null;

        Assert.True(after == before, $"the owner's connection.json changed: before {before}, after {after ?? "(none)"}\n{second.Describe()}");
    }

    [LinuxFact]
    public async Task Goal1_OwnerKeepsAnsweringAndRunningItsServerAfterTheSecondBackendIsRefused()
    {
        await using BackendScenario scenario = BackendScenario.Create("owner-answers");
        (BackendProcess first, _, string runId) = await StartOwnerWithRunningServerAsync(scenario);

        await RunSecondBackendAsync(scenario, scenario.Settings.ToJson());
        ApiResult answer = await first.GetAsync("/api/status");

        Assert.True(answer.Code == 200, $"the owner must still answer: {answer.Describe()}");
        Assert.Equal("running", JsonFields.Text(answer.Json, "server.state"));
        Assert.Equal(runId, JsonFields.Text(answer.Json, "server.runId"));
    }

    [LinuxFact]
    public async Task Goal1_KilledOwnerGivesTheDataDirectoryUpAtOnceWhileItsServerLivesOn()
    {
        await using BackendScenario scenario = BackendScenario.Create("owner-killed");
        // A server that ignores Enter and closed input outlives the backend that started it.
        scenario.Stub.Mode = StubBehavior.IgnoreEnter;
        (BackendProcess crashed, _, _) = await StartOwnerWithRunningServerAsync(scenario);
        int stubPid = scenario.Stub.SinglePid();

        await crashed.KillAsync();
        bool stubAliveAfterKill = LinuxProcess.IsAlive(stubPid);
        BackendProcess next = await scenario.StartBackendAsync();
        ApiResult answer = await next.GetAsync("/api/status");
        bool stubAliveWhileNextRuns = LinuxProcess.IsAlive(stubPid);

        Assert.True(stubAliveAfterKill, "precondition: the server outlives the killed backend");
        Assert.True(answer.Code == 200, $"the next backend must own the directory and answer: {answer.Describe()}");
        Assert.True(stubAliveWhileNextRuns, "the left server must still run, so it was not holding the ownership lock");
    }

    /// <summary>Starts the owner with a release and a running server; returns it with the serverId and runId.</summary>
    static async Task<(BackendProcess Owner, string ServerId, string RunId)> StartOwnerWithRunningServerAsync(BackendScenario scenario)
    {
        BackendProcess owner = await scenario.StartWithReleaseAsync();
        JsonNode running = await BackendScenario.StartServerAsync(owner);
        return (owner, JsonFields.Text(running, "server.serverId"), JsonFields.Text(running, "server.runId"));
    }

    /// <summary>
    /// Runs a second backend on the owner's data directory with its own configuration file.
    /// BackendScenario.RunBackendExpectingRefusalAsync cannot be used: it stops waiting as soon as dataDirectory/connection.json
    /// exists, and the owner wrote that file already, so the second backend would be killed before it could exit by itself
    /// or do any damage. Only the default data directory's connection.json is watched; otherwise the wait ends at
    /// <see cref="SecondBackendWait"/>, and a backend still running then is killed.
    /// </summary>
    static Task<RefusalRun> RunSecondBackendAsync(BackendScenario scenario, JsonObject config)
    {
        string configPath = Path.Combine(scenario.Root, "second-backend.json");
        File.WriteAllText(configPath, config.ToJsonString());
        return BackendProcess.RunExpectingRefusalAsync(
            configPath,
            scenario.Root,
            scenario.BackendEnvironment(),
            [Path.Combine(scenario.DefaultDataDirectory, "connection.json")],
            SecondBackendWait);
    }

    /// <summary>An open run record has no end time and no exit kind yet, whether the fields are absent or null.</summary>
    static bool IsOpen(JsonNode record) =>
        record is JsonObject fields
        && (!fields.ContainsKey("endedAt") || fields["endedAt"] == null)
        && (!fields.ContainsKey("exitKind") || fields["exitKind"] == null);
}

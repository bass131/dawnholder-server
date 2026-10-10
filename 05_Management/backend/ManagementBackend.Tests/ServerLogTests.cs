using System.Text.Json.Nodes;
using Dawnholder.Management.Backend.Tests.Support;

namespace Dawnholder.Management.Backend.Tests;

/// <summary>E: log collection, GET /api/logs and retention (backend-design.md 「로그 조회」, 「로그 저장과 보존」).</summary>
public sealed class ServerLogTests
{
    static readonly TimeSpan LogWait = TimeSpan.FromSeconds(20);

    public static TheoryData<string> OutOfRangeQueries => new()
    {
        "minutes=0",
        "minutes=1441",
        "minutes=abc",
        "minutes=1.5",
        "contains=a&contains=b&contains=c&contains=d&contains=e&contains=f",
        "contains=",
        $"contains={new string('x', 65)}",
        "limit=0",
        "limit=5001",
        "limit=many",
        "runId=00000000-0000-4000-8000-000000000000",
    };

    [LinuxFact]
    public async Task E1_StdoutAndStderrLinesCarrySeqCollectedAtAndStream()
    {
        await using BackendScenario scenario = BackendScenario.Create("e1");
        scenario.Stub
            .WriteLine("stdout", "e1 out alpha")
            .WriteLine("stderr", "e1 err beta")
            .WriteLine("stdout", "e1 out gamma");
        BackendProcess backend = await scenario.StartWithReleaseAsync();
        JsonNode running = await BackendScenario.StartServerAsync(backend);
        string runId = JsonFields.Text(running, "server.runId");

        await LogQuery.WaitForTextAsync(backend, "minutes=10", "e1 out gamma", LogWait);
        JsonNode logs = await LogQuery.WaitForTextAsync(backend, "minutes=10", "e1 err beta", LogWait);
        IReadOnlyList<JsonNode> lines = LogQuery.Lines(logs);
        JsonNode alpha = lines.Single(line => JsonFields.Text(line, "text") == "e1 out alpha");
        JsonNode beta = lines.Single(line => JsonFields.Text(line, "text") == "e1 err beta");
        JsonNode gamma = lines.Single(line => JsonFields.Text(line, "text") == "e1 out gamma");
        DateTimeOffset[] collected = [.. lines.Select(line => JsonFields.Time(line, "collectedAt"))];

        Assert.Equal(JsonFields.Text(running, "server.serverId"), JsonFields.Text(logs, "serverId"));
        JsonFields.Time(logs, "from");
        JsonFields.Time(logs, "to");
        Assert.False(JsonFields.Flag(logs, "truncated"));
        Assert.Equal("stdout", JsonFields.Text(alpha, "stream"));
        Assert.Equal("stderr", JsonFields.Text(beta, "stream"));
        Assert.Equal("stdout", JsonFields.Text(gamma, "stream"));
        JsonNode[] scripted = [alpha, beta, gamma];
        Assert.All(scripted, line => Assert.Equal(runId, JsonFields.Text(line, "runId")));
        Assert.All(scripted, line => Assert.True(JsonFields.Number(line, "seq") >= 1));
        Assert.All(scripted, line => Assert.False(JsonFields.Flag(line, "textTruncated")));
        Assert.True(JsonFields.Number(alpha, "seq") < JsonFields.Number(gamma, "seq"), "seq must follow the order of one stream");
        Assert.True(collected.SequenceEqual(collected.Order()), "lines must be in ascending collectedAt order");
        JsonFields.Number(logs, "retention.totalBytes");
        JsonFields.Array(logs, "retention.recentDeletions");
    }

    [LinuxFact]
    public async Task E2_ContainsMatchesAnyWordIgnoringCase()
    {
        await using BackendScenario scenario = BackendScenario.Create("e2-contains");
        scenario.Stub
            .WriteLine("stdout", "e2 boot ok")
            .WriteLine("stdout", "e2 WARN disk slow")
            .WriteLine("stderr", "e2 Error: socket reset")
            .WriteLine("stdout", "e2 plain last");
        BackendProcess backend = await scenario.StartWithReleaseAsync();
        JsonNode running = await BackendScenario.StartServerAsync(backend);
        string runId = JsonFields.Text(running, "server.runId");
        await LogQuery.WaitForTextAsync(backend, "minutes=10", "e2 plain last", LogWait);

        JsonNode filtered = await LogQuery.GetAsync(backend, $"minutes=10&contains=warn&contains=ERROR&runId={runId}");

        string[] expected = ["e2 Error: socket reset", "e2 WARN disk slow"];
        Assert.Equal(expected, LogQuery.Texts(filtered).Order(StringComparer.Ordinal));
    }

    [LinuxFact]
    public async Task E2_RunIdSelectsOneRun()
    {
        await using BackendScenario scenario = BackendScenario.Create("e2-run");
        scenario.Stub.WriteLine("stdout", "e2 run line");
        BackendProcess backend = await scenario.StartWithReleaseAsync();
        string firstRun = JsonFields.Text(await BackendScenario.StartServerAsync(backend), "server.runId");
        await LogQuery.WaitForTextAsync(backend, $"runId={firstRun}", "e2 run line", LogWait);
        await BackendScenario.StopServerAsync(backend);
        string secondRun = JsonFields.Text(await BackendScenario.StartServerAsync(backend), "server.runId");
        await LogQuery.WaitForTextAsync(backend, $"runId={secondRun}", "e2 run line", LogWait);

        JsonNode first = await LogQuery.GetAsync(backend, $"runId={firstRun}");
        JsonNode second = await LogQuery.GetAsync(backend, $"runId={secondRun}");
        JsonNode all = await LogQuery.GetAsync(backend, "minutes=10");
        string[] allRuns = [.. LogQuery.Lines(all).Select(line => JsonFields.Text(line, "runId")).Distinct()];

        Assert.NotEqual(firstRun, secondRun);
        Assert.NotEmpty(LogQuery.Lines(first));
        Assert.All(LogQuery.Lines(first), line => Assert.Equal(firstRun, JsonFields.Text(line, "runId")));
        Assert.NotEmpty(LogQuery.Lines(second));
        Assert.All(LogQuery.Lines(second), line => Assert.Equal(secondRun, JsonFields.Text(line, "runId")));
        Assert.Contains(firstRun, allRuns);
        Assert.Contains(secondRun, allRuns);
    }

    [LinuxFact]
    public async Task E2_MinutesExcludesLinesCollectedEarlier()
    {
        await using BackendScenario scenario = BackendScenario.Create("e2-minutes");
        scenario.Stub.WriteLine("stdout", "e2 minutes line");
        BackendProcess first = await scenario.StartWithReleaseAsync();
        JsonNode oldRun = await BackendScenario.StartServerAsync(first);
        string serverId = JsonFields.Text(oldRun, "server.serverId");
        string oldRunId = JsonFields.Text(oldRun, "server.runId");
        await LogQuery.WaitForTextAsync(first, $"runId={oldRunId}", "e2 minutes line", LogWait);
        await BackendScenario.StopServerAsync(first);
        await first.TerminateAsync(TimeSpan.FromSeconds(30));
        LogQuery.Age(DataLayout.LogFiles(scenario.DataDirectory, serverId, oldRunId), TimeSpan.FromMinutes(30));

        BackendProcess second = await scenario.StartBackendAsync();
        string newRunId = JsonFields.Text(await BackendScenario.StartServerAsync(second), "server.runId");
        await LogQuery.WaitForTextAsync(second, $"runId={newRunId}", "e2 minutes line", LogWait);
        JsonNode recent = await LogQuery.GetAsync(second, "minutes=10");
        JsonNode hour = await LogQuery.GetAsync(second, "minutes=60");
        string[] recentRuns = [.. LogQuery.Lines(recent).Select(line => JsonFields.Text(line, "runId")).Distinct()];
        string[] hourRuns = [.. LogQuery.Lines(hour).Select(line => JsonFields.Text(line, "runId")).Distinct()];

        Assert.DoesNotContain(oldRunId, recentRuns);
        Assert.Contains(newRunId, recentRuns);
        Assert.Contains(oldRunId, hourRuns);
        Assert.Contains(newRunId, hourRuns);
    }

    [LinuxTheory]
    [MemberData(nameof(OutOfRangeQueries))]
    public async Task E2_OutOfRangeQueryIsInvalidQuery(string query)
    {
        await using BackendScenario scenario = BackendScenario.Create("e2-invalid");
        BackendProcess backend = await scenario.StartBackendAsync();

        ApiResult result = await backend.GetAsync($"/api/logs?{query}");

        ApiAssert.Error(result, 400, "invalidQuery");
    }

    [LinuxFact]
    public async Task E2_BoundaryQueriesAreAccepted()
    {
        await using BackendScenario scenario = BackendScenario.Create("e2-bounds");
        BackendProcess backend = await scenario.StartBackendAsync();
        string longWord = new('x', 64);
        string[] queries =
        [
            "minutes=1",
            "minutes=1440",
            "limit=1",
            "limit=5000",
            string.Join('&', Enumerable.Repeat($"contains={longWord}", 5)),
            string.Empty,
        ];

        foreach (string query in queries)
        {
            ApiResult result = await backend.GetAsync($"/api/logs?{query}");
            Assert.True(result.Code == 200, $"'{query}' is inside the documented ranges: {result.Describe()}");
        }
    }

    [LinuxFact]
    public async Task E3_LimitKeepsMostRecentLinesAndMarksTruncated()
    {
        await using BackendScenario scenario = BackendScenario.Create("e3");
        scenario.Stub.LineIntervalMilliseconds = 20;
        scenario.Stub.WriteLine("stdout", "e3 line", count: 30);
        BackendProcess backend = await scenario.StartWithReleaseAsync();
        string runId = JsonFields.Text(await BackendScenario.StartServerAsync(backend), "server.runId");
        await LogQuery.WaitForTextAsync(backend, $"runId={runId}", StubBehavior.CountedText("e3 line", 30), LogWait);

        JsonNode limited = await LogQuery.GetAsync(backend, $"runId={runId}&limit=5");
        JsonNode full = await LogQuery.GetAsync(backend, $"runId={runId}&limit=1000");

        string[] lastFive = [.. Enumerable.Range(26, 5).Select(index => StubBehavior.CountedText("e3 line", index))];
        Assert.Equal(lastFive, LogQuery.Texts(limited));
        Assert.True(JsonFields.Flag(limited, "truncated"), "limit=5 dropped lines, so truncated must be true");
        Assert.Contains(StubBehavior.CountedText("e3 line", 1), LogQuery.Texts(full));
        Assert.False(JsonFields.Flag(full, "truncated"), "limit=1000 kept every line, so truncated must be false");
    }

    [LinuxFact]
    public async Task E4_LineOver8192CharactersIsCutAndMarked()
    {
        await using BackendScenario scenario = BackendScenario.Create("e4");
        string longLine = "e4 long " + new string('x', 9000);
        scenario.Stub.WriteLine("stdout", longLine).WriteLine("stdout", "e4 short");
        BackendProcess backend = await scenario.StartWithReleaseAsync();
        string runId = JsonFields.Text(await BackendScenario.StartServerAsync(backend), "server.runId");

        JsonNode logs = await LogQuery.WaitForTextAsync(backend, $"runId={runId}", "e4 short", LogWait);
        JsonNode cut = LogQuery.Lines(logs).Single(line => JsonFields.Text(line, "text").StartsWith("e4 long ", StringComparison.Ordinal));
        JsonNode whole = LogQuery.Lines(logs).Single(line => JsonFields.Text(line, "text") == "e4 short");

        Assert.Equal(longLine[..8192], JsonFields.Text(cut, "text"));
        Assert.True(JsonFields.Flag(cut, "textTruncated"), "the 9,008-character line must be marked truncated");
        Assert.False(JsonFields.Flag(whole, "textTruncated"));
    }

    [LinuxFact]
    public async Task E5_OldLogFilesAreRemovedAtBackendStartForAge()
    {
        await using BackendScenario scenario = BackendScenario.Create("e5");
        scenario.Settings.RetentionDays = 1;
        scenario.Stub.WriteLine("stdout", "e5 line");
        BackendProcess first = await scenario.StartWithReleaseAsync();
        JsonNode run = await BackendScenario.StartServerAsync(first);
        string serverId = JsonFields.Text(run, "server.serverId");
        string runId = JsonFields.Text(run, "server.runId");
        await LogQuery.WaitForTextAsync(first, $"runId={runId}", "e5 line", LogWait);
        await BackendScenario.StopServerAsync(first);
        await first.TerminateAsync(TimeSpan.FromSeconds(30));
        IReadOnlyList<string> files = DataLayout.LogFiles(scenario.DataDirectory, serverId, runId);
        Assert.Single(files);
        LogQuery.Age(files, TimeSpan.FromDays(3));
        long agedBytes = LogQuery.Size(files);

        BackendProcess second = await scenario.StartBackendAsync();
        JsonNode logs = await LogQuery.WaitForAsync(
            second, "minutes=1440", response => LogQuery.Deletions(response, "age").Count > 0, LogWait, "an age deletion");
        JsonNode deletion = LogQuery.Deletions(logs, "age")[0];

        Assert.All(files, file => Assert.False(File.Exists(file), $"{file} is older than retentionDays and must be removed"));
        Assert.Equal(agedBytes, JsonFields.Number(deletion, "bytes"));
        JsonFields.Time(deletion, "from");
        JsonFields.Time(deletion, "to");
        Assert.True(
            File.Exists(Path.Combine(DataLayout.ServerDirectory(scenario.DataDirectory, serverId), "retention-log.jsonl")),
            "removed ranges must be written to <serverId>/retention-log.jsonl");
    }

    [LinuxFact]
    public async Task E6_OverRetentionBytesRemovesOldestFilesFirstForSize()
    {
        await using BackendScenario scenario = BackendScenario.Create("e6");
        string filler = "e6 filler " + new string('x', 190);
        scenario.Stub.WriteLine("stdout", filler, count: 40);
        BackendProcess first = await scenario.StartWithReleaseAsync();
        (string serverId, string olderRun) = await RunUntilLastLineAsync(first, StubBehavior.CountedText(filler, 40));
        (_, string newerRun) = await RunUntilLastLineAsync(first, StubBehavior.CountedText(filler, 40));
        await first.TerminateAsync(TimeSpan.FromSeconds(30));
        IReadOnlyList<string> olderFiles = DataLayout.LogFiles(scenario.DataDirectory, serverId, olderRun);
        IReadOnlyList<string> newerFiles = DataLayout.LogFiles(scenario.DataDirectory, serverId, newerRun);
        Assert.NotEmpty(olderFiles);
        Assert.NotEmpty(newerFiles);

        // Room for the newer run but not for both: only the older run's files have to go.
        scenario.Settings.RetentionBytes = LogQuery.Size(newerFiles) + (LogQuery.Size(olderFiles) / 2);
        BackendProcess second = await scenario.StartBackendAsync();
        await LogQuery.WaitForAsync(
            second, "minutes=1440", response => LogQuery.Deletions(response, "size").Count > 0, LogWait, "a size deletion");

        Assert.All(olderFiles, file => Assert.False(File.Exists(file), $"{file} is the oldest and must be removed for size"));
        Assert.All(newerFiles, file => Assert.True(File.Exists(file), $"{file} fits in retentionBytes and must be kept"));
    }

    [LinuxFact]
    public async Task E7_LastFileOfRunningRunIsKeptByPeriodicRetention()
    {
        await using BackendScenario scenario = BackendScenario.Create("e7");
        scenario.Stub.WriteLine("stdout", "e7 before");
        BackendProcess first = await scenario.StartWithReleaseAsync();
        (string serverId, string finishedRun) = await RunUntilLastLineAsync(first, "e7 before");
        await first.TerminateAsync(TimeSpan.FromSeconds(30));
        IReadOnlyList<string> finishedFiles = DataLayout.LogFiles(scenario.DataDirectory, serverId, finishedRun);
        Assert.NotEmpty(finishedFiles);

        // The finished run fits at backend start. The running run then grows past the limit, so periodic retention
        // first removes the finished run (proof that it runs) and next meets the running run's protected last file.
        string runningText = "e7 running " + new string('x', 189);
        scenario.Settings.RetentionBytes = LogQuery.Size(finishedFiles) + 4096;
        scenario.Settings.RetentionIntervalSeconds = 1;
        scenario.Stub.Output.Clear();
        scenario.Stub.LineIntervalMilliseconds = 75;
        scenario.Stub.WriteLine("stdout", runningText, count: 80);
        BackendProcess second = await scenario.StartBackendAsync();
        string runningRun = JsonFields.Text(await BackendScenario.StartServerAsync(second), "server.runId");
        await LogQuery.WaitForAsync(
            second, "minutes=10", response => LogQuery.Deletions(response, "size").Count > 0, TimeSpan.FromSeconds(30),
            "periodic retention removing the finished run");
        await LogQuery.WaitForTextAsync(second, $"runId={runningRun}&limit=5000", StubBehavior.CountedText(runningText, 80), TimeSpan.FromSeconds(30));
        long runningBytes = LogQuery.Size(DataLayout.LogFiles(scenario.DataDirectory, serverId, runningRun));
        bool keptThroughCycles = await StaysTrueAsync(
            () => DataLayout.LogFiles(scenario.DataDirectory, serverId, runningRun).Count > 0,
            TimeSpan.FromSeconds((3 * scenario.Settings.RetentionIntervalSeconds) + 1));
        JsonNode runningLogs = await LogQuery.GetAsync(second, $"runId={runningRun}&limit=5000");
        JsonNode status = await second.StatusAsync();

        Assert.All(finishedFiles, file => Assert.False(File.Exists(file), $"{file} must be removed by periodic retention"));
        Assert.True(runningBytes > scenario.Settings.RetentionBytes, $"running run has {runningBytes} bytes, not over the limit");
        Assert.True(keptThroughCycles, "the running run's last log file was removed");
        Assert.Contains(StubBehavior.CountedText(runningText, 1), LogQuery.Texts(runningLogs));
        Assert.Equal("running", JsonFields.Text(status, "server.state"));
    }

    static async Task<(string ServerId, string RunId)> RunUntilLastLineAsync(BackendProcess backend, string lastLine)
    {
        JsonNode running = await BackendScenario.StartServerAsync(backend);
        string runId = JsonFields.Text(running, "server.runId");
        await LogQuery.WaitForTextAsync(backend, $"runId={runId}&limit=5000", lastLine, LogWait);
        await BackendScenario.StopServerAsync(backend);
        return (JsonFields.Text(running, "server.serverId"), runId);
    }

    static async Task<bool> StaysTrueAsync(Func<bool> condition, TimeSpan window)
    {
        DateTime end = DateTime.UtcNow + window;
        while (DateTime.UtcNow < end)
        {
            if (!condition())
            {
                return false;
            }

            await Task.Delay(250);
        }

        return condition();
    }
}

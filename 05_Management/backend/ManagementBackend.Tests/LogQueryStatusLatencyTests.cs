using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using Dawnholder.Management.Backend.Tests.Support;
using Xunit.Abstractions;

namespace Dawnholder.Management.Backend.Tests;

/// <summary>
/// E8: a status query sent while a large log query runs answers within 0.5 s (screen-design.md 「백엔드 조정」 B-4;
/// PR1 verdict observation 1 measured about 2 s). The log files are written straight into the layout of
/// backend-design.md 「로그 저장과 보존」 with every line inside the query window, and the query asks for a word no
/// line holds, so any implementation has to read every line. Timings and sizes go to the test output.
/// </summary>
public sealed class LogQueryStatusLatencyTests
{
    const string Query = "minutes=60&contains=e8-absent-word&limit=1000";
    const long FileBytes = 16L * 1024 * 1024;
    const long InitialBytes = 512L * 1024 * 1024;
    const long MaximumBytes = 2L * 1024 * 1024 * 1024;

    static readonly TimeSpan SoloFloor = TimeSpan.FromSeconds(1.5);
    static readonly TimeSpan StatusBound = TimeSpan.FromSeconds(0.5);
    static readonly TimeSpan StatusDelay = TimeSpan.FromMilliseconds(300);
    static readonly TimeSpan LogRequestTimeout = TimeSpan.FromSeconds(60);

    readonly ITestOutputHelper _output;

    public LogQueryStatusLatencyTests(ITestOutputHelper output) => _output = output;

    [LinuxFact(Timeout = 240_000)]
    public async Task E8_StatusAnswersWithinHalfASecondWhileALargeLogQueryRuns()
    {
        await using BackendScenario scenario = BackendScenario.Create("e8");
        scenario.Settings.RetentionBytes = 4L * 1024 * 1024 * 1024; // the fixture must survive retention at backend start
        BackendProcess first = await scenario.StartBackendAsync();
        string serverId = JsonFields.Text(await first.StatusAsync(), "server.serverId");
        await first.TerminateAsync(TimeSpan.FromSeconds(30));

        // The backend is stopped while the files are written and restarted after, so it cannot have cached a file list.
        LogFixture fixture = new(scenario.DataDirectory, serverId);
        fixture.GrowTo(InitialBytes);
        BackendProcess backend = await scenario.StartBackendAsync();
        (ApiResult solo, TimeSpan soloTime) = await TimedLogQueryAsync(backend);
        Report("first", fixture, solo, soloTime);
        if (soloTime < SoloFloor && fixture.Bytes < MaximumBytes)
        {
            long target = Math.Min(MaximumBytes, (long)(fixture.Bytes * (SoloFloor / soloTime) * 1.5));
            await backend.TerminateAsync(TimeSpan.FromSeconds(30));
            fixture.GrowTo(target);
            backend = await scenario.StartBackendAsync();
            (solo, soloTime) = await TimedLogQueryAsync(backend);
            Report("grown", fixture, solo, soloTime);
        }

        await backend.StatusAsync(); // warm the status path before timing it
        Task<(ApiResult Result, TimeSpan Elapsed)> during = TimedLogQueryAsync(backend);
        await Task.Delay(StatusDelay);
        bool inFlightWhenSent = !during.IsCompleted;
        Stopwatch statusWatch = Stopwatch.StartNew();
        ApiResult status = await backend.GetAsync("/api/status");
        statusWatch.Stop();
        bool inFlightWhenAnswered = !during.IsCompleted;
        (ApiResult duringResult, TimeSpan duringTime) = await during;
        _output.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"E8 measured: log_query_status={duringResult.Code} log_query_seconds={duringTime.TotalSeconds:F3} "
            + $"status_sent_after_seconds={StatusDelay.TotalSeconds:F3} status_code={status.Code} status_seconds={statusWatch.Elapsed.TotalSeconds:F3} "
            + $"log_query_in_flight_when_sent={inFlightWhenSent} when_answered={inFlightWhenAnswered}"));

        Assert.True(solo.Code == 200, $"the solo log query must succeed: {solo.Describe()}");
        Assert.True(
            soloTime >= SoloFloor,
            $"fixture: the solo log query took {soloTime.TotalSeconds:F3} s over {fixture.Bytes} bytes; it must take at least {SoloFloor.TotalSeconds} s for this measurement");
        Assert.True(inFlightWhenSent, $"the log query ended within {StatusDelay.TotalMilliseconds} ms, so the status query did not overlap it");
        Assert.True(status.Code == 200, $"GET /api/status during the log query: {status.Describe()}");
        Assert.True(
            statusWatch.Elapsed < StatusBound,
            $"GET /api/status sent during a {duringTime.TotalSeconds:F3} s log query answered after {statusWatch.Elapsed.TotalSeconds:F3} s; it must answer within {StatusBound.TotalSeconds} s");
        Assert.True(duringResult.Code == 200, $"the overlapped log query must succeed: {duringResult.Describe()}");
    }

    static async Task<(ApiResult Result, TimeSpan Elapsed)> TimedLogQueryAsync(BackendProcess backend)
    {
        Stopwatch watch = Stopwatch.StartNew();
        ApiResult result = await backend.SendAsync(HttpMethod.Get, $"/api/logs?{Query}", timeout: LogRequestTimeout);
        return (result, watch.Elapsed);
    }

    void Report(string round, LogFixture fixture, ApiResult solo, TimeSpan soloTime) =>
        _output.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"E8 {round} fixture: files={fixture.Files} lines={fixture.Lines} bytes={fixture.Bytes}; "
            + $"solo log query: status={solo.Code} seconds={soloTime.TotalSeconds:F3} response_bytes={solo.Body.Length}"));

    /// <summary>
    /// One finished run of the server, recorded the way backend-design.md 「서버·실행 기록」 fixes run.json, and its
    /// 16 MiB log files written directly. Lines are collected a few minutes ago, inside the query window.
    /// </summary>
    sealed class LogFixture
    {
        readonly string _runDirectory;
        readonly DateTime _firstCollected = DateTime.UtcNow - TimeSpan.FromMinutes(5);

        public LogFixture(string dataDirectory, string serverId)
        {
            string runId = Guid.NewGuid().ToString();
            _runDirectory = DataLayout.RunDirectory(dataDirectory, serverId, runId);
            Directory.CreateDirectory(_runDirectory);
            JsonObject record = new()
            {
                ["runId"] = runId,
                ["startedAt"] = Stamp(_firstCollected - TimeSpan.FromSeconds(1)),
                ["endedAt"] = Stamp(_firstCollected + TimeSpan.FromMinutes(1)),
                ["pid"] = 4194303, // the run is over; the largest Linux pid rather than one likely to be alive
                ["releaseCommit"] = new string('e', 40),
                ["exitKind"] = "graceful",
                ["exitCode"] = 0,
                ["signal"] = null,
                ["reason"] = null,
            };
            File.WriteAllText(DataLayout.RunRecord(dataDirectory, serverId, runId), record.ToJsonString());
        }

        public int Files { get; private set; }

        public long Lines { get; private set; }

        public long Bytes { get; private set; }

        /// <summary>Adds whole 16 MiB files until <see cref="Bytes"/> reaches <paramref name="target"/>.</summary>
        public void GrowTo(long target)
        {
            while (Bytes < target)
            {
                Files++;
                string path = Path.Combine(_runDirectory, $"log-{Files:D6}.jsonl");
                using StreamWriter writer = new(path, append: false, new UTF8Encoding(false), bufferSize: 1 << 20);
                long fileBytes = 0;
                while (fileBytes < FileBytes)
                {
                    Lines++;
                    string line = string.Create(
                        CultureInfo.InvariantCulture,
                        $"{{\"seq\":{Lines},\"collectedAt\":\"{Stamp(_firstCollected.AddTicks(Lines * 10))}\",\"stream\":\"stdout\",\"text\":\"e8 fixture line {Lines:D8}\",\"textTruncated\":false}}\n");
                    writer.Write(line);
                    fileBytes += line.Length;
                }

                Bytes += fileBytes;
            }
        }

        static string Stamp(DateTime time) => time.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture);
    }
}

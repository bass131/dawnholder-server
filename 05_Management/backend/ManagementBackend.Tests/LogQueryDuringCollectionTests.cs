using System.Text.Json.Nodes;
using Dawnholder.Management.Backend.Tests.Support;

namespace Dawnholder.Management.Backend.Tests;

/// <summary>
/// screen-design.md 「백엔드 조정」 B-4 moved log reading out of the collection lock: GET /api/logs decides only the file
/// list under the lock and reads after it. backend-design.md 「로그 조회」 still answers whole lines in collection order and
/// a read failure as an error. Independent verifier test (PR2 verification): queries run while the stub is still writing
/// and the run rotates into its next 16 MiB file, and every answer must be 200 with a gap-free prefix of the stub's lines.
/// </summary>
public sealed class LogQueryDuringCollectionTests
{
    // About 8 KB of JSON per line, so the run passes 16 MiB and rotates once (as in LogRotationTests).
    const int CountedLines = 2300;

    // A short pause between stub lines keeps the writing going for seconds, so queries overlap the collection.
    const int LineIntervalMilliseconds = 2;

    static readonly string LongText = new('q', 8000);

    [LinuxFact]
    public async Task QueriesWhileCollectingAndRotatingAnswerWholeLinesWithoutGaps()
    {
        await using BackendScenario scenario = BackendScenario.Create("log-query-during-collection");
        scenario.Stub.LineIntervalMilliseconds = LineIntervalMilliseconds;
        scenario.Stub.WriteLine("stdout", LongText, CountedLines);
        BackendProcess backend = await scenario.StartWithReleaseAsync();
        JsonNode running = await BackendScenario.StartServerAsync(backend);
        string serverId = JsonFields.Text(running, "server.serverId");
        string runId = JsonFields.Text(running, "server.runId");
        string path = $"/api/logs?minutes=10&limit=5000&runId={runId}";

        List<string> problems = [];
        int answers = 0;
        int answersWhileWriting = 0;
        int mostCountedLines = 0;
        DateTime deadline = DateTime.UtcNow + TimeSpan.FromSeconds(120);
        while (mostCountedLines < CountedLines && DateTime.UtcNow < deadline)
        {
            ApiResult result = await backend.GetAsync(path);
            answers++;
            if (result.Code != 200)
            {
                problems.Add($"answer {answers}: {Shorten(result.Describe())}");
                continue;
            }

            IReadOnlyList<JsonNode> lines = LogQuery.Lines(result.Json!);
            long[] seqs = [.. lines.Select(line => JsonFields.Number(line, "seq"))];
            string[] counted = [.. lines.Select(line => JsonFields.Text(line, "text")).Where(text => text.StartsWith(LongText, StringComparison.Ordinal))];
            bool seqWithoutGap = seqs.Zip(seqs.Skip(1)).All(pair => pair.Second == pair.First + 1);
            int firstWrong = -1;
            for (int index = 0; index < counted.Length && firstWrong < 0; index++)
            {
                if (counted[index] != StubBehavior.CountedText(LongText, index + 1)) firstWrong = index;
            }

            if (!seqWithoutGap)
            {
                problems.Add($"answer {answers}: seq has a gap or repeat [{string.Join(",", seqs.Take(5))}…{string.Join(",", seqs.TakeLast(5))}]");
            }

            if (firstWrong >= 0)
            {
                problems.Add($"answer {answers}: counted line {firstWrong + 1} is not whole or out of order ({counted[firstWrong].Length} chars)");
            }

            if (counted.Length > 0 && counted.Length < CountedLines)
            {
                answersWhileWriting++;
            }

            mostCountedLines = Math.Max(mostCountedLines, counted.Length);
        }

        int files = DataLayout.LogFiles(scenario.DataDirectory, serverId, runId).Count;
        string summary = $"answers={answers}, whileWriting={answersWhileWriting}, mostCountedLines={mostCountedLines}, files={files}";

        Assert.True(problems.Count == 0, $"{summary}; {string.Join(" | ", problems.Take(10))}");
        Assert.True(mostCountedLines == CountedLines, $"every stub line must be answered in the end: {summary}");
        Assert.True(answersWhileWriting >= 1, $"no query overlapped the collection, so this run proves nothing: {summary}");
        Assert.True(files >= 2, $"the run must rotate so a query also overlaps a file change: {summary}");
    }

    static string Shorten(string text) => text.Length <= 300 ? text : $"{text[..300]}…";
}

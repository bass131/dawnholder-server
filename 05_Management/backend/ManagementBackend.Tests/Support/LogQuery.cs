using System.Globalization;
using System.Text.Json.Nodes;

namespace Dawnholder.Management.Backend.Tests.Support;

/// <summary>GET /api/logs and the log files of backend-design.md 「로그 저장과 보존」.</summary>
internal static class LogQuery
{
    public static async Task<JsonNode> GetAsync(BackendProcess backend, string query)
    {
        ApiResult result = await backend.GetAsync($"/api/logs?{query}");
        Assert.True(result.Code == 200, $"GET /api/logs?{query}: {result.Describe()}");
        return result.Json!;
    }

    public static IReadOnlyList<JsonNode> Lines(JsonNode logs) =>
        [.. JsonFields.Array(logs, "lines").Select(line => line ?? throw new InvalidOperationException("null log line"))];

    public static IReadOnlyList<string> Texts(JsonNode logs) => [.. Lines(logs).Select(line => JsonFields.Text(line, "text"))];

    /// <summary>The collection is asynchronous, so tests poll until the stub's line has been read.</summary>
    public static async Task<JsonNode> WaitForTextAsync(BackendProcess backend, string query, string text, TimeSpan timeout)
    {
        DateTime deadline = DateTime.UtcNow + timeout;
        while (true)
        {
            JsonNode logs = await GetAsync(backend, query);
            if (Texts(logs).Contains(text))
            {
                return logs;
            }

            if (DateTime.UtcNow >= deadline)
            {
                Assert.Fail($"'{text}' did not appear in GET /api/logs?{query} within {timeout}; last {JsonFields.Show(logs)}");
            }

            await Task.Delay(100);
        }
    }

    /// <summary>Polls GET /api/logs until <paramref name="condition"/> holds on the response.</summary>
    public static async Task<JsonNode> WaitForAsync(
        BackendProcess backend,
        string query,
        Func<JsonNode, bool> condition,
        TimeSpan timeout,
        string what)
    {
        DateTime deadline = DateTime.UtcNow + timeout;
        while (true)
        {
            JsonNode logs = await GetAsync(backend, query);
            if (condition(logs))
            {
                return logs;
            }

            if (DateTime.UtcNow >= deadline)
            {
                Assert.Fail($"timed out after {timeout} waiting for {what}; last {JsonFields.Show(logs)}");
            }

            await Task.Delay(100);
        }
    }

    /// <summary>Deletions in retention.recentDeletions with the given reason ("age" or "size").</summary>
    public static IReadOnlyList<JsonNode> Deletions(JsonNode logs, string reason) =>
        [.. JsonFields.Array(logs, "retention.recentDeletions")
            .Where(deletion => deletion != null && JsonFields.Text(deletion, "reason") == reason)
            .Select(deletion => deletion!)];

    /// <summary>
    /// Makes log files look collected <paramref name="age"/> earlier: every line's collectedAt and the file time move
    /// back. Only the line format fixed by the design is touched; run it while no backend is running.
    /// </summary>
    public static void Age(IEnumerable<string> files, TimeSpan age)
    {
        foreach (string file in files)
        {
            List<string> lines = [];
            DateTime newest = DateTime.MinValue;
            foreach (string line in File.ReadLines(file).Where(line => line.Length > 0))
            {
                JsonObject entry = JsonNode.Parse(line)!.AsObject();
                DateTime collected = JsonFields.Time(entry, "collectedAt").UtcDateTime - age;
                entry["collectedAt"] = collected.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture);
                lines.Add(entry.ToJsonString());
                newest = collected > newest ? collected : newest;
            }

            Assert.True(lines.Count > 0, $"log file {file} has no lines to age");
            File.WriteAllText(file, string.Join('\n', lines) + "\n");
            File.SetLastWriteTimeUtc(file, newest);
        }
    }

    public static long Size(IEnumerable<string> files) => files.Sum(file => new FileInfo(file).Length);
}

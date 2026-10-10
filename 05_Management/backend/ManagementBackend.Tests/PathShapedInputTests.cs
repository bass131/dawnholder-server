using System.Text.Json.Nodes;
using Dawnholder.Management.Backend.Tests.Support;

namespace Dawnholder.Management.Backend.Tests;

/// <summary>
/// commit and runId values shaped like paths are refused before anything is built from them (backend-design.md
/// 「운영 실행본」 1: lowercase hex of 40 characters only; 「로그 조회」: a runId missing from the records is invalidQuery).
/// </summary>
public sealed class PathShapedInputTests
{
    static readonly TimeSpan LogWait = TimeSpan.FromSeconds(20);

    [LinuxFact]
    public async Task CommitShapedLikeAPathIsInvalidCommitAndCreatesNothing()
    {
        await using BackendScenario scenario = BackendScenario.Create("path-commit");
        ReleaseSourceRepository source = await scenario.CreateReleaseSourceAsync();
        BackendProcess backend = await scenario.StartBackendAsync();
        // Every value is exactly 40 characters, so the length rule alone cannot be what refuses it.
        string[] values =
        [
            PadToCommitLength("../../escaped/"),
            PadToCommitLength("/escaped/"),
            PadToCommitLength("..%2F..%2Fescaped%2F"),
            PadToCommitLength(@"..\..\escaped\"),
            PadToCommitLength(source.FirstCommit[..20] + "/../"),
            source.FirstCommit[..39] + "\0",
        ];
        // JSON escapes decode to real separators before the backend sees the value.
        string escapedSlashes = "{\"commit\":\"..\\u002f..\\u002fescaped\\u002f" + new string('a', 26) + "\"}";
        IReadOnlyList<string> before = Entries(scenario.DataDirectory);

        List<(string Name, ApiResult Result)> answers = [];
        foreach (string value in values)
        {
            answers.Add(($"POST {Show(value)}", await scenario.PostReleaseAsync(backend, value)));
            answers.Add(($"PUT {Show(value)}", await backend.PutAsync("/api/releases/current", new { commit = value })));
        }

        answers.Add(("POST escaped slashes", await backend.SendAsync(HttpMethod.Post, "/api/releases", escapedSlashes)));
        answers.Add(("PUT escaped slashes", await backend.SendAsync(HttpMethod.Put, "/api/releases/current", escapedSlashes)));
        IReadOnlyList<string> after = Entries(scenario.DataDirectory);
        JsonNode status = await backend.StatusAsync();
        string[] escaped = [.. Directory.EnumerateFileSystemEntries(scenario.Root, "escaped*", SearchOption.AllDirectories)];

        foreach ((string name, ApiResult result) in answers)
        {
            Assert.True(result.Code == 400, $"{name}: expected 400, got {result.Describe()}");
            ApiAssert.Error(result, 400, "invalidCommit");
        }

        Assert.Equal(before, after);
        Assert.Empty(escaped);
        Assert.True(JsonFields.IsNull(status, "currentRelease"), $"a refused commit must not set the current release: {JsonFields.Show(status)}");
    }

    [LinuxFact]
    public async Task RunIdShapedLikeAPathIsInvalidQuery()
    {
        await using BackendScenario scenario = BackendScenario.Create("path-runid");
        scenario.Stub.WriteLine("stdout", "path-shaped runId line");
        BackendProcess backend = await scenario.StartWithReleaseAsync();
        JsonNode running = await BackendScenario.StartServerAsync(backend);
        string runId = JsonFields.Text(running, "server.runId");
        string serverId = JsonFields.Text(running, "server.serverId");
        await LogQuery.WaitForTextAsync(backend, $"runId={runId}", "path-shaped runId line", LogWait);
        string[] queries =
        [
            $"runId=..%2F{runId}",
            $"runId={runId}%2F..%2F{runId}",
            $"runId=.%2F{runId}",
            "runId=..%2F..%2Fservers",
            $"runId=..%2F..%2F{serverId}",
            "runId=%2Fetc%2Fpasswd",
        ];
        IReadOnlyList<string> before = Entries(scenario.DataDirectory);

        List<(string Query, ApiResult Result)> answers = [];
        foreach (string query in queries)
        {
            answers.Add((query, await backend.GetAsync($"/api/logs?{query}")));
        }

        JsonNode exact = await LogQuery.GetAsync(backend, $"runId={runId}");
        IReadOnlyList<string> after = Entries(scenario.DataDirectory);

        foreach ((string query, ApiResult result) in answers)
        {
            Assert.True(result.Code == 400, $"'{query}': expected 400, got {result.Describe()}");
            ApiAssert.Error(result, 400, "invalidQuery");
        }

        Assert.Contains("path-shaped runId line", LogQuery.Texts(exact));
        Assert.Equal(before, after);
    }

    static string PadToCommitLength(string value)
    {
        Assert.True(value.Length <= 40, $"test value is longer than a commit: {value}");
        return value + new string('a', 40 - value.Length);
    }

    static string Show(string value) => value.Replace("\0", "\\0", StringComparison.Ordinal);

    /// <summary>Every file and directory under <paramref name="root"/>, relative and ordered, without sizes or times.</summary>
    static IReadOnlyList<string> Entries(string root) =>
        [.. Directory.EnumerateFileSystemEntries(root, "*", SearchOption.AllDirectories)
            .Select(entry => Path.GetRelativePath(root, entry))
            .Order(StringComparer.Ordinal)];
}

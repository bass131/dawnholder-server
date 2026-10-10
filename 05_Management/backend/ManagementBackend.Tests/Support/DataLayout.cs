namespace Dawnholder.Management.Backend.Tests.Support;

/// <summary>
/// The files under dataDirectory that backend-design.md fixes by path (「서버·실행 기록」, 「로그 저장과 보존」,
/// 「운영 실행본」). Tests read only these; everything else stays the backend's own business.
/// </summary>
internal static class DataLayout
{
    public static string ServerDirectory(string dataDirectory, string serverId) =>
        Path.Combine(dataDirectory, "servers", serverId);

    public static string ServerRecord(string dataDirectory, string serverId) =>
        Path.Combine(ServerDirectory(dataDirectory, serverId), "server.json");

    public static string RunDirectory(string dataDirectory, string serverId, string runId) =>
        Path.Combine(ServerDirectory(dataDirectory, serverId), "runs", runId);

    public static string RunRecord(string dataDirectory, string serverId, string runId) =>
        Path.Combine(RunDirectory(dataDirectory, serverId, runId), "run.json");

    /// <summary>log-&lt;6 digits&gt;.jsonl files of one run, in rotation order.</summary>
    public static IReadOnlyList<string> LogFiles(string dataDirectory, string serverId, string runId)
    {
        string directory = RunDirectory(dataDirectory, serverId, runId);
        return Directory.Exists(directory)
            ? [.. Directory.EnumerateFiles(directory, "log-??????.jsonl").Order(StringComparer.Ordinal)]
            : [];
    }

    public static string BuildWork(string dataDirectory) => Path.Combine(dataDirectory, "build-work");

    /// <summary>
    /// Directories named after <paramref name="commit"/> or starting with it (a "&lt;commit&gt;.partial" build output),
    /// wherever the backend keeps its releases.
    /// </summary>
    public static IReadOnlyList<string> DirectoriesNamedFor(string dataDirectory, string commit) =>
        [.. Directory.EnumerateDirectories(dataDirectory, "*", SearchOption.AllDirectories)
            .Where(directory => Path.GetFileName(directory).StartsWith(commit, StringComparison.Ordinal))];

    /// <summary>The single manifest.json inside a directory named exactly <paramref name="commit"/>.</summary>
    public static string ReleaseManifest(string dataDirectory, string commit)
    {
        string[] manifests = [.. Directory.EnumerateFiles(dataDirectory, "manifest.json", SearchOption.AllDirectories)
            .Where(file => Path.GetFileName(Path.GetDirectoryName(file)) == commit)];
        Assert.True(manifests.Length == 1, $"expected one <commit>/manifest.json for {commit}, found [{string.Join(", ", manifests)}]");
        return manifests[0];
    }
}

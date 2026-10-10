using System.Text.Json.Nodes;
using Dawnholder.Management.Backend.Tests.Support;

namespace Dawnholder.Management.Backend.Tests;

/// <summary>
/// Log file rotation of backend-design.md 「로그 저장과 보존」: past 16 MiB a run continues in the next six-digit file,
/// and the numbered files together still hold every collected line once, in seq order.
/// </summary>
public sealed class LogRotationTests
{
    const long RotationBytes = 16L * 1024 * 1024;

    // A file may pass 16 MiB by at most one line; 64 KiB is far above one 8,192-character line in JSON.
    const long OneLineAllowance = 64L * 1024;

    const int LongLines = 2300;

    [LinuxFact]
    public async Task RunOver16MiBContinuesInTheNextNumberedFileWithEveryLineOnce()
    {
        await using BackendScenario scenario = BackendScenario.Create("log-rotation");
        // About 8 KB of JSON per line, so 2,300 lines pass 16 MiB once.
        string longText = new('r', 8000);
        scenario.Stub.WriteLine("stdout", longText, LongLines);
        BackendProcess backend = await scenario.StartWithReleaseAsync();
        JsonNode running = await BackendScenario.StartServerAsync(backend);
        string serverId = JsonFields.Text(running, "server.serverId");
        string runId = JsonFields.Text(running, "server.runId");
        string lastText = StubBehavior.CountedText(longText, LongLines);
        await LogQuery.WaitForAsync(
            backend,
            $"runId={runId}&limit=1",
            logs => LogQuery.Texts(logs).Contains(lastText),
            TimeSpan.FromSeconds(120),
            "the last long line");

        IReadOnlyList<string> files = DataLayout.LogFiles(scenario.DataDirectory, serverId, runId);
        JsonNode[] entries = [.. files.SelectMany(File.ReadLines).Where(line => line.Length > 0).Select(line => JsonNode.Parse(line)!)];
        long[] seqs = [.. entries.Select(entry => JsonFields.Number(entry, "seq"))];
        int longLinesStored = entries.Count(entry => JsonFields.Text(entry, "text").StartsWith(longText, StringComparison.Ordinal));
        JsonNode all = await LogQuery.GetAsync(backend, $"runId={runId}&limit=5000");

        Assert.True(files.Count >= 2, $"expected at least two log files past 16 MiB, found [{string.Join(", ", files.Select(Path.GetFileName))}]");
        for (int index = 0; index < files.Count; index++)
        {
            Assert.Equal($"log-{index + 1:D6}.jsonl", Path.GetFileName(files[index]));
        }

        foreach (string file in files.SkipLast(1))
        {
            long size = new FileInfo(file).Length;
            Assert.True(
                size >= RotationBytes - OneLineAllowance && size <= RotationBytes + OneLineAllowance,
                $"{Path.GetFileName(file)} has {size} bytes; a full file ends at 16 MiB give or take one line");
        }

        // Across the file boundary seq neither repeats nor skips, and every long line the stub wrote is stored once.
        Assert.True(seqs.Zip(seqs.Skip(1)).All(pair => pair.Second == pair.First + 1), "seq must increase by one across all files");
        Assert.Equal(LongLines, longLinesStored);
        Assert.Equal(seqs.Length, LogQuery.Lines(all).Count);
        Assert.False(JsonFields.Flag(all, "truncated"), "all lines fit in limit=5000");
    }
}

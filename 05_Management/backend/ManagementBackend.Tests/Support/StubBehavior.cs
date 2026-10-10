using System.Globalization;
using System.Text.Json;

namespace Dawnholder.Management.Backend.Tests.Support;

/// <summary>
/// What the GameServerStub should do, turned into the environment variables the backend passes through
/// server.environment. Names match 05_Management/backend/GameServerStub/StubOptions.cs.
/// </summary>
internal sealed class StubBehavior
{
    public const string Normal = "normal";
    public const string IgnoreEnter = "ignore-enter";
    public const string Crash = "crash";
    public const string ExitBeforeListen = "exit-before-listen";
    public const string NoListen = "no-listen";

    public StubBehavior(int port, string pidDirectory)
    {
        Port = port;
        PidDirectory = pidDirectory;
    }

    public int Port { get; }

    /// <summary>The stub writes &lt;pid&gt;.pid here, so tests can observe and clean up every stub process.</summary>
    public string PidDirectory { get; }

    public string Mode { get; set; } = Normal;

    public int ExitCode { get; set; } = 3;

    public int CrashAfterMilliseconds { get; set; } = 3000;

    public int ListenDelayMilliseconds { get; set; }

    public int ExitDelayMilliseconds { get; set; }

    public int LineIntervalMilliseconds { get; set; }

    public List<StubLine> Output { get; } = [];

    public StubBehavior WriteLine(string stream, string text, int count = 1)
    {
        Output.Add(new StubLine(stream, text, count));
        return this;
    }

    public Dictionary<string, string> ToEnvironment() => new(StringComparer.Ordinal)
    {
        ["DAWNHOLDER_STUB_PORT"] = Port.ToString(CultureInfo.InvariantCulture),
        ["DAWNHOLDER_STUB_MODE"] = Mode,
        ["DAWNHOLDER_STUB_EXIT_CODE"] = ExitCode.ToString(CultureInfo.InvariantCulture),
        ["DAWNHOLDER_STUB_CRASH_AFTER_MS"] = CrashAfterMilliseconds.ToString(CultureInfo.InvariantCulture),
        ["DAWNHOLDER_STUB_LISTEN_DELAY_MS"] = ListenDelayMilliseconds.ToString(CultureInfo.InvariantCulture),
        ["DAWNHOLDER_STUB_EXIT_DELAY_MS"] = ExitDelayMilliseconds.ToString(CultureInfo.InvariantCulture),
        ["DAWNHOLDER_STUB_LINE_INTERVAL_MS"] = LineIntervalMilliseconds.ToString(CultureInfo.InvariantCulture),
        ["DAWNHOLDER_STUB_PID_DIRECTORY"] = PidDirectory,
        ["DAWNHOLDER_STUB_OUTPUT"] = JsonSerializer.Serialize(
            Output.Select(line => new { stream = line.Stream, text = line.Text, count = line.Count })),
    };

    /// <summary>Stub processes that wrote a pid file, oldest first.</summary>
    public IReadOnlyList<int> StartedPids() =>
        [.. new DirectoryInfo(PidDirectory).EnumerateFiles("*.pid")
            .OrderBy(file => file.LastWriteTimeUtc)
            .Select(file => int.Parse(Path.GetFileNameWithoutExtension(file.Name), CultureInfo.InvariantCulture))];

    /// <summary>The single stub started so far; fails when none or several were started.</summary>
    public int SinglePid()
    {
        IReadOnlyList<int> pids = StartedPids();
        Assert.True(pids.Count == 1, $"expected exactly one stub process, saw [{string.Join(", ", pids)}]");
        return pids[0];
    }

    /// <summary>Formatting rule of a counted stub line ("text 0001"), part of the stub contract.</summary>
    public static string CountedText(string text, int index) => $"{text} {index.ToString("D4", CultureInfo.InvariantCulture)}";
}

internal sealed record StubLine(string Stream, string Text, int Count);

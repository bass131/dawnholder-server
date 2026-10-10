using System.Text.Json;

namespace Dawnholder.Management.GameServerStub;

/// <summary>Failure shapes the management backend must handle around a game server process.</summary>
internal enum StubMode
{
    /// <summary>Listens, then stops with exit code 0 on Enter or when standard input closes.</summary>
    Normal,

    /// <summary>Listens and keeps running whatever arrives on standard input.</summary>
    IgnoreEnter,

    /// <summary>Listens, then exits with <see cref="StubOptions.ExitCode"/> after <see cref="StubOptions.CrashAfter"/>.</summary>
    Crash,

    /// <summary>Exits with <see cref="StubOptions.ExitCode"/> before it ever listens.</summary>
    ExitBeforeListen,

    /// <summary>Keeps running without listening and without reading standard input.</summary>
    NoListen,
}

/// <summary>Stub settings, read only from environment variables so the backend passes them through server.environment.</summary>
internal sealed class StubOptions
{
    public const string PortVariable = "DAWNHOLDER_STUB_PORT";
    public const string ModeVariable = "DAWNHOLDER_STUB_MODE";
    public const string ExitCodeVariable = "DAWNHOLDER_STUB_EXIT_CODE";
    public const string CrashAfterVariable = "DAWNHOLDER_STUB_CRASH_AFTER_MS";
    public const string ListenDelayVariable = "DAWNHOLDER_STUB_LISTEN_DELAY_MS";
    public const string ExitDelayVariable = "DAWNHOLDER_STUB_EXIT_DELAY_MS";
    public const string PidDirectoryVariable = "DAWNHOLDER_STUB_PID_DIRECTORY";
    public const string OutputVariable = "DAWNHOLDER_STUB_OUTPUT";
    public const string LineIntervalVariable = "DAWNHOLDER_STUB_LINE_INTERVAL_MS";
    public const string MaxLifetimeVariable = "DAWNHOLDER_STUB_MAX_LIFETIME_SECONDS";

    static readonly Dictionary<string, StubMode> ModeNames = new(StringComparer.Ordinal)
    {
        ["normal"] = StubMode.Normal,
        ["ignore-enter"] = StubMode.IgnoreEnter,
        ["crash"] = StubMode.Crash,
        ["exit-before-listen"] = StubMode.ExitBeforeListen,
        ["no-listen"] = StubMode.NoListen,
    };

    static readonly JsonSerializerOptions OutputJson = new() { PropertyNameCaseInsensitive = true };

    public required int Port { get; init; }

    public required StubMode Mode { get; init; }

    public required int ExitCode { get; init; }

    public required TimeSpan CrashAfter { get; init; }

    public required TimeSpan ListenDelay { get; init; }

    public required TimeSpan ExitDelay { get; init; }

    public required string? PidDirectory { get; init; }

    public required IReadOnlyList<StubOutputLine> Output { get; init; }

    public required TimeSpan LineInterval { get; init; }

    public required TimeSpan MaxLifetime { get; init; }

    /// <exception cref="FormatException">A variable is missing or outside its range.</exception>
    public static StubOptions FromEnvironment()
    {
        string modeName = Environment.GetEnvironmentVariable(ModeVariable) ?? "normal";
        if (!ModeNames.TryGetValue(modeName, out StubMode mode))
        {
            throw new FormatException($"{ModeVariable} must be one of {string.Join(", ", ModeNames.Keys)}; got '{modeName}'.");
        }

        return new StubOptions
        {
            Port = ReadInt(PortVariable, null, 1, 65535),
            Mode = mode,
            ExitCode = ReadInt(ExitCodeVariable, 3, 1, 255),
            CrashAfter = TimeSpan.FromMilliseconds(ReadInt(CrashAfterVariable, 2000, 0, 600_000)),
            ListenDelay = TimeSpan.FromMilliseconds(ReadInt(ListenDelayVariable, 0, 0, 600_000)),
            ExitDelay = TimeSpan.FromMilliseconds(ReadInt(ExitDelayVariable, 0, 0, 600_000)),
            PidDirectory = Environment.GetEnvironmentVariable(PidDirectoryVariable),
            Output = ReadOutput(),
            LineInterval = TimeSpan.FromMilliseconds(ReadInt(LineIntervalVariable, 0, 0, 10_000)),
            MaxLifetime = TimeSpan.FromSeconds(ReadInt(MaxLifetimeVariable, 300, 1, 3600)),
        };
    }

    static int ReadInt(string name, int? fallback, int min, int max)
    {
        string? text = Environment.GetEnvironmentVariable(name);
        if (text == null && fallback != null)
        {
            return fallback.Value;
        }

        if (!int.TryParse(text, out int value) || value < min || value > max)
        {
            throw new FormatException($"{name} must be an integer {min}..{max}; got '{text}'.");
        }

        return value;
    }

    static IReadOnlyList<StubOutputLine> ReadOutput()
    {
        string? json = Environment.GetEnvironmentVariable(OutputVariable);
        if (string.IsNullOrEmpty(json))
        {
            return [];
        }

        try
        {
            List<StubOutputLine> lines = JsonSerializer.Deserialize<List<StubOutputLine>>(json, OutputJson) ?? [];
            foreach (StubOutputLine line in lines)
            {
                if (line.Stream is not ("stdout" or "stderr") || line.Count < 1)
                {
                    throw new FormatException($"{OutputVariable} entries need stream stdout|stderr and count >= 1.");
                }
            }

            return lines;
        }
        catch (JsonException error)
        {
            throw new FormatException($"{OutputVariable} is not a JSON array of output lines: {error.Message}");
        }
    }
}

/// <summary>
/// One scripted output entry. With <see cref="Count"/> above 1 the stub writes "<see cref="Text"/> 0001" up to the count,
/// so tests can ask for many distinct lines without a long environment value.
/// </summary>
internal sealed record StubOutputLine(string Stream, string Text, int Count = 1);

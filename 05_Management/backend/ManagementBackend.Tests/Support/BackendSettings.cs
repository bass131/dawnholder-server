using System.Text.Json.Nodes;

namespace Dawnholder.Management.Backend.Tests.Support;

/// <summary>
/// The backend configuration file of backend-design.md 「설정」: dotted keys are nested objects, times and sizes are
/// JSON integers. Defaults keep every test away from 7777, the real lock file and the real data directory.
/// </summary>
internal sealed class BackendSettings
{
    public BackendSettings(string dataDirectory, string portLockFile, StubBehavior stub)
    {
        DataDirectory = dataDirectory;
        PortLockFile = portLockFile;
        Stub = stub;
    }

    public int ListenPort { get; set; }

    public string DataDirectory { get; set; }

    public string DisplayName { get; set; } = "시험 서버";

    public string PortLockFile { get; set; }

    public int StartTimeoutSeconds { get; set; } = 10;

    public int StopTimeoutSeconds { get; set; } = 5;

    public string? SourceRepository { get; set; }

    /// <summary>Paths inside the temporary release repository built by <see cref="ReleaseSourceRepository"/>.</summary>
    public string ProjectPath { get; set; } = "GameServerStub/GameServerStub.csproj";

    public List<string> ArchivePaths { get; } = ["GameServerStub", "global.json"];

    public string EntryAssembly { get; set; } = "GameServerStub.dll";

    public int BuildTimeoutSeconds { get; set; } = 300;

    public int RetentionDays { get; set; } = 7;

    public long RetentionBytes { get; set; } = 1L << 30;

    public int RetentionIntervalSeconds { get; set; } = 600;

    public StubBehavior Stub { get; }

    public JsonObject ToJson()
    {
        JsonObject environment = new();
        foreach ((string name, string value) in Stub.ToEnvironment())
        {
            environment[name] = value;
        }

        JsonObject release = new()
        {
            ["projectPath"] = ProjectPath,
            ["archivePaths"] = new JsonArray([.. ArchivePaths.Select(path => (JsonNode?)JsonValue.Create(path))]),
            ["entryAssembly"] = EntryAssembly,
            ["buildTimeoutSeconds"] = BuildTimeoutSeconds,
        };
        if (SourceRepository != null)
        {
            release["sourceRepository"] = SourceRepository;
        }

        return new JsonObject
        {
            ["listenPort"] = ListenPort,
            ["dataDirectory"] = DataDirectory,
            ["server"] = new JsonObject
            {
                ["displayName"] = DisplayName,
                ["port"] = Stub.Port,
                ["portLockFile"] = PortLockFile,
                ["dotnetPath"] = BuiltProcesses.Dotnet,
                ["environment"] = environment,
                ["startTimeoutSeconds"] = StartTimeoutSeconds,
                ["stopTimeoutSeconds"] = StopTimeoutSeconds,
            },
            ["release"] = release,
            ["logs"] = new JsonObject
            {
                ["retentionDays"] = RetentionDays,
                ["retentionBytes"] = RetentionBytes,
                ["retentionIntervalSeconds"] = RetentionIntervalSeconds,
            },
        };
    }
}

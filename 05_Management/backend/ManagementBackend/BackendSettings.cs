using System.Text.Json;

namespace Dawnholder.Management.Backend;

internal sealed class BackendSettings
{
    public int ListenPort { get; init; } = 47321;
    public string DataDirectory { get; init; } = Path.Combine(Home, ".local/share/dawnholder/management");
    public ServerSettings Server { get; init; } = new();
    public ReleaseSettings Release { get; init; } = new();
    public LogSettings Logs { get; init; } = new();

    internal static string Home => Environment.GetEnvironmentVariable("HOME")
        ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    public static BackendSettings Load(string[] args)
    {
        string path = Path.Combine(Home, ".config/dawnholder/management/backend.json");
        if (args.Length != 0)
        {
            if (args.Length != 2 || args[0] != "--config")
                throw new ArgumentException("설정 인자는 --config <절대 경로>입니다.");
            path = args[1];
            Absolute(path, "config");
        }

        BackendSettings settings;
        try
        {
            settings = File.Exists(path)
                ? JsonSerializer.Deserialize<BackendSettings>(File.ReadAllText(path), JsonFiles.Options)
                    ?? throw new ArgumentException("config: 설정 객체가 필요합니다.")
                : new BackendSettings();
        }
        catch (JsonException error)
        {
            throw new ArgumentException($"설정 {error.Path}: 올바른 JSON 값이 필요합니다.", error);
        }

        settings.Validate();
        return settings;
    }

    private static void Absolute(string? path, string key)
    {
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path))
            throw new ArgumentException($"{key}: 절대 경로가 필요합니다.");
    }

    private static void Relative(string? path, string key)
    {
        if (string.IsNullOrWhiteSpace(path) || Path.IsPathRooted(path)
            || path.Split('/', '\\').Any(part => part is ".." or "." or "") || path.Contains('\0'))
        {
            throw new ArgumentException($"{key}: 저장소 안의 상대 경로가 필요합니다.");
        }
    }

    private static void Positive(long value, string key)
    {
        if (value < 1) throw new ArgumentException($"{key}: 양의 정수가 필요합니다.");
    }

    private void Validate()
    {
        Absolute(DataDirectory, "dataDirectory");
        if (ListenPort is < 0 or > 65535) throw new ArgumentException("listenPort: 0~65535 범위여야 합니다.");
        if (Server == null) throw new ArgumentException("server: 객체가 필요합니다.");
        if (Release == null) throw new ArgumentException("release: 객체가 필요합니다.");
        if (Logs == null) throw new ArgumentException("logs: 객체가 필요합니다.");
        if (Server.Port is < 1 or > 65535) throw new ArgumentException("server.port: 1~65535 범위여야 합니다.");
        Absolute(Server.PortLockFile, "server.portLockFile");
        Absolute(Server.DotnetPath, "server.dotnetPath");
        if (!File.Exists(Server.DotnetPath)) throw new ArgumentException("server.dotnetPath: 실행 파일이 없습니다.");
        if (string.IsNullOrWhiteSpace(Server.DisplayName)) throw new ArgumentException("server.displayName: 이름이 필요합니다.");
        Positive(Server.StartTimeoutSeconds, "server.startTimeoutSeconds");
        Positive(Server.StopTimeoutSeconds, "server.stopTimeoutSeconds");
        if (Server.Environment == null || Server.Environment.Any(pair =>
            string.IsNullOrEmpty(pair.Key) || pair.Key.Contains('=') || pair.Key.Contains('\0')
            || pair.Value == null || pair.Value.Contains('\0')))
        {
            throw new ArgumentException("server.environment: 올바른 환경 변수 이름과 값이 필요합니다.");
        }
        if (!string.IsNullOrEmpty(Release.SourceRepository)) Absolute(Release.SourceRepository, "release.sourceRepository");
        Relative(Release.ProjectPath, "release.projectPath");
        Relative(Release.EntryAssembly, "release.entryAssembly");
        if (Release.ArchivePaths == null || Release.ArchivePaths.Length == 0)
            throw new ArgumentException("release.archivePaths: 경로가 필요합니다.");
        foreach (string entry in Release.ArchivePaths) Relative(entry, "release.archivePaths");
        Positive(Release.BuildTimeoutSeconds, "release.buildTimeoutSeconds");
        Positive(Logs.RetentionDays, "logs.retentionDays");
        Positive(Logs.RetentionBytes, "logs.retentionBytes");
        Positive(Logs.RetentionIntervalSeconds, "logs.retentionIntervalSeconds");
    }
}

internal sealed class ServerSettings
{
    public string DisplayName { get; init; } = "Dawnholder 라이브 서버";
    public int Port { get; init; } = 7777;
    public string PortLockFile { get; init; } = Path.Combine(BackendSettings.Home, ".cache/dawnholder/locks/server-7777.lock");
    public string? DotnetPath { get; init; } = System.Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
    public Dictionary<string, string> Environment { get; init; } = [];
    public int StartTimeoutSeconds { get; init; } = 12;
    public int StopTimeoutSeconds { get; init; } = 15;
}

internal sealed class ReleaseSettings
{
    public string? SourceRepository { get; init; }
    public string ProjectPath { get; init; } = "02_Server/GameServer/GameServer.csproj";
    public string[] ArchivePaths { get; init; } =
        ["02_Server/GameServer", "02_Server/Network", "98_Shared", "Directory.Build.props", "global.json", ".editorconfig"];
    public string EntryAssembly { get; init; } = "GameServer.dll";
    public int BuildTimeoutSeconds { get; init; } = 600;
}

internal sealed class LogSettings
{
    public int RetentionDays { get; init; } = 7;
    public long RetentionBytes { get; init; } = 1073741824;
    public int RetentionIntervalSeconds { get; init; } = 600;
}

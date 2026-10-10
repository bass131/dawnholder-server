namespace Dawnholder.Management.Backend;

// Persisted runs describe history only; ServerSupervisor determines live process state.
internal sealed class ServerRecordStore
{
    private readonly object _gate = new();
    private readonly Dictionary<string, RunRecord> _runs = [];
    private RunRecord? _lastExit;

    public ServerRecordStore(BackendSettings settings)
    {
        string root = Path.Combine(settings.DataDirectory, "servers");
        Directory.CreateDirectory(root);
        string[] records = Directory.GetFiles(root, "server.json", SearchOption.AllDirectories);
        if (records.Length > 1) throw new IOException("서버 기록이 둘 이상입니다.");
        Server = records.Length == 1 ? JsonFiles.Read<ServerRecord>(records[0])
            : new ServerRecord(Guid.NewGuid().ToString(), settings.Server.DisplayName, settings.Server.Port, DateTime.UtcNow);
        if (!Guid.TryParse(Server.ServerId, out _)) throw new IOException("serverId 기록이 올바르지 않습니다.");
        Server = Server with { DisplayName = settings.Server.DisplayName, Port = settings.Server.Port };
        Root = Path.Combine(root, Server.ServerId);
        RunsDirectory = Path.Combine(Root, "runs");
        Directory.CreateDirectory(RunsDirectory);
        JsonFiles.Write(Path.Combine(Root, "server.json"), Server);
        foreach (string recordPath in Directory.GetFiles(RunsDirectory, "run.json", SearchOption.AllDirectories))
        {
            RunRecord record = JsonFiles.Read<RunRecord>(recordPath);
            if (!Guid.TryParse(record.RunId, out _)) throw new IOException("runId 기록이 올바르지 않습니다.");
            if (record.EndedAt == null)
            {
                record = record with { EndedAt = DateTime.UtcNow, ExitKind = "unknown", Reason = "백엔드 재시작 때 열린 기록을 닫았습니다." };
                JsonFiles.Write(recordPath, record);
            }

            _runs.Add(record.RunId, record);
        }

        _lastExit = _runs.Values.OrderByDescending(run => run.EndedAt).FirstOrDefault();
    }

    public ServerRecord Server { get; }
    public string Root { get; }
    public string RunsDirectory { get; }

    public object? LastExit
    {
        get
        {
            lock (_gate)
            {
                return _lastExit == null ? null : new
                {
                    _lastExit.RunId, _lastExit.EndedAt, kind = _lastExit.ExitKind, _lastExit.ExitCode, _lastExit.Signal,
                };
            }
        }
    }

    public bool HasRun(string runId)
    {
        lock (_gate) return _runs.ContainsKey(runId);
    }

    public void Begin(string runId, DateTime startedAt, int? pid, string commit)
    {
        lock (_gate)
        {
            Save(new RunRecord(runId, startedAt, null, pid, commit, null, null, null, null));
        }
    }

    public void Finish(string runId, string kind, int? exitCode, int? signal, string? reason)
    {
        lock (_gate)
        {
            RunRecord record = _runs[runId] with
            {
                EndedAt = DateTime.UtcNow, ExitKind = kind, ExitCode = exitCode, Signal = signal, Reason = reason,
            };
            Save(record);
            _lastExit = record;
        }
    }

    private void Save(RunRecord record)
    {
        JsonFiles.Write(Path.Combine(RunsDirectory, record.RunId, "run.json"), record);
        _runs[record.RunId] = record;
    }
}

internal sealed record ServerRecord(string ServerId, string DisplayName, int Port, DateTime CreatedAt);
internal sealed record RunRecord(string RunId, DateTime StartedAt, DateTime? EndedAt, int? Pid,
    string ReleaseCommit, string? ExitKind, int? ExitCode, int? Signal, string? Reason);

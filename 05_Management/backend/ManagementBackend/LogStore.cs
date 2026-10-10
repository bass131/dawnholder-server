using System.Text;
using System.Text.Json;

namespace Dawnholder.Management.Backend;

// Collection owns the active segment; readers use length snapshots without holding the collection lock.
internal sealed class LogStore : BackgroundService
{
    private const int MaximumText = 8192;
    private const long RotationBytes = 16 * 1024 * 1024;

    private readonly object _gate = new();
    private readonly object _retentionGate = new();
    private readonly ServerRecordStore _records;
    private readonly LogSettings _settings;
    private string? _activeRun;
    private string? _activeFile;
    private long _sequence;
    private int _segment;
    private string? _error;
    private readonly Queue<LogLine> _recent = new();

    public LogStore(ServerRecordStore records, LogSettings settings)
    {
        _records = records;
        _settings = settings;
        RetainSafely();
    }

    public string? Error => Volatile.Read(ref _error);

    public void Begin(string runId)
    {
        lock (_gate)
        {
            _activeRun = runId;
            _sequence = 0;
            _segment = 1;
            _activeFile = SegmentPath();
            _recent.Clear();
        }
    }

    public void End()
    {
        lock (_gate)
        {
            _activeRun = null;
            _activeFile = null;
        }

        RetainSafely();
    }

    public string RecentText()
    {
        lock (_gate) return string.Join('\n', _recent.Select(line => line.Text));
    }

    public async Task CollectAsync(StreamReader reader, string stream)
    {
        // ReadLineAsync would allocate unbounded text for a missing newline; retain only the allowed prefix.
        StringBuilder line = new(MaximumText);
        bool truncated = false;
        bool skipLf = false;
        char[] buffer = new char[4096];
        try
        {
            int count;
            while ((count = await reader.ReadAsync(buffer)) > 0)
            {
                for (int index = 0; index < count; index++)
                {
                    char character = buffer[index];
                    if (skipLf && character == '\n') { skipLf = false; continue; }
                    skipLf = false;
                    if (character is '\n' or '\r')
                    {
                        Append(stream, line.ToString(), truncated);
                        line.Clear();
                        truncated = false;
                        skipLf = character == '\r';
                    }
                    else if (line.Length < MaximumText)
                    {
                        line.Append(character);
                    }
                    else
                    {
                        truncated = true;
                    }
                }
            }

            if (line.Length > 0 || truncated) Append(stream, line.ToString(), truncated);
        }
        catch (IOException error)
        {
            RecordError(error);
        }
    }

    public object Query(LogQuery query)
    {
        try
        {
            LogFileSnapshot[] files;
            LogFileSnapshot? retention;
            lock (_gate)
            {
                files = SnapshotFiles();
                retention = Snapshot(Path.Combine(_records.Root, "retention-log.jsonl"));
            }

            DateTime to = DateTime.UtcNow;
            DateTime from = to.AddMinutes(-query.Minutes);
            DateTime? oldest = null;
            long totalBytes = 0;
            long matches = 0;
            PriorityQueue<LogLine, (DateTime, long)> selected = new();
            foreach (LogFileSnapshot file in files)
            {
                totalBytes += file.Bytes;
                string runId = Path.GetFileName(Path.GetDirectoryName(file.Path))!;
                foreach (LogEntry entry in ReadEntries(file))
                {
                    if (oldest == null || entry.CollectedAt < oldest) oldest = entry.CollectedAt;
                    if (entry.CollectedAt < from || entry.CollectedAt > to || (query.RunId != null && query.RunId != runId)
                        || (query.Contains.Length > 0 && !query.Contains.Any(word => entry.Text.Contains(word, StringComparison.OrdinalIgnoreCase))))
                    {
                        continue;
                    }
                    matches++;
                    LogLine line = new(runId, entry.Seq, entry.CollectedAt, entry.Stream, entry.Text, entry.TextTruncated);
                    selected.Enqueue(line, (entry.CollectedAt, matches));
                    if (selected.Count > query.Limit) selected.Dequeue();
                }
            }

            List<LogLine> lines = [];
            while (selected.TryDequeue(out LogLine? line, out _)) lines.Add(line);
            RetentionDeletion[] recentDeletions = retention != null
                ? ReadCompleteLines(retention).Where(line => line.Length != 0)
                    .TakeLast(100).Select(line => JsonSerializer.Deserialize<RetentionDeletion>(line, JsonFiles.Options)
                        ?? throw new IOException("보존 기록이 비어 있습니다.")).ToArray()
                : [];
            return new
            {
                _records.Server.ServerId, from, to, lines, truncated = matches > query.Limit,
                retention = new { oldestCollectedAt = oldest, totalBytes, recentDeletions },
            };
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException)
        {
            RecordError(error);
            throw new ApiFailure(500, "logReadFailed", "로그 파일을 읽지 못했습니다.");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(TimeSpan.FromSeconds(_settings.RetentionIntervalSeconds));
        while (await timer.WaitForNextTickAsync(stoppingToken)) RetainSafely();
    }

    private static LogFileSnapshot? Snapshot(string path)
    {
        try { return new(path, new FileInfo(path).Length); }
        catch (FileNotFoundException) { return null; }
        catch (DirectoryNotFoundException) { return null; }
    }

    private static IEnumerable<string> ReadCompleteLines(LogFileSnapshot file)
    {
        FileStream stream;
        try { stream = new(file.Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete); }
        catch (FileNotFoundException) { yield break; } // Retention may delete a snapshotted segment before open.
        catch (DirectoryNotFoundException) { yield break; }
        using (stream)
        using (StreamReader reader = new(stream))
        {
            long remaining = file.Bytes;
            while (remaining > 0 && reader.ReadLine() is { } text)
            {
                // Our JSONL writer uses UTF-8 without BOM and one LF. Ignore an unfinished last line,
                // including a line completed or appended after the snapshot; malformed complete lines still fail.
                remaining -= Encoding.UTF8.GetByteCount(text) + 1;
                if (remaining < 0) yield break;
                yield return text;
            }
        }
    }

    private static IEnumerable<LogEntry> ReadEntries(LogFileSnapshot file)
    {
        foreach (string text in ReadCompleteLines(file))
        {
            if (text.Length == 0) continue;
            yield return JsonSerializer.Deserialize<LogEntry>(text, JsonFiles.Options)
                ?? throw new IOException($"비어 있는 로그 줄: {file}");
        }
    }

    private string SegmentPath() => Path.Combine(_records.RunsDirectory, _activeRun!, $"log-{_segment:D6}.jsonl");

    private IEnumerable<string> Files() => Directory.EnumerateFiles(_records.RunsDirectory, "log-??????.jsonl", SearchOption.AllDirectories);

    private LogFileSnapshot[] SnapshotFiles() => Files().Select(Snapshot).OfType<LogFileSnapshot>().ToArray();

    private void Append(string stream, string text, bool truncated)
    {
        lock (_gate)
        {
            if (_activeRun == null) return;
            LogEntry entry = new(++_sequence, DateTime.UtcNow, stream, text, truncated);
            _recent.Enqueue(new(_activeRun, entry.Seq, entry.CollectedAt, stream, text, truncated));
            while (_recent.Count > 10) _recent.Dequeue();
            try
            {
                if (File.Exists(_activeFile) && new FileInfo(_activeFile).Length > RotationBytes)
                {
                    _segment++;
                    _activeFile = SegmentPath();
                }

                Directory.CreateDirectory(Path.GetDirectoryName(_activeFile!)!);
                File.AppendAllText(_activeFile!, JsonSerializer.Serialize(entry, JsonFiles.Options) + "\n");
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                // Keep draining both pipes after storage failure, otherwise a full pipe could stop the server.
                RecordError(error);
            }
        }
    }

    private void RecordError(Exception error)
    {
        Volatile.Write(ref _error, error.Message);
        Console.Error.WriteLine($"로그 저장소 오류: {error.Message}");
    }

    private void RetainSafely()
    {
        // Serialize deletion decisions with each other, never with a query or the pipe readers.
        lock (_retentionGate)
        {
            try { Retain(); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException)
            {
                RecordError(error);
            }
        }
    }

    private void Retain()
    {
        List<LogFileRange> ranges = [];
        LogFileSnapshot[] files;
        lock (_gate) files = SnapshotFiles();
        foreach (LogFileSnapshot file in files)
        {
            DateTime? first = null;
            DateTime? last = null;
            foreach (LogEntry entry in ReadEntries(file))
            {
                first ??= entry.CollectedAt;
                last = entry.CollectedAt;
            }

            if (last != null) ranges.Add(new(file.Path, first!.Value, last.Value, file.Bytes));
        }

        long total = ranges.Sum(range => range.Bytes);
        TimeSpan retentionAge = TimeSpan.FromDays(_settings.RetentionDays);
        foreach (LogFileRange range in ranges.OrderBy(range => range.To))
        {
            string? reason = DateTime.UtcNow - range.To > retentionAge ? "age" : total > _settings.RetentionBytes ? "size" : null;
            if (reason == null) continue;
            lock (_gate)
            {
                // Collection may rotate or append while the ranges are read. Never delete the active
                // segment or use a stale range to remove a segment that grew after its snapshot.
                if (range.Path == _activeFile || Snapshot(range.Path)?.Bytes != range.Bytes) continue;
                File.Delete(range.Path);
                total -= range.Bytes;
                File.AppendAllText(Path.Combine(_records.Root, "retention-log.jsonl"),
                    JsonSerializer.Serialize(new RetentionDeletion(range.From, range.To, range.Bytes, reason), JsonFiles.Options) + "\n");
            }
        }
    }

    private sealed record LogFileSnapshot(string Path, long Bytes);
    private sealed record LogFileRange(string Path, DateTime From, DateTime To, long Bytes);
}

internal sealed record LogEntry(long Seq, DateTime CollectedAt, string Stream, string Text, bool TextTruncated);
internal sealed record LogLine(string RunId, long Seq, DateTime CollectedAt, string Stream, string Text, bool TextTruncated);
internal sealed record RetentionDeletion(DateTime From, DateTime To, long Bytes, string Reason);

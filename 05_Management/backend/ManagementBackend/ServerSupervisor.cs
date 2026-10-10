using System.Diagnostics;

namespace Dawnholder.Management.Backend;

// Owns the process, stdin and port lease together. Disk records never authorize process control.
internal sealed class ServerSupervisor : IHostedService
{
    private readonly object _state = new();
    private readonly SemaphoreSlim _commands = new(1, 1);
    private readonly BackendSettings _settings;
    private readonly ReleaseStore _releases;
    private readonly ServerRecordStore _records;
    private readonly LogStore _logs;
    private readonly CancellationToken _stopping;
    private readonly DateTime _backendStartedAt;
    private Process? _process;
    private PortLease? _lease;
    private Task _completion = Task.CompletedTask;
    private string _phase = "stopped";
    private string? _runId;
    private DateTime? _startedAt;
    private ReleaseIdentity? _release;
    private DateTime? _stopRequestedAt;
    private string _exitKind = "abnormal";
    private string? _reason;
    private string? _recordError;

    public ServerSupervisor(BackendSettings settings, ReleaseStore releases, ServerRecordStore records,
        LogStore logs, IHostApplicationLifetime lifetime, DateTime backendStartedAt)
    {
        _settings = settings;
        _releases = releases;
        _records = records;
        _logs = logs;
        _stopping = lifetime.ApplicationStopping;
        _backendStartedAt = backendStartedAt;
    }

    public object Status()
    {
        lock (_state) return StatusLocked();
    }

    public async Task<object> StartServerAsync()
    {
        if (!await _commands.WaitAsync(0)) throw Busy();
        try
        {
            _stopping.ThrowIfCancellationRequested();
            Process? process = null;
            string runId;
            lock (_state)
            {
                if (_phase is "starting" or "stopping") throw Busy();
                if (_process != null) throw new ApiFailure(409, "alreadyRunning", "서버가 이미 실행 중입니다.");
                ReleaseIdentity release = _releases.Current
                    ?? throw new ApiFailure(409, "noCurrentRelease", "현재 운영 실행본을 먼저 지정하세요.");
                _lease = PortLease.TryAcquire(_settings.Server.PortLockFile)
                    ?? throw new ApiFailure(409, "portBusy", "다른 실행이 게임 포트 잠금을 사용 중입니다.");
                try
                {
                    if (PortObservation.Read(_settings.Server.Port, null).Listening)
                        throw new ApiFailure(409, "portBusy", "다른 프로세스가 게임 포트에서 대기 중입니다.");
                }
                catch
                {
                    _lease.Dispose();
                    _lease = null;
                    throw;
                }

                runId = Guid.NewGuid().ToString();
                _runId = runId;
                _startedAt = DateTime.UtcNow;
                _release = release;
                _stopRequestedAt = null;
                _phase = "starting";
                _exitKind = "startFailed";
                _reason = null;
            }

            try
            {
                lock (_state)
                {
                    _records.Begin(runId, _startedAt!.Value, null, _release!.Commit);
                    ProcessStartInfo start = new(_settings.Server.DotnetPath!)
                    {
                        WorkingDirectory = _releases.DirectoryFor(_release.Commit),
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                    };
                    start.ArgumentList.Add(Path.Combine(start.WorkingDirectory, _settings.Release.EntryAssembly));
                    foreach ((string key, string value) in _settings.Server.Environment) start.Environment[key] = value;
                    process = Process.Start(start) ?? throw new IOException("서버 프로세스 실행 실패");
                    _process = process;
                    // Keep StandardInput open until process exit; EOF is a real GameServer stop request.
                    _records.Begin(runId, _startedAt.Value, process.Id, _release.Commit);
                    _logs.Begin(runId);
                    Task readers = Task.WhenAll(_logs.CollectAsync(process.StandardOutput, "stdout"), _logs.CollectAsync(process.StandardError, "stderr"));
                    _completion = ObserveExitAsync(process, runId, readers);
                }

                Stopwatch elapsed = Stopwatch.StartNew();
                while (elapsed.Elapsed < TimeSpan.FromSeconds(_settings.Server.StartTimeoutSeconds))
                {
                    _stopping.ThrowIfCancellationRequested();
                    lock (_state)
                    {
                        if (!ReferenceEquals(_process, process) || process.HasExited) break;
                        if (PortObservation.Read(_settings.Server.Port, process.Id).Owner == "self")
                        {
                            _phase = "running";
                            _exitKind = "abnormal";
                            return StatusLocked();
                        }
                    }

                    await Task.Delay(50, _stopping);
                }

                throw new IOException("시간 안에 서버 프로세스의 포트 대기를 확인하지 못했습니다.");
            }
            catch (Exception error)
            {
                Task completion;
                lock (_state)
                {
                    if (_runId == runId)
                    {
                        _exitKind = "startFailed";
                        _reason = error.Message;
                        if (_process != null)
                        {
                            KillOwnedLocked();
                            // A failure before readers were installed still needs an exit observer.
                            if (_completion.IsCompleted && _phase != "stopped")
                                _completion = ObserveExitAsync(_process, runId, Task.CompletedTask);
                        }
                        else
                        {
                            FinishLocked(runId, null);
                        }
                    }

                    completion = _completion;
                }

                await completion;
                throw new ApiFailure(500, "startFailed", "서버 시작에 실패했습니다. " + error.Message + "\n" + _logs.RecentText());
            }
        }
        finally
        {
            _commands.Release();
        }
    }

    public async Task<object> StopServerAsync()
    {
        if (!await _commands.WaitAsync(0)) throw Busy();
        try
        {
            lock (_state)
            {
                if (_phase is "starting" or "stopping") throw Busy();
                if (_process == null || _process.HasExited) throw NotRunning();
                RequestStopLocked("관리 API의 정상 종료 요청");
                // Snapshot under the state lock before the exit observer can turn this response into stopped.
                return StatusLocked();
            }
        }
        finally
        {
            _commands.Release();
        }
    }

    public async Task<object> ForceStopAsync()
    {
        if (!await _commands.WaitAsync(0)) throw Busy();
        try
        {
            Task completion;
            lock (_state)
            {
                if (_process == null || _process.HasExited) throw NotRunning();
                _exitKind = "forced";
                _reason = "관리 API의 강제 종료 요청";
                KillOwnedLocked();
                completion = _completion;
            }

            await completion;
            return Status();
        }
        finally
        {
            _commands.Release();
        }
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        // ApplicationStopping cancels an in-flight start/build. Host's grace period includes this whole cleanup.
        await _commands.WaitAsync();
        try
        {
            Task completion;
            lock (_state)
            {
                if (_process != null && !_process.HasExited) RequestStopLocked("백엔드 종료에 따른 서버 정리");
                completion = _completion;
            }

            try { await completion.WaitAsync(TimeSpan.FromSeconds(_settings.Server.StopTimeoutSeconds)); }
            catch (TimeoutException)
            {
                lock (_state)
                {
                    _exitKind = "forced";
                    _reason = "백엔드 종료 때 정상 종료 시간 초과";
                    KillOwnedLocked();
                }

                await completion;
            }

            await _releases.WaitForIdleAsync();
        }
        finally
        {
            _commands.Release();
        }
    }

    private static ApiFailure Busy() => new(409, "busy", "다른 서버 명령을 처리 중입니다.");
    private static ApiFailure NotRunning() => new(409, "notRunning", "실행 중인 서버가 없습니다.");

    private object StatusLocked()
    {
        bool alive = _process != null && !_process.HasExited;
        int? pid = alive ? _process!.Id : null;
        PortObservation port = PortObservation.Read(_settings.Server.Port, pid);
        bool heldByOther = _lease == null && PortLease.IsHeldByOther(_settings.Server.PortLockFile);

        return new
        {
            backend = new { startedAt = _backendStartedAt, pid = Environment.ProcessId },
            server = new
            {
                _records.Server.ServerId, _records.Server.DisplayName,
                state = _phase, pid, runId = _runId, startedAt = _startedAt, release = _release,
                stopRequestedAt = _stopRequestedAt,
                stopTimedOut = alive && _stopRequestedAt.HasValue
                    && DateTime.UtcNow - _stopRequestedAt.Value >= TimeSpan.FromSeconds(_settings.Server.StopTimeoutSeconds),
                port = _settings.Server.Port, portListening = port.Listening, portOwner = port.Owner,
                portOwnerDetail = port.Detail, portLockHeldByOther = heldByOther,
            },
            currentRelease = _releases.Current, lastExit = _records.LastExit,
            logError = _logs.Error, recordError = _recordError,
        };
    }

    private void RequestStopLocked(string reason)
    {
        _reason = reason;
        if (_stopRequestedAt != null) return;
        _phase = "stopping";
        _stopRequestedAt = DateTime.UtcNow;
        _exitKind = "graceful";
        try
        {
            _process!.StandardInput.WriteLine();
            _process.StandardInput.Flush();
        }
        catch (IOException error)
        {
            // A concurrent exit can close the pipe first; the exit observer still owns reaping and the lease.
            _reason += "; 표준 입력 종료: " + error.Message;
        }
    }

    private void KillOwnedLocked()
    {
        if (_process != null && !_process.HasExited) _process.Kill();
    }

    private async Task ObserveExitAsync(Process process, string runId, Task readers)
    {
        await process.WaitForExitAsync();
        await readers;
        lock (_state)
        {
            FinishLocked(runId, process.ExitCode);
        }
    }

    private void FinishLocked(string runId, int? exitCode)
    {
        try
        {
            if (_records.HasRun(runId))
                _records.Finish(runId, _exitKind, exitCode, _exitKind == "forced" ? 9 : null, _reason);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            _recordError = error.Message;
            Console.Error.WriteLine($"실행 기록 저장 실패: {error}");
        }
        finally
        {
            _logs.End();
            _lease?.Dispose();
            _lease = null;
            _process?.Dispose();
            _process = null;
            _phase = "stopped";
            _runId = null;
            _startedAt = null;
            _release = null;
            _stopRequestedAt = null;
        }
    }
}

namespace Dawnholder.Server.GameServer.Persistence;

/// <summary>Retains cancellation resources until the actual repository task and cancellation callbacks finish.</summary>
sealed class PersistenceOperationTracker
{
    readonly object _gate = new();
    readonly CancellationTokenSource _cancellation = new();
    Task _cancelCompletion = Task.CompletedTask;
    bool _cancellationRequested;
    bool _finished;

    internal CancellationToken Token => _cancellation.Token;
    internal Task? RepositoryTask { get; set; }
    internal PersistenceDeadlineWatch? Watch { get; set; }

    internal void RequestCancellation()
    {
        lock (_gate)
        {
            if (!_finished && !_cancellationRequested)
            {
                _cancellationRequested = true;
                _cancelCompletion = _cancellation.CancelAsync();
            }
        }
    }

    internal void Finish()
    {
        Task cancellation;
        lock (_gate)
        {
            _finished = true;
            cancellation = _cancelCompletion;
        }
        Watch?.Dispose();
        _ = DisposeAfterCancellationAsync(cancellation);
    }

    async Task DisposeAfterCancellationAsync(Task cancellation)
    {
        try
        {
            await cancellation.ConfigureAwait(false);
        }
        catch (Exception)
        {
            // A cancellation callback is not a store outcome; the owner still settles the repository task.
        }
        finally
        {
            _cancellation.Dispose();
        }
    }
}

/// <summary>Schedules against the supplied monotonic deadline, including budgets longer than a native timer span.</summary>
sealed class PersistenceDeadlineWatch : IDisposable
{
    static readonly TimeSpan MaxTimerSpan = TimeSpan.FromDays(30);
    readonly object _gate = new();
    readonly PersistenceDeadline _deadline;
    readonly Action _expired;
    readonly ITimer _timer;
    bool _disposed;

    internal PersistenceDeadlineWatch(TimeProvider timeProvider, PersistenceDeadline deadline, Action expired)
    {
        _deadline = deadline;
        _expired = expired;
        _timer = timeProvider.CreateTimer(_ => Tick(), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    internal void Start()
    {
        lock (_gate)
        {
            if (!_disposed)
                Arm();
        }
    }

    void Arm()
    {
        TimeSpan remaining = _deadline.GetRemaining();
        _timer.Change(remaining < MaxTimerSpan ? remaining : MaxTimerSpan, Timeout.InfiniteTimeSpan);
    }

    void Tick()
    {
        lock (_gate)
        {
            if (_disposed)
                return;
            if (!_deadline.IsExpired)
            {
                Arm();
                return;
            }
        }
        // The callback may enter the owner's gate, so never run it while holding this timer's gate.
        _expired();
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
                return;
            _disposed = true;
            _timer.Dispose();
        }
    }
}

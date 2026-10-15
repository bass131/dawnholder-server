namespace Dawnholder.Server.GameServer.Tests.Persistence;

/// <summary>
/// Test-only TimeProvider. The monotonic timestamp and the wall clock move only when a test says so,
/// and timers created through this provider fire inside <see cref="Advance"/>. It lets deadline tests
/// observe expiry without real waiting and does not encode any product timing rule.
/// </summary>
public sealed class ControllableTimeProvider : TimeProvider
{
    readonly object _gate = new();
    readonly List<ManualTimer> _timers = new();
    long _ticks;
    DateTimeOffset _utcNow = new(2026, 10, 14, 0, 0, 0, TimeSpan.Zero);

    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    public override long GetTimestamp()
    {
        lock (_gate)
            return _ticks;
    }

    public override DateTimeOffset GetUtcNow()
    {
        lock (_gate)
            return _utcNow;
    }

    /// <summary>Moves monotonic time and the wall clock together, then fires every timer that became due.</summary>
    public void Advance(TimeSpan delta)
    {
        if (delta < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(delta));
        long target;
        lock (_gate)
        {
            target = _ticks + delta.Ticks;
            _ticks = target;
            _utcNow += delta;
        }
        FireDue();
    }

    /// <summary>Moves only the wall clock. Monotonic deadlines must not notice this.</summary>
    public void MoveWallClock(TimeSpan delta)
    {
        lock (_gate)
            _utcNow += delta;
    }

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        ArgumentNullException.ThrowIfNull(callback);
        ManualTimer timer = new(this, callback, state);
        lock (_gate)
            _timers.Add(timer);
        timer.Change(dueTime, period);
        return timer;
    }

    void FireDue()
    {
        // Callbacks run outside the lock so product continuations can create or change timers.
        for (int round = 0; round < 10_000; round++)
        {
            ManualTimer? due = null;
            lock (_gate)
            {
                foreach (ManualTimer timer in _timers)
                {
                    if (timer.DueTicks is long at && at <= _ticks && (due is null || at < due.DueTicks))
                        due = timer;
                }
                if (due is null)
                    return;
                due.DueTicks = due.PeriodTicks is long period && period > 0 ? due.DueTicks + period : null;
            }
            due.Invoke();
        }
        throw new InvalidOperationException("Timer rounds exceeded; a periodic timer is firing without bound.");
    }

    void Schedule(ManualTimer timer, TimeSpan dueTime, TimeSpan period)
    {
        bool fireNow = false;
        lock (_gate)
        {
            if (!_timers.Contains(timer))
                return;
            timer.PeriodTicks = period == Timeout.InfiniteTimeSpan || period <= TimeSpan.Zero ? null : period.Ticks;
            if (dueTime == Timeout.InfiniteTimeSpan)
            {
                timer.DueTicks = null;
            }
            else
            {
                timer.DueTicks = _ticks + Math.Max(0, dueTime.Ticks);
                fireNow = dueTime <= TimeSpan.Zero;
            }
        }
        if (fireNow)
            ThreadPool.QueueUserWorkItem(_ => FireDue());
    }

    void Remove(ManualTimer timer)
    {
        lock (_gate)
            _timers.Remove(timer);
    }

    sealed class ManualTimer(ControllableTimeProvider owner, TimerCallback callback, object? state) : ITimer
    {
        internal long? DueTicks { get; set; }

        internal long? PeriodTicks { get; set; }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            owner.Schedule(this, dueTime, period);
            return true;
        }

        public void Dispose() => owner.Remove(this);

        public ValueTask DisposeAsync()
        {
            owner.Remove(this);
            return ValueTask.CompletedTask;
        }

        internal void Invoke() => callback(state);
    }
}

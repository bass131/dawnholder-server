namespace Dawnholder.Server.GameServer.Persistence;

/// <summary>생성 시 고정한 monotonic 예산을 모든 단계가 공유한다. UTC 변경은 영향을 주지 않는다.</summary>
public sealed class PersistenceDeadline : IEquatable<PersistenceDeadline>
{
    readonly TimeProvider _timeProvider;
    readonly long _startedAt;
    readonly TimeSpan _budget;

    PersistenceDeadline(TimeProvider timeProvider, TimeSpan budget)
    {
        _timeProvider = timeProvider;
        _startedAt = timeProvider.GetTimestamp();
        _budget = budget;
    }

    public static PersistenceDeadline FromBudget(TimeProvider timeProvider, TimeSpan budget)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        if (budget < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(budget), "The budget must be finite and nonnegative.");
        return new PersistenceDeadline(timeProvider, budget);
    }

    public TimeSpan GetRemaining()
    {
        TimeSpan elapsed = _timeProvider.GetElapsedTime(_startedAt, _timeProvider.GetTimestamp());
        if (elapsed >= _budget)
            return TimeSpan.Zero;
        // 비정상 clock의 역행도 최초 예산보다 시간을 늘릴 수 없다.
        if (elapsed <= TimeSpan.Zero)
            return _budget;
        return _budget - elapsed;
    }

    public bool IsExpired => GetRemaining() == TimeSpan.Zero;

    public bool Equals(PersistenceDeadline? other) =>
        other is not null && ReferenceEquals(_timeProvider, other._timeProvider) &&
        _startedAt == other._startedAt && _budget == other._budget;

    public override bool Equals(object? obj) => obj is PersistenceDeadline other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(_timeProvider, _startedAt, _budget);
}

namespace Dawnholder.Server.GameServer.Persistence;

public sealed class AcquireAndLoadResult : IEquatable<AcquireAndLoadResult>
{
    AcquireAndLoadResult(
        AcquireAndLoadStatus status, OperationReceipt? receipt = null,
        PersistenceObservation? current = null, PersistenceResultCode? code = null)
    {
        Status = status;
        Receipt = receipt;
        Current = current;
        Code = code;
    }

    public AcquireAndLoadStatus Status { get; }
    public OperationReceipt? Receipt { get; }
    public PersistenceObservation? Current { get; }
    public PersistenceResultCode? Code { get; }
    public bool IsCurrent => PersistenceContractGuard.IsCurrent(Receipt, Current);

    public static AcquireAndLoadResult Acquired(OperationReceipt receipt, PersistenceObservation current)
    {
        PersistenceContractGuard.Receipt(receipt, current, PersistenceOperationKind.Acquire, PersistenceResultCode.Acquired);
        return new(AcquireAndLoadStatus.Acquired, receipt, current);
    }

    public static AcquireAndLoadResult Rejected(PersistenceResultCode code)
    {
        PersistenceContractGuard.NotApplied(code);
        return new(AcquireAndLoadStatus.Rejected, code: code);
    }

    public static AcquireAndLoadResult Conflict() => new(AcquireAndLoadStatus.Conflict, code: PersistenceResultCode.Conflict);
    public static AcquireAndLoadResult NotDispatched() => new(AcquireAndLoadStatus.NotDispatched);
    public static AcquireAndLoadResult AcquireUnknown() => new(AcquireAndLoadStatus.AcquireUnknown);

    public bool Equals(AcquireAndLoadResult? other) =>
        other is not null && Status == other.Status && Equals(Receipt, other.Receipt) &&
        Equals(Current, other.Current) && Code == other.Code;

    public override bool Equals(object? obj) => obj is AcquireAndLoadResult other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Status, Receipt, Current, Code);
}

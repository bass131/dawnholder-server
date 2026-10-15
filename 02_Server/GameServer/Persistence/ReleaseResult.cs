namespace Dawnholder.Server.GameServer.Persistence;

public sealed class ReleaseResult : IEquatable<ReleaseResult>
{
    ReleaseResult(
        ReleaseStatus status, OperationReceipt? receipt = null,
        PersistenceObservation? current = null, PersistenceResultCode? code = null)
    {
        Status = status;
        Receipt = receipt;
        Current = current;
        Code = code;
    }

    public ReleaseStatus Status { get; }
    public OperationReceipt? Receipt { get; }
    public PersistenceObservation? Current { get; }
    public PersistenceResultCode? Code { get; }

    public static ReleaseResult Released(OperationReceipt receipt, PersistenceObservation current)
    {
        PersistenceContractGuard.Receipt(receipt, current, PersistenceOperationKind.ReleaseRuntime, PersistenceResultCode.Released);
        return new(ReleaseStatus.Released, receipt, current);
    }

    public static ReleaseResult Rejected(PersistenceResultCode code)
    {
        PersistenceContractGuard.NotApplied(code);
        return new(ReleaseStatus.Rejected, code: code);
    }

    public static ReleaseResult NotDispatched() => new(ReleaseStatus.NotDispatched);
    public static ReleaseResult ReleaseUnknown() => new(ReleaseStatus.ReleaseUnknown);

    public bool Equals(ReleaseResult? other) =>
        other is not null && Status == other.Status && Equals(Receipt, other.Receipt) &&
        Equals(Current, other.Current) && Code == other.Code;

    public override bool Equals(object? obj) => obj is ReleaseResult other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Status, Receipt, Current, Code);
}

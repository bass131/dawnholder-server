namespace Dawnholder.Server.GameServer.Persistence;

public sealed class ResolveOperationResult : IEquatable<ResolveOperationResult>
{
    ResolveOperationResult(
        OperationResolutionStatus status, OperationReceipt? receipt = null,
        PersistenceObservation? current = null, PersistenceResultCode? code = null)
    {
        Status = status;
        Receipt = receipt;
        Current = current;
        Code = code;
    }

    public OperationResolutionStatus Status { get; }
    public OperationReceipt? Receipt { get; }
    public PersistenceObservation? Current { get; }
    public PersistenceResultCode? Code { get; }

    public static ResolveOperationResult StillUnknown() => new(OperationResolutionStatus.StillUnknown);

    public static ResolveOperationResult NotApplied(PersistenceResultCode code, PersistenceObservation current)
    {
        PersistenceContractGuard.NotApplied(code);
        ArgumentNullException.ThrowIfNull(current);
        return new(OperationResolutionStatus.NotApplied, current: current, code: code);
    }

    public static ResolveOperationResult Applied(OperationReceipt receipt, PersistenceObservation current)
    {
        PersistenceContractGuard.SameIdentity(receipt, current);
        PersistenceContractGuard.KindDomain(receipt.Kind, recovery: false);
        return new(OperationResolutionStatus.Applied, receipt, current);
    }

    public static ResolveOperationResult FenceLost(OperationReceipt? historical, PersistenceObservation current)
    {
        ArgumentNullException.ThrowIfNull(current);
        if (historical is not null)
        {
            PersistenceContractGuard.SameIdentity(historical, current);
            PersistenceContractGuard.KindDomain(historical.Kind, recovery: false);
        }
        return new(OperationResolutionStatus.FenceLost, historical, current);
    }

    public bool Equals(ResolveOperationResult? other) =>
        other is not null && Status == other.Status && Equals(Receipt, other.Receipt) &&
        Equals(Current, other.Current) && Code == other.Code;

    public override bool Equals(object? obj) => obj is ResolveOperationResult other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Status, Receipt, Current, Code);
}

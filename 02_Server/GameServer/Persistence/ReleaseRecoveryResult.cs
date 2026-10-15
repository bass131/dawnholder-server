namespace Dawnholder.Server.GameServer.Persistence;

public sealed class ReleaseRecoveryResult : IEquatable<ReleaseRecoveryResult>
{
    ReleaseRecoveryResult(
        ReleaseRecoveryStatus status, OperationReceipt? receipt = null,
        PersistenceObservation? current = null, PersistenceResultCode? code = null)
    {
        Status = status;
        Receipt = receipt;
        Current = current;
        Code = code;
    }

    public ReleaseRecoveryStatus Status { get; }
    public OperationReceipt? Receipt { get; }
    public PersistenceObservation? Current { get; }
    public PersistenceResultCode? Code { get; }

    public static ReleaseRecoveryResult Released(OperationReceipt receipt, PersistenceObservation current)
    {
        PersistenceContractGuard.Receipt(receipt, current, PersistenceOperationKind.ReleaseRecovery, PersistenceResultCode.Released);
        return new(ReleaseRecoveryStatus.Released, receipt, current, receipt.Code);
    }

    public static ReleaseRecoveryResult Rejected(PersistenceResultCode code)
    {
        PersistenceContractGuard.NotApplied(code);
        return new(ReleaseRecoveryStatus.Rejected, code: code);
    }

    public static ReleaseRecoveryResult NotDispatched() => new(ReleaseRecoveryStatus.NotDispatched);
    public static ReleaseRecoveryResult ReleaseUnknown() => new(ReleaseRecoveryStatus.ReleaseUnknown);

    public bool Equals(ReleaseRecoveryResult? other) =>
        other is not null && Status == other.Status && Equals(Receipt, other.Receipt) &&
        Equals(Current, other.Current) && Code == other.Code;

    public override bool Equals(object? obj) => obj is ReleaseRecoveryResult other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Status, Receipt, Current, Code);
}

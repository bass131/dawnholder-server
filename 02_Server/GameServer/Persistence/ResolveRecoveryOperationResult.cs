namespace Dawnholder.Server.GameServer.Persistence;

/// <summary>관리 operation의 이력 정산. 현재 관측이 바뀌어도 당시 진단값은 이력으로만 보존한다.</summary>
public sealed class ResolveRecoveryOperationResult : IEquatable<ResolveRecoveryOperationResult>
{
    ResolveRecoveryOperationResult(
        RecoveryOperationResolutionStatus status, OperationReceipt? receipt = null,
        PersistenceObservation? current = null, InspectRecoveryResult.StoredState? stored = null,
        PersistenceResultCode? code = null)
    {
        Status = status;
        Receipt = receipt;
        Current = current;
        Stored = stored;
        Code = code;
    }

    public RecoveryOperationResolutionStatus Status { get; }
    public OperationReceipt? Receipt { get; }
    public PersistenceObservation? Current { get; }
    public InspectRecoveryResult.StoredState? Stored { get; }
    public PersistenceResultCode? Code { get; }

    public static ResolveRecoveryOperationResult StillUnknown() => new(RecoveryOperationResolutionStatus.StillUnknown);

    public static ResolveRecoveryOperationResult NotApplied(PersistenceResultCode code, PersistenceObservation current)
    {
        PersistenceContractGuard.NotApplied(code);
        ArgumentNullException.ThrowIfNull(current);
        return new(RecoveryOperationResolutionStatus.NotApplied, current: current, code: code);
    }

    public static ResolveRecoveryOperationResult Applied(
        OperationReceipt receipt, PersistenceObservation current, InspectRecoveryResult.StoredState? stored = null)
    {
        PersistenceContractGuard.SameIdentity(receipt, current);
        PersistenceContractGuard.KindDomain(receipt.Kind, recovery: true);
        ValidateStored(receipt, stored);
        return new(RecoveryOperationResolutionStatus.Applied, receipt, current, stored, receipt.Code);
    }

    public static ResolveRecoveryOperationResult FenceLost(
        OperationReceipt? historical, PersistenceObservation current, InspectRecoveryResult.StoredState? stored = null)
    {
        ArgumentNullException.ThrowIfNull(current);
        if (historical is not null)
        {
            PersistenceContractGuard.SameIdentity(historical, current);
            PersistenceContractGuard.KindDomain(historical.Kind, recovery: true);
        }
        ValidateStored(historical, stored);
        return new(RecoveryOperationResolutionStatus.FenceLost, historical, current, stored, historical?.Code);
    }

    static void ValidateStored(OperationReceipt? receipt, InspectRecoveryResult.StoredState? stored)
    {
        if (receipt?.Kind == PersistenceOperationKind.Recover)
        {
            ArgumentNullException.ThrowIfNull(stored);
            stored.ValidateReceipt(receipt);
        }
        else if (stored is not null)
        {
            throw new ArgumentException("Only a recovery load receipt carries diagnostic stored state.", nameof(stored));
        }
    }

    public bool Equals(ResolveRecoveryOperationResult? other) =>
        other is not null && Status == other.Status && Equals(Receipt, other.Receipt) &&
        Equals(Current, other.Current) && Equals(Stored, other.Stored) && Code == other.Code;

    public override bool Equals(object? obj) => obj is ResolveRecoveryOperationResult other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Status, Receipt, Current, Stored, Code);
}

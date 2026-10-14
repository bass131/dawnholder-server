namespace Dawnholder.Server.GameServer.Persistence;

/// <summary>확정 recovery 이력과 현재 관측을 분리한다. Stored는 당시 진단값이며 Runtime Safe가 아니다.</summary>
public sealed class RecoverAndLoadResult : IEquatable<RecoverAndLoadResult>
{
    RecoverAndLoadResult(
        RecoverAndLoadStatus status, OperationReceipt? receipt = null,
        PersistenceObservation? current = null, InspectRecoveryResult.StoredState? stored = null,
        PersistenceResultCode? code = null)
    {
        Status = status;
        Receipt = receipt;
        Current = current;
        Stored = stored;
        Code = code;
    }

    public RecoverAndLoadStatus Status { get; }
    public OperationReceipt? Receipt { get; }
    public PersistenceObservation? Current { get; }
    public InspectRecoveryResult.StoredState? Stored { get; }
    public PersistenceResultCode? Code { get; }
    public bool IsCurrent => PersistenceContractGuard.IsCurrent(Receipt, Current);

    public static RecoverAndLoadResult Recovered(
        OperationReceipt receipt, PersistenceObservation current, InspectRecoveryResult.StoredState stored)
    {
        PersistenceContractGuard.Receipt(receipt, current, PersistenceOperationKind.Recover, PersistenceResultCode.Recovered);
        ArgumentNullException.ThrowIfNull(stored);
        stored.ValidateReceipt(receipt);
        return new(RecoverAndLoadStatus.Recovered, receipt, current, stored, receipt.Code);
    }

    public static RecoverAndLoadResult RecoveredAbsent(
        OperationReceipt receipt, PersistenceObservation current, InspectRecoveryResult.StoredState stored)
    {
        PersistenceContractGuard.Receipt(receipt, current, PersistenceOperationKind.Recover, PersistenceResultCode.RecoveredAbsent);
        ArgumentNullException.ThrowIfNull(stored);
        stored.ValidateReceipt(receipt);
        return new(RecoverAndLoadStatus.RecoveredAbsent, receipt, current, stored, receipt.Code);
    }

    public static RecoverAndLoadResult Rejected(PersistenceResultCode code)
    {
        PersistenceContractGuard.NotApplied(code);
        return new(RecoverAndLoadStatus.Rejected, code: code);
    }

    public static RecoverAndLoadResult NotDispatched() => new(RecoverAndLoadStatus.NotDispatched);
    public static RecoverAndLoadResult RecoverUnknown() => new(RecoverAndLoadStatus.RecoverUnknown);

    public bool Equals(RecoverAndLoadResult? other) =>
        other is not null && Status == other.Status && Equals(Receipt, other.Receipt) &&
        Equals(Current, other.Current) && Equals(Stored, other.Stored) && Code == other.Code;

    public override bool Equals(object? obj) => obj is RecoverAndLoadResult other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Status, Receipt, Current, Stored, Code);
}

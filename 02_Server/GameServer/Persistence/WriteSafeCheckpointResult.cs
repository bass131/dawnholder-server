namespace Dawnholder.Server.GameServer.Persistence;

public sealed class WriteSafeCheckpointResult : IEquatable<WriteSafeCheckpointResult>
{
    WriteSafeCheckpointResult(
        WriteSafeCheckpointStatus status, OperationReceipt? receipt = null,
        PersistenceObservation? current = null, PersistenceResultCode? code = null)
    {
        Status = status;
        Receipt = receipt;
        Current = current;
        Code = code;
    }

    public WriteSafeCheckpointStatus Status { get; }
    public OperationReceipt? Receipt { get; }
    public PersistenceObservation? Current { get; }
    public PersistenceResultCode? Code { get; }
    public bool IsCurrent => PersistenceContractGuard.IsCurrent(Receipt, Current);

    public static WriteSafeCheckpointResult Durable(OperationReceipt receipt, PersistenceObservation current)
    {
        PersistenceContractGuard.Receipt(receipt, current, PersistenceOperationKind.Checkpoint, PersistenceResultCode.CheckpointApplied);
        return new(WriteSafeCheckpointStatus.Durable, receipt, current);
    }

    public static WriteSafeCheckpointResult CheckpointNoChange(OperationReceipt receipt, PersistenceObservation current)
    {
        PersistenceContractGuard.Receipt(receipt, current, PersistenceOperationKind.Checkpoint, PersistenceResultCode.CheckpointNoChange);
        return new(WriteSafeCheckpointStatus.CheckpointNoChange, receipt, current);
    }

    public static WriteSafeCheckpointResult Rejected(PersistenceResultCode code)
    {
        PersistenceContractGuard.NotApplied(code);
        return new(WriteSafeCheckpointStatus.Rejected, code: code);
    }

    public static WriteSafeCheckpointResult Conflict() => new(WriteSafeCheckpointStatus.Conflict, code: PersistenceResultCode.Conflict);
    public static WriteSafeCheckpointResult NotDispatched() => new(WriteSafeCheckpointStatus.NotDispatched);
    public static WriteSafeCheckpointResult CommitUnknown() => new(WriteSafeCheckpointStatus.CommitUnknown);

    public bool Equals(WriteSafeCheckpointResult? other) =>
        other is not null && Status == other.Status && Equals(Receipt, other.Receipt) &&
        Equals(Current, other.Current) && Code == other.Code;

    public override bool Equals(object? obj) => obj is WriteSafeCheckpointResult other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Status, Receipt, Current, Code);
}

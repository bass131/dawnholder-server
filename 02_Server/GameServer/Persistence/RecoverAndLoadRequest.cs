namespace Dawnholder.Server.GameServer.Persistence;

public sealed class RecoverAndLoadRequest : IEquatable<RecoverAndLoadRequest>
{
    public RecoverAndLoadRequest(
        PersistenceAuthority observedAuthority, Guid recoveryOwnerId, Guid operationId, string reason)
    {
        Operation = new OperationIdentity(observedAuthority, recoveryOwnerId, operationId, reason);
        ObservedAuthority = observedAuthority;
    }

    public OperationIdentity Operation { get; }
    public PersistenceAuthority ObservedAuthority { get; }
    public string Reason => Operation.Reason!;

    public bool Equals(RecoverAndLoadRequest? other) => other is not null && Operation.Equals(other.Operation) && ObservedAuthority.Equals(other.ObservedAuthority);
    public override bool Equals(object? obj) => obj is RecoverAndLoadRequest other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Operation, ObservedAuthority);
}

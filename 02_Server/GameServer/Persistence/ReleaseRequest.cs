namespace Dawnholder.Server.GameServer.Persistence;

public sealed class ReleaseRequest : IEquatable<ReleaseRequest>
{
    public ReleaseRequest(PersistenceAuthority currentAuthority, long sequence, Guid operationId)
    {
        Operation = new OperationIdentity(currentAuthority, sequence, operationId, recovery: false);
        Authority = currentAuthority;
    }

    public OperationIdentity Operation { get; }
    public PersistenceAuthority Authority { get; }

    public bool Equals(ReleaseRequest? other) => other is not null && Operation.Equals(other.Operation);
    public override bool Equals(object? obj) => obj is ReleaseRequest other && Equals(other);
    public override int GetHashCode() => Operation.GetHashCode();
}

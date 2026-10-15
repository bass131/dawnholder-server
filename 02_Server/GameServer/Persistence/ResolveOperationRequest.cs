namespace Dawnholder.Server.GameServer.Persistence;

public sealed class ResolveOperationRequest : IEquatable<ResolveOperationRequest>
{
    public ResolveOperationRequest(OperationIdentity operation, ResolveMode mode)
    {
        PersistenceContractGuard.OperationDomain(operation, recovery: false);
        PersistenceContractGuard.Defined(mode, nameof(mode));
        Operation = operation;
        Mode = mode;
    }

    public OperationIdentity Operation { get; }
    public ResolveMode Mode { get; }

    public bool Equals(ResolveOperationRequest? other) => other is not null && Operation.Equals(other.Operation) && Mode == other.Mode;
    public override bool Equals(object? obj) => obj is ResolveOperationRequest other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Operation, Mode);
}

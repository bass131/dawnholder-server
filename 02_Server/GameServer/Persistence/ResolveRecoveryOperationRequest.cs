namespace Dawnholder.Server.GameServer.Persistence;

public sealed class ResolveRecoveryOperationRequest : IEquatable<ResolveRecoveryOperationRequest>
{
    public ResolveRecoveryOperationRequest(OperationIdentity operation, ResolveMode mode)
    {
        PersistenceContractGuard.OperationDomain(operation, recovery: true);
        PersistenceContractGuard.Defined(mode, nameof(mode));
        Operation = operation;
        Mode = mode;
    }

    public OperationIdentity Operation { get; }
    public ResolveMode Mode { get; }

    public bool Equals(ResolveRecoveryOperationRequest? other) => other is not null && Operation.Equals(other.Operation) && Mode == other.Mode;
    public override bool Equals(object? obj) => obj is ResolveRecoveryOperationRequest other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Operation, Mode);
}

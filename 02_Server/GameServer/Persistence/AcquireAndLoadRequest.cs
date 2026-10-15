using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Persistence;

public sealed class AcquireAndLoadRequest : IEquatable<AcquireAndLoadRequest>
{
    public AcquireAndLoadRequest(
        PersistenceIdentity identity, Guid operationId, Guid ownerId, long expectedFence,
        CharacterClass intendedClass, SafeDefaults safeDefaults)
    {
        Operation = new OperationIdentity(identity, operationId, ownerId, expectedFence, intendedClass, safeDefaults);
    }

    public OperationIdentity Operation { get; }
    public CharacterClass IntendedClass => Operation.IntendedClass!.Value;
    public SafeDefaults SafeDefaults => Operation.SafeDefaults!;

    public bool Equals(AcquireAndLoadRequest? other) => other is not null && Operation.Equals(other.Operation);
    public override bool Equals(object? obj) => obj is AcquireAndLoadRequest other && Equals(other);
    public override int GetHashCode() => Operation.GetHashCode();
}

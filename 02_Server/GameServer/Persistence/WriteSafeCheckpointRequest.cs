namespace Dawnholder.Server.GameServer.Persistence;

public sealed class WriteSafeCheckpointRequest : IEquatable<WriteSafeCheckpointRequest>
{
    public WriteSafeCheckpointRequest(
        PersistenceAuthority currentAuthority, long sequence, Guid operationId,
        PersistenceToken expectedCharacterToken, PersistenceToken? expectedProgressToken, SafeCheckpoint safe)
    {
        Operation = new OperationIdentity(currentAuthority, sequence, operationId, expectedCharacterToken, expectedProgressToken, safe);
        Authority = currentAuthority;
    }

    public OperationIdentity Operation { get; }
    public PersistenceAuthority Authority { get; }
    public PersistenceToken ExpectedCharacterToken => Operation.ExpectedCharacterToken!;
    public PersistenceToken? ExpectedProgressToken => Operation.ExpectedProgressToken;
    public SafeCheckpoint Safe => Operation.Safe!;

    public bool Equals(WriteSafeCheckpointRequest? other) => other is not null && Operation.Equals(other.Operation);
    public override bool Equals(object? obj) => obj is WriteSafeCheckpointRequest other && Equals(other);
    public override int GetHashCode() => Operation.GetHashCode();
}

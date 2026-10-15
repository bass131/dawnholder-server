namespace Dawnholder.Server.GameServer.Persistence;

/// <summary>Immutable final hand-off for one character and runtime lifetime, independent of game entities.</summary>
public sealed class CloseCaptureTicket : IEquatable<CloseCaptureTicket>
{
    public CloseCaptureTicket(PersistenceIdentity identity, Guid runtimeOwnerId, SafeCheckpoint? checkpoint)
    {
        ArgumentNullException.ThrowIfNull(identity);
        if (runtimeOwnerId == Guid.Empty)
            throw new ArgumentException("A runtime lifetime is required.", nameof(runtimeOwnerId));
        Identity = identity;
        RuntimeOwnerId = runtimeOwnerId;
        Checkpoint = checkpoint;
    }

    public PersistenceIdentity Identity { get; }
    public Guid RuntimeOwnerId { get; }
    public SafeCheckpoint? Checkpoint { get; }
    public CloseWriteDecision WriteDecision => Checkpoint is null
        ? CloseWriteDecision.NoWriteNeeded
        : CloseWriteDecision.CheckpointRequired;

    public bool Equals(CloseCaptureTicket? other) => other is not null
        && Identity.Equals(other.Identity)
        && RuntimeOwnerId == other.RuntimeOwnerId
        && Equals(Checkpoint, other.Checkpoint);

    public override bool Equals(object? obj) => obj is CloseCaptureTicket other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Identity, RuntimeOwnerId, Checkpoint);
}

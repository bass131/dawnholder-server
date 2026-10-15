namespace Dawnholder.Server.GameServer.Persistence;

/// <summary>한 저장 주체의 owner·fence·sequence를 나누지 않고 비교한다.</summary>
public sealed class PersistenceAuthority : IEquatable<PersistenceAuthority>
{
    public PersistenceAuthority(
        PersistenceIdentity identity, PersistenceOwnerKind ownerKind, Guid? ownerId,
        long fence, long lastSequence)
    {
        ArgumentNullException.ThrowIfNull(identity);
        PersistenceContractGuard.Defined(ownerKind, nameof(ownerKind));
        PersistenceContractGuard.NonNegative(fence, nameof(fence));
        PersistenceContractGuard.NonNegative(lastSequence, nameof(lastSequence));
        if (ownerKind == PersistenceOwnerKind.Free)
        {
            if (ownerId is not null || lastSequence != 0)
                throw new ArgumentException("Free authority has no owner and sequence zero.");
        }
        else if (ownerId is null || ownerId == Guid.Empty || fence == 0)
        {
            throw new ArgumentException("Held authority requires an owner and a positive fence.");
        }
        Identity = identity;
        OwnerKind = ownerKind;
        OwnerId = ownerId;
        Fence = fence;
        LastSequence = lastSequence;
    }

    public PersistenceIdentity Identity { get; }
    public PersistenceOwnerKind OwnerKind { get; }
    public Guid? OwnerId { get; }
    public long Fence { get; }
    public long LastSequence { get; }

    public bool Equals(PersistenceAuthority? other) =>
        other is not null && Identity.Equals(other.Identity) && OwnerKind == other.OwnerKind &&
        OwnerId == other.OwnerId && Fence == other.Fence && LastSequence == other.LastSequence;

    public override bool Equals(object? obj) => obj is PersistenceAuthority other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Identity, OwnerKind, OwnerId, Fence, LastSequence);
}

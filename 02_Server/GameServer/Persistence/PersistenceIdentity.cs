namespace Dawnholder.Server.GameServer.Persistence;

/// <summary>게임 entity/session ID와 구별되는 고정 저장 바인딩.</summary>
public sealed class PersistenceIdentity : IEquatable<PersistenceIdentity>
{
    public PersistenceIdentity(byte slotId, Guid accountId, Guid characterId)
    {
        if (slotId != 1)
            throw new ArgumentOutOfRangeException(nameof(slotId), "Only slot 1 is supported.");
        PersistenceContractGuard.NonEmpty(accountId, nameof(accountId));
        PersistenceContractGuard.NonEmpty(characterId, nameof(characterId));
        SlotId = slotId;
        AccountId = accountId;
        CharacterId = characterId;
    }

    public byte SlotId { get; }
    public Guid AccountId { get; }
    public Guid CharacterId { get; }

    public bool Equals(PersistenceIdentity? other) =>
        other is not null && SlotId == other.SlotId &&
        AccountId == other.AccountId && CharacterId == other.CharacterId;

    public override bool Equals(object? obj) => obj is PersistenceIdentity other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(SlotId, AccountId, CharacterId);
}

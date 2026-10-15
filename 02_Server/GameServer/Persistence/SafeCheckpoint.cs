using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Persistence;

/// <summary>Town·풀 HP·잠긴 보스 상태만 표현한다. 실제 content 범위 확인은 caller 책임이다.</summary>
public sealed class SafeCheckpoint : IEquatable<SafeCheckpoint>
{
    public SafeCheckpoint(CharacterClass characterClass, float townX, float townY, int maxHp)
    {
        PersistenceContractGuard.Defined(characterClass, nameof(characterClass));
        PersistenceContractGuard.Finite(townX, nameof(townX));
        PersistenceContractGuard.Finite(townY, nameof(townY));
        PersistenceContractGuard.Positive(maxHp, nameof(maxHp));
        CharacterClass = characterClass;
        TownX = townX == 0f ? 0f : townX;
        TownY = townY == 0f ? 0f : townY;
        MaxHp = maxHp;
    }

    public CharacterClass CharacterClass { get; }
    public float TownX { get; }
    public float TownY { get; }
    public int MaxHp { get; }
    public int Hp => MaxHp;
    public bool BossUnlocked => false;

    public bool Equals(SafeCheckpoint? other) =>
        other is not null && CharacterClass == other.CharacterClass &&
        TownX.Equals(other.TownX) && TownY.Equals(other.TownY) && MaxHp == other.MaxHp;

    public override bool Equals(object? obj) => obj is SafeCheckpoint other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(CharacterClass, TownX, TownY, MaxHp);
}

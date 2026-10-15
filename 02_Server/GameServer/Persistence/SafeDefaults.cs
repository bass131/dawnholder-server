namespace Dawnholder.Server.GameServer.Persistence;

/// <summary>기존 저장 class로 선택할 두 content 후보. 선택 의도로 저장 class를 바꾸지 않는다.</summary>
public sealed class SafeDefaults : IEquatable<SafeDefaults>
{
    public SafeDefaults(float townX, float townY, int knightMaxHp, int mageMaxHp)
    {
        PersistenceContractGuard.Finite(townX, nameof(townX));
        PersistenceContractGuard.Finite(townY, nameof(townY));
        PersistenceContractGuard.Positive(knightMaxHp, nameof(knightMaxHp));
        PersistenceContractGuard.Positive(mageMaxHp, nameof(mageMaxHp));
        TownX = townX == 0f ? 0f : townX;
        TownY = townY == 0f ? 0f : townY;
        KnightMaxHp = knightMaxHp;
        MageMaxHp = mageMaxHp;
    }

    public float TownX { get; }
    public float TownY { get; }
    public int KnightMaxHp { get; }
    public int MageMaxHp { get; }

    public bool Equals(SafeDefaults? other) =>
        other is not null && TownX.Equals(other.TownX) && TownY.Equals(other.TownY) &&
        KnightMaxHp == other.KnightMaxHp && MageMaxHp == other.MageMaxHp;

    public override bool Equals(object? obj) => obj is SafeDefaults other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(TownX, TownY, KnightMaxHp, MageMaxHp);
}

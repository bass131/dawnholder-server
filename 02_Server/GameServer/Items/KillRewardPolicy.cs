using Shared.GameData;

namespace Dawnholder.Server.GameServer.Items;

// Temporary killer-only, deterministic content policy. Recipient selection and drop values have
// one replacement point; an unsupported kind is a policy error, never an implicit normal reward.
internal static class KillRewardPolicy
{
    static readonly InventoryReward s_normal = new(10, new InventorySlot(ItemId.Material, 1));
    static readonly InventoryReward s_golem = new(10, new InventorySlot(ItemId.CoinPouch, 1));
    static readonly InventoryReward s_boss = new(50, new InventorySlot(ItemId.Material, 1), new InventorySlot(ItemId.CoinPouch, 1));

    internal static (int RecipientId, InventoryReward Reward) Resolve(int killerId, EnemyKind kind, int enemyId)
    {
        if (killerId <= 0) throw new ArgumentOutOfRangeException(nameof(killerId));
        if (enemyId <= 0) throw new ArgumentOutOfRangeException(nameof(enemyId));
        InventoryReward reward = kind switch
        {
            EnemyKind.Normal => s_normal,
            EnemyKind.Golem => s_golem,
            EnemyKind.Boss => s_boss,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "No reward policy for this enemy kind."),
        };
        return (killerId, reward);
    }
}

using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Dungeon;

internal static class DungeonClearPackets
{
    internal static ArraySegment<byte> Result(InventoryResult result)
    {
        if (result is not (InventoryResult.Success or InventoryResult.InventoryFull or InventoryResult.CurrencyCap))
            throw new ArgumentOutOfRangeException(nameof(result), result, "No dungeon clear wire result for this outcome.");

        IReadOnlyList<InventorySlot> rewards = DungeonClearRewards.Items;
        if (rewards.Count > DungeonClearRewards.MaxItemKinds)
            throw new InvalidOperationException("Dungeon clear rewards exceed the result packet's item slots.");

        // Rejections carry no gains. Success preserves table order and leaves unused slots 0/0.
        S_DungeonClearResult packet = new() { result = (byte)result };
        if (result == InventoryResult.Success)
        {
            packet.currency = DungeonClearRewards.Currency;
            if (rewards.Count > 0)
            {
                packet.reward0ItemId = (int)rewards[0].ItemId;
                packet.reward0Count = rewards[0].Count;
            }
            if (rewards.Count > 1)
            {
                packet.reward1ItemId = (int)rewards[1].ItemId;
                packet.reward1Count = rewards[1].Count;
            }
            if (rewards.Count > 2)
            {
                packet.reward2ItemId = (int)rewards[2].ItemId;
                packet.reward2Count = rewards[2].Count;
            }
        }
        return packet.Write();
    }
}

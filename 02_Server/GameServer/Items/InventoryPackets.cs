using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Items;

// Fixed scalar PDL fields avoid the existing generator's unverified list paths.
internal static class InventoryPackets
{
    internal static ArraySegment<byte> Snapshot(InventoryState state)
    {
        IReadOnlyList<InventorySlot> slots = state.Slots;
        return new S_InventorySnapshot
        {
            revision = state.Revision,
            currency = state.Currency,
            slot0ItemId = (int)slots[0].ItemId,
            slot0Count = slots[0].Count,
            slot1ItemId = (int)slots[1].ItemId,
            slot1Count = slots[1].Count,
            slot2ItemId = (int)slots[2].ItemId,
            slot2Count = slots[2].Count,
            slot3ItemId = (int)slots[3].ItemId,
            slot3Count = slots[3].Count,
            slot4ItemId = (int)slots[4].ItemId,
            slot4Count = slots[4].Count,
            slot5ItemId = (int)slots[5].ItemId,
            slot5Count = slots[5].Count,
            slot6ItemId = (int)slots[6].ItemId,
            slot6Count = slots[6].Count,
            slot7ItemId = (int)slots[7].ItemId,
            slot7Count = slots[7].Count,
        }.Write();
    }

    internal static ArraySegment<byte> UseResult(InventoryResult result, ItemId itemId, uint revision)
        => new S_ItemUseResult { result = (byte)result, itemId = (int)itemId, revision = revision }.Write();
}

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
            slot8ItemId = (int)slots[8].ItemId,
            slot8Count = slots[8].Count,
            slot9ItemId = (int)slots[9].ItemId,
            slot9Count = slots[9].Count,
            slot10ItemId = (int)slots[10].ItemId,
            slot10Count = slots[10].Count,
            slot11ItemId = (int)slots[11].ItemId,
            slot11Count = slots[11].Count,
            slot12ItemId = (int)slots[12].ItemId,
            slot12Count = slots[12].Count,
            slot13ItemId = (int)slots[13].ItemId,
            slot13Count = slots[13].Count,
            slot14ItemId = (int)slots[14].ItemId,
            slot14Count = slots[14].Count,
            slot15ItemId = (int)slots[15].ItemId,
            slot15Count = slots[15].Count,
            slot16ItemId = (int)slots[16].ItemId,
            slot16Count = slots[16].Count,
            slot17ItemId = (int)slots[17].ItemId,
            slot17Count = slots[17].Count,
            slot18ItemId = (int)slots[18].ItemId,
            slot18Count = slots[18].Count,
            slot19ItemId = (int)slots[19].ItemId,
            slot19Count = slots[19].Count,
            slot20ItemId = (int)slots[20].ItemId,
            slot20Count = slots[20].Count,
            slot21ItemId = (int)slots[21].ItemId,
            slot21Count = slots[21].Count,
            slot22ItemId = (int)slots[22].ItemId,
            slot22Count = slots[22].Count,
            slot23ItemId = (int)slots[23].ItemId,
            slot23Count = slots[23].Count,
        }.Write();
    }

    internal static ArraySegment<byte> UseResult(InventoryResult result, ItemId itemId, uint revision)
        => new S_ItemUseResult { result = (byte)result, itemId = (int)itemId, revision = revision }.Write();
}

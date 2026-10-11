using System;
using System.Buffers.Binary;
using Dawnholder.Client.State;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Client.Network.Handlers.Dungeon
{
    internal sealed class DungeonClearResultHandler : IClientPacketHandler
    {
        const int WireLength = 33; // v18: header, result, currency, three (item, count) pairs.

        public void Handle(UnityClientSession session, ArraySegment<byte> buffer)
        {
            if (buffer.Array == null || buffer.Count != WireLength) return;
            var wire = new ReadOnlySpan<byte>(buffer.Array, buffer.Offset, buffer.Count);
            if (BinaryPrimitives.ReadUInt16LittleEndian(wire) != WireLength ||
                BinaryPrimitives.ReadUInt16LittleEndian(wire.Slice(2)) != (ushort)PacketID.S_DungeonClearResult)
                return;

            var packet = new S_DungeonClearResult();
            packet.Read(buffer);
            var result = (InventoryResult)packet.result;
            if (result != InventoryResult.Success && result != InventoryResult.CurrencyCap &&
                result != InventoryResult.InventoryFull) return;
            if (packet.currency < 0 || packet.currency > InventoryLimits.MaxCurrency) return;

            var items = new[]
            {
                new InventorySlot((ItemId)packet.reward0ItemId, packet.reward0Count),
                new InventorySlot((ItemId)packet.reward1ItemId, packet.reward1Count),
                new InventorySlot((ItemId)packet.reward2ItemId, packet.reward2Count),
            };
            foreach (InventorySlot item in items)
            {
                if (!IsValidItem(item)) return;
            }

            // Capture before enqueue: ClientNet reuses the wire buffer, and a scene may have no window yet.
            int currency = packet.currency;
            session.EnqueueApply(() => DungeonClearRewardState.EnsureInstance().Apply(session, result, currency, items));
        }

        static bool IsValidItem(InventorySlot item) => item.ItemId == ItemId.None
            ? item.Count == 0
            : ItemCatalog.IsDefined(item.ItemId) && item.Count >= 1 && item.Count <= InventoryLimits.MaxStack;
    }
}

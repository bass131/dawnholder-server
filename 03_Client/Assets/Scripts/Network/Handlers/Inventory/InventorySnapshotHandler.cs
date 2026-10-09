using System;
using System.Buffers.Binary;
using Dawnholder.Client.State;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Client.Network.Handlers.Inventory
{
    internal sealed class InventorySnapshotHandler : IClientPacketHandler
    {
        const int WireLength = 76; // v17: header + revision + currency + eight (item, count) pairs.

        public void Handle(UnityClientSession session, ArraySegment<byte> buffer)
        {
            if (buffer.Array == null || buffer.Count != WireLength) return;
            var wire = new ReadOnlySpan<byte>(buffer.Array, buffer.Offset, buffer.Count);
            if (BinaryPrimitives.ReadUInt16LittleEndian(wire) != WireLength ||
                BinaryPrimitives.ReadUInt16LittleEndian(wire.Slice(2)) != (ushort)PacketID.S_InventorySnapshot)
                return;

            var packet = new S_InventorySnapshot();
            packet.Read(buffer);
            var slots = new[]
            {
                new InventorySlot((ItemId)packet.slot0ItemId, packet.slot0Count),
                new InventorySlot((ItemId)packet.slot1ItemId, packet.slot1Count),
                new InventorySlot((ItemId)packet.slot2ItemId, packet.slot2Count),
                new InventorySlot((ItemId)packet.slot3ItemId, packet.slot3Count),
                new InventorySlot((ItemId)packet.slot4ItemId, packet.slot4Count),
                new InventorySlot((ItemId)packet.slot5ItemId, packet.slot5Count),
                new InventorySlot((ItemId)packet.slot6ItemId, packet.slot6Count),
                new InventorySlot((ItemId)packet.slot7ItemId, packet.slot7Count),
            };
            if (!InventorySnapshotState.IsValidSnapshot(packet.currency, slots)) return;
            uint revision = packet.revision;
            int currency = packet.currency;

            // Decode before enqueue: ClientNet reuses its receive buffer before the main queue runs.
            session.EnqueueApply(() =>
            {
                var mirror = InventoryState.EnsureInstance();
                InventorySnapshotApplyResult applied = mirror.ApplySnapshot(revision, currency, slots);
                if (applied == InventorySnapshotApplyResult.Rejected) return;
                session.Inventory.ReceiveSnapshot(revision);
                if (applied == InventorySnapshotApplyResult.Applied) mirror.NotifyChanged();
            });
        }
    }
}

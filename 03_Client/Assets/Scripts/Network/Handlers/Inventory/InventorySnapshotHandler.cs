using System;
using System.Buffers.Binary;
using Dawnholder.Client.State;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Client.Network.Handlers.Inventory
{
    internal sealed class InventorySnapshotHandler : IClientPacketHandler
    {
        const int WireLength = 204; // v23: header + revision + currency + 24 (item, count) pairs.

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
                new InventorySlot((ItemId)packet.slot8ItemId, packet.slot8Count),
                new InventorySlot((ItemId)packet.slot9ItemId, packet.slot9Count),
                new InventorySlot((ItemId)packet.slot10ItemId, packet.slot10Count),
                new InventorySlot((ItemId)packet.slot11ItemId, packet.slot11Count),
                new InventorySlot((ItemId)packet.slot12ItemId, packet.slot12Count),
                new InventorySlot((ItemId)packet.slot13ItemId, packet.slot13Count),
                new InventorySlot((ItemId)packet.slot14ItemId, packet.slot14Count),
                new InventorySlot((ItemId)packet.slot15ItemId, packet.slot15Count),
                new InventorySlot((ItemId)packet.slot16ItemId, packet.slot16Count),
                new InventorySlot((ItemId)packet.slot17ItemId, packet.slot17Count),
                new InventorySlot((ItemId)packet.slot18ItemId, packet.slot18Count),
                new InventorySlot((ItemId)packet.slot19ItemId, packet.slot19Count),
                new InventorySlot((ItemId)packet.slot20ItemId, packet.slot20Count),
                new InventorySlot((ItemId)packet.slot21ItemId, packet.slot21Count),
                new InventorySlot((ItemId)packet.slot22ItemId, packet.slot22Count),
                new InventorySlot((ItemId)packet.slot23ItemId, packet.slot23Count),
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
                session.Guild.ReceiveInventoryRevision(revision);
                if (applied == InventorySnapshotApplyResult.Applied) mirror.NotifyChanged();
            });
        }
    }
}

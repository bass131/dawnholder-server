using System;
using System.Buffers.Binary;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Client.Network.Handlers.Inventory
{
    internal sealed class ItemUseResultHandler : IClientPacketHandler
    {
        const int WireLength = 13;

        public void Handle(UnityClientSession session, ArraySegment<byte> buffer)
        {
            if (buffer.Array == null || buffer.Count != WireLength) return;
            var wire = new ReadOnlySpan<byte>(buffer.Array, buffer.Offset, buffer.Count);
            if (BinaryPrimitives.ReadUInt16LittleEndian(wire) != WireLength ||
                BinaryPrimitives.ReadUInt16LittleEndian(wire.Slice(2)) != (ushort)PacketID.S_ItemUseResult)
                return;

            var packet = new S_ItemUseResult();
            packet.Read(buffer);
            var result = (InventoryResult)packet.result;
            var item = (ItemId)packet.itemId;
            if (!Enum.IsDefined(typeof(InventoryResult), result) || !ItemCatalog.IsDefined(item)) return;
            uint revision = packet.revision;
            // A result is a presentation signal, never a currency/stack update.
            session.EnqueueApply(() => session.Inventory.ReceiveUseResult(result, item, revision));
        }
    }
}

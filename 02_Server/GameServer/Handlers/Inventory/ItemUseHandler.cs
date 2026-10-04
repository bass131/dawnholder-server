using System.Buffers.Binary;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Handlers.Inventory;

internal sealed class ItemUseHandler : IPacketHandler
{
    public bool RequiresSelectedClass => true;

    public void Handle(GameSession session, ArraySegment<byte> buffer)
    {
        const int PacketLength = 12;
        if (buffer.Array == null || buffer.Count != PacketLength) return;
        ReadOnlySpan<byte> bytes = buffer.AsSpan();
        if (BinaryPrimitives.ReadUInt16LittleEndian(bytes) != PacketLength ||
            BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(2, 2)) != (ushort)PacketID.C_ItemUse)
        {
            return;
        }

        C_ItemUse packet = new();
        packet.Read(buffer);
        ItemId itemId = (ItemId)packet.itemId;
        if (packet.itemId <= 0 || !ItemCatalog.IsDefined(itemId)) return;
        session.SubmitItemUse(itemId, packet.expectedRevision);
    }
}

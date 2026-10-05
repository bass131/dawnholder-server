using System.Buffers.Binary;
using Dawnholder.Server.GameServer.Sessions;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Handlers.Inventory;

internal sealed class InventoryRequestHandler : IPacketHandler
{
    public bool RequiresSelectedClass => true;

    public void Handle(GameSession session, ArraySegment<byte> buffer)
    {
        // Generated Read assumes fixed fields exist. Reject both short and trailing input before
        // calling it; header agreement also protects direct receive/handler callers.
        const int PacketLength = 5;
        if (buffer.Array == null || buffer.Count != PacketLength) return;
        ReadOnlySpan<byte> bytes = buffer.AsSpan();
        if (BinaryPrimitives.ReadUInt16LittleEndian(bytes) != PacketLength ||
            BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(2, 2)) != (ushort)PacketID.C_InventoryRequest)
        {
            return;
        }

        C_InventoryRequest packet = new();
        packet.Read(buffer);
        if (packet.reserved != 0) return;
        session.SubmitInventoryRequest();
    }
}

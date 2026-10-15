using System.Buffers.Binary;
using Dawnholder.Server.GameServer.Sessions;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Handlers.Dungeon;

internal sealed class DungeonClearConfirmHandler : IPacketHandler
{
    public bool RequiresSelectedClass => true;

    public void Handle(GameSession session, ArraySegment<byte> buffer)
    {
        const int PacketLength = 5;
        if (buffer.Array == null || buffer.Count != PacketLength) return;
        ReadOnlySpan<byte> bytes = buffer.AsSpan();
        if (BinaryPrimitives.ReadUInt16LittleEndian(bytes) != PacketLength ||
            BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(2, 2)) != (ushort)PacketID.C_DungeonClearConfirm)
        {
            return;
        }

        C_DungeonClearConfirm packet = new();
        packet.Read(buffer);
        if (packet.reserved != 0) return;
        session.SubmitDungeonClearConfirm();
    }
}

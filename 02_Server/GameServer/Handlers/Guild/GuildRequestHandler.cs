using System.Buffers.Binary;
using Dawnholder.Server.GameServer.Sessions;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Handlers.Guild;

internal sealed class GuildRequestHandler : IPacketHandler
{
    public bool RequiresSelectedClass => true;

    public void Handle(GameSession session, ArraySegment<byte> buffer)
    {
        if (!GuildRequestFrame.Matches(buffer, PacketID.C_GuildRequest, 5) || buffer.AsSpan()[4] != 0) return;
        C_GuildRequest packet = new();
        packet.Read(buffer);
        session.SubmitGuildRequest();
    }
}

// All three requests use exact fixed frames. Read never sees a short, oversized, mismatched,
// or reserved-bearing frame; direct handler callers receive the same boundary checks.
internal static class GuildRequestFrame
{
    internal static bool Matches(ArraySegment<byte> buffer, PacketID id, int length)
        => buffer.Array != null && buffer.Count == length &&
            BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan()) == length &&
            BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan().Slice(2, 2)) == (ushort)id;
}

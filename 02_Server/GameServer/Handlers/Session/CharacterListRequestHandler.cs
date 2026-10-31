using Dawnholder.Server.GameServer.Sessions;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Handlers.Session;

internal sealed class CharacterListRequestHandler : IPacketHandler
{
    public bool RequiresSelectedClass => false;

    public void Handle(GameSession session, ArraySegment<byte> buffer)
    {
        if (!CharacterMenuRequestReader.TryRead(buffer, PacketID.C_CharacterListRequest, out uint requestId))
            return;
        session.SubmitCharacterMenuRequest(requestId, PacketID.C_CharacterListRequest);
    }
}

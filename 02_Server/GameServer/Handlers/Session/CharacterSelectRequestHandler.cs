using Dawnholder.Server.GameServer.Sessions;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Handlers.Session;

internal sealed class CharacterSelectRequestHandler : IPacketHandler
{
    public bool RequiresSelectedClass => false;

    public void Handle(GameSession session, ArraySegment<byte> buffer)
    {
        if (!CharacterMenuRequestReader.TryRead(buffer, PacketID.C_CharacterSelectRequest, out uint requestId))
            return;
        C_CharacterSelectRequest request = new();
        request.Read(buffer);
        session.SubmitCharacterMenuRequest(requestId, PacketID.C_CharacterSelectRequest,
            text: request.characterIdD);
    }
}

using System;
using Shared.Protocol;

namespace Dawnholder.Client.Network.Handlers.CharacterMenu
{
    internal sealed class CharacterListResultHandler : IClientPacketHandler
    {
        public void Handle(UnityClientSession session, ArraySegment<byte> buffer) =>
            session.ReceiveCharacterMenuResult(buffer, PacketID.S_CharacterListResult);
    }
}

using System;
using Dawnholder.Client.Network.CharacterLoad;

namespace Dawnholder.Client.Network.Handlers.Session
{
    internal sealed class CharacterLoadResultHandler : IClientPacketHandler
    {
        public void Handle(UnityClientSession session, ArraySegment<byte> buffer)
        {
            if (!CharacterLoadAuthority.TryDecode(buffer, out var status, out byte characterClass)) return;
            session.EnqueueApply(() => session.ApplyCharacterLoad(status, characterClass));
        }
    }
}

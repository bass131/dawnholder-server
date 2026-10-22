using System;

namespace Dawnholder.Client.Network.Handlers.Guild
{
    internal sealed class GuildActionResultHandler : IClientPacketHandler
    {
        public void Handle(UnityClientSession session, ArraySegment<byte> buffer)
        {
            if (!GuildPacketReader.TryReadResult(buffer, out var result)) return;
            session.EnqueueApply(() => session.Guild.ReceiveResult(result));
        }
    }
}

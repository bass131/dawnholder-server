using System;

namespace Dawnholder.Client.Network.Handlers.Guild
{
    internal sealed class GuildSnapshotHandler : IClientPacketHandler
    {
        public void Handle(UnityClientSession session, ArraySegment<byte> buffer)
        {
            if (!GuildPacketReader.TryReadSnapshot(buffer, out var snapshot)) return;
            session.EnqueueApply(() => session.Guild.ReceiveSnapshot(snapshot));
        }
    }
}

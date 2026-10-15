using Dawnholder.Server.GameServer.Sessions;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Handlers.Guild;

internal sealed class StrongholdUpgradeHandler : IPacketHandler
{
    public bool RequiresSelectedClass => true;

    public void Handle(GameSession session, ArraySegment<byte> buffer)
    {
        if (!GuildRequestFrame.Matches(buffer, PacketID.C_StrongholdUpgrade, 8)) return;
        C_StrongholdUpgrade packet = new();
        packet.Read(buffer);
        session.SubmitStrongholdUpgrade(packet.expectedGuildRevision);
    }
}

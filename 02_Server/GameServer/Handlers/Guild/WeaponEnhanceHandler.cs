using Dawnholder.Server.GameServer.Sessions;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Handlers.Guild;

internal sealed class WeaponEnhanceHandler : IPacketHandler
{
    public bool RequiresSelectedClass => true;

    public void Handle(GameSession session, ArraySegment<byte> buffer)
    {
        if (!GuildRequestFrame.Matches(buffer, PacketID.C_WeaponEnhance, 8)) return;
        C_WeaponEnhance packet = new();
        packet.Read(buffer);
        session.SubmitWeaponEnhance(packet.expectedInventoryRevision);
    }
}

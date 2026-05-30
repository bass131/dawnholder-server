using Dawnholder.Server.GameServer.Sessions;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Handlers;

// C_Ping 핸들러: decode → session.RespondPong(clientTimestampMs) — Send는 session 안.
internal sealed class PingHandler : IPacketHandler
{
    public void Handle(GameSession session, ArraySegment<byte> buffer)
    {
        C_Ping ping = new C_Ping();
        ping.Read(buffer);

        Console.WriteLine($"[GameSession] Ping received (clientTs={ping.clientTimestampMs}) → Pong");
        session.RespondPong(ping.clientTimestampMs);
    }
}

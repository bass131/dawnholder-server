using Dawnholder.Server.GameServer.Sessions;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Handlers;

// M3 Phase 03 (헌법 #4 봉합): GameSession에서 추출된 ping 핸들러.
//
// **이전 위치**: GameSession.HandlePing (inline).
// **변경**: decode → session.RespondPong(clientTimestampMs) — Send는 session 안.
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

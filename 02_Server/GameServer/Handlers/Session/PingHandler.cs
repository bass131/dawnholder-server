using Dawnholder.Server.GameServer.Handlers;
using Dawnholder.Server.GameServer.Sessions;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Handlers.Session;

// C_Ping 핸들러: decode → session.RespondPong(clientTimestampMs) — Send는 session 안.
internal sealed class PingHandler : IPacketHandler
{
    // ping은 class 선택과 무관(연결 keepalive) — 전제조건 게이트 미적용.
    public bool RequiresSelectedClass => false;

    public void Handle(GameSession session, ArraySegment<byte> buffer)
    {
        C_Ping ping = new C_Ping();
        ping.Read(buffer);

        Console.WriteLine($"[GameSession] Ping received (clientTs={ping.clientTimestampMs}) → Pong");
        session.RespondPong(ping.clientTimestampMs);
    }
}

using Dawnholder.Server.GameServer.Sessions;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Handlers;

// M3 Phase 03 (헌법 #4 봉합): GameSession에서 추출된 handshake 핸들러.
//
// **이전 위치**: GameSession.HandleHandshake (inline).
// **변경**: decode + version 검증 + (OK이면 session.CompleteHandshakeAndEnter()
//   / mismatch면 session.RejectHandshake(reason)) — lifecycle은 session 안.
//
// **first-packet 게이트는 GameSession.OnRecvPacket 책임** (외부 핸들러가
// lifecycle gating 권한 X). 본 핸들러는 *게이트 통과 후*의 decode + 검증만.
internal sealed class HandshakeHandler : IPacketHandler
{
    public void Handle(GameSession session, ArraySegment<byte> buffer)
    {
        C_Handshake pkt = new C_Handshake();
        pkt.Read(buffer);

        if (pkt.clientVersion != ProtocolVersion.Current)
        {
            string reason =
                $"ProtocolVersion mismatch (client={pkt.clientVersion}, server={ProtocolVersion.Current})";
            session.RejectHandshake(reason);
            return;
        }

        Console.WriteLine($"[GameSession] Handshake OK (version={pkt.clientVersion})");
        session.CompleteHandshakeAndEnter();
    }
}

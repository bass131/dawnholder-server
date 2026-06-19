using Dawnholder.Server.GameServer.Handlers;
using Dawnholder.Server.GameServer.Sessions;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Handlers.Session;

// C_Handshake 핸들러: decode + version 검증 → lifecycle은 session 안.
//
// **first-packet 게이트는 GameSession.OnRecvPacket 책임** (외부 핸들러가
// lifecycle gating 권한 X). 본 핸들러는 *게이트 통과 후*의 decode + 검증만.
internal sealed class HandshakeHandler : IPacketHandler
{
    // handshake는 class 선택 전 단계 자체 — 전제조건 게이트 미적용.
    public bool RequiresSelectedClass => false;

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

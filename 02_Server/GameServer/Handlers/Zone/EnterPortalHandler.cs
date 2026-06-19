using Dawnholder.Server.GameServer.Handlers;
using Dawnholder.Server.GameServer.Sessions;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Handlers.Zone;

// C_EnterPortal 핸들러: handler = decode + 선결 검증 + session 캡슐화 메서드 호출만.
//   portal 근접 검증 / migration 로직 / tick 마샬링은 session.SubmitEnterPortal 안에서.
//
// **헌법 #1 (Server Authority)**: 패킷 필드는 portalId 하나뿐. 목적지 맵 / spawn 좌표는
//   서버가 PortalTable에서 결정 — 클라가 목적지를 지정할 수 없음.
//
// **헌법 #3 (Trust Boundary)**: portalId 범위 검증은 session.SubmitEnterPortal에서
//   tick thread 안에 박혀있음. invalid portalId → silent drop.
internal sealed class EnterPortalHandler : IPacketHandler
{
    // 포털 진입 — class 선택 전엔 stats null → AddPlayerWithId 불가. dispatch 일괄 게이트가 silent drop.
    public bool RequiresSelectedClass => true;

    public void Handle(GameSession session, ArraySegment<byte> buffer)
    {
        C_EnterPortal pkt = new C_EnterPortal();
        pkt.Read(buffer);

        session.SubmitEnterPortal(pkt.portalId);
    }
}

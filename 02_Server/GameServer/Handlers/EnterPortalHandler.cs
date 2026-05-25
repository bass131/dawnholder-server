using Dawnholder.Server.GameServer.Sessions;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Handlers;

// M4.2 Phase 03: C_EnterPortal 핸들러.
//
// **책임 분리 (Phase 03 박힌 패턴 — AttackHandler / MoveIntentHandler 정합)**:
//   handler = decode + 선결 검증(handshake/class선택 완료 확인) + session 캡슐화 메서드 호출만.
//   portal 근접 검증 / migration 로직 / tick 마샬링은 session.SubmitEnterPortal 안에서.
//
// **헌법 #1 (Server Authority)**: 패킷 필드는 portalId 하나뿐. 목적지 맵 / spawn 좌표는
//   서버가 PortalTable에서 결정 — 클라가 목적지를 지정할 수 없음.
//
// **헌법 #3 (Trust Boundary)**: portalId 범위 검증은 session.SubmitEnterPortal에서
//   tick thread 안에 박혀있음 (현재 맵의 portal 목록과 대조).
//   invalid portalId → silent drop (disconnect X — 클라 버그 또는 network glitch 가능).
//
// **헌법 #5 (틱 블로킹 금지)**: 동기 코드만, await/Task.Delay/Thread.Sleep 금지.
//   tick 마샬링은 session.SubmitEnterPortal 내부 EnqueueJob으로.
internal sealed class EnterPortalHandler : IPacketHandler
{
    public void Handle(GameSession session, ArraySegment<byte> buffer)
    {
        // M4.1 Phase 02 (P0-2 패턴): 캐릭터 선택 전 portal 시도 = silent drop.
        // 클래스 선택 없이는 stats가 null → AddPlayerWithId 불가.
        // AttackHandler/MoveIntentHandler 동일 패턴 — 선결 조건 불충족 시 trust boundary 위반.
        if (!session.HasSelectedClass)
        {
            Console.WriteLine(
                $"[Trust] C_EnterPortal before CharacterSelect — silent drop (cheat-flag candidate)");
            return;
        }

        C_EnterPortal pkt = new C_EnterPortal();
        pkt.Read(buffer);

        // session 캡슐화 메서드 위임 — portal 근접 검증 + migration 로직은 session 안.
        session.SubmitEnterPortal(pkt.portalId);
    }
}

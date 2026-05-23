using Dawnholder.Server.GameServer.Sessions;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Handlers;

// M3 Phase 06 Step 5 (응급 전투 인프라): C_Attack 핸들러.
//
// **이전 위치**: 없음 (신설). Phase 03에서 박힌 핸들러 패턴 정합 —
//   HandshakeHandler / MoveIntentHandler / PingHandler 와 시그니처 통일.
//
// **책임 분리 (Phase 03 박힌 패턴)**: handler = decode + session 캡슐화 메서드 호출만.
//   mutation / 검증 / tick 마샬링은 session.SubmitAttack 안 + GameMap.ProcessAttack 안에서.
//
// **헌법 #1 (Server Authority)**: 패킷 필드는 `targetEntityId` 하나뿐. attacker는
//   `session._entityId`에서 강제 (헌법 #3 정합) — 클라가 다른 entityId로 도용 차단.
//   Codex β 사전 검증 HIGH #2 봉합 (direction/facing/ray 모델은 응급 범위 초과 폐기).
//
// **헌법 #5 (틱 블로킹 금지)**: 본 핸들러는 동기 코드만, await/Task.Delay/Thread.Sleep 금지.
//   handshake 미완 게이트는 `GameSession.OnRecvPacket`이 책임 (first-packet 강제 정합).
internal sealed class AttackHandler : IPacketHandler
{
    public void Handle(GameSession session, ArraySegment<byte> buffer)
    {
        // M4.1 Phase 02 (P0-2 봉합 — 헌법 #3 trust boundary 강화):
        // class 선택 전 C_Attack 수신 시 silent drop + [Trust] 경고 로그.
        // MoveIntentHandler 정합 패턴 — 캐릭터 선택 전 전투 입력은 신뢰 경계 위반.
        // M4.2에서 cheat-flag 카운터 박을 예정.
        if (!session.HasSelectedClass)
        {
            Console.WriteLine(
                $"[Trust] C_Attack before CharacterSelect — silent drop (cheat-flag candidate)");
            return;
        }

        C_Attack pkt = new C_Attack();
        pkt.Read(buffer);

        session.SubmitAttack(pkt.targetEntityId);
    }
}

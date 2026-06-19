using Dawnholder.Server.GameServer.Sessions;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Handlers;

// C_Attack 핸들러: handler = decode + session 캡슐화 메서드 호출만.
//   mutation / 검증 / tick 마샬링은 session.SubmitAttack 안 + GameMap.ProcessAttack 안에서.
//
// **헌법 #1 + #3 (도용 방지)**: 패킷 필드는 `targetEntityId` 하나뿐. attacker는
//   `session._entityId`에서 강제 — 클라가 다른 entityId로 도용 차단. attacker 필드를
//   패킷에 추가하면 이 방어가 무너짐.
internal sealed class AttackHandler : IPacketHandler
{
    // 전투 입력 — class 선택 전 = 신뢰 경계 위반. dispatch 일괄 게이트가 silent drop.
    public bool RequiresSelectedClass => true;

    public void Handle(GameSession session, ArraySegment<byte> buffer)
    {
        C_Attack pkt = new C_Attack();
        pkt.Read(buffer);

        // attackerClientTick = 클라가 공격 버튼 눌렀을 당시의 lastReceivedServerTick (lag compensation rewind용).
        // 헌법 #3 (Trust Boundary): 값 자체는 untrusted — ProcessAttack에서 범위 검증.
        session.SubmitAttack(pkt.targetEntityId, pkt.attackerClientTick);
    }
}

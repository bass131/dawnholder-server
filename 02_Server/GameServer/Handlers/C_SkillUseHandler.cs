using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Handlers;

// C_SkillUse 핸들러: decode + skillId 범위 검증 + session 캡슐화 메서드 호출만.
//   mutation / 쿨다운 / 박스 판정은 session.SubmitSkillUse → GameMap.ProcessSkill 안에서.
//
// **헌법 #3 (Trust Boundary)**:
//   1. class 선택 전(HasSelectedClass) = silent drop(cheat 후보 로그).
//   2. skillId 범위 검증: SkillId.None(0) 또는 미정의 = silent drop(cheat 후보).
//      현재 서버가 아는 유효 스킬은 Thunderbolt(1)뿐 — 확장 시 이 목록에 추가.
//   3. caster entityId는 session._entityId에서 강제 — 클라가 다른 entityId 도용 차단(AttackHandler 동형).
internal sealed class C_SkillUseHandler : IPacketHandler
{
    public void Handle(GameSession session, ArraySegment<byte> buffer)
    {
        // 헌법 #3: class 선택 전 스킬 입력은 신뢰 경계 위반.
        if (!session.HasSelectedClass)
        {
            Console.WriteLine(
                "[Trust] C_SkillUse before CharacterSelect — silent drop (cheat-flag candidate)");
            return;
        }

        C_SkillUse pkt = new C_SkillUse();
        pkt.Read(buffer);

        // skillId 범위 검증 — None(0) 또는 알 수 없는 값은 cheat 후보.
        if (pkt.skillId != (byte)SkillId.Thunderbolt)
        {
            Console.WriteLine(
                $"[Trust] C_SkillUse unknown skillId={pkt.skillId} — silent drop (cheat-flag candidate)");
            return;
        }

        // attackerClientTick은 untrusted — ProcessSkill에서 rewind 범위 검증.
        session.SubmitSkillUse(pkt.skillId, pkt.attackerClientTick);
    }
}

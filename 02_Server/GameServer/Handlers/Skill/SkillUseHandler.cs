using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Handlers;

// C_SkillUse 핸들러: decode + 신뢰 경계 검증 + session 캡슐화 메서드 호출만.
//   mutation / 쿨다운 / 박스 판정은 session.SubmitSkillUse → GameMap.ProcessSkill 안에서.
//
// **헌법 #3 (Trust Boundary) — 3단계 검증**:
//   1. class 선택 전(HasSelectedClass) = silent drop + cheat-flag 로그.
//   2. skillId 범위 검증: None(0) 또는 카탈로그에 없는 값 = silent drop + cheat-flag 로그.
//   3. 캐스터 클래스 검증: SkillCatalog.CanCast(caster.Class, skillId) false = silent drop + cheat-flag 로그.
//      caster 클래스는 session.GetCasterClass()에서 강제 — 클라가 보낸 값 절대 신뢰 X.
internal sealed class SkillUseHandler : IPacketHandler
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

        // skillId 범위 검증 — None(0) 또는 카탈로그 미등록 값은 cheat 후보.
        // SkillCatalog.GetRequiredClass는 미등록 skillId에 null 반환.
        if (SkillCatalog.GetRequiredClass((SkillId)pkt.skillId) == null)
        {
            Console.WriteLine(
                $"[Trust] C_SkillUse unknown skillId={pkt.skillId} — silent drop (cheat-flag candidate)");
            return;
        }

        // 클래스 게이트 (헌법 §3 신뢰 경계 핵심):
        //   caster 클래스는 서버 측 session에서 가져옴 — 클라가 보낸 값 신뢰 X.
        //   Knight가 Thunderbolt, Mage가 Dash 등 클래스 불일치 시전은 치터 의심.
        CharacterClass casterClass = session.GetCasterClass();
        if (!SkillCatalog.CanCast(casterClass, (SkillId)pkt.skillId))
        {
            Console.WriteLine(
                $"[Trust] C_SkillUse class mismatch: {casterClass} cannot cast skillId={pkt.skillId} — silent drop (cheat-flag candidate)");
            return;
        }

        // attackerClientTick은 untrusted — ProcessSkill에서 rewind 범위 검증.
        // facing(M4.13 v13): 클라 화면 방향 — 대쉬 방향 권위. 헌법 #3 정규화 (1=right, 그 외=left=-1).
        //   cheat 무해(거리 고정·벽통과 불가)지만 trust-boundary 규율상 부호 정규화.
        sbyte facing = pkt.facing == 1 ? (sbyte)1 : (sbyte)-1;

        // verticalDir(M4.15 v14): 텔레포트 4방향 수직 의도 — 0=수평/1=위/2=아래 (PDL 단일 진실).
        //   헌법 #3 whitelist 정규화: 허용 집합 {0,1,2}만 통과, 그 외(3·99·255 등 cheat)는 안전 기본 0(수평).
        //   **3진 정의역** — facing의 2진 패턴(==1?1:-1) 모방 금지: "1 아니면 0"으로 짜면 2(아래)가 0으로
        //   뭉개져 아래 텔레포트가 죽음. whitelist 술어(is 1 or 2) 필수.
        byte verticalDir = pkt.verticalDir is 1 or 2 ? pkt.verticalDir : (byte)0;
        session.SubmitSkillUse(pkt.skillId, pkt.attackerClientTick, facing, verticalDir);
    }
}

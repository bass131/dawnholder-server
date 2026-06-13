namespace Shared.GameData;

// 스킬 식별자. C_SkillUse.skillId로 wire에 byte 직렬화.
//
// **서버 권위 (헌법 #1/#3)**: 클라는 "이 스킬 쓰겠다" 의도만 전송.
//   서버가 skillId 범위 검증(None/미정의는 silent drop=cheat 후보) + 쿨다운 판정 + 타격 대상 확정.
//   평타(C_Attack)와 별개 채널 — 평타↔스킬 분리.
//
// **stability 약속 (AnimState/EnemyKind 패턴 정합)**: 값 영원히 고정, 새 스킬은 append-only.
//   값 변경 = breaking change = Protocol.Version bump 의무.
public enum SkillId : byte
{
    None        = 0,  // 미지정/예약 — 유효 스킬 아님 (서버 silent drop)
    Thunderbolt = 1,  // Mage 광역 — 공격자 중심 X,Y 박스 내 적 각자 위치에 낙뢰
    Dash        = 2,  // Knight 전진 대시 — 짧은 거리 고속 이동
    Teleport    = 3,  // Mage 순간이동 — 커서 방향 일정 거리 텔레포트
}

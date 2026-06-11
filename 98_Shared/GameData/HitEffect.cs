namespace Shared.GameData;

// 피격 연출 종류. S_HitResult.hitEffect로 wire에 byte 직렬화.
//
// **stability 약속 (SkillId/AnimState 패턴 정합)**: 값 영원히 고정, 새 효과는 append-only.
//   값 변경 = breaking change = Protocol.Version bump 의무.
public enum HitEffect : byte
{
    Melee      = 0,  // Knight 근접 평타 (VFX 없음 — 클라가 0은 검사 안 함)
    Projectile = 1,  // Mage 투사체 도착
    Lightning  = 2,  // Mage Thunderbolt AoE 낙뢰
    Dash       = 3,  // Knight Dash 스킬 충돌
}

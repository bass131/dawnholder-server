using Shared.Protocol;

namespace Shared.GameData;

/// <summary>
/// SkillId → 요구 CharacterClass 매핑 단일 진실.
///
/// **헌법 §3 (Trust Boundary) 실현**: 서버는 반드시 이 카탈로그로 캐스터 클래스를
/// 검증한다 — caster 클래스는 서버 측 PlayerEntity에서 가져오며, 클라가 보낸 값을
/// 신뢰하지 않는다.
///
/// **헌법 §4 (Shared Code Discipline)**: 클라 입력 게이트와 서버 거부 로직이
/// 동일 매핑을 참조 → 불일치(클라는 보냈는데 서버가 버리는 유령 입력) 원천 차단.
///
/// **append-only 설계**: 새 스킬은 switch 케이스를 맨 끝에 추가만 한다.
/// 기존 케이스 값 변경 = breaking (서버/클라 양쪽 동시 배포 필요).
/// </summary>
public static class SkillCatalog
{
    /// <summary>
    /// 지정 클래스가 해당 스킬을 시전할 수 있는지 반환한다.
    /// <para>None(0) 또는 미정의 skillId는 항상 false — 서버 silent drop 대상.</para>
    /// </summary>
    public static bool CanCast(CharacterClass cls, SkillId skillId)
        => GetRequiredClass(skillId) == cls;

    /// <summary>
    /// 스킬을 시전할 수 있는 클래스를 반환한다.
    /// 매핑이 없는 skillId(None 포함)는 null 반환 — 항상 거부.
    /// </summary>
    public static CharacterClass? GetRequiredClass(SkillId skillId)
        => skillId switch
        {
            SkillId.Thunderbolt => CharacterClass.Mage,
            SkillId.Dash        => CharacterClass.Knight,
            SkillId.Teleport    => CharacterClass.Mage,
            _                   => null,   // None=0 + 미정의 전부
        };
}

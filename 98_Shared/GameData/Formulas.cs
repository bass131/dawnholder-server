using System;

namespace Shared.GameData;

/// <summary>
/// 서버 권위 전투 공식 모음 (M4.1 Phase 05 신설).
///
/// **헌법 #1 (Server Authority)**:
/// 본 클래스의 공식은 <em>서버가 권위 판정에 사용</em>하는 유일한 출처.
/// 클라이언트는 타격 숫자 표시 등 hint 용도로만 호출 가능 — 절대 <c>Hp -=</c> 패턴 사용 X.
/// 클라 코드에서 <c>Hp -=</c> / <c>entity.Hp -= ComputeDamage(...)</c> 패턴 발견 시
/// 헌법 #1 위반 = reviewer 차단 근거.
///
/// **헌법 #4 (Shared Code Discipline)**:
/// 98_Shared에 정의되어 서버/클라 양쪽이 *동일 어셈블리*를 참조.
/// 복사-붙여넣기 금지 — 공식이 달라지면 prediction mispredict 누적.
///
/// **Deterministic 보장**:
/// 모든 메서드는 순수 함수 (pure function) — 같은 input → 항상 같은 output.
/// <c>DateTime.Now</c> / seed 없는 <c>Random</c> 금지. Float 연산은 IEEE 754 명시 패턴만.
/// </summary>
public static class Formulas
{
    /// <summary>
    /// 공격자 스탯과 대상 방어력을 반영한 최종 데미지 계산.
    ///
    /// <para>공식: <c>Max(1, baseDamage + attacker.Attack - target.Defense)</c></para>
    /// <list type="bullet">
    ///   <item>최소 1 데미지 보장 — 방어력이 공격력+기본 데미지를 초과해도 0 이하 불가.</item>
    ///   <item><c>Math.Max</c> 분기로 int overflow 안전 (baseDamage + Attack이 int.MaxValue에 근접해도 Max가 잡음).</item>
    ///   <item>결과는 <c>int</c> — <c>S_HitResult.damage</c> 필드 타입과 직접 호환 (PDL 정합, Protocol.Version bump X).</item>
    /// </list>
    ///
    /// <para><strong>서버 사용 예</strong>: <c>int dmg = Formulas.ComputeDamage(attacker.Stats, enemyStats, CombatConstants.BaseDamage);</c></para>
    /// <para><strong>클라 hint 사용 예</strong>: UI 데미지 숫자 미리 표시용. 서버 확인 전 임시값.</para>
    /// </summary>
    /// <param name="attacker">공격자 플레이어 스탯 (Attack 필드 사용).</param>
    /// <param name="target">대상 스탯 (Defense 필드 사용).</param>
    /// <param name="baseDamage">기본 공격력 (CombatConstants.BaseDamage 등 서버 상수).</param>
    /// <returns>최소 1 이상의 최종 데미지 값.</returns>
    public static int ComputeDamage(PlayerStats attacker, EnemyStats target, int baseDamage)
        => Math.Max(1, baseDamage + attacker.Attack - target.Defense);
}

/// <summary>
/// 적 엔티티 스탯 컨테이너 (M4.1 Phase 05 신설).
///
/// <para>value type <c>struct</c> 선택 이유: 단순 스탯 홀더라 힙 할당 불필요.
/// default Defense=0은 struct 기본값으로 자연 충족 — 무방어 적 표현에 명시 초기화 불필요.</para>
///
/// <para>Worker B가 <c>EnemyEntity</c> 통합 시 본 struct를 wrapping 또는 직접 사용.</para>
/// </summary>
public struct EnemyStats
{
    /// <summary>방어력. default=0 (무방어). 공격자 Attack에서 차감.</summary>
    public int Defense;

    /// <summary>최대 HP. 스폰 시 CurrentHp 초기화 기준.</summary>
    public int MaxHp;
}

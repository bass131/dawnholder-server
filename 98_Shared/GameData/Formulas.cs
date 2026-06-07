using System;

namespace Shared.GameData;

/// <summary>
/// 서버 권위 전투 공식 모음.
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
    ///   <item>주의: 중간 <c>int</c> 덧셈은 이론상 wrap 가능하나 baseDamage는 작은 서버
    ///       상수라 현실 입력에선 무발생. wrap돼도 <c>Math.Max</c>가 ≥1로 잡아 *음수는 절대 반환 안 함*(거대 입력 시
    ///       magnitude만 부정확 — 비현실적). 콘텐츠 스탯이 int 한계에 근접하는 M5+엔 long 격상 재검토.</item>
    ///   <item>결과는 <c>int</c> — <c>S_HitResult.damage</c> 필드 타입과 직접 호환 (PDL 정합).</item>
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

    /// <summary>
    /// 적→플레이어 데미지 계산 (보스 공격 판정용 오버로드).
    ///
    /// <para>공식: <c>Max(1, baseDamage + attacker.Attack - target.Defense)</c></para>
    /// <para>기존 PlayerStats→EnemyStats 오버로드와 공식 동일 — 양방향 전투 대칭.</para>
    ///
    /// <para><strong>헌법 #1</strong>: 서버만 호출. 클라는 HP 감소에 절대 사용 X.</para>
    /// </summary>
    /// <param name="attacker">공격자 적 스탯 (Attack 필드 사용).</param>
    /// <param name="target">대상 플레이어 스탯 (Defense 필드 사용).</param>
    /// <param name="baseDamage">기본 공격력 (CombatConstants 보스 상수).</param>
    public static int ComputeDamage(EnemyStats attacker, PlayerStats target, int baseDamage)
        => Math.Max(1, baseDamage + attacker.Attack - target.Defense);
}

/// <summary>
/// 적 엔티티 스탯 컨테이너.
///
/// <para>value type <c>struct</c> 선택 이유: 단순 스탯 홀더라 힙 할당 불필요.
/// default Defense=0은 struct 기본값으로 자연 충족 — 무방어 적 표현에 명시 초기화 불필요.</para>
///
/// <para>AI 이동 파라미터 3종 (MoveSpeed/AggroRange/PatrolRange):
/// 서버 FSM이 이 값을 읽어 patrol/chase 반경·속도를 결정. 클라는 읽기 전용 hint 용도.</para>
/// </summary>
public struct EnemyStats
{
    /// <summary>방어력. default=0 (무방어). 공격자 Attack에서 차감.</summary>
    public int Defense;

    /// <summary>최대 HP. 스폰 시 CurrentHp 초기화 기준.</summary>
    public int MaxHp;

    /// <summary>
    /// 공격력. default=0 (무공격 — 기존 Normal/Golem/default 회귀 0).
    /// 보스 공격 판정: ComputeDamage(EnemyStats, PlayerStats, baseDamage)에서 사용.
    /// </summary>
    public int Attack;

    // ── AI 이동 파라미터 ──────────────────────────────────────

    /// <summary>
    /// 적 이동 속도 (유닛/초). Normal enemy 기본값 ~2.0.
    ///
    /// <para>⚠️ target rewind 미적용 상태(M4.4 이월)라 적이 빠르면 클라 보간 지연과
    /// 서버 판정 위치가 어긋나 조준-판정 빗맞음 발생 → Normal enemy는 플레이어보다
    /// 느리게 설정(Warrior=4 / Ranger=6 대비 ~2.0). 빠른 적은 rewind 구현 후 조정.</para>
    /// </summary>
    public float MoveSpeed;

    /// <summary>
    /// 적이 플레이어를 감지해 추격을 시작하는 반경 (유닛). Normal enemy 기본값 ~6.0.
    /// 이 반경 안에 플레이어가 들어오면 FSM: Idle/Patrol → Chase 전환.
    /// </summary>
    public float AggroRange;

    /// <summary>
    /// 적이 순찰하는 반경 (유닛). Normal enemy 기본값 ~4.0.
    /// 스폰 좌표 기준 ±PatrolRange 범위를 왕복. AggroRange보다 작게 유지.
    /// </summary>
    public float PatrolRange;

    // ── Normal enemy 기본값 factory ──────────────────────────────────────────

    /// <summary>
    /// Normal enemy 기본 스탯.
    /// 서버가 몬스터 테이블 데이터로 교체 전까지의 합리적 초기값.
    /// </summary>
    public static EnemyStats NormalDefault() => new EnemyStats
    {
        Defense = 0,
        MaxHp = 30,
        Attack = 5,
        MoveSpeed = 2.0f,
        AggroRange = 6.0f,
        PatrolRange = 4.0f,
    };

    // ── Golem enemy 기본값 factory ────────────────────────────────────────────

    /// <summary>
    /// Golem enemy 기본 스탯. 느리고 단단한 탱커 컨셉.
    ///
    /// <list type="bullet">
    ///   <item>MaxHp=60: Normal(30)의 2배 — Warrior 3타/Ranger 4타 분량.</item>
    ///   <item>Defense=5: Warrior 데미지 25→20, Ranger 22→17 (ComputeDamage Max(1, base10+Attack-Def)).</item>
    ///   <item>MoveSpeed=1.2f: Normal(2.0)보다 느림 — 스펙 "MoveSpeed &lt; 2.0".</item>
    ///   <item>AggroRange=4.0f: Normal(6.0)보다 좁음 — 시야가 짧은 둔한 골렘.</item>
    ///   <item>PatrolRange=2.5f: AggroRange(4.0)보다 작게 유지 (invariant: PatrolRange &lt; AggroRange).</item>
    /// </list>
    /// </summary>
    public static EnemyStats GolemDefault() => new EnemyStats
    {
        Defense = 5,
        MaxHp = 60,
        Attack = 8,
        MoveSpeed = 1.2f,
        AggroRange = 4.0f,
        PatrolRange = 2.5f,
    };

    // ── Boss 기본값 factory ────────────────────────────────────────────────────

    /// <summary>
    /// Boss 기본 스탯.
    ///
    /// <list type="bullet">
    ///   <item>MaxHp=100: EnemyDefaultHp.ByKind[Boss]=100과 일치 의무.</item>
    ///   <item>Attack=12: 페이즈1 CombatConstants.BossBaseDamage(8) + Attack(12) - 플레이어 Defense.
    ///       Warrior Defense=5 기준 데미지 = Max(1, 8+12-5) = 15.</item>
    ///   <item>Defense=3: 플레이어→보스 방향은 CombatSystem이 처리 (PlayerStats→EnemyStats 오버로드).</item>
    ///   <item>MoveSpeed/AggroRange/PatrolRange=0: 보스는 이동 없는 고정형 (BossBehaviorSystem 전담).</item>
    /// </list>
    /// </summary>
    public static EnemyStats BossDefault() => new EnemyStats
    {
        Defense = 3,
        MaxHp = 100,
        Attack = 12,
        MoveSpeed = 0f,
        AggroRange = 0f,
        PatrolRange = 0f,
    };
}

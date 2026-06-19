using Shared.GameData;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Maps.Systems;
using Dawnholder.Server.GameServer.Maps.States;

namespace Dawnholder.Server.GameServer.Combat;

/// <summary>
/// EnemyKind별 정적 데이터 테이블 — "새 적 = 데이터 1행".
///
/// <para>
/// <strong>배치 사유(02_Server/Combat/ — 서버 전용)</strong>:
/// <list type="bullet">
///   <item><description>
///     <c>InitialFsmState</c>가 <c>ActorState&lt;EnemyEntity&gt;</c>(서버 FSM 타입)를 보유.
///   </description></item>
///   <item><description>
///     <c>InitialAttackCooldownTicks</c> / <c>RespawnTicks</c>가 <c>CombatConstants</c>(서버 전용)을 참조.
///   </description></item>
///   <item><description>
///     클라가 볼 필요 없는 서버 판정 데이터만 포함 (least-exposure 원칙).
///   </description></item>
/// </list>
/// </para>
///
/// <para>
/// <strong>behavior-invariant 보장</strong>:
/// 각 필드 값은 기존 switch/if 분기가 반환하던 값을 그대로 옮김.
/// GameMap·EnemyEntity·EnemyStates·RespawnSystem이 참조하는 모든 수치가
/// 이 테이블과 동일 — 게임 동작 0 변경.
/// </para>
///
/// <para>
/// <strong>새 적 추가 방법</strong>:
/// 1. <c>EnemyKind</c> enum에 값 append (98_Shared/GameData/Enums/EnemyKind.cs).
/// 2. <c>EnemyStats</c>에 factory 메서드 추가 (98_Shared/GameData/Combat/Formulas.cs).
/// 3. 이 파일 <c>Catalog</c> 배열에 <c>EnemyEntry</c> 1행 추가.
/// 그 외 파일 수정 없음.
/// </para>
/// </summary>
internal static class EnemyCatalog
{
    /// <summary>
    /// EnemyKind별 정적 데이터 1행.
    /// 값은 기존 switch 분기와 1:1 동치 — 테스트 <c>EnemyCatalogValueTests</c>가 동치 검증.
    /// </summary>
    internal sealed record EnemyEntry(
        /// <summary>이 항목의 종류 ID. 조회 키.</summary>
        EnemyKind Kind,

        /// <summary>
        /// 스폰 시 최대 HP. EnemyStats.MaxHp와 동일.
        /// GameMap ctor maxHp switch 대체.
        /// </summary>
        int MaxHp,

        /// <summary>
        /// 서버 권위 스탯 전체(Defense / Attack / MoveSpeed / AggroRange / PatrolRange / AggroOnSight).
        /// GameMap.SpawnEnemy stats switch 대체.
        /// </summary>
        EnemyStats Stats,

        /// <summary>
        /// 스폰 시 EnemyEntity.State 초기값.
        /// EnemyEntity ctor State 분기 대체.
        /// Boss = Idle(0), Normal/Golem = Patrol(1).
        /// </summary>
        EnemyState InitialState,

        /// <summary>
        /// 스폰 시 EnemyEntity.AttackCooldownTicks 초기값.
        /// EnemyEntity ctor AttackCooldownTicks 분기 대체.
        /// Boss = BossPhase1CooldownTicks(40), Normal/Golem = NormalAttackCooldownTicks(30).
        /// </summary>
        int InitialAttackCooldownTicks,

        /// <summary>
        /// FSM 초기 State 인스턴스(Flyweight).
        /// GameMap.SpawnEnemy Fsm 생성 분기 대체.
        /// Boss = BossStates.Idle, Normal/Golem = EnemyStates.Patrol.
        /// </summary>
        ActorState<EnemyEntity> InitialFsmState,

        /// <summary>
        /// true = 보스 종류. 보스는 사망 시 StageClear + 재출현 없음.
        /// 제어 흐름이 다른 Boss 분기들의 boolean 플래그 단일 출처.
        /// </summary>
        bool IsBoss,

        /// <summary>
        /// 사망 후 재출현 대기 틱. Boss = 0 (재출현 없음).
        /// RespawnSystem.Enqueue kind 분기 대체.
        /// Normal = 100틱(5초), Golem = 120틱(6초).
        /// </summary>
        int RespawnTicks,

        /// <summary>
        /// 공격 windup(휘두르기 준비) 틱 수.
        /// EnemyAttackState.Enter kind 분기 대체.
        /// Normal = 0(즉시 타격), Golem = 6틱(300ms).
        /// Boss는 EnemyAttackState를 사용하지 않으므로 의미 없음(0).
        /// </summary>
        int AttackWindupTicks,

        /// <summary>
        /// 공격 패턴 byte. S_EnemyAttack.attackPattern 필드에 사용.
        /// Normal = 0, Golem = 1. Boss는 BossStates가 직접 처리(의미 없음, 0).
        /// EnemyAttackState.ApplyAttack kind 분기 대체.
        /// </summary>
        byte AttackPattern
    );

    /// <summary>
    /// 종류별 카탈로그 배열. 인덱스 = (byte)EnemyKind.
    /// EnemyKind의 값은 0=Normal, 1=Boss, 2=Golem (stability 약속 — enum.cs 주석 참조).
    /// </summary>
    static readonly EnemyEntry[] Catalog =
    {
        // Normal (index 0) — 슬라임 계열, 후공, 5초 재출현
        new(
            Kind:                        EnemyKind.Normal,
            MaxHp:                       EnemyStats.NormalDefault().MaxHp,       // 30
            Stats:                       EnemyStats.NormalDefault(),
            InitialState:                EnemyState.Patrol,
            InitialAttackCooldownTicks:  CombatConstants.NormalAttackCooldownTicks, // 30
            InitialFsmState:             EnemyStates.Patrol,
            IsBoss:                      false,
            RespawnTicks:                RespawnSystem.NormalEnemyRespawnTicks,    // 100
            AttackWindupTicks:           CombatConstants.NormalAttackWindupTicks,  // 0
            AttackPattern:               0
        ),

        // Boss (index 1) — 보스, 선공, 재출현 없음, StageClear
        new(
            Kind:                        EnemyKind.Boss,
            MaxHp:                       EnemyStats.BossDefault().MaxHp,          // 150
            Stats:                       EnemyStats.BossDefault(),
            InitialState:                EnemyState.Idle,
            InitialAttackCooldownTicks:  CombatConstants.BossPhase1CooldownTicks, // 40
            InitialFsmState:             BossStates.Idle,
            IsBoss:                      true,
            RespawnTicks:                0,
            AttackWindupTicks:           0,  // Boss는 EnemyAttackState 미사용
            AttackPattern:               0   // Boss는 BossStates가 직접 처리
        ),

        // Golem (index 2) — 골렘, 선공, 6초 재출현, windup=6틱
        new(
            Kind:                        EnemyKind.Golem,
            MaxHp:                       EnemyStats.GolemDefault().MaxHp,          // 60
            Stats:                       EnemyStats.GolemDefault(),
            InitialState:                EnemyState.Patrol,
            InitialAttackCooldownTicks:  CombatConstants.NormalAttackCooldownTicks, // 30
            InitialFsmState:             EnemyStates.Patrol,
            IsBoss:                      false,
            RespawnTicks:                RespawnSystem.GolemRespawnTicks,           // 120
            AttackWindupTicks:           CombatConstants.GolemAttackWindupTicks,    // 6
            AttackPattern:               1
        ),
    };

    /// <summary>
    /// kind에 해당하는 카탈로그 항목을 반환.
    ///
    /// <para>
    /// 알 수 없는 kindId는 GameMap ctor의 <c>Enum.IsDefined</c> 검증이 먼저 걸러내므로
    /// 여기서 ArrayIndexOutOfRange가 발생하면 저작 버그 — fail loud.
    /// </para>
    /// </summary>
    internal static EnemyEntry For(EnemyKind kind) => Catalog[(byte)kind];
}

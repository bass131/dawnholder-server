using System.Numerics;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Maps.States;
using Shared.GameData;

namespace Dawnholder.Server.GameServer.Combat;


// 서버 소유 entity — owner GameSession 없음 (player와 가장 큰 차이). spawn/mutation/broadcast
// 모두 서버 권위 (헌법 #1).
//
// position은 `float x/y` 두 필드로 박혀 있고 PlayerEntity의 `Vector2 Position`과 다름 —
// `S_EntitySpawn` 패킷이 (x, y) 두 필드로 직렬화하기 때문에 wire format과 1:1로 박으면
// 추후 변환 코드 1단계 절약.
//
// **IsDead derived**: `Hp <= 0`. 음수 보호 자동(예: 데미지 overflow로 Hp=-5여도 IsDead true).
public class EnemyEntity
{
    // State 초기화: Boss = Idle, Normal/Golem = Patrol. Fsm은 GameMap.SpawnEnemy에서 생성
    // (OwningMap 세팅 후) — kind별 초기 State(BossStates.Idle / EnemyStates.Patrol).
    public EnemyEntity(int entityId, EnemyKind kind, float x, float y, int maxHp, EnemyStats stats = default)
    {
        EntityId = entityId;
        Kind = kind;
        X = x;
        Y = y;
        SpawnX = x;
        SpawnY = y;
        MaxHp = maxHp;
        Hp = maxHp;
        Stats = stats;

        State = kind == EnemyKind.Boss ? EnemyState.Idle : EnemyState.Patrol;
        PatrolDir = 1;

        // 초기 쿨다운: 스폰 직후 즉시 공격 방지.
        // Boss = 페이즈 1 쿨다운(40틱=2초). Normal/Golem = 일반몹 쿨다운(30틱=1.5초).
        if (kind == EnemyKind.Boss)
            AttackCooldownTicks = CombatConstants.BossPhase1CooldownTicks;
        else
            AttackCooldownTicks = CombatConstants.NormalAttackCooldownTicks;
    }

    public int EntityId { get; }
    public EnemyKind Kind { get; }
    public float X { get; set; }
    public float Y { get; set; }
    public int Hp { get; set; }
    public int MaxHp { get; }
    public bool IsDead => Hp <= 0;

    // 서버 권위 스탯 (헌법 #1: 적 스탯도 서버가 결정). struct default = Defense:0 — 무방어 적.
    public EnemyStats Stats { get; }

    // 적 entity의 피격 판정 AABB. 1×1 unit 박스 (center = X/Y, halfExtent = 0.5×0.5).
    public AABB Hitbox => new AABB(new Vector2(X, Y), new Vector2(CombatConstants.HitboxHalfExtent, CombatConstants.HitboxHalfExtent));

    // ── AI 상태 필드 ─────────────────────────────────────────────────────────
    // tick thread invariant 안에서만 읽기/쓰기 (GameMap 단일 actor 보장 — lock 불필요).

    /// <summary>현재 AI 상태. Normal = Patrol 시작, Boss = Idle 고정.</summary>
    public EnemyState State { get; set; }

    /// <summary>
    /// Chase 대상 player entityId. null = 타겟 없음 (Patrol/Idle 상태).
    /// Chase 도중 target이 사라지거나 de-aggro 시 null로 초기화 후 Patrol 복귀.
    /// </summary>
    public int? TargetEntityId { get; set; }

    /// <summary>
    /// 스폰 좌표의 X. Patrol 왕복의 중심점.
    /// ctor에서 x 값으로 초기화 — respawn 시 이 좌표로 되돌아옴.
    /// </summary>
    public float SpawnX { get; }

    /// <summary>
    /// 스폰 좌표의 Y. Patrol/Idle 기준 Y.
    /// 이번 scope에서 AI는 X축 수평 이동만 — Y는 고정.
    /// </summary>
    public float SpawnY { get; }

    /// <summary>
    /// 현재 순찰 방향. +1 = 오른쪽, -1 = 왼쪽.
    /// Patrol 경계 닿으면 반전. Chase에서 Patrol 복귀 시에도 유지.
    /// </summary>
    public int PatrolDir { get; set; }

    /// <summary>
    /// Respawn 대기 카운트다운 (tick 단위).
    /// 0 = 살아있음 또는 respawn 대기 없음.
    /// >0 = 죽은 후 카운트다운 중. 매 tick 감소 → 0 도달 시 respawn.
    /// Boss는 respawn 없음 (StageClear 1회성) — 이 필드 불사용.
    /// </summary>
    public int RespawnTicksRemaining { get; set; }

    // 애니메이션 상태 latch 카운터 (tick 단위). PlayerEntity latch 설계와 동일.
    // 우선순위: Death > Hit > Attack > Walk > Idle (적은 Jump 없음).
    // tick thread invariant — EnemyAISystem.Update 안에서만 읽기/쓰기.
    public int AttackLatchTicks { get; set; }    // Attack 상태 남은 latch 틱 수
    public int HitLatchTicks    { get; set; }    // Hit 상태 남은 latch 틱 수

    // 공격 windup(준비/휘두르기) 남은 틱 수. EnemyAttackState.Enter가 kind별 windup으로 세팅,
    // Tick에서 0 도달 시 ApplyMeleeDamage 실행. 0 = windup 없음(진입 즉시 타격, 옛 거동).
    // 골렘은 swing 애니가 길어 windup>0 — "애니 끝나고 hit" 체감 보장(헌법 #5: 틱 카운터만).
    // tick thread invariant — EnemyAISystem.Update 안에서만 읽기/쓰기.
    public int AttackWindupTicks { get; set; }

    // 피격 넉백 속도(X). Normal/Golem EnemyHitState에서만 적용/감쇠.
    // 적은 지형 물리 없이 순수 X 적분 (기존 적 이동 모델과 동일).
    public float KnockbackVx { get; set; }

    // tick thread invariant — EnemyAISystem.Update 안에서만 R/W.
    // >0: 이 tick 이후까지 이동/AI 봉쇄. 0 도달 시 자동 해제.
    // Boss는 이 필드를 세팅해도 BossBehaviorSystem에 가드 없음 → 면역(설계 의도).
    public long FrozenUntilTick { get; set; }

    // ── 보스 FSM 상태 필드 ───────────────────────────────────────────────────
    // BossStates(Idle/Move/Telegraph/Attack) 전용. Normal/Golem은 이 필드를 사용하지 않음.
    // tick thread invariant — BossBehaviorSystem.Update → Fsm.Tick 경로 안에서만 읽기/쓰기.

    /// <summary>페이즈 2 전환 여부. HP ≤ 50% 시 true로 1회 전환 (idempotent).</summary>
    public bool IsPhase2 { get; set; }

    /// <summary>
    /// 보스+일반몹 공통 공격 쿨다운 남은 틱 수.
    /// Boss: 0이 되면 탐지 후 Move 전환(BossIdleState가 카운트다운).
    ///       post-attack은 쿨다운(긴 리듬), 배회 종료 후엔 BossIdlePauseTicks(짧은 숨) — Idle dwell로 통합.
    /// Normal/Golem: 0일 때 ChaseState.Tick이 Attack으로 전환. 공격 후 NormalAttackCooldownTicks 리셋.
    ///               EnemyAISystem.Update가 매 틱 감소 (boss는 AI continue로 미도달 — 충돌 0).
    /// ctor에서 초기 쿨다운 값으로 초기화 — 스폰 즉시 공격 방지.
    /// </summary>
    public int AttackCooldownTicks { get; set; }

    /// <summary>보스 Move(배회) 남은 틱. 타겟 없이 배회 중 0 도달 시 Idle 복귀.
    /// 타겟 추격 중엔 감소 안 함(추격은 사거리 도달/타겟 상실까지 지속).</summary>
    public int MoveTicksRemaining { get; set; }

    /// <summary>
    /// telegraph(예고) 남은 틱 수. 0보다 크면 예고 중.
    /// 0 도달 틱에 실제 데미지 판정 실행 → 쿨다운 리셋.
    /// </summary>
    public int TelegraphTicksRemaining { get; set; }

    // AI State machine + 소속 맵 back-ref. Normal/Golem = EnemyStates, Boss = BossStates.
    // OwningMap: State가 같은 맵 player를 스캔하는 통로. GameMap.SpawnEnemy에서 세팅.
    internal GameMap? OwningMap { get; set; }
    internal StateMachine<EnemyEntity>? Fsm { get; set; }

    /// <summary>
    /// freeze 중첩 규칙: max(기존, 신규) 적용 — 더 늦은 만료가 우선.
    /// 평타(긴 freeze) + 썬더볼트(짧은 freeze) 중첩 시 조기 해제 방지 (plan-auditor 우려 B).
    /// Boss에 호출돼도 BossBehaviorSystem에 가드가 없으므로 데미지 지연만 발동, 이동은 계속.
    /// </summary>
    public void ApplyFreeze(long untilTick)
        => FrozenUntilTick = Math.Max(FrozenUntilTick, untilTick);

    // 피격 진입. Normal/Golem → 진짜 HitState(AI 멈춤 + 넉백). Boss → latch만(이동 없는 고정형, FSM 전환 불가).
    //
    // 보스에 Fsm이 생긴 뒤에도 EnemyStates.Hit로 전환되면 안 됨 — 보스는 BossStates 전용 FSM.
    // Kind==Boss 가드로 latch만 적용(헌법 #1: 보스 피격 피드백은 HitLatchTicks로만).
    public void EnterHitState(float dirX)
    {
        HitLatchTicks = CombatConstants.AnimLatchTicks;
        if (Kind == EnemyKind.Boss || Fsm == null) return;
        KnockbackVx = Constants.KnockbackInitialVx * (dirX < 0f ? -1f : 1f);
        Fsm.ChangeState(EnemyStates.Hit, this);
    }
}

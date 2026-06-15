using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Maps;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Maps.States;

// 보스 AI State 4종(Idle/Move/Telegraph/Attack). Flyweight(GPP-06) 정적 인스턴스 — 틱 루프 new 0 (헌법 #5).
//
// wire State enum은 Idle 고정 (BossIdleState.Enter만 세팅, Move/Telegraph/Attack은 미변경).
// EnemyState enum에 신규값 추가 금지 — 보스 시각은 animState(AttackLatch/Walk)가 구동
// (BossBehaviorSystem.Update의 주기 broadcast). "명시적 State"의 이점은 Fsm 클래스 구조에 있음.
// blind-timer(쿨다운만으로 telegraph 반복) 폐기 → 탐지/이동 구동으로 교체.
//
// 사이클:
//   Idle(dwell+탐지) → Move(접근/배회) → {사거리→Telegraph→Attack→Idle | 배회종료→Idle}.
//   Idle:      AttackCooldownTicks 카운트다운 → 0 도달 시 탐지 후 Move 전환.
//   Move:      타겟 有 → MoveChase(접근), 사거리 도달 시 BeginTelegraph → Telegraph.
//              타겟 無 → MovePatrol(배회) BossWanderTicks 소진 → Idle(짧은 pause).
//              매 틱 재탐지(배회 중 진입한 player 포착).
//   Telegraph: 예고 카운트다운 → 0 도달 틱에 Attack 전환.
//   Attack:    Enter에서 데미지 판정 + 쿨다운 리셋, Tick에서 Idle 복귀.
internal static class BossStates
{
    internal static readonly BossIdleState      Idle      = new();
    internal static readonly BossMoveState      Move      = new();
    internal static readonly BossTelegraphState Telegraph = new();
    internal static readonly BossAttackState    Attack    = new();

    // 범위 내 플레이어에게 데미지 적용 + S_EnemyAttack broadcast.
    // BossAttackState.Enter(= telegraph 완료 틱)에서만 호출 — tick thread invariant 보장.
    //
    // 헌법 #1: player.Position = 서버 권위 위치만 사용. 클라 신고 위치 절대 금지.
    // EnemyStates.ApplyMeleeDamage 공통 헬퍼에 위임 — 보스 파라미터만 전달.
    // 보스 동작은 1비트도 변경 없음 (BossBehaviorTests/BossStageClearTests/LagSimIntegrationTests 전건이 guard).
    internal static void ApplyBossAttack(GameMap map, EnemyEntity boss)
        => EnemyStates.ApplyMeleeDamage(
            map, boss,
            CombatConstants.BossAttackHalfExtent,
            CombatConstants.BossBaseDamage,
            boss.IsPhase2 ? (byte)1 : (byte)0);

    // telegraph 시작: 예고 틱 결정 + AttackLatch 세팅 + 즉시 broadcast → Telegraph 반환.
    // 옛 BossIdleState의 쿨다운-0 셋업과 동일 — 트리거가 "Move 사거리 도달"로 바뀌었을 뿐.
    internal static ActorState<EnemyEntity> BeginTelegraph(EnemyEntity enemy)
    {
        enemy.TelegraphTicksRemaining = enemy.IsPhase2
            ? Constants.BossPhase2TelegraphTicks
            : Constants.BossTelegraphTicks;
        enemy.AttackLatchTicks = enemy.TelegraphTicksRemaining + CombatConstants.AnimLatchTicks;

        S_EntityState telegraphPkt = new S_EntityState
        {
            entityId   = enemy.EntityId,
            x          = enemy.X,
            y          = enemy.Y,
            state      = (byte)enemy.State,
            animState  = (byte)AnimState.Attack,
            serverTick = (int)enemy.OwningMap!.CurrentTick,
        };
        enemy.OwningMap.BroadcastToAll(telegraphPkt.Write());
        return BossStates.Telegraph;
    }
}

// 보스 Idle State: post-attack 쿨다운 또는 배회 후 짧은 pause 카운트다운.
// 0 도달 시 탐지(초기 타겟 세팅) 후 Move 전환.
internal sealed class BossIdleState : ActorState<EnemyEntity>
{
    public override AnimState AnimState => AnimState.Idle;

    public override void Enter(EnemyEntity enemy) => enemy.State = EnemyState.Idle;

    public override ActorState<EnemyEntity>? Tick(EnemyEntity enemy)
    {
        // dwell 카운트다운(= AttackCooldownTicks). post-attack=긴 쿨다운, 배회후=짧은 pause.
        if (enemy.AttackCooldownTicks > 0)
        {
            enemy.AttackCooldownTicks--;
            return null;
        }
        // dwell 끝 → 탐지(초기 타겟 세팅) 후 Move 전환. (Move가 매 틱 재탐지도 함.)
        enemy.TargetEntityId = EnemyStates.FindClosestInAggro(enemy)?.EntityId;
        return BossStates.Move;
    }
}

// 보스 Move: 타겟 有 → 접근(MoveChase), 사거리 도달 시 Telegraph. 타겟 상실(de-aggro) → 배회로.
//   타겟 無 → 배회(MovePatrol) N틱 후 Idle. 매 틱 재탐지(배회 중 진입한 player 포착).
// wire State는 건드리지 않음 → Idle 고정 유지(v9 안전). 걷는 시각은 animState=Walk(ComputeBossAnimState).
internal sealed class BossMoveState : ActorState<EnemyEntity>
{
    public override AnimState AnimState => AnimState.Walk;

    public override void Enter(EnemyEntity enemy) => enemy.MoveTicksRemaining = CombatConstants.BossWanderTicks;

    public override ActorState<EnemyEntity>? Tick(EnemyEntity enemy)
    {
        // 타겟 해석 / 없으면 재탐지(배회 중 진입 포착).
        PlayerEntity? target = enemy.TargetEntityId.HasValue
            ? enemy.OwningMap!.GetPlayer(enemy.TargetEntityId.Value)
            : null;
        if (target == null)
        {
            target = EnemyStates.FindClosestInAggro(enemy);
            enemy.TargetEntityId = target?.EntityId;
        }

        if (target != null)
        {
            float dx = target.Position.X - enemy.X;
            float absDx = dx < 0 ? -dx : dx;

            if (absDx > enemy.Stats.AggroRange * CombatConstants.DeAggroHysteresis)   // de-aggro 히스테리시스(몬스터와 동일)
            {
                enemy.TargetEntityId = null;
                // 아래 배회 블록으로 fall-through
            }
            else if (absDx <= CombatConstants.BossAttackTriggerRange)
            {
                return BossStates.BeginTelegraph(enemy);  // 사거리 도달 → 예고 시작
            }
            else
            {
                EnemyStates.MoveChase(enemy);   // 접근 (추격은 타임아웃 없음)
                return null;
            }
        }

        // 타겟 없음 → 배회. N틱 소진 시 Idle(짧은 pause 세팅).
        EnemyStates.MovePatrol(enemy);
        if (enemy.MoveTicksRemaining > 0) enemy.MoveTicksRemaining--;
        if (enemy.MoveTicksRemaining == 0)
        {
            enemy.AttackCooldownTicks = CombatConstants.BossIdlePauseTicks;
            return BossStates.Idle;
        }
        return null;
    }
}

// 보스 Telegraph State: 예고 카운트다운. 0 도달 틱에 Attack 전환.
// 데미지는 여기서 X — BossAttackState.Enter가 같은 틱(전환 트리거 틱)에 실행.
//
// **버그 1 봉합(M6)**: hit-stun(HitLatchTicks>0) 중에는 카운트다운을 정지(pause).
//   옛 구현은 hit 중에도 telegraph가 계속 감겨, 보스를 활발히 때리는 동안 모든 공격의
//   준비자세가 체감상 단축되는 *지속 손상*이 발생("이후 공격부터 계속 빨라짐").
//   pause로 hit 후 남은 예고가 온전히 재생 → 회피 공정성 보장(헌법 #1 서버 권위 타이밍).
//   BossBehaviorSystem이 동일 가드로 AttackLatchTicks 감소도 함께 멈춤 → 애니 latch와 정합.
internal sealed class BossTelegraphState : ActorState<EnemyEntity>
{
    public override AnimState AnimState => AnimState.Attack;

    public override ActorState<EnemyEntity>? Tick(EnemyEntity enemy)
    {
        // hit-stun 중에는 예고 카운트다운 정지 — 데미지 타이밍을 hit만큼 뒤로 미룸.
        if (enemy.HitLatchTicks > 0)
            return null;

        enemy.TelegraphTicksRemaining--;
        if (enemy.TelegraphTicksRemaining == 0)
            return BossStates.Attack;
        return null;
    }
}

// 보스 Attack State: 데미지 판정 + 쿨다운 리셋(Enter) → Idle 복귀(Tick).
// 쿨다운 카운트다운은 Idle dwell이 담당 → 여기선 Idle 복귀만.
// (옛 off-by-one cooldown-- 제거: blind-timer 폐기로 불필요.)
internal sealed class BossAttackState : ActorState<EnemyEntity>
{
    public override AnimState AnimState => AnimState.Attack;

    public override void Enter(EnemyEntity enemy)
    {
        BossStates.ApplyBossAttack(enemy.OwningMap!, enemy);
        enemy.AttackCooldownTicks = enemy.IsPhase2
            ? CombatConstants.BossPhase2CooldownTicks
            : CombatConstants.BossPhase1CooldownTicks;
    }

    public override ActorState<EnemyEntity>? Tick(EnemyEntity enemy) => BossStates.Idle;
}

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
    internal static void ApplyBossAttack(GameMap map, EnemyEntity boss)
    {
        AABB bossAttackBox = new AABB(
            new Vector2(boss.X, boss.Y),
            new Vector2(CombatConstants.BossAttackHalfExtent, CombatConstants.BossAttackHalfExtent));

        byte attackPattern = boss.IsPhase2 ? (byte)1 : (byte)0;

        foreach (PlayerEntity player in map.Players)
        {
            AABB playerBox = new AABB(player.Position, new Vector2(0.5f, 0.5f));
            if (!bossAttackBox.Intersects(playerBox)) continue;

            int damage = Formulas.ComputeDamage(boss.Stats, player.Stats, CombatConstants.BossBaseDamage);
            player.Hp -= damage;

            float dirX = player.Position.X >= boss.X ? 1f : -1f;
            player.EnterHitState(dirX);

            // 피격 직후 권위 HP 통지 — 음수면 SendPlayerHp 내부에서 0 floor.
            map.SendPlayerHp(player);

            S_EnemyAttack attackPkt = new S_EnemyAttack
            {
                attackerId = boss.EntityId,
                targetId   = player.EntityId,
                damage     = damage,
                targetCurrentHp = player.Hp,
                attackPattern   = attackPattern,
            };
            map.BroadcastToAll(attackPkt.Write());

            if (player.Hp <= 0)
            {
                Vector2 spawn = map.PlayerSpawnPosition;
                player.Position = spawn;
                player.Velocity = Vector2.Zero;
                player.OnGround = false;
                player.Hp = player.Stats.MaxHp;
                player.Revive();
                // 부활 직후 full HP 권위 통지 — 클라 HUD 표시 미러 제거의 핵심.
                map.SendPlayerHp(player);
            }
        }
    }

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
            entityId  = enemy.EntityId,
            x         = enemy.X,
            y         = enemy.Y,
            state     = (byte)enemy.State,
            animState = (byte)AnimState.Attack,
        };
        enemy.OwningMap!.BroadcastToAll(telegraphPkt.Write());
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

            if (absDx > enemy.Stats.AggroRange * 1.5f)   // de-aggro 히스테리시스(몬스터와 동일)
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
internal sealed class BossTelegraphState : ActorState<EnemyEntity>
{
    public override AnimState AnimState => AnimState.Attack;

    public override ActorState<EnemyEntity>? Tick(EnemyEntity enemy)
    {
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

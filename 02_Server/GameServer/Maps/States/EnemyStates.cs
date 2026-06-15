using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Maps;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Maps.States;

// 적(Normal/Golem) AI State 4종 (Patrol / Chase / Attack / Hit). Flyweight(GPP-06) 정적 인스턴스 — 틱 루프 new 0 (헌법 #5).
//
// 행동 불변(옛 EnemyAISystem enum+switch와 비트 동일):
//   aggro 진입: |dx| <= AggroRange 가장 가까운 player → Chase + Target.
//   de-aggro: target이 AggroRange*1.5 초과 또는 소멸 → Patrol 복귀.
//   전환되는 그 틱에 *전환 후 상태의 이동*까지 수행 (옛 구조: 전이 후 같은 틱 movement 실행).
//   피격: HitState(AI 이동 멈춤 + 넉백 감쇠) → HitLatchTicks 소진 후 aggro 재판정.
//   공격: |dx| <= NormalAttackTriggerRange + 쿨다운 0 → Attack → 1틱 후 Chase 복귀 (쿨다운이 재공격 차단).
internal static class EnemyStates
{
    internal static readonly PatrolState        Patrol = new();
    internal static readonly ChaseState         Chase  = new();
    internal static readonly EnemyHitState      Hit    = new();
    internal static readonly EnemyAttackState   Attack = new();

    // Normal/Golem 공통 근접 데미지 적용. BossStates.ApplyBossAttack(보스)과 동형.
    //
    // 헌법 #1: 데미지·HP 감소·부활 전부 서버 판정. 클라는 S_EnemyAttack broadcast로만 수신.
    // 헌법 #5: DB/await 없음 — 순수 동기 연산 + write queue는 별도 worker.
    internal static void ApplyMeleeDamage(GameMap map, EnemyEntity attacker, float attackHalfExtent, int baseDamage, byte attackPattern)
    {
        AABB attackBox = new AABB(
            new Vector2(attacker.X, attacker.Y),
            new Vector2(attackHalfExtent, attackHalfExtent));

        foreach (PlayerEntity player in map.Players)
        {
            AABB playerBox = new AABB(player.Position, new Vector2(CombatConstants.HitboxHalfExtent, CombatConstants.HitboxHalfExtent));
            if (!attackBox.Intersects(playerBox)) continue;

            if (player.IsInvulnerable(map.CurrentTick)) continue;

            int damage = Formulas.ComputeDamage(attacker.Stats, player.Stats, baseDamage);
            player.Hp -= damage;

            float dirX = player.Position.X >= attacker.X ? 1f : -1f;
            player.EnterHitState(dirX);

            map.SendPlayerHp(player);

            S_EnemyAttack attackPkt = new S_EnemyAttack
            {
                attackerId      = attacker.EntityId,
                targetId        = player.EntityId,
                damage          = damage,
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
                map.SendPlayerHp(player);
            }
        }
    }

    // hit-stun 종료 후 복귀 State 결정.
    //
    // 후공/선공 공통 트리거: 피격으로 세팅된 TargetEntityId(공격자)가 추격 사거리 안이면 Chase.
    //   - de-aggro 히스테리시스(AggroRange*1.5) 적용 — 데미지 받은 직후 멀리 있으면 Patrol 복귀.
    //
    // target 잃은 경우:
    //   - 선공(AggroOnSight=true): 주변 재탐지 → 있으면 Chase, 없으면 Patrol.
    //   - 후공(AggroOnSight=false): 시야 aggro 없음 → 무조건 Patrol 복귀.
    internal static ActorState<EnemyEntity> ResolveAfterHit(EnemyEntity enemy)
    {
        if (enemy.TargetEntityId.HasValue)
        {
            PlayerEntity? t = enemy.OwningMap!.GetPlayer(enemy.TargetEntityId.Value);
            if (t != null && System.MathF.Abs(t.Position.X - enemy.X) <= enemy.Stats.AggroRange * CombatConstants.DeAggroHysteresis)
                return Chase;
        }

        if (enemy.Stats.AggroOnSight)
        {
            PlayerEntity? closest = FindClosestInAggro(enemy);
            if (closest != null)
            {
                enemy.TargetEntityId = closest.EntityId;
                return Chase;
            }
        }

        enemy.TargetEntityId = null;
        return Patrol;
    }

    internal static PlayerEntity? FindClosestInAggro(EnemyEntity enemy)
    {
        PlayerEntity? closest = null;
        float closestDist = float.MaxValue;
        float aggroRange = enemy.Stats.AggroRange;
        foreach (PlayerEntity p in enemy.OwningMap!.Players)
        {
            float dx = p.Position.X - enemy.X;
            float absDx = dx < 0 ? -dx : dx;
            if (absDx <= aggroRange && absDx < closestDist)
            {
                closest = p;
                closestDist = absDx;
            }
        }
        return closest;
    }

    internal static void MovePatrol(EnemyEntity enemy)
    {
        float step = enemy.Stats.MoveSpeed * Constants.TickDuration;
        enemy.X += enemy.PatrolDir * step;

        float leftBound  = enemy.SpawnX - enemy.Stats.PatrolRange;
        float rightBound = enemy.SpawnX + enemy.Stats.PatrolRange;
        if (enemy.X <= leftBound)
        {
            enemy.X = leftBound;
            enemy.PatrolDir = 1;
        }
        else if (enemy.X >= rightBound)
        {
            enemy.X = rightBound;
            enemy.PatrolDir = -1;
        }
    }

    internal static void MoveChase(EnemyEntity enemy)
    {
        if (!enemy.TargetEntityId.HasValue) return;
        PlayerEntity? target = enemy.OwningMap!.GetPlayer(enemy.TargetEntityId.Value);
        if (target == null) return;

        float step = enemy.Stats.MoveSpeed * Constants.TickDuration;
        float dx = target.Position.X - enemy.X;
        if (dx > 0f)
            enemy.X += step;
        else if (dx < 0f)
            enemy.X -= step;
    }
}

internal sealed class PatrolState : ActorState<EnemyEntity>
{
    public override AnimState AnimState => AnimState.Walk;

    public override void Enter(EnemyEntity enemy) => enemy.State = EnemyState.Patrol;

    public override ActorState<EnemyEntity>? Tick(EnemyEntity enemy)
    {
        // 선공만 시야 aggro. 후공(AggroOnSight=false)은 피격 트리거만 사용 — 여기서 Chase 전환 없음.
        if (enemy.Stats.AggroOnSight)
        {
            PlayerEntity? closest = EnemyStates.FindClosestInAggro(enemy);
            if (closest != null)
            {
                enemy.TargetEntityId = closest.EntityId;
                EnemyStates.MoveChase(enemy);   // 전환 틱 같은-틱 Chase 이동 (옛 구조 보존)
                return EnemyStates.Chase;
            }
        }
        EnemyStates.MovePatrol(enemy);
        return null;
    }
}

internal sealed class ChaseState : ActorState<EnemyEntity>
{
    public override AnimState AnimState => AnimState.Walk;

    public override void Enter(EnemyEntity enemy) => enemy.State = EnemyState.Chase;

    public override ActorState<EnemyEntity>? Tick(EnemyEntity enemy)
    {
        PlayerEntity? target = enemy.TargetEntityId.HasValue
            ? enemy.OwningMap!.GetPlayer(enemy.TargetEntityId.Value)
            : null;

        bool targetLost = target == null;
        bool deAggro = false;
        if (target != null)
        {
            float dx = target.Position.X - enemy.X;
            float absDx = dx < 0 ? -dx : dx;
            deAggro = absDx > enemy.Stats.AggroRange * CombatConstants.DeAggroHysteresis;
        }

        if (targetLost || deAggro)
        {
            enemy.TargetEntityId = null;
            EnemyStates.MovePatrol(enemy);   // 전환 틱 같은-틱 Patrol 이동 (옛 구조 보존)
            return EnemyStates.Patrol;
        }

        PlayerEntity? closest = EnemyStates.FindClosestInAggro(enemy);
        if (closest != null && closest.EntityId != enemy.TargetEntityId)
            enemy.TargetEntityId = closest.EntityId;

        // 현재 타겟이 사거리 안 + 쿨다운 0 → 즉시 공격 전환 (telegraph 없는 패턴).
        PlayerEntity? cur = enemy.TargetEntityId.HasValue ? enemy.OwningMap!.GetPlayer(enemy.TargetEntityId.Value) : null;
        if (cur != null)
        {
            float adx = System.MathF.Abs(cur.Position.X - enemy.X);
            if (adx <= CombatConstants.NormalAttackTriggerRange && enemy.AttackCooldownTicks == 0)
                return EnemyStates.Attack;
        }

        EnemyStates.MoveChase(enemy);
        return null;
    }
}

// 피격 hitstun. AI 이동(Patrol/Chase) 멈춤 + 넉백 감쇠. 순수 X (적은 지형 물리 없음).
// stun 길이 = HitLatchTicks(EnterHitState가 AnimLatchTicks 세팅, EnemyAISystem이 매 틱 감소) → 0 도달 시 aggro 재판정 복귀.
internal sealed class EnemyHitState : ActorState<EnemyEntity>
{
    public override AnimState AnimState => AnimState.Hit;

    public override ActorState<EnemyEntity>? Tick(EnemyEntity enemy)
    {
        enemy.X += enemy.KnockbackVx * Constants.TickDuration;
        enemy.KnockbackVx *= Constants.KnockbackDecayPerTick;
        if (System.MathF.Abs(enemy.KnockbackVx) < Constants.ExternalImpulseEpsilon)
            enemy.KnockbackVx = 0f;

        if (enemy.HitLatchTicks > 0)
            return null;

        // Patrol↔Chase 전환과 달리 복귀 틱엔 이동 안 함 — stun 풀린 첫 틱은 정지, 다음 틱부터 이동(회복 박자, 의도).
        return EnemyStates.ResolveAfterHit(enemy);
    }

    public override void Exit(EnemyEntity enemy) => enemy.KnockbackVx = 0f;
}

// Normal/Golem 공격 State. Enter에서 즉시 데미지 판정 + 쿨다운 리셋 → 1틱 후 Chase 복귀.
// 보스와 달리 telegraph 없음 — 사거리 도달 + 쿨다운 0이면 ChaseState.Tick이 Attack으로 전환.
// 쿨다운(AttackCooldownTicks)이 재공격을 차단 — Chase 복귀 직후 다시 공격 조건을 재평가.
//
// attackPattern: slime(Normal)=0, golem=1. C2 이펙트 분기 힌트로 wire에 포함.
internal sealed class EnemyAttackState : ActorState<EnemyEntity>
{
    public override AnimState AnimState => AnimState.Attack;

    public override void Enter(EnemyEntity enemy)
    {
        byte pattern = enemy.Kind == EnemyKind.Golem ? (byte)1 : (byte)0;
        EnemyStates.ApplyMeleeDamage(enemy.OwningMap!, enemy, CombatConstants.NormalAttackHalfExtent, CombatConstants.NormalBaseDamage, pattern);
        enemy.AttackCooldownTicks = CombatConstants.NormalAttackCooldownTicks;
        enemy.AttackLatchTicks    = CombatConstants.AnimLatchTicks;
    }

    // 1틱 후 Chase 복귀. 쿨다운이 재공격 차단 (Chase.Tick: 쿨다운 > 0이면 Attack 전환 불가).
    public override ActorState<EnemyEntity>? Tick(EnemyEntity enemy) => EnemyStates.Chase;
}


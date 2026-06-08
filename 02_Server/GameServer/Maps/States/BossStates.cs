using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Maps;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Maps.States;

// 보스 AI State 3종 (Idle / Telegraph / Attack). Flyweight(GPP-06) 정적 인스턴스 — 틱 루프 new 0 (헌법 #5).
//
// wire State enum은 Idle 고정 (BossIdleState.Enter만 세팅, Telegraph/Attack은 안 건드림).
// EnemyState enum에 신규값 추가 금지 — 보스 시각은 animState(AttackLatch)가 구동
// (BossBehaviorSystem.Update의 주기 broadcast). "명시적 State"의 이점은 Fsm 클래스 구조에 있음.
//
// 사이클 (옛 BossBehaviorSystem 조건 분기와 비트 동일):
//   Idle:      쿨다운 감소 → 0 도달 틱에 telegraph 시작 broadcast + Telegraph 전환.
//   Telegraph: 예고 카운트다운 → 0 도달 틱에 Attack 전환(데미지는 AttackState가 같은 틱 실행).
//   Attack:    Enter에서 데미지 판정 + 쿨다운 리셋, Tick에서 쿨다운 1회 감소 후 Idle 복귀.
internal static class BossStates
{
    internal static readonly BossIdleState      Idle      = new();
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
            }
        }
    }
}

// 보스 Idle State: 공격 쿨다운 감소. 0 도달 틱에 telegraph 시작 broadcast + Telegraph 전환.
internal sealed class BossIdleState : ActorState<EnemyEntity>
{
    public override AnimState AnimState => AnimState.Idle;

    public override void Enter(EnemyEntity enemy) => enemy.State = EnemyState.Idle;

    public override ActorState<EnemyEntity>? Tick(EnemyEntity enemy)
    {
        if (enemy.AttackCooldownTicks > 0)
            enemy.AttackCooldownTicks--;

        if (enemy.AttackCooldownTicks == 0)
        {
            // telegraph 시작: 틱 수 결정 + AttackLatch 세팅 + 즉시 broadcast (쿨다운 0 도달 틱).
            // 카운트다운은 다음 틱 BossTelegraphState부터 — 이 틱엔 셋업만(옛 코드와 동일 타이밍).
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
            // 보스는 OwningMap 없이 존재 불가 — SpawnEnemy에서 반드시 세팅.
            enemy.OwningMap!.BroadcastToAll(telegraphPkt.Write());

            return BossStates.Telegraph;
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

// 보스 Attack State: 데미지 판정 + 쿨다운 리셋(Enter) → 쿨다운 1회 감소 후 Idle 복귀(Tick).
//
// ⚠️ Tick의 cooldown-- 1회가 비트 보존 핵심: 안 하면 Attack→Idle 전환이 틱 1개를 "소비"해
//   쿨다운 감소가 1틱 밀림 → 다음 telegraph 영구 지연(누적 drift). 공격 틱(S)에 cooldown=N set →
//   다음 틱(S+1) AttackState.Tick이 첫 감소(N→N-1) → 그 다음(S+2)부터 IdleState.Tick이 이어받음
//   = 옛 조건분기 코드(공격 틱엔 감소 X, 다음 틱부터 else 분기 감소)와 동일 타이밍.
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

    public override ActorState<EnemyEntity>? Tick(EnemyEntity enemy)
    {
        if (enemy.AttackCooldownTicks > 0)
            enemy.AttackCooldownTicks--;
        return BossStates.Idle;
    }
}

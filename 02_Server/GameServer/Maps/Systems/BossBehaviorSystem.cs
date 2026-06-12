using Dawnholder.Server.GameServer.Combat;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Maps;

/// <summary>
/// §2.2 BossBehaviorSystem — Boss entity State 머신 구동.
///
/// **단일 책임**: EnemyKind.Boss의 Fsm.Tick 1회 진행 + 페이즈 2 전환(HP 구동) + latch 감소 + broadcast.
///
/// **호출 규율(§1.1)**: GameMap.Tick에서 EnemyAISystem 다음에 호출.
///   tick thread invariant — lock 없음.
///
/// **헌법 #1 (Server Authority)**: 데미지 판정은 BossStates.ApplyBossAttack — player.Position 서버 권위.
///
/// **헌법 #5 정합**: Task.Delay / Thread.Sleep / DateTime 타이머 전혀 없음.
///   모든 타이밍은 State 내부 tick 카운터(int) 감소만.
///
/// **State 머신 전략**: 4-State + 탐지 구동.
///   BossIdleState(dwell+탐지) → BossMoveState(접근/배회) → {사거리→BossTelegraphState(예고)→BossAttackState(판정+리셋)→Idle | 배회종료→Idle}.
///   ApplyBossAttack은 BossStates 정적 헬퍼 — BossAttackState.Enter가 호출(telegraph 완료 틱).
///
/// **페이즈 2 전환**: HP 구동 병렬 관심사 — State에 분산하지 않고 여기서 단독 처리.
///   쿨다운 clamp 조건 TelegraphTicksRemaining == 0도 그대로 유지.
/// </summary>
internal sealed class BossBehaviorSystem
{
    internal void Update(GameMap map, long tickNumber)
    {
        bool shouldBroadcast = tickNumber % Constants.SnapshotTickInterval == 0;

        foreach (EnemyEntity enemy in map.Enemies.Values)
        {
            if (enemy.Kind != EnemyKind.Boss) continue;
            if (enemy.IsDead) continue;

            // ── 페이즈 2 전환 체크 (1회성 idempotent) ─────────────────────────
            if (!enemy.IsPhase2 && enemy.Hp <= (int)(enemy.MaxHp * CombatConstants.BossPhase2HpThreshold))
            {
                enemy.IsPhase2 = true;
                // 쿨다운 중이면 페이즈 2 쿨다운으로 clamp.
                // 진행 중 telegraph는 유지 — 이미 예고한 타이밍을 단축하면 회피 공정성 깨짐.
                if (enemy.TelegraphTicksRemaining == 0 &&
                    enemy.AttackCooldownTicks > CombatConstants.BossPhase2CooldownTicks)
                {
                    enemy.AttackCooldownTicks = CombatConstants.BossPhase2CooldownTicks;
                }
            }

            // ── 보스 State 머신 1틱 진행 ──────────────────────────────────────
            enemy.Fsm!.Tick(enemy);

            // ── latch 카운터 감소 ──────────────────────────────────────────────
            if (enemy.HitLatchTicks > 0) enemy.HitLatchTicks--;
            if (enemy.AttackLatchTicks > 0) enemy.AttackLatchTicks--;

            // ── S_EntityState broadcast (SnapshotTickInterval 마다) ────────────
            if (shouldBroadcast)
            {
                byte bossAnimState = ComputeBossAnimState(enemy);
                S_EntityState statePkt = new S_EntityState
                {
                    entityId   = enemy.EntityId,
                    x          = enemy.X,
                    y          = enemy.Y,
                    state      = (byte)enemy.State,
                    animState  = bossAnimState,
                    serverTick = (int)tickNumber,
                };
                map.BroadcastToAll(statePkt.Write());
            }
        }
    }

    /// <summary>
    /// 보스 animState 계산. 우선순위: Death > Attack > Hit > Walk/Idle.
    /// Attack이 Hit보다 높음 — telegraph/공격 모션이 피격에 끊기지 않게.
    /// 이동 중이면 Walk — FSM 현재 상태 AnimState 사용(BossMoveState→Walk, 그 외→Idle).
    /// Telegraph/Attack의 AnimState=Attack은 위 AttackLatch 분기가 먼저 잡으므로 여기 도달 X.
    /// </summary>
    static byte ComputeBossAnimState(EnemyEntity boss)
    {
        if (boss.IsDead)               return (byte)AnimState.Death;
        if (boss.AttackLatchTicks > 0) return (byte)AnimState.Attack;
        if (boss.HitLatchTicks > 0)    return (byte)AnimState.Hit;
        // 이동 중이면 Walk — FSM 현재 상태 AnimState 사용(BossMoveState→Walk, 그 외→Idle).
        // Telegraph/Attack의 AnimState=Attack은 위 AttackLatch 분기가 먼저 잡으므로 여기 도달 X.
        return (byte)boss.Fsm!.AnimState;
    }
}

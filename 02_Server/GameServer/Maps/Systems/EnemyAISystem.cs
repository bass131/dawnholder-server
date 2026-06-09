using Dawnholder.Server.GameServer.Combat;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Maps;

/// <summary>
/// §2.2 EnemyAISystem — GameMap(컨테이너)에서 enemy AI FSM 로직 추출.
///
/// FSM 로직은 PatrolState/ChaseState(EnemyStates.cs)로 이주.
/// 본 System은 Fsm.Tick 구동 + latch 감소 + S_EntityState broadcast 담당.
/// 사망은 CombatSystem이 즉시 처리(S_EntityDeath + RemoveEnemy). 서버 권위 — 죽음 연출은 클라 VFX.
/// </summary>
internal sealed class EnemyAISystem
{
    internal void Update(GameMap map, long tickNumber)
    {
        bool shouldBroadcast = tickNumber % Constants.SnapshotTickInterval == 0;

        foreach (EnemyEntity enemy in map.Enemies.Values)
        {
            if (enemy.Kind == EnemyKind.Boss) continue;

            // freeze 가드: FrozenUntilTick 동안 AI(Fsm.Tick)·latch 감소 스킵 = 이동 봉쇄.
            // 만료 틱에 도달하면 즉시 해제. Boss는 이 가드 없음 → freeze 면역(헌법 #1).
            if (enemy.FrozenUntilTick > 0)
            {
                if (tickNumber >= enemy.FrozenUntilTick)
                    enemy.FrozenUntilTick = 0;
                else
                    continue;
            }

            enemy.Fsm!.Tick(enemy);
            if (enemy.HitLatchTicks > 0) enemy.HitLatchTicks--;
            if (enemy.AttackLatchTicks > 0) enemy.AttackLatchTicks--;

            if (shouldBroadcast)
            {
                byte animState = ComputeEnemyAnimState(enemy);
                S_EntityState statePacket = new S_EntityState
                {
                    entityId = enemy.EntityId,
                    x = enemy.X,
                    y = enemy.Y,
                    state = (byte)enemy.State,
                    animState = animState,
                };
                map.BroadcastToAll(statePacket.Write());
            }
        }
    }

    /// <summary>
    /// 적 entity의 현재 시각 애니메이션 상태를 계산. 서버 권위 (헌법 #1).
    ///
    /// **우선순위**: Hit > Attack > Walk(Patrol/Chase) > Idle.
    /// 죽은 적은 CombatSystem이 즉시 RemoveEnemy하므로 이 함수 도달 불가.
    /// </summary>
    static byte ComputeEnemyAnimState(EnemyEntity enemy)
    {
        if (enemy.HitLatchTicks > 0)
            return (byte)Shared.GameData.AnimState.Hit;

        if (enemy.AttackLatchTicks > 0)
            return (byte)Shared.GameData.AnimState.Attack;

        if (enemy.State == EnemyState.Patrol || enemy.State == EnemyState.Chase)
            return (byte)Shared.GameData.AnimState.Walk;

        return (byte)Shared.GameData.AnimState.Idle;
    }
}

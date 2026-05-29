using Dawnholder.Server.GameServer.Combat;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Maps;

/// <summary>
/// §2.2 EnemyAISystem — GameMap(컨테이너)에서 enemy AI FSM 로직 추출.
///
/// **단일 책임**: Normal enemy AI 1틱 진행 (aggro 판정 / Patrol↔Chase 전이 / 히스테리시스 / 이동 / S_EntityState broadcast).
/// **호출 규율(§1.1)**: GameMap.Tick 안에서만 호출 (직접 호출, EnqueueJob 경유 아님 — tick 루프 동기).
/// **데이터 소유**: GameMap이 소유. EnemyAISystem은 map.Players/map.Enemies 읽기 접근,
///   EnemyEntity 필드 직접 변경 (enemy는 mutable value-typed fields 노출).
/// **System 간 직접 호출 X(§2.2)**: 다른 System 직접 참조 없음.
///
/// **FSM 전이 규칙** (GameMap.UpdateEnemies 본문 그대로 옮김 — 동작 보존):
///   Patrol → Chase: 같은 맵 player 중 |dx| <= AggroRange인 가장 가까운 player 발견 시.
///   Chase → Patrol: target이 사라지거나 |dx| > AggroRange * 1.5 (de-aggro 히스테리시스).
///   Patrol 경계: SpawnX ± PatrolRange 도달 시 PatrolDir 반전.
/// </summary>
internal sealed class EnemyAISystem
{
    /// <summary>
    /// Normal enemy AI FSM 1틱 진행.
    /// GameMap.UpdateEnemies(tickNumber) 본문을 그대로 옮김 — 동작 완전 보존.
    /// </summary>
    internal void Update(GameMap map, long tickNumber)
    {
        float dt = Constants.TickDuration; // 1틱 시간 (초)
        bool shouldBroadcast = tickNumber % Constants.SnapshotTickInterval == 0;

        foreach (EnemyEntity enemy in map.Enemies.Values)
        {
            // Boss는 이번 Phase에서 AI 없음 (Idle 고정). Phase 09에서 별도 behavior.
            if (enemy.Kind != EnemyKind.Normal) continue;

            float moveSpeed = enemy.Stats.MoveSpeed;
            float aggroRange = enemy.Stats.AggroRange;
            float patrolRange = enemy.Stats.PatrolRange;

            // --- aggro 판정 (Patrol 및 Chase 상태 모두에서 매 tick 재판정) ---
            // 같은 맵 player 중 |dx| <= AggroRange인 가장 가까운 player를 탐색.
            PlayerEntity? closest = null;
            float closestDist = float.MaxValue;
            foreach (PlayerEntity p in map.Players)
            {
                float dx = p.Position.X - enemy.X;
                float absDx = dx < 0 ? -dx : dx;
                if (absDx <= aggroRange && absDx < closestDist)
                {
                    closest = p;
                    closestDist = absDx;
                }
            }

            // --- 상태 전이 ---
            if (enemy.State == EnemyState.Patrol)
            {
                if (closest != null)
                {
                    // aggro 진입 → Chase 전환
                    enemy.State = EnemyState.Chase;
                    enemy.TargetEntityId = closest.EntityId;
                }
            }
            else if (enemy.State == EnemyState.Chase)
            {
                // 타겟 유효성 재확인
                // (1) TargetEntityId가 아직 _players에 있는지 (portal 이동/disconnect 대응)
                // (2) 거리가 de-aggro 임계 초과하지 않는지
                PlayerEntity? target = null;
                if (enemy.TargetEntityId.HasValue)
                {
                    target = map.GetPlayer(enemy.TargetEntityId.Value);
                }

                bool targetLost = target == null;
                bool deAggro = false;
                if (target != null)
                {
                    float dx = target.Position.X - enemy.X;
                    float absDx = dx < 0 ? -dx : dx;
                    deAggro = absDx > aggroRange * 1.5f;
                }

                if (targetLost || deAggro)
                {
                    // de-aggro → Patrol 복귀
                    enemy.State = EnemyState.Patrol;
                    enemy.TargetEntityId = null;
                    target = null;
                }
                else if (closest != null && closest.EntityId != enemy.TargetEntityId)
                {
                    // 더 가까운 target으로 교체 (선택적 최적화 — 현재 target은 이미 범위 안)
                    enemy.TargetEntityId = closest.EntityId;
                    target = closest;
                }
            }

            // --- 이동 처리 ---
            float step = moveSpeed * dt;

            if (enemy.State == EnemyState.Patrol)
            {
                // SpawnX 중심 ±PatrolRange 왕복
                enemy.X += enemy.PatrolDir * step;

                // 경계 clamp + 방향 반전
                float leftBound  = enemy.SpawnX - patrolRange;
                float rightBound = enemy.SpawnX + patrolRange;
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
            else if (enemy.State == EnemyState.Chase && enemy.TargetEntityId.HasValue)
            {
                PlayerEntity? target = map.GetPlayer(enemy.TargetEntityId.Value);
                if (target != null)
                {
                    float dx = target.Position.X - enemy.X;
                    if (dx > 0f)
                        enemy.X += step;
                    else if (dx < 0f)
                        enemy.X -= step;
                    // dx == 0f 정확히 겹치면 이동 없음 (공격 판정은 CombatSystem에서)
                }
            }

            // --- S_EntityState broadcast ---
            // SnapshotTickInterval 마다 전원에게 전송.
            if (shouldBroadcast)
            {
                S_EntityState statePacket = new S_EntityState
                {
                    entityId = enemy.EntityId,
                    x = enemy.X,
                    y = enemy.Y,
                    state = (byte)enemy.State,
                };
                map.BroadcastToAll(statePacket.Write());
            }
        }
    }
}

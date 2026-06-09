using Dawnholder.Server.GameServer.Combat;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Maps;

/// <summary>
/// §2.2 DeferredDamageSystem — "N틱 뒤 데미지 적용" 큐.
///
/// **단일 책임**: DeferredImpact 큐 관리 + tick 카운트다운 + 도착 시 HP 변경·broadcast·사망 처리.
/// **호출 규율(§1.1)**: GameMap.Tick 안에서만 호출 (RespawnSystem 직전).
/// **헌법 #5 정합**: await / Task.Delay / Thread.Sleep 전혀 없음. impactTick 비교만.
/// **헌법 #1**: Hp 변경은 이 System에서만 (deferred path). 클라는 S_HitResult 수신 후 표시만.
/// **사망 경로**: CombatSystem 즉시 데미지 경로와 동일 — S_EntityDeath + Normal respawn enqueue.
/// </summary>
internal sealed class DeferredDamageSystem
{
    // tick thread invariant — GameMap.Tick 단일 스레드에서만 R/W.
    readonly List<DeferredImpact> _queue = new();

    /// <summary>
    /// 지연 데미지 1건을 큐에 추가. P3(평타)/P4(썬더볼트)가 호출.
    /// impactTick = currentTick + delayTicks — 호출자가 계산해서 전달.
    /// </summary>
    internal void Enqueue(DeferredImpact impact) => _queue.Add(impact);

    /// <summary>
    /// 매 틱 호출. impactTick 도달 항목을 처리하고 큐에서 제거.
    /// 역방향 순회 — 제거 시 인덱스 어긋남 방지 (RespawnSystem.Process 동일 패턴).
    /// </summary>
    internal void Process(GameMap map, long tickNumber)
    {
        for (int i = _queue.Count - 1; i >= 0; i--)
        {
            DeferredImpact impact = _queue[i];
            if (tickNumber < impact.ImpactTick) continue;

            _queue.RemoveAt(i);

            EnemyEntity? target = map.GetEnemyById(impact.TargetEntityId);
            // 도착 전 사망/디스폰 — skip (헌법 #2 은퇴 ID 재사용 금지로 stale id 무효 자동 처리).
            if (target == null || target.IsDead) continue;

            // 피격 aggro — 도착 시점에 공격자 등록. Knight 즉시 경로(CombatSystem)와 대칭.
            // 후공(AggroOnSight=false) 적은 이 세팅이 유일한 Chase 트리거 → freeze 풀린 뒤 추격.
            // 평타·썬더볼트 모두 deferred 경유라 여기 한 곳에서 일관 처리. Boss는 Fsm=null이라 무해.
            target.TargetEntityId = impact.AttackerEntityId;

            target.Hp -= impact.Damage;

            S_HitResult hit = new S_HitResult
            {
                attackerEntityId = impact.AttackerEntityId,
                targetEntityId   = target.EntityId,
                damage           = impact.Damage,
                currentHp        = Math.Max(0, target.Hp),
                maxHp            = target.MaxHp,
                hitEffect        = impact.HitEffect,
            };
            map.BroadcastToAll(hit.Write());

            if (target.Hp <= 0)
            {
                S_EntityDeath death = new S_EntityDeath { entityId = target.EntityId };
                map.BroadcastToAll(death.Write());

                if (target.Kind == EnemyKind.Boss && !map.IsStageCleared)
                {
                    map.SetStageCleared();
                    S_StageClear stageClear = new S_StageClear { bossEntityId = target.EntityId };
                    map.BroadcastToAll(stageClear.Write());
                }
                map.RemoveEnemy(target.EntityId);
                if (target.Kind == EnemyKind.Normal)
                    map.EnqueueRespawn(target);
            }
        }
    }
}

/// <summary>
/// 지연 데미지 1건의 데이터. tick thread invariant — 생성 후 불변.
///
/// attackerEntityId: S_HitResult.attackerEntityId에 그대로 사용. 처음부터 포함 필수.
///   (S_HitResult PDL에 attackerEntityId 있음 — 나중에 추가하면 P3 struct 재수정 필요, plan-auditor 우려 D)
/// hitEffect: 0=근접, 1=투사체 도착, 2=낙뢰 — 클라 VFX 분기용.
/// </summary>
internal readonly struct DeferredImpact
{
    internal int AttackerEntityId { get; init; }
    internal int TargetEntityId   { get; init; }
    internal int Damage            { get; init; }
    internal long ImpactTick       { get; init; }
    internal byte HitEffect        { get; init; }
}

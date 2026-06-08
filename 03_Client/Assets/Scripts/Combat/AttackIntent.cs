#nullable enable
using Dawnholder.Client.Network;
using Shared.Protocol;
using UnityEngine;

namespace Dawnholder.Client.Combat
{
    // 공격 의도 송신 공유 로직 — 직업별 전략이 공통으로 사용.
    //
    // **클라 책임 = target 추천 + intent 송신만** (헌법 #1):
    //   데미지/range/cooldown 판정은 서버 전용 — 클라 자체 판정 X.
    //
    // **TargetingRangeSquared = 9.0f** — 클라 측 *타게팅 힌트* (3.0f 사거리의 제곱).
    //   어느 적을 C_Attack target으로 지명할지 결정하는 UX 용도.
    //   서버 권위 판정(AABB hitbox in CombatConstants)과 *의도적으로 분리*된 별개 개념 —
    //   서버가 최종 hit/miss 결정. 헌법 #1/#4 정합 — 밸런스 수식 복붙 X, 클라 UX 힌트.
    //   서버 AABB halfExtent(1.5) + 적 반경(0.5) ≈ 2 units 기준 TargetingRange 3.0f는 여유분 포함.
    //
    // **targetEntityId = 0 = 허공 sentinel** — entity id는 서버 등록 순서상 1+이므로 0은 안전.
    //   사거리 안 타겟이 없어도 C_Attack을 송신해 서버가 허공 스윙으로 처리.
    public static class AttackIntent
    {
        const float TargetingRangeSquared = 9.0f;

        // 가장 가까운 적을 타게팅해 C_Attack 송신. 세션/handshake 준비 안 됐으면 false.
        // targetEntityId = 타겟 entity id (없으면 0 = 허공 sentinel).
        // attackerClientTick = 마지막 S_Snapshot serverTick (lag comp 기준점).
        public static bool TrySend(Vector3 origin, out int targetEntityId)
        {
            targetEntityId = 0;

            UnityClientSession? session = UnityClientSession.Instance;
            if (session == null) return false;

            // 타겟은 *선택적 힌트* — 없으면 0으로 송신 (허공 스윙). 서버가 최종 hit/miss 결정.
            if (EnemyRegistry.Instance != null)
                EnemyRegistry.Instance.TryGetNearest(origin, TargetingRangeSquared, out targetEntityId);

            C_Attack pkt = new C_Attack
            {
                targetEntityId = targetEntityId,
                attackerClientTick = session.LastReceivedServerTick
            };
            session.SendIntent(pkt.Write());
            Debug.Log($"[Attack] → target entity {targetEntityId} clientTick={pkt.attackerClientTick}");
            return true;
        }
    }
}

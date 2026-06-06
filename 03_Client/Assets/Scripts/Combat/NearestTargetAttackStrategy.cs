#nullable enable
using Dawnholder.Client.Network;
using Shared.Protocol;
using UnityEngine;

namespace Dawnholder.Client.Combat
{
    // 가장 가까운 적을 타게팅하는 기본 공격 전략.
    //
    // **클라 책임 = target 추천 + intent 송신만** (헌법 #1):
    //   - 가장 가까운 enemy/boss → C_Attack { targetEntityId, attackerClientTick } 송신.
    //   - 데미지/range/cooldown *서버가 최종 검사* — 클라 자체 판정 X.
    //   - 자체 rate-limit 없음 (서버가 silent drop).
    //
    // **TargetingRangeSquared = 9.0f** — 클라 측 *타게팅 힌트* (3.0f 사거리의 제곱).
    //   어느 적을 C_Attack target으로 지명할지 결정하는 UX 용도.
    //   서버 권위 판정(AABB hitbox in CombatConstants)과 *의도적으로 분리*된 별개 개념 —
    //   서버가 최종 hit/miss 결정. 헌법 #1/#4 정합 — 밸런스 수식 복붙 X, 클라 UX 힌트.
    //   서버 AABB halfExtent(1.5) + 적 반경(0.5) ≈ 2 units 기준 TargetingRange 3.0f는 여유분 포함.
    public sealed class NearestTargetAttackStrategy : IAttackStrategy
    {
        const float TargetingRangeSquared = 9.0f;

        public void TryAttack(Vector3 origin)
        {
            UnityClientSession? session = UnityClientSession.Instance;
            if (session == null) return;
            if (EnemyRegistry.Instance == null) return;

            if (!EnemyRegistry.Instance.TryGetNearest(origin, TargetingRangeSquared, out int targetEntityId))
            {
                // 타게팅 범위 내 enemy 없음 — silent (서버가 어차피 최종 판정).
                return;
            }

            // attackerClientTick = 마지막으로 수신한 S_Snapshot의 serverTick (lag comp 기준점).
            // 서버 ProcessAttack이 이 tick으로 position history를 rewind해 hitbox 판정.
            // 검증 규칙: tick < 0 || > 현재서버tick || (현재서버tick - tick) > 4 → silent drop.
            // 첫 Snapshot 수신 전(= 0) 공격은 drop되지만 게임 극초반이라 실전 영향 없음.
            C_Attack pkt = new C_Attack
            {
                targetEntityId = targetEntityId,
                attackerClientTick = session.LastReceivedServerTick
            };
            session.SendIntent(pkt.Write());
            Debug.Log($"[Attack] → target entity {targetEntityId} clientTick={pkt.attackerClientTick}");
        }
    }
}

#nullable enable
using UnityEngine;

namespace Dawnholder.Client.Combat
{
    // 기사 근접 공격 전략 — AttackIntent.TrySend 래퍼.
    public sealed class KnightMeleeAttack : IAttackStrategy
    {
        // 3 units 사거리 — 서버 AABB halfExtent(1.5) + 적 반경(0.5) ≈ 2 units 기준 여유분 포함.
        public float TargetingRangeSquared => 9.0f;

        public bool TryAttack(Vector3 origin)
        {
            return AttackIntent.TrySend(origin, TargetingRangeSquared, out _);
        }
    }
}

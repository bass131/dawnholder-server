#nullable enable
using UnityEngine;

namespace Dawnholder.Client.Combat
{
    // 기사 근접 공격 전략 — AttackIntent.TrySend 래퍼.
    public sealed class KnightMeleeAttack : IAttackStrategy
    {
        public bool TryAttack(Vector3 origin)
        {
            return AttackIntent.TrySend(origin, out _);
        }
    }
}

#nullable enable
using UnityEngine;

namespace Dawnholder.Client.Combat
{
    [CreateAssetMenu(menuName = "Dawnholder/ClassConfig/Knight")]
    public sealed class KnightClassConfig : ClassConfig
    {
        public override IAttackStrategy CreateStrategy() => new KnightMeleeAttack();
    }
}

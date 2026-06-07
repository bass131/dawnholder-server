#nullable enable
using UnityEngine;

namespace Dawnholder.Client.Combat
{
    [CreateAssetMenu(menuName = "Dawnholder/ClassConfig/Mage")]
    public sealed class MageClassConfig : ClassConfig
    {
        [SerializeField] GameObject? _projectilePrefab;

        public override IAttackStrategy CreateStrategy() => new MageRangedAttack(_projectilePrefab, EffectAnchorOffset);
    }
}

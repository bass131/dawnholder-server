#nullable enable
using UnityEngine;

namespace Dawnholder.Client.Combat
{
    [CreateAssetMenu(menuName = "Dawnholder/ClassConfig/Mage")]
    public sealed class MageClassConfig : ClassConfig
    {
        // 투사체 prefab 필드 제거 — 선예측 스폰 폐지(M4.8 기둥1).
        // 투사체는 서버 확정(S_ProjectileLaunch) 수신 후 ProjectileLaunchHandler가 Resources.Load로 스폰.

        public override IAttackStrategy CreateStrategy() => new MageRangedAttack();
    }
}

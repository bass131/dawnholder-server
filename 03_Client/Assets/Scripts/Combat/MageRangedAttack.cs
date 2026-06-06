#nullable enable
using UnityEngine;

namespace Dawnholder.Client.Combat
{
    // 마법사 원거리 공격 전략 — 서버 intent 송신 + 투사체 시각 연출.
    //
    // 투사체 = 시각 연출 전용, 판정은 서버가 lag-comp로 이미 확정 (헌법 #1).
    //   도달 여부와 데미지는 무관 — 콜라이더/물리 없음.
    public sealed class MageRangedAttack : IAttackStrategy
    {
        readonly GameObject? _projectilePrefab;
        bool _warnedMissingPrefab;

        public MageRangedAttack(GameObject? projectilePrefab)
        {
            _projectilePrefab = projectilePrefab;
        }

        public void TryAttack(Vector3 origin)
        {
            if (!AttackIntent.TrySend(origin, out int targetId))
                return;

            if (_projectilePrefab == null)
            {
                if (!_warnedMissingPrefab)
                {
                    // Assets/Resources/ClassConfigs/ 의 MageClassConfig 에셋에서
                    // _projectilePrefab 필드에 투사체 prefab을 연결하세요.
                    Debug.LogWarning("[MageRangedAttack] _projectilePrefab 미연결 — 투사체 시각 생략. " +
                                     "MageClassConfig 에셋의 Projectile Prefab 필드를 채워주세요.");
                    _warnedMissingPrefab = true;
                }
                return;
            }

            // 타겟 Transform 조회 실패(직후 사망 race) 시 스폰 생략 — 방향 없는 투사체 방지.
            if (EnemyRegistry.Instance == null) return;
            if (!EnemyRegistry.Instance.TryGetTransform(targetId, out Transform? target)) return;

            GameObject proj = Object.Instantiate(_projectilePrefab, origin, Quaternion.identity);
            ProjectileVisual visual = proj.GetComponent<ProjectileVisual>()
                                     ?? proj.AddComponent<ProjectileVisual>();
            visual.Launch(target);
        }
    }
}

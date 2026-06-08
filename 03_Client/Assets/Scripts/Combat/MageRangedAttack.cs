#nullable enable
using Dawnholder.Client.Prediction;
using Dawnholder.Client.Rendering;
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

        public bool TryAttack(Vector3 origin)
        {
            if (!AttackIntent.TrySend(origin, out int targetId))
                return false;

            // 여기 이후는 전부 *투사체 시각* 분기 — C_Attack은 이미 송신됨.
            // 시각 생략 사유(prefab 미연결/타겟 race)가 있어도 공격 자체는 성립 →
            // commit window 시작을 위해 true 반환 (서버 AttackState와 정합).
            if (_projectilePrefab == null)
            {
                if (!_warnedMissingPrefab)
                {
                    Debug.LogWarning("[MageRangedAttack] _projectilePrefab 미연결 — 투사체 시각 생략. " +
                                     "MageClassConfig 에셋의 Projectile Prefab 필드를 채워주세요.");
                    _warnedMissingPrefab = true;
                }
                return true;
            }

            // 타겟 Transform 조회 (없으면 null — facing 방향 직진 폴백).
            Transform? spawnRoot = LocalPlayerMovement.Instance?.transform;
            Transform? target = null;
            int facing = 1;
            if (spawnRoot != null)
            {
                LocalPlayerMotion? motion = spawnRoot.GetComponent<LocalPlayerMotion>();
                if (motion != null) facing = motion.Facing;
            }
            if (targetId != 0 && EnemyRegistry.Instance != null)
                EnemyRegistry.Instance.TryGetTransform(targetId, out target);

            ProjectileSpawner.Spawn(_projectilePrefab, spawnRoot, target, facing);
            return true;
        }
    }
}

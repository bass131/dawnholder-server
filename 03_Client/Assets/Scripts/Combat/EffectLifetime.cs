#nullable enable
using UnityEngine;

namespace Dawnholder.Client.Combat
{
    // 이펙트 GameObject 수명 자동 파괴 컴포넌트.
    // BossAttackEffectSpawner가 이펙트 prefab에 없을 때 런타임 주입.
    // ProjectileVisual._maxLifetime 패턴과 동일 컨셉.
    public class EffectLifetime : MonoBehaviour
    {
        [SerializeField] float _lifetime = 1.5f;

        float _elapsed;

        void Update()
        {
            _elapsed += Time.deltaTime;
            if (_elapsed >= _lifetime)
                Destroy(gameObject);
        }
    }
}

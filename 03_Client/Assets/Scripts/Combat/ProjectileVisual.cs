#nullable enable
using UnityEngine;

namespace Dawnholder.Client.Combat
{
    // 투사체 시각 연출 전용 컴포넌트.
    //
    // 헌법 #1: 판정/데미지는 서버가 lag-comp로 이미 확정.
    //   이 컴포넌트의 도달 여부와 실제 데미지는 무관.
    //   물리/콜라이더/충돌 콜백 일절 없음 — 순수 시각.
    public class ProjectileVisual : MonoBehaviour
    {
        [SerializeField] float _speed = 10f;
        [SerializeField] float _maxLifetime = 2f;

        Transform? _target;
        Vector3 _direction;
        float _elapsed;
        SpriteRenderer? _sr;

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
        }

        public void Launch(Transform? target)
        {
            _target = target;
            if (target != null)
                _direction = (target.position - transform.position).normalized;
            else
                _direction = Vector3.right;
        }

        // 타겟 없이 방향 벡터 직접 지정 — facing 방향 허공 스윙 연출용.
        public void LaunchDirection(Vector3 direction)
        {
            _target = null;
            _direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector3.right;
        }

        void Update()
        {
            _elapsed += Time.deltaTime;
            if (_elapsed >= _maxLifetime)
            {
                Destroy(gameObject);
                return;
            }

            // 타겟 생존 시 방향 추적(호밍), 소멸 시 마지막 방향 직진.
            // 도달 판정 = 이번 프레임 이동량 기준 — 거리 임계값 방식은 한 프레임에
            // 타겟을 지나치면(overshoot) 영원히 도달 못 하고 주위에서 진동.
            float step = _speed * Time.deltaTime;
            if (_target != null)
            {
                Vector3 toTarget = _target.position - transform.position;
                float dist = toTarget.magnitude;
                if (dist <= step)
                {
                    Destroy(gameObject);
                    return;
                }
                _direction = toTarget / dist;
            }

            transform.position += _direction * step;

            // sprite flip — 이동 방향 기준.
            if (_sr != null && _direction.x != 0f)
                _sr.flipX = _direction.x < 0f;
        }
    }
}

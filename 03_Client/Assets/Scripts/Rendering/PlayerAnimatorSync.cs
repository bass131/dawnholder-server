using UnityEngine;

namespace Dawnholder.Client.Rendering
{
    // 캐릭터 이동 시각 wiring: LocalPlayerController(prediction)가 갱신한 transform.position
    // 변화를 감지해 Animator.SetBool("IsMoving", ...) + flipX 적용. 위치 변경 자체는
    // LocalPlayerController가 처리 (헌법 #1 prediction layer). 본 컴포넌트는 *시각만* 갱신.
    [RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
    public class PlayerAnimatorSync : MonoBehaviour
    {
        // PPU 64 기준 1픽셀의 1/16 — prediction jitter 무시.
        const float MoveEpsilon = 0.001f;

        Animator _anim;
        SpriteRenderer _sr;
        float _lastX;

        void Awake()
        {
            _anim = GetComponent<Animator>();
            _sr = GetComponent<SpriteRenderer>();
            _lastX = transform.position.x;
        }

        void LateUpdate()
        {
            float dx = transform.position.x - _lastX;
            bool moving = Mathf.Abs(dx) > MoveEpsilon;
            _anim.SetBool("IsMoving", moving);
            if (moving) _sr.flipX = (dx < 0f);
            _lastX = transform.position.x;
        }
    }
}

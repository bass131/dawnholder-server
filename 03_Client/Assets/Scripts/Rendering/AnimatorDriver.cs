#nullable enable
using Shared.GameData;
using UnityEngine;

namespace Dawnholder.Client.Rendering
{
    // IMotionState 공급원을 읽어 Animator + SpriteRenderer.flipX를 구동하는 공통 컴포넌트.
    // Animator / SpriteRenderer 모두 옵셔널 — enemy placeholder(Animator 없음)에서도 flipX만 동작.
    [DisallowMultipleComponent]
    public class AnimatorDriver : MonoBehaviour
    {
        // Phase 11 Animator Controller 셋업 시 이 이름으로 int 파라미터를 만들어야 함.
        // Shared.GameData.AnimState 값(0~5)을 그대로 매핑.
        public const string AnimStateParam = "AnimState";

        // sprite가 기본으로 왼쪽을 보고 그려졌으면 true → flip 기준 반전.
        // 우향 기본 sprite(Player)는 false, 좌향 기본(Mushroom/ToxicFrog placeholder)은 spawn 시 set.
        public bool SpriteDefaultFacesLeft;

        Animator? _anim;
        SpriteRenderer? _sr;
        IMotionState? _source;

        void Awake()
        {
            _anim = GetComponent<Animator>();
            _sr = GetComponent<SpriteRenderer>();
            _source = GetComponent<IMotionState>();
        }

        void LateUpdate()
        {
            if (_source == null) return;

            AnimState state = _source.CurrentAnimState;
            int facing = _source.Facing;

            // controller 미연결 Animator(11 전 RemotePlayer)에 파라미터 set 시 콘솔 경고 방지.
            if (_anim != null && _anim.runtimeAnimatorController != null)
                _anim.SetInteger(AnimStateParam, (int)state);

            if (_sr != null && facing != 0)
                _sr.flipX = (facing < 0) ^ SpriteDefaultFacesLeft;
        }
    }
}

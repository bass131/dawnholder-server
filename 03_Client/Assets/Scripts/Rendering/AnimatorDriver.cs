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

        // 공격 모션이 여러 개인 controller(Knight Attack0/1)만 이 int 파라미터를 가짐.
        // Attack 진입 순간 0/1 랜덤 — 시각 전용. 판정은 서버가 이미 확정(헌법 #1)이라 클라 랜덤 무해.
        public const string AttackVariantParam = "AttackVariant";

        // sprite가 기본으로 왼쪽을 보고 그려졌으면 true → flip 기준 반전.
        // 우향 기본 sprite(Player)는 false, 좌향 기본(Mushroom/ToxicFrog placeholder)은 spawn 시 set.
        public bool SpriteDefaultFacesLeft;

        Animator? _anim;
        SpriteRenderer? _sr;
        IMotionState? _source;
        AnimState _prevState;
        bool _variantChecked;
        bool _hasAttackVariant;

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
            {
                // Animator.parameters는 호출마다 배열 할당 → controller 확인 후 1회만 조회.
                if (!_variantChecked)
                {
                    _variantChecked = true;
                    foreach (var p in _anim.parameters)
                        if (p.name == AttackVariantParam) { _hasAttackVariant = true; break; }
                }

                if (_hasAttackVariant && state == AnimState.Attack && _prevState != AnimState.Attack)
                    _anim.SetInteger(AttackVariantParam, Random.Range(0, 2));

                _anim.SetInteger(AnimStateParam, (int)state);
            }
            _prevState = state;

            if (_sr != null && facing != 0)
                _sr.flipX = (facing < 0) ^ SpriteDefaultFacesLeft;
        }
    }
}

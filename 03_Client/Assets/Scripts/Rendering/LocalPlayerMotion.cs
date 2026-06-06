#nullable enable
using Dawnholder.Client.Prediction;
using Shared.GameData;
using UnityEngine;

namespace Dawnholder.Client.Rendering
{
    // 로컬 플레이어의 IMotionState 공급원 — prediction transform 델타로 Idle/Walk 도출.
    // ⚠️ LocalPlayer에 AnimatorDriver를 부착하면 PlayerAnimatorSync와 flipX가 이중 세팅됨 (11에서 교체/공존 결정).
    [DisallowMultipleComponent]
    public class LocalPlayerMotion : MonoBehaviour, IMotionState
    {
        const float MoveEpsilon = 0.001f;

        AnimState _animState = AnimState.Idle;
        int _facing = 1;

        // PlayerPredictor에 직접 의존하지 않고 LocalPlayerMovement에서 읽는 설계:
        // LocalPlayerMovement가 predictor를 private으로 감싸므로 transform 델타로 추론.
        float _lastX;

        public AnimState CurrentAnimState => _animState;
        public int Facing => _facing;

        void Awake()
        {
            _lastX = transform.position.x;
        }

        void LateUpdate()
        {
            float dx = transform.position.x - _lastX;
            bool moving = Mathf.Abs(dx) > MoveEpsilon;

            _animState = moving ? AnimState.Walk : AnimState.Idle;

            if (moving)
                _facing = dx > 0f ? 1 : -1;

            _lastX = transform.position.x;
        }
    }
}

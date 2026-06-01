#nullable enable
using Shared.GameData;
using UnityEngine;

namespace Dawnholder.Client.Rendering
{
    // 타인 플레이어의 IMotionState 공급원.
    // S_Snapshot.animState(서버 권위)를 그대로 노출 — 클라 추측 없음(헌법 #1).
    // Facing은 RemoteEntity가 보간한 transform.x 변화로 추론.
    [DisallowMultipleComponent]
    public class RemotePlayerMotion : MonoBehaviour, IMotionState
    {
        const float FacingEpsilon = 0.001f;

        AnimState _animState;
        int _facing = 1;
        float _lastX;

        public AnimState CurrentAnimState => _animState;
        public int Facing => _facing;

        void Awake()
        {
            _lastX = transform.position.x;
        }

        // RemoteEntityRegistry.UpdateSnapshot 경로에서 호출.
        public void SetAnimState(byte raw)
        {
            _animState = (AnimState)raw;
        }

        void LateUpdate()
        {
            float dx = transform.position.x - _lastX;
            if (Mathf.Abs(dx) > FacingEpsilon)
                _facing = dx > 0f ? 1 : -1;
            _lastX = transform.position.x;
        }
    }
}

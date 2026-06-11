#nullable enable
using Shared.GameData;
using UnityEngine;

namespace Dawnholder.Client.Rendering
{
    // 타인 플레이어의 IMotionState 공급원.
    // animState — S_Snapshot.animState(서버 권위) 그대로 노출.
    // Facing   — 서버 권위 vx 부호로 결정. vx≈0(정지)이면 마지막 facing 유지(헌법 #1 — 클라 추측 없음).
    [DisallowMultipleComponent]
    public class RemotePlayerMotion : MonoBehaviour, IMotionState
    {
        AnimState _animState;
        int _facing = 1;

        public AnimState CurrentAnimState => _animState;
        public int Facing => _facing;

        // RemoteEntityRegistry.UpdateSnapshot 경로에서 호출.
        public void SetAnimState(byte raw)
        {
            _animState = (AnimState)raw;
        }

        // RemoteEntityRegistry.UpdateSnapshot 경로에서 호출.
        // vx≈0(정지) → facing 유지 → 보간 노이즈로 인한 좌우 jitter 차단.
        public void SetVelocityX(float vx)
        {
            if (Mathf.Abs(vx) > MotionConstants.RemoteFacingVelocityEpsilon)
                _facing = vx > 0f ? 1 : -1;
        }
    }
}

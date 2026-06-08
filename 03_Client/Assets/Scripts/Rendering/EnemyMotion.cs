#nullable enable
using Shared.GameData;
using UnityEngine;

namespace Dawnholder.Client.Rendering
{
    // enemy/boss의 IMotionState 공급원.
    // S_EntityState.animState(서버 권위)를 노출 — state 필드가 아님(AI FSM 상태라 시각 미사용).
    // Facing은 RemoteEntity가 보간한 transform.x 변화로 추론.
    [DisallowMultipleComponent]
    public class EnemyMotion : MonoBehaviour, IMotionState
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

        // EnemyRegistry.UpdatePosition 경로에서 호출.
        // animState = S_EntityState.animState (시각용 필드). state(AI FSM)는 전달하지 않음.
        public void SetAnimState(byte raw)
        {
            _animState = (AnimState)raw;
        }

        // S_EntityDeath 수신 시 클라가 직접 호출 — 서버가 더이상 animState를 보내지 않으므로
        // Death 클립 전환을 클라 측에서 강제 주입.
        public void ForceDeathState()
        {
            _animState = AnimState.Death;
        }

        void LateUpdate()
        {
            float dx = transform.position.x - _lastX;
            if (Mathf.Abs(dx) > FacingEpsilon)
            {
                int moveFacing = dx > 0f ? 1 : -1;
                // 피격 중 넉백은 공격자 반대 방향 → 역방향이 공격자.
                _facing = _animState == AnimState.Hit ? -moveFacing : moveFacing;
            }
            _lastX = transform.position.x;
        }
    }
}

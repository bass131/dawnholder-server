#nullable enable
using Dawnholder.Client.Prediction;
using Shared.GameData;
using UnityEngine;

namespace Dawnholder.Client.Rendering
{
    // enemy/boss의 IMotionState 공급원.
    // S_EntityState.animState(서버 권위)를 노출 — state 필드가 아님(AI FSM 상태라 시각 미사용).
    // Facing은 RemoteEntity가 보간한 transform.x 변화로 추론.
    // 예외: 정지 상태로 공격 중(보스 telegraph/strike)이면 이동이 없어 facing이 멈추므로
    //   공격 대상(로컬 플레이어) 쪽을 바라본다 — facing은 시각 전용(판정 AABB는 서버에서 좌우 대칭).
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
            else if (_animState == AnimState.Attack)
            {
                // 정지 + 공격 중: 이동이 없어 facing이 옛 추격 방향에 멈춤 → 대상을 바라보게 보정.
                LocalPlayerMovement? lp = LocalPlayerMovement.Instance;
                if (lp != null)
                {
                    float pdx = lp.transform.position.x - transform.position.x;
                    if (Mathf.Abs(pdx) > FacingEpsilon)
                        _facing = pdx > 0f ? 1 : -1;
                }
            }
            _lastX = transform.position.x;
        }
    }
}

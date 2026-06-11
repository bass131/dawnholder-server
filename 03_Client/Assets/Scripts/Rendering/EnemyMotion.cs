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
    //   서버 S_EnemyAttack.targetId 기반으로 공격 대상 Transform을 받아 그쪽을 바라본다.
    //   facing은 시각 전용(판정 AABB는 서버에서 좌우 대칭).
    [DisallowMultipleComponent]
    public class EnemyMotion : MonoBehaviour, IMotionState
    {
        const float FacingEpsilon = MotionConstants.FacingEpsilon;

        AnimState _animState;
        int _facing = 1;
        float _lastX;

        // S_EnemyAttack 수신 시 EnemyRegistry.SetAttackTarget이 주입 — null이면 폴백으로 기존 _facing 유지.
        Transform? _attackTarget;

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

        // S_EnemyAttack 수신 시 EnemyRegistry 경유로 주입 — 서버 targetId 기반 대상 Transform.
        // 공격 애니메이션이 끝나면(AnimState가 Attack 아닐 때) 다음 LateUpdate 틱에서 자연히 무시됨.
        public void SetAttackTarget(Transform? target)
        {
            _attackTarget = target;
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
                // 정지 + 공격 중: 이동이 없어 facing이 옛 추격 방향에 멈춤 → 서버 targetId 기반 대상을 바라봄.
                // _attackTarget이 null이면(패킷 미도착/대상 소멸) 기존 _facing을 그대로 유지.
                if (_attackTarget != null)
                {
                    float pdx = _attackTarget.position.x - transform.position.x;
                    if (Mathf.Abs(pdx) > FacingEpsilon)
                        _facing = pdx > 0f ? 1 : -1;
                }
            }
            _lastX = transform.position.x;
        }
    }
}

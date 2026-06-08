#nullable enable
using Dawnholder.Client.Prediction;
using Shared.GameData;
using UnityEngine;

namespace Dawnholder.Client.Rendering
{
    // 로컬 플레이어의 IMotionState 공급원 — hybrid 방식.
    //
    // Idle/Walk/Jump = 로컬 prediction 즉각 반영 (반응성 우선).
    // Attack/Hit/Death = 서버 animState 우선 (클라가 추측 불가 — 헌법 #1.
    //   서버가 latch 8틱으로 지속 보장).
    [DisallowMultipleComponent]
    public class LocalPlayerMotion : MonoBehaviour, IMotionState
    {
        const float MoveEpsilon = 0.001f;

        AnimState _animState = AnimState.Idle;
        AnimState _serverState = AnimState.Idle;
        int _facing = 1;
        float _lastX;

        LocalPlayerMovement? _movement;

        public AnimState CurrentAnimState => _animState;
        public int Facing => _facing;

        void Awake()
        {
            _lastX = transform.position.x;
            _movement = GetComponent<LocalPlayerMovement>(); // 같은 GameObject에 박힌다고 가정.
        }

        // SnapshotHandler가 본인 분기에서 호출 — 서버 animState 전달.
        public void SetServerAnimState(byte raw)
        {
            _serverState = (AnimState)raw;
        }

        // 우선순위 순수 함수 — serverState가 Attack/Hit/Death면 그대로, 아니면 예측 상태로 도출.
        public static AnimState ResolveAnimState(AnimState serverState, bool onGround, bool moving)
        {
            if (serverState == AnimState.Attack
                || serverState == AnimState.Hit
                || serverState == AnimState.Death)
                return serverState;

            if (!onGround) return AnimState.Jump;
            if (moving) return AnimState.Walk;
            return AnimState.Idle;
        }

        void LateUpdate()
        {
            float dx = transform.position.x - _lastX;
            bool moving = Mathf.Abs(dx) > MoveEpsilon;

            // Movement 컴포넌트 없으면 onGround=true 취급 (테스트 씬 안전).
            bool onGround = _movement != null ? _movement.OnGround : true;

            _animState = ResolveAnimState(_serverState, onGround, moving);

            if (moving)
            {
                // 피격 중엔 넉백이 *공격자 반대 방향*으로 날아가므로, 이동(넉백)의 반대로 바라보면
                // 공격자를 향함 — "맞은 쪽을 쳐다보는" 자연스러운 반응. (별도 배선 불필요)
                if (_serverState == AnimState.Hit)
                    _facing = dx > 0f ? -1 : 1;
                else
                    _facing = dx > 0f ? 1 : -1;
            }

            _lastX = transform.position.x;
        }
    }
}

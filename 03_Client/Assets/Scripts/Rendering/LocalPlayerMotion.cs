#nullable enable
using Dawnholder.Client.Prediction;
using Shared.GameData;
using UnityEngine;

namespace Dawnholder.Client.Rendering
{
    // 로컬 플레이어의 IMotionState 공급원 — hybrid 방식.
    //
    // Idle/Walk/Jump = 로컬 prediction 즉각 반영 (반응성 우선).
    // Attack = commit window 동안 로컬 선예측 (서버 확인 전 스윙 모션 즉시 재생).
    //   서버 Attack animState가 도착하면 이어받아 latch 연장 — 예측보다 서버 window가 길어도 OK.
    // Hit/Death = 서버 animState 우선 (클라가 예측 불가 — 헌법 #1).
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

        // 공격 입력 시 타겟 방향으로 facing 강제 — LateUpdate가 commit window 동안 이동 facing을
        // 억제(localAttackPredicted 가드)하므로 이 값이 스윙 내내 유지됨. 잔여 이동(dx≠0)이 있어도 불변.
        // 로컬 연출 전용 (헌법 #1 — 서버 FacingDir 변경은 서버 측 별도).
        public void FaceToward(float targetX)
        {
            float d = targetX - transform.position.x;
            if (Mathf.Abs(d) > MoveEpsilon)
                _facing = d > 0f ? 1 : -1;
        }

        // 우선순위 순수 함수 — serverState Hit/Death 최우선, 그 다음 로컬 Attack 선예측, 나머지 예측.
        // localAttackPredicted: NotifyAttack() 후 commit window 잔여가 있음 (Movement.IsMovementLocked 거울).
        // - 보수적 방향: 로컬 Attack 예측은 "서버보다 먼저 잠금"이라 오예측해도 window 만료로 자연 복구.
        // - 서버 Attack 도착 전에도 모션 재생 → 반응성 향상 (rubber-band 0 유지).
        public static AnimState ResolveAnimState(AnimState serverState, bool localAttackPredicted, bool localChannelPredicted, bool onGround, bool moving)
        {
            if (serverState == AnimState.Hit || serverState == AnimState.Death)
                return serverState;

            // 스킬 시전 선예측 — Attack 스윙보다 우선(같은 commit window라 localAttackPredicted도 true).
            //   서버는 시전 중 Attack을 안 보내므로(SkillSystem이 AttackState 미진입) 이 로컬 예측이 유일한 시전 모션.
            if (localChannelPredicted)
                return AnimState.Channeling;

            // 로컬 commit window OR 서버 Attack 중 Attack 우선.
            if (localAttackPredicted || serverState == AnimState.Attack)
                return AnimState.Attack;

            if (!onGround) return AnimState.Jump;
            if (moving) return AnimState.Walk;
            return AnimState.Idle;
        }

        void LateUpdate()
        {
            float dx = transform.position.x - _lastX;
            bool moving = Mathf.Abs(dx) > MoveEpsilon;

            // Movement 컴포넌트 없으면 onGround=true, 로컬 attack 예측=false 취급 (테스트 씬 안전).
            bool onGround = _movement != null ? _movement.OnGround : true;
            bool localAttackPredicted = _movement != null && _movement.CommitWindowRemaining > 0f;
            bool localChannelPredicted = _movement != null && _movement.IsChannelingWindow;

            _animState = ResolveAnimState(_serverState, localAttackPredicted, localChannelPredicted, onGround, moving);

            // facing 우선순위: 피격 넉백 > 공격 타겟(FaceToward) > 이동 방향.
            //   공격 commit window 중엔 이동 facing을 억제 — 안 그러면 공격 누른 프레임의 잔여 이동(dx≠0)이
            //   FaceToward가 잡아둔 타겟 방향을 덮어써, 이동 반대편 적을 칠 때 스윙 내내 반대로 고정됨.
            //   (source-gating은 다음 Update부터 moveX=0이라 한두 프레임 dx가 남는다 = 한 프레임이면 충분히 뒤집힘.)
            if (moving)
            {
                // 피격 중엔 넉백이 *공격자 반대 방향*으로 날아가므로, 이동(넉백)의 반대로 바라보면
                // 공격자를 향함 — "맞은 쪽을 쳐다보는" 자연스러운 반응. (별도 배선 불필요)
                if (_serverState == AnimState.Hit)
                    _facing = dx > 0f ? -1 : 1;
                else if (!localAttackPredicted)
                    _facing = dx > 0f ? 1 : -1;
            }

            _lastX = transform.position.x;
        }
    }
}

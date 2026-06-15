#nullable enable
using Dawnholder.Client.Audio;
using Dawnholder.Client.Prediction;
using Shared.GameData;
using UnityEngine;

namespace Dawnholder.Client.Rendering
{
    // 로컬 플레이어 발소리/착지음 — 코드 구동 (애니메이션 이벤트 .anim 편집 회피, 무인 견고).
    // Walk 상태 + 접지 중이면 일정 간격 발소리. 접지 상승엣지(공중→착지)에 착지음.
    // 순수 표현 (헌법 #1). LocalPlayerMotion/Movement와 같은 GameObject에 부착 전제.
    [DisallowMultipleComponent]
    public class FootstepTicker : MonoBehaviour
    {
        const float StepInterval = 0.32f;

        LocalPlayerMotion? _motion;
        LocalPlayerMovement? _movement;
        float _stepTimer;
        bool _wasGrounded = true;

        void Awake()
        {
            _motion = GetComponent<LocalPlayerMotion>();
            _movement = GetComponent<LocalPlayerMovement>();
        }

        void Update()
        {
            if (_motion == null) return;

            bool grounded = _movement == null || _movement.OnGround;

            if (grounded && !_wasGrounded)
                AudioManager.Instance?.PlaySfx(SoundKeys.JumpLand);
            _wasGrounded = grounded;

            if (grounded && _motion.CurrentAnimState == AnimState.Walk)
            {
                _stepTimer += Time.deltaTime;
                if (_stepTimer >= StepInterval)
                {
                    _stepTimer = 0f;
                    AudioManager.Instance?.PlaySfx(SoundKeys.Footstep);
                }
            }
            else
            {
                _stepTimer = StepInterval;  // 멈췄다 다시 걸으면 첫 걸음 즉시
            }
        }
    }
}

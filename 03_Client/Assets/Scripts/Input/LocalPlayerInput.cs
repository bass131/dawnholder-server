#nullable enable
using Dawnholder.Client.Combat;
using Dawnholder.Client.Prediction;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dawnholder.Client.Input
{
    // 입력 번역 전담 컴포넌트 — Unity 입력 → 게임 의도 번역.
    //
    // **책임 1개**: Unity Input System 콜백을 받아 LocalPlayerMovement / IAttackStrategy에 전달.
    //   상태 보유 최소 — 입력 번역만.
    //
    // **콜백 wire**: PlayerInput component의 Behavior=Send Messages 모드에서
    //   OnMove/OnJump/OnAttack 메서드명이 자동 wire됨.
    [RequireComponent(typeof(PlayerInput))]
    [RequireComponent(typeof(LocalPlayerMovement))]
    public class LocalPlayerInput : MonoBehaviour
    {
        // Phase 05 ClassConfig SO가 직업별 공격 전략을 주입 예정.
        // 현재는 Awake에서 NearestTargetAttackStrategy 임시 고정.
        IAttackStrategy _attackStrategy = null!;

        LocalPlayerMovement _movement = null!;

        void Awake()
        {
            _movement = GetComponent<LocalPlayerMovement>();
            _attackStrategy = new NearestTargetAttackStrategy();
        }

        // Input System "Move" 액션 콜백.
        void OnMove(InputValue value)
        {
            Vector2 raw = value.Get<Vector2>();
            _movement.SetMoveX(EncodeInputX(raw.x));
        }

        // "Jump" 액션 콜백 — 클라 에지 ("started" phase만 캡처).
        // PlayerInput component의 Behavior=Send Messages 모드에서 이 메서드명이 자동 wire.
        // 송신 cycle 전에 다시 누르면 같은 에지로 합쳐짐 (정상 — cadence별 1 점프).
        //
        // 공중 점프 차단은 점프 입력 *시점* OnGround 검사 (착지 직후 재점프 OK, 공중 점프 차단).
        // cadence 시점에 검사하면 Predict 후 OnGround=false 박혀서 지면 점프도 차단됨.
        // 헌법 #1 영향 X — 서버가 어차피 권위적으로 재검증, 본 게이트는 UX + 송신 절감용.
        void OnJump(InputValue value)
        {
            if (value.isPressed && _movement.OnGround)
                _movement.RequestJump();
        }

        // "Attack" 액션 콜백 (Space 또는 좌클릭) — down 에지만 처리.
        void OnAttack(InputValue value)
        {
            if (!value.isPressed) return; // up edge 무시 — down 시점 한 번만.
            _attackStrategy.TryAttack(transform.position);
        }

        // Vector2(아날로그 가능) → sbyte(-1/0/1) 변환.
        // 임계값 0.5 — 게임패드 아날로그 스틱 미세 흔들림 차단.
        static sbyte EncodeInputX(float x)
        {
            if (x > 0.5f) return 1;
            if (x < -0.5f) return -1;
            return 0;
        }
    }
}

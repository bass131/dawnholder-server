using UnityEngine;
using UnityEngine.InputSystem;

namespace Dawnholder.Client.Input
{
    // Phase 01 (M2): 오프라인 로컬 이동. Unity 환경 검증용.
    // 네트워크 코드 없음 — 입력값을 그대로 transform에 적용.
    // Phase 04 이후 transform 직접 조작은 제거되고 prediction 모듈로 대체된다.
    //
    // 부착 방법:
    //   1) 같은 GameObject에 PlayerInput 컴포넌트 추가
    //   2) Actions = Assets/InputSystem_Actions.inputactions
    //   3) Default Map = "Player"
    //   4) Behavior = "Send Messages"  ← OnMove(InputValue)가 자동 호출됨
    [RequireComponent(typeof(PlayerInput))]
    public class LocalPlayerController : MonoBehaviour
    {
        [SerializeField, Tooltip("초당 이동 속도 (units/sec)")]
        float moveSpeed = 5f;

        Vector2 _moveInput;

        // PlayerInput Behavior=SendMessages가 "Move" 액션 → "OnMove" 메서드로 dispatch.
        void OnMove(InputValue value)
        {
            _moveInput = value.Get<Vector2>();
        }

        void Update()
        {
            // 좌우만 사용. Y는 Phase 07(점프)에서 다룬다.
            Vector3 delta = new Vector3(_moveInput.x, 0f, 0f) * moveSpeed * Time.deltaTime;
            transform.position += delta;
        }
    }
}

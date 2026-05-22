using UnityEngine;
using UnityEngine.InputSystem;

namespace Dawnholder.Client.Gameplay
{
    /// <summary>
    /// NPC 인터랙션 컴포넌트 (M3.8 Phase 04 — 캡스톤 1 데모용).
    /// 플레이어가 NPC 옆 trigger 안에서 E 키 누르면 NpcDialogPanel.Show(dialogText) 호출.
    ///
    /// **헌법 #1 (Server Authority)**: 본 컴포넌트 = 클라 단독 hardcoded 텍스트, 서버 패킷 X.
    /// NPC 위치/존재는 *서버 spawn 데이터* 차원 (M6 길드 진입 시 정식화).
    ///
    /// **요구 컴포넌트**: BoxCollider2D (isTrigger=true), 플레이어는 "Player" 태그 박혀있어야 함.
    ///
    /// **새 InputSystem 활용** (헌법 03_Client/CLAUDE.md "새 Input System 패키지 사용. 레거시 Input.GetKey 금지" 정합).
    /// 직접 InputAction 박음 (Inspector reference 없이 작동 = 데모용 단순) — 정식 패턴은 PauseMenuController.cs 정합.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class NpcInteractable : MonoBehaviour
    {
        [SerializeField, TextArea(2, 5)]
        string dialogText = "보스가 마을을 위협하고 있어요. 도와주세요!\n사냥터는 오른쪽에 있습니다.";

        [SerializeField] string playerTag = "Player";

        bool _isPlayerNear;
        InputAction _interactAction;

        void Awake()
        {
            _interactAction = new InputAction("NpcInteract", InputActionType.Button, binding: "<Keyboard>/e");
            _interactAction.performed += OnInteractPerformed;
        }

        void OnEnable() => _interactAction?.Enable();
        void OnDisable() => _interactAction?.Disable();

        void OnDestroy()
        {
            if (_interactAction != null)
            {
                _interactAction.performed -= OnInteractPerformed;
                _interactAction.Dispose();
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag(playerTag))
            {
                _isPlayerNear = true;
            }
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag(playerTag))
            {
                _isPlayerNear = false;
                if (NpcDialogPanel.IsShown) NpcDialogPanel.Hide();
            }
        }

        void OnInteractPerformed(InputAction.CallbackContext _)
        {
            if (!_isPlayerNear) return;
            if (NpcDialogPanel.IsShown) return; // 이미 열려있으면 NpcDialogPanel이 닫기 처리
            NpcDialogPanel.Show(dialogText);
        }
    }
}

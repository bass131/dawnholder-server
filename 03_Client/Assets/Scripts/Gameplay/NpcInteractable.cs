using Dawnholder.Client.Audio;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Dawnholder.Client.Gameplay
{
    /// <summary>
    /// NPC 인터랙션 컴포넌트. 플레이어가 NPC 옆 trigger 안에서 E 키 누르면 NpcDialogPanel.Show(dialogText, portrait) 호출.
    ///
    /// **헌법 §1**: 클라 단독 hardcoded 텍스트, 서버 패킷 X.
    ///
    /// **요구 컴포넌트**: BoxCollider2D (isTrigger=true), 플레이어는 "Player" 태그 박혀있어야 함.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class NpcInteractable : MonoBehaviour
    {
        [FormerlySerializedAs("dialogText")]
        [SerializeField, TextArea(2, 5)]
        string _dialogText = "보스가 마을을 위협하고 있어요. 도와주세요!\n사냥터는 오른쪽에 있습니다.";

        // 영호 Phase 05 조정 지점: 각 NPC prefab에서 Inspector로 초상화 sprite 배정.
        [SerializeField] Sprite? _portrait;

        [FormerlySerializedAs("playerTag")]
        [SerializeField] string _playerTag = "Player";

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
            if (other.CompareTag(_playerTag))
            {
                _isPlayerNear = true;
            }
        }

        void OnTriggerStay2D(Collider2D other)
        {
            // OnTriggerEnter 못 잡힌 케이스 보완 (처음부터 trigger 안에 있는 경우).
            if (!_isPlayerNear && other.CompareTag(_playerTag))
            {
                _isPlayerNear = true;
            }
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag(_playerTag))
            {
                _isPlayerNear = false;
                if (NpcDialogPanel.IsShown)
                {
                    AudioManager.Instance?.PlaySfx(SoundKeys.PanelOpen);
                    NpcDialogPanel.Hide();
                }
            }
        }

        void OnInteractPerformed(InputAction.CallbackContext _)
        {
            if (!_isPlayerNear) return;
            if (NpcDialogPanel.IsShown) return;
            AudioManager.Instance?.PlaySfx(SoundKeys.PanelOpen);
            NpcDialogPanel.Show(_dialogText, _portrait);
        }
    }
}

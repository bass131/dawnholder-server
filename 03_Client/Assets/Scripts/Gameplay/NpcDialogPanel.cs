using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Dawnholder.Client.Gameplay
{
    /// <summary>
    /// NPC 대화 박스 UI (M3.8 Phase 04 — 캡스톤 1 데모용 단순 텍스트 출력).
    ///
    /// **클라 단독 hardcoded** — 서버 패킷 X. PRD MVP 제외 항목 "퀘스트/NPC" 정합 (캡스톤 데모용 흡수,
    /// 본 마감 후 M6 길드 진입 시 정식화).
    ///
    /// **헌법 #1 (Server Authority)**: 본 패널은 *상태 변경 없음* (텍스트 출력만) → 클라 단독 OK.
    /// 만약 *퀘스트 진행도* / *보상 지급* 같은 상태 변경이면 서버 권위 의무.
    ///
    /// **위치 = `Scripts/Gameplay/`** (plan-auditor 결함 봉합 결정, `Scripts/UI/`는 정유현 영역 침범 차단).
    ///
    /// **새 InputSystem 활용** (헌법 03_Client/CLAUDE.md "레거시 Input.GetKey 금지" 정합).
    /// ESC 또는 E 재입력 → Hide. 직접 InputAction 박음 (Inspector reference 없이 작동).
    ///
    /// **Static Show 패턴** — Scene 안 인스턴스 1개 가정 (Gameplay.unity에 비활성 상태로 배치).
    /// </summary>
    public class NpcDialogPanel : MonoBehaviour
    {
        static NpcDialogPanel _instance;

        [SerializeField] GameObject root;          // 본 패널 GameObject (활성/비활성 토글)
        [SerializeField] Text dialogText;          // 대화 텍스트 컴포넌트
        [SerializeField] string defaultCloseHint = "[E] 또는 [ESC] — 닫기";
        [SerializeField] Text closeHintText;       // 닫기 안내 (옵션)

        bool _isShown;
        InputAction _closeAction;

        void Awake()
        {
            _instance = this;
            if (root != null) root.SetActive(false);
            if (closeHintText != null) closeHintText.text = defaultCloseHint;

            _closeAction = new InputAction("NpcDialogClose", InputActionType.Button);
            _closeAction.AddBinding("<Keyboard>/escape");
            _closeAction.AddBinding("<Keyboard>/e");
            _closeAction.performed += OnClosePerformed;
        }

        void OnDestroy()
        {
            if (_closeAction != null)
            {
                _closeAction.performed -= OnClosePerformed;
                _closeAction.Disable();
                _closeAction.Dispose();
            }
            if (_instance == this) _instance = null;
        }

        void OnClosePerformed(InputAction.CallbackContext _)
        {
            if (_isShown) Hide();
        }

        /// <summary>대화 박스 표시. 이미 열려있으면 silent skip (중복 표시 가드).</summary>
        public static void Show(string text)
        {
            if (_instance == null)
            {
                Debug.LogWarning("[NpcDialogPanel] Instance is null — Gameplay.unity에 prefab 배치 누락 가능");
                return;
            }
            if (_instance._isShown) return;
            _instance._isShown = true;
            if (_instance.dialogText != null) _instance.dialogText.text = text;
            if (_instance.root != null) _instance.root.SetActive(true);
            _instance._closeAction?.Enable();
        }

        public static void Hide()
        {
            if (_instance == null) return;
            _instance._isShown = false;
            if (_instance.root != null) _instance.root.SetActive(false);
            _instance._closeAction?.Disable();
        }

        public static bool IsShown => _instance != null && _instance._isShown;
    }
}

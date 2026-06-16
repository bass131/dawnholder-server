using Dawnholder.Client.Audio;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace Dawnholder.Client.UI
{
    /// <summary>
    /// 일시정지 메뉴 핸들러. ESC 토글로 메뉴 표시/숨김 + Time.timeScale 0/1 전환.
    /// 재개 / 메인 메뉴로 / 게임 종료 3 버튼 처리.
    ///
    /// **헌법 #1 (Server Authority)**: Time.timeScale 0는 *본인 클라만* 정지합니다.
    /// 서버 권위 타임라인엔 영향 없음 — 멀티 게임에서 다른 플레이어는 계속 움직이고,
    /// 본인은 메뉴를 *표시*만 합니다. 서버에 일시정지 신호 보내지 X.
    ///
    /// **InputAction 참조**: <see cref="InputActionReference"/>로 외부(InputSystem_Actions
    /// asset)의 액션을 참조. OnEnable/OnDisable에서 Enable/Disable + performed 콜백 토글.
    /// </summary>
    public class PauseMenuController : MonoBehaviour
    {
        [Header("Canvas")]
        [Tooltip("이 컨트롤러가 토글할 PauseMenu 루트. 시작 시 SetActive(false)여야 정상.")]
        [FormerlySerializedAs("pauseCanvas")]
        [SerializeField] GameObject _pauseCanvas;

        [Header("Input")]
        [Tooltip("InputSystem_Actions의 UI/TogglePauseMenu 액션 참조 (Binding: <Keyboard>/escape).")]
        [FormerlySerializedAs("togglePauseAction")]
        [SerializeField] InputActionReference _togglePauseAction;

        [Header("Scenes")]
        [FormerlySerializedAs("mainMenuSceneName")]
        [SerializeField] string _mainMenuSceneName = "MainMenu";

        bool _isPaused;

        void OnEnable()
        {
            if (_togglePauseAction != null && _togglePauseAction.action != null)
            {
                _togglePauseAction.action.performed += OnTogglePerformed;
                _togglePauseAction.action.Enable();
            }
            else
            {
                Debug.LogWarning("[PauseMenu] OnEnable — togglePauseAction is NULL. ESC will not work.");
            }
        }

        void OnDisable()
        {
            if (_togglePauseAction != null && _togglePauseAction.action != null)
            {
                _togglePauseAction.action.performed -= OnTogglePerformed;
                _togglePauseAction.action.Disable();
            }
        }

        void OnTogglePerformed(InputAction.CallbackContext _) => Toggle();

        public void Toggle()
        {
            _isPaused = !_isPaused;
            if (_pauseCanvas != null)
            {
                _pauseCanvas.SetActive(_isPaused);
                if (_isPaused) AudioManager.Instance?.PlaySfx(SoundKeys.PanelOpen);
                else           AudioManager.Instance?.PlaySfx(SoundKeys.PanelClose);
            }
            // timeScale은 *Realtime* 기반 입력(InputSystem)은 영향 X — ESC 재누름으로 닫기 보장.
            Time.timeScale = _isPaused ? 0f : 1f;
        }

        public void OnResumeClicked()
        {
            AudioManager.Instance?.PlaySfx(SoundKeys.ButtonClick);
            if (!_isPaused) return;
            Toggle();
        }

        public void OnMainMenuClicked()
        {
            AudioManager.Instance?.PlaySfx(SoundKeys.ButtonClick);
            // 함정 방지: timeScale 0인 상태로 씬 로드하면 새 씬도 정지된 채 로드됨.
            Time.timeScale = 1f;
            _isPaused = false;

            // SceneTransition Singleton 경유(페이드). 단 에디터에서 게임플레이 씬 직접 Play
            // 시엔 MainMenu의 FadeCanvas가 생성된 적 없어 Instance == null → 직접 LoadScene fallback.
            if (SceneTransition.Instance != null)
                SceneTransition.Instance.LoadScene(_mainMenuSceneName);
            else
                SceneManager.LoadScene(_mainMenuSceneName);
        }

        public void OnQuitClicked()
        {
            AudioManager.Instance?.PlaySfx(SoundKeys.ButtonClick);
            Debug.Log("[PauseMenu] Quit clicked");
            Time.timeScale = 1f;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Dawnholder.Client.Network;

namespace Dawnholder.Client.UI
{
    /// <summary>
    /// 메인 메뉴 버튼 핸들러. MainMenu 씬의 "시작" / "종료" 버튼 onClick에 연결.
    ///
    /// **헌법 #1 (Server Authority)**: 시작 버튼은 *씬 로드* + *서버 가용성 점검*만 트리거.
    /// 권위 상태는 건드리지 않음 (ConnectionProbe로 짧은 TCP probe → close,
    /// 게임 본 connection은 Town 씬의 GameEntryPoint가 박음).
    ///
    /// **SceneTransition Singleton 경유** (페이드). 직접 SceneManager 호출 X.
    ///
    /// **서버 가용성 게이트**:
    /// - InputField에 서버 주소 박음. 기본값 = 127.0.0.1
    /// - Start 버튼 → ConnectionProbe.TryConnect → 성공 시 PlayerPrefs 저장 + CharacterSelect 로드
    /// - 실패 시 errorMessageText에 오류 표시 + Scene 진입 차단
    /// - 중복 클릭 차단 (probe 진행 중엔 버튼 비활성)
    ///
    /// **MainMenu 복귀 시 Disconnect (ADR-027)**:
    /// Awake에서 NetworkService.Disconnect()를 호출합니다.
    /// NetworkService는 PersistentServices 프리팹 소속이라 항상 존재하지만
    /// 에디터 씬 단독 Play 방어를 위해 null-safe(?.) 호출로 처리.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("서버 연결 게이트 (M3.8 Phase 05 5-B)")]
        [FormerlySerializedAs("serverHostInputField")]
        [SerializeField] TMP_InputField _serverHostInputField;
        [FormerlySerializedAs("errorMessageText")]
        [SerializeField] TMP_Text _errorMessageText;
        [FormerlySerializedAs("startButton")]
        [SerializeField] Button _startButton;
        [FormerlySerializedAs("serverPort")]
        [SerializeField] int _serverPort = 7777;
        [FormerlySerializedAs("connectTimeoutMs")]
        [SerializeField] int _connectTimeoutMs = 3000;
        [FormerlySerializedAs("defaultHost")]
        [SerializeField] string _defaultHost = "127.0.0.1";

        const string ServerHostPrefsKey = "ServerHost";
        bool _probing;

        void Awake()
        {
            // MainMenu 진입 시 명시적 소켓 정리.
            // NetworkService는 PersistentServices 프리팹 소속 영속 서비스라 항상 존재.
            // 단 에디터 씬 단독 Play(PersistentServices 미생성) 방어로 null-safe 호출.
            // 연결이 이미 없으면 Disconnect()가 silent no-op이므로 항상 안전.
            NetworkService.Instance?.Disconnect();
        }

        void Start()
        {
            // PlayerPrefs에 박힌 옛 값 → InputField 자동 채움. 없으면 인스펙터 _defaultHost.
            if (_serverHostInputField != null)
            {
                string saved = PlayerPrefs.GetString(ServerHostPrefsKey, _defaultHost);
                if (string.IsNullOrWhiteSpace(saved)) saved = _defaultHost;
                _serverHostInputField.text = saved;
            }

            if (_errorMessageText != null)
                _errorMessageText.text = "";
        }

        public void OnStartClicked()
        {
            if (_probing) return;  // 중복 클릭 차단

            string host = ResolveHost();
            if (string.IsNullOrWhiteSpace(host))
            {
                ShowError("서버 주소를 입력해주세요");
                return;
            }

            _probing = true;
            if (_startButton != null) _startButton.interactable = false;
            ShowError("");  // 옛 오류 메시지 지움

            Debug.Log($"[MainMenu] Server probe 시도 → {host}:{_serverPort}");

            ConnectionProbe.TryConnect(host, _serverPort, OnProbeResult, _connectTimeoutMs);
        }

        void OnProbeResult(bool success, string errorMessage)
        {
            _probing = false;
            if (_startButton != null) _startButton.interactable = true;

            if (success)
            {
                // 성공한 host를 PlayerPrefs에 박음 → NetworkService.Connect()가 Town 씬 진입 시 읽어 사용.
                string host = ResolveHost();
                PlayerPrefs.SetString(ServerHostPrefsKey, host);
                PlayerPrefs.Save();

                Debug.Log($"[MainMenu] Server probe 성공 ({host}) → CharacterSelect Scene 로드");
                SceneTransition.Instance.LoadScene("CharacterSelect");
            }
            else
            {
                ShowError(errorMessage);
                Debug.LogWarning($"[MainMenu] Server probe 실패: {errorMessage}");
            }
        }

        string ResolveHost()
        {
            if (_serverHostInputField != null && !string.IsNullOrWhiteSpace(_serverHostInputField.text))
                return _serverHostInputField.text.Trim();
            return _defaultHost;
        }

        void ShowError(string message)
        {
            if (_errorMessageText != null)
                _errorMessageText.text = message;
        }

        public void OnQuitClicked()
        {
            Debug.Log("[MainMenu] Quit clicked");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}

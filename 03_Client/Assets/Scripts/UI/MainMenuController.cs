using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Dawnholder.Client.Network;

namespace Dawnholder.Client.UI
{
    /// <summary>
    /// 메인 메뉴 버튼 핸들러. MainMenu 씬의 "시작" / "종료" 버튼 onClick에 연결.
    ///
    /// **헌법 #1 (Server Authority)**: 시작 버튼은 *씬 로드*만 트리거. 캐릭터/인벤토리/연결
    /// 같은 권위 상태는 건드리지 않음. 단 M3.8 Phase 05 5-B부터는 *서버 가용성 점검*만 박힘
    /// (ConnectionProbe로 짧은 TCP probe → close, 게임 본 connection은 Gameplay Scene의 NetworkBootstrap이 박음).
    ///
    /// **SceneTransition Singleton 경유** (페이드 폴리시, 정유현 Phase 05 박음). 직접 SceneManager 호출 X.
    ///
    /// **M3.8 Phase 05 5-B — 서버 가용성 게이트**:
    /// - InputField에 서버 주소 박음 (Hamachi 가상 IP 또는 LAN/공인 IP). 기본값 = 127.0.0.1
    /// - Start 버튼 → ConnectionProbe.TryConnect → 성공 시 PlayerPrefs 저장 + CharacterSelect 로드
    /// - 실패 시 errorMessageText에 오류 표시 + Scene 진입 차단 (사용자가 재입력 후 재시도)
    /// - 중복 클릭 차단 (probe 진행 중엔 버튼 비활성)
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("서버 연결 게이트 (M3.8 Phase 05 5-B)")]
        [SerializeField] TMP_InputField serverHostInputField;
        [SerializeField] TMP_Text errorMessageText;
        [SerializeField] Button startButton;
        [SerializeField] int serverPort = 7777;
        [SerializeField] int connectTimeoutMs = 3000;
        [SerializeField] string defaultHost = "127.0.0.1";

        const string ServerHostPrefsKey = "ServerHost";
        bool _probing;

        void Start()
        {
            // PlayerPrefs에 박힌 옛 값 → InputField 자동 채움. 없으면 인스펙터 defaultHost.
            if (serverHostInputField != null)
            {
                string saved = PlayerPrefs.GetString(ServerHostPrefsKey, defaultHost);
                if (string.IsNullOrWhiteSpace(saved)) saved = defaultHost;
                serverHostInputField.text = saved;
            }

            if (errorMessageText != null)
                errorMessageText.text = "";
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
            if (startButton != null) startButton.interactable = false;
            ShowError("");  // 옛 오류 메시지 지움

            Debug.Log($"[MainMenu] Server probe 시도 → {host}:{serverPort}");

            ConnectionProbe.TryConnect(host, serverPort, OnProbeResult, connectTimeoutMs);
        }

        void OnProbeResult(bool success, string errorMessage)
        {
            _probing = false;
            if (startButton != null) startButton.interactable = true;

            if (success)
            {
                // 성공한 host를 PlayerPrefs에 박음 → NetworkBootstrap이 Gameplay Scene에서 읽어 사용.
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
            if (serverHostInputField != null && !string.IsNullOrWhiteSpace(serverHostInputField.text))
                return serverHostInputField.text.Trim();
            return defaultHost;
        }

        void ShowError(string message)
        {
            if (errorMessageText != null)
                errorMessageText.text = message;
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

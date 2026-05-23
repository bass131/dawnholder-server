using System;
using System.Net;
using Dawnholder.Client.Net;
using Dawnholder.Client.Scenes;
using Dawnholder.Client.UI;
using Shared.Protocol;
using UnityEngine;

namespace Dawnholder.Client.Network
{
    /// <summary>
    /// Gameplay Scene 진입 시 서버 connect + handshake 완료 후 C_CharacterSelect 자동 송신.
    ///
    /// **M4.1 Phase 02 변경 (5-B/5-C/5-D)**:
    /// - S_HandshakeResult(ok=true) 수신 event 기반으로 C_CharacterSelect 송신 (race 봉합).
    /// - PlayerPrefs "SelectedCharacterClass" 미박힘 or invalid 시 → MainMenu로 돌려보냄.
    ///   이 게이트가 P0-2 봉합 핵심: 클라가 C_CharacterSelect 안 보내면 서버는 EnterGameWorld 안 함.
    /// - 옛 default Warrior 자동 진입 가닥 제거.
    ///
    /// **event 기반 race 봉합 (옵션 A, Phase 02 §3단계)**:
    ///   HandshakeOk 수신 전 C_CharacterSelect 송신 시 서버 측 silent drop 위험.
    ///   UnityClientSession.OnHandshakeOkEvent 콜백을 등록해 handshake 확인 후 송신.
    ///
    /// **PlayerPrefs key**:
    ///   - "ServerHost" (string) — MainMenu에서 probe 성공 시 박음.
    ///   - "SelectedCharacterClass" (int) — CharacterSelectController에서 박음 (0=Warrior / 1=Ranger).
    ///
    /// **인스펙터 노출**:
    /// - serverHost / serverPort: connect 대상 (PlayerPrefs 미박힘 시 fallback)
    /// - pingIntervalSeconds: Ping 송신 주기 (기본 1초)
    /// </summary>
    public class NetworkBootstrap : MonoBehaviour
    {
        [SerializeField] string serverHost = "127.0.0.1";
        [SerializeField] int serverPort = 7777;
        [SerializeField] float pingIntervalSeconds = 1.0f;

        // M3.8 Phase 05 5-B: MainMenu에서 PlayerPrefs.SetString("ServerHost", ...) 박은 값을 우선.
        // 키 없거나 빈 문자열이면 인스펙터 박힌 serverHost 그대로 fallback.
        const string ServerHostPrefsKey = "ServerHost";

        // M4.1 Phase 02 5-A/5-B: CharacterSelectController와 동일 key.
        // byte 값 0 = Warrior / 1 = Ranger. 미박힘(-1) 또는 범위 밖 = MainMenu 돌려보냄.
        const int ClassPrefsInvalid = -1;

        Connector _connector;
        UnityClientSession _session;
        float _accumSec;
        bool _isConnected;

        // M4.1 Phase 02 5-D: C_CharacterSelect 이미 송신됐는지 idempotent 가드.
        // handshake 콜백이 두 번 불릴 edge-case (재연결 등) 방어.
        bool _characterSelectSent;

        void Start()
        {
            // M4.1 Phase 02 5-C: Gameplay 진입 전 class 선택 검증.
            // 미박힘 또는 invalid 값이면 MainMenu 돌려보냄.
            int classValue = PlayerPrefs.GetInt(CharacterSelectController.SelectedClassPrefsKey, ClassPrefsInvalid);
            if (!IsValidClassValue(classValue))
            {
                Debug.LogWarning("[NetworkBootstrap] SelectedCharacterClass 미박힘 or invalid → MainMenu로 돌려보냄. 캐릭터 선택 후 진입해주세요.");
                ReturnToMainMenu();
                return;
            }

            string host = PlayerPrefs.GetString(ServerHostPrefsKey, serverHost);
            if (string.IsNullOrWhiteSpace(host)) host = serverHost;

            IPAddress ip = IPAddress.Parse(host);
            IPEndPoint endPoint = new IPEndPoint(ip, serverPort);

            _connector = new Connector();
            _connector.Connect(endPoint, () =>
            {
                _session = new UnityClientSession();
                _isConnected = true;

                // M4.1 Phase 02 5-B: event 기반 race 봉합.
                // UnityClientSession 생성 시점에 콜백 등록.
                // S_HandshakeResult(ok=true) 수신 후 main thread에서 OnHandshakeOk 호출됨.
                _session.OnHandshakeOkEvent += OnHandshakeOk;

                return _session;
            });

            Debug.Log($"[NetworkBootstrap] Connect 시도 → {endPoint} (PlayerPrefs host={host})");
        }

        // M4.1 Phase 02 5-B: S_HandshakeResult(ok=true) 수신 event 핸들러.
        // UnityClientSession.HandleHandshakeResult가 main thread dispatcher를 통해 호출.
        // handshake 완료 확인 후 PlayerPrefs 선택값 읽어 C_CharacterSelect 송신.
        void OnHandshakeOk()
        {
            if (_characterSelectSent) return; // idempotent 가드
            _characterSelectSent = true;

            int classValue = PlayerPrefs.GetInt(CharacterSelectController.SelectedClassPrefsKey, ClassPrefsInvalid);
            if (!IsValidClassValue(classValue))
            {
                // 이 경로는 Start에서 이미 검증했으나 race edge-case 방어.
                Debug.LogWarning("[NetworkBootstrap] OnHandshakeOk: SelectedCharacterClass invalid → MainMenu로 돌려보냄.");
                ReturnToMainMenu();
                return;
            }

            byte classByte = (byte)classValue;
            var packet = new C_CharacterSelect { characterClass = classByte };

            if (_session != null)
            {
                _session.Send(packet.Write());
                Debug.Log($"[NetworkBootstrap] C_CharacterSelect 송신 (class={classByte}, handshake 완료 후) — 서버 EnterGameWorldIfReady 기다리는 중");
            }
            else
            {
                Debug.LogWarning("[NetworkBootstrap] OnHandshakeOk: _session null — 송신 불가");
            }
        }

        // M4.1 Phase 02 5-C: class 선택값 유효성 검증.
        // 0=Warrior / 1=Ranger만 유효. PlayerPrefs.GetInt 미박힘 기본값(-1) = invalid.
        static bool IsValidClassValue(int classValue)
        {
            return classValue == (int)CharacterClass.Warrior || classValue == (int)CharacterClass.Ranger;
        }

        // M4.1 Phase 02 5-C: MainMenu로 돌려보냄. SceneTransition 경유 (fade 정합).
        // Toast 안내 = Debug.LogWarning으로 Console 표시. Scene UI Toast는 unity-bridge 또는 본인 직접 박음 권유.
        void ReturnToMainMenu()
        {
            if (SceneTransition.Instance != null)
            {
                SceneTransition.Instance.LoadScene("MainMenu");
            }
            else
            {
                Debug.LogWarning("[NetworkBootstrap] SceneTransition.Instance null — direct LoadScene fallback");
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
            }
        }

        void Update()
        {
            // session이 아직 없거나(connect 진행 중/실패) 끊긴 상태면 송신 X.
            if (!_isConnected || _session == null) return;

            _accumSec += Time.deltaTime;
            if (_accumSec < pingIntervalSeconds) return;
            _accumSec = 0f;

            // Ping 패킷 생성 + 직렬화 + 송신.
            // clientTimestampMs는 Unix epoch ms (서버에 그대로 echo됨).
            C_Ping ping = new C_Ping
            {
                clientTimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
            _session.Send(ping.Write());
        }

        void OnApplicationQuit()
        {
            // Unity Stop(에디터) 또는 빌드 종료 시 connection 정상 종료.
            if (_session != null)
                _session.OnHandshakeOkEvent -= OnHandshakeOk;
            _session?.Disconnect();
            _isConnected = false;
        }
    }
}

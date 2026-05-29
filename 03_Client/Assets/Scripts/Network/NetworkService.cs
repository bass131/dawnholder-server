using System;
using System.Net;
using Dawnholder.Client.Net;
using Dawnholder.Client.Scenes;
using Dawnholder.Client.UI;
using Shared.Protocol;
using UnityEngine;
using UnityEngine.Serialization;

namespace Dawnholder.Client.Network
{
    /// <summary>
    /// 소켓 연결/세션을 보유하는 영속 서비스 (ADR-027 A안).
    ///
    /// **PersistentServices 프리팹 소속** — PersistentServicesBootstrap가 앱 전체에 1회만 생성하므로
    /// DontDestroyOnLoad/중복가드는 이 컴포넌트 내부에 없어도 됩니다.
    /// (PersistentServicesBootstrap가 1회 생성 보장 → 중복 불가 → 가드 불필요, ADR-027 정신.)
    ///
    /// **자동 연결 없음**:
    /// Start()에서 connect하지 않습니다. 연결은 GameEntryPoint(Town 씬 진입 시)가
    /// Connect()를 명시적으로 호출합니다. 이렇게 하면 "접속 시점이 씬 로드에 암묵적으로
    /// 묶여 불분명"하던 문제가 해소됩니다.
    ///
    /// **연결 생명주기**:
    ///   - GameEntryPoint.Start() → Connect() : 게임플레이 최초 진입 시 1회
    ///   - 포탈 맵 이동(Town↔사냥터↔보스) : 연결 유지 (재연결 없음)
    ///   - MainMenu 복귀 : MainMenuController.Awake() → Disconnect() 로 명시 해제
    ///
    /// **PlayerPrefs key**:
    ///   - "ServerHost"           (string) — MainMenuController에서 probe 성공 시 박음
    ///   - "SelectedCharacterClass" (int)  — CharacterSelectController에서 박음
    ///
    /// **인스펙터 노출**:
    ///   - serverHost / serverPort : PlayerPrefs 미박힘 시 fallback
    ///   - pingIntervalSeconds     : Ping 송신 주기 (기본 1초)
    ///
    /// **비교 — 제거된 ① 땜질 코드 목록**:
    ///   - DontDestroyOnLoad(gameObject) in Awake        → PersistentServicesBootstrap로 일원화
    ///   - Instance != null 중복 가드 + _isDuplicate     → PersistentServicesBootstrap 1회 생성으로 불필요
    ///   - SceneManager.sceneLoaded += OnSceneLoaded     → 씬 감지 자동 teardown 제거
    ///   - OnSceneLoaded() (MainMenu/CharacterSelect 감지) → 제거
    ///   - Start()의 auto-connect 흐름                   → Connect() 명시 호출로 이전
    ///   - MenuSceneNames 상수 + foreach 씬 이름 비교     → 제거
    /// </summary>
    public class NetworkService : MonoBehaviour
    {
        public static NetworkService Instance { get; private set; }

        [FormerlySerializedAs("serverHost")]
        [SerializeField] string _serverHost = "127.0.0.1";
        [FormerlySerializedAs("serverPort")]
        [SerializeField] int _serverPort = 7777;
        [FormerlySerializedAs("pingIntervalSeconds")]
        [SerializeField] float _pingIntervalSeconds = 1.0f;

        const string ServerHostPrefsKey = "ServerHost";
        const int ClassPrefsInvalid = -1;

        Connector _connector;
        UnityClientSession _session;
        float _accumSec;
        bool _isConnected;

        // C_CharacterSelect 이미 송신됐는지 idempotent 가드.
        // handshake 콜백이 두 번 불릴 edge-case (재연결 등) 방어.
        bool _characterSelectSent;

        /// <summary>
        /// 현재 소켓 연결 상태. GameEntryPoint가 재진입 루프백 판단에 사용.
        /// true면 Connect() 호출은 no-op.
        /// </summary>
        public bool IsConnected => _isConnected;

        void Awake()
        {
            // PersistentServices 프리팹으로 PersistentServicesBootstrap가 1회 생성하므로
            // 이론상 중복이 없지만, 에디터 씬 단독 Play 방어로 Instance 등록만 함.
            if (Instance != null && Instance != this)
            {
                // 에디터에서 PersistentServicesBootstrap 없이 씬 단독 Play 시 두 번째 인스턴스 방어.
                Debug.LogWarning("[NetworkService] 인스턴스 중복 감지 — 파괴. PersistentServices 프리팹이 씬에도 배치됐나요?");
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// 서버에 소켓 연결을 시작합니다. 이미 연결된 상태라면 no-op.
        ///
        /// 호출 주체: GameEntryPoint (Town 씬 진입 시 1회).
        /// 포탈 루프백(Ending→Town 재진입)에서도 IsConnected=true라 재연결 없음.
        ///
        /// <paramref name="hostOverride"/>가 null이면 PlayerPrefs "ServerHost" →
        /// 인스펙터 serverHost 순서로 fallback.
        /// <paramref name="characterClassOverride"/>가 -1이면 PlayerPrefs
        /// "SelectedCharacterClass"에서 읽음.
        /// </summary>
        public void Connect(string hostOverride = null, int characterClassOverride = ClassPrefsInvalid)
        {
            if (_isConnected)
            {
                Debug.Log("[NetworkService] 이미 연결됨 — Connect() no-op (포탈 루프백 안전망).");
                return;
            }

            // 캐릭터 클래스 검증 — 미선택/invalid면 MainMenu로 돌려보냄.
            int classValue = characterClassOverride != ClassPrefsInvalid
                ? characterClassOverride
                : PlayerPrefs.GetInt(CharacterSelectController.SelectedClassPrefsKey, ClassPrefsInvalid);

            if (!IsValidClassValue(classValue))
            {
                Debug.LogWarning("[NetworkService] SelectedCharacterClass 미박힘 or invalid → MainMenu로 돌려보냄. 캐릭터 선택 후 진입해주세요.");
                ReturnToMainMenu();
                return;
            }

            string host = hostOverride ?? PlayerPrefs.GetString(ServerHostPrefsKey, _serverHost);
            if (string.IsNullOrWhiteSpace(host)) host = _serverHost;

            // PlayerPrefs는 디스크 영속이라 손상/변조된 host 문자열이 박힐 수 있음 (신뢰 경계 밖).
            // Parse 실패 시 connect 흐름이 죽고 fallback도 못 타므로 방어적으로 잡아 MainMenu로 복귀.
            if (!IPAddress.TryParse(host, out IPAddress ip))
            {
                Debug.LogWarning($"[NetworkService] host '{host}' 파싱 실패 (손상된 PlayerPrefs?) → MainMenu로 돌려보냄.");
                ReturnToMainMenu();
                return;
            }
            IPEndPoint endPoint = new IPEndPoint(ip, _serverPort);

            _connector = new Connector();
            _connector.Connect(endPoint, () =>
            {
                _session = new UnityClientSession();
                _isConnected = true;

                // event 기반 race 봉합 (M4.1 Phase 02 패턴 유지):
                // S_HandshakeResult(ok=true) 수신 후 main thread에서 OnHandshakeOk 호출됨.
                // C_CharacterSelect 송신 race 봉합 핵심.
                _session.OnHandshakeOkEvent += OnHandshakeOk;

                return _session;
            });

            Debug.Log($"[NetworkService] Connect 시도 → {endPoint} (host={host})");
        }

        /// <summary>
        /// 소켓 연결을 명시적으로 해제합니다. 오브젝트는 파괴되지 않습니다.
        ///
        /// 호출 주체: MainMenuController (MainMenu 진입 시).
        /// 연결이 없는 상태에서 호출해도 안전 (null 가드).
        /// </summary>
        public void Disconnect()
        {
            if (!_isConnected && _session == null)
            {
                // 이미 끊긴 상태 — silent no-op (MainMenu Awake가 방어적으로 호출해도 안전).
                return;
            }

            Debug.Log("[NetworkService] Disconnect() 호출 — 소켓 정리.");
            TeardownConnection();
        }

        // 연결 정리 내부 메서드. OnApplicationQuit + Disconnect() 공통 경로.
        void TeardownConnection()
        {
            if (_session != null)
                _session.OnHandshakeOkEvent -= OnHandshakeOk;
            _session?.Disconnect();
            _session = null;
            _isConnected = false;
            _characterSelectSent = false;

            // PendingSpawn 잔류 방어 (reviewer 🟡): pending spawn은 *현 세션 한정* 유효.
            // 세션 종료(MainMenu 복귀/끊김) 시 비워서, 다음 세션이 옛 좌표를 1회 오소비하는 stale 잔류 차단.
            UnityClientSession.ConsumePendingSpawn();
        }

        // S_HandshakeResult(ok=true) 수신 event 핸들러.
        // UnityClientSession.HandleHandshakeResult가 main thread dispatcher를 통해 호출.
        // handshake 완료 확인 후 PlayerPrefs 선택값 읽어 C_CharacterSelect 송신.
        void OnHandshakeOk()
        {
            if (_characterSelectSent) return; // idempotent 가드
            _characterSelectSent = true;

            int classValue = PlayerPrefs.GetInt(CharacterSelectController.SelectedClassPrefsKey, ClassPrefsInvalid);
            if (!IsValidClassValue(classValue))
            {
                Debug.LogWarning("[NetworkService] OnHandshakeOk: SelectedCharacterClass invalid → MainMenu로 돌려보냄.");
                ReturnToMainMenu();
                return;
            }

            byte classByte = (byte)classValue;
            var packet = new C_CharacterSelect { characterClass = classByte };

            if (_session != null)
            {
                _session.Send(packet.Write());
                Debug.Log($"[NetworkService] C_CharacterSelect 송신 (class={classByte}, handshake 완료 후) — 서버 EnterGameWorldIfReady 기다리는 중");
            }
            else
            {
                Debug.LogWarning("[NetworkService] OnHandshakeOk: _session null — 송신 불가");
            }
        }

        static bool IsValidClassValue(int classValue)
        {
            return classValue == (int)CharacterClass.Warrior || classValue == (int)CharacterClass.Ranger;
        }

        void ReturnToMainMenu()
        {
            if (SceneTransition.Instance != null)
            {
                SceneTransition.Instance.LoadScene("MainMenu");
            }
            else
            {
                Debug.LogWarning("[NetworkService] SceneTransition.Instance null — direct LoadScene fallback");
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
            }
        }

        void Update()
        {
            // session이 없거나(connect 진행 중/실패) 끊긴 상태면 Ping 송신 X.
            if (!_isConnected || _session == null) return;

            _accumSec += Time.deltaTime;
            if (_accumSec < _pingIntervalSeconds) return;
            _accumSec = 0f;

            C_Ping ping = new C_Ping
            {
                clientTimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
            _session.Send(ping.Write());
        }

        void OnApplicationQuit()
        {
            // Unity Stop(에디터) 또는 빌드 종료 시 connection 정상 종료.
            TeardownConnection();
        }
    }
}

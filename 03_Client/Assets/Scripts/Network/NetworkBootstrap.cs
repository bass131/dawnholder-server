using System;
using System.Net;
using Dawnholder.Client.Net;
using Shared.Protocol;
using UnityEngine;

namespace Dawnholder.Client.Network
{
    /// <summary>
    /// Phase 04 시연용 트리거 + Phase 05 Ping 자동 송신.
    /// 빈 GameObject 하나에 본 컴포넌트 + <see cref="MainThreadDispatcher"/>를 같이 붙이고
    /// Play를 누르면 자동으로 서버에 connect → 1초마다 Ping 송신.
    ///
    /// **인스펙터 노출**:
    /// - serverHost / serverPort: connect 대상
    /// - pingIntervalSeconds: Ping 송신 주기 (기본 1초)
    ///
    /// **Phase 06+에선** UI 버튼이나 게임 시작 흐름과 통합 예정.
    /// </summary>
    public class NetworkBootstrap : MonoBehaviour
    {
        [SerializeField] string serverHost = "127.0.0.1";
        [SerializeField] int serverPort = 7777;
        [SerializeField] float pingIntervalSeconds = 1.0f;

        // M3.8 Phase 05 5-B: MainMenu에서 PlayerPrefs.SetString("ServerHost", ...) 박은 값을 우선.
        // 키 없거나 빈 문자열이면 인스펙터 박힌 serverHost 그대로 fallback.
        // Hamachi 검증 / 발표장 비상시 즉석 변경 안전망.
        const string ServerHostPrefsKey = "ServerHost";

        Connector _connector;
        UnityClientSession _session;
        float _accumSec;
        bool _isConnected;

        void Start()
        {
            string host = PlayerPrefs.GetString(ServerHostPrefsKey, serverHost);
            if (string.IsNullOrWhiteSpace(host)) host = serverHost;

            IPAddress ip = IPAddress.Parse(host);
            IPEndPoint endPoint = new IPEndPoint(ip, serverPort);

            _connector = new Connector();
            _connector.Connect(endPoint, () =>
            {
                _session = new UnityClientSession();
                _isConnected = true;
                return _session;
            });

            Debug.Log($"[Unity] Connect 시도 → {endPoint} (PlayerPrefs host={host})");
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
            // Phase 07: 임시 BitConverter 코드 → 자체 PDL 자동 생성 코드 (C_Ping).
            C_Ping ping = new C_Ping
            {
                clientTimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
            _session.Send(ping.Write());
        }

        void OnApplicationQuit()
        {
            // Unity Stop(에디터) 또는 빌드 종료 시 connection 정상 종료.
            _session?.Disconnect();
            _isConnected = false;
        }
    }
}

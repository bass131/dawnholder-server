using System.Net;
using Dawnholder.Client.Net;
using UnityEngine;

namespace Dawnholder.Client.Network
{
    /// <summary>
    /// Phase 04 시연용 트리거. 빈 GameObject 하나에 본 컴포넌트 +
    /// <see cref="MainThreadDispatcher"/>를 같이 붙이고 Play를 누르면 자동으로
    /// 서버에 connect 시도.
    ///
    /// **인스펙터 노출**: serverHost / serverPort. 같은 머신 시연은 기본값
    /// (127.0.0.1:7777). LAN 시연 시 서버 머신의 LAN IP 입력.
    ///
    /// **Phase 05+에선** UI 버튼이나 게임 시작 흐름과 통합 예정. 본 컴포넌트는
    /// 그때 삭제하거나 dev-only로.
    /// </summary>
    public class NetworkBootstrap : MonoBehaviour
    {
        [SerializeField] string serverHost = "127.0.0.1";
        [SerializeField] int serverPort = 7777;

        Connector _connector;
        UnityClientSession _session;

        void Start()
        {
            IPAddress ip = IPAddress.Parse(serverHost);
            IPEndPoint endPoint = new IPEndPoint(ip, serverPort);

            // Connector는 SessionFactory 패턴을 통해 *언제 어떤 세션을 만들지*를
            // 호출자(여기)가 결정하게 함. 서버 Listener와 동일 발상의 데칼코마니.
            _connector = new Connector();
            _connector.Connect(endPoint, () =>
            {
                _session = new UnityClientSession();
                return _session;
            });

            Debug.Log($"[Unity] Connect 시도 → {endPoint}");
        }

        void OnApplicationQuit()
        {
            // Unity Stop(에디터) 또는 빌드 종료 시 connection 정상 종료 시도.
            // 서버측에서도 OnDisconnected 로그가 떠야 양쪽 clean shutdown 검증.
            _session?.Disconnect();
        }
    }
}

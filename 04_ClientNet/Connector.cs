using System.Net;
using System.Net.Sockets;

namespace Dawnholder.Client.Net;

/// <summary>
/// 서버에 능동적으로 connect 시도하는 측 (클라이언트 전용).
///
/// **서버 Connector와의 비대칭** (Y2 갈래 핵심):
/// - 서버 Connector는 부하 테스트나 cross-server 통신에서 *클라 흉내*용으로 존재.
/// - 클라 Connector는 진짜 본업 — 게임 시작 시 서버 1곳에 1회 connect.
/// - 알고리즘은 동일(SocketAsyncEventArgs.ConnectAsync). 의미만 다름.
///
/// **count 파라미터를 살린 이유**:
/// 클라 한 인스턴스는 사실상 1번 connect로 끝. 그러나 같은 라이브러리를
/// `99_Tools/`의 헤드리스 봇(부하 테스트)에서 그대로 재사용할 예정이라
/// "한 프로세스 안에서 N개 가짜 클라"를 띄우는 시나리오가 자연스럽게 들어감.
/// 본 클라 코드에서는 그냥 count=1로 호출하면 됨.
///
/// **타입 좁힘**: Func&lt;ClientSession&gt;로 받음 (서버는 Func&lt;Session&gt;).
/// 클라가 만들 세션은 ClientSession을 반드시 상속해야 함을 컴파일 타임에 강제.
/// </summary>
public class Connector
{
    // _sessionFactory: Connect() 한 번 호출에 한 번 set. 같은 Connector 인스턴스로
    // 다중 Connect를 부르는 시나리오는 정의되지 않음 (필요 시 별도 인스턴스).
    Func<ClientSession>? _sessionFactory;

    /// <summary>
    /// 지정된 EndPoint로 비동기 connect 시도. count는 같은 endpoint로 N번 동시 시도.
    /// </summary>
    public void Connect(IPEndPoint endPoint, Func<ClientSession> sessionFactory, int count = 1)
    {
        _sessionFactory = sessionFactory;

        for (int i = 0; i < count; i++)
        {
            Socket socket = new Socket(
                endPoint.AddressFamily, // IPv4 / IPv6 자동 결정
                SocketType.Stream,
                ProtocolType.Tcp);

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.Completed += OnConnectCompleted;
            args.RemoteEndPoint = endPoint;

            // UserToken으로 socket을 들고 다니는 이유:
            // count > 1 시 각 시도마다 별도 socket이 필요한데, 클로저 캡처 대신
            // SAEA의 UserToken에 실어두면 OnConnectCompleted에서 해당 시도의
            // socket을 정확히 꺼낼 수 있음. (사실 ConnectSocket으로도 됨 —
            // 서버 패턴 유지를 위해 동일 구조 보존.)
            args.UserToken = socket;

            RegisterConnect(args);
        }
    }

    void RegisterConnect(SocketAsyncEventArgs args)
    {
        Socket? socket = args.UserToken as Socket;
        if (socket == null)
            return;

        bool pending = socket.ConnectAsync(args);

        // 동기 완료 시 Completed 이벤트가 안 뜸 → 직접 호출.
        if (pending == false)
            OnConnectCompleted(null, args);
    }

    void OnConnectCompleted(object? sender, SocketAsyncEventArgs args)
    {
        if (args.SocketError == SocketError.Success)
        {
            ClientSession session = _sessionFactory!.Invoke();
            session.Start(args.ConnectSocket!);
            session.OnConnected(args.RemoteEndPoint!);
        }
        else
        {
            // 실패 사유 예: ConnectionRefused (서버 안 떠있음), HostUnreachable, TimedOut.
            // Phase 04에서 재시도/백오프 정책 도입 예정. 지금은 로그만.
            Console.WriteLine($"[Connector] OnConnectCompleted Error : {args.SocketError}");
        }
    }
}

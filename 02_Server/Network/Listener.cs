using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Dawnholder.Server.Network
{
    public class Listener
    {
        Socket? _listenSocket;
        Func<Session>? _sessionFactory;

        public void Init(
            IPEndPoint endPoint,
            Func<Session> sessionFactory,
            int register = 10,
            int backLog = 100)
        {
            // 1. 소켓 생성 및 설정
            // 문지기 역할을 하는 소켓 생성
            _listenSocket = new Socket(
                endPoint.AddressFamily, // IP 주소 패밀리 (예: IPv4, IPv6)
                SocketType.Stream, // 스트림 소켓 (TCP 통신에 사용)
                ProtocolType.Tcp); // TCP 프로토콜 사용

            // 클라이언트 연결이 수락되었을 때 호출될 이벤트 핸들러 등록
            _sessionFactory = sessionFactory;


            // 2. 소켓(문지기) 옵션 설정(교육)
            _listenSocket.Bind(endPoint); // 소켓을 엔드포인트에 바인딩

            // 3. 대기열 설정 및 영업 시작
            // backlog : 대기열의 최대 크기
            _listenSocket.Listen(backLog); // 최대 10개의 대기 연결 허용

            // 4. 비동기적으로 클라이언트 연결 수락 시작
            // 비동기 소켓 작업에 대한 이벤트 인자 객체 생성
            for (int i = 0; i < register; i++)
            {
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                // 클라이언트 연결 수락이 완료되었을 때 호출될 이벤트 핸들러 등록
                args.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);
                RegisterAccept(args); // 비동기적으로 클라이언트 연결 수락 시작
            }
        }

        // 클라이언트 연결 수락을 비동기적으로 시작하는 메서드
        void RegisterAccept(SocketAsyncEventArgs args)
        {
            args.AcceptSocket = null; // 이전에 수락된 소켓이 있다면 초기화

            bool pending = _listenSocket!.AcceptAsync(args);

            if (pending == false)
            {
                OnAcceptCompleted(null, args);
            }
            
        }

        // 클라이언트 연결 수락이 완료되었을 때 호출되는 이벤트 핸들러
        // 항상 멀티쓰레드로 동작하기 때문에, Red Zone이 발생할 수 있음
        // 앞으로 조심해서 신경을 곤두세워야할 부분.
        //
        // M3.8 Phase 05 후속 봉합 — accept callback race window:
        // 클라가 accept 통과 직후 close 박으면 (예: ConnectionProbe.TryConnect 패턴 = probe 후 즉시 Shutdown+Close),
        // AcceptSocket.RemoteEndPoint 박는 시점에 socket이 이미 disposed → ObjectDisposedException → IOCP worker thread 죽음.
        // register(10) worker thread 점진 소모 시 신규 accept 거부 → 서버 효과적 사망.
        // 봉합 = RemoteEndPoint 박을 때 try-catch + race 발생 시 session 박지 않고 socket close + 다음 accept 재등록만.
        // 헌법 #3 Trust Boundary fail-closed 정합 — disposed socket = untrusted, 서버 process 보호 우선.
        void OnAcceptCompleted(object? sender, SocketAsyncEventArgs args)
        {
            if (args.SocketError == SocketError.Success)
            {
                Socket acceptedSocket = args.AcceptSocket!;
                EndPoint? remote = null;
                try
                {
                    remote = acceptedSocket.RemoteEndPoint;
                }
                catch (ObjectDisposedException)
                {
                    // race window — 클라가 accept 직후 close. session 박지 않고 skip.
                }

                if (remote != null)
                {
                    Session session = _sessionFactory!.Invoke(); // 팩토리 메서드를 호출하여 세션 생성
                    session.Start(acceptedSocket); // 클라이언트 소켓으로 세션 초기화
                    session.OnConnected(remote); // 연결 성공 알림
                }
                else
                {
                    try { acceptedSocket.Close(); } catch { /* swallow */ }
                }
            }
            else
            {
                Console.WriteLine(args.SocketError.ToString());
            }

            RegisterAccept(args); // 다음 클라이언트 연결 수락을 위해 다시 등록 (race 분기에서도 의무)
        }

        public Socket Accept()
        {
            return _listenSocket!.Accept(); // 클라이언트 연결을 수락하고 새로운 소켓 반환
        }
    }
}

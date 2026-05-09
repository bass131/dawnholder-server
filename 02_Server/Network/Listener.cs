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
        Socket? m_listenSocket;
        Func<Session>? m_SessionFactory;

        public void Init(
            IPEndPoint _endPoint,
            Func<Session> _sessionFactory,
            int _register = 10,
            int _backLog = 100)
        {
            // 1. 소켓 생성 및 설정
            // 문지기 역할을 하는 소켓 생성
            m_listenSocket = new Socket(
                _endPoint.AddressFamily, // IP 주소 패밀리 (예: IPv4, IPv6)
                SocketType.Stream, // 스트림 소켓 (TCP 통신에 사용)
                ProtocolType.Tcp); // TCP 프로토콜 사용

            // 클라이언트 연결이 수락되었을 때 호출될 이벤트 핸들러 등록
            m_SessionFactory = _sessionFactory;


            // 2. 소켓(문지기) 옵션 설정(교육)
            m_listenSocket.Bind(_endPoint); // 소켓을 엔드포인트에 바인딩

            // 3. 대기열 설정 및 영업 시작
            // backlog : 대기열의 최대 크기
            m_listenSocket.Listen(_backLog); // 최대 10개의 대기 연결 허용

            // 4. 비동기적으로 클라이언트 연결 수락 시작
            // 비동기 소켓 작업에 대한 이벤트 인자 객체 생성
            for (int i = 0; i < _register; i++)
            {
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                // 클라이언트 연결 수락이 완료되었을 때 호출될 이벤트 핸들러 등록
                args.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);
                RegisterAccept(args); // 비동기적으로 클라이언트 연결 수락 시작
            }
        }

        // 클라이언트 연결 수락을 비동기적으로 시작하는 메서드
        void RegisterAccept(SocketAsyncEventArgs _args)
        {
            _args.AcceptSocket = null; // 이전에 수락된 소켓이 있다면 초기화

            bool _pending = m_listenSocket!.AcceptAsync(_args);

            if (_pending == false)
            {
                OnAcceptCompleted(null, _args);
            }
            
        }

        // 클라이언트 연결 수락이 완료되었을 때 호출되는 이벤트 핸들러
        // 항상 멀티쓰레드로 동작하기 때문에, Red Zone이 발생할 수 있음
        // 앞으로 조심해서 신경을 곤두세워야할 부분.
        void OnAcceptCompleted(object? sender, SocketAsyncEventArgs args)
        {
            if (args.SocketError == SocketError.Success)
            {
                // 클라이언트 연결이 성공적으로 수락된 경우,
                // 등록된 이벤트 핸들러를 호출하여 클라이언트 소켓을 전달

                // 클라이언트 외부에서 생성할 수도 있지만.
                // 성향의 차이로 나뉘어진다. 
                // 1. 외부에서 생성해도 상관없다
                // 2. 내부에 게임 세션을 구현하는게 코드상 깔끔하다.

                Session session = m_SessionFactory!.Invoke(); // 팩토리 메서드를 호출하여 세션 생성
                session.Start(args.AcceptSocket!); // 클라이언트 소켓으로 세션 초기화
                session.OnConnected(args.AcceptSocket!.RemoteEndPoint!); // 연결 성공 알림
            }
            else
            {
                Console.WriteLine(args.SocketError.ToString());
            }

            RegisterAccept(args); // 다음 클라이언트 연결 수락을 위해 다시 등록
        }

        public Socket Accept()
        {
            return m_listenSocket!.Accept(); // 클라이언트 연결을 수락하고 새로운 소켓 반환
        }
    }
}

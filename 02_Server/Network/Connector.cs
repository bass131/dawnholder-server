using System;
using System.Net;
using System.Net.Sockets;

namespace Dawnholder.Server.Network
{
    public class Connector
    {
        Func<Session>? m_SessionFactory;

        // Connect : 서버에 접속을 시도하는 함수
        public void Connect(IPEndPoint _endPoint, Func<Session> _sessionFactory, int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                Socket socket = new Socket(
                _endPoint.AddressFamily, // 주소 체계
                SocketType.Stream, // 소켓 타입
                ProtocolType.Tcp); // 프로토콜 타입

                m_SessionFactory = _sessionFactory;

                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.Completed += OnConnectCompleted; // 연결 완료 이벤트 핸들러 등록
                args.RemoteEndPoint = _endPoint; // 접속할 서버의 주소
                args.UserToken = socket; // 연결 성공 시 사용할 소켓

                // UserToken으로 소켓을 전달하는 이유.
                // 몇명의 유저가 연결할지 모르기 때문에
                // 연결 성공 시 사용할 소켓을 전달하기 위함
                RegisterConnect(args); // 연결 등록
            }
        }

        void RegisterConnect(SocketAsyncEventArgs _args)
        {
            Socket? socket = _args.UserToken as Socket; // UserToken에 저장된 소켓을 가져옴

            if (socket == null) // 소켓이 없으면 리턴
                return;

            bool pending = socket.ConnectAsync(_args);

            if(pending == false)
            {
                OnConnectCompleted(null, _args);
            }
        }

        void OnConnectCompleted(object? _sender, SocketAsyncEventArgs _args)
        {
            if(_args.SocketError == SocketError.Success)
            {
                Session session = m_SessionFactory!.Invoke();
                session.Start(_args.ConnectSocket!);
                session.OnConnected(_args.RemoteEndPoint!);
            }
            else
            {
                System.Console.WriteLine($"OnConnectCompleted Error : {_args.SocketError}");
            }
        }
    }
}
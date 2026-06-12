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
    /// <summary>
    /// TCP accept 루프. 신규 연결을 받아 <see cref="Session"/> 인스턴스를 생성·시작한다.
    /// </summary>
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
            _listenSocket = new Socket(
                endPoint.AddressFamily,
                SocketType.Stream,
                ProtocolType.Tcp);

            _sessionFactory = sessionFactory;

            _listenSocket.Bind(endPoint);

            _listenSocket.Listen(backLog);

            for (int i = 0; i < register; i++)
            {
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);
                RegisterAccept(args);
            }
        }

        public Socket Accept()
        {
            return _listenSocket!.Accept();
        }

        void RegisterAccept(SocketAsyncEventArgs args)
        {
            args.AcceptSocket = null;

            bool pending = _listenSocket!.AcceptAsync(args);

            if (pending == false)
            {
                OnAcceptCompleted(null, args);
            }

        }

        // accept callback은 IOCP worker thread에서 멀티스레드로 동작.
        //
        // accept callback race window: 클라가 accept 통과 직후 close 박으면
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
                    Session session = _sessionFactory!.Invoke();
                    session.Start(acceptedSocket);
                    session.OnConnected(remote);
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

            RegisterAccept(args); // race 분기에서도 다음 accept 재등록 의무
        }
    }
}

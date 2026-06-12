using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Dawnholder.Server.Network
{
    /// <summary>
    /// length-prefixed 프레임 분할을 처리하는 추상 패킷 세션. 완성된 패킷을 <see cref="OnRecvPacket"/>으로 전달한다.
    /// </summary>
    public abstract class PacketSession : Session
    {
        public static readonly int HeaderSize = 2;

        public const int PacketIdSize = 2;

        // 상수는 FrameValidator로 일원화 — PacketSession은 인용으로 유지(기존 테스트 참조 호환).
        public const int MinFrameSize = FrameValidator.MinFrameSize;
        public const int MaxFrameSize = FrameValidator.MaxFrameSize;

        //[size(2)][packetID(2)][...][size(2)][packetID(2)][...]
        public sealed override int OnRecv(ArraySegment<byte> buffer)
        {
            int processLen = 0;
            int packetCount = 0;

            while(true)
            {
                // 최소한 헤더는 파싱할 수 있는지 확인.
                if(buffer.Count < HeaderSize)
                    break;

                ushort dataSize = BitConverter.ToUInt16(buffer.Array!, buffer.Offset);

                // 순서 중요 — 정상 분할 패킷(buffer.Count < dataSize) 체크보다 *먼저* 검증.
                // 안 그러면 dataSize=70000 같은 attack frame이 영원히 wait에 잡혀 disconnect 안 됨.
                if (!FrameValidator.TryValidateFrameHeader(dataSize, out var reason))
                {
                    Console.WriteLine(
                        $"[Trust] invalid frame size {dataSize} ({reason}) — disconnect");
                    Disconnect();
                    return processLen;
                }

                if(buffer.Count < dataSize)
                    break;

                OnRecvPacket(new ArraySegment<byte>(buffer.Array!, buffer.Offset, dataSize));
                packetCount++;

                processLen += dataSize;
                buffer = new ArraySegment<byte>(buffer.Array!, buffer.Offset + dataSize, buffer.Count - dataSize);

            }
            if (packetCount > 1)
            {
                Console.WriteLine($"[PacketSession] 모아보내기 {packetCount} Packets");
                Console.WriteLine($"Receive Socket Data Success.");
            }

            return processLen;
        }

        public abstract void OnRecvPacket(ArraySegment<byte> buffer);
    }
    /// <summary>
    /// 비동기 TCP 소켓 I/O를 추상화하는 기반 세션. IOCP 기반 send/recv 루프와 연결 생명주기를 관리한다.
    /// </summary>
    public abstract class Session
    {
        #region Private Member Variables
        protected Socket? _socket;
        protected int _disconnected = 0; // 0: 연결됨, 1: 끊김
        protected object _lock = new object();
        protected Queue<ArraySegment<byte>> _sendQueue = new Queue<ArraySegment<byte>>();
        protected List<ArraySegment<byte>> _pendingList = new List<ArraySegment<byte>>();
        protected SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();
        protected SocketAsyncEventArgs _recvArgs = new SocketAsyncEventArgs();

        RecvBuffer _recvBuffer = new RecvBuffer(65535);
        #endregion

        public abstract void OnConnected(EndPoint endPoint);
        public abstract void OnDisconnected(EndPoint endPoint);
        public abstract int  OnRecv(ArraySegment<byte> buffer);
        public abstract void OnSend(int numOfBytes);

        public void Start(Socket socket)
        {
            _socket = socket;

            _recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecvCompleted);
            _sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);

            RegisterRecv();
        }

        // virtual: 테스트 subclass에서 실제 socket I/O 차단 가능 (testability hook).
        public virtual void Send(ArraySegment<byte> sendBuff)
        {
            lock (_lock)
            {
                _sendQueue.Enqueue(sendBuff);

                if (_pendingList.Count == 0)
                    RegisterSend();
            }
        }

        public void Send(List<ArraySegment<byte>> sendBuffList)
        {
            if(sendBuffList.Count == 0)
                return;

            lock (_lock)
            {
                foreach (ArraySegment<byte> sendBuff in sendBuffList)
                    _sendQueue.Enqueue(sendBuff);

                if (_pendingList.Count == 0)
                    RegisterSend();
            }
        }

        // virtual: 테스트 subclass에서 override 가능 (실제 socket 없이 Disconnect 호출 추적).
        public virtual void Disconnect()
        {
            // 재진입/멀티스레드 교착 방지 — 플래그를 원자적으로 설정.
            if (Interlocked.Exchange(ref _disconnected, 1) == 1)
                return;

            OnDisconnected(_socket!.RemoteEndPoint!); // player cleanup enqueue 포함

            // Shutdown / Close / Clear 각 단계 독립 보호 — 어느 단계 예외도 다음 단계를 막지 않음.
            // Shutdown이 throw해도 Close가 socket handle을 반드시 닫고(FD 누수 차단),
            // Close가 throw해도 Clear가 SendQueue/PendingList를 반드시 정리.
            try { _socket!.Shutdown(SocketShutdown.Both); }
            catch (Exception e) { Console.WriteLine($"[Session] socket Shutdown 예외 (이미 reset?) — 무시: {e.Message}"); }

            try { _socket!.Close(); }
            catch (Exception e) { Console.WriteLine($"[Session] socket Close 예외 — 무시: {e.Message}"); }

            Clear();
        }

        void Clear()
        {
            lock (_lock)
            {
                _sendQueue.Clear();
                _pendingList.Clear();
            }
        }

        #region Network Connection Recv
        private void RegisterSend()
        {
            if (_disconnected == 1)
                return;

            while (_sendQueue.Count > 0)
            {
                ArraySegment<byte> buffer = _sendQueue.Dequeue();
                _pendingList.Add(buffer);
            }

            _sendArgs.BufferList = _pendingList;

            try
            {
                bool pending = _socket!.SendAsync(_sendArgs);

                if (pending == false) // SendAsync가 즉시 완료된 경우 직접 호출
                    OnSendCompleted(null, _sendArgs);
            }
            catch (Exception e)
            {
                Console.WriteLine($"RegisterSend Failed {e}");
            }
        }

        private void OnSendCompleted(object? sender, SocketAsyncEventArgs args)
        {
            lock (_lock)
            {
                if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
                {
                    try
                    {
                        _sendArgs.BufferList = null;
                        _pendingList.Clear();

                        OnSend(_sendArgs.BytesTransferred);

                        if (_sendQueue.Count > 0)
                        {
                            RegisterSend();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"OnSendCompleted Failed : {ex}");
                    }
                }
                else
                {
                    Disconnect();
                }
            }
        }

        private void RegisterRecv()
        {
            if (_disconnected == 1)
                return;

            _recvBuffer.Clean();
            ArraySegment<byte> segment = _recvBuffer.WriteSegment;
            _recvArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);

            try
            {
                bool pending = _socket!.ReceiveAsync(_recvArgs);

                if (pending == false) // ReceiveAsync가 즉시 완료된 경우 직접 호출
                {
                    OnRecvCompleted(null, _recvArgs);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"RegisterRecv Failed {e}");
            }
        }

        private void OnRecvCompleted(object? sender, SocketAsyncEventArgs args)
        {
            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
            {
                try
                {
                    if (_recvBuffer.OnWrite(args.BytesTransferred) == false)
                    {
                        Disconnect();
                        return;
                    }

                    int processedSize = OnRecv(_recvBuffer.ReadSegment);
                    if (processedSize < 0 || processedSize > _recvBuffer.DataSize)
                    {
                        Disconnect();
                        return;
                    }

                    if (_recvBuffer.OnRead(processedSize) == false)
                    {
                        Disconnect();
                        return;
                    }

                    RegisterRecv();
                }
                catch (Exception ex)
                {
                    // decode 예외 fail-closed — 로그만 찍고 두면 세션이 half-open으로 잔존(recv 멈추되 socket 생존).
                    // 명시적 Disconnect로 자원 회수 + GameSession.OnDisconnected 트리거. RegisterRecv는 호출 안 함(Disconnect가 차단).
                    Console.WriteLine($"[Trust] OnRecvCompleted decode failed — disconnect: {ex.Message}");
                    Disconnect();
                }
            }
            else
            {
                Disconnect();
            }
        }
        #endregion
    }
}

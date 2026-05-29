// 세션 클래스
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Dawnholder.Server.Network
{
    public abstract class PacketSession : Session
    {
        public static readonly int HeaderSize = 2;

        // Phase 09 (M2.5 Trust-boundary): packet-id 크기 (모든 frame에 박힘).
        public const int PacketIdSize = 2;

        // Phase 03 (M4.1 Trust-boundary symmetry): 상수를 FrameValidator로 일원화.
        // PacketSession은 FrameValidator 인용으로 유지 — 기존 테스트 참조 호환 보존.
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

                // 완전체로 패킷을 수신했는지 확인.
                ushort dataSize = BitConverter.ToUInt16(buffer.Array!, buffer.Offset);

                // Phase 03 (M4.1 Trust-boundary symmetry): FrameValidator 위임.
                // 순서 중요 — 정상 분할 패킷(buffer.Count < dataSize) 체크보다 *먼저*.
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

                // 여기까지 왔으면 패킷 조립 가능

                // 패킷 처리
                OnRecvPacket(new ArraySegment<byte>(buffer.Array!, buffer.Offset, dataSize));
                packetCount++;

                // 패킷 처리 후 버퍼 이동
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
    public abstract class Session
    {
        #region Private Member Variables
        protected Socket? _socket;
        protected int _disconnected = 0; // 연결이 끊어진 상태를 나타내는 플래그 (0: 연결됨, 1: 끊김)

        RecvBuffer _recvBuffer = new RecvBuffer(65535); // 수신 버퍼

        protected object _lock = new object(); // 멀티쓰레드 환경에서 큐에 대한 동기화를 위한 락 객체
        protected Queue<ArraySegment<byte>> _sendQueue = new Queue<ArraySegment<byte>>(); // 전송할 데이터 큐

        protected List<ArraySegment<byte>> _pendingList = new List<ArraySegment<byte>>(); // 현재 전송이 대기 중인 데이터의 버퍼 리스트
        protected SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();
        protected SocketAsyncEventArgs _recvArgs = new SocketAsyncEventArgs();
        #endregion

        public abstract void OnConnected(EndPoint endPoint); // 연결이 성공적으로 완료되었을 때 호출될 메서드
        public abstract void OnDisconnected(EndPoint endPoint); // 연결이 끊어졌을 때 호출될 메서드
        public abstract int  OnRecv(ArraySegment<byte> buffer); // 데이터 수신이 완료되었을 때 호출될 메서드
        public abstract void OnSend(int numOfBytes); // 데이터 전송이 완료되었을 때 호출될 메서드

        void Clear() // 세션 초기화 메서드
        {
            lock (_lock) // 멀티쓰레드 환경에서 큐에 대한 동기화를 위해 락을 사용
            {
                _sendQueue.Clear(); // 전송할 데이터 큐 초기화
                _pendingList.Clear(); // 대기 중인 데이터의 버퍼 리스트 초기화
            }
        }

        // Start : 세션을 초기화하고 비동기적으로 데이터 수신 시작
        public void Start(Socket socket)
        {
            _socket = socket; // 클라이언트와의 통신에 사용할 소켓 저장

            // 1. 데이터 수신이 완료되었을 때 호출될 이벤트 핸들러 등록
            _recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecvCompleted);
            _sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);

            RegisterRecv(); // 비동기적으로 데이터 수신 시작
        }
        // Send : 데이터를 큐에 추가하고 전송 시작
        // virtual: 테스트 subclass에서 실제 socket I/O 차단 가능 (Phase 09 (M2.5) testability).
        public virtual void Send(ArraySegment<byte> sendBuff)
        {
            lock (_lock) // 멀티쓰레드 환경에서 큐에 대한 동기화를 위해 락을 사용
            {
                _sendQueue.Enqueue(sendBuff); // 전송할 데이터를 큐에 추가

                if (_pendingList.Count == 0) // 현재 전송이 대기 중인 데이터가 없다면
                    RegisterSend(); // 새로 추가된 데이터를 전송하기 위해 RegisterSend 호출
            }
        }

        // Send : 데이터를 큐에 추가하고 전송 시작
        public void Send(List<ArraySegment<byte>> sendBuffList)
        {
            if(sendBuffList.Count == 0) // 전송할 데이터가 없는 경우
                return;

            lock (_lock) // 멀티쓰레드 환경에서 큐에 대한 동기화를 위해 락을 사용
            {
                foreach (ArraySegment<byte> sendBuff in sendBuffList)
                    _sendQueue.Enqueue(sendBuff); // 전송할 데이터를 큐에 추가

                if (_pendingList.Count == 0) // 현재 전송이 대기 중인 데이터가 없다면
                    RegisterSend(); // 새로 추가된 데이터를 전송하기 위해 RegisterSend 호출
            }
        }

        // Disconnect : 연결을 종료하는 메서드
        // virtual: 테스트 subclass에서 override 가능 (실제 socket 없이 Disconnect 호출 추적).
        // Phase 09 (M2.5) length-validation 테스트의 필수 testability hook.
        public virtual void Disconnect()
        {
            // 멀티쓰레드 환경에서 교착상태를 방지하기 위해
            // 연결이 끊어진 상태를 나타내는 플래그를 원자적으로 설정
            if (Interlocked.Exchange(ref _disconnected, 1) == 1)
                return; // 이미 연결이 끊어진 상태라면 종료

            OnDisconnected(_socket!.RemoteEndPoint!); // 연결이 끊어졌음을 알림 (player cleanup enqueue 포함)

            // Cross-review γ10 (β13/A5 봉합 + 2라운드 정제): Shutdown / Close / Clear 각 단계 독립 보호.
            // Shutdown이 throw해도 Close가 socket handle을 반드시 닫고(FD 누수 차단),
            // Close가 throw해도 Clear가 SendQueue/PendingList를 반드시 정리. 어느 단계 예외도 다음 단계를 막지 않음.
            // (옛 코드는 Shutdown 예외 시 Close+Clear 둘 다 skip, 1라운드 봉합은 Shutdown 예외 시 Close만 skip이었음.)
            try { _socket!.Shutdown(SocketShutdown.Both); } // 소켓의 송수신을 모두 종료
            catch (Exception e) { Console.WriteLine($"[Session] socket Shutdown 예외 (이미 reset?) — 무시: {e.Message}"); }

            try { _socket!.Close(); } // 소켓 닫기 — Shutdown 예외와 무관하게 반드시 시도
            catch (Exception e) { Console.WriteLine($"[Session] socket Close 예외 — 무시: {e.Message}"); }

            Clear(); // 세션 초기화 — 위 예외들과 무관하게 항상 실행
        }

        #region Network Connection Recv
        // RegisterSend : 데이터를 전송하기 위해 RegisterSend 호출
        private void RegisterSend()
        {
            if (_disconnected == 1) // 연결이 끊어진 상태라면 전송 시도하지 않음
                return;

            while (_sendQueue.Count > 0) // 전송할 데이터가 큐에 남아있는 동안
            {
                ArraySegment<byte> buffer = _sendQueue.Dequeue(); // 큐에서 전송할 데이터를 하나씩 꺼냄
                _pendingList.Add(buffer); // 전송할 데이터를 버퍼 리스트에 추가
            }

            _sendArgs.BufferList = _pendingList;
            // SocketAsyncEventArgs의 BufferList에 대기 중인 데이터의 버퍼 리스트 설정

            try
            {
                bool pending = _socket!.SendAsync(_sendArgs); // 비동기적으로 데이터 전송 시작

                if (pending == false) // SendAsync가 즉시 완료된 경우 (즉, 전송이 대기 상태가 아닌 경우)
                    OnSendCompleted(null, _sendArgs); // 전송이 즉시 완료되었으므로 OnSendCompleted를 직접 호출하여 후속 작업 수행
            }
            catch (Exception e)
            {
                Console.WriteLine($"RegisterSend Failed {e}");
            }
        }

        // OnSendCompleted : 데이터 전송이 완료되었을 때 호출되는 이벤트 핸들러
        private void OnSendCompleted(object? sender, SocketAsyncEventArgs args)
        {
            lock (_lock) // 멀티쓰레드 환경에서 큐에 대한 동기화를 위해 락을 사용
            {
                if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
                { // 전송이 성공적으로 완료된 경우
                    try
                    {
                        _sendArgs.BufferList = null; // 전송이 완료된 후 버퍼 리스트 초기화 (굳이 넣을 필요는 없음)
                        _pendingList.Clear(); // 대기 중인 데이터의 버퍼 리스트 초기화

                        OnSend(_sendArgs.BytesTransferred); // 완료된 전송 처리

                        if (_sendQueue.Count > 0) // 큐에 추가로 전송할 데이터가 남아있는 경우
                        {
                            RegisterSend();
                            // 큐에 추가로 전송할 데이터가 남아있는 경우
                            // RegisterSend를 호출하여 다음 데이터를 전송
                        }                   
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"OnSendCompleted Failed : {ex}");
                    }
                }
                else
                {
                    Disconnect(); // 전송 중 오류가 발생하면 연결 종료
                }
            }        
        }

        // RegisterRecv : 데이터를 수신하기 위해 RegisterRecv 호출
        private void RegisterRecv()
        {
            if (_disconnected == 1) // 연결이 끊어진 상태라면 수신 시도하지 않음
                return;

            _recvBuffer.Clean(); // 버퍼 정리
            ArraySegment<byte> segment = _recvBuffer.WriteSegment; // 수신 버퍼에서 쓸 수 있는 공간의 참조.
            _recvArgs.SetBuffer(segment.Array, segment.Offset, segment.Count); // 수신 버퍼 설정

            try
            {
                bool pending = _socket!.ReceiveAsync(_recvArgs); // 비동기적으로 데이터 수신 시작

                if (pending == false) // ReceiveAsync가 즉시 완료된 경우 (즉, 수신이 대기 상태가 아닌 경우)
                {
                    OnRecvCompleted(null, _recvArgs); // 수신이 즉시 완료되었으므로 OnRecvCompleted를 직접 호출하여 후속 작업 수행
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"RegisterRecv Failed {e}");
            }
        }

        // OnRecvCompleted : 데이터 수신이 완료되었을 때 호출되는 이벤트 핸들러
        private void OnRecvCompleted(object? sender, SocketAsyncEventArgs args)
        {
            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
            { 
                // 수신이 성공적으로 완료된 경우
                try
                { 
                    // Write 커서 이동.
                    if (_recvBuffer.OnWrite(args.BytesTransferred) == false) // 쓰기 커서를 이동시켰는데, 버퍼가 꽉 찼다면
                    {
                        Disconnect(); // 연결 종료
                        return;
                    }

                    // 읽을 수 있는 데이터의 참조를 전달
                    int processedSize = OnRecv(_recvBuffer.ReadSegment);
                    if (processedSize < 0 || processedSize > _recvBuffer.DataSize)
                    {
                        // 처리된 데이터의 크기가 음수이거나, 버퍼의 크기보다 크다면
                        Disconnect(); // 연결 종료
                        return;
                    }

                    // Read 커서 이동.
                    if (_recvBuffer.OnRead(processedSize) == false) 
                    {
                        // 읽은 데이터의 크기만큼 Read 커서 이동시켰는데, 버퍼가 꽉 찼다면
                        Disconnect(); // 연결 종료
                        return;
                    }

                    RegisterRecv(); // 다음 데이터 수신을 위해 다시 비동기적으로 수신 시작
                }
                catch (Exception ex)
                {
                    // Phase 09 (M2.5 Trust-boundary): decode 예외 fail-closed.
                    // 이전엔 로그만 찍고 세션이 half-open 상태로 잔존 (recv 멈추되 socket은 살아있음).
                    // 이제 명시적으로 Disconnect → 자원 회수 + GameSession.OnDisconnected 트리거.
                    // RegisterRecv는 호출하지 않음 — Disconnect가 그 길을 차단.
                    Console.WriteLine($"[Trust] OnRecvCompleted decode failed — disconnect: {ex.Message}");
                    Disconnect();
                }
            }
            else
            {
                Disconnect(); // 수신 중 오류가 발생하면 연결 종료
            }
        }
        #endregion
    }
}

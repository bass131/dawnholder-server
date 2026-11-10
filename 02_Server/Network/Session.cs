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

            while (true)
            {
                // 최소한 헤더는 파싱할 수 있는지 확인.
                if (buffer.Count < HeaderSize)
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

                if (buffer.Count < dataSize)
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

        const long MaxSendPendingBackingBytes = 32L * 1024 * 1024;
        const int MaxSendPendingSegments = 4096;

        // _lock이 queue와 진행 중인 pending의 합을 소유한다. 완료 전에는 차감하지 않는다.
        long _sendPendingBackingBytes;
        int _sendPendingSegments;
        long _sendPendingPeakBackingBytes;
        int _sendPendingPeakSegments;
        bool _sendInProgress;
        EndPoint? _remoteEndPoint;
        #endregion

        public abstract void OnConnected(EndPoint endPoint);
        public abstract void OnDisconnected(EndPoint endPoint);
        public abstract int OnRecv(ArraySegment<byte> buffer);
        public abstract void OnSend(int numOfBytes);

        public void Start(Socket socket)
        {
            _socket = socket;
            _remoteEndPoint = GetRemoteEndPoint();

            _recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecvCompleted);
            _sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);

            RegisterRecv();
        }

        // virtual: 테스트 subclass에서 실제 socket I/O 차단 가능 (testability hook).
        public virtual void Send(ArraySegment<byte> sendBuff)
        {
            bool failed;
            lock (_lock)
            {
                if (Volatile.Read(ref _disconnected) != 0)
                    return;

                failed = !TryEnqueueSend(sendBuff);
                if (!failed && !_sendInProgress)
                    failed = !RegisterSend();
            }

            if (failed)
                DisconnectAfterTransportFailure();
        }

        public void Send(List<ArraySegment<byte>> sendBuffList)
        {
            if (sendBuffList.Count == 0)
                return;

            bool failed = false;
            lock (_lock)
            {
                if (Volatile.Read(ref _disconnected) != 0)
                    return;

                foreach (ArraySegment<byte> sendBuff in sendBuffList)
                {
                    if (!TryEnqueueSend(sendBuff))
                    {
                        failed = true;
                        break;
                    }
                }

                if (!failed && !_sendInProgress)
                    failed = !RegisterSend();
            }

            if (failed)
                DisconnectAfterTransportFailure();
        }

        // virtual: 테스트 subclass에서 override 가능 (실제 socket 없이 Disconnect 호출 추적).
        public virtual void Disconnect()
        {
            // 재진입/멀티스레드 교착 방지 — 플래그를 원자적으로 설정.
            if (Interlocked.Exchange(ref _disconnected, 1) == 1)
                return;

            bool notificationFailed = false;
            try
            {
                OnDisconnected(GetRemoteEndPoint()); // player cleanup enqueue 포함
            }
            catch
            {
                notificationFailed = true;
                throw;
            }
            finally
            {
                try
                {
                    CleanupTransport();
                }
                catch when (notificationFailed)
                {
                    // 콜백의 원래 예외가 정리 예외에 가려지지 않게 한다.
                }
            }
        }

        EndPoint GetRemoteEndPoint()
        {
            try
            {
                return _socket?.RemoteEndPoint ?? _remoteEndPoint ?? new IPEndPoint(IPAddress.Any, 0);
            }
            catch (Exception error)
            {
                // 먼저 닫힌 소켓도 peer 주소 조회 실패와 무관하게 정리 콜백을 받아야 한다.
                ReportCleanupFailure("RemoteEndPoint", error);
                return _remoteEndPoint ?? new IPEndPoint(IPAddress.Any, 0);
            }
        }

        void DisconnectAfterTransportFailure()
        {
            try { Disconnect(); }
            catch (Exception error)
            {
                // 전송 경계의 실패/알림 예외는 Send를 호출한 틱까지 전파하지 않는다.
                // 직접 Disconnect를 호출하면 기존처럼 알림 예외를 다시 던진다.
                ReportCleanupFailure("Disconnect", error);
            }
        }

        void CleanupTransport()
        {
            // 각 단계와 진단 출력을 분리해 Shutdown 실패에도 Close/Clear를 시도한다.
            try { _socket!.Shutdown(SocketShutdown.Both); }
            catch (Exception e) { ReportCleanupFailure("Shutdown", e); }

            try { _socket!.Close(); }
            catch (Exception e) { ReportCleanupFailure("Close", e); }

            Clear();
        }

        static void ReportCleanupFailure(string stage, Exception error)
        {
            try { Console.WriteLine($"[Session] socket {stage} 예외 — 무시: {error.Message}"); }
            catch
            {
                // 진단 출력 실패는 transport 정리를 중단할 이유가 아니다.
            }
        }

        void Clear()
        {
            long peakBackingBytes;
            int peakSegments;
            lock (_lock)
            {
                _sendQueue.Clear();
                _pendingList.Clear();
                _sendPendingBackingBytes = 0;
                _sendPendingSegments = 0;
                _sendInProgress = false;
                peakBackingBytes = _sendPendingPeakBackingBytes;
                peakSegments = _sendPendingPeakSegments;

                try { _sendArgs.BufferList = null; }
                catch (InvalidOperationException)
                {
                    // 취소가 아직 완료되지 않은 I/O는 완료 콜백에서 배열 참조를 해제한다.
                }
            }

            try
            {
                Console.WriteLine($"[Session] send-pending peak: backingBytes={peakBackingBytes} segments={peakSegments}");
            }
            catch
            {
                // 측정 출력 실패도 연결 정리나 원래 알림 예외를 가리지 않는다.
            }
        }

        #region Network Connection Recv
        private bool TryEnqueueSend(ArraySegment<byte> buffer)
        {
            // 생성 Write()는 작은 패킷도 65,535바이트 배열을 붙드므로 Count가 아닌 배열 크기를 센다.
            long backingBytes = buffer.Array?.LongLength ?? 0;
            if (Volatile.Read(ref _disconnected) != 0 ||
                _sendPendingBackingBytes + backingBytes > MaxSendPendingBackingBytes ||
                _sendPendingSegments >= MaxSendPendingSegments)
            {
                return false;
            }

            _sendQueue.Enqueue(buffer);
            _sendPendingBackingBytes += backingBytes;
            _sendPendingSegments++;
            _sendPendingPeakBackingBytes = Math.Max(_sendPendingPeakBackingBytes, _sendPendingBackingBytes);
            _sendPendingPeakSegments = Math.Max(_sendPendingPeakSegments, _sendPendingSegments);
            return true;
        }

        private bool RegisterSend()
        {
            try
            {
                // OnSend가 재진입해 Send해도 현재 pump가 FIFO로 다음 등록을 맡는다.
                _sendInProgress = true;
                while (Volatile.Read(ref _disconnected) == 0 && _sendQueue.Count > 0)
                {
                    while (_sendQueue.Count > 0)
                        _pendingList.Add(_sendQueue.Dequeue());

                    _sendArgs.BufferList = _pendingList;
                    if (_socket!.SendAsync(_sendArgs))
                        return true;

                    if (!ProcessSendCompletion(_sendArgs))
                        return false;
                }

                _sendInProgress = false;
                return true;
            }
            catch (Exception e)
            {
                ReportCleanupFailure("RegisterSend", e);
                return false;
            }
        }

        private void OnSendCompleted(object? sender, SocketAsyncEventArgs args)
        {
            bool failed;
            lock (_lock)
            {
                try
                {
                    if (Volatile.Read(ref _disconnected) != 0)
                    {
                        _sendArgs.BufferList = null;
                        return;
                    }

                    failed = !ProcessSendCompletion(args) || !RegisterSend();
                }
                catch (Exception error)
                {
                    ReportCleanupFailure("OnSendCompleted", error);
                    failed = true;
                }
            }

            if (failed)
                DisconnectAfterTransportFailure();
        }

        private bool ProcessSendCompletion(SocketAsyncEventArgs args)
        {
            if (args.BytesTransferred <= 0 || args.SocketError != SocketError.Success)
                return false;

            _sendArgs.BufferList = null;
            foreach (ArraySegment<byte> buffer in _pendingList)
                _sendPendingBackingBytes -= buffer.Array?.LongLength ?? 0;
            _sendPendingSegments -= _pendingList.Count;
            _pendingList.Clear();

            OnSend(args.BytesTransferred);
            return true;
        }

        private void RegisterRecv()
        {
            try
            {
                while (Volatile.Read(ref _disconnected) == 0)
                {
                    _recvBuffer.Clean();
                    ArraySegment<byte> segment = _recvBuffer.WriteSegment;
                    _recvArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);

                    if (_socket!.ReceiveAsync(_recvArgs))
                        return;

                    // 즉시 완료도 비동기 완료와 같은 검사를 거치고, 재귀 없이 다음 수신을 등록한다.
                    if (!ProcessRecvCompletion(_recvArgs))
                    {
                        DisconnectAfterTransportFailure();
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                ReportCleanupFailure("RegisterRecv", e);
                DisconnectAfterTransportFailure();
            }
        }

        private void OnRecvCompleted(object? sender, SocketAsyncEventArgs args)
        {
            try
            {
                if (ProcessRecvCompletion(args))
                {
                    RegisterRecv();
                    return;
                }
            }
            catch (Exception error)
            {
                ReportCleanupFailure("OnRecvCompleted", error);
            }

            DisconnectAfterTransportFailure();
        }

        private bool ProcessRecvCompletion(SocketAsyncEventArgs args)
        {
            if (Volatile.Read(ref _disconnected) != 0 ||
                args.BytesTransferred <= 0 || args.SocketError != SocketError.Success)
            {
                return false;
            }

            if (!_recvBuffer.OnWrite(args.BytesTransferred))
                return false;

            int processedSize = OnRecv(_recvBuffer.ReadSegment);
            if (processedSize < 0 || processedSize > _recvBuffer.DataSize ||
                Volatile.Read(ref _disconnected) != 0)
            {
                return false;
            }

            return _recvBuffer.OnRead(processedSize);
        }
        #endregion
    }
}

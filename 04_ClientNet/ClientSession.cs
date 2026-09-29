using System.Net;
using System.Net.Sockets;

namespace Dawnholder.Client.Net;

// ─────────────────────────────────────────────────────────────────────────────
// ⚠️ Unity main thread 침범 금지
//
// 콜백은 호출 스레드 또는 socket 워커에서 실행될 수 있으며 스레드 보장이 없습니다.
// Unity main thread라고 가정하지 않습니다. 따라서:
//
//   - GameObject / Transform / MonoBehaviour 등 Unity API 직접 호출 금지.
//   - 받은 데이터/이벤트는 main-thread queue에 박아두고 Unity의 Update()에서
//     drain 하는 패턴을 사용한다.
//
// 위반 시 런타임 예외:
//   UnityException: get_isActiveAndEnabled can only be called from the main thread
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// 길이 prefix(2byte) 기반 패킷 프레이밍을 제공하는 Session 래퍼.
///
/// **클라 컨텍스트 메모**:
/// 서버측 PacketSession과 동일 알고리즘. 클라도 TCP byte stream을 받기 때문에
/// 한 번의 OnRecv에 패킷이 0.5개 / 1개 / 1.5개 / N개 섞여 올 수 있음.
/// → 헤더(2byte size)만큼 모이면 size를 읽고, size만큼 모이면 한 패킷으로 잘라
///    OnRecvPacket에 던지고 반복.
///
/// 패킷 와이어 포맷: [size(2)][packetId(2)][payload...] 가 N번 반복.
/// </summary>
public abstract class PacketSession : ClientSession
{
    public static readonly int HeaderSize = 2;

    public sealed override int OnRecv(ArraySegment<byte> buffer)
    {
        int processLen = 0;
        int packetCount = 0;

        while (true)
        {
            // 최소한 헤더(2byte)는 읽을 수 있어야 size를 알 수 있음.
            if (buffer.Count < HeaderSize)
                break;

            // 패킷이 통째로 도착했는지 확인.
            ushort dataSize = BitConverter.ToUInt16(buffer.Array!, buffer.Offset);

            // Trust Boundary: invalid frame size는 fail-closed.
            // 순서 중요 — 정상 분할 패킷(buffer.Count < dataSize) 체크보다 *먼저*.
            // 안 그러면 dataSize=0(무한루프) / dataSize=70000(attack frame)이
            // wait에 잡혀 disconnect 안 됨.
            if (!FrameValidator.TryValidateFrameHeader(dataSize, out var reason))
            {
                Console.WriteLine($"[Trust] invalid frame size {dataSize} ({reason}) — disconnect");
                Disconnect();
                return processLen;
            }

            if (buffer.Count < dataSize)
                break;

            // 한 패킷 분량을 잘라서 핸들러로.
            OnRecvPacket(new ArraySegment<byte>(buffer.Array!, buffer.Offset, dataSize));
            packetCount++;

            // 다음 패킷으로 커서 전진.
            processLen += dataSize;
            buffer = new ArraySegment<byte>(
                buffer.Array!,
                buffer.Offset + dataSize,
                buffer.Count - dataSize);
        }

        if (packetCount > 1)
        {
            // 클라에선 거의 안 일어나야 정상 (서버 push가 burst로 올 때만) — 진단 로그.
            Console.WriteLine($"[ClientPacketSession] 모아받기 {packetCount} Packets");
        }

        return processLen;
    }

    /// <summary>패킷 1개가 완전히 모였을 때 호출. 직렬화 해석은 상위 계층에서.</summary>
    public abstract void OnRecvPacket(ArraySegment<byte> buffer);
}

/// <summary>
/// 클라이언트 socket 세션의 base 클래스.
///
/// **서버 Session과의 비대칭** (Y2 갈래 핵심):
/// - 서버 Session: 다수 connection 중 1개. Listener가 accept 후 만들어 풀에 둠.
/// - 클라 Session: 단일 connection. Connector가 connect 후 1회 Start 호출.
///   동일 패턴(SocketAsyncEventArgs + RecvBuffer + SendQueue + PendingList)을
///   유지한 이유 = 한 번 익혀 둔 비동기 socket 패턴을 양쪽에서 *왜 쓰는지*
///   인지하는 것이 학습 가치 (ADR-012).
/// </summary>
public abstract class ClientSession
{
    #region Private Member Variables

    protected Socket? _socket;
    protected int _disconnected = 0; // 0 = 연결됨, 1 = 끊김. Interlocked로만 변경.
    // _lock: Send 호출이 main thread / socket worker thread 양쪽에서 들어올 수
    // 있어 큐 보호 필요. 클라라고 단순화 안 한 이유 = 위 메모의 두 스레드 시나리오.
    protected readonly object _lock = new object();
    protected readonly Queue<ArraySegment<byte>> _sendQueue = new Queue<ArraySegment<byte>>();
    protected readonly List<ArraySegment<byte>> _pendingList = new List<ArraySegment<byte>>();
    protected readonly SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();
    protected readonly SocketAsyncEventArgs _recvArgs = new SocketAsyncEventArgs();

    readonly object _lifecycleGate = new();
    EndPoint _remoteEndPoint = new IPEndPoint(IPAddress.None, 0);
    readonly RecvBuffer _recvBuffer = new RecvBuffer(65535);

    #endregion

    public bool IsDisconnected => Volatile.Read(ref _disconnected) != 0;

    public abstract void OnConnected(EndPoint endPoint);
    public abstract void OnDisconnected(EndPoint endPoint);
    public abstract int OnRecv(ArraySegment<byte> buffer);
    public abstract void OnSend(int numOfBytes);

    /// <summary>Connector가 connect 성공 후 호출. 비동기 수신 시작.</summary>
    public void Start(Socket socket)
    {
        if (socket == null) throw new ArgumentNullException(nameof(socket));
        try
        {
            lock (_lifecycleGate)
            {
                if (IsDisconnected)
                {
                    socket.Dispose();
                    return;
                }
                if (_socket != null)
                {
                    if (!ReferenceEquals(_socket, socket)) socket.Dispose();
                    throw new InvalidOperationException("A client session can only be started once.");
                }
                _socket = socket;
                _remoteEndPoint = socket.RemoteEndPoint ?? _remoteEndPoint;
                _recvArgs.Completed += OnRecvCompleted;
                _sendArgs.Completed += OnSendCompleted;
            }
            RegisterRecv();
        }
        catch (Exception error)
        {
            Console.WriteLine($"[ClientSession] Start failed: {error}");
            Disconnect();
        }
    }

    /// <summary>패킷 1개를 전송 큐에 넣고, 대기 중이 없으면 즉시 전송 시작.</summary>
    public void Send(ArraySegment<byte> sendBuff)
    {
        lock (_lock)
        {
            if (IsDisconnected) return;
            _sendQueue.Enqueue(sendBuff);

            if (_pendingList.Count == 0)
                RegisterSend();
        }
    }

    /// <summary>여러 패킷을 한 번에 전송 큐에 넣음 (모아보내기).</summary>
    public void Send(List<ArraySegment<byte>> sendBuffList)
    {
        if (sendBuffList.Count == 0)
            return;

        lock (_lock)
        {
            if (IsDisconnected) return;
            foreach (ArraySegment<byte> sendBuff in sendBuffList)
                _sendQueue.Enqueue(sendBuff);

            if (_pendingList.Count == 0)
                RegisterSend();
        }
    }

    /// <summary>연결 종료. 중복 호출 방지(Interlocked) + 양방향 shutdown + close.</summary>
    public void Disconnect()
    {
        // Interlocked로 0→1 전이를 원자적으로 보장. 다른 스레드가 동시에 호출해도 1회만 실행.
        if (Interlocked.Exchange(ref _disconnected, 1) == 1)
            return;

        Socket? socket;
        EndPoint endPoint;
        lock (_lifecycleGate)
        {
            socket = _socket;
            endPoint = _remoteEndPoint;
        }

        // Notification failures must not skip transport cleanup (including pre-Start close).
        try { OnDisconnected(endPoint); }
        catch (Exception error) { Console.WriteLine($"[ClientSession] OnDisconnected failed: {error}"); }

        // Shutdown / Close / Clear 각 단계 독립 보호.
        // Shutdown throw → Close 여전히 실행(FD 누수 차단), Close throw → Clear 여전히 실행.
        // 서버 Session.cs와 자매 봉합 (ADR-012 Y2 분리 정합 — 양쪽 동시 수정).
        try { socket?.Shutdown(SocketShutdown.Both); }
        catch (Exception e) { Console.WriteLine($"[ClientSession] socket Shutdown 예외 (이미 reset?) — 무시: {e.Message}"); }

        try { socket?.Close(); }
        catch (Exception e) { Console.WriteLine($"[ClientSession] socket Close 예외 — 무시: {e.Message}"); }

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

    #region Network Connection Send / Recv

    void RegisterSend()
    {
        if (_disconnected == 1)
            return;

        // 큐에 쌓인 모든 segment를 PendingList로 옮김 → 한 번의 SendAsync 호출로 묶어 보냄.
        while (_sendQueue.Count > 0)
        {
            ArraySegment<byte> buffer = _sendQueue.Dequeue();
            _pendingList.Add(buffer);
        }

        _sendArgs.BufferList = _pendingList;

        try
        {
            bool pending = _socket!.SendAsync(_sendArgs);

            // SendAsync가 즉시 완료(=동기 처리)된 경우엔 Completed 이벤트가 안 뜸 → 직접 호출.
            if (pending == false)
                OnSendCompleted(null, _sendArgs);
        }
        catch (Exception e)
        {
            Console.WriteLine($"[ClientSession] RegisterSend Failed {e}");
            Disconnect();
        }
    }

    void OnSendCompleted(object? sender, SocketAsyncEventArgs args)
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
                        RegisterSend();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ClientSession] OnSendCompleted Failed : {ex}");
                    Disconnect();
                }
            }
            else
            {
                Disconnect();
            }
        }
    }

    void RegisterRecv()
    {
        if (_disconnected == 1)
            return;

        _recvBuffer.Clean();
        ArraySegment<byte> segment = _recvBuffer.WriteSegment;
        _recvArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);

        try
        {
            bool pending = _socket!.ReceiveAsync(_recvArgs);

            if (pending == false)
                OnRecvCompleted(null, _recvArgs);
        }
        catch (Exception e)
        {
            Console.WriteLine($"[ClientSession] RegisterRecv Failed {e}");
            Disconnect();
        }
    }

    void OnRecvCompleted(object? sender, SocketAsyncEventArgs args)
    {
        if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
        {
            try
            {
                // Write 커서 이동. 실패 = 버퍼 invariant 깨짐 = 즉시 끊고 종료.
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
                Console.WriteLine($"[ClientSession] OnRecvCompleted Failed : {ex}");
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

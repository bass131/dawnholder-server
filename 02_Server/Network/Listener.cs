using System.Net;
using System.Net.Sockets;

namespace Dawnholder.Server.Network;

/// <summary>Accept와 세션 시작을 소유한다. Stop은 제어 스레드에서 호출한다.</summary>
public sealed class Listener : IDisposable
{
    readonly object _gate = new();
    Socket? _listenSocket;
    CancellationTokenSource? _cancellation;
    Task[] _acceptLoops = Array.Empty<Task>();
    Task _completion = Task.CompletedTask;
    bool _initialized;
    bool _stopping;

    public EndPoint? LocalEndPoint { get { lock (_gate) return _listenSocket?.LocalEndPoint; } }
    public Task Completion => Volatile.Read(ref _completion);

    public void Init(IPEndPoint endPoint, Func<Session> sessionFactory, int register = 10, int backLog = 100)
    {
        ArgumentNullException.ThrowIfNull(sessionFactory);
        if (register <= 0) throw new ArgumentOutOfRangeException(nameof(register));
        lock (_gate)
        {
            if (_initialized || _stopping) throw new InvalidOperationException("Listener는 한 번만 시작할 수 있습니다.");
            _initialized = true;
            Socket socket = new(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                socket.Bind(endPoint);
                socket.Listen(backLog);
                _listenSocket = socket;
                _cancellation = new CancellationTokenSource();
                CancellationToken token = _cancellation.Token;
                _acceptLoops = Enumerable.Range(0, register)
                    .Select(_ => Task.Run(() => AcceptLoopAsync(socket, sessionFactory, token)))
                    .ToArray();
                Volatile.Write(ref _completion, Task.WhenAll(_acceptLoops));
            }
            catch
            {
                socket.Dispose();
                _listenSocket = null;
                throw;
            }
        }
    }

    public Socket Accept()
    {
        Socket socket;
        lock (_gate)
        {
            if (_stopping || _listenSocket == null) throw new InvalidOperationException("Listener가 실행 중이 아닙니다.");
            socket = _listenSocket;
        }
        return socket.Accept();
    }

    public void Stop(TimeSpan? timeout = null)
    {
        Task completion;
        TimeSpan wait = timeout ?? TimeSpan.FromSeconds(2);
        Volatile.Write(ref _stopping, true);
        if (!Monitor.TryEnter(_gate, wait))
            throw new TimeoutException("진행 중인 세션 시작이 아직 완료되지 않았습니다.");
        try
        {
            _cancellation?.Cancel();
            _listenSocket?.Dispose();
            _listenSocket = null;
            completion = Completion;
        }
        finally { Monitor.Exit(_gate); }
        try
        {
            if (!completion.Wait(wait))
                throw new TimeoutException("진행 중인 accept가 아직 종료되지 않았습니다.");
        }
        finally
        {
            // 완료된 fault와 아직 실행 중인 timeout을 구분한다. fault 자체는 호출자에게 전파한다.
            if (completion.IsCompleted)
            {
                lock (_gate)
                {
                    _cancellation?.Dispose();
                    _cancellation = null;
                }
            }
        }
    }

    public void Dispose() => Stop();

    async Task AcceptLoopAsync(Socket socket, Func<Session> sessionFactory, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            Socket accepted;
            try { accepted = await socket.AcceptAsync(token).ConfigureAwait(false); }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { return; }
            catch (ObjectDisposedException) when (token.IsCancellationRequested) { return; }
            catch (SocketException) when (token.IsCancellationRequested) { return; }
            catch (SocketException ex) when (IsTransientAcceptError(ex.SocketErrorCode))
            {
                Console.WriteLine($"[Listener] accept 재시도: {ex.SocketErrorCode}");
                try { await Task.Delay(50, token).ConfigureAwait(false); }
                catch (OperationCanceledException) when (token.IsCancellationRequested) { return; }
                continue;
            }

            // Stop과 session.Start를 같은 전송 경계에서 직렬화한다. 게임 틱은 이 lock을 쓰지 않는다.
            lock (_gate)
            {
                if (_stopping)
                {
                    accepted.Dispose();
                    continue;
                }
                Session? session = null;
                try
                {
                    EndPoint remote = accepted.RemoteEndPoint
                        ?? throw new InvalidOperationException("Accepted socket의 endpoint가 없습니다.");
                    session = sessionFactory();
                    session.Start(accepted);
                    session.OnConnected(remote);
                }
                catch (Exception ex)
                {
                    try { session?.Disconnect(); }
                    catch (Exception cleanupError) { Console.WriteLine($"[Listener] session cleanup: {cleanupError.Message}"); }
                    accepted.Dispose();
                    Console.WriteLine($"[Listener] accepted session: {ex.Message}");
                }
            }
        }
    }

    static bool IsTransientAcceptError(SocketError error)
        => error is SocketError.ConnectionAborted or SocketError.ConnectionReset
            or SocketError.Interrupted or SocketError.TryAgain or SocketError.WouldBlock
            or SocketError.NoBufferSpaceAvailable or SocketError.TooManyOpenSockets;
}

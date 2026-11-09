using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Dawnholder.Server.Network;

namespace GameServer.Tests.Network;

// Real loopback transport and read-only observations shared by session contract tests.
internal sealed class SessionSocketPair(Socket local, Socket peer) : IDisposable
{
    internal static readonly TimeSpan Limit = TimeSpan.FromSeconds(5);

    internal Socket Local { get; } = local;
    internal Socket Peer { get; } = peer;

    internal static async Task<SessionSocketPair> Connect(int receiveBuffer = 4096)
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Server.ReceiveBufferSize = receiveBuffer;
        listener.Start();
        var local = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
        {
            NoDelay = true,
            SendBufferSize = 4096,
            ReceiveBufferSize = receiveBuffer,
        };
        try
        {
            await local.ConnectAsync(listener.LocalEndpoint).WaitAsync(Limit);
            Socket peer = await listener.AcceptSocketAsync().WaitAsync(Limit);
            peer.NoDelay = true;
            return new SessionSocketPair(local, peer);
        }
        catch
        {
            local.Dispose();
            throw;
        }
    }

    internal static async Task<bool> WaitUntil(Func<bool> condition)
    {
        var elapsed = Stopwatch.StartNew();
        while (!condition() && elapsed.Elapsed < Limit)
            await Task.Delay(10);
        return condition();
    }

    public void Dispose()
    {
        Local.Dispose();
        Peer.Dispose();
    }
}

internal sealed class TransportProbeSession : Session, IDisposable
{
    private int _notifications;

    internal int Notifications => Volatile.Read(ref _notifications);
    internal bool Disconnected => Volatile.Read(ref _disconnected) != 0;
    internal ConcurrentQueue<int> SendCompletions { get; } = new();
    internal Action<int>? Sent { get; set; }
    internal Func<ArraySegment<byte>, int>? Received { get; set; }

    internal (int Queued, int Pending, long RetainedBytes) Buffers
    {
        get
        {
            lock (_lock)
            {
                long bytes = _sendQueue.Sum(segment => (long)segment.Array!.Length)
                    + _pendingList.Sum(segment => (long)segment.Array!.Length);
                return (_sendQueue.Count, _pendingList.Count, bytes);
            }
        }
    }

    internal void AttachWithoutReceive(Socket socket) => _socket = socket;

    public override void OnConnected(EndPoint endPoint) { }
    public override void OnDisconnected(EndPoint endPoint) => Interlocked.Increment(ref _notifications);
    public override int OnRecv(ArraySegment<byte> buffer) => Received?.Invoke(buffer) ?? buffer.Count;

    public override void OnSend(int numOfBytes)
    {
        SendCompletions.Enqueue(numOfBytes);
        Sent?.Invoke(numOfBytes);
    }

    public void Dispose()
    {
        // Closed-socket registration tests deliberately make RemoteEndPoint unavailable.
        try { Disconnect(); }
        catch (ObjectDisposedException) { }
    }
}

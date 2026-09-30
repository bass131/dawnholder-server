using System.Net;
using System.Net.Sockets;
using System.Text;
using Dawnholder.Server.Network;

namespace GameServer.Tests.Network;

[Collection("ConsoleSerial")]
public sealed class SessionDisconnectCleanupTests
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(5);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Disconnect_NotifiesBeforeClose_CleansBeforeReturningOrRethrowing(bool throws)
    {
        using SocketPair pair = await SocketPair.Connect();
        var expected = new InvalidOperationException("termination observer failed");
        var session = new ProbeSession { CallbackError = throws ? expected : null };
        session.Start(pair.Local);
        session.SeedPendingBuffers();

        Exception? observed = Record.Exception(session.Disconnect);

        Assert.Same(throws ? expected : null, observed);
        Assert.Equal(1, session.Notifications);
        Assert.True(session.SocketOpenAtNotification);
        Assert.Equal(pair.Peer.LocalEndPoint, session.NotificationEndpoint);
        Assert.True(pair.Local.SafeHandle.IsClosed);
        Assert.Equal(0, session.QueuedBytes);
        Assert.Equal(0, session.PendingBytes);
        await AssertPeerEof(pair.Peer);
        session.Disconnect();
        Assert.Equal(1, session.Notifications);
    }

    [Fact]
    public async Task Disconnect_CallbackClosesSocketThenThrows_PreservesOriginalExceptionAndClearsBuffers()
    {
        using SocketPair pair = await SocketPair.Connect();
        var expected = new InvalidOperationException("original callback error");
        var session = new ProbeSession { CallbackError = expected, CloseSocketInCallback = true };
        session.Start(pair.Local);
        session.SeedPendingBuffers();

        Exception? observed = Record.Exception(session.Disconnect);

        Assert.Same(expected, observed);
        Assert.True(session.SocketOpenAtNotification);
        Assert.True(pair.Local.SafeHandle.IsClosed);
        Assert.Equal(0, session.QueuedBytes);
        Assert.Equal(0, session.PendingBytes);
        // The observer disposed a socket with a pending receive. The peer may
        // observe reset instead of FIN; either shows this already-closed transport.
        SocketException? reset = null;
        try { await AssertPeerEof(pair.Peer); }
        catch (SocketException error) { reset = error; }
        if (reset != null) Assert.Equal(SocketError.ConnectionReset, reset.SocketErrorCode);
        session.Disconnect();
        Assert.Equal(1, session.Notifications);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Disconnect_EndpointUnavailable_ClearsBuffersEvenWhenDiagnosticWriterFails(bool logThrows)
    {
        using SocketPair pair = await SocketPair.Connect();
        var session = new ProbeSession();
        // Keep receive completion from racing this endpoint-failure case. The real
        // connected socket is attached through the existing protected session seam.
        session.AttachWithoutReceive(pair.Local);
        session.SeedPendingBuffers();
        pair.Local.Dispose();
        using var writer = new ThrowingWriter();
        TextWriter original = Console.Out;
        Exception? observed;
        try
        {
            if (logThrows) Console.SetOut(writer);
            observed = Record.Exception(session.Disconnect);
        }
        finally
        {
            if (logThrows) Console.SetOut(original);
        }

        Assert.IsType<ObjectDisposedException>(observed);
        Assert.Equal(0, session.Notifications);
        Assert.Equal(0, session.QueuedBytes);
        Assert.Equal(0, session.PendingBytes);
        if (logThrows) Assert.True(writer.Attempts > 0);
        session.Disconnect();
        Assert.Equal(0, session.Notifications);
    }

    private static async Task AssertPeerEof(Socket peer)
    {
        using var timeout = new CancellationTokenSource(Limit);
        int received = await peer.ReceiveAsync(new byte[1], SocketFlags.None, timeout.Token);
        Assert.Equal(0, received);
    }

    private sealed class ProbeSession : Session
    {
        public Exception? CallbackError { get; init; }
        public bool CloseSocketInCallback { get; init; }
        public int Notifications { get; private set; }
        public bool SocketOpenAtNotification { get; private set; }
        public EndPoint? NotificationEndpoint { get; private set; }
        public int QueuedBytes => _sendQueue.Sum(segment => segment.Count);
        public int PendingBytes => _pendingList.Sum(segment => segment.Count);

        public void AttachWithoutReceive(Socket socket) => _socket = socket;

        public void SeedPendingBuffers()
        {
            // Seed owned containers without relying on kernel backpressure. This
            // asserts managed cleanup, not cancellation of an in-flight OS send.
            _sendQueue.Enqueue(new ArraySegment<byte>(new byte[] { 1, 2 }));
            _pendingList.Add(new ArraySegment<byte>(new byte[] { 3, 4, 5 }));
        }

        public override void OnConnected(EndPoint endPoint) { }

        public override void OnDisconnected(EndPoint endPoint)
        {
            Notifications++;
            NotificationEndpoint = endPoint;
            SocketOpenAtNotification = !_socket!.SafeHandle.IsClosed;
            if (CloseSocketInCallback) _socket.Dispose();
            if (CallbackError != null) throw CallbackError;
        }

        public override int OnRecv(ArraySegment<byte> buffer) => buffer.Count;
        public override void OnSend(int numOfBytes) { }
    }

    private sealed class ThrowingWriter : TextWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
        public int Attempts { get; private set; }
        public override void WriteLine(string? value)
        {
            Attempts++;
            throw new IOException("diagnostic sink unavailable");
        }
    }

    private sealed class SocketPair(Socket local, Socket peer) : IDisposable
    {
        public Socket Local { get; } = local;
        public Socket Peer { get; } = peer;

        public static async Task<SocketPair> Connect()
        {
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var local = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                await local.ConnectAsync(listener.LocalEndpoint).WaitAsync(Limit);
                Socket peer = await listener.AcceptSocketAsync().WaitAsync(Limit);
                return new SocketPair(local, peer);
            }
            catch
            {
                local.Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            Local.Dispose();
            Peer.Dispose();
        }
    }
}

using System.Net;
using System.Net.Sockets;
using Dawnholder.Client.Net;

namespace Dawnholder.Server.GameServer.Tests.Network;

public sealed class ClientConnectorLifecycleTests
{
    static readonly TimeSpan Limit = TimeSpan.FromSeconds(5);
    static TcpListener Listen()
    {
        TcpListener listener = new(IPAddress.Loopback, 0);
        listener.Start();
        return listener;
    }

    [Fact]
    public async Task SuccessfulAttempt_TransfersSocketOnce_DisposeDoesNotCloseTakenSocket()
    {
        using TcpListener listener = Listen();
        using IConnectionAttempt attempt = new Connector().BeginConnect((IPEndPoint)listener.LocalEndpoint);
        using Socket peer = await listener.AcceptSocketAsync().WaitAsync(Limit);
        Assert.Equal(ConnectionStatus.Success, (await attempt.Completion.WaitAsync(Limit)).Status);
        Assert.True(attempt.TryTakeConnectedSocket(out Socket? socket));
        using Socket owned = socket!;
        Assert.False(attempt.TryTakeConnectedSocket(out _));
        attempt.Dispose();
        await owned.SendAsync(new byte[] { 42 }, SocketFlags.None);
        byte[] received = new byte[1];
        using CancellationTokenSource timeout = new(Limit);
        Assert.Equal(1, await peer.ReceiveAsync(received, SocketFlags.None, timeout.Token));
        Assert.Equal(42, received[0]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CompletedButUntakenSocket_CancelOrDisposeClosesIt(bool dispose)
    {
        using TcpListener listener = Listen();
        using IConnectionAttempt attempt = new Connector().BeginConnect((IPEndPoint)listener.LocalEndpoint);
        using Socket peer = await listener.AcceptSocketAsync().WaitAsync(Limit);
        ConnectionOutcome outcome = await attempt.Completion.WaitAsync(Limit);
        Assert.Equal(ConnectionStatus.Success, outcome.Status);
        if (dispose) attempt.Dispose(); else attempt.Cancel();
        Assert.False(attempt.TryTakeConnectedSocket(out _));
        Assert.Same(outcome, await attempt.Completion); // completion is immutable, ownership is revoked
        using CancellationTokenSource timeout = new(Limit);
        Assert.Equal(0, await peer.ReceiveAsync(new byte[1], SocketFlags.None, timeout.Token));
    }

    [Fact]
    public async Task RefusedConnection_ReportsFailure_WithoutSocketOwnership()
    {
        using TcpListener reservation = Listen();
        IPEndPoint endpoint = (IPEndPoint)reservation.LocalEndpoint;
        reservation.Stop();
        using IConnectionAttempt attempt = new Connector().BeginConnect(endpoint);
        ConnectionOutcome result = await attempt.Completion.WaitAsync(Limit);
        Assert.Equal(ConnectionStatus.Failed, result.Status);
        Assert.IsType<SocketException>(result.Error);
        Assert.False(attempt.TryTakeConnectedSocket(out _));
    }

    [Fact]
    public async Task ImmediateRepeatedCancel_CompletesOnce_AndNeverTransfersSocket()
    {
        using TcpListener listener = Listen();
        using IConnectionAttempt attempt = new Connector().BeginConnect((IPEndPoint)listener.LocalEndpoint);
        attempt.Cancel();
        attempt.Cancel();
        ConnectionOutcome result = await attempt.Completion.WaitAsync(Limit);
        // Loopback can finish before cancellation. Either completion is legal; neither permits take.
        Assert.Contains(result.Status, new[] { ConnectionStatus.Success, ConnectionStatus.Canceled });
        Assert.Same(result, await attempt.Completion);
        Assert.False(attempt.TryTakeConnectedSocket(out _));
    }

    sealed class ProbeSession : ClientSession
    {
        internal readonly TaskCompletionSource<EndPoint> Connected = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal readonly TaskCompletionSource Closed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal int CloseCount;
        internal bool ThrowReceive, ThrowSend, ThrowClose;
        internal bool WasStartedAtConnected;
        internal int Queued => _sendQueue.Count + _pendingList.Count;
        internal void SeedQueues()
        {
            _sendQueue.Enqueue(new ArraySegment<byte>(new byte[] { 1 }));
            _pendingList.Add(new ArraySegment<byte>(new byte[] { 2 }));
        }
        internal void InjectSocket(Socket socket) => _socket = socket;
        public override void OnConnected(EndPoint endpoint)
        {
            WasStartedAtConnected = _socket?.Connected == true;
            Connected.TrySetResult(endpoint);
        }
        public override void OnDisconnected(EndPoint endpoint)
        {
            Interlocked.Increment(ref CloseCount);
            Closed.TrySetResult();
            if (ThrowClose) throw new InvalidOperationException("injected close observer failure");
        }
        public override int OnRecv(ArraySegment<byte> bytes)
        {
            if (ThrowReceive) throw new InvalidOperationException("injected receive callback failure");
            return bytes.Count;
        }
        public override void OnSend(int count)
        {
            if (ThrowSend) throw new InvalidOperationException("injected send callback failure");
        }
    }

    [Fact]
    public async Task LegacyOverlappingConnects_KeepTheirOwnFactory_AndStartBeforeConnected()
    {
        using TcpListener first = Listen();
        using TcpListener second = Listen();
        ProbeSession a = new(), b = new();
        Connector connector = new();
        int callsA = 0, callsB = 0;
        try
        {
            connector.Connect((IPEndPoint)first.LocalEndpoint, () => { Interlocked.Increment(ref callsA); return a; });
            connector.Connect((IPEndPoint)second.LocalEndpoint, () => { Interlocked.Increment(ref callsB); return b; });
            using Socket peerA = await first.AcceptSocketAsync().WaitAsync(Limit);
            using Socket peerB = await second.AcceptSocketAsync().WaitAsync(Limit);
            Assert.Equal(first.LocalEndpoint, await a.Connected.Task.WaitAsync(Limit));
            Assert.Equal(second.LocalEndpoint, await b.Connected.Task.WaitAsync(Limit));
            Assert.True(a.WasStartedAtConnected && b.WasStartedAtConnected);
            Assert.Equal(1, callsA);
            Assert.Equal(1, callsB);
        }
        finally { a.Disconnect(); b.Disconnect(); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FailedActivationOrSend_ClosesOnce_AndClearsQueues(bool send)
    {
        using Socket disposed = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        disposed.Dispose();
        ProbeSession session = new();
        try
        {
            if (send)
            {
                session.InjectSocket(disposed); // force the synchronous SendAsync failure boundary
                session.Send(new ArraySegment<byte>(new byte[] { 1 }));
            }
            else session.Start(disposed);
            // The terminal path itself must clean up, before test teardown calls Disconnect.
            Assert.True(session.IsDisconnected);
            Assert.Equal(1, session.CloseCount);
            Assert.Equal(0, session.Queued);
            session.Disconnect();
            Assert.Equal(1, session.CloseCount);
        }
        finally { session.Disconnect(); }
    }

    [Fact]
    public void DisconnectBeforeStart_StillCleansQueues_AndRejectsLateSocket()
    {
        ProbeSession session = new() { ThrowClose = true };
        session.SeedQueues();
        session.Disconnect();
        using Socket late = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        session.Start(late);
        Assert.True(late.SafeHandle.IsClosed);
        Assert.Equal(1, session.CloseCount);
        Assert.Equal(0, session.Queued);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TerminalCompletionException_NotifiesOnce_AndClosesTransport(bool send)
    {
        using TcpListener listener = Listen();
        using Socket client = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        await client.ConnectAsync(listener.LocalEndpoint).WaitAsync(Limit);
        using Socket peer = await listener.AcceptSocketAsync().WaitAsync(Limit);
        ProbeSession session = new() { ThrowReceive = !send, ThrowSend = send, ThrowClose = true };
        session.Start(client);
        try
        {
            if (send) session.Send(new ArraySegment<byte>(new byte[] { 1 }));
            else await peer.SendAsync(new byte[] { 1 }, SocketFlags.None);
            await session.Closed.Task.WaitAsync(Limit);
            using CancellationTokenSource timeout = new(Limit);
            byte[] bytes = new byte[16];
            while (await peer.ReceiveAsync(bytes, SocketFlags.None, timeout.Token) > 0) { }
            Assert.True(session.IsDisconnected);
            Assert.Equal(1, session.CloseCount);
            Assert.Equal(0, session.Queued);
        }
        finally { session.Disconnect(); }
    }
}

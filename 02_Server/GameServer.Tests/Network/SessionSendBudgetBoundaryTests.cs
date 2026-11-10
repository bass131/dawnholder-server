using System.Net;
using System.Net.Sockets;
using Dawnholder.Server.Network;
using Xunit.Abstractions;

namespace GameServer.Tests.Network;

[Collection("ConsoleSerial")]
public sealed class SessionSendBudgetBoundaryTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RetainedBytes_ExactLimitStaysOpen_OneMoreByteTerminates(bool batch)
    {
        using SessionSocketPair pair = await SessionSocketPair.Connect();
        using var session = new TransportProbeSession();
        session.Start(pair.Local);
        // A full-size send to an unread peer keeps the entire accepted array in flight.
        Send(session, new ArraySegment<byte>(new byte[33_554_432]), batch);

        Assert.False(session.Disconnected);
        Assert.Empty(session.SendCompletions);
        Assert.Equal((0, 1, 33_554_432L), session.Buffers);
        output.WriteLine($"exactByteLimit: {session.Buffers}");

        Exception? escaped = Record.Exception(() => Send(session, new ArraySegment<byte>(new byte[1]), batch));

        Assert.Null(escaped);
        Assert.True(session.Disconnected);
        Assert.Equal(1, session.Notifications);
        Assert.Equal((0, 0, 0L), session.Buffers);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Segments_InFlightPlusQueuedExactLimitStaysOpen_NextTerminates(bool batch)
    {
        using SessionSocketPair pair = await SessionSocketPair.Connect();
        using var session = new TransportProbeSession();
        session.Start(pair.Local);
        session.Send(new ArraySegment<byte>(new byte[1_048_576]));
        Assert.Empty(session.SendCompletions);
        Assert.Equal(1, session.Buffers.Pending);

        // Queue behind one real blocked send, avoiding platform scatter/gather limits.
        var queued = Enumerable.Range(0, 4095)
            .Select(_ => new ArraySegment<byte>(new byte[1])).ToList();
        if (batch) session.Send(queued);
        else foreach (ArraySegment<byte> segment in queued) session.Send(segment);

        Assert.False(session.Disconnected);
        Assert.Empty(session.SendCompletions);
        Assert.Equal((4095, 1, 1_052_671L), session.Buffers);
        output.WriteLine($"exactSegmentLimit: {session.Buffers}");

        Send(session, new ArraySegment<byte>(new byte[1]), batch);

        Assert.True(session.Disconnected);
        Assert.Equal(1, session.Notifications);
        Assert.Equal((0, 0, 0L), session.Buffers);
    }

    [Fact]
    public async Task CompletedSend_ReleasesRetainedBytes_ForTheNextFullBudget()
    {
        using SessionSocketPair pair = await SessionSocketPair.Connect();
        using var session = new TransportProbeSession();
        session.Start(pair.Local);
        using var timeout = new CancellationTokenSource(SessionSocketPair.Limit);
        for (int index = 0; index < 3; index++)
        {
            byte[] backing = new byte[33_554_432];
            backing[0] = (byte)(index + 1);
            session.Send(new ArraySegment<byte>(backing, 0, 1));
            byte[] received = new byte[1];
            int count = await pair.Peer.ReceiveAsync(received, SocketFlags.None, timeout.Token);
            Assert.Equal(1, count);
            Assert.Equal(index + 1, received[0]);
            Assert.True(await SessionSocketPair.WaitUntil(() => session.SendCompletions.Count == index + 1));
            Assert.Equal((0, 0, 0L), session.Buffers);
            Assert.False(session.Disconnected);
        }
        Assert.Equal(0, session.Notifications);
    }

    [Fact]
    public async Task CompletedBatches_ReleaseSegmentBudget_AcrossMoreThan4096LifetimeSegments()
    {
        using SessionSocketPair pair = await SessionSocketPair.Connect();
        using var session = new TransportProbeSession();
        session.Start(pair.Local);
        using var timeout = new CancellationTokenSource(SessionSocketPair.Limit);
        for (int batch = 0; batch < 9; batch++)
        {
            var segments = Enumerable.Range(0, 512)
                .Select(index => new ArraySegment<byte>(new[] { (byte)index })).ToList();
            session.Send(segments);
            byte[] received = new byte[512];
            await ReadAll(pair.Peer, received, timeout.Token);
            Assert.Equal(Enumerable.Range(0, 512).Select(index => (byte)index), received);
            Assert.True(await SessionSocketPair.WaitUntil(() => session.SendCompletions.Count == batch + 1));
            Assert.Equal((0, 0, 0L), session.Buffers);
            Assert.False(session.Disconnected);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SendRacingDisconnect_WithPendingIo_ClosesOnceAndReleasesBuffers(bool batch)
    {
        using SessionSocketPair pair = await SessionSocketPair.Connect();
        using var session = new TransportProbeSession();
        session.Start(pair.Local);
        session.Send(new ArraySegment<byte>(new byte[1_048_576]));
        Assert.Equal(1, session.Buffers.Pending);
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task sender = Task.Run(async () =>
        {
            await start.Task;
            for (int index = 0; index < 512; index++)
                Send(session, new ArraySegment<byte>(new byte[16]), batch);
        });
        Task closer = Task.Run(async () =>
        {
            await start.Task;
            session.Disconnect();
        });
        start.SetResult();
        await Task.WhenAll(sender, closer).WaitAsync(SessionSocketPair.Limit);
        Send(session, new ArraySegment<byte>(new byte[16]), batch);

        Assert.Equal(1, session.Notifications);
        Assert.Equal((0, 0, 0L), session.Buffers);
        Assert.True(pair.Local.SafeHandle.IsClosed);
    }

    [Fact]
    public async Task ReentrantOnSend_KeepsQueuedMessagesInOrder()
    {
        using SessionSocketPair pair = await SessionSocketPair.Connect();
        using var session = new TransportProbeSession();
        session.Start(pair.Local);
        int callback = 0;
        session.Sent = _ =>
        {
            if (Interlocked.Increment(ref callback) != 1) return;
            session.Send(new ArraySegment<byte>(new byte[] { 2 }));
            session.Send(new List<ArraySegment<byte>> { new(new byte[] { 3 }) });
        };
        session.Send(new ArraySegment<byte>(new byte[] { 1 }));
        byte[] received = new byte[3];
        using var timeout = new CancellationTokenSource(SessionSocketPair.Limit);
        await ReadAll(pair.Peer, received, timeout.Token);

        Assert.Equal(new byte[] { 1, 2, 3 }, received);
        Assert.True(await SessionSocketPair.WaitUntil(() => session.Buffers == (0, 0, 0L)));
        Assert.False(session.Disconnected);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SendDuringDisconnectNotification_DoesNotQueueBeforeCleanupFinishes(bool batch)
    {
        using SessionSocketPair pair = await SessionSocketPair.Connect();
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var session = new DisconnectGateSession(entered, release);
        session.Start(pair.Local);
        session.Send(new ArraySegment<byte>(new byte[1_048_576]));
        Task closing = Task.Run(session.Disconnect);
        try
        {
            Assert.True(entered.Wait(SessionSocketPair.Limit), "Disconnect must reach its notification gate");
            Assert.Equal(1, session.RetainedSegments);
            var segment = new ArraySegment<byte>(new byte[32]);
            if (batch) session.Send(new List<ArraySegment<byte>> { segment });
            else session.Send(segment);
            Assert.Equal(1, session.RetainedSegments);
        }
        finally
        {
            release.Set();
            await closing.WaitAsync(SessionSocketPair.Limit);
        }
        Assert.Equal(0, session.RetainedSegments);
        Assert.Equal(1, session.Notifications);
        Assert.True(pair.Local.SafeHandle.IsClosed);
    }

    [Fact]
    public async Task AsyncSendCallbackFailure_AfterRealPendingSend_ClosesAndClears()
    {
        using SessionSocketPair pair = await SessionSocketPair.Connect();
        using var session = new TransportProbeSession();
        session.Sent = _ => throw new InvalidOperationException("async completion observer failed");
        session.Start(pair.Local);
        Exception? escaped = Record.Exception(() => session.Send(new ArraySegment<byte>(new byte[65_536])));
        Assert.Null(escaped);
        Assert.Equal(1, session.Buffers.Pending);
        Assert.Empty(session.SendCompletions);
        byte[] received = new byte[65_536];
        using var timeout = new CancellationTokenSource(SessionSocketPair.Limit);
        await ReadAll(pair.Peer, received, timeout.Token);

        Assert.True(await SessionSocketPair.WaitUntil(() => session.Notifications == 1 && pair.Local.SafeHandle.IsClosed));
        Assert.Single(session.SendCompletions);
        Assert.Equal((0, 0, 0L), session.Buffers);
        session.Disconnect();
        Assert.Equal(1, session.Notifications);
    }

    private static void Send(TransportProbeSession session, ArraySegment<byte> segment, bool batch)
    {
        if (batch) session.Send(new List<ArraySegment<byte>> { segment });
        else session.Send(segment);
    }

    private static async Task ReadAll(Socket peer, byte[] destination, CancellationToken cancellationToken)
    {
        int offset = 0;
        while (offset < destination.Length)
        {
            int count = await peer.ReceiveAsync(destination.AsMemory(offset), SocketFlags.None, cancellationToken);
            Assert.True(count > 0, "Peer closed before all ordered bytes arrived");
            offset += count;
        }
    }

    private sealed class DisconnectGateSession(ManualResetEventSlim entered, ManualResetEventSlim release) : Session
    {
        public int Notifications { get; private set; }
        public int RetainedSegments
        {
            get
            {
                lock (_lock) return _sendQueue.Count + _pendingList.Count;
            }
        }

        public override void OnConnected(EndPoint endPoint) { }
        public override void OnDisconnected(EndPoint endPoint)
        {
            Notifications++;
            entered.Set();
            if (!release.Wait(SessionSocketPair.Limit)) throw new TimeoutException("Notification gate was not released");
        }
        public override int OnRecv(ArraySegment<byte> buffer) => buffer.Count;
        public override void OnSend(int numOfBytes) { }
    }
}

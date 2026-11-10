using System.Net.Sockets;

namespace GameServer.Tests.Network;

[Collection("ConsoleSerial")]
public sealed class SessionReceiveCompletionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PartialInput_SurvivesImmediateOrAsyncCompletion_AndContinuesReceiving(bool preload)
    {
        using SessionSocketPair pair = await SessionSocketPair.Connect();
        using var session = new TransportProbeSession();
        var first = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        var complete = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        var next = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        int callbacks = 0;
        session.Received = buffer =>
        {
            int call = Interlocked.Increment(ref callbacks);
            if (call == 1)
            {
                first.SetResult(buffer.ToArray());
                return 0;
            }
            if (call == 2) complete.SetResult(buffer.ToArray());
            else next.SetResult(buffer.ToArray());
            return buffer.Count;
        };
        using var timeout = new CancellationTokenSource(SessionSocketPair.Limit);
        if (preload)
        {
            await pair.Peer.SendAsync(new byte[] { 11, 22 }, SocketFlags.None, timeout.Token);
            Assert.True(await SessionSocketPair.WaitUntil(() => pair.Local.Available == 2));
        }
        session.Start(pair.Local);
        Assert.Equal(preload, first.Task.IsCompleted);
        if (!preload) await pair.Peer.SendAsync(new byte[] { 11, 22 }, SocketFlags.None, timeout.Token);
        Assert.Equal(new byte[] { 11, 22 }, await first.Task.WaitAsync(timeout.Token));

        await pair.Peer.SendAsync(new byte[] { 33 }, SocketFlags.None, timeout.Token);
        Assert.Equal(new byte[] { 11, 22, 33 }, await complete.Task.WaitAsync(timeout.Token));
        await pair.Peer.SendAsync(new byte[] { 44 }, SocketFlags.None, timeout.Token);
        Assert.Equal(new byte[] { 44 }, await next.Task.WaitAsync(timeout.Token));
        Assert.Equal(0, session.Notifications);
    }

    [Theory]
    [InlineData(false, "negative")]
    [InlineData(true, "negative")]
    [InlineData(false, "oversized")]
    [InlineData(true, "oversized")]
    [InlineData(false, "throws")]
    [InlineData(true, "throws")]
    public async Task InvalidConsumerResult_ClosesOnce_InImmediateAndAsyncPaths(bool preload, string failure)
    {
        using SessionSocketPair pair = await SessionSocketPair.Connect();
        using var session = new TransportProbeSession();
        int callbacks = 0;
        session.Received = _ =>
        {
            Interlocked.Increment(ref callbacks);
            return failure switch
            {
                "negative" => -1,
                "oversized" => 2,
                _ => throw new InvalidOperationException("receive consumer failed"),
            };
        };
        using var timeout = new CancellationTokenSource(SessionSocketPair.Limit);
        if (preload)
        {
            await pair.Peer.SendAsync(new byte[] { 42 }, SocketFlags.None, timeout.Token);
            Assert.True(await SessionSocketPair.WaitUntil(() => pair.Local.Available == 1));
        }
        Exception? escaped = Record.Exception(() => session.Start(pair.Local));
        Assert.Null(escaped);
        Assert.Equal(preload ? 1 : 0, Volatile.Read(ref callbacks));
        if (!preload) await pair.Peer.SendAsync(new byte[] { 42 }, SocketFlags.None, timeout.Token);
        Assert.True(await SessionSocketPair.WaitUntil(() => session.Notifications == 1 && pair.Local.SafeHandle.IsClosed));

        Assert.Equal(1, Volatile.Read(ref callbacks));
        Assert.Equal((0, 0, 0L), session.Buffers);
        session.Disconnect();
        Assert.Equal(1, session.Notifications);
    }

    [Fact]
    public async Task PeerEof_WithPendingReceive_NotifiesOnceWithoutCallingConsumer()
    {
        using SessionSocketPair pair = await SessionSocketPair.Connect();
        using var session = new TransportProbeSession();
        int callbacks = 0;
        session.Received = buffer =>
        {
            Interlocked.Increment(ref callbacks);
            return buffer.Count;
        };
        session.Start(pair.Local);
        pair.Peer.Shutdown(SocketShutdown.Send);

        Assert.True(await SessionSocketPair.WaitUntil(() => session.Notifications == 1 && pair.Local.SafeHandle.IsClosed));
        Assert.Equal(0, Volatile.Read(ref callbacks));
        session.Disconnect();
        Assert.Equal(1, session.Notifications);
    }
}

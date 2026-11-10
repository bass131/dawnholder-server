using System.Buffers.Binary;
using System.Net.Sockets;
using Xunit.Abstractions;

namespace GameServer.Tests.Network;

// A connection must terminate above either retained-array or segment budget;
// normal ordered frames and sends after termination have separate assertions.
[Collection("ConsoleSerial")]
public sealed class SessionSendBackpressureTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnreadPeer_RetainedArraysAbove32MiB_Disconnects(bool batch)
    {
        const long retainedArrayBudget = 33_554_432;
        using SessionSocketPair pair = await SessionSocketPair.Connect();
        using var session = new TransportProbeSession();
        session.Start(pair.Local);
        // Like generated writers: only a prefix is sent, while the full array stays live.
        SendBurst(session, count: 1024, arrayLength: 65_535, prefixLength: 1024, batch: batch);
        bool notified = await SessionSocketPair.WaitUntil(() => session.Notifications > 0);
        var buffers = session.Buffers;
        output.WriteLine($"notified={notified}, buffers={buffers}, budget={retainedArrayBudget}");

        Assert.True(notified, $"Unread peer retained {buffers.RetainedBytes} bytes without termination");
        Assert.Equal(1, session.Notifications);
        Assert.InRange(buffers.RetainedBytes, 0, retainedArrayBudget);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnreadPeer_MoreThan4096SmallSegments_Disconnects(bool batch)
    {
        const int segmentBudget = 4096;
        using SessionSocketPair pair = await SessionSocketPair.Connect();
        using var session = new TransportProbeSession();
        session.Start(pair.Local);
        SendBurst(session, count: 8192, arrayLength: 256, prefixLength: 256, batch: batch);
        bool notified = await SessionSocketPair.WaitUntil(() => session.Notifications > 0);
        var buffers = session.Buffers;
        output.WriteLine($"notified={notified}, buffers={buffers}, budget={segmentBudget}");

        Assert.True(notified, $"Unread peer retained {buffers.Queued + buffers.Pending} segments without termination");
        Assert.Equal(1, session.Notifications);
        Assert.InRange(buffers.Queued + buffers.Pending, 0, segmentBudget);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SendAfterDisconnect_DoesNotRetainNewSegments(bool batch)
    {
        using SessionSocketPair pair = await SessionSocketPair.Connect();
        using var session = new TransportProbeSession();
        session.Start(pair.Local);
        session.Disconnect();
        Assert.Equal(1, session.Notifications);
        var before = session.Buffers;

        SendBurst(session, count: 3, arrayLength: 32, prefixLength: 8, batch: batch);
        var after = session.Buffers;
        output.WriteLine($"before={before}, after={after}");

        Assert.Equal(before.Queued, after.Queued);
        Assert.Equal(before.Pending, after.Pending);
        Assert.Equal(before.RetainedBytes, after.RetainedBytes);
        Assert.Equal(1, session.Notifications);
    }

    [Fact]
    public async Task ReadingPeer_BelowBudgets_ReceivesEveryFrameInOrder()
    {
        const int frameCount = 100;
        const int frameSize = 8;
        using SessionSocketPair pair = await SessionSocketPair.Connect();
        using var session = new TransportProbeSession();
        session.Start(pair.Local);
        byte[] received = new byte[frameCount * frameSize];
        using var timeout = new CancellationTokenSource(SessionSocketPair.Limit);
        Task read = ReadAll(pair.Peer, received, timeout.Token);
        for (int sequence = 0; sequence < frameCount; sequence++)
        {
            byte[] frame = new byte[frameSize];
            BinaryPrimitives.WriteInt32LittleEndian(frame, frameSize);
            BinaryPrimitives.WriteInt32LittleEndian(frame.AsSpan(4), sequence);
            session.Send(new ArraySegment<byte>(frame));
        }
        await read;

        for (int sequence = 0; sequence < frameCount; sequence++)
        {
            Assert.Equal(frameSize, BinaryPrimitives.ReadInt32LittleEndian(received.AsSpan(sequence * frameSize)));
            Assert.Equal(sequence, BinaryPrimitives.ReadInt32LittleEndian(received.AsSpan(sequence * frameSize + 4)));
        }
        Assert.Equal(0, session.Notifications);
    }

    private static void SendBurst(TransportProbeSession session, int count, int arrayLength, int prefixLength, bool batch)
    {
        var segments = new List<ArraySegment<byte>>();
        for (int index = 0; index < count; index++)
        {
            var segment = new ArraySegment<byte>(new byte[arrayLength], 0, prefixLength);
            if (batch) segments.Add(segment);
            else session.Send(segment);
        }
        if (batch) session.Send(segments);
    }

    private static async Task ReadAll(Socket socket, byte[] destination, CancellationToken cancellationToken)
    {
        int received = 0;
        while (received < destination.Length)
        {
            int count = await socket.ReceiveAsync(destination.AsMemory(received), SocketFlags.None, cancellationToken);
            Assert.True(count > 0, "Premature EOF lost ordered frames");
            received += count;
        }
    }
}

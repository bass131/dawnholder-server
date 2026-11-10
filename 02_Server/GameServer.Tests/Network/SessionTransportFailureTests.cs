using System.Net.Sockets;
using Xunit.Abstractions;

namespace GameServer.Tests.Network;

// Send/receive registration and completion callback failures must close once
// and release queued/pending segments. All failures use the real Session entry.
[Collection("ConsoleSerial")]
public sealed class SessionTransportFailureTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("send")]
    [InlineData("receive")]
    [InlineData("callback")]
    public async Task TransportException_TerminatesAndNotifiesExactlyOnce(string operation)
    {
        using SessionSocketPair pair = await SessionSocketPair.Connect();
        using var session = new TransportProbeSession();
        Exception? escaped = TriggerFailure(session, pair.Local, operation);
        bool terminated = await SessionSocketPair.WaitUntil(() => session.Disconnected);
        var buffers = session.Buffers;
        output.WriteLine($"operation={operation}, terminated={terminated}, notifications={session.Notifications}, "
            + $"buffers={buffers}, callbacks={session.SendCompletions.Count}, escaped={escaped?.GetType().Name ?? "none"}");

        Assert.Null(escaped);
        Assert.True(terminated, $"{operation} exception left the connection active");
        Assert.Equal(1, session.Notifications);
        session.Disconnect();
        session.Disconnect();
        Assert.Equal(1, session.Notifications);
    }

    [Theory]
    [InlineData("send")]
    [InlineData("receive")]
    [InlineData("callback")]
    public async Task TransportException_ClearsQueuedAndPendingSegments(string operation)
    {
        using SessionSocketPair pair = await SessionSocketPair.Connect();
        using var session = new TransportProbeSession();
        TriggerFailure(session, pair.Local, operation);
        await SessionSocketPair.WaitUntil(() => session.Disconnected);
        var buffers = session.Buffers;
        output.WriteLine($"operation={operation}, disconnected={session.Disconnected}, buffers={buffers}");

        Assert.Equal(0, buffers.Queued);
        Assert.Equal(0, buffers.Pending);
        Assert.Equal(0, buffers.RetainedBytes);
    }

    private static Exception? TriggerFailure(TransportProbeSession session, Socket local, string operation)
    {
        if (operation == "send")
        {
            // Do not start a concurrent receive: its close callback would mask registration failure.
            session.AttachWithoutReceive(local);
            local.Dispose();
            return Record.Exception(() => session.Send(new ArraySegment<byte>(new byte[32])));
        }
        if (operation == "receive")
        {
            local.Dispose();
            return Record.Exception(() => session.Start(local));
        }
        session.Sent = _ => throw new InvalidOperationException("scripted OnSend failure");
        session.Start(local);
        return Record.Exception(() => session.Send(new ArraySegment<byte>(new byte[32])));
    }
}

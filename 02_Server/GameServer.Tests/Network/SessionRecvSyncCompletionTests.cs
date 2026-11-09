using System.Diagnostics;
using System.Net.Sockets;
using Xunit.Abstractions;

namespace GameServer.Tests.Network;

// Buffered socket receives must make progress without growing the call stack.
// Consume one byte so the real RecvBuffer exposes a one-byte free tail repeatedly.
[Collection("ConsoleSerial")]
public sealed class SessionRecvSyncCompletionTests(ITestOutputHelper output)
{
    [Fact]
    public async Task PreloadedPeer_ConsecutiveReceives_DoNotGrowCallStack()
    {
        const int sampleCount = 32;
        using SessionSocketPair pair = await SessionSocketPair.Connect(receiveBuffer: 1024 * 1024);
        using var session = new TransportProbeSession();
        byte[] preload = new byte[96 * 1024];
        using var timeout = new CancellationTokenSource(SessionSocketPair.Limit);
        int sent = 0;
        while (sent < preload.Length)
        {
            sent += await pair.Peer.SendAsync(preload.AsMemory(sent), SocketFlags.None, timeout.Token);
        }
        bool buffered = await SessionSocketPair.WaitUntil(() => pair.Local.Available >= 65_535 + sampleCount);
        Assert.True(buffered, "Fixture must preload more than one complete receive buffer");

        var depths = new List<int>();
        session.Received = _ =>
        {
            lock (depths)
            {
                depths.Add(new StackTrace().FrameCount);
                // Bounded observation: never try to crash the host with stack exhaustion.
                if (depths.Count == sampleCount) session.Disconnect();
            }
            return 1;
        };
        session.Start(pair.Local);
        bool completed = await SessionSocketPair.WaitUntil(() => session.Disconnected);
        int[] observed;
        lock (depths) observed = depths.ToArray();
        output.WriteLine($"preloaded={sent}, completed={completed}, depths=[{string.Join(",", observed)}]");

        Assert.True(completed, "Receive probe did not reach its bounded sample count");
        Assert.Equal(sampleCount, observed.Length);
        int growth = observed.Max() - observed.Min();
        Assert.InRange(growth, 0, 8);
    }
}

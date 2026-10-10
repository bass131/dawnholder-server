using System.Net;
using System.Net.Sockets;
using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Maps;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Tests.Integration;

// Own a fixture per test; never overlap the shared IntegrationTests world or another singleton owner.
[CollectionDefinition("ServerFixtureLifecycle", DisableParallelization = true)]
public sealed class ServerFixtureLifecycleCollection { }

[Collection("ServerFixtureLifecycle")]
public sealed class ServerFixtureLifecycleTests
{
    static readonly TimeSpan Limit = TimeSpan.FromSeconds(5);

    static async Task<byte[]> ReadFrame(NetworkStream stream)
    {
        using CancellationTokenSource timeout = new(Limit);
        byte[] header = new byte[4];
        await stream.ReadExactlyAsync(header, timeout.Token);
        int size = BitConverter.ToUInt16(header, 0);
        Assert.InRange(size, 4, ushort.MaxValue);
        byte[] frame = new byte[size];
        header.CopyTo(frame, 0);
        await stream.ReadExactlyAsync(frame.AsMemory(4), timeout.Token);
        return frame;
    }

    static async Task EnterWorld(TcpClient client, int port)
    {
        await client.ConnectAsync(IPAddress.Loopback, port).WaitAsync(Limit);
        NetworkStream stream = client.GetStream();
        await stream.WriteAsync(new C_Handshake { clientVersion = ProtocolVersion.Current }.Write()).AsTask().WaitAsync(Limit);
        byte[] handshakeBytes = await ReadFrame(stream);
        Assert.Equal(PacketID.S_HandshakeResult, (PacketID)BitConverter.ToUInt16(handshakeBytes, 2));
        S_HandshakeResult handshake = new();
        handshake.Read(new(handshakeBytes));
        Assert.True(handshake.ok, handshake.reason);
        Assert.Equal(ProtocolVersion.Current, handshake.serverVersion);
        await stream.WriteAsync(new C_CharacterSelect { characterClass = 0 }.Write()).AsTask().WaitAsync(Limit);
        byte[] enterBytes = await ReadFrame(stream);
        Assert.Equal(PacketID.S_EnterMap, (PacketID)BitConverter.ToUInt16(enterBytes, 2));
        S_EnterMap enter = new();
        enter.Read(new(enterBytes));
        Assert.True(enter.entityId > 0);
        byte[] hpBytes = await ReadFrame(stream);
        Assert.Equal(PacketID.S_PlayerHp, (PacketID)BitConverter.ToUInt16(hpBytes, 2));
        S_PlayerHp hp = new();
        hp.Read(new(hpBytes));
        Assert.Equal(enter.entityId, hp.entityId);
        Assert.Equal(150, hp.currentHp);
        Assert.Equal(150, hp.maxHp);
    }

    static async Task AssertClosed(NetworkStream stream)
    {
        using CancellationTokenSource timeout = new(Limit);
        byte[] buffer = new byte[4096];
        try
        {
            // Buffered snapshots may precede EOF; consuming only one read would hide a leaked socket.
            while (await stream.ReadAsync(buffer, timeout.Token) != 0) { }
        }
        catch (IOException ex) when (ex.InnerException is SocketException socket &&
            socket.SocketErrorCode is SocketError.ConnectionReset or SocketError.ConnectionAborted)
        {
            // Both graceful EOF and an explicit peer reset demonstrate transport closure.
        }
    }

    [Fact]
    public async Task Dispose_ClosesEnteredClientAndListener_ThenNextFixtureCanEnter()
    {
        using (ServerFixture fixture = new())
        using (TcpClient client = new())
        {
            Assert.InRange(fixture.Port, 1, ushort.MaxValue);
            await EnterWorld(client, fixture.Port);
            await Task.Run(fixture.Dispose).WaitAsync(TimeSpan.FromSeconds(15));
            await AssertClosed(client.GetStream());
            Assert.Null(GameWorld.Instance);
            Assert.All(fixture.World.AllLiveMaps, map => Assert.Empty(map.Players));
            long stoppedTick = fixture.World.CurrentTick;
            await Task.Delay(100);
            Assert.Equal(stoppedTick, fixture.World.CurrentTick);
            using TcpClient rejected = new();
            SocketException error = await Assert.ThrowsAsync<SocketException>(
                () => rejected.ConnectAsync(IPAddress.Loopback, fixture.Port).WaitAsync(Limit));
            Assert.Equal(SocketError.ConnectionRefused, error.SocketErrorCode);
            await Task.Run(fixture.Dispose).WaitAsync(TimeSpan.FromSeconds(15));
        }
        using (ServerFixture replacement = new())
        using (TcpClient client = new())
        {
            await EnterWorld(client, replacement.Port);
            await Task.Run(replacement.Dispose).WaitAsync(TimeSpan.FromSeconds(15));
            await AssertClosed(client.GetStream());
            Assert.Null(GameWorld.Instance);
        }
    }

    [Fact]
    public async Task Dispose_BeforeAnyClient_IsRepeatable_AndReleasesWorld()
    {
        using ServerFixture fixture = new();
        await Task.Run(fixture.Dispose).WaitAsync(TimeSpan.FromSeconds(15));
        await Task.Run(fixture.Dispose).WaitAsync(TimeSpan.FromSeconds(15));
        Assert.Null(GameWorld.Instance);
        using ServerFixture replacement = new();
        using TcpClient client = new();
        await EnterWorld(client, replacement.Port);
    }
}

using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Collections.Concurrent;
using Dawnholder.Server.GameServer.Hosting;
using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Persistence;
using Dawnholder.Server.GameServer.Persistence.Admission;
using Dawnholder.Server.GameServer.Persistence.Catalog;
using Dawnholder.Server.GameServer.Persistence.Sql;
using Dawnholder.Server.GameServer.Sessions;
using Dawnholder.Server.Network;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Tests.Network;

[Collection("GameWorldRegistryTests")]
public sealed class ServerHostLifecycleTests
{
    static GameWorld NewWorld() => new(new Dictionary<MapId, (MapTerrain?, MapContent?)>());
    static readonly TimeSpan Limit = TimeSpan.FromSeconds(5);

    // Observe connection ownership without introducing a production API only for tests.
    static int OwnedConnections(ServerHost host)
        => ((ConcurrentDictionary<GameSession, byte>)typeof(ServerHost)
            .GetField("_sessions", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(host)!).Count;

    sealed class ThrowingProvider : Dictionary<MapId, (MapTerrain?, MapContent?)>,
        IReadOnlyDictionary<MapId, (MapTerrain?, MapContent?)>
    {
        bool IReadOnlyDictionary<MapId, (MapTerrain?, MapContent?)>.TryGetValue(
            MapId key, out (MapTerrain?, MapContent?) value)
            => throw new InvalidOperationException("injected map construction failure");
    }

    [Fact]
    public void FailedWorldConstruction_DoesNotPublishPartialWorld()
    {
        Assert.Throws<InvalidOperationException>(() => new GameWorld(new ThrowingProvider()));
        Assert.Null(GameWorld.Instance);
        GameWorld replacement = NewWorld();
        replacement.Stop();
    }

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

    [CharacterMenuDatabaseFact]
    public async Task StoredMenuSqlEndToEnd()
    {
        using CharacterMenuDatabaseInput input = CharacterMenuDatabaseInput.Open();
        string run = Guid.NewGuid().ToString("N")[..8];
        string account = "w_" + run;
        string name = "M" + run;
        Guid characterId = Guid.Empty;

        // Each phase creates a fresh host/catalog over real SQL connections. The second
        // host must read the first host's durable identity and class after normal close.
        for (int phase = 0; phase < 2; phase++)
        {
            GameWorld world = new(MapDataLoader.LoadAll());
            var spawn = world.Map.PlayerSpawnPosition;
            SafeDefaults defaults = new(spawn.X, spawn.Y, PlayerStats.Knight().MaxHp, PlayerStats.Mage().MaxHp);
            SqlCharacterCatalogRepository catalogRepository = new(input.Options, input.CreateConnection);
            CharacterCatalogService catalog = new(catalogRepository,
                new SqlCharacterRepository(input.Options, input.CreateConnection), defaults, TimeProvider.System);
            world.AttachPersistence(new CharacterAdmissionServices(catalog, TimeProvider.System));
            using ServerHost host = new(world, new IPEndPoint(IPAddress.Loopback, 0));
            try
            {
                Assert.True(await catalogRepository.PreflightAsync(
                    PersistenceDeadline.FromBudget(TimeProvider.System, Limit), CancellationToken.None));
                host.Start();
                IPEndPoint endpoint = Assert.IsType<IPEndPoint>(host.LocalEndPoint);
                using TcpClient client = new();
                await client.ConnectAsync(endpoint.Address, endpoint.Port).WaitAsync(Limit);
                NetworkStream stream = client.GetStream();
                await stream.WriteAsync(new C_Handshake { clientVersion = ProtocolVersion.Current }.Write());
                Assert.Equal((ushort)8, BitConverter.ToUInt16(await ReadFrame(stream), 2));
                await stream.WriteAsync(new C_Login { accountName = account }.Write());
                Assert.Equal(new byte[] { 5, 0, 42, 0, 0 }, await ReadFrame(stream));

                await stream.WriteAsync(new C_CharacterListRequest { requestId = 1 }.Write());
                S_CharacterListResult listed = new();
                byte[] listFrame = await ReadFrame(stream);
                Assert.Equal((ushort)50, BitConverter.ToUInt16(listFrame, 2));
                listed.Read(new(listFrame));
                Assert.Equal((byte)0, listed.result);
                Assert.Equal(phase == 0 ? (byte)0 : (byte)1, listed.occupiedMask);
                if (phase == 0)
                {
                    await stream.WriteAsync(new C_CharacterCreateRequest
                    {
                        requestId = 2, slot = 1, classId = 1, rawName = name,
                    }.Write());
                    byte[] createFrame = await ReadFrame(stream);
                    Assert.Equal((ushort)52, BitConverter.ToUInt16(createFrame, 2));
                    S_CharacterCreateResult created = new();
                    created.Read(new(createFrame));
                    Assert.Equal((byte)0, created.result);
                    Assert.Equal((byte)1, created.slot);
                    Assert.Equal(name, created.name);
                    Assert.Equal((byte)1, created.classId);
                    Assert.True(Guid.TryParseExact(created.characterIdD, "D", out characterId));
                    Assert.NotEqual(Guid.Empty, characterId);
                }
                else
                {
                    Assert.Equal(characterId.ToString("D"), listed.slot1CharacterIdD);
                    Assert.Equal(name, listed.slot1Name);
                    Assert.Equal((byte)1, listed.slot1ClassId);
                    await stream.WriteAsync(new C_CharacterSelect { characterClass = 0 }.Write());
                    Assert.Equal(new byte[] { 6, 0, 48, 0, 3, 255 }, await ReadFrame(stream));
                }

                await stream.WriteAsync(new C_CharacterSelectRequest
                {
                    requestId = 4, characterIdD = characterId.ToString("D"),
                }.Write());
                byte[] selectFrame = await ReadFrame(stream);
                Assert.Equal((ushort)54, BitConverter.ToUInt16(selectFrame, 2));
                S_CharacterSelectResult selected = new();
                selected.Read(new(selectFrame));
                Assert.Equal((byte)0, selected.result);
                Assert.Equal(characterId.ToString("D"), selected.selectedCharacterIdD);
                Assert.Equal(new byte[] { 6, 0, 48, 0, 0, 1 }, await ReadFrame(stream));
                Assert.Equal((ushort)3, BitConverter.ToUInt16(await ReadFrame(stream), 2));
                byte[] guildFrame;
                int frames = 0;
                do
                {
                    guildFrame = await ReadFrame(stream);
                    Assert.True(++frames <= 128, "Guild admission did not follow the real map entry.");
                }
                while (BitConverter.ToUInt16(guildFrame, 2) != 44);
                S_GuildSnapshot guild = new();
                guild.Read(new(guildFrame));
                Assert.Equal(5, guild.treasury);
                Assert.Equal(1, guild.memberCount);
                string operations = phase == 0 ? "list/create/select" : "list/select";
                Console.WriteLine($"[CharacterMenu SQL] phase={phase} account={account} character={characterId:D} " +
                    $"{operations}=Success; wire54,48(Mage),3,44 observed; normal host close follows.");
            }
            finally
            {
                host.Stop(TimeSpan.FromSeconds(10));
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Stop_ClosesAcceptedSocket_AndRemovesWorldOwnership(bool enterWorld)
    {
        GameWorld world = NewWorld();
        using ServerHost host = new(world, new IPEndPoint(IPAddress.Loopback, 0));
        using TcpClient client = new();
        host.Start();
        IPEndPoint endpoint = Assert.IsType<IPEndPoint>(host.LocalEndPoint);
        await client.ConnectAsync(endpoint.Address, endpoint.Port).WaitAsync(Limit);
        NetworkStream stream = client.GetStream();
        if (enterWorld)
        {
            await stream.WriteAsync(new C_Handshake { clientVersion = ProtocolVersion.Current }.Write());
            Assert.Equal(PacketID.S_HandshakeResult, (PacketID)BitConverter.ToUInt16(await ReadFrame(stream), 2));
            await stream.WriteAsync(new C_Login { accountName = "host_account" }.Write());
            byte[] loginBytes = await ReadFrame(stream);
            Assert.Equal(PacketID.S_LoginResult, (PacketID)BitConverter.ToUInt16(loginBytes, 2));
            S_LoginResult login = new();
            login.Read(new(loginBytes));
            Assert.Equal((byte)LoginResult.Accepted, login.result);
            await stream.WriteAsync(new C_CharacterSelect { characterClass = 0 }.Write());
            byte[] loadBytes = await ReadFrame(stream);
            Assert.Equal(new byte[] { 6, 0, 48, 0, 0, 0 }, loadBytes);
            PacketID[] expected = { PacketID.S_EnterMap, PacketID.S_PlayerHp };
            foreach (PacketID id in expected)
                Assert.Equal(id, (PacketID)BitConverter.ToUInt16(await ReadFrame(stream), 2));
        }
        else
        {
            // A partial protocol header establishes that the socket is open without completing
            // a handshake or creating a PlayerEntity. Host still owns this accepted connection.
            await stream.WriteAsync(new byte[] { 4 });
        }
        Assert.True(SpinWait.SpinUntil(() => OwnedConnections(host) == 1, Limit));
        host.Stop(Limit);
        Assert.Equal(0, OwnedConnections(host));
        Assert.Null(host.LocalEndPoint);
        Assert.Null(GameWorld.Instance);
        Assert.All(world.AllLiveMaps, map => Assert.Empty(map.Players));
        using CancellationTokenSource timeout = new(Limit);
        byte[] remaining = new byte[4096];
        while (await stream.ReadAsync(remaining, timeout.Token) != 0) { }
        long stoppedTick = world.CurrentTick;
        await Task.Delay(100);
        Assert.Equal(stoppedTick, world.CurrentTick);
        host.Stop();
        Assert.Throws<InvalidOperationException>(() => host.Start());
    }

    [Fact]
    public void StartFailure_RollsBackWorld_AndAllowsNextWorld()
    {
        using TcpListener occupied = new(IPAddress.Loopback, 0);
        occupied.Start();
        GameWorld world = NewWorld();
        using ServerHost host = new(world, (IPEndPoint)occupied.LocalEndpoint);
        Assert.Throws<SocketException>(() => host.Start());
        Assert.Null(GameWorld.Instance);
        Assert.Null(host.LocalEndPoint);
        host.Stop();
        GameWorld replacement = NewWorld();
        replacement.Stop();
    }

    [Fact]
    public void StopBeforeStart_IsIdempotent_AndDoesNotLeavePublishedWorld()
    {
        GameWorld world = NewWorld();
        using ServerHost host = new(world, new IPEndPoint(IPAddress.Loopback, 0));
        host.Stop();
        host.Stop();
        Assert.Null(GameWorld.Instance);
        Assert.Throws<InvalidOperationException>(() => host.Start());
    }

    [Fact]
    public void StopTimeout_KeepsWorldOwnedUntilCleanupCanFinish()
    {
        GameWorld world = NewWorld();
        using ServerHost host = new(world, new IPEndPoint(IPAddress.Loopback, 0));
        using ManualResetEventSlim entered = new();
        using ManualResetEventSlim release = new();
        world.Map.EnqueueJob(() => { entered.Set(); release.Wait(TimeSpan.FromSeconds(10)); });
        host.Start();
        try
        {
            Assert.True(entered.Wait(Limit));
            Assert.Throws<TimeoutException>(() => host.Stop(TimeSpan.FromMilliseconds(30)));
            Assert.Same(world, GameWorld.Instance);
            Assert.Throws<InvalidOperationException>(() => NewWorld());
        }
        finally { release.Set(); host.Stop(Limit); }
        Assert.Null(GameWorld.Instance);
    }

    [Fact]
    public async Task StopFromWorldTick_IsRejected_ThenExternalStopCompletes()
    {
        GameWorld world = NewWorld();
        using ServerHost host = new(world, new IPEndPoint(IPAddress.Loopback, 0));
        TaskCompletionSource<Exception?> observed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        world.Map.EnqueueJob(() => observed.SetResult(Record.Exception(() => host.Stop())));
        host.Start();
        Assert.IsType<InvalidOperationException>(await observed.Task.WaitAsync(Limit));
        Assert.Same(world, GameWorld.Instance);
        host.Stop(Limit);
        Assert.Null(GameWorld.Instance);
    }

    sealed class BlockingSession : Session
    {
        internal readonly ManualResetEventSlim Entered = new();
        internal readonly ManualResetEventSlim Release = new();
        public override void OnConnected(EndPoint endpoint)
        {
            Entered.Set();
            Release.Wait(TimeSpan.FromSeconds(10));
        }
        public override void OnDisconnected(EndPoint endpoint) { }
        public override int OnRecv(ArraySegment<byte> bytes) => bytes.Count;
        public override void OnSend(int count) { }
    }

    [Fact]
    public async Task ListenerStop_DoesNotClaimCompletionWhileAcceptedCallbackIsRunning()
    {
        using Listener listener = new();
        BlockingSession session = new();
        int factoryCalls = 0;
        listener.Init(new IPEndPoint(IPAddress.Loopback, 0), () =>
        {
            Interlocked.Increment(ref factoryCalls);
            return session;
        }, register: 1);
        IPEndPoint endpoint = Assert.IsType<IPEndPoint>(listener.LocalEndPoint);
        using TcpClient client = new();
        try
        {
            await client.ConnectAsync(endpoint.Address, endpoint.Port).WaitAsync(Limit);
            Assert.True(session.Entered.Wait(Limit));
            Assert.Throws<TimeoutException>(() => listener.Stop(TimeSpan.FromMilliseconds(30)));
            Assert.False(listener.Completion.IsCompleted);
        }
        finally
        {
            session.Release.Set();
            listener.Stop(Limit);
            session.Disconnect();
            session.Entered.Dispose();
            session.Release.Dispose();
        }
        Assert.True(listener.Completion.IsCompletedSuccessfully);
        listener.Stop();
        using TcpClient late = new();
        await Assert.ThrowsAsync<SocketException>(async () =>
            await late.ConnectAsync(endpoint.Address, endpoint.Port).WaitAsync(Limit));
        Assert.Equal(1, factoryCalls);
    }

    [Fact]
    public async Task TerminalAcceptFault_IsReportedAfterConnectionsAndWorldAreCleaned()
    {
        GameWorld world = NewWorld();
        using ServerHost host = new(world, new IPEndPoint(IPAddress.Loopback, 0));
        using TcpClient client = new();
        host.Start();
        IPEndPoint endpoint = Assert.IsType<IPEndPoint>(host.LocalEndPoint);
        await client.ConnectAsync(endpoint.Address, endpoint.Port).WaitAsync(Limit);
        Assert.True(SpinWait.SpinUntil(() => OwnedConnections(host) == 1, Limit));
        // Deliberate resource fault injection. This proves cleanup after a terminal accept fault,
        // not reproducibility of any particular OS transient SocketError.
        Listener listener = (Listener)typeof(ServerHost)
            .GetField("_listener", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(host)!;
        Socket socket = (Socket)typeof(Listener)
            .GetField("_listenSocket", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(listener)!;
        socket.Dispose();
        Assert.True(SpinWait.SpinUntil(() => listener.Completion.IsCompleted, Limit));
        Assert.True(listener.Completion.IsFaulted);
        Assert.Throws<AggregateException>(() => host.Stop(Limit));
        Assert.Null(GameWorld.Instance);
        Assert.Equal(0, OwnedConnections(host));
        Assert.Null(host.LocalEndPoint);
        host.Stop();
    }
}

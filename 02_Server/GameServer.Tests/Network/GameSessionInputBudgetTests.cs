using System.Diagnostics;
using System.Net;
using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Sessions;
using Dawnholder.Server.GameServer.Tests.Maps;
using Shared.GameData;
using Shared.Protocol;
using Xunit.Abstractions;

namespace GameServer.Tests.Network;

// Exercise wire dispatch, real map queue admission and emitted responses.
// Budgets are requirement values; no limiter implementation is reproduced here.
[Collection("ConsoleSerial")]
public sealed class GameSessionInputBudgetTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("attack", 20)]
    [InlineData("skill", 20)]
    [InlineData("menu", 10)]
    [InlineData("ping", 10)]
    [InlineData("other", 30)]
    [InlineData("move", 500)]
    public void Flood_IsBoundedBeforeQueueOrHandler(string kind, int budget)
    {
        using var fixture = new InputFixture(enter: kind != "menu");
        BurstResult burst = Flood(fixture, kind, 4 * budget + 3);
        output.WriteLine($"kind={kind}, sent={burst.Sent}, accepted={burst.Accepted}, "
            + $"elapsedMs={burst.Elapsed.TotalMilliseconds:F3}, possibleWindows={burst.PossibleWindows}");

        Assert.InRange(burst.Accepted, 1, budget * burst.PossibleWindows);
        Assert.Equal(0, fixture.Session.DisconnectCalls);
    }

    [Theory]
    [InlineData("attack", 20)]
    [InlineData("skill", 20)]
    [InlineData("menu", 10)]
    [InlineData("ping", 10)]
    [InlineData("other", 30)]
    public void Flood_DropsBeforePerInputLogs_AndWarnsOncePerWindow(string kind, int budget)
    {
        using var fixture = new InputFixture(enter: kind != "menu");
        BurstResult burst = Flood(fixture, kind, 4 * budget + 3);
        string[] lines = fixture.Log.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        int perInputLogs = lines.Count(IsPerInputLog);
        int warnings = lines.Length - perInputLogs;
        output.WriteLine($"kind={kind}, inputLogs={perInputLogs}, otherLines={warnings}, "
            + $"possibleWindows={burst.PossibleWindows}, log={fixture.Log}");

        Assert.InRange(perInputLogs, 0, budget * burst.PossibleWindows);
        // For these valid packets no tick handler emits other diagnostics. Warning wording is not prescribed.
        Assert.InRange(warnings, 1, burst.PossibleWindows);
        Assert.Equal(0, fixture.Session.DisconnectCalls);
    }

    [Fact]
    public async Task PortalAndDungeonConfirmation_ShareOneThirtyRequestBudget()
    {
        const int sharedBudget = 30;
        for (int attempt = 0; attempt < 3; attempt++)
        {
            // Keep both a wall-aligned window and a fresh-session window away from their edge.
            bool aligned = await SessionSocketPair.WaitUntil(() => DateTimeOffset.UtcNow.Millisecond < 200);
            Assert.True(aligned, "Could not observe a safe real-clock window for the fixture");
            var lifetime = Stopwatch.StartNew();
            long wallSecond = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            using var fixture = new InputFixture(enter: true);
            for (int index = 0; index < sharedBudget; index++)
            {
                fixture.Session.OnRecvPacket(new C_EnterPortal { portalId = 0 }.Write());
                fixture.Session.OnRecvPacket(new C_DungeonClearConfirm().Write());
            }
            int admitted = fixture.Map.EnqueueCalls;
            bool withinWindow = lifetime.Elapsed < TimeSpan.FromMilliseconds(500)
                && wallSecond == DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            output.WriteLine($"attempt={attempt}, elapsedMs={lifetime.Elapsed.TotalMilliseconds:F3}, "
                + $"withinWindow={withinWindow}, combinedAdmissions={admitted}");
            if (!withinWindow) continue;

            Assert.Equal(sharedBudget, admitted);
            Assert.Equal(0, fixture.Session.DisconnectCalls);
            return;
        }
        Assert.Fail("Fixture crossed a real-clock window in all three attempts; no budget conclusion");
    }

    [Theory]
    [InlineData("attack")]
    [InlineData("skill")]
    [InlineData("menu")]
    [InlineData("ping")]
    [InlineData("other")]
    [InlineData("move")]
    public void NormalFrequency_ReachesQueueOrResponse(string kind)
    {
        using var fixture = new InputFixture(enter: kind != "menu");
        BurstResult result = Flood(fixture, kind, 1);
        Assert.Equal(1, result.Accepted);
        Assert.Equal(0, fixture.Session.DisconnectCalls);
    }

    [Fact]
    public void Movement_ExactlyFiveHundredInputsRemainAccepted()
    {
        using var fixture = new InputFixture(enter: true);
        BurstResult result = Flood(fixture, "move", 500);
        Assert.Equal(500, result.Accepted);
    }

    [Fact]
    public async Task AfterRealWindowElapses_InputIsAcceptedAgain()
    {
        using var fixture = new InputFixture(enter: true);
        Flood(fixture, "ping", 43);
        int before = fixture.Session.Count(PacketID.S_Pong);
        var elapsed = Stopwatch.StartNew();
        bool windowElapsed = await SessionSocketPair.WaitUntil(() => elapsed.Elapsed >= TimeSpan.FromMilliseconds(1100));
        Assert.True(windowElapsed);
        fixture.Session.OnRecvPacket(new C_Ping { clientTimestampMs = 101 }.Write());

        Assert.Equal(before + 1, fixture.Session.Count(PacketID.S_Pong));
        Assert.Equal(0, fixture.Session.DisconnectCalls);
    }

    [Fact]
    public void ExhaustingAttack_DoesNotConsumeSkillPingOrAnotherConnectionsBudget()
    {
        using var fixture = new InputFixture(enter: true);
        InputSession other = fixture.CreateSession("budget_peer", enter: true);
        fixture.Map.EnqueueCalls = 0;
        Flood(fixture, "attack", 83);
        int before = fixture.Map.EnqueueCalls;
        fixture.Session.OnRecvPacket(Packet("skill", 1));
        other.OnRecvPacket(Packet("attack", 1));
        fixture.Session.OnRecvPacket(Packet("ping", 1));

        Assert.Equal(before + 2, fixture.Map.EnqueueCalls);
        Assert.Equal(1, fixture.Session.Count(PacketID.S_Pong));
        Assert.Equal(0, fixture.Session.DisconnectCalls);
        Assert.Equal(0, other.DisconnectCalls);
    }

    [Fact]
    public void HandshakeAndLoginGates_PrecedeGameplayAdmission()
    {
        using var fixture = new InputFixture(enter: false);
        var untrusted = new InputSession(fixture.World, fixture.Map);
        untrusted.OnRecvPacket(Packet("attack", 1));
        Assert.Equal(1, untrusted.DisconnectCalls);
        Assert.Equal(0, fixture.Map.EnqueueCalls);

        var handshaken = new InputSession(fixture.World, fixture.Map);
        handshaken.OnRecvPacket(new C_Handshake { clientVersion = ProtocolVersion.Current }.Write());
        handshaken.OnRecvPacket(new C_CharacterSelect { characterClass = 0 }.Write());
        handshaken.OnRecvPacket(Packet("attack", 1));
        fixture.Map.Tick(2);
        Assert.False(handshaken.IsLoggedIn);
        Assert.Equal(0, fixture.Map.EnqueueCalls);
        Assert.Empty(fixture.Map.Players);
        Assert.Equal(0, handshaken.DisconnectCalls);
    }

    private static BurstResult Flood(InputFixture fixture, string kind, int count)
    {
        var elapsed = Stopwatch.StartNew();
        for (int index = 0; index < count; index++)
            fixture.Session.OnRecvPacket(Packet(kind, (uint)(index + 1)));
        TimeSpan receiptDuration = elapsed.Elapsed;
        if (kind == "menu") LifecycleTestWorld.Tick(fixture.World!, 10);
        int accepted = kind switch
        {
            "ping" => fixture.Session.Count(PacketID.S_Pong),
            "menu" => fixture.Session.Count(PacketID.S_CharacterListResult),
            _ => fixture.Map.EnqueueCalls,
        };
        // Any fixed one-second window can straddle the burst's unknown first boundary.
        int windows = 1 + (int)Math.Ceiling(receiptDuration.TotalSeconds);
        return new BurstResult(count, accepted, receiptDuration, windows);
    }

    private static ArraySegment<byte> Packet(string kind, uint sequence) => kind switch
    {
        "attack" => new C_Attack { targetEntityId = 99, attackerClientTick = 1 }.Write(),
        "skill" => new C_SkillUse { skillId = (byte)SkillId.Dash, attackerClientTick = 1, facing = 1 }.Write(),
        "menu" => new C_CharacterListRequest { requestId = sequence }.Write(),
        "ping" => new C_Ping { clientTimestampMs = sequence }.Write(),
        "other" => new C_EnterPortal { portalId = 0 }.Write(),
        "move" => new C_MoveIntent { input = 2, clientTick = sequence }.Write(),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private static bool IsPerInputLog(string line)
        => line.StartsWith("[GameSession] OnRecv ", StringComparison.Ordinal)
        || line.StartsWith("[GameSession] Ping received ", StringComparison.Ordinal);

    private sealed record BurstResult(int Sent, int Accepted, TimeSpan Elapsed, int PossibleWindows);

    private sealed class CountingMap : GameMap
    {
        internal CountingMap() : base(MapId.Town) { }
        internal int EnqueueCalls { get; set; }
        public override void EnqueueJob(Action job)
        {
            EnqueueCalls++;
            base.EnqueueJob(job);
        }
    }

    private sealed class InputSession(GameWorld? world, GameMap map) : GameSession(world)
    {
        private readonly List<byte[]> _packets = new();
        internal int DisconnectCalls { get; private set; }
        protected override GameMap GetMap() => map;
        public override void Send(ArraySegment<byte> packet) => _packets.Add(packet.ToArray());
        public override void Disconnect()
        {
            DisconnectCalls++;
            OnDisconnected(new IPEndPoint(IPAddress.Loopback, 0));
        }
        internal int Count(PacketID id) => _packets.Count(packet => BitConverter.ToUInt16(packet, 2) == (ushort)id);
        internal void Clear() => _packets.Clear();
        internal void EnterIsolatedMap()
        {
            SetCharacterClass(0);
            EnterGameWorldIfReady();
        }
    }

    private sealed class InputFixture : IDisposable
    {
        private readonly TextWriter _original = Console.Out;

        internal InputFixture(bool enter)
        {
            Console.SetOut(Log);
            try
            {
                // The existing map-queue tests use a worldless session. Registering this
                // injected map with a real World would require replacing its owned registry.
                if (!enter) World = new GameWorld(new Dictionary<MapId, (MapTerrain?, MapContent?)>());
                Session = CreateSession("budget_primary", enter);
                Map.EnqueueCalls = 0;
                Session.Clear();
                Log.GetStringBuilder().Clear();
            }
            catch
            {
                World?.Stop();
                Console.SetOut(_original);
                throw;
            }
        }

        internal StringWriter Log { get; } = new();
        internal GameWorld? World { get; }
        internal CountingMap Map { get; } = new();
        internal InputSession Session { get; }

        internal InputSession CreateSession(string account, bool enter)
        {
            var session = new InputSession(World, Map);
            session.OnRecvPacket(new C_Handshake { clientVersion = ProtocolVersion.Current }.Write());
            if (World is not null)
            {
                session.OnRecvPacket(new C_Login { accountName = account }.Write());
                World.Accounts.Tick();
                Assert.True(session.IsLoggedIn, "Menu fixture must use the real accepted login path");
            }
            if (enter)
            {
                session.EnterIsolatedMap();
                Map.Tick(1);
                Assert.Contains(Map.Players, player => ReferenceEquals(player.Owner, session));
            }
            Assert.False(session.IsClosing, "Fixture must keep the connection active");
            return session;
        }

        public void Dispose()
        {
            try { World?.Stop(); }
            finally { Console.SetOut(_original); }
        }
    }
}

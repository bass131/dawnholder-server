using System.Buffers.Binary;
using System.Net;
using Dawnholder.Server.GameServer.Handlers;
using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Sessions;
using Dawnholder.Server.GameServer.Tests.Maps;
using Shared.GameData;
using Shared.Protocol;

namespace GameServer.Tests.Network;

// The clock is an input; all admission, parsing, queueing and replies use product entry points.
// Literal budgets below come from the approved ingress contract, never from limiter internals.
[Collection("ConsoleSerial")]
public sealed class InputBudgetBoundaryTests
{
    [Theory]
    [InlineData("attack", 20)]
    [InlineData("skill", 20)]
    [InlineData("menu", 10)]
    [InlineData("ping", 10)]
    [InlineData("other", 30)]
    [InlineData("move", 500)]
    public void ExactLimit_DropsNextBeforeLogging_AndRecoversAtOneSecond(string kind, int limit)
    {
        using var fixture = new BudgetFixture(enter: kind != "menu");
        // Login consumes Menu too; test a fresh window without weakening the ten-input boundary.
        if (kind == "menu") fixture.Clock.AdvanceMilliseconds(1000);
        fixture.Send(kind, limit);
        Assert.Equal(limit, fixture.Accepted(kind));
        Assert.Equal(0, fixture.WarningCount(kind));

        fixture.Send(kind, 3);
        Assert.Equal(limit, fixture.Accepted(kind));
        Assert.Equal(1, fixture.WarningCount(kind));
        Assert.Equal(kind == "move" ? 0 : limit, fixture.InputLogCount);

        fixture.Clock.AdvanceMilliseconds(999);
        fixture.Send(kind, 1);
        Assert.Equal(limit, fixture.Accepted(kind));
        Assert.Equal(1, fixture.WarningCount(kind));

        fixture.Clock.AdvanceMilliseconds(1);
        fixture.Send(kind, limit);
        Assert.Equal(2 * limit, fixture.Accepted(kind));
        Assert.Equal(1, fixture.WarningCount(kind));
        fixture.Send(kind, 2);
        Assert.Equal(2 * limit, fixture.Accepted(kind));
        Assert.Equal(2, fixture.WarningCount(kind));
        Assert.Equal(0, fixture.Session.DisconnectCalls);

        // Idle time does not accumulate credits beyond a single new fixed window.
        fixture.Clock.AdvanceMilliseconds(9000);
        fixture.Send(kind, limit + 1);
        Assert.Equal(3 * limit, fixture.Accepted(kind));
        Assert.Equal(3, fixture.WarningCount(kind));
        Assert.Equal(kind == "move" ? 0 : 3 * limit, fixture.InputLogCount);
    }

    [Theory]
    [InlineData("attack", 20)]
    [InlineData("skill", 20)]
    [InlineData("menu", 10)]
    [InlineData("ping", 10)]
    [InlineData("other", 30)]
    public void ExhaustedConnection_DoesNotConsumePeersBudget(string kind, int limit)
    {
        using var fixture = new BudgetFixture(enter: kind != "menu");
        fixture.Send(kind, limit + 1);
        BudgetSession peer = fixture.CreateSession("budget_peer", enter: kind != "menu");
        int before = fixture.Accepted(kind, peer);
        peer.OnRecvPacket(Packet(kind, 100));

        Assert.Equal(before + 1, fixture.Accepted(kind, peer));
        Assert.Equal(0, peer.DisconnectCalls);
        Assert.Equal(0, fixture.Session.DisconnectCalls);
    }

    [Fact]
    public void ExhaustingAttack_DoesNotConsumeSkillPingOtherOrMovement()
    {
        using var fixture = new BudgetFixture(enter: true);
        fixture.Send("attack", 21);
        fixture.Send("skill", 20);
        fixture.Send("other", 30);
        fixture.Send("ping", 10);
        fixture.Send("move", 500);

        Assert.Equal(570, fixture.Map.EnqueueCalls);
        Assert.Equal(10, fixture.Session.Packets(PacketID.S_Pong).Count());
        Assert.Equal(1, fixture.WarningCount("attack"));
        Assert.Equal(0, fixture.WarningCount("move"));
        Assert.Equal(0, fixture.Session.DisconnectCalls);
    }

    [Fact]
    public void PortalAndDungeonConfirmation_ConsumeOneSharedThirtyRequestWindow()
    {
        using var fixture = new BudgetFixture(enter: true);
        for (int index = 0; index < 15; index++)
        {
            fixture.Session.OnRecvPacket(new C_EnterPortal { portalId = 0 }.Write());
            fixture.Session.OnRecvPacket(new C_DungeonClearConfirm().Write());
        }
        Assert.Equal(30, fixture.Map.EnqueueCalls);

        fixture.Session.OnRecvPacket(new C_EnterPortal { portalId = 0 }.Write());
        fixture.Session.OnRecvPacket(new C_DungeonClearConfirm().Write());
        Assert.Equal(30, fixture.Map.EnqueueCalls);
        Assert.Equal(1, fixture.WarningCount("other"));
        Assert.Equal(30, fixture.InputLogCount);
    }

    [Fact]
    public void AllFourMenuHandlers_ShareTenAdmissions_AndKeepRequestIds()
    {
        using var fixture = new BudgetFixture(enter: false);
        // Keep all ten request admissions after the fixture's login window.
        fixture.Clock.AdvanceMilliseconds(1000);
        foreach (uint sequence in new uint[] { 1, 2 })
            fixture.Session.OnRecvPacket(new C_CharacterListRequest { requestId = sequence }.Write());
        foreach (uint sequence in new uint[] { 3, 4 })
            fixture.Session.OnRecvPacket(new C_CharacterCreateRequest
            {
                requestId = sequence,
                slot = 0,
                classId = 0,
                rawName = "budget_name",
            }.Write());
        foreach (uint sequence in new uint[] { 5, 6 })
            fixture.Session.OnRecvPacket(new C_CharacterSelectRequest
            {
                requestId = sequence,
                characterIdD = "11111111-1111-1111-1111-111111111111",
            }.Write());
        // Invalid selections reach the handler but keep the fixture in the menu.
        fixture.Session.OnRecvPacket(new C_CharacterSelect { characterClass = 255 }.Write());
        fixture.Session.OnRecvPacket(new C_CharacterSelect { characterClass = 255 }.Write());
        foreach (uint sequence in new uint[] { 9, 10 })
            fixture.Session.OnRecvPacket(new C_CharacterListRequest { requestId = sequence }.Write());
        fixture.TickMenu();
        Assert.Equal(10, fixture.InputLogCount);
        Assert.Equal(new uint[] { 1, 2, 3, 4, 5, 6, 9, 10 }, fixture.Session.MenuRequestIds());

        fixture.Session.OnRecvPacket(new C_CharacterListRequest { requestId = 11 }.Write());
        fixture.Session.OnRecvPacket(new C_CharacterCreateRequest { requestId = 12, rawName = "extra" }.Write());
        fixture.Session.OnRecvPacket(new C_CharacterSelectRequest { requestId = 13, characterIdD = "" }.Write());
        fixture.Session.OnRecvPacket(new C_CharacterSelect { characterClass = 0 }.Write());
        fixture.TickMenu();
        Assert.Equal(10, fixture.InputLogCount);
        Assert.Equal(1, fixture.WarningCount("menu"));
        Assert.Equal(new uint[] { 1, 2, 3, 4, 5, 6, 9, 10 }, fixture.Session.MenuRequestIds());
        Assert.False(fixture.Session.HasSelectedClass);
        Assert.Equal(0, fixture.Session.DisconnectCalls);
    }

    [Fact]
    public void FirstPacketGate_RejectsBeforeBudgetLoggingOrPayloadParsing()
    {
        using var fixture = new BudgetFixture(enter: true);
        var untrusted = new BudgetSession(null, fixture.Map, fixture.Clock);
        untrusted.OnRecvPacket(HeaderOnly(PacketID.C_Attack));
        Assert.Equal(1, untrusted.DisconnectCalls);
        Assert.Equal(0, fixture.Map.EnqueueCalls);
        Assert.Equal(0, fixture.InputLogCount);
        Assert.Equal(0, fixture.WarningCount("attack"));
        Assert.Contains("First packet", fixture.Log.ToString());
    }

    [Fact]
    public void BudgetPrecedesClassGate_AndClassGatePrecedesPayloadParsing()
    {
        using var fixture = new BudgetFixture(enter: true);
        var unselected = new BudgetSession(null, fixture.Map, fixture.Clock);
        unselected.OnRecvPacket(new C_Handshake { clientVersion = ProtocolVersion.Current }.Write());
        fixture.Log.GetStringBuilder().Clear();
        for (int index = 0; index < 22; index++)
            unselected.OnRecvPacket(HeaderOnly(PacketID.C_Attack));

        Assert.Equal(0, fixture.Map.EnqueueCalls);
        Assert.Equal(20, fixture.InputLogCount);
        Assert.Equal(20, fixture.Lines.Count(line => line.Contains("before CharacterSelect", StringComparison.Ordinal)));
        Assert.Equal(1, fixture.WarningCount("attack"));
        Assert.Equal(0, unselected.DisconnectCalls);

        unselected.EnterIsolatedMap();
        fixture.Map.Tick(2);
        fixture.Map.EnqueueCalls = 0;
        unselected.OnRecvPacket(Packet("attack", 1));
        Assert.Equal(0, fixture.Map.EnqueueCalls);
        fixture.Clock.AdvanceMilliseconds(1000);
        unselected.OnRecvPacket(Packet("attack", 2));
        Assert.Equal(1, fixture.Map.EnqueueCalls);
    }

    [Fact]
    public void LoginSharesMenuBudget_AndDuplicateHandshakeStillClosesExhaustedConnection()
    {
        using var fixture = new BudgetFixture(enter: false);
        var session = new BudgetSession(fixture.World, fixture.Map, fixture.Clock);
        session.OnRecvPacket(new C_Handshake { clientVersion = ProtocolVersion.Current }.Write());
        for (uint sequence = 1; sequence <= 11; sequence++)
        {
            session.OnRecvPacket(Packet("menu", sequence));
            session.OnRecvPacket(Packet("ping", sequence));
        }
        fixture.TickMenu();
        Assert.False(session.IsLoggedIn);
        session.OnRecvPacket(new C_Login { accountName = "late_login" }.Write());
        fixture.World!.Accounts.Tick();
        Assert.False(session.IsLoggedIn);
        Assert.Empty(session.Packets(PacketID.S_LoginResult));

        fixture.Clock.AdvanceMilliseconds(1000);
        session.OnRecvPacket(new C_Login { accountName = "late_login" }.Write());
        fixture.World.Accounts.Tick();
        Assert.True(session.IsLoggedIn);

        fixture.Log.GetStringBuilder().Clear();
        for (int index = 0; index < 100; index++)
            session.OnRecvPacket(new C_Login { accountName = "late_login" }.Write());
        // The successful login consumed one admission in this new window.
        Assert.Equal(9, fixture.InputLogCount);
        Assert.Equal(9, fixture.Lines.Count(line => line.Contains("Duplicate C_Login", StringComparison.Ordinal)));
        Assert.Equal(1, fixture.WarningCount("menu"));
        Assert.Equal(0, session.DisconnectCalls);

        session.OnRecvPacket(new C_Handshake { clientVersion = ProtocolVersion.Current }.Write());
        Assert.Equal(1, session.DisconnectCalls);
        Assert.Contains("Duplicate C_Handshake", fixture.Log.ToString());
        int sentAtClose = session.Sent.Count;
        session.OnRecvPacket(Packet("ping", 100));
        Assert.Equal(sentAtClose, session.Sent.Count);
        Assert.Equal(1, session.DisconnectCalls);
    }

#if DEBUG
    [Fact]
    public void DebugCheatHasNoIngressBudget()
    {
        using var fixture = new BudgetFixture(enter: true);
        for (int index = 0; index < 100; index++)
            fixture.Session.OnRecvPacket(new C_CheatCommand { cheatType = 0 }.Write());
        Assert.Equal(100, fixture.InputLogCount);
        Assert.DoesNotContain("[InputBudget]", fixture.Log.ToString());
        Assert.Equal(0, fixture.Session.DisconnectCalls);
    }
#endif

    [Theory]
    [InlineData(PacketID.C_Attack, "Attack")]
    [InlineData(PacketID.C_SkillUse, "Skill")]
    [InlineData(PacketID.C_CharacterListRequest, "Menu")]
    [InlineData(PacketID.C_CharacterCreateRequest, "Menu")]
    [InlineData(PacketID.C_CharacterSelectRequest, "Menu")]
    [InlineData(PacketID.C_CharacterSelect, "Menu")]
    [InlineData(PacketID.C_Ping, "Ping")]
    [InlineData(PacketID.C_PartyInvite, "Other")]
    [InlineData(PacketID.C_PartyRespond, "Other")]
    [InlineData(PacketID.C_PartyLeave, "Other")]
    [InlineData(PacketID.C_EnterPortal, "Other")]
    [InlineData(PacketID.C_InventoryRequest, "Other")]
    [InlineData(PacketID.C_ItemUse, "Other")]
    [InlineData(PacketID.C_GuildRequest, "Other")]
    [InlineData(PacketID.C_WeaponEnhance, "Other")]
    [InlineData(PacketID.C_StrongholdUpgrade, "Other")]
    [InlineData(PacketID.C_DungeonClearConfirm, "Other")]
    [InlineData(PacketID.C_MoveIntent, "None")]
    [InlineData(PacketID.C_Handshake, "None")]
    [InlineData(PacketID.C_Login, "Menu")]
#if DEBUG
    [InlineData(PacketID.C_CheatCommand, "None")]
#endif
    public void RegisteredHandler_DeclaresRequiredBudget(PacketID id, string expectedKind)
    {
        Assert.True(HandlerRegistry.TryGet(id, out IPacketHandler handler));
        Assert.Equal(expectedKind, handler.BudgetKind.ToString());
    }

    private static ArraySegment<byte> Packet(string kind, uint sequence) => kind switch
    {
        "attack" => new C_Attack { targetEntityId = 99, attackerClientTick = checked((int)sequence) }.Write(),
        "skill" => new C_SkillUse { skillId = (byte)SkillId.Dash, attackerClientTick = checked((int)sequence), facing = 1 }.Write(),
        "menu" => new C_CharacterListRequest { requestId = sequence }.Write(),
        "ping" => new C_Ping { clientTimestampMs = sequence }.Write(),
        "other" => new C_EnterPortal { portalId = 0 }.Write(),
        "move" => new C_MoveIntent { input = 2, clientTick = sequence }.Write(),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private static ArraySegment<byte> HeaderOnly(PacketID id)
    {
        byte[] header = new byte[4];
        BinaryPrimitives.WriteUInt16LittleEndian(header, 4);
        BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(2), (ushort)id);
        return new ArraySegment<byte>(header);
    }

    private sealed class ManualClock : TimeProvider
    {
        // Nonzero origin ensures tests do not assume a wall-clock second boundary.
        private long _milliseconds = 12345;
        public override long TimestampFrequency => 1000;
        public override long GetTimestamp() => _milliseconds;
        internal void AdvanceMilliseconds(long milliseconds) => _milliseconds += milliseconds;
    }

    private sealed class CountingMap() : GameMap(MapId.Town)
    {
        internal int EnqueueCalls { get; set; }
        public override void EnqueueJob(Action job)
        {
            EnqueueCalls++;
            base.EnqueueJob(job);
        }
    }

    private sealed class BudgetSession(GameWorld? world, GameMap map, TimeProvider clock)
        : GameSession(world, timeProvider: clock)
    {
        internal List<byte[]> Sent { get; } = new();
        internal int DisconnectCalls { get; private set; }
        protected override GameMap GetMap() => map;
        public override void Send(ArraySegment<byte> packet) => Sent.Add(packet.ToArray());
        public override void Disconnect()
        {
            DisconnectCalls++;
            OnDisconnected(new IPEndPoint(IPAddress.Loopback, 0));
        }
        internal IEnumerable<byte[]> Packets(PacketID id)
            => Sent.Where(packet => BinaryPrimitives.ReadUInt16LittleEndian(packet.AsSpan(2)) == (ushort)id);
        internal uint[] MenuRequestIds()
            => Sent.Where(packet => (PacketID)BinaryPrimitives.ReadUInt16LittleEndian(packet.AsSpan(2))
                    is PacketID.S_CharacterListResult or PacketID.S_CharacterCreateResult or PacketID.S_CharacterSelectResult)
                .Select(packet => BinaryPrimitives.ReadUInt32LittleEndian(packet.AsSpan(4))).ToArray();
        internal void EnterIsolatedMap()
        {
            SetCharacterClass(0);
            EnterGameWorldIfReady();
        }
    }

    private sealed class BudgetFixture : IDisposable
    {
        private readonly TextWriter _original = Console.Out;
        private long _tick = 10;
        private uint _sequence;

        internal BudgetFixture(bool enter)
        {
            Console.SetOut(Log);
            try
            {
                // Use a real World for menu replies and an isolated counting map for gameplay admission.
                if (!enter) World = new GameWorld(new Dictionary<MapId, (MapTerrain?, MapContent?)>());
                Session = CreateSession("budget_primary", enter);
                Map.EnqueueCalls = 0;
                Session.Sent.Clear();
                Log.GetStringBuilder().Clear();
            }
            catch
            {
                World?.Stop();
                Console.SetOut(_original);
                throw;
            }
        }

        internal ManualClock Clock { get; } = new();
        internal StringWriter Log { get; } = new();
        internal GameWorld? World { get; }
        internal CountingMap Map { get; } = new();
        internal BudgetSession Session { get; }
        internal string[] Lines => Log.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        internal int InputLogCount => Lines.Count(line => line.StartsWith("[GameSession] OnRecv ", StringComparison.Ordinal)
            || line.StartsWith("[GameSession] Ping received ", StringComparison.Ordinal));

        internal int WarningCount(string kind) => Lines.Count(line => kind == "move"
            ? line.StartsWith("[Cheat]", StringComparison.Ordinal) && line.Contains("intent rate exceeded", StringComparison.Ordinal)
            : line.StartsWith($"[InputBudget] {char.ToUpperInvariant(kind[0])}{kind[1..]} ", StringComparison.Ordinal));

        internal void Send(string kind, int count)
        {
            for (int index = 0; index < count; index++) Session.OnRecvPacket(Packet(kind, ++_sequence));
        }

        internal int Accepted(string kind, BudgetSession? session = null)
        {
            session ??= Session;
            if (kind == "menu") TickMenu();
            return kind switch
            {
                "menu" => session.Packets(PacketID.S_CharacterListResult).Count(),
                "ping" => session.Packets(PacketID.S_Pong).Count(),
                _ => Map.EnqueueCalls,
            };
        }

        internal void TickMenu()
        {
            if (World is not null) LifecycleTestWorld.Tick(World, ++_tick);
        }

        internal BudgetSession CreateSession(string account, bool enter)
        {
            var session = new BudgetSession(World, Map, Clock);
            session.OnRecvPacket(new C_Handshake { clientVersion = ProtocolVersion.Current }.Write());
            if (World is not null)
            {
                session.OnRecvPacket(new C_Login { accountName = account }.Write());
                World.Accounts.Tick();
                Assert.True(session.IsLoggedIn);
            }
            if (enter)
            {
                session.EnterIsolatedMap();
                Map.Tick(1);
                Assert.True(session.HasSelectedClass);
                Assert.Contains(Map.Players, player => ReferenceEquals(player.Owner, session));
            }
            Assert.False(session.IsClosing);
            return session;
        }

        public void Dispose()
        {
            try { World?.Stop(); }
            finally { Console.SetOut(_original); }
        }
    }
}

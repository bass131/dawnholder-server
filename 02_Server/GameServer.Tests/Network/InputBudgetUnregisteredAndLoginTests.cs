using System.Buffers.Binary;
using System.Net;
using Dawnholder.Server.GameServer.Handlers;
using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;
using Shared.Protocol;

namespace GameServer.Tests.Network;

[Collection("ConsoleSerial")]
public sealed class InputBudgetUnregisteredAndLoginTests
{
    private const ushort UnknownPacketId = ushort.MaxValue;

    [Fact]
    public void UnknownPackets_ExceedingThirty_LogOnlyAdmissionsAndOneWarning_KeepConnection()
    {
        using var fixture = new BudgetFixture();
        fixture.SendUnknown(35);

        Assert.Equal(30, fixture.UnknownLogCount);
        Assert.Equal(1, fixture.WarningCount("Other"));
        Assert.Equal(0, fixture.Session.DisconnectCalls);
        Assert.False(fixture.Session.IsClosing);
    }

    [Fact]
    public void ThirtyUnknownPackets_ExhaustRegisteredOtherInputBudget()
    {
        using var fixture = new BudgetFixture();
        fixture.Session.EnterIsolatedMap();
        fixture.Map.Tick(1);
        fixture.Map.EnqueueCalls = 0;
        fixture.ClearLog();

        fixture.SendUnknown(30);
        fixture.Session.OnRecvPacket(new C_EnterPortal { portalId = 0 }.Write());

        Assert.Equal(0, fixture.Map.EnqueueCalls);
        Assert.Equal(0, fixture.InputLogCount("C_EnterPortal"));
        Assert.Equal(30, fixture.UnknownLogCount);
        Assert.Equal(1, fixture.WarningCount("Other"));
        Assert.Equal(0, fixture.Session.DisconnectCalls);
    }

    [Fact]
    public void UnknownPacketBudget_StaysExhaustedUntilOneSecond_ThenRenewsAllowanceAndWarning()
    {
        using var fixture = new BudgetFixture();
        fixture.SendUnknown(31);
        int firstWindowLogs = fixture.UnknownLogCount;
        int firstWindowWarnings = fixture.WarningCount("Other");

        fixture.Clock.AdvanceMilliseconds(999);
        fixture.SendUnknown(1);
        int beforeBoundaryLogs = fixture.UnknownLogCount;
        int beforeBoundaryWarnings = fixture.WarningCount("Other");

        fixture.Clock.AdvanceMilliseconds(1);
        fixture.SendUnknown(31);
        int renewedWindowLogs = fixture.UnknownLogCount;
        int renewedWindowWarnings = fixture.WarningCount("Other");

        Assert.Equal((30, 30, 60), (firstWindowLogs, beforeBoundaryLogs, renewedWindowLogs));
        Assert.Equal((1, 1, 2), (firstWindowWarnings, beforeBoundaryWarnings, renewedWindowWarnings));
        Assert.Equal(0, fixture.Session.DisconnectCalls);
        Assert.False(fixture.Session.IsClosing);
    }

    [Fact]
    public void DuplicateLogin_ExceedingMenuBudget_DropsBothLogsAndWarnsOnce_KeepConnection()
    {
        using var fixture = new BudgetFixture(withWorld: true);
        fixture.Login();
        Assert.True(fixture.Session.IsLoggedIn);
        fixture.ClearLog();
        fixture.Session.Sent.Clear();

        for (int index = 0; index < 12; index++)
            fixture.Session.OnRecvPacket(new C_Login { accountName = "budget_login" }.Write());
        fixture.World!.Accounts.Tick();

        // The first successful login has already consumed one of this window's ten admissions.
        Assert.Equal(9, fixture.InputLogCount("C_Login"));
        Assert.Equal(9, fixture.DuplicateLoginLogCount);
        Assert.Equal(1, fixture.WarningCount("Menu"));
        Assert.Empty(fixture.Session.Sent);
        Assert.True(fixture.Session.IsLoggedIn);
        Assert.Equal(0, fixture.Session.DisconnectCalls);
        Assert.False(fixture.Session.IsClosing);
    }

    [Fact]
    public void TenMenuInputs_ExhaustLoginBudgetBeforeLoggingOrAccountSubmission()
    {
        using var fixture = new BudgetFixture(withWorld: true);
        for (uint sequence = 1; sequence <= 10; sequence++)
        {
            fixture.Session.OnRecvPacket(new C_CharacterListRequest
            {
                requestId = sequence,
            }.Write());
        }
        Assert.Equal(10, fixture.InputLogCount("C_CharacterListRequest"));
        Assert.Equal(0, fixture.WarningCount("Menu"));

        fixture.Login();

        Assert.False(fixture.Session.IsLoggedIn);
        Assert.Empty(fixture.Session.Packets(PacketID.S_LoginResult));
        Assert.Equal(0, fixture.InputLogCount("C_Login"));
        Assert.Equal(0, fixture.DuplicateLoginLogCount);
        Assert.Equal(1, fixture.WarningCount("Menu"));
        Assert.Equal(0, fixture.Session.DisconnectCalls);
        Assert.False(fixture.Session.IsClosing);
    }

    [Fact]
    public void UnknownPacket_WithinBudget_PreservesExistingDroppedLog()
    {
        using var fixture = new BudgetFixture();
        fixture.SendUnknown(1);

        Assert.Equal(new[] { "[GameSession] Unknown PacketId 65535 — dropped" }, fixture.Lines);
        Assert.Equal(0, fixture.WarningCount("Other"));
        Assert.Equal(0, fixture.Session.DisconnectCalls);
        Assert.False(fixture.Session.IsClosing);
    }

    [Fact]
    public void FirstLogin_WithinBudget_CompletesNormally()
    {
        using var fixture = new BudgetFixture(withWorld: true);
        fixture.Login();

        Assert.True(fixture.Session.IsLoggedIn);
        byte[] response = Assert.Single(fixture.Session.Packets(PacketID.S_LoginResult));
        var result = new S_LoginResult();
        result.Read(new ArraySegment<byte>(response));
        Assert.Equal((byte)LoginResult.Accepted, result.result);
        Assert.Equal(1, fixture.InputLogCount("C_Login"));
        Assert.Equal(0, fixture.DuplicateLoginLogCount);
        Assert.Equal(0, fixture.WarningCount("Menu"));
        Assert.Equal(0, fixture.Session.DisconnectCalls);
        Assert.False(fixture.Session.IsClosing);
    }

    [Fact]
    public void UnknownPacket_BeforeHandshake_DisconnectsWithoutBudgetOrUnknownLog()
    {
        using var fixture = new BudgetFixture(handshake: false);
        fixture.SendUnknown(1);

        Assert.Equal(1, fixture.Session.DisconnectCalls);
        Assert.True(fixture.Session.IsClosing);
        Assert.Equal(0, fixture.UnknownLogCount);
        Assert.DoesNotContain("[InputBudget]", fixture.Log.ToString());
        Assert.Contains("First packet", fixture.Log.ToString());
    }

    private static ArraySegment<byte> UnknownPacket()
    {
        byte[] header = new byte[4];
        BinaryPrimitives.WriteUInt16LittleEndian(header, 4);
        BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(2), UnknownPacketId);
        return new ArraySegment<byte>(header);
    }

    private sealed class ManualClock : TimeProvider
    {
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

        public override void Send(ArraySegment<byte> packet) => Sent.Add(packet.ToArray());

        public override void Disconnect()
        {
            DisconnectCalls++;
            OnDisconnected(new IPEndPoint(IPAddress.Loopback, 0));
        }

        internal IEnumerable<byte[]> Packets(PacketID id)
            => Sent.Where(packet => BinaryPrimitives.ReadUInt16LittleEndian(packet.AsSpan(2)) == (ushort)id);

        internal void EnterIsolatedMap()
        {
            SetCharacterClass(0);
            EnterGameWorldIfReady();
        }

        protected override GameMap GetMap() => map;
    }

    private sealed class BudgetFixture : IDisposable
    {
        private readonly TextWriter _original = Console.Out;

        internal BudgetFixture(bool withWorld = false, bool handshake = true)
        {
            Console.SetOut(Log);
            try
            {
                Assert.False(HandlerRegistry.TryGet((PacketID)UnknownPacketId, out _));
                if (withWorld) World = new GameWorld(new Dictionary<MapId, (MapTerrain?, MapContent?)>());
                Session = new BudgetSession(World, Map, Clock);
                if (handshake)
                    Session.OnRecvPacket(new C_Handshake { clientVersion = ProtocolVersion.Current }.Write());
                Session.Sent.Clear();
                ClearLog();
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
        internal string[] Lines => Log.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        internal int UnknownLogCount => Lines.Count(line => line.StartsWith("[GameSession] Unknown PacketId ", StringComparison.Ordinal));
        internal int DuplicateLoginLogCount => Lines.Count(line => line.StartsWith("[Trust] Duplicate C_Login", StringComparison.Ordinal));

        public void Dispose()
        {
            try
            {
                World?.Stop();
            }
            finally
            {
                Console.SetOut(_original);
                Log.Dispose();
            }
        }

        internal int InputLogCount(string packetName)
            => Lines.Count(line => line.StartsWith($"[GameSession] OnRecv {packetName} (", StringComparison.Ordinal));

        internal int WarningCount(string kind)
            => Lines.Count(line => line.StartsWith($"[InputBudget] {kind} ", StringComparison.Ordinal));

        internal void ClearLog() => Log.GetStringBuilder().Clear();

        internal void SendUnknown(int count)
        {
            for (int index = 0; index < count; index++) Session.OnRecvPacket(UnknownPacket());
        }

        internal void Login()
        {
            Session.OnRecvPacket(new C_Login { accountName = "budget_login" }.Write());
            World!.Accounts.Tick();
        }
    }
}

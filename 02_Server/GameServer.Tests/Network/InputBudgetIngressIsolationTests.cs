using System.Buffers.Binary;
using System.Net;
using Dawnholder.Server.GameServer.Handlers;
using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;
using Shared.Protocol;

namespace GameServer.Tests.Network;

// Exercise the receive boundary with literal contract limits and a controllable input clock.
[Collection("ConsoleSerial")]
public sealed class InputBudgetIngressIsolationTests
{
    [Fact]
    public void UnknownIds_ShareExactThirtiethAdmission_AndRenewAtOneSecond()
    {
        using var fixture = new IngressFixture();
        fixture.SendUnknown(29);
        Assert.Equal(29, fixture.Count("[GameSession] Unknown PacketId "));
        Assert.Equal(0, fixture.Warnings("Other"));

        fixture.Session.OnRecvPacket(Header(65534));
        Assert.Equal(30, fixture.Count("[GameSession] Unknown PacketId "));
        Assert.Contains("[GameSession] Unknown PacketId 65534 — dropped", fixture.Lines);
        Assert.Equal(0, fixture.Warnings("Other"));

        fixture.SendUnknown(1);
        Assert.Equal(30, fixture.Count("[GameSession] Unknown PacketId "));
        Assert.Equal(1, fixture.Warnings("Other"));
        fixture.Clock.AdvanceMilliseconds(999);
        fixture.SendUnknown(1);
        Assert.Equal(30, fixture.Count("[GameSession] Unknown PacketId "));
        Assert.Equal(1, fixture.Warnings("Other"));

        fixture.Clock.AdvanceMilliseconds(1);
        fixture.SendUnknown(30);
        Assert.Equal(60, fixture.Count("[GameSession] Unknown PacketId "));
        Assert.Equal(1, fixture.Warnings("Other"));
        fixture.SendUnknown(1);
        Assert.Equal(60, fixture.Count("[GameSession] Unknown PacketId "));
        Assert.Equal(2, fixture.Warnings("Other"));
        Assert.Equal(0, fixture.Session.DisconnectCalls);
        Assert.False(fixture.Session.IsClosing);
    }

    [Fact]
    public void Login_TenthAdmissionRuns_EleventhSkipsBothLogs_AndNextWindowRenews()
    {
        using var fixture = new IngressFixture();
        fixture.SendLogins(1);
        fixture.AssertAcceptedLogin();
        fixture.SendLogins(8);
        Assert.Equal(9, fixture.Inputs("C_Login"));
        Assert.Equal(8, fixture.Count("[Trust] Duplicate C_Login"));

        fixture.SendLogins(1);
        Assert.Equal(10, fixture.Inputs("C_Login"));
        Assert.Equal(9, fixture.Count("[Trust] Duplicate C_Login"));
        Assert.Equal(0, fixture.Warnings("Menu"));
        fixture.SendLogins(1);
        Assert.Equal(10, fixture.Inputs("C_Login"));
        Assert.Equal(9, fixture.Count("[Trust] Duplicate C_Login"));
        Assert.Equal(1, fixture.Warnings("Menu"));

        fixture.Clock.AdvanceMilliseconds(999);
        fixture.SendLogins(1);
        Assert.Equal(10, fixture.Inputs("C_Login"));
        Assert.Equal(9, fixture.Count("[Trust] Duplicate C_Login"));
        Assert.Equal(1, fixture.Warnings("Menu"));
        fixture.Clock.AdvanceMilliseconds(1);
        fixture.SendLogins(10);
        Assert.Equal(20, fixture.Inputs("C_Login"));
        Assert.Equal(19, fixture.Count("[Trust] Duplicate C_Login"));
        Assert.Equal(1, fixture.Warnings("Menu"));
        fixture.SendLogins(1);
        Assert.Equal(20, fixture.Inputs("C_Login"));
        Assert.Equal(19, fixture.Count("[Trust] Duplicate C_Login"));
        Assert.Equal(2, fixture.Warnings("Menu"));
        fixture.AssertAcceptedLogin();
        Assert.Equal(0, fixture.Session.DisconnectCalls);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void OtherAndMenu_KeepIndependentAllowancesAndWarnings(bool otherFirst)
    {
        using var fixture = new IngressFixture();
        if (otherFirst)
        {
            fixture.SendUnknown(31);
            Assert.Equal(1, fixture.Warnings("Other"));
            Assert.Equal(0, fixture.Warnings("Menu"));
            fixture.SendLogins(10);
        }
        else
        {
            fixture.SendLogins(11);
            Assert.Equal(1, fixture.Warnings("Menu"));
            Assert.Equal(0, fixture.Warnings("Other"));
            fixture.SendUnknown(30);
        }

        Assert.Equal(30, fixture.Count("[GameSession] Unknown PacketId "));
        Assert.Equal(10, fixture.Inputs("C_Login"));
        Assert.Equal(9, fixture.Count("[Trust] Duplicate C_Login"));
        fixture.AssertAcceptedLogin();
        Assert.Equal(otherFirst ? 0 : 1, fixture.Warnings("Menu"));
        Assert.Equal(otherFirst ? 1 : 0, fixture.Warnings("Other"));

        if (otherFirst) fixture.SendLogins(1);
        else fixture.SendUnknown(1);
        Assert.Equal(30, fixture.Count("[GameSession] Unknown PacketId "));
        Assert.Equal(10, fixture.Inputs("C_Login"));
        Assert.Equal(1, fixture.Warnings("Other"));
        Assert.Equal(1, fixture.Warnings("Menu"));
        Assert.Equal(0, fixture.Session.DisconnectCalls);
    }

    [Fact]
    public void FirstLogin_WhenMenuIsExhausted_DoesNotSubmit_AndSucceedsAfterRenewal()
    {
        using var fixture = new IngressFixture();
        for (uint request = 1; request <= 10; request++)
            fixture.Session.OnRecvPacket(new C_CharacterListRequest { requestId = request }.Write());
        Assert.Equal(10, fixture.Inputs("C_CharacterListRequest"));
        Assert.Equal(0, fixture.Warnings("Menu"));

        fixture.SendLogins(1);
        Assert.False(fixture.Session.IsLoggedIn);
        Assert.Empty(fixture.Session.LoginResults);
        Assert.Equal(0, fixture.Inputs("C_Login"));
        Assert.Equal(0, fixture.Count("[Trust] Duplicate C_Login"));
        Assert.Equal(1, fixture.Warnings("Menu"));
        fixture.Clock.AdvanceMilliseconds(999);
        fixture.SendLogins(1);
        Assert.False(fixture.Session.IsLoggedIn);
        Assert.Empty(fixture.Session.LoginResults);
        Assert.Equal(0, fixture.Inputs("C_Login"));
        Assert.Equal(1, fixture.Warnings("Menu"));

        fixture.Clock.AdvanceMilliseconds(1);
        fixture.SendLogins(1);
        fixture.AssertAcceptedLogin();
        Assert.Equal(1, fixture.Inputs("C_Login"));
        Assert.Equal(0, fixture.Count("[Trust] Duplicate C_Login"));
        Assert.Equal(1, fixture.Warnings("Menu"));
        Assert.Equal(0, fixture.Session.DisconnectCalls);
    }

    [Fact]
    public void RegisteredOtherRejections_ConsumeTheSameAllowanceAsUnknownIds()
    {
        using var fixture = new IngressFixture();
        // A header is enough: the unchanged class gate must reject before payload parsing.
        for (int index = 0; index < 30; index++)
            fixture.Session.OnRecvPacket(Header((ushort)PacketID.C_EnterPortal));
        Assert.Equal(30, fixture.Inputs("C_EnterPortal"));
        Assert.Equal(30, fixture.Count("[Trust] C_EnterPortal before CharacterSelect"));
        Assert.Equal(0, fixture.Warnings("Other"));

        fixture.SendUnknown(1);
        Assert.Equal(0, fixture.Count("[GameSession] Unknown PacketId "));
        Assert.Equal(1, fixture.Warnings("Other"));
        Assert.Equal(0, fixture.Session.DisconnectCalls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RepeatedHandshake_ClosesBeforeBudgetOrPayloadParsing(bool exhaustBudgets)
    {
        using var fixture = new IngressFixture();
        if (exhaustBudgets)
        {
            fixture.SendUnknown(31);
            fixture.SendLogins(11);
        }
        fixture.Log.GetStringBuilder().Clear();

        fixture.Session.OnRecvPacket(Header((ushort)PacketID.C_Handshake));
        Assert.Equal(1, fixture.Session.DisconnectCalls);
        Assert.True(fixture.Session.IsClosing);
        Assert.Equal(1, fixture.Count("[Trust] Duplicate C_Handshake"));
        Assert.Equal(0, fixture.Count("[InputBudget]"));
        Assert.Equal(0, fixture.Inputs("C_Handshake"));

        fixture.SendUnknown(1);
        fixture.SendLogins(1);
        Assert.Equal(0, fixture.Count("[GameSession] Unknown PacketId "));
        Assert.Equal(0, fixture.Inputs("C_Login"));
        Assert.Equal(1, fixture.Session.DisconnectCalls);
    }

    private static ArraySegment<byte> Header(ushort id)
    {
        byte[] bytes = new byte[4];
        BinaryPrimitives.WriteUInt16LittleEndian(bytes, 4);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(2), id);
        return new ArraySegment<byte>(bytes);
    }

    private sealed class ManualClock : TimeProvider
    {
        private long _milliseconds = 41713;
        public override long TimestampFrequency => 1000;
        public override long GetTimestamp() => _milliseconds;
        internal void AdvanceMilliseconds(long milliseconds) => _milliseconds += milliseconds;
    }

    private sealed class IngressSession(GameWorld world, TimeProvider clock) : GameSession(world, timeProvider: clock)
    {
        internal List<S_LoginResult> LoginResults { get; } = new();
        internal int DisconnectCalls { get; private set; }

        public override void Send(ArraySegment<byte> packet)
        {
            if (BinaryPrimitives.ReadUInt16LittleEndian(packet.AsSpan(2)) != (ushort)PacketID.S_LoginResult) return;
            var result = new S_LoginResult();
            result.Read(packet);
            LoginResults.Add(result);
        }

        public override void Disconnect()
        {
            DisconnectCalls++;
            OnDisconnected(new IPEndPoint(IPAddress.Loopback, 0));
        }
    }

    private sealed class IngressFixture : IDisposable
    {
        private readonly TextWriter _original = Console.Out;

        internal IngressFixture()
        {
            Console.SetOut(Log);
            try
            {
                Assert.False(HandlerRegistry.TryGet((PacketID)65535, out _));
                Assert.False(HandlerRegistry.TryGet((PacketID)65534, out _));
                World = new GameWorld(new Dictionary<MapId, (MapTerrain?, MapContent?)>());
                Session = new IngressSession(World, Clock);
                Session.OnRecvPacket(new C_Handshake { clientVersion = ProtocolVersion.Current }.Write());
                Log.GetStringBuilder().Clear();
            }
            catch
            {
                World?.Stop();
                Console.SetOut(_original);
                Log.Dispose();
                throw;
            }
        }

        internal ManualClock Clock { get; } = new();
        internal StringWriter Log { get; } = new();
        internal GameWorld World { get; }
        internal IngressSession Session { get; }
        internal string[] Lines => Log.ToString().Split('\n',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        public void Dispose()
        {
            try
            {
                World.Stop();
            }
            finally
            {
                Console.SetOut(_original);
                Log.Dispose();
            }
        }

        internal int Count(string prefix) => Lines.Count(line => line.StartsWith(prefix, StringComparison.Ordinal));
        internal int Inputs(string name) => Count($"[GameSession] OnRecv {name} (");
        internal int Warnings(string kind) => Count($"[InputBudget] {kind} ");

        internal void SendUnknown(int count)
        {
            for (int index = 0; index < count; index++) Session.OnRecvPacket(Header(65535));
        }

        internal void SendLogins(int count)
        {
            for (int index = 0; index < count; index++)
                Session.OnRecvPacket(new C_Login { accountName = "ingress_account" }.Write());
            World.Accounts.Tick();
        }

        internal void AssertAcceptedLogin()
        {
            Assert.True(Session.IsLoggedIn);
            S_LoginResult reply = Assert.Single(Session.LoginResults);
            Assert.Equal((byte)LoginResult.Accepted, reply.result);
        }
    }
}


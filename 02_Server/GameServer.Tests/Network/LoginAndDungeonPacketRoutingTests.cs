using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Sessions;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Tests.Network;

// Re-verification of the main merge (goal 2026-10-11-multi-account-login 「PR 경계와 점검」: Content's dungeon packets
// took 39·40 and version 18, so login moved to 41·42 and version 19). G1: one real session handles login and the
// dungeon packets without either reaching the other's handler. Login and confirm go in as literal wire bytes, and
// the login reply is compared as literal bytes, so a wrong ID in PDL, generated code or the handler table shows here.
[Collection("GameWorldRegistryTests")]
public sealed class LoginAndDungeonPacketRoutingTests : IDisposable
{
    const ushort LoginReplyId = 42;
    const ushort DungeonClearResultId = 39;

    // Ticks after an input, with periodic world packets in between; a login reply leaves within one tick.
    const int SettleTicks = 3;

    // C_Login, ID 41: size 26, id 41, UTF-16 byte length 20, then "g1_routing" in UTF-16 LittleEndian.
    static readonly byte[] LoginWire =
    {
        26, 0, 41, 0, 20, 0,
        (byte)'g', 0, (byte)'1', 0, (byte)'_', 0, (byte)'r', 0, (byte)'o', 0,
        (byte)'u', 0, (byte)'t', 0, (byte)'i', 0, (byte)'n', 0, (byte)'g', 0,
    };

    // A second C_Login, ID 41, on the same connection: size 22, UTF-16 byte length 16, "g1_other".
    static readonly byte[] SecondLoginWire =
    {
        22, 0, 41, 0, 16, 0,
        (byte)'g', 0, (byte)'1', 0, (byte)'_', 0, (byte)'o', 0,
        (byte)'t', 0, (byte)'h', 0, (byte)'e', 0, (byte)'r', 0,
    };

    // C_DungeonClearConfirm, ID 40: size 5, id 40, reserved 0 (dungeon goal P1). The server registers no handler for it.
    static readonly byte[] ConfirmWire = { 5, 0, 40, 0, 0 };

    // S_LoginResult, ID 42: size 5, id 42, result Accepted 0.
    static readonly byte[] AcceptedReplyWire = { 5, 0, 42, 0, 0 };

    readonly LoginTestWorld _login = new();

    public void Dispose() => _login.Dispose();

    // G1, before login: handshake -> 40 (confirm) -> C_CharacterSelect(Knight) -> literal 41 "g1_routing" ->
    // C_CharacterSelect(Knight). Observed: nothing is sent and the session stays open, logged out and outside the
    // world until the login; the login is answered with exactly the bytes 05 00 2A 00 00 and the account is
    // "g1_routing"; the select after the login enters the world, so the dropped early select left nothing behind.
    [Fact]
    public void G1_BeforeLogin_Confirm40AndClassSelectAreDropped_ThenLiteralLogin41_IsAnsweredWithAccepted42Bytes()
    {
        WireRecordingSession session = Handshaken();

        session.OnRecvPacket(ConfirmWire);
        _login.Tick();
        session.OnRecvPacket(new C_CharacterSelect { characterClass = 0 }.Write());
        Settle();
        byte[][] sentBeforeLogin = session.TakeFrames();
        bool loggedInBeforeLogin = session.IsLoggedIn;
        bool closedBeforeLogin = session.IsClosing;
        bool playerBeforeLogin = HasPlayer(session);

        session.OnRecvPacket(LoginWire);
        _login.Tick();
        byte[][] sentForLogin = session.TakeFrames();
        string? accountAfterLogin = session.Account?.Name;
        session.OnRecvPacket(new C_CharacterSelect { characterClass = 0 }.Write());
        _login.Tick();
        bool playerAfterSelect = HasPlayer(session);

        Assert.Empty(sentBeforeLogin);
        Assert.False(loggedInBeforeLogin, "the confirm (40) or the class select changed the login state");
        Assert.False(closedBeforeLogin, "the confirm (40) reached the login handler, which closes on a bad name");
        Assert.False(playerBeforeLogin, "a class select before login entered the world");
        Assert.Equal(new[] { AcceptedReplyWire }, sentForLogin);
        Assert.Equal("g1_routing", accountAfterLogin);
        Assert.True(playerAfterSelect, "the class select after the login did not enter the world");
        Assert.Equal(0, session.DisconnectCalls);
    }

    // G1, after login and entry: 40 (confirm) and then a second literal 41 "g1_other" on the same connection.
    // Observed over the following ticks: no S_LoginResult (42) and no dungeon result (39) is sent, the connection
    // stays open, the account stays "g1_routing" and the player stays in the world.
    [Fact]
    public void G1_AfterLoginAndEntry_Confirm40AndSecondLogin41_GetNoReply_AndKeepTheAccountAndPlayer()
    {
        WireRecordingSession session = Handshaken();
        session.OnRecvPacket(LoginWire);
        _login.Tick();
        session.OnRecvPacket(new C_CharacterSelect { characterClass = 0 }.Write());
        _login.Tick();
        Assert.True(HasPlayer(session), "fixture: the literal login and a Knight select did not enter the world");
        session.TakeFrames();

        session.OnRecvPacket(ConfirmWire);
        Settle();
        session.OnRecvPacket(SecondLoginWire);
        Settle();
        ushort[] sentIds = session.TakeFrames().Select(frame => BitConverter.ToUInt16(frame, 2)).ToArray();

        Assert.DoesNotContain(LoginReplyId, sentIds);
        Assert.DoesNotContain(DungeonClearResultId, sentIds);
        Assert.False(session.IsClosing);
        Assert.Equal(0, session.DisconnectCalls);
        Assert.Equal("g1_routing", session.Account?.Name);
        Assert.True(HasPlayer(session), "the player left the world after the confirm or the second login");
    }

    WireRecordingSession Handshaken()
    {
        WireRecordingSession session = new(_login.World);
        session.OnConnected(LoginTestWorld.Endpoint);
        session.OnRecvPacket(new C_Handshake { clientVersion = ProtocolVersion.Current }.Write());
        Assert.Single(session.TakeFrames());
        return session;
    }

    void Settle()
    {
        for (int i = 0; i < SettleTicks; i++)
        {
            _login.Tick();
        }
    }

    bool HasPlayer(GameSession session) => _login.Players.Any(player => ReferenceEquals(player.Owner, session));

    // Keeps every frame the server sends as raw bytes. LoginTestSession decodes results with the generated reader,
    // which would hide a wrong wire ID, so this test keeps its own recorder.
    sealed class WireRecordingSession : GameSession
    {
        readonly List<byte[]> _frames = new();

        internal WireRecordingSession(GameWorld world)
            : base(world)
        {
        }

        internal int DisconnectCalls { get; private set; }

        public override void Send(ArraySegment<byte> packet)
        {
            lock (_frames)
            {
                _frames.Add(packet.ToArray());
            }
        }

        // Without a socket, a close reaches the session through OnDisconnected as in LoginTestSession.
        public override void Disconnect()
        {
            DisconnectCalls++;
            OnDisconnected(LoginTestWorld.Endpoint);
        }

        internal byte[][] TakeFrames()
        {
            lock (_frames)
            {
                byte[][] frames = _frames.ToArray();
                _frames.Clear();
                return frames;
            }
        }
    }
}

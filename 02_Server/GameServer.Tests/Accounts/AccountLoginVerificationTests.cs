using System.Buffers.Binary;
using System.Text;
using Dawnholder.Server.GameServer.Accounts;
using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Sessions;
using Dawnholder.Server.GameServer.Tests.Network;
using Shared.GameData;
using Shared.Protocol;
using Xunit.Abstractions;

namespace Dawnholder.Server.GameServer.Tests.Accounts;

// Independent verification of the multi-account login (goal 2026-10-11-multi-account-login), items the
// requirement tests left to the verifier: D1 a length prefix smaller than the frame, D4 login against a
// disconnect on another thread, D5 the account number never reaching the client, and the two login log
// formats. Everything goes through the real world tick and the session's raw packet receive.
[Collection("GameWorldRegistryTests")]
public sealed class AccountLoginVerificationTests : IDisposable
{
    readonly LoginTestWorld _login = new();
    readonly ITestOutputHelper _output;

    public AccountLoginVerificationTests(ITestOutputHelper output) => _output = output;

    public void Dispose() => _login.Dispose();

    // D1 (completion condition 6, lead judgement 1): the UInt16 prefix claims fewer UTF-16 bytes than the frame
    // carries. The generated reader would decode only the claimed part ("abc" and the 16-character name are valid
    // on their own), so the leftover bytes must still end in InvalidName, a close, and no account change.
    [Theory]
    [InlineData(6, "abcdef", false)]              // prefix "abc", 3 more characters left over
    [InlineData(0, "abc", false)]                 // prefix 0, the whole name left over
    [InlineData(32, "abcdefghijklmnopq", false)] // prefix 16 characters, the 17th left over
    [InlineData(6, "abc", true)]                  // exact name, one stray byte after it
    public void D1_LengthPrefixSmallerThanTheFrame_IsRejectedAsInvalidName(
        int claimedBytes, string carried, bool strayByte)
    {
        LoginTestSession online = _login.Entered("alice_01");
        LoginTestSession session = _login.Handshaken();
        byte[] payload = Encoding.Unicode.GetBytes(carried);
        if (strayByte) payload = payload.Concat(new byte[] { 0x61 }).ToArray();
        byte[] frame = LoginFrame(claimedBytes, payload);
        string claimedName = Encoding.Unicode.GetString(payload, 0, claimedBytes);

        Exception? escaped = Record.Exception(() => session.OnRecvPacket(new ArraySegment<byte>(frame)));
        _login.Tick();
        _login.Tick();

        Assert.Null(escaped);
        Assert.Equal(new[] { LoginResult.InvalidName }, session.LoginResults);
        Assert.True(session.IsClosing, "the connection with a short length prefix was not closed");
        Assert.Null(session.Account);
        bool claimedNameOnline = _login.World.Accounts.TryGetOnlineSession(claimedName, out _);
        Assert.False(claimedNameOnline, $"the decoded prefix '{claimedName}' was registered");
        Assert.Equal(1, _login.World.Accounts.OnlineCount);
        bool firstStillOnline = _login.World.Accounts.TryGetOnlineSession("alice_01", out GameSession? owner);
        Assert.True(firstStillOnline);
        Assert.Same(online, owner);
    }

    // D4 (goal "스레드와 순서 계약"): a socket thread submits C_Login while another thread closes the same session.
    // Whichever runs first, after the following ticks no registration for that session is left. Repeated so both
    // orders occur; the order counts are written to the test output, not asserted.
    [Fact]
    public void D4_LoginSubmitRacingADisconnect_LeavesNoRegistrationAfterTheTicks()
    {
        const int rounds = 300;
        List<string> leftover = new();
        int accepted = 0;
        for (int round = 0; round < rounds; round++)
        {
            string name = $"race_{round:D4}";
            LoginTestSession session = _login.Handshaken();
            using Barrier start = new(2);
            Thread submit = new(() =>
            {
                start.SignalAndWait();
                session.Receive(new C_Login { accountName = name });
            });
            Thread close = new(() =>
            {
                start.SignalAndWait();
                session.OnDisconnected(LoginTestWorld.Endpoint);
            });
            submit.Start();
            close.Start();
            submit.Join();
            close.Join();

            _login.Tick();
            _login.Tick();

            if (session.LoginResults.Contains(LoginResult.Accepted)) accepted++;
            if (_login.World.Accounts.TryGetOnlineSession(name, out _)) leftover.Add(name);
        }

        _output.WriteLine($"D4 submit/close: rounds={rounds} accepted-before-close={accepted} " +
            $"not-registered={rounds - accepted} leftover={leftover.Count}");
        Assert.Empty(leftover);
        Assert.Equal(0, _login.World.Accounts.OnlineCount);
    }

    // D4: the login job is already queued; the tick thread runs it while another thread closes the session, so
    // the close can land between the job's closing check and its registration. The ticks that follow must still
    // leave no registration.
    [Fact]
    public void D4_LoginJobOnTheTickRacingADisconnect_LeavesNoRegistrationAfterTheTicks()
    {
        const int rounds = 300;
        List<string> leftover = new();
        int accepted = 0;
        for (int round = 0; round < rounds; round++)
        {
            string name = $"tick_{round:D4}";
            LoginTestSession session = _login.Handshaken();
            session.Receive(new C_Login { accountName = name });
            using Barrier start = new(2);
            Thread tick = new(() =>
            {
                start.SignalAndWait();
                _login.Tick();
            });
            Thread close = new(() =>
            {
                start.SignalAndWait();
                session.OnDisconnected(LoginTestWorld.Endpoint);
            });
            tick.Start();
            close.Start();
            tick.Join();
            close.Join();

            _login.Tick();
            _login.Tick();

            if (session.LoginResults.Contains(LoginResult.Accepted)) accepted++;
            if (_login.World.Accounts.TryGetOnlineSession(name, out _)) leftover.Add(name);
        }

        _output.WriteLine($"D4 tick/close: rounds={rounds} accepted-before-close={accepted} " +
            $"not-registered={rounds - accepted} leftover={leftover.Count}");
        Assert.Empty(leftover);
        Assert.Equal(0, _login.World.Accounts.OnlineCount);
    }

    // D5 (goal 만들 것 6): the account number stays on the server. S_LoginResult is the 4-byte header and one result
    // byte, and none of the packets the session receives through entry and later ticks carries the account GUID in
    // either byte order or as text.
    [Fact]
    public void D5_AccountNumber_IsNeverSentToTheClient()
    {
        RawRecordingSession session = new(_login.World);
        session.OnConnected(LoginTestWorld.Endpoint);
        session.OnRecvPacket(new C_Handshake { clientVersion = ProtocolVersion.Current }.Write());
        session.OnRecvPacket(new C_Login { accountName = "dave_04" }.Write());
        _login.Tick();
        session.OnRecvPacket(new C_CharacterSelect { characterClass = 0 }.Write());
        for (int i = 0; i < 5; i++) _login.Tick();

        AccountIdentity? account = session.Account;
        Assert.True(account.HasValue, "fixture: dave_04 was not logged in");
        Guid accountId = account!.Value.AccountId;
        byte[][] packets = session.Packets;
        byte[][] loginResults = packets.Where(p => Id(p) == PacketID.S_LoginResult).ToArray();
        byte[][] idForms =
        {
            accountId.ToByteArray(),
            accountId.ToByteArray(bigEndian: true),
            Encoding.Unicode.GetBytes(accountId.ToString("D")),
            Encoding.Unicode.GetBytes(accountId.ToString("N")),
            Encoding.UTF8.GetBytes(accountId.ToString("D")),
            Encoding.UTF8.GetBytes(accountId.ToString("N")),
        };

        byte[] onlyResult = Assert.Single(loginResults);
        Assert.Equal(new byte[] { 5, 0, (byte)PacketID.S_LoginResult, 0, 0 }, onlyResult);
        Assert.Contains(packets, p => Id(p) == PacketID.S_EnterMap);
        Assert.Contains(packets, p => Id(p) == PacketID.S_Snapshot);
        List<string> carriers = packets
            .Where(p => idForms.Any(form => new ReadOnlySpan<byte>(p).IndexOf(new ReadOnlySpan<byte>(form)) >= 0))
            .Select(p => Id(p).ToString())
            .ToList();
        Assert.Empty(carriers);
    }

    // Goal 설계 3 and LoginHandler contract: accepted and AlreadyOnline lines name the normalized account, an
    // InvalidName line carries only a length and never the name that was sent, for a rule violation and for a
    // short length prefix alike.
    [Fact]
    public void LoginLog_NamesNormalizedAccounts_AndNeverTheRejectedRawName()
    {
        TextWriter original = Console.Out;
        StringWriter capture = new();
        Console.SetOut(capture);
        try
        {
            _login.Entered("Alice_01");
            LoginTestSession duplicate = _login.Handshaken();
            duplicate.Receive(new C_Login { accountName = "ALICE_01" });
            LoginTestSession invalid = _login.Handshaken();
            invalid.Receive(new C_Login { accountName = "Zq-Secret-Name" });
            LoginTestSession shortPrefix = _login.Handshaken();
            byte[] payload = Encoding.Unicode.GetBytes("qqqSECRET");
            shortPrefix.OnRecvPacket(new ArraySegment<byte>(LoginFrame(6, payload)));
            _login.Tick();
            _login.Tick();
        }
        finally
        {
            Console.SetOut(original);
        }

        string log = capture.ToString();
        _output.WriteLine(log);
        Assert.Contains("[Login] accepted account=alice_01", log);
        Assert.Contains("[Login] rejected account=alice_01 result=AlreadyOnline", log);
        int invalidLines = log.Split('\n').Count(line => line.Contains("result=InvalidName"));
        Assert.Equal(2, invalidLines);
        Assert.DoesNotContain("Secret", log, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("qqq", log);
    }

    static PacketID Id(byte[] packet) => (PacketID)BinaryPrimitives.ReadUInt16LittleEndian(packet.AsSpan(2));

    // Generated packet layout: [size:2][id:2], then a string as [byte length:2][UTF-16 bytes], all LittleEndian.
    static byte[] LoginFrame(int claimedBytes, byte[] payload)
    {
        byte[] frame = new byte[6 + payload.Length];
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(0), (ushort)frame.Length);
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(2), (ushort)PacketID.C_Login);
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(4), (ushort)claimedBytes);
        payload.CopyTo(frame, 6);
        return frame;
    }

    // Keeps every byte the server sends; LoginTestSession exposes only decoded login results.
    sealed class RawRecordingSession : GameSession
    {
        readonly List<byte[]> _packets = new();

        internal RawRecordingSession(GameWorld world)
            : base(world)
        {
        }

        internal byte[][] Packets
        {
            get
            {
                lock (_packets)
                {
                    return _packets.ToArray();
                }
            }
        }

        public override void Send(ArraySegment<byte> packet)
        {
            lock (_packets)
            {
                _packets.Add(packet.ToArray());
            }
        }

        public override void Disconnect() => OnDisconnected(LoginTestWorld.Endpoint);
    }
}

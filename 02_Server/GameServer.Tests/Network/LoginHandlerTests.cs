using System.Buffers.Binary;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Tests.Network;

// Goal T7 (completion condition 6) and the T8 second-login rule (completion condition 7), through the real C_Login
// handler. Bad input ends in InvalidName and a closed connection, never an escaping exception, and never changes the
// online list or another session's account.
[Collection("GameWorldRegistryTests")]
public sealed class LoginHandlerTests : IDisposable
{
    readonly LoginTestWorld _login = new();

    public void Dispose() => _login.Dispose();

    // T7: names outside the rule: empty, 2 and 17 characters, a space, Hangul, a hyphen, a control character.
    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData("abcdefghijklmnopq")]
    [InlineData("ab cd")]
    [InlineData("한글이름")]
    [InlineData("with-hyphen")]
    [InlineData("tab\tname")]
    public void T7_InvalidName_IsRejectedAndClosedWithoutChangingAccounts(string name)
    {
        LoginTestSession online = _login.Entered("alice_01");
        LoginTestSession session = _login.Handshaken();

        Exception? escaped = Record.Exception(() => session.Receive(new C_Login { accountName = name }));
        _login.Tick();

        Assert.Null(escaped);
        Assert.Equal(new[] { LoginResult.InvalidName }, session.LoginResults);
        Assert.True(session.IsClosing, "the connection with an invalid name was not closed");
        AssertAccountsUnchanged(online, session);
    }

    // T7: a raw C_Login whose UInt16 length prefix (bytes of UTF-16) claims more than the 4 payload bytes it carries.
    [Theory]
    [InlineData(6)]      // one character more than the frame carries
    [InlineData(0xFFFF)] // the largest prefix
    public void T7_LengthPrefixBeyondTheFrame_IsRejectedWithoutException(int claimedBytes)
    {
        LoginTestSession online = _login.Entered("alice_01");
        LoginTestSession session = _login.Handshaken();
        byte[] frame = LoginFrame(claimedBytes, payload: new byte[] { (byte)'a', 0, (byte)'b', 0 });

        Exception? escaped = Record.Exception(() => session.OnRecvPacket(new ArraySegment<byte>(frame)));
        _login.Tick();

        Assert.Null(escaped);
        Assert.Equal(new[] { LoginResult.InvalidName }, session.LoginResults);
        Assert.True(session.IsClosing, "the connection with a broken length prefix was not closed");
        AssertAccountsUnchanged(online, session);
    }

    // T8: a second C_Login with another name on the same connection never changes the account, whether the first
    // login is still queued or already accepted.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void T8_SecondLoginOnTheSameConnection_DoesNotChangeTheAccount(bool firstAlreadyAccepted)
    {
        LoginTestSession session = _login.Handshaken();
        session.Receive(new C_Login { accountName = "alice_01" });
        if (firstAlreadyAccepted) _login.Tick();

        session.Receive(new C_Login { accountName = "bob_02" });
        _login.Tick();
        _login.Tick();

        Assert.Equal("alice_01", session.Account?.Name);
        Assert.Equal(new[] { LoginResult.Accepted }, session.LoginResults);
        Assert.False(_login.World.Accounts.TryGetOnlineSession("bob_02", out _));
        bool nameOnline = _login.World.Accounts.TryGetOnlineSession("alice_01", out GameSession? owner);
        Assert.True(nameOnline);
        Assert.Same(session, owner);
        Assert.Equal(1, _login.World.Accounts.OnlineCount);
    }

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

    void AssertAccountsUnchanged(LoginTestSession online, LoginTestSession rejected)
    {
        Assert.Null(rejected.Account);
        Assert.False(rejected.IsLoggedIn);
        Assert.Equal(1, _login.World.Accounts.OnlineCount);
        bool nameOnline = _login.World.Accounts.TryGetOnlineSession("alice_01", out GameSession? owner);
        Assert.True(nameOnline);
        Assert.Same(online, owner);
        Assert.Equal("alice_01", online.Account?.Name);
    }
}

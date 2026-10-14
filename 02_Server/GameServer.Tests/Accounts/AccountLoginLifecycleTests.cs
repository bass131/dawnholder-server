using Dawnholder.Server.GameServer.Accounts;
using Dawnholder.Server.GameServer.Entities;
using Dawnholder.Server.GameServer.Sessions;
using Dawnholder.Server.GameServer.Tests.Network;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Tests.Accounts;

// Goal T3~T6 (completion conditions 1~4 and 11): login decisions and the online list on the real world tick.
// Expected account IDs are the independent uuid5 values recorded in AccountIdResolverTests.
[Collection("GameWorldRegistryTests")]
public sealed class AccountLoginLifecycleTests : IDisposable
{
    static readonly Guid AliceId = Guid.Parse("d0064115-982b-56a6-ae70-528b3cde4a09");
    static readonly Guid BobId = Guid.Parse("1e316721-9f92-58ce-be46-4845b00466d6");
    static readonly TimeSpan SubmitLimit = TimeSpan.FromSeconds(5);

    readonly LoginTestWorld _login = new();

    public void Dispose() => _login.Dispose();

    // T3: alice_01 and Bob_02 each go handshake -> login -> class select. Both get Accepted, both enter the world,
    // and each session holds its own name (lowercased) and number.
    [Fact]
    public void T3_TwoDifferentNames_BothLogInAndEnterWithTheirOwnAccounts()
    {
        LoginTestSession alice = _login.Handshaken();
        LoginTestSession bob = _login.Handshaken();

        alice.Receive(new C_Login { accountName = "alice_01" });
        bob.Receive(new C_Login { accountName = "Bob_02" });
        _login.Tick();
        alice.Receive(new C_CharacterSelect { characterClass = 0 });
        bob.Receive(new C_CharacterSelect { characterClass = 1 });
        _login.Tick();

        Assert.Equal(new[] { LoginResult.Accepted }, alice.LoginResults);
        Assert.Equal(new[] { LoginResult.Accepted }, bob.LoginResults);
        Assert.Equal(new AccountIdentity("alice_01", AliceId), alice.Account);
        Assert.Equal(new AccountIdentity("bob_02", BobId), bob.Account);
        Assert.True(alice.IsLoggedIn);
        Assert.True(bob.IsLoggedIn);
        Assert.NotNull(_login.PlayerOf(alice));
        Assert.NotNull(_login.PlayerOf(bob));
        Assert.Equal(2, _login.World.Accounts.OnlineCount);
    }

    // T4: alice_01 is in the world. A second connection logging in as alice_01 gets AlreadyOnline and is closed.
    // The first session keeps its account, player and online entry, and still gets the ticks that follow.
    [Fact]
    public void T4_LoginWithAnOnlineName_IsRejectedAndTheFirstSessionKeepsPlaying()
    {
        LoginTestSession first = _login.Entered("alice_01");
        PlayerEntity player = _login.PlayerOf(first)!;
        LoginTestSession second = _login.Handshaken();

        second.Receive(new C_Login { accountName = "alice_01" });
        _login.Tick();

        Assert.Equal(new[] { LoginResult.AlreadyOnline }, second.LoginResults);
        Assert.True(second.IsClosing, "the rejected connection was not closed");
        Assert.Null(second.Account);
        Assert.False(second.IsLoggedIn);
        bool nameOnline = _login.World.Accounts.TryGetOnlineSession("alice_01", out GameSession? owner);
        Assert.True(nameOnline);
        Assert.Same(first, owner);
        Assert.Equal(new AccountIdentity("alice_01", AliceId), first.Account);
        Assert.False(first.IsClosing);
        Assert.Same(player, _login.PlayerOf(first));

        first.ClearReceived();
        first.Receive(new C_MoveIntent { input = 0b00000010, clientTick = 77 });
        _login.Tick();
        _login.Tick();

        Assert.True(first.Received(PacketID.S_Snapshot), "the first session stopped receiving snapshots");
        Assert.Equal(77u, player.LastClientTick);
        // The rejected connection's close cleanup has run by now; it must not remove the first session's entry.
        bool stillOnline = _login.World.Accounts.TryGetOnlineSession("alice_01", out GameSession? ownerAfterCleanup);
        Assert.True(stillOnline, "the rejected connection's cleanup removed the online entry");
        Assert.Same(first, ownerAfterCleanup);
    }

    // T5: two connections submit C_Login("carol_03") from two threads before the same tick. One tick later exactly
    // one is Accepted and owns the name; the other gets AlreadyOnline and is closed.
    [Fact]
    public void T5_SameNameSubmittedBeforeOneTick_AcceptsExactlyOne()
    {
        LoginTestSession[] contenders = { _login.Handshaken(), _login.Handshaken() };
        using Barrier start = new(contenders.Length);

        Task[] submits = contenders
            .Select(session => Task.Run(() =>
            {
                start.SignalAndWait(SubmitLimit);
                session.Receive(new C_Login { accountName = "carol_03" });
            }))
            .ToArray();
        Assert.True(Task.WaitAll(submits, SubmitLimit), "fixture: the login submissions did not finish");
        _login.Tick();

        LoginTestSession winner = Assert.Single(contenders, s => s.LoginResults.SequenceEqual(new[] { LoginResult.Accepted }));
        LoginTestSession loser = Assert.Single(contenders, s => s.LoginResults.SequenceEqual(new[] { LoginResult.AlreadyOnline }));
        Assert.True(loser.IsClosing);
        Assert.Null(loser.Account);
        Assert.Equal("carol_03", winner.Account?.Name);
        bool nameOnline = _login.World.Accounts.TryGetOnlineSession("carol_03", out GameSession? owner);
        Assert.True(nameOnline);
        Assert.Same(winner, owner);
        Assert.Equal(1, _login.World.Accounts.OnlineCount);

        // The loser's close cleanup runs in the next tick and must leave the winner's entry alone.
        _login.Tick();
        bool stillOnline = _login.World.Accounts.TryGetOnlineSession("carol_03", out GameSession? ownerAfterCleanup);
        Assert.True(stillOnline, "the rejected connection's cleanup removed the online entry");
        Assert.Same(winner, ownerAfterCleanup);
    }

    // T6: the name is free again once close cleanup runs, however the session ended. A unit fixture cannot make a
    // TCP FIN or reset; both reach GameSession through OnDisconnected. The cases differ in the entry (the session's
    // own Disconnect or the transport callback) and in how far the session got (world, or before a class select).
    [Theory]
    [InlineData(SessionEnd.DisconnectInWorld)]
    [InlineData(SessionEnd.TransportLossInWorld)]
    [InlineData(SessionEnd.DisconnectBeforeCharacterSelect)]
    public void T6_EndedSession_ReleasesTheNameForANewLogin(SessionEnd end)
    {
        LoginTestSession first = end == SessionEnd.DisconnectBeforeCharacterSelect
            ? _login.LoggedIn("dave_04")
            : _login.Entered("dave_04");

        if (end == SessionEnd.TransportLossInWorld) first.OnDisconnected(LoginTestWorld.Endpoint);
        else first.Disconnect();
        _login.Tick();

        bool stillOnline = _login.World.Accounts.TryGetOnlineSession("dave_04", out _);
        Assert.False(stillOnline, "close cleanup left the name online");
        Assert.Null(_login.PlayerOf(first));

        LoginTestSession next = _login.Handshaken();
        next.Receive(new C_Login { accountName = "dave_04" });
        _login.Tick();

        Assert.Equal(new[] { LoginResult.Accepted }, next.LoginResults);
        bool nameOnline = _login.World.Accounts.TryGetOnlineSession("dave_04", out GameSession? owner);
        Assert.True(nameOnline);
        Assert.Same(next, owner);
    }

    // T6: tick order "close cleanup -> login jobs". The close arrives after the new login, yet cleanup runs first
    // in that tick, so the login submitted in the tick of the close is Accepted.
    [Fact]
    public void T6_LoginSubmittedInTheTickOfTheClose_IsAccepted()
    {
        LoginTestSession first = _login.Entered("erin_05");
        LoginTestSession next = _login.Handshaken();

        next.Receive(new C_Login { accountName = "erin_05" });
        first.OnDisconnected(LoginTestWorld.Endpoint);
        _login.Tick();

        Assert.Equal(new[] { LoginResult.Accepted }, next.LoginResults);
        bool nameOnline = _login.World.Accounts.TryGetOnlineSession("erin_05", out GameSession? owner);
        Assert.True(nameOnline);
        Assert.Same(next, owner);
    }

    // T6: a login whose session closes before the tick registers nothing, so the name stays free.
    [Fact]
    public void T6_LoginOfASessionClosedBeforeTheTick_RegistersNothing()
    {
        LoginTestSession closing = _login.Handshaken();

        closing.Receive(new C_Login { accountName = "mallory_06" });
        closing.OnDisconnected(LoginTestWorld.Endpoint);
        _login.Tick();

        Assert.DoesNotContain(LoginResult.Accepted, closing.LoginResults);
        Assert.Null(closing.Account);
        Assert.False(_login.World.Accounts.TryGetOnlineSession("mallory_06", out _));
        Assert.Equal(0, _login.World.Accounts.OnlineCount);

        LoginTestSession next = _login.Handshaken();
        next.Receive(new C_Login { accountName = "mallory_06" });
        _login.Tick();

        Assert.Equal(new[] { LoginResult.Accepted }, next.LoginResults);
    }

    // How the first session ends in T6.
    public enum SessionEnd
    {
        DisconnectInWorld,
        TransportLossInWorld,
        DisconnectBeforeCharacterSelect,
    }
}

using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Tests.Network;

// Goal T8 (completion condition 7): before a successful login a connection cannot pick a class or play.
// These use only today's packets, so they compile before the login packets exist. The single gate the
// design adds sits in CharacterSelectHandler; game input already needs a selected class.
[Collection("GameWorldRegistryTests")]
public sealed class LoginGateTests : IDisposable
{
    readonly LoginTestWorld _login = new();

    public void Dispose() => _login.Dispose();

    // T8: handshake -> C_CharacterSelect(Knight) with no C_Login -> no S_EnterMap, no player for the session.
    [Fact]
    public void T8_CharacterSelectBeforeLogin_DoesNotEnterWorld()
    {
        LoginTestSession session = _login.Handshaken();

        session.Receive(new C_CharacterSelect { characterClass = 0 });
        _login.Tick();
        _login.Tick();

        bool enterMapSent = session.Received(PacketID.S_EnterMap);
        bool playerExists = _login.PlayerOf(session) != null;
        Assert.False(enterMapSent, "S_EnterMap was sent before any login");
        Assert.False(playerExists, "a player entity exists for a connection that never logged in");
        Assert.False(session.IsClosing);
    }

    // T8 (protective, passes before the change): handshake -> C_MoveIntent, C_Attack with no login and no class.
    // The inputs are dropped and nothing enters the world.
    [Fact]
    public void T8_GameInputBeforeLogin_IsDroppedAndDoesNotEnterWorld()
    {
        LoginTestSession session = _login.Handshaken();

        session.Receive(new C_MoveIntent { input = 0b00000010, clientTick = 77 });
        session.Receive(new C_Attack { targetEntityId = 1, attackerClientTick = 1 });
        _login.Tick();

        bool enterMapSent = session.Received(PacketID.S_EnterMap);
        bool playerExists = _login.PlayerOf(session) != null;
        Assert.False(enterMapSent);
        Assert.False(playerExists);
    }
}

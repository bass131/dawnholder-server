using System.Net;
using Dawnholder.Server.GameServer.Entities;
using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Sessions;
using Dawnholder.Server.GameServer.Tests.Maps;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Tests.Network;

// Shared fixture for the multi-account login tests (goal T3~T8): one real GameWorld driven tick by tick
// through LifecycleTestWorld, and sessions that only record what the server sends. Nothing on the
// handshake, login, character select or close path is replaced.
internal sealed class LoginTestWorld : IDisposable
{
    internal static readonly IPEndPoint Endpoint = new(IPAddress.Loopback, 0);

    long _tick;

    internal LoginTestWorld() => World = new GameWorld(new Dictionary<MapId, (MapTerrain?, MapContent?)>());

    internal GameWorld World { get; }

    internal IEnumerable<PlayerEntity> Players => World.AllLiveMaps.SelectMany(map => map.Players);

    public void Dispose() => World.Stop();

    internal void Tick() => LifecycleTestWorld.Tick(World, ++_tick);

    // A connection that passed the version handshake and nothing else.
    internal LoginTestSession Handshaken()
    {
        LoginTestSession session = new(World);
        session.OnConnected(Endpoint);
        session.Receive(new C_Handshake { clientVersion = ProtocolVersion.Current });
        return session;
    }

    // Handshake and an Accepted login, not yet in the world.
    internal LoginTestSession LoggedIn(string accountName)
    {
        LoginTestSession session = Handshaken();
        session.Receive(new C_Login { accountName = accountName });
        Tick();
        Assert.True(session.LoginResults.SequenceEqual(new[] { LoginResult.Accepted }), $"fixture: {accountName} was not accepted");
        return session;
    }

    // Handshake, an Accepted login and a Knight select, with the player in Town.
    internal LoginTestSession Entered(string accountName)
    {
        LoginTestSession session = LoggedIn(accountName);
        session.Receive(new C_CharacterSelect { characterClass = 0 });
        Tick();
        Assert.True(PlayerOf(session) != null, $"fixture: {accountName} did not enter the world");
        return session;
    }

    internal PlayerEntity? PlayerOf(LoginTestSession session)
        => Players.FirstOrDefault(player => ReferenceEquals(player.Owner, session));
}

internal sealed class LoginTestSession : GameSession
{
    readonly List<byte[]> _packets = new();

    internal LoginTestSession(GameWorld world)
        : base(world)
    {
    }

    internal int DisconnectCalls { get; private set; }

    // Every S_LoginResult this connection received, in order.
    internal LoginResult[] LoginResults
    {
        get
        {
            lock (_packets)
            {
                return _packets
                    .Where(packet => (PacketID)BitConverter.ToUInt16(packet, 2) == PacketID.S_LoginResult)
                    .Select(packet =>
                    {
                        S_LoginResult result = new();
                        result.Read(new ArraySegment<byte>(packet));
                        return (LoginResult)result.result;
                    })
                    .ToArray();
            }
        }
    }

    public override void Send(ArraySegment<byte> packet)
    {
        // Results may be sent from the tick while another test thread submits packets (T5).
        lock (_packets)
        {
            _packets.Add(packet.ToArray());
        }
    }

    // Normal and abrupt transport closes both reach the session through OnDisconnected.
    public override void Disconnect()
    {
        DisconnectCalls++;
        OnDisconnected(LoginTestWorld.Endpoint);
    }

    internal void Receive(IPacket packet) => OnRecvPacket(packet.Write());

    internal bool Received(PacketID id)
    {
        lock (_packets)
        {
            return _packets.Any(packet => (PacketID)BitConverter.ToUInt16(packet, 2) == id);
        }
    }

    internal void ClearReceived()
    {
        lock (_packets)
        {
            _packets.Clear();
        }
    }
}

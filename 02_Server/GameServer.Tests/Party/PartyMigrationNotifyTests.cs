using System.Net;
using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Maps.Transitions;
using Dawnholder.Server.GameServer.Party;
using Dawnholder.Server.GameServer.Sessions;
using Dawnholder.Server.GameServer.Tests.Maps;
using Shared.GameData;
using Shared.Protocol;
using Xunit.Abstractions;

namespace GameServer.Tests.Party;

// A client that already saw a party must see partyId=0 after a migration-gap disband.
// World ticks, map departure/arrival and PartyFlow.Leave are the real product paths.
[Collection("ConsoleSerial")]
public sealed class PartyMigrationNotifyTests(ITestOutputHelper output)
{
    [Fact]
    public void DisbandDuringMapGap_ArrivingMemberReceivesEmptyPartyUpdate()
    {
        using var fixture = new PartyFixture();
        CapturedSession moving = fixture.First;
        CapturedSession leaver = fixture.Second;
        GameMap town = fixture.World.Map;
        Portal portal = town.Portals.First(candidate => candidate.Dest == MapId.HuntingGround);
        town.GetPlayer(moving.EntityId)!.Position = portal.Position;
        town.EnqueueJob(() => MapMigration.Execute(moving, moving.EntityId, town, portal.PortalId, _ => 0));
        PartyFlow.Leave(fixture.World, leaver, leaver.EntityId);

        // The new instance was absent from this tick's map snapshot. Party.Tick
        // runs after departure and before that instance's first arrival tick.
        fixture.Tick();
        Assert.Equal(1, moving.ReadMigrating());
        Assert.DoesNotContain(fixture.World.AllLiveMaps, map => map.GetPlayer(moving.EntityId) != null);
        Assert.Null(fixture.World.Party.GetPartyByEntity(moving.EntityId));

        for (int index = 0; index < 5; index++) fixture.Tick();
        bool arrived = fixture.World.AllLiveMaps.Any(map => ReferenceEquals(map.GetPlayer(moving.EntityId)?.Owner, moving));
        bool leaverNotified = leaver.PartyIds.Contains(0);
        int[] movingPartyIds = moving.PartyIds;
        output.WriteLine($"arrived={arrived}, migrating={moving.ReadMigrating()}, leaverNotified={leaverNotified}, "
            + $"arrivingPartyIds=[{string.Join(",", movingPartyIds)}], receivedIds=[{string.Join(",", moving.PacketIds)}]");

        Assert.True(arrived);
        Assert.Equal(0, moving.ReadMigrating());
        Assert.True(leaverNotified);
        Assert.Contains(0, movingPartyIds);
    }

    [Fact]
    public void DisbandWhileBothMembersPresent_NotifiesBothMembers()
    {
        using var fixture = new PartyFixture();
        PartyFlow.Leave(fixture.World, fixture.Second, fixture.Second.EntityId);
        fixture.Tick();
        fixture.Tick();

        Assert.Null(fixture.World.Party.GetPartyByEntity(fixture.First.EntityId));
        Assert.Contains(0, fixture.First.PartyIds);
        Assert.Contains(0, fixture.Second.PartyIds);
    }

    private sealed class CapturedSession(GameWorld world) : GameSession(world)
    {
        private readonly List<byte[]> _packets = new();
        internal ushort[] PacketIds => _packets.Select(packet => BitConverter.ToUInt16(packet, 2)).ToArray();
        internal int[] PartyIds => _packets
            .Where(packet => BitConverter.ToUInt16(packet, 2) == (ushort)PacketID.S_PartyUpdate)
            .Select(packet => BitConverter.ToInt32(packet, 4)).ToArray();
        public override void Send(ArraySegment<byte> packet) => _packets.Add(packet.ToArray());
        public override void Disconnect() => OnDisconnected(new IPEndPoint(IPAddress.Loopback, 0));
        internal void Clear() => _packets.Clear();
    }

    private sealed class PartyFixture : IDisposable
    {
        private long _tick;

        internal PartyFixture()
        {
            World = new GameWorld(new Dictionary<MapId, (MapTerrain?, MapContent?)>());
            try
            {
                First = Join();
                Second = Join();
                Tick();
                Assert.True(World.IsActiveSession(First, First.EntityId));
                Assert.True(World.IsActiveSession(Second, Second.EntityId));
                PartyFlow.Invite(World, First, First.EntityId, Second.EntityId);
                Tick();
                Tick();
                PartyFlow.Respond(World, Second, Second.EntityId, First.EntityId, accepted: true);
                Tick();
                Tick();
                PartyState? party = World.Party.GetPartyByEntity(First.EntityId);
                Assert.NotNull(party);
                Assert.True(party.PartyId > 0);
                Assert.Contains(party.PartyId, First.PartyIds);
                Assert.Contains(party.PartyId, Second.PartyIds);
                First.Clear();
                Second.Clear();
            }
            catch
            {
                World.Stop();
                throw;
            }
        }

        internal GameWorld World { get; }
        internal CapturedSession First { get; }
        internal CapturedSession Second { get; }
        internal void Tick() => LifecycleTestWorld.Tick(World, ++_tick);
        public void Dispose() => World.Stop();

        private CapturedSession Join()
        {
            var session = new CapturedSession(World);
            session.OnRecvPacket(new C_Handshake { clientVersion = ProtocolVersion.Current }.Write());
            LifecycleTestWorld.LogIn(World, session);
            session.OnRecvPacket(new C_CharacterSelect { characterClass = 0 }.Write());
            return session;
        }
    }
}

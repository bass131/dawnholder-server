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

[Collection("ConsoleSerial")]
public sealed class PartyArrivalStateTests(ITestOutputHelper output)
{
    [Fact]
    public void ArrivalWithParty_SendsOneCurrentStateOnlyToArrivalAfterTransitionAndHp()
    {
        using var fixture = new ArrivalFixture(formParty: true);
        int partyId = fixture.World.Party.GetPartyByEntity(fixture.First.EntityId)!.PartyId;
        fixture.Depart();
        fixture.Tick();

        Assert.Equal(0, fixture.First.ReadMigrating());
        Assert.Contains(PacketID.S_MapTransition, fixture.First.PacketIds);
        Assert.Empty(fixture.First.Updates);
        fixture.Drain();

        S_PartyUpdate update = Assert.Single(fixture.First.Updates);
        AssertParty(update, partyId, fixture.First.EntityId, fixture.Second.EntityId, 0, 1);
        Assert.Empty(fixture.Second.Updates);
        Assert.Empty(fixture.Third.Updates);
        PacketID[] packets = fixture.First.PacketIds;
        int transition = Array.IndexOf(packets, PacketID.S_MapTransition);
        int hp = Array.IndexOf(packets, PacketID.S_PlayerHp);
        int state = Array.IndexOf(packets, PacketID.S_PartyUpdate);
        Assert.True(transition >= 0 && hp > transition && state > hp,
            $"Expected transition < HP < party state, got {string.Join(",", packets)}");
    }

    [Fact]
    public void ArrivalWithoutParty_SendsOneEmptyStateOnlyToArrival()
    {
        using var fixture = new ArrivalFixture(formParty: false);
        fixture.Depart();
        fixture.Drain();

        S_PartyUpdate update = Assert.Single(fixture.First.Updates);
        AssertParty(update, 0, 0, 0, 0, 0);
        Assert.Empty(fixture.Second.Updates);
        Assert.Empty(fixture.Third.Updates);
        int transition = Array.IndexOf(fixture.First.PacketIds, PacketID.S_MapTransition);
        int state = Array.IndexOf(fixture.First.PacketIds, PacketID.S_PartyUpdate);
        Assert.True(transition >= 0 && state > transition);
    }

    [Fact]
    public void DisbandInGap_ReformBeforeArrivalQuery_UsesNewPartyAndPreservesFormationBroadcast()
    {
        using var fixture = new ArrivalFixture(formParty: true);
        int oldParty = fixture.World.Party.GetPartyByEntity(fixture.First.EntityId)!.PartyId;
        PartyFlow.Leave(fixture.World, fixture.Second, fixture.Second.EntityId);
        fixture.Depart();
        Assert.Null(fixture.World.Party.GetPartyByEntity(fixture.First.EntityId));

        // Requests wait in the party actor until maps have admitted the arrival.
        PartyFlow.Invite(fixture.World, fixture.Third, fixture.Third.EntityId, fixture.First.EntityId);
        PartyFlow.Respond(fixture.World, fixture.First, fixture.First.EntityId, fixture.Third.EntityId, true);
        fixture.Drain();

        int newParty = fixture.World.Party.GetPartyByEntity(fixture.First.EntityId)!.PartyId;
        Assert.NotEqual(oldParty, newParty);
        S_PartyUpdate[] arrivingUpdates = fixture.First.Updates;
        Assert.Equal(2, arrivingUpdates.Length); // Formation broadcast plus one arrival snapshot.
        Assert.All(arrivingUpdates, update =>
            AssertParty(update, newParty, fixture.Third.EntityId, fixture.First.EntityId, 0, 0));
        Assert.Equal(0, Assert.Single(fixture.Second.Updates).partyId);
        Assert.Equal(newParty, Assert.Single(fixture.Third.Updates).partyId);
    }

    [Fact]
    public void DisbandQueuedAfterArrivalQuery_FinalPacketIsEmptyForBothMembers()
    {
        using var fixture = new ArrivalFixture(formParty: true);
        int oldParty = fixture.World.Party.GetPartyByEntity(fixture.First.EntityId)!.PartyId;
        fixture.First.OnPacket = packet =>
        {
            if (packet == PacketID.S_MapTransition)
            {
                // This actor job appends Leave behind the arrival's already queued query.
                fixture.World.Party.EnqueueJob(() =>
                    PartyFlow.Leave(fixture.World, fixture.Second, fixture.Second.EntityId));
            }
        };
        fixture.Depart();
        fixture.Drain();

        Assert.Equal(new[] { oldParty, 0 }, fixture.First.Updates.Select(packet => packet.partyId));
        Assert.Equal(0, Assert.Single(fixture.Second.Updates).partyId);
        Assert.Empty(fixture.Third.Updates);
        Assert.Null(fixture.World.Party.GetPartyByEntity(fixture.First.EntityId));
    }

    [Fact]
    public void DisconnectInGap_DoesNotArriveOrReceiveCurrentState()
    {
        using var fixture = new ArrivalFixture(formParty: true);
        int entityId = fixture.First.EntityId;
        fixture.Depart();
        fixture.First.Disconnect();
        fixture.Drain();

        Assert.True(fixture.First.IsClosing);
        Assert.False(fixture.World.IsActiveSession(fixture.First, entityId));
        Assert.DoesNotContain(PacketID.S_MapTransition, fixture.First.PacketIds);
        Assert.Empty(fixture.First.Updates);
        Assert.Equal(0, Assert.Single(fixture.Second.Updates).partyId);
    }

    [Fact]
    public void DisconnectAtTransition_ActorRechecksSessionBeforeSendingState()
    {
        using var fixture = new ArrivalFixture(formParty: true);
        fixture.First.OnPacket = packet =>
        {
            if (packet == PacketID.S_MapTransition) fixture.First.Disconnect();
        };
        fixture.Depart();
        fixture.Drain();

        Assert.Contains(PacketID.S_MapTransition, fixture.First.PacketIds);
        Assert.True(fixture.First.IsClosing);
        Assert.Empty(fixture.First.Updates);
    }

    [Fact]
    public void CurrentStateQuery_WithAnotherSessionsEntity_IsIgnored()
    {
        using var fixture = new ArrivalFixture(formParty: true);
        PartyFlow.SendCurrentState(fixture.World, fixture.Second, fixture.First.EntityId);
        fixture.Drain();

        Assert.Empty(fixture.First.Updates);
        Assert.Empty(fixture.Second.Updates);
        Assert.Empty(fixture.Third.Updates);
    }

    [Fact]
    public void WorldOwnership_SecondLiveWorldIsRejected()
    {
        using var fixture = new ArrivalFixture(formParty: false);
        Assert.Same(fixture.World, GameWorld.Instance);
        Assert.Throws<InvalidOperationException>(() =>
            new GameWorld(new Dictionary<MapId, (MapTerrain?, MapContent?)>()));
        Assert.Same(fixture.World, GameWorld.Instance);
    }

    [Fact]
    public void ArrivalWithPendingInvite_DoesNotFollowInviteWithPopupClosingEmptyState()
    {
        using var fixture = new ArrivalFixture(formParty: false);
        fixture.Depart();
        PartyFlow.Invite(fixture.World, fixture.Second, fixture.Second.EntityId, fixture.First.EntityId);
        fixture.Drain();

        bool pending = fixture.World.Party.TryGetPendingInvite(fixture.First.EntityId, out int inviter);
        Assert.True(pending);
        Assert.Equal(fixture.Second.EntityId, inviter);
        PacketID[] packets = fixture.First.PacketIds;
        int transition = Array.IndexOf(packets, PacketID.S_MapTransition);
        int invite = Array.IndexOf(packets, PacketID.S_PartyInviteRecv);
        output.WriteLine($"pending={pending}, transitionIndex={transition}, inviteIndex={invite}, packets={string.Join(",", packets)}");
        Assert.True(transition >= 0 && invite > transition);
        Assert.Single(packets, id => id == PacketID.S_PartyInviteRecv);
        // The approved exception suppresses the empty resync while the invitation is actionable.
        Assert.Empty(fixture.First.Updates);
        Assert.Empty(fixture.Second.Updates);
        Assert.Empty(fixture.Third.Updates);
    }

    [Fact]
    public void InviteQueuedAfterArrivalQuery_EmptyStatePrecedesInviteAndAcceptanceStillWorks()
    {
        using var fixture = new ArrivalFixture(formParty: false);
        fixture.First.OnPacket = packet =>
        {
            if (packet == PacketID.S_MapTransition)
            {
                // The wrapper runs first, but appends Invite behind the arrival query.
                fixture.World.Party.EnqueueJob(() =>
                    PartyFlow.Invite(fixture.World, fixture.Second, fixture.Second.EntityId, fixture.First.EntityId));
            }
        };
        fixture.Depart();
        fixture.Drain();

        AssertParty(Assert.Single(fixture.First.Updates), 0, 0, 0, 0, 0);
        Assert.Empty(fixture.Second.Updates);
        Assert.Empty(fixture.Third.Updates);
        PacketID[] packets = fixture.First.PacketIds;
        int transition = Array.IndexOf(packets, PacketID.S_MapTransition);
        int state = Array.IndexOf(packets, PacketID.S_PartyUpdate);
        int invite = Array.IndexOf(packets, PacketID.S_PartyInviteRecv);
        Assert.True(transition >= 0 && state > transition && invite > state,
            $"Expected transition < empty state < invite, got {string.Join(",", packets)}");
        Assert.Single(packets, id => id == PacketID.S_PartyInviteRecv);
        Assert.True(fixture.World.Party.TryGetPendingInvite(fixture.First.EntityId, out int inviter));
        Assert.Equal(fixture.Second.EntityId, inviter);

        fixture.First.Clear();
        PartyFlow.Respond(fixture.World, fixture.First, fixture.First.EntityId, inviter, true);
        fixture.Drain();

        PartyState? party = fixture.World.Party.GetPartyByEntity(fixture.First.EntityId);
        Assert.NotNull(party);
        AssertParty(Assert.Single(fixture.First.Updates), party.PartyId, inviter, fixture.First.EntityId, 1, 0);
        AssertParty(Assert.Single(fixture.Second.Updates), party.PartyId, inviter, fixture.First.EntityId, 1, 0);
        Assert.Empty(fixture.Third.Updates);
        Assert.False(fixture.World.Party.TryGetPendingInvite(fixture.First.EntityId, out _));
    }

    [Fact]
    public void ArrivalWithInviteReceivedBeforeDeparture_SuppressesEmptyStateAndAllowsAcceptance()
    {
        using var fixture = new ArrivalFixture(formParty: false);
        PartyFlow.Invite(fixture.World, fixture.Second, fixture.Second.EntityId, fixture.First.EntityId);
        fixture.Drain();
        Assert.Contains(PacketID.S_PartyInviteRecv, fixture.First.PacketIds);
        Assert.True(fixture.World.Party.TryGetPendingInvite(fixture.First.EntityId, out int inviter));
        Assert.Equal(fixture.Second.EntityId, inviter);
        fixture.First.Clear();

        fixture.Depart();
        fixture.Drain();

        Assert.Contains(PacketID.S_MapTransition, fixture.First.PacketIds);
        Assert.Empty(fixture.First.Updates);
        Assert.Empty(fixture.Second.Updates);
        Assert.Empty(fixture.Third.Updates);
        Assert.True(fixture.World.Party.TryGetPendingInvite(fixture.First.EntityId, out _));
        PartyFlow.Respond(fixture.World, fixture.First, fixture.First.EntityId, inviter, true);
        fixture.Drain();

        PartyState? party = fixture.World.Party.GetPartyByEntity(fixture.First.EntityId);
        Assert.NotNull(party);
        AssertParty(Assert.Single(fixture.First.Updates), party.PartyId, inviter, fixture.First.EntityId, 1, 0);
        AssertParty(Assert.Single(fixture.Second.Updates), party.PartyId, inviter, fixture.First.EntityId, 1, 0);
        Assert.Empty(fixture.Third.Updates);
        Assert.False(fixture.World.Party.TryGetPendingInvite(fixture.First.EntityId, out _));
    }

    [Fact]
    public void ArrivalAfterInviteExpiry_SendsOneEmptyState()
    {
        using var fixture = new ArrivalFixture(formParty: false);
        PartyFlow.Invite(fixture.World, fixture.Second, fixture.Second.EntityId, fixture.First.EntityId);
        fixture.Drain();
        Assert.True(fixture.World.Party.TryGetPendingInvite(fixture.First.EntityId, out _));

        // Drive the real actor expiry; do not remove the invitation directly in the fixture.
        for (int index = 0; index < PartyRegistry.InviteTimeoutTicks; index++) fixture.Tick();
        Assert.False(fixture.World.Party.TryGetPendingInvite(fixture.First.EntityId, out _));
        fixture.First.Clear();
        fixture.Second.Clear();
        fixture.Third.Clear();
        fixture.Depart();
        fixture.Drain();

        AssertParty(Assert.Single(fixture.First.Updates), 0, 0, 0, 0, 0);
        Assert.Empty(fixture.Second.Updates);
        Assert.Empty(fixture.Third.Updates);
        PacketID[] packets = fixture.First.PacketIds;
        int transition = Array.IndexOf(packets, PacketID.S_MapTransition);
        int state = Array.IndexOf(packets, PacketID.S_PartyUpdate);
        Assert.True(transition >= 0 && state > transition);
    }

    [Fact]
    public void DisbandInGap_PendingNewInvite_SuppressesEmptyStateUntilAcceptance()
    {
        using var fixture = new ArrivalFixture(formParty: true);
        int oldParty = fixture.World.Party.GetPartyByEntity(fixture.First.EntityId)!.PartyId;
        PartyFlow.Leave(fixture.World, fixture.Second, fixture.Second.EntityId);
        fixture.Depart();
        Assert.Null(fixture.World.Party.GetPartyByEntity(fixture.First.EntityId));
        PartyFlow.Invite(fixture.World, fixture.Third, fixture.Third.EntityId, fixture.First.EntityId);
        fixture.Drain();

        Assert.Contains(PacketID.S_PartyInviteRecv, fixture.First.PacketIds);
        Assert.True(fixture.World.Party.TryGetPendingInvite(fixture.First.EntityId, out int inviter));
        Assert.Equal(fixture.Third.EntityId, inviter);
        Assert.Empty(fixture.First.Updates);
        Assert.Empty(fixture.Third.Updates);
        Assert.Equal(0, Assert.Single(fixture.Second.Updates).partyId);
        PartyFlow.Respond(fixture.World, fixture.First, fixture.First.EntityId, inviter, true);
        fixture.Drain();

        PartyState? party = fixture.World.Party.GetPartyByEntity(fixture.First.EntityId);
        Assert.NotNull(party);
        Assert.NotEqual(oldParty, party.PartyId);
        AssertParty(Assert.Single(fixture.First.Updates), party.PartyId, inviter, fixture.First.EntityId, 0, 0);
        AssertParty(Assert.Single(fixture.Third.Updates), party.PartyId, inviter, fixture.First.EntityId, 0, 0);
        Assert.Equal(0, Assert.Single(fixture.Second.Updates).partyId);
        Assert.False(fixture.World.Party.TryGetPendingInvite(fixture.First.EntityId, out _));
    }

    private static void AssertParty(S_PartyUpdate update, int partyId, int leader, int member,
        byte leaderClass, byte memberClass)
    {
        Assert.Equal(partyId, update.partyId);
        Assert.Equal(leader, update.leaderEntityId);
        Assert.Equal(leader, update.member0EntityId);
        Assert.Equal(member, update.member1EntityId);
        Assert.Equal(leaderClass, update.member0Class);
        Assert.Equal(memberClass, update.member1Class);
    }

    private sealed class CapturedSession(GameWorld world) : GameSession(world)
    {
        private readonly List<byte[]> _packets = new();
        internal Action<PacketID>? OnPacket { get; set; }
        internal PacketID[] PacketIds => _packets.Select(Id).ToArray();
        internal S_PartyUpdate[] Updates => _packets
            .Where(packet => Id(packet) == PacketID.S_PartyUpdate)
            .Select(packet =>
            {
                var update = new S_PartyUpdate();
                update.Read(new ArraySegment<byte>(packet));
                return update;
            }).ToArray();

        public override void Send(ArraySegment<byte> packet)
        {
            byte[] bytes = packet.ToArray();
            _packets.Add(bytes);
            OnPacket?.Invoke(Id(bytes));
        }

        public override void Disconnect() => OnDisconnected(new IPEndPoint(IPAddress.Loopback, 0));
        internal void Clear() => _packets.Clear();
        private static PacketID Id(byte[] bytes) => (PacketID)BitConverter.ToUInt16(bytes, 2);
    }

    private sealed class ArrivalFixture : IDisposable
    {
        private long _tick;

        internal ArrivalFixture(bool formParty)
        {
            World = new GameWorld(new Dictionary<MapId, (MapTerrain?, MapContent?)>());
            try
            {
                First = Join(0);
                Second = Join(1);
                Third = Join(0);
                Tick();
                Assert.True(World.IsActiveSession(First, First.EntityId));
                Assert.True(World.IsActiveSession(Second, Second.EntityId));
                Assert.True(World.IsActiveSession(Third, Third.EntityId));
                if (formParty)
                {
                    PartyFlow.Invite(World, First, First.EntityId, Second.EntityId);
                    Tick();
                    PartyFlow.Respond(World, Second, Second.EntityId, First.EntityId, true);
                    Drain();
                    Assert.NotNull(World.Party.GetPartyByEntity(First.EntityId));
                }
                First.Clear();
                Second.Clear();
                Third.Clear();
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
        internal CapturedSession Third { get; }
        public void Dispose() => World.Stop();
        internal void Tick() => LifecycleTestWorld.Tick(World, ++_tick);

        internal void Drain()
        {
            for (int index = 0; index < 4; index++) Tick();
        }

        internal void Depart()
        {
            GameMap town = World.Map;
            Portal portal = town.Portals.First(candidate => candidate.Dest == MapId.HuntingGround);
            town.GetPlayer(First.EntityId)!.Position = portal.Position;
            town.EnqueueJob(() => MapMigration.Execute(First, First.EntityId, town, portal.PortalId, _ => 0));
            Tick();
            Assert.Equal(1, First.ReadMigrating());
            Assert.DoesNotContain(World.AllLiveMaps, map => map.GetPlayer(First.EntityId) != null);
        }

        private CapturedSession Join(byte characterClass)
        {
            var session = new CapturedSession(World);
            session.OnRecvPacket(new C_Handshake { clientVersion = ProtocolVersion.Current }.Write());
            LifecycleTestWorld.LogIn(World, session);
            session.OnRecvPacket(new C_CharacterSelect { characterClass = characterClass }.Write());
            return session;
        }
    }
}

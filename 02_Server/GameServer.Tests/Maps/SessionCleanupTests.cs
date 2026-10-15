using System.Collections;
using System.Net;
using System.Numerics;
using System.Reflection;
using Dawnholder.Server.GameServer.Entities;
using Dawnholder.Server.GameServer.Guild;
using Dawnholder.Server.GameServer.Items;
using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Tests.Maps;

// Drives the real world tick without starting a scheduler. No cleanup implementation is mocked.
internal static class LifecycleTestWorld
{
    // Prepare only the new account phase so the caller's map-tick timing remains under test.
    internal static void LogIn(GameWorld world, GameSession session)
    {
        session.OnRecvPacket(new C_Login { accountName = $"test_{Guid.NewGuid():N}"[..16] }.Write());
        world.Accounts.Tick();
        Assert.True(session.IsLoggedIn, "fixture: account login must precede character selection");
    }

    internal static void Tick(GameWorld world, long tick)
        => typeof(GameWorld).GetMethod("OnTick", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(world, new object[] { tick });
}

[Collection("GameWorldRegistryTests")]
public sealed class SessionCleanupTests : IDisposable
{
    readonly GameWorld _world = new(new Dictionary<MapId, (MapTerrain?, MapContent?)>());
    static readonly IPEndPoint Endpoint = new(IPAddress.Loopback, 0);

    sealed class CapturedSession : GameSession
    {
        internal readonly List<byte[]> Packets = new();
        internal Action<PacketID>? OnPacket;
        internal Action? OnMapRead;
        internal CapturedSession(GameWorld world) : base(world) { }
        public override void Send(ArraySegment<byte> packet)
        {
            byte[] bytes = packet.ToArray();
            Packets.Add(bytes);
            OnPacket?.Invoke(Id(bytes));
        }
        public override void Disconnect() => OnDisconnected(Endpoint);
        protected override GameMap? GetMap()
        {
            GameMap? map = base.GetMap();
            OnMapRead?.Invoke();
            return map;
        }
    }

    static PacketID Id(byte[] bytes) => (PacketID)BitConverter.ToUInt16(bytes, 2);
    void Tick(long tick) => LifecycleTestWorld.Tick(_world, tick);
    IEnumerable<PlayerEntity> Players => _world.AllLiveMaps.SelectMany(map => map.Players);
    CapturedSession Join()
    {
        CapturedSession session = new(_world);
        session.OnConnected(Endpoint);
        session.OnRecvPacket(new C_Handshake { clientVersion = ProtocolVersion.Current }.Write());
        LifecycleTestWorld.LogIn(_world, session);
        session.OnRecvPacket(new C_CharacterSelect { characterClass = 0 }.Write());
        return session;
    }
    public void Dispose() => _world.Stop();

    [Fact]
    public void CloseBeforeEntry_BarrierRemovesSession_AndLatePacketsCannotReviveIt()
    {
        CapturedSession session = Join();
        session.OnDisconnected(Endpoint);
        Task barrier = _world.FlushSessionClosuresAsync();
        Assert.False(barrier.IsCompleted);
        Tick(1);
        Assert.True(barrier.IsCompletedSuccessfully);
        Assert.Empty(Players);
        Assert.Equal(-1, session.EntityId);
        session.OnRecvPacket(new C_CharacterSelect { characterClass = 0 }.Write());
        Tick(2);
        Assert.Empty(Players);
        Assert.DoesNotContain(session.Packets, p => Id(p) == PacketID.S_EnterMap);
    }

    [Theory]
    [InlineData(0)] // close before departure
    [InlineData(1)] // source removed, destination job pending
    [InlineData(2)] // destination registered; close from first transition publication
    [InlineData(3)] // destination complete
    public void CloseAtTransferBoundary_LeavesNoOwnerInAnyMap(int boundary)
    {
        CapturedSession session = Join();
        Tick(1);
        PlayerEntity player = Assert.Single(Players);
        int entityId = player.EntityId;
        player.Position = new Vector2(20, 0);
        session.OnRecvPacket(new C_EnterPortal { portalId = 1 }.Write());
        if (boundary == 2)
            session.OnPacket = id => { if (id == PacketID.S_MapTransition) session.OnDisconnected(Endpoint); };
        if (boundary >= 1) _world.Map.Tick(2);
        Assert.InRange(Players.Count(p => ReferenceEquals(p.Owner, session)), 0, 1);
        if (boundary >= 2)
        {
            Assert.True(_world.TryGetInstance(MapId.HuntingGround, InstanceKey.ForSolo(entityId), out GameMap? hunting));
            hunting!.Tick(2);
        }
        if (boundary != 2) session.OnDisconnected(Endpoint);
        Task barrier = _world.FlushSessionClosuresAsync();
        Tick(3);
        Assert.True(barrier.IsCompletedSuccessfully);
        Assert.DoesNotContain(Players, p => ReferenceEquals(p.Owner, session));
        Assert.Equal(-1, session.EntityId);
        Tick(4);
        Assert.DoesNotContain(Players, p => p.EntityId == entityId);
    }

    [Fact]
    public void CleanupIgnoresTransientRoutingHint_WhenDestinationAlreadyOwnsPlayer()
    {
        CapturedSession session = Join();
        Tick(1);
        PlayerEntity player = Assert.Single(Players);
        player.Position = new Vector2(20, 0);
        session.OnRecvPacket(new C_EnterPortal { portalId = 1 }.Write());
        Tick(2);
        Tick(3);
        Assert.True(_world.TryGetInstance(MapId.HuntingGround, InstanceKey.ForSolo(session.EntityId), out GameMap? hunting));
        Assert.Same(session, Assert.Single(hunting!.Players).Owner);
        // Contract probe: a lagging routing hint must never select the cleanup owner.
        // This is a controlled state fixture, not a claim to reproduce a thread interleaving.
        session.SetMigrating(1);
        session.OnDisconnected(Endpoint);
        Tick(4);
        Assert.Empty(Players);
        Assert.Equal(-1, session.EntityId);
    }

    [Fact]
    public void DuplicateClose_NotifiesObserverOnce_AndPreservesOtherPlayer()
    {
        CapturedSession closing = Join();
        CapturedSession observer = Join();
        Tick(1);
        int leavingId = closing.EntityId;
        observer.Packets.Clear();
        closing.OnDisconnected(Endpoint);
        closing.OnDisconnected(Endpoint);
        Tick(2);
        Tick(3);
        Assert.Same(observer, Assert.Single(Players).Owner);
        byte[] packet = Assert.Single(observer.Packets, p => Id(p) == PacketID.S_PlayerLeave);
        S_PlayerLeave leave = new();
        leave.Read(new ArraySegment<byte>(packet));
        Assert.Equal(leavingId, leave.entityId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void QueuedPartyRequest_CannotCreateStateAfterRequesterCloses(bool closeResponder)
    {
        CapturedSession inviter = Join();
        CapturedSession responder = Join();
        Tick(1);
        int inviterId = inviter.EntityId;
        int responderId = responder.EntityId;
        inviter.OnRecvPacket(new C_PartyInvite { targetEntityId = responderId }.Write());
        if (closeResponder)
        {
            Tick(2);
            responder.OnRecvPacket(new C_PartyRespond { inviterEntityId = inviterId, accept = 1 }.Write());
            responder.OnDisconnected(Endpoint);
        }
        else inviter.OnDisconnected(Endpoint);
        Tick(3);
        Tick(4);
        Assert.Null(_world.Party.GetPartyByEntity(inviterId));
        Assert.Null(_world.Party.GetPartyByEntity(responderId));
        Assert.False(_world.Party.TryGetPendingInvite(responderId, out _));
    }

    [Fact]
    public void QueuedMove_IsRejectedEvenBeforeClosingPlayerIsRemoved()
    {
        CapturedSession session = Join();
        Tick(1);
        PlayerEntity player = Assert.Single(Players);
        uint before = player.LastClientTick;
        session.SubmitMoveIntent(1, false, true, 1, 123);
        session.OnDisconnected(Endpoint);
        // Deliberately drain the map before world cleanup to exercise execution-time closing checks.
        _world.Map.Tick(2);
        Assert.Same(player, Assert.Single(Players));
        Assert.Equal(before, player.LastClientTick);
        Assert.Equal(0, player.InputQueueCount);
        Tick(3);
        Assert.Empty(Players);
    }

    [Theory]
    [InlineData(false)] // positive control: identical attack is otherwise valid
    [InlineData(true)]
    public void QueuedAttack_OnlyLiveOwnerCanDamageTarget(bool closeBeforeExecution)
    {
        CapturedSession session = Join();
        Tick(1);
        PlayerEntity player = Assert.Single(Players);
        EnemyEntity enemy = _world.Map.SpawnEnemy(EnemyKind.Normal, 1, 0, 100);
        player.Position = Vector2.Zero;
        session.OnRecvPacket(new C_Attack { targetEntityId = enemy.EntityId, attackerClientTick = 2 }.Write());
        if (closeBeforeExecution) session.OnDisconnected(Endpoint);
        // Keep the player registered while executing the queued command. Otherwise removal alone
        // would make a missing execution-time closing check invisible to this test.
        _world.Map.Tick(2);
        Assert.Same(player, Assert.Single(Players));
        if (closeBeforeExecution) Assert.Equal(100, enemy.Hp);
        else Assert.InRange(enemy.Hp, 1, 99);
        Tick(3);
        if (closeBeforeExecution) Assert.Empty(Players);
    }

    [Theory]
    [InlineData(false)] // positive control: the same Knight input commits a dash
    [InlineData(true)]
    public void QueuedSkill_OnlyLiveOwnerCanCommitDash(bool closeBeforeExecution)
    {
        CapturedSession session = Join();
        Tick(1);
        PlayerEntity player = Assert.Single(Players);
        long previousCast = player.GetLastSkillTick((byte)SkillId.Dash);
        session.OnRecvPacket(new C_SkillUse
        {
            skillId = (byte)SkillId.Dash,
            attackerClientTick = 2,
            facing = 1,
        }.Write());
        if (closeBeforeExecution) session.OnDisconnected(Endpoint);
        _world.Map.Tick(2);
        Assert.Same(player, Assert.Single(Players));
        if (closeBeforeExecution)
        {
            Assert.Equal(0, player.ExternalImpulseVx);
            Assert.Equal(previousCast, player.GetLastSkillTick((byte)SkillId.Dash));
        }
        else
        {
            Assert.True(player.ExternalImpulseVx > 0);
            Assert.True(player.GetLastSkillTick((byte)SkillId.Dash) > previousCast);
        }
        Tick(3);
        if (closeBeforeExecution) Assert.Empty(Players);
    }

    [Fact]
    public void EntryAndTransfer_KeepInitialPacketOrderAndPlayerState()
    {
        CapturedSession observer = Join();
        Tick(1);
        CapturedSession session = Join();
        Tick(2);
        Assert.Equal(new[] { PacketID.S_HandshakeResult, PacketID.S_LoginResult, PacketID.S_EnterMap, PacketID.S_PlayerHp, PacketID.S_PlayerJoin },
            session.Packets.Take(5).Select(Id));
        S_PlayerJoin entryRoster = new();
        entryRoster.Read(new ArraySegment<byte>(session.Packets[4]));
        Assert.Equal(observer.EntityId, entryRoster.entityId);
        PlayerEntity original = Assert.Single(Players, p => ReferenceEquals(p.Owner, session));
        int originalId = original.EntityId;
        original.Hp = 37;
        // Populate the destination too, so transition order cannot pass with an empty roster.
        // Solo players now enter different instances; party membership preserves this roster fixture.
        var party = _world.Party.CreateParty(observer.EntityId, session.EntityId)!;
        PlayerEntity other = Assert.Single(Players, p => ReferenceEquals(p.Owner, observer));
        other.Position = new Vector2(20, 0);
        observer.OnRecvPacket(new C_EnterPortal { portalId = 1 }.Write());
        Tick(3);
        Tick(4);
        Assert.True(_world.TryGetInstance(MapId.HuntingGround, InstanceKey.ForParty(party.PartyId), out GameMap? hunting));
        Assert.Same(observer, Assert.Single(hunting!.Players).Owner);
        original.Position = new Vector2(20, 0);
        session.Packets.Clear();
        session.OnRecvPacket(new C_EnterPortal { portalId = 1 }.Write());
        Tick(5);
        Assert.Equal(2, Players.Count());
        PlayerEntity moved = Assert.Single(Players, p => ReferenceEquals(p.Owner, session));
        Assert.Equal(originalId, moved.EntityId);
        Assert.Equal(37, moved.Hp);
        Assert.Same(original.Stats, moved.Stats);
        Assert.Equal(new[] { PacketID.S_MapTransition, PacketID.S_PlayerHp, PacketID.S_PlayerJoin },
            session.Packets.Take(3).Select(Id));
        S_PlayerJoin destinationRoster = new();
        destinationRoster.Read(new ArraySegment<byte>(session.Packets[2]));
        Assert.Equal(observer.EntityId, destinationRoster.entityId);
    }

    [Theory]
    [InlineData(0)] // protected hook without the required account
    [InlineData(1)] // seed preparation fails before any membership commit
    [InlineData(2)] // the map owner disappears during preparation, before the final owner check
    public void RejectedAdmission_ClosesAndReclaimsMapInventoryAndAccount(int rejection)
    {
        CapturedSession session;
        if (rejection == 0)
        {
            session = new CapturedSession(_world);
            session.OnRecvPacket(new C_Handshake { clientVersion = ProtocolVersion.Current }.Write());
            session.SetCharacterClass(0);
            session.EnterGameWorldIfReady();
            Assert.False(session.IsLoggedIn);
        }
        else
        {
            session = Join();
            if (rejection == 1)
            {
                // Unreachable by a short login sequence: inject only the exhausted revision.
                typeof(GuildRegistry).GetField("_state", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .SetValue(_world.Guild, new GuildState(uint.MaxValue, 0, 1));
            }
            else
            {
                session.OnMapRead = () =>
                {
                    session.OnMapRead = null;
                    Assert.True(_world.Map.RemovePlayer(session.EntityId));
                };
            }
        }

        Tick(1);

        Assert.True(session.IsClosing);
        Assert.Equal(-1, session.EntityId);
        Assert.Empty(Players);
        Assert.Equal(0, _world.Accounts.OnlineCount);
        Assert.Empty((IDictionary)typeof(InventoryRegistry)
            .GetField("_registeredSessions", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(_world.Inventory)!);
        Assert.Empty((IDictionary)typeof(InventoryRegistry)
            .GetField("_states", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_world.Inventory)!);
        Assert.Empty((IDictionary)typeof(GuildRegistry)
            .GetField("_members", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_world.Guild)!);
        Assert.DoesNotContain(session.Packets, packet => Id(packet) is PacketID.S_EnterMap
            or PacketID.S_PlayerHp or PacketID.S_PlayerJoin or PacketID.S_GuildSnapshot);
    }

    [Theory]
    [InlineData(PacketID.S_EnterMap, false)]
    [InlineData(PacketID.S_EnterMap, true)]
    [InlineData(PacketID.S_PlayerHp, false)]
    [InlineData(PacketID.S_PlayerHp, true)]
    [InlineData(PacketID.S_PlayerJoin, false)]
    [InlineData(PacketID.S_PlayerJoin, true)]
    public void InitialPublicationFailure_CleansConnectionButRetainsOneSeed(PacketID failureAt, bool throws)
    {
        CapturedSession observer = Join();
        Tick(1);
        CapturedSession session = Join();
        string accountName = session.Account!.Value.Name;
        bool faultReached = false;
        session.OnPacket = id =>
        {
            if (id != failureAt) return;
            faultReached = true;
            if (throws) throw new IOException("fixture: initial publication send failed");
            session.Disconnect();
        };
        observer.Packets.Clear();

        Tick(2);

        Assert.True(faultReached);
        Assert.True(session.IsClosing);
        Assert.Equal(-1, session.EntityId);
        Assert.Same(observer, Assert.Single(Players).Owner);
        Assert.Equal(1, _world.Accounts.OnlineCount);
        var registrations = (Dictionary<int, GameSession>)typeof(InventoryRegistry)
            .GetField("_registeredSessions", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(_world.Inventory)!;
        Assert.Same(observer, Assert.Single(registrations).Value);
        Assert.DoesNotContain(session.Packets, packet => Id(packet) == PacketID.S_GuildSnapshot);
        Assert.DoesNotContain(observer.Packets, packet => Id(packet) == PacketID.S_PlayerJoin);
        S_GuildSnapshot afterClose = new();
        afterClose.Read(new ArraySegment<byte>(observer.Packets.Last(packet => Id(packet) == PacketID.S_GuildSnapshot)));
        Assert.Equal(10, afterClose.treasury);
        Assert.Equal(2, afterClose.memberCount);
        Assert.Equal(1, afterClose.onlineMemberCount);

        CapturedSession returning = new(_world);
        returning.OnRecvPacket(new C_Handshake { clientVersion = ProtocolVersion.Current }.Write());
        returning.OnRecvPacket(new C_Login { accountName = accountName }.Write());
        _world.Accounts.Tick();
        Assert.True(returning.IsLoggedIn);
        returning.OnRecvPacket(new C_CharacterSelect { characterClass = 0 }.Write());
        Tick(3);
        S_GuildSnapshot afterReturn = new();
        afterReturn.Read(new ArraySegment<byte>(Assert.Single(returning.Packets,
            packet => Id(packet) == PacketID.S_GuildSnapshot)));
        Assert.Equal(10, afterReturn.treasury);
        Assert.Equal(2, afterReturn.memberCount);
        Assert.Equal(2, afterReturn.onlineMemberCount);
    }
}

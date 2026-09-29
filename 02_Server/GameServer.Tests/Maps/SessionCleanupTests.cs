using System.Net;
using System.Numerics;
using System.Reflection;
using Dawnholder.Server.GameServer.Entities;
using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Tests.Maps;

// Drives the real world tick without starting a scheduler. No cleanup implementation is mocked.
internal static class LifecycleTestWorld
{
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
        internal CapturedSession(GameWorld world) : base(world) { }
        public override void Send(ArraySegment<byte> packet)
        {
            byte[] bytes = packet.ToArray();
            Packets.Add(bytes);
            OnPacket?.Invoke(Id(bytes));
        }
        public override void Disconnect() => OnDisconnected(Endpoint);
    }

    static PacketID Id(byte[] bytes) => (PacketID)BitConverter.ToUInt16(bytes, 2);
    void Tick(long tick) => LifecycleTestWorld.Tick(_world, tick);
    IEnumerable<PlayerEntity> Players => Enum.GetValues<MapId>().SelectMany(id => _world.GetMap(id)!.Players);
    CapturedSession Join()
    {
        CapturedSession session = new(_world);
        session.OnConnected(Endpoint);
        session.OnRecvPacket(new C_Handshake { clientVersion = ProtocolVersion.Current }.Write());
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
        if (boundary >= 2) _world.GetMap(MapId.HuntingGround)!.Tick(2);
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
        Assert.Same(session, Assert.Single(_world.GetMap(MapId.HuntingGround)!.Players).Owner);
        // Contract probe: a lagging routing hint must never select the cleanup owner.
        // This is a controlled state fixture, not a claim to reproduce a thread interleaving.
        session.SetMigrating(1);
        session.OnDisconnected(Endpoint);
        Tick(3);
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
            skillId = (byte)SkillId.Dash, attackerClientTick = 2, facing = 1,
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
        Assert.Equal(new[] { PacketID.S_HandshakeResult, PacketID.S_EnterMap, PacketID.S_PlayerHp, PacketID.S_PlayerJoin },
            session.Packets.Take(4).Select(Id));
        S_PlayerJoin entryRoster = new();
        entryRoster.Read(new ArraySegment<byte>(session.Packets[3]));
        Assert.Equal(observer.EntityId, entryRoster.entityId);
        PlayerEntity original = Assert.Single(Players, p => ReferenceEquals(p.Owner, session));
        int originalId = original.EntityId;
        original.Hp = 37;
        // Populate the destination too, so transition order cannot pass with an empty roster.
        PlayerEntity other = Assert.Single(Players, p => ReferenceEquals(p.Owner, observer));
        other.Position = new Vector2(20, 0);
        observer.OnRecvPacket(new C_EnterPortal { portalId = 1 }.Write());
        Tick(3);
        Assert.Same(observer, Assert.Single(_world.GetMap(MapId.HuntingGround)!.Players).Owner);
        original.Position = new Vector2(20, 0);
        session.Packets.Clear();
        session.OnRecvPacket(new C_EnterPortal { portalId = 1 }.Write());
        Tick(4);
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
}

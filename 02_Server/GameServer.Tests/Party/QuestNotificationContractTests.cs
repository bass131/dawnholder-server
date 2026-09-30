using System.Net;
using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Entities;
using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Maps.Transitions;
using Dawnholder.Server.GameServer.Party;
using Dawnholder.Server.GameServer.Quest;
using Dawnholder.Server.GameServer.Sessions;
using Dawnholder.Server.GameServer.Tests.Maps;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Tests.Party;

// Real world phases, session handlers, notifier and packet serialization; no scheduler/sockets.
[Collection("GameWorldPartyIntegrationTests")]
public sealed class QuestNotificationContractTests : IDisposable
{
    readonly GameWorld _world = new(new Dictionary<MapId, (MapTerrain?, MapContent?)>());
    readonly List<CapturedSession> _sessions = new();
    long _tick;
    static readonly IPEndPoint Endpoint = new(IPAddress.Loopback, 0);

    sealed class CapturedSession : GameSession
    {
        internal readonly List<byte[]> Packets = new();
        internal CapturedSession(GameWorld world) : base(world) { }
        public override void Send(ArraySegment<byte> payload) => Packets.Add(payload.ToArray());
        public override void Disconnect() => OnDisconnected(Endpoint);
    }

    void Tick() => LifecycleTestWorld.Tick(_world, ++_tick);
    CapturedSession Join()
    {
        CapturedSession session = new(_world);
        _sessions.Add(session);
        session.OnConnected(Endpoint);
        session.OnRecvPacket(new C_Handshake { clientVersion = ProtocolVersion.Current }.Write());
        session.OnRecvPacket(new C_CharacterSelect { characterClass = 0 }.Write());
        Tick();
        Assert.True(session.EntityId >= 0);
        return session;
    }

    (int Id, CapturedSession Session) AddInMap(MapId mapId)
    {
        CapturedSession session = new(_world);
        _sessions.Add(session);
        int id = _world.NextEntityId();
        _world.GetMap(mapId)!.AddPlayerWithId(new PlayerTransferState(id, PlayerStats.Knight(), 100), session, Vector2.Zero);
        return (id, session);
    }

    void ClearPackets() { foreach (CapturedSession session in _sessions) session.Packets.Clear(); }
    static PacketID Id(byte[] bytes) => (PacketID)BitConverter.ToUInt16(bytes, 2);
    static PacketID[] DomainIds(CapturedSession session) => session.Packets.Select(Id)
        .Where(id => id == PacketID.S_PartyUpdate || id == PacketID.S_QuestUpdate).ToArray();
    static S_QuestUpdate[] Quests(CapturedSession session) => session.Packets
        .Where(p => Id(p) == PacketID.S_QuestUpdate).Select(p =>
        {
            S_QuestUpdate packet = new();
            packet.Read(new ArraySegment<byte>(p));
            return packet;
        }).ToArray();
    static S_PartyUpdate[] Parties(CapturedSession session) => session.Packets
        .Where(p => Id(p) == PacketID.S_PartyUpdate).Select(p =>
        {
            S_PartyUpdate packet = new();
            packet.Read(new ArraySegment<byte>(p));
            return packet;
        }).ToArray();

    // Invoke the production death hook, which enqueues the real world quest job.
    void Death(int killer, EnemyKind kind = EnemyKind.Normal)
    {
        GameMap map = _world.GetMap(MapId.HuntingGround)!;
        EnemyEntity enemy = map.SpawnEnemy(kind, x: 0, y: 0, maxHp: 10);
        map.HandleEnemyDeath(enemy, killer);
    }

    public void Dispose() => _world.Stop();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void QueuedKill_UsesMembershipAtExecution_WorldPartyPhaseAlwaysRunsFirst(bool killQueuedFirst)
    {
        CapturedSession a = Join(), b = Join(), outsider = Join();
        int aId = a.EntityId, bId = b.EntityId;
        // A real accepted invitation must publish PartyUpdate before quest notifications.
        _world.Party.RecordInvite(aId, bId, _world.CurrentTick);
        ClearPackets();
        if (killQueuedFirst) Death(aId);
        b.OnRecvPacket(new C_PartyRespond { inviterEntityId = aId, accept = 1 }.Write());
        if (!killQueuedFirst) Death(aId);
        Assert.Null(_world.Party.GetMembershipByEntity(aId));
        Tick();
        PartyMembership membership = _world.Party.GetMembershipByEntity(aId)!;
        Assert.NotNull(membership);
        Assert.Equal(1, _world.Quest.GetPartyProgress(membership.PartyId));
        Assert.Equal(0, _world.Quest.GetSoloProgress(aId));
        // Map send queue already ran this tick; no extra immediate send or extra quest job.
        Assert.Empty(DomainIds(a));
        Assert.Empty(DomainIds(b));
        Tick();
        foreach (CapturedSession session in new[] { a, b })
        {
            Assert.Equal(new[] { PacketID.S_PartyUpdate, PacketID.S_QuestUpdate }, DomainIds(session));
            Assert.Equal(membership.PartyId, Assert.Single(Parties(session)).partyId);
            Assert.Equal(1, Assert.Single(Quests(session)).currentCount);
        }
        Assert.Empty(DomainIds(outsider));
    }

    [Fact]
    public void QueuedKill_BeforeLeave_CreditsRestoredSolo_AndCannotReviveRetiredParty()
    {
        CapturedSession a = Join(), b = Join();
        int aId = a.EntityId;
        _world.Quest.OnKill(aId);
        _world.Quest.OnKill(aId);
        PartyState party = _world.Party.CreateParty(aId, b.EntityId)!;
        _world.Quest.OnKill(aId);
        ClearPackets();
        Death(aId); // Enqueued before Leave; execution is after Party phase.
        a.OnRecvPacket(new C_PartyLeave { reserved = 0 }.Write());
        Tick();
        Assert.Null(_world.Party.GetMembershipByEntity(aId));
        Assert.Equal(0, _world.Quest.TrackedPartyProgressCount);
        Assert.Equal(0, _world.Quest.GetPartyProgress(party.PartyId));
        Assert.Equal(3, _world.Quest.GetSoloProgress(aId));
        Tick();
        Assert.Equal(new[] { PacketID.S_PartyUpdate, PacketID.S_QuestUpdate }, DomainIds(a));
        Assert.Equal(0, Assert.Single(Parties(a)).partyId);
        Assert.Equal(3, Assert.Single(Quests(a)).currentCount);
        Assert.Equal(new[] { PacketID.S_PartyUpdate }, DomainIds(b));
    }

    [Fact]
    public void QueuedKill_ReformedPartyGetsCredit_OldCleanupPreservesNewPartyAndSolo()
    {
        CapturedSession a = Join(), b = Join(), c = Join();
        int aId = a.EntityId;
        _world.Quest.OnKill(aId);
        PartyState old = _world.Party.CreateParty(aId, b.EntityId)!;
        _world.Quest.OnKill(aId);
        PartyState? replacement = null;
        Death(aId);
        _world.Party.EnqueueJob(() =>
        {
            _world.Party.Disband(old.PartyId);
            replacement = _world.Party.CreateParty(aId, c.EntityId);
        });
        ClearPackets();
        Tick();
        Assert.NotNull(replacement);
        Assert.NotEqual(old.PartyId, replacement!.PartyId);
        Assert.Equal(0, _world.Quest.GetPartyProgress(old.PartyId));
        Assert.Equal(1, _world.Quest.GetPartyProgress(replacement.PartyId));
        Assert.Equal(1, _world.Quest.TrackedPartyProgressCount);
        Assert.Equal(1, _world.Quest.GetSoloProgress(aId));
        Tick();
        Assert.Equal(1, Assert.Single(Quests(a)).currentCount);
        Assert.Empty(Quests(b));
        Assert.Equal(1, Assert.Single(Quests(c)).currentCount);
    }

    [Theory]
    [InlineData("direct")]
    [InlineData("leave")]
    [InlineData("disconnect")]
    public void EveryDisbandPath_CleansOnlyRetiredProgress_AndPreservesNotificationPolicy(string path)
    {
        CapturedSession a = Join(), b = Join(), c = Join(), d = Join();
        int aId = a.EntityId, bId = b.EntityId;
        _world.Quest.OnKill(aId);
        PartyState retired = _world.Party.CreateParty(aId, bId)!;
        PartyState other = _world.Party.CreateParty(c.EntityId, d.EntityId)!;
        for (int i = 0; i < QuestConstants.BossUnlockKillCount; i++) _world.Quest.OnKill(aId);
        _world.Quest.OnKill(c.EntityId);
        // Stale invites are deliberately seeded to distinguish leave and disconnect policy.
        _world.Party.RecordInvite(aId, c.EntityId, 0);
        _world.Party.RecordInvite(d.EntityId, aId, 0);
        ClearPackets();
        if (path == "direct") Assert.True(_world.Party.Disband(retired.PartyId));
        else if (path == "leave") a.OnRecvPacket(new C_PartyLeave { reserved = 0 }.Write());
        else a.OnDisconnected(Endpoint);
        // Even direct Disband must queue progress cleanup instead of mutating Quest here.
        Assert.Equal(2, _world.Quest.TrackedPartyProgressCount);
        Tick();
        Assert.Null(_world.Party.GetMembershipByEntity(aId));
        Assert.Null(_world.Party.GetMembershipByEntity(bId));
        Assert.Equal(0, _world.Quest.GetPartyProgress(retired.PartyId));
        Assert.Equal(1, _world.Quest.GetPartyProgress(other.PartyId));
        Assert.Equal(1, _world.Quest.TrackedPartyProgressCount);
        Assert.Equal(1, _world.Quest.GetSoloProgress(aId));
        Assert.True(_world.Quest.IsBossUnlocked(aId));
        Assert.True(_world.Quest.IsBossUnlocked(bId));
        Assert.Equal(path != "disconnect", _world.Party.TryGetPendingInvite(aId, out _));
        Assert.Equal(path != "disconnect", _world.Party.TryGetPendingInvite(c.EntityId, out _));
        Tick();
        Assert.Empty(Quests(a));
        Assert.Empty(Quests(b));
        Assert.Empty(DomainIds(c));
        Assert.Empty(DomainIds(d));
        if (path == "leave") Assert.Equal(0, Assert.Single(Parties(a)).partyId);
        else Assert.Empty(Parties(a));
        if (path != "direct") Assert.Equal(0, Assert.Single(Parties(b)).partyId);
        else Assert.Empty(Parties(b));
        ClearPackets();
        Assert.False(_world.Party.Disband(retired.PartyId));
        PartyFlow.CleanupOnDisconnect(_world, aId);
        Tick();
        Tick();
        Assert.All(_sessions, s => Assert.Empty(DomainIds(s)));
        Assert.Equal(1, _world.Quest.TrackedPartyProgressCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void KillAndBossReset_KeepQueueOrder_AndResetDoesNotEmitQuestPacket(bool resetFirst)
    {
        CapturedSession a = Join(), b = Join();
        PartyState party = _world.Party.CreateParty(a.EntityId, b.EntityId)!;
        for (int i = 0; i < QuestConstants.BossUnlockKillCount; i++) _world.Quest.OnKill(a.EntityId);
        ClearPackets();
        if (resetFirst) Death(a.EntityId, EnemyKind.Boss);
        Death(a.EntityId);
        if (!resetFirst) Death(a.EntityId, EnemyKind.Boss);
        Tick();
        Assert.Equal(resetFirst ? 1 : 0, _world.Quest.GetPartyProgress(party.PartyId));
        Assert.True(_world.Quest.IsBossUnlocked(a.EntityId));
        Assert.True(_world.Quest.IsBossUnlocked(b.EntityId));
        Tick();
        foreach (CapturedSession session in new[] { a, b })
        {
            S_QuestUpdate update = Assert.Single(Quests(session));
            Assert.Equal(resetFirst ? 1 : QuestConstants.BossUnlockKillCount, update.currentCount);
            Assert.Equal(QuestConstants.BossUnlockKillCount, update.targetCount);
        }
    }

    [Fact]
    public void CrossMapPackets_PreserveEachRecipientsOrderedClampedProgress_ExcludeNonmember()
    {
        var a = AddInMap(MapId.Town);
        var b = AddInMap(MapId.HuntingGround);
        var outsider = AddInMap(MapId.Town);
        _world.Party.CreateParty(a.Id, b.Id);
        ClearPackets();
        int target = QuestConstants.BossUnlockKillCount;
        for (int i = 0; i < target + 2; i++) Death(i % 2 == 0 ? a.Id : b.Id);
        Tick();
        Assert.Empty(Quests(a.Session));
        Assert.Empty(Quests(b.Session));
        Tick();
        int[] expected = Enumerable.Range(1, target).Concat(new[] { target, target }).ToArray();
        Assert.Equal(expected, Quests(a.Session).Select(p => p.currentCount));
        Assert.Equal(expected, Quests(b.Session).Select(p => p.currentCount));
        Assert.All(Quests(a.Session).Concat(Quests(b.Session)), p => Assert.Equal(target, p.targetCount));
        Assert.Empty(Quests(outsider.Session));
        ClearPackets();
        _world.Quest.EnqueueJob(_world.Quest.ResetAllQuestProgress);
        Tick();
        Tick();
        Assert.All(_sessions, s => Assert.Empty(Quests(s)));
    }

    [Fact]
    public void Notifier_UsesCapturedRecipientsAndValues_AfterMembershipAndProgressChange()
    {
        var a = AddInMap(MapId.Town);
        var b = AddInMap(MapId.HuntingGround);
        var c = AddInMap(MapId.Town);
        PartyState old = _world.Party.CreateParty(a.Id, b.Id)!;
        IReadOnlyList<QuestProgressUpdate> captured = _world.Quest.OnKill(a.Id);
        _world.Party.Disband(old.PartyId);
        _world.Party.CreateParty(a.Id, c.Id);
        _world.Quest.OnKill(a.Id);
        _world.Quest.OnKill(a.Id);
        ClearPackets();
        QuestNotifier.Send(_world, captured);
        Assert.All(_sessions, s => Assert.Empty(Quests(s)));
        Tick();
        Assert.Equal(1, Assert.Single(Quests(a.Session)).currentCount);
        Assert.Equal(1, Assert.Single(Quests(b.Session)).currentCount);
        Assert.Empty(Quests(c.Session));
    }

#if DEBUG
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DebugCommand_QueuedGuardRejectsClosedSession_WhileActiveSessionCompletes(bool closeBeforeTick)
    {
        CapturedSession a = Join(), b = Join();
        int aId = a.EntityId, bId = b.EntityId;
        PartyState party = _world.Party.CreateParty(aId, bId)!;
        ClearPackets();
        a.OnRecvPacket(new C_CheatCommand { cheatType = 0 }.Write());
        Assert.False(_world.Quest.IsBossUnlocked(aId));
        if (closeBeforeTick) a.OnDisconnected(Endpoint);
        Tick();
        Assert.Equal(!closeBeforeTick, _world.Quest.IsBossUnlocked(aId));
        Assert.Equal(!closeBeforeTick, _world.Quest.IsBossUnlocked(bId));
        Assert.Equal(closeBeforeTick ? 0 : QuestConstants.BossUnlockKillCount,
            _world.Quest.GetPartyProgress(party.PartyId));
        Tick();
        if (closeBeforeTick)
        {
            Assert.Empty(Quests(a));
            Assert.Empty(Quests(b));
        }
        else
        {
            Assert.Equal(QuestConstants.BossUnlockKillCount, Assert.Single(Quests(a)).currentCount);
            Assert.Equal(QuestConstants.BossUnlockKillCount, Assert.Single(Quests(b)).currentCount);
        }
    }
#endif
}

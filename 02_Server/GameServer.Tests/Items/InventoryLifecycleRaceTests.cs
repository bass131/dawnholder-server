using System.Buffers.Binary;
using System.Collections;
using System.Net;
using System.Numerics;
using System.Reflection;
using Dawnholder.Server.GameServer.Entities;
using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Sessions;
using Dawnholder.Server.GameServer.Tests.Maps;
using Shared.GameData;
using Shared.Protocol;
using Xunit.Abstractions;

namespace Dawnholder.Server.GameServer.Tests.Items;

// Independent verification of economy lifetimes on the real receive path, real combat and portal
// jobs, and the real world tick: a kill and a map move in the same tick, requests caught by a move
// or a close, sends that fail after a commit, and the per-connection request guard.
// Expected values are literals from the PR1 acceptance table referenced from
// 01_Phases/goals/2026-10-05-items-inventory-currency/goal.md; economy packets are decoded with
// the fixed wire table below, never with generated code or product calculations.
[Collection("GameWorldRegistryTests")]
public sealed class InventoryLifecycleRaceTests : IDisposable
{
    // Test allowance, not a contract value (same meaning as in InventoryWireContractTests).
    const int SettleTicks = 3;
    const int MaterialId = 1;
    const int CoinPouchId = 2;

    static readonly IPEndPoint Endpoint = new(IPAddress.Loopback, 0);
    readonly GameWorld _world = new(new Dictionary<MapId, (MapTerrain?, MapContent?)>());
    readonly ITestOutputHelper _output;
    long _tick;

    public InventoryLifecycleRaceTests(ITestOutputHelper output) => _output = output;

    public void Dispose() => _world.Stop();

    // ── A kill and a map move in one tick ───────────────────────────────────────

    // HuntingGround portal 2 leads to Town, which the world ticks before HuntingGround: the player
    // is in no map between the source tick and the next destination tick. The killer's connection
    // is still active (INV-05: connection/ownership activity, not presence in a map), and the
    // economy must survive the move (INV-03), so the earned reward must still arrive.
    [Fact]
    public void KillThenPortalToAnEarlierTickedMap_InOneTick_StillRewardsTheKiller()
    {
        ProbeSession knight = JoinAndEnter(CharacterClass.Knight);
        GameMap hunting = MoveToHuntingGround(knight);
        StandAt(hunting, knight, 5f);
        EnemyEntity enemy = hunting.SpawnEnemy(EnemyKind.Normal, 6f, 0f, maxHp: 1);
        int mark = knight.Packets.Count;

        SubmitMelee(knight, enemy);
        EnterPortal(knight, portalId: 2);
        Tick();
        bool enemyKilledInThatTick = !hunting.Enemies.ContainsKey(enemy.EntityId);
        Settle();
        bool killerArrivedInTown = _world.Map.GetPlayer(knight.EntityId) != null;
        _output.WriteLine($"economy packets after kill+portal: {Describe(knight.EconomyPacketsSince(mark))}");

        Assert.True(enemyKilledInThatTick, "fixture: the melee job must kill before the portal job runs");
        Assert.True(killerArrivedInTown, "fixture: the portal job must move the killer to Town");
        AssertQueriedState(knight, revision: 1, currency: 10, (MaterialId, 1));
    }

    // Positive control: Town portal 1 leads to HuntingGround, which the world ticks after Town, so
    // the player is already in the destination map when the economy drains.
    [Fact]
    public void KillThenPortalToALaterTickedMap_InOneTick_RewardsTheKiller()
    {
        ProbeSession knight = JoinAndEnter(CharacterClass.Knight);
        StandAt(_world.Map, knight, 20f);
        EnemyEntity enemy = _world.Map.SpawnEnemy(EnemyKind.Normal, 21f, 0f, maxHp: 1);

        SubmitMelee(knight, enemy);
        EnterPortal(knight, portalId: 1);
        Tick();
        bool enemyKilledInThatTick = !_world.Map.Enemies.ContainsKey(enemy.EntityId);
        Settle();
        bool killerArrivedInHuntingGround = _world.GetMap(MapId.HuntingGround)!.GetPlayer(knight.EntityId) != null;

        Assert.True(enemyKilledInThatTick, "fixture: the melee job must kill before the portal job runs");
        Assert.True(killerArrivedInHuntingGround, "fixture: the portal job must move the killer");
        AssertQueriedState(knight, revision: 1, currency: 10, (MaterialId, 1));
    }

    // Positive control for the same map pair: the kill and the move are one tick apart.
    [Fact]
    public void KillThenPortalToAnEarlierTickedMap_OnTheNextTick_RewardsTheKiller()
    {
        ProbeSession knight = JoinAndEnter(CharacterClass.Knight);
        GameMap hunting = MoveToHuntingGround(knight);
        StandAt(hunting, knight, 5f);
        EnemyEntity enemy = hunting.SpawnEnemy(EnemyKind.Normal, 6f, 0f, maxHp: 1);

        SubmitMelee(knight, enemy);
        Tick();
        bool enemyKilled = !hunting.Enemies.ContainsKey(enemy.EntityId);
        EnterPortal(knight, portalId: 2);
        Settle();
        bool killerArrivedInTown = _world.Map.GetPlayer(knight.EntityId) != null;

        Assert.True(enemyKilled, "fixture: the melee job must kill the enemy");
        Assert.True(killerArrivedInTown, "fixture: the portal job must move the killer to Town");
        AssertQueriedState(knight, revision: 1, currency: 10, (MaterialId, 1));
    }

    // A query queued before the portal job may be answered or dropped, but the request guard must
    // be released so the connection can query again after the move.
    [Fact]
    public void RequestCaughtByAPortalJob_ReleasesTheGuard_AndStateSurvives()
    {
        ProbeSession knight = JoinAndEnter(CharacterClass.Knight);
        KillWithMelee(knight, _world.Map.SpawnEnemy(EnemyKind.Golem, 1f, 0f, maxHp: 1));
        Settle();
        GameMap hunting = MoveToHuntingGround(knight);
        StandAt(hunting, knight, 5f);
        int mark = knight.Packets.Count;

        knight.OnRecvPacket(Wire.InventoryRequest());
        EnterPortal(knight, portalId: 2);
        Settle();
        bool killerArrivedInTown = _world.Map.GetPlayer(knight.EntityId) != null;
        _output.WriteLine($"replies to the request caught by the move: {Describe(knight.EconomyPacketsSince(mark))}");

        Assert.True(killerArrivedInTown, "fixture: the portal job must move the player to Town");
        AssertQueriedState(knight, revision: 1, currency: 10, (CoinPouchId, 1));
    }

    // ── Close: no orphan economy entry ──────────────────────────────────────────

    [Fact]
    public void ClosedConnections_LeaveNoEconomyEntryBehind()
    {
        ProbeSession rewarded = JoinAndEnter(CharacterClass.Knight);
        ProbeSession queried = JoinAndEnter(CharacterClass.Knight);
        ProbeSession closingKiller = JoinAndEnter(CharacterClass.Knight);
        KillWithMelee(rewarded, _world.Map.SpawnEnemy(EnemyKind.Normal, 1f, 0f, maxHp: 1));
        queried.OnRecvPacket(Wire.InventoryRequest());
        Settle();
        int entriesWhileConnected = EconomyEntryCount();

        rewarded.OnDisconnected(Endpoint);
        queried.OnRecvPacket(Wire.InventoryRequest());
        queried.OnDisconnected(Endpoint);
        closingKiller.OnPacket = packet =>
        {
            if (Wire.IdOf(packet) == (ushort)PacketID.S_EntityDeath) closingKiller.OnDisconnected(Endpoint);
        };
        KillWithMelee(closingKiller, _world.Map.SpawnEnemy(EnemyKind.Normal, 1f, 0f, maxHp: 1));
        Settle();
        int entriesAfterClose = EconomyEntryCount();

        // Positive control: both live connections owned an entry, so the count is observable.
        Assert.Equal(2, entriesWhileConnected);
        Assert.Equal(0, entriesAfterClose);
    }

    // ── Commit, then a failing send ─────────────────────────────────────────────

    [Fact]
    public void UseWhoseRepliesFailToSend_StaysCommitted_AndTheSameBytesAreStaleAfterwards()
    {
        ProbeSession session = JoinAndEnter(CharacterClass.Knight);
        GainGolemReward(session);
        byte[] use = Wire.ItemUse(CoinPouchId, expectedRevision: 1);

        session.FailEconomySends = true;
        session.OnRecvPacket(use.ToArray());
        Settle();
        session.FailEconomySends = false;
        int failedSends = session.FailedEconomySends;
        int mark = session.Packets.Count;
        session.OnRecvPacket(use.ToArray());
        Settle();
        List<byte[]> replayReplies = session.EconomyPacketsSince(mark);

        Assert.True(failedSends >= 1, "fixture: the use reply must have hit the failing send");
        // A reply proves the guard was released after the failed job; Stale proves no second use.
        Assert.Equal(2, replayReplies.Count);
        AssertItemUseResult(replayReplies[0], Wire.Stale, CoinPouchId, revision: 2);
        AssertSnapshot(replayReplies[1], revision: 2, currency: 60);
    }

    [Fact]
    public void KillPushThatFailsToSend_IsNeitherRetriedNorGrantedAgain()
    {
        ProbeSession session = JoinAndEnter(CharacterClass.Knight);

        session.FailEconomySends = true;
        KillWithMelee(session, _world.Map.SpawnEnemy(EnemyKind.Normal, 1f, 0f, maxHp: 1));
        Settle();
        session.FailEconomySends = false;
        int failedSends = session.FailedEconomySends;
        int mark = session.Packets.Count;
        Settle(SettleTicks * 2);
        List<byte[]> laterPushes = session.EconomyPacketsSince(mark);

        Assert.Equal(1, failedSends);
        Assert.Empty(laterPushes);
        AssertQueriedState(session, revision: 1, currency: 10, (MaterialId, 1));
    }

    // ── Per-connection request guard across request kinds ───────────────────────

    [Fact]
    public void UseSubmittedWhileAQueryIsInFlight_IsDropped_AndTheNextUseIsServed()
    {
        ProbeSession session = JoinAndEnter(CharacterClass.Knight);
        GainGolemReward(session);
        int mark = session.Packets.Count;

        session.OnRecvPacket(Wire.InventoryRequest());
        session.OnRecvPacket(Wire.ItemUse(CoinPouchId, expectedRevision: 1));
        Settle();
        List<byte[]> busyReplies = session.EconomyPacketsSince(mark);
        mark = session.Packets.Count;
        session.OnRecvPacket(Wire.ItemUse(CoinPouchId, expectedRevision: 1));
        Settle();
        List<byte[]> laterReplies = session.EconomyPacketsSince(mark);

        AssertSnapshot(Assert.Single(busyReplies), revision: 1, currency: 10, (CoinPouchId, 1));
        Assert.Equal(2, laterReplies.Count);
        AssertItemUseResult(laterReplies[0], Wire.Success, CoinPouchId, revision: 2);
        AssertSnapshot(laterReplies[1], revision: 2, currency: 60);
    }

    // ── Input boundary ──────────────────────────────────────────────────────────

    [Fact]
    public void EconomyInputBeforeClassSelection_IsDroppedWithoutReplyOrClose()
    {
        ProbeSession session = new(_world);
        session.OnConnected(Endpoint);
        session.OnRecvPacket(new C_Handshake { clientVersion = ProtocolVersion.Current }.Write());
        Tick();
        int mark = session.Packets.Count;

        session.OnRecvPacket(Wire.InventoryRequest());
        session.OnRecvPacket(Wire.ItemUse(CoinPouchId, expectedRevision: 0));
        Settle();

        Assert.Empty(session.EconomyPacketsSince(mark));
        Assert.False(session.IsClosing);
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void ItemUseWithAnOutOfTableItemId_IsDroppedWithoutReplyOrStateChange(int itemId)
    {
        ProbeSession session = JoinAndEnter(CharacterClass.Knight);
        GainGolemReward(session);
        int mark = session.Packets.Count;

        Exception? thrown = Record.Exception(() => session.OnRecvPacket(Wire.ItemUse(itemId, expectedRevision: 1)));
        Settle();

        Assert.Null(thrown);
        Assert.Empty(session.EconomyPacketsSince(mark));
        Assert.False(session.IsClosing);
        AssertQueriedState(session, revision: 1, currency: 10, (CoinPouchId, 1));
    }

    // ── Fixture helpers ─────────────────────────────────────────────────────────

    void Tick() => LifecycleTestWorld.Tick(_world, ++_tick);

    void Settle(int ticks = SettleTicks)
    {
        for (int i = 0; i < ticks; i++) Tick();
    }

    ProbeSession JoinAndEnter(CharacterClass characterClass)
    {
        ProbeSession session = new(_world);
        session.OnConnected(Endpoint);
        session.OnRecvPacket(new C_Handshake { clientVersion = ProtocolVersion.Current }.Write());
        session.OnRecvPacket(new C_CharacterSelect { characterClass = (byte)characterClass }.Write());
        Tick();
        Assert.True(session.EntityId >= 0, "fixture: the real handshake and class selection must enter the world");
        return session;
    }

    // Town portal 1 at x=20 leads to HuntingGround (PortalTable); the move completes within Settle.
    GameMap MoveToHuntingGround(ProbeSession session)
    {
        StandAt(_world.Map, session, 20f);
        EnterPortal(session, portalId: 1);
        Settle();
        GameMap hunting = _world.GetMap(MapId.HuntingGround)!;
        Assert.True(hunting.GetPlayer(session.EntityId) != null, "fixture: Town portal 1 must reach HuntingGround");
        return hunting;
    }

    // Places the player and lets a few ticks record the position, so rewind-based attack checks
    // and portal proximity both see the new spot.
    void StandAt(GameMap map, ProbeSession session, float x)
    {
        map.GetPlayer(session.EntityId)!.Position = new Vector2(x, 0f);
        Settle();
    }

    void EnterPortal(ProbeSession session, int portalId)
        => session.OnRecvPacket(new C_EnterPortal { portalId = (byte)portalId }.Write());

    void SubmitMelee(ProbeSession attacker, EnemyEntity target)
        => attacker.OnRecvPacket(new C_Attack { targetEntityId = target.EntityId, attackerClientTick = (int)(_tick + 1) }.Write());

    void KillWithMelee(ProbeSession attacker, EnemyEntity target)
    {
        SubmitMelee(attacker, target);
        Tick();
        Assert.False(_world.Map.Enemies.ContainsKey(target.EntityId), "fixture: the real melee path must kill the target");
    }

    void GainGolemReward(ProbeSession session)
    {
        int mark = session.Packets.Count;
        KillWithMelee(session, _world.Map.SpawnEnemy(EnemyKind.Golem, 1f, 0f, maxHp: 1));
        Settle();
        AssertSnapshot(Assert.Single(session.EconomyPacketsSince(mark)), revision: 1, currency: 10, (CoinPouchId, 1));
    }

    void AssertQueriedState(ProbeSession session, uint revision, int currency, params (int ItemId, int Count)[] occupied)
    {
        int mark = session.Packets.Count;
        session.OnRecvPacket(Wire.InventoryRequest());
        Settle();
        AssertSnapshot(Assert.Single(session.EconomyPacketsSince(mark)), revision, currency, occupied);
    }

    // Read-only observation of the registry's private per-connection map. No product hook exists for
    // this count; a renamed field fails here loudly instead of passing silently.
    int EconomyEntryCount()
    {
        object registry = _world.Inventory;
        FieldInfo? field = registry.GetType().GetField("_states", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.True(field != null, "fixture: the registry's per-connection state map was not found");
        return ((ICollection)field!.GetValue(registry)!).Count;
    }

    static string Describe(List<byte[]> packets)
        => packets.Count == 0
            ? "none"
            : string.Join(", ", packets.Select(packet => Wire.IdOf(packet) == Wire.InventorySnapshotId
                ? $"snapshot(rev={Wire.ReadSnapshot(packet).Revision})"
                : $"useResult(code={Wire.ReadItemUseResult(packet).Result})"));

    static void AssertSnapshot(byte[] packet, uint revision, int currency, params (int ItemId, int Count)[] occupied)
    {
        (uint actualRevision, int actualCurrency, (int ItemId, int Count)[] actualSlots) = Wire.ReadSnapshot(packet);
        (int ItemId, int Count)[] expectedSlots = occupied
            .Concat(Enumerable.Repeat((0, 0), Wire.SlotCount - occupied.Length))
            .ToArray();

        Assert.Equal(Wire.InventorySnapshotId, Wire.IdOf(packet));
        Assert.Equal(Wire.InventorySnapshotLength, packet.Length);
        Assert.Equal(revision, actualRevision);
        Assert.Equal(currency, actualCurrency);
        Assert.Equal(expectedSlots, actualSlots);
    }

    static void AssertItemUseResult(byte[] packet, byte result, int itemId, uint revision)
    {
        (byte actualResult, int actualItemId, uint actualRevision) = Wire.ReadItemUseResult(packet);

        Assert.Equal(Wire.ItemUseResultId, Wire.IdOf(packet));
        Assert.Equal(Wire.ItemUseResultLength, packet.Length);
        Assert.Equal(result, actualResult);
        Assert.Equal(itemId, actualItemId);
        Assert.Equal(revision, actualRevision);
    }

    sealed class ProbeSession : GameSession
    {
        internal readonly List<byte[]> Packets = new();
        internal Action<byte[]>? OnPacket;
        internal bool FailEconomySends;
        internal int FailedEconomySends;

        internal ProbeSession(GameWorld world) : base(world) { }

        // Fault injection for the transport only: economy packets throw while the flag is set.
        public override void Send(ArraySegment<byte> packet)
        {
            byte[] bytes = packet.ToArray();
            bool economyPacket = Wire.IdOf(bytes) is Wire.InventorySnapshotId or Wire.ItemUseResultId;
            if (FailEconomySends && economyPacket)
            {
                FailedEconomySends++;
                throw new IOException("injected send failure");
            }
            Packets.Add(bytes);
            OnPacket?.Invoke(bytes);
        }

        public override void Disconnect() => OnDisconnected(Endpoint);

        internal List<byte[]> EconomyPacketsSince(int mark)
            => Packets.Skip(mark)
                .Where(bytes => Wire.IdOf(bytes) is Wire.InventorySnapshotId or Wire.ItemUseResultId)
                .ToList();
    }

    // Fixed wire table of the PR1 acceptance: LittleEndian, 4-byte header (ushort size including the
    // header, ushort id). InventoryWireContractTests keeps its own private copy of this oracle and
    // is a fixed input of this verification, so it is not refactored to share one.
    static class Wire
    {
        internal const ushort InventoryRequestId = 35;
        internal const ushort InventorySnapshotId = 36;
        internal const ushort ItemUseId = 37;
        internal const ushort ItemUseResultId = 38;
        internal const int InventorySnapshotLength = 76;
        internal const int ItemUseResultLength = 13;
        internal const int SlotCount = 8;
        internal const byte Success = 0;
        internal const byte Stale = 1;

        internal static byte[] InventoryRequest() => Frame(InventoryRequestId, new byte[] { 0 });

        internal static byte[] ItemUse(int itemId, uint expectedRevision)
        {
            byte[] payload = new byte[8];
            BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(0, 4), itemId);
            BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(4, 4), expectedRevision);
            return Frame(ItemUseId, payload);
        }

        internal static ushort IdOf(byte[] packet) => BinaryPrimitives.ReadUInt16LittleEndian(packet.AsSpan(2, 2));

        internal static (uint Revision, int Currency, (int ItemId, int Count)[] Slots) ReadSnapshot(byte[] packet)
        {
            uint revision = BinaryPrimitives.ReadUInt32LittleEndian(packet.AsSpan(4, 4));
            int currency = BinaryPrimitives.ReadInt32LittleEndian(packet.AsSpan(8, 4));
            (int ItemId, int Count)[] slots = new (int ItemId, int Count)[SlotCount];
            for (int i = 0; i < SlotCount; i++)
            {
                int offset = 12 + (i * 8);
                slots[i] = (BinaryPrimitives.ReadInt32LittleEndian(packet.AsSpan(offset, 4)),
                    BinaryPrimitives.ReadInt32LittleEndian(packet.AsSpan(offset + 4, 4)));
            }
            return (revision, currency, slots);
        }

        internal static (byte Result, int ItemId, uint Revision) ReadItemUseResult(byte[] packet)
            => (packet[4],
                BinaryPrimitives.ReadInt32LittleEndian(packet.AsSpan(5, 4)),
                BinaryPrimitives.ReadUInt32LittleEndian(packet.AsSpan(9, 4)));

        static byte[] Frame(ushort id, byte[] payload)
        {
            byte[] packet = new byte[4 + payload.Length];
            BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(0, 2), (ushort)packet.Length);
            BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(2, 2), id);
            payload.CopyTo(packet, 4);
            return packet;
        }
    }
}

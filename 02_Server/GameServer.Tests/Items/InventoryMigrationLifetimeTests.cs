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

// Independent verification of the economy connection lifetime across map moves, on the real
// receive path, real melee/projectile/portal jobs and the real world tick. Requirements: PR1
// acceptance "상태는 World 틱의 세션 경제 수명에 속한다. 맵 이동에서 유지, 종료 때 정리, 재접속은 빈
// 상태" and goal INV-03/05/15 in 01_Phases/goals/2026-10-05-items-inventory-currency/goal.md.
// Expected values are acceptance literals; economy packets are decoded with the fixed wire table
// below. InventoryLifecycleRaceTests and InventoryWireContractTests are fixed verification inputs,
// so their private helpers are copied here instead of being refactored into a shared one.
[Collection("GameWorldRegistryTests")]
public sealed class InventoryMigrationLifetimeTests : IDisposable
{
    // Test allowance, not a contract value (same meaning as in InventoryLifecycleRaceTests).
    const int SettleTicks = 3;
    const int MaterialId = 1;
    const int CoinPouchId = 2;

    static readonly IPEndPoint Endpoint = new(IPAddress.Loopback, 0);
    readonly GameWorld _world = new(new Dictionary<MapId, (MapTerrain?, MapContent?)>());
    readonly ITestOutputHelper _output;
    long _tick;

    public InventoryMigrationLifetimeTests(ITestOutputHelper output) => _output = output;

    public void Dispose() => _world.Stop();

    // ── Deferred damage across a move ───────────────────────────────────────────

    // HuntingGround portal 2 leads to Town, which the world ticks first. The portal job runs in
    // HuntingGround's job phase and the Mage projectile lands later in the same map tick, so both
    // the kill and that tick's economy drain happen while the Mage is in no map.
    [Fact]
    public void DeferredImpactLandingInTheMoveGap_StillRewardsTheMage()
    {
        ProbeSession mage = JoinAndEnter(CharacterClass.Mage);
        GameMap hunting = MoveToHuntingGround(mage);
        StandAt(hunting, mage, 5f);
        EnemyEntity enemy = hunting.SpawnEnemy(EnemyKind.Normal, 6f, 0f, maxHp: 1);
        int travelTicks = LaunchProjectile(mage, enemy);
        Settle(travelTicks - 1);
        bool aliveBeforeLanding = hunting.Enemies.ContainsKey(enemy.EntityId);
        int mark = mage.Packets.Count;

        EnterPortal(mage, portalId: 2);
        Tick();
        bool killedInThePortalTick = !hunting.Enemies.ContainsKey(enemy.EntityId);
        bool inNoMapAfterThatTick = IsInNoMap(mage.EntityId);
        Settle();
        bool arrivedInTown = _world.Map.GetPlayer(mage.EntityId) != null;
        _output.WriteLine($"economy packets after the deferred kill in the move gap: {Describe(mage.EconomyPacketsSince(mark))}");

        Assert.True(aliveBeforeLanding, "fixture: the projectile must still be in flight before the portal tick");
        Assert.True(killedInThePortalTick, "fixture: the deferred impact must land in the portal tick");
        Assert.True(inNoMapAfterThatTick, "fixture: that tick's economy drain must see the Mage in no map");
        Assert.True(arrivedInTown, "fixture: the portal job must move the Mage to Town");
        AssertQueriedState(mage, revision: 1, currency: 10, (MaterialId, 1));
    }

    // Positive control for the fixture above: the same projectile kill without a move. The kill
    // push travels through the next map tick, so it is collected before the query.
    [Fact]
    public void DeferredImpactWithoutAMove_RewardsTheMage()
    {
        ProbeSession mage = JoinAndEnter(CharacterClass.Mage);
        GameMap hunting = MoveToHuntingGround(mage);
        StandAt(hunting, mage, 5f);
        EnemyEntity enemy = hunting.SpawnEnemy(EnemyKind.Normal, 6f, 0f, maxHp: 1);
        int mark = mage.Packets.Count;

        int travelTicks = LaunchProjectile(mage, enemy);
        Settle(travelTicks);
        bool killed = !hunting.Enemies.ContainsKey(enemy.EntityId);
        Settle();
        List<byte[]> pushes = mage.EconomyPacketsSince(mark);

        Assert.True(killed, "fixture: the deferred impact must kill the enemy");
        AssertSnapshot(Assert.Single(pushes), revision: 1, currency: 10, (MaterialId, 1));
        AssertQueriedState(mage, revision: 1, currency: 10, (MaterialId, 1));
    }

    // The killer's activity is its connection, not presence in the enemy's map (INV-05): the
    // projectile lands in HuntingGround after the Mage already arrived in Town.
    [Fact]
    public void DeferredImpactLandingAfterTheMageReachedAnotherMap_RewardsTheMage()
    {
        ProbeSession mage = JoinAndEnter(CharacterClass.Mage);
        GameMap hunting = MoveToHuntingGround(mage);
        StandAt(hunting, mage, 5f);
        EnemyEntity enemy = hunting.SpawnEnemy(EnemyKind.Normal, 6f, 0f, maxHp: 1);

        int travelTicks = LaunchProjectile(mage, enemy);
        EnterPortal(mage, portalId: 2);
        Tick();
        bool aliveWhileMoving = hunting.Enemies.ContainsKey(enemy.EntityId);
        int mark = mage.Packets.Count;
        Settle(travelTicks - 1);
        bool killed = !hunting.Enemies.ContainsKey(enemy.EntityId);
        bool inTownAtLanding = _world.Map.GetPlayer(mage.EntityId) != null;
        Settle();
        List<byte[]> pushes = mage.EconomyPacketsSince(mark);

        Assert.True(aliveWhileMoving, "fixture: the projectile must still be in flight when the Mage leaves");
        Assert.True(killed, "fixture: the deferred impact must kill the enemy");
        Assert.True(inTownAtLanding, "fixture: the Mage must be in Town when the impact lands");
        AssertSnapshot(Assert.Single(pushes), revision: 1, currency: 10, (MaterialId, 1));
        AssertQueriedState(mage, revision: 1, currency: 10, (MaterialId, 1));
    }

    // ── A reward earned in the move gap is the connection's real state ──────────

    [Fact]
    public void GolemRewardEarnedInTheMoveGap_CanBeSpentAfterArrival()
    {
        ProbeSession knight = JoinAndEnter(CharacterClass.Knight);
        GameMap hunting = MoveToHuntingGround(knight);
        StandAt(hunting, knight, 5f);
        EnemyEntity golem = hunting.SpawnEnemy(EnemyKind.Golem, 6f, 0f, maxHp: 1);

        SubmitMelee(knight, golem);
        EnterPortal(knight, portalId: 2);
        Tick();
        bool inNoMapAfterTheKill = IsInNoMap(knight.EntityId);
        Settle();
        int mark = knight.Packets.Count;
        knight.OnRecvPacket(Wire.ItemUse(CoinPouchId, expectedRevision: 1));
        Settle();
        List<byte[]> useReplies = knight.EconomyPacketsSince(mark);

        Assert.True(inNoMapAfterTheKill, "fixture: the kill tick must end in the move gap");
        Assert.Equal(2, useReplies.Count);
        AssertItemUseResult(useReplies[0], Wire.Success, CoinPouchId, revision: 2);
        AssertSnapshot(useReplies[1], revision: 2, currency: 60);
    }

    // ── Close during a move ─────────────────────────────────────────────────────

    // The reward commits in the gap; the connection then closes before the destination job runs.
    // Close cleanup must remove the registration and state, and the destination must not add a
    // ghost player. A new connection starts empty.
    [Fact]
    public void CloseInTheMoveGapAfterTheRewardCommitted_LeavesNoRegistrationStateOrPlayer()
    {
        ProbeSession knight = JoinAndEnter(CharacterClass.Knight);
        GameMap hunting = MoveToHuntingGround(knight);
        StandAt(hunting, knight, 5f);
        EnemyEntity enemy = hunting.SpawnEnemy(EnemyKind.Normal, 6f, 0f, maxHp: 1);
        int knightId = knight.EntityId;

        SubmitMelee(knight, enemy);
        EnterPortal(knight, portalId: 2);
        Tick();
        bool inNoMap = IsInNoMap(knightId);
        int entriesInTheGap = EconomyEntryCount();
        knight.OnDisconnected(Endpoint);
        Settle();
        bool ghostInAnyMap = !IsInNoMap(knightId);
        int registrationsAfterClose = RegistrationCount();
        int entriesAfterClose = EconomyEntryCount();

        Assert.True(inNoMap, "fixture: the close must arrive while the Knight is in the move gap");
        Assert.Equal(1, entriesInTheGap);
        Assert.False(ghostInAnyMap);
        Assert.Equal(0, registrationsAfterClose);
        Assert.Equal(0, entriesAfterClose);

        ProbeSession reconnected = JoinAndEnter(CharacterClass.Knight);
        AssertQueriedState(reconnected, revision: 0, currency: 0);
    }

    // The close lands inside the portal job, after the kill was queued and before the economy
    // drains in the same tick. An observer in HuntingGround triggers it when it is told the Knight
    // left, which is the exact point the Knight entered the move gap.
    [Fact]
    public void CloseInTheMoveGapBeforeTheRewardDrains_DropsTheRewardWithoutOrphans()
    {
        ProbeSession knight = JoinAndEnter(CharacterClass.Knight);
        ProbeSession observer = JoinAndEnter(CharacterClass.Knight);
        GameMap hunting = MoveToHuntingGround(knight);
        MoveToHuntingGround(observer);
        StandAt(hunting, knight, 5f);
        EnemyEntity enemy = hunting.SpawnEnemy(EnemyKind.Normal, 6f, 0f, maxHp: 1);
        int knightId = knight.EntityId;
        int knightMark = knight.Packets.Count;
        bool closedInTheGap = false;
        observer.OnPacket = packet =>
        {
            if (Wire.IdOf(packet) != (ushort)PacketID.S_PlayerLeave) return;
            S_PlayerLeave leave = new();
            leave.Read(new ArraySegment<byte>(packet));
            if (leave.entityId != knightId) return;
            closedInTheGap = IsInNoMap(knightId);
            knight.OnDisconnected(Endpoint);
        };

        SubmitMelee(knight, enemy);
        EnterPortal(knight, portalId: 2);
        Tick();
        observer.OnPacket = null;
        bool killedInThatTick = !hunting.Enemies.ContainsKey(enemy.EntityId);
        Settle();
        List<byte[]> knightEconomyPackets = knight.EconomyPacketsSince(knightMark);
        bool ghostInAnyMap = !IsInNoMap(knightId);
        int registrationsAfterClose = RegistrationCount();
        int entriesAfterClose = EconomyEntryCount();

        Assert.True(killedInThatTick, "fixture: the melee job must kill before the portal job runs");
        Assert.True(closedInTheGap, "fixture: the close must arrive after the Knight left HuntingGround");
        Assert.Empty(knightEconomyPackets);
        Assert.False(ghostInAnyMap);
        Assert.Equal(1, registrationsAfterClose);
        Assert.Equal(0, entriesAfterClose);
        AssertQueriedState(observer, revision: 0, currency: 0);
    }

    // ── Admission and registration lifetime ─────────────────────────────────────

    [Fact]
    public void AdmissionClosedBeforeItsJobRuns_RegistersNothing()
    {
        ProbeSession session = new(_world);
        session.OnConnected(Endpoint);
        session.OnRecvPacket(new C_Handshake { clientVersion = ProtocolVersion.Current }.Write());
        session.OnRecvPacket(new C_CharacterSelect { characterClass = (byte)CharacterClass.Knight }.Write());

        session.OnDisconnected(Endpoint);
        Settle();

        Assert.True(session.EntityId < 0);
        Assert.Empty(_world.Map.Players);
        Assert.Equal(0, RegistrationCount());
        Assert.Equal(0, EconomyEntryCount());
    }

    // A send failure during admission closes the connection; the registration that preceded the
    // sends must be removed by the same tick's close cleanup.
    [Fact]
    public void AdmissionThatClosesWhileSendingEnterMap_LeavesNoRegistration()
    {
        ProbeSession session = new(_world);
        int registrationsAtEnterMap = -1;
        session.OnPacket = packet =>
        {
            if (Wire.IdOf(packet) != (ushort)PacketID.S_EnterMap) return;
            registrationsAtEnterMap = RegistrationCount();
            session.OnDisconnected(Endpoint);
        };
        session.OnConnected(Endpoint);
        session.OnRecvPacket(new C_Handshake { clientVersion = ProtocolVersion.Current }.Write());
        session.OnRecvPacket(new C_CharacterSelect { characterClass = (byte)CharacterClass.Knight }.Write());

        Tick();
        int registrationsAfterTick = RegistrationCount();

        Assert.Equal(1, registrationsAtEnterMap);
        Assert.Empty(_world.Map.Players);
        Assert.Equal(0, registrationsAfterTick);
        Assert.Equal(0, EconomyEntryCount());
    }

    // Registration starts with admission even without any economy use (state stays lazy), survives
    // a map move, and ends with each close.
    [Fact]
    public void Registrations_FollowLiveConnectionsAcrossMovesAndCloses()
    {
        ProbeSession first = JoinAndEnter(CharacterClass.Knight);
        ProbeSession second = JoinAndEnter(CharacterClass.Mage);
        ProbeSession third = JoinAndEnter(CharacterClass.Knight);
        int registrationsAfterJoin = RegistrationCount();
        int entriesAfterJoin = EconomyEntryCount();

        MoveToHuntingGround(first);
        int registrationsAfterMove = RegistrationCount();
        first.OnDisconnected(Endpoint);
        second.OnDisconnected(Endpoint);
        Settle();
        int registrationsAfterTwoCloses = RegistrationCount();
        third.OnDisconnected(Endpoint);
        Settle();
        int registrationsAfterAllCloses = RegistrationCount();

        Assert.Equal(3, registrationsAfterJoin);
        Assert.Equal(0, entriesAfterJoin);
        Assert.Equal(3, registrationsAfterMove);
        Assert.Equal(1, registrationsAfterTwoCloses);
        Assert.Equal(0, registrationsAfterAllCloses);
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

    bool IsInNoMap(int entityId)
        => new[] { MapId.Town, MapId.HuntingGround, MapId.BossRoom, MapId.Ending }
            .All(id => _world.GetMap(id)!.GetPlayer(entityId) == null);

    void EnterPortal(ProbeSession session, int portalId)
        => session.OnRecvPacket(new C_EnterPortal { portalId = (byte)portalId }.Write());

    void SubmitMelee(ProbeSession attacker, EnemyEntity target)
        => attacker.OnRecvPacket(new C_Attack { targetEntityId = target.EntityId, attackerClientTick = (int)(_tick + 1) }.Write());

    // The real Mage attack queues a DeferredImpact and announces its travel time.
    int LaunchProjectile(ProbeSession mage, EnemyEntity target)
    {
        int mark = mage.Packets.Count;
        SubmitMelee(mage, target);
        Tick();
        byte[] launchBytes = Assert.Single(mage.Packets.Skip(mark), bytes => Wire.IdOf(bytes) == (ushort)PacketID.S_ProjectileLaunch);
        S_ProjectileLaunch launch = new();
        launch.Read(new ArraySegment<byte>(launchBytes));
        Assert.True(launch.travelTicks >= 2, "fixture: the projectile must stay in flight for at least two ticks");
        return launch.travelTicks;
    }

    void AssertQueriedState(ProbeSession session, uint revision, int currency, params (int ItemId, int Count)[] occupied)
    {
        int mark = session.Packets.Count;
        session.OnRecvPacket(Wire.InventoryRequest());
        Settle();
        AssertSnapshot(Assert.Single(session.EconomyPacketsSince(mark)), revision, currency, occupied);
    }

    // Read-only observation of the registry's private maps. No product hook exists for these
    // counts; a renamed field fails here loudly instead of passing silently.
    int EconomyEntryCount() => PrivateCollectionCount("_states");

    int RegistrationCount() => PrivateCollectionCount("_registeredSessions");

    int PrivateCollectionCount(string fieldName)
    {
        object registry = _world.Inventory;
        FieldInfo? field = registry.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.True(field != null, $"fixture: the registry field {fieldName} was not found");
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

        internal ProbeSession(GameWorld world) : base(world) { }

        public override void Send(ArraySegment<byte> packet)
        {
            byte[] bytes = packet.ToArray();
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
    // header, ushort id).
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

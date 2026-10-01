using System.Net;
using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Entities;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;
using Shared.Protocol;

namespace GameServer.Tests.Maps;

public class MapPublicationContractTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Entry_PublishesOrderedRosterAndOneJoinPerLiveObserver(bool migration)
    {
        int nextId = 100;
        var source = new GameMap(MapId.Town, () => ++nextId);
        var destination = new GameMap(migration ? MapId.HuntingGround : MapId.Town,
            () => ++nextId, content: new MapContent(7.5f, 4f, new[]
            {
                new EnemySpawnPoint((byte)EnemyKind.Normal, 100f, 0f),
            }));
        var deliveries = new List<Delivery>();
        var first = new RecordingSession("first", destination, deliveries);
        var second = new RecordingSession("second", destination, deliveries);
        var closing = new RecordingSession("closing", destination, deliveries);
        PlayerEntity p1 = destination.AddPlayer(first, new Vector2(-10.5f, 2.25f), PlayerStats.ForClass(CharacterClass.Mage));
        PlayerEntity p2 = destination.AddPlayer(second, new Vector2(12.25f, 3.5f), PlayerStats.ForClass(CharacterClass.Knight));
        PlayerEntity stale = destination.AddPlayer(closing, new Vector2(30f, 0f));
        destination.AddPlayer(null, new Vector2(40f, 0f));
        closing.CloseWithoutWorldCleanup();
        Assert.Contains(stale, destination.Players); // Exercise filters while the closing owner is still present.

        var entrant = new RecordingSession("entrant", migration ? source : destination, deliveries, destination);
        Vector2 spawn = new(7.5f, 4f);
        int expectedHp = PlayerStats.ForClass(CharacterClass.Mage).InitialHp;
        var expected = new List<Delivery>();
        if (migration)
        {
            var sourceObserver = new RecordingSession("source", source, deliveries);
            source.AddPlayer(sourceObserver, new Vector2(-100f, 0f));
            entrant.Enter(CharacterClass.Mage);
            source.Tick(1);
            PlayerEntity player = source.GetPlayer(entrant.EntityId)!;
            player.Hp = expectedHp = 23;
            Portal portal = source.Portals.Single(p => p.Dest == MapId.HuntingGround);
            player.Position = portal.Position;
            spawn = portal.DestSpawn;
            deliveries.Clear();
            entrant.OnRecvPacket(new C_EnterPortal { portalId = portal.PortalId }.Write());
            source.Tick(2);
            expected.Add(new("source", Bytes(new S_PlayerLeave { entityId = entrant.EntityId }.Write())));
            expected.Add(new("entrant", Bytes(new S_MapTransition
            {
                destMapId = (byte)MapId.HuntingGround,
                spawnX = spawn.X,
                spawnY = spawn.Y,
            }.Write())));
        }
        else
        {
            entrant.Enter(CharacterClass.Mage);
            deliveries.Clear(); // Handshake acknowledgement precedes queued world entry.
        }

        byte[] firstRoster = JoinBytes(p1.EntityId, -10.5f, 2.25f, CharacterClass.Mage);
        byte[] secondRoster = JoinBytes(p2.EntityId, 12.25f, 3.5f, CharacterClass.Knight);
        EnemyEntity enemy = Assert.Single(destination.Enemies.Values);
        byte[] enemyRoster = Bytes(new S_EntitySpawn
        {
            entityId = enemy.EntityId,
            entityKind = (byte)EnemyKind.Normal,
            x = 100f,
            y = 0f,
            currentHp = enemy.MaxHp,
            maxHp = enemy.MaxHp,
        }.Write());
        destination.Tick(3);

        if (!migration)
            expected.Add(new("entrant", Bytes(new S_EnterMap
            {
                entityId = entrant.EntityId,
                spawnX = spawn.X,
                spawnY = spawn.Y,
            }.Write())));
        expected.Add(new("entrant", Bytes(new S_PlayerHp
        {
            entityId = entrant.EntityId,
            currentHp = expectedHp,
            maxHp = PlayerStats.ForClass(CharacterClass.Mage).MaxHp,
        }.Write())));
        expected.Add(new("entrant", firstRoster));
        expected.Add(new("entrant", secondRoster));
        expected.Add(new("entrant", enemyRoster));
        byte[] entrantJoin = JoinBytes(entrant.EntityId, spawn.X, spawn.Y, CharacterClass.Mage);
        expected.Add(new("first", entrantJoin));
        expected.Add(new("second", entrantJoin));

        Delivery[] actual = deliveries.Where(d => IsEntryPacket(Id(d.Payload))).ToArray();
        AssertDeliveries(expected, actual);
        Assert.DoesNotContain(deliveries, d => d.Recipient == "closing");
        Assert.Equal(expectedHp, destination.GetPlayer(entrant.EntityId)!.Hp);
        if (migration) Assert.Null(source.GetPlayer(entrant.EntityId));
    }

    [Theory]
    [InlineData(3, AnimState.Hit, AnimState.Attack)]
    [InlineData(1, AnimState.Walk, AnimState.Idle)]
    public void Tick_PublishesAfterLatchUpdate_InAiThenBossOrder_BeforeGravity(
        int latch, AnimState normalAnimation, AnimState bossAnimation)
    {
        var map = new GameMap(MapId.HuntingGround);
        // Insert the boss first: publication order must follow system order, not insertion order.
        EnemyEntity boss = map.SpawnEnemy(EnemyKind.Boss, 200f, 12f, 150);
        EnemyEntity normal = map.SpawnEnemy(EnemyKind.Normal, 100f, 8f, 30);
        normal.HitLatchTicks = boss.HitLatchTicks = latch;
        normal.AttackLatchTicks = boss.AttackLatchTicks = latch;
        var deliveries = new List<Delivery>();
        var first = new RecordingSession("first", map, deliveries);
        var second = new RecordingSession("second", map, deliveries);
        var closing = new RecordingSession("closing", map, deliveries);
        map.AddPlayer(first, new Vector2(-1000f, 0f));
        map.AddPlayer(second, new Vector2(-1001f, 0f));
        map.AddPlayer(closing, new Vector2(-1002f, 0f));
        map.AddPlayer(null, new Vector2(-1003f, 0f));
        closing.CloseWithoutWorldCleanup();
        long tick = Constants.SnapshotTickInterval * 7;
        float expectedNormalX = 100f + normal.Stats.MoveSpeed * Constants.TickDuration;

        map.Tick(tick);

        byte[] normalBytes = StateBytes(normal.EntityId, expectedNormalX, 8f, EnemyState.Patrol, normalAnimation, tick);
        byte[] bossBytes = StateBytes(boss.EntityId, 200f, 12f, EnemyState.Idle, bossAnimation, tick);
        AssertDeliveries(new[]
        {
            new Delivery("first", normalBytes), new Delivery("second", normalBytes),
            new Delivery("first", bossBytes), new Delivery("second", bossBytes),
        }, deliveries.Where(d => Id(d.Payload) == PacketID.S_EntityState).ToArray());
        Assert.Equal(latch - 1, normal.HitLatchTicks);
        Assert.Equal(latch - 1, boss.HitLatchTicks);
        Assert.True(normal.Y < 8f);
        Assert.True(boss.Y < 12f);
        Assert.DoesNotContain(deliveries, d => d.Recipient == "closing");
    }

    [Fact]
    public void Tick_FreezeSuppressesNormalUntilExpiry_ButDoesNotSuppressBoss()
    {
        var map = new GameMap(MapId.HuntingGround);
        EnemyEntity normal = map.SpawnEnemy(EnemyKind.Normal, 100f, 0f, 30);
        EnemyEntity boss = map.SpawnEnemy(EnemyKind.Boss, 200f, 0f, 150);
        var deliveries = new List<Delivery>();
        map.AddPlayer(new RecordingSession("observer", map, deliveries), new Vector2(-1000f, 0f));
        normal.FrozenUntilTick = 2;
        boss.FrozenUntilTick = 100;
        normal.HitLatchTicks = normal.AttackLatchTicks = 3;
        // The production interval is one; there is no reachable non-publication tick to invent here.
        Assert.Equal(1, Constants.SnapshotTickInterval);

        map.Tick(1);
        Delivery first = Assert.Single(deliveries, d => Id(d.Payload) == PacketID.S_EntityState);
        Assert.Equal(StateBytes(boss.EntityId, 200f, 0f, EnemyState.Idle, AnimState.Idle, 1), first.Payload);
        Assert.Equal(3, normal.HitLatchTicks);
        Assert.Equal(100f, normal.X);
        deliveries.Clear();

        map.Tick(2);
        Delivery[] expired = deliveries.Where(d => Id(d.Payload) == PacketID.S_EntityState).ToArray();
        Assert.Equal(2, expired.Length);
        Assert.Equal(StateBytes(normal.EntityId, 100f + normal.Stats.MoveSpeed * Constants.TickDuration,
            0f, EnemyState.Patrol, AnimState.Hit, 2), expired[0].Payload);
        Assert.Equal(StateBytes(boss.EntityId, 200f, 0f, EnemyState.Idle, AnimState.Idle, 2), expired[1].Payload);
        Assert.Equal(0, normal.FrozenUntilTick);
        Assert.Equal(2, normal.HitLatchTicks);
    }

    [Fact]
    public void Tick_DoesNotPublishRemovedNormalOrDeadBoss()
    {
        var map = new GameMap(MapId.HuntingGround);
        EnemyEntity normal = map.SpawnEnemy(EnemyKind.Normal, 100f, 0f, 30);
        EnemyEntity boss = map.SpawnEnemy(EnemyKind.Boss, 200f, 0f, 150);
        var deliveries = new List<Delivery>();
        PlayerEntity player = map.AddPlayer(new RecordingSession("observer", map, deliveries), new Vector2(-1000f, 0f));
        normal.Hp = 0;
        map.HandleEnemyDeath(normal, player.EntityId);
        boss.Hp = 0; // Exercise the boss system's explicit dead guard while still registered.
        deliveries.Clear();

        map.Tick(Constants.SnapshotTickInterval);

        Assert.DoesNotContain(deliveries, d => Id(d.Payload) == PacketID.S_EntityState);
        Assert.Null(map.GetEnemyById(normal.EntityId));
    }

    [Fact]
    public void Migration_CloseBeforeDestinationTick_PublishesNoArrivalOrJoin()
    {
        int nextId = 0;
        var source = new GameMap(MapId.Town, () => ++nextId);
        var destination = new GameMap(MapId.HuntingGround, () => ++nextId);
        var deliveries = new List<Delivery>();
        var entrant = new RecordingSession("entrant", source, deliveries, destination);
        var observer = new RecordingSession("observer", destination, deliveries);
        destination.AddPlayer(observer, Vector2.Zero);
        entrant.Enter(CharacterClass.Knight);
        source.Tick(1);
        int entityId = entrant.EntityId;
        Portal portal = source.Portals.Single(p => p.Dest == MapId.HuntingGround);
        source.GetPlayer(entityId)!.Position = portal.Position;
        entrant.OnRecvPacket(new C_EnterPortal { portalId = portal.PortalId }.Write());
        source.Tick(2);
        Assert.Null(source.GetPlayer(entityId));
        entrant.CloseWithoutWorldCleanup();
        deliveries.Clear();

        destination.Tick(3);

        Assert.Null(destination.GetPlayer(entityId));
        Assert.DoesNotContain(deliveries, d => IsEntryPacket(Id(d.Payload)));
        Assert.DoesNotContain(deliveries, d => d.Recipient == "entrant");
    }

    static bool IsEntryPacket(PacketID id) => id is PacketID.S_EnterMap or PacketID.S_MapTransition
        or PacketID.S_PlayerHp or PacketID.S_PlayerJoin or PacketID.S_EntitySpawn or PacketID.S_PlayerLeave;

    static PacketID Id(byte[] bytes) => (PacketID)(bytes[2] | bytes[3] << 8);
    static byte[] Bytes(ArraySegment<byte> segment) => segment.ToArray();

    static byte[] JoinBytes(int id, float x, float y, CharacterClass characterClass)
        => Bytes(new S_PlayerJoin { entityId = id, spawnX = x, spawnY = y, characterClass = (byte)characterClass }.Write());

    static byte[] StateBytes(int id, float x, float y, EnemyState state, AnimState animation, long tick)
        => Bytes(new S_EntityState
        {
            entityId = id,
            x = x,
            y = y,
            state = (byte)state,
            animState = (byte)animation,
            serverTick = (int)tick,
        }.Write());

    static void AssertDeliveries(IEnumerable<Delivery> expected, Delivery[] actual)
    {
        Delivery[] entries = expected.ToArray();
        Assert.Equal(entries.Length, actual.Length);
        for (int i = 0; i < entries.Length; i++)
        {
            Assert.Equal(entries[i].Recipient, actual[i].Recipient);
            Assert.Equal(entries[i].Payload, actual[i].Payload);
        }
    }

    sealed record Delivery(string Recipient, byte[] Payload);

    sealed class RecordingSession(string name, GameMap map, List<Delivery> deliveries, GameMap? destination = null) : GameSession
    {
        public void Enter(CharacterClass characterClass)
        {
            CompleteHandshakeAndEnter();
            SetCharacterClass((byte)characterClass);
            EnterGameWorldIfReady();
        }

        public void CloseWithoutWorldCleanup() => OnDisconnected(new IPEndPoint(IPAddress.Loopback, 0));
        public override void Send(ArraySegment<byte> segment) => deliveries.Add(new(name, Bytes(segment)));
        protected override GameMap? GetMap() => map;
        protected override GameMap? GetDestMap(MapId _) => destination;
        protected override void RequestWorldClose() { }
        public override void Disconnect() => CloseWithoutWorldCleanup();
    }
}

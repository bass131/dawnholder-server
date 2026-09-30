using System.Numerics;
using Dawnholder.Server.GameServer.Entities;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Tests.Maps;

public sealed class RespawnPlacementTests
{
    static readonly EnemyRespawnPlacement Placement = new(new(-31, 7), new(43, 11));

    sealed class Observer : GameSession
    {
        public List<byte[]> Packets { get; } = new();
        public override void Send(ArraySegment<byte> packet) => Packets.Add(packet.ToArray());
        public override void OnSend(int bytes) { }
        public override void Disconnect() { }

        public S_EntitySpawn[] Spawns() => Packets
            .Where(bytes => (PacketID)BitConverter.ToUInt16(bytes, 2) == PacketID.S_EntitySpawn)
            .Select(bytes => { S_EntitySpawn packet = new(); packet.Read(new(bytes)); return packet; })
            .ToArray();
    }

    // The short countdown isolates placement from delay; separate tests below exercise every real tick.
    static EnemyEntity RespawnNextTick(GameMap map, EnemyEntity dead, long tick)
    {
        map.RemoveEnemy(dead.EntityId);
        map.EnqueueRespawn(dead);
        dead.RespawnTicksRemaining = 1;
        int[] before = map.Enemies.Keys.ToArray();
        map.Tick(tick);
        return Assert.Single(map.Enemies.Values, e => !before.Contains(e.EntityId));
    }

    static void AssertPosition(EnemyEntity enemy, Vector2 expected)
    {
        Assert.Equal(expected.X, enemy.SpawnX);
        Assert.Equal(expected.Y, enemy.SpawnY);
        // Respawn runs after this tick's AI/physics: the newly published position is still the spawn.
        Assert.Equal(expected.X, enemy.X);
        Assert.Equal(expected.Y, enemy.Y);
    }

    [Fact]
    public void InjectedPlacement_PreservesContentSpawn_ThenUsesBothCoordinates()
    {
        MapContent content = new(0, 0, new[] { new EnemySpawnPoint((byte)EnemyKind.Golem, 13, 17) });
        GameMap map = new(Placement, content: content);
        EnemyEntity initial = Assert.Single(map.Enemies.Values);
        AssertPosition(initial, new(13, 17));
        EnemyEntity first = RespawnNextTick(map, initial, 1);
        AssertPosition(first, new(-31, 7));
        EnemyEntity second = RespawnNextTick(map, first, 2);
        AssertPosition(second, new(43, 11));
        AssertPosition(RespawnNextTick(map, second, 3), new(-31, 7));
    }

    [Fact]
    public void ExplicitDefaultPlacement_RemainsZero_InsteadOfSubstitutingProductionDefault()
    {
        GameMap map = new(default(EnemyRespawnPlacement), content: MapContent.Empty);
        EnemyEntity initial = map.SpawnEnemy(EnemyKind.Golem, 17, 19, 60);
        EnemyEntity first = RespawnNextTick(map, initial, 1);
        AssertPosition(first, Vector2.Zero);
        AssertPosition(RespawnNextTick(map, first, 2), Vector2.Zero);
    }

    [Fact]
    public void InterleavedMaps_OwnIndependentFirstLeftAndAlternation()
    {
        GameMap a = new(Placement, content: MapContent.Empty);
        GameMap b = new(new EnemyRespawnPlacement(new(101, 13), new(103, 17)), content: MapContent.Empty);
        EnemyEntity a0 = a.SpawnEnemy(EnemyKind.Golem, 0, 0, 60);
        EnemyEntity b0 = b.SpawnEnemy(EnemyKind.Golem, 0, 0, 60);
        EnemyEntity a1 = RespawnNextTick(a, a0, 1);
        AssertPosition(a1, new(-31, 7));
        EnemyEntity b1 = RespawnNextTick(b, b0, 1);
        AssertPosition(b1, new(101, 13));
        AssertPosition(RespawnNextTick(b, b1, 2), new(103, 17));
        AssertPosition(RespawnNextTick(a, a1, 2), new(43, 11));
    }

    [Fact]
    public void TickingOtherMap_DoesNotConsumePendingRespawnOrCountdown()
    {
        GameMap a = new(Placement, content: MapContent.Empty);
        GameMap b = new(new EnemyRespawnPlacement(new(101, 13), new(103, 17)), content: MapContent.Empty);
        EnemyEntity pendingA = a.SpawnEnemy(EnemyKind.Golem, 0, 0, 71);
        EnemyEntity pendingB = b.SpawnEnemy(EnemyKind.Golem, 0, 0, 83);
        a.RemoveEnemy(pendingA.EntityId);
        a.EnqueueRespawn(pendingA);
        pendingA.RespawnTicksRemaining = 2;
        b.RemoveEnemy(pendingB.EntityId);
        b.EnqueueRespawn(pendingB);
        pendingB.RespawnTicksRemaining = 1;
        b.Tick(1);
        Assert.Empty(a.Enemies);
        Assert.Equal(2, pendingA.RespawnTicksRemaining);
        EnemyEntity spawnedB = Assert.Single(b.Enemies.Values);
        Assert.Equal(83, spawnedB.MaxHp);
        AssertPosition(spawnedB, new(101, 13));
        a.Tick(1);
        Assert.Empty(a.Enemies);
        a.Tick(2);
        EnemyEntity spawnedA = Assert.Single(a.Enemies.Values);
        Assert.Equal(71, spawnedA.MaxHp);
        AssertPosition(spawnedA, new(-31, 7));
        Assert.Same(spawnedB, Assert.Single(b.Enemies.Values));
    }

    [Fact]
    public void SimultaneousExpiry_PublishesReverseQueueOrder_AndTogglesPerGolem()
    {
        GameMap map = new(Placement, content: MapContent.Empty);
        Observer observer = new();
        map.AddPlayer(observer, new Vector2(200, 0));
        EnemyStats statsA = new() { MaxHp = 501, Defense = 3, Attack = 7, MoveSpeed = 1.25f };
        EnemyStats statsB = new() { MaxHp = 502, Defense = 9, Attack = 13, MoveSpeed = 2.75f };
        EnemyEntity a = map.SpawnEnemy(EnemyKind.Golem, 0, 0, 71, statsA);
        EnemyEntity b = map.SpawnEnemy(EnemyKind.Golem, 0, 0, 83, statsB);
        foreach (EnemyEntity dead in new[] { a, b })
        {
            map.RemoveEnemy(dead.EntityId);
            map.EnqueueRespawn(dead);
            dead.RespawnTicksRemaining = 1;
        }
        map.Tick(1);
        S_EntitySpawn[] packets = observer.Spawns();
        Assert.Equal(new[] { 83, 71 }, packets.Select(p => p.maxHp));
        EnemyEntity first = map.Enemies[packets[0].entityId];
        EnemyEntity second = map.Enemies[packets[1].entityId];
        AssertPosition(first, new(-31, 7));
        AssertPosition(second, new(43, 11));
        Assert.Equal(statsB, first.Stats);
        Assert.Equal(statsA, second.Stats);
        Assert.Equal(83, first.Hp);
        Assert.Equal(71, second.Hp);
        Assert.DoesNotContain(a.EntityId, map.Enemies.Keys);
        Assert.DoesNotContain(b.EntityId, map.Enemies.Keys);
        Assert.NotEqual(first.EntityId, second.EntityId);
        AssertPosition(RespawnNextTick(map, first, 2), new(-31, 7));
    }

    [Fact]
    public void NormalRespawn_DoesNotAdvanceGolemAlternation()
    {
        GameMap map = new(Placement, content: MapContent.Empty);
        EnemyEntity golem = map.SpawnEnemy(EnemyKind.Golem, 0, 0, 60);
        EnemyEntity first = RespawnNextTick(map, golem, 1);
        AssertPosition(first, new(-31, 7));
        EnemyEntity normal = map.SpawnEnemy(EnemyKind.Normal, 23, 29, 30);
        AssertPosition(RespawnNextTick(map, normal, 2), new(23, 29));
        AssertPosition(RespawnNextTick(map, first, 3), new(43, 11));
    }

    [Theory]
    [InlineData(EnemyKind.Normal, 100, 17f, 19f)]
    [InlineData(EnemyKind.Golem, 120, -31f, 7f)]
    public void QueuedDeath_FirstDecrementOccursInDeathTick_ThenPublishesOneFreshFullHpSpawn(
        EnemyKind kind, int delay, float expectedX, float expectedY)
    {
        GameMap map = new(Placement, content: MapContent.Empty);
        Observer first = new();
        Observer second = new();
        map.AddPlayer(first, new Vector2(200, 0));
        map.AddPlayer(second, new Vector2(210, 0));
        EnemyStats stats = new() { MaxHp = 999, Defense = 11, Attack = 23, MoveSpeed = 1.75f,
            PatrolRange = 3, AggroRange = 4, AggroOnSight = false };
        EnemyEntity dead = map.SpawnEnemy(kind, 17, 19, 87, stats);
        dead.X = 55;
        dead.Y = 57;
        dead.Hp = 0;
        map.EnqueueJob(() => map.HandleEnemyDeath(dead, killerEntityId: 0));
        map.Tick(1);
        Assert.Equal(delay - 1, dead.RespawnTicksRemaining);
        Assert.Empty(map.Enemies);
        for (int tick = 2; tick < delay; tick++) map.Tick(tick);
        Assert.Empty(map.Enemies);
        Assert.Empty(first.Spawns());
        Assert.Empty(second.Spawns());
        map.Tick(delay);
        EnemyEntity spawned = Assert.Single(map.Enemies.Values);
        Assert.NotEqual(dead.EntityId, spawned.EntityId);
        Assert.Equal(kind, spawned.Kind);
        Assert.Equal(87, spawned.Hp);
        Assert.Equal(87, spawned.MaxHp); // MaxHp is carried separately from stats.MaxHp=999.
        Assert.Equal(stats, spawned.Stats);
        AssertPosition(spawned, new(expectedX, expectedY));
        foreach (Observer observer in new[] { first, second })
        {
            S_EntitySpawn packet = Assert.Single(observer.Spawns());
            Assert.Equal(spawned.EntityId, packet.entityId);
            Assert.Equal((byte)kind, packet.entityKind);
            Assert.Equal(expectedX, packet.x);
            Assert.Equal(expectedY, packet.y);
            Assert.Equal(87, packet.currentHp);
            Assert.Equal(87, packet.maxHp);
        }
        map.Tick(delay + 1);
        Assert.Single(map.Enemies);
        Assert.Single(first.Spawns());
        Assert.Single(second.Spawns());
    }
}

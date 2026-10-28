using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Entities;
using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Maps;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Tests.Maps;

// 세 맵 재설계(goal 2026-10-24-level-redesign) PR1에서 GameWorld 맵 factory가 재출현 배치를 고르는 연결.
// MapLayoutRespawnContractTests는 사냥터·보스방 데이터가 모두 있는 월드를 본다. 여기서는 맵 데이터에 그 맵 키가 없는
// 기본 생성 분기, 공개 맵, 거부된 포탈 요청과 복사본 재생성 뒤의 교대 상태, provider 필수 계약을 본다.
// 배치는 GameMap에 넣지 않고 실제 수신·포탈·월드 틱이 만든 맵만 관찰한다. 기대 좌표는 승인 요구의 리터럴이다.
[Collection("GameWorldRegistryTests")]
public sealed class MapLayoutFactoryContractTests
{
    // 데이터가 없는 맵에는 content 적이 없어 재출현시킬 골렘을 입력으로 하나 넣는다. 이 위치는 기대값이 아니다.
    const float SeededGolemX = 29f;

    // 사냥터에 없는 포탈 번호. 서버는 이 요청을 버린다.
    const int UnknownPortalId = 99;

    [Fact]
    public void HuntingGroundCopies_WithoutMapDataEntry_AlternateHuntingGroundPlacementPerCopy()
    {
        using InstanceMapTestWorld world = new(TownOnlyMapData());
        InstanceProbeSession solo = world.Join();
        InstanceProbeSession leader = world.Join();
        InstanceProbeSession member = world.Join();
        int partyId = world.FormParty(leader, member);
        GameMap soloCopy = world.MoveThroughPortal(solo, portalId: 1);
        GameMap partyCopy = world.MoveThroughPortal(leader, portalId: 1);
        Assert.Equal(InstanceKey.ForSolo(solo.EntityId), soloCopy.InstanceKey);
        Assert.Equal(InstanceKey.ForParty(partyId), partyCopy.InstanceKey);
        Assert.NotSame(soloCopy, partyCopy);

        // 키가 없으면 content 없이 생성되므로 처음에는 적이 없다.
        Assert.Empty(soloCopy.Enemies);
        Assert.Empty(partyCopy.Enemies);

        EnemyEntity soloFirst = RespawnNextTick(world, soloCopy, SeedGolem(soloCopy));
        AssertRespawnedAt(soloFirst, new Vector2(26f, 0f));

        // 혼자 복사본이 왼쪽 차례를 쓴 뒤에도 파티 복사본은 자기 첫 차례인 왼쪽에서 시작한다.
        EnemyEntity partyFirst = RespawnNextTick(world, partyCopy, SeedGolem(partyCopy));
        AssertRespawnedAt(partyFirst, new Vector2(26f, 0f));
        AssertRespawnedAt(RespawnNextTick(world, soloCopy, soloFirst), new Vector2(32f, 0f));
        AssertRespawnedAt(RespawnNextTick(world, partyCopy, partyFirst), new Vector2(32f, 0f));
    }

    // 보존: 사냥터가 아닌 복사본은 데이터 키가 없어도 기존 기본 배치를 쓴다.
    [Fact]
    public void BossRoomCopy_WithoutMapDataEntry_KeepsDefaultPlacement()
    {
        using InstanceMapTestWorld world = new(TownOnlyMapData());
        InstanceProbeSession solo = world.Join();
        world.MoveThroughPortal(solo, portalId: 1);
        world.UnlockBossRoom(solo.EntityId);
        GameMap bossRoom = world.MoveThroughPortal(solo, portalId: 1);
        Assert.Equal(InstanceKey.ForSolo(solo.EntityId), bossRoom.InstanceKey);
        Assert.Empty(bossRoom.Enemies);

        EnemyEntity first = RespawnNextTick(world, bossRoom, SeedGolem(bossRoom));
        AssertRespawnedAt(first, new Vector2(-8.5f, 0f));
        AssertRespawnedAt(RespawnNextTick(world, bossRoom, first), new Vector2(9.5f, 0f));
    }

    // 보존: 월드 생성 때 만드는 공개 맵도 사냥터가 아니므로 기본 배치를 쓴다.
    [Fact]
    public void TownPublicMapFromWorld_KeepsDefaultPlacement()
    {
        using InstanceMapTestWorld world = new(TownOnlyMapData());
        GameMap town = world.Town;
        Assert.Null(town.InstanceKey);

        EnemyEntity first = RespawnNextTick(world, town, SeedGolem(town));
        AssertRespawnedAt(first, new Vector2(-8.5f, 0f));
        AssertRespawnedAt(RespawnNextTick(world, town, first), new Vector2(9.5f, 0f));
    }

    // 교대 차례는 복사본마다의 상태다. 버려진 요청은 그 상태를 건드리지 않고, 정리된 복사본의 차례는 새 복사본에 남지 않는다.
    [Fact]
    public void HuntingGroundCopy_KeepsTurnAfterRejectedPortal_AndRecreatedCopyStartsLeft()
    {
        using InstanceMapTestWorld world = new(TownOnlyMapData());
        InstanceProbeSession solo = world.Join();
        GameMap copy = world.MoveThroughPortal(solo, portalId: 1);
        EnemyEntity first = RespawnNextTick(world, copy, SeedGolem(copy));
        AssertRespawnedAt(first, new Vector2(26f, 0f));

        solo.OnRecvPacket(new C_EnterPortal { portalId = UnknownPortalId }.Write());
        world.Settle();
        Assert.Same(copy, solo.CurrentMap);
        AssertRespawnedAt(RespawnNextTick(world, copy, first), new Vector2(32f, 0f));

        world.MoveThroughPortal(solo, portalId: 2);
        Assert.False(world.IsStillTicked(copy));
        GameMap recreated = world.MoveThroughPortal(solo, portalId: 1);
        Assert.NotSame(copy, recreated);
        Assert.Equal(InstanceKey.ForSolo(solo.EntityId), recreated.InstanceKey);
        AssertRespawnedAt(RespawnNextTick(world, recreated, SeedGolem(recreated)), new Vector2(26f, 0f));
    }

    // provider는 필수다. 거부된 생성은 단일 월드 자리를 차지하지 않아 뒤이은 정상 생성이 된다.
    [Fact]
    public void GameWorld_RejectsMissingProvider_WithoutClaimingTheSingleWorld()
    {
        Assert.Throws<ArgumentNullException>(() => new GameWorld(null!));

        using InstanceMapTestWorld world = new(TownOnlyMapData());
        Assert.Same(world.World, GameWorld.Instance);
        Assert.Equal(MapId.Town, world.Town.MapId);
    }

    // 마을만 데이터가 있다. 사냥터·보스방·엔딩은 factory의 키 없는 분기로 만들어진다.
    static IReadOnlyDictionary<MapId, (MapTerrain? Terrain, MapContent? Content)> TownOnlyMapData()
        => new Dictionary<MapId, (MapTerrain?, MapContent?)>
        {
            [MapId.Town] = (null, new MapContent(InstanceMapTestWorld.TownSpawnX, 0f, Array.Empty<EnemySpawnPoint>())),
        };

    static EnemyEntity SeedGolem(GameMap map)
        => map.SpawnEnemy(EnemyKind.Golem, SeededGolemX, 0f, EnemyCatalog.For(EnemyKind.Golem).MaxHp);

    // 짧은 대기로 배치를 지연과 분리한다(MapLayoutRespawnContractTests·RespawnPlacementTests와 같은 고정 방법).
    static EnemyEntity RespawnNextTick(InstanceMapTestWorld world, GameMap map, EnemyEntity dead)
    {
        map.RemoveEnemy(dead.EntityId);
        map.EnqueueRespawn(dead);
        dead.RespawnTicksRemaining = 1;
        int[] before = map.Enemies.Keys.ToArray();
        world.Tick();
        return Assert.Single(map.Enemies.Values, e => !before.Contains(e.EntityId));
    }

    static void AssertRespawnedAt(EnemyEntity enemy, Vector2 expected)
    {
        Assert.Equal(EnemyKind.Golem, enemy.Kind);
        Assert.Equal(expected.X, enemy.SpawnX);
        Assert.Equal(expected.Y, enemy.SpawnY);
        // 재출현은 그 틱의 AI·물리 뒤에 일어나므로 이 시점의 위치는 아직 재출현 지점이다.
        Assert.Equal(expected.X, enemy.X);
        Assert.Equal(expected.Y, enemy.Y);
    }
}

using System.Numerics;
using Dawnholder.Server.GameServer.Entities;
using Dawnholder.Server.GameServer.Maps;
using Shared.GameData;

namespace Dawnholder.Server.GameServer.Tests.Maps;

// 세 맵 재설계(goal 2026-10-24-level-redesign) PR1의 사냥터 한정 골렘 재출현 요구.
// GameWorld가 실제 입장(수신 → 포탈 → 복사본 생성)으로 만든 맵만 관찰한다. 배치를 GameMap에 직접 넣지 않는다.
// 기대 좌표는 승인 좌표표의 리터럴이다. 직접 만든 GameMap의 옛 기본 배치 시험은 RespawnPlacementTests에 그대로 있다.
[Collection("GameWorldRegistryTests")]
public sealed class MapLayoutRespawnContractTests : IDisposable
{
    // 레이아웃 content 입력. 골렘 초기 위치는 content가 정하고 재출현 위치만 맵 배치를 따른다.
    const float HuntingGroundGolemX = 29f;
    const float HuntingGroundNormalX = 40f;

    // 사냥터가 아닌 복사본의 배치 경계를 보려고 보스방 content에도 골렘을 둔다. 보스는 fixture와 같은 자리다.
    const float BossRoomGolemX = 45f;

    // 기존 Normal 재출현 지연(EnemyCatalog 선언, RespawnPlacementTests의 기존 단정과 같은 값).
    const int NormalRespawnTicks = 100;

    readonly InstanceMapTestWorld _fixture = new(LayoutMapData());

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public void HuntingGroundCopyFromWorld_KeepsContentGolem_ThenRespawnsLeftRightLeft()
    {
        InstanceProbeSession solo = _fixture.Join();
        GameMap copy = _fixture.MoveThroughPortal(solo, portalId: 1);
        Assert.Equal(InstanceKey.ForSolo(solo.EntityId), copy.InstanceKey);

        EnemyEntity initial = GolemOf(copy);
        Assert.Equal(29f, initial.SpawnX);
        Assert.Equal(0f, initial.SpawnY);
        EnemyEntity first = RespawnNextTick(copy, initial);
        AssertRespawnedAt(first, new Vector2(26f, 0f));
        EnemyEntity second = RespawnNextTick(copy, first);
        AssertRespawnedAt(second, new Vector2(32f, 0f));
        AssertRespawnedAt(RespawnNextTick(copy, second), new Vector2(26f, 0f));
    }

    [Fact]
    public void SoloAndPartyCopies_KeepIndependentRespawnQueueAndAlternation()
    {
        InstanceProbeSession solo = _fixture.Join();
        InstanceProbeSession leader = _fixture.Join();
        InstanceProbeSession member = _fixture.Join();
        int partyId = _fixture.FormParty(leader, member);
        GameMap soloCopy = _fixture.MoveThroughPortal(solo, portalId: 1);
        GameMap partyCopy = _fixture.MoveThroughPortal(leader, portalId: 1);
        Assert.Equal(InstanceKey.ForSolo(solo.EntityId), soloCopy.InstanceKey);
        Assert.Equal(InstanceKey.ForParty(partyId), partyCopy.InstanceKey);
        Assert.NotSame(soloCopy, partyCopy);

        // 파티 복사본만 틱해도 혼자 복사본의 대기 재출현과 남은 틱은 소비되지 않는다.
        EnemyEntity soloDead = GolemOf(soloCopy);
        QueueShortRespawn(soloCopy, soloDead);
        _fixture.TickOnly(partyCopy);
        Assert.Equal(1, soloDead.RespawnTicksRemaining);
        Assert.DoesNotContain(soloCopy.Enemies.Values, e => e.Kind == EnemyKind.Golem);
        _fixture.TickOnly(soloCopy);
        EnemyEntity soloFirst = GolemOf(soloCopy);
        AssertRespawnedAt(soloFirst, new Vector2(26f, 0f));

        // 혼자 복사본이 왼쪽 차례를 쓴 뒤에도 파티 복사본의 첫 재출현은 왼쪽이다.
        EnemyEntity partyFirst = RespawnNextTick(partyCopy, GolemOf(partyCopy));
        AssertRespawnedAt(partyFirst, new Vector2(26f, 0f));
        AssertRespawnedAt(RespawnNextTick(partyCopy, partyFirst), new Vector2(32f, 0f));
        AssertRespawnedAt(RespawnNextTick(soloCopy, soloFirst), new Vector2(32f, 0f));
    }

    // 보존: 사냥터가 아닌 복사본은 기존 기본 배치 좌(-8.5,0)/우(9.5,0)를 그대로 쓴다.
    [Fact]
    public void BossRoomCopyFromWorld_KeepsExistingDefaultGolemPlacement()
    {
        InstanceProbeSession solo = _fixture.Join();
        _fixture.MoveThroughPortal(solo, portalId: 1);
        _fixture.UnlockBossRoom(solo.EntityId);
        GameMap bossRoom = _fixture.MoveThroughPortal(solo, portalId: 1);

        EnemyEntity first = RespawnNextTick(bossRoom, GolemOf(bossRoom));
        AssertRespawnedAt(first, new Vector2(-8.5f, 0f));
        AssertRespawnedAt(RespawnNextTick(bossRoom, first), new Vector2(9.5f, 0f));
    }

    // 보존: Normal은 자기 content 지점에, 기존 지연 뒤 새 번호로 한 마리만 다시 나오고 골렘은 건드리지 않는다.
    [Fact]
    public void HuntingGroundCopyNormal_RespawnsAtOwnContentSpawn_AfterExistingDelay_WithFreshEntityId()
    {
        InstanceProbeSession solo = _fixture.Join();
        GameMap copy = _fixture.MoveThroughPortal(solo, portalId: 1);
        EnemyEntity golem = GolemOf(copy);
        EnemyEntity normal = Assert.Single(copy.Enemies.Values, e => e.Kind == EnemyKind.Normal);

        normal.Hp = 0;
        copy.EnqueueJob(() => copy.HandleEnemyDeath(normal, killerEntityId: solo.EntityId));
        _fixture.Tick();
        // 첫 감소는 사망 처리와 같은 틱에 일어난다.
        Assert.Equal(NormalRespawnTicks - 1, normal.RespawnTicksRemaining);
        for (int tick = 2; tick < NormalRespawnTicks; tick++) _fixture.Tick();
        Assert.DoesNotContain(copy.Enemies.Values, e => e.Kind == EnemyKind.Normal);
        _fixture.Tick();

        EnemyEntity respawned = Assert.Single(copy.Enemies.Values, e => e.Kind == EnemyKind.Normal);
        Assert.NotEqual(normal.EntityId, respawned.EntityId);
        AssertRespawnedAt(respawned, new Vector2(40f, 0f));
        Assert.Same(golem, GolemOf(copy));
    }

    static IReadOnlyDictionary<MapId, (MapTerrain? Terrain, MapContent? Content)> LayoutMapData()
        => new Dictionary<MapId, (MapTerrain?, MapContent?)>
        {
            [MapId.Town] = (null, new MapContent(InstanceMapTestWorld.TownSpawnX, 0f, Array.Empty<EnemySpawnPoint>())),
            [MapId.HuntingGround] = (null, new MapContent(0f, 0f, new[]
            {
                new EnemySpawnPoint((byte)EnemyKind.Golem, HuntingGroundGolemX, 0f),
                new EnemySpawnPoint((byte)EnemyKind.Normal, HuntingGroundNormalX, 0f),
            })),
            [MapId.BossRoom] = (null, new MapContent(0f, 0f, new[]
            {
                new EnemySpawnPoint((byte)EnemyKind.Boss, InstanceMapTestWorld.BossX, 0f),
                new EnemySpawnPoint((byte)EnemyKind.Golem, BossRoomGolemX, 0f),
            })),
        };

    static EnemyEntity GolemOf(GameMap map) => Assert.Single(map.Enemies.Values, e => e.Kind == EnemyKind.Golem);

    // 짧은 대기는 배치를 지연과 분리한다(RespawnPlacementTests와 같은 고정 방법). 지연 보존은 Normal 시험이 실제 틱으로 본다.
    static void QueueShortRespawn(GameMap map, EnemyEntity dead)
    {
        map.RemoveEnemy(dead.EntityId);
        map.EnqueueRespawn(dead);
        dead.RespawnTicksRemaining = 1;
    }

    EnemyEntity RespawnNextTick(GameMap map, EnemyEntity dead)
    {
        QueueShortRespawn(map, dead);
        int[] before = map.Enemies.Keys.ToArray();
        _fixture.Tick();
        return Assert.Single(map.Enemies.Values, e => !before.Contains(e.EntityId));
    }

    static void AssertRespawnedAt(EnemyEntity enemy, Vector2 expected)
    {
        Assert.Equal(expected.X, enemy.SpawnX);
        Assert.Equal(expected.Y, enemy.SpawnY);
        // 재출현은 그 틱의 AI·물리 뒤에 일어나므로 이 시점의 위치는 아직 재출현 지점이다.
        Assert.Equal(expected.X, enemy.X);
        Assert.Equal(expected.Y, enemy.Y);
    }
}

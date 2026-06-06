using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Maps;
using Shared.GameData;

namespace Dawnholder.Server.GameServer.Tests.Maps;

// 맵 레지스트리 골격 단위 테스트.
//
// **검증 범위**:
//   - 4맵 등록 (_maps.Count == 4)
//   - 각 MapId GetMap 성공 (null X)
//   - Map 프로퍼티(호환용) = Town 반환
//
// **싱글톤 관리**: GameWorld는 싱글톤 — 하나만 허용.
// IClassFixture로 GameWorldFixture를 공유해 xUnit 병렬 실행 충돌을 피함.
// 단, GameWorld는 TickScheduler를 포함하므로 Start() 호출 없이 레지스트리 조회만 테스트.
// (TickScheduler는 통합 테스트 ServerFixture에서 Start/Stop 패턴으로 별도 검증.)
[Collection("GameWorldRegistryTests")]
public class GameWorldRegistryTests : IDisposable
{
    readonly GameWorld _world;

    public GameWorldRegistryTests()
    {
        // 레지스트리 골격만 검증 — 빈 provider (평지+빈 콘텐츠 4맵, GameWorld provider 필수 인자)
        _world = new GameWorld(new Dictionary<MapId, (MapTerrain?, MapContent?)>());
    }

    public void Dispose()
    {
        _world.Stop(); // Instance = null 해제 → 다음 테스트 인스턴스 생성 가능
    }

    [Fact]
    public void Maps_Count_Is_Four()
    {
        // 4맵 (Town / HuntingGround / BossRoom / Ending) 등록 확인.
        // MapId enum 모든 값을 조회해 null 아닌 것이 4개인지 검증.
        int count = Enum.GetValues<MapId>()
            .Count(id => _world.GetMap(id) != null);
        Assert.Equal(4, count);
    }

    [Fact]
    public void GetMap_Town_ReturnsNonNull()
    {
        GameMap? map = _world.GetMap(MapId.Town);
        Assert.NotNull(map);
        Assert.Equal(MapId.Town, map!.MapId);
    }

    [Fact]
    public void GetMap_HuntingGround_ReturnsNonNull()
    {
        GameMap? map = _world.GetMap(MapId.HuntingGround);
        Assert.NotNull(map);
        Assert.Equal(MapId.HuntingGround, map!.MapId);
    }

    [Fact]
    public void GetMap_BossRoom_ReturnsNonNull()
    {
        GameMap? map = _world.GetMap(MapId.BossRoom);
        Assert.NotNull(map);
        Assert.Equal(MapId.BossRoom, map!.MapId);
    }

    [Fact]
    public void GetMap_Ending_ReturnsNonNull()
    {
        GameMap? map = _world.GetMap(MapId.Ending);
        Assert.NotNull(map);
        Assert.Equal(MapId.Ending, map!.MapId);
    }

    [Fact]
    public void Map_Property_Returns_Town()
    {
        // 호환용 Map 프로퍼티 = Town 맵 반환 (GameSession.GetMap() → 플레이어 Town spawn 보존).
        GameMap town = _world.Map;
        Assert.NotNull(town);
        Assert.Equal(MapId.Town, town.MapId);
    }
}

// xUnit Collection: 같은 Collection 안의 테스트는 순차 실행.
// GameWorld 싱글톤이 1개만 허용하므로 병렬 실행 차단.
[CollectionDefinition("GameWorldRegistryTests", DisableParallelization = true)]
public class GameWorldRegistryTestsCollection { }

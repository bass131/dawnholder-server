using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Maps;
using Shared.GameData;

namespace Dawnholder.Server.GameServer.Tests.Maps;

// 맵 레지스트리 골격 단위 테스트.
//
// **검증 범위**:
//   - 공용 맵 둘만 초기 등록, 복사본은 입장 때 생성(goal 만들 것 1·3)
//   - 인스턴스 종류는 공용 GetMap 조회에서 null
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
        // 레지스트리 골격만 검증 — 빈 provider (GameWorld provider 필수 인자)
        _world = new GameWorld(new Dictionary<MapId, (MapTerrain?, MapContent?)>());
    }

    public void Dispose()
    {
        _world.Stop(); // Instance = null 해제 → 다음 테스트 인스턴스 생성 가능
    }

    [Fact]
    public void PublicMaps_Count_Is_Two_AndInstancesStartEmpty()
    {
        int count = Enum.GetValues<MapId>()
            .Count(id => _world.GetMap(id) != null);
        Assert.Equal(2, count);
        Assert.Equal(0, _world.LiveInstanceCount);
        Assert.Equal(new[] { MapId.Town, MapId.Ending }, _world.AllLiveMaps.Select(map => map.MapId));
    }

    [Fact]
    public void GetMap_Town_ReturnsNonNull()
    {
        GameMap? map = _world.GetMap(MapId.Town);
        Assert.NotNull(map);
        Assert.Equal(MapId.Town, map!.MapId);
    }

    [Fact]
    public void GetMap_HuntingGround_ReturnsNull()
    {
        GameMap? map = _world.GetMap(MapId.HuntingGround);
        Assert.Null(map);
    }

    [Fact]
    public void GetMap_BossRoom_ReturnsNull()
    {
        GameMap? map = _world.GetMap(MapId.BossRoom);
        Assert.Null(map);
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

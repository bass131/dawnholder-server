using Shared.GameData;

namespace Dawnholder.Server.GameServer.Tests;

// TerrainBaker 생성 데이터 sanity 검증 (M4.4-01).
// bake 재실행으로 데이터가 갈려도 항상 성립해야 하는 불변식만 검사 —
// 구체 좌표는 레벨 디자인 소관이라 박지 않음 (디자인 변경 = 테스트 회귀 아님).
public class MapTerrainDataTests
{
    // 서버 MapId enum 정합: Town=0 / HuntingGround=1 / BossRoom=2
    public static readonly TheoryData<int> PlayMapIds = new() { 0, 1, 2 };

    [Theory]
    [MemberData(nameof(PlayMapIds))]
    public void GetSolids_PlayMaps_NonEmpty(int mapId)
    {
        Assert.NotEmpty(MapTerrainData.GetSolids(mapId));
    }

    [Theory]
    [MemberData(nameof(PlayMapIds))]
    public void Solids_MinStrictlyLessThanMax(int mapId)
    {
        foreach (TerrainAabb a in MapTerrainData.GetSolids(mapId))
        {
            Assert.True(a.MinX < a.MaxX, $"map {mapId}: MinX {a.MinX} >= MaxX {a.MaxX}");
            Assert.True(a.MinY < a.MaxY, $"map {mapId}: MinY {a.MinY} >= MaxY {a.MaxY}");
        }
    }

    [Theory]
    [MemberData(nameof(PlayMapIds))]
    public void Solids_WithinSaneWorldBounds(int mapId)
    {
        // 좌표 폭주(좌표 변환 결함 등) 조기 검출용 느슨한 경계.
        const float Limit = 10_000f;
        foreach (TerrainAabb a in MapTerrainData.GetSolids(mapId))
        {
            Assert.InRange(a.MinX, -Limit, Limit);
            Assert.InRange(a.MaxX, -Limit, Limit);
            Assert.InRange(a.MinY, -Limit, Limit);
            Assert.InRange(a.MaxY, -Limit, Limit);
        }
    }

    [Theory]
    [MemberData(nameof(PlayMapIds))]
    public void Platforms_RangesValid(int mapId)
    {
        // 발판은 미저작 시 빈 배열이 정상 — 비어있지 않을 때만 불변식 검사.
        foreach (TerrainPlatform p in MapTerrainData.GetPlatforms(mapId))
        {
            Assert.True(p.MinX < p.MaxX, $"map {mapId}: platform MinX {p.MinX} >= MaxX {p.MaxX}");
        }
    }

    [Fact]
    public void UnknownMap_ReturnsEmpty_NotThrow()
    {
        Assert.Empty(MapTerrainData.GetSolids(99));
        Assert.Empty(MapTerrainData.GetPlatforms(99));
    }
}

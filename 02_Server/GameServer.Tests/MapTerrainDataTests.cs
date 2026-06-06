using Shared.GameData;

namespace Dawnholder.Server.GameServer.Tests;

// MapTerrainData(코드 생성 정적 클래스)는 M4.4 Phase 03에서 은퇴.
// 지형 데이터는 terrain.bin 런타임 로드로 전환 (MapDataFile.ReadTerrain).
//
// 본 파일은 MapDataFile round-trip 불변식 검증으로 교체:
//   - 빈 terrain round-trip (지형 없는 맵 = Ending 등)
//   - killPlaneY = NegativeInfinity round-trip (미사용 맵 약속)
//   - MapId enum 값 정합 (Town=0 / HuntingGround=1 / BossRoom=2)
//
// 구체 지형 데이터 테스트는 MapDataFileTests(round-trip/무결성)와 TerrainPhysicsTests(물리 계약)에서 커버.
public class MapTerrainDataReplacementTests
{
    // MapId enum 값 정합 — 코드와 파일명 약속이 일치하는지 회귀 안전망.
    // (bin 파일명 = map_{id}.terrain.bin, id = (int)MapId enum)
    [Fact]
    public void MapId_Town_IsZero()
        => Assert.Equal(0, (int)Dawnholder.Server.GameServer.Maps.MapId.Town);

    [Fact]
    public void MapId_HuntingGround_IsOne()
        => Assert.Equal(1, (int)Dawnholder.Server.GameServer.Maps.MapId.HuntingGround);

    [Fact]
    public void MapId_BossRoom_IsTwo()
        => Assert.Equal(2, (int)Dawnholder.Server.GameServer.Maps.MapId.BossRoom);

    // 빈 terrain round-trip (Ending 맵 등 지형 없는 맵).
    [Fact]
    public void EmptyTerrain_RoundTrip_Stable()
    {
        MapTerrain src = new MapTerrain(
            System.Array.Empty<TerrainAabb>(),
            System.Array.Empty<TerrainPlatform>(),
            killPlaneY: float.NegativeInfinity);

        byte[] bytes = MapDataFile.WriteTerrain(3, src); // id=3 (Ending)
        MapTerrain dst = MapDataFile.ReadTerrain(bytes, 3);

        Assert.Equal(0, dst.Solids.Length);
        Assert.Equal(0, dst.Platforms.Length);
        Assert.Equal(float.NegativeInfinity, dst.KillPlaneY);
    }

    // 솔리드 범위 불변식 — MinX < MaxX, MinY < MaxY.
    [Fact]
    public void Solids_MinStrictlyLessThanMax()
    {
        MapTerrain terrain = new MapTerrain(
            new[]
            {
                new TerrainAabb(-10f, 0f, 10f, 2f),
                new TerrainAabb(5f, 2f, 15f, 4f),
            },
            System.Array.Empty<TerrainPlatform>());

        foreach (TerrainAabb a in terrain.Solids)
        {
            Assert.True(a.MinX < a.MaxX, $"MinX {a.MinX} >= MaxX {a.MaxX}");
            Assert.True(a.MinY < a.MaxY, $"MinY {a.MinY} >= MaxY {a.MaxY}");
        }
    }
}

using System.Numerics;
using Dawnholder.Server.GameServer.Maps;

namespace Dawnholder.Server.GameServer.Tests.Maps;

// M4.2 Phase 02: PortalTable 단위 테스트.
//
// **검증 범위**: PortalTable.GetPortalsFor(MapId) 반환값 정합 + GameMap.Portals 노출.
//   MapSpawnTableTests와 층위 분리 정합:
//     - 본 파일 = PortalTable 자체 (portal 정의 선언 정합) + GameMap.Portals 노출 검증.
//     - 실제 포탈 이동(C_EnterPortal 핸들러) 검증은 Phase 03 담당.
//
// **헌법 #1**: portal 목적지/spawn 좌표는 서버 권위 — 이 테스트가 올바른 값이 선언됐는지 보장.
//   클라는 C_EnterPortal.portalId만 보내고, 서버가 이 테이블로 목적지/spawn을 결정.
//
// **헌법 #3 (Trust Boundary)**: portalId는 클라가 보내는 untrusted 값 (Phase 03에서 검증).
//   본 테스트는 테이블 *정의* 정합만 — 클라 입력 검증은 Phase 03 핸들러 테스트 담당.
public class PortalTableTests
{
    // --- Town ---

    [Fact]
    public void Town_ReturnsOnePortal()
    {
        // Town 우측 끝 → HuntingGround. 포탈 1개.
        IReadOnlyList<Portal> portals = PortalTable.GetPortalsFor(MapId.Town);
        Assert.Single(portals);
    }

    [Fact]
    public void Town_Portal_Dest_IsHuntingGround()
    {
        // 데모 핵심 흐름: Town → HuntingGround → BossRoom 전진.
        Portal p = PortalTable.GetPortalsFor(MapId.Town)[0];
        Assert.Equal(MapId.HuntingGround, p.Dest);
    }

    [Fact]
    public void Town_Portal_PortalId_Is_One()
    {
        // 맵 안에서 첫 번째(유일한) portal = id 1.
        Portal p = PortalTable.GetPortalsFor(MapId.Town)[0];
        Assert.Equal(1, p.PortalId);
    }

    [Fact]
    public void Town_Portal_Position_IsRightEdge()
    {
        // Town 우측 끝 (x=20). 마을 구역 경계.
        // 값 변경 시 이 테스트 즉시 실패 → 의도치 않은 좌표 변경 감지.
        Portal p = PortalTable.GetPortalsFor(MapId.Town)[0];
        Assert.Equal(20f, p.Position.X);
        Assert.Equal(0f, p.Position.Y);
    }

    [Fact]
    public void Town_Portal_DestSpawn_IsHuntingGroundLeftEntry()
    {
        // HuntingGround 좌측 입장 spawn (x=2). Normal enemy(x=10)와 충분한 거리.
        Portal p = PortalTable.GetPortalsFor(MapId.Town)[0];
        Assert.Equal(2f, p.DestSpawn.X);
        Assert.Equal(0f, p.DestSpawn.Y);
    }

    // --- HuntingGround ---

    [Fact]
    public void HuntingGround_ReturnsOnePortal()
    {
        IReadOnlyList<Portal> portals = PortalTable.GetPortalsFor(MapId.HuntingGround);
        Assert.Single(portals);
    }

    [Fact]
    public void HuntingGround_Portal_Dest_IsBossRoom()
    {
        // 전진 흐름 2단계: HuntingGround → BossRoom.
        Portal p = PortalTable.GetPortalsFor(MapId.HuntingGround)[0];
        Assert.Equal(MapId.BossRoom, p.Dest);
    }

    [Fact]
    public void HuntingGround_Portal_DestSpawn_IsBossRoomLeftEntry()
    {
        // BossRoom 좌측 spawn (x=22). Boss(x=30)와 충분한 거리.
        Portal p = PortalTable.GetPortalsFor(MapId.HuntingGround)[0];
        Assert.Equal(22f, p.DestSpawn.X);
        Assert.Equal(0f, p.DestSpawn.Y);
    }

    // --- BossRoom ---

    [Fact]
    public void BossRoom_ReturnsOnePortal()
    {
        IReadOnlyList<Portal> portals = PortalTable.GetPortalsFor(MapId.BossRoom);
        Assert.Single(portals);
    }

    [Fact]
    public void BossRoom_Portal_Dest_IsEnding()
    {
        // 보스 클리어 후 결과 화면으로.
        Portal p = PortalTable.GetPortalsFor(MapId.BossRoom)[0];
        Assert.Equal(MapId.Ending, p.Dest);
    }

    // --- Ending ---

    [Fact]
    public void Ending_ReturnsOnePortal()
    {
        // Ending → Town 루프.
        IReadOnlyList<Portal> portals = PortalTable.GetPortalsFor(MapId.Ending);
        Assert.Single(portals);
    }

    [Fact]
    public void Ending_Portal_Dest_IsTown()
    {
        Portal p = PortalTable.GetPortalsFor(MapId.Ending)[0];
        Assert.Equal(MapId.Town, p.Dest);
    }

    // --- 미등록 MapId ---

    [Fact]
    public void UnknownMapId_ReturnsEmpty()
    {
        // 등록되지 않은 MapId → 빈 목록 반환 (fail-safe).
        // MapSpawnTableTests와 동일 패턴.
        IReadOnlyList<Portal> portals = PortalTable.GetPortalsFor((MapId)99);
        Assert.Empty(portals);
    }

    // --- GameMap.Portals 노출 ---

    [Fact]
    public void GameMap_Town_Portals_MatchesTable()
    {
        // GameMap이 PortalTable에서 portal 목록을 올바르게 초기화했는지.
        // GameMap.Portals == PortalTable.GetPortalsFor(MapId.Town) 정합.
        GameMap map = new GameMap(MapId.Town);
        Assert.Equal(PortalTable.GetPortalsFor(MapId.Town), map.Portals);
    }

    [Fact]
    public void GameMap_HuntingGround_Portals_MatchesTable()
    {
        GameMap map = new GameMap(MapId.HuntingGround);
        Assert.Equal(PortalTable.GetPortalsFor(MapId.HuntingGround), map.Portals);
    }

    [Fact]
    public void GameMap_BossRoom_Portals_HasPortalToEnding()
    {
        GameMap map = new GameMap(MapId.BossRoom);
        // portal 1개 + 목적지 Ending 검증.
        Portal p = Assert.Single(map.Portals);
        Assert.Equal(MapId.Ending, p.Dest);
    }
}

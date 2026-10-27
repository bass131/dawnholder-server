using System.Numerics;
using Dawnholder.Server.GameServer.Maps;

namespace Dawnholder.Server.GameServer.Tests.Maps;

// Town 재배치의 서버 포탈 선행 계약 시험.
// 기대값 원천: goal 2026-10-24-level-redesign Town 선행시험 계약 v1의 요구 리터럴(서버 Town 두 포탈 경계)과
// main 3de484ef의 비Town 행. PortalTable 값을 읽어 기대값을 만들지 않고, 행 전체를 record 값으로 비교한다.
// Town 쪽은 Town 정방향 출발과 HG 역방향 도착만 바뀌고, 그 밖의 포탈 행은 main 그대로여야 한다.
public class TownMapLayoutPortalContractTests
{
    // --- Town 두 포탈 경계 ---

    [Fact]
    public void Town_ForwardPortal_DepartsFromGateToHuntingGroundEntry()
    {
        // Town 성문 (25,0) 출발 → HuntingGround 좌측 입장 (2,0). Town 포탈은 이 한 행뿐이다.
        Portal expected = new(PortalId: 1, Position: new Vector2(25f, 0f),
                              Dest: MapId.HuntingGround, DestSpawn: new Vector2(2f, 0f));

        Assert.Equal(new[] { expected }, PortalTable.GetPortalsFor(MapId.Town));
    }

    [Fact]
    public void HuntingGround_ReversePortal_KeepsDepartureAndArrivesInsideTownGate()
    {
        // HG 역방향 출발 (5,0)은 main 그대로, Town 도착만 성문 안쪽 (21,0)이다.
        Portal expected = new(PortalId: 2, Position: new Vector2(5f, 0f),
                              Dest: MapId.Town, DestSpawn: new Vector2(21f, 0f));

        Assert.Equal(expected, PortalTable.GetPortalsFor(MapId.HuntingGround)[1]);
    }

    [Fact]
    public void Ending_Portal_KeepsTownDefaultArrival()
    {
        // Ending → Town 기본 spawn (0,0)은 Town Player 시작 위치와 같고 main 그대로다.
        Portal expected = new(PortalId: 1, Position: new Vector2(5f, 0f),
                              Dest: MapId.Town, DestSpawn: new Vector2(0f, 0f));

        Assert.Equal(new[] { expected }, PortalTable.GetPortalsFor(MapId.Ending));
    }

    // --- 비Town 행 보존 ---

    [Fact]
    public void EnemyRespawnDefault_KeepsMainGolemPositions()
    {
        // Town 선행 범위는 비Town 골렘의 새 배치를 포함하지 않는다.
        EnemyRespawnPlacement actual = EnemyRespawnPlacement.Default;
        Assert.Equal(new Vector2(-8.5f, 0f), actual.GolemLeft);
        Assert.Equal(new Vector2(9.5f, 0f), actual.GolemRight);
    }

    [Fact]
    public void HuntingGround_ForwardPortal_StaysAtMainValues()
    {
        Portal expected = new(PortalId: 1, Position: new Vector2(25f, 0f),
                              Dest: MapId.BossRoom, DestSpawn: new Vector2(22f, 0f));

        IReadOnlyList<Portal> portals = PortalTable.GetPortalsFor(MapId.HuntingGround);
        Assert.Equal(2, portals.Count);
        Assert.Equal(expected, portals[0]);
    }

    [Fact]
    public void BossRoom_Portals_StayAtMainValues()
    {
        Portal[] expected =
        {
            new(PortalId: 1, Position: new Vector2(35f, 0f), Dest: MapId.Ending, DestSpawn: new Vector2(0f, 0f)),
            new(PortalId: 2, Position: new Vector2(18f, 0f),
                Dest: MapId.HuntingGround, DestSpawn: new Vector2(22f, 0f)),
        };

        Assert.Equal(expected, PortalTable.GetPortalsFor(MapId.BossRoom));
    }
}

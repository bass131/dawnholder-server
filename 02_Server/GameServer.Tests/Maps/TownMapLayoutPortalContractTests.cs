using System.Numerics;
using Dawnholder.Server.GameServer.Maps;

namespace Dawnholder.Server.GameServer.Tests.Maps;

// Town 재배치의 서버 포탈 선행 계약 시험.
// 기대값 원천: goal 2026-10-24-level-redesign Town 선행시험 계약 v1의 요구 리터럴(서버 Town 두 포탈 경계).
// PortalTable 값을 읽어 기대값을 만들지 않고, 행 전체를 record 값으로 비교한다.
// 사냥터·보스방 포탈 행은 목업 v3 적용과 함께 MapLayoutPortalContractTests가 맡는다.
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
    public void HuntingGround_ReversePortal_ArrivesInsideTownGate()
    {
        // HG 역방향은 입구 숲 뒤쪽 (-2,0)에서 출발하고, Town 성문 안쪽 (21,0)에 도착한다.
        Portal expected = new(PortalId: 2, Position: new Vector2(-2f, 0f),
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

    // --- 기본 골렘 배치 보존 ---

    [Fact]
    public void EnemyRespawnDefault_KeepsMainGolemPositions()
    {
        // 사냥터는 자기 배치를 따로 주입받는다. 기본값은 다른 맵과 기존 시험을 위해 그대로 둔다.
        EnemyRespawnPlacement actual = EnemyRespawnPlacement.Default;
        Assert.Equal(new Vector2(-8.5f, 0f), actual.GolemLeft);
        Assert.Equal(new Vector2(9.5f, 0f), actual.GolemRight);
    }
}

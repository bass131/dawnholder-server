using Dawnholder.Tools.HeadlessBot.Scenarios;

namespace Dawnholder.Server.GameServer.Tests.Integration;

/// <summary>
/// M4.2 Phase 05: 맵 전환 end-to-end 통합 테스트 — 회귀 안전망.
///
/// <para>
/// <b>검증 목적 3가지</b>:<br/>
///   1. 4맵 루프 완주 — Town → HuntingGround → BossRoom → Ending → Town 한 사이클.<br/>
///   2. ADR-026 (entityId 유지) — 모든 맵 이동 후 LocalEntityId가 동일.<br/>
///   3. 서버 권위 spawn 좌표 — S_MapTransition.spawnX가 PortalTable 정의와 일치.
/// </para>
///
/// <para>
/// <b>ServerFixture 재사용</b>: M2BasicMovementIntegrationTests.cs에서 정의한
/// ServerFixture (port 0 bind + GameWorld.Start). [Collection("IntegrationTests")]
/// 공유 = 동일 서버 인스턴스 sequential 실행 → GameWorld 싱글톤 위반 방지.
/// </para>
///
/// <para>
/// <b>결정론</b>: MapTransitionScenario는 tick 기반 이동 (Constants.TickIntervalMs),
/// 실시간 sleep 최소화, 서버 권위 좌표만 사용. 매 실행 동일 결과 보장.
/// </para>
///
/// <para>
/// <b>M4.1 baseline 회귀 0 보장</b>: 본 파일은 신규 테스트만 추가.
/// 기존 통과 테스트(221개) 영향 없음.
/// </para>
/// </summary>
[Collection("IntegrationTests")]
public class MapTransitionIntegrationTests
{
    readonly ServerFixture _server;

    public MapTransitionIntegrationTests(ServerFixture server)
    {
        _server = server;
    }

    /// <summary>
    /// 맵 전환 4맵 루프 완주 — Town → HG → BossRoom → Ending → Town.
    ///
    /// <para>
    /// <b>검증 항목</b>:<br/>
    ///   - Result.Success = true (4회 portal 모두 성공)<br/>
    ///   - 각 맵 진입 플래그 (EnteredHuntingGround / EnteredBossRoom / EnteredEnding / ReturnedToTown)<br/>
    ///   - spawn 좌표 검증 (SpawnCoordinatesCorrect = true)
    /// </para>
    /// </summary>
    [Fact]
    public async Task MapTransition_FullLoop_Succeeds()
    {
        MapTransitionScenario.Result r = await MapTransitionScenario.Run(
            "127.0.0.1", _server.Port);

        Assert.True(r.Success, $"4맵 루프 실패: {r.Reason}");
        Assert.True(r.EnteredHuntingGround, "HuntingGround 진입 실패");
        Assert.True(r.EnteredBossRoom, "BossRoom 진입 실패");
        Assert.True(r.EnteredEnding, "Ending 진입 실패");
        Assert.True(r.ReturnedToTown, "Town 복귀 실패");
        Assert.True(r.SpawnCoordinatesCorrect, "서버 권위 spawn 좌표 검증 실패");
    }

    /// <summary>
    /// ADR-026 간접 검증 — entityId는 모든 맵 이동 후 유지.
    ///
    /// <para>
    /// ADR-026: "entityId는 맵 이동 시 유지 — 클라는 LocalEntityId 변경 X."<br/>
    /// 서버는 맵 전환 시 S_MapTransition만 발송 (S_EnterMap 재발송 X). 봇의 LocalEntityId는
    /// 최초 S_EnterMap에서 받은 값 그대로이므로, 4맵 루프 완주 후 EntityId가 초기값과 동일한지 검증.
    /// </para>
    /// </summary>
    [Fact]
    public async Task MapTransition_EntityIdPreserved()
    {
        MapTransitionScenario.Result r = await MapTransitionScenario.Run(
            "127.0.0.1", _server.Port);

        Assert.True(r.Success, $"시나리오 실패 (entityId 검증 전제 조건): {r.Reason}");
        Assert.True(r.EntityId > 0, $"초기 entityId 비정상: {r.EntityId}");
        Assert.True(r.EntityIdPreservedAcrossAllMaps,
            $"entityId가 맵 이동 중 변경됨. ADR-026 위반 — entityId={r.EntityId}");
    }

    /// <summary>
    /// 맵 전환 10회 반복 회귀 — deterministic 재현 (LongRunning Skip).
    ///
    /// <para>
    /// <b>Skip 이유</b>: 10회 × 4맵 portal 이동 = 약 60초 이상 소요.
    /// 자동 회귀에는 Fact 2건으로 충분. 수동 트리거:
    /// <c>dotnet test --filter MapTransition_TenRuns</c>.
    /// </para>
    /// </summary>
    [Fact(Skip = "LongRunning: 10회 반복 맵 전환 회귀 — 수동 트리거. " +
                 "`dotnet test --filter MapTransition_TenRuns_AllSucceed`로 명시 실행.")]
    public async Task MapTransition_TenRuns_AllSucceed()
    {
        int failureCount = 0;
        List<string> failures = new();
        for (int i = 0; i < 10; i++)
        {
            MapTransitionScenario.Result r = await MapTransitionScenario.Run(
                "127.0.0.1", _server.Port);
            if (!r.Success)
            {
                failureCount++;
                failures.Add($"run #{i}: {r.Reason}");
            }
            await Task.Delay(300);
        }
        Assert.True(failureCount == 0,
            $"10회 중 {failureCount}회 실패:\n  " + string.Join("\n  ", failures));
    }
}

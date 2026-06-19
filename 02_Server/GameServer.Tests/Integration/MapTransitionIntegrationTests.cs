using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Quest;
using Dawnholder.Tools.HeadlessBot.Scenarios;

namespace Dawnholder.Server.GameServer.Tests.Integration;

/// <summary>
/// 맵 전환 end-to-end 통합 테스트 — 회귀 안전망.
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
/// <b>결정론</b>: MapTransitionSmoke는 tick 기반 이동 (Constants.TickIntervalMs),
/// 실시간 sleep 최소화, 서버 권위 좌표만 사용. 매 실행 동일 결과 보장.
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
        MapTransitionSmoke.Result r = await MapTransitionSmoke.Run(
            "127.0.0.1", _server.Port,
            seedBossGate: SeedBossGate);

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
        MapTransitionSmoke.Result r = await MapTransitionSmoke.Run(
            "127.0.0.1", _server.Port,
            seedBossGate: SeedBossGate);

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
            MapTransitionSmoke.Result r = await MapTransitionSmoke.Run(
                "127.0.0.1", _server.Port,
                seedBossGate: SeedBossGate);
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

    // ── 헬퍼 ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// BossRoom 게이트 전제조건 시드 — MapTransitionSmoke.Run의 seedBossGate 훅용.
    ///
    /// <para>
    /// <b>왜 시드가 필요한가</b>: BossRoom 진입에는 Q3에서 추가된 40킬 게이트가 걸린다.
    /// 실제 40킬 그라인드는 리스폰 주기(100틱/마리)와 HG 적 수 제한으로 60~90초+ 소요 →
    /// 타임아웃/flaky 위험. 이 테스트의 목적은 4맵 루프 전환 메커니즘(entityId 유지 + spawn
    /// 좌표)이지 게이트 자체가 아니다. 게이트 검증은 BossPortalGateTests(stub) + R2
    /// BossGateSmoke(별도)가 담당한다.
    /// </para>
    ///
    /// <para>
    /// <b>타이밍 정합</b>: EnqueueJob 잡이 완료될 때까지 await → 시나리오가
    /// MoveToPortal/SendEnterPortal로 진행하기 전에 _soloProgress[eid]=40 확정.
    /// MapMigration.Execute(게이트)는 이후 틱에서 GetKillCount=40을 읽어 정당 통과.
    /// OnTick 순서상 map.Tick이 Quest.Tick보다 먼저지만, 시드가 이미 이전 틱에 드레인
    /// 완료이므로 순서 무관.
    /// </para>
    ///
    /// <para>
    /// <b>우회가 아님</b>: 게이트는 서버 권위 카운트(Quest.GetKillCount)를 실제로 읽어
    /// 정당 통과 — 클라이언트 주장 값을 신뢰하지 않는 헌법 §3 신뢰경계 그대로.
    /// </para>
    /// </summary>
    Task SeedBossGate(int entityId)
    {
        TaskCompletionSource tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        GameWorld.Instance.Quest.EnqueueJob(() =>
        {
            for (int i = 0; i < QuestConstants.BossUnlockKillCount; i++)
                GameWorld.Instance.Quest.OnKill(entityId, GameWorld.Instance);
            tcs.SetResult();
        });
        return tcs.Task;
    }
}

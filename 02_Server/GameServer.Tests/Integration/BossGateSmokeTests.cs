using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Quest;
using Dawnholder.Tools.HeadlessBot.Scenarios;

namespace Dawnholder.Server.GameServer.Tests.Integration;

/// <summary>
/// BossRoom 포탈 잠금 게이트(Q3) e2e 통합 테스트 — 회귀 안전망.
///
/// <para>
/// <b>검증 목적 2가지</b>:<br/>
///   1. 거부 경로 — 봇이 killCount=0으로 HG→BossRoom 포탈 시도 시
///      S_PortalLocked(requiredCount=40, currentCount=0)를 받고 S_MapTransition은 오지 않음.<br/>
///   2. 통과 경로 — seedBossGate로 killCount=40 충족 후 재시도 시
///      S_MapTransition(destSpawnX≈22) 수신하여 BossRoom 진입 성공.
/// </para>
///
/// <para>
/// <b>ServerFixture 재사용</b>: M2BasicMovementIntegrationTests.cs에서 정의한
/// [CollectionDefinition("IntegrationTests")] + ServerFixture 공유.
/// 동일 서버 인스턴스 sequential 실행 → GameWorld 싱글톤 위반 방지.
/// </para>
///
/// <para>
/// <b>시드 설계</b>: 실제 40킬 그라인드는 60~90초 이상 소요(리스폰 5s, HG 적 적음) → flaky.
/// seedBossGate delegate가 Party.EnqueueJob을 통해 in-process로 _soloProgress[eid]=40 충족.
/// 게이트는 서버 권위 GetKillCount를 실제 검사 — 클라이언트 주장값 신뢰 X (헌법 §1·§3 정합).
/// </para>
/// </summary>
[Collection("IntegrationTests")]
public class BossGateSmokeTests
{
    readonly ServerFixture _server;

    public BossGateSmokeTests(ServerFixture server)
    {
        _server = server;
    }

    /// <summary>
    /// 거부 + 통과 경로 전체 플로우 — 메인 회귀 안전망.
    ///
    /// <para>
    /// <b>검증 항목</b>:<br/>
    ///   - r.SawPortalLocked = true (killCount=0 시 게이트 발동)<br/>
    ///   - r.RequiredCount == 40 (서버 QuestConstants.BossUnlockKillCount SSOT)<br/>
    ///   - r.CurrentCount == 0 (봇 킬카운트 초기값)<br/>
    ///   - r.EnteredBossRoom = true (killCount=40 시드 후 재시도 성공)<br/>
    ///   - r.Success = true
    /// </para>
    /// </summary>
    [Fact]
    public async Task BossGate_DenyThenAllow_Succeeds()
    {
        BossGateSmoke.Result r = await BossGateSmoke.Run(
            "127.0.0.1", _server.Port,
            seedBossGate: SeedBossGate);

        Assert.True(r.SawPortalLocked, $"killCount=0에서 S_PortalLocked 미수신 — 게이트 미발동. reason={r.Reason}");
        Assert.Equal(40, r.RequiredCount);
        Assert.Equal(0, r.CurrentCount);
        Assert.True(r.EnteredBossRoom, $"killCount=40 시드 후 BossRoom 진입 실패. reason={r.Reason}");
        Assert.True(r.Success, $"시나리오 실패: {r.Reason}");
    }

    /// <summary>
    /// 거부 경로 단독 — killCount=0에서 S_PortalLocked만 확인 (seedBossGate 없음).
    ///
    /// <para>
    /// BossGateSmoke.Run(seedBossGate=null)은 거부 경로까지만 검증하고 성공 반환.
    /// 이 테스트는 게이트 발동 자체에만 집중 — 통과 경로는 DenyThenAllow가 커버.
    /// </para>
    /// </summary>
    [Fact]
    public async Task BossGate_DenyOnly_PortalLockedReceived()
    {
        BossGateSmoke.Result r = await BossGateSmoke.Run(
            "127.0.0.1", _server.Port,
            seedBossGate: null);

        Assert.True(r.Success, $"거부 경로 시나리오 실패: {r.Reason}");
        Assert.True(r.SawPortalLocked, "S_PortalLocked 미수신");
        Assert.Equal(40, r.RequiredCount);
        Assert.Equal(0, r.CurrentCount);
        // seedBossGate 없으므로 BossRoom 진입은 없음.
        Assert.False(r.EnteredBossRoom, "seedBossGate 없이 BossRoom 진입 — 게이트 결함");
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// BossRoom 게이트 전제조건 시드.
    ///
    /// <para>
    /// Party.EnqueueJob으로 QuestConstants.BossUnlockKillCount(40)회 OnKill 적립.
    /// TaskCompletionSource로 잡 완료를 await → 시나리오가 SendEnterPortal로
    /// 진행하기 전에 _soloProgress[eid]=40 확정. 게이트는 이후 틱에서
    /// GetKillCount=40을 읽어 정당 통과.
    /// </para>
    ///
    /// <para>
    /// <b>우회가 아님</b>: 게이트는 서버 권위 카운트를 실제 검사 — 클라이언트 주장값 신뢰 X.
    /// </para>
    /// </summary>
    Task SeedBossGate(int entityId)
    {
        TaskCompletionSource tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        GameWorld.Instance.Party.EnqueueJob(() =>
        {
            for (int i = 0; i < QuestConstants.BossUnlockKillCount; i++)
                GameWorld.Instance.Party.OnKill(entityId, GameWorld.Instance);
            tcs.SetResult();
        });
        return tcs.Task;
    }
}

using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Quest;
using Dawnholder.Tools.HeadlessBot.Scenarios;

namespace Dawnholder.Server.GameServer.Tests.Integration;

/// <summary>
/// 파티 + 공유 퀘스트 킬카운트 e2e 통합 테스트 — 회귀 안전망.
///
/// <para>
/// <b>검증 목적</b>:<br/>
///   1. 파티 결성(wire e2e) — 봇A가 봇B 초대 → 수락 → 양측 S_PartyUpdate(partyId>0, 양 멤버 포함).<br/>
///   2. Cross-map 이동 — A만 Town→HG, B는 Town 잔류.<br/>
///   3. 공유 킬카운트(시드) — in-process OnKill 2회 → 양측 S_QuestUpdate(currentCount>=2).<br/>
///   4. Cross-map 전달 — B가 Town 잔류해도 S_QuestUpdate 수신(GameWorld.SendToEntity 경로).<br/>
///   5. targetCount SSOT — S_QuestUpdate.targetCount==20(QuestConstants.BossUnlockKillCount).<br/>
///   6. 해산 — B disconnect → A가 S_PartyUpdate(partyId==0) 수신.
/// </para>
///
/// <para>
/// <b>설계 (in-process 시드 — 봇 근접전투 제거)</b>:<br/>
/// 봇 근접전투는 위치/serverTick/aggro 타이밍에 민감해 flaky함.
/// R1의 고유 e2e 가치는 "파티 wire + 공유 카운트 cross-map 전달 + 해산"이므로
/// SeedPartyKills가 Party.EnqueueJob으로 OnKill 2회를 직접 적립한다.
/// 이는 실제 킬과 동일한 OnKill 코드 경로(파티면 KillCount++ → 양 멤버 SendQuestUpdate)를 탄다.
/// 20킬 전투 그라인드 검증은 xUnit QuestKillCountTests가 담당.
/// </para>
///
/// <para>
/// <b>ServerFixture 재사용</b>: M2BasicMovementIntegrationTests.cs에서 정의한
/// [CollectionDefinition("IntegrationTests")] + ServerFixture 공유.
/// 동일 서버 인스턴스 sequential 실행 → GameWorld 싱글톤 위반 방지.
/// </para>
/// </summary>
[Collection("IntegrationTests")]
public class PartyQuestSmokeTests
{
    readonly ServerFixture _server;

    public PartyQuestSmokeTests(ServerFixture server)
    {
        _server = server;
    }

    /// <summary>
    /// 파티 결성 + cross-map 이동 + 공유 킬카운트(시드) + cross-map S_QuestUpdate + 해산 전체 플로우.
    ///
    /// <para>
    /// <b>검증 항목</b>:<br/>
    ///   - r.PartyFormed = true (초대→수락→S_PartyUpdate 양측 수신)<br/>
    ///   - r.SharedCountA >= 2 (A=HG, S_QuestUpdate currentCount)<br/>
    ///   - r.SharedCountB >= 2 (B=Town, cross-map 전달 증명)<br/>
    ///   - r.TargetCount == 20 (QuestConstants.BossUnlockKillCount SSOT)<br/>
    ///   - r.Disbanded = true (B disconnect → A가 partyId==0 수신)<br/>
    ///   - r.Success = true
    /// </para>
    /// </summary>
    [Fact]
    public async Task PartyQuest_FormSeedShare_Succeeds()
    {
        PartyQuestSmoke.Result r = await PartyQuestSmoke.Run(
            "127.0.0.1", _server.Port,
            seedPartyKills: SeedPartyKills);

        Assert.True(r.PartyFormed,
            $"파티 결성 실패 — 초대/수락/S_PartyUpdate 플로우 오류. reason={r.Reason}");

        Assert.True(r.SharedCountA >= 2,
            $"probeA(HG) S_QuestUpdate currentCount 미달: expected>=2, actual={r.SharedCountA}. reason={r.Reason}");

        Assert.True(r.SharedCountB >= 2,
            $"probeB(Town) S_QuestUpdate cross-map 전달 실패: expected>=2, actual={r.SharedCountB}. reason={r.Reason}");

        Assert.Equal(QuestConstants.BossUnlockKillCount, r.TargetCount);

        Assert.True(r.Disbanded,
            $"파티 해산 실패 — B disconnect 후 A가 S_PartyUpdate(partyId==0) 미수신. reason={r.Reason}");

        Assert.True(r.Success, $"시나리오 실패: {r.Reason}");
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 파티 공유 킬카운트 시드.
    ///
    /// <para>
    /// Party.EnqueueJob으로 OnKill 2회 적립. 파티가 존재하면 OnKill은
    /// PartyState.KillCount++ 후 양 멤버에게 S_QuestUpdate를 발송한다.
    /// TaskCompletionSource로 잡 완료를 await → 시나리오가 WaitForQuestCount로
    /// 진행하기 전에 서버 KillCount=2 확정.
    /// </para>
    ///
    /// <para>
    /// <b>우회가 아님</b>: 실제 킬과 동일한 OnKill 코드 경로를 탄다.
    /// 봇 근접전투 없이 flaky 요소(위치/serverTick/aggro)를 제거한 결정론 재현.
    /// </para>
    /// </summary>
    Task SeedPartyKills(int entityId)
    {
        TaskCompletionSource tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        GameWorld.Instance.Party.EnqueueJob(() =>
        {
            for (int i = 0; i < 2; i++)
                GameWorld.Instance.Party.OnKill(entityId, GameWorld.Instance);
            tcs.SetResult();
        });
        return tcs.Task;
    }
}

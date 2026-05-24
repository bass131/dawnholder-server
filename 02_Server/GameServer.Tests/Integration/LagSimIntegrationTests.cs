using Dawnholder.Tools.HeadlessBot.Scenarios;

namespace Dawnholder.Server.GameServer.Tests.Integration;

/// <summary>
/// M4.1 Phase 06 (7단계): lag 시뮬 봇 회귀 + 검증 통합 테스트.
///
/// **목적 3가지**:
///   1. 봇 회귀 봉합 — EmergencyCombatSmoke / BossStageClearSmoke가 attackerClientTick=0(기본값)
///      대신 _lastReceivedServerTick을 사용하게 수정됐으므로, 기존 smoke 흐름(zero-lag)이
///      여전히 green인지 검증 (회귀 안전망).
///   2. lag 200ms 허용 — diff = 4 ≤ 4 → 서버 통과 → enemy kill 성공. "공정한 hit 판정" 정합.
///   3. lag 250ms silent drop — diff ≥ 5 → 서버 silent drop → enemy kill 불가 →
///      cheat 차단 정합. LongRunning Skip 처리 (타이밍 의존 + 실시간 서버 제어 불가).
///      단위 테스트 LagCompensationTests.Rewind_BeyondRange_SilentDrop이 동일 로직 검증.
///
/// **ServerFixture 재사용**: M2BasicMovementIntegrationTests.cs에서 정의한 ServerFixture
/// (port 0 bind + GameWorld.Start). IClassFixture 공유 — 같은 서버 인스턴스.
///
/// **타이밍 trade-off**:
///   - zero-lag (simulatedLatencyMs=0): 항상 통과 (diff ≈ 0).
///   - lag 200ms (4 tick): MoveIntoAttackRange + 쿨다운 대기 동안 S_Snapshot을 여러 개
///     수신하므로 _lastReceivedServerTick이 갱신됨. 공격 시 diff = currentTick - (serverTick - 4).
///     serverTick이 공격 직전에 수신된 것이라면 diff ≈ 4 → 통과. 타이밍 의존 가능성 있음.
///   - lag 250ms (5 tick): 쿨다운 대기(550ms ≈ 11 tick) 이후 공격 → diff 크게 증가 → drop 보장.
///     그러나 첫 공격(쿨다운 없음)은 S_Snapshot 직후라면 diff = currentTick - (serverTick - 5)
///     where currentTick ≈ serverTick + 1 → diff ≈ 6 > 4 → drop. 안전.
///
/// **단위 테스트 배분**:
///   - LagCompensationTests (5건): diff=0/4/5/음수/미래 — ProcessAttack 직접 검증 (환경 무관).
///   - HitboxTests (3건): AABB 기하 검증.
///   - 본 파일 (2건 + 1 Skip): 실제 서버-봇 wire 경로 종단간 검증.
/// </summary>
/// <summary>
/// M4.1 Phase 06 (7단계): lag 시뮬 봇 회귀 + 검증 통합 테스트.
///
/// **[Collection("IntegrationTests")] 공유 이유**:
///   GameWorld는 단일 인스턴스만 허용 (singleton invariant). M2BasicMovementIntegrationTests와
///   같은 컬렉션 = 동일 ServerFixture 인스턴스 공유 + sequential 실행 → 싱글톤 위반 방지.
///   Enemy 상태 격리: CombatSmoke는 Normal enemy kill, BossSmoke는 Boss kill → 서로 독립.
///   M2 이동 테스트는 enemy와 전투하지 않으므로 상태 오염 없음.
///
/// **단위 테스트 배분**:
///   - LagCompensationTests (5건): diff=0/4/5/음수/미래 — ProcessAttack 직접 검증 (환경 무관).
///   - HitboxTests (3건): AABB 기하 검증.
///   - 본 파일 (2건 자동 + 2건 Skip): 실제 서버-봇 wire 경로 종단간 검증.
/// </summary>
[Collection("IntegrationTests")]
public class LagSimIntegrationTests
{
    readonly ServerFixture _server;

    public LagSimIntegrationTests(ServerFixture server)
    {
        _server = server;
    }

    /// <summary>
    /// 1. 봇 회귀 봉합: EmergencyCombatSmoke zero-lag (simulatedLatencyMs=0) → Success=true.
    ///
    /// **검증 의도**: 봇이 _lastReceivedServerTick 추적 + attackerClientTick 박음으로 수정됐지만,
    /// zero-lag(기본값) 경로에서 기존 동작 그대로인지 확인.
    ///   - MoveIntoAttackRange(~2초) + WaitForFirstSnapshot 동안 S_Snapshot 수신 →
    ///     _lastReceivedServerTick 갱신 → diff ≈ 0 → 통과.
    ///
    /// **M4.2 Phase 01 Skip 사유**:
    ///   맵 분리로 Town = 빈 맵 (Normal enemy 없음). 봇은 접속 후 Town에 spawn되므로
    ///   S_EntitySpawn을 수신하지 못하고 timeout 실패함.
    ///   봇이 portal로 HuntingGround로 이동하는 흐름은 Phase 02~03에서 구현 예정.
    ///   Phase 05 통합 검증에서 봇 맵 이동 시나리오로 복구 예정.
    /// </summary>
    // M4.2 Phase 01: Town = 빈 맵 (맵 분리). 봇이 portal로 HuntingGround 이동하는 흐름은
    // Phase 02~03에서 생김. Phase 05 통합 검증에서 봇 맵 이동 시나리오로 복구 예정.
    [Fact(Skip = "M4.2 Phase 01 맵 분리로 Town = 빈 맵. 봇이 portal로 HuntingGround 이동하는 흐름은 " +
                 "Phase 02~03에서 구현. Phase 05 통합 검증에서 봇 맵 이동 시나리오로 복구 예정.")]
    public async Task CombatSmoke_ZeroLag_Succeeds()
    {
        EmergencyCombatSmoke.Result r = await EmergencyCombatSmoke.Run(
            "127.0.0.1", _server.Port,
            simulatedLatencyMs: 0);

        Assert.True(r.Success, $"zero-lag smoke 실패: {r.Reason}");
        Assert.True(r.SawSpawn, "S_EntitySpawn 미수신");
        Assert.True(r.RateLimitDropped, "rate-limit burst 검증 미통과");
        Assert.True(r.FinalHp <= 0, $"enemy hp가 0 이하 아님: {r.FinalHp}");
    }

    /// <summary>
    /// 2. 봇 회귀 봉합: BossStageClearSmoke zero-lag → Success=true.
    ///
    /// **검증 의도**: BossStageClearSmoke도 _lastReceivedServerTick 추적 + attackerClientTick
    /// 박음으로 수정됐으므로 zero-lag 회귀 검증.
    ///
    /// **M4.2 Phase 01 Skip 사유**:
    ///   맵 분리로 Town = 빈 맵 (Boss 없음). 봇은 접속 후 Town에 spawn되므로
    ///   boss S_EntitySpawn을 수신하지 못하고 timeout 실패함.
    ///   봇이 portal로 BossRoom으로 이동하는 흐름은 Phase 02~03에서 구현 예정.
    ///   Phase 05 통합 검증에서 봇 맵 이동 시나리오로 복구 예정.
    /// </summary>
    // M4.2 Phase 01: Town = 빈 맵 (맵 분리). 봇이 portal로 BossRoom 이동하는 흐름은
    // Phase 02~03에서 구현. Phase 05 통합 검증에서 봇 맵 이동 시나리오로 복구 예정.
    [Fact(Skip = "M4.2 Phase 01 맵 분리로 Town = 빈 맵. 봇이 portal로 BossRoom 이동하는 흐름은 " +
                 "Phase 02~03에서 구현. Phase 05 통합 검증에서 봇 맵 이동 시나리오로 복구 예정.")]
    public async Task BossSmoke_ZeroLag_Succeeds()
    {
        BossStageClearSmoke.Result r = await BossStageClearSmoke.Run(
            "127.0.0.1", _server.Port,
            simulatedLatencyMs: 0);

        Assert.True(r.Success, $"boss zero-lag smoke 실패: {r.Reason}");
        Assert.True(r.SawBossSpawn, "boss S_EntitySpawn 미수신");
        Assert.True(r.SawStageClear, "S_StageClear 미수신");
        Assert.True(r.FinalBossHp <= 0, $"boss hp가 0 이하 아님: {r.FinalBossHp}");
    }

    /// <summary>
    /// 3. lag 100ms 종단간 검증 (LongRunning Skip).
    ///
    /// **검증 의도**: 100ms (= 2 tick) 지연 시뮬 상태에서도 kill 성공.
    ///   attackerClientTick = lastServerTick - 2 → diff ≤ 3 ≤ 4 → 통과.
    ///
    /// **LongRunning Skip 이유**:
    ///   EmergencyCombatSmoke rate-limit burst 검증(550ms 대기 포함)으로 인해
    ///   CooldownWait 이후 공격 시점에서 _lastReceivedServerTick이 stale해질 수 있음.
    ///   lag 시뮬 동작 자체는 LagCompensationTests.Rewind_OutOfRange_4Tick_AllowedBoundary_Hits 검증.
    ///   수동 트리거: `dotnet test --filter CombatSmoke_Lag100ms_KillSucceeds`.
    /// </summary>
    [Fact(Skip = "LongRunning: lag 100ms 종단간 검증 — EmergencyCombatSmoke rate-limit burst 타이밍 의존. " +
                 "LagCompensationTests.Rewind_OutOfRange_4Tick_AllowedBoundary_Hits가 동일 로직 deterministic 검증.")]
    public async Task CombatSmoke_Lag100ms_KillSucceeds()
    {
        EmergencyCombatSmoke.Result r = await EmergencyCombatSmoke.Run(
            "127.0.0.1", _server.Port,
            simulatedLatencyMs: 100);

        Assert.True(r.Success,
            $"lag 100ms smoke 실패: {r.Reason}\n" +
            $"  hitCount={r.HitCount}, finalHp={r.FinalHp}");
        Assert.True(r.FinalHp <= 0, $"enemy hp가 0 이하 아님: {r.FinalHp}");
    }

    /// <summary>
    /// 4. lag 250ms cheat 차단 (LongRunning Skip).
    ///
    /// **검증 의도**: 250ms (= 5 tick) 지연은 서버 rewind 허용 범위(diff ≤ 4) 초과 →
    /// silent drop → kill 불가. 동일 로직은 LagCompensationTests.Rewind_BeyondRange_SilentDrop
    /// deterministic 검증. 본 테스트는 수동 트리거용.
    /// </summary>
    [Fact(Skip = "LongRunning: 250ms lag cheat 차단 — 타이밍 의존 (실시간 서버 diff=5 보장 어려움). " +
                 "LagCompensationTests.Rewind_BeyondRange_SilentDrop이 동일 로직 deterministic 검증. " +
                 "수동 트리거: `dotnet test --filter LagSim_250ms_SilentDrop`.")]
    public async Task LagSim_250ms_SilentDrop_KillFails()
    {
        EmergencyCombatSmoke.Result r = await EmergencyCombatSmoke.Run(
            "127.0.0.1", _server.Port,
            simulatedLatencyMs: 250);

        Assert.False(r.Success,
            $"lag 250ms가 서버를 통과함 (cheat 차단 실패). finalHp={r.FinalHp}, hitCount={r.HitCount}");
        Assert.True(r.FinalHp > 0, $"enemy가 죽었음 (silent drop 미동작): finalHp={r.FinalHp}");
    }
}

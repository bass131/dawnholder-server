namespace Shared.GameData;

/// <summary>
/// 게임 전역 상수. 클라/서버 양쪽이 같은 값을 봐야 하는 모든 것.
/// 이 주석이 Unity 측에서 F12 시 그대로 보여야 한다 (ADR-010 검증).
///
/// **헌법 #1 (Server Authority) 정합 규칙**: 이동/물리/공식 상수는 *오직 여기*에서만
/// 정의. 클라가 별도 const 박으면 prediction(Phase 05+) 도입 즉시 무한 drift.
/// </summary>
public static class Constants
{
    /// <summary>서버 시뮬레이션 틱 레이트 (TPS). ADR-004 참조.</summary>
    public const int ServerTickRate = 20;

    /// <summary>틱 간격 (밀리초). 1000 / ServerTickRate.</summary>
    public const int TickIntervalMs = 1000 / ServerTickRate;

    /// <summary>
    /// 틱 간격 (초, float). 서버 시뮬레이션의 dt 단일 출처.
    /// 클라 prediction(Phase 05+)도 같은 값으로 적분해야 drift 0.
    /// </summary>
    public const float TickDuration = 1.0f / ServerTickRate;

    /// <summary>
    /// S_Snapshot 브로드캐스트 주기 (tick 단위). 2 tick = 100ms (10Hz).
    /// 너무 길면 클라 보간 buffer가 빔 → last-known 정지 패턴으로 어색해짐.
    /// </summary>
    public const int SnapshotTickInterval = 2;

    /// <summary>
    /// 단일 패킷 frame 최대 크기 (byte). PacketSession.OnRecv가 length 헤더 상한으로
    /// 사용 — 초과 시 fail-closed disconnect. 헌법 #3 (Trust Boundary) 코드 실현의 핵심 상수.
    /// </summary>
    public const int MaxPacketSize = 4096;

    /// <summary>
    /// 플레이어 공격 commit window 지속 틱 수.
    /// 이 값은 클라이언트 Phase 03 prediction과 동일한 값으로 사용된다.
    ///
    /// 주의: 서버 전용 CombatConstants.AnimLatchTicks(적 시각 latch)와는 *의미가 다름*.
    /// AnimLatchTicks = 적이 Hit/Attack 모션을 화면에 유지하는 최소 틱 수 (시각 안정성 목적).
    /// AttackCommitWindowTicks = 플레이어 공격 동작 중 이동이 잠기는 서버 권위 window (게임플레이 목적).
    /// 현재 둘 다 8이지만 의미가 달라 합치지 않는다.
    /// </summary>
    public const int AttackCommitWindowTicks = 8;

    /// <summary>피격 넉백 초기 수평 속도 (units/s). EnterHitState에서 방향 부호와 함께 적용.</summary>
    public const float KnockbackInitialVx = 7f;

    /// <summary>
    /// 틱당 넉백 감쇠 계수. KnockbackVx에 매 틱 곱해 지수 감쇠.
    /// 0.75^8 ≈ 0.1 → 8틱(= HitState 지속) 후 초기 속도의 10% 이하로 수렴.
    /// </summary>
    public const float KnockbackDecayPerTick = 0.75f;

    /// <summary>
    /// 보스 페이즈 1 공격 예고 틱 (telegraph). 16틱 = 800ms @20TPS.
    /// telegraph 타이밍 단일 출처 (M4.6 Phase 05, 서버 CombatConstants에서 이동).
    /// 플레이어向 공정성 신호라 클라 예고 UI 등이 참조 가능 = 양쪽 공유. 서버 BossStates가 이 값으로 판정.
    /// </summary>
    public const int BossTelegraphTicks = 16;

    /// <summary>
    /// 보스 페이즈 2 공격 예고 틱 (telegraph). 10틱 = 500ms @20TPS — P1보다 짧아 난이도 상승.
    /// telegraph 타이밍 단일 출처 (M4.6 Phase 05) — BossTelegraphTicks(P1)와 한 곳.
    /// </summary>
    public const int BossPhase2TelegraphTicks = 10;
}

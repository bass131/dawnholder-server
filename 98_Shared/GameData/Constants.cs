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
    /// S_Snapshot 브로드캐스트 주기 (tick 단위). 1 tick = 50ms (20Hz).
    /// M4.11 P1: RemotePlayer 보간 궤적 부드러움을 위해 10Hz(=2) → 20Hz(=1)로 상향.
    /// trade-off: 대역폭 2배 증가이나 로컬/발표 규모(≤10명)에서 무시 가능.
    /// </summary>
    public const int SnapshotTickInterval = 1;

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

    /// <summary>
    /// 연속 공격 최소 간격(틱). 서버 rate-limit 권위 판정 + 클라 입력 게이트가 *같은 값*을 쓰는 단일 진실.
    /// commit window(8틱=이동 잠금)보다 길어(10틱=500ms @20TPS) 스윙 종료 후에도 재공격까지 추가 대기.
    /// → "한 번 들어간 공격은 끝까지 커밋" + 유령 스윙(클라 예측-서버 거부 갭) 차단.
    /// 주의: range/damage는 여전히 서버 전용(CombatConstants) — *타이밍*만 공유(commit window와 동형).
    /// </summary>
    public const int AttackCooldownTicks = 10;

    /// <summary>
    /// 썬더볼트 스킬 쿨다운(틱). 평타 쿨다운(AttackCooldownTicks)과 *독립* — 스킬은 별도 쿨다운.
    /// 서버 SkillSystem이 권위 판정(쿨다운 중 발동 silent drop), 클라는 *같은 값*으로 입력 게이트 거울 →
    /// 쿨다운 중 키 입력 시 송신·캐스팅 모션 둘 다 억제(모션만 나가고 서버가 drop하는 불일치 차단).
    /// 40틱 = 2초 @20TPS. 타이밍만 공유 — 박스 크기/데미지는 서버 전용(CombatConstants) 유지.
    /// </summary>
    public const int ThunderboltCooldownTicks = 40;

    /// <summary>
    /// Dash 스킬 쿨다운(틱). ThunderboltCooldownTicks와 *독립* — Knight 전용 쿨다운.
    /// 서버 SkillSystem이 권위 판정, 클라는 *같은 값*으로 입력 게이트 거울(Phase 04+ 연결 시).
    /// 20틱 = 1초 @20TPS. 타이밍만 공유 — lunge 세기/박스/데미지는 서버 전용(CombatConstants) 유지.
    /// </summary>
    public const int DashCooldownTicks = 20;

    /// <summary>
    /// Teleport 스킬 쿨다운(틱). Mage 전용 순간이동 쿨다운.
    /// 서버 SkillSystem이 권위 판정, 클라는 *같은 값*으로 입력 게이트 거울(Phase 06 연결 시).
    /// 30틱 = 1.5초 @20TPS. 타이밍만 공유 — 이동 거리/경계 clamp는 서버 전용(CombatConstants) 유지.
    /// </summary>
    public const int TeleportCooldownTicks = 30;

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

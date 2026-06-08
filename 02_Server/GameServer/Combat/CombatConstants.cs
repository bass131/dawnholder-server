namespace Dawnholder.Server.GameServer.Combat;

// 서버 권위 전투 상수 단일 출처. 헌법 #1 (Server Authority) + 헌법 #2 정합 — 클라가 보지 못함.
//   데미지/range/쿨다운은 서버 단독 판정이라 wire format에 노출되면 안 됨.
//
// 본 상수는 Normal/Boss 공통. 보스 전용 range/damage가 필요해지면 본 클래스에 별도 상수 추가
// (한 파일에 모아두면 형평성 점검 쉬움).
internal static class CombatConstants
{
    // 3.0f units = ground level 2 unit 간격 + 약간의 여유 = 점프 공격 miss 위험 완화.
    public const float AttackRange = 3.0f;

    // AABB 전환으로 ProcessAttack에서 직접 미사용. 박스 크기 산출 참고용 + 옛 dist² 비교 로직 추적용 보존.
    public const float AttackRangeSquared = AttackRange * AttackRange;

    // AABB attack hitbox 크기. AttackRange=3.0f → halfExtent=1.5f → 전체 3×3 unit 박스.
    public const float AttackHalfExtent = AttackRange / 2f;

    public const int BaseDamage = 10;

    // 500ms = 1초에 2회 한도. cheat가 매 frame 공격 보내도 silent drop으로 잘림.
    // PlayerEntity.LastAttackTickMs와 함께 사용 — `Environment.TickCount64 - last < 500`이면 drop.
    public const long AttackCooldownMs = 500;

    // AnimState latch 지속 틱 수.
    //
    // **latch 필요성**: Attack/Hit는 1틱 순간 이벤트. 20TPS에서 단 1번만 보내면
    //   클라가 50ms 윈도우 안에 패킷을 놓칠 수 있음. 최소 N틱 유지해 클라가 확실히 수신.
    //
    // **tick 단위 기반 이유 (헌법 #5 정합)**:
    //   ms 타이머를 tick 루프 안에 박으면 Thread.Sleep/DateTime 의존 발생.
    //   tick 카운터는 순수 정수 감소 — blocking call 0 보장.
    public const int AnimLatchTicks = 8; // Attack/Hit latch 지속 틱 수 (8틱 = 400ms @20TPS)

    // ── 보스 전투 상수 ────────────────────────────────────────────────────────
    //
    // 값은 사용자 확정 정량값 (M4.5 Phase 04 CP-2 명세). 임의 변경 금지.
    // 틱 단위 = ms 환산 시 20TPS 기준 (1틱=50ms).

    /// <summary>보스 기본 공격 데미지. Formulas.ComputeDamage의 baseDamage 인자로 전달.</summary>
    public const int BossBaseDamage = 8;

    /// <summary>보스 공격 AABB half-extent (x/y 동일). 중심 ±2.5f → 전체 5×5 unit.</summary>
    public const float BossAttackHalfExtent = 2.5f;

    // telegraph 예고 틱(P1=16/P2=10)은 98_Shared/Constants.cs로 이동 (M4.6 Phase 05, 단일 출처).
    //   사유: telegraph는 플레이어向 공정성 신호 → 클라 예고 UI 등에 참조 가능 = 양쪽 공유 OK.
    //   나머지(cooldown/damage/range/threshold)는 순수 서버 판정값이라 여기 유지 (least-exposure).

    /// <summary>
    /// 페이즈 1 공격 쿨다운 (틱 단위).
    /// 40틱 = 2초 @20TPS — 판정 완료 후 다음 telegraph 시작까지.
    /// </summary>
    public const int BossPhase1CooldownTicks = 40;

    /// <summary>
    /// 페이즈 2 공격 쿨다운 (틱 단위).
    /// 24틱 = 1.2초 @20TPS — 페이즈 2 전환 후 가속.
    /// </summary>
    public const int BossPhase2CooldownTicks = 24;

    /// <summary>HP ≤ MaxHp * 0.5 이하로 내려가면 페이즈 2 전환. 1회성.</summary>
    public const float BossPhase2HpThreshold = 0.5f;

    /// <summary>보스가 telegraph(예고)를 시작하는 사거리. |player.X - boss.X| ≤ 이 값이면 공격 시작.
    /// BossAttackHalfExtent(2.5)와 같게 — 예고 시작 시점에 공격 AABB가 닿을 거리.</summary>
    public const float BossAttackTriggerRange = 2.5f;

    /// <summary>배회 사이클의 Idle 짧은 dwell (틱). 10틱=0.5초. 배회 종료 후 다음 탐지까지의 숨.
    /// (post-attack dwell은 BossPhase1/2CooldownTicks가 담당 — Idle이 AttackCooldownTicks를 카운트다운.)</summary>
    public const int BossIdlePauseTicks = 10;

    /// <summary>타겟 없을 때 한 번의 Move 배회 지속 틱. 20틱=1초 배회 후 Idle 복귀.</summary>
    public const int BossWanderTicks = 20;
}

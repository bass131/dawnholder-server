namespace Dawnholder.Server.GameServer.Combat;

// 서버 권위 전투 상수 단일 출처. 헌법 #1 (Server Authority) + 헌법 #2 정합 — 클라가 보지 못함.
//   데미지/range/쿨다운은 서버 단독 판정이라 wire format에 노출되면 안 됨.
//
// 본 상수는 Normal/Boss 공통. 보스 전용 range/damage가 필요해지면 본 클래스에 별도 상수 추가
// (한 파일에 모아두면 형평성 점검 쉬움).
internal static class CombatConstants
{
    // ── 근접 평타 (Knight) ────────────────────────────────────────────────────

    // 3.0f units = ground level 2 unit 간격 + 약간의 여유 = 점프 공격 miss 위험 완화.
    public const float AttackRange = 3.0f;

    // AABB 전환으로 ProcessAttack에서 직접 미사용. 박스 크기 산출 참고용 + 옛 dist² 비교 로직 추적용 보존.
    public const float AttackRangeSquared = AttackRange * AttackRange;

    // Knight 평타 AABB 박스 X/Y half-extent. X=사거리(1.5f), Y=층 분리(1.0f, 임시 시작값 Play 튜닝 대상).
    // AttackRange=3.0f → KnightAttackHalfX=1.5f → X 전체 3 unit. Y를 X보다 좁게(1.0f)해 위아래 층 오판정 완화.
    public const float AttackHalfExtent = AttackRange / 2f; // 하위 호환용 — KnightAttackHalfX와 동일값. 새 코드는 KnightAttackHalfX/Y 사용.
    public const float KnightAttackHalfX = AttackRange / 2f; // 1.5f
    public const float KnightAttackHalfY = 1.0f;             // 임시 시작값. Play 튜닝 대상.

    public const int BaseDamage = 10;

    // AttackLungeInitialVx는 98_Shared.Constants로 이전(M4.13 P4 — 클라 replay 공유). 참조는 Constants.AttackLungeInitialVx 사용.

    // 1초에 2회 한도. cheat가 매 frame 공격 보내도 silent drop으로 잘림.
    // 값은 98_Shared 단일 진실(AttackCooldownTicks)에서 역산 — 클라 입력 게이트가 같은 값을 거울로 사용.
    // (타이밍만 공유. range/damage는 위 AttackRange/BaseDamage처럼 서버 전용 유지.)
    public const long AttackCooldownMs = Shared.GameData.Constants.AttackCooldownTicks * Shared.GameData.Constants.TickIntervalMs;

    // ActionGate 쿨다운 tick 통일용. AttackCooldownMs(500ms) → tick 환산 = 10틱 @20TPS.
    // 단일 진실은 98_Shared.Constants.AttackCooldownTicks — 여기서 참조만.
    public const int MeleeCooldownTicks = Shared.GameData.Constants.AttackCooldownTicks;

    // AnimState latch 지속 틱 수.
    //
    // **latch 필요성**: Attack/Hit는 1틱 순간 이벤트. 20TPS에서 단 1번만 보내면
    //   클라가 50ms 윈도우 안에 패킷을 놓칠 수 있음. 최소 N틱 유지해 클라가 확실히 수신.
    //
    // **tick 단위 기반 이유 (헌법 #5 정합)**:
    //   ms 타이머를 tick 루프 안에 박으면 Thread.Sleep/DateTime 의존 발생.
    //   tick 카운터는 순수 정수 감소 — blocking call 0 보장.
    public const int AnimLatchTicks = 8; // Attack/Hit latch 지속 틱 수 (8틱 = 400ms @20TPS)

    // ── Mage 평타 원거리 ──────────────────────────────────────────────────────
    // 임시 시작값. P5 클라 연결 후 Play 튜닝 대상.

    // StunTicks — 빙결 계열 스킬 도입 시 부활. 도착 후 추가 정지 틱(stun-lock) 용도.
    // 현재 호출자 0 (M4.15 P03 — 에너지볼트/번개 stun 제거). 빙결 스킬 시 여기에 재추가.
    // public const int StunTicks = 8;

    // Mage 평타 AABB 박스 X/Y half-extent. X=사거리, Y=층 분리(임시 시작값 Play 튜닝 대상).
    // X=11.0f: ±11 units 사거리. 영호 승인값(2026-06-14, Phase 02).
    // Y=1.0f: 층간격 초과 오판정 제거용. 임시 시작값 Play 튜닝 대상.
    // (옛 MageAttackHalfExtent=8.0f 단일값 → X/Y 분리로 교체.)
    public const float MageAttackHalfX = 11.0f;
    public const float MageAttackHalfY = 1.0f;

    // 투사체 이동 속도 (unit/tick). 거리를 이 값으로 나눠 travelTicks 산출.
    public const float ProjectileSpeedPerTick = 2.0f;

    // travelTicks 최솟값. 0틱 즉시 도착 방지 (발사 연출 최소 보장).
    public const int MinTravelTicks = 2;

    // MaxTravelTicks 제거 (M4.15 Phase 04).
    // 옛 상한(10틱) artifact 제거 — 사거리(MageAttackHalfX)가 비행 거리 bound.
    // 상한이 클라 거리역산 속도를 폭증시키던 원인(먼 거리: dist/travelTicks↑)이라 제거.
    // MinTravelTicks 하한만 유지 (발사 연출 최소 보장).

    // ── 썬더볼트 AoE ──────────────────────────────────────────────────────────
    // 임시 시작값. P5 클라 연결 후 Play 튜닝 대상.

    // 공격자 중심 AABB 박스 X축 절반 크기 (unit). 전방/후방 대칭.
    // 13.0f = ±13 units 박스. 영호 Play 튜닝(2026-06-09: 가로 범위 조금 더 확대).
    public const float ThunderboltBoxHalfX = 13.0f;

    // 공격자 중심 AABB 박스 Y축 절반 크기 (unit). 층 분리 강화(3.0→1.5, 임시 시작값 Play 튜닝 대상).
    // 영호 승인값(2026-06-14, Phase 02).
    public const float ThunderboltBoxHalfY = 1.5f;

    // 썬더볼트 발동 → 낙뢰 도착까지의 지연 틱 수. freeze 지속과 동일.
    public const int LightningDelayTicks = 4; // 4틱 = 200ms @20TPS

    // 썬더볼트 쿨다운 (틱 단위). 98_Shared 단일 진실에서 가져옴 — 클라가 같은 값으로 입력 게이트 거울
    //   (평타 AttackCooldown과 동형, 타이밍만 공유 / 박스·데미지는 서버 전용 유지 = least-exposure).
    // tick 기반 이유: 헌법 #5 — ms 타이머를 tick 루프에 박으면 DateTime 의존. 순수 정수 비교 = blocking 0.
    public const int ThunderboltCooldownTicks = Shared.GameData.Constants.ThunderboltCooldownTicks;

    // ── Teleport 스킬 ─────────────────────────────────────────────────────────
    // 클라이언트는 쿨다운(98_Shared Constants.TeleportCooldownTicks)만 공유 — 거리/경계는 여기.

    // Teleport 이동 거리 (unit). 수평 FacingDir 방향 이동.
    // 15.0→5.0(P07, 1/3 축소)→3.5(P09, 영호 Play 튜닝 — 짧은 점멸 거동).
    public const float TeleportDistance = 3.5f;

    // 수직 텔레포트 최대 발판 탐지 사거리 (unit). 영호 Play 튜닝 (5.0→3.0, 약 1~2층 — 5는 너무 멀리 빨림).
    // 위/아래 방향 모두 이 사거리 안에 발판이 없으면 이동 없음(이펙트는 출력).
    public const float TeleportVerticalRange = 3.0f;

    // Teleport 쿨다운 (틱). 98_Shared 단일 진실에서 가져옴 (DashCooldownTicks와 동형).
    public const int TeleportCooldownTicks = Shared.GameData.Constants.TeleportCooldownTicks;

    // ── Dash 스킬 ─────────────────────────────────────────────────────────────
    // DashSpeed/DashTravelTicks는 98_Shared.Constants로 이전(M4.13 P4 — 클라 replay 공유). 박스(DashBoxHalfX/Y)·데미지는 서버 전용 유지(least-exposure).

    // Dash 경로 AABB 박스 반폭 (unit). 전방 이동 경로를 스윕하는 박스 — X 방향이 핵심.
    // FacingDir 방향으로 쏘는 앞 공간 스캔. DashBoxHalfY=1.0f (층 분리 강화, 1.5→1.0, 임시 시작값 Play 튜닝 대상).
    // 박스 크기는 이동 거리(4.0)와 독립적인 시전 시점 1회성 임팩트 판정.
    // 영호 승인값(2026-06-14, Phase 02).
    public const float DashBoxHalfX = 2.5f;
    public const float DashBoxHalfY = 1.0f;

    // Dash 쿨다운 (틱). 98_Shared 단일 진실에서 가져옴 (ThunderboltCooldownTicks와 동형).
    public const int DashCooldownTicks = Shared.GameData.Constants.DashCooldownTicks;

    // ── 일반몹 공격 (Normal / Golem) ──────────────────────────────────────────
    // 시작값 — Play 튜닝 대상. 보스 상수(BossBaseDamage/BossAttackHalfExtent)와 병치해
    // 형평성 점검 쉽게.

    /// <summary>일반몹 기본 공격 데미지. EnemyStats.Attack(Normal=5/Golem=8)와 합산.</summary>
    public const int NormalBaseDamage = 4;

    /// <summary>일반몹 공격 AABB half-extent (x/y 동일). 보스(2.5)보다 작은 중형 박스.</summary>
    public const float NormalAttackHalfExtent = 1.5f;

    /// <summary>
    /// |dx| ≤ 이 값 + 쿨다운 0 → 즉시 공격. NormalAttackHalfExtent와 같은 값으로
    /// 트리거 거리와 공격 박스를 연결 보장.
    /// </summary>
    public const float NormalAttackTriggerRange = 1.5f;

    /// <summary>일반몹 공격 쿨다운 (틱 단위). 30틱 = 1.5초 @20TPS.</summary>
    public const int NormalAttackCooldownTicks = 30;

    /// <summary>
    /// 슬라임(Normal)의 공격 windup(준비/휘두르기) 틱 수. swing 애니가 짧아 windup 0 — 진입 즉시 타격.
    /// windup = "Attack 진입 ~ 실제 데미지 판정"까지의 지연. 0이면 옛 거동(Enter 즉시 데미지)과 동일.
    /// </summary>
    public const int NormalAttackWindupTicks = 0;

    /// <summary>
    /// 골렘의 공격 windup(준비/휘두르기) 틱 수. 6틱 = 300ms @20TPS.
    /// 골렘은 swing 모션이 길어 보이므로 휘두르기 애니가 진행되는 동안 데미지를 미뤘다가
    /// windup 경과 후 타격 — "애니 끝나고 hit" 체감을 맞춤(플레이테스트 버그 2 봉합).
    /// **튜닝 지점**: swing 애니 길이에 맞춰 영호가 Play에서 조정. AttackLatchTicks(8) 이내로 두면
    ///   클라가 Attack 애니를 유지하는 latch 윈도우 안에서 타격이 떨어짐.
    /// **헌법 #5 정합**: tick 카운터 감소만 — Thread.Sleep/await 없음.
    /// </summary>
    public const int GolemAttackWindupTicks = 6;

    // ── 보스 전투 ─────────────────────────────────────────────────────────────
    //
    // 값은 사용자 확정 정량값. 임의 변경 금지.
    // 틱 단위 = ms 환산 시 20TPS 기준 (1틱=50ms).

    /// <summary>보스 기본 공격 데미지. Formulas.ComputeDamage의 baseDamage 인자로 전달.</summary>
    public const int BossBaseDamage = 8;

    /// <summary>보스 공격 AABB half-extent (x/y 동일). 중심 ±2.5f → 전체 5×5 unit.</summary>
    public const float BossAttackHalfExtent = 2.5f;

    // telegraph 예고 틱(P1=16/P2=10)은 98_Shared/Constants.cs로 이동 (단일 출처).
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

    // ── 엔티티 히트박스 ───────────────────────────────────────────────────────

    // 적·플레이어 피격 판정 AABB half-extent (x/y 동일). 1×1 unit 박스.
    // EnemyEntity.Hitbox / BossStates.ApplyBossAttack playerBox 공통 단일 출처.
    public const float HitboxHalfExtent = 0.5f;

    // ── 적 AI ─────────────────────────────────────────────────────────────────

    // de-aggro 히스테리시스 계수. Chase/BossMove 중 |dx| > AggroRange * DeAggroHysteresis이면 Patrol 복귀.
    // 1.5f = 추격 개시 범위보다 50% 더 벗어나야 이탈 — 진입·이탈 경계 분리로 떨림 방지.
    public const float DeAggroHysteresis = 1.5f;

    // ── Lag-comp rewind 검증 ──────────────────────────────────────────────────

    internal const long MaxRewindTicks = 4; // 200ms @ 20TPS — lag-comp rewind 상한
}

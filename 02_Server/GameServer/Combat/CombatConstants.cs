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
}

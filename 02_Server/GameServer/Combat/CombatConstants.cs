namespace Dawnholder.Server.GameServer.Combat;

// M3 Phase 06 Step 5 (응급 전투 — AttackHandler 사전 의존):
// 서버 권위 전투 상수 단일 출처. 헌법 #1 (Server Authority) 정합 — 클라가 보지 못함
// (헌법 #2 "Protocol is Sacred" 정합 wire에 박힌 약속 X. 데미지/range/쿨다운은
// 서버 단독 판정이라 wire format에 노출되면 안 됨).
//
// **응급 단순화 — Phase 07 보스/M4 본 마감 확장 trade-off**:
//   - `AttackRangeSquared = 3.0² = 9.0` 으로 박음. ground level 2 unit 간격에서 자연
//     수치 (player MoveSpeed=5 정합 도보 1초). `dist² < range²` 패턴으로 sqrt 회피.
//   - `BaseDamage = 10` 고정. 데미지 공식(스탯/방어/크리)은 M4 backlog — 응급은 *덜
//     박더라도 권위·신뢰경계는 지킴* (헌법 #1).
//   - `AttackCooldownMs = 500` rate-limit silent drop 기준. 본 마감은 cheat-flag 별도
//     (Codex MEDIUM #5 정합 — 응답 X = no HP change + no broadcast).
//
// **Phase 07 보스 확장 약속**: 본 상수는 Normal/Boss 공통. 보스 전용 range/damage가 필요해지면
// 본 클래스에 `BossAttackRangeSquared` 같은 별도 상수 추가 (한 파일에 모아두면 형평성 점검 쉬움).
internal static class CombatConstants
{
    // dist² < range² 패턴 — sqrt 회피 (성능 + 정밀도, 표준 패턴).
    // 3.0f units = ground level 2 unit 간격 + 약간의 여유 = 점프 공격 miss 위험 완화
    // (Phase 05 Y mispredict 잔류 — 시연은 지상 공격 위주이지만 range 넉넉히가 안전).
    public const float AttackRange = 3.0f;
    public const float AttackRangeSquared = AttackRange * AttackRange;

    public const int BaseDamage = 10;

    // 500ms = 1초에 2회 한도. cheat가 매 frame 공격 보내도 silent drop으로 잘림.
    // PlayerEntity.LastAttackTickMs와 함께 사용 — `Environment.TickCount64 - last < 500`이면 drop.
    public const long AttackCooldownMs = 500;
}

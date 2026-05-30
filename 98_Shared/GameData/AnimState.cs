namespace Shared.GameData;

// M4.3 Phase 08a: 시각 애니메이션 상태 열거형.
//
// **개념 분리 (핵심)**:
//   이 enum은 "화면에 뭘 그리는가"를 나타냄 — 시각 표현 전용.
//   서버 AI 행동상태 EnemyState(서버 FSM 판단: "AI가 뭘 하려는가")와는 *다른 레이어*.
//
//   예: EnemyState.Patrol → AnimState.Walk (순찰 중엔 걷는 모션)
//       EnemyState.Chase  → AnimState.Walk (추격 중에도 걷는 모션)
//       EnemyState.Idle   → AnimState.Idle
//
//   둘을 합치면 "공격 모션 중엔 AI 상태가 Attack이라 추격 못 함" 같은
//   시각 표현이 게임 로직에 결합되는 문제가 생김. 분리가 정석.
//
// **서버 권위 (헌법 #1)**: 서버가 매 틱 이 enum 값을 계산해 패킷에 실어 보냄.
//   클라이언트는 받은 byte를 이 enum으로 해석해 Animator에 반영만 함.
//   클라이언트가 "지금 공격 중이겠지"를 *추측하지 않음*.
//
// **byte 기반 이유**: S_Snapshot / S_EntityState 패킷에 1바이트로 직렬화.
//   wire에서 (byte)AnimState 캐스트로 박힘.
//
// **stability 약속 (EnemyState.cs 패턴 정합)**:
//   값은 영원히 고정. 새 상태 추가는 5부터 append-only.
//   클라가 이 값을 기반으로 Animator 파라미터를 매핑하므로 값 변경 = breaking change
//   = Protocol.Version bump 의무.
public enum AnimState : byte
{
    Idle   = 0,  // 정지 — 기본 대기 상태
    Walk   = 1,  // 이동 — 수평 이동 중 (vx != 0 또는 Patrol/Chase)
    Jump   = 2,  // 공중 — OnGround=false (플레이어 전용; 적은 현재 미사용)
    Attack = 3,  // 공격 — 공격 수행 틱 + latch 지속
    Hit    = 4,  // 피격 — 피격 틱 + latch 지속
    Death  = 5,  // 사망 — HP <= 0 (latch 없음 — 고정 상태)
}

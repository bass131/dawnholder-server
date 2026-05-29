namespace Dawnholder.Server.GameServer.Combat;

// M4.3 Phase 07: 적 AI 유한 상태 기계(FSM, Finite State Machine) 상태 열거형.
//
// **왜 별도 파일인가?**
//   EnemyKind.cs 패턴 정합 — enum 하나당 파일 하나. 다른 파일에서 명확히 import.
//
// **byte cast 약속**:
//   S_EntityState.state 필드는 byte — 이 enum을 byte로 캐스트해 wire에 박음.
//   PDL byte 필드 = 값 0~255. 현재 3개 상태면 충분. 새 상태 추가 = append-only (안정 ID 보장).
//
// **stability 약속** (EnemyKind.cs 정합):
//   값은 영원히 고정. Idle=0, Patrol=1, Chase=2. 새 상태는 *3부터 순서 append-only*.
//   클라가 S_EntityState.state를 이 enum으로 해석하므로 값 변경 = breaking change = Protocol.Version bump 의무.
public enum EnemyState : byte
{
    Idle   = 0,   // 정지 (초기값 + Boss 전용)
    Patrol = 1,   // 순찰 — SpawnX 중심 ±PatrolRange 왕복
    Chase  = 2,   // 추격 — 타겟 플레이어 방향으로 이동
}

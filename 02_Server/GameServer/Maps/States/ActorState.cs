using Shared.GameData;

namespace Dawnholder.Server.GameServer.Maps.States;

// 행동 State 패턴 추상 베이스 (GPP-06 State).
//
// 왜 PlayerEntity가 아닌 참조 전달: State 클래스가 PlayerEntity 필드를
// 직접 mutate하려면 인자로 받아야 함. "데이터 소유 = 컨테이너" 원칙(§2.2).
//
// 왜 abstract class가 아닌 virtual: 기본 구현이 "no-op"인 Enter/Exit는
// 모든 State가 반드시 재정의하지 않아도 됨. Tick만 필수.
public abstract class ActorState
{
    // AnimState 노출 — StateMachine 외부(GameMap.ComputePlayerAnimState)가
    // 현재 상태의 시각 표현을 읽는 단일 접점.
    public abstract AnimState AnimState { get; }

    // true이면 틱 루프가 이동 입력(inputX, rawJump)을 0으로 강제.
    // 기본 false — 이동 계열 State는 재정의 불필요.
    public virtual bool LocksMovement => false;

    // false이면 피격(EnterHitState)으로 이 상태를 끊을 수 없음(불가침 commit).
    // 기본 true — 대부분의 상태는 피격에 의해 끊어질 수 있음.
    public virtual bool InterruptibleByHit => true;

    // 상태 진입 시 1회. 기본 no-op.
    public virtual void Enter(PlayerEntity player) { }

    // 매 tick 호출. 다음 상태로 전환할 필요가 있으면 비-null 반환.
    // null 반환 = 상태 유지.
    public abstract ActorState? Tick(PlayerEntity player);

    // 상태 이탈 시 1회. 기본 no-op.
    public virtual void Exit(PlayerEntity player) { }
}

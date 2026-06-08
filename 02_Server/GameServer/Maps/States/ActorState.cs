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

    // 상태 진입 시 1회. 기본 no-op.
    public virtual void Enter(PlayerEntity player) { }

    // 매 tick 호출. 다음 상태로 전환할 필요가 있으면 비-null 반환.
    // null 반환 = 상태 유지.
    public abstract ActorState? Tick(PlayerEntity player);

    // 상태 이탈 시 1회. 기본 no-op.
    public virtual void Exit(PlayerEntity player) { }
}

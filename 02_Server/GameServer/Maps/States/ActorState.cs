using Shared.GameData;

namespace Dawnholder.Server.GameServer.Maps.States;

// 행동 State 패턴 추상 베이스 (GPP-06 State). TActor = PlayerEntity 또는 EnemyEntity.
//
// 왜 참조 전달: State 클래스가 actor 필드를 직접 mutate하려면 인자로 받아야 함.
// "데이터 소유 = 컨테이너" 원칙(§2.2).
//
// Enter/Exit 기본 no-op: 모든 State가 반드시 재정의하지 않아도 됨. Tick만 필수.
public abstract class ActorState<TActor>
{
    // AnimState 노출 — StateMachine 외부가 현재 상태의 시각 표현을 읽는 단일 접점.
    public abstract AnimState AnimState { get; }

    // true이면 틱 루프가 이동 입력(inputX, rawJump)을 0으로 강제.
    // 기본 false — 이동 계열 State는 재정의 불필요.
    public virtual bool LocksMovement => false;

    // false이면 피격(EnterHitState)으로 이 상태를 끊을 수 없음(불가침 commit).
    // 기본 true — 대부분의 상태는 피격에 의해 끊어질 수 있음.
    public virtual bool InterruptibleByHit => true;

    // 이 상태에서 지정 행동을 받아들이는가. ActionGate가 상태 허용 여부를 이 단일 접점에서 조회.
    // 기본 true — 이동 계열 State(Idle/Move/Jump)는 모든 행동 허용.
    // Attack/Hit/Death는 false override — commit window·hitstun·사망 중 행동 거부.
    public virtual bool AcceptsAction(ActionKind kind) => true;

    // 상태 진입 시 1회. 기본 no-op.
    public virtual void Enter(TActor actor) { }

    // 매 tick 호출. 다음 상태로 전환할 필요가 있으면 비-null 반환.
    // null 반환 = 상태 유지.
    public abstract ActorState<TActor>? Tick(TActor actor);

    // 상태 이탈 시 1회. 기본 no-op.
    public virtual void Exit(TActor actor) { }
}

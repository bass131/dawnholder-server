using Shared.GameData;

namespace Dawnholder.Server.GameServer.Maps.States;

// 단일 actor의 State를 관장하는 드라이버. TActor = PlayerEntity 또는 EnemyEntity.
//
// 왜 이 클래스가 Exit→Enter 순서를 보장: State가 직접 전환하면
// Enter/Exit 순서가 호출부마다 흩어지는 문제. 드라이버 한 곳에서 순서 고정.
//
// 자기전이(self-transition) 가드: 동일 인스턴스로 ChangeState 호출 시
// Exit/Enter를 스킵. 매 tick 조건이 같아도 이벤트가 중복 발생하지 않음.
public sealed class StateMachine<TActor>
{
    ActorState<TActor> _current;

    public ActorState<TActor> CurrentState => _current;

    // 현재 상태의 시각 표현. ComputePlayerAnimState(이동 계열 분기)가 이 값 사용.
    public AnimState AnimState => _current.AnimState;

    public StateMachine(ActorState<TActor> initialState, TActor actor)
    {
        _current = initialState;
        _current.Enter(actor);
    }

    // 현재 상태를 next로 교체.
    // 자기전이 가드: same instance면 no-op.
    public void ChangeState(ActorState<TActor> next, TActor actor)
    {
        if (ReferenceEquals(_current, next)) return;
        _current.Exit(actor);
        _current = next;
        _current.Enter(actor);
    }

    // tick thread에서 매 tick 호출.
    // State.Tick이 다음 상태를 반환하면 전환.
    public void Tick(TActor actor)
    {
        ActorState<TActor>? next = _current.Tick(actor);
        if (next != null)
            ChangeState(next, actor);
    }
}

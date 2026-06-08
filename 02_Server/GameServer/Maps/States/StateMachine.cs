using Shared.GameData;

namespace Dawnholder.Server.GameServer.Maps.States;

// 단일 PlayerEntity의 이동 계열 State를 관장하는 드라이버.
//
// 왜 이 클래스가 Exit→Enter 순서를 보장: State가 직접 전환하면
// Enter/Exit 순서가 호출부마다 흩어지는 문제. 드라이버 한 곳에서 순서 고정.
//
// 자기전이(self-transition) 가드: 동일 인스턴스로 ChangeState 호출 시
// Exit/Enter를 스킵. 매 tick 조건이 같아도 이벤트가 중복 발생하지 않음.
public sealed class StateMachine
{
    ActorState _current;

    public ActorState CurrentState => _current;

    // 현재 상태의 시각 표현. GameMap.ComputePlayerAnimState(이동 계열 분기)가 이 값 사용.
    public AnimState AnimState => _current.AnimState;

    public StateMachine(ActorState initialState, PlayerEntity player)
    {
        _current = initialState;
        _current.Enter(player);
    }

    // 현재 상태를 next로 교체.
    // 자기전이 가드: same instance면 no-op.
    public void ChangeState(ActorState next, PlayerEntity player)
    {
        if (ReferenceEquals(_current, next)) return;
        _current.Exit(player);
        _current = next;
        _current.Enter(player);
    }

    // tick thread에서 매 tick 호출.
    // State.Tick이 다음 상태를 반환하면 전환.
    public void Tick(PlayerEntity player)
    {
        ActorState? next = _current.Tick(player);
        if (next != null)
            ChangeState(next, player);
    }
}

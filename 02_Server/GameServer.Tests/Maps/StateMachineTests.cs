using System.Numerics;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Maps.States;
using Shared.GameData;

namespace GameServer.Tests.Maps;

/// <summary>
/// StateMachine 드라이버 단위 테스트.
///
/// 검증 대상:
///   1. 초기 상태 Enter가 ctor에서 호출됨
///   2. ChangeState: Exit(현재) → Enter(다음) 순서 보장
///   3. ChangeState 자기전이 가드: 같은 인스턴스 전달 시 Exit/Enter 미호출
///   4. Tick이 State.Tick 반환값으로 ChangeState 발동
///   5. IdleState — Idle 유지 / Move 전환 / Jump 전환
///   6. MoveState — Move 유지 / Idle 전환 / Jump 전환
///   7. JumpState — Jump 유지 / 착지 후 Idle or Move 전환
///   8. PlayerEntity ctor 시 ActionFsm이 Idle로 초기화
/// </summary>
public class StateMachineTests
{
    // ── 호출 추적 State (Enter/Exit 순서 검증용) ────────────────────────────

    sealed class TrackingState : ActorState
    {
        public AnimState _animState;
        public override AnimState AnimState => _animState;

        public int EnterCount;
        public int ExitCount;
        public ActorState? NextState;

        public TrackingState(AnimState anim) => _animState = anim;

        public override void Enter(PlayerEntity p) => EnterCount++;
        public override void Exit(PlayerEntity p)  => ExitCount++;
        public override ActorState? Tick(PlayerEntity p) => NextState;
    }

    static PlayerEntity MakePlayer() =>
        new PlayerEntity(1, Vector2.Zero);

    // ── 1. ctor Enter 호출 ────────────────────────────────────────────────

    [Fact]
    public void Ctor_CallsEnterOnInitialState()
    {
        PlayerEntity p = MakePlayer();
        var initial = new TrackingState(AnimState.Idle);

        _ = new StateMachine(initial, p);

        Assert.Equal(1, initial.EnterCount);
    }

    // ── 2. ChangeState Exit→Enter 순서 ───────────────────────────────────

    [Fact]
    public void ChangeState_CallsExitThenEnter_InOrder()
    {
        PlayerEntity p = MakePlayer();
        var stateA = new TrackingState(AnimState.Idle);
        var stateB = new TrackingState(AnimState.Walk);
        var sm = new StateMachine(stateA, p);

        var order = new List<string>();
        // override를 쓸 수 없으므로 TrackingState 대신 직접 카운트로 순서 추론.
        // A.Exit 후 B.Enter 가 됐으면: A.ExitCount=1 B.EnterCount=1 순서 확인.
        sm.ChangeState(stateB, p);

        Assert.Equal(1, stateA.ExitCount);
        Assert.Equal(1, stateB.EnterCount); // ChangeState에서 1번 → 총 1
        Assert.Equal(1, stateA.EnterCount); // ctor에서 1번
    }

    // Enter/Exit 순서를 직접 기록하는 별도 검증
    [Fact]
    public void ChangeState_ExitBeforeEnter()
    {
        PlayerEntity p = MakePlayer();
        var callLog = new List<string>();

        var stateA = new LoggingState("A", callLog);
        var stateB = new LoggingState("B", callLog);
        var sm = new StateMachine(stateA, p); // A.Enter 기록

        callLog.Clear(); // ctor 로그 초기화
        sm.ChangeState(stateB, p);

        // 순서: A.Exit → B.Enter
        Assert.Equal(new[] { "A.Exit", "B.Enter" }, callLog);
    }

    sealed class LoggingState : ActorState
    {
        readonly string _name;
        readonly List<string> _log;
        public override AnimState AnimState => AnimState.Idle;

        public LoggingState(string name, List<string> log)
        { _name = name; _log = log; }

        public override void Enter(PlayerEntity p) => _log.Add($"{_name}.Enter");
        public override void Exit(PlayerEntity p)  => _log.Add($"{_name}.Exit");
        public override ActorState? Tick(PlayerEntity p) => null;
    }

    // ── 3. 자기전이 가드 ──────────────────────────────────────────────────

    [Fact]
    public void ChangeState_SelfTransition_SkipsExitAndEnter()
    {
        PlayerEntity p = MakePlayer();
        var state = new TrackingState(AnimState.Idle);
        var sm = new StateMachine(state, p);
        int enterAfterCtor = state.EnterCount; // = 1

        sm.ChangeState(state, p); // 자기전이

        Assert.Equal(enterAfterCtor, state.EnterCount); // Enter 추가 호출 없음
        Assert.Equal(0, state.ExitCount);               // Exit 미호출
    }

    // ── 4. Tick → State 반환값으로 ChangeState 발동 ──────────────────────

    [Fact]
    public void Tick_TransitionsWhenStateReturnsNext()
    {
        PlayerEntity p = MakePlayer();
        var stateA = new TrackingState(AnimState.Idle);
        var stateB = new TrackingState(AnimState.Walk);
        stateA.NextState = stateB; // Tick 시 stateB 반환

        var sm = new StateMachine(stateA, p);
        sm.Tick(p);

        Assert.Same(stateB, sm.CurrentState);
    }

    [Fact]
    public void Tick_NoTransition_WhenStateReturnsNull()
    {
        PlayerEntity p = MakePlayer();
        var stateA = new TrackingState(AnimState.Idle);
        stateA.NextState = null;

        var sm = new StateMachine(stateA, p);
        sm.Tick(p);

        Assert.Same(stateA, sm.CurrentState);
    }

    // ── 5. IdleState 전환 ─────────────────────────────────────────────────

    [Fact]
    public void IdleState_StaysIdle_WhenGroundedAndNoVelocity()
    {
        PlayerEntity p = MakePlayer();
        p.OnGround = true;
        p.Velocity = Vector2.Zero;

        // PlayerEntity ctor이 Idle로 초기화. 추가 Tick으로 확인.
        p.ActionFsm.Tick(p);

        Assert.IsType<IdleState>(p.ActionFsm.CurrentState);
        Assert.Equal(AnimState.Idle, p.ActionFsm.AnimState);
    }

    [Fact]
    public void IdleState_TransitionsToMove_WhenVelocityExceedsEpsilon()
    {
        PlayerEntity p = MakePlayer();
        p.OnGround = true;
        p.Velocity = new Vector2(0.02f, 0f); // > VxEpsilon(0.01f)

        p.ActionFsm.Tick(p);

        Assert.IsType<MoveState>(p.ActionFsm.CurrentState);
        Assert.Equal(AnimState.Walk, p.ActionFsm.AnimState);
    }

    [Fact]
    public void IdleState_TransitionsToJump_WhenAirborne()
    {
        PlayerEntity p = MakePlayer();
        p.OnGround = false;
        p.Velocity = Vector2.Zero;

        p.ActionFsm.Tick(p);

        Assert.IsType<JumpState>(p.ActionFsm.CurrentState);
        Assert.Equal(AnimState.Jump, p.ActionFsm.AnimState);
    }

    // ── 6. MoveState 전환 ─────────────────────────────────────────────────

    [Fact]
    public void MoveState_StaysMove_WhenGroundedWithVelocity()
    {
        PlayerEntity p = MakePlayer();
        p.OnGround = true;
        p.Velocity = new Vector2(2f, 0f);
        // Idle→Move 전환 먼저
        p.ActionFsm.Tick(p);
        Assert.IsType<MoveState>(p.ActionFsm.CurrentState);

        // 그 상태에서 유지 확인
        p.ActionFsm.Tick(p);
        Assert.IsType<MoveState>(p.ActionFsm.CurrentState);
    }

    [Fact]
    public void MoveState_TransitionsToIdle_WhenVelocityDropsBelowEpsilon()
    {
        PlayerEntity p = MakePlayer();
        p.OnGround = true;
        p.Velocity = new Vector2(2f, 0f);
        p.ActionFsm.Tick(p); // → Move

        p.Velocity = Vector2.Zero; // 정지
        p.ActionFsm.Tick(p);

        Assert.IsType<IdleState>(p.ActionFsm.CurrentState);
    }

    [Fact]
    public void MoveState_TransitionsToJump_WhenAirborne()
    {
        PlayerEntity p = MakePlayer();
        p.OnGround = true;
        p.Velocity = new Vector2(2f, 0f);
        p.ActionFsm.Tick(p); // → Move

        p.OnGround = false;
        p.ActionFsm.Tick(p);

        Assert.IsType<JumpState>(p.ActionFsm.CurrentState);
    }

    // ── 7. JumpState 전환 ─────────────────────────────────────────────────

    [Fact]
    public void JumpState_StaysJump_WhileAirborne()
    {
        PlayerEntity p = MakePlayer();
        p.OnGround = false;
        p.ActionFsm.Tick(p); // → Jump
        Assert.IsType<JumpState>(p.ActionFsm.CurrentState);

        p.ActionFsm.Tick(p); // 여전히 공중
        Assert.IsType<JumpState>(p.ActionFsm.CurrentState);
    }

    [Fact]
    public void JumpState_TransitionsToIdle_WhenLandedWithNoVelocity()
    {
        PlayerEntity p = MakePlayer();
        p.OnGround = false;
        p.ActionFsm.Tick(p); // → Jump

        p.OnGround = true;
        p.Velocity = Vector2.Zero;
        p.ActionFsm.Tick(p);

        Assert.IsType<IdleState>(p.ActionFsm.CurrentState);
    }

    [Fact]
    public void JumpState_TransitionsToMove_WhenLandedWithVelocity()
    {
        PlayerEntity p = MakePlayer();
        p.OnGround = false;
        p.ActionFsm.Tick(p); // → Jump

        p.OnGround = true;
        p.Velocity = new Vector2(2f, 0f);
        p.ActionFsm.Tick(p);

        Assert.IsType<MoveState>(p.ActionFsm.CurrentState);
    }

    // ── 8. PlayerEntity ctor 초기화 ───────────────────────────────────────

    [Fact]
    public void PlayerEntity_Ctor_InitializesActionFsmToIdle()
    {
        PlayerEntity p = new PlayerEntity(42, new Vector2(5f, 0f));

        Assert.NotNull(p.ActionFsm);
        Assert.IsType<IdleState>(p.ActionFsm.CurrentState);
        Assert.Equal(AnimState.Idle, p.ActionFsm.AnimState);
    }
}

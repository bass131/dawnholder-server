using System.Numerics;
using Shared.GameData;

namespace Dawnholder.Server.GameServer.Tests;

// 공유 물리 공식의 결정론 + 함정 회피 검증.
// 양쪽이 호출하는 단일 출처라 본 테스트가 통과하면 prediction drift 0 보장 (헌법 #1).
//
// 테스트 카테고리:
//   1) 정지 + ground 정상 (회귀)
//   2) 좌우 이동
//   3) Jump 정상 동작
//   4) Jump 차단 (더블점프)
//   5) 중력 (낙하 가속)
//   6) Ground clamp
//   7) 결정론 (반복 시뮬)
//   8) 포물선 (점프 → 자연 낙하 → ground 복귀)
public class PhysicsTests
{
    const float Dt = Constants.TickDuration; // 0.05

    // === 1) 정지 + ground ===
    [Fact]
    public void IdleOnGround_StaysAtRest()
    {
        PhysicsState state = PhysicsState.AtRest(Vector2.Zero);
        PhysicsInput input = new PhysicsInput(0, false, Dt);

        PhysicsState next = Physics.Step(state, input);

        Assert.Equal(0f, next.Position.X, 4);
        Assert.Equal(0f, next.Position.Y, 4);
        Assert.Equal(0f, next.Velocity.Y, 4);
        Assert.True(next.OnGround);
    }

    // === 2) 좌우 이동 ===
    [Fact]
    public void RightInputOnGround_MovesRightOnly()
    {
        PhysicsState state = PhysicsState.AtRest(Vector2.Zero);
        PhysicsInput input = new PhysicsInput(1, false, Dt);

        PhysicsState next = Physics.Step(state, input);

        float expectedDx = Constants.MoveSpeed * Dt; // 5 * 0.05 = 0.25
        Assert.Equal(expectedDx, next.Position.X, 4);
        Assert.Equal(0f, next.Position.Y, 4);
        Assert.True(next.OnGround);
    }

    [Fact]
    public void LeftInputOnGround_MovesLeftOnly()
    {
        PhysicsState state = PhysicsState.AtRest(Vector2.Zero);
        PhysicsInput input = new PhysicsInput(-1, false, Dt);

        PhysicsState next = Physics.Step(state, input);

        float expectedDx = -Constants.MoveSpeed * Dt;
        Assert.Equal(expectedDx, next.Position.X, 4);
        Assert.True(next.OnGround);
    }

    // === 3) Jump 정상 동작 ===
    [Fact]
    public void JumpOnGround_AppliesJumpSpeed()
    {
        PhysicsState state = PhysicsState.AtRest(Vector2.Zero);
        PhysicsInput input = new PhysicsInput(0, true, Dt);

        PhysicsState next = Physics.Step(state, input);

        // vy = JumpSpeed (8) → newY = 8 * 0.05 = 0.4
        Assert.Equal(Physics.JumpSpeed * Dt, next.Position.Y, 4);
        Assert.Equal(Physics.JumpSpeed, next.Velocity.Y, 4);
        Assert.False(next.OnGround); // 점프 직후 공중
    }

    // === 4) Jump 차단 (더블점프 차단) ===
    [Fact]
    public void JumpInAir_Ignored_NoDoubleJump()
    {
        // 공중 상태 (y=1, vy=양수) — 점프 후 상승 중
        PhysicsState state = new PhysicsState(
            new Vector2(0f, 1f),
            new Vector2(0f, 5f),
            onGround: false);
        PhysicsInput input = new PhysicsInput(0, true, Dt);

        PhysicsState next = Physics.Step(state, input);

        // vy가 JumpSpeed로 *덮어쓰여지지 않음*. 중력만 적용.
        float expectedVy = 5f + Physics.Gravity * Dt; // 5 + (-20)*0.05 = 4.0
        Assert.Equal(expectedVy, next.Velocity.Y, 4);
        Assert.False(next.OnGround);
    }

    // === 5) 중력 (낙하 가속) ===
    [Fact]
    public void InAir_AccumulatesGravity()
    {
        PhysicsState state = new PhysicsState(
            new Vector2(0f, 2f),
            Vector2.Zero,
            onGround: false);
        PhysicsInput input = new PhysicsInput(0, false, Dt);

        PhysicsState next = Physics.Step(state, input);

        // vy = 0 + (-20) * 0.05 = -1.0
        Assert.Equal(Physics.Gravity * Dt, next.Velocity.Y, 4);
        // newY = 2 + (-1) * 0.05 = 1.95
        Assert.Equal(1.95f, next.Position.Y, 4);
        Assert.False(next.OnGround);
    }

    // === 6) Ground clamp ===
    [Fact]
    public void FallingThroughGround_ClampsToGroundY()
    {
        // 거의 ground (y=0.04, vy=-2) — 한 tick: -2 * 0.05 = -0.1, newY = -0.06 (ground 아래)
        PhysicsState state = new PhysicsState(
            new Vector2(0f, 0.04f),
            new Vector2(0f, -2f),
            onGround: false);
        PhysicsInput input = new PhysicsInput(0, false, Dt);

        PhysicsState next = Physics.Step(state, input);

        Assert.Equal(Physics.GroundY, next.Position.Y, 4);
        Assert.Equal(0f, next.Velocity.Y, 4); // vy 리셋
        Assert.True(next.OnGround);
    }

    // === 7) 좌우 + 점프 동시 ===
    [Fact]
    public void JumpWithRightInput_AppliesBoth()
    {
        PhysicsState state = PhysicsState.AtRest(Vector2.Zero);
        PhysicsInput input = new PhysicsInput(1, true, Dt);

        PhysicsState next = Physics.Step(state, input);

        Assert.Equal(Constants.MoveSpeed * Dt, next.Position.X, 4);
        Assert.Equal(Physics.JumpSpeed * Dt, next.Position.Y, 4);
        Assert.False(next.OnGround);
    }

    // === 8) 결정론 — 같은 입력 100번 반복 ===
    [Fact]
    public void Determinism_RepeatedStepsAreReproducible()
    {
        PhysicsState state = PhysicsState.AtRest(Vector2.Zero);
        PhysicsInput input = new PhysicsInput(1, false, Dt);

        // 100 step → 5초간 우측 이동 (ground 유지, vy 항상 0)
        for (int i = 0; i < 100; i++)
        {
            state = Physics.Step(state, input);
        }

        // 5 unit/s * 5s = 25
        Assert.Equal(25f, state.Position.X, 2);
        Assert.True(state.OnGround);
    }

    // === 9) 포물선 — 점프 한 번 후 자연 낙하 → ground 복귀 ===
    [Fact]
    public void ParabolicArc_JumpAndFall_LandsOnGround()
    {
        PhysicsState state = PhysicsState.AtRest(Vector2.Zero);

        // tick 0: 점프 시작
        state = Physics.Step(state, new PhysicsInput(0, true, Dt));
        Assert.False(state.OnGround);

        // jump 후 jumpPressed=false 유지 — 자연 낙하 시뮬 (최대 50 tick = 2.5초)
        bool landedAtSomePoint = false;
        for (int i = 0; i < 50; i++)
        {
            state = Physics.Step(state, new PhysicsInput(0, false, Dt));
            if (state.OnGround)
            {
                landedAtSomePoint = true;
                break;
            }
        }

        Assert.True(landedAtSomePoint, "포물선 후 ground 복귀 실패");
        Assert.Equal(Physics.GroundY, state.Position.Y, 4);
    }

    // === 10) 더블점프 차단 회귀 — 같은 tick에 jump 두 번 시도 ===
    [Fact]
    public void Jump_ThenJumpNextTickInAir_OnlyFirstJumpApplies()
    {
        PhysicsState state = PhysicsState.AtRest(Vector2.Zero);

        // tick 1: 점프 (ground → 공중)
        state = Physics.Step(state, new PhysicsInput(0, true, Dt));
        Assert.Equal(Physics.JumpSpeed, state.Velocity.Y, 4);

        // tick 2: 공중에서 다시 점프 시도 → 무시되고 중력만 적용
        state = Physics.Step(state, new PhysicsInput(0, true, Dt));

        // vy = JumpSpeed + Gravity*dt = 8 + (-1) = 7.0 (재점프 X)
        Assert.Equal(Physics.JumpSpeed + Physics.Gravity * Dt, state.Velocity.Y, 4);
    }
}

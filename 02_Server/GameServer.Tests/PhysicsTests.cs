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
//   C) 직업값 단위 테스트 (Knight vs Mage 이동·점프·factory 고정)
public class PhysicsTests
{
    const float Dt = Constants.TickDuration; // 0.05

    // 지형 의미론 테스트용 — 좌표/기하를 5.0/8.0 기준으로 만들었으므로 유지.
    static readonly MoveParams FlatParams = new MoveParams(5f, 8f);

    // === 1) 정지 + ground ===
    [Fact]
    public void IdleOnGround_StaysAtRest()
    {
        PhysicsState state = PhysicsState.AtRest(Vector2.Zero);
        PhysicsInput input = new PhysicsInput(0, false, Dt);

        PhysicsState next = Physics.Step(state, input, FlatParams);

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

        PhysicsState next = Physics.Step(state, input, FlatParams);

        float expectedDx = FlatParams.MoveSpeed * Dt; // 5 * 0.05 = 0.25
        Assert.Equal(expectedDx, next.Position.X, 4);
        Assert.Equal(0f, next.Position.Y, 4);
        Assert.True(next.OnGround);
    }

    [Fact]
    public void LeftInputOnGround_MovesLeftOnly()
    {
        PhysicsState state = PhysicsState.AtRest(Vector2.Zero);
        PhysicsInput input = new PhysicsInput(-1, false, Dt);

        PhysicsState next = Physics.Step(state, input, FlatParams);

        float expectedDx = -FlatParams.MoveSpeed * Dt;
        Assert.Equal(expectedDx, next.Position.X, 4);
        Assert.True(next.OnGround);
    }

    // === 3) Jump 정상 동작 ===
    [Fact]
    public void JumpOnGround_AppliesJumpSpeed()
    {
        PhysicsState state = PhysicsState.AtRest(Vector2.Zero);
        PhysicsInput input = new PhysicsInput(0, true, Dt);

        PhysicsState next = Physics.Step(state, input, FlatParams);

        // vy = JumpVel (8) → newY = 8 * 0.05 = 0.4
        Assert.Equal(FlatParams.JumpVel * Dt, next.Position.Y, 4);
        Assert.Equal(FlatParams.JumpVel, next.Velocity.Y, 4);
        Assert.False(next.OnGround);
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

        PhysicsState next = Physics.Step(state, input, FlatParams);

        // vy가 JumpVel로 *덮어쓰여지지 않음*. 중력만 적용.
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

        PhysicsState next = Physics.Step(state, input, FlatParams);

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

        PhysicsState next = Physics.Step(state, input, FlatParams);

        Assert.Equal(Physics.GroundY, next.Position.Y, 4);
        Assert.Equal(0f, next.Velocity.Y, 4);
        Assert.True(next.OnGround);
    }

    // === 7) 좌우 + 점프 동시 ===
    [Fact]
    public void JumpWithRightInput_AppliesBoth()
    {
        PhysicsState state = PhysicsState.AtRest(Vector2.Zero);
        PhysicsInput input = new PhysicsInput(1, true, Dt);

        PhysicsState next = Physics.Step(state, input, FlatParams);

        Assert.Equal(FlatParams.MoveSpeed * Dt, next.Position.X, 4);
        Assert.Equal(FlatParams.JumpVel * Dt, next.Position.Y, 4);
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
            state = Physics.Step(state, input, FlatParams);
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
        state = Physics.Step(state, new PhysicsInput(0, true, Dt), FlatParams);
        Assert.False(state.OnGround);

        // jump 후 jumpPressed=false 유지 — 자연 낙하 시뮬 (최대 50 tick = 2.5초)
        bool landedAtSomePoint = false;
        for (int i = 0; i < 50; i++)
        {
            state = Physics.Step(state, new PhysicsInput(0, false, Dt), FlatParams);
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
        state = Physics.Step(state, new PhysicsInput(0, true, Dt), FlatParams);
        Assert.Equal(FlatParams.JumpVel, state.Velocity.Y, 4);

        // tick 2: 공중에서 다시 점프 시도 → 무시되고 중력만 적용
        state = Physics.Step(state, new PhysicsInput(0, true, Dt), FlatParams);

        // vy = JumpVel + Gravity*dt = 8 + (-1) = 7.0 (재점프 X)
        Assert.Equal(FlatParams.JumpVel + Physics.Gravity * Dt, state.Velocity.Y, 4);
    }

    // ── C) 직업값 단위 테스트 ────────────────────────────────────────────────────

    // C-1: 같은 N틱 입력으로 Knight(4) vs Mage(6) 이동 거리 4:6 비례.
    [Fact]
    public void ClassParams_KnightVsMage_MoveDistanceRatio_4to6()
    {
        const int Ticks = 20; // 1초 (20 TPS)
        PhysicsInput input = new PhysicsInput(1, false, Dt);

        var knight = new MoveParams(PlayerStats.Knight().MoveSpeed, PlayerStats.Knight().JumpVel);
        var mage  = new MoveParams(PlayerStats.Mage().MoveSpeed,  PlayerStats.Mage().JumpVel);

        PhysicsState ws = PhysicsState.AtRest(Vector2.Zero);
        PhysicsState rs = PhysicsState.AtRest(Vector2.Zero);

        for (int i = 0; i < Ticks; i++)
        {
            ws = Physics.Step(ws, input, knight);
            rs = Physics.Step(rs, input, mage);
        }

        // 1초 이동 거리: Knight=4.0, Mage=6.0
        Assert.Equal(4f, ws.Position.X, 3);
        Assert.Equal(6f, rs.Position.X, 3);
    }

    // C-2: jumpVel 파라미터가 점프 초속에 반영됨.
    [Fact]
    public void ClassParams_JumpVel_AppliedAsInitialVy()
    {
        PhysicsState state = PhysicsState.AtRest(Vector2.Zero);
        float jumpVel = 8f;
        var move = new MoveParams(4f, jumpVel);

        PhysicsState next = Physics.Step(state, new PhysicsInput(0, true, Dt), move);

        Assert.Equal(jumpVel, next.Velocity.Y, 4);
    }

    // C-3: PlayerStats factory의 MoveSpeed/JumpVel 고정 값 단언 (4/6/8/8).
    [Fact]
    public void PlayerStats_Factory_FixedValues_4_6_8_8()
    {
        PlayerStats knight = PlayerStats.Knight();
        PlayerStats mage  = PlayerStats.Mage();

        Assert.Equal(4f, knight.MoveSpeed, 4);
        Assert.Equal(8f, knight.JumpVel,   4);
        Assert.Equal(6f, mage.MoveSpeed,  4);
        Assert.Equal(8f, mage.JumpVel,    4);
    }
}

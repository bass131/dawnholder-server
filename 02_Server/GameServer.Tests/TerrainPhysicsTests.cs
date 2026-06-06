using System.Numerics;
using Shared.GameData;

namespace Dawnholder.Server.GameServer.Tests;

// 지형 물리(StepWithTerrain) 계약 검증 (M4.4 Phase 02).
//
// 설계 계약:
//   - 2-인자 Step == 3-인자 Step(null) == StepFlat (평지 fallback 동일성)
//   - new MapTerrain(null, null) 도 빈 지형이라 fallback 위임
//   - 솔리드 AABB: X 스윕 y∈[MinY,MaxY) 조건, Y 스윕 착지/머리 충돌
//   - 발판(one-way): 상승 통과 허용, 하강 착지
//   - 지형 모드 GroundY clamp 없음 → 구멍 = 무한 낙하
//
// 케이스 카테고리:
//   F) fallback 회귀 (null / 빈 terrain)
//   S) 슬래브 착지 / 안정 서기
//   W) 벽 차단 (좌/우) + 벽-점프 회귀
//   C) 머리 충돌
//   P) one-way 발판 통과/착지/아래-비차단
//   E) epsilon / tunneling / 턱(ledge)
//   M) MapId enum 정합 어서션
public class TerrainPhysicsTests
{
    const float Dt     = Constants.TickDuration; // 0.05s
    const float DtClient = 0.016f;               // 클라 디스플레이 dt (60 fps)
    const float Eps    = 0.001f;                 // 어서션 tolerance

    // ── 헬퍼 ──────────────────────────────────────────────────────────────────

    /// <summary>솔리드만 있는 미니 지형 팩토리.</summary>
    static MapTerrain Solids(params TerrainAabb[] solids)
        => new MapTerrain(solids, System.Array.Empty<TerrainPlatform>());

    /// <summary>발판만 있는 미니 지형 팩토리.</summary>
    static MapTerrain Platforms(params TerrainPlatform[] platforms)
        => new MapTerrain(System.Array.Empty<TerrainAabb>(), platforms);

    /// <summary>N틱 시뮬 루프. 각 틱에 같은 input을 적용.</summary>
    static PhysicsState SimN(PhysicsState state, PhysicsInput input, MapTerrain? terrain, int ticks)
    {
        for (int i = 0; i < ticks; i++)
            state = Physics.Step(state, input, terrain);
        return state;
    }

    // ── F) fallback 회귀 ──────────────────────────────────────────────────────

    // F-1: 20틱 시퀀스(정지→이동→점프→낙하)에 대해 2-인자와 3-인자(null)가 float 완전 동일.
    [Fact]
    public void Fallback_NullTerrain_IdenticalToTwoArgOverload()
    {
        PhysicsState s2    = PhysicsState.AtRest(Vector2.Zero);
        PhysicsState s3    = PhysicsState.AtRest(Vector2.Zero);

        PhysicsInput[] seq = new[]
        {
            new PhysicsInput(0,  false, Dt),  // 정지 ×4
            new PhysicsInput(0,  false, Dt),
            new PhysicsInput(0,  false, Dt),
            new PhysicsInput(0,  false, Dt),
            new PhysicsInput(1,  false, Dt),  // 이동 ×4
            new PhysicsInput(1,  false, Dt),
            new PhysicsInput(1,  false, Dt),
            new PhysicsInput(1,  false, Dt),
            new PhysicsInput(0,  true,  Dt),  // 점프
            new PhysicsInput(0,  false, Dt),  // 낙하 ×11
            new PhysicsInput(0,  false, Dt),
            new PhysicsInput(0,  false, Dt),
            new PhysicsInput(0,  false, Dt),
            new PhysicsInput(0,  false, Dt),
            new PhysicsInput(0,  false, Dt),
            new PhysicsInput(0,  false, Dt),
            new PhysicsInput(0,  false, Dt),
            new PhysicsInput(0,  false, Dt),
            new PhysicsInput(0,  false, Dt),
            new PhysicsInput(0,  false, Dt),
        };

        foreach (PhysicsInput inp in seq)
        {
            s2 = Physics.Step(s2, inp);
            s3 = Physics.Step(s3, inp, null);
        }

        // float 완전 동일 — 같은 코드 경로이므로 epsilon 마진 불필요
        Assert.Equal(s2.Position.X,  s3.Position.X);
        Assert.Equal(s2.Position.Y,  s3.Position.Y);
        Assert.Equal(s2.Velocity.X,  s3.Velocity.X);
        Assert.Equal(s2.Velocity.Y,  s3.Velocity.Y);
        Assert.Equal(s2.OnGround,    s3.OnGround);
    }

    // F-2: new MapTerrain(null, null) — 빈 지형이라 fallback 위임 → 2-인자와 동일.
    [Fact]
    public void Fallback_EmptyTerrain_IdenticalToTwoArgOverload()
    {
        TerrainAabb[]?     nullSolids    = null;
        TerrainPlatform[]? nullPlatforms = null;
        MapTerrain emptyTerrain = new MapTerrain(nullSolids!, nullPlatforms!);

        PhysicsState s2 = PhysicsState.AtRest(Vector2.Zero);
        PhysicsState s3 = PhysicsState.AtRest(Vector2.Zero);

        // 점프 포함 15틱
        for (int i = 0; i < 15; i++)
        {
            bool jump = (i == 2);
            var inp = new PhysicsInput(1, jump, Dt);
            s2 = Physics.Step(s2, inp);
            s3 = Physics.Step(s3, inp, emptyTerrain);
        }

        Assert.Equal(s2.Position.X, s3.Position.X);
        Assert.Equal(s2.Position.Y, s3.Position.Y);
        Assert.Equal(s2.Velocity.X, s3.Velocity.X);
        Assert.Equal(s2.Velocity.Y, s3.Velocity.Y);
        Assert.Equal(s2.OnGround,   s3.OnGround);
    }

    // ── S) 슬래브 착지 / 안정 서기 ───────────────────────────────────────────

    // S-1: 슬래브 (MinX=0, MinY=0, MaxX=10, MaxY=2, 윗면 y=2) 위 y=5 자유낙하 → y=2 착지.
    [Theory]
    [InlineData(Dt)]
    [InlineData(DtClient)]
    public void Slab_FreeFall_LandsOnTopFace(float dt)
    {
        MapTerrain terrain = Solids(new TerrainAabb(0f, 0f, 10f, 2f));
        PhysicsState state = new PhysicsState(
            new Vector2(5f, 5f),
            new Vector2(0f, 0f),
            onGround: false);

        bool landed = false;
        for (int i = 0; i < 60; i++)
        {
            state = Physics.Step(state, new PhysicsInput(0, false, dt), terrain);
            if (state.OnGround)
            {
                landed = true;
                break;
            }
        }

        Assert.True(landed, "슬래브 위 자유낙하 — 착지 실패");
        Assert.Equal(2f, state.Position.Y, 3);
        Assert.Equal(0f, state.Velocity.Y, 3);
        Assert.True(state.OnGround);
    }

    // S-2: 슬래브 윗면 y=2에서 vy=0으로 10틱 → y 불변, onGround 항상 true.
    [Fact]
    public void Slab_IdleOnTop_StaysStable()
    {
        MapTerrain terrain = Solids(new TerrainAabb(0f, 0f, 10f, 2f));
        PhysicsState state = new PhysicsState(
            new Vector2(5f, 2f),
            Vector2.Zero,
            onGround: true);

        for (int i = 0; i < 10; i++)
        {
            state = Physics.Step(state, new PhysicsInput(0, false, Dt), terrain);
            Assert.Equal(2f, state.Position.Y, 3);
            Assert.True(state.OnGround, $"tick {i+1}: onGround=false (불안정)");
        }
    }

    // ── W) 벽 차단 + 벽-점프 회귀 ──────────────────────────────────────────

    // W-1: 바닥(y=0) + 우벽(x=5~7) 향해 inputX=+1 연속 → x가 5에서 멈춤.
    [Fact]
    public void Wall_Right_BlocksAtMinX()
    {
        // 바닥: (-10,-2,10,0 — 윗면 y=0), 우벽: (5,0,7,4)
        MapTerrain terrain = Solids(
            new TerrainAabb(-10f, -2f, 10f, 0f),  // 바닥
            new TerrainAabb(5f, 0f, 7f, 4f));      // 우벽

        // x=0, y=0 (바닥 위 — y == MaxY of 바닥 = 0)
        PhysicsState state = new PhysicsState(
            new Vector2(0f, 0f),
            Vector2.Zero,
            onGround: true);

        // inputX=+1로 충분히 이동
        for (int i = 0; i < 30; i++)
            state = Physics.Step(state, new PhysicsInput(1, false, Dt), terrain);

        Assert.Equal(5f, state.Position.X, 3);
        Assert.Equal(0f, state.Velocity.X, 3);

        // 추가 틱에도 x=5 유지
        state = Physics.Step(state, new PhysicsInput(1, false, Dt), terrain);
        Assert.Equal(5f, state.Position.X, 3);
    }

    // W-2: 좌벽(x=-7~-5) 향해 inputX=-1 → x가 -5(MaxX)에서 멈춤.
    [Fact]
    public void Wall_Left_BlocksAtMaxX()
    {
        MapTerrain terrain = Solids(
            new TerrainAabb(-10f, -2f, 10f, 0f),  // 바닥
            new TerrainAabb(-7f, 0f, -5f, 4f));   // 좌벽

        PhysicsState state = new PhysicsState(
            new Vector2(0f, 0f),
            Vector2.Zero,
            onGround: true);

        for (int i = 0; i < 30; i++)
            state = Physics.Step(state, new PhysicsInput(-1, false, Dt), terrain);

        Assert.Equal(-5f, state.Position.X, 3);
        Assert.Equal(0f, state.Velocity.X, 3);

        state = Physics.Step(state, new PhysicsInput(-1, false, Dt), terrain);
        Assert.Equal(-5f, state.Position.X, 3);
    }

    // W-3: 벽-점프 회귀. 벽(x=5)에 붙어 있는 상태(바닥 위 y=0)에서 jumpPressed → vy=JumpSpeed.
    //
    // 과거 결함: 벽 MinX와 바닥 MaxY가 동일 좌표(y=0)일 때 벽 MinY 조건이 맞아 점프 즉시
    // 머리 충돌(y<MinY 미충족 = y==MinY)로 잡힐 가능성. 등호 제외(y < faceY) 규칙으로 봉합.
    [Fact]
    public void WallJump_Regression_JumpSucceeds_NotStuck()
    {
        MapTerrain terrain = Solids(
            new TerrainAabb(-10f, -2f, 10f, 0f),
            new TerrainAabb(5f, 0f, 7f, 4f));

        // W-1에서 멈춘 상태 재구성
        PhysicsState state = new PhysicsState(
            new Vector2(5f, 0f),
            Vector2.Zero,
            onGround: true);

        // jumpPressed 1틱
        state = Physics.Step(state, new PhysicsInput(0, true, Dt), terrain);

        Assert.Equal(Physics.JumpSpeed, state.Velocity.Y, 3);
        Assert.True(state.Position.Y > 0f, $"점프 후 y가 0에 박힘 (vy={state.Velocity.Y})");
        Assert.False(state.OnGround);
    }

    // ── C) 머리 충돌 ─────────────────────────────────────────────────────────

    // C-1: 천장(aabb 0,1.4,10,3 — 아랫면 y=1.4) 아래 y=0에서 점프 → y=1.4에서 vy=0, 이후 낙하.
    //
    // JumpSpeed=8, Gravity=-20, dt=0.05 정확한 틱 계산:
    //   점프 tick: y=0.40, vy=8
    //   tick+1:    y=0.75, vy=7
    //   tick+2:    y=1.05, vy=6
    //   tick+3:    y=1.30, vy=5
    //   tick+4:    y_pre=1.30, vy=4 → newY=1.50; faceY=1.4; y(1.30) < faceY(1.4) && newY(1.50)>=faceY → 충돌.
    //              newY=1.4, vy=0.
    [Fact]
    public void Ceiling_HeadCollision_StopsAtMinY()
    {
        MapTerrain terrain = Solids(
            new TerrainAabb(-10f, -2f, 10f, 0f),   // 바닥
            new TerrainAabb(0f, 1.4f, 10f, 3.0f)); // 천장(아랫면 y=1.4)

        PhysicsState state = new PhysicsState(
            new Vector2(5f, 0f),
            Vector2.Zero,
            onGround: true);

        // 점프
        state = Physics.Step(state, new PhysicsInput(0, true, Dt), terrain);
        Assert.True(state.Velocity.Y > 0f, "점프 미적용");

        // 최대 30틱 — 천장 충돌 후 vy==0이 되는 틱 탐지 (충돌 직후 y≈1.4)
        bool hitCeiling = false;
        for (int i = 0; i < 30; i++)
        {
            state = Physics.Step(state, new PhysicsInput(0, false, Dt), terrain);
            if (state.Velocity.Y == 0f && state.Position.Y >= 1.3f && !state.OnGround)
            {
                hitCeiling = true;
                break;
            }
        }

        Assert.True(hitCeiling, "천장 충돌 미발생");
        Assert.Equal(1.4f, state.Position.Y, 3);
        Assert.Equal(0f, state.Velocity.Y, 3);
        Assert.False(state.OnGround);

        // 이후 낙하
        state = Physics.Step(state, new PhysicsInput(0, false, Dt), terrain);
        Assert.True(state.Velocity.Y < 0f, "천장 충돌 후 중력 미적용");
    }

    // ── P) one-way 발판 ───────────────────────────────────────────────────────

    // P-1: 발판(y=1.5, x 0~10) 아래 y=0에서 점프 → 상승 틱 동안 발판 통과 (차단 없음).
    //
    // JumpSpeed=8, Gravity=-20, dt=0.05:
    //   최대 높이 ≈ 8²/(2*20) = 1.6. 발판 y=1.5 < 1.6 → 상승 중 통과 가능.
    //   착지 조건 "y >= faceY - eps" + "vy <= 0": 상승 중(vy>0)이면 착지 경로 자체 진입 안 함.
    [Fact]
    public void Platform_OneWay_Ascending_PassesThrough()
    {
        MapTerrain terrain = new MapTerrain(
            new[] { new TerrainAabb(-10f, -2f, 10f, 0f) },  // 바닥
            new[] { new TerrainPlatform(1.5f, 0f, 10f) });  // 발판 y=1.5

        PhysicsState state = new PhysicsState(
            new Vector2(5f, 0f),
            Vector2.Zero,
            onGround: true);

        // 점프
        state = Physics.Step(state, new PhysicsInput(0, true, Dt), terrain);
        Assert.True(state.Velocity.Y > 0f, "점프 미적용");

        // 상승 중 매 틱 y > 1.5 구간에서도 차단이 없어야 한다
        bool passingAbovePlatform = false;
        for (int i = 0; i < 20; i++)
        {
            state = Physics.Step(state, new PhysicsInput(0, false, Dt), terrain);
            if (state.Position.Y > 1.5f && state.Velocity.Y > 0f)
                passingAbovePlatform = true;
            if (state.Velocity.Y <= 0f) break; // 최고점 도달
        }

        Assert.True(passingAbovePlatform, "발판 y=1.5를 상승 중 통과하지 못했음");
    }

    // P-2: P-1 이후 하강 전환 → 발판 y=1.5 위에 착지.
    [Fact]
    public void Platform_OneWay_Descending_LandsOnFace()
    {
        MapTerrain terrain = new MapTerrain(
            new[] { new TerrainAabb(-10f, -2f, 10f, 0f) },
            new[] { new TerrainPlatform(1.5f, 0f, 10f) });

        PhysicsState state = new PhysicsState(
            new Vector2(5f, 0f),
            Vector2.Zero,
            onGround: true);

        // 점프 + 최대 60틱 — 발판 착지 대기
        state = Physics.Step(state, new PhysicsInput(0, true, Dt), terrain);

        bool landedOnPlatform = false;
        for (int i = 0; i < 60; i++)
        {
            state = Physics.Step(state, new PhysicsInput(0, false, Dt), terrain);
            // 발판 y=1.5에 착지하면 성공
            if (state.OnGround && System.MathF.Abs(state.Position.Y - 1.5f) < Eps)
            {
                landedOnPlatform = true;
                break;
            }
            // 바닥(y=0)에 착지하면 발판을 미착지 통과 — 실패
            if (state.OnGround && state.Position.Y < 1f)
                break;
        }

        Assert.True(landedOnPlatform, "발판 착지 실패");
        Assert.Equal(1.5f, state.Position.Y, 3);
        Assert.Equal(0f, state.Velocity.Y, 3);
        Assert.True(state.OnGround);
    }

    // P-3: 발판 면 아래(y=1.0 < faceY=1.5)에서 vy=0으로 시작 → 착지 후보 아님 (낙하).
    //
    // 착지 조건 "y >= faceY - eps": y=1.0, faceY=1.5, 차이=0.5 >> eps → 조건 불성립.
    // 따라서 발판을 바로 밑에서 아래→위 이동 시 차단 없어야 한다.
    [Fact]
    public void Platform_OneWay_BelowFace_NotLanding()
    {
        MapTerrain terrain = new MapTerrain(
            new[] { new TerrainAabb(-10f, -6f, 10f, -2f) },  // 바닥 y=-2
            new[] { new TerrainPlatform(1.5f, 0f, 10f) });   // 발판 y=1.5

        // y=1.0 (발판 아래), vy=0 — 자유낙하 시작
        PhysicsState state = new PhysicsState(
            new Vector2(5f, 1.0f),
            Vector2.Zero,
            onGround: false);

        PhysicsState next = Physics.Step(state, new PhysicsInput(0, false, Dt), terrain);

        // 1틱 후 발판 착지가 아닌 낙하
        Assert.False(next.OnGround, "발판 면 아래에서 착지 판정됨 (오류)");
        Assert.True(next.Position.Y < 1.0f, "낙하 미발생");
    }

    // ── E) epsilon / tunneling / 턱 ───────────────────────────────────────────

    // E-1: 슬래브(0,0,10,2) 윗면 끝 x==MaxX(=10)에서 하강 → eps 포함 조건으로 착지 성공.
    [Fact]
    public void Epsilon_SlabEdgeMaxX_LandsSuccessfully()
    {
        MapTerrain terrain = Solids(new TerrainAabb(0f, 0f, 10f, 2f));
        PhysicsState state = new PhysicsState(
            new Vector2(10f, 5f),    // x = MaxX 정확히
            new Vector2(0f, 0f),
            onGround: false);

        bool landed = false;
        for (int i = 0; i < 60; i++)
        {
            state = Physics.Step(state, new PhysicsInput(0, false, Dt), terrain);
            if (state.OnGround)
            {
                landed = true;
                break;
            }
        }

        Assert.True(landed, "슬래브 끝(x==MaxX)에서 착지 실패");
        Assert.Equal(2f, state.Position.Y, 3);
    }

    // E-2: 고속 낙하 tunneling 방지. vy=-40(틱당 -2u)로 두께 1 슬래브(MinY=1, MaxY=2) 단번 통과 시도.
    //
    // y=3, vy=-40, dt=0.05 → newY = 3 + (-40)*0.05 = 1.0
    //   → newY(1.0) <= faceY(2.0) && y(3.0) >= faceY(2.0)-eps(1.9999) → 착지 조건 성립.
    // 슬래브가 얇아도 교차 판정이 작동해야 함.
    [Fact]
    public void HighSpeed_TunnelingPrevention_LandsOnSlab()
    {
        MapTerrain terrain = Solids(new TerrainAabb(-5f, 1f, 5f, 2f));
        PhysicsState state = new PhysicsState(
            new Vector2(0f, 3f),
            new Vector2(0f, -40f),  // 고속 낙하
            onGround: false);

        state = Physics.Step(state, new PhysicsInput(0, false, Dt), terrain);

        Assert.Equal(2f, state.Position.Y, 3);
        Assert.Equal(0f, state.Velocity.Y, 3);
        Assert.True(state.OnGround);
    }

    // E-3: 턱(ledge) 걷어내기. 슬래브(0,0,4,2) 위 x=3.5에서 inputX=+1 → 다음 틱 onGround=false.
    //
    // x=3.5 + 5*0.05=0.25 = 3.75 → 슬래브 MaxX=4 이내 → 아직 ground.
    // x=3.75 + 0.25 = 4.0 → MaxX 끝에서 착지 가능 (eps 허용).
    // x=4.0 + 0.25 = 4.25 → 슬래브 x-범위 밖 → 착지 후보 제거 → 낙하.
    [Fact]
    public void Ledge_WalkOff_FallsAfterEdge()
    {
        MapTerrain terrain = Solids(new TerrainAabb(0f, 0f, 4f, 2f));
        PhysicsState state = new PhysicsState(
            new Vector2(3.5f, 2f),
            Vector2.Zero,
            onGround: true);

        // inputX=+1로 충분히 이동
        bool fellOff = false;
        for (int i = 0; i < 20; i++)
        {
            state = Physics.Step(state, new PhysicsInput(1, false, Dt), terrain);
            if (!state.OnGround && state.Position.X > 4f)
            {
                fellOff = true;
                break;
            }
        }

        Assert.True(fellOff, "슬래브 끝 통과 후 낙하 미발생");
    }

    // ── D) 대각(축 분리 순서) 동시 충돌 — reviewer 🟡 봉합 ──────────────────────

    // D-1: 벽에 붙은 채 점프 + 전진 동시 입력. X 스냅(newX=벽.MinX)이 먼저 확정되고,
    // 상승 검사는 등호 제외(y < faceY)라 바닥에 묻힌 벽 아랫면에 안 잡힘 → 정상 상승.
    // 축 분리 순서(X 먼저)와 벽-점프 봉합이 *대각 입력에서도* 함께 성립하는지 고정.
    [Fact]
    public void Diagonal_JumpIntoWall_SnapsXAndRises()
    {
        MapTerrain terrain = Solids(
            new TerrainAabb(-10f, -2f, 10f, 0f),  // 바닥
            new TerrainAabb(5f, 0f, 7f, 4f));     // 벽 (MinY == 바닥 윗면)
        PhysicsState state = new PhysicsState(
            new Vector2(4.9f, 0f),
            Vector2.Zero,
            onGround: true);

        state = Physics.Step(state, new PhysicsInput(1, true, Dt), terrain);

        Assert.Equal(5f, state.Position.X, 3);            // X: 벽 면에 스냅
        Assert.Equal(0f, state.Velocity.X, 3);            //    vx=0
        Assert.Equal(Physics.JumpSpeed, state.Velocity.Y, 3); // Y: 점프 정상 (머리충돌 X)
        Assert.True(state.Position.Y > 0f, "벽 모서리 대각 점프가 상승하지 않음");
    }

    // D-2: 대각 하강 착지 — X 이동 *후* 좌표(newX)로 착지 면 x-범위를 평가하는 순서 고정.
    // 슬래브 왼쪽 바깥(x=-0.2)에서 전진+낙하 → newX가 슬래브 범위에 든 틱에 윗면 착지.
    [Fact]
    public void Diagonal_FallingForward_LandsOnSlabEdgeUsingPostMoveX()
    {
        MapTerrain terrain = Solids(new TerrainAabb(0f, 0f, 10f, 2f));
        PhysicsState state = new PhysicsState(
            new Vector2(-0.2f, 2.5f),
            new Vector2(0f, -5f),
            onGround: false);

        bool landed = false;
        for (int i = 0; i < 10; i++)
        {
            state = Physics.Step(state, new PhysicsInput(1, false, Dt), terrain);
            if (state.OnGround) { landed = true; break; }
        }

        Assert.True(landed, "대각 하강이 슬래브 윗면에 착지하지 않음");
        Assert.Equal(2f, state.Position.Y, 3);                  // 윗면 y=2
        Assert.True(state.Position.X >= 0f, "착지 시점 X가 슬래브 범위 밖");
    }

    // ── M) MapId 정합 어서션 ─────────────────────────────────────────────────

    // M-1: MapId enum 값이 MapTerrainData mapId와 일치.
    [Fact]
    public void MapId_EnumValues_MatchTerrainDataKeys()
    {
        Assert.Equal(0, (int)Dawnholder.Server.GameServer.Maps.MapId.Town);
        Assert.Equal(1, (int)Dawnholder.Server.GameServer.Maps.MapId.HuntingGround);
        Assert.Equal(2, (int)Dawnholder.Server.GameServer.Maps.MapId.BossRoom);
    }

    // M-2: MapTerrain round-trip 후 Solids 값 동등 (MapDataFile.WriteTerrain/ReadTerrain).
    // ForMap(코드 생성 정적 데이터)은 M4.4 Phase 03 은퇴 → bin 파일 로드로 전환.
    // 본 테스트는 "bin 왕복 후 값 보존" 계약을 검증 (MapDataFileTests R-1의 회귀 정합).
    [Fact]
    public void MapTerrain_BinRoundTrip_SolidsValuesPreserved()
    {
        TerrainAabb[] solids = new[]
        {
            new TerrainAabb(-43f, -4f, 56f, 0f),
            new TerrainAabb(-43f, 0f, -35f, 10f),
        };
        MapTerrain src = new MapTerrain(solids, System.Array.Empty<TerrainPlatform>());

        byte[] bytes = MapDataFile.WriteTerrain(0, src);
        MapTerrain dst = MapDataFile.ReadTerrain(bytes, 0);

        Assert.Equal(src.Solids.Length, dst.Solids.Length);
        for (int i = 0; i < src.Solids.Length; i++)
        {
            Assert.Equal(src.Solids[i].MinX, dst.Solids[i].MinX);
            Assert.Equal(src.Solids[i].MinY, dst.Solids[i].MinY);
            Assert.Equal(src.Solids[i].MaxX, dst.Solids[i].MaxX);
            Assert.Equal(src.Solids[i].MaxY, dst.Solids[i].MaxY);
        }
    }
}

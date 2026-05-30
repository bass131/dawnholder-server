namespace Shared.GameData;

using System.Numerics;

/// <summary>
/// 결정론적 물리 공식. 양쪽이 같은 함수 호출 → 같은 결과.
///
/// **헌법 #1 (Server Authority) 정합**: 공식 = Shared, 실행 = 서버. 클라 prediction도
/// 같은 함수를 호출해 양쪽 결과 일치 → drift 0. 클라 별도 const/공식 박으면 무한 drift.
///
/// **결정론**: 같은 입력 → 같은 출력. float은 동일 플랫폼 + 같은 컴파일 옵션이면 충분히
/// 결정론적. Unity Burst/SIMD 컴파일 옵션 다르면 미세 drift 가능.
///
/// **fixed timestep 가정**: dt = Constants.TickDuration (50ms 고정). 가변 dt 들어오면
/// 결정론 깨짐 — 호출자 책임. 클라가 Time.deltaTime 그대로 넘기면 fps 의존이라 금지.
/// </summary>
public static class Physics
{
    /// <summary>중력 가속도 (units/s²). Y up = 양수, gravity는 음수 (내림).</summary>
    public const float Gravity = -20.0f;

    /// <summary>점프 시작 시 부여되는 vy (units/s). 8.0 = 포물선 약 0.8초.</summary>
    public const float JumpSpeed = 8.0f;

    /// <summary>지면 Y 좌표. 캐릭터 발바닥이 닿는 높이.</summary>
    public const float GroundY = 0.0f;

    /// <summary>ground 판정 epsilon — float 비교 오차 흡수.</summary>
    private const float GroundEpsilon = 0.0001f;

    /// <summary>
    /// 1 step 결정론 시뮬레이션. 같은 (state, input) → 같은 PhysicsState.
    ///
    /// **순서** (재정렬 시 점프 물리 깨짐):
    ///   1. 수평 velocity = inputX * MoveSpeed (즉시 반응, 관성 X)
    ///   2. 시작 시점 ground 판정 (pos.Y와 vy 둘 다 확인)
    ///   3. jumpPressed && startedOnGround → vy = JumpSpeed (점프 시작)
    ///      그 외 공중일 때 → vy += Gravity * dt (낙하 가속)
    ///   4. 위치 적분 (Euler explicit)
    ///   5. 적분 후 newY <= GroundY → clamp + vy=0 + onGround=true
    ///
    /// **함정 회피**:
    ///   - jump 적용 후 적분에 vy=JumpSpeed가 박혀 newY > GroundY로 즉시 위로 → onGround=false
    ///     같은 tick 안에서 재점프 시도해도 startedOnGround였던 시점 상태로만 판단 (한 번만 적용)
    ///   - 중력은 *공중일 때만* 적용 → ground에서 vy가 음수로 누적되지 않음
    /// </summary>
    public static PhysicsState Step(PhysicsState state, PhysicsInput input)
    {
        float vx = input.InputX * Constants.MoveSpeed;

        bool startedOnGround = state.Position.Y <= GroundY + GroundEpsilon
                            && state.Velocity.Y <= 0f;

        float vy = state.Velocity.Y;
        if (input.JumpPressed && startedOnGround)
        {
            vy = JumpSpeed;
        }
        else if (!startedOnGround)
        {
            vy += Gravity * input.Dt;
        }

        float newX = state.Position.X + vx * input.Dt;
        float newY = state.Position.Y + vy * input.Dt;

        bool onGround;
        if (newY <= GroundY)
        {
            newY = GroundY;
            vy = 0f;
            onGround = true;
        }
        else
        {
            onGround = false;
        }

        return new PhysicsState(
            new Vector2(newX, newY),
            new Vector2(vx, vy),
            onGround);
    }
}

/// <summary>
/// 시뮬레이션 한 step의 입력. inputX(-1/0/1) + jumpPressed + dt.
/// readonly struct → 값 전달, GC 압박 0.
/// </summary>
public readonly struct PhysicsInput
{
    public readonly sbyte InputX;
    public readonly bool JumpPressed;
    public readonly float Dt;

    public PhysicsInput(sbyte inputX, bool jumpPressed, float dt)
    {
        InputX = inputX;
        JumpPressed = jumpPressed;
        Dt = dt;
    }
}

/// <summary>
/// 시뮬레이션 한 step의 상태. 위치/속도/ground 플래그.
/// readonly struct → 결정론 안정성 + GC 압박 0.
/// </summary>
public readonly struct PhysicsState
{
    public readonly Vector2 Position;
    public readonly Vector2 Velocity;
    public readonly bool OnGround;

    public PhysicsState(Vector2 position, Vector2 velocity, bool onGround)
    {
        Position = position;
        Velocity = velocity;
        OnGround = onGround;
    }

    /// <summary>spawn / 초기화용 — 위치 외 velocity=0, OnGround는 위치로 추정.</summary>
    public static PhysicsState AtRest(Vector2 position)
        => new PhysicsState(position, Vector2.Zero, position.Y <= 0.0001f);
}

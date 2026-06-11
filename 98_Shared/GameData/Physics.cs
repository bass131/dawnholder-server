namespace Shared.GameData;

using System.Numerics;

/// <summary>
/// 직업별 이동 파라미터. 두 float을 낱개로 넘기지 않는 이유: 둘 다 float이라
/// 호출부에서 자리 바꿈 실수가 컴파일에 안 잡힘 — 필드명으로 방지.
/// readonly struct → 값 전달, GC 압박 0.
/// </summary>
public readonly struct MoveParams
{
    public readonly float MoveSpeed;
    public readonly float JumpVel;

    public MoveParams(float moveSpeed, float jumpVel)
    {
        MoveSpeed = moveSpeed;
        JumpVel   = jumpVel;
    }
}

/// <summary>
/// 시뮬레이션 한 step의 입력. inputX(-1/0/1) + jumpPressed + dt + externalVelX(넉백 임펄스).
/// readonly struct → 값 전달, GC 압박 0.
///
/// ExternalVelX: 기본값 0. 0이면 기존 이동 동작과 완전히 동일 (기존 호출자 전부 불변).
/// 넉백 등 외부 임펄스가 있을 때 3인자 ctor 대신 4인자 ctor을 사용한다.
/// </summary>
public readonly struct PhysicsInput
{
    public readonly sbyte InputX;
    public readonly bool JumpPressed;
    public readonly float Dt;
    /// <summary>
    /// 넉백 등 외부 수평 임펄스 (units/s). 0이면 동작 변화 없음.
    /// InputX * MoveSpeed에 *더해져* 최종 vx를 만든다.
    /// 지형 X-스윕이 vx를 사용하므로 넉백도 자동으로 벽에 막힌다.
    /// </summary>
    public readonly float ExternalVelX;

    // 기존 3인자 ctor — ExternalVelX=0 위임. 기존 호출자 전부 이 ctor을 그대로 쓴다.
    public PhysicsInput(sbyte inputX, bool jumpPressed, float dt)
        : this(inputX, jumpPressed, dt, 0f) { }

    // 4인자 ctor — 넉백 임펄스 있을 때 사용.
    public PhysicsInput(sbyte inputX, bool jumpPressed, float dt, float externalVelX)
    {
        InputX = inputX;
        JumpPressed = jumpPressed;
        Dt = dt;
        ExternalVelX = externalVelX;
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

    /// <summary>지면 Y 좌표. 캐릭터 발바닥이 닿는 높이.</summary>
    public const float GroundY = 0.0f;

    /// <summary>ground 판정 epsilon — float 비교 오차 흡수.</summary>
    private const float GroundEpsilon = 0.0001f;

    /// <summary>
    /// 1 step 결정론 시뮬레이션 (terrain null 위임). move 파라미터 필수 — silent fallback 없음.
    /// </summary>
    public static PhysicsState Step(PhysicsState state, PhysicsInput input, MoveParams move)
        => Step(state, input, null, move);

    /// <summary>
    /// 1 step 결정론 시뮬레이션. 같은 (state, input, terrain, move) → 같은 PhysicsState.
    ///
    /// <para><b>분기 규칙</b>:
    /// terrain이 null이거나 지형이 하나도 없으면 <see cref="StepFlat"/>으로 위임 (평지 fallback).
    /// 그 외엔 지형 경로 (솔리드 AABB + one-way 발판).</para>
    ///
    /// <para><b>지형 경로 순서</b> (재정렬 금지 — 결과가 달라짐):
    ///   1. vx = inputX * MoveSpeed
    ///   2. 지지 판정: vy≤0 &amp;&amp; 솔리드 윗면 or 발판 면 위에 서 있으면 startedOnGround=true
    ///   3. 점프/중력 (StepFlat 동일 구조)
    ///   4. X축 스윕 — 솔리드 측면 차단 (y 범위 [MinY, MaxY) 조건으로 바닥 위 보행 간섭 방지)
    ///   5. Y축 스윕 — 하강: 솔리드 윗면+발판 중 가장 높은 면에 착지. 상승: 솔리드 아랫면 충돌.
    ///      one-way 의미: "시작이 면 위" 조건이 아래→위 통과를 자연 허용.
    ///   6. GroundY clamp 없음 — 지형 구멍 낙하는 Phase 03 kill-plane이 처리.</para>
    /// </summary>
    public static PhysicsState Step(PhysicsState state, PhysicsInput input, MapTerrain? terrain, MoveParams move)
    {
        if (terrain == null || (terrain.Solids.Length == 0 && terrain.Platforms.Length == 0))
            return StepFlat(state, input, move);

        return StepWithTerrain(state, input, terrain, move);
    }

    /// <summary>
    /// 평지 시뮬레이션 (GroundY=0 clamp). 물리 로직 불변 — MoveSpeed/JumpVel만 파라미터로.
    ///
    /// **순서** (재정렬 시 점프 물리 깨짐):
    ///   1. 수평 velocity = inputX * MoveSpeed (즉시 반응, 관성 X)
    ///   2. 시작 시점 ground 판정 (pos.Y와 vy 둘 다 확인)
    ///   3. jumpPressed && startedOnGround → vy = JumpVel (점프 시작)
    ///      그 외 공중일 때 → vy += Gravity * dt (낙하 가속)
    ///   4. 위치 적분 (Euler explicit)
    ///   5. 적분 후 newY <= GroundY → clamp + vy=0 + onGround=true
    ///
    /// **함정 회피**:
    ///   - jump 적용 후 적분에 vy=JumpVel가 박혀 newY > GroundY로 즉시 위로 → onGround=false
    ///     같은 tick 안에서 재점프 시도해도 startedOnGround였던 시점 상태로만 판단 (한 번만 적용)
    ///   - 중력은 *공중일 때만* 적용 → ground에서 vy가 음수로 누적되지 않음
    /// </summary>
    private static PhysicsState StepFlat(PhysicsState state, PhysicsInput input, MoveParams move)
    {
        float vx = input.InputX * move.MoveSpeed + input.ExternalVelX;

        bool startedOnGround = state.Position.Y <= GroundY + GroundEpsilon
                            && state.Velocity.Y <= 0f;

        float vy = state.Velocity.Y;
        if (input.JumpPressed && startedOnGround)
        {
            vy = move.JumpVel;
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

    /// <summary>
    /// 지형 경로 — 솔리드 AABB + one-way 발판 충돌. terrain은 non-null, 비어 있지 않음.
    ///
    /// <para><b>X 차단 조건</b>: y가 [MinY, MaxY) 범위일 때만 측면 차단.
    /// 바닥 윗면(y==MaxY)에서 보행 중 같은 솔리드가 벽으로 작용하지 않도록 윗경계 제외.</para>
    ///
    /// <para><b>착지 조건 (vy≤0)</b>: "시작 y &gt;= faceY-eps" + "newY &lt;= faceY".
    /// one-way 발판은 같은 조건으로 아래서 출발하면 후보 제외 = 위로 통과 자연 허용.</para>
    ///
    /// <para><b>GroundY clamp 없음</b>: 지형 구멍 낙하 허용. kill-plane / 스폰 보정은 Phase 03 소관.</para>
    /// </summary>
    private static PhysicsState StepWithTerrain(PhysicsState state, PhysicsInput input, MapTerrain terrain, MoveParams move)
    {
        float x  = state.Position.X;
        float y  = state.Position.Y;
        float vy = state.Velocity.Y;
        float dt = input.Dt;
        float eps = GroundEpsilon;

        // 1. 수평 velocity
        float vx = input.InputX * move.MoveSpeed + input.ExternalVelX;

        // 2. 지지 판정: vy≤0이고 어떤 솔리드 윗면(MaxY) 또는 발판 면(Y) 위에 서 있는지
        bool startedOnGround = false;
        if (vy <= 0f)
        {
            for (int i = 0; i < terrain.Solids.Length; i++)
            {
                TerrainAabb s = terrain.Solids[i];
                float faceY = s.MaxY;
                if (System.MathF.Abs(y - faceY) <= eps && s.MinX - eps <= x && x <= s.MaxX + eps)
                {
                    startedOnGround = true;
                    break;
                }
            }
            if (!startedOnGround)
            {
                for (int i = 0; i < terrain.Platforms.Length; i++)
                {
                    TerrainPlatform p = terrain.Platforms[i];
                    float faceY = p.Y;
                    if (System.MathF.Abs(y - faceY) <= eps && p.MinX - eps <= x && x <= p.MaxX + eps)
                    {
                        startedOnGround = true;
                        break;
                    }
                }
            }
        }

        // 3. 점프/중력 (StepFlat과 동일 구조)
        if (input.JumpPressed && startedOnGround)
        {
            vy = move.JumpVel;
        }
        else if (!startedOnGround)
        {
            vy += Gravity * dt;
        }

        // 4. X축 스윕: y가 [MinY, MaxY) 범위인 솔리드 측면만 차단 (윗경계 제외)
        float newX = x + vx * dt;
        if (vx > 0f)
        {
            float bestFace = float.MaxValue;
            for (int i = 0; i < terrain.Solids.Length; i++)
            {
                TerrainAabb s = terrain.Solids[i];
                if (y >= s.MinY && y < s.MaxY && x <= s.MinX && newX > s.MinX)
                {
                    if (s.MinX < bestFace) bestFace = s.MinX;
                }
            }
            if (bestFace < float.MaxValue)
            {
                newX = bestFace;
                vx   = 0f;
            }
        }
        else if (vx < 0f)
        {
            float bestFace = float.MinValue;
            for (int i = 0; i < terrain.Solids.Length; i++)
            {
                TerrainAabb s = terrain.Solids[i];
                if (y >= s.MinY && y < s.MaxY && x >= s.MaxX && newX < s.MaxX)
                {
                    if (s.MaxX > bestFace) bestFace = s.MaxX;
                }
            }
            if (bestFace > float.MinValue)
            {
                newX = bestFace;
                vx   = 0f;
            }
        }

        // 5. Y축 스윕
        float newY = y + vy * dt;
        bool onGround = false;

        if (vy <= 0f)
        {
            // 하강/정지: 솔리드 윗면 + 발판 면 중 가장 높은 면에 착지
            float bestFace = float.MinValue;
            for (int i = 0; i < terrain.Solids.Length; i++)
            {
                TerrainAabb s = terrain.Solids[i];
                float faceY = s.MaxY;
                if (s.MinX - eps <= newX && newX <= s.MaxX + eps
                    && y >= faceY - eps
                    && newY <= faceY)
                {
                    if (faceY > bestFace) bestFace = faceY;
                }
            }
            for (int i = 0; i < terrain.Platforms.Length; i++)
            {
                TerrainPlatform p = terrain.Platforms[i];
                float faceY = p.Y;
                if (p.MinX - eps <= newX && newX <= p.MaxX + eps
                    && y >= faceY - eps
                    && newY <= faceY)
                {
                    if (faceY > bestFace) bestFace = faceY;
                }
            }
            if (bestFace > float.MinValue)
            {
                newY     = bestFace;
                vy       = 0f;
                onGround = true;
            }
        }
        else
        {
            // 상승: 솔리드 아랫면(MinY)만 충돌. 발판은 위로 통과 허용 (검사 제외)
            // y < faceY (등호 제외): 바닥에 서서 벽에 붙은 상태(y == 벽.MinY == 바닥 윗면)에서
            // 점프 시 바닥에 묻힌 벽 아랫면이 머리 충돌로 잡히면 점프 불가 — 시작이 면보다
            // 엄격히 아래일 때만 진짜 천장.
            float bestFace = float.MaxValue;
            for (int i = 0; i < terrain.Solids.Length; i++)
            {
                TerrainAabb s = terrain.Solids[i];
                float faceY = s.MinY;
                if (s.MinX - eps <= newX && newX <= s.MaxX + eps
                    && y < faceY
                    && newY >= faceY)
                {
                    if (faceY < bestFace) bestFace = faceY;
                }
            }
            if (bestFace < float.MaxValue)
            {
                newY     = bestFace;
                vy       = 0f;
                onGround = false;
            }
        }

        return new PhysicsState(
            new Vector2(newX, newY),
            new Vector2(vx, vy),
            onGround);
    }
}

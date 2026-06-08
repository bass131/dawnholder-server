using System.Numerics;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Maps.States;
using Shared.GameData;

namespace GameServer.Tests.Maps;

/// <summary>
/// 플레이어 공격 commit window(AttackState) 서버 권위 이동 잠금 검증.
///
/// 검증 대상:
///   1. 공격 중 이동 입력 → 위치 델타 0 (이동 잠금)
///   2. 대량 이동 입력도 commit window 안에서는 우회 불가
///   3. 경계 틱: 정확히 AttackCommitWindowTicks 틱 동안 잠금, (N+1)번째 틱에 복귀
///   4. window 종료 후 이동 재개 (입력이 있으면 실제로 움직임)
///   5. 공격 중 점프 입력 무시 (LocksMovement가 jumpPressed도 0으로 강제)
/// </summary>
public class CommitWindowTests
{
    static GameMap MakeFlatMap() => new GameMap(MapId.Town);

    static PlayerEntity AddGroundedPlayer(GameMap map, float x = 0f)
    {
        PlayerEntity p = map.AddPlayer(null, new Vector2(x, 0f));
        p.OnGround = true;
        p.Velocity = Vector2.Zero;
        return p;
    }

    // ── 1. 공격 중 이동 입력 → 위치 델타 0 ──────────────────────────────────

    [Fact]
    public void AttackState_BlocksMovement_InputXIgnored()
    {
        GameMap map = MakeFlatMap();
        PlayerEntity p = AddGroundedPlayer(map);
        float startX = p.Position.X;

        p.EnterAttackState();
        // 이동 입력 주입 — LocksMovement=true이므로 GameMap.Tick이 inputX=0으로 강제
        p.EnqueueInput(1, false, 1u);
        map.Tick(1);

        // commit window 중 이동 입력이 있어도 X 좌표 변화 없어야 함
        Assert.Equal(startX, p.Position.X);
    }

    // ── 2. 대량 이동 입력도 우회 불가 ───────────────────────────────────────

    [Fact]
    public void AttackState_BlocksMovement_MultipleInputs()
    {
        GameMap map = MakeFlatMap();
        PlayerEntity p = AddGroundedPlayer(map);
        float startX = p.Position.X;

        p.EnterAttackState();
        // 여러 틱 동안 이동 입력 주입
        for (int i = 1; i <= 4; i++)
        {
            p.EnqueueInput(1, false, (uint)i);
            map.Tick(i);
        }

        // 4틱 모두 commit window 안 (AttackCommitWindowTicks=8) → 이동 없어야 함
        Assert.Equal(startX, p.Position.X);
    }

    // ── 3. 경계 틱: AttackCommitWindowTicks 동안 잠금, (N+1)번째에 복귀 ─────

    [Fact]
    public void AttackState_ExpiresAtExactBoundaryTick()
    {
        GameMap map = MakeFlatMap();
        PlayerEntity p = AddGroundedPlayer(map);

        p.EnterAttackState();

        // AttackCommitWindowTicks 틱 동안 이동 입력 → 모두 무시되어야 함
        int window = Constants.AttackCommitWindowTicks;
        for (int i = 1; i <= window; i++)
        {
            p.EnqueueInput(1, false, (uint)i);
            map.Tick(i);
            // window 기간 내에는 AttackState여야 함
            // (마지막 tick에 StateTicksRemaining이 0이 되어 전이 직전 — Tick 완료 후 상태 확인)
        }

        // window 틱 소진 → AttackState가 ResolveGrounded로 전이됨 (Idle or Move)
        // 이제 이동 가능한 상태여야 함
        Assert.False(p.ActionFsm.CurrentState.LocksMovement,
            "AttackState window 완료 후 LocksMovement가 false여야 함");
    }

    // ── 4. window 종료 후 이동 재개 ──────────────────────────────────────────

    [Fact]
    public void AfterAttackState_MovementResumes()
    {
        GameMap map = MakeFlatMap();
        PlayerEntity p = AddGroundedPlayer(map);
        float startX = p.Position.X;

        p.EnterAttackState();

        // window 소진
        int window = Constants.AttackCommitWindowTicks;
        for (int i = 1; i <= window; i++)
            map.Tick(i);

        // window 종료 후 이동 입력 → 실제 이동이 일어나야 함
        p.EnqueueInput(1, false, (uint)(window + 1));
        map.Tick(window + 1);

        Assert.True(p.Position.X > startX,
            "AttackState 종료 후 이동 입력에 따라 X 좌표가 증가해야 함");
    }

    // ── 5. 공격 중 점프 입력 무시 ────────────────────────────────────────────

    [Fact]
    public void AttackState_BlocksJump()
    {
        GameMap map = MakeFlatMap();
        PlayerEntity p = AddGroundedPlayer(map);

        p.EnterAttackState();
        // 점프 입력 주입 — LocksMovement=true이므로 rawJump=false로 강제
        p.EnqueueInput(0, true, 1u);
        map.Tick(1);

        // 점프가 실행됐다면 OnGround=false이어야 하는데, LocksMovement가 막아야 함
        // 평지 맵에서 jump가 실행되면 vy > 0 → OnGround=false
        Assert.True(p.OnGround,
            "AttackState 중 점프 입력은 무시되어야 함 (여전히 OnGround=true)");
    }

    // ── 등가성 검증: 이동 State의 animState 바이트 동일성 ────────────────────

    /// <summary>
    /// (OnGround, vx 부호, attack, hit, dead) 매트릭스에서
    /// ActionFsm.AnimState 바이트가 옛 우선순위 매핑(Death>Hit>Attack>Jump>Walk>Idle)과 동일한지 확인.
    ///
    /// 주의: attack+hit 동시(alive) 케이스는 FSM에서 한 상태만 활성이라 의도적 제외 (주석 명시).
    /// 옛 구현에서는 HitLatchTicks > AttackLatchTicks로 Hit 우선이었음.
    /// 새 FSM에서는 AttackState.InterruptibleByHit=false이면 EnterHitState가 no-op.
    /// → 동시 케이스는 "누가 먼저 진입했냐"에 따라 다르므로 등가성 검증 제외.
    /// </summary>
    [Fact]
    public void AnimState_EquivalenceMatrix_MatchesLegacyPriority()
    {
        // (OnGround, vx, expectedOldAnimState)
        // FSM이 이동 State만 있을 때(전투 없음) 옛 우선순위와 바이트 동일 확인.
        var movementCases = new (bool onGround, float vx, AnimState expected)[]
        {
            (true,  0f,    AnimState.Idle),
            (true,  0.02f, AnimState.Walk),  // > VxEpsilon
            (true, -0.02f, AnimState.Walk),
            (false, 0f,    AnimState.Jump),
            (false, 2f,    AnimState.Jump),  // 공중이면 vx 무관하게 Jump
        };

        foreach ((bool onGround, float vx, AnimState expected) in movementCases)
        {
            GameMap map = MakeFlatMap();
            PlayerEntity p = AddGroundedPlayer(map);
            p.OnGround = onGround;
            p.Velocity = new Vector2(vx, 0f);
            // FSM이 물리 상태를 반영하도록 Tick 1회
            p.ActionFsm.Tick(p);

            Assert.Equal((byte)expected, (byte)p.ActionFsm.AnimState);
        }

        // VxEpsilon 경계: vx==VxEpsilon → Idle (등호는 Move 아님)
        {
            GameMap map = MakeFlatMap();
            PlayerEntity p = AddGroundedPlayer(map);
            p.OnGround = true;
            p.Velocity = new Vector2(PlayerMovementStates.VxEpsilon, 0f);
            p.ActionFsm.Tick(p);
            Assert.Equal((byte)AnimState.Idle, (byte)p.ActionFsm.AnimState);
        }

        // VxEpsilon 바로 위: vx = VxEpsilon + 0.001f → Walk
        {
            GameMap map = MakeFlatMap();
            PlayerEntity p = AddGroundedPlayer(map);
            p.OnGround = true;
            p.Velocity = new Vector2(PlayerMovementStates.VxEpsilon + 0.001f, 0f);
            p.ActionFsm.Tick(p);
            Assert.Equal((byte)AnimState.Walk, (byte)p.ActionFsm.AnimState);
        }
    }
}

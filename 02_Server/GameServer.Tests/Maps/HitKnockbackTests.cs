using System.Numerics;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Maps.States;
using Shared.GameData;

namespace GameServer.Tests.Maps;

/// <summary>
/// HitState 피격 hitstun + 넉백 서버 권위 검증.
///
/// 검증 대상:
///   1. HitState 중 이동 입력 무시 — 위치는 넉백만으로 변화
///   2. 넉백 방향: 공격자 반대 방향 (dirX 부호 반전)
///   3. hitstun 동안 ExternalImpulseVx 감쇠 → 0 수렴
///   4. 지형 벽 막힘: ExternalVelX가 벽에 막히는지 (terrain X-스윕)
///   5. 불가침 commit 중 피격 → EnterHitState no-op (ExternalImpulseVx=0 불변)
///   6. ExternalVelX=0이면 기존 이동 동작과 동일 (PhysicsInput 3인자 ctor 호환)
/// </summary>
public class HitKnockbackTests
{
    static GameMap MakeFlatMap() => new GameMap(MapId.Town);

    static PlayerEntity AddGroundedPlayer(GameMap map, float x = 0f)
    {
        PlayerEntity p = map.AddPlayer(null, new Vector2(x, 0f));
        p.OnGround = true;
        p.Velocity = Vector2.Zero;
        return p;
    }

    // ── 1. HitState 중 이동 입력 무시 ────────────────────────────────────────

    [Fact]
    public void HitState_IgnoresInputX_PositionChangedByKnockbackOnly()
    {
        GameMap map = MakeFlatMap();
        // 충분한 오른쪽 공간을 위해 x=0 시작
        PlayerEntity p = AddGroundedPlayer(map, 0f);

        // dirX=+1: 오른쪽으로 날아감 (ExternalImpulseVx > 0). 공격자는 왼쪽에 있다고 가정.
        // 반대 방향(-1) 이동 입력이 LocksMovement로 무시되는지 확인.
        p.EnterHitState(1f);
        Assert.True(p.ExternalImpulseVx > 0f, "dirX=+1 → 오른쪽 넉백 (양수 ExternalImpulseVx)");

        // 넉백 반대 방향(-1) 이동 입력 주입 → LocksMovement가 막아야 함
        p.EnqueueInput(-1, false, 1u);
        float xBefore = p.Position.X;
        map.Tick(1);
        float xAfter = p.Position.X;

        // LocksMovement=true이므로 inputX=-1은 무시 → 넉백 방향(+)으로만 이동
        Assert.True(xAfter >= xBefore,
            "HitState 중 이동 입력은 무시되어야 함. 넉백 방향(오른쪽)으로만 이동.");
    }

    // ── 2. 넉백 방향: dirX 부호와 동일 방향 ────────────────────────────────
    //
    // dirX는 "플레이어가 바라보는 상대적 방향" 또는 "플레이어-보스 위치 관계에 따른 방향".
    // BossBehaviorSystem: dirX = player.X >= boss.X ? 1f : -1f
    //   → 플레이어가 보스 오른쪽에 있으면 dirX=1 → 오른쪽(+)으로 날아감(보스 반대 방향).
    // EnterHitState: ExternalImpulseVx = KnockbackInitialVx * Sign(dirX) → dirX와 같은 부호.

    [Fact]
    public void HitState_KnockbackDirection_SameAsDirX()
    {
        // dirX=+1 → ExternalImpulseVx > 0 (오른쪽)
        {
            GameMap map = MakeFlatMap();
            PlayerEntity p = AddGroundedPlayer(map, 5f);
            p.EnterHitState(1f);
            Assert.True(p.ExternalImpulseVx > 0f,
                "dirX=+1 → ExternalImpulseVx 양수(오른쪽 방향)여야 함");
        }

        // dirX=-1 → ExternalImpulseVx < 0 (왼쪽)
        {
            GameMap map = MakeFlatMap();
            PlayerEntity p = AddGroundedPlayer(map, 5f);
            p.EnterHitState(-1f);
            Assert.True(p.ExternalImpulseVx < 0f,
                "dirX=-1 → ExternalImpulseVx 음수(왼쪽 방향)여야 함");
        }

        // dirX=0 → 기본 오른쪽(+1) fallback
        {
            GameMap map = MakeFlatMap();
            PlayerEntity p = AddGroundedPlayer(map, 5f);
            p.EnterHitState(0f);
            // dirX=0이면 MathF.Sign(1f)=1 → ExternalImpulseVx = KnockbackInitialVx * 1 > 0
            Assert.True(p.ExternalImpulseVx > 0f,
                "dirX=0 → 오른쪽 기본(+1) fallback. ExternalImpulseVx 양수여야 함");
        }
    }

    // ── 3. hitstun 동안 ExternalImpulseVx 감쇠 → 0 수렴 ────────────────────

    [Fact]
    public void HitState_KnockbackDecays_ToZero()
    {
        GameMap map = MakeFlatMap();
        PlayerEntity p = AddGroundedPlayer(map, 10f); // 넉백 여유 공간

        p.EnterHitState(1f); // dirX=+1 → 오른쪽 넉백
        Assert.True(p.ExternalImpulseVx > 0f);

        // HitState 지속(AnimLatchTicks) 틱 동안 감쇠 진행
        for (int i = 1; i <= Dawnholder.Server.GameServer.Combat.CombatConstants.AnimLatchTicks; i++)
            map.Tick(i);

        // AnimLatchTicks 틱 후 HitState 종료 → Exit에서 ExternalImpulseVx=0 보장
        Assert.Equal(0f, p.ExternalImpulseVx);
    }

    // ── 4. 지형 벽 막힘 ───────────────────────────────────────────────────────

    [Fact]
    public void HitState_Knockback_StoppedByWall()
    {
        // 솔리드 벽 x=[3,4] 구성. 플레이어 x=0에서 시작.
        // dirX=+1 → ExternalImpulseVx > 0 → 오른쪽 넉백 → 벽(x=3 MinX)에 막혀야 함.
        TerrainAabb wall = new TerrainAabb(3f, -1f, 4f, 5f);
        MapTerrain terrain = new MapTerrain(
            new TerrainAabb[] { wall },
            new TerrainPlatform[0],
            killPlaneY: -10f
        );
        GameMap map = new GameMap(MapId.Town, terrain: terrain);
        PlayerEntity p = map.AddPlayer(null, new Vector2(0f, 0f));
        p.OnGround = true;
        p.Velocity = Vector2.Zero;

        // dirX=+1 → ExternalImpulseVx > 0 (오른쪽 넉백) → 벽(MinX=3)에 막혀야 함
        p.EnterHitState(1f);
        Assert.True(p.ExternalImpulseVx > 0f);

        // 여러 틱 진행
        for (int i = 1; i <= 3; i++)
            map.Tick(i);

        // 벽(MinX=3)을 넘어서지 않아야 함 (X-스윕이 차단)
        Assert.True(p.Position.X <= 3f,
            $"넉백이 벽(x=3)을 넘으면 안 됨. 현재 x={p.Position.X}");
    }

    // ── 5. 불가침 commit 중 피격 → EnterHitState no-op ───────────────────────

    [Fact]
    public void AttackState_InterruptibleByHit_False_BlocksEnterHitState()
    {
        GameMap map = MakeFlatMap();
        PlayerEntity p = AddGroundedPlayer(map);

        p.EnterAttackState();

        // AttackState.InterruptibleByHit=false이므로 EnterHitState가 no-op이어야 함
        p.EnterHitState(1f);

        // ExternalImpulseVx는 변화 없어야 함 (no-op이므로 초기값 0 유지)
        // AttackState(InterruptibleByHit=false) 중 EnterHitState는 ExternalImpulseVx를 설정하면 안 됨.
        Assert.Equal(0f, p.ExternalImpulseVx);

        // ActionFsm 상태도 여전히 AttackState여야 함
        Assert.IsType<AttackState>(p.ActionFsm.CurrentState);
    }

    // ── 6. ExternalVelX=0이면 기존 이동 동작 불변 ────────────────────────────

    [Fact]
    public void PhysicsInput_ExternalVelX_Zero_PreservesOriginalBehavior()
    {
        // 3인자 ctor(ExternalVelX=0)와 4인자 ctor(ExternalVelX=0)의 결과가 동일한지 확인
        var state = new PhysicsState(new Vector2(0f, 0f), Vector2.Zero, true);
        var move  = new MoveParams(5f, 10f);

        var input3 = new PhysicsInput(1, false, Constants.TickDuration);
        var input4 = new PhysicsInput(1, false, Constants.TickDuration, 0f);

        PhysicsState result3 = Physics.Step(state, input3, move);
        PhysicsState result4 = Physics.Step(state, input4, move);

        Assert.Equal(result3.Position.X, result4.Position.X);
        Assert.Equal(result3.Position.Y, result4.Position.Y);
        Assert.Equal(result3.Velocity.X, result4.Velocity.X);
    }

    // ── 넉백이 실제로 이동에 반영되는지 확인 ──────────────────────────────────

    [Fact]
    public void HitState_KnockbackVx_MovesPlayerInCorrectDirection()
    {
        GameMap map = MakeFlatMap();
        PlayerEntity p = AddGroundedPlayer(map, 0f);
        float startX = p.Position.X;

        // dirX=+1 → ExternalImpulseVx > 0 → 오른쪽(+X)으로 날아감
        p.EnterHitState(1f);
        Assert.True(p.ExternalImpulseVx > 0f);

        map.Tick(1);

        // 넉백 방향(오른쪽)으로 이동했어야 함
        Assert.True(p.Position.X > startX,
            $"dirX=+1 넉백 후 오른쪽으로 이동해야 함. startX={startX}, 현재={p.Position.X}");
    }
}

using System.Numerics;
using Dawnholder.Server.GameServer.Maps;
using Shared.GameData;

namespace Dawnholder.Server.GameServer.Tests;

// GameMap의 intent 적용 + snapshot 브로드캐스트 동작 검증.
//
// GameSession 측 검증(rate-limit, range cheat-log)은 PacketSession 상속 + ServerCore
// 의존이라 단위 테스트보단 통합 테스트 영역.
public class MoveIntentTests
{
    [Fact]
    public void Tick_AppliesPendingInputX_RightDirection()
    {
        GameMap map = new GameMap();
        PlayerEntity e = map.AddPlayer(null, new Vector2(0f, 0f));
        e.PendingInputX = 1;

        map.Tick(1);

        // 1 tick = 1 * MoveSpeed * TickDuration = 5 * 0.05 = 0.25
        float expected = Constants.MoveSpeed * Constants.TickDuration;
        Assert.Equal(expected, e.Position.X, 4);
        Assert.Equal((sbyte)0, e.PendingInputX); // 적용 후 리셋
    }

    [Fact]
    public void Tick_AppliesPendingInputX_LeftDirection()
    {
        GameMap map = new GameMap();
        PlayerEntity e = map.AddPlayer(null, new Vector2(0f, 0f));
        e.PendingInputX = -1;

        map.Tick(1);

        float expected = -Constants.MoveSpeed * Constants.TickDuration;
        Assert.Equal(expected, e.Position.X, 4);
    }

    [Fact]
    public void Tick_NoMovementWhenPendingInputZero()
    {
        GameMap map = new GameMap();
        PlayerEntity e = map.AddPlayer(null, new Vector2(0f, 0f));

        map.Tick(1);

        Assert.Equal(0f, e.Position.X);
    }

    [Fact]
    public void Tick_ContinuousInputAccumulates()
    {
        GameMap map = new GameMap();
        PlayerEntity e = map.AddPlayer(null, new Vector2(0f, 0f));

        // 클라가 키를 계속 누르고 있는 시나리오: 매 tick 새 intent 도착.
        for (int i = 0; i < 20; i++)
        {
            e.PendingInputX = 1;
            map.Tick(i + 1);
        }

        // 20 tick (=1초) 동안 누적 = MoveSpeed * 1.0s = 5.0
        Assert.Equal(Constants.MoveSpeed * 1.0f, e.Position.X, 3);
    }

    [Fact]
    public void Tick_DoesNotMutateYPosition()
    {
        GameMap map = new GameMap();
        PlayerEntity e = map.AddPlayer(null, new Vector2(0f, 0f));
        e.PendingInputX = 1;

        map.Tick(1);

        Assert.Equal(0f, e.Position.Y);
    }

    // === GameMap.Tick의 Physics.Step 위임 검증 ===
    // (단위 시나리오는 PhysicsTests가 검증, 본 묶음은 GameMap.Tick wire 회귀)

    [Fact]
    public void Tick_JumpPressed_OnGround_AppliesJumpVelocity()
    {
        // PendingJumpPressed=true + OnGround=true (기본값) → vy=JumpSpeed
        GameMap map = new GameMap();
        PlayerEntity e = map.AddPlayer(null, new Vector2(0f, 0f));
        e.PendingJumpPressed = true;

        map.Tick(1);

        // vy = JumpSpeed (8) → newY = 8 * 0.05 = 0.4
        Assert.Equal(Shared.GameData.Physics.JumpSpeed, e.Velocity.Y, 4);
        Assert.Equal(Shared.GameData.Physics.JumpSpeed * Constants.TickDuration, e.Position.Y, 4);
        Assert.False(e.OnGround);
    }

    [Fact]
    public void Tick_JumpInAir_Ignored()
    {
        // 공중에서 jumpPressed 무시 (더블점프 차단)
        GameMap map = new GameMap();
        PlayerEntity e = map.AddPlayer(null, new Vector2(0f, 1f));
        e.Velocity = new Vector2(0f, 5f);  // 점프 상승 중
        e.OnGround = false;
        e.PendingJumpPressed = true;

        map.Tick(1);

        // vy = 5 + Gravity*dt = 5 + (-20)*0.05 = 4.0 (jumpPressed 무시, 중력만)
        float expectedVy = 5f + Shared.GameData.Physics.Gravity * Constants.TickDuration;
        Assert.Equal(expectedVy, e.Velocity.Y, 4);
    }

    [Fact]
    public void Tick_ResetsPendingFlags_AfterStep()
    {
        // PendingInputX + PendingJumpPressed 모두 reset (다음 tick 누적 차단)
        GameMap map = new GameMap();
        PlayerEntity e = map.AddPlayer(null, new Vector2(0f, 0f));
        e.PendingInputX = 1;
        e.PendingJumpPressed = true;

        map.Tick(1);

        Assert.Equal((sbyte)0, e.PendingInputX);
        Assert.False(e.PendingJumpPressed);  // 에지 처리 — 1tick 후 false (D4 (a))
    }

    [Fact]
    public void EnqueueJob_MarshalsToTickThread()
    {
        // ConcurrentQueue 마샬링 검증 — IOCP가 EnqueueJob 호출 → Tick에서 실행.
        GameMap map = new GameMap();
        bool jobRan = false;

        map.EnqueueJob(() => jobRan = true);
        Assert.False(jobRan); // Tick 호출 전엔 실행 X

        map.Tick(1);
        Assert.True(jobRan);
    }
}

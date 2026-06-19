using System.Numerics;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Entities;
using Shared.GameData;

namespace Dawnholder.Server.GameServer.Tests;

// GameMap의 intent 적용 + snapshot 브로드캐스트 동작 검증.
//
// GameSession 측 검증(rate-limit, range cheat-log)은 PacketSession 상속 + ServerCore
// 의존이라 단위 테스트보단 통합 테스트 영역.
//
// AddPlayer(null stats) → PlayerStats.Knight() 기본값 → MoveSpeed=4, JumpVel=8.
// 기존 5.0 기준 기대값은 4.0 기준으로 정직 재계산 (명세 §E 지침).
public class MoveIntentTests
{
    // Knight 기본 스탯 (테스트 기준값 단일화)
    static readonly PlayerStats KnightStats = PlayerStats.Knight();

    [Fact]
    public void Tick_AppliesPendingInputX_RightDirection()
    {
        GameMap map = new GameMap();
        PlayerEntity e = map.AddPlayer(null, new Vector2(0f, 0f));
        e.EnqueueInput(1, false, 1u);

        map.Tick(1);

        // 1 tick = 1 * MoveSpeed * TickDuration = 4 * 0.05 = 0.20
        float expected = KnightStats.MoveSpeed * Constants.TickDuration;
        Assert.Equal(expected, e.Position.X, 4);
        Assert.Equal(0, e.InputQueueCount);
    }

    [Fact]
    public void Tick_AppliesPendingInputX_LeftDirection()
    {
        GameMap map = new GameMap();
        PlayerEntity e = map.AddPlayer(null, new Vector2(0f, 0f));
        e.EnqueueInput(-1, false, 1u);

        map.Tick(1);

        float expected = -KnightStats.MoveSpeed * Constants.TickDuration;
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
            e.EnqueueInput(1, false, (uint)(i + 1));
            map.Tick(i + 1);
        }

        // 20 tick (=1초) 동안 누적 = MoveSpeed * 1.0s = 4.0
        Assert.Equal(KnightStats.MoveSpeed * 1.0f, e.Position.X, 3);
    }

    // 같은 맵·같은 입력에서 직업별 MoveParams가 엔티티 단위로 주입되는지 (Phase 04 완료 조건).
    [Fact]
    public void Tick_SameInput_KnightAndMage_MoveAtClassSpeed()
    {
        GameMap map = new GameMap();
        PlayerEntity knight = map.AddPlayer(null, new Vector2(0f, 0f), PlayerStats.Knight());
        PlayerEntity mage  = map.AddPlayer(null, new Vector2(0f, 0f), PlayerStats.Mage());

        for (int i = 0; i < 20; i++)
        {
            knight.EnqueueInput(1, false, (uint)(i + 1));
            mage.EnqueueInput(1, false, (uint)(i + 1));
            map.Tick(i + 1);
        }

        Assert.Equal(4f, knight.Position.X, 3);
        Assert.Equal(6f, mage.Position.X, 3);
    }

    [Fact]
    public void Tick_DoesNotMutateYPosition()
    {
        GameMap map = new GameMap();
        PlayerEntity e = map.AddPlayer(null, new Vector2(0f, 0f));
        e.EnqueueInput(1, false, 1u);

        map.Tick(1);

        Assert.Equal(0f, e.Position.Y);
    }

    // === GameMap.Tick의 Physics.Step 위임 검증 ===
    // (단위 시나리오는 PhysicsTests가 검증, 본 묶음은 GameMap.Tick wire 회귀)

    [Fact]
    public void Tick_JumpPressed_OnGround_AppliesJumpVelocity()
    {
        // jumpPressed=true + OnGround=true (기본값) → vy=JumpVel
        GameMap map = new GameMap();
        PlayerEntity e = map.AddPlayer(null, new Vector2(0f, 0f));
        e.EnqueueInput(0, true, 1u);

        map.Tick(1);

        // vy = JumpVel (8) → newY = 8 * 0.05 = 0.4
        Assert.Equal(KnightStats.JumpVel, e.Velocity.Y, 4);
        Assert.Equal(KnightStats.JumpVel * Constants.TickDuration, e.Position.Y, 4);
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
        e.EnqueueInput(0, true, 1u);

        map.Tick(1);

        // vy = 5 + Gravity*dt = 5 + (-20)*0.05 = 4.0 (jumpPressed 무시, 중력만)
        float expectedVy = 5f + Shared.GameData.Physics.Gravity * Constants.TickDuration;
        Assert.Equal(expectedVy, e.Velocity.Y, 4);
    }

    [Fact]
    public void Tick_ConsumesOneInputPerTick()
    {
        // 큐에 입력 1개 → 적용 후 큐 비어있음. 단일 소비 불변식.
        GameMap map = new GameMap();
        PlayerEntity e = map.AddPlayer(null, new Vector2(0f, 0f));
        e.EnqueueInput(1, false, 1u);

        map.Tick(1);

        Assert.Equal(0, e.InputQueueCount);
    }

    [Fact]
    public void EnqueueJob_MarshalsToTickThread()
    {
        // ConcurrentQueue 마샬링 검증 — IOCP가 EnqueueJob 호출 → Tick에서 실행.
        GameMap map = new GameMap();
        bool jobRan = false;

        map.EnqueueJob(() => jobRan = true);
        Assert.False(jobRan);

        map.Tick(1);
        Assert.True(jobRan);
    }
}

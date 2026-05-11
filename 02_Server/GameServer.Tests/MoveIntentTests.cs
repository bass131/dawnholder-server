using System.Numerics;
using Dawnholder.Server.GameServer.Maps;
using Shared.GameData;

namespace Dawnholder.Server.GameServer.Tests;

// Phase 04 (M2): GameMap의 intent 적용 + snapshot 브로드캐스트 동작 검증.
//
// GameSession 측 검증(rate-limit, range cheat-log)은 PacketSession 상속 + ServerCore
// 의존이라 단위 테스트보단 통합 테스트 영역. 본 Phase는 manual 검증으로 갈음.
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

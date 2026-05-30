using System.Numerics;
using Dawnholder.Server.GameServer.Maps;
using Shared.GameData;

namespace Dawnholder.Server.GameServer.Tests;

/// <summary>
/// 입력 큐 단위 테스트 (Phase 10 — rubber-band 근본 fix).
///
/// 검증 (스펙 §테스트 a~d):
///   (a) coalescing 방지 — 틱 사이 입력 2개 enqueue → 둘 다 보존, 2틱에 걸쳐 순서대로 적용.
///   (b) 빈 틱 ack 불변 — 큐 빈 틱 Tick 호출 → LastClientTick 안 올라감 + neutral 적용(vx=0).
///   (c) FIFO 순서 + 적용 tick = ack — 입력 3개 순서대로 → 매 틱 적용한 입력의 clientTick이 LastClientTick에 반영.
///   (d) 큐 상한 drop — 상한+1개 enqueue → oldest drop, count=상한 유지.
/// </summary>
public class InputQueueTests
{
    // ── (a) coalescing 방지 ──────────────────────────────────────────────────

    [Fact]
    public void TwoInputsInOneTick_BothPreserved_AppliedOverTwoTicks()
    {
        // 한 틱 사이에 입력 2개 도착 → 둘 다 큐에 보존 (drop 0).
        // 틱 1: 첫 번째 입력(+1) 적용 → 이동.
        // 틱 2: 두 번째 입력(-1) 적용 → 반대 이동.
        GameMap map = new GameMap();
        PlayerEntity e = map.AddPlayer(null, new Vector2(0f, 0f));

        // tick 전에 2개 enqueue (coalescing 상황 재현).
        e.EnqueueInput(1, false, clientTick: 10u);
        e.EnqueueInput(-1, false, clientTick: 11u);

        Assert.Equal(2, e.InputQueueCount); // 둘 다 보존

        map.Tick(1); // +1 소비

        float posAfterTick1 = e.Position.X;
        Assert.True(posAfterTick1 > 0f, "첫 번째 입력(+1)이 적용되지 않았다");
        Assert.Equal(1, e.InputQueueCount); // 1개 남음

        map.Tick(2); // -1 소비

        // 틱 2 후 위치 = 원점 복귀 (거리 동일, 방향 반대).
        Assert.Equal(0f, e.Position.X, 4);
        Assert.Equal(0, e.InputQueueCount); // 큐 비어있음
    }

    // ── (b) 빈 틱 ack 불변 ────────────────────────────────────────────────────

    [Fact]
    public void EmptyQueueTick_LastClientTickUnchanged_NeutralApplied()
    {
        // 큐가 빈 상태에서 Tick → LastClientTick 불변 + 수평 이동 없음.
        // neutral 적용: inputX=0이므로 vx=0 → position.X 변화 없음.
        GameMap map = new GameMap();
        PlayerEntity e = map.AddPlayer(null, new Vector2(0f, 0f));
        e.LastClientTick = 99u; // 임의 초기값

        map.Tick(1); // 큐 비어있음

        Assert.Equal(99u, e.LastClientTick); // 불변 — 적용 안 한 입력을 ack하면 reconcile 무력화
        Assert.Equal(0f, e.Position.X);      // neutral(0) 적용 → 이동 없음
    }

    [Fact]
    public void EmptyQueueTick_AfterPreviousInput_LastClientTickFrozen()
    {
        // 틱 1: 입력 있음 → LastClientTick 갱신.
        // 틱 2: 큐 빔 → LastClientTick 불변 (클라 replay 범위 보존).
        GameMap map = new GameMap();
        PlayerEntity e = map.AddPlayer(null, new Vector2(0f, 0f));

        e.EnqueueInput(1, false, clientTick: 5u);
        map.Tick(1);
        Assert.Equal(5u, e.LastClientTick); // 적용된 틱의 clientTick

        // 틱 2: 큐 비어있음
        map.Tick(2);
        Assert.Equal(5u, e.LastClientTick); // 갱신 없음
    }

    // ── (c) FIFO 순서 + 적용 tick = ack ──────────────────────────────────────

    [Fact]
    public void ThreeInputs_FifoOrder_EachTickAcksAppliedClientTick()
    {
        // 입력 3개를 순서대로 enqueue → 3틱에 걸쳐 FIFO 순서로 적용.
        // 매 틱 적용된 입력의 clientTick이 LastClientTick에 반영.
        GameMap map = new GameMap();
        PlayerEntity e = map.AddPlayer(null, new Vector2(0f, 0f));

        e.EnqueueInput(1,  false, clientTick: 100u);
        e.EnqueueInput(-1, false, clientTick: 101u);
        e.EnqueueInput(0,  true,  clientTick: 102u);

        map.Tick(1);
        Assert.Equal(100u, e.LastClientTick); // 첫 번째 적용

        map.Tick(2);
        Assert.Equal(101u, e.LastClientTick); // 두 번째 적용

        map.Tick(3);
        Assert.Equal(102u, e.LastClientTick); // 세 번째 적용

        Assert.Equal(0, e.InputQueueCount);
    }

    // ── (d) 큐 상한 drop ──────────────────────────────────────────────────────

    [Fact]
    public void QueueCapExceeded_OldestDropped_CountStaysAtMax()
    {
        // MaxInputQueue(6)개 이상 enqueue → oldest drop, count = 6 유지.
        // PlayerEntity.MaxInputQueue는 private const → InputQueueCount로 간접 확인.
        GameMap map = new GameMap();
        PlayerEntity e = map.AddPlayer(null, new Vector2(0f, 0f));

        // 6개 enqueue → 상한 도달
        for (uint i = 1; i <= 6; i++)
            e.EnqueueInput(1, false, clientTick: i);

        Assert.Equal(6, e.InputQueueCount);

        // 7번째 enqueue → oldest(clientTick=1) drop, count 여전히 6
        e.EnqueueInput(1, false, clientTick: 7u);
        Assert.Equal(6, e.InputQueueCount);
    }

    [Fact]
    public void QueueCapExceeded_OldestIsDropped_NotNewest()
    {
        // oldest drop 정책 검증: 7번째 enqueue 후 dequeue하면 clientTick=2(원래 2번째)가 나와야 함.
        // (oldest=1이 drop됐으므로 앞쪽부터 2,3,4,5,6,7 순서).
        GameMap map = new GameMap();
        PlayerEntity e = map.AddPlayer(null, new Vector2(0f, 0f));

        for (uint i = 1; i <= 6; i++)
            e.EnqueueInput(1, false, clientTick: i);

        // 7번째 → oldest(clientTick=1) drop
        e.EnqueueInput(1, false, clientTick: 7u);

        // Tick 6번 → 순서대로 clientTick = 2, 3, 4, 5, 6, 7 이어야 함
        uint[] expectedTicks = [2u, 3u, 4u, 5u, 6u, 7u];
        for (int i = 0; i < expectedTicks.Length; i++)
        {
            map.Tick(i + 1);
            Assert.Equal(expectedTicks[i], e.LastClientTick);
        }

        Assert.Equal(0, e.InputQueueCount);
    }

    // ── 추가 회귀: neutral 틱에서 세계는 계속 흐름(중력) ────────────────────

    [Fact]
    public void EmptyQueueTick_GravityStillApplied_WhenAirborne()
    {
        // 큐 빈 틱이어도 Physics.Step(0, false) 1회 실행 → 중력 적용.
        // "세계는 계속 흐름" 불변식.
        GameMap map = new GameMap();
        PlayerEntity e = map.AddPlayer(null, new Vector2(0f, 1f)); // 공중
        e.Velocity = new Vector2(0f, 0f);
        e.OnGround = false;

        map.Tick(1); // 큐 비어있음 — neutral 입력

        // 중력으로 Y가 감소해야 함
        Assert.True(e.Position.Y < 1f, "neutral 틱에서 중력이 적용되지 않았다");
    }
}

using Dawnholder.Server.GameServer.Loop;

namespace Dawnholder.Server.GameServer.Tests;

// 시간 의존 테스트라 ±2~3 tick 정도 flaky 위험 있어 허용 오차 넉넉히.
// CI에서도 통과해야 하므로 18~22 범위 (PRD 20 TPS ±10%).
public class TickSchedulerTests
{
    [Fact]
    public void FiresApproximately20TicksPerSecond()
    {
        int count = 0;
        TickScheduler scheduler = new TickScheduler(_ => Interlocked.Increment(ref count));

        scheduler.Start();
        Thread.Sleep(1000); // 테스트 코드는 헌법 #5 적용 영역 아님 (게임 루프 X).
        scheduler.Stop();

        Assert.InRange(count, 18, 22);
    }

    [Fact]
    public void StopHaltsTickIncrement()
    {
        TickScheduler scheduler = new TickScheduler(_ => { });

        scheduler.Start();
        Thread.Sleep(300); // ~6 tick
        scheduler.Stop();

        long afterStop = scheduler.CurrentTick;
        Thread.Sleep(200);
        long laterCheck = scheduler.CurrentTick;

        Assert.Equal(afterStop, laterCheck);
    }

    [Fact]
    public void TickNumberIsMonotonicallyIncreasing()
    {
        List<long> observed = new();
        object listLock = new();

        TickScheduler scheduler = new TickScheduler(tick =>
        {
            lock (listLock) observed.Add(tick);
        });

        scheduler.Start();
        Thread.Sleep(500);
        scheduler.Stop();

        Assert.True(observed.Count >= 8);
        for (int i = 1; i < observed.Count; i++)
            Assert.True(observed[i] > observed[i - 1], $"tick {observed[i]} <= prev {observed[i - 1]}");
    }
}

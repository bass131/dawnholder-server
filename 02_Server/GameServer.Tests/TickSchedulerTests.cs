using Dawnholder.Server.GameServer.Loop;

namespace Dawnholder.Server.GameServer.Tests;

// 시간 의존 테스트라 ±2~3 tick 정도 flaky 위험 있어 허용 오차 넉넉히.
// CI에서도 통과해야 하므로 18~22 범위 (PRD 20 TPS ±10%).
public class TickSchedulerTests
{
    [Fact]
    public void StopTimeout_PreservesRunningTask_AndRetryWaitsForActualExit()
    {
        using ManualResetEventSlim entered = new();
        using ManualResetEventSlim release = new();
        TickScheduler scheduler = new(_ => { entered.Set(); release.Wait(TimeSpan.FromSeconds(10)); });
        scheduler.Start();
        try
        {
            Assert.True(entered.Wait(TimeSpan.FromSeconds(3)));
            Assert.Throws<TimeoutException>(() => scheduler.Stop(TimeSpan.FromMilliseconds(30)));
            Assert.Throws<InvalidOperationException>(() => scheduler.Start());
        }
        finally { release.Set(); scheduler.Stop(TimeSpan.FromSeconds(3)); }
        long stoppedTick = scheduler.CurrentTick;
        scheduler.Stop(); // repeated stop remains harmless
        Assert.Equal(stoppedTick, scheduler.CurrentTick);
    }

    [Fact]
    public async Task StopFromTick_IsRejectedWithoutDeadlockingExternalStop()
    {
        TaskCompletionSource<Exception?> observed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TickScheduler? scheduler = null;
        scheduler = new TickScheduler(_ => observed.TrySetResult(Record.Exception(() => scheduler!.Stop())));
        scheduler.Start();
        try
        {
            Assert.IsType<InvalidOperationException>(await observed.Task.WaitAsync(TimeSpan.FromSeconds(3)));
        }
        finally { scheduler.Stop(); }
    }

    [Fact]
    public void FaultedRunTask_IsReportedByStop()
    {
        using ManualResetEventSlim faultEntered = new();
        TickScheduler scheduler = new(_ => { });
        InvalidOperationException failure = new("injected metrics callback failure");
        scheduler.OnMetricsSnapshot += _ => { faultEntered.Set(); throw failure; };
        scheduler.Start();
        try
        {
            Assert.True(faultEntered.Wait(TimeSpan.FromSeconds(4)));
            AggregateException error = Assert.Throws<AggregateException>(() => scheduler.Stop());
            Assert.Contains(failure, error.Flatten().InnerExceptions);
        }
        finally
        {
            // A fault must be observed, but this task has already exited; no process is left running.
            try { scheduler.Stop(); } catch (AggregateException) { }
        }
    }

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

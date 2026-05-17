using System.Diagnostics;
using Shared.GameData;

namespace Dawnholder.Server.GameServer.Loop;

// Phase 02 (M2): 50ms 고정 간격으로 OnTick 콜백을 부르는 스케줄러.
//
// **헌법 #5** ("틱 루프 블로킹 금지"): Task.Delay / Thread.Sleep 사용 금지.
// 대신 SpinWait.SpinUntil — 내부적으로 짧은 spin → Thread.Yield 패턴이라
// CPU 100% 점유 안 함. 명시적 Sleep 호출 없음(헌법 정신 부합).
//
// **Drift 보정**: "이전 tick + 50ms"가 아니라 "시작 시각 + N×50ms" 절대 기준.
// 전자는 매 tick OS scheduler 오차가 누적, 후자는 매 tick 흡수됨.
// 30초 돌리면 정확히 ~600 tick(±5).
//
// **단일 thread 보장**: 백그라운드 Task 1개로 run. OnTick 콜백은 항상 같은
// thread에서 호출됨 → GameMap (actor) 안에서 lock 불필요.
public class TickScheduler
{
    readonly Action<long> _onTick;
    CancellationTokenSource? _cts;
    Task? _runTask;

    long _tickNumber;
    public long CurrentTick => Interlocked.Read(ref _tickNumber);

    // Phase 08 Step 4: 통합 테스트에서 p99 검증 가능하도록 외부 hook 노출.
    // 매 1초마다(= ServerTickRate tick) snapshot 발화. 콘솔 출력과 동시.
    public event Action<TickMetrics.Stats>? OnMetricsSnapshot;

    public TickScheduler(Action<long> onTick)
    {
        _onTick = onTick ?? throw new ArgumentNullException(nameof(onTick));
    }

    public void Start()
    {
        if (_runTask != null) throw new InvalidOperationException("이미 Start 됨");
        _cts = new CancellationTokenSource();
        _runTask = Task.Factory.StartNew(
            () => RunLoop(_cts.Token),
            _cts.Token,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
    }

    public void Stop()
    {
        if (_cts == null) return;
        _cts.Cancel();
        try { _runTask?.Wait(2000); } catch (AggregateException) { /* cancellation */ }
        _cts.Dispose();
        _cts = null;
        _runTask = null;
    }

    void RunLoop(CancellationToken ct)
    {
        Stopwatch sw = Stopwatch.StartNew();
        Stopwatch tickWork = new Stopwatch();

        // 메트릭: 최근 ~1초 분량의 tick 소요시간 측정 (PRD: tick p99 < 10ms).
        // Phase 08: TickMetrics로 책임 분리 (SRP). avg/max만 → p50/p95/p99/max/avg.
        TickMetrics metrics = new TickMetrics();

        long intervalMs = Constants.TickIntervalMs;

        while (!ct.IsCancellationRequested)
        {
            long nextTargetMs = (_tickNumber + 1) * intervalMs;

            // 다음 tick 시각까지 대기. SpinWait.SpinUntil은 짧은 spin 후 Yield하므로
            // 헌법 #5의 "Sleep 절대 금지" 정신과 부합 + CPU 점유 낮음.
            SpinWait.SpinUntil(
                () => sw.ElapsedMilliseconds >= nextTargetMs || ct.IsCancellationRequested);

            if (ct.IsCancellationRequested) break;

            Interlocked.Increment(ref _tickNumber);

            // OnTick 호출 + 소요시간 측정.
            tickWork.Restart();
            try
            {
                _onTick(_tickNumber);
            }
            catch (Exception ex)
            {
                // 한 tick 실패해도 루프는 죽지 않게 (서버 가용성 우선).
                Console.WriteLine($"[Tick] #{_tickNumber} 콜백 예외: {ex}");
            }
            tickWork.Stop();

            long elapsedMicros = tickWork.ElapsedTicks * 1_000_000L / Stopwatch.Frequency;
            metrics.Record(elapsedMicros);

            // 버킷 가득 차면(= 1초 분량 = ServerTickRate 개) 메트릭 출력 + 리셋.
            if (metrics.IsBucketFull)
            {
                TickMetrics.Stats s = metrics.SnapshotAndReset();
                Console.WriteLine($"[Tick] #{_tickNumber} 1초 메트릭: {s.Format()}");
                OnMetricsSnapshot?.Invoke(s);
            }
        }
    }
}

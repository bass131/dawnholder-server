using Shared.GameData;

namespace Dawnholder.Server.GameServer.Loop;

// Phase 08 (M2): TickScheduler에서 측정 책임만 분리한 SRP 클래스.
//
// **왜 별도 클래스로 뽑았나** (Phase 07 InputBits.cs 패턴 정합):
// - TickScheduler는 "정확한 간격으로 콜백 부르기"가 본질.
// - 측정 + 통계 + 출력은 별개 책임. 섞으면 둘 다 테스트 어려워짐.
// - 분리하면 TickMetrics만 단독 xUnit 가능 (실제 tick 안 돌리고 알려진 입력으로).
//
// **왜 percentile?** (avg는 거짓말, max는 outlier 휘둘림)
// - avg=0.05ms 인데 사용자가 끊김 느낀다면? → 가끔 100ms 튄 tick이 avg에 묻힌 것.
// - p99 = "최악의 1%도 이 정도" — 진짜 체감 기준.
// - PRD 박힌 기준: tick p99 < 10ms.
//
// **알고리즘**: nearest-rank percentile (가장 단순, 학습 친화).
// - 100개 sample → 정렬 → p99 = 99번째 값.
// - 9개 sample → p99 = ceil(0.99 * 9) = 9번째 값 (= max).
// - HDR histogram 같은 고급 알고리즘은 오버킬 (1초당 20 sample뿐).
public class TickMetrics
{
    readonly List<long> _samples;
    public int BucketSize { get; }

    /// <param name="bucketSize">자동 출력 임계 (sample 개수). 기본 = 1초 분량 = ServerTickRate.</param>
    public TickMetrics(int bucketSize = Constants.ServerTickRate)
    {
        if (bucketSize <= 0) throw new ArgumentOutOfRangeException(nameof(bucketSize));
        BucketSize = bucketSize;
        _samples = new List<long>(capacity: bucketSize);
    }

    /// <summary>매 tick 호출. 단위 = 마이크로초 (microsecond).</summary>
    public void Record(long elapsedMicros)
    {
        _samples.Add(elapsedMicros);
    }

    /// <summary>버킷이 찼는지 (자동 출력 트리거 판정용).</summary>
    public bool IsBucketFull => _samples.Count >= BucketSize;

    /// <summary>현재 sample 개수.</summary>
    public int Count => _samples.Count;

    /// <summary>통계 계산 후 buffer 비움. 일정 간격으로 호출.</summary>
    public Stats SnapshotAndReset()
    {
        Stats s = Compute();
        _samples.Clear();
        return s;
    }

    /// <summary>현재 buffer 기준 통계 계산 (테스트용 — buffer 안 비움).</summary>
    public Stats Compute()
    {
        if (_samples.Count == 0) return Stats.Empty;

        long[] sorted = _samples.ToArray();
        Array.Sort(sorted);

        long sum = 0;
        for (int i = 0; i < sorted.Length; i++) sum += sorted[i];

        return new Stats(
            P50Ms: PercentileMs(sorted, 50),
            P95Ms: PercentileMs(sorted, 95),
            P99Ms: PercentileMs(sorted, 99),
            MaxMs: sorted[^1] / 1000.0,
            AvgMs: (sum / (double)sorted.Length) / 1000.0,
            Count: sorted.Length);
    }

    // nearest-rank: ceil(p/100 * N)번째 (1-indexed) → 0-indexed로 -1.
    static double PercentileMs(long[] sortedMicros, int p)
    {
        int idx = (int)Math.Ceiling(p / 100.0 * sortedMicros.Length) - 1;
        if (idx < 0) idx = 0;
        if (idx >= sortedMicros.Length) idx = sortedMicros.Length - 1;
        return sortedMicros[idx] / 1000.0;
    }

    public readonly record struct Stats(
        double P50Ms,
        double P95Ms,
        double P99Ms,
        double MaxMs,
        double AvgMs,
        int Count)
    {
        public static Stats Empty => new(0, 0, 0, 0, 0, 0);

        public string Format() =>
            $"p50={P50Ms:F2}ms p95={P95Ms:F2}ms p99={P99Ms:F2}ms max={MaxMs:F2}ms avg={AvgMs:F2}ms n={Count}";
    }
}

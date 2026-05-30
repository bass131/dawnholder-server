using Dawnholder.Server.GameServer.Loop;

namespace Dawnholder.Server.GameServer.Tests.Loop;

// TickMetrics 단독 테스트.
// 알려진 입력 → 알려진 percentile 출력. tick 루프 실제로 안 돌림 (분리의 가치).
public class TickMetricsTests
{
    [Fact]
    public void Empty_buffer_returns_Stats_Empty()
    {
        TickMetrics m = new TickMetrics();

        TickMetrics.Stats s = m.Compute();

        Assert.Equal(TickMetrics.Stats.Empty, s);
        Assert.Equal(0, s.Count);
    }

    [Fact]
    public void Single_sample_all_percentiles_equal_max()
    {
        // sample 1개라면 가장 작은 값 = 가장 큰 값. 모든 percentile이 같아야 함 (edge case).
        TickMetrics m = new TickMetrics();
        m.Record(5_000); // 5ms

        TickMetrics.Stats s = m.Compute();

        Assert.Equal(5.0, s.P50Ms);
        Assert.Equal(5.0, s.P95Ms);
        Assert.Equal(5.0, s.P99Ms);
        Assert.Equal(5.0, s.MaxMs);
        Assert.Equal(5.0, s.AvgMs);
        Assert.Equal(1, s.Count);
    }

    [Fact]
    public void All_same_value_yields_uniform_stats()
    {
        // 같은 값 100개. avg=p50=p95=p99=max 모두 동일.
        TickMetrics m = new TickMetrics(bucketSize: 100);
        for (int i = 0; i < 100; i++) m.Record(2_000); // 2ms

        TickMetrics.Stats s = m.Compute();

        Assert.Equal(2.0, s.P50Ms);
        Assert.Equal(2.0, s.P95Ms);
        Assert.Equal(2.0, s.P99Ms);
        Assert.Equal(2.0, s.MaxMs);
        Assert.Equal(2.0, s.AvgMs);
        Assert.Equal(100, s.Count);
    }

    [Fact]
    public void Linear_distribution_yields_expected_percentiles()
    {
        // input: 10, 20, 30, ..., 1000 μs (100개, 균등).
        // nearest-rank: ceil(p/100 * N) → 1-indexed.
        //   p50: ceil(0.5*100)=50 → 50번째 값 = 500μs = 0.50ms
        //   p95: ceil(0.95*100)=95 → 95번째 값 = 950μs = 0.95ms
        //   p99: ceil(0.99*100)=99 → 99번째 값 = 990μs = 0.99ms
        //   max: 1000μs = 1.00ms
        //   avg: (10+20+...+1000)/100 = 505μs = 0.505ms
        TickMetrics m = new TickMetrics(bucketSize: 100);
        for (int i = 1; i <= 100; i++) m.Record(i * 10L);

        TickMetrics.Stats s = m.Compute();

        Assert.Equal(0.50, s.P50Ms);
        Assert.Equal(0.95, s.P95Ms);
        Assert.Equal(0.99, s.P99Ms);
        Assert.Equal(1.00, s.MaxMs);
        Assert.Equal(0.505, s.AvgMs);
        Assert.Equal(100, s.Count);
    }

    [Fact]
    public void Outlier_does_not_drag_p50_or_p95_but_lifts_p99_and_max()
    {
        // 99개 정상(1ms) + 1개 outlier(100ms). avg는 거짓말, p99/max만 진실.
        TickMetrics m = new TickMetrics(bucketSize: 100);
        for (int i = 0; i < 99; i++) m.Record(1_000); // 1ms
        m.Record(100_000); // 100ms outlier

        TickMetrics.Stats s = m.Compute();

        Assert.Equal(1.0, s.P50Ms);       // 절반은 1ms
        Assert.Equal(1.0, s.P95Ms);       // 상위 5등도 1ms
        Assert.Equal(1.0, s.P99Ms);       // 상위 1등도 1ms (ceil(0.99*100)=99번째=1ms)
        Assert.Equal(100.0, s.MaxMs);     // max만 outlier
        Assert.True(s.AvgMs > 1.9 && s.AvgMs < 2.1); // avg≈1.99ms (1ms 평균이 outlier 1개에 끌려감)
    }

    [Fact]
    public void Snapshot_and_reset_clears_buffer()
    {
        TickMetrics m = new TickMetrics();
        m.Record(1_000);
        m.Record(2_000);
        Assert.Equal(2, m.Count);

        TickMetrics.Stats s = m.SnapshotAndReset();

        Assert.Equal(2, s.Count);    // snapshot에는 직전 값 들어있어야
        Assert.Equal(0, m.Count);    // 그 후 비워져야
    }

    [Fact]
    public void IsBucketFull_transitions_after_threshold()
    {
        TickMetrics m = new TickMetrics(bucketSize: 3);

        Assert.False(m.IsBucketFull);
        m.Record(100); Assert.False(m.IsBucketFull);
        m.Record(200); Assert.False(m.IsBucketFull);
        m.Record(300); Assert.True(m.IsBucketFull); // 임계 달성

        m.SnapshotAndReset();
        Assert.False(m.IsBucketFull);                // 리셋 후 다시 false
    }

    [Fact]
    public void Constructor_rejects_non_positive_bucket_size()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TickMetrics(bucketSize: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TickMetrics(bucketSize: -1));
    }
}

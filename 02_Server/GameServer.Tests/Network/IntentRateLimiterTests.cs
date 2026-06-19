using Dawnholder.Server.GameServer.Sessions;

namespace GameServer.Tests.Network;

/// <summary>
/// IntentRateLimiter 단독 테스트.
///
/// **이 테스트의 핵심 이득**: 추출 전에는 rate-limit 로직을 검증하려면 GameSession 전체
/// (socket lifecycle + handshake state + GameMap + tick 처리)를 세워야 했음.
/// IntentRateLimiter로 추출된 후, socket 없이 이 클래스만 격리해서 검증 가능.
///
/// **trust-boundary invariant 보존 근거**:
///   1. 임계값(500/s)은 IntentRateLimiter.LimitPerSecond 상수 단일 진실 공급원.
///   2. GameSession.SubmitMoveIntent는 TryConsume() 결과만 보고 drop 여부 결정.
///   3. 동일 입력(호출 횟수 + 시간 경과) → 동일 거부/허용 판정 (헌법 #3 정합).
///
/// **검증 3종**:
///   - 윈도우 갱신: 1초 경과 후 카운트 리셋 → 다시 통과
///   - 임계 초과: 501번째 TryConsume → false 반환
///   - 첫 경고 1회: 윈도우 내 임계 초과 시 firstWarn=true는 최초 1회만
/// </summary>
public class IntentRateLimiterTests
{
    // ── 1. 임계 이내: 모두 통과 ─────────────────────────────────────────

    [Fact]
    public void ExactlyAtLimit_AllPass()
    {
        // 500번까지는 정확히 임계이므로 모두 통과해야 함.
        IntentRateLimiter limiter = new();

        int passCount = 0;
        for (int i = 0; i < IntentRateLimiter.LimitPerSecond; i++)
        {
            if (limiter.TryConsume(out _)) passCount++;
        }

        Assert.Equal(IntentRateLimiter.LimitPerSecond, passCount);
    }

    // ── 2. 임계 초과: 501번째부터 drop ───────────────────────────────────

    [Fact]
    public void OverLimit_501st_Dropped()
    {
        IntentRateLimiter limiter = new();

        // 500번 통과
        for (int i = 0; i < IntentRateLimiter.LimitPerSecond; i++)
            limiter.TryConsume(out _);

        // 501번째: drop
        bool result = limiter.TryConsume(out _);
        Assert.False(result);
    }

    [Fact]
    public void OverLimit_MultipleDrops_AllReturnFalse()
    {
        IntentRateLimiter limiter = new();

        for (int i = 0; i < IntentRateLimiter.LimitPerSecond; i++)
            limiter.TryConsume(out _);

        // 501~510: 모두 drop
        for (int i = 0; i < 10; i++)
        {
            bool result = limiter.TryConsume(out _);
            Assert.False(result);
        }
    }

    // ── 3. 첫 경고 1회 invariant ─────────────────────────────────────────

    [Fact]
    public void FirstWarn_OnlyOnFirstExceedance_InWindow()
    {
        // 윈도우 내 임계 초과 시 firstWarn=true는 정확히 1회만 발생.
        // oscillation attack 방지를 위해 카운트는 계속 증가하지만 경고는 1회.
        IntentRateLimiter limiter = new();

        for (int i = 0; i < IntentRateLimiter.LimitPerSecond; i++)
            limiter.TryConsume(out _);

        // 501번째: drop + firstWarn=true (첫 경고)
        bool first = limiter.TryConsume(out bool firstWarn501);
        Assert.False(first);
        Assert.True(firstWarn501);

        // 502~510: drop이지만 firstWarn=false (이미 이 윈도우에서 경고 발생)
        for (int i = 0; i < 10; i++)
        {
            limiter.TryConsume(out bool warnSubsequent);
            Assert.False(warnSubsequent);
        }
    }

    [Fact]
    public void NoWarn_WhenUnderLimit()
    {
        // 통과하는 경우에는 firstWarn이 항상 false
        IntentRateLimiter limiter = new();

        for (int i = 0; i < IntentRateLimiter.LimitPerSecond; i++)
        {
            limiter.TryConsume(out bool warn);
            Assert.False(warn); // 통과 시 경고 없음
        }
    }

    // ── 4. 윈도우 갱신: 1초 경과 후 리셋 ────────────────────────────────

    [Fact]
    public void WindowReset_After1Second_CounterResets()
    {
        // 1초 경과 후 카운트가 리셋되어 다시 통과할 수 있어야 함.
        // Thread.Sleep 없이 테스트하기 위해 내부 구현이 ElapsedMilliseconds 기준임을 이용.
        // → 실제 1.1초 대기 (GameSessionRateLimitTests.Case_J 패턴 정합).
        IntentRateLimiter limiter = new();

        // 윈도우 1: 임계 초과
        for (int i = 0; i < IntentRateLimiter.LimitPerSecond + 100; i++)
            limiter.TryConsume(out _);

        // 윈도우 1에서 임계 초과 확인
        bool dropInWindow1 = !limiter.TryConsume(out _);
        // (limiter 상태: countInWindow > 500, loggedThisWindow=true)

        // 1.1초 대기 → 다음 TryConsume에서 윈도우 리셋
        Thread.Sleep(1100);

        // 윈도우 2: 다시 처음부터 시작 → 통과
        bool passAfterReset = limiter.TryConsume(out bool warnAfterReset);
        Assert.True(passAfterReset); // 윈도우 리셋 후 첫 호출은 통과
        Assert.False(warnAfterReset); // 리셋 후 통과이므로 경고 없음
    }

    [Fact]
    public void WindowReset_FirstWarnResetsPerWindow()
    {
        // 윈도우 리셋 후 새 윈도우에서 임계 초과 시 firstWarn이 다시 true.
        // 두 윈도우 각각 1번씩 경고 = 총 2번.
        IntentRateLimiter limiter = new();

        // 윈도우 1: 임계 초과 → 첫 경고 발생
        for (int i = 0; i < IntentRateLimiter.LimitPerSecond; i++)
            limiter.TryConsume(out _);

        limiter.TryConsume(out bool warn1);
        Assert.True(warn1); // 윈도우 1의 첫 경고

        // 윈도우 1에서 추가 경고 없음
        limiter.TryConsume(out bool warnExtra);
        Assert.False(warnExtra);

        // 1.1초 대기 → 윈도우 리셋
        Thread.Sleep(1100);

        // 윈도우 2: 다시 임계 초과 → 새 윈도우에서 첫 경고 발생
        for (int i = 0; i < IntentRateLimiter.LimitPerSecond; i++)
            limiter.TryConsume(out _);

        limiter.TryConsume(out bool warn2);
        Assert.True(warn2); // 윈도우 2의 첫 경고

        // 윈도우 2에서 추가 경고 없음
        limiter.TryConsume(out bool warnExtra2);
        Assert.False(warnExtra2);
    }
}

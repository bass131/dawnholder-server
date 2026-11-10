namespace Dawnholder.Server.GameServer.Sessions;

/// <summary>
/// 연결이 소유하는 입력 예산 하나의 1초 fixed window. 수신 흐름에서만 소비한다.
/// 기본값은 기존 이동 예산이며, 다른 종류는 GameSession에서 한도를 지정한다.
///
/// **Fixed window vs sliding window**: fixed window는 윈도우 경계에서 2× burst를 허용할 수 있음
/// (윈도우 직전 500 + 직후 500 = 1초 내 1000). 현재 게임 로직에선 이 edge가 큰 문제가 안 되고
/// 구현이 단순해서 fixed 유지. 정확한 rate-limit이 필요하면 sliding window로 교체.
/// </summary>
internal sealed class IntentRateLimiter
{
    // 임계값: 1초 윈도우 내 허용 intent 수.
    // 240Hz 모니터 사용자의 정상 wire rate ~300-500/s 기준.
    public const int LimitPerSecond = 500;

    readonly TimeProvider _timeProvider;
    readonly int _limit;
    long _windowStart;
    int _countInWindow;
    bool _loggedThisWindow;

    public IntentRateLimiter(int limit = LimitPerSecond, TimeProvider? timeProvider = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);
        _limit = limit;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _windowStart = _timeProvider.GetTimestamp();
    }

    public int Limit => _limit;

    /// <summary>
    /// intent 소비 시도.
    /// </summary>
    /// <param name="firstWarn">
    /// 이 호출에서 처음으로 임계 초과 경고를 내야 하면 true (윈도우당 1회 로그 보장).
    /// 통과하거나 이미 경고한 창이면 false.
    /// </param>
    /// <returns>true = 통과 (intent 처리 허용) / false = 임계 초과, drop해야 함</returns>
    public bool TryConsume(out bool firstWarn)
    {
        // 1초 경과 시 윈도우 리셋
        long now = _timeProvider.GetTimestamp();
        if (_timeProvider.GetElapsedTime(_windowStart, now) >= TimeSpan.FromSeconds(1))
        {
            _windowStart = now;
            _countInWindow = 0;
            _loggedThisWindow = false;
        }

        // 상한에 머물러 초과 요청이 카운터를 overflow시켜 다시 통과하지 못하게 한다.
        if (_countInWindow >= _limit)
        {
            // 임계 초과 → drop. 윈도우당 첫 1회만 경고 플래그 세팅.
            if (!_loggedThisWindow)
            {
                _loggedThisWindow = true;
                firstWarn = true;
            }
            else
            {
                firstWarn = false;
            }
            return false;
        }

        _countInWindow++;
        firstWarn = false;
        return true;
    }
}

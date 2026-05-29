using System.Diagnostics;

namespace Dawnholder.Server.GameServer.Network;

/// <summary>
/// 플레이어 intent(C_MoveIntent) 속도 제한 — 1초 fixed 윈도우.
///
/// **추출 사유**: GameSession에서 rate-limit 윈도우 로직을 분리해 socket 없이 단독 테스트 가능.
/// 추출 전에는 GameSession 전체(socket lifecycle + handshake state + map 마샬링)를 세워야
/// 이 로직을 검증할 수 있었음.
///
/// **trust-boundary invariant 보존**: 추출 후에도 동일 입력에 동일 거부/허용 결과.
/// 임계값(500/s), 윈도우 갱신, 첫 경고 1회 로직은 byte-for-byte 동일하게 이전됨.
///
/// **헌법 #3 정합**: 임계 초과 intent → TryConsume()이 false 반환 → 호출자(GameSession)가 drop.
/// 카운트는 임계 이상이어도 계속 증가 (oscillation attack 방지).
///
/// **Fixed window vs sliding window**: fixed window는 윈도우 경계에서 2× burst를 허용할 수 있음
/// (윈도우 직전 500 + 직후 500 = 1초 내 1000). 현재 게임 로직에선 이 edge가 큰 문제가 안 되고
/// 구현이 단순해서 fixed 유지. 정확한 rate-limit이 필요하면 sliding window로 교체.
/// </summary>
internal sealed class IntentRateLimiter
{
    // 임계값: 1초 윈도우 내 허용 intent 수.
    // 240Hz 모니터 사용자의 정상 wire rate ~300-500/s 기준. M4.3R Phase 04 추출 후에도 동일 상수.
    public const int LimitPerSecond = 500;

    readonly Stopwatch _window = Stopwatch.StartNew();
    int _countInWindow;
    bool _loggedThisWindow;

    /// <summary>
    /// intent 소비 시도.
    /// </summary>
    /// <param name="firstWarn">
    /// 이 호출에서 처음으로 임계 초과 경고를 내야 하면 true (윈도우당 1회 로그 보장).
    /// drop이 아닌 경우(통과 또는 이미 경고 박힌 경우) false.
    /// </param>
    /// <returns>true = 통과 (intent 처리 허용) / false = 임계 초과, drop해야 함</returns>
    public bool TryConsume(out bool firstWarn)
    {
        // 1초 경과 시 윈도우 리셋
        if (_window.ElapsedMilliseconds >= 1000)
        {
            _window.Restart();
            _countInWindow = 0;
            _loggedThisWindow = false;
        }

        _countInWindow++;

        if (_countInWindow > LimitPerSecond)
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

        firstWarn = false;
        return true;
    }
}

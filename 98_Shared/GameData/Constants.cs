namespace Shared.GameData;

/// <summary>
/// 게임 전역 상수. 클라/서버 양쪽이 같은 값을 봐야 하는 모든 것.
/// 이 주석이 Unity 측에서 F12 시 그대로 보여야 한다 (ADR-010 검증).
/// </summary>
public static class Constants
{
    /// <summary>서버 시뮬레이션 틱 레이트 (TPS). ADR-004 참조.</summary>
    public const int ServerTickRate = 20;

    /// <summary>틱 간격 (밀리초). 1000 / ServerTickRate.</summary>
    public const int TickIntervalMs = 1000 / ServerTickRate;
}

namespace Shared.GameData;

/// <summary>
/// 게임 전역 상수. 클라/서버 양쪽이 같은 값을 봐야 하는 모든 것.
/// 이 주석이 Unity 측에서 F12 시 그대로 보여야 한다 (ADR-010 검증).
///
/// **헌법 #1 (Server Authority) 정합 규칙**: 이동/물리/공식 상수는 *오직 여기*에서만
/// 정의. 클라가 별도 const 박으면 prediction(Phase 05+) 도입 즉시 무한 drift.
/// </summary>
public static class Constants
{
    /// <summary>서버 시뮬레이션 틱 레이트 (TPS). ADR-004 참조.</summary>
    public const int ServerTickRate = 20;

    /// <summary>틱 간격 (밀리초). 1000 / ServerTickRate.</summary>
    public const int TickIntervalMs = 1000 / ServerTickRate;

    /// <summary>
    /// 틱 간격 (초, float). 서버 시뮬레이션의 dt 단일 출처.
    /// 클라 prediction(Phase 05+)도 같은 값으로 적분해야 drift 0.
    /// </summary>
    public const float TickDuration = 1.0f / ServerTickRate;

    /// <summary>
    /// 캐릭터 좌우 이동 속도 (units/sec). Phase 04 도입.
    /// 클라/서버 단일 출처 — 다르면 prediction 즉시 깨짐.
    /// </summary>
    public const float MoveSpeed = 5.0f;

    /// <summary>
    /// S_Snapshot 브로드캐스트 주기 (tick 단위). 2 tick = 100ms (10Hz).
    /// 너무 길면 클라 보간 buffer가 빔 → last-known 정지 패턴으로 어색해짐.
    /// </summary>
    public const int SnapshotTickInterval = 2;

    /// <summary>
    /// 단일 패킷 frame 최대 크기 (byte). PacketSession.OnRecv가 length 헤더 상한으로
    /// 사용 — 초과 시 fail-closed disconnect. 헌법 #3 (Trust Boundary) 코드 실현의 핵심 상수.
    /// </summary>
    public const int MaxPacketSize = 4096;
}

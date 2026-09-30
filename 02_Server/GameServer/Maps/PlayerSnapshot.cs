using System.Numerics;
using Shared.GameData;

namespace Dawnholder.Server.GameServer.Maps;

/// <summary>
/// 월드 tick thread에서 캡처한 플레이어 값과 불변 기본 정의 참조.
/// 캡처 뒤 entity 변경은 이 값에 반영되지 않는다. DB 식별자·저장 큐·복원 형식은 정의하지 않는다.
/// CaptureSnapshot이 생성한 값의 계약이며 임의 default struct의 유효성을 보장하지 않는다.
/// </summary>
public readonly record struct PlayerSnapshot
{
    /// <summary>캡처 당시 서버 entity ID.</summary>
    public int EntityId { get; init; }

    /// <summary>캡처 당시 서버 권위 좌표.</summary>
    public Vector2 Position { get; init; }

    /// <summary>캡처 당시 현재 전투 HP. 음수 또는 최대 초과 값도 보정하지 않는다.</summary>
    public int CurrentHp { get; init; }

    /// <summary>캡처 당시 entity의 현재 최대 HP. 기본 정의 Stats.MaxHp와 별개다.</summary>
    public int MaxHp { get; init; }

    /// <summary>캡처한 entity와 공유하는 불변 클래스 기본 정의.</summary>
    public PlayerStats Stats { get; init; }
}

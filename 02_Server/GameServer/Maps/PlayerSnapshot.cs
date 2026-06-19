using System.Numerics;
using Shared.GameData;

namespace Dawnholder.Server.GameServer.Maps;

/// <summary>
/// 플레이어의 "저장 가능 상태" 청사진 — M8 영속화가 DB 엔티티로 변환할 DTO(Data Transfer Object).
///
/// 담기는 것: 재접속 후 복원해야 할 권위 상태만.
/// 담기지 않는 것: 입력 큐·lag comp ring·FSM transient·물리 런타임 등 매 틱 재계산 가능한 휘발 상태.
///
/// 왜 record struct인가?
///   - readonly: 생성 후 불변 — 스냅샷이 write queue를 통해 백그라운드 스레드로 넘어갈 때 race 없음.
///   - struct: 힙 할당 없음. 스냅샷이 20TPS로 큐에 쌓이더라도 GC 압력 최소.
///   - record: 값 동등성(Value equality) 무료 제공 — 테스트에서 Assert.Equal 직접 사용 가능.
/// </summary>
public readonly record struct PlayerSnapshot
{
    /// <summary>플레이어 서버 권위 ID. DB의 primary key와 대응.</summary>
    public int EntityId { get; init; }

    /// <summary>마지막 서버 권위 좌표. 재접속 시 스폰 위치 기준.</summary>
    public Vector2 Position { get; init; }

    /// <summary>현재 전투 HP. Stats.MaxHp와 별개로 전투 중 독립 mutate됨.</summary>
    public int Hp { get; init; }

    /// <summary>현재 최대 HP. 스탯 변경(레벨업 등) 시 Stats.MaxHp와 동기화 대상.</summary>
    public int MaxHp { get; init; }

    /// <summary>
    /// 캐릭터 클래스 + 기본 스탯 묶음.
    /// M8이 DB 저장 시: Class byte만 저장하고 로드 시 PlayerStats.ForClass()로 재생성하거나
    /// 스탯을 컬럼별로 직렬화하는 방식 중 하나를 선택 — PlayerSnapshot은 양쪽 모두 수용.
    /// </summary>
    public PlayerStats Stats { get; init; }
}

using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Combat;

/// <summary>
/// 플레이어 캐릭터 클래스별 기본 스탯 컨테이너 (M3.8 Phase 03 — 캡스톤 1 데모용).
///
/// **헌법 #1 (Server Authority)**: 클라이언트는 `CharacterClass` byte만 전송. 서버가 본 클래스로
/// PlayerStats를 생성 — 스탯 수치를 클라가 직접 보내는 경로 없음.
///
/// **응급 단순화**: 수치는 하드코딩된 factory 메서드 (Warrior/Ranger). M4.1 Phase 02에서
/// `98_Shared/GameData/Formulas.cs` 도입 시 본 factory가 Formulas.cs 호출로 교체 예정.
///
/// **`init` setter**: 생성 후 불변 스탯(MaxHp/Attack/Defense/MoveSpeed)은 `init` 강제.
/// Hp만 `set` — 전투 중 감소 허용.
/// </summary>
public sealed class PlayerStats
{
    public CharacterClass Class { get; init; }
    public int Hp { get; set; }
    public int MaxHp { get; init; }
    public int Attack { get; init; }
    public int Defense { get; init; }
    public float MoveSpeed { get; init; }

    // M3.8 Phase 03: 전사 — 고체력/고방어/저속. 근접 탱커 컨셉.
    // M4.1 Phase 02 예정: Formulas.cs 이동 + 이 팩토리를 래퍼로 전환.
    public static PlayerStats Warrior() => new()
    {
        Class = CharacterClass.Warrior,
        Hp = 150,
        MaxHp = 150,
        Attack = 15,
        Defense = 5,
        MoveSpeed = 4f,
    };

    // M3.8 Phase 03: 원거리 — 저체력/저방어/고속. 기동형 딜러 컨셉.
    public static PlayerStats Ranger() => new()
    {
        Class = CharacterClass.Ranger,
        Hp = 80,
        MaxHp = 80,
        Attack = 12,
        Defense = 2,
        MoveSpeed = 6f,
    };
}

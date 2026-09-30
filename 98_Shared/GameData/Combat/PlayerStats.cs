using Shared.Protocol;

namespace Shared.GameData;

/// <summary>
/// 플레이어 캐릭터 클래스별 기본 스탯 컨테이너.
///
/// **98_Shared 위치 이유**: Formulas.cs가 PlayerStats를 직접 참조하므로 양쪽(서버/클라)이
/// 같은 정의를 컴파일해야 함 (헌법 #4 Shared Code Discipline).
///
/// **헌법 #1 (Server Authority)**: 클라이언트는 <c>CharacterClass</c> byte만 전송.
/// 서버가 본 클래스로 PlayerStats를 생성 — 스탯 수치를 클라가 직접 보내는 경로 없음.
/// 클라는 Formulas.ComputeDamage를 hint 표시용으로만 호출 가능. HP 감소는 서버만.
///
/// 모든 값은 생성 후 불변인 기본 정의다. InitialHp는 생성 시의 HP이며 현재 전투 HP는
/// 서버 PlayerEntity가 별도로 소유한다.
/// </summary>
public sealed class PlayerStats
{
    private PlayerStats(CharacterClass cls, int initialHp, int maxHp, int attack, int defense, float moveSpeed, float jumpVel)
    {
        Class = cls;
        InitialHp = initialHp;
        MaxHp = maxHp;
        Attack = attack;
        Defense = defense;
        MoveSpeed = moveSpeed;
        JumpVel = jumpVel;
    }

    public CharacterClass Class { get; }
    public int InitialHp { get; }
    public int MaxHp { get; }
    public int Attack { get; }
    public int Defense { get; }
    public float MoveSpeed { get; }
    public float JumpVel { get; }

    // 전사 — 고체력/고방어/저속. 근접 탱커 컨셉.
    public static PlayerStats Knight()
        => new(CharacterClass.Knight, initialHp: 150, maxHp: 150, attack: 15, defense: 5, moveSpeed: 4f, jumpVel: 8f);

    // 원거리 — 저체력/저방어/고속. 기동형 딜러 컨셉.
    public static PlayerStats Mage()
        => new(CharacterClass.Mage, initialHp: 80, maxHp: 80, attack: 12, defense: 2, moveSpeed: 6f, jumpVel: 8f);

    // 클래스 → 스탯 매핑 단일 출처. invalid byte도 Knight fallback — 서버/클라 동일 fallback 약속
    // (헌법 #3: 클라가 보낸 class byte는 untrusted — 범위 밖 값도 안전한 기본값으로 수렴).
    public static PlayerStats ForClass(CharacterClass cls)
        => cls == CharacterClass.Mage ? Mage() : Knight();
}

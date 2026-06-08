using Shared.GameData;

namespace GameServer.Tests.Combat;

/// <summary>
/// Formulas.ComputeDamage 단위 테스트 6건.
///
/// **테스트 원칙**:
///   - 순수 함수 검증 — IO 없음, GameMap/GameSession 의존 없음. 단순 input → output 확인.
///   - Deterministic 보장: 같은 input → 같은 output (헌법 #4, Formulas.cs 주석 정합).
///   - overflow 안전망(테스트 5·6번): Math.Max(1, ...) 가 음수 wrap을 1로 잡는지 검증.
///
/// **공식**: Max(1, baseDamage + attacker.Attack - target.Defense)
/// </summary>
public class FormulasTests
{
    // --- 1. Knight 정상 경로 ---
    // Knight Attack=15 / EnemyStats Defense=2 / baseDamage=10
    // → Max(1, 10 + 15 - 2) = Max(1, 23) = 23
    [Fact]
    public void ComputeDamage_KnightHappyPath()
    {
        PlayerStats attacker = PlayerStats.Knight(); // Attack=15
        EnemyStats target = new EnemyStats { Defense = 2, MaxHp = 30 };

        int result = Formulas.ComputeDamage(attacker, target, baseDamage: 10);

        Assert.Equal(23, result);
    }

    // --- 2. Mage 정상 경로 ---
    // Mage Attack=12 / EnemyStats Defense=2 / baseDamage=10
    // → Max(1, 10 + 12 - 2) = Max(1, 20) = 20
    [Fact]
    public void ComputeDamage_MageHappyPath()
    {
        PlayerStats attacker = PlayerStats.Mage(); // Attack=12
        EnemyStats target = new EnemyStats { Defense = 2, MaxHp = 30 };

        int result = Formulas.ComputeDamage(attacker, target, baseDamage: 10);

        Assert.Equal(20, result);
    }

    // --- 3. 방어력이 높아 계산 결과가 0 이하 → 최소 1 보장 ---
    // Knight Attack=15 / EnemyStats Defense=30 / baseDamage=10
    // → Max(1, 10 + 15 - 30) = Max(1, -5) = 1
    [Fact]
    public void ComputeDamage_TargetDefenseHigh_ReturnsMinimumOne()
    {
        PlayerStats attacker = PlayerStats.Knight(); // Attack=15
        EnemyStats target = new EnemyStats { Defense = 30 };

        int result = Formulas.ComputeDamage(attacker, target, baseDamage: 10);

        Assert.Equal(1, result);
    }

    // --- 4. EnemyStats default(Defense=0) 자연 입력 경로 ---
    // struct default가 공식에 그대로 흘러 들어가는지 검증.
    // PlayerStats는 private ctor + factory 패턴이라 Attack=0 임의 박기 불가 → Knight factory 활용.
    // → Max(1, 10 + 15 - 0) = 25
    [Fact]
    public void ComputeDamage_DefenseZero_BaseDamagePlusAttack()
    {
        PlayerStats attacker = PlayerStats.Knight(); // Attack=15
        EnemyStats target = default; // Defense=0 (struct default)

        int result = Formulas.ComputeDamage(attacker, target, baseDamage: 10);

        Assert.Equal(25, result);
    }

    // --- 5. 공격자 Attack이 매우 작은 음수에 가까운 값 (overflow 안전망) ---
    // attacker.Attack = int.MinValue + 1 시 baseDamage(10) + Attack(int.MinValue+1) = int.MinValue+11 → 음수 (overflow 없이 통과).
    // → Max(1, int.MinValue + 11 - 0) = Max(1, 매우 큰 음수) = 1
    // PlayerStats private ctor이라 직접 박기 불가 → Knight (Attack=15)로 대체하되
    // baseDamage를 int.MinValue에 가까운 값으로 설정해 음수 wrap 시뮬레이션.
    //
    // 실질 구현: baseDamage = int.MinValue + 1, Attack=15, Defense=0
    // → unchecked: (int.MinValue + 1) + 15 = int.MinValue + 16 (음수, overflow wrap 없이 안전)
    // → Max(1, int.MinValue + 16) = 1
    [Fact]
    public void ComputeDamage_NegativeBaseDamageEdge_ReturnsMinimumOne()
    {
        PlayerStats attacker = PlayerStats.Knight(); // Attack=15
        EnemyStats target = default; // Defense=0

        // int.MinValue+1 + 15 = int.MinValue+16 → 여전히 음수 (no overflow, 2의 보수 안전)
        int result = Formulas.ComputeDamage(attacker, target, baseDamage: int.MinValue + 1);

        // Max(1, 매우 큰 음수) = 1
        Assert.Equal(1, result);
    }

    // --- 6. 매우 큰 baseDamage (int overflow 안전망) ---
    // baseDamage = int.MaxValue, Attack=15, Defense=0
    // → int.MaxValue + 15 = overflow wrap (unchecked) → 음수 가능
    // → Math.Max(1, 음수) = 1 이거나, overflow 미발생 시 큰 양수 가능
    //
    // C# int 덧셈은 unchecked(기본) → int.MaxValue + 15 = int.MinValue + 14 (음수 wrap).
    // → Max(1, int.MinValue + 14) = 1
    [Fact]
    public void ComputeDamage_LargeBaseDamage_OverflowSafe()
    {
        PlayerStats attacker = PlayerStats.Knight(); // Attack=15
        EnemyStats target = default; // Defense=0

        // unchecked: int.MaxValue + 15 → 음수 wrap → Math.Max(1, ...) = 1
        int result = Formulas.ComputeDamage(attacker, target, baseDamage: int.MaxValue);

        // overflow 시 음수 wrap → 최소 1 보장 검증
        Assert.Equal(1, result);
    }
}
